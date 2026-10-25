using Avalonia;
using QuizDesk.Mobile;
internal static class Program {
    [STAThread] static void Main(string[] args) {
#if QA
        MobilePlatform.DataDirectory=Environment.GetEnvironmentVariable("QUIZDESK_MOBILE_QA_DIR")??Path.Combine(AppContext.BaseDirectory,"preview-data");
#else
        MobilePlatform.DataDirectory=Path.Combine(AppContext.BaseDirectory,"preview-data");
#endif
        AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace().StartWithClassicDesktopLifetime(args);
    }
}
