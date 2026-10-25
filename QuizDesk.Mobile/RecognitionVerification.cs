#if QA
using Avalonia.Controls;
using Avalonia.Input;
namespace QuizDesk.Mobile;
public partial class MainView {
    async Task<string> VerifyRecognition(){
        string fixture=Environment.GetEnvironmentVariable("QUIZDESK_STUDY_FIXTURE");await Load(fixture);
        Check(bank.Count==30&&bank.Single(q=>q.Number==27).Answer=="A","Screenshot question 27 not resolved after reimport");
        setup.ShuffleQuestions=false;setup.ShuffleOptions=false;Start(true);MoveToQuestion(exam.FindIndex(q=>q.Number==27));ChooseAnswer("A");
        Check(answerButtons["A"].Classes.Contains("correct")&&feedbackText.Text.Contains("Chính xác")&&!feedbackText.Text.Contains("Chưa có đáp án"),"Screenshot question still unscored");
        Check(!Controls<Button>(page).Any(b=>Equals(b.Content,"Nộp bài")),"Practice submission returned");await Capture("mon15-question27-correct");Finish();
        StartStudy();int position=study.Cards.FindIndex(c=>c.Number==27);Check(position>=0,"Question 27 missing from flashcards");study.Move(position);UpdateStudy();SaveStudy();
        Check(!Controls<Button>(page).Any(b=>Equals(b.Content,"Lật thẻ")),"Redundant flip button remains");
        var card=flashCard;Click(flashCard);Check(study.Revealed&&flashContent.Text.StartsWith("A.")&&flashKnown.IsEnabled&&ReferenceEquals(card,flashCard),"Tap card failed to reveal reviewed answer");await Capture("mon15-question27-card-back");
        var args=new KeyEventArgs {RoutedEvent=KeyDownEvent,Key=Key.Space};StudyKey(this,args);Check(args.Handled&&!study.Revealed&&!flashKnown.IsEnabled,"Keyboard flip regressed");
        Check(!studyStore.Load(out _).Revealed,"Card flip progress not saved");ExitStudy();
        return "PASS actual Mon15 reimport resolves screenshot question 27 to A with immediate practice feedback and no submit; reviewed A flashcard accessible by tap/Space, stable controls, no duplicate flip button, reveal checkpoint durable.";
    }
}
#endif
