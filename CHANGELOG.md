# QuizDesk 5.17.2 — 30/10/2026

- Lưu tiến trình ôn tập riêng theo từng bộ đề khi thoát; thêm Tiếp tục ôn tập trong Học & thi.
- Giữ nguyên câu đang ôn, các đáp án đã chọn, phản hồi và thứ tự câu/lựa chọn đã trộn.
- Ôn tập lại từ đầu có xác nhận trước khi thay lượt đã lưu; tiếp tục dùng snapshot cũ,
  lượt mới dùng nội dung và cấu hình hiện tại. Ôn tập không tạo điểm hay lịch sử thi thử.
- Đồng bộ version 5.17.2; Android versionCode 60116, iOS build 31.

# QuizDesk 5.17.1 — 29/10/2026

- Màn hình biên soạn hiển thị ký hiệu và toàn bộ nội dung lựa chọn đúng ngay trên từng câu hỏi;
  nội dung dài tự xuống dòng, giữ nguyên ký hiệu toán học.
- Đáp án có media được ghi chú để mở xem trong Sửa; câu thiếu đáp án hoặc cần rà soát vẫn giữ cảnh báo.
- Đồng bộ version 5.17.1; Android versionCode 60115, iOS build 30.

# QuizDesk 5.17.0 — 28/10/2026

- Tối ưu snapshot thư viện bằng deep copy trực tiếp thay cho vòng JSON/base64;
  giữ tách biệt câu hỏi, lựa chọn, media và cấu hình, giảm dữ liệu trung gian.
- Cache thông tin tóm tắt thư viện, tự làm mới khi lưu/xóa hoặc file thay đổi;
  mở bộ đề vẫn đọc và kiểm tra dữ liệu đầy đủ.
- Trộn câu hỏi/lựa chọn bằng shuffle tuyến tính, giữ liên kết đáp án/media và seed.
- Giải mã khung ảnh đầu tiên ngoài luồng giao diện; chế độ thường dùng tối đa
  min(4, max(1, số lõi khả dụng − 1)) tác vụ. Performance Mode giới hạn 1 tác vụ mới.
- Tối ưu sao chép pixel bằng buffer trực tiếp có kiểm tra kích thước, bỏ bộ đệm
  managed mỗi frame; cache metadata GIF. Giữ nguyên chất lượng, play/pause và thời lượng GIF.
- Lưu bộ đề mới nhập ở luồng nền. Lưu đáp án/checkpoint vẫn ghi an toàn như trước.

- Thêm Performance Mode trong Cài đặt cho thiết bị cấu hình yếu, tự lưu và khôi phục khi mở lại app.
- Khi bật: tạm tắt kính/blur/nền kính và chuyển động giao diện; đồng hồ header hiển thị
  giờ/phút và cập nhật theo ranh giới phút để giảm số lần cập nhật giao diện.
- Giữ nguyên cấu hình kính, slider và Giảm chuyển động; tắt Performance Mode để khôi phục.
  Các điều khiển giao diện bị ghi đè được vô hiệu hóa trong lúc bật chế độ.
- Đồng hồ thi vẫn cập nhật mỗi giây; lưu bài/đáp án, lịch sử và media học tập giữ nguyên.
- Đồng bộ version 5.17.0; Android versionCode 60114, iOS build 29.

# QuizDesk 5.16.8 — 28/10/2026

- Sửa lỗi ký hiệu như µ bị mất khi nhập Word: 12µs/50µs không còn thành 12s/50s.
- Đọc w:sym/font Symbol sang Unicode, gồm µ, ±, τ, °, ×, ÷, ≤, ≥, √, ∑ và ký hiệu thông dụng;
  hỗ trợ cả run chữ mã hóa bằng Symbol, giữ nguyên chữ Unicode có sẵn.
- Ký hiệu/font chưa hỗ trợ được giữ bằng nhãn để rà soát thay vì bị xóa âm thầm.
- Giữ ký hiệu trong câu hỏi/lựa chọn/giải thích, nội dung flashcards và file .quiz.
- Đề đã nhập cần nhập lại từ file gốc; lượt flashcards đã lưu cần bắt đầu lượt mới
  vì nội dung snapshot cũ không thể tự khôi phục các ký hiệu đã mất.
- Đồng bộ version 5.16.8; Android versionCode 60113, iOS build 28.

# QuizDesk 5.16.7 — 27/10/2026

- Thêm hai slider trong Cài đặt → Phong cách kính: độ sáng (-50% đến +50%) và
  độ trong suốt (0–100). Mặc định 0%/50% giữ giao diện của bản trước.
- Áp dụng tint/alpha ngay khi kéo trên mọi port, giữ nguyên độ đậm của chữ.
  Mức trong suốt là cường độ tương đối của lớp kính, có giới hạn nền để giữ khả năng đọc.
