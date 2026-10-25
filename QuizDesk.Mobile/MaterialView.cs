using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace QuizDesk.Mobile;

public partial class MainView {
    static void Tone(Control target,AvaloniaProperty property,string role)=>target.Bind(property,target.GetResourceObservable("QD"+role));
    static readonly System.Collections.Concurrent.ConcurrentDictionary<string,Geometry> IconGeometry=new();
    static Control Icon(string name) {
        string data=name switch {
            "home"=>"M3 10 L12 3 L21 10 M5 9 V21 H10 V14 H14 V21 H19 V9",
            "bank"=>"M3 4 H9 Q12 4 12 7 Q12 4 15 4 H21 V20 H15 Q12 20 12 22 Q12 20 9 20 H3 Z M12 7 V22",
            "setup"=>"M8 4 H5 V22 H19 V4 H16 M8 2 H16 V6 H8 Z M8 11 H16 M8 15 H16 M8 19 H13",
            "history"=>"M3 11 A9 9 0 1 1 5 18 M3 4 V11 H10 M12 7 V12 L16 14",
            "preferences"=>"M4 6 H20 M4 12 H20 M4 18 H20 M8 3 V9 M16 9 V15 M10 15 V21",
            "help"=>"M12 22 A10 10 0 1 0 12 2 A10 10 0 1 0 12 22 M9 8 C9 4 17 5 15 10 L12 13 V15 M12 18 V19",
            "upload"=>"M12 16 V3 M7 8 L12 3 L17 8 M4 15 V21 H20 V15",
            "add"=>"M12 4 V20 M4 12 H20",
            "arrow"=>"M4 12 H20 M14 6 L20 12 L14 18",
            _=>"M5 5 H19 V19 H5 Z"
        };
        var path=new Avalonia.Controls.Shapes.Path {Data=IconGeometry.GetOrAdd(name,_=>Geometry.Parse(data)),StrokeThickness=1.8,StrokeLineCap=PenLineCap.Round,StrokeJoin=PenLineJoin.Round,Width=24,Height=24};
        Tone(path,Avalonia.Controls.Shapes.Shape.StrokeProperty,"OnSurfaceVariant");return path;
    }
    Button IconButton(string label,string icon,Action action) {
        var button=Btn(label,action);button.Classes.Add("icon");button.Content=Icon(icon);
        AutomationProperties.SetName(button,label);ToolTip.SetTip(button,label);return button;
    }
    Button NavButton(string key,string label) {
        var indicator=new Border {Child=Icon(key),Width=64,Height=32,CornerRadius=new CornerRadius(10),HorizontalAlignment=HorizontalAlignment.Center};
        indicator.Classes.Add("navIndicator");
        var caption=Text(label,12);caption.HorizontalAlignment=HorizontalAlignment.Center;caption.Classes.Add("supporting");
        var content=new StackPanel {Spacing=4,Children={indicator,caption}};
        var button=Btn(label,()=>Navigate(key));button.Classes.Add("nav");button.Content=content;
        AutomationProperties.SetName(button,label);navButtons[key]=button;return button;
    }
    static Control Chip(string text) {
        var label=Text(text,12);label.Classes.Add("primary");label.FontWeight=FontWeight.Medium;
        var chip=new Border {Child=label,Padding=new Thickness(12,6),CornerRadius=new CornerRadius(8),HorizontalAlignment=HorizontalAlignment.Left};
        Tone(chip,Border.BackgroundProperty,"SecondaryContainer");return chip;
    }
    static TextBlock Supporting(string text,double size=14) {var label=Text(text,size);label.Classes.Add("supporting");return label;}
    Control Metric(string value,string label) {
        var number=Text(value,30);number.Classes.Add("primary");
        return Card(Stack(number,Supporting(label,12)));
    }
    void SetDestination(string key) {
        foreach(var pair in navButtons) {
            pair.Value.Classes.Set("selected",pair.Key==key);
            // The text/icon navigation button keeps its transparent container.
            pair.Value.Classes.Set("accent",false);
        }
    }
}
