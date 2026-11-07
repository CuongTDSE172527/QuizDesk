using System.Diagnostics;
using Avalonia.Controls;

namespace QuizDesk.Mobile;

public partial class MainView {
    readonly ReviewStore reviewStore;
    long questionShownAt;
    List<(ReviewCandidate Candidate, Question Question)> smartSession;
    Guid smartSessionId;
    int smartIndex;
    bool smartRated;

    List<ReviewCandidate> ReadReviewCatalog() =>
        ReviewCatalog.Build(library, historyStore.Load(out _), reviewStore.Load());

    bool RecordRecall(Guid session, Guid? bankId, Guid questionId, bool remembered, string source, double? seconds = null) {
        if (bankId is not Guid id || questionId == Guid.Empty) return false;
        try {
            reviewStore.Record(new(session, id, questionId, DateTimeOffset.UtcNow,
                remembered ? RecallRating.Remembered : RecallRating.Again, source, seconds));
            return true;
        } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException) {
            Notify("Chưa lưu được lịch ôn: " + ex.Message, true);
            return false;
        }
    }

    bool RecordPracticeRecall(bool remembered) =>
        RecordRecall(activeAttemptId, examBankId, exam[index].StudyId, remembered, "practice",
            Stopwatch.GetElapsedTime(questionShownAt).TotalSeconds);

    void AddSmartReviewCard() {
        var body = Stack(Text("Hôm nay cần ôn", 22), Supporting("Đang kiểm tra lịch ôn…"));
        page.Children.Add(Card(body));
        _ = LoadSmartReviewCard(body);
    }

    async Task LoadSmartReviewCard(StackPanel body) {
        try {
            var catalog = await Task.Run(ReadReviewCatalog);
            if (currentPage != "home" || active || studying || body.Parent is not Border card || !page.Children.Contains(card)) return;
            var now = DateTimeOffset.UtcNow;
            var due = catalog.Where(item => ReviewSchedule.DueToday(item.State, now, TimeZoneInfo.Local))
                .OrderBy(item => item.State?.Due ?? DateTimeOffset.MinValue).ToList();
            var wrong = catalog.Where(item => item.WrongBook).ToList();
            var count = Number(preferences.SmartReviewLimit, 100);
            count.ValueChanged += (_, _) => {
                preferences.SmartReviewLimit = Value(count);
                SavePreferences();
            };
            body.Children.Clear();
            body.Children.Add(Text("Hôm nay cần ôn", 22));
            string status = due.Count > 0 ? $"{due.Count} câu đến hạn hoặc chưa ôn."
                : catalog.Count > 0 ? $"Lần tới {catalog.Min(item => item.State.Due).ToLocalTime():dd/MM/yyyy}."
                : "Nhập bộ đề có đáp án để tạo lịch ôn.";
            body.Children.Add(Supporting(status));
            body.Children.Add(Field("Số câu tối đa mỗi lượt", count));
            var start = Btn("Ôn theo lịch", () => StartSmartReview(due), true);
            start.IsEnabled = due.Count > 0;
            var book = Btn($"Sổ câu sai ({wrong.Count})", () => StartSmartReview(wrong));
            book.IsEnabled = wrong.Count > 0;
            body.Children.Add(Row(start, book));
            body.Children.Add(Supporting("Câu sai hoặc đã đánh dấu rời sổ sau hai lượt ôn đúng liên tiếp.", 12));
        } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or System.Text.Json.JsonException) {
            body.Children.Add(Supporting("Chưa đọc được lịch ôn: " + ex.Message));
        }
    }

    async void StartSmartReview(IReadOnlyList<ReviewCandidate> candidates) {
        if (busy || active || studying || smartSession != null) return;
        busy = true;
        shell.IsEnabled = false;
        try {
            var selected = candidates.Take(preferences.SmartReviewLimit).ToList();
            var loaded = await Task.Run(() => {
                var found = new Dictionary<Guid, Question>();
                foreach (var group in selected.GroupBy(item => item.BankId)) {
                    var bank = library.Load(group.Key);
                    var ids = group.Select(item => item.QuestionId).ToHashSet();
                    foreach (var question in bank.Content.Bank.Where(question => ids.Contains(question.StudyId)))
                        found[question.StudyId] = question;
                }
                return selected.Where(item => found.ContainsKey(item.QuestionId))
                    .Select(item => (item, found[item.QuestionId])).ToList();
            });
            if (loaded.Count == 0) { Navigate("home"); return; }
            smartSession = loaded;
            smartSessionId = Guid.NewGuid();
            smartIndex = 0;
            navigation.IsEnabled = false;
            RenderSmartReview();
        } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException) {
            Notify("Chưa mở được lượt ôn: " + ex.Message, true);
        } finally {
            busy = false;
            shell.IsEnabled = true;
        }
    }

    void RenderSmartReview() {
        page.Children.Clear();
        pageScroll.Offset = default;
        smartRated = false;
        var (candidate, question) = smartSession[smartIndex];
        long opened = Stopwatch.GetTimestamp();
        Heading("Ôn tập thông minh", $"Câu {smartIndex + 1} / {smartSession.Count}", candidate.BankTitle);
        var body = Stack(Text(question.Text, 22), Gallery(question.Media.Where(media => media.Target == "question")));
        var feedback = new StackPanel { Spacing = 12 };
        var advance = Btn(smartIndex + 1 == smartSession.Count ? "Xong lượt ôn" : "Câu tiếp", () => {
            if (!smartRated) return;
            if (++smartIndex == smartSession.Count) ExitSmartReview();
            else RenderSmartReview();
        }, true);
        advance.IsEnabled = false;
        void Rate(bool remembered) {
            if (smartRated || !RecordRecall(smartSessionId, candidate.BankId, candidate.QuestionId, remembered,
                "scheduled", Stopwatch.GetElapsedTime(opened).TotalSeconds)) return;
            smartRated = true;
            advance.IsEnabled = true;
            feedback.Children.Clear();
            feedback.Children.Add(Text(remembered ? "Đã ghi nhận nhớ đúng." : "Đã đưa vào sổ câu sai."));
            feedback.Children.Add(Text("Đáp án: " + (question.IsEssay ? question.Answer : question.Options[question.Answer])));
            if (question.Note != "") feedback.Children.Add(Text(question.Note));
            feedback.Children.Add(Gallery(question.Media.Where(media => media.Target == "note" || media.Target == question.Answer)));
        }
        if (question.IsEssay) {
            body.Children.Add(new TextBox { AcceptsReturn = true, MinHeight = 120, MaxLength = 32768, PlaceholderText = "Gõ bài làm để tự đối chiếu" });
            body.Children.Add(Btn("Xem đáp án mẫu", () => {
                if (smartRated) return;
                feedback.Children.Clear();
                feedback.Children.Add(Text(question.Answer));
                feedback.Children.Add(Row(Btn("Đã nhớ", () => Rate(true)), Btn("Cần ôn lại", () => Rate(false))));
            }));
        } else {
            foreach (var option in question.Options) {
                string label = option.Key + ". " + option.Value;
                var choice = Btn(label, () => Rate(option.Key == question.Answer));
                choice.Name = "SmartChoice" + option.Key;
                choice.Content = Text(label);
                Avalonia.Automation.AutomationProperties.SetName(choice, label);
                body.Children.Add(Stack(choice, Gallery(question.Media.Where(media => media.Target == option.Key))));
            }
        }
        body.Children.Add(feedback);
        page.Children.Add(Card(body));
        page.Children.Add(Row(advance, Btn("Thoát lượt ôn", ExitSmartReview)));
    }

    void ExitSmartReview() {
        smartSession = null;
        navigation.IsEnabled = true;
        Navigate("home");
        IncomingChanged();
    }
}
