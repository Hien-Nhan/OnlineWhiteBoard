# OnlineWhiteBoard 🎨

> **Hệ thống Bảng Trắng Trực Tuyến Thời Gian Thực (Real-Time Collaborative Whiteboard)**  
> Ứng dụng vẽ và cộng tác đa người dùng xây dựng trên nền tảng **WinUI 3 (Windows App SDK + Win2D)**, giao thức **TCP Socket bất đồng bộ** và cơ sở dữ liệu **SQLite**.

---

## 🌟 Tính Năng Nổi Bật

- **Vẽ cộng tác thời gian thực:** Nhiều người dùng trong cùng một phòng vẽ chung với độ trễ cực thấp (< 50ms trong mạng nội bộ), hỗ trợ hiển thị danh sách người dùng online.
- **Cô lập phòng vẽ độc lập (`RoomID`):** Tự do tạo phòng mới hoặc gia nhập phòng có sẵn; dữ liệu nét vẽ giữa các phòng hoàn toàn riêng biệt.
- **Bộ công cụ vẽ phong phú:**
  - Bút vẽ tự do (Pen) khử răng cưa và bo tròn góc nét (`RoundCap`).
  - Tẩy xóa thông minh (Eraser).
  - Hình khối cơ bản: Đường thẳng, Hình chữ nhật, Hình tròn/Ellipse với chế độ xem trước nét đứt khi kéo chuột.
  - Bảng màu nhanh, hộp chọn màu tùy chỉnh mã HEX và thanh trượt cỡ nét (1 - 30px).
- **Thao tác góc nhìn chuyên nghiệp (Canvas Viewport):**
  - Thu phóng tự do từ **25% đến 800%** bám sát theo vị trí con trỏ chuột (*Anchor Zoom*).
  - Kéo di chuyển bảng vẽ (Pan) mượt mà bằng chuột giữa, chuột phải hoặc công cụ chuyên dụng. Có cơ chế chống trôi mất bảng.
  - Chế độ **Mở rộng bảng** (Focus Mode) ẩn/hiện các thanh công cụ để tối ưu không gian vẽ.
- **Lưu trữ lịch sử bền vững (SQLite Persistence):**
  - Tự động lưu vết toàn bộ nét vẽ vào file cơ sở dữ liệu `whiteboard.db`.
  - Tự động phát lại đầy đủ các nét vẽ cũ cho thành viên mới tham gia phòng sau.
- **Tiện ích phòng & Xuất dữ liệu:**
  - Sao chép nhanh mã phòng vào Clipboard hệ thống chỉ với một click.
  - Xác nhận cảnh báo nguy hiểm trước khi xóa sạch bảng vẽ của phòng.
  - Xuất bảng vẽ ra file ảnh `.png` độ phân giải cao **2560 × 1440 px** (192 DPI).
- **Ổn định & Chống chịu lỗi:**
  - Bắt ngoại lệ socket an toàn, tự động nhận biết mất kết nối và cho phép người dùng rời phòng trở về màn hình đăng nhập an toàn mà không bị crash ứng dụng.

---

## 🏛️ Kiến Trúc Hệ Thống

Dự án được thiết kế theo mô hình phân tầng mô-đun hóa cao trong một Solution duy nhất (`code/WhiteboardApp.slnx`):

