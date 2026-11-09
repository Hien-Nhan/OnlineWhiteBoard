using System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace WhiteboardClient.Views;

/// <summary>
/// Quản lý tính năng Thu phóng (Zoom), Kéo di chuyển bảng (Pan) và Mở rộng bảng vẽ.
/// Phụ trách: Thành viên C (Hạng mục T9a).
/// </summary>
public sealed partial class BoardPage
{
    /// <summary>
    /// Tỉ lệ để tờ giấy vẽ 1280x720 vừa khít vùng hiển thị (ứng với zoom = 1.0).
    /// </summary>
    private double FitScale()
    {
        double availW = Math.Max(50, Surface.ActualWidth - BoardMargin * 2);
        double availH = Math.Max(50, Surface.ActualHeight - BoardMargin * 2);
        return Math.Min(availW / BoardW, availH / BoardH);
    }

    /// <summary>
    /// Cập nhật ma trận biến đổi tọa độ (_scale, _offX, _offY) kết hợp giữa FitScale, _zoom và độ lệch pan.
    /// </summary>
    private void UpdateTransform()
    {
        _scale = (float)(FitScale() * _zoom);
        _offX = (float)((Surface.ActualWidth - BoardW * _scale) / 2.0 + _panX);
        _offY = (float)((Surface.ActualHeight - BoardH * _scale) / 2.0 + _panY);
    }

    /// <summary>
    /// Khóa biên không cho kéo bảng ra khỏi khung nhìn hoàn toàn: luôn chừa lại tối thiểu PanKeepVisible (80px).
    /// </summary>
    private void ClampPan()
    {
        double sc = FitScale() * _zoom;
        double w = BoardW * sc;
        double h = BoardH * sc;
        double vw = Surface.ActualWidth;
        double vh = Surface.ActualHeight;

        double minX = PanKeepVisible - w - (vw - w) / 2.0;
        double maxX = (vw - PanKeepVisible) - (vw - w) / 2.0;
        double minY = PanKeepVisible - h - (vh - h) / 2.0;
        double maxY = (vh - PanKeepVisible) - (vh - h) / 2.0;

        _panX = Math.Max(minX, Math.Min(maxX, _panX));
        _panY = Math.Max(minY, Math.Min(maxY, _panY));
    }

    /// <summary>
    /// Phóng to / thu nhỏ quanh một điểm neo (anchor) trên màn hình:
    /// Điểm trên bảng vẽ nằm dưới con trỏ chuột được giữ nguyên vị trí, không bị lệch khi zoom.
    /// Giới hạn mức zoom trong khoảng an toàn [MinZoom = 0.25 (25%), MaxZoom = 8.0 (800%)].
    /// </summary>
    private void ZoomAt(Point anchor, float newZoom)
    {
        UpdateTransform();
        double bx = (anchor.X - _offX) / _scale;
        double by = (anchor.Y - _offY) / _scale;

        _zoom = Math.Max(MinZoom, Math.Min(MaxZoom, newZoom));
        double sc = FitScale() * _zoom;
        _panX = anchor.X - bx * sc - (Surface.ActualWidth - BoardW * sc) / 2.0;
        _panY = anchor.Y - by * sc - (Surface.ActualHeight - BoardH * sc) / 2.0;

        ClampPan();
        UpdateZoomUi();
        BoardCanvas.Invalidate();
    }

    /// <summary>
    /// Đưa bảng vẽ về trạng thái ban đầu: vừa khít cửa sổ (zoom = 1) và canh giữa (pan = 0).
    /// </summary>
    private void ResetView()
    {
        _zoom = 1f;
        _panX = 0;
        _panY = 0;
        UpdateZoomUi();
        BoardCanvas.Invalidate();
    }

    /// <summary>
    /// Cập nhật nhãn phần trăm hiển thị mức Zoom trên thanh trạng thái.
    /// </summary>
    private void UpdateZoomUi()
    {
        UpdateTransform();
        ZoomText.Text = $"{Math.Round(_scale * 100)}%";
    }

    private Point ViewCenter() => new(Surface.ActualWidth / 2.0, Surface.ActualHeight / 2.0);

    /// <summary>
    /// Nút phóng to (+): tăng 25% quanh tâm vùng nhìn.
    /// </summary>
    private void ZoomInButton_Click(object sender, RoutedEventArgs e) => ZoomAt(ViewCenter(), _zoom * 1.25f);

    /// <summary>
    /// Nút thu nhỏ (-): giảm 25% quanh tâm vùng nhìn.
    /// </summary>
    private void ZoomOutButton_Click(object sender, RoutedEventArgs e) => ZoomAt(ViewCenter(), _zoom / 1.25f);

    /// <summary>
    /// Nút bấm vào phần trăm Zoom: trở về vừa khít màn hình.
    /// </summary>
    private void ZoomResetButton_Click(object sender, RoutedEventArgs e) => ResetView();

    /// <summary>
    /// Ẩn/hiện hai panel công cụ bên trái và bên phải để tối ưu diện tích vẽ trên màn hình nhỏ.
    /// </summary>
    private void FocusButton_Click(object sender, RoutedEventArgs e)
    {
        _panelsHidden = !_panelsHidden;
        Visibility v = _panelsHidden ? Visibility.Collapsed : Visibility.Visible;
        LeftPanel.Visibility = v;
        RightPanel.Visibility = v;
        FocusText.Text = _panelsHidden ? "Hiện công cụ" : "Mở rộng bảng";
        ResetView();
    }

    /// <summary>
    /// Sự kiện cuộn con lăn chuột: Zoom mượt mà quanh đúng vị trí con trỏ chuột.
    /// </summary>
    private void Surface_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        PointerPoint pp = e.GetCurrentPoint(Surface);
        int delta = pp.Properties.MouseWheelDelta;
        if (delta == 0) return;

        // Một nấc cuộn chuột (120 đơn vị) tương ứng phóng/thu khoảng ~20%
        ZoomAt(pp.Position, _zoom * (float)Math.Pow(1.0015, delta));
        e.Handled = true;
    }
}
