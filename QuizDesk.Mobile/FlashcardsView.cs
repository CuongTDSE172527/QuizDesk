using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace QuizDesk.Mobile;

public partial class MainView {
    FlashcardStore studyStore;
    FlashcardSession study,restoredStudy;
    bool studying;
    TextBlock flashHeading,flashSide,flashContent,flashNote,flashSource,flashProgress;
    Button flashCard,flashKnown,flashLearning,flashPrevious,flashNext;
    ProgressBar flashBar;
    Expander flashHint;
    TextBlock flashChoices;
    MediaGallery flashMedia;
    void StudyEntry() {
        int count=bank.Count(q=>setup.Included.Contains(q.Number)&&QuestionReview.ScoringAnswer(q)!="");
        var actions=Row(Btn("Học flashcards",StartStudy,true));
        var saved=studyStore.Load(out string warning);
        if(warning!="")Notify(warning,true);
        if(saved!=null)actions.Children.Add(Btn("Tiếp tục flashcards",()=>OpenStudy(saved)));
        page.Children.Add(Card(Stack(Text("Học bằng flashcards",22),Text("Tự nhớ đáp án trước khi lật thẻ. Đánh dấu Đã nhớ hoặc Cần học lại để luyện tập tiếp.",14),
            Supporting($"{count} câu đã chọn có thể tạo thẻ. Câu thiếu đáp án hoặc còn mơ hồ được giữ trong bộ đề để bạn sửa.",13),
            Supporting(saved==null?"Thẻ được tạo từ bộ đề đang chọn.":"Lượt học đã lưu: "+saved.Title+". Bắt đầu lượt mới để lấy các sửa đổi mới nhất của bộ đề.",12),actions)));
    }
    void StartStudy(){
        try{if(active||studying||busy)return;if(!PersistBank())return;OpenStudy(FlashcardSession.Create(title,currentBankId,bank,setup));}
        catch(Exception ex){Notify(ex.Message,true);}
    }
    void OpenStudy(FlashcardSession state) {
        if(active||busy)return;
        study=state;studying=true;study.InProgress=true;notice.IsVisible=false;navigation.IsEnabled=false;currentPage="study";SetDestination("setup");
        Focusable=true;Focus();RenderStudy();SaveStudy();
    }
    void SaveStudy(){if(study==null)return;try{studyStore.Save(study);}catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException){Notify("Chưa lưu được tiến độ flashcards: "+ex.Message,true);}}
    void ExitStudy(){SaveStudy();studying=false;page.Spacing=20;if(study!=null){study.InProgress=false;SaveStudy();}navigation.IsEnabled=true;Navigate("setup");IncomingChanged();}
    void FlipStudy(){if(study==null||study.Summary)return;study.Flip();UpdateStudy();SaveStudy();Animate(flashCard,120,true);}
    void MoveStudy(int delta){if(study==null)return;study.Move(study.Position+delta);UpdateStudy();SaveStudy();Animate(flashCard,110);}
    void RateStudy(bool known){if(study==null||!study.Revealed)return;study.Rate(known);if(study.Summary)RenderStudy();else UpdateStudy();SaveStudy();}
    void StudyKey(object sender,KeyEventArgs args){
        if(!studying||study==null||study.Summary||currentDialog!=null||args.KeyModifiers!=KeyModifiers.None)return;
        if(args.Key==Key.Space){FlipStudy();args.Handled=true;}
        else if(args.Key==Key.Left){MoveStudy(-1);args.Handled=true;}
        else if(args.Key==Key.Right){MoveStudy(1);args.Handled=true;}
    }
    void RenderStudy() {
        page.Children.Clear();page.Spacing=12;pageScroll.Offset=default;
        var name=Text(study.Title,16);name.MaxLines=1;name.TextTrimming=TextTrimming.CharacterEllipsis;name.FontWeight=FontWeight.SemiBold;ToolTip.SetTip(name,study.Title);
        var header=new Grid {ColumnDefinitions=new ColumnDefinitions("*,Auto"),ColumnSpacing=12};header.Children.Add(Stack(Supporting("FLASHCARDS",12),name));
        var exit=Btn("Lưu & thoát",ExitStudy);Grid.SetColumn(exit,1);header.Children.Add(exit);page.Children.Add(header);
        if(study.Excluded>0)page.Children.Add(Supporting($"{study.Excluded} câu chưa có đáp án rõ ràng vẫn ở bộ đề để sửa.",12));
        if(study.Summary){
            int pending=study.Cards.Count-study.Remembered;
            page.Children.Add(Card(Stack(Text("Tiến độ học của bạn",24),Text($"{study.Remembered} / {study.Cards.Count} thẻ đã nhớ",30),
                Text($"{study.Known.Count(p=>!p.Value)} thẻ cần học lại · {study.Cards.Count-study.Known.Count} thẻ chưa đánh dấu."),Supporting("Đây là mức độ nhớ bạn tự đánh giá, không phải điểm bài thi.",13))));
            if(pending>0)page.Children.Add(Btn("Ôn thẻ cần học ("+pending+")",()=>{study.Repeat(false);RenderStudy();SaveStudy();},true));
            page.Children.Add(Row(Btn("Học lại toàn bộ",()=>{study.Repeat(true);RenderStudy();SaveStudy();}),Btn("Về thiết lập bài thi",ExitStudy)));
            return;
        }
        flashHeading=Text("",18);flashProgress=Supporting("",13);flashBar=new ProgressBar {Minimum=0,Maximum=study.Cards.Count,Height=6};
        page.Children.Add(Stack(flashHeading,flashProgress,flashBar));
        flashSide=Supporting("",12);flashContent=Text("",22);flashContent.FontWeight=FontWeight.Medium;
        flashNote=Text("",15);flashSource=Supporting("",12);
        flashCard=Btn("",FlipStudy);flashCard.Name="Flashcard";flashMedia=Gallery();flashCard.Content=Stack(flashSide,flashContent,flashMedia,flashNote,flashSource);
        flashCard.MinHeight=240;flashCard.Padding=new Thickness(24);flashCard.HorizontalAlignment=HorizontalAlignment.Stretch;flashCard.HorizontalContentAlignment=HorizontalAlignment.Stretch;
        flashCard.Classes.Add("flashcard");page.Children.Add(flashCard);
        flashChoices=Text("",14);flashHint=new Expander {Header="Gợi ý · Các lựa chọn trong đề",Content=flashChoices,HorizontalAlignment=HorizontalAlignment.Stretch,HorizontalContentAlignment=HorizontalAlignment.Stretch};
        flashLearning=Btn("Cần học lại",()=>RateStudy(false));flashKnown=Btn("Đã nhớ",()=>RateStudy(true),true);
        page.Children.Add(Row(flashLearning,flashKnown));
        flashPrevious=Btn("← Thẻ trước",()=>MoveStudy(-1));flashNext=Btn("Thẻ tiếp →",()=>MoveStudy(1));
        page.Children.Add(Row(flashPrevious,flashNext,Btn("Xem tiến độ",()=>{study.Summary=true;RenderStudy();SaveStudy();})));
        page.Children.Add(flashHint);
        page.Children.Add(Supporting("Chạm thẻ hoặc nhấn Space để lật · ← / → để chuyển thẻ. Tiến độ được lưu trên máy.",12));
        UpdateStudy();
    }
    void UpdateStudy(){
        var card=study.Current;flashHeading.Text=$"Thẻ {study.Position+1} / {study.Order.Count} · Câu gốc {card.Number}";
        flashProgress.Text=$"Đã nhớ {study.Remembered} / {study.Cards.Count} · Đã đánh dấu {study.Known.Count} thẻ";flashBar.Value=study.Remembered;
        flashMedia.Set(card.Media.Where(m=>m.Target=="question"||study.Revealed&&(m.Target=="note"||m.Target==card.Answer)));
        flashSide.Text=study.Revealed?"ĐÁP ÁN":"CÂU HỎI";flashContent.Text=study.Revealed?card.Back:card.Front;
        flashNote.IsVisible=study.Revealed&&!string.IsNullOrWhiteSpace(card.Note);flashNote.Text=study.Revealed?card.Note:"";
        flashSource.IsVisible=study.Revealed&&(!string.IsNullOrEmpty(card.Source)||!string.IsNullOrEmpty(card.OriginalAnswer));
        flashSource.Text=study.Revealed?(card.Source+(card.OriginalAnswer==""?"":"\nĐáp án gốc để đối chiếu: "+card.OriginalAnswer)):"";
        flashChoices.Text=card.Choices;flashHint.IsVisible=!string.IsNullOrEmpty(card.Choices);flashHint.IsExpanded=false;
        flashCard.Classes.Set("revealed",study.Revealed);Avalonia.Automation.AutomationProperties.SetName(flashCard,(study.Revealed?"Đáp án: ":"Câu hỏi: ")+flashContent.Text+". Nhấn để lật thẻ.");
        flashKnown.IsEnabled=flashLearning.IsEnabled=study.Revealed;flashPrevious.IsEnabled=study.Position>0;flashNext.IsEnabled=study.Position<study.Order.Count-1;
        pageScroll.Offset=default;
    }
}
