# Tự động build Android / Windows / macOS / iOS

Sau khi commit và **push lên `main`**, bốn workflow tự chạy:

| Workflow | Kết quả |
| --- | --- |
| Android APK release | APK đã ký + SHA-256 |
| Windows EXE release | Bộ cài Windows x64 EXE + SHA-256 |
| macOS DMG release | Hai ảnh đĩa DMG Apple Silicon (arm64) và Intel (x64) + SHA-256 riêng |
| iOS build (Simulator and iPhone IPA) | IPA chưa ký + SHA-256 lên Releases; Simulator/IPA/log vẫn ở Artifacts |

Mọi push `main` đều kích hoạt, kể cả chỉ sửa tài liệu. Checkout được ghim đúng
`github.sha` của sự kiện, nên push tiếp không làm lượt chạy trước build nhầm commit.
Windows/Android/macOS vẫn hỗ trợ push tag `vX.Y.Z` và **Run workflow** thủ công.
Không cần bấm chạy riêng từng workflow sau push `main`.

Các bản build thành công luôn được lưu trong **Actions → lượt chạy → Artifacts**
trong 7 ngày. Windows/Android/macOS tự publish vào cùng Release khi version/tag
chưa có. Với push tự động, nếu version đã thuộc commit khác hoặc đã đủ asset
của platform đó, bước publish báo bỏ qua và giữ nguyên file cũ; bản build mới
vẫn tải được trong Artifacts. Muốn phát hành code mới, tăng version rồi push.
Chạy thủ công vẫn kiểm tra xung đột tag/file nghiêm ngặt, không ghi đè binary.
iOS đưa IPA chưa ký và SHA-256 lên cùng Release sau khi build thành công; Simulator
và log vẫn ở Artifacts. Pull request chỉ build, không publish. Không upload TestFlight.

## macOS

Workflow build trên runner `macos-26`, tạo hai bản self-contained từ cùng commit:
`osx-arm64` cho Apple Silicon, `osx-x64` cho Intel. Bản Intel được cross-publish
trên runner Apple Silicon. Dùng `hdiutil` của macOS để tạo DMG kéo thả vào Applications.

```text
QuizDesk-X.Y.Z-macos-arm64.dmg
QuizDesk-X.Y.Z-macos-arm64-DMG-SHA256.txt
QuizDesk-X.Y.Z-macos-x64.dmg
QuizDesk-X.Y.Z-macos-x64-DMG-SHA256.txt
```

Cần cả hai job build thành công trước khi publish macOS. Mỗi kiến trúc có
artifact/checksum riêng. App được ký ad-hoc trên runner và kiểm tra lại sau khi
sao chép khỏi DMG; chưa có Developer ID/notarization. Không cần Apple signing
secret cho luồng build thử này. Chưa chạy thử giao diện trên Mac người dùng.
PKG vẫn tạo riêng để chẩn đoán; không publish lên Releases mới.

## Thiết lập Android một lần

Trong repo: **Settings → Secrets and variables → Actions → New repository secret**:

| Secret | Nội dung |
| --- | --- |
| `ANDROID_KEYSTORE_BASE64` | Base64 của file khóa ký QuizDesk gốc |
| `ANDROID_KEYSTORE_PASSWORD` | Mật khẩu keystore |
| `ANDROID_KEY_PASSWORD` | Mật khẩu private key |
| `ANDROID_KEY_ALIAS` | Alias của khóa ký |

Giữ khóa gốc để APK mới cập nhật đè bản đang cài mà giữ dữ liệu. Không tạo khóa
mới cho mỗi lượt CI. Script release dừng nếu thiếu khóa hoặc sai chứng chỉ.
Không đưa keystore, Base64 hoặc mật khẩu vào file source, commit hay log.

