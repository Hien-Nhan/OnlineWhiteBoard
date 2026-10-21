using System;
using System.Globalization;

namespace WhiteboardShared;

/// <summary>Loại nét vẽ: vẽ tự do (bút/tẩy) hoặc hình khối kéo thả.</summary>
public enum DrawKind
{
    Freehand,
    Shape
}

/// <summary>Loại hình khối.</summary>
public enum ShapeKind
{
    Line,
    Rectangle,
    Circle
}

/// <summary>
/// Gói tin nét vẽ — đúng định dạng dây (wire format) mà Client đang gửi và Server đang chuyển tiếp:
/// <code>
/// DRAW;FREEHAND;x1;y1;x2;y2;#RRGGBB;cỡ;mã_phòng
/// DRAW;SHAPE;LINE|RECTANGLE|CIRCLE;x1;y1;x2;y2;#RRGGBB;cỡ;mã_phòng
/// </code>
/// Không phụ thuộc giao diện nên Client (WinUI 3), StressTest và các bài kiểm thử đều dùng chung được.
/// </summary>
public sealed class DrawPacket
{
    public const string CommandName = "DRAW";
    public const string FreehandTag = "FREEHAND";
    public const string ShapeTag = "SHAPE";
    public const int MinSize = 1;
    public const int MaxSize = 200;
    public const string DefaultColor = "#000000";

    public DrawKind Kind { get; init; }
    public ShapeKind Shape { get; init; }
    public int X1 { get; init; }
    public int Y1 { get; init; }
    public int X2 { get; init; }
    public int Y2 { get; init; }
    public string ColorHex { get; init; } = DefaultColor;
    public int Size { get; init; } = MinSize;
    public string RoomId { get; init; } = "";

    public static DrawPacket CreateFreehand(int x1, int y1, int x2, int y2, string colorHex, int size, string roomId) =>
        new()
        {
            Kind = DrawKind.Freehand,
            X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
            ColorHex = NormalizeColor(colorHex),
            Size = ClampSize(size),
            RoomId = roomId
        };

    public static DrawPacket CreateShape(ShapeKind shape, int x1, int y1, int x2, int y2, string colorHex, int size, string roomId) =>
        new()
        {
            Kind = DrawKind.Shape,
            Shape = shape,
            X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
            ColorHex = NormalizeColor(colorHex),
            Size = ClampSize(size),
            RoomId = roomId
        };

    /// <summary>Chuỗi gửi lên Server (không kèm ký tự xuống dòng — lớp mạng tự thêm).</summary>
    public string Serialize()
    {
        string common = string.Join(";",
            X1.ToString(CultureInfo.InvariantCulture),
            Y1.ToString(CultureInfo.InvariantCulture),
            X2.ToString(CultureInfo.InvariantCulture),
            Y2.ToString(CultureInfo.InvariantCulture),
            ColorHex,
            Size.ToString(CultureInfo.InvariantCulture),
            RoomId);

        return Kind == DrawKind.Freehand
            ? $"{CommandName};{FreehandTag};{common}"
            : $"{CommandName};{ShapeTag};{ShapeToTag(Shape)};{common}";
    }

    /// <summary>
    /// Giải một dòng nhận từ Server. Trả về false nếu không phải gói DRAW hợp lệ
    /// (không ném ngoại lệ — gói sai định dạng chỉ đơn giản là bị bỏ qua).
    /// </summary>
    public static bool TryParse(string? line, out DrawPacket? packet)
    {
        packet = null;
        if (string.IsNullOrWhiteSpace(line)) return false;

        string[] p = line.Replace("\0", "").Replace("\uFEFF", "").Trim().Split(';');
        if (p.Length < 2 || p[0] != CommandName) return false;

        int offset;
        DrawKind kind;
        ShapeKind shape = ShapeKind.Line;

        if (p[1] == FreehandTag && p.Length >= 9)
        {
            kind = DrawKind.Freehand;
            offset = 2;
        }
        else if (p[1] == ShapeTag && p.Length >= 10)
        {
            kind = DrawKind.Shape;
            shape = TagToShape(p[2]);
            offset = 3;
        }
        else
        {
            return false;
        }

        if (!TryInt(p[offset], out int x1) || !TryInt(p[offset + 1], out int y1) ||
            !TryInt(p[offset + 2], out int x2) || !TryInt(p[offset + 3], out int y2) ||
            !TryDouble(p[offset + 5], out double size))
        {
            return false;
        }

        packet = new DrawPacket
        {
            Kind = kind,
            Shape = shape,
            X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
            ColorHex = NormalizeColor(p[offset + 4]),
            Size = ClampSize((int)Math.Round(size)),
            RoomId = p[offset + 6].Trim()
        };
        return true;
    }

    /// <summary>Chuẩn hóa về dạng #RRGGBB (chữ hoa); sai định dạng thì trả về màu đen.</summary>
    public static string NormalizeColor(string? hex)
    {
        if (hex is { Length: 7 } && hex[0] == '#')
        {
            bool ok = true;
            for (int i = 1; i < 7 && ok; i++) ok = Uri.IsHexDigit(hex[i]);
            if (ok) return hex.ToUpperInvariant();
        }
        return DefaultColor;
    }

    public static int ClampSize(int size) => Math.Max(MinSize, Math.Min(MaxSize, size));

    private static string ShapeToTag(ShapeKind shape) => shape switch
    {
        ShapeKind.Rectangle => "RECTANGLE",
        ShapeKind.Circle => "CIRCLE",
        _ => "LINE"
    };

    private static ShapeKind TagToShape(string tag) => tag switch
    {
        "RECTANGLE" => ShapeKind.Rectangle,
        "CIRCLE" => ShapeKind.Circle,
        _ => ShapeKind.Line
    };

    private static bool TryInt(string s, out int value) =>
        int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

    // Chấp nhận cả "4.0" lẫn "4,0" như Client WinForms cũ.
    private static bool TryDouble(string s, out double value) =>
        double.TryParse(s.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
}
