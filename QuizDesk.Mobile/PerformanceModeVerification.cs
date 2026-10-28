#if QA
using Avalonia.Controls;
using Avalonia.Media;

namespace QuizDesk.Mobile;
public partial class MainView {
    async Task<string> VerifyPerformanceMode(string phase){
        if(phase=="performance-mode-restart"){
            Check(preferences.PerformanceMode,"Performance Mode did not survive restart");
            Navigate("preferences");await Task.Delay(60);
            Check(!aeroPaletteInstalled&&!glassBackdrop.IsVisible&&ReduceVisualMotion,"Restart still enabled visual effects");
            Check(!glassSettingsPanel.IsEffectivelyEnabled&&!motionSetting.IsEffectivelyEnabled,"Restart did not disable overridden settings");
            Check(headerDateTime.Text.Count(c=>c==':')==1&&page.Transitions==null,"Restart still renders seconds/animations");
            Check(preferences.AeroGlass&&!preferences.ReducedMotion&&preferences.GlassBrightness==20&&preferences.GlassTransparency==70,"Restart overwrote original visual preferences");
            return "PASS Performance Mode restored on a fresh process with solid UI/minute clock; original glass/motion preferences preserved.";
        }
        preferences.PerformanceMode=false;preferences.AeroGlass=true;preferences.ReducedMotion=false;
        preferences.GlassBrightness=20;preferences.GlassTransparency=70;UpdateAero();
        Navigate("preferences");
        var toggle=Controls<ToggleSwitch>(page).Single(t=>t.Name=="PerformanceMode");
        toggle.IsChecked=true;await Task.Delay(80);
        Check(preferencesStore.Load(out _).PerformanceMode,"Performance Mode was not persisted");
        Check(!aeroPaletteInstalled&&!glassBackdrop.IsVisible&&Background is ISolidColorBrush solid&&solid.Color.A==255,"Performance Mode still renders glass");
        if(aeroWindow!=null)Check(aeroWindow.TransparencyLevelHint.SequenceEqual(new[]{WindowTransparencyLevel.None}),"Native blur still requested");
        Check(!glassSettingsPanel.IsEffectivelyEnabled&&!motionSetting.IsEffectivelyEnabled,"Visual override controls remain enabled");
        Check(headerDateTime.Text.Count(c=>c==':')==1&&headerDateTimeTimer.Interval<=TimeSpan.FromMinutes(1),"Header did not switch to minute updates");
        Check(timer.Interval==TimeSpan.FromSeconds(1),"Performance Mode changed exam timer accuracy");
        var before=preferencesStore.Load(out _);
        Check(before.AeroGlass&&!before.ReducedMotion&&before.GlassBrightness==20&&before.GlassTransparency==70,"Performance Mode destroyed visual preferences");
        await Capture("performance-mode-settings");
        Navigate("home");Check(page.Transitions==null&&page.Opacity==1,"Performance Mode navigation still animates");
        bank=Enumerable.Range(1,3).Select(n=>new Question {Number=n,Text="Performance Mode "+n,Answer="A",Options=new(){{"A","One"},{"B","Two"}}}).ToList();
        setup=new(){Count=3,Minutes=20,ShuffleQuestions=false,ShuffleOptions=false,Included=new(bank.Select(q=>q.Number))};
        AdoptBank(library.Create(BankSnapshot()));Start(false);MoveToQuestion(1);ChooseAnswer("A");
        Check(questionText.Transitions==null&&answerMarkers["A"].Transitions==null&&workspace.LoadActive(out _).Answers.Count==1,"Mode changed exam state or still animates answers");
        timer.Stop();active=false;workspace.Complete(activeAttemptId);navigation.IsEnabled=true;
        Navigate("preferences");toggle=Controls<ToggleSwitch>(page).Single(t=>t.Name=="PerformanceMode");
        toggle.IsChecked=false;await Task.Delay(80);
        Check(aeroPaletteInstalled&&glassSettingsPanel.IsEffectivelyEnabled&&motionSetting.IsEffectivelyEnabled,"Disabling mode did not restore glass/settings");
        Check(!ReduceVisualMotion&&headerDateTime.Text.Count(c=>c==':')==2&&headerDateTimeTimer.Interval==TimeSpan.FromSeconds(1),"Disabling mode did not restore motion/second clock");
        Navigate("home");Check(page.Transitions!=null,"Disabling mode did not restore navigation animation");
        Navigate("preferences");Controls<ToggleSwitch>(page).Single(t=>t.Name=="PerformanceMode").IsChecked=true;
        return "PASS Performance Mode live on/off: solid/no blur, animation suppression, minute-aligned header, unchanged exam timer and checkpoint, persistent independent visual preferences and restoration.";
    }
}
#endif