Để sao chép Base64 vào clipboard trên máy cá nhân (thay đường dẫn bằng file
khóa gốc thật), chạy PowerShell:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes('C:\duong-dan-khoa-goc\quizdesk-dev.keystore')) | Set-Clipboard
```

Dán vào secret `ANDROID_KEYSTORE_BASE64`, sau đó xóa nội dung clipboard.
Mặc định workflow kiểm tra chứng chỉ QuizDesk cũ có SHA-256:

```text
45fca4ca62aa2dea2c0e7dc710ac8f54529a785bdca977b09d209fd74dcc1172
```

Nếu chủ động dùng chứng chỉ khác, đặt repository **variable**
`ANDROID_SIGNING_CERT_SHA256` bằng SHA-256 của chứng chỉ đó (không phải hash của
file keystore). APK ký khóa khác sẽ không cập nhật đè bản cũ thông thường.

Chỉ APK đã qua `apksigner verify` và kiểm tra package `com.quizdesk.mobile`,
phiên bản/chứng chỉ mới được upload. Mật khẩu được truyền bằng tham chiếu biến
môi trường của .NET Android; keystore được xóa khỏi runner sau khi build.

## Tạo phiên bản phát hành mới

1. Commit toàn bộ source muốn phát hành, bao gồm workflow/script mới.
2. Đồng bộ version `X.Y.Z` ở:
   - `QuizDesk.Desktop/QuizDesk.Desktop.csproj`: `Version`.
   - `QuizDesk.Mobile/QuizDesk.Mobile.csproj`: `Version`.
   - `QuizDesk.Android/QuizDesk.Android.csproj`: `ApplicationDisplayVersion`.
   Tăng cả `ApplicationVersion` Android (số nguyên lớn hơn bản đã phát hành,
   hiện bản gốc là 60100). Nếu cập nhật iOS cùng đợt, đồng bộ version/build iOS.
   Inno Setup nhận version trực tiếp từ CI, không cần sửa tên bộ cài thủ công.
3. Commit/push các thay đổi version, rồi tạo tag khớp source, ví dụ `v5.16.4`:

```powershell
git tag v5.16.4
git push origin v5.16.4
```

4. Theo dõi cả Android APK release, Windows EXE release và macOS DMG release.
5. Với version mới, file được đưa vào cùng Release `QuizDesk X.Y.Z`:

```text
QuizDesk-X.Y.Z-Android.apk
QuizDesk-X.Y.Z-Android-SHA256.txt
QuizDesk-Setup-X.Y.Z-x64.exe
QuizDesk-X.Y.Z-Windows-SHA256.txt
```

Không cần tạo Release bằng tay. GitHub token mặc định của workflow được dùng;
không cần PAT. Repo private thì Release cũng chỉ người có quyền repo mới tải được.
EXE hiện không ký Authenticode; workflow không thêm chứng chỉ Windows.

## Chạy thủ công / thử lại

Sau khi commit/push thay đổi lên nhánh mặc định:

1. Vào workflow tương ứng → **Run workflow** → chọn nhánh `main`.
2. Để trống ô `tag` để dùng version source (hiện là `v5.16.4`), hoặc nhập tag
   khớp version. Không cần tự tạo tag trước khi chạy thủ công.
3. Nếu chạy thủ công, chạy cả Windows/Android/macOS trên cùng commit; hoặc chỉ
   push `main` để cả bốn workflow tự bắt đầu.
4. Sau khi build thành công, job publish tạo tag tại đúng commit đã build nếu
   tag chưa có. Tag đã có phải trỏ đúng commit đó (cũng hỗ trợ annotated tag).

Nếu một tag đã trỏ đến commit khác, workflow dừng, không di chuyển tag hay ghi
đè file đã phát hành. Để build lại đúng source cũ, chọn chính tag đó ở dropdown
**Use workflow from**, hoặc tăng version và phát hành tag mới. Tag được chọn
phải chứa workflow/script mới. Không dùng `v5.11.0` để phát hành source mới.

Push `main` kích hoạt cả bốn workflow. Tag do `GITHUB_TOKEN` tạo khi publish
không kích hoạt thêm lượt chạy, tránh build lặp. Có thể push tag từ máy cá nhân
để gọi riêng nhóm workflow phát hành Windows/Android/macOS.

Build và publish là hai job riêng. Build/test thất bại thì không upload lên
Release; platform còn lại vẫn có thể hoàn tất độc lập. Nếu chỉ publish lỗi,
chọn chạy lại job lỗi để dùng lại artifact đã build. Artifact giữ 7 ngày;
file trên Releases không theo hạn lưu artifact này.

Nhóm publish tách theo platform/kiến trúc/commit để không hủy lượt của nhau.
Nếu cùng tạo tag/Release, script đọc lại và đối chiếu commit trước khi upload. Chạy lại bỏ qua asset đã có nếu SHA-256 giống nhau; dừng nếu cùng tên
nhưng bytes khác hoặc không xác minh được. Không dùng `--clobber` và không xóa
Release/bộ cài cũ. Nếu cần thay đổi binary đã phát hành, tăng version và tạo tag mới.
Build lại toàn bộ có thể tạo bytes khác do thời gian đóng gói, nên tránh rebuild
để thay thế asset đã công bố.

## Kiểm tra và quyền

- Chạy 24 kiểm thử publish giả lập (không ghi GitHub), 19 kiểm thử nhập nhiều file,
  30 gộp đề, 39 media và 25 xuất định dạng/trích câu và 25 xuất Office trước khi đóng gói.
- macOS có 12 kiểm thử logic đóng gói với Apple tools giả lập.
- Kiểm tra SHA-256 artifact tải xuống trước khi publish.
- Actions được pin vào commit SHA của bản chính thức.
- Build job chỉ có `contents: read`; publish job mới có `contents: write`.
- Workflow release không chạy trên pull request. Push `main` hoặc tag tự chạy;
  chạy thủ công dùng nhánh/tag được chọn;
  tag mới chỉ được tạo trong job publish sau khi build thành công.
- Android cần GitHub Secrets trước lượt chạy đầu; Windows không cần signing secret.
- Chưa chạy build APK/EXE hoặc publish thật trên GitHub cho các workflow mới.

## macOS DMG và chẩn đoán PKG

Actions đóng gói `QuizDesk-<version>-macos-arm64.dmg` (chip M) và
`QuizDesk-<version>-macos-x64.dmg` (Intel), kèm `DMG-SHA256.txt` riêng.
Releases macOS dùng DMG. Mở ảnh đĩa, kéo QuizDesk.app vào Applications rồi eject.
Ứng dụng chưa có Developer ID/notarization; DMG chỉ thay luồng cài PKG.

Build phải qua `hdiutil verify`, mount chỉ đọc, kiểm tra version/kiến trúc/chữ ký
và sao chép bundle ra khỏi ảnh đĩa rồi xác minh lại chữ ký. Chưa kiểm thử chạy UI.
PKG vẫn giữ trong Actions Artifacts cho việc chẩn đoán, không upload PKG mới lên Releases.
Cài thử PKG ARM64 thất bại sẽ hiện trong step summary và artifact `macos-install-logs-arm64-…`;
không chặn DMG đã kiểm tra độc lập. Lỗi build/kiểm tra DMG vẫn chặn publication.

Bản đã phát hành không bị thay thế. Nếu `v5.14.0` đã trỏ tới commit khác,
DMG của commit mới chỉ nằm trong Actions Artifacts. Muốn publish bản sửa,
tăng version đồng bộ rồi push để tạo release mới theo quy trình hiện tại.

## IPA trên GitHub Releases

Workflow iOS chạy khi push `main`, push tag `v*`, pull request vào `main`, hoặc Run workflow.
Job `Publish unsigned IPA to GitHub Releases` chỉ chạy khi job `unsigned-ipa` thành công
và sự kiện không phải pull request. Job build chỉ có quyền đọc; job publish có `contents: write`.
Checkout và release tag dùng đúng commit đã build; kiểm tra version iOS đồng bộ với các nền tảng.

File tải trực tiếp trong Releases:

```text
QuizDesk-X.Y.Z-ios-arm64-unsigned.ipa
QuizDesk-X.Y.Z-iOS-unsigned-SHA256.txt
```

IPA gốc được đối chiếu checksum trước khi đổi tên, rồi kiểm tra SHA-256 lần nữa sau
khi tải Artifact về job publish. Giữ thêm IPA/log ở Actions Artifacts trong 7 ngày.
Đây là IPA chưa ký: cần ký trước khi cài iPhone, không phải bản TestFlight/App Store.
Không cần thêm Apple signing secrets cho luồng build chưa ký hiện tại.
Version đã gắn tag ở commit khác không bị thay thế; tăng version để publish commit mới.
Lượt chạy lại cùng commit bỏ qua asset giống hệt và từ chối file khác nội dung.
Run workflow có tag tùy chọn; bỏ trống để lấy version từ source như Android/Windows/macOS.

Version hiện tại là **5.16.4** (Android 60109, iOS build 24), đã chuẩn bị cho release
mới chứa DMG macOS và IPA chưa ký. Commit/push main để Actions build và publish.

### Giữ chữ ký khi đóng gói macOS

Sao chép `.app` đã ký bằng `/usr/bin/ditto --rsrc --extattr`, không dùng
`shutil.copytree` trên macOS: chữ ký các DLL không phải Mach-O có thể nằm trong
extended attributes, không chỉ trong nội dung file. Xác minh staging trước khi tạo
DMG/PKG; DMG còn được mount chỉ đọc và sao chép ra ngoài để xác minh lại.
Không ký lại payload sau khi sao chép hoặc bỏ qua lỗi codesign.
