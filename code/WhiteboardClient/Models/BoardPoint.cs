namespace WhiteboardClient.Models;

/// <summary>
/// Toạ độ nguyên trên bảng vẽ LOGIC cố định (1280 x 720), độc lập với độ phân giải màn hình thực tế.
/// Phụ trách: Thành viên A (Hạng mục T7).
/// </summary>
public readonly record struct BoardPoint(int X, int Y)
{
    /// <summary>
    /// Tính bình phương khoảng cách Euclid tới một điểm khác trên bảng logic.
    /// Dùng để lọc bớt các điểm quá sát nhau khi rê chuột, giúp giảm tải gói tin mạng.
    /// </summary>
    public int DistanceSquaredTo(BoardPoint other)
    {
        int dx = X - other.X;
        int dy = Y - other.Y;
        return dx * dx + dy * dy;
    }
}
