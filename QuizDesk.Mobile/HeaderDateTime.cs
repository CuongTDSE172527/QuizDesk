using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
namespace QuizDesk.Mobile;
public partial class MainView {
    readonly TextBlock headerDateTime=new(){Name="HeaderDateTime",FontSize=12,LineHeight=18,TextAlignment=TextAlignment.Right,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(8,0)};
    readonly DispatcherTimer headerDateTimeTimer=new(){Interval=TimeSpan.FromSeconds(1)};
    TextBlock brandName;
    void UpdateHeaderDateTime(){
        bool compact=Bounds.Width<600;
        headerDateTime.Width=compact?84:190;
        if(brandName!=null)brandName.IsVisible=Bounds.Width<=0||Bounds.Width>=380;
        var now=DateTimeOffset.Now;
        headerDateTimeTimer.Interval=preferences.PerformanceMode
            ?TimeSpan.FromMilliseconds(Math.Max(50,60000-now.Second*1000-now.Millisecond))
            :TimeSpan.FromSeconds(1);
        string format=preferences.PerformanceMode
            ?compact?"dd/MM/yyyy\nHH:mm":"dd/MM/yyyy · HH:mm"
            :compact?"dd/MM/yyyy\nHH:mm:ss":"dd/MM/yyyy · HH:mm:ss";
        string text=now.ToString(format,CultureInfo.InvariantCulture);
        if(headerDateTime.Text!=text)headerDateTime.Text=text;
    }
}
