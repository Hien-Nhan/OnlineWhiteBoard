# Danh sách Test Case - Hạng mục T3: Server Quản lý phòng & Phát nét vẽ
**Người thực hiện (Code & Test):** Thành viên C (Thành viên 3)

| Mã TC | Tên Test Case | Các bước thực hiện | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Đánh giá |
|---|---|---|---|---|---|---|
| **TC_T3_01** | Gia nhập phòng thành công | 1. Mở kết nối TCP tới cổng 8888.<br>2. Gửi lệnh vào phòng. | `JOIN_ROOM;UserA;ROOM001\n` | Server thêm client vào ROOM001 và phản hồi `USER_LIST;UserA\n`. | Server log xác nhận client vào phòng, client nhận đúng `USER_LIST`. | **ĐẠT (PASS)** |
| **TC_T3_02** | Đồng bộ nét vẽ cùng phòng | 1. Client 1 và Client 2 cùng vào ROOM001.<br>2. Client 1 gửi gói DRAW. | `DRAW;FREEHAND;10;20;30;40;#FF0000;4;ROOM001\n` | Client 2 nhận được nguyên vẹn gói tin DRAW. Client 1 không bị gửi lặp lại chính nét vẽ của mình. | Client 2 nhận đúng chuỗi gói tin vẽ. | **ĐẠT (PASS)** |
| **TC_T3_03** | Cô lập dữ liệu giữa các phòng | 1. Client 1 vào ROOM001.<br>2. Client 3 vào ROOM002.<br>3. Client 1 gửi nét vẽ vào ROOM001. | `DRAW;FREEHAND;10;20;30;40;#00FF00;2;ROOM001\n` | Client 3 ở ROOM002 tuyệt đối không nhận được gói DRAW của ROOM001. | Client 3 không nhận bất kỳ dữ liệu nào từ ROOM001. | **ĐẠT (PASS)** |
| **TC_T3_04** | Đồng bộ lệnh xóa bảng | 1. Client 1 gửi lệnh xóa bảng phòng ROOM001. | `CLEAR_CANVAS;ROOM001\n` | Các client đang ở ROOM001 nhận được gói `CLEAR_CANVAS;ROOM001`. | Client 2 trong phòng nhận đúng lệnh xóa bảng. | **ĐẠT (PASS)** |
| **TC_T3_05** | Tự động dọn dẹp khi ngắt kết nối | 1. Client 1 đóng socket đột ngột.<br>2. Kiểm tra trạng thái phòng. | Đóng kết nối TCP | Server không bị văng lỗi (crash), tự động xoá Client 1 và broadcast danh sách user mới cho các client còn lại. | Server log ghi nhận ngắt kết nối an toàn, phòng cập nhật lại sĩ số. | **ĐẠT (PASS)** |

