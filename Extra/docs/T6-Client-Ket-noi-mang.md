# Tài Liệu T6: Client Kết Nối Mạng & Đa Luồng

- **Người thực hiện tài liệu:** Thành viên A
- **Hạng mục code liên quan:** T6 (Leader L phụ trách `NetworkClient.cs` và `BoardSession.cs`)

---

## 1. Mục tiêu
Tài liệu mô tả kiến trúc tầng mạng phía Client (`NetworkClient.cs`), giải thích cách thức ứng dụng WinUI 3 mở kết nối TCP Socket, nhận dữ liệu bất đồng bộ trên luồng nền và chuyển tiếp an toàn lên luồng giao diện người dùng (UI Thread).

---

## 2. Luồng hoạt động của `NetworkClient`

```mermaid
sequenceDiagram
    participant UI as Giao diện WinUI 3
    participant NC as NetworkClient (Static)
    participant BG as Background Reader Thread
    participant SRV as WhiteboardServer (TCP)

    UI->>NC: ConnectAsync(host, port, timeoutMs)
    NC->>SRV: TcpClient.ConnectAsync()
    SRV-->>NC: Kết nối thành công
    NC-->>UI: Hoàn tất kết nối

    UI->>NC: StartReceiving()
    NC->>BG: Khởi tạo luồng nền độc lập (_readThread)
    loop Lắng nghe liên tục
        BG->>SRV: StreamReader.ReadLine()
        SRV-->>BG: Dòng gói tin (ví dụ DRAW;...)
        BG->>UI: OnDataReceived event
        UI->>UI: DispatcherQueue.TryEnqueue() cập nhật Canvas
    end
```

---

## 3. Các kỹ thuật Lập trình mạng cốt lõi

### 3.1. Kết nối có Timeout an toàn (`ConnectAsync`)
```csharp
using var cts = new CancellationTokenSource(timeoutMs);
await client.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);
```
- Ngăn chặn tình trạng ứng dụng bị "đóng băng" (UI freeze) khi người dùng nhập sai địa chỉ IP hoặc Server chưa bật. Hết 4000ms sẽ tự động ném `TimeoutException` để hiển thị thông báo lỗi thân thiện.

### 3.2. Cấu hình Socket không trễ (`NoDelay = true`)
```csharp
client.NoDelay = true; // Vô hiệu hóa thuật toán Nagle
```
- Trong ứng dụng vẽ thời gian thực, tọa độ chuột cần được truyền đi ngay lập tức. Thuật toán Nagle gom gói nhỏ sẽ gây ra hiện tượng giật nét, vì vậy việc bật `NoDelay` là bắt buộc.

### 3.3. Luồng nhận nền độc lập & Chuyển giao luồng (`Thread Marshaling`)
- Dữ liệu mạng được đọc tuần tự trong vòng lặp `while ((line = reader.ReadLine()) != null)` chạy trên một luồng nền riêng biệt (`IsBackground = true`).
- Khi có gói tin mới, sự kiện `OnDataReceived` được kích hoạt trên luồng nền. Để vẽ lên Win2D `CanvasControl`, `BoardPage` sử dụng `DispatcherQueue`:
```csharp
private void HandleNetworkData(string data)
{
    _dispatcher?.TryEnqueue(() => ProcessPacket(data));
}
```
- Kỹ thuật này giải quyết triệt để lỗi xuyên luồng (*Cross-thread Violation Exception*) trong ứng dụng Windows App SDK.

### 3.4. Gửi dữ liệu có khóa đồng bộ (`SendLock`)
- Nhiều thao tác vẽ hoặc lệnh hệ thống có thể được gọi đồng thời từ các sự kiện UI khác nhau. Hàm `SendMessage` sử dụng `lock (SendLock)` để đảm bảo các chuỗi ký tự ghi xuống `StreamWriter` không bị xen lẫn vào nhau.
