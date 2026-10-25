using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace QuizDesk.Mobile;

public partial class MainView {
    ObservableCollection<Question> questionItems;
    TextBox questionSearch;
    TextBlock reviewSummaryText,openedBankCount,openedBankDates;
    Border reviewSummaryCard,emptyBankCard;
    Button clearQuestionsButton;
    sealed record EditorViewport(Guid? BankId,ListBox List,ScrollViewer Inner,Vector InnerOffset,Vector PageOffset,int ItemIndex,double? RowTop,double? ScreenTop);
    EditorViewport CaptureEditorViewport(Question original){
        var inner=questionList?.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        var row=original==null?null:questionList?.ContainerFromItem(original);
        return new(currentBankId,questionList,inner,inner?.Offset??default,pageScroll.Offset,original==null?-1:questionItems?.IndexOf(original)??-1,row?.TranslatePoint(default,questionList)?.Y,row?.TranslatePoint(default,this)?.Y);
    }
    bool MatchesEditor(Question q,string query)=>(!onlyReviewQuestions||QuestionReview.RequiresReview(q))&&(q.Text.Contains(query,StringComparison.OrdinalIgnoreCase)||q.Number.ToString()==query);
    void RefreshEditorSummary(){
        int reviewCount=bank.Count(QuestionReview.RequiresReview),missing=bank.Count(q=>q.Answer=="");
        reviewSummaryText.Text=$"{reviewCount} câu cần kiểm tra · {missing} câu chưa có đáp án\nCâu tô vàng cần sửa/bổ sung. Câu có lựa chọn trùng hoặc đáp án chưa rõ sẽ chưa tính điểm.";
        reviewSummaryCard.IsVisible=reviewCount>0;emptyBankCard.IsVisible=bank.Count==0;clearQuestionsButton.IsEnabled=bank.Count>0;
        if(openedBankCount!=null)openedBankCount.Text=$"{bank.Count} câu hỏi · Đang chọn để làm bài";
        if(openedBankDates!=null&&currentBankId.HasValue){
            try{var stored=library.Load(currentBankId.Value);openedBankDates.Text=(stored.ImportedAt.HasValue?"Nhập: "+Stamp(stored.ImportedAt.Value):"Tạo trong thư viện: "+Stamp(stored.CreatedAt))+"\nSửa gần nhất: "+Stamp(stored.UpdatedAt);}
            catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException){Notify("Không đọc được mốc lưu bộ đề: "+ex.Message,true);}
        }
        UpdateSelection();
    }
    void RefreshEditedQuestion(Question original,Question edited,EditorViewport viewport){
        // A queued file activation/navigation must not apply an old dialog's
        // scroll position to a different bank or a newly constructed editor.
        if(currentPage!="bank"||currentBankId!=viewport.BankId||!ReferenceEquals(questionList,viewport.List)||questionItems==null)return;
        int at=original==null?-1:questionItems.IndexOf(original);
        bool matches=MatchesEditor(edited,questionSearch.Text??"");
        bool autoScroll=questionList.AutoScrollToSelectedItem;questionList.AutoScrollToSelectedItem=false;
        if(at>=0){if(matches)questionItems[at]=edited;else questionItems.RemoveAt(at);}
        else if(matches&&original==null)questionItems.Add(edited);
        var target=matches?edited:questionItems.ElementAtOrDefault(Math.Clamp(viewport.ItemIndex,0,Math.Max(0,questionItems.Count-1)));
        questionList.SelectedItem=target;RefreshEditorSummary();
        Dispatcher.UIThread.Post(()=>{
            if(currentPage!="bank"||currentBankId!=viewport.BankId||!ReferenceEquals(questionList,viewport.List))return;
            questionList.UpdateLayout();
            if(viewport.Inner!=null)viewport.Inner.Offset=viewport.InnerOffset;
            pageScroll.Offset=viewport.PageOffset;
            questionList.UpdateLayout();
            if(original!=null&&target!=null&&viewport.RowTop.HasValue){
                var row=questionList.ContainerFromItem(target);
                if(row==null){questionList.ScrollIntoView(target);questionList.UpdateLayout();row=questionList.ContainerFromItem(target);}
                var top=row?.TranslatePoint(default,questionList);
                if(top.HasValue&&viewport.Inner!=null){viewport.Inner.Offset=new(viewport.Inner.Offset.X,Math.Max(0,viewport.Inner.Offset.Y+top.Value.Y-viewport.RowTop.Value));questionList.UpdateLayout();}
                var screen=row?.TranslatePoint(default,this);
                if(screen.HasValue&&viewport.ScreenTop.HasValue)pageScroll.Offset=new(pageScroll.Offset.X,Math.Max(0,pageScroll.Offset.Y+screen.Value.Y-viewport.ScreenTop.Value));
            }
            // Existing rows keep their position. Added rows have no previous
            // position, so bring the new item into view only in that case.
            if(original==null&&target!=null)questionList.ScrollIntoView(target);
            questionList.AutoScrollToSelectedItem=autoScroll;
        },DispatcherPriority.Loaded);
    }
}
