using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Data.Sqlite;
using System.Runtime.InteropServices;
using System.Text;

namespace WhiteboardServer
{
    class Program
    {
        // Cấu hình kết nối tới file database cục bộ
        private static string _dbPath = "Data Source=whiteboard.db";
        private static string _serverIP = "0.0.0.0"; // nghe tren moi card mang (de may khac cung WiFi ket noi duoc)
        private static int _serverPort = 8888;

        // Cấu hình ghi log (đọc từ config.ini, xem ServerLogger.cs)
        private static bool _logEnabled = true;
        private static string _logDir = "logs";
        private static LogLevel _logLevel = LogLevel.Info;
        private static int _logKeepDays = 14;
        private static int _stopLogged = 0;
        private static PosixSignalRegistration? _sigTerm; // giữ tham chiếu để không bị GC
        private static PosixSignalRegistration? _sigHup;

        // Danh sách lưu trữ các kết nối client kết nối tới hệ thống (Dành cho TV4 và TV5)
        private static List<TcpClient> clientList = new List<TcpClient>();
        // Quản lý xem thiết bị Client nào đang đứng ở RoomID nào để cô lập mạng
        private static Dictionary<TcpClient, string> clientRooms = new Dictionary<TcpClient, string>();
        private static Dictionary<TcpClient, string> clientNames = new Dictionary<TcpClient, string>();
        // logic Server cần quản lý:
        public static Dictionary<string, List<TcpClient>> Rooms = new Dictionary<string, List<TcpClient>>();

        static void Main(string[] args)
        {
            // Thiết lập mã hóa UTF-8 cho console để hiển thị tiếng Việt đúng cách
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("--- KHỞI ĐỘNG MÁY CHỦ WHITEBOARD ---");
            Console.WriteLine(">>> Bat dau khoi tao he thong Server...");
            //Nạp thông số mạng động từ file config.ini trước khi mở socket
            DocFileConfigIni("config.ini");

            // Bật ghi log ra file serverlog_yyyy-MM-dd.log (sau khi đã biết cấu hình)
            ServerLogger.Configure(_logEnabled, _logDir, _logLevel, _logKeepDays);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                ServerLogger.Error("[SERVER FATAL] Lỗi không bắt được, server có thể dừng", e.ExceptionObject as Exception);
            // Ghi dòng "đã dừng" khi tắt server bằng Ctrl+C, đóng cửa sổ hoặc tín hiệu hệ điều hành
            AppDomain.CurrentDomain.ProcessExit += (s, e) => GhiLogDungServer("thoát tiến trình");
            Console.CancelKeyPress += (s, e) => GhiLogDungServer("Ctrl+C");
            _sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, ctx => GhiLogDungServer("SIGTERM"));
            _sigHup = PosixSignalRegistration.Create(PosixSignal.SIGHUP, ctx => GhiLogDungServer("SIGHUP"));
            ServerLogger.Info($"=== MÁY CHỦ KHỞI ĐỘNG === IP={_serverIP} | Port={_serverPort} | DB={_dbPath}");
            if (_logEnabled)
                Console.WriteLine($"[LOG] Đang ghi log (mức {_logLevel} trở lên) vào thư mục: {ServerLogger.LogDirectoryFullPath}");
            else
                Console.WriteLine("[LOG] Ghi log ra file đang TẮT (LogEnabled=false trong config.ini).");

            // Tu dong kiem tra va tao bang du lieu ban dau
            TaoDatabaseForm();

            //  KHU VỰC LẬP TRÌNH SOCKET 
            ServerLogger.Info("[SERVER] Khoi dong Socket cho Client ket noi...");

            try
            {
                IPAddress ipAddr = IPAddress.Parse(_serverIP);
                TcpListener server = new TcpListener(ipAddr, _serverPort);
                server.Start();

                ServerLogger.Info($"[SERVER] Dang lang nghe tai cong {_serverPort}...");
                InDiaChiLAN();

                while (true)
                {
                    // Phương thức chặn đứng chờ thiết bị kết nối
                    TcpClient client = server.AcceptTcpClient();

                    client.NoDelay = true;
                    ServerLogger.Info($"[SERVER] Client moi ket noi: {client.Client.RemoteEndPoint}");

                    // Đồng bộ bảo vệ tài nguyên danh sách khi thêm Client mới
                    lock (clientList)
                    {
                        clientList.Add(client);
                    }
                    // Kích hoạt đa luồng xử lý riêng biệt cho Client
                    Thread clientThread = new Thread(() => HandleClient(client));
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                ServerLogger.Error("[SERVER ERROR] Server dung do loi o vong lap chinh", ex);
            }
        }

