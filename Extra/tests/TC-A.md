# Danh Sách Test Case Của Thành Viên A

Người phụ trách: **Thành viên A**  
Hạng mục: **T1** (Giao thức mạng & Shared Packets) và **T7** (Bảng vẽ Win2D & Nét vẽ tự do)

---

## PHẦN 1: TEST CASES CHO HẠNG MỤC T1 (SHARED PACKET & PROTOCOL)

| Mã TC | Tên Test Case | Dữ liệu đầu vào (Input String) | Kết quả mong đợi (Expected Output) | Trạng thái |
|:---:|---|---|---|:---:|
| **TC-T1-01** | Parse gói kết nối phòng hợp lệ | `JOIN_ROOM;Alice;ROOM123\n` | `packet != null`, `Command="JOIN_ROOM"`, `Username="Alice"`, `RoomID="ROOM123"` | ✅ Đạt |
| **TC-T1-02** | Parse gói tạo phòng mới | `CREATE_ROOM;Bob;Phong_Hoc_Tap\n` | `packet != null`, `Command="CREATE_ROOM"`, `Username="Bob"`, `RoomName="Phong_Hoc_Tap"` | ✅ Đạt |
| **TC-T1-03** | Parse gói vẽ tự do (Freehand) chuẩn | `DRAW;FREEHAND;10;20;30;40;#E74C3C;4;ROOM123\n` | `packet != null`, `RoomID="ROOM123"`, `Kind=Freehand`, `X1=10`, `Y1=20`, `X2=30`, `Y2=40`, `ColorHex="#E74C3C"`, `Size=4` | ✅ Đạt |
| **TC-T1-04** | Parse gói vẽ hình khối (Shape - Rectangle) | `DRAW;SHAPE;RECTANGLE;100;100;250;200;#000000;2;ROOM123\n` | `packet != null`, `RoomID="ROOM123"`, `Shape=Rectangle`, `X1=100`, `Y1=100`, `X2=250`, `Y2=200`, `Size=2` | ✅ Đạt |
| **TC-T1-05** | Chuẩn hóa mã màu Hex không hợp lệ | Chuỗi màu `"#xyz123"` hoặc rỗng `""` | `NormalizeColor()` trả về `#000000` (màu đen mặc định), không văng Exception | ✅ Đạt |
| **TC-T1-06** | Giới hạn kích thước nét vẽ (Clamp Size) | Size = `-10` hoặc Size = `999` | Size được ép về khoảng an toàn `[1, 200]` (`-10` thành `1`, `999` thành `200`) | ✅ Đạt |
| **TC-T1-07** | Xử lý gói tin rác hoặc chuỗi rỗng | `""` hoặc `"   \r\n"` hoặc `";;;;;;"` | Trả về `false` hoặc `null`, không làm sập ứng dụng (No Exception) | ✅ Đạt |
| **TC-T1-08** | Parse gói xóa bảng vẽ | `CLEAR_CANVAS;ROOM123\n` | `Command="CLEAR_CANVAS"`, `RoomID="ROOM123"` | ✅ Đạt |

---

## PHẦN 2: TEST CASES CHO HẠNG MỤC T7 (CLIENT BẢNG VẼ WIN2D & VẼ TỰ DO)

| Mã TC | Tên Test Case | Điều kiện thực hiện & Thao tác | Kết quả mong đợi (Expected Output) | Trạng thái |
|:---:|---|---|---|:---:|
| **TC-T7-01** | Bảng vẽ logic hiển thị đúng kích thước | Khởi động Client, vào phòng vẽ | Bảng vẽ hiển thị khung tỷ lệ 16:9 (1280×720 logic), nền trắng có viền và bóng đổ nhẹ | ✅ Đạt |
| **TC-T7-02** | Vẽ chấm đơn (Click chuột không di chuyển) | Bấm chuột trái tại điểm `(100, 100)` rồi nhả ngay | Một chấm tròn nhỏ đường kính bằng cỡ nét hiện tại xuất hiện tại `(100, 100)` | ✅ Đạt |
| **TC-T7-03** | Vẽ nét tự do (Bút chì - Pen) | Bấm giữ chuột trái và di chuyển liên tục trên bảng | Nét vẽ mượt mà theo đường di chuyển của chuột, đầu nét và góc nối bo tròn | ✅ Đạt |
| **TC-T7-04** | Đồng bộ nét vẽ thời gian thực giữa 2 Client | Mở 2 Client A và B cùng vào phòng `ROOM101`. Client A quẹt 1 nét | Client B lập tức hiển thị nét vẽ của A với độ trễ < 50ms | ✅ Đạt |
| **TC-T7-05** | Danh sách người dùng online | Client 1 tạo phòng, Client 2 và 3 lần lượt vào phòng | Khung bên phải của cả 3 client hiển thị đúng 3 người, có gắn nhãn "(bạn)" ở tên mình | ✅ Đạt |
| **TC-T7-06** | Nhận gói tin rác từ mạng khi đang vẽ | Giả lập gửi chuỗi lỗi `DRAW;MALFORMED;;;\n` vào socket | Canvas không bị crash, giao diện vẫn tương tác vẽ bình thường | ✅ Đạt |
| **TC-T7-07** | Thay đổi kích thước cửa sổ | Co giãn cửa sổ ứng dụng Client | Bảng vẽ 1280×720 tự động co giãn (`FitScale`) giữ nguyên tỷ lệ, không bị méo nét vẽ | ✅ Đạt |
