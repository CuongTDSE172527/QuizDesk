# QuizDesk 5.9.0 — Rà soát bảo mật (23/10/2026)

Phạm vi: mã nguồn dùng chung, nhập/mở file, lưu/khôi phục dữ liệu, Android
exported activity, metadata đóng gói và dependency NuGet Windows/Android.
Ứng dụng vẫn chạy offline; không bổ sung máy chủ hay gửi nội dung bộ đề đi.
Đây là rà soát mã và kiểm thử hồi quy có giới hạn, không phải chứng nhận hay
cam kết không còn lỗ hổng. Không gán CVE cho các phát hiện nội bộ dưới đây.

| Phát hiện | Phân loại | Thay đổi |
| --- | --- | --- |
| DOCX/XLSX/ODT chỉ giới hạn kích thước file nén, thiếu giới hạn phần XML giải nén | Nguy cơ từ chối dịch vụ khi nhập file không tin cậy | 2.048 mục/archive, 128 MiB tổng kích thước khai báo sau giải nén, 16 MiB/phần XML và stream đếm byte thực tế; từ chối part trùng tên |
| XML chưa có giới hạn độ sâu/số nút thống nhất | Hardening chống cạn tài nguyên | 64 tầng, 1 triệu nút, 128 thuộc tính/nút; cấm DTD, XmlResolver=null cho mọi reader XML |
| Regex xử lý đề không có timeout hữu hạn | Nguy cơ xử lý CPU lâu với văn bản được tạo có chủ đích | Timeout 250 ms/mỗi thao tác; kiểm thử HTML gồm 20.000 thẻ script không đóng |
| Nhánh .quiz trả về trước kiểm tra giới hạn 5.000 câu chung | Thiếu kiểm tra input | Giới hạn ngay trong loader công khai .quiz; JSON/Word/text cũng chặn khi đang đọc câu |
| Chuỗi kế thừa kiểu Word dùng đệ quy không giới hạn | Nguy cơ stack overflow/CPU khi nhập Word | Duyệt lặp, tối đa 64 kiểu; tra cứu/cache kiểu bằng dictionary |
| Chỉ số cột XLSX có thể overflow; bảng có thể tạo quá nhiều ô/dòng | Nguy cơ cạn tài nguyên/đầu vào sai | Cột ASCII có độ dài hữu hạn, số học checked, tối đa 256 cột/10.002 dòng; kiểm tra chỉ mục chuỗi, từ chối worksheet bên ngoài archive |
| Một số dữ liệu lưu hỏng/null có thể gây lỗi lúc khởi động | Độ bền khôi phục | Giới hạn file lưu và kiểm tra cấu trúc trước khi sử dụng; cô lập bản ghi lỗi, giữ file gốc và trả cảnh báo |
| Audit chưa được yêu cầu fail-closed trong cấu hình nguồn | Phòng ngừa dependency có lỗ hổng đã biết | NuGetAudit=true, Mode=all, Level=low; NU1900–NU1905 thành lỗi build; nguồn audit HTTPS tách khỏi nguồn tải package; script kiểm tra lại audit-security.ps1 |

XML: XDocument của .NET hiện đại đã có mặc định chống XXE; không coi đây là
một lỗ hổng đọc file ngoài đã chứng minh ở bản cũ. Bản này đặt policy rõ ràng
và thêm giới hạn tài nguyên. Zip parts chỉ được đọc từ archive, không giải nén
ra đường dẫn tùy ý. Mục lục ZIP được quét trước khi tạo đối tượng entry:
kiểm tra cả số mục thực tế để chặn số đếm giả, tối đa 4 MiB metadata và tên
mục 2.048 byte. Chỉ file do bộ chuyển DOC cục bộ tạo mới được chuẩn hóa part
trùng sau khi SHA256 xác nhận nội dung giống hệt; không nới policy file ngoài. UI hiển thị nội dung dưới dạng chữ, không chạy HTML/XAML
trong file đề. Android giữ tương thích Samsung My Files và kiểm tra scheme
content/file trước khi đưa URI của activity exported cho ContentResolver.

