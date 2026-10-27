using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;

namespace QuizDesk.Mobile;
public partial class MainView {
    readonly DispatcherTimer glassSettingsSaveTimer=new(){Interval=TimeSpan.FromMilliseconds(250)};
    bool glassSettingsSavePending;

    Control GlassSettings(){
        var glass=new ToggleSwitch {Name="AeroGlass",Content="Phong cách kính",IsChecked=preferences.AeroGlass};
        var brightness=new Slider {Name="GlassBrightness",Minimum=-50,Maximum=50,Value=preferences.GlassBrightness,TickFrequency=1,IsSnapToTickEnabled=true,SmallChange=1,LargeChange=10,MinHeight=40};
        var transparency=new Slider {Name="GlassTransparency",Minimum=0,Maximum=100,Value=preferences.GlassTransparency,TickFrequency=1,IsSnapToTickEnabled=true,SmallChange=1,LargeChange=10,MinHeight=40};
        AutomationProperties.SetName(brightness,"Độ sáng của kính");
        AutomationProperties.SetName(transparency,"Độ trong suốt của kính");
        var brightnessValue=Supporting(GlassBrightnessText(),13);
        var transparencyValue=Supporting(preferences.GlassTransparency+"%",13);
        static Control SliderField(string name,Slider slider,TextBlock value,string left,string right){
            var heading=new Grid {ColumnDefinitions=new ColumnDefinitions("*,Auto"),ColumnSpacing=12};
            heading.Children.Add(Text(name,14));Grid.SetColumn(value,1);heading.Children.Add(value);
            var ends=new Grid {ColumnDefinitions=new ColumnDefinitions("*,Auto")};
            ends.Children.Add(Supporting(left,12));var end=Supporting(right,12);Grid.SetColumn(end,1);ends.Children.Add(end);
            return new StackPanel {Spacing=4,Children={heading,slider,ends}};
        }
        var tuning=Stack(SliderField("Độ sáng của kính",brightness,brightnessValue,"Tối hơn","Sáng hơn"),SliderField("Độ trong suốt",transparency,transparencyValue,"Đặc hơn","Trong hơn"));
        tuning.IsEnabled=preferences.AeroGlass;
        brightness.ValueChanged+=(_,_)=>{
            preferences.GlassBrightness=(int)Math.Round(brightness.Value);
            brightnessValue.Text=GlassBrightnessText();ApplyGlassTuning();QueueGlassSave();
        };
        transparency.ValueChanged+=(_,_)=>{
            preferences.GlassTransparency=(int)Math.Round(transparency.Value);
            transparencyValue.Text=preferences.GlassTransparency+"%";ApplyGlassTuning();QueueGlassSave();
        };
        glass.IsCheckedChanged+=(_,_)=>{
            preferences.AeroGlass=glass.IsChecked==true;tuning.IsEnabled=preferences.AeroGlass;
            UpdateAero();glassSettingsSaveTimer.Stop();glassSettingsSavePending=false;SavePreferences();
        };
        return Card(Stack(glass,tuning,Supporting("Áp dụng ngay khi kéo và tự lưu. Mức trong suốt điều chỉnh lớp nền kính; chữ giữ nguyên độ rõ. Trên mobile, hiệu ứng nằm trong app.")));
    }

    string GlassBrightnessText()=>preferences.GlassBrightness>0?"+"+preferences.GlassBrightness+"%":preferences.GlassBrightness+"%";
    void QueueGlassSave(){glassSettingsSavePending=true;glassSettingsSaveTimer.Stop();glassSettingsSaveTimer.Start();}
    void GlassSaveTick(object sender,EventArgs args)=>FlushGlassSettings();
    void FlushGlassSettings(){
        glassSettingsSaveTimer.Stop();
        if(!glassSettingsSavePending)return;
        glassSettingsSavePending=false;SavePreferences();
    }
}