- Tự lưu sau khi ngừng kéo khoảng 250 ms, ghi ngay khi app chuyển nền hoặc view đóng;
  khôi phục lựa chọn khi mở lại. Slider được vô hiệu hóa khi tắt Phong cách kính.
- Đồng bộ version 5.16.7; Android versionCode 60112, iOS build 27.

# QuizDesk 5.16.6 — 27/10/2026

- Triển khai phong cách kính dùng chung cho Windows, macOS, iOS và Android;
  bảng màu sáng/tối, khung nội dung và thanh điều hướng bán trong suốt.
- Desktop yêu cầu blur khi hệ thống hỗ trợ. Mobile và desktop không có native blur
  dùng lớp kính trên nền màu mềm, đặc trong app; không nhìn xuyên ra ngoài ứng dụng.
- Công tắc Phong cách kính trong Cài đặt có mặt trên mọi port, dùng lại lựa chọn Aero đã lưu.
- Sửa ánh sáng thanh đầu bị hụt mép: nền phủ sát hai bên/mép trên của vùng header,
  giữ khoảng đệm riêng cho logo, đồng hồ và nút; hỗ trợ layout rộng và hẹp.
- Đồng bộ version 5.16.6; Android versionCode 60111, iOS build 26.

# QuizDesk 5.16.5 — 27/10/2026

- Thêm kính mờ Aero cho desktop Windows: nền và thanh điều hướng bán trong suốt,
  ánh sáng nhẹ ở thanh đầu; khung nội dung có lớp phủ giữ chữ dễ đọc.
- Thêm công tắc Kính mờ Aero trong Cài đặt, tự lưu và khôi phục khi mở lại app.
- Dùng AcrylicBlur/Blur khi hệ thống hỗ trợ; tự trở về bảng màu đặc khi không hỗ trợ
  hoặc tắt hiệu ứng. Theo giao diện sáng/tối/hệ thống; không áp dụng opacity lên chữ.
- Mobile/macOS giữ bảng màu nền đặc.
- Đồng bộ version 5.16.5; Android versionCode 60110, iOS build 25.

# QuizDesk 5.16.4 — 27/10/2026

- Tách tài liệu Word tổng hợp thành các bộ đề theo tiêu đề môn/phần; bỏ qua mục lục Word.
- Ghép đáp án riêng từng môn trước khi lưu hoặc gộp; số câu trùng giữa các môn không ảnh hưởng nhau.
- Giữ các câu bổ sung trùng số trong tài liệu nhiều môn, đánh số lại và ghi số gốc ở câu đổi số.
- Hỗ trợ nhập một/nhiều file và gộp các môn; giữ cảnh báo cho đáp án chưa rõ hoặc lựa chọn trùng.
- Đồng bộ version 5.16.4; Android versionCode 60109, iOS build 24.

# QuizDesk 5.16.3 — 27/10/2026

- Không cảnh báo rà soát khi Word có một đáp án được tô màu/highlight/in đậm rõ ràng.
- Cảnh báo khi thiếu đáp án, nhiều lựa chọn cùng được đánh dấu hoặc nguồn đáp án mâu thuẫn.
- Ghi chú trong bảng đáp án không tự tạo cảnh báo rà soát.
- Đồng bộ version 5.16.3; Android versionCode 60108, iOS build 23.

# QuizDesk 5.16.2 — 27/10/2026

- Nhận diện đáp án tô màu chữ trong Word, bao gồm màu chữ kế thừa từ style.
- Giữ yêu cầu rà soát khi nhiều lựa chọn cùng được tô màu; ưu tiên đáp án ghi rõ.
- Đồng bộ version 5.16.2; Android versionCode 60107, iOS build 22.

# QuizDesk 5.16.1 — 27/10/2026

- Tăng chữ số trong hai ô Số câu thi thử và Thời gian lên 20, dùng nét semibold
  và căn giữa theo chiều dọc để dễ đọc hơn trên PC/mobile.
- Đồng bộ version 5.16.1; Android versionCode 60106, iOS build 21.

# QuizDesk 5.16.0 — 27/10/2026

- Căn ngang nút Tiếp tục flashcards với Học flashcards: áp dụng cùng khoảng cách
  của hàng nút ngay khi tạo. Màn hình hẹp vẫn tự xuống dòng khi cần.
- Đồng bộ version 5.16.0; Android versionCode 60105, iOS build 20.

# QuizDesk 5.15.0 — 26/10/2026

- Sửa lỗi build DMG ARM64/Intel báo `Avalonia.Base.dll: code object is not signed at all`:
  dùng ditto giữ metadata chữ ký khi sao chép bundle đã ký cho DMG và PKG.
  Xác minh chữ ký ngay sau staging, trong ảnh đĩa và sau khi sao chép ra ngoài;
  không bỏ qua lỗi xác minh và không ký lại để che lỗi bản sao.

