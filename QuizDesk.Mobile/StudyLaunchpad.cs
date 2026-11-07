using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace QuizDesk.Mobile;

public partial class MainView {
    void Home() {
        int selected = bank.Count(question => setup.Included.Contains(question.Number));
        var savedPractice = currentBankId is Guid id ? PracticeWorkspace(id).LoadActive(out _) : null;
        if (savedPractice?.Practice != true || savedPractice.LibraryBankId != currentBankId)
            savedPractice = null;

        var content = new StackPanel { Spacing = 12 };
        if (savedPractice != null) {
            content.Children.Add(Chip("ÔN TẬP ĐANG DỞ"));
            content.Children.Add(LaunchTitle(title));
            content.Children.Add(LaunchDetail($"Câu {savedPractice.Index + 1} / {savedPractice.Exam.Count} · Đã trả lời {savedPractice.Answers.Count} câu"));
            content.Children.Add(new ProgressBar {
                Minimum = 0, Maximum = savedPractice.Exam.Count, Value = savedPractice.Answers.Count, Height = 6
            });
            content.Children.Add(Row(Btn("Tiếp tục ôn tập", () => ResumePractice(savedPractice), true)));
        } else if (bank.Count > 0) {
            content.Children.Add(Chip("BỘ ĐỀ ĐANG CHỌN"));
            content.Children.Add(LaunchTitle(title));
            content.Children.Add(LaunchDetail($"{selected} / {bank.Count} câu được chọn · Xem đáp án ngay khi ôn"));
            content.Children.Add(Row(selected > 0
                ? Btn("Bắt đầu ôn tập", StartNewPractice, true)
                : Btn("Chọn câu để học", () => OpenLibraryBank(currentBankId.Value), true)));
        } else {
            content.Children.Add(Chip("BẮT ĐẦU HỌC"));
            content.Children.Add(LaunchTitle("Thử ngay một lượt ôn tập"));
            content.Children.Add(LaunchDetail("Trả lời 2 câu mẫu và xem phản hồi ngay. Có đề riêng rồi? Nhập file để học với bộ đề của bạn."));
            content.Children.Add(Row(Btn("Thử 2 câu mẫu", async () => await LoadSample(true), true), Btn("＋ Nhập file đề", Import)));
        }

        var hero = Card(content);
        Tone(hero, Border.BackgroundProperty, "PrimaryContainer");
        page.Children.Add(hero);

        var savedStudy = studyStore.Load(out _);
        if (bank.Count > 0 || savedStudy != null) {
            page.Children.Add(Text("Chọn cách học", 20));
            bool hasCards = bank.Any(question => setup.Included.Contains(question.Number)
                && QuestionReview.ScoringAnswer(question) != "");
            var flashAction = savedStudy != null
                ? Btn("Tiếp tục flashcards", () => OpenStudy(savedStudy))
                : Btn("Học flashcards", StartStudy);
            flashAction.IsEnabled = savedStudy != null || hasCards;
            var flashcards = LaunchMode("Flashcards",
                savedStudy != null
                    ? $"{savedStudy.Title} · Đã nhớ {savedStudy.Remembered}/{savedStudy.Cards.Count} thẻ"
                    : hasCards ? "Lật thẻ để tự kiểm tra trí nhớ." : "Chọn câu trắc nghiệm có đáp án để tạo thẻ.",
                flashAction);
            var mockAction = Btn("Thiết lập bài thi", () => Navigate("setup"));
            mockAction.IsEnabled = selected > 0;
            var mock = LaunchMode("Thi thử", "Chọn số câu, đặt thời gian và xem kết quả sau khi nộp bài.", mockAction);
            var modes = new Grid { RowSpacing = 12, ColumnSpacing = 12 };
            modes.Children.Add(flashcards);
            modes.Children.Add(mock);
            void ArrangeModes(bool wide) {
                modes.ColumnDefinitions = new ColumnDefinitions(wide ? "*,*" : "*");
                modes.RowDefinitions = new RowDefinitions(wide ? "Auto" : "Auto,Auto");
                Grid.SetColumn(mock, wide ? 1 : 0);
                Grid.SetRow(mock, wide ? 0 : 1);
            }
            ArrangeModes(false);
            modes.SizeChanged += (_, change) => {
                if ((change.PreviousSize.Width >= 620) != (change.NewSize.Width >= 620))
                    ArrangeModes(change.NewSize.Width >= 620);
            };
            page.Children.Add(modes);
        }

        var openLibrary = Btn("Mở thư viện", () => Navigate("bank"));
        var createBank = Btn("Tạo bộ đề", NewBank);
        page.Children.Add(bank.Count > 0
            ? Row(openLibrary, createBank, Btn("＋ Nhập file đề", Import))
            : Row(openLibrary, createBank));
        page.Children.Add(Supporting("Word · PDF · Excel · Văn bản · .quiz", 12));
    }

    static TextBlock LaunchTitle(string title) {
        var heading = LaunchDetail(title, 32);
        heading.FontWeight = FontWeight.SemiBold;
        return heading;
    }

    static TextBlock LaunchDetail(string value, double size = 15) {
        var text = Text(value, size);
        text.Classes.Add("onHero");
        return text;
    }

    Border LaunchMode(string title, string description, Button action) {
        action.HorizontalAlignment = HorizontalAlignment.Left;
        return Card(Stack(Text(title, 20), Supporting(description), action));
    }
}
