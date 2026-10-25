# Đưa QuizDesk lên GitHub / GitLab

Khuyến nghị GitHub private cho project cá nhân này. Có thể mở public về sau
nếu bạn muốn chia sẻ mã nguồn và đã chọn giấy phép phù hợp.

Chỉ đưa thư mục mã nguồn QuizDeskMobile (hoặc nội dung Source.zip) vào repo;
không đưa toàn bộ work/outputs, SDK, emulator, cache NuGet, file đề cá nhân hay
thư viện/History trên thiết bị. .gitignore đã loại bin/obj, qa-*, dist, bộ cài
và khóa ký. Giữ khóa Android/iOS ở nơi riêng; khi cần CI dùng repository secrets.

Cấu trúc đề xuất:
- Repo: Core, UI dùng chung, Windows/macOS/Android/iOS hosts, tests, assets,
  build scripts, changelog và hướng dẫn.
- Releases: APK/EXE/PKG/Source.zip tương ứng từng tag, ví dụ v5.11.0.
- Bộ đề/media người dùng: chia sẻ .quiz trực tiếp, không dùng Git làm kho video.

GitHub Actions có runner Windows/Linux/macOS; có thể thiết lập build theo tag
và kiểm tra Core theo pull request. Repository private có quota phút/storage;
đặt ngân sách và chỉ build bản cài khi ra release để hạn chế phát sinh.
GitLab phù hợp khi đã dùng hệ sinh thái GitLab hoặc cần tự host; macOS hosted
runner hiện ở beta và thuộc Premium/Ultimate hoặc chương trình open source.

Repository: https://github.com/CuongTDSE172527/QuizDesk (private).
Mã nguồn 5.11.0 được lưu ở nhánh main; bộ cài được phân phối qua Releases.
Khóa ký và bộ đề cá nhân không nằm trong repo.

Tài liệu chính thức (đối chiếu 25/10/2026):
https://docs.github.com/en/actions/reference/runners/github-hosted-runners
https://docs.github.com/en/actions/concepts/billing-and-usage
https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases
https://docs.gitlab.com/ci/runners/hosted_runners/macos/
