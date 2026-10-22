# Tài Liệu T1: Đặc Tả Giao Thức Mạng (Protocol Specification)

- **Hạng mục code:** T1 (Shared: Gói tin & Giao thức mạng)
- **File mã nguồn:** `code/WhiteboardShared/NetworkPacket.cs` và `code/WhiteboardShared/DrawPacket.cs`

---

## 1. Định dạng dây tổng quan (Wire Protocol Format)
- **Mã hóa:** Văn bản UTF-8 (không dùng BOM).
- **Phân cách dòng:** Mỗi gói tin nằm trên **một dòng riêng biệt**, kết thúc bằng ký tự xuống dòng `\n` (hỗ trợ cả `\r\n`).
- **Phân cách trường dữ liệu:** Các trường dữ liệu trong gói được phân tách bằng dấu chấm phẩy (`;`).
- **Phân tách danh sách con:** Dùng dấu phẩy (`,`) cho danh sách nhiều phần tử (ví dụ danh sách người online).

---

## 2. Bảng quy ước các lệnh trong hệ thống

| Lệnh (Command) | Định dạng gói tin trên dây | Phía gửi | Phía nhận | Ý nghĩa |
|---|---|:---:|:---:|---|
| `CONNECT` / `JOIN_ROOM` | `JOIN_ROOM;<Username>;<RoomID>\n` | Client | Server | Yêu cầu tham gia vào phòng có sẵn |
| `CREATE_ROOM` | `CREATE_ROOM;<Username>;<RoomName>\n` | Client | Server | Khởi tạo phòng vẽ mới |
| `USER_LIST` | `USER_LIST;<RoomID>;<User1,User2,...>\n` | Server | Client | Phát sóng danh sách thành viên trong phòng |
| `CLEAR_CANVAS` | `CLEAR_CANVAS;<RoomID>\n` | Client / Server | Cả hai | Xóa toàn bộ nội dung bảng vẽ của phòng |
| `DRAW` (Freehand) | `DRAW;FREEHAND;<x1>;<y1>;<x2>;<y2>;<#HEX>;<Size>;<RoomID>\n` | Client / Server | Cả hai | Đoạn thẳng nét vẽ tự do (bút/tẩy) |
| `DRAW` (Shape) | `DRAW;SHAPE;<LINE\|RECTANGLE\|CIRCLE>;<x1>;<y1>;<x2>;<y2>;<#HEX>;<Size>;<RoomID>\n` | Client / Server | Cả hai | Hình khối kéo thả |

---

## 3. 10 Ví dụ gói tin mẫu (≥ 10 ví dụ theo chuẩn DoD)

### A. Các gói tin HỢP LỆ (Valid Packets)
1. **Tham gia phòng:**
   `JOIN_ROOM;NguyenVanA;ROOM888\n`
   $\rightarrow$ Vào phòng `ROOM888` với tên hiển thị là `NguyenVanA`.
2. **Tạo phòng mới:**
   `CREATE_ROOM;TranThiB;Phong_Thao_Luan_Mang\n`
   $\rightarrow$ Tạo phòng tên `Phong_Thao_Luan_Mang`.
3. **Vẽ nét bút màu đỏ, cỡ 4px:**
   `DRAW;FREEHAND;120;340;125;348;#E74C3C;4;ROOM888\n`
   $\rightarrow$ Vẽ đoạn từ `(120, 340)` tới `(125, 348)` với màu đỏ trong phòng `ROOM888`.
4. **Vẽ tẩy (Eraser - bản chất là màu trắng):**
   `DRAW;FREEHAND;200;200;210;210;#FFFFFF;16;ROOM888\n`
   $\rightarrow$ Tẩy vùng vẽ kích thước 16px.
5. **Vẽ hình chữ nhật màu xanh:**
   `DRAW;SHAPE;RECTANGLE;50;50;300;200;#2980B9;3;ROOM888\n`
   $\rightarrow$ Vẽ khung chữ nhật từ `(50, 50)` tới `(300, 200)`.
6. **Vẽ hình tròn:**
   `DRAW;SHAPE;CIRCLE;400;300;600;500;#27AE60;2;ROOM888\n`
   $\rightarrow$ Vẽ ellipse/tròn trong vùng bao xác định.
7. **Cập nhật danh sách online:**
   `USER_LIST;ROOM888;Alice,Bob,Charlie\n`
   $\rightarrow$ Phòng có 3 người online: Alice, Bob và Charlie.
8. **Xóa bảng vẽ:**
   `CLEAR_CANVAS;ROOM888\n`
   $\rightarrow$ Ra lệnh xóa sạch nét vẽ trên màn hình của tất cả mọi người trong phòng `ROOM888`.

### B. Các gói tin KHÔNG HỢP LỆ (Invalid Packets - Hệ thống tự lọc bỏ, không ném ngoại lệ)
9. **Gói tin thiếu tham số tọa độ:**
   `DRAW;FREEHAND;120;340;#E74C3C;ROOM888\n`
   $\rightarrow$ `DrawPacket.TryParse()` trả về `false`, gói tin bị hủy, ứng dụng không sập.
10. **Tọa độ không phải số nguyên:**
    `DRAW;FREEHAND;abc;def;100;200;#000000;4;ROOM888\n`
    $\rightarrow$ Hàm parse số `TryInt()` thất bại, trả về `false`.
11. **Mã màu sai quy chuẩn:**
    `DRAW;FREEHAND;10;10;20;20;RED;4;ROOM888\n`
    $\rightarrow$ `NormalizeColor()` tự động thay `RED` thành màu mặc định `#000000` (màu đen).
12. **Gói tin rác hoặc ký tự lạ:**
    `\0\0\0HELLO_WORLD;;;;;;;\n`
    $\rightarrow$ Tự động loại bỏ ký tự rỗng `\0`, không khớp `CommandName` nên bị loại bỏ an toàn.