        // Ghi log "máy chủ đã dừng" đúng một lần dù có nhiều sự kiện tắt cùng lúc
        private static void GhiLogDungServer(string lyDo)
        {
            if (Interlocked.Exchange(ref _stopLogged, 1) == 1) return;
            ServerLogger.Info($"=== MÁY CHỦ ĐÃ DỪNG ({lyDo}) ===");
            ServerLogger.Shutdown();
        }

        // Địa chỉ IP:Port của client (an toàn kể cả khi socket đã đóng)
        private static string LayDiaChiClient(TcpClient client)
        {
            try { return client.Client.RemoteEndPoint?.ToString() ?? "?"; }
            catch { return "?"; }
        }

        // Tên người dùng đã đăng ký với server (dùng cho log)
        private static string LayTenClient(TcpClient client)
        {
            lock (clientNames)
            {
                return clientNames.TryGetValue(client, out var name) ? name : "Ẩn danh";
            }
        }

        // In ra các địa chỉ IP của máy này để các máy khác cùng WiFi nhập vào Client
        private static void InDiaChiLAN()
        {
            Console.WriteLine();
            Console.WriteLine("==================================================");
            Console.WriteLine(" CAC MAY KHAC CUNG WIFI: nhap IP duoi day vao Client");
            Console.WriteLine("==================================================");
            bool found = false;
            var lanIps = new List<string>();
            try
            {
                foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback) continue;

                    string label = (ni.Name + " " + ni.Description).ToLower();
                    bool ao = label.Contains("vmware") || label.Contains("virtual") || label.Contains("vethernet")
                              || label.Contains("hyper-v") || label.Contains("wsl") || label.Contains("vpn")
                              || label.Contains("loopback") || label.Contains("tap");

                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        string ip = ua.Address.ToString();
                        if (ip.StartsWith("169.254.")) continue; // địa chỉ link-local, không dùng được

                        found = true;
                        lanIps.Add($"{ip} ({ni.Name})");
                        Console.WriteLine($"   {ip,-16} <- {ni.Name}{(ao ? "  (mang ao, thuong KHONG dung)" : "")}");
                    }
                }
            }
            catch (Exception ex)
            {
                ServerLogger.Warn("   (Khong doc duoc danh sach IP cua may chu)", ex);
            }
            if (!found) Console.WriteLine("   Chua tim thay IP mang nao. Hay kiem tra ket noi WiFi.");
            ServerLogger.Info(found
                ? $"[SERVER] Dia chi IP cua may chu: {string.Join(", ", lanIps)} | Cong {_serverPort}"
                : "[SERVER] Khong tim thay dia chi IP mang nao.");
            Console.WriteLine($"   Cong (Port): {_serverPort}");
            Console.WriteLine("   Cung may chay Server: dung 127.0.0.1");
            Console.WriteLine("==================================================");
            Console.WriteLine();
        }

        // Cập nhật danh sách User
        private static void BroadcastUserList(string roomID)
        {
            List<string> usersInRoom = new List<string>();

            // Lọc tìm tất cả những người đang ở trong roomID này
            lock (clientRooms)
            {
                foreach (var kvp in clientRooms)
                {
                    if (kvp.Value == roomID)
                    {
                        lock (clientNames)
                        {
                            if (clientNames.ContainsKey(kvp.Key))
                                usersInRoom.Add(clientNames[kvp.Key]);
                        }
                    }
                }
            }

            // Ghép tên thành chuỗi: User1,User2,User3
            string userListString = string.Join(",", usersInRoom);

            // Cấu trúc gọi về Client chỉ còn USER_LIST;DanhSachTen
            string packetData = $"USER_LIST;{userListString}";

            // Bắn danh sách này cho toàn bộ phòng (Dùng lại hàm BroadcastData nhưng truyền sender = null để gửi cho TẤT CẢ)
            BroadcastData(roomID, packetData, null);
        }

        // Hàm HandleClient xử lý dòng dữ liệu mạng Real-time liên tục
        private static void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);

            string remote = LayDiaChiClient(client);
            string currentRoomID = "ROOM001";
            lock (clientRooms)
            {
                clientRooms[client] = currentRoomID;
            }

            try
            {
                while (true)
                {
                    // Đọc gói tin nét vẽ truyền từ Client lên qua mạng
                    string message = reader.ReadLine();
                    if (message == null) break; // Client rời phòng hoặc đóng ứng dụng
                    if (string.IsNullOrWhiteSpace(message)) continue; // bỏ qua dòng trống

                    ServerLogger.Debug($"[NHẬN ĐƯỢC TỪ CLIENT] {remote} | {message}");

                    string username = "Ẩn danh";
                    string packetData = message;

                    // Phân tách nếu Client có gửi kèm tên dạng "Username|Gói_Tin"
                    if (message.Contains("|"))
                    {
                        var parts = message.Split('|');
                        username = parts[0];
                        packetData = parts[1];
                    }

                    // Phân tách giao thức bằng dấu chấm phẩy ;
                    string[] protocolParts = packetData.Split(';');

                    if (protocolParts.Length >= 2)
                    {
                        string command = protocolParts[0];

                        // CẬP NHẬT ROOM ID CHO CÁC LỆNH VÀO PHÒNG
                        if (command == "CONNECT" || command == "JOIN_ROOM" || command == "CREATE_ROOM")
                        {
                            if (protocolParts.Length >= 3)
                            {
                                username = protocolParts[1];
                                currentRoomID = protocolParts[2]; // Nhận mã phòng thực tế từ Client
                            }
                            else if (command == "CONNECT" && protocolParts.Length >= 2)
                            {
                                username = protocolParts[1];
                                currentRoomID = "ROOM001";
                            }

                            // Lưu/Cập nhật thông tin phòng của Client vào Dictionary
                            lock (clientRooms) { clientRooms[client] = currentRoomID; }
                            lock (clientNames) { clientNames[client] = username; }

                            ServerLogger.Info($"[HỆ THỐNG] Thành viên '{username}' đã kết nối vào phòng: {currentRoomID} (lệnh {command}, từ {remote})");

                            // Đồng bộ lịch sử phòng cũ cho người mới vào
                            DongBoLichSuChoClientMoi(client, currentRoomID);

                            // Cập nhật danh sách User hiển thị trên UI cho cả phòng nhìn thấy nhau
                            BroadcastUserList(currentRoomID);
                            continue;
                        }
                        else if (command == "CLEAR_CANVAS" || command == "USER_LIST")
                        {
                            currentRoomID = protocolParts[1];

                            // [ĐÃ SỬA LỖI]: Nếu nhận được lệnh CLEAR_CANVAS, xóa sạch lịch sử vẽ của phòng đó trong SQLite
                            if (command == "CLEAR_CANVAS")
                            {
                                try
                                {
                                    using var conn = new SqliteConnection(_dbPath);
                                    conn.Open();

                                    var sqlDelete = "DELETE FROM DrawHistory WHERE RoomID = @room";
                                    using var cmd = new SqliteCommand(sqlDelete, conn);
                                    cmd.Parameters.AddWithValue("@room", currentRoomID);

                                    int rowsAffected = cmd.ExecuteNonQuery();
                                    ServerLogger.Info($"[SQLite] Đã XÓA SẠCH {rowsAffected} nét vẽ lịch sử của phòng [{currentRoomID}] do '{LayTenClient(client)}' ({remote}) gửi lệnh CLEAR_CANVAS.");
                                }
                                catch (Exception ex)
                                {
                                    ServerLogger.Error($"[SQLite Error] Không thể xóa lịch sử phòng {currentRoomID}", ex);
                                }
                            }
                        }
                        // TRÍCH XUẤT ROOM ID TỪ GÓI VẼ "DRAW"
                        else if (command == "DRAW")
                        {
                            // Vị trí cuối cùng của mảng chính là Room ID được Client đính kèm theo nét vẽ
                            currentRoomID = protocolParts[protocolParts.Length - 1];

                            // Cập nhật lại bộ nhớ tạm thời của Server để chắc chắn không bị lệch phòng khi truyền phát
                            lock (clientRooms) { clientRooms[client] = currentRoomID; }
                        }
                    }

                    // Hành động 1: Lưu nét vẽ vào đúng phòng hiện tại (SQLite)
                    if (packetData.StartsWith("DRAW"))
                    {
                        LuuNetVe(currentRoomID, username, packetData);
                    }

                    // Hành động 2: Phát sóng chuyển tiếp gói tin nguyên bản cho các máy khác CÙNG PHÒNG
                    BroadcastData(currentRoomID, packetData, client);
                }
            }
            catch (Exception ex)
            {
                // Bắt lỗi ngắt kết nối đột ngột của thiết bị mạng
                ServerLogger.Warn($"[MẤT KẾT NỐI] Kết nối {remote} kết thúc do ngoại lệ", ex);
            }
            finally
            {
                // Lấy tên trước khi xóa khỏi danh sách để ghi log cho đúng người
                string leftName = LayTenClient(client);

                int remaining;
                lock (clientList) { clientList.Remove(client); remaining = clientList.Count; }
                lock (clientRooms) { clientRooms.Remove(client); }
                lock (clientNames) { clientNames.Remove(client); }

                client.Close();
                ServerLogger.Info($"[NGẮT KẾT NỐI] '{leftName}' ({remote}) đã thoát khỏi phòng [{currentRoomID}]. Tổng số client còn kết nối: {remaining}.");

                // Gửi tín hiệu điểm danh lại cho phòng
                if (!string.IsNullOrEmpty(currentRoomID))
                {
                    BroadcastUserList(currentRoomID);
                }
            }
        }

        // Bộ nạp thông số file config.ini thủ công
        private static void DocFileConfigIni(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[WARNING] Khong tim thay file {filePath}. He thong su dung cau hinh mac dinh.");
                return;
            }

            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                string cleanLine = line.Trim();
                if (string.IsNullOrEmpty(cleanLine) || cleanLine.StartsWith(";") || cleanLine.StartsWith("["))
                    continue;

                if (cleanLine.Contains("="))
                {
                    int idx = cleanLine.IndexOf('=');
                    string key = cleanLine.Substring(0, idx).Trim().ToLower();
                    string val = cleanLine.Substring(idx + 1).Trim();

                    if (key == "path") _dbPath = val;
                    if (key == "ip") _serverIP = val;
                    if (key == "port") _serverPort = int.Parse(val);

                    // Cấu hình log
                    if (key == "logenabled") _logEnabled = !(val.Equals("false", StringComparison.OrdinalIgnoreCase) || val == "0");
                    if (key == "logdir") _logDir = val;
                    if (key == "loglevel" && ServerLogger.TryParseLevel(val, out var parsedLevel)) _logLevel = parsedLevel;
                    if (key == "logkeepdays" && int.TryParse(val, out var parsedDays) && parsedDays >= 0) _logKeepDays = parsedDays;
                }
            }
            Console.WriteLine($"[CONFIG.INI] Da nap cau hinh: DB={_dbPath} | IP={_serverIP} | Port={_serverPort}");
        }

        // Hàm 1: Khởi tạo tệp tin và tạo cấu trúc bảng chứa nét vẽ
        private static void TaoDatabaseForm()
        {
            using var conn = new SqliteConnection(_dbPath);
            conn.Open();

            var sqlCheckTable = @"
                CREATE TABLE IF NOT EXISTS DrawHistory (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RoomID TEXT NOT NULL DEFAULT 'DefaultRoom',
                    Username TEXT NOT NULL,
                    PacketData TEXT NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );";

            using var cmd = new SqliteCommand(sqlCheckTable, conn);
            cmd.ExecuteNonQuery();

            ServerLogger.Info("[SQLite] Check va khoi tao bang DrawHistory xong.");
        }

        // Hàm 2: Bản ghi nét vẽ khi nhận được tín hiệu qua mạng
        public static void LuuNetVe(string roomID, string nguoiVe, string chuoiToaDo)
        {
            using var conn = new SqliteConnection(_dbPath);
            conn.Open();

            var sqlInsert = "INSERT INTO DrawHistory (RoomID, Username, PacketData) VALUES (@room, @user, @packet)";

            using var cmd = new SqliteCommand(sqlInsert, conn);
            cmd.Parameters.AddWithValue("@room", roomID);
            cmd.Parameters.AddWithValue("@user", nguoiVe);
            cmd.Parameters.AddWithValue("@packet", chuoiToaDo);

            cmd.ExecuteNonQuery();
            ServerLogger.Debug($"[SQLite] Da ghi net ve cua: {nguoiVe} vao Phong: {roomID}");
        }

        // Hàm 3: Truy vấn ngược toàn bộ lịch sử để đồng bộ phòng vẽ
        public static void DocLichSuPhong(string roomID)
        {
            Console.WriteLine($"[SQLite] Lay danh sach lich su cua phong [{roomID}] tu file db...");

            using var conn = new SqliteConnection(_dbPath);
            conn.Open();

            var sqlSelect = "SELECT Id, Username, PacketData FROM DrawHistory WHERE RoomID = @room ORDER BY Id ASC";

            using var cmd = new SqliteCommand(sqlSelect, conn);
            cmd.Parameters.AddWithValue("@room", roomID);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                int maSo = reader.GetInt32(0);
                string tenUser = reader.GetString(1);
                string thongTinMa = reader.GetString(2);

                Console.WriteLine($"   => Phong: {roomID} | Record #{maSo} | User: {tenUser} | Data: {thongTinMa}");
            }
        }

        // Bắn ngược toàn bộ lịch sử lưu trong SQLite cho Client mới kết nối muộn
        private static void DongBoLichSuChoClientMoi(TcpClient targetClient, string roomID)
        {
            try
            {
                var sb = new StringBuilder();
                int count = 0;

                using (var conn = new SqliteConnection(_dbPath))
                {
                    conn.Open();

                    // Chỉ lấy chuỗi PacketData gốc để gửi trực tiếp cho Client vẽ lại
                    var sqlSelect = "SELECT PacketData FROM DrawHistory WHERE RoomID = @room ORDER BY Id ASC";
                    using var cmd = new SqliteCommand(sqlSelect, conn);
                    cmd.Parameters.AddWithValue("@room", roomID);
                    using var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        sb.Append(reader.GetString(0)).Append('\n');
                        count++;
                    }
                }

                if (count > 0)
                {
                    // Ghi một lần, dưới khóa chung với Broadcast để các dòng không bị xen lẫn nhau.
                    // Dùng UTF-8 KHÔNG có BOM.
                    byte[] data = new UTF8Encoding(false).GetBytes(sb.ToString());
                    lock (clientList)
                    {
                        NetworkStream stream = targetClient.GetStream();
                        stream.Write(data, 0, data.Length);
                        stream.Flush();
                    }
                }
                ServerLogger.Info($"[SQLite] Đã đồng bộ xong {count} nét vẽ cũ của phòng [{roomID}] cho thành viên mới.");
            }
            catch (Exception ex)
            {
                ServerLogger.Error($"[SQLite Error] Đồng bộ lịch sử phòng {roomID} thất bại", ex);
            }
        }

        //Gui thong diep toi tat ca client ngoai tru sender
        private static void BroadcastData(string roomID, string message, TcpClient sender)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");

            lock (clientList)
            {
                foreach (TcpClient client in clientList)
                {
                    // Không gửi lại cho chính client gửi
                    if (client == sender)
                        continue;
                    //CÔ LẬP PHÒNG: Kiểm tra nếu client đích không ở cùng RoomID thì bỏ qua
                    lock (clientRooms)
                    {
                        if (!clientRooms.ContainsKey(client) || clientRooms[client] != roomID)
                            continue;
                    }

                    try
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(data, 0, data.Length);
                        ServerLogger.Debug($"[Broadcast Phong {roomID}] Da chuyen tiep tin nhan.");
                    }
                    catch (Exception ex)
                    {
                        ServerLogger.Warn($"[Broadcast Error] Gui toi client that bai (phong {roomID})", ex);
                    }
                }
            }
        }
    }
}
