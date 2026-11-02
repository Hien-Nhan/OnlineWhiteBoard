namespace WhiteboardClient.Models;

/// <summary>Thông tin truyền từ màn hình đăng nhập sang bảng vẽ.</summary>
public sealed record BoardSession(string UserName, string RoomId, bool CreateRoom)
{
    /// <summary>Lệnh báo danh gửi cho Server.</summary>
    public string JoinCommand => CreateRoom ? "CREATE_ROOM" : "JOIN_ROOM";
}
