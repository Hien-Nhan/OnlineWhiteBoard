# Tài Liệu T3: Server Quản Lý Phòng & Phát Nét Vẽ (Room Management & Broadcast)

- **Người thực hiện tài liệu:** Thành viên D
- **Người thực hiện code:** Thành viên C
- **Người review PR:** Thành viên E
- **Hạng mục code liên quan:** Hạng mục **T3** (`code/WhiteboardServer/RoomManager.cs` và `code/WhiteboardServer/ClientHandler.cs`)
- **Hạng mục phụ thuộc:** Hạng mục **T2** (Server TCP nền & Cấu hình mạng)

---

## 1. Mục tiêu & Tổng quan kiến trúc

Mô-đun **T3 (Server: Quản lý phòng & Phát nét vẽ)** đóng vai trò là "trái tim" định tuyến thông tin của máy chủ `WhiteboardServer`. Hệ thống cho phép nhiều nhóm người dùng cùng hoạt động đồng thời trên một máy chủ mà không bị can thiệp dữ liệu lẫn nhau.

### Mục tiêu kỹ thuật cốt lõi:
1. **Quản lý phòng vẽ linh hoạt:** Hỗ trợ tạo và gia nhập phòng vẽ bất kỳ theo mã phòng (`RoomID`), tự động khởi tạo phòng khi có người vào và tự động hủy giải phóng bộ nhớ khi phòng không còn ai.
2. **Cô lập dữ liệu tuyệt đối (Room Isolation):** Dữ liệu nét vẽ, lệnh xóa bảng, danh sách thành viên chỉ được luân chuyển nội bộ giữa các client đang ở **cùng một phòng**.
3. **Phát sóng thời gian thực (Real-time Broadcast):** Định tuyến tọa độ nét vẽ tức thì đến các thành viên trong phòng với độ trễ thấp (< 50ms trong mạng nội bộ).
4. **An toàn đa luồng (Thread-safety):** Xử lý đồng thời hàng chục kết nối đọc/ghi socket độc lập mà không xảy ra hiện tượng tranh chấp tài nguyên (*Race condition*) hoặc khóa chết (*Deadlock*).

---

## 2. Thiết kế các thành phần cốt lõi

```
WhiteboardServer
├── Program.cs           (Vòng lặp lắng nghe TcpListener)
├── ClientHandler.cs     (Quản lý phiên kết nối của từng Client)
└── RoomManager.cs       (Bộ điều phối trung tâm quản lý danh sách phòng)
```

### 2.1. Lớp `RoomManager` (Singleton Pattern)
- **Vị trí file:** `code/WhiteboardServer/RoomManager.cs`
- **Mô hình Singleton:** Sử dụng `Lazy<RoomManager>` đảm bảo chỉ tồn tại duy nhất một thực thể quản trị phòng trong suốt vòng đời của Server:
  ```csharp
  private static readonly Lazy<RoomManager> _instance = new(() => new RoomManager());
  public static RoomManager Instance => _instance.Value;
  ```
- **Cấu trúc lưu trữ:**
  - `Dictionary<string, List<ClientHandler>> _rooms`: Bảng băm lưu cặp khóa - giá trị gồm `Mã phòng` và `Danh sách ClientHandler` đang có mặt trong phòng đó. Sử dụng `StringComparer.OrdinalIgnoreCase` để không phân biệt hoa thường khi nhập mã phòng.
  - `object _lock = new()`: Đối tượng khóa dùng để đồng bộ hóa mọi thao tác thêm, xóa, duyệt từ điển phòng.

### 2.2. Lớp `ClientHandler` (Phiên kết nối Client độc lập)
- **Vị trí file:** `code/WhiteboardServer/ClientHandler.cs`
- Mỗi kết nối từ ứng dụng Client sẽ được `Program.cs` trao quyền cho một đối tượng `ClientHandler` chạy trên một tiến trình nền riêng biệt (`Thread` với `IsBackground = true`).
- Chịu trách nhiệm:
  - Thiết lập thuộc tính socket không trễ: `_tcpClient.NoDelay = true`.
  - Vòng lặp đọc luồng mạng UTF-8: `reader.ReadLine()`.
  - Phân tách và phân loại gói tin (`ProcessPacket`).
  - Đồng bộ hóa gửi dữ liệu có khóa: `Send(string message)` dùng `lock (_sendLock)`.

---

## 3. Cơ chế hoạt động chi tiết

### 3.1. Quy trình Tham gia phòng (`JoinRoom`)
Khi người dùng bấm "Vào phòng" hoặc "Tạo phòng mới" từ Client, gói tin `JOIN_ROOM;<Username>;<RoomID>` hoặc `CREATE_ROOM;<Username>;<RoomName>` được gửi tới Server:

