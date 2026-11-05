using System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace WhiteboardClient.Views;

/// <summary>
/// Phần tính toán biến đổi hiển thị: Thu phóng (Zoom 25% - 800%), kéo di chuyển bảng (Pan), mở rộng bảng.
/// Phụ trách: Thành viên C (Hạng mục T9a)
/// </summary>
public sealed partial class BoardPage
{
    /// <summary>Tỉ lệ để tờ giấy vừa khít vùng hiển thị (ứng với zoom = 1).</summary>
    private double FitScale()
    {
        double availW = Math.Max(50, Surface.ActualWidth - BoardMargin * 2);
        double availH = Math.Max(50, Surface.ActualHeight - BoardMargin * 2);
        return Math.Min(availW / BoardW, availH / BoardH);
    }

    private void UpdateTransform()
    {
        _scale = (float)(FitScale() * _zoom);
        _offX = (float)((Surface.ActualWidth - BoardW * _scale) / 2.0 + _panX);
        _offY = (float)((Surface.ActualHeight - BoardH * _scale) / 2.0 + _panY);
    }

    /// <summary>Không cho kéo bảng ra khỏi khung nhìn hoàn toàn: luôn chừa lại một góc.</summary>
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

    /// <summary>Phóng/thu quanh một điểm trên màn hình: điểm trên bảng nằm dưới con trỏ được giữ nguyên chỗ.</summary>
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

    private void ResetView()
    {
        _zoom = 1f;
        _panX = 0;
        _panY = 0;
        UpdateZoomUi();
        BoardCanvas.Invalidate();
    }

    private void UpdateZoomUi()
    {
        UpdateTransform();
        ZoomText.Text = $"{Math.Round(_scale * 100)}%";
    }

    private Point ViewCenter() => new(Surface.ActualWidth / 2.0, Surface.ActualHeight / 2.0);

    private void ZoomInButton_Click(object sender, RoutedEventArgs e) => ZoomAt(ViewCenter(), _zoom * 1.25f);

    private void ZoomOutButton_Click(object sender, RoutedEventArgs e) => ZoomAt(ViewCenter(), _zoom / 1.25f);

    private void ZoomResetButton_Click(object sender, RoutedEventArgs e) => ResetView();

    /// <summary>Ẩn/hiện hai panel hai bên để bảng vẽ chiếm toàn bộ chiều ngang.</summary>
    private void FocusButton_Click(object sender, RoutedEventArgs e)
    {
        _panelsHidden = !_panelsHidden;
        Visibility v = _panelsHidden ? Visibility.Collapsed : Visibility.Visible;
        LeftPanel.Visibility = v;
        RightPanel.Visibility = v;
        FocusText.Text = _panelsHidden ? "Hiện công cụ" : "Mở rộng bảng";
        ResetView();
    }

    private void Surface_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        PointerPoint pp = e.GetCurrentPoint(Surface);
        int delta = pp.Properties.MouseWheelDelta;
        if (delta == 0) return;

        // Một nấc cuộn chuột (120) ~ +/-20%
        ZoomAt(pp.Position, _zoom * (float)Math.Pow(1.0015, delta));
        e.Handled = true;
    }
}
