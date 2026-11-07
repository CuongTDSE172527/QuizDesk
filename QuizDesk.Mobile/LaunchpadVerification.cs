#if QA
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace QuizDesk.Mobile;

public partial class MainView {
    async Task<string> VerifyLaunchpad() {
        preferences.PerformanceMode = true;
        ApplyPerformanceMode();
        var window = ((IClassicDesktopStyleApplicationLifetime)Avalonia.Application.Current.ApplicationLifetime).MainWindow;
        window.MinWidth = 320;
        window.Width = 360;
        window.Height = 860;
        await Capture("launchpad-empty-360");
        Click(Find(page, "Thử 2 câu mẫu"));
        for (int i = 0; i < 50 && !active; i++) await Task.Delay(50);
        Check(active && practice && exam.Count == 2 && currentPage == "setup", "Sample did not open practice directly");
        var id = currentBankId;
        ChooseAnswer(exam[0].Answer);
        MoveToQuestion(1);
        ExitPractice();
        Navigate("home");
        await Capture("launchpad-resume-360");
        Check(Find(page, "Tiếp tục ôn tập").Bounds.Width <= page.Bounds.Width, "Resume overflows narrow page");
        Click(Find(page, "Tiếp tục ôn tập"));
        Check(active && practice && index == 1 && answers.ContainsKey(0) && currentBankId == id, "Home resume lost position, answer or selected bank");
        ExitPractice();
        Navigate("home");
        Click(Find(page, "Học flashcards"));
        Check(studying && study.BankId == id, "Home flashcards used wrong bank");
        ExitStudy();
        Navigate("home");
        Click(Find(page, "Tiếp tục flashcards"));
        Check(studying && study.BankId == id, "Home lost saved flashcards");
        ExitStudy();
        Navigate("home");
        Click(Find(page, "Thiết lập bài thi"));
        Check(currentPage == "setup" && !active, "Mock started before configuration");
        Navigate("home");
        window.Width = 1280;
        await Capture("launchpad-resume-desktop");
        preferences.Theme = "dark";
        ApplyTheme();
        await Capture("launchpad-resume-dark");
        Click(Find(page, "Mở thư viện"));
        Check(currentPage == "bank" && library.List(out _).Single().Id == id, "Home navigation duplicated or lost bank");
        return "PASS sample starts practice, position and answer resume from Home, flashcards start and resume, mock opens settings, library preserved, narrow and wide Home rendered.";
    }
}
#endif
