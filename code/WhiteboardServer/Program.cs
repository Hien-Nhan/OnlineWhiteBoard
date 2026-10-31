
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WhiteboardServer;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        Console.WriteLine("==================================================");
        Console.WriteLine("        MAY CHU WHITEBOARD ONLINE (SERVER)        ");
        Console.WriteLine("==================================================");

        try
        {
            // Doc cau hinh tu config.ini (T2 - Thanh vien B)
            ServerConfig config = ServerConfig.Load();

            IPAddress ipAddr = IPAddress.Parse(config.IP);
            TcpListener listener = new TcpListener(ipAddr, config.Port);

            listener.Start();

            Console.WriteLine();
            Console.WriteLine("==================================================");
            Console.WriteLine($"[SERVER] Dang lang nghe ket noi tai cong {config.Port}");
            Console.WriteLine("==================================================");

            // Hien thi cac dia chi IP de Client ket noi
            PrintLocalIPs(config.Port);

            Console.WriteLine("[SERVER] Server san sang nhan ket noi...");
            Console.WriteLine();

            // Vong lap nhan ket noi tu Client
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();

                Console.WriteLine($"[SERVER] Client moi ket noi: {client.Client.RemoteEndPoint}");

                // Khoi tao ClientHandler va quan ly Room
                var handler = new ClientHandler(client, RoomManager.Instance);
                handler.Start();
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[SERVER SOCKET ERROR] {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SERVER FATAL ERROR] {ex.Message}");
        }
    }

    private static void PrintLocalIPs(int port)
    {
        Console.WriteLine();
        Console.WriteLine(">>> DIA CHI IP DE KET NOI TU CLIENT:");
        Console.WriteLine($"   - Cung may (Localhost): 127.0.0.1 (Cong {port})");

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

                    Console.WriteLine($"   - Cung mang WiFi / LAN ({ni.Name}): {ip}");
                }
            }
        }
        catch
        {
            // Bo qua neu khong doc duoc danh sach card mang
        }

        Console.WriteLine();
    }
}
