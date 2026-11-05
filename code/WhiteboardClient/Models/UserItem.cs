namespace WhiteboardClient.Models;

/// <summary>Một dòng trong danh sách người đang online.</summary>
public sealed class UserItem
{
    public UserItem(string name, bool isSelf)
    {
        Name = name;
        Label = isSelf ? name + " (Bạn)" : name;
    }

    /// <summary>Tên gốc — PersonPicture dùng để lấy chữ cái đầu và màu nền.</summary>
    public string Name { get; }

    /// <summary>Tên hiển thị (có hậu tố "(Bạn)" cho chính mình).</summary>
    public string Label { get; }
}
