# Hướng dẫn chạy Whiteboard Online (không cần Visual Studio)

## Cách nhanh nhất: bấm đúp `Whiteboard.bat`
File `Whiteboard.bat` ở thư mục gốc mở một menu, chọn bằng số:

| Số | Chức năng | File thật (trong `Extra/scripts/`) |
|----|-----------|------------------------------------|
| 1 | Chạy Server (tự in các địa chỉ IP của máy) | `1-Chay-Server.bat` |
| 2 | Chạy Client | `2-Chay-Client.bat` |
| 3 | Mở Firewall cho Server (tự xin quyền Administrator) | `3-Mo-Firewall.bat` |
| 4 | Đóng gói Client sang thư mục `Client-Publish` | `4-Dong-Goi-Client.bat` |
| 5 | Xem địa chỉ IP của máy | (có sẵn trong menu) |

Mỗi file trong `Extra/scripts/` cũng bấm đúp chạy riêng được. Các script tự kiểm tra đã cài .NET SDK chưa.

## Thao tác trên bảng vẽ
| Thao tác | Cách làm |
|----------|----------|
| Phóng to / thu nhỏ | Cuộn chuột (zoom quanh con trỏ), hoặc nút **+ / −** ở góc dưới phải. Từ 25% đến 800% |
| Về lại vừa khít cửa sổ | Bấm vào ô phần trăm (ví dụ "108%") ở góc dưới phải |
| Kéo / di chuyển bảng | Giữ **chuột phải** hoặc **chuột giữa** rồi kéo, hoặc chọn công cụ **Di chuyển bảng** rồi kéo bằng chuột trái / ngón tay |
| Bảng to hơn | Bấm **Mở rộng bảng** ở thanh trên để ẩn hai panel bên; bấm lại để hiện |
| Vẽ hình | Chọn Đường thẳng / Hình chữ nhật / Hình tròn, kéo chuột (có nét đứt xem trước), nhả chuột để chốt |

Zoom chỉ thay đổi cách bạn xem, không ảnh hưởng người khác. Ảnh lưu ra có độ phân giải 2560 x 1440.

## Yêu cầu
- **Windows 10 (1809 trở lên) hoặc Windows 11** — Client WinUI 3 chỉ chạy trên Windows.
- Đã cài **.NET SDK 10** (kiểm tra: `dotnet --version`).
- Lần chạy đầu cần Internet (tải SQLite cho Server; Windows App SDK + Win2D cho Client).
- Server là ứng dụng console thuần nên có thể chạy trên máy khác hệ điều hành; chỉ Client cần Windows.

## Chạy trên 1 máy (thử nghiệm)
Chọn **1** để chạy Server (giữ cửa sổ này mở), rồi chọn **2** để mở Client.
Muốn thêm người dùng thử thì chọn **2** thêm lần nữa; ở Client đầu tiên nhập tên -> "Tạo phòng mới" -> "Sao chép mã",
ở các Client sau nhập tên khác + mã phòng -> "Vào phòng". IP Server để `127.0.0.1`.


## Chạy nhiều máy cùng WiFi
**Máy làm Server:**
1. Chọn **3** trong menu (hoặc bấm đúp `3-Mo-Firewall.bat`), bấm "Yes" khi Windows hỏi quyền Administrator. Chỉ làm 1 lần.
2. Chọn **1** để chạy Server. Cửa sổ sẽ in ra các địa chỉ IP, ví dụ `192.168.1.25`.
   Chọn IP thuộc WiFi (thường bắt đầu bằng 192.168. hoặc 10.), bỏ qua dòng ghi "mang ao".

**Các máy Client (kể cả máy Server):**
1. Mở Client (chọn **2** trong menu, hoặc `WhiteboardClient.exe` nếu đã đóng gói bằng mục **4**).
2. Nhập **Tên** + **Địa chỉ Server (IP)** vừa thấy + Cổng 8888.
3. Người đầu tiên bấm "Tạo phòng mới", gửi **mã phòng** (nút "Sao chép mã") cho người khác.
4. Người khác nhập mã phòng rồi bấm "Vào phòng".

## Mở bằng Visual Studio
- Mở `code/WhiteboardApp.slnx` (Visual Studio 2022 17.13+ hoặc 2026, có workload **".NET desktop development"** và **"Windows application development"**).
- Chọn **WhiteboardServer** chạy trước, sau đó **WhiteboardClient**.
- Nếu Visual Studio báo lỗi nền tảng "Any CPU" khi build Client, hãy chạy bằng mục **2** của menu (`2-Chay-Client.bat`)
  (hoặc `dotnet run -p:Platform=x64` trong thư mục `code/WhiteboardClient`).

## Nếu không kết nối được
- Tất cả máy phải cùng một WiFi. WiFi công cộng/trường học thường chặn các máy nhìn thấy nhau
  ("client isolation") -> dùng hotspot điện thoại để thử.
- Đã chạy mục 4 (Mở Firewall) chưa? Mạng WiFi nên đặt là "Private".
- Từ máy Client, kiểm tra trong PowerShell: `Test-NetConnection 192.168.1.25 -Port 8888`
  (thay bằng IP Server). Kết quả `TcpTestSucceeded : True` là thông.
- Nếu dùng .NET 8 thay vì 10: đổi `net10.0` thành `net8.0` ở `WhiteboardServer`, `WhiteboardShared`,
  `WhiteboardStressTest`, và `net10.0-windows10.0.19041.0` thành `net8.0-windows10.0.19041.0` ở `WhiteboardClient`.
