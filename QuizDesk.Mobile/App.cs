using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;

namespace QuizDesk.Mobile;
public partial class App : Application {
    public MainView CurrentView {get;private set;}
    public override void Initialize(){Name="QuizDesk";Styles.Add(new FluentTheme());Styles.Add(new Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("avares://QuizDesk.Mobile/")){Source=new Uri("avares://QuizDesk.Mobile/QuizDeskTheme.axaml")});}
    MainView CreateView()=>CurrentView=new MainView();
    public override void OnFrameworkInitializationCompleted() {
        var activation=this.TryGetFeature<IActivatableLifetime>();if(activation!=null){activation.Activated+=ActivatedFromOutside;activation.Deactivated+=(_,_)=>CurrentView?.PersistSession();}
        if(ApplicationLifetime is IActivityApplicationLifetime activities)activities.MainViewFactory=CreateView;
        else if(ApplicationLifetime is ISingleViewApplicationLifetime single)single.MainView=CreateView();
        else if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop){var window=CreateDesktopWindow();window.Content=CreateView();desktop.MainWindow=window;}
        base.OnFrameworkInitializationCompleted();
    }
    protected virtual Avalonia.Controls.Window CreateDesktopWindow()=>new() {Title="QuizDesk Preview",Width=410,Height=850,MinWidth=320,MinHeight=500};
}
public static class MobilePlatform {
    public static Func<string,Task> PlayVideo {get;set;}
    public static bool DesktopHost {get;set;}
    public static Func<string,string,byte[],Task<bool>> ShareFile {get;set;}
    public static string DataDirectory {get;set;}
    public static event Action IncomingQuiz;
    public static string IncomingError {get;set;}
    public static bool BringToFront {get;set;}
    public static void SignalIncoming(){BringToFront=true;IncomingQuiz?.Invoke();}
    public static async Task ReceiveQuiz(Stream input,string name) {
        try{await new ExternalQuizInbox(DataDirectory??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QuizDesk")).AddAsync(input,name);}
        catch(Exception ex){IncomingError="KhÃ´ng nháº­n Ä‘Æ°á»£c file: "+ex.Message;}
        SignalIncoming();
    }
}
