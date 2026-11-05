# Bộ Kiểm Thử (Test Cases) - Thành Viên D

- **Người thực hiện:** Thành viên D
- **Module:** `WhiteboardServer` (T4) & `WhiteboardClient` (T9b)

## 1. Test Cases Hạng Mục T4: Server SQLite & Xóa Bảng

| STT | Mã Test Case | Tên Kịch Bản | Dữ Liệu Đầu Vào | Kết Quả Mong Đợi | Trạng Thái |
|:---:|:---:|:---|:---|:---|:---:|
| 1 | **TC-D-01** | Khởi tạo CSDL ban đầu | Chạy `DatabaseManager.InitializeDatabase()` khi chưa có file `whiteboard.db` | File CSDL được tạo, có bảng `DrawHistory` và Index `IX_DrawHistory_RoomID_Id` | **Đạt (Pass)** |
| 2 | **TC-D-02** | Lưu nét vẽ tự do vào SQLite | Client gửi `DRAW;FREEHAND;10;20;30;40;#FF0000;4;ROOM001` | Nét vẽ được chèn thành công vào bảng với đúng `RoomID="ROOM001"` | **Đạt (Pass)** |
| 3 | **TC-D-03** | Lưu nét vẽ hình khối vào SQLite | Client gửi `DRAW;SHAPE;RECTANGLE;0;0;100;100;#00FF00;2;ROOM002` | Nét vẽ hình chữ nhật được lưu đúng thông tin phòng và tọa độ | **Đạt (Pass)** |
| 4 | **TC-D-04** | Đồng bộ lịch sử cho người mới vào phòng | Client B gửi `JOIN_ROOM;UserB;ROOM001` sau khi Client A đã vẽ 5 nét | Server tự động truy vấn SQLite và gửi tuần tự 5 nét vẽ cho Client B | **Đạt (Pass)** |
| 5 | **TC-D-05** | Xóa sạch bảng vẽ theo phòng | Client gửi `CLEAR_CANVAS;ROOM001` | Lịch sử phòng `ROOM001` bị xóa khỏi CSDL, phòng `ROOM002` không bị ảnh hưởng | **Đạt (Pass)** |

---

## 2. Test Cases Hạng Mục T9b: Client Thao Tác (Sao Chép, Rời Phòng, Xóa Bảng, Lưu PNG)

| STT | Mã Test Case | Tên Kịch Bản | Thao Tác Kiểm Thử | Kết Quả Mong Đợi | Trạng Thái |
|:---:|:---:|:---|:---|:---|:---:|
| 6 | **TC-D-06** | Sao chép mã phòng | Bấm nút "Sao chép mã" trên thanh điều hướng | Mã phòng được sao chép vào Clipboard hệ thống, nút chuyển trạng thái "Đã sao chép!" trong 1.5s | **Đạt (Pass)** |
| 7 | **TC-D-07** | Rời phòng về màn hình đăng nhập | Bấm nút "Rời phòng" | Ứng dụng đóng socket kết nối tới Server và chuyển mượt về giao diện `LoginPage` | **Đạt (Pass)** |
| 8 | **TC-D-08** | Xác nhận trước khi xóa bảng | Bấm nút "Xóa toàn bộ bảng", chọn "Hủy" trên dialog | Bảng vẽ giữ nguyên, không có gói tin nào gửi lên mạng | **Đạt (Pass)** |
| 9 | **TC-D-09** | Xóa toàn bộ bảng của phòng | Bấm nút "Xóa toàn bộ bảng", chọn "Xóa" trên dialog | Gửi gói `CLEAR_CANVAS;<RoomID>` lên Server; toàn bộ client cùng phòng được dọn trắng bảng tức thì | **Đạt (Pass)** |
| 10 | **TC-D-10** | Lưu bảng vẽ ra ảnh PNG | Vẽ một số nét vẽ, bấm "Lưu ảnh (PNG)", chọn nơi lưu | File ảnh `.png` được xuất thành công ở độ phân giải 2560×1440, hình ảnh sắc nét | **Đạt (Pass)** |
