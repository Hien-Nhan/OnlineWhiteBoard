using System;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using WhiteboardClient.Models;
using WhiteboardClient.Services;
using WhiteboardShared;
using Color = Windows.UI.Color;

namespace WhiteboardClient.Views;

/// <summary>
/// Phần bắt và xử lý sự kiện thiết bị trỏ (Chuột, bút cảm ứng, màn hình chạm),
/// quy đổi tọa độ màn hình về tọa độ logic (1280 x 720) và phát gói tin vẽ thời gian thực.
/// Phụ trách: Thành viên A (Hạng mục T7).
/// </summary>
public sealed partial class BoardPage
{
    /// <summary>
    /// Quy đổi tọa độ điểm trên vùng chứa Surface sang tọa độ nguyên trên bảng logic (0..1279, 0..719).
    /// Có khóa cận dưới cho hệ số tỷ lệ để chống lỗi chia cho 0 khi cửa sổ chưa hoàn tất bố cục.
    /// </summary>
    private BoardPoint ToBoard(Point p)
    {
        UpdateTransform();
        float safeScale = Math.Max(0.05f, _scale);

        int x = (int)Math.Round((p.X - _offX) / safeScale);
        int y = (int)Math.Round((p.Y - _offY) / safeScale);

        x = Math.Max(0, Math.Min(BoardW - 1, x));
        y = Math.Max(0, Math.Min(BoardH - 1, y));
        return new BoardPoint(x, y);
    }

