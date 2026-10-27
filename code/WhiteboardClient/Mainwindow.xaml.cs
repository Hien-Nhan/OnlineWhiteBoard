using System;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WhiteboardClient.Services;
using WhiteboardClient.Views;
using WinRT.Interop;

namespace WhiteboardClient;

/// <summary>Cửa sổ duy nhất của ứng dụng: nền Mica, thanh tiêu đề tuỳ biến và một Frame chứa các trang.</summary>
public sealed partial class MainWindow : Window
{
    private const int DefaultWidth = 1500;
    private const int DefaultHeight = 920;

    public MainWindow()
    {
        InitializeComponent();

        Title = "Whiteboard Online";
        SystemBackdrop = new MicaBackdrop();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            // Nút thu/phóng/đóng trong suốt để thấy nền Mica
            AppWindowTitleBar titleBar = AppWindow.TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }

        CenterOnScreen(DefaultWidth, DefaultHeight);

        // Thoát ứng dụng: đóng kết nối để Server cập nhật lại danh sách người online ngay
        Closed += (_, _) => NetworkClient.Disconnect();

        RootFrame.Navigate(typeof(LoginPage));
    }

    /// <summary>Đặt kích thước theo DIP (đã nhân tỉ lệ DPI) và đưa cửa sổ ra giữa vùng làm việc.</summary>
    private void CenterOnScreen(int widthDip, int heightDip)
    {
        IntPtr hwnd = WindowNative.GetWindowHandle(this);
        double scale = GetDpiForWindow(hwnd) / 96.0;

        DisplayArea area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest);
        RectInt32 work = area.WorkArea;

        int w = Math.Min((int)(widthDip * scale), work.Width);
        int h = Math.Min((int)(heightDip * scale), work.Height);
        AppWindow.MoveAndResize(new RectInt32(
            work.X + (work.Width - w) / 2,
            work.Y + (work.Height - h) / 2,
            w, h));
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);
}
