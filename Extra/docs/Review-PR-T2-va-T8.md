# Biên Bản Đánh Giá & Review Pull Request (Hạng Mục T2 & T8)

- **Người thực hiện review (Reviewer):** Thành viên D
- **Người thực hiện mã nguồn (Author):** Thành viên B
- **Người viết tài liệu (Doc):** Thành viên C
- **Quy tắc phân công:** Xoay vòng `L → A → B → C → D → E → L` (Người review PR = Người code + 2)

---

## 1. Tổng Quan Quy Trình Review

Căn cứ theo quy định tại `Extra/rulegit.md`:
> *"Reviewer build và chạy thử, không chỉ đọc. Để lại ≥ 1 nhận xét có nội dung (câu hỏi hoặc gợi ý) – đây là phần học. Cần ≥ 1 approval mới merge."*

Người D đã thực hiện kéo mã nguồn của Thành viên B, tiến hành build độc lập, kiểm tra logic giải thuật, đánh giá mức độ tương thích giao thức mạng và kiểm thử tính năng thực tế.

---

## 2. Review Hạng Mục T2: Server TCP Nền & Cấu Hình Mạng

### 2.1. Thông tin hạng mục
- **Mã hạng mục:** **T2**
- **Nhánh tính năng:** `feature/T2-server-tcp-config`
- **Các file thay đổi:**
  - `code/WhiteboardServer/ServerConfig.cs` (Tạo mới)
  - `code/WhiteboardServer/config.ini` (Tạo mới)
  - `code/WhiteboardServer/Program.cs` (Cập nhật)
  - `code/WhiteboardServer/WhiteboardServer.csproj` (Cập nhật)

### 2.2. Kết quả kiểm tra biên dịch & Chạy thử
- **Trạng thái Build:** ✅ **Thành công (0 Error, 0 Warning)**.
- **Khởi động Server:** File `config.ini` được đọc chính xác, Server mở cổng `8888` và in ra danh sách địa chỉ IP LAN / WiFi đầy đủ.

### 2.3. Nhận xét chi tiết (Code Review Analysis)

#### ✅ Điểm tốt (Strengths):
1. **Tách biệt cấu hình (Separation of Concerns):** Việc chuyển các tham số tĩnh như `Port`, `MaxConnections` ra file `config.ini` giúp người vận hành có thể đổi cổng mà không cần biên dịch lại mã nguồn C#.
2. **Cơ chế Fallback an toàn:** Class `ServerConfig` cài đặt giá trị mặc định thông minh (`Port = 8888`, `MaxConnections = 50`). Nếu file `config.ini` bị xóa hoặc sai định dạng, Server vẫn khởi động bình thường mà không bị crash.
3. **Hỗ trợ kết nối mạng LAN thực tế:** `Program.cs` đã duyệt toàn bộ danh sách card mạng thông qua `NetworkInterface.GetAllNetworkInterfaces()`, in ra địa chỉ IP của từng card (Ethernet, Wi-Fi), giúp các thành viên trong nhóm dễ dàng biết IP để cấu hình Client kết nối.

#### 💡 Câu hỏi & Gợi ý cải tiến (Suggestions for Author B):
1. **Vấn đề giới hạn kết nối (`MaxConnections`):**
   - *Nhận xét:* File `config.ini` đã cấu hình `MaxConnections=50`, tuy nhiên trong vòng lặp `while (true)` của `Program.cs`, Server vẫn đang gọi `listener.AcceptTcpClient()` và tạo `ClientHandler` không giới hạn.
   - *Gợi ý:* Nên bổ sung biến đếm số lượng kết nối đang hoạt động (ví dụ `Interlocked.Increment/Decrement` hoặc tham chiếu qua `RoomManager`), nếu vượt quá `MaxConnections` thì gửi thông báo từ chối kết nối trước khi ngắt socket.
2. **Kiểm tra phạm vi cổng hợp lệ:**
   - *Nhận xét:* Hàm parse `Port` nên kiểm tra thêm điều kiện nằm trong khoảng `1 <= Port <= 65535` để tránh trường hợp người dùng cấu hình số âm hoặc vượt ngưỡng cổng TCP.

### 2.4. Kết luận đánh giá T2
- **Trạng thái:** **APPROVED ✅ (Chấp thuận)**
- **Đánh giá chung:** Code đạt yêu cầu nghiệm thu, chạy ổn định, các gợi ý trên có thể đưa vào vòng refactor tiếp theo.

---

## 3. Review Hạng Mục T8: Client Tẩy, Hình Khối, Màu Sắc & Cỡ Nét

### 3.1. Thông tin hạng mục
- **Mã hạng mục:** **T8**
- **Nhánh tính năng:** `feature/client-canvas-tools` (hoặc tích hợp trong `BoardPage`)
- **Các file thay đổi:**
  - `code/WhiteboardClient/Views/BoardPage.xaml`
  - `code/WhiteboardClient/Views/BoardPage.xaml.cs`
  - `code/WhiteboardShared/DrawPacket.cs` (liên đới)

### 3.2. Kết quả kiểm tra biên dịch & Chạy thử
- **Trạng thái Build:** ✅ **Thành công trên kiến trúc x64 (WinUI 3)**.
- **Kiểm thử chức năng:**
  - Bút vẽ & Tẩy: Hoạt động trơn tru, không giật lag.
  - Hình khối (Đường thẳng, Chữ nhật, Tròn): Xem trước nét đứt chính xác.
  - Chọn màu & Cỡ nét: Thay đổi thời gian thực, đồng bộ đúng mã màu HEX.

