using Avalonia;
using Avalonia.Controls;

namespace QuizDesk.Mobile;
public partial class MainView {
    void AdaptLayout() {
        bool wide=Bounds.Width>=840;if(wideLayout==wide)return;wideLayout=wide;
        shell.RowDefinitions=new RowDefinitions(wide?"Auto,*":"Auto,*,Auto");
        shell.ColumnDefinitions=new ColumnDefinitions(wide?"96,*":"*");
        Grid.SetRow(appBar,0);Grid.SetColumn(appBar,wide?1:0);
        Grid.SetRow(contentContainer,1);Grid.SetColumn(contentContainer,wide?1:0);
        Grid.SetRow(navigation,wide?0:2);Grid.SetColumn(navigation,0);Grid.SetRowSpan(navigation,wide?2:1);
        navigationGrid.ColumnDefinitions=new ColumnDefinitions(wide?"*":"*,*,*,*");
        navigationGrid.RowDefinitions=new RowDefinitions(wide?"Auto,Auto,Auto,Auto":"*");
        navigationGrid.Margin=wide?new Thickness(8,20,8,0):default;
        int i=0;foreach(var key in new[]{"home","bank","setup","history"}){var button=navButtons[key];Grid.SetColumn(button,wide?0:i);Grid.SetRow(button,wide?i:0);i++;}
    }
}