1. `ClientHandler.HandleRoomJoin()` trích xuất `Username` và `RoomID`.
2. Gọi `RoomManager.Instance.JoinRoom(targetRoom, this)`:
   - Nếu client đang ở một phòng khác trước đó, hàm sẽ tự động gọi `LeaveRoomInternal()` để đưa client ra khỏi phòng cũ.
   - Kiểm tra `_rooms`: Nếu mã phòng chưa tồn tại, tự động tạo mới một `List<ClientHandler>`.
   - Thêm `this` vào danh sách thành viên của phòng và gán `client.CurrentRoomId = targetRoom`.
3. **Tích hợp T4 (SQLite History):** Server gọi `DatabaseManager.GetRoomHistory(targetRoom)` để phát lại tuần tự toàn bộ các nét vẽ đã lưu trước đó cho thành viên mới (người vào sau vẫn thấy trọn vẹn bức vẽ).
4. **Cập nhật danh sách online:** Kích hoạt `RoomManager.Instance.BroadcastUserList(targetRoom)` để thông báo danh sách thành viên mới nhất cho tất cả mọi người trong phòng.

---

### 3.2. Cơ chế Phát nét vẽ thời gian thực (`Broadcast`)
Khi người dùng vẽ nét tự do hoặc hình khối, gói tin `DRAW;...;<RoomID>` được gửi lên Server:

```csharp
public void Broadcast(string roomId, string message, ClientHandler? sender = null)
{
    if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(message)) return;

    List<ClientHandler> targets;
    lock (_lock)
    {
        if (!_rooms.TryGetValue(roomId, out var list) || list.Count == 0) return;
        // Kỹ thuật Snapshot: Sao chép danh sách để tránh giữ lock lâu trong khi thực hiện I/O mạng
        targets = new List<ClientHandler>(list);
    }

    foreach (var client in targets)
    {
        // Loại trừ người gửi: Người vẽ không nhận lại nét vẽ của chính mình
        if (sender != null && ReferenceEquals(client, sender))
        {
            continue;
        }

        client.Send(message);
    }
}
```

#### Điểm sáng kỹ thuật:
- **Loại trừ người gửi (`sender: this`):** Client khi vẽ đã tự dựng nét trên canvas cục bộ ngay lập tức để tạo cảm giác mượt mà không trễ. Server gửi trả lại chính nét đó sẽ làm dư thừa băng thông và gây hiện tượng vẽ đè (chớp nháy). Bằng cách so sánh tham chiếu `ReferenceEquals(client, sender)`, gói vẽ chỉ chuyển tiếp đến những người còn lại trong phòng.
- **Kỹ thuật Snapshot Copy:** Bản sao danh sách `targets` được tạo ra trong phạm vi lock siêu ngắn. Vòng lặp `foreach` gửi dữ liệu mạng qua TCP thực hiện bên ngoài khối lock, giúp tránh hiện tượng nghẽn luồng kết nối (*Lock Contention*) khi mạng chập chờn.

---

### 3.3. Cơ chế Xóa bảng theo phòng (`CLEAR_CANVAS`)
- Khi một người trong phòng bấm nút "Xóa toàn bộ bảng", lệnh `CLEAR_CANVAS;<RoomID>` được gửi đến Server.
- Server gọi `_roomManager.Broadcast(targetRoom, rawPacket, sender: this)` gửi lệnh xóa bảng tới các client khác trong phòng để làm trắng bảng vẽ.
- Đồng thời gọi `DatabaseManager.ClearRoomHistory(targetRoom)` (Hạng mục T4) để dọn sạch dữ liệu nét vẽ phòng đó trong SQLite.

---

### 3.4. Cơ chế Ngắt kết nối & Dọn dẹp phòng trống (Tránh rò rỉ bộ nhớ)
Khi client đóng ứng dụng, bấm nút "Rời phòng", hoặc mất kết nối mạng đột ngột (`IOException`/`SocketException`):
1. Khối `finally` trong `ProcessLoop` kích hoạt phương thức `Disconnect()`.
2. Hàm gọi `_roomManager.LeaveRoom(this)`:
   - Gỡ bỏ client khỏi danh sách phòng tương ứng.
   - **Tự dọn dẹp phòng trống:** Nếu phòng không còn bất kỳ thành viên nào (`clientList.Count == 0`), phòng sẽ được xóa bỏ hoàn toàn khỏi `Dictionary _rooms`:
     ```csharp
     if (clientList.Count == 0)
     {
         _rooms.Remove(oldRoom);
         Console.WriteLine($"[ROOM] Phòng [{oldRoom}] đã trống và được dọn dẹp.");
     }
     ```
   - Nhờ vậy, bộ nhớ RAM của Server không bị phình to sau thời gian dài vận hành với nhiều phòng rác.
