#if QA
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace QuizDesk.Mobile;
public partial class MainView {
    async Task<string> VerifyImportReview(string phase) {
        if(phase=="review-restart") {
            Check(bank.Count==4&&bank[0].Answer=="B"&&bank[1].Answer=="A"&&bank[2].Options["B"]=="Chó","Edited answers/choices lost after restart");
            Check(bank.All(q=>!QuestionReview.RequiresReview(q)),"Resolved warnings reappeared after restart");
            Navigate("bank");OpenLibraryBank(currentBankId.Value);await Capture("review-07-restored");
            return "PASS: confirmed answers, explanations and resolved warnings survive fresh process; library stays usable.";
        }
        var detailed=new Question {Number=999,Text="Kiểm tra đáp án",Answer="B",Options=new(){{"A","Sai"},{"B","12µs · "+new string('x',400)}}};
        var row=QuestionRow(detailed);
        Check(Controls<TextBlock>(row).Single(t=>t.Name=="CorrectAnswerDetail").Text==detailed.Options["B"],"Editor answer detail lost/truncated option text or symbols");
        Check(Controls<TextBlock>(row).Any(t=>t.Text?.Contains("Đáp án đúng: B")==true),"Editor hides correct answer label");
        detailed.Answer="";Check(!Controls<TextBlock>(QuestionRow(detailed)).Any(t=>t.Name=="CorrectAnswerDetail"),"Missing answer shows invented detail");
        detailed.Answer="H";Check(Controls<TextBlock>(QuestionRow(detailed)).Any(t=>t.Text?.Contains("Không tìm thấy nội dung")==true),"Missing option reference not identified");
        preferences.Theme="light";ApplyTheme();
        string path=Path.Combine(dataRoot,"review-source.txt");
        File.WriteAllText(path,"Câu 1. Chọn quả?\nA. Táo\nA. Lê\nC. Nho\nĐáp án: A\nCâu 2. Chọn số?\nA. Một\nB. Hai\nCâu 3. Chọn con vật?\nA. Mèo\nB. Mèo\nĐáp án: B\nCâu 4. Câu có đáp án hợp lệ\nA. Một\nB. Hai\nĐáp án: A");
        await Load(path);Check(bank.Count==4,"Import rejected bank containing duplicate and missing answers");OpenLibraryBank(currentBankId.Value);await Task.Delay(150);
        Check(bank.Count(QuestionReview.RequiresReview)==3,"Review count incorrect");
        var firstRow=Controls<Border>(page).First(c=>c.Name=="QuestionRow1");
        Check((firstRow.Background as ISolidColorBrush)?.Color==Color.Parse("#FFF4DB"),"Duplicate question not highlighted");
        await Capture("review-01-highlight-light");
        var filter=Controls<CheckBox>(page).Single(c=>c.Name=="OnlyReviewQuestions");filter.IsChecked=true;
        Check(questionList.Items.Count==3,"Review filter hides wrong questions");
        setup.ShuffleQuestions=false;setup.ShuffleOptions=false;Navigate("setup");Start(true);ChooseAnswer("A");
        Check(exam[index].Answer==""&&!answerButtons["A"].Classes.Contains("wrong")&&feedbackText.Text.Contains("chấm điểm"),"Unconfirmed answer is marked wrong in practice");
        await Capture("review-02-unscored-practice");Finish();Navigate("bank");OpenLibraryBank(currentBankId.Value);
        Edit(bank[0]);await Task.Delay(100);Check(Controls<TextBlock>(modal).Any(t=>t.Text?.Contains("Trùng nhãn")==true),"Editor hides source warning");await Capture("review-03-editor");
        Controls<ComboBox>(modal).Single(c=>c.Name=="CorrectAnswer").SelectedIndex=2;Click(Find(modal,"Lưu câu hỏi"));await Task.Delay(100);
        Check(bank[0].Answer=="B"&&!QuestionReview.RequiresReview(bank[0]),"Saving a confirmed answer did not clear source warning");
        Check(Controls<TextBlock>(QuestionRow(bank[0])).Single(t=>t.Name=="CorrectAnswerDetail").Text==bank[0].Options["B"],"Saved answer detail is stale");
        Edit(bank[1]);await Task.Delay(80);Controls<ComboBox>(modal).Single(c=>c.Name=="CorrectAnswer").SelectedIndex=1;
        Controls<TextBox>(modal).Single(c=>c.Name=="QuestionNote").Text="Đáp án do người dùng bổ sung.";Click(Find(modal,"Lưu câu hỏi"));await Task.Delay(100);
        Check(bank[1].Answer=="A"&&!QuestionReview.RequiresReview(bank[1]),"Missing answer cannot be added");
        Edit(bank[2]);await Task.Delay(80);Click(Find(modal,"Lưu câu hỏi"));await Task.Delay(100);
        Check(QuestionReview.RequiresReview(bank[2]),"Unchanged duplicate content incorrectly cleared");
        Edit(bank[2]);await Task.Delay(80);Controls<TextBox>(modal).Single(c=>c.Name=="OptionB").Text="Chó";Click(Find(modal,"Lưu câu hỏi"));await Task.Delay(100);
        Check(bank.All(q=>!QuestionReview.RequiresReview(q)),"Resolved questions remain highlighted");
        filter=Controls<CheckBox>(page).Single(c=>c.Name=="OnlyReviewQuestions");filter.IsChecked=true;Check(questionList.Items.Count==0,"Resolved rows remain in review filter");filter.IsChecked=false;
        await Capture("review-04-resolved");
        Check(library.Load(currentBankId.Value).Activities.Any(a=>a.Changes.Any(c=>c.Field.Contains("Cảnh báo"))),"Clearing warnings not recorded in library history");
        preferences.Theme="dark";ApplyTheme();
        var pending=library.Create(new(){Title="Đề chưa có đáp án",Bank=Reader.Parse("Câu 1. Câu chưa có đáp án\nA. Một\nB. Hai"),Setup=new(){Count=1,Included=new(){1}}});
        var fixedId=currentBankId.Value;OpenLibraryBank(pending.Id);await Capture("review-05-highlight-dark");
        Check((Controls<Border>(page).First(c=>c.Name=="QuestionRow1").Background as ISolidColorBrush)?.Color==Color.Parse("#42371E"),"Dark warning color missing");
        OpenLibraryBank(fixedId);preferences.Theme="light";ApplyTheme();SavePreferences();PersistSession();
        string export=Path.Combine(dataRoot,"review-fixed.quiz");QuizFile.Save(export,bank,setup);Check(Importers.Load(export,out _).All(q=>!QuestionReview.RequiresReview(q)),"Export resurrected resolved warnings");
        return "PASS: mixed import retains every question; yellow light/dark highlights and review filter; actual editor resolves repeated labels, adds missing answer/explanation, keeps unresolved duplicate-content warning; unscored practice is neutral; export and timestamped audit persist changes.";
    }
}
#endif
