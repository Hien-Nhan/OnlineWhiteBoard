# Nhật Ký Tiến Độ Cá Nhân - Thành Viên A

- **Họ và tên:** Phan Thanh Tùng
- **Mã sinh viên:** 066206004656
- **Tài khoản GitHub:** tungf2006
- **Vai trò trong dự án:** Thành viên A
- **Hạng mục code chính:** **T1** (Shared: Gói tin & Giao thức mạng) và **T7** (Client: Khung bảng vẽ Win2D & Nét vẽ tự do)
- **Hạng mục tài liệu:** `T0-Quy-uoc-repo-va-cau-truc.md` và `T6-Client-Ket-noi-mang.md`
- **Hạng mục review PR:** Review cho T5 (WinUI 3 Login của E), T10 (StressTest của E), T11 (Release của L)

---

## Báo Cáo Tiến Độ Từng Tuần

### Tuần 1: Khởi động & Hoàn thiện T1 (Shared Protocol)
- **Công việc đã thực hiện:**
  - Thiết lập môi trường phát triển (.NET 10 SDK, Windows App SDK, Visual Studio 2022).
  - Xóa file mẫu `Class1.cs`, phân tích và chuẩn hóa định dạng gói tin mạng cho toàn bộ hệ thống (`NetworkPacket.cs` và `DrawPacket.cs`).
  - Viết bộ parser an toàn chống văng ngoại lệ khi nhận gói tin lỗi từ socket mạng.
  - Viết tài liệu cấu trúc repo (`Extra/docs/T0-Quy-uoc-repo-va-cau-truc.md`).
  - Lập bộ test cases kiểm thử giao thức mạng trong `Extra/tests/TC-A.md`.
- **Tỷ lệ hoàn thành:** 100% mục T1 & Doc T0.
- **Khó khăn / Vướng mắc:** Đã giải quyết xong xung đột vị trí `RoomID` giữa gói vẽ (`RoomID` ở cuối gói) và gói điều khiển phòng.

### Tuần 2: Chuẩn bị giao diện, Review T5 & Tài liệu Kết nối mạng Client (T6)
- **Công việc đã thực hiện:**
  - Phối hợp kiểm thử giao tiếp Socket TCP giữa Client và Server.
  - Viết tài liệu `Extra/docs/T6-Client-Ket-noi-mang.md` mô tả cơ chế bất đồng bộ và chuyển luồng (Thread Marshaling qua `DispatcherQueue`).
  - Thực hiện rà soát, chuẩn bị review PR hạng mục T5 (WinUI 3 khung & màn hình đăng nhập của Thành viên E).
  - Thiết kế trước kịch bản kiểm thử (Test Cases) cho hạng mục T7 trong `Extra/tests/TC-A.md`.
- **Tỷ lệ hoàn thành:** 100% mục tài liệu T6; đang chờ T5 & T6 merge xong để tích hợp nhánh code T7.