### 3.3. Nhận xét chi tiết (Code Review Analysis)

#### ✅ Điểm tốt (Strengths):
1. **Thiết kế công cụ Tẩy thông minh:**
   - Thay vì phải sửa đổi giao thức mạng phức tạp, công cụ Tẩy được hiện thực bằng cách vẽ nét đè màu trắng (`#FFFFFF`) với kích thước gấp 4 lần cỡ nét bút (`_brush * 4`). Gói tin phát sinh vẫn là `DRAW;FREEHAND;...`, hoàn toàn tương thích ngược với các phiên bản Client cũ.
2. **Kỹ thuật Xem trước hình khối (Rubber-banding Preview):**
   - Khi kéo vẽ hình khối (`PointerMoved`), ứng dụng chỉ vẽ phác thảo cục bộ bằng đường nét đứt (`CanvasStrokeStyle`) trên canvas tạm thời và **không gửi dữ liệu lên mạng**. Chỉ khi người dùng nhả chuột (`PointerReleased`), nét vẽ hoàn chỉnh mới được đóng gói gửi lên Server. Kỹ thuật này giúp tiết kiệm băng thông mạng tối đa và chống nghẽn socket.
3. **Trải nghiệm người dùng (UX) tinh tế:**
   - Bảng màu có hàm tính toán độ sáng tương đối:
     `double luminance = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;`
     Chữ hiển thị mã màu tự động đổi sang màu Đen nếu nền sáng, hoặc màu Trắng nếu nền tối, giúp người dùng luôn đọc rõ mã màu.
   - Khi đang chọn Tẩy mà người dùng chọn một màu mới, hệ thống tự động chuyển lại công cụ Bút vẽ (`Pen`).

#### 💡 Câu hỏi & Gợi ý cải tiến (Suggestions for Author B):
1. **Chuẩn hóa tọa độ hình khối khi kéo ngược hướng:**
   - *Nhận xét:* Khi người dùng kéo chuột từ góc dưới-phải lên góc trên-trái, tọa độ kết thúc nhỏ hơn tọa độ bắt đầu ($x_2 < x_1$ hoặc $y_2 < y_1$). Cần đảm bảo hàm `DrawShapeOnBoard` luôn tính toán:
     `float left = Math.Min(x1, x2); float top = Math.Min(y1, y2);`
     `float width = Math.Abs(x2 - x1); float height = Math.Abs(y2 - y1);`
     để Win2D không nhận kích thước âm gây lỗi ngoại lệ.
2. **Tối ưu phím tắt:**
   - *Gợi ý:* Có thể cân nhắc thêm phím tắt nhanh (ví dụ: `P` cho Pen, `E` cho Eraser, `R` cho Rectangle, `C` cho Circle) để nâng cao trải nghiệm cho người dùng thích thao tác bàn phím.

### 3.4. Kết luận đánh giá T8
- **Trạng thái:** **APPROVED ✅ (Chấp thuận)**
- **Đánh giá chung:** Hoàn thành xuất sắc toàn bộ kịch bản nghiệm thu **TC_06**, **TC_07**, **TC_08**.

---

## 4. Mẫu Nhận Xét (Review Comments) để dán lên GitHub Pull Request

### Mẫu 1: Nhận xét PR cho T2 (Server TCP & Cấu hình)
```markdown
### Review PR Hạng mục T2 (Server TCP nền & Cấu hình)
- **Người review:** Thành viên D (@HoangLN714)
- **Kết quả build:** Đạt (Build succeeded, 0 error).

#### Đánh giá:
- Đã tách cấu hình tĩnh ra file `config.ini` và class `ServerConfig.cs` rất rõ ràng, có giá trị mặc định phòng ngừa lỗi file.
- Đã in đầy đủ danh sách IP card mạng LAN/WiFi giúp kiểm thử kết nối nhiều máy tính dễ dàng.

#### Gợi ý nhỏ:
1. Giá trị `MaxConnections=50` trong `config.ini` hiện chưa được kiểm tra chặn ở vòng lặp `AcceptTcpClient`. Nhóm có thể bổ sung bộ đếm connection ở giai đoạn hoàn thiện.
2. Nên kiểm tra dải giá trị cổng hợp lệ `1 <= Port <= 65535`.

**Quyết định:** LGTM! Approve PR. 🚀
```

### Mẫu 2: Nhận xét PR cho T8 (Client Tẩy, Hình khối, Màu & Cỡ nét)
```markdown
### Review PR Hạng mục T8 (Client Công cụ vẽ)
- **Người review:** Thành viên D (@HoangLN714)
- **Kết quả build:** Đạt trên nền tảng WinUI 3 x64.

#### Đánh giá:
- Công cụ Tẩy xử lý khéo léo qua màu trắng `#FFFFFF` với kích thước gấp 4 lần cỡ bút, tận dụng lại giao thức DRAW có sẵn mà không làm phức tạp hóa gói tin.
- Cơ chế xem trước hình khối (preview nét đứt) cục bộ khi kéo chuột rất mượt mà, chỉ phát gói tin khi nhả chuột giúp tối ưu băng thông mạng.
- Tính năng tự động đổi màu chữ theo độ tương phản nền màu (`luminance`) rất trực quan.

#### Gợi ý nhỏ:
- Kiểm tra lại việc chuẩn hóa tọa độ `Math.Min` và `Math.Abs` khi người dùng kéo hình khối ngược chiều từ dưới lên trên.

**Quyết định:** Code hoạt động chuẩn xác theo các test case TC_06, TC_07, TC_08. Approve PR! 👍
```
