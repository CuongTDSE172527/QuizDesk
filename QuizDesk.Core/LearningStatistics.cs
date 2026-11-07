namespace QuizDesk;

public sealed record AccuracySummary(string Label, int Right, int Wrong) {
    public double? Percent => Right + Wrong == 0 ? null : 100.0 * Right / (Right + Wrong);
}
public sealed record LearningBank(string Key, string Title, Guid? Id);
public sealed record MissedQuestion(string Bank, string Text, int Count);
public sealed record LearningStats(AccuracySummary Accuracy, IReadOnlyList<AccuracySummary> Banks,
    IReadOnlyList<AccuracySummary> Timeline, IReadOnlyList<MissedQuestion> Missed, double? AverageSeconds,
    int Remembered, int Learning, int Unseen, int StudyDays, int Streak);

public sealed class LearningStatistics {
    sealed record Activity(string BankKey, Guid Question, string Text, DateTimeOffset At, bool? Right, double? Seconds);
    readonly List<Activity> activities = new();
    readonly IReadOnlyList<ReviewCandidate> catalog;
    public IReadOnlyList<LearningBank> Banks { get; }

    public LearningStatistics(IReadOnlyList<LibraryBankSummary> banks, IReadOnlyList<ReviewCandidate> catalog,
        IReadOnlyList<ExamAttempt> attempts, IReadOnlyList<ReviewEvent> reviews) {
        this.catalog = catalog;
        var labels = banks.ToDictionary(bank => Key(bank.Id), bank => new LearningBank(Key(bank.Id), bank.Title, bank.Id));
        var byQuestion = catalog.ToDictionary(question => question.QuestionId);
        var uniqueTitles = banks.GroupBy(bank => bank.Title, StringComparer.Ordinal).Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single().Id);
        var byBankAndNumber = catalog.GroupBy(question => question.BankId)
            .ToDictionary(group => group.Key, group => group.ToDictionary(question => question.Number));
        foreach (var attempt in attempts) {
            Guid? bankId = attempt.LibraryBankId;
            if (!bankId.HasValue && uniqueTitles.TryGetValue(attempt.Title, out var candidate)) {
                var current = byBankAndNumber.GetValueOrDefault(candidate);
                if (current != null && attempt.Questions.All(question => current.TryGetValue(question.Number, out var match) && match.Text == question.Text))
                    bankId = candidate;
            }
            string key = bankId.HasValue ? Key(bankId.Value) : "legacy:" + attempt.Title;
            labels.TryAdd(key, new(key, attempt.Title, bankId));
            foreach (var question in attempt.Questions) {
                bool? right = question.Outcome switch { "correct" => true, "wrong" => false, _ => null };
                activities.Add(new(key, question.StudyId, question.Text, attempt.FinishedAt, right, question.Seconds));
            }
        }
        foreach (var review in reviews) {
            var key = Key(review.BankId);
            if (!labels.ContainsKey(key)) labels[key] = new(key, "Bộ đề đã xóa", review.BankId);
            string question = byQuestion.TryGetValue(review.QuestionId, out var match) ? match.Text : "";
            activities.Add(new(key, review.QuestionId, question, review.At, review.Rating == RecallRating.Remembered, review.Seconds));
        }
        Banks = labels.Values.OrderBy(bank => bank.Title, StringComparer.CurrentCulture).ToArray();
    }

    static string Key(Guid id) => id.ToString("N");
    static AccuracySummary Count(string title, IEnumerable<Activity> items) {
        int right = 0, wrong = 0;
        foreach (var item in items)
            if (item.Right == true) right++; else if (item.Right == false) wrong++;
        return new(title, right, wrong);
    }

    public LearningStats Calculate(DateTimeOffset now, TimeZoneInfo zone, int? days = null, string bankKey = null) {
        DateOnly Day(DateTimeOffset instant) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, zone).DateTime);
        var today = Day(now);
        var from = days.HasValue ? today.AddDays(1 - days.Value) : DateOnly.MinValue;
        var selected = activities.Where(item => (bankKey == null || item.BankKey == bankKey)
            && item.At <= now && Day(item.At) >= from).ToList();
        var dates = selected.Select(item => Day(item.At)).ToHashSet();
        int streak = 0;
        var cursor = dates.Contains(today) ? today : today.AddDays(-1);
        while (dates.Contains(cursor)) { streak++; cursor = cursor.AddDays(-1); }
        var groups = selected.ToLookup(item => item.BankKey);
        var perBank = Banks.Where(bank => (bankKey == null || bank.Key == bankKey) && groups.Contains(bank.Key))
            .Select(bank => Count(bank.Title, groups[bank.Key])).ToArray();
        var timeline = selected.GroupBy(item => {
            var date = Day(item.At);
            return days.HasValue ? date : new DateOnly(date.Year, date.Month, 1);
        }).OrderByDescending(group => group.Key).Take(days ?? 24).Reverse()
            .Select(group => Count(group.Key.ToString(days.HasValue ? "dd/MM" : "MM/yyyy"), group)).ToArray();
        var titles = Banks.ToDictionary(bank => bank.Key, bank => bank.Title);
        var missed = selected.Where(item => item.Right == false && !string.IsNullOrWhiteSpace(item.Text))
            .GroupBy(item => (item.BankKey, item.Question, LegacyText: item.Question == Guid.Empty ? item.Text : ""))
            .OrderByDescending(group => group.Count()).Take(10)
            .Select(group => new MissedQuestion(titles[group.Key.BankKey], group.OrderByDescending(item => item.At).First().Text, group.Count())).ToArray();
        var timings = selected.Where(item => item.Seconds is double seconds && double.IsFinite(seconds) && seconds > 0)
            .Select(item => item.Seconds.Value).ToArray();
        var questions = catalog.Where(item => bankKey == null || Key(item.BankId) == bankKey).ToList();
        return new(Count("Tổng", selected), perBank, timeline, missed, timings.Length == 0 ? null : timings.Average(),
            questions.Count(item => item.State?.Streak >= 2), questions.Count(item => item.State != null && item.State.Streak < 2),
            questions.Count(item => item.State == null), dates.Count, streak);
    }
}
