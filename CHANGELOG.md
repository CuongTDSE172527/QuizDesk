# QuizDesk 5.12.0 — đang chuẩn bị phát hành (26/10/2026)

- Sửa thứ tự tham số lipo khi kiểm tra binary iPhone; tránh dừng trước bước
  đóng gói IPA chưa ký dù dotnet publish đã chạy xong.

- Sửa checkout khi chạy release thủ công: dùng nhánh/tag đang chọn, tự lấy tag
  từ version source nếu để trống, tạo tag mới tại commit đã build khi publish.
  Chặn tag trỏ sai commit; đồng bộ version 5.12.0, Android build 60101 và iOS build 16.

- Thêm GitHub Actions build APK đã ký và bộ cài EXE Windows theo tag; tự đưa
  file/SHA-256 vào cùng Release, kiểm tra version và giữ nguyên các asset đã phát hành.

- Nhập tối đa 20 file mỗi lượt; chọn mỗi file thành một bộ đề hoặc gộp thành
  một bộ tổng hợp, có thể trộn thứ tự câu hỏi.
- Đọc câu hỏi và bảng đáp án theo từng file trước khi gộp. Đánh số lại từ 1,
  giữ đáp án, giải thích, media và cảnh báo; nhật ký ghi file/số câu gốc.
- Báo kết quả từng file. Chế độ tách vẫn lưu các file hợp lệ khi file khác lỗi;
  chế độ gộp chỉ tạo bộ tổng hợp khi toàn bộ file hợp lệ, tối đa 5.000 câu.

# QuizDesk 5.11.0 — 25/10/2026

- Thêm media cho câu hỏi, lựa chọn A–H và giải thích: PNG/JPEG/WebP/GIF,
  MP4/WebM. Thêm/xóa đính kèm trong biên soạn; phát/dừng GIF theo yêu cầu.
- Giữ media nhúng trong DOC/DOCX, kể cả đoạn chỉ có ảnh. Sửa nhận dạng tiêu đề
  câu bị tách đoạn, câu/lựa chọn đánh số tự động và lựa chọn chỉ có hình.
- Với file Khí tượng: nhập 53 câu, giữ ảnh bản đồ câu19 và đáp án C.
- .quiz ZIP v2 đóng gói media cùng nội dung, dùng offline và mở qua liên kết file
  như trước. File có media yêu cầu 5.11.0+; file chữ cũ vẫn tương thích.
- Thư viện, gộp/trộn câu, trộn lựa chọn, bài đang làm, flashcards và lịch sử giữ
  media theo câu/nhãn. Nhật ký ghi thay đổi media bằng tên/kích thước/SHA256.
- Video phát native Android/iOS hoặc bằng app mặc định trên Windows/macOS;
  bộ đếm giờ thi tiếp tục. Codec phụ thuộc thiết bị, chưa stream URL/YouTube.
- Kiểm tra chữ ký định dạng, SHA256 gói media, đường dẫn, kích thước/count và
  giới hạn bộ đệm ảnh/GIF; không tải liên kết ngoài hay thực thi OLE. Nội dung
  không hỗ trợ được báo rõ. PDF/Excel/HTML/ODT vẫn nhập chữ, thêm media thủ công.
- Android versionCode60100, cùng khóa ký; iOS build15, chỉ mã nguồn trên Windows.

---

# QuizDesk 5.10.0 — 25/10/2026

- Thêm chọn nhiều bộ trong thư viện để tạo một bộ gộp mới.
- Đặt tên và bật/tắt trộn thứ tự câu; đánh số lại 1..N, giữ đáp án, giải thích,
  đáp án gốc/nguồn và các cảnh báo đi theo câu. Giữ đủ câu giống nội dung.
- Giữ nguyên các bộ nguồn; bộ gộp có thể sửa, xuất .quiz, học flashcards và thi.
- Nhật ký lưu nguồn, số câu gốc/số mới, thời gian và cách trộn; bản gộp độc lập
  với các chỉnh sửa/xóa nguồn về sau. Lịch sử thi và lượt học cũ giữ nguyên.
- Chặn nguồn trống/đã xóa, thiếu nguồn, quá 5.000 câu hoặc quá dung lượng.
  Thao tác lỗi/hủy không tạo bộ dở dang. Kiểm tra cấu hình được làm trước khi lưu.
- Mục chọn thư viện đổi tên thành Chọn bộ đề; có nút Gộp đã chọn và Xóa đã chọn.

Cài đè giữ dữ liệu. Android versionCode 60000, cùng khóa ký. iOS mã nguồn.
Chưa kiểm thử native trên Android/Mac/iOS thật; Mac chưa ký Developer ID/notarize.

