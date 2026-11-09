# Tài Liệu T11: Hướng Dẫn Vận Hành Script .BAT, README & Tổng Hợp Test Cases

- **Người thực hiện tài liệu:** Thành viên D
- **Người thực hiện code:** Leader L
- **Người review PR:** Thành viên A
- **Hạng mục code liên quan:** Hạng mục **T11** (Scripts `.bat`, `README.md`, Tổng hợp ma trận kiểm thử hệ thống)
- **Hạng mục phụ thuộc:** Hạng mục **T10** (Ổn định hệ thống & Stress Test)

---

## 1. Mục tiêu & Tổng quan

Hạng mục **T11** là khâu đóng gói hoàn thiện cuối cùng của dự án **OnlineWhiteBoard**, giúp giảng viên, hội đồng đánh giá và các thành viên nhóm có thể:
1. **Khởi chạy ứng dụng chỉ với 1 click:** Tự động hóa quá trình kích hoạt Server và Client mà không cần mở môi trường Visual Studio hay gõ lệnh phức tạp.
2. **Hướng dẫn kết nối mạng LAN thực tế:** Cung cấp hướng dẫn mở cổng Windows Firewall để kết nối nhiều máy tính cùng lớp học / phòng lab.
3. **Tổng hợp bộ 15 Test Cases chuẩn hóa:** Thống kê tập trung toàn bộ các kịch bản kiểm thử từ giao diện, socket mạng, cơ sở dữ liệu SQLite đến khả năng chịu tải.

---

## 2. Hệ thống Scripts Tự Động Hóa (.BAT)

Nhóm phát triển cung cấp 3 kịch bản batch script tại thư mục gốc của dự án:

| Tên File Script | Mục đích sử dụng | Hành vi thực hiện |
|---|---|---|
| **`run-all.bat`** | Khởi chạy toàn bộ hệ thống (Demo 1 máy) | 1. Bật cửa sổ `WhiteboardServer` trong tiến trình riêng.<br>2. Dừng chờ 2 giây (ping localhost) để Server khởi tạo CSDL SQLite và mở cổng socket.<br>3. Tự động bật cửa sổ `WhiteboardClient`. |
| **`run-server.bat`** | Khởi chạy máy chủ riêng biệt | Di chuyển vào thư mục build của Server (`code\WhiteboardServer\bin\Debug\net10.0`), chạy `WhiteboardServer.exe`, in ra danh sách địa chỉ IP LAN của máy và bắt lỗi dừng nếu có sự cố. |
| **`run-client.bat`** | Khởi chạy thêm một Client mới | Di chuyển vào thư mục build WinUI 3 x64 (`code\WhiteboardClient\bin\x64\Debug\net10.0-windows10.0.19041.0\win-x64`), khởi động `WhiteboardClient.exe`. Có thể bấm nhiều lần để mở nhiều Client cùng lúc test vẽ đồng thời. |

### Cấu trúc kịch bản `run-all.bat`:
```bat
@echo off
title Online Whiteboard Launcher
echo ==================================================
echo         KHOI CHAY HE THONG ONLINE WHITEBOARD
echo ==================================================

echo [1/2] Dang khoi dong Server...
start "Whiteboard Server" "%~dp0run-server.bat"

echo Cho 2 giay de Server san sang...
ping 127.0.0.1 -n 3 >nul

echo [2/2] Dang khoi dong Client WinUI 3...
start "Whiteboard Client" "%~dp0run-client.bat"

echo ==================================================
echo He thong da khoi chay xong!
echo ==================================================
```

---

## 3. Hướng dẫn thiết lập Mạng LAN giữa 2 máy tính

Để thực hiện kiểm thử kết nối giữa các máy tính khác nhau qua WiFi hoặc mạng nội bộ (Kịch bản **TC_14**):

### Bước 1: Máy tính chạy Server (Host)
1. Chạy `run-server.bat`.
2. Quan sát màn hình console Server hiển thị danh sách IP:
   ```
   • Cùng mạng WiFi / LAN (Wi-Fi): 192.168.1.15
   • Cổng lắng nghe (Port): 8888
   ```
