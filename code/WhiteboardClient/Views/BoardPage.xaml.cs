using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using WhiteboardClient.Models;
using WhiteboardClient.Services;
using WhiteboardShared;
using Color = Windows.UI.Color;

namespace WhiteboardClient.Views;

/// <summary>
/// Màn hình bảng vẽ chính (Core &amp; Vòng đời).
/// Bảng vẽ có kích thước LOGIC cố định (1280 x 720) giống nhau trên mọi thiết bị,
/// tự động co giãn vừa khít cửa sổ giúp các máy có màn hình khác nhau vẫn đồng bộ chính xác vị trí nét vẽ.
/// Phụ trách: Thành viên A (Hạng mục T7) &amp; Thành viên E (Xử lý mất kết nối - T10).
/// </summary>
public sealed partial class BoardPage : Page
{
    private const int BoardW = 1280;
    private const int BoardH = 720;
    private const double BoardMargin = 8;
    private const float RenderDpi = 192f;   // Lưu ở mật độ điểm ảnh gấp đôi (2560 x 1440 px) để phóng to vẫn sắc nét
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 8f;
    private const double PanKeepVisible = 80;

    // Bảng màu nhanh mặc định
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

    /// <summary>Danh sách thành viên đang trực tuyến trong phòng (liên kết giao diện qua x:Bind).</summary>
    public ObservableCollection<UserItem> Users { get; } = new();

    // ---- Trạng thái phiên làm việc & công cụ ----
    private BoardSession _session = new("", "", true);
    private DrawTool _tool = DrawTool.Pen;
    private Color _color = Colors.Black;
    private int _brush = 4;

    private CanvasRenderTarget? _board;

    // ---- Trạng thái thao tác vẽ & con trỏ ----
    private bool _isDrawing;        // Đang vẽ nét tự do (bút / tẩy)
    private bool _isDrawingShape;   // Đang kéo hình khối (đường thẳng / chữ nhật / tròn)
    private uint? _activePointerId; // Khóa ID con trỏ đang thao tác để chống nhảy nét khi chạm đa điểm
    private BoardPoint _lastSentPoint;
    private BoardPoint _shapeStart;
    private BoardPoint _shapeEnd;

    // ---- Hệ số biến đổi tọa độ hiển thị ----
    private float _scale = 1f;
    private float _offX;
    private float _offY;

    // ---- Trạng thái thu phóng (Zoom) & kéo bảng (Pan) ----
    private float _zoom = 1f;
    private double _panX;
    private double _panY;
    private bool _isPanning;
    private Point _panPointerStart;
    private double _panStartX;
    private double _panStartY;
    private bool _panelsHidden;

    // ---- Trạng thái kết nối & điều phối luồng UI ----
    private bool _connectionLost;
    private volatile bool _closed;
    private DispatcherQueue? _dispatcher;
    private readonly DispatcherTimer _copyTimer;
    private readonly DispatcherTimer _noticeTimer;

    // Ngăn các sự kiện ValueChanged/SelectionChanged kích hoạt sớm khi XAML đang khởi tạo
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

        // Khởi tạo bộ đệm bảng vẽ ngoài màn hình (Offscreen RenderTarget): 1280 x 720 DIP ở 192 DPI (2560 x 1440 pixel)
        EnsureBoardResource(CanvasDevice.GetSharedDevice());

        // Gán nguồn bảng màu trước khi thiết lập ô màu mặc định đầu tiên
        PaletteView.ItemsSource = PaletteColors;

        // Tự động tính lại tỷ lệ hiển thị và giữ bảng trong vùng nhìn khi thay đổi kích thước cửa sổ
        Surface.SizeChanged += (_, _) =>
        {
            if (_closed) return;
            ClampPan();
            UpdateZoomUi();
            BoardCanvas.Invalidate();
        };

