namespace WhiteboardClient.Models;

/// <summary>
/// Biểu diễn một người dùng trong danh sách thành viên đang trực tuyến tại phòng vẽ.
/// Phụ trách: Thành viên A (Hạng mục T7).
/// </summary>
public sealed class UserItem
{
    public UserItem(string name, bool isSelf)
    {
        string safeName = string.IsNullOrWhiteSpace(name) ? "Ẩn danh" : name.Trim();
        Name = safeName;
        IsSelf = isSelf;
        Label = isSelf ? $"{safeName} (Bạn)" : safeName;
    }

    /// <summary>Tên gốc của người dùng — dùng cho PersonPicture lấy chữ cái đại diện.</summary>
    public string Name { get; }

    /// <summary>Đánh dấu người dùng này có phải là chính client hiện tại hay không.</summary>
    public bool IsSelf { get; }

    /// <summary>Tên hiển thị trên danh sách (có hậu tố "(Bạn)" cho chính mình).</summary>
    public string Label { get; }
}
