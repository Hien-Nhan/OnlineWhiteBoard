using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WhiteboardClient.Services;

/// <summary>
/// Quản lý kết nối TCP tới Server (giao thức văn bản, mỗi lệnh một dòng, phân tách bằng dấu ';').
/// Quy trình: ConnectAsync() -> (đăng ký sự kiện) -> StartReceiving() -> SendMessage()...
/// </summary>
public static class NetworkClient
{
    private static TcpClient? _client;
    private static StreamWriter? _writer;
    private static StreamReader? _reader;
    private static Thread? _readThread;
    private static readonly object SendLock = new();

    /// <summary>Có một dòng dữ liệu từ Server (chạy trên luồng nền, KHÔNG phải luồng giao diện).</summary>
    public static event Action<string>? OnDataReceived;

    /// <summary>Mất kết nối ngoài ý muốn (Server tắt, rớt mạng...). Chạy trên luồng nền.</summary>
    public static event Action? OnDisconnected;

    public static string Host { get; private set; } = "";
    public static int Port { get; private set; }

    public static bool IsConnected
    {
        get
        {
            TcpClient? c = _client;
            return c != null && c.Connected;
        }
    }

    /// <summary>Kết nối tới Server. Ném Exception nếu thất bại hoặc quá thời gian chờ.</summary>
    public static async Task ConnectAsync(string host, int port, int timeoutMs)
    {
        Disconnect();

        var client = new TcpClient();
        try
        {
            using var cts = new CancellationTokenSource(timeoutMs);
            try
            {
                await client.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("Hết thời gian chờ kết nối.");
            }
        }
        catch
        {
            client.Close();
            throw;
        }

        client.NoDelay = true;
        NetworkStream stream = client.GetStream();
        var utf8 = new UTF8Encoding(false);

        _writer = new StreamWriter(stream, utf8) { AutoFlush = true, NewLine = "\n" };
        _reader = new StreamReader(stream, utf8);
        _client = client;
        Host = host;
        Port = port;
    }

    /// <summary>Bắt đầu luồng nhận dữ liệu. Gọi sau khi giao diện đã đăng ký xong sự kiện.</summary>
    public static void StartReceiving()
    {
        TcpClient? client = _client;
        StreamReader? reader = _reader;
        if (client == null || reader == null || _readThread != null) return;

        _readThread = new Thread(() =>
        {
            try
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    OnDataReceived?.Invoke(line);
                }
            }
            catch (Exception)
            {
                // Kết nối bị đóng: xử lý ngay bên dưới
            }

            // Chỉ báo "mất kết nối" nếu đây vẫn là kết nối hiện tại
            // (nếu người dùng tự rời phòng thì _client đã bị gán null).
            if (ReferenceEquals(client, _client))
            {
                OnDisconnected?.Invoke();
            }
        })
        {
            IsBackground = true
        };
        _readThread.Start();
    }

    /// <summary>Gửi một dòng lệnh tới Server. Trả về false nếu gửi thất bại.</summary>
    public static bool SendMessage(string message)
    {
        StreamWriter? writer = _writer;
        if (writer == null || !IsConnected) return false;
        try
        {
            lock (SendLock)
            {
                writer.WriteLine(message.TrimEnd('\r', '\n'));
            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Đóng kết nối (người dùng chủ động rời đi).</summary>
    public static void Disconnect()
    {
        TcpClient? client = _client;
        _client = null;
        _writer = null;
        _reader = null;
        _readThread = null;
        if (client != null)
        {
            try { client.Close(); } catch (Exception) { }
        }
    }
}
