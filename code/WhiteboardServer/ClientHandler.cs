using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WhiteboardServer;

/// <summary>
/// Quản lý một phiên kết nối TCP của Client (Hạng mục T3 - Thành viên C).
/// Đọc các gói tin theo giao thức, phân loại lệnh và gọi RoomManager để điều phối.
/// </summary>
public sealed class ClientHandler
{
    private readonly TcpClient _tcpClient;
    private readonly RoomManager _roomManager;
    private NetworkStream? _stream;
    private StreamReader? _reader;
    private readonly object _sendLock = new();

    public string Username { get; set; } = "Ẩn danh";
    public string? CurrentRoomId { get; set; }
    public bool IsConnected => _tcpClient != null && _tcpClient.Connected;
    public EndPoint? RemoteEndPoint => _tcpClient?.Client?.RemoteEndPoint;

    public ClientHandler(TcpClient tcpClient, RoomManager? roomManager = null)
    {
        _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
        _roomManager = roomManager ?? RoomManager.Instance;
    }

    /// <summary>
    /// Khởi chạy luồng xử lý riêng cho client này.
    /// </summary>
    public void Start()
    {
        Thread thread = new Thread(ProcessLoop)
        {
            IsBackground = true,
            Name = $"ClientHandler_{RemoteEndPoint}"
        };
        thread.Start();
    }