- Actions iOS xuất IPA chưa ký và SHA-256 lên GitHub Releases cùng version/tag với các nền tảng.
  Tự chạy khi push main/tag, hỗ trợ chạy thủ công; PR chỉ build, giữ Artifacts/log.
  Kiểm tra version iOS, checksum và commit; không di chuyển tag hoặc ghi đè asset đã phát hành.

- Thêm DMG macOS ARM64/Intel: kéo QuizDesk vào Applications, không qua script PKG.
  Actions kiểm tra ảnh đĩa, mount và sao chép app rồi kiểm tra chữ ký/version/kiến trúc.
  Releases macOS dùng DMG; PKG giữ trong Artifacts để chẩn đoán, không còn publish PKG mới.
  Chưa có Developer ID/notarization; chưa kết luận nguyên nhân PKG lỗi trên máy người dùng.

- Actions macOS ARM64 cài thử PKG thật trên runner Apple Silicon để chẩn đoán;
  kiểm tra version, kiến trúc và chữ ký sau cài. Lưu log installer/codesign; lỗi cài PKG
  không chặn phát hành DMG đã qua kiểm tra riêng.

- Đồng bộ version 5.15.0; Android versionCode 60104, iOS build 19.

# QuizDesk 5.14.0 — 26/10/2026

- Thêm ngày/tháng/năm và giờ 24 giờ cạnh nút hướng dẫn: dd/MM/yyyy · HH:mm:ss.
  Dùng múi giờ thiết bị, cập nhật mỗi giây; màn hình nhỏ hiển thị ngày/giờ hai dòng.
  Làm mới ngay khi quay lại app, dừng timer khi view đóng; không ảnh hưởng giờ thi.
- Chọn bộ đề bằng cách nhấn vào khung, đổi nền/viền toàn khung; bỏ checkbox
  và dòng chọn riêng. Giữ chọn tất cả, Ctrl+A/⌘A và thao tác nhật ký độc lập.
- Đồng bộ version 5.14.0; Android versionCode 60103, iOS build 18.

# QuizDesk 5.13.0 — 26/10/2026

- Tự chạy Actions khi push main; thêm macOS PKG cho Apple Silicon và Intel.
  Giữ Artifacts cho mọi bản build; chỉ publish version mới, không thay file đã phát hành.

- Xuất DOCX, XLSX, JSON, CSV, TSV, TXT và HTML bên cạnh .quiz; có thể kèm hoặc
  bỏ đáp án/giải thích. DOCX/XLSX tạo trực tiếp trên PC/mobile, không cần Office,
  LibreOffice hay server. DOCX giữ ảnh PNG/JPEG/GIF; XLSX xuất bảng chữ có lọc,
  cố định tiêu đề và ô dạng văn bản. Không thêm định dạng cũ DOC/XLS.
- JSON/HTML giữ media; DOCX ghi chú tên video/WebP không nhúng. CSV/TSV/TXT/XLSX
  chỉ xuất chữ. CSV/TSV bảo vệ ô công thức; XLSX dùng ô văn bản và báo lỗi khi ô
  vượt 32.767 ký tự, không cắt mất dữ liệu.
- Nhập nhiều file: mỗi file một bộ hoặc gộp thành một bộ; đọc câu hỏi và bảng
  đáp án riêng từng file trước khi gộp, đánh số lại và giữ ánh xạ nguồn.
- Bỏ giới hạn 20 file/lượt; thêm Ctrl+A/⌘A chọn tất cả bộ đề trong thư viện.
  Bộ tổng hợp vẫn giữ giới hạn 5.000 câu và dung lượng xử lý hiện có.
- Chọn số câu ngẫu nhiên từ từng bộ để tạo đề tổng hợp/thi thử, không lặp trong
  bộ nguồn; trộn thứ tự chung, giữ đúng đáp án/giải thích/media và không sửa bộ gốc.
- Thanh tiêu đề và Cài đặt lấy version từ assembly build thay vì ghi cứng.
  Đồng bộ 5.13.0, Android build 60102 và iOS build 17. Đóng gói macOS đọc version
  từ project Desktop.
- GitHub Actions build APK đã ký và EXE theo tag, đính kèm file/SHA-256 vào cùng
  Release. Chạy thủ công có thể để trống tag; tạo tag mới ở đúng commit đã build
  khi publish. Không di chuyển tag hoặc ghi đè asset đã phát hành.
- Sửa thứ tự tham số lipo để bước kiểm tra binary iPhone không chặn đóng gói IPA.
- Thêm kiểm thử xuất Office, xuất dữ liệu, trích câu theo bộ và phát hành.

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