```
WhiteboardApp.slnx
├── WhiteboardShared        (.NET 10 Class Library)
│   └── Gói tin NetworkPacket, DrawPacket, bộ parser an toàn, enum công cụ
├── WhiteboardServer        (.NET 10 Console App)
│   ├── Program.cs          - Lắng nghe TcpListener trên cổng cấu hình (mặc định 8888)
│   ├── ServerConfig.cs     - Đọc cấu hình từ config.ini
│   ├── RoomManager.cs      - Bộ điều phối Singleton quản lý danh sách phòng và broadcast
│   ├── ClientHandler.cs    - Quản lý phiên làm việc đa luồng của từng Client
│   └── DatabaseManager.cs  - Tích hợp SQLite lưu trữ lịch sử nét vẽ và dọn bảng
├── WhiteboardClient        (WinUI 3 - Windows App SDK + Win2D)
│   ├── Views/LoginPage     - Giao diện đăng nhập, kết nối IP/Port, nhớ thông tin
│   ├── Views/BoardPage     - Bảng vẽ đồ họa GPU Win2D, xử lý chuột, zoom, pan, công cụ
│   └── Services/           - NetworkClient kết nối TCP socket bất đồng bộ nền
└── WhiteboardStressTest    (.NET 10 Console App)
    └── Công cụ kiểm thử chịu tải hàng chục client kết nối đồng thời
```

---

## 🚀 Hướng Dẫn Cài Đặt & Khởi Chạy Nhanh

### Yêu cầu môi trường:
- Hệ điều hành: **Windows 10 (bản 1809 trở lên)** hoặc **Windows 11**.
- Cài đặt **.NET 10 SDK** (hoặc Visual Studio 2022 phiên bản mới nhất hỗ trợ .NET 10).

### Cách 1: Khởi chạy bằng 1-Click (Khuyên dùng khi demo)
Tại thư mục gốc của dự án, sử dụng các file `.bat` có sẵn:
1. **`run-all.bat`**: Tự động bật máy chủ Server, sau đó tự động bật giao diện Client.
2. **`run-server.bat`**: Khởi chạy riêng máy chủ `WhiteboardServer`.
3. **`run-client.bat`**: Mở thêm một cửa sổ Client mới (có thể mở nhiều Client cùng lúc để thử nghiệm vẽ đồng thời nhiều người).

### Cách 2: Khởi chạy từ dòng lệnh (CLI)
```powershell
# Chạy Server
cd code/WhiteboardServer
dotnet run

# Chạy Client WinUI 3
cd ../WhiteboardClient
dotnet run -p:Platform=x64
```

---

## 🌐 Hướng Dẫn Kết Nối Mạng LAN Giữa 2 Máy Tính

1. **Trên máy chủ (Server):**
   - Chạy `run-server.bat`. Server sẽ in ra địa chỉ IP mạng nội bộ của bạn (ví dụ: `192.168.1.15`).
   - Mở cổng Firewall trên máy Server (nếu cần):
     ```powershell
     New-NetFirewallRule -DisplayName "WhiteboardServer Port 8888" -Direction Inbound -LocalPort 8888 -Protocol TCP -Action Allow
     ```
2. **Trên máy khách (Client):**
   - Kết nối vào cùng mạng WiFi / LAN với máy Server.
   - Mở `run-client.bat`.
   - Tại màn hình đăng nhập: Nhập Tên của bạn, địa chỉ **IP Server** (ví dụ `192.168.1.15`), cổng **8888** và bấm **"Tạo phòng mới"** hoặc **"Vào phòng"**.

---

## 👥 Ma Trận Phân Công Công Việc Nhóm

Dự án áp dụng nghiêm ngặt quy tắc xoay vòng trách nhiệm **`L → A → B → C → D → E → L`**:
*(Người viết tài liệu = Người code + 1, Người review PR = Người code + 2)*

