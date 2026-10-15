# QUY TRÌNH GIT – Online Whiteboard

> Mọi thành viên (kể cả Leader) làm theo đúng quy trình này. Giảng viên kiểm tra repo cuối mỗi tuần, nên lịch sử commit phải **rõ ràng, đúng người, đúng việc**.

## 1. Mô hình nhánh

```
main      ●────────────────────●─────────────────●        ← bản ổn định, demo được (chỉ Leader merge từ develop)
           \                  / \               /
develop     ●──●──●──●──●──●─●───●──●──●──●──●─●          ← nhánh tích hợp của cả nhóm
               \  /  \   /     \   /
feature/…       ●●    ●●●        ●●                         ← mỗi chức năng/module 1 nhánh, rẽ từ develop
```

| Nhánh | Dùng để | Ai được push trực tiếp | Merge vào |
|---|---|---|---|
| `main` | Bản ổn định, nộp/demo | **Không ai** (chỉ merge PR từ `develop`, do Leader duyệt) | – |
| `develop` | Tích hợp code các module | **Không ai** (chỉ merge PR từ `feature/*`) | `main` (cuối tuần) |
| `feature/<module>-<chức-năng>` | Làm 1 chức năng/file cụ thể | Chủ nhánh | `develop` |

### Tên nhánh feature (đặt theo module/chức năng, viết thường, nối bằng `-`)

| Module | Người | Nhánh gợi ý |
|---|---|---|
| M1 | Leader | `feature/protocol-spec`, `feature/shared-packet` |
| M7 | Leader | `feature/stress-test-tool` |
| M2 | TV1 | `feature/server-config`, `feature/server-session`, `feature/server-validation` |
| M3 | TV2 | `feature/server-database`, `feature/server-logger`, `feature/server-rooms` |
| M4 | TV3 | `feature/client-network`, `feature/client-reconnect` |
| M5 | TV4 | `feature/client-status-ui`, `feature/client-login`, `feature/client-error-handling` |
| M6 | TV5 | `feature/canvas-drawing`, `feature/canvas-sync` |
| Tài liệu / test | ai làm | `feature/docs-<mục>`, `feature/test-<module>` |

Quy tắc: **1 nhánh = 1 chức năng nhỏ, sống ngắn (vài ngày)**, xong thì PR rồi xóa nhánh. Không dùng tên như `test`, `abc`, `update`, `tên-mình`.

### Nhánh cũ đang có trên GitHub
- `client`, `server`, `ui`, `update` đã được merge hết vào `main` → **xóa** sau khi Leader xác nhận (`git branch -r --merged origin/main`).
- `feature/canvas-graphics` còn **1 commit chưa vào main** → TV5 xem lại, merge qua PR vào `develop` hoặc bỏ nếu trùng.

## 2. Thiết lập một lần (mỗi người)

```bash
git clone https://github.com/<tổ-chức-hoặc-user>/<tên-repo>.git
cd <tên-repo>

# BẮT BUỘC dùng tài khoản cá nhân để lịch sử ghi đúng người
git config user.name  "Họ Tên Thật"
git config user.email "email-gắn-với-tài-khoản-github-của-bạn"

git checkout develop
git pull
```

### Leader thiết lập một lần
```bash
git checkout main && git pull
git checkout -b develop
git push -u origin develop
```
Trên GitHub → Settings → Branches → bật **Branch protection** cho `main` và `develop`:
- Require a pull request before merging (≥ 1 approval)
- Chặn push trực tiếp (kể cả Leader)
- Tạo Labels / Issues cho từng đầu việc trong `PHAN_CONG_CONG_VIEC.md`

## 3. Quy trình làm việc hằng ngày (từng bước)

```bash
# 1. Cập nhật develop mới nhất
git checkout develop
git pull

# 2. Tạo nhánh chức năng từ develop
git checkout -b feature/server-logger

# 3. Code… rồi xem mình đã đổi gì
git status
git diff

# 4. Thêm ĐÚNG file mình sửa (hạn chế `git add .`)
git add Code/WhiteboardApp/WhiteboardServer/Logger.cs
git add Extra/progress/TV2_<tên>.md

# 5. Commit theo quy ước (mục 4)
git commit -m "feat(logger): ghi log file theo ngày cho kết nối và lỗi"

# 6. Trước khi push: cập nhật theo develop để giảm conflict
git fetch origin
git rebase origin/develop        # nếu conflict: sửa file → git add <file> → git rebase --continue

# 7. Push nhánh của mình
git push -u origin feature/server-logger
# (đã rebase rồi push lại thì dùng: git push --force-with-lease)

# 8. Mở Pull Request trên GitHub: feature/server-logger → develop
```

**Mỗi ngày làm việc:** commit nhỏ, push nhánh của mình **cuối buổi** để không mất việc và để giảng viên thấy tiến độ.

## 4. Quy ước commit message

Định dạng:

```
<type>(<scope>): <mô tả ngắn, bắt đầu bằng động từ, ≤ 72 ký tự>

[nội dung chi tiết nếu cần: làm gì, vì sao]
[Refs #<số issue>]
```

### type
| type | Dùng khi |
|---|---|
| `feat` | Thêm chức năng mới |
| `fix` | Sửa lỗi |
| `refactor` | Sắp xếp lại code, không đổi hành vi |
| `test` | Thêm/sửa test, tool test, test case |
| `docs` | README, PROTOCOL, nhật ký tiến độ, báo cáo `.md` |
| `chore` | Cấu hình, `.gitignore`, csproj, thư viện |
| `style` | Định dạng code (không đổi logic) |

