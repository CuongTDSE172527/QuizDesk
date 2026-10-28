#if QA
namespace QuizDesk.Mobile;
public partial class MainView {
    async Task<string> VerifyWordSymbols(){
        string original=Environment.GetEnvironmentVariable("QUIZDESK_WORD_SYMBOL_FIXTURE")??throw new Exception("Missing Word symbol fixture");
        await Load(original);
        Check(bank.Count==32&&bank[0].Options["A"]=="12µs.","DME micro sign was lost in the UI import");
        // The source fixture has no answer key. Confirm A only inside isolated QA data.
        bank[0].Answer="A";StartStudy();
        Check(studying,"Cannot start Symbol flashcard QA");
        study.Flip();UpdateStudy();
        Check(flashContent.Text=="A. 12µs."&&flashChoices.Text.Contains("18µs."),"Flashcard UI text lost the micro sign");
        await Capture("word-symbol-micro-flashcard");
        ExitStudy();
        return "PASS actual DME import to flashcard UI: 12µs answer and 18µs choice preserved; original input file unchanged.";
    }
}
#endif
