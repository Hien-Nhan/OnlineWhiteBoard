# Tài Liệu T9a: Client Zoom, Kéo Bảng & Mở Rộng Bảng Vẽ (Canvas Transform & Viewport)

- **Người thực hiện tài liệu:** Thành viên D
- **Người thực hiện code:** Thành viên C
- **Người review PR:** Thành viên E
- **Hạng mục code liên quan:** Hạng mục **T9a** (`code/WhiteboardClient/Views/BoardPage.xaml` và `code/WhiteboardClient/Views/BoardPage.xaml.cs`)
- **Hạng mục phụ thuộc:** Hạng mục **T7** (Client: Khung Win2D & Bảng vẽ tự do)

---

## 1. Mục tiêu & Yêu cầu kỹ thuật

Hạng mục **T9a** trang bị cho bảng vẽ trực tuyến khả năng thao tác không gian hiển thị chuyên nghiệp, giúp người dùng linh hoạt quan sát tổng thể hoặc phóng to để vẽ các chi tiết nhỏ mà không làm sai lệch vị trí nét vẽ giữa các người dùng với nhau.

### Yêu cầu cốt lõi:
1. **Tách biệt không gian hiển thị (Screen Viewport) và không gian logic (Board Space):**
   - Kích thước logic của bảng vẽ luôn cố định ở chuẩn 16:9 (`1280 × 720` px, render xuất ảnh chất lượng cao `2560 × 1440` px ở 192 DPI).
   - Mọi thao tác phóng to, thu nhỏ, kéo bảng chỉ là **phép biến đổi góc nhìn cục bộ** trên máy của người đó. Tọa độ nét vẽ gửi lên mạng TCP luôn quy về tọa độ logic gốc `[0..1279, 0..719]`, đảm bảo máy khác (dù kích thước cửa sổ khác, zoom khác) vẫn thấy nét vẽ xuất hiện ở đúng vị trí thực.
2. **Thu phóng quanh điểm trỏ chuột (Anchor Zoom):** Khi cuộn bánh xe chuột, điểm trên bảng vẽ nằm ngay dưới đầu con trỏ chuột được giữ cố định vị trí, bảng vẽ mở rộng/thu nhỏ đều quanh điểm đó.
3. **Giới hạn an toàn:** Tỷ lệ zoom cho phép từ `25%` đến `800%`.
4. **Kéo bảng vẽ (Pan):** Hỗ trợ kéo di chuyển bảng mượt mà bằng nút chuột giữa, nút chuột phải hoặc công cụ chuyên dụng "Di chuyển bảng".
5. **Chống trôi mất bảng (`ClampPan`):** Không cho phép kéo toàn bộ bảng vẽ trôi hoàn toàn ra ngoài khung hình; luôn giữ lại tối thiểu một góc bảng (80px) để người dùng có thể kéo lại.
6. **Mở rộng không gian vẽ (Focus Mode):** Một nút bấm ẩn/hiện đồng thời hai thanh công cụ hai bên (thanh công cụ vẽ bên trái và danh sách online bên phải) để bảng vẽ chiếm trọn 100% diện tích màn hình.

---

## 2. Các công thức toán học biến đổi không gian (Transform Math)

Hệ thống thiết lập ma trận chiếu 2D với 3 tham số trạng thái:
- `_zoom`: Hệ số zoom tương đối (mặc định = `1.0f`).
- `_panX`, `_panY`: Độ lệch tịnh tiến của bảng vẽ theo hai trục X và Y.

### 2.1. Tính tỷ lệ co giãn vừa khít (`FitScale` & `_scale`)
Để tờ giấy 1280×720 luôn vừa vặn bên trong cửa sổ ứng dụng (có chừa lề viền `BoardMargin = 20`):

$$FitScale = \min\left(\frac{W_{surface} - 2 \times Margin}{1280}, \frac{H_{surface} - 2 \times Margin}{720}\right)$$

$$Scale = FitScale \times Zoom$$

### 2.2. Điểm gốc hiển thị (`_offX`, `_offY`)
Vị trí góc trên-bên trái của tờ giấy trên màn hình hiển thị:

$$OffX = \frac{W_{surface} - 1280 \times Scale}{2} + PanX$$

$$OffY = \frac{H_{surface} - 720 \times Scale}{2} + PanY$$

