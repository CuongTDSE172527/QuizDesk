using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
namespace QuizDesk.Mobile;
public partial class MainView {
    TextBox essayInput;
    Button essayReveal;
    bool showingEssay,essayRevealed;
    readonly DispatcherTimer essaySaveTimer=new(){Interval=TimeSpan.FromMilliseconds(400)};
    bool essayTimerReady;
    void CreateEssayInput(StackPanel choices) {
        if(!essayTimerReady){essaySaveTimer.Tick+=(_,_)=>{essaySaveTimer.Stop();if(active)Checkpoint();};essayTimerReady=true;}
        essayInput=new TextBox {Name="EssayResponse",AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,MinHeight=180,MaxLength=32768,PlaceholderText="Nhập câu trả lời tự luận…"};
        essayReveal=Btn("Xem đáp án mẫu",()=>{essayRevealed=!essayRevealed;UpdateEssayFeedback(exam[index]);});
        choices.Children.Add(essayInput);choices.Children.Add(essayReveal);
        essayInput.PropertyChanged+=(_,change)=>{
            if(change.Property!=TextBox.TextProperty)return;
            if(showingEssay||!active||!exam[index].IsEssay)return;
            var value=essayInput.Text??"";if(string.IsNullOrWhiteSpace(value))answers.Remove(index);else answers[index]=value;
            UpdateAnswerState();essaySaveTimer.Stop();essaySaveTimer.Start();
        };
    }
    void ShowEssayInput(Question q) {
        showingEssay=true;try{essayInput.IsVisible=q.IsEssay;essayInput.Text=q.IsEssay?answers.GetValueOrDefault(index,""):"";essayReveal.IsVisible=q.IsEssay&&practice;essayRevealed=false;}finally{showingEssay=false;}
    }
    void UpdateEssayFeedback(Question q) {
        essayReveal.Content=essayRevealed?"Ẩn đáp án mẫu":"Xem đáp án mẫu";
        questionFeedback.IsVisible=practice&&essayRevealed;
        feedbackText.Text="Đáp án mẫu / tiêu chí đối chiếu:\n"+(q.Answer==""?"Chưa bổ sung đáp án mẫu.":q.Answer)+"\n"+q.Note;
        Tone(questionFeedback,Border.BackgroundProperty,"ContainerLow");Tone(feedbackText,TextBlock.ForegroundProperty,"OnSurface");
        noteMedia.Set(practice&&essayRevealed?q.Media.Where(m=>m.Target=="note"):Array.Empty<QuestionMedia>());
    }
}
