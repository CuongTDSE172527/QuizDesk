using QuizDesk;

partial class Tests {
    static void SmartReviewChecks(string folder) {
        var now = DateTimeOffset.Parse("2026-10-31T17:30:00Z");
        var local = TimeZoneInfo.CreateCustomTimeZone("test+07", TimeSpan.FromHours(7), "Test", "Test");
        var question = Guid.NewGuid();
        var bank = Guid.NewGuid();
        ReviewEvent Event(int days, RecallRating rating) => new(Guid.NewGuid(), bank, question, now.AddDays(days), rating, "practice");
        var wrong = Event(0, RecallRating.Again);
        var right1 = Event(1, RecallRating.Remembered);
        var right2 = Event(2, RecallRating.Remembered);
        var state = ReviewSchedule.Apply(null, right1);
        Check(state.IntervalDays == 1 && state.Due == right1.At.AddDays(1), "First recalled item scheduled next day");
        Check(!ReviewSchedule.DueToday(state, right1.At, local) && ReviewSchedule.DueToday(state, right2.At, local), "Due today uses injected local calendar");
        Check(ReviewSchedule.Apply(state, wrong).Streak == 0, "Miss resets streak");
        Check(ReviewSchedule.InWrongBook(new[]{wrong,right1},null), "One success leaves item in book");
        Check(!ReviewSchedule.InWrongBook(new[]{wrong,right1,right2},null), "Two successes remove item from book");
        Check(ReviewSchedule.InWrongBook(new[]{right1,right2},right2.At.AddHours(1)), "A newer exam flag reopens item");
        var store = new ReviewStore(folder);
        Check(store.Record(wrong) && !store.Record(wrong), "Idempotent recording");
        Check(new ReviewStore(folder).Load().Single() == wrong, "Result survives restart");
        Console.WriteLine($"TOTAL {checks} smart review checks passed");
    }
}
