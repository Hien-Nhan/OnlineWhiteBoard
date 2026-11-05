# Báo Cáo Tiến Độ Cá Nhân - Thành Viên D

- **Họ và tên:** HoangLN714
- **Email:** 1977vlog113@gmail.com
- **Vai trò trong dự án:** Thành viên D
- **Hạng mục code chính:** 
  - **T4:** Server: SQLite lịch sử nét vẽ + Xóa bảng
  - **T9b:** Client: Sao chép mã, rời phòng, xóa bảng, lưu PNG
- **Hạng mục tài liệu:** T3 (Server: phòng + phát nét vẽ), T9a (Client: zoom + kéo bảng), T11 (Script .bat, README, gộp test case)
- **Hạng mục review PR:** Review cho T2 (Server TCP nền & Cấu hình của B), T8 (Client: tẩy, hình khối, màu, cỡ nét của B)

---

## 1. Tiến Độ Hạng Mục T4: Server: SQLite Lịch Sử & Xóa Bảng

- **Tình trạng:** Hoàn thành 100%
- **Các file đã thực hiện:**
  - `code/WhiteboardServer/DatabaseManager.cs`: Thiết kế mô-đun SQLite tĩnh quản lý cơ sở dữ liệu `whiteboard.db`, tạo bảng `DrawHistory` và chỉ mục `IX_DrawHistory_RoomID_Id` tăng tốc truy vấn. Các hàm cốt lõi:
    - `InitializeDatabase`: Khởi tạo cấu trúc bảng tự động.
    - `SaveDraw`: Lưu trữ nét vẽ theo từng phòng và người dùng.
    - `GetRoomHistory`: Tải toàn bộ nét vẽ theo đúng trình tự thời gian.
    - `ClearRoomHistory`: Xóa toàn bộ dữ liệu nét vẽ khi có lệnh dọn bảng.
  - `code/WhiteboardServer/ClientHandler.cs`: Tích hợp các hook sự kiện:
    - Phát lại lịch sử nét vẽ cũ cho Client mới tham gia phòng (`GetRoomHistory`).
    - Lưu vết nét vẽ thời gian thực từ gói `DRAW` (`SaveDraw`).
    - Dọn sạch lịch sử trong cơ sở dữ liệu khi nhận gói `CLEAR_CANVAS` (`ClearRoomHistory`).
  - `code/WhiteboardServer/Program.cs`: Gọi khởi tạo cơ sở dữ liệu SQLite khi máy chủ khởi động.
- **Kiểm thử:** Đã xây dựng bộ test case `Extra/tests/TC-D.md` đạt 5/5 kịch bản (khởi tạo, lưu trữ nét vẽ, đồng bộ cho người mới, cô lập phòng, xóa bảng).
- **Kết quả biên dịch:** 0 lỗi (Build succeeded).

---

## 2. Tiến Độ Hạng Mục T9b: Client: Sao Chép Mã, Rời Phòng, Xóa Bảng, Lưu PNG

- **Tình trạng:** Hoàn thành 100%
- **Các file và hàm đã thực hiện trên WinUI 3:**
  - `code/WhiteboardClient/Views/BoardPage.xaml`:
    - `CopyButton` (Nút Sao chép mã phòng).
    - `LeaveButton` (Nút Rời phòng và đăng xuất).
    - `ClearButton` (Nút Xóa toàn bộ bảng vẽ kèm cảnh báo nguy hiểm).
    - `SaveButton` (Nút Lưu ảnh PNG chất lượng cao).
  - `code/WhiteboardClient/Views/BoardPage.xaml.cs`:
    - `CopyButton_Click`: Đưa `RoomId` vào `Clipboard`, hiển thị phản hồi tức thì "Đã sao chép!".
    - `LeaveButton_Click`: Ngắt kết nối socket mạng `NetworkClient.Disconnect()` và điều hướng trở lại màn hình `LoginPage`.
    - `ClearButton_Click`: Mở hộp thoại xác nhận `ContentDialog`, nếu người dùng đồng ý sẽ phát gói `CLEAR_CANVAS;<RoomID>` lên Server để dọn sạch cho mọi thành viên.
    - `SaveButton_Click`: Mở `FileSavePicker` cho người dùng chọn đường dẫn lưu file, xuất nội dung bảng vẽ đồ họa độ nét cao 2560×1440 (192 DPI) ra định dạng `.png`.
    - `ProcessPacket`: Lắng nghe gói phản hồi `CLEAR_CANVAS` từ Server để gọi `ClearBoardLocal()` làm sạch bảng vẽ của người dùng.
- **Kiểm thử:** Đạt 5/5 test case T9b (sao chép clipboard, rời phòng sạch kết nối, xác nhận hủy xóa bảng, xóa toàn bộ bảng vẽ thời gian thực, lưu file ảnh PNG hợp lệ).
