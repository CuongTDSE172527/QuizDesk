using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace QuizDesk.Mobile;

public partial class MainView {
    Window aeroWindow;
    readonly ResourceDictionary aeroPalette=CreateAeroPalette();
    bool aeroPaletteInstalled,updatingAero;

    static ResourceDictionary CreateAeroPalette(){
        static ResourceDictionary Palette(string surface,string card,string navigation,string high,string outline,string shine)=>new(){
            ["QDSurface"]=new SolidColorBrush(Color.Parse(surface)),
            ["QDContainerLow"]=new SolidColorBrush(Color.Parse(card)),
            ["QDContainer"]=new SolidColorBrush(Color.Parse(navigation)),
            ["QDContainerHigh"]=new SolidColorBrush(Color.Parse(high)),
            ["QDOutlineVariant"]=new SolidColorBrush(Color.Parse(outline)),
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
        resources.ThemeDictionaries[ThemeVariant.Light]=Palette("#66EDF5FF","#EBFFFFFF","#BDE0EDFF","#E0E7F0FB","#8085A9D0","#55FFFFFF");
        resources.ThemeDictionaries[ThemeVariant.Dark]=Palette("#66101A2B","#E01B283C","#AD1C2C43","#DE2A3A50","#668AACD5","#20C9E7FF");
        return resources;
    }

    void AttachAero(){
        if(!MobilePlatform.DesktopHost||!OperatingSystem.IsWindows()||aeroWindow!=null)return;
        aeroWindow=TopLevel.GetTopLevel(this) as Window;
        if(aeroWindow==null)return;
        aeroWindow.PropertyChanged+=AeroWindowChanged;
        UpdateAero();
    }

    void AeroWindowChanged(object sender,AvaloniaPropertyChangedEventArgs args){
        if(args.Property==TopLevel.ActualTransparencyLevelProperty||args.Property==TopLevel.ActualThemeVariantProperty)UpdateAero();
    }

    void UpdateAero(){
        if(aeroWindow==null||updatingAero)return;
        updatingAero=true;
        try {
            bool enabled=preferences.AeroGlass;
            aeroWindow.TransparencyBackgroundFallback=new SolidColorBrush(Color.Parse(aeroWindow.ActualThemeVariant==ThemeVariant.Dark?"#131720":"#F6F7FB"));
            aeroWindow.Background=Brushes.Transparent;
            aeroWindow.TransparencyLevelHint=enabled
                ?new[]{WindowTransparencyLevel.AcrylicBlur,WindowTransparencyLevel.Blur,WindowTransparencyLevel.None}
                :new[]{WindowTransparencyLevel.None};
            bool glass=enabled&&(aeroWindow.ActualTransparencyLevel==WindowTransparencyLevel.AcrylicBlur||aeroWindow.ActualTransparencyLevel==WindowTransparencyLevel.Blur);
            if(glass!=aeroPaletteInstalled){
                if(glass){Resources.MergedDictionaries.Add(aeroPalette);Tone(appBar,Panel.BackgroundProperty,"AeroShine");}
                else {Resources.MergedDictionaries.Remove(aeroPalette);appBar.ClearValue(Panel.BackgroundProperty);}
                aeroPaletteInstalled=glass;
            }
        } finally {updatingAero=false;}
    }

    void DetachAero(){
        if(aeroWindow!=null)aeroWindow.PropertyChanged-=AeroWindowChanged;
        if(aeroPaletteInstalled){Resources.MergedDictionaries.Remove(aeroPalette);appBar.ClearValue(Panel.BackgroundProperty);aeroPaletteInstalled=false;}
        aeroWindow=null;
    }
}