| Hạng mục | Tên công việc | Code | Viết Doc | Review PR | Tài liệu chi tiết |
|:---:|---|:---:|:---:|:---:|---|
| **T0** | Khởi tạo repo + 4 project rỗng | **L** | **A** | **B** | [`Extra/docs/T0-Quy-uoc-repo-va-cau-truc.md`](Extra/docs/T0-Quy-uoc-repo-va-cau-truc.md) |
| **T1** | Shared: Gói tin & Giao thức mạng | **A** | **B** | **C** | [`Extra/docs/T1-Giao-thuc-mang.md`](Extra/docs/T1-Giao-thuc-mang.md) |
| **T2** | Server: TCP nền & Cấu hình mạng | **B** | **C** | **D** | `code/WhiteboardServer/ServerConfig.cs` |
| **T3** | Server: Quản lý phòng & Phát nét vẽ | **C** | **D** | **E** | [`Extra/docs/T3-Server-Quan-ly-phong-va-phat-net-ve.md`](Extra/docs/T3-Server-Quan-ly-phong-va-phat-net-ve.md) |
| **T4** | Server: SQLite lịch sử + Xóa bảng | **D** | **E** | **L** | [`Extra/tests/TC-D.md`](Extra/tests/TC-D.md) |
| **T5** | Client: Khung WinUI 3 & Đăng nhập | **E** | **L** | **A** | `code/WhiteboardClient/Views/LoginPage.xaml` |
| **T6** | Client: Kết nối mạng & Đa luồng Socket | **L** | **A** | **B** | [`Extra/docs/T6-Client-Ket-noi-mang.md`](Extra/docs/T6-Client-Ket-noi-mang.md) |
| **T7** | Client: Khung Win2D & Nét vẽ tự do | **A** | **B** | **C** | [`Extra/tests/TC-A.md`](Extra/tests/TC-A.md) |
| **T8** | Client: Tẩy, hình khối, màu sắc, cỡ nét | **B** | **C** | **D** | `code/WhiteboardClient/Views/BoardPage.xaml` |
| **T9a** | Client: Zoom, kéo bảng & mở rộng bảng | **C** | **D** | **E** | [`Extra/docs/T9a-Client-Zoom-Keo-bang-va-Mo-rong.md`](Extra/docs/T9a-Client-Zoom-Keo-bang-va-Mo-rong.md) |
| **T9b** | Client: Sao chép mã, rời phòng, xóa bảng, lưu PNG | **D** | **E** | **L** | [`Extra/tests/TC-D.md`](Extra/tests/TC-D.md) |
| **T10** | Ổn định hệ thống (Server + Client) & StressTest | **E** | **L** | **A** | `code/WhiteboardStressTest` |
| **T11** | Scripts `.bat`, README & Tổng hợp 15 Test Cases | **L** | **D** | **A** | [`Extra/docs/T11-Script-bat-README-va-Tong-hop-Test-Case.md`](Extra/docs/T11-Script-bat-README-va-Tong-hop-Test-Case.md) |

---

## 🧪 Bộ 15 Test Cases Nghiệm Thu Hệ Thống

Chi tiết từng kịch bản, bước thực hiện và minh chứng xem tại tài liệu:  
👉 [`Extra/docs/T11-Script-bat-README-va-Tong-hop-Test-Case.md`](Extra/docs/T11-Script-bat-README-va-Tong-hop-Test-Case.md)

- **TC_01 - TC_02:** Kiểm tra đăng nhập dữ liệu sai & xử lý lỗi khi Server chưa bật.
- **TC_03 - TC_05:** Tạo phòng, vào phòng, hiển thị danh sách online, sao chép mã và rời phòng an toàn.
- **TC_06 - TC_08:** Kiểm thử công cụ bút vẽ, tẩy, đường thẳng, hình chữ nhật, hình tròn, màu sắc và cỡ nét.
- **TC_09:** Đồng bộ thời gian thực giữa nhiều Client cùng phòng.
- **TC_10:** Lưu trữ và phát lại lịch sử nét vẽ từ cơ sở dữ liệu SQLite (`whiteboard.db`).
- **TC_11 - TC_12:** Xóa bảng cho cả phòng và xuất file ảnh PNG 2560×1440.
- **TC_13:** Thu phóng quanh con trỏ chuột (25% - 800%), kéo bảng và chế độ mở rộng toàn màn hình.
- **TC_14 - TC_15:** Kết nối mạng LAN thực tế và kiểm thử độ bền khi mất kết nối đột ngột.
