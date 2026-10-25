#if QA
using Avalonia.Controls;
namespace QuizDesk.Mobile;
public partial class MainView {
    async Task<string> VerifyMerge(string phase){
        if(phase=="merge-restart"){
            var resumed=library.Load(Guid.Parse(File.ReadAllText(Path.Combine(dataRoot,"merge-result-id.txt"))));Check(resumed.Content.Bank.Count==4&&resumed.Activities.Any(a=>a.Kind=="merged"),"Merged bank/audit not restored");
            Check(library.List(out _).Count==3&&historyStore.Load(out _).Count==1,"Merge changed originals/history across restart");
            AdoptBank(resumed);RememberSelectedBank();Navigate("setup");Start(false);Check(exam.Count==4&&exam.All(q=>q.Note=="Giải thích "+q.Text),"Merged bank cannot build exam with aligned explanations");Finish();
            return "PASS merged bank, sources and timestamped audit survive restart; source attempt history retained; merged bank builds an exam with aligned answers/notes.";
        }
        WorkspaceBank Fixture(string name,string prefix)=>new(){Title=name,Bank=Enumerable.Range(1,2).Select(i=>new Question {Number=i*7,Text=prefix+i,Answer="B",Note="Giải thích "+prefix+i,Options=new(){{"A","Một"},{"B","Hai"}}}).ToList(),Setup=new(){Count=2,Minutes=30,ShuffleQuestions=false,Included=new(){7,14}}};
        var a=library.Create(Fixture("Nguồn A","A"),"a.docx",true);var b=library.Create(Fixture("Nguồn B","B"),"b.docx",true);
        AdoptBank(a);RememberSelectedBank();Start(false);ChooseAnswer("B");Finish();Check(historyStore.Load(out _).Count==1,"History fixture absent");Navigate("bank");Click(Find(page,"Chọn bộ đề"));
        Check(!mergeBanksButton.IsEnabled,"Merge enabled with empty selection");libraryChecks[a.Id].IsChecked=true;Check(!mergeBanksButton.IsEnabled&&deleteBanksButton.IsEnabled,"Single-source selection actions incorrect");libraryChecks[b.Id].IsChecked=true;
        Check(mergeBanksButton.IsEnabled&&bankSelectionSummary.Text.Contains("4"),"Selection count/merge state incorrect");await Capture("merge-01-selected");
        Click(mergeBanksButton);await Task.Delay(80);Check(currentDialog!=null,"Merge dialog not opened");await Capture("merge-02-dialog");
        Click(Find(modal,"Hủy"));await Task.Delay(80);Check(library.List(out _).Count==2&&selectedBanks.Count==2,"Cancel changed sources or lost selection");
        Click(mergeBanksButton);await Task.Delay(60);var name=Controls<TextBox>(modal).Single(t=>t.Name=="MergedBankTitle");name.Text=" ";Click(Find(modal,"Tạo bộ đề gộp"));await Task.Delay(50);
        Check(currentDialog!=null&&library.List(out _).Count==2,"Empty title created bank");name.Text="Bộ tổng hợp QA";Controls<ToggleSwitch>(modal).Single(t=>t.Name=="MergeShuffle").IsChecked=false;
        Click(Find(modal,"Tạo bộ đề gộp"));for(int i=0;i<200&&currentDialog!=null;i++)await Task.Delay(20);
        Check(currentDialog==null&&currentBankId.HasValue&&bank.Count==4&&!selectingBanks,"Merge did not select newly created bank");
        Check(bank.Select(q=>q.Number).SequenceEqual(new[]{1,2,3,4})&&!setup.ShuffleQuestions,"Unshuffled merge numbering/config wrong");
        var saved=library.Load(currentBankId.Value);Check(saved.Activities.Any(a=>a.Kind=="merged"&&a.Changes.Count==4)&&library.List(out _).Count==3,"Merged creation/audit incomplete");
        Check(library.Load(a.Id).Content.Bank.Select(q=>q.Number).SequenceEqual(new[]{7,14})&&library.Load(b.Id).Content.Bank.Count==2&&historyStore.Load(out _).Count==1,"Merge changed source banks/history");
        Check(bank.All(q=>q.Answer=="B"&&q.Note=="Giải thích "+q.Text),"Merge mismatched keys/explanations");await Capture("merge-03-created");
        ShowBankActivity(saved.Id);await Task.Delay(70);Check(Controls<TextBlock>(modal).Any(t=>t.Text=="GỘP BỘ ĐỀ"),"Merge audit category not displayed");await Capture("merge-04-audit");Click(Find(modal,"Đóng"));await Task.Delay(60);
        Navigate("setup");StartStudy();Check(study.Cards.Count==4,"Merged flashcard count incorrect");ExitStudy();
        setup.Count=4;setup.ShuffleQuestions=true;PersistBank();File.WriteAllText(Path.Combine(dataRoot,"merge-result-id.txt"),saved.Id.ToString());PersistSession();
        return "PASS actual selection/cancel/empty-name/merge UI, unique numbering, answer-note association, source/history preservation, audit category/timestamp, flashcards and persisted merged setup.";
    }
}
#endif
