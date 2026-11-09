# Danh sách Test Case - Hạng mục T3: Server Quản lý phòng & Phát nét vẽ
**Người thực hiện (Code & Test):** Thành viên C (Thành viên 3)

| Mã TC | Tên Test Case | Các bước thực hiện | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Đánh giá |
|---|---|---|---|---|---|---|
| **TC_T3_01** | Gia nhập phòng thành công | 1. Mở kết nối TCP tới cổng 8888.<br>2. Gửi lệnh vào phòng. | `JOIN_ROOM;UserA;ROOM001\n` | Server thêm client vào ROOM001 và phản hồi `USER_LIST;UserA\n`. | Server log xác nhận client vào phòng, client nhận đúng `USER_LIST`. | **ĐẠT (PASS)** |
| **TC_T3_02** | Đồng bộ nét vẽ cùng phòng | 1. Client 1 và Client 2 cùng vào ROOM001.<br>2. Client 1 gửi gói DRAW. | `DRAW;FREEHAND;10;20;30;40;#FF0000;4;ROOM001\n` | Client 2 nhận được nguyên vẹn gói tin DRAW. Client 1 không bị gửi lặp lại chính nét vẽ của mình. | Client 2 nhận đúng chuỗi gói tin vẽ. | **ĐẠT (PASS)** |
| **TC_T3_03** | Cô lập dữ liệu giữa các phòng | 1. Client 1 vào ROOM001.<br>2. Client 3 vào ROOM002.<br>3. Client 1 gửi nét vẽ vào ROOM001. | `DRAW;FREEHAND;10;20;30;40;#00FF00;2;ROOM001\n` | Client 3 ở ROOM002 tuyệt đối không nhận được gói DRAW của ROOM001. | Client 3 không nhận bất kỳ dữ liệu nào từ ROOM001. | **ĐẠT (PASS)** |
| **TC_T3_04** | Đồng bộ lệnh xóa bảng | 1. Client 1 gửi lệnh xóa bảng phòng ROOM001. | `CLEAR_CANVAS;ROOM001\n` | Các client đang ở ROOM001 nhận được gói `CLEAR_CANVAS;ROOM001`. | Client 2 trong phòng nhận đúng lệnh xóa bảng. | **ĐẠT (PASS)** |
| **TC_T3_05** | Tự động dọn dẹp khi ngắt kết nối | 1. Client 1 đóng socket đột ngột.<br>2. Kiểm tra trạng thái phòng. | Đóng kết nối TCP | Server không bị văng lỗi (crash), tự động xoá Client 1 và broadcast danh sách user mới cho các client còn lại. | Server log ghi nhận ngắt kết nối an toàn, phòng cập nhật lại sĩ số. | **ĐẠT (PASS)** |

---

# Danh sách Test Case - Hạng mục T9a: Client Zoom + Kéo bảng + Mở rộng bảng
**Người thực hiện (Code & Test):** Thành viên C (Thành viên 3)

| Mã TC | Tên Test Case | Các bước thực hiện | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Đánh giá |
|---|---|---|---|---|---|---|
| **TC_T9a_01** | Cuộn chuột để phóng to / thu nhỏ (Zoom In / Out) | 1. Đặt con trỏ lên bảng vẽ.<br>2. Cuộn con lăn chuột lên để phóng to, cuộn xuống để thu nhỏ.<br>3. Kiểm tra nhãn phần trăm Zoom. | Cuộn chuột (Mouse Wheel Delta +/- 120) | Tỷ lệ phóng to mượt mà, mức zoom nằm trong dải [25%, 800%]. Nhãn ZoomText cập nhật chính xác (ví dụ 125%, 200%). | Bảng phóng to thu nhỏ mượt mà, đúng biên 25%-800%. | **ĐẠT (PASS)** |
| **TC_T9a_02** | Zoom giữ nguyên điểm neo dưới con trỏ chuột | 1. Đặt con trỏ chuột lên một nét vẽ cụ thể trên bảng.<br>2. Cuộn chuột phóng to từ 100% lên 300%. | Tọa độ con trỏ `Point anchor` | Điểm vẽ trên bảng nằm ngay dưới mũi tên chuột giữ nguyên vị trí, không bị nhảy trượt sang vị trí khác. | Điểm logic dưới con trỏ đứng yên khi zoom. | **ĐẠT (PASS)** |
| **TC_T9a_03** | Kéo di chuyển bảng vẽ (Pan) | 1. Giữ chuột phải hoặc chuột giữa rồi kéo rê.<br>2. Hoặc chọn công cụ "Di chuyển bảng" (Pan) rồi kéo bằng chuột trái. | Kéo chuột (Pointer drag) | Bảng vẽ di chuyển mượt mà theo hướng kéo của chuột, con trỏ đổi sang hình bàn tay / 4 chiều (`SizeAll`). | Bảng di chuyển theo tay kéo, đổi đúng icon con trỏ. | **ĐẠT (PASS)** |
| **TC_T9a_04** | Khóa biên kéo bảng (ClampPan) chống trôi mất bảng | 1. Kéo bảng thật xa về một phía (trên, dưới, trái, phải). | Kéo tọa độ lệch lớn (`_panX`, `_panY` lớn) | Bảng vẽ bị chặn lại khi mép bảng chạm ngưỡng an toàn (luôn giữ lại ít nhất 80px trong tầm nhìn), không bị kéo biến mất. | Bảng vẽ luôn nằm trong vùng nhìn thấy được, không bị mất. | **ĐẠT (PASS)** |
| **TC_T9a_05** | Nút Mở rộng bảng (Focus) và Đưa về vừa khít | 1. Bấm nút "Mở rộng bảng" trên thanh trên.<br>2. Bấm nút phần trăm (ResetZoom). | Click nút `FocusButton` và `ZoomResetButton` | Khi bấm mở rộng: hai panel bên trái và phải ẩn đi để bảng vẽ to rộng tối đa. Bấm nút phần trăm: bảng trở về vừa khít cửa sổ (100%). | Ẩn/hiện panel mượt mà, nút reset đưa bảng về vừa khít. | **ĐẠT (PASS)** |

