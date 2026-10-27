using System;
using System.IO;

namespace WhiteboardClient.Services;

/// <summary>Thông tin đăng nhập được nhớ cho lần mở sau.</summary>
public sealed class ClientSettings
{
    public string Name { get; set; } = "";
    public string Server { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8888;
}

/// <summary>
/// Lưu tên + IP Server vào %LOCALAPPDATA%\WhiteboardOnline\client_settings.txt (định dạng key=value như bản cũ).
/// Không dùng ApplicationData của WinRT vì ứng dụng unpackaged không có package identity.
/// </summary>
public static class SettingsService
{
    private static readonly string Folder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WhiteboardOnline");

    private static readonly string FilePath = Path.Combine(Folder, "client_settings.txt");

    public static ClientSettings Load()
    {
        var settings = new ClientSettings();
        try
        {
            if (!File.Exists(FilePath)) return settings;
            foreach (string line in File.ReadAllLines(FilePath))
            {
                int idx = line.IndexOf('=');
                if (idx <= 0) continue;
                string key = line.Substring(0, idx).Trim().ToLowerInvariant();
                string val = line.Substring(idx + 1).Trim();
                if (val.Length == 0) continue;

                if (key == "name") settings.Name = val;
                else if (key == "server") settings.Server = val;
                else if (key == "port" && int.TryParse(val, out int port) && port is >= 1 and <= 65535) settings.Port = port;
            }
        }
        catch (Exception) { /* file hỏng/không đọc được: dùng giá trị mặc định */ }
        return settings;
    }

    public static void Save(ClientSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllLines(FilePath, new[]
            {
                "name=" + settings.Name,
                "server=" + settings.Server,
                "port=" + settings.Port
            });
        }
        catch (Exception) { /* không lưu được cũng không sao */ }
    }
}