    private void ProcessLoop()
    {
        try
        {
            _tcpClient.NoDelay = true; // Tắt thuật toán Nagle để gửi gói tin vẽ tức thì, giảm độ trễ
            _stream = _tcpClient.GetStream();
            _reader = new StreamReader(_stream, Encoding.UTF8);

            Console.WriteLine($"[CLIENT CONNECTED] Thiết bị mới kết nối từ: {RemoteEndPoint}");

            while (true)
            {
                string? line = _reader.ReadLine();
                if (line == null)
                {
                    // Client chủ động ngắt kết nối hoặc đóng cửa sổ
                    break;
                }

                line = line.Trim();
                if (line.Length == 0) continue;

                ProcessPacket(line);
            }
        }
        catch (IOException)
        {
            // Mất kết nối mạng hoặc client thoát đột ngột
        }
        catch (SocketException)
        {
            // Lỗi socket cấp mạng
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CLIENT ERROR] Lỗi xử lý client {Username} ({RemoteEndPoint}): {ex.Message}");
        }
        finally
        {
            Disconnect();
        }
    }

    private void ProcessPacket(string rawMessage)
    {
        Console.WriteLine($"[RECV] {rawMessage}");

        // Xử lý gói nếu có tiền tố dạng "Username|Message"
        string message = rawMessage;
        if (rawMessage.Contains('|'))
        {
            var parts = rawMessage.Split('|', 2);
            Username = parts[0];
            message = parts[1];
        }

        string[] p = message.Split(';');
        if (p.Length == 0) return;

        string command = p[0].ToUpperInvariant();

        switch (command)
        {
            case "CREATE_ROOM":
            case "JOIN_ROOM":
            case "CONNECT":
                HandleRoomJoin(command, p);
                break;

            case "DRAW":
                HandleDraw(message, p);
                break;

            case "CLEAR_CANVAS":
                HandleClearCanvas(message, p);
                break;

            case "USER_LIST":
                if (!string.IsNullOrEmpty(CurrentRoomId))
                {
                    _roomManager.BroadcastUserList(CurrentRoomId);
                }
                break;

            default:
                Console.WriteLine($"[UNKNOWN PACKET] Không hỗ trợ lệnh: {command}");
                break;
        }
    }

    private void HandleRoomJoin(string command, string[] p)
    {
        string newUsername = Username;
        string targetRoom = "ROOM001";

        if (p.Length >= 3)
        {
            newUsername = p[1].Trim();
            targetRoom = p[2].Trim();
        }
        else if (p.Length == 2)
        {
            newUsername = p[1].Trim();
        }

        if (!string.IsNullOrEmpty(newUsername))
        {
            Username = newUsername;
        }

        // Vào phòng mới thông qua RoomManager
        _roomManager.JoinRoom(targetRoom, this);

        // TODO (T4 - Thành viên D): Gọi HistoryStore để phát lại lịch sử nét vẽ cũ cho Client mới vào phòng
        // HistoryStore.ReplayHistory(this, targetRoom);

        // Phát danh sách user online cho toàn phòng nhìn thấy nhau
        _roomManager.BroadcastUserList(targetRoom);
    }

    private void HandleDraw(string rawDrawPacket, string[] p)
    {
        // Gói tin DRAW chuẩn định dạng dây:
        // DRAW;FREEHAND;x1;y1;x2;y2;#RRGGBB;size;roomId
        // DRAW;SHAPE;LINE|RECTANGLE|CIRCLE;x1;y1;x2;y2;#RRGGBB;size;roomId
        // Tham số cuối cùng của gói DRAW là mã phòng
        string targetRoom = CurrentRoomId ?? "";
        if (p.Length >= 3)
        {
            string lastToken = p[^1].Trim();
            if (!string.IsNullOrEmpty(lastToken))
            {
                targetRoom = lastToken;
            }
        }

        if (string.IsNullOrEmpty(targetRoom)) return;

        // 1. Chuyển tiếp nét vẽ cho các client khác trong cùng phòng (loại trừ người vẽ)
        _roomManager.Broadcast(targetRoom, rawDrawPacket, sender: this);

        // TODO (T4 - Thành viên D): Lưu nét vẽ vào SQLite
        // HistoryStore.SaveStroke(targetRoom, Username, rawDrawPacket);
    }

    private void HandleClearCanvas(string rawPacket, string[] p)
    {
        string targetRoom = CurrentRoomId ?? "";
        if (p.Length >= 2 && !string.IsNullOrWhiteSpace(p[1]))
        {
            targetRoom = p[1].Trim();
        }

        if (string.IsNullOrEmpty(targetRoom)) return;

        Console.WriteLine($"[CLEAR_CANVAS] Client '{Username}' yêu cầu xóa bảng phòng [{targetRoom}]");

        // Broadcast lệnh xóa bảng cho tất cả thành viên trong phòng (bao gồm cả sender nếu cần đồng bộ)
        _roomManager.Broadcast(targetRoom, rawPacket, sender: this);

        // TODO (T4 - Thành viên D): Xóa lịch sử bảng vẽ phòng này trong SQLite
        // HistoryStore.ClearHistory(targetRoom);
    }

    /// <summary>
    /// Gửi một dòng tin nhắn dạng UTF-8 (kết thúc bằng \n) tới client này.
    /// Thread-safe với lock gửi tin.
    /// </summary>
    public void Send(string message)
    {
        lock (_sendLock)
        {
            if (!IsConnected || _stream == null) return;

            try
            {
                string line = message.EndsWith("\n") ? message : message + "\n";
                byte[] bytes = Encoding.UTF8.GetBytes(line);
                _stream.Write(bytes, 0, bytes.Length);
                _stream.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SEND ERROR] Gửi tin cho '{Username}' thất bại: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Xử lý dọn dẹp khi client rời kết nối mạng.
    /// </summary>
    private void Disconnect()
    {
        string? roomToNotify = CurrentRoomId;

        _roomManager.LeaveRoom(this);

        if (!string.IsNullOrEmpty(roomToNotify))
        {
            _roomManager.BroadcastUserList(roomToNotify);
        }

        Close();
        Console.WriteLine($"[DISCONNECTED] Client '{Username}' ({RemoteEndPoint}) đã ngắt kết nối.");
    }

    public void Close()
    {
        try
        {
            _reader?.Dispose();
            _stream?.Dispose();
            _tcpClient?.Close();
        }
        catch
        {
            // Bỏ qua lỗi dọn dẹp kết nối
        }
    }
}