---

# QuizDesk 5.9.1 — Changelog

Ngày phát hành: 23/10/2026. Áp dụng cho Windows, macOS, Android và mã nguồn iOS.

## Sửa nhận dạng câu hỏi và đáp án

- Hỗ trợ nhiều màu highlight của Word, tô nền đoạn chữ/đoạn văn và định dạng
  kế thừa từ style; nhận cả trường hợp chỉ tô nhãn A./B./…
- Chỉ một lựa chọn được đánh dấu riêng sẽ được ghi nhận là gợi ý để rà lại.
  Nhiều lựa chọn cùng có màu hoặc chỉ tô một từ nhấn mạnh không được tự chọn.
- Ưu tiên bảng đáp án, dòng “Sau rà soát” và đáp án rõ ràng hơn màu/in đậm.
  Kết luận chưa chốt không bị thay bằng suy đoán từ màu.
- Thêm các dạng “Đáp án là…”, “Đáp án đúng là:…”, “Câu trả lời đúng:…”,
  “Correct answer:…”, dấu hai chấm toàn chiều, nhãn “B — …”/“B: …” và ngoặc kép.
- Tách đáp án ở dòng/đoạn kế tiếp khỏi lựa chọn; giữ xuống dòng trong câu trả lời.
  Dừng ở đáp án đã nhận đủ, không kéo phụ lục vào câu hỏi hoặc đáp án.
- Không so khớp gần đúng để bỏ qua mâu thuẫn giữa nhãn và nội dung; giữ dấu
  tiếng Việt, số, đơn vị và ký hiệu so sánh trong quá trình đối chiếu.

## Flashcards

- Bỏ nút “Lật thẻ” dư. Chạm vào thẻ hoặc nhấn Space để lật.
- Giữ chuyển thẻ trước/sau, đánh dấu Đã nhớ/Cần học lại, hoạt ảnh nhanh và
  lưu/khôi phục tiến độ. Chỉ cho đánh giá sau khi đã lật xem đáp án.

## Bộ đề Môn 15

- Đã đối chiếu Mon-15-dap-an-goc-va-ra-soat.docx: câu 27 lấy A từ dòng
  “Sau rà soát”, dù đáp án gốc dùng “quy định” còn lựa chọn dùng “quy trình”.
- Có 30 câu, 25 đáp án dùng được. Các câu 10, 11, 16, 26, 30 vẫn chưa chốt;
  giữ cảnh báo, không tự gán đáp án hay tính điểm.
- Kèm Mon-15-da-nhan-dien.quiz đã kiểm tra để mở trực tiếp trong app.

## Bao gồm thay đổi 5.9.0 khi nâng cấp từ bản cũ

- Ôn tập thay Nộp bài bằng Thoát ôn tập; không bảng điểm hay lịch sử thi.
  Thi thử vẫn có xác nhận nộp bài, điểm và lịch sử.
- Giới hạn ZIP/XML, kích thước và cấu trúc dữ liệu nhập, số câu, bảng tính/CSV;
  timeout regex và giới hạn chuỗi kế thừa kiểu Word; xử lý file lưu hỏng an toàn.
- Giữ hỗ trợ DOC/XLS cũ, chuẩn hóa riêng các part trùng giống hệt do bộ chuyển
  DOC tạo; file nén từ bên ngoài có part trùng vẫn bị từ chối.
- Bật NuGet Audit cả dependency gián tiếp; lỗi audit/cảnh báo CVE làm build dừng.
  Rà soát Windows/Android tại thời điểm phát hành không báo advisory đã biết.

## Nâng cấp và kiểm chứng

- Cài đè bản mới để giữ dữ liệu. Android dùng cùng khóa ký, versionCode 59001.
- Bộ đề đã lưu không tự được đọc lại: nhập lại file gốc hoặc mở bản .quiz mới
  để áp dụng nhận dạng mới. Bản chụp lịch sử và lượt flashcards cũ được giữ nguyên.
- Qua 430 kiểm tra lõi, gồm 53 kiểm tra nhận dạng mới và 31 kiểm tra bảo mật.
  Kiểm tra UI thực tế trên Windows ở kích thước điện thoại: câu 27 được chấm A,
  lật bằng chạm/Space, không còn nút lật dư, tiến độ và các luồng cũ vẫn hoạt động.
- Chưa kiểm thử native 5.9.1 trên Android/macOS/iOS thật. Mac chưa ký Developer ID/
  notarize. iOS mới có mã nguồn và quy trình build, chưa có IPA.
- Rà soát bảo mật có giới hạn; parser bên thứ ba vẫn chạy trong process.
  Không coi kết quả này là chứng nhận không còn lỗ hổng.
