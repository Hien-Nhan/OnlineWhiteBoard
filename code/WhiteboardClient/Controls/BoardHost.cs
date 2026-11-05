using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace WhiteboardClient.Controls;

/// <summary>
/// Vùng nhận thao tác chuột/bút/cảm ứng trên bảng vẽ. Là một Grid trong suốt (để vùng trống cũng nhận sự kiện)
/// và đổi con trỏ thành chữ thập — WinUI 3 chỉ cho đổi con trỏ từ bên trong lớp dẫn xuất.
/// </summary>
public sealed class BoardHost : Grid
{
    public BoardHost()
    {
        Background = new SolidColorBrush(Colors.Transparent);
        SetCursorShape(InputSystemCursorShape.Cross);
    }

    /// <summary>Đổi hình con trỏ khi rê qua bảng vẽ (chữ thập khi vẽ, bàn tay/mũi tên 4 chiều khi kéo bảng).</summary>
    public void SetCursorShape(InputSystemCursorShape shape)
    {
        ProtectedCursor = InputSystemCursor.Create(shape);
    }
}
