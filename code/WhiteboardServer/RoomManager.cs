using System;
using System.Collections.Generic;
using System.Linq;

namespace WhiteboardServer;

/// <summary>
/// Quản lý các phòng vẽ (Room) và phân phối tin nhắn theo phòng (Hạng mục T3 - Thành viên C).
/// Đảm bảo tính an toàn đa luồng (thread-safe) khi nhiều client cùng truy cập.
/// </summary>
public sealed class RoomManager
{
    private static readonly Lazy<RoomManager> _instance = new(() => new RoomManager());
    public static RoomManager Instance => _instance.Value;

    private readonly object _lock = new();

    // Map: Mã phòng -> Danh sách ClientHandler đang có mặt trong phòng
    private readonly Dictionary<string, List<ClientHandler>> _rooms = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Cho client gia nhập vào một phòng cụ thể.
    /// Nếu phòng chưa tồn tại, tự động tạo phòng mới.
    /// </summary>
    public void JoinRoom(string roomId, ClientHandler client)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            roomId = "DEFAULT_ROOM";
        }

        lock (_lock)
        {
            // Rời phòng cũ nếu đang ở phòng khác
            LeaveRoomInternal(client);

            if (!_rooms.TryGetValue(roomId, out var clientList))
            {
                clientList = new List<ClientHandler>();
                _rooms[roomId] = clientList;
            }

            if (!clientList.Contains(client))
            {
                clientList.Add(client);
            }

            client.CurrentRoomId = roomId;
            Console.WriteLine($"[ROOM] '{client.Username}' đã vào phòng [{roomId}]. Tổng số người trong phòng: {clientList.Count}");
        }
    }

    /// <summary>
    /// Cho client rời khỏi phòng hiện tại khi ngắt kết nối hoặc chuyển phòng.
    /// </summary>
    public void LeaveRoom(ClientHandler client)
    {
        lock (_lock)
        {
            LeaveRoomInternal(client);
        }
    }

    private void LeaveRoomInternal(ClientHandler client)
    {
        string? oldRoom = client.CurrentRoomId;
        if (string.IsNullOrEmpty(oldRoom)) return;

        if (_rooms.TryGetValue(oldRoom, out var clientList))
        {
            clientList.Remove(client);
            Console.WriteLine($"[ROOM] '{client.Username}' đã rời phòng [{oldRoom}]. Còn lại: {clientList.Count} người.");

            if (clientList.Count == 0)
            {
                _rooms.Remove(oldRoom);
                Console.WriteLine($"[ROOM] Phòng [{oldRoom}] đã trống và được dọn dẹp.");
            }
        }

        client.CurrentRoomId = null;
    }

    /// <summary>
    /// Chuyển tiếp (Broadcast) tin nhắn/nét vẽ tới tất cả các client CÙNG PHÒNG.
    /// Có thể loại trừ người gửi (sender) để người vẽ không phải nhận lại nét vẽ của chính mình.
    /// </summary>
    public void Broadcast(string roomId, string message, ClientHandler? sender = null)
    {
        if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(message)) return;

        List<ClientHandler> targets;
        lock (_lock)
        {
            if (!_rooms.TryGetValue(roomId, out var list) || list.Count == 0) return;
            // Sao chép danh sách để tránh lock trong lúc gửi qua mạng IO
            targets = new List<ClientHandler>(list);
        }

        foreach (var client in targets)
        {
            if (sender != null && ReferenceEquals(client, sender))
            {
                continue;
            }

            client.Send(message);
        }
    }

    /// <summary>
    /// Phát danh sách thành viên online trong phòng cho toàn bộ client trong phòng đó.
    /// Định dạng gói: USER_LIST;User1,User2,User3
    /// </summary>
    public void BroadcastUserList(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId)) return;

        List<string> userNames;
        lock (_lock)
        {
            if (!_rooms.TryGetValue(roomId, out var list)) return;
            userNames = list.Select(c => c.Username).Where(u => !string.IsNullOrEmpty(u)).ToList();
        }

        string packet = $"USER_LIST;{string.Join(",", userNames)}";
        // Gửi cho TẤT CẢ thành viên (sender = null)
        Broadcast(roomId, packet, sender: null);
    }

    /// <summary>
    /// Lấy danh sách tên thành viên trong phòng (hỗ trợ kiểm thử và log).
    /// </summary>
    public IReadOnlyList<string> GetUsersInRoom(string roomId)
    {
        lock (_lock)
        {
            if (_rooms.TryGetValue(roomId, out var list))
            {
                return list.Select(c => c.Username).ToList();
            }
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Đếm số người đang ở trong một phòng.
    /// </summary>
    public int GetUserCount(string roomId)
    {
        lock (_lock)
        {
            return _rooms.TryGetValue(roomId, out var list) ? list.Count : 0;
        }
    }
}