        _ready = true;
        PaletteView.SelectedIndex = 0;
        SelectTool(DrawTool.Pen);
        SetColor(Colors.Black, fromPalette: true);
    }

    // ==================================================================
    //  VÒNG ĐỜI TRANG & QUẢN LÝ TÀI NGUYÊN ĐỒ HỌA WIN2D
    // ==================================================================
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is BoardSession session)
        {
            _session = session;
        }

        RoomCodeText.Text = _session.RoomId;
        GreetingText.Text = "Xin chào, " + _session.UserName;
        ConnText.Text = $"Đã kết nối tới {NetworkClient.Host}:{NetworkClient.Port}";

        _dispatcher = DispatcherQueue;
        NetworkClient.OnDataReceived += HandleNetworkData;
        NetworkClient.OnDisconnected += HandleDisconnected;
        NetworkClient.StartReceiving();

        // Báo danh với Server: tạo hoặc gia nhập phòng (Server sẽ phát lại lịch sử nét vẽ + danh sách online)
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
        // Đánh dấu đóng trang ngay lập tức để chặn mọi gói tin trễ trong hàng đợi DispatcherQueue
        _closed = true;

        CanvasRenderTarget? board = _board;
        _board = null;

        try
        {
            // Win2D yêu cầu gỡ CanvasControl khỏi cây giao diện khi trang bị hủy để tránh rò rỉ bộ nhớ GPU
            BoardCanvas.RemoveFromVisualTree();
        }
        catch (Exception)
        {
            // Bỏ qua nếu cây giao diện đã tự giải phóng trước đó
        }

        board?.Dispose();
    }

    private void BoardCanvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        // Đảm bảo bộ đệm bảng vẽ luôn tương thích với thiết bị GPU hiện hành (kể cả khi thu hồi/tái tạo GPU)
        EnsureBoardResource(sender.Device);
    }

    private void EnsureBoardResource(CanvasDevice? device)
    {
        if (_closed) return;
        CanvasDevice targetDevice = device ?? CanvasDevice.GetSharedDevice();

        if (_board != null && _board.Device == targetDevice) return;

        CanvasRenderTarget? oldBoard = _board;
        _board = new CanvasRenderTarget(targetDevice, BoardW, BoardH, RenderDpi);
        ClearBoardLocal();
        oldBoard?.Dispose();
    }

    // ==================================================================
    //  NHẬN & XỬ LÝ GÓI TIN TỪ SERVER (Background Thread -> UI Thread)
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
        if (_closed || _board == null) return;

        string clean = raw.Replace("\0", "").Replace("\uFEFF", "").Trim();
        if (clean.Length == 0) return;

        string[] p = clean.Split(';');
        if (p.Length == 0) return;

        try
        {
            string cmd = p[0].Trim().ToUpperInvariant();

            if (cmd == "USER_LIST")
            {
                // Hỗ trợ song song cả 2 quy chuẩn gói USER_LIST:
                // 1) Chuẩn 2 phần từ RoomManager (T3): USER_LIST;User1,User2
                // 2) Chuẩn 3 phần từ NetworkPacket (T1): USER_LIST;RoomID;User1,User2
                string userCsv = p.Length >= 3 ? p[2] : (p.Length >= 2 ? p[1] : string.Empty);
                UpdateUserList(userCsv);
            }
            else if (cmd == DrawPacket.CommandName)
            {
                if (DrawPacket.TryParse(clean, out DrawPacket? packet) && packet != null)
                {
                    ApplyRemoteDraw(packet);
                }
            }
            else if (cmd == "CLEAR_CANVAS")
            {
                ClearBoardLocal();
            }
        }
        catch (Exception)
        {
            // Bỏ qua an toàn các gói tin lỗi hoặc hỏng định dạng mà không làm gián đoạn phiên vẽ
        }
    }

    private void ApplyRemoteDraw(DrawPacket packet)
    {
        if (_closed || _board == null) return;

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
            DrawShapeOnBoard(
                tool,
                new BoardPoint(packet.X1, packet.Y1),
                new BoardPoint(packet.X2, packet.Y2),
                color,
                packet.Size);
        }

        BoardCanvas.Invalidate();
    }

    private void UpdateUserList(string csv)
    {
        if (_closed) return;

        string[] names = csv.Split(',', StringSplitOptions.RemoveEmptyEntries);

        Users.Clear();
        bool markedSelf = false;
        foreach (string raw in names)
        {
            string name = raw.Trim();
            if (name.Length == 0) continue;

            bool isSelf = !markedSelf && string.Equals(name, _session.UserName, StringComparison.Ordinal);
            if (isSelf)
            {
                markedSelf = true;
            }
            Users.Add(new UserItem(name, isSelf));
        }

        OnlineHeader.Text = $"Đang online ({Users.Count})";
    }
}
