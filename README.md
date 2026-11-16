# OnlineWhiteBoard

Bảng trắng trực tuyến cho phép nhiều người cùng vào một phòng và **vẽ chung theo thời gian thực**, dùng để trao đổi và phác thảo ý kiến nhóm.

## Thông tin nhóm

**Giáo viên hướng dẫn:** Mai Ngọc Châu

| Ký hiệu | Họ tên | GitHub |
|---|---|---|
| Leader | Huỳnh Võ Hiền Nhân | [Hien-Nhan](https://github.com/Hien-Nhan) |
| TV1 | Phan Thanh Tùng | [tungf2006](https://github.com/tungf2006) |
| TV2 | Bùi Minh Hiếu | [hieuminh636](https://github.com/hieuminh636) |
| TV3 | Phạm Trung Hiếu | [phamhieu71874](https://github.com/phamhieu71874) |
| TV4 | Lê Như Hoàng | [HoangLN714](https://github.com/HoangLN714) |
| TV5 | Phạm Trí Nguyên | [phamtringuyen66-lgtm](https://github.com/phamtringuyen66-lgtm) |

## Công nghệ sử dụng

| Thành phần | Công nghệ |
|---|---|
| Ngôn ngữ / nền tảng | C#, .NET 10 |
| Giao diện Client | **WinUI 3** (Windows App SDK 1.8), phong cách Fluent, nền Mica, hỗ trợ chế độ sáng/tối |
| Vẽ đồ họa | **Win2D** (`CanvasControl`, `CanvasRenderTarget`) |
| Mạng | **TCP Socket** (`TcpListener` / `TcpClient`), giao thức văn bản, mỗi lệnh một dòng |
| Xử lý đồng thời | Server dùng một luồng riêng cho mỗi client; Client nhận dữ liệu ở luồng nền rồi chuyển về giao diện |
| Cơ sở dữ liệu | **SQLite** (`Microsoft.Data.Sqlite`) lưu lịch sử nét vẽ theo phòng |
| Công cụ | Visual Studio, Git/GitHub, file `.bat` để chạy nhanh |

## Kiến trúc

```
 Client (WinUI 3)  ──TCP──►  Server (.NET console)  ──►  SQLite
 Client (WinUI 3)  ◄──TCP──  (quản lý phòng, phát lại nét vẽ cho cả phòng)
```

- Bảng vẽ có kích thước logic cố định **1280 × 720** nên mọi máy, dù độ phân giải khác nhau, đều thấy cùng một hình.
- Các lệnh chính: `CREATE_ROOM`, `JOIN_ROOM`, `DRAW`, `CLEAR_CANVAS`, `USER_LIST`.

## Chạy được bằng localhost và LAN

| Chế độ | Cách dùng |
|---|---|
| **Localhost** (một máy) | Chạy Server và Client trên cùng một máy, ở ô *Địa chỉ Server* nhập `127.0.0.1`, cổng `8888` |
| **LAN / WiFi** (nhiều máy) | Chạy Server trên một máy, các máy còn lại cùng WiFi nhập **IP của máy Server** (dạng `192.168.x.x`, hiển thị trong cửa sổ Server), cổng `8888` |

Server mặc định nghe trên mọi card mạng (`IP=0.0.0.0`, `PORT=8888` trong `config.ini`).
Khi chạy LAN, chọn mục **Mở Firewall** trong menu một lần trên máy Server. Nếu máy khác không vào được, dùng mục **Kiểm tra kết nối** để biết nguyên nhân. Hướng dẫn chi tiết xem `Extra/HUONG_DAN.md`.

## Tóm tắt chức năng hiện có

**Phòng và người dùng**
- Tạo phòng mới (tự sinh mã phòng) hoặc vào phòng có sẵn bằng mã
- Sao chép mã phòng để gửi cho người khác
- Danh sách người đang online trong phòng
- Ghi nhớ tên và địa chỉ Server cho lần mở sau
- Thông báo rõ nguyên nhân khi kết nối thất bại hoặc bị mất kết nối

**Công cụ vẽ**
- Bút vẽ tự do và tẩy
- Đường thẳng, hình chữ nhật, hình tròn (có nét đứt xem trước khi kéo)
- Bảng màu nhanh và chọn màu tùy ý
- Điều chỉnh cỡ nét
- Hỗ trợ chuột, bút cảm ứng và màn hình cảm ứng

**Cộng tác thời gian thực**
- Nét vẽ của mỗi người hiện ngay trên máy của mọi người trong phòng
- Người vào sau tự nhận lại toàn bộ lịch sử nét vẽ của phòng (lưu trong SQLite)
- Xóa bảng đồng bộ cho cả phòng (có hộp thoại xác nhận)

**Xem và lưu bảng**
- Phóng to / thu nhỏ từ 25% đến 800% (cuộn chuột hoặc nút +/−)
- Kéo di chuyển bảng (chuột phải, chuột giữa hoặc công cụ *Di chuyển bảng*)
- Chế độ *Mở rộng bảng* để ẩn hai thanh bên
- Lưu bảng vẽ thành ảnh PNG (2560 × 1440)

## Yêu cầu hệ thống

- Client: **Windows 10 (1809) trở lên hoặc Windows 11**
- **.NET SDK 10** (kiểm tra bằng `dotnet --version`)
- Lần chạy đầu cần Internet để tải các gói NuGet

## Cách chạy nhanh

Bấm đúp **`Whiteboard.bat`** ở thư mục gốc để mở menu:

| Số | Chức năng |
|---|---|
| 1 | Chạy Server |
| 2 | Chạy Client |
| 3 | Mở Firewall cho Server (chạy nhiều máy) |
| 4 | Đóng gói Client để chạy trên máy khác |
| 5 | Kiểm tra kết nối |
| 6 | Xem địa chỉ IP của máy |

Hoặc dùng Visual Studio: mở `code/WhiteboardApp.slnx`, chạy **WhiteboardServer** trước rồi **WhiteboardClient**.

## Cấu trúc thư mục

```
OnlineWhiteBoard/
├── Whiteboard.bat                 Menu chạy nhanh
├── code/
│   ├── WhiteboardApp.slnx
│   ├── WhiteboardClient/          WinUI 3: App, MainWindow, Views/, Controls/, Models/, Services/
│   ├── WhiteboardServer/          TCP server + SQLite
│   ├── WhiteboardShared/          Gói tin dùng chung (NetworkPacket, DrawPacket)
│   └── WhiteboardStressTest/      Công cụ kiểm tra tải
└── Extra/
    ├── HUONG_DAN.md               Hướng dẫn chạy chi tiết
    ├── scripts/                   Các file .bat và script kiểm tra kết nối
    ├── progress/                  Tiến độ từng thành viên
    ├── rulegit.md                 Quy tắc làm việc với Git
    └── Whiteboard_System_Test_Cases.xlsx
```