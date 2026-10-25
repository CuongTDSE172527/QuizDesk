#if QA
using Avalonia.Controls;
namespace QuizDesk.Mobile;
public partial class MainView {
    async Task<string> VerifyMotion(string phase){
        if(phase=="motion-restart"){
            Check(preferences.ReducedMotion,"Reduced motion preference not restored");Navigate("preferences");Check(page.Transitions==null&&page.Opacity==1,"Reduced motion still animates navigation");
            return "PASS reduced-motion setting survives a fresh process and disables navigation transitions.";
        }
        var legacy=System.Text.Json.JsonSerializer.Deserialize<AppPreferences>("{\"Version\":1,\"Theme\":\"dark\",\"Minutes\":42}");legacy.Normalize();Check(!legacy.ReducedMotion&&legacy.Theme=="dark"&&legacy.Minutes==42,"Legacy preferences migration failed");
        preferences.ReducedMotion=false;Navigate("home");await Task.Delay(45);
        Check(page.Opacity>0.78&&page.Opacity<1,"Navigation transition did not produce intermediate frames");
        Navigate("setup");Navigate("history");await Task.Delay(200);
        Check(page.Opacity==1&&page.RenderTransform.Value.IsIdentity,"Rapid navigation left content faded or displaced");
        bank=Enumerable.Range(1,103).Select(n=>new Question {Number=n,Text="Motion "+n,Answer="B",Options=new(){{"A","Một"},{"B","Hai"}}}).ToList();
        setup=new(){Count=103,Minutes=30,ShuffleQuestions=false,ShuffleOptions=false,Included=new(bank.Select(q=>q.Number))};AdoptBank(library.Create(BankSnapshot()));Start(false);
        var pool=jumpButtons.ToArray();MoveToQuestion(102);Check(jumpButtons.Count==50&&jumpButtons.Count(b=>b.IsVisible)==3&&jumpButtons[2].Tag is 102,"Last navigation page incorrectly mapped");
        Click(jumpButtons[0]);Check(index==100,"Reused jump button targeted its old question");ChooseAnswer("B");ChooseAnswer("B");Check(answers[index]=="B"&&answerButtons["B"].IsChecked==true,"Same answer deselected");
        MoveToQuestion(0);MoveToQuestion(100);Check(jumpButtons[0].IsChecked==true&&pool.SequenceEqual(jumpButtons),"Paging rebuilt controls or lost answered marker");
        for(int i=0;i<70;i++){MoveToQuestion(i);ChooseAnswer("A");}await Task.Delay(200);
        Check(questionText.Opacity==1&&questionText.RenderTransform.Value.IsIdentity,"Fast question switching left animation incomplete");
        Check(workspace.LoadActive(out _).Index==69&&workspace.LoadActive(out _).Answers.Count==71,"Motion lost checkpoint state");
        timer.Stop();active=false;workspace.Complete(activeAttemptId);navigation.IsEnabled=true;Navigate("setup");StartStudy();var card=flashCard;
        for(int i=0;i<31;i++)FlipStudy();Check(study.Revealed&&ReferenceEquals(card,flashCard)&&studyStore.Load(out _).Revealed,"Rapid flip lost face/persistence or rebuilt card");
        for(int i=0;i<20;i++)MoveStudy(1);await Task.Delay(200);Check(study.Position==20&&flashCard.Opacity==1&&flashCard.RenderTransform.Value.IsIdentity,"Rapid flashcard moves lost position/animation");ExitStudy();
        Navigate("preferences");Controls<ToggleSwitch>(page).Single(t=>Equals(t.Content,"Giảm chuyển động")).IsChecked=true;
        Navigate("home");Check(page.Transitions==null&&page.Opacity==1,"Reduced motion not applied immediately");Check(preferencesStore.Load(out _).ReducedMotion,"Reduced motion not saved");await Capture("motion-home");
        return "PASS rapid navigation, 103-question bounded/reused navigator, last page/direct targets, repeat answers, checkpoint durability, 31 flips/20 flashcard moves, legacy settings and reduced-motion persistence.";
    }
}
#endif
