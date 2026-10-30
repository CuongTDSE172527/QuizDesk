#if QA
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
namespace QuizDesk.Mobile;
public partial class MainView {
    async Task<string> VerifyFlags(string phase) {
        if(phase=="flags-restart") {
            Check(active&&!practice&&index==54&&flaggedQuestions.SetEquals(new[]{0,54}),"Flags/index not restored in fresh process");
            Check(answers.GetValueOrDefault(0)=="A","Flag save affected answers");
            Check(jumpButtons[4].Content.ToString()=="55 ★","Restored jump marker missing");
            Click(nextFlagged);Check(index==0,"Flag next does not wrap across pages");
            flagQuestion.RaiseEvent(new RoutedEventArgs(ToggleButton.ClickEvent));
            Check(!flaggedQuestions.Contains(0)&&!jumpButtons[0].Content.ToString().Contains("★"),"Unflag did not clear marker");
            Click(nextFlagged);Check(index==54,"Next did not skip cleared flag");
            flagQuestion.RaiseEvent(new RoutedEventArgs(ToggleButton.ClickEvent));Check(!nextFlagged.IsEnabled,"Empty flag navigation enabled");
            Start(false);Check(flaggedQuestions.Count==0,"New mock retained flags");
            Start(true);Check(flagQuestion==null&&nextFlagged==null,"Practice exposes mock flags");ExitPractice();
            return "PASS fresh-process flags/answers/index; cross-page wrap; unflag; empty state; new attempt reset; practice unchanged.";
        }
        bank=Enumerable.Range(1,60).Select(n=>new Question {Number=n,Text="Flag test "+n,Answer="A",Options=new(){{"A","Correct"},{"B","Other"}}}).ToList();
        setup=new(){Count=60,Minutes=30,Included=new(bank.Select(q=>q.Number)),ShuffleQuestions=false,ShuffleOptions=false};
        title="Đánh dấu xem lại";AdoptBank(library.Create(BankSnapshot()));RememberSelectedBank();Start(false);
        flagQuestion.RaiseEvent(new RoutedEventArgs(ToggleButton.ClickEvent));ChooseAnswer("A");
        Check(flaggedQuestions.Contains(0)&&answers.Count==1&&jumpButtons[0].Content.ToString()=="1 ★","Answer cleared flag or marker missing");
        MoveToQuestion(54);flagQuestion.RaiseEvent(new RoutedEventArgs(ToggleButton.ClickEvent));
        var saved=workspace.LoadActive(out _);Check(saved.Flagged.SetEquals(new[]{0,54})&&saved.Index==54,"Flag checkpoint not durable");
        Click(nextFlagged);Check(index==0,"Next flag did not wrap");Click(nextFlagged);Check(index==54,"Next flag did not change navigator page");
        await Capture("flags-mock");PersistSession();
        return "PASS mock flags preserve answers and save both pages; screenshot captured; ready for fresh-process restore.";
    }
}
#endif