Giới hạn tương thích bổ sung: 16 triệu ký tự văn bản, tối đa 1.000 trang PDF;
nội dung câu 65.536 ký tự, giải thích 131.072 ký tự, mỗi lựa chọn 32.768 ký tự.
File quá lớn/phức tạp cần chia nhỏ hoặc lưu lại. Những giới hạn này áp dụng khi
đọc cả trực tiếp và qua luồng nhập/mở .quiz. Định dạng .quiz và JSON lưu giữ nguyên.

Dependency và runtime:
- Làm mới NuGet bằng --force --no-http-cache, audit cả dependency gián tiếp;
  các đồ thị Windows/Android không có advisory được nguồn NuGet báo tại lần chạy.
- Không có patch mới hơn trong nhánh patch hiện tại của các package Core.
  Không thay phiên bản major chỉ để làm mới số phiên bản.
- Metadata servicing chính thức Microsoft: .NET 10.0.13, SDK 10.0.402,
  latest release ngày 13/10/2026; trùng runtime đang đóng gói.
- Chạy audit-security.ps1 khi build; trên Mac có workload iOS dùng -IncludeIOS.
  Đồ thị NuGet riêng iOS và native iOS build chưa được kiểm tra trên Windows.

Kiểm thử:
- 31 kiểm tra bảo mật với fixture giả lập: file nén phình lớn, tổng archive vượt
  giới hạn, DTD/entity, XML sâu, tên part trùng, kiểu Word dài, cột XLSX overflow,
  sheet bên ngoài, CSV rộng, regex chạy lâu, file lớn và dữ liệu lưu null/hỏng.
- Hồi quy các định dạng cũ/mới và VOR/ILS/Mon15 thật; kiểm tra ôn tập thoát không
  nộp bài, giữ phản hồi từng câu và checkpoint sau đóng app. Thi thử vẫn có điểm
  và lịch sử. Báo cáo VALIDATION của gói phát hành liệt kê kết quả thực tế.

Giới hạn còn lại:
- PdfPig, b2xtranslator và RtfPipe vẫn chạy trong process. Giới hạn file/văn bản
  và timeout regex không phải sandbox bộ nhớ/CPU tuyệt đối cho parser bên thứ ba.
- Feed NuGet chỉ phản ánh advisory đã biết, không bao phủ mọi native dependency,
  mã chưa có advisory, lỗi logic chưa tìm thấy hoặc kiểm thử xâm nhập toàn diện.
- Chưa chạy bản phát hành trên điện thoại Android/Mac/iOS thật; kiểm tra UI dùng
  host Windows ở kích thước desktop và điện thoại. iOS có nguồn, chưa IPA.
- Mac chưa ký Developer ID/notarize. APK thử nghiệm tiếp tục dùng cùng khóa ký
  cục bộ để cài đè; khóa riêng không nằm trong Source.zip. Phát hành store cần
  quản lý signing/secret phù hợp; không đổi khóa âm thầm làm mất đường cập nhật.
- Bộ đề/lịch sử được lưu cục bộ dạng đọc được. Quyền của tài khoản hệ điều hành
  và sandbox thiết bị là ranh giới bảo vệ; bản này không bổ sung mã hóa có khóa.

Tài liệu đối chiếu:
- OWASP File Upload (giới hạn cả nội dung sau giải nén):
  https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html
- OWASP XML External Entity Prevention (.NET defaults và policy XML):
  https://cheatsheetseries.owasp.org/cheatsheets/XML_External_Entity_Prevention_Cheat_Sheet.html
- Microsoft NuGet Audit (dependency gián tiếp và nguồn audit):
  https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages
- Metadata .NET 10 servicing:
  https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json
