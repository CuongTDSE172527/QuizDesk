using System.Text.Json;

namespace QuizDesk;

public enum RecallRating { Again, Remembered }
public sealed record ReviewEvent(Guid SessionId, Guid BankId, Guid QuestionId, DateTimeOffset At,
    RecallRating Rating, string Source, double? Seconds = null);
public sealed record ReviewState(DateTimeOffset Last, DateTimeOffset Due, int IntervalDays, double Ease, int Streak);

public static class ReviewSchedule {
    public static ReviewState Apply(ReviewState previous, ReviewEvent review) {
        double ease = previous?.Ease ?? 2.3;
        bool remembered = review.Rating == RecallRating.Remembered;
        int streak = remembered ? (previous?.Streak ?? 0) + 1 : 0;
        int days = remembered ? streak switch {
            1 => 1, 2 => 3, _ => (int)Math.Clamp(Math.Round(previous.IntervalDays * ease), 4, 365)
        } : 0;
        ease = remembered ? Math.Min(3.0, ease + 0.1) : Math.Max(1.3, ease - 0.2);
        return new(review.At, remembered ? review.At.AddDays(days) : review.At.AddMinutes(15), days, ease, streak);
    }

    public static bool DueToday(ReviewState state, DateTimeOffset now, TimeZoneInfo zone) =>
        state == null || TimeZoneInfo.ConvertTime(state.Due, zone).Date <= TimeZoneInfo.ConvertTime(now, zone).Date;

    public static bool InWrongBook(IEnumerable<ReviewEvent> reviews, DateTimeOffset? lastExamIssue) {
        var ordered = reviews.OrderByDescending(item => item.At).ToArray();
        var lastAgain = ordered.FirstOrDefault(item => item.Rating == RecallRating.Again)?.At;
        var issue = lastAgain > lastExamIssue || lastExamIssue == null ? lastAgain : lastExamIssue;
        return issue.HasValue && ordered.Count(item => item.At > issue && item.Rating == RecallRating.Remembered) < 2;
    }
}

// One immutable file per completed response. The rename commits the response atomically
// and the deterministic name prevents duplicate reviews when an attempt is resumed.
public sealed class ReviewStore(string folder) {
    readonly object gate = new();
    sealed record Envelope(int Version, ReviewEvent Review);
    List<ReviewEvent> cache;

    public IReadOnlyList<ReviewEvent> Load() {
        lock (gate) {
            if (cache == null) {
                var loaded = new List<ReviewEvent>();
                if (Directory.Exists(folder))
                    foreach (string path in Directory.EnumerateFiles(folder, "*.json")) {
                        using var stream = InputSafety.OpenFile(path, 32768);
                        var item = JsonSerializer.Deserialize<Envelope>(stream);
                        if (item?.Version != 1 || item.Review == null) throw new InvalidDataException("Dữ liệu lịch ôn không hợp lệ.");
                        loaded.Add(item.Review);
                    }
                cache = loaded;
            }
            return cache.ToArray();
        }
    }

    public bool Record(ReviewEvent review) {
        if (review.SessionId == Guid.Empty || review.BankId == Guid.Empty || review.QuestionId == Guid.Empty)
            return false;
        lock (gate) {
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, $"{review.SessionId:N}-{review.QuestionId:N}.json");
            if (File.Exists(path)) return false;
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                using (var stream = File.Create(temp)) {
                    JsonSerializer.Serialize(stream, new Envelope(1, review));
                    stream.Flush(true);
                }
                try { File.Move(temp, path); }
                catch (IOException) when (File.Exists(path)) { cache = null; return false; }
                cache?.Add(review);
                return true;
            } finally { File.Delete(temp); }
        }
    }
}

public sealed record ReviewCandidate(Guid BankId, string BankTitle, Guid QuestionId, int Number,
    string Text, ReviewState State, bool WrongBook, bool Ready = true);

public static class ReviewCatalog {
    public static List<ReviewCandidate> Build(MobileBankLibrary library,
        IReadOnlyList<ExamAttempt> attempts, IReadOnlyList<ReviewEvent> reviews) {
        var banks = library.List(out _);
        var uniqueTitles = banks.GroupBy(bank => bank.Title, StringComparer.Ordinal)
            .Where(group => group.Count() == 1).ToDictionary(group => group.Key, group => group.Single().Id);
        var attemptsByBank = attempts.Select(attempt => (Attempt: attempt, BankId: attempt.LibraryBankId ??
                uniqueTitles.GetValueOrDefault(attempt.Title)))
            .ToLookup(entry => entry.BankId, entry => entry.Attempt);
        var reviewGroups = reviews.ToLookup(review => review.QuestionId);
        var result = new List<ReviewCandidate>();
        foreach (var summary in banks) {
            var bank = library.Load(summary.Id);
            if (bank.DeletedAt.HasValue) continue;
            var byNumber = bank.Content.Bank.ToDictionary(question => question.Number);
            var issues = new Dictionary<Guid, DateTimeOffset>();
            foreach (var attempt in attemptsByBank[summary.Id])
                foreach (var item in attempt.Questions.Where(question => question.Outcome == "wrong" || question.Flagged)) {
                    var id = item.StudyId;
                    if (id == Guid.Empty && byNumber.TryGetValue(item.Number, out var original)
                        && original.Text == item.Text) id = original.StudyId;
                    if (id != Guid.Empty && attempt.FinishedAt > issues.GetValueOrDefault(id)) issues[id] = attempt.FinishedAt;
                }
            foreach (var question in bank.Content.Bank) {
                bool ready = question.IsEssay ? !string.IsNullOrWhiteSpace(question.Answer) : QuestionReview.ScoringAnswer(question) != "";
                var records = reviewGroups[question.StudyId].OrderBy(review => review.At).ToArray();
                ReviewState state = null;
                foreach (var review in records) state = ReviewSchedule.Apply(state, review);
                DateTimeOffset? issue = issues.TryGetValue(question.StudyId, out var date) ? date : null;
                result.Add(new(summary.Id, bank.Content.Title, question.StudyId, question.Number,
                    question.Text, state, ReviewSchedule.InWrongBook(records, issue), ready));
            }
        }
        return result;
    }
}
