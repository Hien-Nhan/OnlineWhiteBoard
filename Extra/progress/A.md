# Nhật Ký Tiến Độ Cá Nhân - Thành Viên A

- **Họ và tên:** *(Điền họ tên của bạn)*
- **Mã sinh viên:** *(Điền MSSV)*
- **Tài khoản GitHub:** *(Điền GitHub Username)*
- **Vai trò trong dự án:** Thành viên A
- **Hạng mục code chính:** **T1** (Shared: Gói tin & Giao thức mạng) và **T7** (Client: Khung bảng vẽ Win2D & Nét vẽ tự do)
- **Hạng mục tài liệu:** `T0-Quy-uoc-repo-va-cau-truc.md` và `T6-Client-Ket-noi-mang.md`
- **Hạng mục review PR:** Review cho T5 (WinUI 3 Login của E), T10 (StressTest của E), T11 (Release của L)

---

## Báo Cáo Tiến Độ Từng Tuần

### Tuần 1: Khởi động & Hoàn thiện T1 (Shared Protocol)
- **Công việc đã thực hiện:**
  - Thiết lập môi trường phát triển (.NET 10 SDK, Windows App SDK, Visual Studio 2022).
  - Phân tích và chuẩn hóa định dạng gói tin mạng cho toàn bộ hệ thống (`NetworkPacket.cs` và `DrawPacket.cs`).
  - Viết bộ parser an toàn chống văng ngoại lệ khi nhận gói tin lỗi từ socket mạng.
  - Viết tài liệu cấu trúc repo (`Extra/docs/T0-Quy-uoc-repo-va-cau-truc.md`).
  - Lập 10 test cases kiểm thử giao thức mạng trong `Extra/tests/TC-A.md`.
- **Tỷ lệ hoàn thành:** 100% mục T1.
- **Khó khăn / Vướng mắc:** Đã giải quyết xong xung đột vị trí `RoomID` giữa gói vẽ và gói điều khiển phòng.

### Tuần 2: Chuẩn bị giao diện & Kết nối mạng Client
- **Công việc đã thực hiện:**
  - Hỗ trợ TV E thiết lập WinUI 3 unpackaged và self-contained.
  - Phối hợp với Leader L kiểm thử giao tiếp Socket TCP giữa Client và Server.
  - Viết tài liệu `Extra/docs/T6-Client-Ket-noi-mang.md` mô tả cơ chế bất đồng bộ và chuyển luồng (Thread Marshaling qua `DispatcherQueue`).
- **Tỷ lệ hoàn thành:** 100% mục tài liệu T6.

### Tuần 3 - 4: Thực hiện T7 (Bảng vẽ Win2D & Đồng bộ nét vẽ thời gian thực)
- **Công việc đã thực hiện:**
  - Xây dựng vùng vẽ logic chuẩn 1280×720 trên `CanvasControl` (Win2D).
  - Lập trình bắt sự kiện trỏ chuột/bút vẽ (`PointerPressed`, `PointerMoved`, `PointerReleased`).
  - Cấu hình nét vẽ tự do (`Freehand`) với khử răng cưa `Antialiasing` và bo tròn đầu nét `RoundCap`.
  - Kết nối luồng nhận dữ liệu từ `NetworkClient` để render thời gian thực nét vẽ từ người khác gửi tới.
  - Hiển thị danh sách thành viên online trong phòng.
  - Viết test cases nghiệm thu cho bảng vẽ Win2D trong `Extra/tests/TC-A.md`.
- **Tỷ lệ hoàn thành:** 100% mục T7.
