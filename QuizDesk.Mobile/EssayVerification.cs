#if QA
using Avalonia.Controls;
namespace QuizDesk.Mobile;
public partial class MainView {
    async Task<string> VerifyEssay(string phase) {
        if(phase=="essay-restart") {
            Check(active&&!practice&&exam[index].IsEssay&&essayInput.Text=="Bài thi tự luận\n12µs","Fresh process lost essay input");
            Check(!essayReveal.IsVisible&&!questionFeedback.IsVisible,"Mock exposes model");Finish();
            var attempt=historyStore.Load(out _).Single();Check(attempt.Unscored==1&&attempt.Correct==1,"Essay automatically graded or MC score regressed");
            var essay=attempt.Questions.Single(q=>q.IsEssay);Check(essay.Picked=="Bài thi tự luận\n12µs"&&essay.Answer.Contains("Mẫu"),"History lost model or response");
            ShowAttempt(attempt);var panel=Controls<Expander>(page).First();panel.IsExpanded=true;await Task.Delay(100);
            Click(Find(page,"Tự chấm: Sai"));Check(historyStore.Load(out _).Single().Score==5,"Self grade incorrect not persisted");
            panel=Controls<Expander>(page).First();panel.IsExpanded=true;await Task.Delay(100);Click(Find(page,"Tự chấm: Đúng"));Check(historyStore.Load(out _).Single().Score==10,"Self grade correct not persisted");
            panel=Controls<Expander>(page).First();panel.IsExpanded=true;await Task.Delay(100);Click(Find(page,"Bỏ tự chấm"));Check(historyStore.Load(out _).Single().Unscored==1,"Reset self grading failed");
            await Capture("essay-history");return "PASS fresh process essay response, hidden model, mixed scoring/history and manual grade persistence/reset.";
        }
        var mixed=Reader.Parse("Câu 1. [Tự luận] Giải thích đơn vị?\nĐáp án: Mẫu: một phần triệu giây.\nCâu 2. Kết quả?\nA. 4\nB. 5\nĐáp án: B");
        AdoptBank(library.Create(new(){Title="Tự luận và trắc nghiệm",Bank=mixed,Setup=new(){Count=2,Minutes=30,Included=new(){1,2},ShuffleQuestions=false,ShuffleOptions=false}}));RememberSelectedBank();Navigate("bank");OpenLibraryBank(currentBankId.Value);
        Edit(bank[0]);await Task.Delay(80);Check(Controls<CheckBox>(modal).Single(c=>c.Name=="EssayType").IsChecked==true,"Editor missing essay type");
        Controls<TextBox>(modal).Single(c=>c.Name=="EssayModel").Text="Mẫu: 12µs và giải thích.";Click(Find(modal,"Lưu câu hỏi"));await Task.Delay(80);Check(bank[0].IsEssay&&bank[0].Answer.Contains("12µs"),"Essay editor did not save model");
        Start(true);Check(essayInput.IsVisible&&answerButtons.Values.All(b=>!b.IsVisible)&&!questionFeedback.IsVisible,"Essay UI wrong or model revealed early");
        essayInput.Text="Bài ôn\n12µs";Click(essayReveal);Check(questionFeedback.IsVisible&&feedbackText.Text.Contains("Mẫu"),"Practice model not revealed");
        MoveToQuestion(1);Check(!essayInput.IsVisible&&answerButtons["B"].IsVisible,"Mixed MC question lost choices");ChooseAnswer("B");MoveToQuestion(0);Check(essayInput.Text=="Bài ôn\n12µs","Moving lost essay draft");
        ExitPractice();Click(Find(page,"Tiếp tục ôn tập"));Check(essayInput.Text=="Bài ôn\n12µs","Saved practice lost essay draft");await Capture("essay-practice");ExitPractice();
        Start(false);essayInput.Text="Bài thi tự luận\n12µs";MoveToQuestion(1);ChooseAnswer("B");MoveToQuestion(0);PersistSession();
        Check(workspace.LoadActive(out _).Answers[0]==essayInput.Text,"Essay draft checkpoint missing");
        return "PASS essay editor, mixed practice, explicit reveal, saved practice, hidden mock model; ready for restart.";
    }
}
#endif