    private void Surface_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_closed || _isDrawing || _isDrawingShape || _isPanning) return;

        PointerPoint pp = e.GetCurrentPoint(Surface);
        PointerPointProperties props = pp.Properties;

        // Kéo di chuyển bảng: chuột phải, chuột giữa, hoặc chuột trái khi đang chọn công cụ "Di chuyển bảng"
        if (props.IsMiddleButtonPressed || props.IsRightButtonPressed ||
            (props.IsLeftButtonPressed && _tool == DrawTool.Pan))
        {
            _isPanning = true;
            _activePointerId = e.Pointer.PointerId;
            _panPointerStart = pp.Position;
            _panStartX = _panX;
            _panStartY = _panY;
            Surface.CapturePointer(e.Pointer);
            e.Handled = true;
            return;
        }

        if (!props.IsLeftButtonPressed || _connectionLost || _board == null) return;

        _activePointerId = e.Pointer.PointerId;
        Surface.CapturePointer(e.Pointer);
        BoardPoint pt = ToBoard(pp.Position);

        if (_tool == DrawTool.Pen || _tool == DrawTool.Eraser)
        {
            _isDrawing = true;
            _lastSentPoint = pt;
            DrawFreehandAndSend(pt, pt); // Vẽ ngay một chấm tròn tại vị trí bắt đầu bấm chuột
        }
        else
        {
            _isDrawingShape = true;
            _shapeStart = pt;
            _shapeEnd = pt;
        }

        e.Handled = true;
    }

    private void Surface_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_closed) return;

        // Nếu đang vẽ/kéo bằng một ngón tay hoặc bút, bỏ qua các điểm chạm phụ phát sinh thêm
        if (_activePointerId.HasValue && e.Pointer.PointerId != _activePointerId.Value) return;

        PointerPoint pp = e.GetCurrentPoint(Surface);

        if (_isPanning)
        {
            _panX = _panStartX + (pp.Position.X - _panPointerStart.X);
            _panY = _panStartY + (pp.Position.Y - _panPointerStart.Y);
            ClampPan();
            BoardCanvas.Invalidate();
            e.Handled = true;
            return;
        }

        BoardPoint pt = ToBoard(pp.Position);
        CoordText.Text = $"X: {pt.X}, Y: {pt.Y}";

        if (_isDrawing)
        {
            // Lọc khoảng cách: chỉ vẽ và gửi gói tin khi con trỏ di chuyển tối thiểu 3 pixel (dx^2 + dy^2 >= 9)
            if (pt.DistanceSquaredTo(_lastSentPoint) >= 9)
            {
                DrawFreehandAndSend(_lastSentPoint, pt);
                _lastSentPoint = pt;
            }
            e.Handled = true;
        }
        else if (_isDrawingShape)
        {
            _shapeEnd = pt;
            BoardCanvas.Invalidate();
            e.Handled = true;
        }
    }

    private void Surface_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_closed) return;
        if (!_isDrawing && !_isDrawingShape && !_isPanning) return;
        if (_activePointerId.HasValue && e.Pointer.PointerId != _activePointerId.Value) return;

        BoardPoint end = ToBoard(e.GetCurrentPoint(Surface).Position);

        // QUAN TRỌNG: Chốt nét vẽ TRƯỚC, rồi mới nhả CapturePointer.
        // ReleasePointerCapture sẽ kích hoạt sự kiện PointerCaptureLost ngay lập tức; nếu nhả trước,
        // hình khối đang kéo dở sẽ bị coi là mất capture đột ngột trước khi kịp hoàn tất.
        FinishStroke(end);
        Surface.ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    // Mất capture bất ngờ (đổi cửa sổ bằng Alt+Tab, thông báo hệ thống đè lên): giữ lại phần đã vẽ tới điểm cuối
    private void Surface_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        if (_activePointerId.HasValue && e.Pointer.PointerId != _activePointerId.Value) return;
        FinishStroke(null);
    }

    // Hệ thống hủy thao tác trỏ (ví dụ cử chỉ vuốt hệ thống): hủy hình đang kéo dở
    private void Surface_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (_activePointerId.HasValue && e.Pointer.PointerId != _activePointerId.Value) return;
        FinishStroke(null, commit: false);
    }

    /// <summary>
    /// Kết thúc phiên vẽ/kéo hiện tại một cách an toàn (Idempotent — gọi lặp lại nhiều lần không gây tác dụng phụ).
    /// </summary>
    private void FinishStroke(BoardPoint? end, bool commit = true)
    {
        _activePointerId = null;

        if (_isPanning)
        {
            _isPanning = false;
            return;
        }

        if (_isDrawing)
        {
            _isDrawing = false;
            if (commit && end is BoardPoint last && last != _lastSentPoint)
            {
                DrawFreehandAndSend(_lastSentPoint, last);
            }
        }
        else if (_isDrawingShape)
        {
            _isDrawingShape = false;
            if (end is BoardPoint last)
            {
                _shapeEnd = last;
            }

            if (commit && _shapeStart != _shapeEnd)
            {
                DrawShapeOnBoard(_tool, _shapeStart, _shapeEnd, _color, _brush);

                ShapeKind kind = _tool switch
                {
                    DrawTool.Rectangle => ShapeKind.Rectangle,
                    DrawTool.Circle => ShapeKind.Circle,
                    _ => ShapeKind.Line
                };
                SendDraw(DrawPacket.CreateShape(
                    kind,
                    _shapeStart.X, _shapeStart.Y,
                    _shapeEnd.X, _shapeEnd.Y,
                    HexColor.ToHex(_color),
                    _brush,
                    _session.RoomId));
            }
            BoardCanvas.Invalidate();
        }
    }

    private void DrawFreehandAndSend(BoardPoint a, BoardPoint b)
    {
        Color color = _tool == DrawTool.Eraser ? Colors.White : _color;
        int size = _tool == DrawTool.Eraser ? _brush * 4 : _brush;

        DrawSegmentOnBoard(a.X, a.Y, b.X, b.Y, color, size);
        SendDraw(DrawPacket.CreateFreehand(a.X, a.Y, b.X, b.Y, HexColor.ToHex(color), size, _session.RoomId));
        BoardCanvas.Invalidate();
    }

    private void SendDraw(DrawPacket packet)
    {
        if (_closed) return;
        if (!NetworkClient.SendMessage(packet.Serialize()))
        {
            MarkConnectionLost();
        }
    }
}
