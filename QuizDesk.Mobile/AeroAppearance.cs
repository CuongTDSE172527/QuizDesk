using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace QuizDesk.Mobile;

public partial class MainView {
    TopLevel aeroTopLevel;
    Window aeroWindow;
    readonly Grid glassBackdrop=new(){IsVisible=false,IsHitTestVisible=false};
    readonly ResourceDictionary aeroPalette=CreateAeroPalette();
    bool aeroPaletteInstalled,updatingAero;

    static LinearGradientBrush GlassGradient(params string[] colors){
        var brush=new LinearGradientBrush {
            StartPoint=new RelativePoint(0,0,RelativeUnit.Relative),
            EndPoint=new RelativePoint(1,1,RelativeUnit.Relative)
        };
        for(int i=0;i<colors.Length;i++)brush.GradientStops.Add(new GradientStop(Color.Parse(colors[i]),(double)i/(colors.Length-1)));
        return brush;
    }

    static ResourceDictionary CreateAeroPalette(){
        static ResourceDictionary Palette(string surface,string card,string navigation,string high,string outline,string shine,string[] backdrop)=>new(){
            ["QDSurface"]=new SolidColorBrush(Color.Parse(surface)),
            ["QDContainerLow"]=new SolidColorBrush(Color.Parse(card)),
            ["QDContainer"]=new SolidColorBrush(Color.Parse(navigation)),
            ["QDContainerHigh"]=new SolidColorBrush(Color.Parse(high)),
            ["QDOutlineVariant"]=new SolidColorBrush(Color.Parse(outline)),
            ["QDGlassBackdrop"]=GlassGradient(backdrop),
            ["QDAeroShine"]=new LinearGradientBrush {
                StartPoint=new RelativePoint(0,0,RelativeUnit.Relative),
                EndPoint=new RelativePoint(0,1,RelativeUnit.Relative),
                GradientStops=new GradientStops {
                    new GradientStop(Color.Parse(shine),0),
                    new GradientStop(Colors.Transparent,1)
                }
            }
        };
        var resources=new ResourceDictionary();
        resources.ThemeDictionaries[ThemeVariant.Light]=Palette("#66EDF5FF","#EBFFFFFF","#BDE0EDFF","#E0E7F0FB","#8085A9D0","#55FFFFFF",new[]{"#DDEBFF","#ECF7FA","#EEE7FA","#F4F7FC"});
        resources.ThemeDictionaries[ThemeVariant.Dark]=Palette("#66101A2B","#E01B283C","#AD1C2C43","#DE2A3A50","#668AACD5","#20C9E7FF",new[]{"#14213B","#263850","#302D4A","#141E30"});
        return resources;
    }

    void AttachAero(){
        if(aeroTopLevel!=null)return;
        aeroTopLevel=TopLevel.GetTopLevel(this);
        if(aeroTopLevel==null)return;
        aeroWindow=aeroTopLevel as Window;
        aeroTopLevel.PropertyChanged+=AeroWindowChanged;
        UpdateAero();
    }

    void AeroWindowChanged(object sender,AvaloniaPropertyChangedEventArgs args){
        if(args.Property==TopLevel.ActualTransparencyLevelProperty||args.Property==TopLevel.ActualThemeVariantProperty)UpdateAero();
    }

    void UpdateAero(){
        if(aeroTopLevel==null||updatingAero)return;
        updatingAero=true;
        try {
            bool enabled=preferences.AeroGlass;
            bool desktopNative=MobilePlatform.DesktopHost&&(OperatingSystem.IsWindows()||OperatingSystem.IsMacOS());
            if(aeroWindow!=null){
                aeroWindow.TransparencyBackgroundFallback=new SolidColorBrush(Color.Parse(aeroTopLevel.ActualThemeVariant==ThemeVariant.Dark?"#131720":"#F6F7FB"));
                aeroWindow.Background=Brushes.Transparent;
                aeroWindow.TransparencyLevelHint=!enabled||!desktopNative
                    ?new[]{WindowTransparencyLevel.None}
                    :OperatingSystem.IsWindows()
                        ?new[]{WindowTransparencyLevel.AcrylicBlur,WindowTransparencyLevel.Blur,WindowTransparencyLevel.None}
                        :new[]{WindowTransparencyLevel.Blur,WindowTransparencyLevel.None};
            }
            bool nativeBlur=desktopNative&&(aeroTopLevel.ActualTransparencyLevel==WindowTransparencyLevel.AcrylicBlur||aeroTopLevel.ActualTransparencyLevel==WindowTransparencyLevel.Blur);
            if(enabled!=aeroPaletteInstalled){
                if(enabled){
                    Resources.MergedDictionaries.Add(aeroPalette);
                    Tone(appBar,Panel.BackgroundProperty,"AeroShine");
                    Tone(glassBackdrop,Panel.BackgroundProperty,"GlassBackdrop");
                } else {
                    Resources.MergedDictionaries.Remove(aeroPalette);
                    appBar.ClearValue(Panel.BackgroundProperty);
                    glassBackdrop.ClearValue(Panel.BackgroundProperty);
                }
                aeroPaletteInstalled=enabled;
            }
            // Keep mobile/fallback rendering opaque behind the translucent UI.
            // No desktop wallpaper access, per-card blur or native mobile overlays.
            glassBackdrop.IsVisible=enabled&&!nativeBlur;
        } finally {updatingAero=false;}
    }

    void DetachAero(){
        if(aeroTopLevel!=null)aeroTopLevel.PropertyChanged-=AeroWindowChanged;
        if(aeroPaletteInstalled){
            Resources.MergedDictionaries.Remove(aeroPalette);
            appBar.ClearValue(Panel.BackgroundProperty);
            glassBackdrop.ClearValue(Panel.BackgroundProperty);
            aeroPaletteInstalled=false;
        }
        glassBackdrop.IsVisible=false;
        aeroTopLevel=null;aeroWindow=null;
    }
}
