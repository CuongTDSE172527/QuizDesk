using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace QuizDesk.Mobile;
public partial class MainView {
    const int JumpPageSize=50;
    int jumpStart=-1;
    TextBlock jumpRange;
    Button jumpPrevious,jumpNext;
    void CreateJumpNavigator(){
        jumpStart=-1;var wrap=new WrapPanel();
        for(int i=0;i<Math.Min(JumpPageSize,exam.Count);i++){
            var button=new ToggleButton {MinWidth=48,MinHeight=48,Margin=new Thickness(0,0,6,6),Padding=new Thickness(6,10),BorderThickness=new Thickness(2)};
            button.Classes.Add("jump");button.Click+=(_,_)=>{if(button.Tag is int target)MoveToQuestion(target);};
            jumpButtons.Add(button);wrap.Children.Add(button);
        }
        if(exam.Count>JumpPageSize){
            jumpRange=Text("",13);jumpPrevious=Btn("← 50 câu",()=>MoveToQuestion(Math.Max(0,jumpStart-JumpPageSize)));
            jumpNext=Btn("50 câu →",()=>MoveToQuestion(Math.Min(exam.Count-1,jumpStart+JumpPageSize)));
            var destination=Number(index+1,exam.Count);
            page.Children.Add(Stack(jumpRange,Row(jumpPrevious,jumpNext),Field("Chuyển thẳng đến câu",destination),Btn("Đến câu",()=>MoveToQuestion(Value(destination)-1))));
        }else{jumpRange=null;jumpPrevious=null;jumpNext=null;}
        page.Children.Add(new ScrollViewer {Content=wrap,MaxHeight=180});
    }
    void UpdateJumpState(){
        int start=index/JumpPageSize*JumpPageSize;
        bool changed=start!=jumpStart;jumpStart=start;
        for(int i=0;i<jumpButtons.Count;i++){
            int target=start+i;var button=jumpButtons[i];button.IsVisible=target<exam.Count;
            if(changed){button.Tag=target;button.Content=(target+1).ToString();Avalonia.Automation.AutomationProperties.SetName(button,"Câu "+(target+1));}
            button.IsChecked=answers.ContainsKey(target);button.Classes.Set("current",target==index);
        }
        if(jumpRange!=null){jumpRange.Text=$"Câu {start+1}–{Math.Min(exam.Count,start+JumpPageSize)} / {exam.Count}";jumpPrevious.IsEnabled=start>0;jumpNext.IsEnabled=start+JumpPageSize<exam.Count;}
    }
}
