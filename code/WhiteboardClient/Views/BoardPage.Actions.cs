using System;
using System.Collections.Generic;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using WhiteboardClient.Services;
using WinRT.Interop;

namespace WhiteboardClient.Views;

/// <summary>
/// Phần hành động bảng: Sao chép mã phòng, rời phòng, xóa bảng (đồng bộ phòng), lưu ảnh PNG, thông báo InfoBar.
/// Phụ trách: Thành viên D (Hạng mục T9b)
/// </summary>
public sealed partial class BoardPage
{
    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var package = new DataPackage();
            package.SetText(_session.RoomId);
            Clipboard.SetContent(package);

            CopyText.Text = "Đã sao chép!";
            _copyTimer.Stop();
            _copyTimer.Start();
        }
        catch (Exception)
        {
            ShowNotice("Không sao chép được. Mã phòng: " + _session.RoomId, InfoBarSeverity.Warning, autoHide: false);
        }
    }

    private void LeaveButton_Click(object sender, RoutedEventArgs e)
    {
        NetworkClient.Disconnect();
        Frame.Navigate(typeof(LoginPage));
        Frame.BackStack.Clear();
    }

    private async void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Xóa bảng vẽ?",
            Content = "Xóa toàn bộ bảng vẽ của TẤT CẢ mọi người trong phòng?\nHành động này không thể hoàn tác.",
            PrimaryButtonText = "Xóa",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Close
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        ClearBoardLocal();
        if (!NetworkClient.SendMessage("CLEAR_CANVAS;" + _session.RoomId)) MarkConnectionLost();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        CanvasRenderTarget? board = _board;
        if (board == null || App.MainWindow == null) return;

        try
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                SuggestedFileName = "Whiteboard_" + _session.RoomId
            };
            picker.FileTypeChoices.Add("PNG Image", new List<string> { ".png" });

            // Ứng dụng unpackaged phải gắn hộp thoại chọn file với handle của cửa sổ
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));

            StorageFile? file = await picker.PickSaveFileAsync();
            if (file == null) return;

            using var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            stream.Size = 0; // ghi đè hoàn toàn nếu file đã tồn tại
            await board.SaveAsync(stream, CanvasBitmapFileFormat.Png);

            ShowNotice("Đã lưu ảnh thành công: " + file.Name, InfoBarSeverity.Success, autoHide: true);
        }
        catch (Exception ex)
        {
            ShowNotice("Không lưu được ảnh: " + ex.Message, InfoBarSeverity.Error, autoHide: false);
        }
    }

    private void ShowNotice(string message, InfoBarSeverity severity, bool autoHide)
    {
        _noticeTimer.Stop();
        NoticeBar.Severity = severity;
        NoticeBar.Message = message;
        NoticeBar.IsOpen = true;
        if (autoHide) _noticeTimer.Start();
    }

    private void ClearBoardLocal()
    {
        if (_board == null) return;
        using (CanvasDrawingSession ds = _board.CreateDrawingSession())
        {
            ds.Clear(Colors.White);
        }
        BoardCanvas?.Invalidate();
    }

    private void MarkConnectionLost()
    {
        if (_connectionLost || _closed) return;
        _connectionLost = true;
        _isDrawing = false;
        _isDrawingShape = false;

        ConnDotOk.Visibility = Visibility.Collapsed;
        ConnDotLost.Visibility = Visibility.Visible;
        ConnText.Text = "Mất kết nối với Server";
        ShowNotice(
            "Mất kết nối tới Server. Hãy bấm \"Rời phòng\" rồi đăng nhập lại khi Server hoạt động.",
            InfoBarSeverity.Error, autoHide: false);
        BoardCanvas.Invalidate();
    }
}
