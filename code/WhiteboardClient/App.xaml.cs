using Microsoft.UI.Xaml;

namespace WhiteboardClient;

/// <summary>Điểm vào của ứng dụng WinUI 3 (thay cho Program.cs + Application.Run của WinForms).</summary>
public partial class App : Application
{
    /// <summary>Cửa sổ chính — các trang cần handle của nó để mở hộp thoại chọn file.</summary>
    public static MainWindow? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}
