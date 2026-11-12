using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using WhiteboardClient.Models;
using WhiteboardClient.Services;
using WhiteboardShared;
using WinRT.Interop;
using Color = Windows.UI.Color;

namespace WhiteboardClient.Views;

/// <summary>
/// Màn hình bảng vẽ chính.
/// Bảng vẽ có kích thước LOGIC cố định (1280 x 720) giống nhau trên mọi máy,
/// được co giãn vừa khít cửa sổ -> các máy màn hình khác nhau vẫn thấy cùng một hình.
/// </summary>
public sealed partial class BoardPage : Page
{
    private const int BoardW = 1280;
    private const int BoardH = 720;
    private const double BoardMargin = 8;
    private const float RenderDpi = 192f;   // bảng được lưu ở độ phân giải gấp đôi -> zoom vẫn nét, ảnh PNG xuất ra 2560 x 1440
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 8f;
    private const double PanKeepVisible = 80;

    // Bảng màu nhanh (giữ nguyên bảng màu của bản WinForms)
    private static readonly ColorSwatch[] PaletteColors =
    {
        new(Colors.Black),
        new(Color.FromArgb(255, 231, 76, 60)),
        new(Color.FromArgb(255, 230, 126, 34)),
        new(Color.FromArgb(255, 241, 196, 15)),
        new(Color.FromArgb(255, 39, 174, 96)),
        new(Color.FromArgb(255, 41, 128, 185)),
        new(Color.FromArgb(255, 142, 68, 173)),
        new(Color.FromArgb(255, 121, 85, 72))
    };

    /// <summary>Danh sách người đang online (x:Bind).</summary>
    public ObservableCollection<UserItem> Users { get; } = new();

    // ---- Trạng thái ----
    private BoardSession _session = new("", "", true);
    private DrawTool _tool = DrawTool.Pen;
    private Color _color = Colors.Black;
    private int _brush = 4;

    private CanvasRenderTarget? _board;

    private bool _isDrawing;        // đang vẽ nét tự do (bút / tẩy)
    private bool _isDrawingShape;   // đang kéo hình (đường thẳng / chữ nhật / tròn)
    private BoardPoint _lastSentPoint;
    private BoardPoint _shapeStart;
    private BoardPoint _shapeEnd;

    private float _scale = 1f;
    private float _offX;
    private float _offY;

    // Zoom / kéo bảng. _zoom = 1 nghĩa là "vừa khít cửa sổ"; _panX/_panY là độ lệch so với vị trí chính giữa.
    private float _zoom = 1f;
    private double _panX;
    private double _panY;
    private bool _isPanning;
    private Point _panPointerStart;
    private double _panStartX;
    private double _panStartY;
    private bool _panelsHidden;

    private bool _connectionLost;
    private volatile bool _closed;
    private DispatcherQueue? _dispatcher;
    private readonly DispatcherTimer _copyTimer;
    private readonly DispatcherTimer _noticeTimer;

    // Ngăn các sự kiện ValueChanged/SelectionChanged chạy trong lúc XAML còn đang khởi tạo
    private readonly bool _ready;
    private readonly Dictionary<DrawTool, ToggleButton> _toolButtons = new();

