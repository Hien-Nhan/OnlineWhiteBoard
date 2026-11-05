using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace WhiteboardClient.Controls;

/// <summary>
/// Vùng nhận thao tác chuột/bút/cảm ứng trên bảng vẽ.
/// Kế thừa từ Grid với nền trong suốt để toàn bộ vùng trống đều bắt được sự kiện Pointer,
/// đồng thời mở quyền truy cập ProtectedCursor giúp đổi hình dạng con trỏ chuột theo công cụ.
/// Phụ trách: Thành viên A (Hạng mục T7) &amp; Thành viên C (Hạng mục T9a).
/// </summary>
public sealed class BoardHost : Grid
{
    private InputSystemCursorShape _currentShape = InputSystemCursorShape.Cross;

    public BoardHost()
    {
        Background = new SolidColorBrush(Colors.Transparent);
        SetCursorShape(InputSystemCursorShape.Cross);
    }

    /// <summary>
    /// Đổi hình con trỏ khi rê qua bảng vẽ (chữ thập khi vẽ, mũi tên 4 chiều khi kéo bảng).
    /// Chỉ cấp phát lại đối tượng con trỏ khi hình dạng thực sự thay đổi để tránh rò rỉ tài nguyên.
    /// </summary>
    public void SetCursorShape(InputSystemCursorShape shape)
    {
        if (_currentShape == shape && ProtectedCursor != null) return;
        _currentShape = shape;
        ProtectedCursor = InputSystemCursor.Create(shape);
    }
}
