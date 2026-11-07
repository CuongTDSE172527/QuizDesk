using QuizDesk;

partial class Tests {
    static void Statistics60Checks() {
        var now = DateTimeOffset.Parse("2026-11-07T00:15:00+07:00");
        var zone = TimeZoneInfo.CreateCustomTimeZone("stats+07", TimeSpan.FromHours(7), "Test", "Test");
        Guid id = Guid.NewGuid(), qid = Guid.NewGuid();
        var summary = new LibraryBankSummary(id,"Đề QA",1,now,null,now,"");
        var question = new ReviewCandidate(id,summary.Title,qid,1,"Câu QA",null,false);
        var attempt = new ExamAttempt { Title=summary.Title, FinishedAt=now.AddDays(-1),
            Questions=new() {new AttemptQuestion { Number=1, Text=question.Text, Picked="A", Answer="B" }} };
        var review = new ReviewEvent(Guid.NewGuid(), id, qid, now, RecallRating.Remembered,"scheduled",12);
        var model = new LearningStatistics(new[]{summary},new[]{question},new[]{attempt},new[]{review});
        var stats = model.Calculate(now,zone,7,id.ToString("N"));
        Check(stats.Accuracy.Percent==50 && stats.StudyDays==2 && stats.Streak==2, "Legacy accuracy and local midnight streak combine");
        Check(stats.Missed.Single().Text==question.Text && stats.AverageSeconds==12, "Missing legacy time excluded from average");
        Check(model.Calculate(now,zone,1).Accuracy.Percent==100, "Local date range excludes yesterday");
        Check(model.Calculate(now,zone,7,"missing").Accuracy.Percent==null, "Empty filter remains unmeasured");
        var legacy = new LearningStatistics(new[]{summary},new[]{question},new[]{attempt},Array.Empty<ReviewEvent>());
        Check(legacy.Calculate(now,zone).AverageSeconds==null, "Legacy-only results do not invent timing");
        Console.WriteLine($"TOTAL {checks} statistics checks passed");
    }
}