3. Nếu phòng vẫn còn thành viên khác, Server tự động phát lại gói `USER_LIST` cập nhật sĩ số mới để loại bỏ tên người vừa rời khỏi danh sách online của các client còn lại.

---

## 4. Sơ đồ tuần tự tương tác (Sequence Diagram)

```mermaid
sequenceDiagram
    autonumber
    actor A as Client A (ROOM1)
    actor B as Client B (ROOM1)
    actor C as Client C (ROOM2)
    participant S as ClientHandler (Server)
    participant RM as RoomManager (Singleton)
    participant DB as DatabaseManager (SQLite)

    Note over A,B: Hai Client cùng tham gia ROOM1
    A->>S: DRAW;FREEHAND;...;ROOM1
    S->>RM: Broadcast("ROOM1", packet, sender: A)
    RM-->>B: client.Send(packet) [Nhận được]
    RM--x A: [Bị loại trừ, không nhận lại]
    RM--x C: [Thuộc ROOM2, tuyệt đối không nhận]
    S->>DB: SaveDraw("ROOM1", "UserA", packet)

    Note over B: Client B rời phòng
    B->>S: Ngắt kết nối / Đóng cửa sổ
    S->>RM: LeaveRoom(ClientB)
    RM->>RM: BroadcastUserList("ROOM1")
    RM-->>A: USER_LIST;UserA [Cập nhật còn 1 người]
```

---

## 5. Đặc tả kỹ thuật Mạng & Đa luồng (Concurrency & Thread-Safety)

| Kỹ thuật áp dụng | Vị trí cài đặt | Lợi ích đạt được |
|---|---|---|
| **Vô hiệu hóa Nagle (`NoDelay = true`)** | `ClientHandler.ProcessLoop` | Gửi gói tin tọa độ nét vẽ ngay lập tức, triệt tiêu độ trễ làm giật đường vẽ. |
| **Khóa đồng bộ danh sách phòng (`_lock`)** | `RoomManager` | Đảm bảo an toàn tuyệt đối khi hàng chục luồng cùng thêm/xóa/đọc danh sách phòng. |
| **Kỹ thuật Snapshot List** | `RoomManager.Broadcast` | Thu hẹp thời gian giữ lock, tránh tình trạng Server bị nghẽn khi một Client nhận chậm. |
| **Khóa gửi tin (`_sendLock`)** | `ClientHandler.Send` | Đảm bảo tính toàn vẹn gói tin, không để các luồng gửi đồng thời làm xáo trộn byte trên stream. |
| **Xử lý ngắt kết nối an toàn** | `ClientHandler.ProcessLoop` | Bắt trọn vẹn `IOException` và `SocketException`, Server không bao giờ bị crash khi client mất mạng đột ngột. |

---

## 6. Đối chiếu Test Cases nghiệm thu

Mô-đun T3 đã được kiểm thử và nghiệm thu thành công với 5 test case chuyên biệt trong tài liệu `Extra/tests/TC-C.md`:

| Mã TC | Tên kịch bản | Thao tác kiểm tra | Kết quả đạt được | Đánh giá |
|:---:|---|---|---|:---:|
| **TC_T3_01** | Gia nhập phòng thành công | Client gửi `JOIN_ROOM;UserA;ROOM001\n` | Server thêm client, trả về đúng danh sách `USER_LIST` | **ĐẠT** |
| **TC_T3_02** | Đồng bộ nét vẽ cùng phòng | Client A và B cùng `ROOM001`, A gửi `DRAW` | Client B nhận tức thì nét vẽ, Client A không bị dội ngược | **ĐẠT** |
| **TC_T3_03** | Cô lập dữ liệu giữa các phòng | Client A ở `ROOM001`, Client C ở `ROOM002`, A vẽ | Client C tuyệt đối không nhận bất kỳ nét vẽ nào từ phòng của A | **ĐẠT** |
| **TC_T3_04** | Đồng bộ lệnh xóa bảng | Client gửi `CLEAR_CANVAS;ROOM001\n` | Toàn bộ thành viên trong phòng nhận lệnh xóa bảng đồng bộ | **ĐẠT** |
| **TC_T3_05** | Tự động dọn dẹp khi ngắt kết nối | Client ngắt mạng hoặc đóng app đột ngột | Server giải phóng socket an toàn, tự trừ sĩ số và cập nhật cho phòng | **ĐẠT** |

Đồng thời đáp ứng trọn vẹn các kịch bản trong **Bộ 15 Test Case tổng thể của dự án**:
- **TC_04 (Phòng: Vào phòng bằng mã và danh sách online)**
- **TC_09 (Đồng bộ thời gian thực: Nhiều người cùng vẽ trong một phòng)**
- **TC_15 (Mạng & ổn định: Mất kết nối và thoát đột ngột)**

