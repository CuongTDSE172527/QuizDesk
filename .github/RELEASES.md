# Build APK / bộ cài EXE và publish GitHub Releases

Hai workflow độc lập:

- `.github/workflows/android-release.yml`: APK universal ARM64/x64, ký bằng khóa gốc.
- `.github/workflows/windows-release.yml`: Windows x64 self-contained, đóng gói Inno Setup EXE.

Cả hai chạy khi push tag `vX.Y.Z`, hoặc chạy thủ công trong Actions trên nhánh/tag
được chọn. Tag nhập thủ công có thể chưa tồn tại. Push nhánh `main` thông
thường không tự phát hành. Workflow iOS vẫn hoạt động riêng như trước.

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
3. Commit/push các thay đổi version, rồi tạo tag khớp source, ví dụ `v5.13.0`:

```powershell
git tag v5.13.0
git push origin v5.13.0
```

4. Theo dõi **Actions → Android APK release** và **Windows EXE release**.
5. Khi mỗi workflow thành công, file được đưa vào cùng Release `QuizDesk X.Y.Z`:

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
2. Để trống ô `tag` để dùng version source (hiện là `v5.13.0`), hoặc nhập tag
   khớp version. Không cần tự tạo tag trước khi chạy thủ công.
3. Chạy cả **Windows EXE release** và **Android APK release** trên cùng một commit.
4. Sau khi build thành công, job publish tạo tag tại đúng commit đã build nếu
   tag chưa có. Tag đã có phải trỏ đúng commit đó (cũng hỗ trợ annotated tag).

Nếu một tag đã trỏ đến commit khác, workflow dừng, không di chuyển tag hay ghi
đè file đã phát hành. Để build lại đúng source cũ, chọn chính tag đó ở dropdown
**Use workflow from**, hoặc tăng version và phát hành tag mới. Tag được chọn
phải chứa workflow/script mới. Không dùng `v5.11.0` để phát hành source mới.

GitHub không kích hoạt workflow khác từ tag tạo bằng `GITHUB_TOKEN`, nên khi
chạy thủ công cần chạy cả hai workflow; workflow Windows không tự gọi Android.
Muốn tự chạy cả hai bằng một thao tác, vẫn có thể tạo/push tag phiên bản từ máy
cá nhân như phần trên.

Build và publish là hai job riêng. Build/test thất bại thì không upload lên
Release; platform còn lại vẫn có thể hoàn tất độc lập. Nếu chỉ publish lỗi,
chọn chạy lại job lỗi để dùng lại artifact đã build. Artifact giữ 7 ngày;
file trên Releases không theo hạn lưu artifact này.

Hai job publish dùng chung concurrency group theo tag để tránh tranh nhau tạo
Release. Chạy lại bỏ qua asset đã có nếu SHA-256 giống nhau; dừng nếu cùng tên
nhưng bytes khác hoặc không xác minh được. Không dùng `--clobber` và không xóa
Release/bộ cài cũ. Nếu cần thay đổi binary đã phát hành, tăng version và tạo tag mới.
Build lại toàn bộ có thể tạo bytes khác do thời gian đóng gói, nên tránh rebuild
để thay thế asset đã công bố.

## Kiểm tra và quyền

- Chạy 18 kiểm thử publish giả lập (không ghi GitHub), 19 kiểm thử nhập nhiều file,
  30 gộp đề, 39 media và 25 xuất định dạng/trích câu và 25 xuất Office trước khi đóng gói.
- Kiểm tra SHA-256 artifact tải xuống trước khi publish.
- Actions được pin vào commit SHA của bản chính thức.
- Build job chỉ có `contents: read`; publish job mới có `contents: write`.
- Không chạy workflow release với pull request. Chạy thủ công dùng nhánh/tag được chọn;
  tag mới chỉ được tạo trong job publish sau khi build thành công.
- Android cần GitHub Secrets trước lượt chạy đầu; Windows không cần signing secret.
- Chưa chạy build APK/EXE hoặc publish thật trên GitHub cho các workflow mới.
