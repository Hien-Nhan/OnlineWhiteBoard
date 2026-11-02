using System;
using Windows.UI;

namespace WhiteboardClient.Services;

/// <summary>Chuyển đổi qua lại giữa Color và chuỗi "#RRGGBB" dùng trong giao thức.</summary>
public static class HexColor
{
    public static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    /// <summary>Đọc "#RRGGBB"; sai định dạng thì trả về màu đen.</summary>
    public static Color Parse(string? hex)
    {
        if (hex is { Length: 7 } && hex[0] == '#')
        {
            try
            {
                return Color.FromArgb(255,
                    Convert.ToByte(hex.Substring(1, 2), 16),
                    Convert.ToByte(hex.Substring(3, 2), 16),
                    Convert.ToByte(hex.Substring(5, 2), 16));
            }
            catch (FormatException) { }
        }
        return Color.FromArgb(255, 0, 0, 0);
    }
}