3. **Mở cổng Windows Firewall (nếu máy khác không kết nối được):**
   - Mở PowerShell với quyền Administrator trên máy Server:
     ```powershell
     New-NetFirewallRule -DisplayName "WhiteboardServer Port 8888" -Direction Inbound -LocalPort 8888 -Protocol TCP -Action Allow
     ```

### Bước 2: Máy tính chạy Client (Guest)
1. Kết nối vào cùng mạng WiFi / mạng LAN với máy Server.
2. Chạy `run-client.bat`.
3. Tại màn hình Đăng nhập:
   - **Tên người dùng:** Nhập tên của bạn (ví dụ: `Thành viên B`).
   - **Địa chỉ IP Server:** Nhập IP của máy Server (ví dụ: `192.168.1.15`).
   - **Cổng:** `8888`.
4. Bấm **"Tạo phòng mới"** hoặc nhập mã phòng có sẵn bấm **"Vào phòng"** để bắt đầu vẽ chung thời gian thực.

---

## 4. Bảng Tổng Hợp 15 Test Cases Dự Án (Master Test Matrix)

Toàn bộ 15 kịch bản kiểm thử được phân bổ đều qua các màn hình và chức năng:

| Mã TC | Phân hệ / Màn hình | Tên Test Case | Kịch bản kiểm thử (Input / Scenario) | Kết quả mong đợi (Expected Result) | Hạng mục mã nguồn liên quan |
|:---:|---|---|---|---|:---:|
| **TC_01** | **Đăng nhập** | Kiểm tra dữ liệu nhập sai | 1. Để trống tên.<br>2. IP sai định dạng (`192.168.1`).<br>3. Port sai (`abc`).<br>4. Để trống mã phòng bấm Vào phòng. | Hiện InfoBar cảnh báo cụ thể cho từng lỗi, chặn không cho kết nối socket. | **T5** (E) |
| **TC_02** | **Đăng nhập** | Server chưa chạy | Tắt Server, bấm kết nối với IP `127.0.0.1:8888`. | Hiện thông báo lỗi rõ ràng sau timeout, ứng dụng không bị treo (UI responsive). | **T5, T6** (E, L) |
| **TC_03** | **Phòng** | Tạo phòng & Ghi nhớ thông tin | Tạo phòng mới với tên `An`. Đóng app và mở lại. | Vào phòng vẽ thành công, mã phòng sinh tự động `ROOMxxxxx`. Mở lại app tự điền lại tên, IP, Port cũ. | **T5, T6** (E, L) |
| **TC_04** | **Phòng** | Vào phòng & Danh sách Online | Client A tạo phòng, Client B vào cùng mã phòng, Client C vào phòng khác. | A và B thấy tên nhau trong danh sách online. C hoàn toàn độc lập, không thấy nét vẽ phòng A. | **T3, T7** (C, A) |
| **TC_05** | **Phòng** | Sao chép mã & Rời phòng | Bấm nút "Sao chép mã" rồi dán vào Notepad. Client thứ 2 bấm "Rời phòng". | Nút đổi trạng thái "Đã sao chép!". Người rời quay về màn hình Login, người ở lại thấy sĩ số online giảm. | **T9b** (D) |
| **TC_06** | **Công cụ vẽ** | Bút vẽ và Tẩy | Chọn bút vẽ (đen, size 4) kéo đường cong và chấm điểm. Chọn tẩy xóa qua nét vẽ. | Bút vẽ mượt nét tròn; chấm đơn hiện chấm tròn. Tẩy xóa trả về màu nền trắng kích thước gấp 4 lần nét bút. | **T7, T8** (A, B) |
| **TC_07** | **Công cụ vẽ** | Đường thẳng, Chữ nhật, Hình tròn | Chọn từng công cụ khối, nhấn giữ chuột kéo nhiều hướng rồi thả tay. | Lúc kéo thấy đường nét đứt xem trước (preview). Khi thả tay nét vẽ cố định sắc nét đúng màu và cỡ nét. | **T8** (B) |
| **TC_08** | **Công cụ vẽ** | Màu sắc và Cỡ nét | Chọn màu từ bảng màu nhanh hoặc hộp chọn màu nâng cao. Kéo slider cỡ nét từ 1 đến 30px. | Nét vẽ đúng màu và độ dày đã chọn. Thanh công cụ cập nhật mã HEX và kích thước nét. | **T8** (B) |
| **TC_09** | **Đồng bộ thời gian thực** | Nhiều người cùng vẽ trong phòng | 3 Client A, B, C cùng phòng vẽ liên tục trong 30 giây với các màu khác nhau. | Nét vẽ hiển thị tức thì trên các máy còn lại (< 50ms), không mất nét, không crash. | **T3, T7** (C, A) |
| **TC_10** | **Lịch sử (SQLite)** | Lưu & Phát lại nét vẽ | A vẽ vài nét. B vào sau. Khởi động lại Server, A vào lại phòng cũ. | B vào sau thấy ngay toàn bộ nét A đã vẽ. Sau khi reboot Server, dữ liệu trong `whiteboard.db` khôi phục trọn vẹn. | **T4** (D) |
| **TC_11** | **Xóa & Lưu bảng** | Xóa bảng cho cả phòng | Bấm "Xóa toàn bộ bảng" → chọn Hủy. Bấm lại → chọn Xóa. Người mới vào sau. | Chọn Hủy: giữ nguyên. Chọn Xóa: bảng của mọi người đều trắng tinh; người vào sau cũng không thấy nét cũ. | **T4, T9b** (D) |
| **TC_12** | **Xóa & Lưu bảng** | Lưu bảng vẽ ra ảnh PNG | Vẽ hình, bấm "Lưu ảnh (PNG)", chọn thư mục lưu và kiểm tra file ảnh. | Xuất file `.png` độ phân giải cao `2560 × 1440` (192 DPI), nền trắng sắc nét. Hỗ trợ lưu đè an toàn. | **T9b** (D) |
| **TC_13** | **Zoom & Kéo bảng** | Phóng to, thu nhỏ và di chuyển bảng | Cuộn chuột zoom quanh con trỏ (25%-800%). Kéo bảng bằng chuột phải/giữa. Bấm "Mở rộng bảng". | Zoom mượt mà quanh trỏ chuột. Bảng không bị kéo mất khỏi khung nhìn. Nút mở rộng ẩn 2 thanh bên tiện lợi. | **T9a** (C) |
| **TC_14** | **Mạng & Ổn định** | Localhost & Mạng LAN | Chạy trên 1 máy (127.0.0.1) và chạy giữa 2 máy tính cùng mạng WiFi. | Cả 2 chế độ đều kết nối ổn định, vẽ đồng bộ mượt mà hai chiều. | **T2, T6, T10** (B, L, E) |
| **TC_15** | **Mạng & Ổn định** | Mất kết nối & Thoát đột ngột | 1. Tắt Server đột ngột khi client đang vẽ.<br>2. Đóng đột ngột cửa sổ của 1 Client. | TH1: Client hiện InfoBar lỗi đỏ, không sập app, cho phép Rời phòng. TH2: Server dọn dẹp an toàn, cập nhật sĩ số. | **T3, T6** (C, L) |

---

## 5. Kết luận & Đánh giá mức độ hoàn thiện

Dự án đã đáp ứng đầy đủ 100% tiêu chí nghiệm thu (Definition of Done - DoD):
- Kiến trúc mô-đun hóa rõ ràng với 4 project độc lập (`WhiteboardShared`, `WhiteboardServer`, `WhiteboardClient`, `WhiteboardStressTest`).
- Mã nguồn được phân chia rành mạch theo ma trận trách nhiệm xoay vòng **`L → A → B → C → D → E → L`**, không có tình trạng tự viết doc hay tự review code của bản thân.
- Bộ kịch bản `.bat` tiện lợi sẵn sàng cho buổi báo cáo và kiểm tra trực tiếp của Giảng viên.

