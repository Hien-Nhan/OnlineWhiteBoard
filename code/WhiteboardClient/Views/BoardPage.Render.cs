using System;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Windows.Foundation;
using Color = Windows.UI.Color;

namespace WhiteboardClient.Views;

/// <summary>
/// Phần hiển thị và kết xuất đồ họa Win2D lên bảng vẽ logic (1280 x 720) và CanvasControl.
/// Phụ trách: Thành viên A (Hạng mục T7).
/// </summary>
public sealed partial class BoardPage
{
    private void BoardCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        CanvasRenderTarget? board = _board;
        if (_closed || board == null) return;

        try
        {
            CanvasDrawingSession ds = args.DrawingSession;
            UpdateTransform();

            float drawW = Math.Max(1f, BoardW * _scale);
            float drawH = Math.Max(1f, BoardH * _scale);
            var dest = new Rect(_offX, _offY, drawW, drawH);

            // Vẽ bóng đổ nhẹ phía sau tờ giấy tạo chiều sâu không gian
            ds.FillRectangle(new Rect(dest.X + 3, dest.Y + 4, dest.Width, dest.Height), Color.FromArgb(40, 0, 0, 0));

            // Kết xuất nội dung bảng vẽ với bộ lọc nội suy chất lượng cao
            ds.DrawImage(board, dest, new Rect(0, 0, BoardW, BoardH), 1f, CanvasImageInterpolation.HighQualityCubic);

            // Viền mảnh phân tách ranh giới tờ giấy 1280 x 720 với nền xung quanh
            ds.DrawRectangle(dest, Color.FromArgb(255, 150, 156, 166), 1f);

            // Hiển thị nét đứt xem trước khi người dùng đang kéo hình khối (T8), giới hạn trong phạm vi tờ giấy
            if (_isDrawingShape)
            {
                ds.Transform = Matrix3x2.CreateScale(_scale) * Matrix3x2.CreateTranslation(_offX, _offY);
                using (ds.CreateLayer(1f, new Rect(0, 0, BoardW, BoardH)))
                {
                    StrokeShape(ds, _tool, _shapeStart, _shapeEnd, _color, _brush, dashed: true);
                }
                ds.Transform = Matrix3x2.Identity;
            }
        }
        catch (Exception)
        {
            // Bảo vệ ứng dụng không bị sập nếu thiết bị đồ họa GPU đang trong quá trình tái tạo tài nguyên
        }
    }

    private void DrawSegmentOnBoard(int x1, int y1, int x2, int y2, Color color, int size)
    {
        CanvasRenderTarget? board = _board;
        if (_closed || board == null) return;

        try
        {
            using CanvasDrawingSession ds = board.CreateDrawingSession();
            StrokeSegment(ds, x1, y1, x2, y2, color, size);
        }
        catch (Exception)
        {
            // Bỏ qua nếu bộ đệm đồ họa đã bị giải phóng khi đóng trang
        }
    }

    private static void StrokeSegment(CanvasDrawingSession ds, int x1, int y1, int x2, int y2, Color color, int size)
    {
        ds.Antialiasing = CanvasAntialiasing.Antialiased;
        int safeSize = Math.Max(1, Math.Min(200, size));

        if (x1 == x2 && y1 == y2)
        {
            ds.FillCircle(x1, y1, safeSize / 2f, color); // Vẽ chấm tròn đơn khi người dùng bấm nhả tại chỗ
            return;
        }

        using var style = new CanvasStrokeStyle
        {
            StartCap = CanvasCapStyle.Round,
            EndCap = CanvasCapStyle.Round,
            LineJoin = CanvasLineJoin.Round
        };
        ds.DrawLine(x1, y1, x2, y2, color, safeSize, style);
    }
}