### scope (theo module/file)
`protocol`, `shared`, `server-core`, `server-config`, `session`, `validation`, `database`, `logger`, `rooms`, `client-net`, `reconnect`, `status-ui`, `login`, `error-handling`, `canvas`, `canvas-sync`, `stress-test`, `readme`, `progress`, `git`

### Ví dụ ĐÚNG ✅
```
feat(logger): ghi log file theo ngày cho kết nối, ngắt kết nối và lỗi
fix(session): bỏ dòng dở dang khi client đứt kết nối giữa chừng
feat(client-net): thêm timeout 5 giây cho ConnectAsync
refactor(server-core): tách Program.cs thành ClientSession và Broadcaster
fix(client-net): bỏ WriteLine thừa gây gói tin rỗng
test(stress-test): thêm kịch bản 50 client gửi nét vẽ trong 60 giây
docs(progress): cập nhật nhật ký TV3 tuần 2
chore(git): thêm *.db vào .gitignore
```

### Ví dụ SAI ❌
```
update
fix
final
sửa lỗi
done
test 123
```

### Quy tắc
1. **1 commit = 1 thay đổi có ý nghĩa**; không gộp nhiều việc không liên quan.
2. Không commit code **không build được** lên nhánh chung.
3. Dùng **tài khoản cá nhân**; không commit hộ người khác (nếu làm chung, thêm dòng `Co-authored-by: Tên <email>`).
4. **Tuyệt đối không commit:** `bin/`, `obj/`, `.vs/`, `*.db`, password, secret, private key, dữ liệu thật (dữ liệu demo phải là dữ liệu giả).
5. Mỗi commit có code nên kèm cập nhật **file nhật ký cá nhân** (`Extra/progress/…`) – có thể gộp ở commit `docs(progress)` trong cùng PR.

## 5. Pull Request & review

**Tiêu đề PR** theo cùng quy ước: `feat(logger): ghi log file theo ngày`.

**Mô tả PR (copy mẫu):**
```
## Làm gì
- …
## File thay đổi
- Code/WhiteboardApp/WhiteboardServer/Logger.cs (mới)
## Cách kiểm tra
- Chạy Server, kết nối 1 client rồi tắt đột ngột, xem logs/server_*.log
## Độ hoàn thiện: 75%
Closes #12
```

**Quy tắc review**
- Reviewer = người được giao ở bảng *Owner/Reviewer* trong `PHAN_CONG_CONG_VIEC.md` (+ Leader nếu cần).
- Reviewer **build và chạy thử**, không chỉ đọc. Để lại ≥ 1 nhận xét có nội dung (câu hỏi hoặc gợi ý) – đây là phần học.
- Cần **≥ 1 approval** mới merge. Không tự approve PR của mình.
- Merge bằng **"Create a merge commit"** (không squash) để giữ từng commit và tên tác giả của mỗi người.
- Merge xong: xóa nhánh feature (`git push origin --delete feature/...` hoặc nút *Delete branch*).

## 6. Merge hằng tuần: `develop` → `main`

Chỉ **Leader** làm, vào **cuối mỗi tuần, trước hạn giảng viên kiểm tra**:

1. Chắc chắn `develop` build được cả Server lẫn Client và chạy được kịch bản demo cơ bản.
2. Mở PR `develop` → `main`, tiêu đề: `chore(release): tuần N – <tóm tắt>`.
3. Sau merge, đặt tag: `git tag -a week-N -m "Tuần N"` rồi `git push origin week-N`.
4. Cập nhật `Extra/TONG_HOP_TIEN_DO.md`.

## 7. Xử lý conflict

- Giảm conflict bằng: nhánh ngắn, `rebase origin/develop` thường xuyên, **chỉ sửa file thuộc module của mình**.
- File hay conflict: `Form1.Designer.cs`, `Form1.cs`, `Program.cs` (server) → quy ước ở `PHAN_CONG_CONG_VIEC.md` (TV4 là người duy nhất sửa Designer).
- Khi conflict: đọc kỹ cả 2 phía → giữ phần đúng của cả hai (không chọn bừa "accept mine/theirs") → build lại → chạy thử → `git add` → `git rebase --continue`.
- Không biết xử lý thì **dừng và hỏi Leader**, không `push --force` lên `develop`/`main`.

## 8. Cấu trúc thư mục repo (theo yêu cầu môn)

```
OnlineWhiteboard_NetworkProgramming/
├── README.md                  ← tên + mã đề, thành viên, kiến trúc, môi trường, cấu hình, hướng dẫn chạy, link video
├── .gitignore
├── Code/
│   └── WhiteboardApp/
│       ├── WhiteboardServer/
│       ├── WhiteboardClient/
│       ├── WhiteboardShared/
│       └── WhiteboardStressTest/        ← mới (M7)
├── DOCX/                      ← báo cáo Word cuối cùng
├── PPTX/                      ← slide thuyết trình
└── Extra/
    ├── PROTOCOL.md
    ├── PHAN_CONG_CONG_VIEC.md
    ├── QUY_TRINH_GIT.md
    ├── TONG_HOP_TIEN_DO.md
    ├── progress/              ← nhật ký .md của từng người
    ├── evidence/              ← log, ảnh, video, CSV kết quả test
    └── Whiteboard_System_Test_Cases.xlsx
```

## 9. Checklist trước khi bấm "Create Pull Request"

- [ ] Build Server + Client không lỗi, đã tự chạy thử
- [ ] Chỉ có file thuộc module của mình trong `git status` (không có `bin/`, `obj/`, `*.db`)
- [ ] Không có password/secret/dữ liệu thật
- [ ] Commit message đúng quy ước, đúng tài khoản cá nhân
- [ ] Đã rebase theo `origin/develop`
- [ ] Đã cập nhật nhật ký `Extra/progress/…` (file nào, % hoàn thiện)
- [ ] Đã gán Reviewer