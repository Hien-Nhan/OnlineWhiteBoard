using System;

namespace WhiteboardShared
{
    public class NetworkPacket
    {
        // Định nghĩa các lệnh hệ thống để tránh gõ sai chính tả
        public const string CONNECT = "CONNECT";
        public const string DISCONNECT = "DISCONNECT";
        public const string DRAW = "DRAW";
        // Các thuộc tính cơ bản của gói tin
        public string? Command { get; set; }      // Tên lệnh (DRAW, CREATE_ROOM, JOIN_ROOM, v.v.)
        public string? Username { get; set; }     // Tên người dùng tương tác

        // --- THUỘC TÍNH MỚI BỔ SUNG ---
        public string? RoomID { get; set; }       // Mã phòng vẽ (Dùng để phân luồng dữ liệu)
        public string? RoomName { get; set; }     // Tên phòng vẽ (Dùng khi khởi tạo phòng)

        public string? Data { get; set; }         // Dữ liệu phụ trợ (tọa độ vẽ, hoặc chuỗi danh sách user)
       

        // Hàm hỗ trợ đóng gói nhanh dữ liệu theo quy ước chuỗi kết thúc bằng \n
        public static string Pack(string command, string data)
        {
            return $"{command};{data}\n";
        }
        public string ToProtocolString()
        {
            switch (Command)
            {
                case "CREATE_ROOM":
                    // Cấu trúc: CREATE_ROOM;Tên_User;Tên_Phòng\n
                    return $"CREATE_ROOM;{Username};{RoomName}\n";

                case "JOIN_ROOM":
                    // Cấu trúc: JOIN_ROOM;Tên_User;Mã_Phòng\n
                    return $"JOIN_ROOM;{Username};{RoomID}\n";

                case "USER_LIST":
                    // Cấu trúc: USER_LIST;Mã_Phòng;User1,User2,User3\n
                    return $"USER_LIST;{RoomID};{Data}\n";

                case "CLEAR_CANVAS":
                    // Cấu trúc: CLEAR_CANVAS;Mã_Phòng\n
                    return $"CLEAR_CANVAS;{RoomID}\n";

                case "DRAW":
                    // Cấu trúc mở rộng: DRAW;Mã_Phòng;X1,Y1;X2,Y2;ColorHex;Thickness\n
                    return $"DRAW;{RoomID};{Data}\n";

                default:
                    return $"{Command};{Data}\n";
            }
        }

        /// <summary>
        /// Hàm giải gói (Parse) chuỗi nhận từ Socket mạng ngược thành đối tượng NetworkPacket cấu trúc
        /// </summary>
        public static NetworkPacket? Parse(string protocolString)
        {
            if (string.IsNullOrWhiteSpace(protocolString)) return null;

            // Loại bỏ ký tự xuống dòng \r\n và cắt chuỗi bằng dấu chấm phẩy ;
            string[] parts = protocolString.TrimEnd('\r', '\n').Split(';');
            if (parts.Length == 0) return null;

            var packet = new NetworkPacket { Command = parts[0] };

            try
            {
                switch (packet.Command)
                {
                    case "CREATE_ROOM":
                        if (parts.Length >= 3)
                        {
                            packet.Username = parts[1];
                            packet.RoomName = parts[2];
                        }
                        break;

                    case "JOIN_ROOM":
                        if (parts.Length >= 3)
                        {
                            packet.Username = parts[1];
                            packet.RoomID = parts[2];
                        }
                        break;

                    case "USER_LIST":
                        if (parts.Length >= 3)
                        {
                            packet.RoomID = parts[1];
                            packet.Data = parts[2];
                        }
                        else if (parts.Length == 2)
                        {
                            packet.Data = parts[1];
                        }
                        break;

                    case "CLEAR_CANVAS":
                        if (parts.Length >= 2)
                        {
                            packet.RoomID = parts[1];
                        }
                        break;

                    case "DRAW":
                        if (parts.Length >= 3)
                        {
                            // Theo wire format chuẩn: RoomID luôn nằm ở cuối gói DRAW
                            packet.RoomID = parts[parts.Length - 1].Trim();
                            // Gộp toàn bộ nội dung lệnh vẽ còn lại vào Data
                            packet.Data = string.Join(";", parts, 1, parts.Length - 2);
                        }
                        break;
                }
            }
            catch
            {
                return null; // Trả về null nếu chuỗi bị sai định dạng cấu trúc mạng
            }

            return packet;
        }
    }
}