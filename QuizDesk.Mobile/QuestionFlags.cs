using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace QuizDesk.Mobile;
public partial class MainView {
    readonly HashSet<int> flaggedQuestions=new();
    ToggleButton flagQuestion;
    Button nextFlagged;
    void CreateFlagControls() {
        flagQuestion=null;nextFlagged=null;
        if(practice)return;
        flagQuestion=new ToggleButton {Name="FlagQuestion",MinHeight=48,Padding=new Avalonia.Thickness(12,10)};
        flagQuestion.Click+=(_,_)=>{
            if(!active||practice)return;
            if(!flaggedQuestions.Add(index))flaggedQuestions.Remove(index);
            UpdateJumpState();Checkpoint();
        };
        nextFlagged=Btn("Câu đánh dấu tiếp →",()=>{
            if(!active||practice||flaggedQuestions.Count==0)return;
            int next=flaggedQuestions.Where(i=>i>index).DefaultIfEmpty(flaggedQuestions.Min()).Min();
            MoveToQuestion(next);
        });
        page.Children.Add(Row(flagQuestion,nextFlagged));
    }
    void UpdateFlagControls() {
        if(flagQuestion==null)return;
        flagQuestion.IsChecked=flaggedQuestions.Contains(index);
        flagQuestion.Content=flagQuestion.IsChecked==true?"★ Bỏ đánh dấu câu này":"☆ Đánh dấu xem lại";
        Avalonia.Automation.AutomationProperties.SetName(flagQuestion,flagQuestion.Content.ToString());
        nextFlagged.Content=$"Câu đánh dấu tiếp ({flaggedQuestions.Count}) →";
        nextFlagged.IsEnabled=flaggedQuestions.Count>0;
    }
}
