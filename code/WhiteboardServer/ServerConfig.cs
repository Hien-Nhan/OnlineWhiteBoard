    
using System;
using System.IO;

namespace WhiteboardServer;

public class ServerConfig
{
    public string IP { get; private set; } = "0.0.0.0";
    public int Port { get; private set; } = 8888;

    public static ServerConfig Load(string? filePath = null)
    {
        filePath ??= Path.Combine(AppContext.BaseDirectory, "config.ini");

        var config = new ServerConfig();

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[CONFIG] Không tìm thấy {filePath}, dùng cấu hình mặc định.");
            return config;
        }

        foreach (string line in File.ReadAllLines(filePath))
        {
            string clean = line.Trim();

            if (string.IsNullOrWhiteSpace(clean) ||
                clean.StartsWith(";") ||
                clean.StartsWith("#") ||
                clean.StartsWith("["))
                continue;

            int index = clean.IndexOf('=');
            if (index <= 0)
                continue;

            string key = clean[..index].Trim();
            string value = clean[(index + 1)..].Trim();

            if (key.Equals("IP", StringComparison.OrdinalIgnoreCase))
            {
                if (System.Net.IPAddress.TryParse(value, out _))
                    config.IP = value;
                else
                    throw new FormatException($"IP không hợp lệ: {value}");
            }
            else if (key.Equals("PORT", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(value, out int port) && port is >= 1 and <= 65535)
                    config.Port = port;
                else
                    throw new FormatException($"PORT không hợp lệ: {value}");
            }
        }

        Console.WriteLine($"[CONFIG] IP={config.IP}, Port={config.Port}");
        return config;
    }
}
