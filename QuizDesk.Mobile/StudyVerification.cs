#if QA
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;

namespace QuizDesk.Mobile;
public partial class MainView {
    async Task<string> VerifyStudy(string phase){
        if(phase=="study-restart"){
            Check(studying&&study.Cards.Count==25&&study.Known.Count==2&&study.Position==2&&study.Revealed,"Study progress/snapshot/face not restored");
            Check(flashContent.Text==study.Current.Back&&flashKnown.IsEnabled,"Restored flashcard face incorrect");await Capture("study-restored");
            study.Summary=true;RenderStudy();await Capture("study-summary");Click(Find(page,"Ôn thẻ cần học (24)"));Check(study.Order.Count==24&&study.Remembered==1,"Missed repeat included learned card");
            Click(Find(page,"Xem tiến độ"));Click(Find(page,"Học lại toàn bộ"));Check(study.Order.Count==25&&study.Known.Count==0,"Full study restart did not reset ratings");
            ExitStudy();Check(!studying&&navigation.IsEnabled&&historyStore.Load(out _).Count==0,"Study exit leaked mock-exam history");
            Click(Find(page,"Tiếp tục flashcards"));Check(studying&&study.Cards.Count==25,"Saved study cannot be reopened");
            string file=ExternalFixture();using(var input=File.OpenRead(file))await MobilePlatform.ReceiveQuiz(input,Path.GetFileName(file));await Task.Delay(100);
            Check(studying&&currentDialog==null&&Inbox.List().Count==1,"Incoming file interrupted study");ExitStudy();await WaitIncoming();Click(Find(modal,"Để sau"));
            return "PASS fresh-process flashcard snapshot/index/ratings/revealed face; missed-only/full repeat; exit/reopen; no exam history; incoming quiz waits until study exit.";
        }
        var desktop=(IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime;
        if(!MobilePlatform.DesktopHost){desktop.MainWindow.Width=360;desktop.MainWindow.Height=760;await Task.Delay(100);}
        string document=Environment.GetEnvironmentVariable("QUIZDESK_STUDY_FIXTURE")??throw new Exception("Missing study QA fixture");
        await Load(document);Check(bank.Count==30&&bank.Count(q=>q.Answer!="")==25,"Reviewed document keys not imported in UI");
        setup.ShuffleQuestions=false;Navigate("setup");await Capture("study-entry");StartStudy();
        Check(!Controls<Avalonia.Controls.Button>(page).Any(b=>Equals(b.Content,"Lật thẻ")),"Redundant flip button remains");
        Check(studying&&study.Cards.Count==25&&study.Excluded==5,"Study eligible count incorrect");
        Check(flashContent.Text==study.Current.Front&&!flashNote.IsVisible&&!flashKnown.IsEnabled,"Front leaks answer or permits premature rating");
        flashCard.BringIntoView();await Capture("study-front");var preserved=flashCard;
        Click(flashCard);Check(flashContent.Text==study.Current.Back&&flashNote.Text.Contains("Giải thích")==false&&flashKnown.IsEnabled,"Flashcard answer not revealed");
        Check(ReferenceEquals(preserved,flashCard),"Flip rebuilt flashcard control");await Capture("study-back");
        if(!MobilePlatform.DesktopHost){
            desktop.MainWindow.Height=640;await Task.Delay(80);flashKnown.BringIntoView();await Task.Delay(120);
            var target=flashKnown.TranslatePoint(new Point(flashKnown.Bounds.Width/2,flashKnown.Bounds.Height/2),this);
            Check(target.HasValue&&target.Value.Y>appBar.Bounds.Height&&target.Value.Y<Bounds.Height-navigation.Bounds.Height,"Study rating unreachable at 360x640");await Capture("study-rating-640");
            desktop.MainWindow.Height=760;await Task.Delay(80);
        }
        Click(Find(page,"Cần học lại"));Check(study.Position==1&&!study.Revealed&&study.Known[0]==false,"Learning mark did not advance to hidden front");
        Click(flashCard);Click(Find(page,"Đã nhớ"));Check(study.Position==2&&study.Remembered==1,"Known mark did not advance");
        var args=new KeyEventArgs {RoutedEvent=KeyDownEvent,Key=Key.Space};StudyKey(this,args);Check(args.Handled&&study.Revealed,"Space key did not flip card");
        PersistSession();Check(studyStore.Load(out _).Position==2&&studyStore.Load(out _).Revealed,"Study state not durably saved");
        preferences.Theme="dark";ApplyTheme();flashCard.BringIntoView();await Capture("study-back-dark");
        return "PASS real Mon15 import: 30 questions, 25 reviewed keys, five unresolved; 25 flashcards; front hides answer; flip stable control; known/learning ratings; keyboard flip; persisted snapshot/index/face; light/dark host snapshots.";
    }
}
#endif
