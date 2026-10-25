using Avalonia;
using Avalonia.Controls;
using QuizDesk;
using QuizDesk.Mobile;

internal static class Program {
    [STAThread] static void Main(string[] args) {
        MobilePlatform.DesktopHost=true;
#if QA
        MobilePlatform.DataDirectory=Environment.GetEnvironmentVariable("QUIZDESK_MOBILE_QA_DIR")??Path.Combine(AppContext.BaseDirectory,"preview-data");
#else
        MobilePlatform.DataDirectory=Path.GetDirectoryName(new PreferencesStore().FilePath);
#endif
        using var instance=new DesktopInstance(MobilePlatform.DataDirectory);
        string[] paths=args.Where(a=>a.EndsWith(".quiz",StringComparison.OrdinalIgnoreCase)).Select(Path.GetFullPath).Take(16).ToArray();
        if(!instance.IsPrimary){try{Deliver(paths).GetAwaiter().GetResult();instance.NotifyPrimary();}catch(Exception ex){File.WriteAllText(Path.Combine(MobilePlatform.DataDirectory,"last-open-error.txt"),ex.ToString());}return;}
        instance.Listen(MobilePlatform.SignalIncoming);Deliver(paths).GetAwaiter().GetResult();
        AppBuilder.Configure<DesktopApp>().UsePlatformDetect().WithInterFont().LogToTrace().StartWithClassicDesktopLifetime(args);
    }
    static async Task Deliver(string[] paths) {
        foreach(string path in paths)try{await using var input=File.OpenRead(path);await MobilePlatform.ReceiveQuiz(input,Path.GetFileName(path));}
        catch(Exception ex){MobilePlatform.IncomingError="Không mở được file: "+ex.Message;}
        MobilePlatform.SignalIncoming();
    }
}
public sealed class DesktopApp : App {
    protected override Window CreateDesktopWindow() {
        var window=new Window {Title="QuizDesk 5.11.0",Width=1280,Height=860,MinWidth=720,MinHeight=600};
        try{window.Icon=new WindowIcon(Path.Combine(AppContext.BaseDirectory,"QuizDesk.ico"));}catch(IOException){}
        window.Closing+=(_,_)=>CurrentView?.PersistSession();
        window.Deactivated+=(_,_)=>CurrentView?.PersistSession();
        window.Activated+=(_,_)=>CurrentView?.ResumeSession();return window;
    }
}
