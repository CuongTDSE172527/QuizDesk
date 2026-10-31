#if QA
using Avalonia.Controls;
using Avalonia.Media;
namespace QuizDesk.Mobile;
public partial class MainView {
    async Task<string> VerifyPracticeColors(string phase) {
        if(phase=="practice-colors-restart") {
            Check(active&&practice&&index==1,"Practice not restored");
            VerifyPracticeJumpColors();ExitPractice();
            Click(Find(page,"Tiếp tục ôn tập"));VerifyPracticeJumpColors();
            Start(false);ChooseAnswer("A");
            Check(jumpButtons.All(b=>!b.Classes.Contains("correct")&&!b.Classes.Contains("wrong")),"Mock reveals grading");
            return "PASS fresh-process and saved-practice colors restored; mock navigator stays neutral.";
        }
        preferences.AeroGlass=false;preferences.Theme="light";ApplyTheme();UpdateAero();
        bank=Enumerable.Range(1,60).Select(n=>new Question {Number=n,Text="Ôn tập màu "+n,Answer=n==3?"":"A",Options=new(){{"A","Đúng"},{"B","Sai"}}}).ToList();
        setup=new(){Count=60,Minutes=30,Included=new(bank.Select(q=>q.Number)),ShuffleQuestions=false,ShuffleOptions=false};
        title="Đúng sai trong ôn tập";AdoptBank(library.Create(BankSnapshot()));RememberSelectedBank();Start(true);
        ChooseAnswer("A");MoveToQuestion(1);ChooseAnswer("B");MoveToQuestion(2);ChooseAnswer("A");VerifyPracticeJumpColors();
        MoveToQuestion(54);Check(jumpButtons.All(b=>!b.Classes.Contains("correct")&&!b.Classes.Contains("wrong")),"Recycled page retained old result colors");
        MoveToQuestion(1);VerifyPracticeJumpColors();await Task.Delay(100);jumpButtons[0].BringIntoView();await Capture("practice-colors-light");
        preferences.Theme="dark";ApplyTheme();await Capture("practice-colors-dark");VerifyPracticeJumpColors();
        PersistSession();return "PASS correct/wrong/ungraded/unanswered, reused navigator pages, light/dark; checkpoint ready for restart.";
    }
    void VerifyPracticeJumpColors() {
        Check(jumpButtons[0].Classes.Contains("correct")&&jumpButtons[0].Content.ToString().Contains("✓"),"Correct jump missing green/check marker");
        Check(jumpButtons[1].Classes.Contains("wrong")&&jumpButtons[1].Content.ToString().Contains("×"),"Wrong jump missing red/cross marker");
        Check(!jumpButtons[2].Classes.Contains("correct")&&!jumpButtons[2].Classes.Contains("wrong"),"Ungraded answer colored");
        Check(!jumpButtons[3].Classes.Contains("correct")&&!jumpButtons[3].Classes.Contains("wrong"),"Unanswered question colored");
        bool dark=preferences.Theme=="dark";
        Check((jumpButtons[0].Background as ISolidColorBrush)?.Color==Color.Parse(dark?"#203D34":"#E4F4EC"),"Correct background not semantic green");
        Check((jumpButtons[1].Background as ISolidColorBrush)?.Color==Color.Parse(dark?"#492731":"#FCE9EC"),"Wrong background not semantic red");
    }
}
#endif