### 2.3. Chuyển đổi tọa độ màn hình sang tọa độ bảng logic (`ToBoard`)
Khi người dùng bấm hoặc kéo chuột tại vị trí $(X_{screen}, Y_{screen})$ trên màn hình:

$$X_{board} = \text{clamp}\left(0, 1279, \text{round}\left(\frac{X_{screen} - OffX}{Scale}\right)\right)$$

$$Y_{board} = \text{clamp}\left(0, 719, \text{round}\left(\frac{Y_{screen} - OffY}{Scale}\right)\right)$$

### 2.4. Thu phóng giữ nguyên điểm neo con trỏ (`ZoomAt`)
Khi người dùng cuộn chuột tại điểm $Anchor(X_a, Y_a)$:
1. Tìm tọa độ logic của điểm $Anchor$ trước khi zoom:
   $$bx = \frac{X_a - OffX_{old}}{Scale_{old}}, \quad by = \frac{Y_a - OffY_{old}}{Scale_{old}}$$
2. Cập nhật hệ số zoom mới trong ngưỡng an toàn:
   $$Zoom_{new} = \max\left(0.25, \min\left(8.0, Zoom_{target}\right)\right)$$
   $$Scale_{new} = FitScale \times Zoom_{new}$$
3. Tính lại độ tịnh tiến $PanX, PanY$ để điểm logic $(bx, by)$ vẫn nằm chính xác tại tọa độ màn hình $(X_a, Y_a)$:
   $$PanX_{new} = X_a - bx \times Scale_{new} - \frac{W_{surface} - 1280 \times Scale_{new}}{2}$$
   $$PanY_{new} = Y_a - by \times Scale_{new} - \frac{H_{surface} - 720 \times Scale_{new}}{2}$$

---

## 3. Chi tiết mã nguồn cài đặt các hàm cốt lõi

### 3.1. Thuật toán `ZoomAt` và `UpdateZoomUi`
```csharp
private void ZoomAt(Point anchor, float newZoom)
{
    UpdateTransform();
    double bx = (anchor.X - _offX) / _scale;
    double by = (anchor.Y - _offY) / _scale;

    _zoom = Math.Max(MinZoom, Math.Min(MaxZoom, newZoom));
    double sc = FitScale() * _zoom;
    _panX = anchor.X - bx * sc - (Surface.ActualWidth - BoardW * sc) / 2.0;
    _panY = anchor.Y - by * sc - (Surface.ActualHeight - BoardH * sc) / 2.0;

    ClampPan();
    UpdateZoomUi();
    BoardCanvas.Invalidate();
}
```

### 3.2. Thuật toán chống trôi bảng (`ClampPan`)
Đảm bảo luôn giữ lại tối thiểu `PanKeepVisible = 80px` của bảng vẽ trong tầm mắt:
```csharp
private void ClampPan()
{
    double sc = FitScale() * _zoom;
    double w = BoardW * sc;
    double h = BoardH * sc;
    double vw = Surface.ActualWidth;
    double vh = Surface.ActualHeight;

    double minX = PanKeepVisible - w - (vw - w) / 2.0;
    double maxX = (vw - PanKeepVisible) - (vw - w) / 2.0;
    double minY = PanKeepVisible - h - (vh - h) / 2.0;
    double maxY = (vh - PanKeepVisible) - (vh - h) / 2.0;

    _panX = Math.Max(minX, Math.Min(maxX, _panX));
    _panY = Math.Max(minY, Math.Min(maxY, _panY));
}
```

### 3.3. Xử lý sự kiện cuộn bánh xe chuột (`Surface_PointerWheelChanged`)
- Mỗi nấc cuộn chuột (`MouseWheelDelta = ±120`) tương ứng độ tăng/giảm tỷ lệ khoảng 20%:
```csharp
private void Surface_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
{
    PointerPoint pp = e.GetCurrentPoint(Surface);
    int delta = pp.Properties.MouseWheelDelta;
    if (delta == 0) return;

    ZoomAt(pp.Position, _zoom * (float)Math.Pow(1.0015, delta));
    e.Handled = true;
}
```

### 3.4. Đa phương thức Kéo bảng vẽ (`Pan`)
Hệ thống cho phép người dùng kéo bảng vẽ bằng 3 cách khác nhau:
1. Nhấn giữ **chuột phải** và di chuyển.
2. Nhấn giữ **chuột giữa** (con lăn) và di chuyển.
3. Chọn công cụ vẽ **"Di chuyển bảng"** (`DrawTool.Pan`) trên thanh công cụ và nhấn giữ chuột trái.

