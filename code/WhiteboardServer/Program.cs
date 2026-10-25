using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WhiteboardServer;

class Program
{
    private static string _serverIP = "0.0.0.0";
    private static int _serverPort = 8888;

    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        Console.WriteLine("==================================================");
        Console.WriteLine("        MÁY CHỦ WHITEBOARD ONLINE (SERVER)        ");
        Console.WriteLine("==================================================");

        // Đọc cấu hình từ config.ini nếu có sẵn (Hỗ trợ T2 của Thành viên B)
        LoadConfigIni("config.ini");

        try
        {
            IPAddress ipAddr = IPAddress.Parse(_serverIP);
            TcpListener listener = new TcpListener(ipAddr, _serverPort);
            listener.Start();

            Console.WriteLine($"[SERVER] Đang lắng nghe kết nối tại cổng {_serverPort}...");
            PrintLocalIPs();

            Console.WriteLine("[SERVER] Sẵn sàng phục vụ các phòng vẽ. Nhấn Ctrl+C để dừng.");
            Console.WriteLine("--------------------------------------------------");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();

                // Chuyển kết nối cho ClientHandler xử lý (Hạng mục T3 - Thành viên C)
                var handler = new ClientHandler(client, RoomManager.Instance);
                handler.Start();
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[SOCKET ERROR] {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SERVER FATAL ERROR] {ex.Message}");
        }
    }

    private static void LoadConfigIni(string filePath)
    {
        if (!File.Exists(filePath)) return;

        try
        {
            foreach (var line in File.ReadAllLines(filePath))
            {
                string clean = line.Trim();
                if (string.IsNullOrEmpty(clean) || clean.StartsWith(";") || clean.StartsWith("#") || clean.StartsWith("["))
                    continue;

                int idx = clean.IndexOf('=');
                if (idx > 0)
                {
                    string key = clean[..idx].Trim().ToLowerInvariant();
                    string val = clean[(idx + 1)..].Trim();

                    if (key == "ip") _serverIP = val;
                    if (key == "port" && int.TryParse(val, out int p)) _serverPort = p;
                }
            }
            Console.WriteLine($"[CONFIG] Đã nạp cấu hình từ {filePath}: IP={_serverIP}, Port={_serverPort}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CONFIG WARNING] Không thể đọc {filePath}: {ex.Message}");
        }
    }

    private static void PrintLocalIPs()
    {
        Console.WriteLine();
        Console.WriteLine(">>> Địa chỉ IP để kết nối từ Client:");
        Console.WriteLine($"   • Cùng máy (Localhost): 127.0.0.1 (Cổng {_serverPort})");

        try
        {
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback) continue;

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    string ip = ua.Address.ToString();
                    if (ip.StartsWith("169.254.")) continue;

                    Console.WriteLine($"   • Cùng mạng WiFi / LAN ({ni.Name}): {ip}");
                }
            }
        }
        catch
        {
            // Bỏ qua nếu không đọc được danh sách card mạng
        }
        Console.WriteLine();
    }
}
