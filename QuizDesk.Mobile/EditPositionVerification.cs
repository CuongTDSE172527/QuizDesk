#if QA
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace QuizDesk.Mobile;
public partial class MainView {
    async Task<string> VerifyEditPosition(){
        var questions=Enumerable.Range(1,150).Select(n=>new Question {Number=n,Text=(n%5==0?"Nhóm đích ":"Câu hỏi ")+n,Answer=n is 80 or 81 or 82 or 120?"":"A",Options=new(){{"A","Một"},{"B","Hai"}}}).ToList();
        AdoptBank(library.Create(new(){Title="Kiểm tra giữ vị trí",Bank=questions,Setup=new(){Count=20,Included=new(questions.Select(q=>q.Number))}}));RememberSelectedBank();Navigate("bank");OpenLibraryBank(currentBankId.Value);
        await Task.Delay(100);var list=questionList;var root=page.Children.ToArray();var search=questionSearch;
        var filter=Controls<CheckBox>(page).Single(c=>c.Name=="OnlyReviewQuestions");
        async Task Position(int number){questionList.ScrollIntoView(bank.Single(q=>q.Number==number));questionList.BringIntoView();await Task.Delay(150);}
        async Task SaveAnswer(int number,int selected){
            var item=bank.Single(q=>q.Number==number);var row=questionList.ContainerFromItem(item);Check(row!=null,"Target row not realized");
            Click(Controls<Button>(row).Single(b=>Equals(b.Content,"Sửa")));await Task.Delay(80);
            Controls<ComboBox>(modal).Single(c=>c.Name=="CorrectAnswer").SelectedIndex=selected;Click(Find(modal,"Lưu câu hỏi"));await Task.Delay(180);
            Check(ReferenceEquals(list,questionList)&&root.SequenceEqual(page.Children),"Saving reconstructed bank page/list");
        }
        await Position(120);var before=list.ContainerFromItem(bank[119]).TranslatePoint(default,this).Value;var inner=list.GetVisualDescendants().OfType<ScrollViewer>().First();
        Check(inner.Offset.Y>1000,"Test did not reach a deep row");await SaveAnswer(120,2);
        var after=list.ContainerFromItem(bank[119]).TranslatePoint(default,this);
        Check(after.HasValue&&Math.Abs(after.Value.Y-before.Y)<3&&inner.Offset.Y>1000,"Edited deep row jumped after warning height changed");
        Check(bank[119].Answer=="B"&&library.Load(currentBankId.Value).Content.Bank[119].Answer=="B","Answer not persisted");await Capture("edit-deep-row");
        Edit(bank[119]);await Task.Delay(80);Click(Find(modal,"Hủy"));await Task.Delay(80);Check(ReferenceEquals(list,questionList)&&bank[119].Answer=="B","Cancel changed row or bank");
        search.Text="Nhóm đích";await Task.Delay(100);await Position(125);var searchOffset=inner.Offset.Y;await SaveAnswer(125,2);
        Check(ReferenceEquals(search,questionSearch)&&search.Text=="Nhóm đích"&&list.Items.Count==30&&Math.Abs(inner.Offset.Y-searchOffset)<3,"Search/results/scroll lost on edit");
        search.Text="";filter.IsChecked=true;await Task.Delay(100);await Position(81);await SaveAnswer(81,1);
        Check(filter.IsChecked==true&&onlyReviewQuestions&&list.Items.Count==2&&((Question)list.SelectedItem).Number==82,"Review filter did not keep next pending neighbor");
        search.Text="Nhóm đích";await Task.Delay(100);await Position(80);await SaveAnswer(80,2);
        Check(search.Text=="Nhóm đích"&&filter.IsChecked==true&&list.Items.Count==0&&!emptyBankCard.IsVisible,"Combined filters lost when last visible warning resolved");await Capture("edit-filter-empty");
        search.Text="";filter.IsChecked=false;await Task.Delay(80);
        Click(Find(page,"Thêm câu"));await Task.Delay(80);
        Controls<TextBox>(modal).Single(c=>c.Name=="QuestionText").Text="Câu mới";Controls<TextBox>(modal).Single(c=>c.Name=="OptionA").Text="Một";Controls<TextBox>(modal).Single(c=>c.Name=="OptionB").Text="Hai";
        Controls<ComboBox>(modal).Single(c=>c.Name=="CorrectAnswer").SelectedIndex=1;Click(Find(modal,"Lưu câu hỏi"));await Task.Delay(150);
        Check(ReferenceEquals(list,questionList)&&bank.Count==151&&openedBankCount.Text.StartsWith("151")&&clearQuestionsButton.IsEnabled&&((Question)list.SelectedItem).Number==151,"Adding question left stale counts/selection");
        return "PASS actual editor save on deep question 120 retains row's screen position despite warning height change; page/ListBox/search controls not rebuilt; saved answer persisted; cancel unchanged; filtered deep question 125 retains search/offset; review filter advances to next unresolved neighbor; combined filters stay active with empty results; adding question updates count/selection without page navigation.";
    }
}
#endif
