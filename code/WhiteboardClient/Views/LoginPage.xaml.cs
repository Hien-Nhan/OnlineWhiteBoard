using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;
using WhiteboardClient.Models;
using WhiteboardClient.Services;

namespace WhiteboardClient.Views;

/// <summary>Màn hình đăng nhập: nhập tên, địa chỉ Server, rồi tạo phòng hoặc vào phòng.</summary>
public sealed partial class LoginPage : Page
{
    private bool _busy;

    public LoginPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        ClientSettings saved = SettingsService.Load();
        NameBox.Text = saved.Name;
        ServerBox.Text = saved.Server;
        PortBox.Text = saved.Port.ToString();

        (string.IsNullOrEmpty(saved.Name) ? NameBox : RoomBox).Focus(FocusState.Programmatic);
    }

    private async void CreateButton_Click(object sender, RoutedEventArgs e) => await StartSessionAsync(create: true);

    private async void JoinButton_Click(object sender, RoutedEventArgs e) => await StartSessionAsync(create: false);

    /// <summary>Nhấn Enter: có mã phòng -> vào phòng, không có -> tạo phòng mới.</summary>
    private async void Input_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter) return;
        e.Handled = true;
        await StartSessionAsync(create: string.IsNullOrWhiteSpace(RoomBox.Text));
    }

    // ------------------------------------------------------------------
    // Kết nối Server rồi mở bảng vẽ
    // ------------------------------------------------------------------
    private async Task StartSessionAsync(bool create)
    {
        if (_busy) return;

        string name = Sanitize(NameBox.Text, 20);
        string host = ServerBox.Text.Trim();
        string room = Sanitize(RoomBox.Text, 20).Replace(" ", "").ToUpperInvariant();

        if (name.Length == 0)
        {
            ShowStatus("Vui lòng nhập tên của bạn.", isError: true);
            NameBox.Focus(FocusState.Programmatic);
            return;
        }
        if (host.Length == 0)
        {
            ShowStatus("Vui lòng nhập địa chỉ IP của Server.", isError: true);
            ServerBox.Focus(FocusState.Programmatic);
            return;
        }
        if (!int.TryParse(PortBox.Text.Trim(), out int port) || port < 1 || port > 65535)
        {
            ShowStatus("Cổng không hợp lệ (1 - 65535).", isError: true);
            PortBox.Focus(FocusState.Programmatic);
            return;
        }
        if (!create && room.Length == 0)
        {
            ShowStatus("Vui lòng nhập mã phòng do người tạo phòng gửi cho bạn.", isError: true);
            RoomBox.Focus(FocusState.Programmatic);
            return;
        }
        if (create)
        {
            room = "ROOM" + Random.Shared.Next(10000, 100000);
        }

        SetBusy(true);
        ShowStatus($"Đang kết nối tới {host}:{port} ...", isError: false);

        try
        {
            await NetworkClient.ConnectAsync(host, port, 4000);
        }
        catch (Exception)
        {
            SetBusy(false);
            ShowStatus(
                $"Không kết nối được tới {host}:{port}. Hãy kiểm tra: Server đã chạy chưa, " +
                $"hai máy có cùng WiFi không, IP có đúng không, và Firewall máy Server đã mở cổng {port} chưa.",
                isError: true);
            return;
        }

        SettingsService.Save(new ClientSettings { Name = name, Server = host, Port = port });
        SetBusy(false);
        StatusBar.IsOpen = false;

        Frame.Navigate(typeof(BoardPage), new BoardSession(name, room, create));
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        CreateButton.IsEnabled = !busy;
        JoinButton.IsEnabled = !busy;
        BusyBar.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ShowStatus(string text, bool isError)
    {
        StatusBar.Severity = isError ? InfoBarSeverity.Error : InfoBarSeverity.Informational;
        StatusBar.Message = text;
        StatusBar.IsOpen = true;
    }

    /// <summary>Bỏ các ký tự đặc biệt mà giao thức dùng để phân tách (; | ,) và xuống dòng.</summary>
    private static string Sanitize(string? s, int maxLength)
    {
        if (s == null) return "";
        foreach (char c in new[] { ';', '|', ',', '\r', '\n', '\t' })
        {
            s = s.Replace(c.ToString(), "");
        }
        s = s.Trim();
        return s.Length > maxLength ? s.Substring(0, maxLength) : s;
    }
}
