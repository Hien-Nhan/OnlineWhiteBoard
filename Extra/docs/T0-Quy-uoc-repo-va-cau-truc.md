# Tài Liệu T0: Quy Ước Repo & Cấu Trúc Hệ Thống

- **Người thực hiện tài liệu:** Thành viên A
- **Hạng mục code liên quan:** T0 (Leader L khởi tạo khung)

---

## 1. Mục tiêu & Tổng quan kiến trúc
Hệ thống **OnlineWhiteBoard** được thiết kế theo mô hình phân tầng Client-Server nhằm phục vụ việc vẽ và cộng tác thời gian thực qua giao thức TCP Socket. Để đảm bảo tính mô-đun hóa, dễ bảo trì và phân công công việc song song không xung đột, mã nguồn được chia thành 4 dự án (projects) riêng biệt trong một Solution duy nhất (`WhiteboardApp.slnx`):

```
WhiteboardApp.slnx
├── WhiteboardShared        (Class Library - .NET 10)
├── WhiteboardServer        (Console App - .NET 10)
├── WhiteboardClient        (WinUI 3 App - Windows App SDK + Win2D)
└── WhiteboardStressTest    (Console App - .NET 10)
```

---

## 2. Chi tiết các Project

### 2.1. `WhiteboardShared` (Thư viện dùng chung)
- **Mục đích:** Chứa các định nghĩa dữ liệu, cấu trúc gói tin (`NetworkPacket`, `DrawPacket`) và các enum công cụ vẽ.
- **Tính chất:** Độc lập hoàn toàn với giao diện đồ họa. Cả Client, Server và StressTest đều tham chiếu (`ProjectReference`) tới dự án này.
- **Lợi ích:** Đảm bảo cả hai đầu Client và Server luôn nói cùng một "ngôn ngữ", không bao giờ bị lệch chuẩn giao thức.

### 2.2. `WhiteboardServer` (Máy chủ TCP Socket)
- **Mục đích:** Lắng nghe kết nối TCP trên cổng cấu hình (mặc định 8888), quản lý danh sách client và định tuyến nét vẽ theo từng phòng (`RoomID`).
- **Lưu trữ:** Tích hợp thư viện SQLite cục bộ (`Microsoft.Data.Sqlite`) để lưu trữ lịch sử nét vẽ và phát lại khi thành viên mới tham gia phòng.

### 2.3. `WhiteboardClient` (Ứng dụng bảng vẽ đồ họa)
- **Mục đích:** Giao diện người dùng đồ họa hiện đại trên Windows (WinUI 3).
- **Đồ họa Win2D:** Sử dụng `CanvasControl` tận dụng sức mạnh GPU (Direct2D qua Win2D) để vẽ mượt mà ở độ phân giải logic 1280×720 (lưu ảnh 2560×1440).
- **Unpackaged & Self-Contained:** Ứng dụng không cần cài đặt gói MSIX rườm rà, có thể copy chạy trực tiếp trên các máy Windows 10/11 sạch.

### 2.4. `WhiteboardStressTest` (Công cụ kiểm thử tải)
- **Mục đích:** Giả lập hàng chục client kết nối đồng thời, gửi liên tục hàng nghìn gói vẽ để đo lường độ trễ mạng, kiểm tra nghẽn socket và tỷ lệ mất gói.

---

## 3. Quy ước Git & Phối hợp nhóm
1. **Mô hình nhánh:**
   - `main`: Chỉ chứa bản phát hành ổn định, sẵn sàng báo cáo/demo.
   - `dev`: Nhánh tích hợp chính của cả nhóm.
   - `feature/T<n>-<tên-chức-năng>`: Nhánh làm việc riêng của từng người, tạo từ `dev` và chỉ merge vào `dev` qua Pull Request (PR).
2. **Quy tắc review chéo:** Người review PR là người được phân công trong bảng ma trận, bắt buộc kéo code về chạy thử trước khi Approve.
3. **Quy tắc commit:** Tuân thủ Conventional Commits (`feat`, `fix`, `docs`, `test`, `refactor`). Tuyệt đối không commit file sinh tự động (`bin/`, `obj/`, `*.db`).