    public BoardPage()
    {
        InitializeComponent();

        _toolButtons[DrawTool.Pen] = PenTool;
        _toolButtons[DrawTool.Eraser] = EraserTool;
        _toolButtons[DrawTool.Line] = LineTool;
        _toolButtons[DrawTool.Rectangle] = RectangleTool;
        _toolButtons[DrawTool.Circle] = CircleTool;
        _toolButtons[DrawTool.Pan] = PanTool;

        _copyTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        _copyTimer.Tick += (_, _) =>
        {
            _copyTimer.Stop();
            CopyText.Text = "Sao chép mã";
        };

        _noticeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _noticeTimer.Tick += (_, _) =>
        {
            _noticeTimer.Stop();
            NoticeBar.IsOpen = false;
        };

        // Bảng vẽ ngoài màn hình: toạ độ logic 1280 x 720 (đơn vị DIP), lưu ở 192 dpi = 2560 x 1440 pixel
        _board = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), BoardW, BoardH, RenderDpi);
        ClearBoardLocal();

        // Gán trong code (không dùng x:Bind) để chắc chắn đã có dữ liệu trước khi chọn ô màu đầu tiên
        PaletteView.ItemsSource = PaletteColors;

        // Đổi kích thước cửa sổ / ẩn hiện panel: giữ bảng không trôi mất khỏi khung nhìn và cập nhật % zoom
        Surface.SizeChanged += (_, _) =>
        {
            ClampPan();
            UpdateZoomUi();
        };

        _ready = true;
        PaletteView.SelectedIndex = 0;
        SelectTool(DrawTool.Pen);
        SetColor(Colors.Black, fromPalette: true);
    }

    // ==================================================================
    //  VÒNG ĐỜI TRANG
    // ==================================================================
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is BoardSession session) _session = session;

        RoomCodeText.Text = _session.RoomId;
        GreetingText.Text = "Xin chào, " + _session.UserName;
        ConnText.Text = $"Đã kết nối tới {NetworkClient.Host}:{NetworkClient.Port}";

        _dispatcher = DispatcherQueue;
        NetworkClient.OnDataReceived += HandleNetworkData;
        NetworkClient.OnDisconnected += HandleDisconnected;
        NetworkClient.StartReceiving();

        // Báo danh với Server: tạo/vào phòng (Server sẽ gửi lại lịch sử nét vẽ + danh sách người dùng)
        if (!NetworkClient.SendMessage($"{_session.JoinCommand};{_session.UserName};{_session.RoomId}"))
        {
            MarkConnectionLost();
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _closed = true;
        NetworkClient.OnDataReceived -= HandleNetworkData;
        NetworkClient.OnDisconnected -= HandleDisconnected;
        _copyTimer.Stop();
        _noticeTimer.Stop();
        base.OnNavigatedFrom(e);
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        // Win2D yêu cầu gỡ CanvasControl khỏi cây giao diện khi trang bị huỷ để không rò rỉ bộ nhớ
        BoardCanvas.RemoveFromVisualTree();

        CanvasRenderTarget? board = _board;
        _board = null;
        board?.Dispose();
    }

    private void BoardCanvas_CreateResources(CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
    {
        // Bảng vẽ đã được tạo ở hàm khởi tạo; không có tài nguyên nào khác cần nạp
    }

   
    // ==================================================================
    //  NÚT CHỨC NĂNG
    // ==================================================================
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

        if (!NetworkClient.SendMessage("CLEAR_CANVAS;" + _session.RoomId))
        {
            MarkConnectionLost();
        }
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

    // ==================================================================
    //  NHẬN DỮ LIỆU TỪ SERVER (luồng nền -> luồng giao diện)
    // ==================================================================
    private void HandleNetworkData(string data)
    {
        if (_closed || string.IsNullOrWhiteSpace(data)) return;
        _dispatcher?.TryEnqueue(() => ProcessPacket(data));
    }

    private void HandleDisconnected()
    {
        if (_closed) return;
        _dispatcher?.TryEnqueue(MarkConnectionLost);
    }

    private void ProcessPacket(string raw)
    {
        if (_closed) return;
        string clean = raw.Replace("\0", "").Replace("\uFEFF", "").Trim();
        if (clean.Length == 0) return;

        string[] p = clean.Split(';');
        try
        {
            if (p[0] == "USER_LIST")
            {
                UpdateUserList(p.Length >= 2 ? p[1] : "");
            }
            else if (p[0] == DrawPacket.CommandName)
            {
                if (DrawPacket.TryParse(clean, out DrawPacket? packet) && packet != null)
                {
                    ApplyRemoteDraw(packet);
                }
            }
            else if (p[0] == "CLEAR_CANVAS")
            {
                ClearBoardLocal();
            }
            else if (p[0] == "CLEAR_FAILED")
            {
                ShowNotice(
                    p.Length >= 2 ? string.Join(";", p, 1, p.Length - 1) : "Server không thể xóa bảng vẽ.",
                    InfoBarSeverity.Error,
                    autoHide: false);
            }
        }
        catch (Exception)
        {
            // Gói tin sai định dạng: bỏ qua, không làm sập ứng dụng
        }
    }

    private void ApplyRemoteDraw(DrawPacket packet)
    {
        Color color = HexColor.Parse(packet.ColorHex);

        if (packet.Kind == DrawKind.Freehand)
        {
            DrawSegmentOnBoard(packet.X1, packet.Y1, packet.X2, packet.Y2, color, packet.Size);
        }
        else
        {
            DrawTool tool = packet.Shape switch
            {
                ShapeKind.Rectangle => DrawTool.Rectangle,
                ShapeKind.Circle => DrawTool.Circle,
                _ => DrawTool.Line
            };
            DrawShapeOnBoard(tool,
                new BoardPoint(packet.X1, packet.Y1), new BoardPoint(packet.X2, packet.Y2),
                color, packet.Size);
        }
        BoardCanvas.Invalidate();
    }

    private void UpdateUserList(string csv)
    {
        string[] names = csv.Split(',', StringSplitOptions.RemoveEmptyEntries);

        Users.Clear();
        bool markedSelf = false;
        foreach (string raw in names)
        {
            string name = raw.Trim();
            if (name.Length == 0) continue;

            bool isSelf = !markedSelf && name == _session.UserName;
            if (isSelf) markedSelf = true;
            Users.Add(new UserItem(name, isSelf));
        }

        OnlineHeader.Text = $"Đang online ({Users.Count})";
    }
}
