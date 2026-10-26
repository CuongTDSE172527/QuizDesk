namespace QuizDesk.Mobile;
public static class AppVersion {
    public static string Display=>typeof(AppVersion).Assembly.GetName().Version?.ToString(3)??"unknown";
}
