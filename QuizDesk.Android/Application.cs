using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using QuizDesk.Mobile;

namespace QuizDesk.Android;
[Application]
public class Application : AvaloniaAndroidApplication<App> {
    public Application(nint handle,JniHandleOwnership ownership):base(handle,ownership){}
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) {
        MobilePlatform.DataDirectory=Path.Combine(FilesDir.AbsolutePath,"QuizDesk");
        return base.CustomizeAppBuilder(builder).WithInterFont();
    }
}
