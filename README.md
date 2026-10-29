# QuizDesk 5.17.1 — Windows, macOS, Android và mã nguồn iOS

Word tổng hợp nhiều môn: khi nhập DOCX (hoặc DOC được chuyển đổi), ứng dụng bỏ qua
mục lục Word và tạo một bộ đề cho mỗi tiêu đề môn/phần. Dùng Heading 1 cho tiêu đề môn
để nhận diện ổn định; các tiêu đề bắt đầu bằng Môn, Phần, Chương cũng được hỗ trợ.
Số câu được giữ riêng từng môn. Nếu có câu bổ sung trùng số trong tài liệu nhiều môn,
app giữ đủ câu, ghép đáp án trước khi đánh số lại và ghi số gốc ở câu đổi số.
Khi nhập nhiều file, chế độ gộp vẫn ghép đáp án riêng từng môn trước khi gộp.
Nếu bảng đáp án dùng số câu trùng trong cùng môn, app yêu cầu xác nhận thay vì tự gán.

[Tải bản cài / Releases](https://github.com/CuongTDSE172527/QuizDesk/releases/latest) · [Changelog](CHANGELOG.md)


## Logo, hiệu năng và chuyển động 5.8.0

Logo mới kết hợp chữ Q, thẻ học và dấu tick, dùng chung trong app và biểu tượng
Windows/macOS/Android/iOS. Android có adaptive icon và biểu tượng theo màu hệ thống
(Android 13+). Nguồn SVG và script tạo ICO/ICNS/PNG nằm trong Assets/Brand.

Chuyển trang 140 ms, hộp thoại/lật flashcards 120 ms, chuyển câu/thẻ 110 ms,
dấu chọn đáp án 90 ms. Hiệu ứng chỉ thay đổi hiển thị; nội dung và tiến độ lưu
ngay khi thao tác. Cài đặt → Giảm chuyển động tắt hoạt ảnh và được nhớ lại.

Bài thi dùng tối đa 50 nút điều hướng tái sử dụng, có chuyển nhóm và chuyển
thẳng đến câu. Giữ nguyên control câu hỏi/đáp án khi chuyển, bỏ các lần ghi
checkpoint trùng nội dung và dùng lại hình học icon. Không dựng lại trang
biên soạn khi lưu câu hỏi. Không thay đổi định dạng .quiz hay dữ liệu đã lưu.

Đo trên Windows với 1.000 câu: mở bài và layout 556 → 194 ms; cấp phát bộ nhớ
55,3 → 10,3 MiB; 100 lần chuyển/chọn có lưu tiến độ 589 → 501 ms. Đây là phép
đo trên máy phát triển, chưa phải số đo trên điện thoại hoặc Mac thực tế.

## Media trong bộ đề — 5.11.0

- DOC/DOCX: lấy media nhúng từ DrawingML/VML theo đoạn chứa câu hỏi, lựa chọn
  hoặc giải thích; nhận cả đoạn chỉ chứa ảnh. Liên kết ngoài không được tải,
  đối tượng OLE được đánh dấu để thêm file gốc thủ công.
- Sửa câu hỏi → chọn Vị trí đính kèm → Thêm ảnh / GIF / video. Có thể xóa từng
  đính kèm; ảnh cho lựa chọn trống sẽ tạo nhãn (Xem media). Chưa đổi trực tiếp
  vị trí đính kèm: xóa và thêm lại ở vị trí mới. PNG/JPEG/WebP/GIF ≤8 MB/file,
  MP4/WebM ≤16 MB/file; tổng media ≤24 MB/bộ, ≤16 media/câu, ≤1000 media/bộ.
- Ảnh hiển thị inline; GIF phát/dừng theo ý người học. Video mở màn hình native
  trên Android/iOS, ứng dụng mặc định trên Windows/macOS; không tự phát và
  đồng hồ thi tiếp tục chạy. Khả năng giải mã phụ thuộc thiết bị/codec; MP4
  H.264/AAC là lựa chọn thuận tiện. Chưa có nhúng YouTube hay tải URL video.
- PDF/Excel/CSV/RTF/HTML/ODT vẫn nhập phần chữ; thêm media bằng màn hình Sửa.
  JSON có thể thêm media: [{name,mime,target,data}], data là Base64, target là
  question/note/A–H. File JSON vẫn chịu giới hạn nội dung chữ hiện có.
- .quiz có media dùng ZIP v2 (quiz.xml, media.json, media/<sha256>.<ext>), cần
  QuizDesk 5.11.0+ ở người nhận. Text-only vẫn xuất XML v1; file cũ vẫn đọc được.
  Không cần server; media được lưu cùng thư viện, bài đang làm, flashcards và
  lịch sử. Gộp/trộn câu và trộn lựa chọn giữ media theo đúng nội dung.
- Giải thích/media ở note chỉ hiện sau chọn đáp án trong ôn tập hoặc mặt sau
  flashcards. Lịch sử có snapshot riêng, không phụ thuộc file/bộ nguồn còn tồn tại.
- Giới hạn giải mã ảnh: cạnh ≤8192, ≤12 triệu pixel/ảnh; tối đa 12 triệu pixel
  đang mở trên mọi gallery (hai bộ đệm native), GIF ≤500 frame. Đóng câu khác
  hoặc giảm kích thước nếu gặp thông báo giới hạn. Rời màn hình giải phóng bộ đệm.
- File mẫu Khí tượng của người dùng: 53 câu (34 đánh số tay +19 đánh số tự động),
  ảnh bản đồ ở câu19; câu33 có tiêu đề tách đoạn vẫn được giữ đúng.

## Gộp và trộn bộ đề 5.10.0

Vào Bộ đề → Chọn bộ đề → đánh dấu ít nhất hai bộ → Gộp đã chọn.
Đặt tên, bật/tắt Trộn thứ tự câu hỏi và chọn Tạo bộ đề gộp. Bộ mới lấy toàn bộ
câu từ các bộ đã chọn, đánh số 1..N, giữ đáp án/giải thích/cảnh báo theo từng câu.
Các bộ gốc giữ nguyên; có thể biên soạn, xuất .quiz, học flashcards hoặc thi thử
trên bộ mới. Các câu có nội dung giống nhau được giữ đủ, không tự gộp/xóa trùng.

Nhật ký của bộ mới ghi tên nguồn, số câu gốc → số mới, ngày giờ tạo và cách
trộn. Bản gộp độc lập; sửa/xóa bộ nguồn về sau không sửa bản gộp hay lịch sử thi.
Chỉ gộp các bộ có câu hỏi, tối đa 5.000 câu và giới hạn tổng nội dung an toàn.
Nút gộp hiện tổng số câu; vẫn có thể xóa những bộ đã chọn bằng nút riêng.

## Nhận dạng và flashcards 5.9.1

Nhận highlight nhiều màu Word và run/paragraph shading, kể cả style kế thừa.
Một lựa chọn được đánh dấu riêng là gợi ý đáp án, có thông tin nguồn để rà lại;
nhiều lựa chọn được tô không tự đoán. Đáp án bằng chữ/bảng/rà soát vẫn ưu tiên.
Bổ sung Đáp án là..., Câu trả lời đúng..., nhãn B —/B: và câu có ngoặc kép;
đáp án xuống dòng không trộn vào lựa chọn. Không bỏ dấu/số/đơn vị/phủ định,
không so khớp gần đúng để giải quyết nhãn/nội dung mâu thuẫn.

Bỏ nút Lật thẻ dư; chạm thẻ hoặc Space vẫn lật, trái/phải vẫn chuyển.
Mon-15-dap-an-goc-va-ra-soat.docx: câu 27 lấy A từ Sau rà soát dù đáp án gốc
viết quy định và lựa chọn A viết quy trình. Vẫn có 25 câu có khóa dùng được,
5 câu 10/11/16/26/30 chưa chốt. Bộ đề đã lưu không tự phân tích lại: nhập lại
file gốc hoặc bản Mon-15-da-nhan-dien.quiz mới để lấy kết quả nhận dạng hiện tại.
Không tự sửa bộ đề đã biên soạn, bản chụp lịch sử hay lượt flashcards đang lưu.

## Bảo mật và ôn tập 5.9.0

Ôn tập có nút Thoát ôn tập; không Nộp bài, không bảng điểm hay lịch sử thi.
Quay lại cũng thoát trực tiếp. Đóng app khi đang ôn vẫn khôi phục câu/đáp án;
nhấn Thoát ôn tập kết thúc lượt đó. Thi thử giữ nguyên nộp bài, điểm và lịch sử.

Nhập DOCX/XLSX/ODT có giới hạn dữ liệu giải nén, XML/nút/độ sâu, từ chối DTD
và part trùng tên. .quiz luôn giới hạn 5.000 câu; giới hạn trường câu hỏi,
bảng tính/CSV và file lưu. Regex xử lý đề có timeout 250 ms mỗi thao tác.
Chuỗi kiểu Word không đệ quy; dữ liệu lưu bị hỏng được cảnh báo và giữ nguyên.
Android chỉ nhận địa chỉ content/file cho luồng mở file từ ứng dụng khác.
Định dạng dữ liệu không đổi; cài đè giữ bộ đề, cài đặt, lịch sử và flashcards.

Directory.Build.props bật NuGet Audit cho cả dependency gián tiếp và dừng build
khi nguồn audit không truy cập được hoặc có cảnh báo CVE. Chạy audit-security.ps1
để kiểm tra mới; trên Mac thêm -IncludeIOS cho đồ thị gói iOS.
Đã kiểm tra runtime .NET 10.0.13 là bản servicing mới nhất từ metadata Microsoft
ngày 23/10/2026; audit NuGet Windows/Android không báo lỗ hổng đã biết.
Đây không phải chứng nhận bảo mật toàn diện; parser bên thứ ba vẫn chạy trong
process ứng dụng. Xem SECURITY.md để biết phạm vi và giới hạn cụ thể.

5.6.0: nhận đáp án dạng nhãn kèm nội dung (B. Nội dung) hoặc chỉ có nội dung.
Đối chiếu toàn bộ nội dung, chuẩn hóa Unicode/khoảng trắng/hoa thường và dấu
chấm cuối câu. Không bỏ dấu tiếng Việt, đơn vị, số hay dấu so sánh; không đoán
bằng độ tương tự. Nhãn mâu thuẫn nội dung, nội dung không khớp hoặc nhiều
lựa chọn cùng khớp sẽ được giữ nguyên và đánh dấu để sửa. Đáp án không được
coi là giải thích. Đã kiểm thử đủ 35 đáp án trong ILS.docx.

Giữ bản sửa mở file từ 5.5.1, đã được người dùng xác nhận thành công qua
Samsung My Files. Bộ đề nhập trước đây không tự thay đổi; hãy nhập lại file
hoặc sửa đáp án trong bộ cũ. Cài đè bản mới để giữ dữ liệu hiện có.

## Sửa vị trí biên soạn 5.7.1

Lưu câu hỏi cập nhật đúng dòng trong danh sách, không dựng lại trang Bộ đề.
Giữ ô tìm kiếm, bộ lọc, vị trí cuộn ngoài/trong danh sách và câu đang sửa.
Nếu câu đã sửa không còn khớp bộ lọc, giữ bộ lọc và chuyển sang câu kế bên.
Cập nhật số câu, cảnh báo, mốc lưu và lựa chọn ngay tại chỗ; hủy sửa giữ nguyên.

## Flashcards và đáp án rà soát 5.7

Nhận Đáp án gốc:/Sau rà soát: trong DOCX và văn bản. Ưu tiên kết quả rà soát,
giữ nguyên đáp án gốc và giải thích. CHƯA CHỐT/đề lỗi không dùng lại đáp án cũ
để chấm. Dừng đọc phần Nguồn đối chiếu, không gắn tài liệu tham khảo vào câu cuối.

Vào Học & thi → Học flashcards hoặc mở bộ đề → Flashcards. Thẻ lấy từ các câu
đã chọn có đáp án dùng được; câu thiếu/mơ hồ vẫn ở bộ đề để sửa. Mặt trước là
câu hỏi; mặt sau là đáp án, giải thích và nguồn. Có gợi ý lựa chọn gốc, lật,
Đã nhớ/Cần học lại, chuyển trước/sau, ôn thẻ chưa nhớ và học lại toàn bộ.
Space lật, trái/phải chuyển thẻ. Lưu & thoát giữ tiến độ; mở lại app khôi phục
lượt đang học. Chỉ giữ một lượt flashcards gần nhất, bằng bản chụp câu hỏi lúc
bắt đầu; bắt đầu lượt mới để lấy các chỉnh sửa mới của bộ đề. Không tính điểm
thi và không ghi lần học vào Lịch sử thi thử. Mọi thứ hoạt động offline.

## Giao diện QuizDesk 5.6

Giữ layout, điều hướng và luồng làm bài; dùng theme QuizDesk riêng cho tất cả
nền tảng. Nền trung tính, xanh chàm, thẻ có viền mảnh, nút bo vừa phải và chữ
phân cấp rõ. Có giao diện sáng/tối theo settings hiện có, màu cảnh báo và phản
hồi đúng/sai kèm ký hiệu. Đáp án đang chọn có dấu chọn và viền; bài thi thử
không lộ đúng/sai trước khi nộp. Chọn/chuyển câu giữ nguyên các control đang có.
Hộp biên soạn, settings, thư viện, lịch sử và preview nhập file cùng theme.
Không di chuyển tab, đổi định dạng dữ liệu hay thay đổi thư viện đã lưu.

## Mở file .quiz từ ngoài ứng dụng

Windows: cài bản Setup, nhấp đúp `.quiz`; nếu hệ điều hành hỏi, chọn QuizDesk
trong **Open with / Mở bằng** và chọn dùng làm mặc định. Bộ cài đăng ký loại
file vào tài khoản hiện tại, không thay đổi lựa chọn mặc định đã có của Windows.
macOS: bộ cài đăng ký loại file `com.quizdesk.quiz`, mở qua Finder / Open With.
Android: chạm file trong Files/Downloads hoặc **Chia sẻ → QuizDesk**. Hỗ trợ
content URI, file URI, MIME riêng và các MIME XML/octet-stream/text phổ biến.
Ứng dụng nguồn phải cấp quyền đọc; nếu không có lựa chọn mở, lưu về Files trước.
iOS: phần mã nguồn đăng ký `.quiz` và nhận file qua cơ chế scene/file activation
của Avalonia 12; dùng **Mở bằng / Chia sẻ → QuizDesk** trong Files khi đã cài app.
Chưa có IPA đã build/chạy thử trên iPhone; kiểm tra biên dịch UIKit trên Windows
không thay thế build native/AOT, ký và chạy trên Mac/iPhone.

Khi nhận file, app hiện tên bộ đề, số câu, giải thích, câu cần kiểm tra và cấu
hình bài thi. **Thêm vào thư viện** tạo một bộ riêng; **Để sau** giữ file chờ;
**Bỏ qua** xóa bản nhận tạm, không xóa file gốc. Các bộ hiện có không bị ghi đè.
Nếu trùng tên file đã nhập, hộp xem trước báo rõ sẽ tạo bản riêng.

File được sao chép ngay khi hệ điều hành cho đọc. Khi đang thi hoặc đang mở
hộp thoại, file chờ trong dữ liệu riêng; không thay thế bài đang làm. Sau khi
nộp sẽ hiện xem trước, hoặc chọn **File nhận đang chờ** trong Bộ đề. Hàng đợi
giữ qua khởi động lại; tối đa 10 file chờ, 50 MB/file và 5000 câu/bộ.
Desktop giữ một cửa sổ cho mỗi thư mục dữ liệu: mở file tiếp theo gửi vào cửa
sổ đang có. Đóng đột ngột sau khi lưu bộ đề không tạo bản nhập trùng khi thử lại.
File `.quiz` không được chứa DTD/external entities.

Không cần server, tài khoản hay kết nối Internet để chia sẻ/nhận file.

## Nhập đề có lỗi đáp án hoặc chưa có đáp án

Đề chưa có đáp án vẫn nhập được: bỏ dòng `Đáp án`, hoặc để trống/bỏ cột
`answer` trong JSON/CSV/Excel. Mở bộ đề rồi chọn **Sửa** để thêm đáp án và
giải thích. Có thể lưu câu chưa có đáp án để tiếp tục biên soạn sau.

Câu có nhãn lựa chọn lặp, lựa chọn trùng nội dung hoặc nhiều dòng đáp án
được giữ lại và tô vàng (cả giao diện sáng/tối). Nhãn bị lặp được chuyển sang
nhãn còn trống để giữ đủ nội dung, kèm thông báo nhãn gốc/nhãn mới. Không tự
quyết định đáp án đúng khi nguồn có mâu thuẫn. Số câu trùng hoặc quá 8 lựa chọn
vẫn cần chuẩn hóa trước khi nhập.

Dùng **Chỉ hiện câu cần sửa / bổ sung đáp án** để rà soát. Trong hộp sửa,
kiểm tra nhãn/nội dung rồi chọn đáp án và nhấn **Lưu câu hỏi** để xác nhận.
Lựa chọn còn trùng nội dung vẫn được đánh dấu sau khi lưu. Cảnh báo được tự
lưu cùng bộ đề, giữ qua khởi động lại và xuất/nhập `.quiz`; việc giải quyết
cảnh báo được ghi vào nhật ký thay đổi có ngày giờ.

Các câu chưa có đáp án hoặc còn lỗi trùng/mơ hồ vẫn được đưa vào bài luyện
nhưng chưa tính điểm, không bị đánh dấu sai. Sau khi sửa, các lần làm bài mới
sử dụng đáp án đã xác nhận; lịch sử lần thi cũ giữ nguyên. Nhiều câu khác nhau
có cùng chữ cái đáp án (ví dụ đều là B) hoàn toàn hợp lệ.

Mẫu `Assets/Samples/Review-chua-co-dap-an.txt` và `Review-trung-dap-an.txt`
minh họa hai trường hợp. 35 kiểm tra nhập/rà soát, 74 kiểm tra định dạng,
31 kiểm tra thư viện và 17 kiểm tra vòng đời mobile đã chạy thành công.

## Đồng bộ tính năng và giao diện

`QuizDesk.Desktop`, `QuizDesk.Android` và `QuizDesk.iOS` dùng chung toàn bộ
`QuizDesk.Mobile` (giao diện Material 3) và `QuizDesk.Core` (xử lý/lưu dữ liệu).
PC có navigation rail ở cửa sổ từ 840dp; khi thu hẹp chuyển sang thanh dưới
giống điện thoại. Các màn hình thư viện, biên soạn, nhập/xuất, thi/ôn tập,
cài đặt và lịch sử đều dùng cùng mã, không còn hai nhánh chức năng riêng.

Đây là đồng bộ tính năng/UI giữa các bản build. Mỗi thiết bị vẫn lưu dữ liệu
cục bộ; dùng Xuất .quiz và nhập file để chuyển bộ đề giữa máy.

Windows/macOS giữ đúng thư mục dữ liệu của desktop 4.1 nên Settings và Lịch sử
thi cũ vẫn được đọc: `%LOCALAPPDATA%/QuizDesk` trên Windows và
`~/Library/Application Support/QuizDesk` trên Mac. Bộ đề đang làm ở desktop cũ
chưa được xuất file không thể khôi phục nếu phiên bản đó không lưu bản nháp.

## Chọn xóa bộ đề

Trong Bộ đề, chọn **Chọn để xóa**, đánh dấu một/nhiều bộ hoặc chọn tất cả,
rồi bấm **Xóa đã chọn**. Hộp xác nhận nêu số bộ và tên các bộ được chọn.
Hủy không thay đổi dữ liệu. Xóa chỉ tác động các bộ đã chọn; những bộ khác
và lịch sử thi thử (bản chụp câu hỏi/đáp án) vẫn giữ nguyên.

Nội dung và nhật ký biên soạn trước/sau của bộ bị xóa được loại khỏi file.
Chỉ giữ metadata tên bộ, file nguồn và thời điểm nhập/tạo/xóa để tra cứu trong
Lịch sử nhập và xóa. Không có khôi phục câu hỏi từ dấu mốc này. Bộ đang chọn
nếu bị xóa sẽ được gỡ khỏi bản nháp, kể cả khi app bị đóng giữa lúc cập nhật;
autosave không thể làm bộ đề đã xóa xuất hiện lại. Nếu một lần ghi xóa lỗi,
file trước đó vẫn còn và UI báo bộ chưa xóa được.

## Build desktop

```powershell
./build-desktop.ps1 -Runtime win-x64
./build-desktop.ps1 -Runtime osx-arm64
./build-desktop.ps1 -Runtime osx-x64
```

Windows có bộ cài cho x64; macOS có Apple Silicon và Intel, kèm runtime.
Bộ cài Mac được xây trên Windows và kiểm tra payload; chưa chạy trên Mac,
chưa có Developer ID/notarization. Android giữ application ID và khóa ký cũ.
Các bước build Android/iOS ở phần dưới vẫn áp dụng.

## Lưu nhiều bộ đề

Mỗi lần nhập file tạo một bộ đề độc lập, kể cả hai file trùng tên. Mục Bộ đề
hiển thị danh sách đã thu gọn; chỉ khi nhấn tên bộ đề mới mở phần biên soạn.
Nhấn lại hoặc chọn Thu gọn để đóng. Mỗi lần chỉ mở một bộ đề để chỉnh sửa.
Bộ đề được chọn dùng cho màn hình Làm bài; có thể đổi tên ngay trong trình soạn.

Nội dung, lựa chọn câu và cấu hình được tự lưu riêng cho từng bộ đề. Nút
Xuất .quiz tạo file để sao lưu hoặc chuyển máy. App lưu nội dung đã đọc từ
tài liệu; không sửa file Word/PDF/Excel nguồn.

Mỗi thẻ có thời điểm nhập/tạo và sửa gần nhất, định dạng
`dd/MM/yyyy · HH:mm:ss` theo múi giờ thiết bị. Nút Lịch sử nhập hiển thị tên
file và thời điểm nhập; Nhật ký thay đổi trên từng bộ đề có ngày giờ, hành động
và chi tiết trước/sau của nội dung câu, lựa chọn, đáp án, giải thích, đổi tên,
xóa/thêm câu, câu được chọn và cấu hình thi. Tự lưu, mở bộ đề và xuất file không
tạo bản ghi thay đổi giả. Lịch sử thi thử vẫn giữ nguyên bản chụp từng lần làm.

Dữ liệu mỗi bộ đề và nhật ký nằm cùng một JSON trong `Library/`, được thay
thế nguyên tử. File lỗi được giữ lại và báo cảnh báo, không ghi đè khi đọc.
Bản nháp đơn ở phiên bản cũ được chuyển vào thư viện một lần, giữ nội dung và
cấu hình. Vì bản cũ không lưu thời điểm nhập, mục chuyển đổi chỉ ghi thời điểm
chuyển dữ liệu, không suy đoán ngày nhập ban đầu. Các bộ đề từng bị thay thế
trước khi có thư viện không thể khôi phục nếu không còn file riêng.

Đã qua 31 kiểm tra thư viện và preview UI: nhập trùng tên, mở/thu gọn, sửa/đổi
tên, nhật ký trước/sau, lịch sử nhập và giữ nguyên thư viện sau tiến trình mới.

## Giao diện mobile

- Hệ màu Material 3 theo các vai trò surface/container/primary/outline/error,
  đầy đủ sáng, tối và tùy chọn theo hệ thống đã lưu trong Settings.
- Thanh app bar 64dp, nút biểu tượng có nhãn accessibility, thanh điều hướng
  80dp với biểu tượng và indicator dạng viên thuốc cho trang đang chọn.
- Nút filled/tonal/outlined/text, vùng chạm tối thiểu 48dp; thẻ 16dp,
  khối nổi bật và dialog 28dp; ô nhập filled có đường nhấn dưới.
- Checkbox, switch, lựa chọn đáp án, trạng thái đúng/sai và thanh tiến độ
  được thiết kế cùng bảng màu. Đúng/sai vẫn có nhãn chữ và giải thích.
- Thu gọn công cụ Bộ đề, giữ tìm kiếm và danh sách ảo hóa; xác nhận xóa,
  chỉnh sửa câu hỏi, lưu bài đang làm và Lịch sử giữ nguyên hành vi.

Triển khai bằng Avalonia dùng chung trên Android/iOS. `Material3.axaml` chứa
token và template; `MaterialView.cs` chứa biểu tượng vector và thành phần
điều hướng. Dùng typography Inter hiện có với vai trò headline/title/body/label.
Tham chiếu: https://m3.material.io/styles/color/roles và
https://m3.material.io/styles/typography/applying-type.

Đã kiểm tra UI thực trong host PC ở 360/410/700dp: sáng/tối, tương phản nút
primary, điều hướng, soạn câu, lựa chọn đúng và khôi phục bài làm qua tiến trình.
Đây là preview của cùng mã giao diện; chưa xác nhận runtime Android/iOS thật.

## Android 5.0.1: sửa crash lúc mở app

Log từ SM-A576B / Android 17 báo `UnsatisfiedLinkError` tại
`AvaloniaAndroidApplication_1.n_onCreate()`, trước khi giao diện được tạo.
Tắt `AndroidEnableMarshalMethods` để dùng đăng ký JNI động; bản Release
đăng ký lại callback OnCreate của lớp Application kế thừa Avalonia.
Không thay đổi bộ đề, Settings, Lịch sử hay định dạng dữ liệu.
APK 5.0.1 dùng cùng application ID và khóa ký với 5.0.0 để cập nhật tại chỗ.
Đã kiểm tra callback trong mã Java sinh ra và DEX đóng gói, chữ ký và alignment.
Chưa xác nhận chạy bản sửa trên điện thoại Android 15/17; cần kiểm tra thiết bị.
Bản 5.2.0 giữ cấu hình khắc phục này; cùng package và khóa ký để cập nhật tại chỗ.

Bản mobile dùng C#/.NET 10 + Avalonia 12.1.4, có giao diện dành cho điện thoại.
Android và iOS dùng chung `QuizDesk.Mobile` và `QuizDesk.Core`.

## Tính năng

- Nhập `.quiz`, DOC/DOCX, XLS/XLSX, PDF có chữ, CSV/TSV, JSON, TXT/MD,
  RTF, HTML và ODT qua trình chọn file của hệ điều hành.
- Không cần đường dẫn thật: file dạng Android content URI/iOS Files được đọc
  qua stream vào cache, kiểm tra giới hạn 50 MB trước khi phân tích.
- Chọn câu, thêm/sửa/xóa câu, xác nhận xóa tất cả, xuất `.quiz`.
- Ôn tập, thi thử, giải thích, điểm và Lịch sử với bộ lọc đúng/sai/bỏ trống.
- Settings sáng/tối/theo hệ thống; số câu, thời gian và trộn được ghi nhớ.
- Thư viện tự lưu từng bộ đề trong dữ liệu riêng của app. Bấm Xuất .quiz để xuất file.
- Bài đang làm được checkpoint sau mỗi lựa chọn/chuyển câu. Khi app bị tạo lại,
  khôi phục câu, đáp án và hạn giờ gốc. Hết giờ khi ở nền thì nộp khi app mở lại.
- Lịch sử dùng cùng ID lần thi để tránh ghi trùng khi khởi động lại sau khi nộp.
- Danh sách bộ đề tạo control theo vùng nhìn thấy, không tạo 5.000 hàng cùng lúc.

File `.quiz` tương thích bản PC. Mỗi thiết bị giữ Settings/Lịch sử riêng; không
có đồng bộ đám mây hoặc tài khoản. App không yêu cầu quyền Internet hay quyền
đọc toàn bộ bộ nhớ. Hệ thống Files có thể tự tải file cloud do bạn chọn.

## Android APK

`QuizDesk-5.6.0-Android.apk` là bản cài thử dành cho Android 8.0/API 26 trở lên,
CPU arm64-v8a hoặc x86_64. Không bao gồm ARM 32-bit. Target API 36.
Đã kèm runtime, không cần .NET/Office trên điện thoại.

Chép/tải APK lên điện thoại, mở bằng Files và cho phép cài từ nguồn đó nếu
Android hỏi. Bản này dùng khóa phát triển tự ký, không phải bản Google Play.
Giữ app để giữ bản nháp/Settings/Lịch sử; gỡ app sẽ xóa dữ liệu riêng theo cơ chế
Android. Xuất bộ đề `.quiz` trước khi gỡ nếu cần giữ câu hỏi.

APK đã được build Release, kiểm tra chữ ký APK v2/v3, 16 KB zip alignment,
manifest và ABI. **Chưa cài/chạy thành công trên thiết bị Android hoặc emulator**:
emulator không khởi động được trong môi trường Windows bị giới hạn hiện tại.
Kiểm tra giao diện bên dưới chạy cùng view mobile trong cửa sổ preview trên PC.
Không coi bản này là một bản mobile đã nghiệm thu trên điện thoại.

Khóa dùng ký APK bàn giao được giữ ở workspace gốc, không nằm trong ZIP mã nguồn.
`build-android.ps1` tự tạo một khóa phát triển mới nếu thiếu; APK ký bằng khóa mới
không thể cập nhật đè APK cũ. Muốn giữ dữ liệu qua cập nhật, phải giữ cùng khóa.
Chọn khóa phân phối riêng trước khi đưa lên store.

## iOS: Simulator và IPA cài thử

`QuizDesk.iOS` có entry point, AppDelegate, manifest iPhone/iPad, launch screen,
app icon, Files import và share sheet để xuất file. Mục tiêu iOS 15+.
Bản Simulator đã build thành công trên GitHub Actions. Chưa xác nhận build Release
cho iPhone hoặc chạy trên iPhone thật; AOT/trimming và sideload cần được kiểm tra.

Trên Mac, cài Xcode tương thích với workload .NET iOS, chọn Xcode bằng
`xcode-select`, hoàn thành thiết lập Xcode và cài .NET 10 SDK. Sau đó:

```sh
dotnet workload install ios
sh build-ios.sh simulator
```

Script tự chọn simulator ARM64 cho Apple Silicon hoặc x64 cho Mac Intel.
Mở Simulator, rồi cài `.app` trong thư mục build bằng:

```sh
xcrun simctl install booted path/to/QuizDesk.iOS.app
xcrun simctl launch booted com.quizdesk.mobile
```

Để build IPA đã ký cho iPhone, cần signing identity và provisioning profile phù hợp:

```sh
export IOS_CODESIGN_KEY='Apple Development: Your Name (...)'
export IOS_PROVISIONING_PROFILE='Your Profile Name or UUID'
sh build-ios.sh device
```

Chứng chỉ/profile phải khớp `com.quizdesk.mobile`, hoặc đổi ApplicationId trước
khi tạo profile. Việc đưa lên TestFlight/App Store nằm ngoài bản bàn giao này.
Script không đăng nhập Apple hay tải app lên store.

### GitHub Actions: IPA chưa ký cho Apple ID thường

Workflow `.github/workflows/ios.yml` có hai job độc lập: Simulator và iPhone.
Push thay đổi lên `main`, hoặc vào **Actions → iOS build (Simulator and iPhone IPA)
→ Run workflow**. Chờ job **Build unsigned IPA for iPhone** thành công.
Trong **Artifacts**, tải `QuizDesk-iPhone-unsigned-IPA-<số run>`, giải nén để lấy
`QuizDesk-ios-arm64-unsigned.ipa`. Artifact Simulator chứa `.app`, không cài lên iPhone.
Log build iPhone nằm trong artifact `QuizDesk-iPhone-build-log-<số run>` kể cả khi build lỗi.
Artifact được giữ 7 ngày; đây là thời gian lưu trên GitHub, khác hạn ký Apple.

Sau push `main` hoặc tag version mới, job publish đưa thêm
`QuizDesk-<version>-ios-arm64-unsigned.ipa` và SHA-256 lên GitHub Releases.
Giữ nguyên tag/file đã phát hành: commit mới cùng version chỉ có Artifacts,
cần tăng version đồng bộ để tạo release mới. Pull request chỉ build.
IPA trên Releases vẫn chưa ký, cần ký lại để cài lên iPhone.

Có thể tạo cùng IPA trên Mac bằng `sh build-ios.sh unsigned`.
Script build Release `ios-arm64`, không ký Apple, kiểm tra kiến trúc/platform của
binary rồi đóng gói `Payload/QuizDesk.iOS.app` vào `dist/ios/*.ipa`, kèm SHA-256.
Không dùng Apple ID, chứng chỉ hay provisioning profile trong GitHub Actions.
IPA này cần ký lại trước khi cài; không thể cài trực tiếp từ Files hay Safari.

Để thử bằng Apple ID thường trên Windows:

1. Cài AltServer và AltStore Classic theo hướng dẫn chính thức:
   https://faq.altstore.io/altstore-classic/how-to-install-altstore-windows
2. Kết nối iPhone, tin cậy máy tính và cài AltStore. Nhập Apple ID trực tiếp vào
   công cụ cài trên máy cá nhân, không lưu trong source hoặc GitHub Secrets.
3. Chuyển IPA sang iPhone, mở AltStore → My Apps → dấu `+` → chọn IPA.
4. Bật Developer Mode nếu iOS yêu cầu, hoàn tất bước tin cậy rồi chạy QuizDesk.
5. Chữ ký miễn phí có hạn 7 ngày; refresh qua AltServer trước khi hết hạn.

Luồng này phục vụ cài thử cá nhân, không phải phát hành qua TestFlight/App Store.
Chưa kiểm chứng cài/chạy QuizDesk qua AltStore trên iPhone thật.

## Chạy source và kiểm tra

`QuizDesk.Mobile.slnx` chỉ chứa core/view/preview/tests, nên build được khi chưa
cài workload native. Build riêng Android hoặc iOS qua project tương ứng.

```sh
dotnet build QuizDesk.Mobile.slnx -c Release -m:1
dotnet run --project QuizDesk.Preview -c Release
dotnet run --project QuizDesk.Mobile.Tests -c Release -- TestAssets/Samples
dotnet run --project QuizDesk.Mobile.Tests -c Release -- --settings ./test-settings
dotnet run --project QuizDesk.Mobile.Tests -c Release -- --history ./test-history
dotnet run --project QuizDesk.Mobile.Tests -c Release -- --mobile ./test-mobile
dotnet run --project QuizDesk.Mobile.Tests -c Release -- --library ./test-library
```

Preview là cửa sổ desktop chứa đúng view mobile, không phải giả lập Android/iOS.

Build Android trên Windows sau khi có .NET 10 SDK, workload `android`, Android SDK
API 36/build tools 36 và JDK phù hợp với workload (bản bàn giao dùng JDK 21):

```powershell
dotnet workload install android
./build-android.ps1 -AndroidSdkDirectory 'C:/Android/sdk' -JavaSdkDirectory 'C:/Java/jdk'
```

`NuGet.Config` dùng nuget.org. Source ZIP dùng cache `.packages` nằm trong source.
Không chứa SDK, JDK, keystore, chứng chỉ Apple hay dữ liệu người dùng.

## Phạm vi kiểm tra và việc cần kiểm tra trên máy thật

Đã qua 74 kiểm tra import/core, 10 Settings, 14 History và 17 mobile workspace/
stream/checkpoint. UI preview đã qua nhập mẫu, sửa/xóa, xác nhận, lưu nháp,
100 lượt chọn/chuyển câu không dựng lại control, 5.000 câu với virtualization,
nộp bài, Lịch sử, lọc và khôi phục trong tiến trình mới.

Trước khi phân phối cần kiểm tra trên Android/iPhone thật: document picker,
xuất file, bàn phím và safe area, xoay màn hình, nền/khóa màn hình, OS kill,
timer khi mở lại, nhập DOC/XLS/PDF/RTF và cập nhật giữ dữ liệu. iOS còn cần
kiểm tra AOT/trimming và signing bằng Xcode/workload tương thích.

Giới hạn nhập đề giữ như PC: PDF ảnh cần OCR ngoài; không có ảnh/công thức toán,
câu nhiều đáp án đúng, file mật khẩu hoặc Word trước 97. Không tự sinh lời giải.

Tài liệu nền tảng: https://docs.avaloniaui.net/docs/app-development/cross-platform-solution-setup
Android: https://docs.avaloniaui.net/docs/deployment/android
iOS: https://docs.avaloniaui.net/docs/deployment/ios

## Nhập nhiều file cùng lúc

Chọn **Nhập file đề**, chọn nhiều file trong trình chọn file của hệ điều
hành, rồi chọn **Tách riêng** (mỗi file một bộ đề) hoặc **Gộp tất cả**. Khi gộp,
đặt tên bộ tổng hợp và tùy chọn trộn thứ tự câu hỏi. Nhập một file vẫn hoạt động
như trước. Tính năng dùng chung cho giao diện PC và mobile; cách chọn nhiều
file phụ thuộc trình quản lý file của hệ điều hành.

Mỗi file được phân tích độc lập, ghép câu với bảng đáp án trong chính file đó.
Sau đó bộ tổng hợp mới được đánh số liên tục từ 1, nên Câu 1 ở hai file không
bị coi là cùng câu. Nhật ký bộ đề giữ tên file, thứ tự file và số câu ban đầu.
Đáp án, giải thích, media và trạng thái cần sửa đi theo câu khi trộn. Không tự
loại câu có nội dung giống nhau. Giới hạn bộ tổng hợp: 5.000 câu và ngân sách
văn bản/media hiện có của app.

Kết quả nhập hiển thị riêng từng file. Nếu tách, các file hợp lệ được lưu ngay,
file lỗi được báo để sửa/nhập lại. Nếu gộp, một file lỗi sẽ dừng lượt nhập và
không tạo bộ tổng hợp thiếu câu. App không tự ghép file chỉ có đáp án sang một
file đề khác; mỗi file được chọn cần là một bộ đề độc lập.

## Build và phát hành tự động

Có hai GitHub Actions riêng để build APK Android và bộ cài EXE Windows, rồi
đính kèm vào cùng GitHub Release theo tag phiên bản. Xem [cách cấu hình khóa ký
và chạy workflow](.github/RELEASES.md).

## Xuất nhiều định dạng và trích đề thi thử

Trong bộ đề đang mở, chọn **Xuất bộ đề** → `.quiz`, DOCX, XLSX, JSON, CSV, TSV, TXT hoặc HTML.
`.quiz` giữ cấu hình thi và media đầy đủ. JSON giữ câu hỏi, đáp án, giải thích,
media và thông tin rà soát; HTML nhúng media để xem/in offline. CSV/TSV/TXT chỉ
xuất chữ. Với định dạng ngoài `.quiz`, có thể bỏ đáp án và giải thích. CSV/TSV
thêm dấu nháy đơn trước ô có nguy cơ bị Excel hiểu thành công thức; nhập lại
các ô này sẽ giữ dấu nháy đơn. Các định dạng ngoài `.quiz` không giữ cấu hình thi.

Trong **Bộ đề**, chọn các bộ nguồn → **Trích câu / gộp**, nhập số câu lấy ở từng
bộ (0 để bỏ qua), bật trộn thứ tự rồi tạo. Ví dụ 10 câu từ bộ A và 15 câu từ bộ B
sẽ tạo đề 25 câu. Mỗi bộ được bốc ngẫu nhiên không lặp, sau đó mới trộn chung và
đánh số lại. Đáp án/giải thích/media giữ theo câu; nhật ký giữ số câu và bộ nguồn.
Mặc định mở màn thiết lập thi thử và dùng đủ số câu vừa trích. Đây là một bộ đề
mới lưu trong thư viện; tạo lại bộ tổng hợp để bốc lượt câu khác. Các lần thi
trên cùng bộ đã tạo chỉ trộn lại chính các câu đó.

Đã bỏ giới hạn 20 file mỗi lượt. Nhập tách xử lý từng file lần lượt; nhập gộp
vẫn giữ giới hạn 5.000 câu và dung lượng media/văn bản của một bộ đề. Chế độ
trích cho phép chọn ít câu từ các bộ nguồn có tổng số câu lớn hơn 5.000.

**Ctrl+A** (Windows) hoặc **⌘A** (macOS) trong thư viện chọn toàn bộ bộ đề,
không chiếm phím khi đang sửa ô văn bản. Trong hộp chọn file hệ thống, dùng
Ctrl+A/⌘A nếu trình chọn file đó hỗ trợ; cách chọn nhiều file trên mobile tùy
trình quản lý file của hệ điều hành.

Version ở thanh tiêu đề và màn Cài đặt đọc từ assembly UI đã build, không gắn
cứng số phiên bản trong giao diện. Cần cài/chạy binary mới để thấy thay đổi.

## Xuất Office trong 5.13.0

Trong **Xuất bộ đề**, chọn **Word (.docx)** hoặc **Excel (.xlsx)**. Có thể kèm
hoặc bỏ đáp án/giải thích. Hai định dạng được tạo trực tiếp bằng Office Open XML
trên PC/mobile, không cần cài Microsoft Office, LibreOffice, không gọi server.

- DOCX: tài liệu A4 với tiêu đề, số câu, lựa chọn và giải thích. Giữ ảnh PNG/JPEG/GIF
  ở vị trí câu hỏi, lựa chọn và ghi chú. GIF có thể chỉ hiển thị ảnh tĩnh trong Word.
  Video/WebP được ghi tên trong tài liệu, không nhúng; dùng .quiz/HTML để giữ đủ media.
- XLSX: sheet `Cau hoi` có cột number, text, A–H, answer, note; cố định dòng tiêu đề,
  lọc dữ liệu, xuống dòng và độ rộng cột. Chỉ xuất chữ; không nhúng ảnh/video.
  Giá trị bắt đầu bằng `=` vẫn là chữ, không tạo công thức. Nếu ô vượt 32.767 ký tự,
  app báo lỗi và không cắt bớt nội dung; dùng DOCX/JSON/.quiz cho bộ đề đó.
- Không thêm định dạng cũ XLS/DOC trong đợt này. .quiz vẫn là bản sao lưu đầy đủ
  nhất cho media, cấu hình thi và thông tin cần rà soát của QuizDesk.

Version hiển thị là **5.17.1**, Android versionCode **60115**, iOS build **30**.
Chạy Actions với version/tag `v5.17.1`, hoặc để trống tag ở lượt chạy thủ công sau
khi commit/push source. Bộ đóng gói macOS tự đọc version từ project Desktop.

## Tự động build khi push

Push lên `main` tự chạy Android, Windows, macOS và iOS. Workflow macOS tạo
DMG kéo vào Applications riêng cho Apple Silicon/Intel. File build ở Artifacts (7 ngày); version mới
tự đưa APK/EXE/DMG lên Releases. Push lại version đã phát hành vẫn build, nhưng
giữ nguyên Release cũ. IPA chưa ký kèm SHA-256 được đưa lên cùng Release; vẫn giữ bản
trong Artifacts. Pull request không publish. Xem
[hướng dẫn Actions](.github/RELEASES.md).

## Ngày giờ trên thanh đầu

Từ 5.14.0, ngày giờ thiết bị hiển thị cạnh nút hướng dẫn, theo định dạng
`dd/MM/yyyy · HH:mm:ss` (24 giờ). Cập nhật mỗi giây, dùng lịch Gregorian và giờ
địa phương; màn hình nhỏ hiển thị ngày và giờ hai dòng. Đồng hồ làm mới ngay
khi quay lại ứng dụng và dùng timer riêng với đồng hồ đếm ngược bài thi.

### Phong cách kính đa nền tảng

Windows, macOS, Android và iOS dùng chung bảng màu kính sáng/tối với các khung
nội dung, thanh điều hướng bán trong suốt và ánh sáng nhẹ phủ kín vùng header.
Vào **Cài đặt → Phong cách kính** để bật/tắt; lựa chọn tự lưu và kế thừa cài đặt Aero.

Windows yêu cầu AcrylicBlur/Blur. macOS yêu cầu Blur khi nền tảng hỗ trợ.
Mobile và desktop không có native blur dùng nền màu mềm, đặc phía sau các lớp
kính trong app. Đây là phong cách kính trong giao diện, không phải native vibrancy
hay UIVisualEffectView trên Apple, và không nhìn xuyên ra wallpaper trên mobile.
Tắt hiệu ứng để dùng bảng màu nền đặc. Không giảm opacity của chữ, thêm blur theo
mỗi thẻ hoặc chạy animation nền liên tục. Khoảng an toàn cho tai thỏ vẫn được giữ.

Trong phần Phong cách kính có hai slider **Độ sáng của kính** (-50% đến +50%,
mặc định 0%) và **Độ trong suốt** (0–100, mặc định 50%). Thay đổi áp dụng ngay
và được nhớ khi mở lại app. Mức trong suốt là cường độ tương đối: 0 làm lớp nền
đặc, 100 đạt mức kính tối đa có giới hạn để giữ nội dung dễ đọc; không làm chữ
trong suốt. Trên mobile, nền phía sau kính vẫn nằm trong app.

### Ký hiệu trong Word

Ký hiệu Unicode thông thường và các ký hiệu phổ biến được Word lưu bằng `w:sym`
hoặc run font Symbol được chuyển sang chữ Unicode khi nhập, ví dụ `12µs`, `±`, `τ`,
`Ω`, `π`, `√`. Không cần cài font Symbol trên điện thoại. Font/ký hiệu chưa có ánh xạ
được giữ bằng nhãn và cảnh báo rà soát, không tự đoán hoặc bỏ mất ký hiệu.
Đây chưa phải bộ chuyển đổi đầy đủ công thức OMML/MathType/OLE.

Nếu bộ đề đã nhập trước bản sửa có chữ `12s` thay vì `12µs`, nhập lại file Word gốc.
Bắt đầu lượt flashcards mới để dùng nội dung đã sửa; lượt học cũ giữ snapshot cũ.

### Performance Mode

Vào **Cài đặt → Performance Mode** để giảm tải đồ họa trên thiết bị cấu hình yếu.
Chế độ này tạm tắt kính/blur và animation, dùng nền đặc, đồng thời cho đồng hồ ở
header chỉ hiển thị giờ/phút và cập nhật mỗi phút. Bộ đếm giờ thi vẫn chạy mỗi giây;
việc lưu câu trả lời, lịch sử, nhập đề và media học tập giữ nguyên.

App nhớ trạng thái sau khi mở lại. Khi bật, các tùy chọn kính và Giảm chuyển động
được vô hiệu hóa nhưng giá trị gốc không bị thay đổi. Tắt Performance Mode để
khôi phục giao diện cũ. Tính năng dùng chung trên các port; không thay đổi chế độ
nguồn điện của hệ điều hành hoặc ép tắt tăng tốc đồ họa.

### Tối ưu xử lý trong 5.17

Chế độ thường tận dụng renderer mặc định của Avalonia và các worker giải mã ảnh nền,
chọn số worker theo số lõi khả dụng và giới hạn tối đa 4 để giữ tài nguyên cho giao diện.
Performance Mode giới hạn tác vụ giải mã ảnh mới xuống 1; tác vụ đang chạy hoàn thành
và giải phóng bộ nhớ bình thường. Không ép CPU/RAM lên 100% hoặc đổi power plan hệ điều hành.

Snapshot thư viện được deep copy trực tiếp; danh sách thư viện cache metadata theo dấu
thời gian/kích thước file, không cache toàn bộ nội dung media. Mở bộ đề vẫn đọc và kiểm tra
file thật. Shuffle tuyến tính và copy pixel trực tiếp giảm thao tác/cấp phát không cần thiết.
Khung ảnh đầu tiên được giải mã ở worker, metadata GIF đọc một lần; chất lượng ảnh và
cách phát media giữ nguyên. Các bước lưu đáp án/checkpoint vẫn giữ độ bền dữ liệu.

Các phép đo chi tiết và giới hạn nằm trong `Validation/5.17.0-OPTIMIZATION.md`;
kết quả máy phát triển không phải cam kết tốc độ trên mọi thiết bị.
