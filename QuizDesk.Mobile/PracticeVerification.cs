#if QA
using Avalonia.Controls;
using Avalonia.Interactivity;
namespace QuizDesk.Mobile;
public partial class MainView {
    async Task<string> VerifyPractice(string phase){
        if(phase=="practice-restart"){
            Check(active&&practice&&index==1&&answers.GetValueOrDefault(1)=="B","Practice checkpoint did not resume correctly");
            Check(!Controls<Button>(page).Any(b=>Equals(b.Content,"Nộp bài")),"Restored practice exposes submission");Click(Find(page,"Thoát ôn tập"));
            Check(!active&&currentPage=="setup"&&workspace.LoadActive(out _)==null&&historyStore.Load(out _).Count==0,"Practice exit left checkpoint, result or history");
            Check(!Controls<Button>(page).Any(b=>Equals(b.Content,"Lưu kết quả")),"Practice exit displayed a scored result");
            var saved=PracticeWorkspace(currentBankId.Value).LoadActive(out _);
            Check(saved!=null&&saved.Index==1&&saved.Answers.GetValueOrDefault(1)=="B","Practice exit lost saved per-bank progress");
            Click(Find(page,"Tiếp tục ôn tập"));Check(active&&practice&&index==1&&questionFeedback.IsVisible,"Resume button did not restore feedback/index");ExitPractice();
            Start(false);Check(Controls<Button>(page).Any(b=>Equals(b.Content,"Nộp bài")),"Mock exam lost submission");ChooseAnswer("B");
            Click(Find(page,"Nộp bài"));await Task.Delay(60);Check(currentDialog!=null,"Mock submission confirmation missing");Click(Find(modal,"Nộp bài"));await Task.Delay(160);
            Check(!active&&historyStore.Load(out _).Count==1&&Controls<Button>(page).Any(b=>Equals(b.Content,"Lưu kết quả")),"Mock score/history flow regressed");await Capture("practice-mock-submission");
            return "PASS fresh process resumes practice index/answer; exit clears only practice checkpoint without score/history; mock exam retains confirmation, score and one history entry.";
        }
        bank=new(){new(){Number=1,Text="Ôn tập câu 1",Answer="B",Note="Giải thích vẫn hiển thị ngay.",Options=new(){{"A","Một"},{"B","Hai"}}},new(){Number=2,Text="Ôn tập câu 2",Answer="B",Options=new(){{"A","Một"},{"B","Hai"}}}};
        setup=new(){Count=2,Minutes=30,ShuffleQuestions=false,ShuffleOptions=false,Included=new(){1,2}};title="Ôn tập không nộp bài";AdoptBank(library.Create(BankSnapshot()));RememberSelectedBank();Start(true);
        Check(!Controls<Button>(page).Any(b=>Equals(b.Content,"Nộp bài"))&&Controls<Button>(page).Any(b=>Equals(b.Content,"Thoát ôn tập")),"Practice actions incorrect");ChooseAnswer("B");Check(questionFeedback.IsVisible&&feedbackText.Text.Contains("Chính xác"),"Practice feedback regressed");await Capture("practice-feedback");
        Navigate("home");Check(active&&noticeText.Text.Contains("Thoát ôn tập")&&!noticeText.Text.Contains("nộp"),"Practice navigation still requests submission");
        var back=new RoutedEventArgs();HandleBack(this,back);Check(back.Handled&&!active&&currentDialog==null&&historyStore.Load(out _).Count==0,"Back submitted practice or opened confirmation");
        var firstBank=currentBankId.Value;
        var firstSaved=PracticeWorkspace(firstBank).LoadActive(out _);
        Check(firstSaved!=null&&firstSaved.Answers.Count==1,"Back did not save practice");
        bank[0].Text="Nội dung đã sửa sau khi ôn";PersistBank();
        Click(Find(page,"Tiếp tục ôn tập"));Check(active&&exam[0].Text=="Ôn tập câu 1"&&answers.GetValueOrDefault(0)=="B","Resume did not retain original snapshot/answer");ExitPractice();
        StartNewPractice();await Task.Delay(80);Check(currentDialog!=null,"Replacing saved practice lacks confirmation");Click(Find(modal,"Hủy"));await Task.Delay(80);Check(!active,"Cancel started a replacement practice");
        var other=library.Create(new(){Title="Bộ ôn thứ hai",Bank=new(){new(){Number=1,Text="Bộ thứ hai",Answer="A",Options=new(){{"A","Đúng"},{"B","Sai"}}}},Setup=new(){Count=1,Included=new(){1}}});
        AdoptBank(other);RememberSelectedBank();Start(true);ChooseAnswer("A");ExitPractice();
        Check(PracticeWorkspace(firstBank).LoadActive(out _).Answers.GetValueOrDefault(0)=="B"&&PracticeWorkspace(other.Id).LoadActive(out _).Answers.GetValueOrDefault(0)=="A","Bank progress overwrote another bank");
        OpenLibraryBank(firstBank);Navigate("setup");Click(Find(page,"Tiếp tục ôn tập"));Check(exam.Count==2&&exam[0].Text=="Ôn tập câu 1","Wrong bank snapshot resumed");ExitPractice();
        Start(true);Finish();Check(!active&&currentPage=="setup"&&historyStore.Load(out _).Count==0,"Internal practice completion still builds score");
        Start(true);MoveToQuestion(1);ChooseAnswer("B");PersistSession();
        return "PASS practice has exit and no submit, instant feedback remains, navigation/back never requests submission, no scored result/history, active practice checkpoint ready for restart.";
    }
}
#endif
