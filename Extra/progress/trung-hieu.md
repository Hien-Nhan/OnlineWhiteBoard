# Báo cáo tiến độ cá nhân - Thành viên C

## Tuần 1: Nghiên cứu cấu trúc và Chuẩn bị T3
- [x] Đọc và nắm rõ tài liệu phân công `PhanCong_DuAn.md`, quy chuẩn Git `rulegit.md`.
- [x] Chuẩn bị test case cho hạng mục T3.

## Tuần 2 - 3: Hạng mục T3 (Server: Quản lý phòng & phát nét vẽ)
- **Tình trạng:** Hoàn thành 100%
- **Các file đã thực hiện:**
  - `code/WhiteboardServer/RoomManager.cs`: Thiết kế mô hình quản lý phòng theo Singleton, đảm bảo an toàn đa luồng (`lock`), xử lý tạo/vào/rời phòng và broadcast dữ liệu cô lập theo từng phòng.
  - `code/WhiteboardServer/ClientHandler.cs`: Tiếp nhận luồng mạng từ mỗi Client, phân tách các lệnh `CREATE_ROOM`, `JOIN_ROOM`, `DRAW`, `CLEAR_CANVAS`, `USER_LIST`. Để sẵn các hook mở rộng cho Thành viên D (lưu SQLite T4) và Thành viên E (dọn dẹp T10).
  - `code/WhiteboardServer/Program.cs`: Vòng lặp lắng nghe `TcpListener` và chuyển giao kết nối cho `ClientHandler`.
- **Kiểm thử:** Đã kiểm thử tự động đạt 5/5 test case (cô lập phòng, broadcast nét vẽ, cập nhật user online).
- **Vướng mắc:** Không có. Code build 0 lỗi, 0 cảnh báo.

