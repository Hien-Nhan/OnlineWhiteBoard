using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace WhiteboardClient.Models;

/// <summary>Một ô màu trong bảng màu nhanh.</summary>
public sealed class ColorSwatch
{
    public ColorSwatch(Color color)
    {
        Color = color;
        Brush = new SolidColorBrush(color);
    }

    public Color Color { get; }
    public SolidColorBrush Brush { get; }
}
