#if QA
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace QuizDesk.Mobile;
public partial class MainView {
    async Task<string> VerifyAero(){
        await Task.Delay(200);
        Check(aeroTopLevel!=null&&aeroWindow!=null,"Glass QA requires attached desktop/preview window");
        var window=aeroWindow;
        foreach(string theme in new[]{"light","dark"}){
            preferences.Theme=theme;ApplyTheme();await Task.Delay(120);
            Navigate("preferences");
            var toggle=Controls<ToggleSwitch>(page).Single(c=>c.Name=="AeroGlass");
            var brightness=Controls<Slider>(page).Single(c=>c.Name=="GlassBrightness");
            var transparency=Controls<Slider>(page).Single(c=>c.Name=="GlassTransparency");
            toggle.IsChecked=false;await Task.Delay(120);
            Check(!brightness.IsEffectivelyEnabled&&!transparency.IsEffectivelyEnabled,"Glass sliders should be disabled while glass is off");
            Check(!aeroPaletteInstalled&&!glassBackdrop.IsVisible&&!preferencesStore.Load(out _).AeroGlass,"Glass off did not restore solid theme/persist");
            Check(window.TransparencyLevelHint.SequenceEqual(new[]{WindowTransparencyLevel.None}),"Glass off still requests native blur");
            Check(Background is ISolidColorBrush solid&&solid.Color.A==255,"Glass off leaves root transparent");
            toggle.IsChecked=true;await Task.Delay(180);
            Check(preferencesStore.Load(out _).AeroGlass&&aeroPaletteInstalled,"Glass on preference/palette not restored");
            bool native=MobilePlatform.DesktopHost&&(OperatingSystem.IsWindows()||OperatingSystem.IsMacOS())&&(window.ActualTransparencyLevel==WindowTransparencyLevel.AcrylicBlur||window.ActualTransparencyLevel==WindowTransparencyLevel.Blur);
            Check(glassBackdrop.IsVisible==!native,"Portable glass backdrop must replace missing native blur");
            Check(Background is ISolidColorBrush background&&background.Color.A<255,"Glass palette is not translucent");
            if(glassBackdrop.IsVisible){
                Check(glassBackdrop.Background is LinearGradientBrush gradient&&gradient.GradientStops.All(s=>s.Color.A==255),"Portable glass backdrop must remain opaque behind translucent panels");
            }
            Check(brightness.IsEffectivelyEnabled&&transparency.IsEffectivelyEnabled,"Glass sliders did not re-enable");
            var palette=(ResourceDictionary)aeroPalette.ThemeDictionaries[theme=="dark"?Avalonia.Styling.ThemeVariant.Dark:Avalonia.Styling.ThemeVariant.Light];
            var card=(SolidColorBrush)palette["QDContainerLow"];
            string before=File.ReadAllText(preferencesStore.FilePath);
            transparency.Value=0;Check(card.Color.A==255,"Minimum transparency must give an opaque card");
            transparency.Value=100;Check(card.Color.A<255&&card.Color.A>=180,"Maximum transparency did not adjust alpha or discarded readability floor");
            byte alpha=card.Color.A;
            brightness.Value=-50;var darker=card.Color;
            brightness.Value=50;var lighter=card.Color;
            Check(lighter.R>darker.R&&lighter.G>darker.G&&lighter.B>darker.B&&lighter.A==alpha,"Brightness must affect tint independently of transparency");
            brightness.Value=0;var neutral=card.Color;
            brightness.Value=25;brightness.Value=0;Check(card.Color==neutral,"Repeated slider changes accumulate tint errors");
            brightness.Value=20;transparency.Value=70;
            Check(File.ReadAllText(preferencesStore.FilePath)==before,"Slider drag writes synchronously instead of debouncing");
            await Task.Delay(320);
            var saved=preferencesStore.Load(out _);
            Check(saved.GlassBrightness==20&&saved.GlassTransparency==70,"Debounced slider values were not persisted");
            Check(!glassBackdrop.IsVisible||((LinearGradientBrush)glassBackdrop.Background).GradientStops.All(s=>s.Color.A==255),"Slider made the portable backdrop transparent");
            await Capture("glass-settings-"+theme);
            Navigate("home");await Capture("glass-home-"+theme);
        }
        window.MinWidth=320;
        foreach(int width in new[]{320,360,1280}){
            window.Width=width;await Task.Delay(120);
            double left=Bounds.Width>=840?96:0;
            Check(appBar.Margin==default&&Math.Abs(appBar.Bounds.X-left)<1&&Math.Abs(appBar.Bounds.Right-Bounds.Width)<1&&Math.Abs(appBar.Bounds.Y)<1,"Header glass has an inset at width "+width);
            Check(appBar.Children.Single() is Grid inset&&inset.Margin==new Thickness(20,4,12,4),"Header controls lost their inner spacing");
            Check(!glassBackdrop.IsVisible||glassBackdrop.Bounds.Size==outer.Bounds.Size,"Portable backdrop does not cover viewport");
            await Capture("glass-width-"+width);
        }
        Navigate("preferences");
        Controls<Slider>(page).Single(c=>c.Name=="GlassBrightness").Value=-17;
        Controls<Slider>(page).Single(c=>c.Name=="GlassTransparency").Value=81;
        PersistSession();
        var paused=preferencesStore.Load(out _);Check(paused.GlassBrightness==-17&&paused.GlassTransparency==81,"App pause must flush pending slider settings");
        Controls<Slider>(page).Single(c=>c.Name=="GlassBrightness").Value=12;
        var content=window.Content;window.Content=null;await Task.Delay(50);
        Check(preferencesStore.Load(out _).GlassBrightness==12,"View detach must flush pending slider settings");
        Check(aeroTopLevel==null&&aeroWindow==null&&!aeroPaletteInstalled&&!glassBackdrop.IsVisible,"Glass detach leaked resources");
        window.Content=content;await Task.Delay(100);
        Check(aeroWindow==window&&aeroTopLevel==window,"Glass did not reattach");
        return "PASS shared glass: brightness/transparency sliders, live tint/alpha, no accumulation, debounce, pause/detach flush, light/dark, on/off, saved preference, native/portable backdrop, opaque fallback, header edge coverage at 320/360/1280, detach/reattach. Desktop host: "+MobilePlatform.DesktopHost+"; actual effect: "+window.ActualTransparencyLevel;
    }
}
#endif
