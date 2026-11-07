using System.Diagnostics;
using Avalonia.Controls;

namespace QuizDesk.Mobile;

public partial class MainView {
    readonly Dictionary<int, double> examSeconds = new();
    long examTimingAt;

    void AccumulateExamTime() {
        if (!active || practice || index < 0 || index >= exam.Count) return;
        long now = Stopwatch.GetTimestamp();
        double seconds = Stopwatch.GetElapsedTime(examTimingAt, now).TotalSeconds;
        examTimingAt = now;
        if (seconds > 0)
            examSeconds[index] = Math.Min(examMinutes * 60, examSeconds.GetValueOrDefault(index) + seconds);
    }

    void StatisticsPage() {
        Heading("Tiến bộ của bạn", "Thống kê", "Theo dõi kết quả ôn tập và thi thử theo bộ đề.");
        var container = Stack(Supporting("Đang tổng hợp dữ liệu…"));
        page.Children.Add(container);
        _ = LoadStatistics(container);
    }

    async Task LoadStatistics(StackPanel container) {
        try {
            var model = await Task.Run(() => {
                var attempts = historyStore.Load(out _);
                var reviews = reviewStore.Load();
                var catalog = ReviewCatalog.Build(library, attempts, reviews);
                return new LearningStatistics(library.List(out _), catalog, attempts, reviews);
            });
            if (currentPage != "statistics" || !page.Children.Contains(container)) return;
            var period = new ComboBox { Name = "StatisticsPeriod", ItemsSource = new[] { "7 ngày", "30 ngày", "Toàn bộ" }, SelectedIndex = 1 };
            var bank = new ComboBox { Name = "StatisticsBank", ItemsSource = new[] { "Tất cả bộ đề" }.Concat(model.Banks.Select(item => item.Title)).ToArray(), SelectedIndex = 0 };
            var results = new StackPanel { Spacing = 14 };
            container.Children.Clear();
            container.Children.Add(Row(Field("Khoảng thời gian", period), Field("Bộ đề", bank)));
            container.Children.Add(results);
            int generation = 0;
            async void Refresh() {
                int request = ++generation;
                int? days = period.SelectedIndex switch { 0 => 7, 1 => 30, _ => null };
                string key = bank.SelectedIndex > 0 ? model.Banks[bank.SelectedIndex - 1].Key : null;
                var stats = await Task.Run(() => model.Calculate(DateTimeOffset.UtcNow, TimeZoneInfo.Local, days, key));
                if (request == generation && page.Children.Contains(container)) RenderStatistics(results, stats, days.HasValue);
            }
            period.SelectionChanged += (_, _) => Refresh();
            bank.SelectionChanged += (_, _) => Refresh();
            Refresh();
        } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or System.Text.Json.JsonException) {
            container.Children.Clear();
            container.Children.Add(Supporting("Chưa đọc được thống kê: " + ex.Message));
        }
    }

    void RenderStatistics(StackPanel results, LearningStats stats, bool daily) {
        results.Children.Clear();
        static string Percent(AccuracySummary row) => row.Percent is double percent ? $"{percent:0.#}%" : "Chưa có dữ liệu";
        results.Children.Add(Card(Stack(
            Text("Độ chính xác " + Percent(stats.Accuracy), 24),
            Supporting($"{stats.Accuracy.Right} đúng · {stats.Accuracy.Wrong} sai · Tính trên câu đã chấm"),
            Text($"{stats.StudyDays} ngày học · Chuỗi hiện tại {stats.Streak} ngày"),
            Text(stats.AverageSeconds is double seconds
                ? $"Thời gian trung bình {seconds:0.#} giây/câu"
                : "Thời gian trung bình: chưa có dữ liệu"))));
        results.Children.Add(Card(Stack(Text("Mức độ ghi nhớ hiện tại", 20),
            Text($"{stats.Remembered} đã thuộc · {stats.Learning} đang học · {stats.Unseen} chưa học"),
            Supporting("Đã thuộc khi nhớ đúng ít nhất hai lần liên tiếp.", 12))));
        Border Chart(string title, IEnumerable<AccuracySummary> rows) {
            var panel = Stack(Text(title, 20));
            foreach (var row in rows) {
                panel.Children.Add(Supporting($"{row.Label} · {Percent(row)} · {row.Right + row.Wrong} câu"));
                if (row.Percent is double percent)
                    panel.Children.Add(new ProgressBar { Minimum = 0, Maximum = 100, Value = percent, Height = 6 });
            }
            if (panel.Children.Count == 1) panel.Children.Add(Supporting("Chưa có dữ liệu"));
            return Card(panel);
        }
        results.Children.Add(Chart(daily ? "Độ chính xác theo ngày" : "Độ chính xác theo tháng · tối đa 24 tháng gần nhất", stats.Timeline));
        results.Children.Add(Chart("Độ chính xác theo bộ đề", stats.Banks));
        var missed = Stack(Text("Câu hay sai", 20));
        foreach (var item in stats.Missed) {
            missed.Children.Add(Text(item.Text));
            missed.Children.Add(Supporting($"{item.Bank} · Sai {item.Count} lần", 12));
        }
        if (stats.Missed.Count == 0) missed.Children.Add(Supporting("Chưa có câu sai trong khoảng này."));
        results.Children.Add(Card(missed));
    }
}
