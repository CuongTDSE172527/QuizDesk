#if QA
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Styling;

namespace QuizDesk.Mobile;
public partial class MainView {
    async Task<string> VerifyAero(){
        Check(MobilePlatform.DesktopHost&&OperatingSystem.IsWindows(),"Aero QA requires Windows desktop");
        await Task.Delay(200);
        Check(aeroWindow!=null,"Desktop window not attached to Aero controller");
        var window=aeroWindow;
        foreach(string theme in new[]{"light","dark"}){
            preferences.Theme=theme;ApplyTheme();await Task.Delay(120);
            Navigate("preferences");
            var toggle=Controls<ToggleSwitch>(page).Single(c=>c.Name=="AeroGlass");
            toggle.IsChecked=false;await Task.Delay(120);
            Check(!aeroPaletteInstalled&&!preferencesStore.Load(out _).AeroGlass,"Aero off did not restore solid theme/persist");
            Check(window.TransparencyLevelHint.SequenceEqual(new[]{WindowTransparencyLevel.None}),"Aero off still requests native blur");
            toggle.IsChecked=true;await Task.Delay(180);
            Check(preferencesStore.Load(out _).AeroGlass,"Aero on preference not persisted");
            bool supported=window.ActualTransparencyLevel==WindowTransparencyLevel.AcrylicBlur||window.ActualTransparencyLevel==WindowTransparencyLevel.Blur;
            Check(aeroPaletteInstalled==supported,"Aero resources must follow actual OS blur support");
            Check(Background is ISolidColorBrush background&&(supported?background.Color.A<255:background.Color.A==255),"Root background blocks glass or leaves fallback transparent");
            await Capture("aero-settings-"+theme);
            Navigate("home");await Capture("aero-home-"+theme);
        }
        var content=window.Content;window.Content=null;await Task.Delay(50);
        Check(aeroWindow==null&&!aeroPaletteInstalled,"Aero did not detach event handlers/resources");
        window.Content=content;await Task.Delay(100);
        Check(aeroWindow==window,"Aero did not reattach");
        if(Environment.GetEnvironmentVariable("QUIZDESK_AERO_PREVIEW")=="1"){
            preferences.Theme="light";ApplyTheme();Navigate("home");
            await Task.Delay(TimeSpan.FromSeconds(45));
        }
        return "PASS Windows desktop Aero: light/dark, on/off, saved preference, actual OS support/fallback, root alpha, detach/reattach. Actual effect: "+window.ActualTransparencyLevel;
    }
}
#endif
