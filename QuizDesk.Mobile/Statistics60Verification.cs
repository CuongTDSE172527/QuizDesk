#if QA
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace QuizDesk.Mobile;
public partial class MainView {
    async Task<string> VerifyStatistics60() {
        var source = library.Create(new WorkspaceBank {
            Title = "Thống kê QA",
            Bank = Reader.Parse("Câu 1. Câu QA?\nA. Một\nB. Hai\nĐáp án: B"),
            Setup = new QuizSetup { Count = 1, Included = new() { 1 } }
        });
        AdoptBank(source);
        Start(false);
        await Task.Delay(100);
        ChooseAnswer("B");
        Finish();
        Check(historyStore.Load(out _).Single().Questions[0].Seconds > 0, "New mock history lacks per-question timing");
        Navigate("home");
        Click(Find(page,"Xem thống kê"));
        for (int i = 0; i < 80 && !Controls<TextBlock>(page).Any(text => text.Text?.Contains("100%") == true); i++)
            await Task.Delay(40);
        Check(Controls<TextBlock>(page).Any(text => text.Text?.Contains("100%") == true), "Statistics did not render score");
        Controls<ComboBox>(page).Single(box => box.Name=="StatisticsPeriod").SelectedIndex=0;
        Controls<ComboBox>(page).Single(box => box.Name=="StatisticsBank").SelectedIndex=1;
        var host = (IClassicDesktopStyleApplicationLifetime)Avalonia.Application.Current.ApplicationLifetime;
        host.MainWindow.MinWidth=320;
        host.MainWindow.Width=360;
        await Capture("statistics-360");
        preferences.Theme="dark";ApplyTheme();
        await Capture("statistics-dark");
        Check(Controls<TextBlock>(page).Any(text => text.Text?.Contains("100%") == true), "Filters lost valid result");
        return "PASS real timed mock, statistics entry, 7-day and bank filters, narrow light and dark desktop preview.";
    }
}
#endif