```csharp
if (props.IsMiddleButtonPressed || props.IsRightButtonPressed ||
    (props.IsLeftButtonPressed && _tool == DrawTool.Pan))
{
    _isPanning = true;
    _panPointerStart = pp.Position;
    _panStartX = _panX;
    _panStartY = _panY;
    Surface.CapturePointer(e.Pointer);
    return;
}
```

### 3.5. Chế độ Mở rộng bảng (`FocusButton_Click`)
Tối ưu không gian làm việc cho các màn hình nhỏ/laptop:
```csharp
private void FocusButton_Click(object sender, RoutedEventArgs e)
{
    _panelsHidden = !_panelsHidden;
    Visibility v = _panelsHidden ? Visibility.Collapsed : Visibility.Visible;
    LeftPanel.Visibility = v;
    RightPanel.Visibility = v;
    FocusText.Text = _panelsHidden ? "Hiện công cụ" : "Mở rộng bảng";
    ResetView();
}
```

---

## 4. Giao diện người dùng & Trải nghiệm (UI/UX)

```
[ Thanh tiêu đề: Mã phòng, Tên user, Trạng thái mạng ]
+---------------------------------------------------------------------------------+
| [Toolbar Trái]  |                                             | [Danh sách User]|
| - Bút vẽ / Tẩy  |        VÙNG HIỂN THỊ BẢNG VẼ WIN2D         | - Online (n)    |
| - Hình khối     |                                             | - Sao chép mã   |
| - Bảng màu      |   (Nền giấy trắng 1280x720 logic,           | - Rời phòng     |
| - Cỡ nét (slider)|    đổ bóng viền, tự co giãn mượt mà)        | - Xóa bảng      |
|                 |                                             | - Lưu PNG       |
+---------------------------------------------------------------------------------+
| [Thanh trạng thái dưới]: Zoom: [-] [ 100% ] [+] | [Mở rộng bảng] | Tọa độ: X, Y |
+---------------------------------------------------------------------------------+
```

- **Cụm điều khiển Zoom:** Nút `ZoomOutButton (-)`, nhãn phần trăm `ZoomResetButton (%)` (bấm vào đưa ngay về 100% vừa khít), nút `ZoomInButton (+)`.
- **Thanh trạng thái (Bottom Status Bar):** Luôn hiển thị công cụ đang chọn, cỡ nét, mã màu HEX, phím tắt hướng dẫn cuộn chuột và tọa độ thời gian thực của chuột `X: ..., Y: ...`.

---

## 5. Đối chiếu Test Cases nghiệm thu

Hạng mục T9a đáp ứng trọn vẹn kịch bản **TC_13** trong Bộ 15 Test Case tổng thể của dự án:

| Kịch bản kiểm tra | Các bước thực hiện | Kết quả mong đợi | Đánh giá |
|---|---|---|:---:|
| **Cuộn chuột thu phóng** | Đặt con trỏ tại góc bảng vẽ, cuộn lên/xuống | Bảng vẽ phóng to/thu nhỏ quanh đúng con trỏ trong ngưỡng 25% - 800% | **ĐẠT** |
| **Nút bấm Zoom trên thanh bar** | Bấm `+`, `−`, bấm vào nút hiển thị `%` | `+` tăng 25%, `−` giảm 25%, bấm ô `%` đưa bảng vẽ vừa khít cửa sổ | **ĐẠT** |
| **Kéo bảng bằng chuột phải / giữa** | Zoom lên > 200%, giữ chuột phải/giữa kéo khắp màn hình | Bảng di chuyển mượt mà theo tay kéo; không bị trôi mất khỏi khung nhìn | **ĐẠT** |
| **Mở rộng bảng (Focus mode)** | Bấm nút "Mở rộng bảng" | Hai thanh công cụ ẩn đi, bảng vẽ tự động phóng lớn vừa khít khung ngang | **ĐẠT** |
| **Vẽ chính xác khi đang Zoom** | Zoom bảng lên 300%, vẽ nét tự do và hình khối | Nét vẽ xuất hiện chuẩn xác ngay dưới đầu bút; máy khác thấy đúng vị trí logic | **ĐẠT** |

