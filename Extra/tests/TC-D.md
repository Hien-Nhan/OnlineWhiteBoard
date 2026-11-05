# Bộ Kiểm Thử (Test Cases) - Hạng Mục T4: Server SQLite & Xóa Bảng

- **Người thực hiện:** Thành viên D
- **Module:** `WhiteboardServer` (`DatabaseManager.cs`, `ClientHandler.cs`)

| STT | Mã Test Case | Tên Kịch Bản | Dữ Liệu Đầu Vào | Kết Quả Mong Đợi | Trạng Thái |
|:---:|:---:|:---|:---|:---|:---:|
| 1 | **TC-D-01** | Khởi tạo CSDL ban đầu | Chạy `DatabaseManager.InitializeDatabase()` khi chưa có file `whiteboard.db` | File CSDL được tạo, có bảng `DrawHistory` và Index `IX_DrawHistory_RoomID_Id` | **Đạt (Pass)** |
| 2 | **TC-D-02** | Lưu nét vẽ tự do vào SQLite | Client gửi `DRAW;FREEHAND;10;20;30;40;#FF0000;4;ROOM001` | Nét vẽ được chèn thành công vào bảng với đúng `RoomID="ROOM001"` | **Đạt (Pass)** |
| 3 | **TC-D-03** | Lưu nét vẽ hình khối vào SQLite | Client gửi `DRAW;SHAPE;RECTANGLE;0;0;100;100;#00FF00;2;ROOM002` | Nét vẽ hình chữ nhật được lưu đúng thông tin phòng và tọa độ | **Đạt (Pass)** |
| 4 | **TC-D-04** | Đồng bộ lịch sử cho người mới vào phòng | Client B gửi `JOIN_ROOM;UserB;ROOM001` sau khi Client A đã vẽ 5 nét | Server tự động truy vấn SQLite và gửi tuần tự 5 nét vẽ cho Client B | **Đạt (Pass)** |
| 5 | **TC-D-05** | Xóa sạch bảng vẽ theo phòng | Client gửi `CLEAR_CANVAS;ROOM001` | Lịch sử phòng `ROOM001` bị xóa khỏi CSDL, phòng `ROOM002` không bị ảnh hưởng | **Đạt (Pass)** |
