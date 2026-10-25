#if QA
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace QuizDesk.Mobile;
public partial class MainView {
    async Task<string> VerifyMedia(){
        string fixture=Environment.GetEnvironmentVariable("QUIZDESK_MEDIA_FIXTURE");await Load(fixture);
        Check(bank.Count==53&&bank.Single(q=>q.Number==19).Media.Count==1,"Meteorology map not imported");
        setup.ShuffleQuestions=false;setup.ShuffleOptions=false;setup.Included=new(bank.Select(q=>q.Number));Start(true);MoveToQuestion(exam.FindIndex(q=>q.Number==19));
        for(int i=0;i<100&&!Controls<MediaImage>(questionMedia).Any(m=>m.IsImageLoaded);i++)await Task.Delay(30);
        Check(Controls<MediaImage>(questionMedia).Single().IsImageLoaded,"Actual map not decoded in practice");await Capture("media-01-map-question19");
        var saved=questionMedia;ChooseAnswer("C");Check(ReferenceEquals(saved,questionMedia)&&answerButtons["C"].Classes.Contains("correct"),"Answer selection rebuilt gallery or lost answer");Finish();
        StartStudy();int position=study.Cards.FindIndex(c=>c.Number==19);study.Move(position);UpdateStudy();await Task.Delay(300);Check(flashMedia.Children.Count==1,"Map missing from flashcard front");await Capture("media-02-flashcard-front");Click(flashCard);await Capture("media-03-flashcard-back");ExitStudy();
        var q=bank.Single(q=>q.Number==19);var animation=MediaFiles.FromBytes("animated.gif",File.ReadAllBytes(Environment.GetEnvironmentVariable("QUIZDESK_GIF_FIXTURE")),"B");
        var edited=new Question {Number=q.Number,Text=q.Text,Answer=q.Answer,Note=q.Note,Options=new(q.Options),Media=q.Media.Select(m=>m.Clone()).Append(animation).ToList()};CommitQuestion(q,edited);
        Navigate("bank");Edit(edited);await Task.Delay(100);Check(Controls<Button>(modal).Any(b=>Equals(b.Content,"＋ Thêm ảnh / GIF / video")),"Media editor picker missing");Check(Controls<Button>(modal).Count(b=>Equals(b.Content,"Xóa đính kèm"))==2,"Media editor list incomplete");await Capture("media-04-editor");Click(Find(modal,"Lưu câu hỏi"));await Task.Delay(100);Check(bank.Single(x=>x.Number==19).Media.Count==2,"Saving question discarded attachments");
        setup.Count=1;setup.Included=new(){19};setup.ShuffleOptions=false;Start(false);
        for(int i=0;i<100&&!Controls<MediaImage>(choiceMedia["B"]).Any(m=>m.IsImageLoaded);i++)await Task.Delay(30);
        var gif=Controls<MediaImage>(choiceMedia["B"]).Single();Check(gif.IsImageLoaded,"Animated GIF decoder failed");int initial=gif.CurrentFrame;Click(Find(gif,"▶ Phát GIF"));await Task.Delay(115);Check(gif.CurrentFrame!=initial,"GIF frames did not advance");Click(Find(gif,"Ⅱ Dừng GIF"));int paused=gif.CurrentFrame;await Task.Delay(180);Check(gif.CurrentFrame==paused,"GIF pause did not stop frames");await Capture("media-05-gif-choice");ChooseAnswer("C");Finish();
        var attempt=historyStore.Load(out _).Single();Check(attempt.Questions[0].Media.Count==2,"History discarded media snapshot");ShowAttempt(attempt);await Task.Delay(300);Check(Controls<MediaGallery>(page).Any(g=>g.Children.Count==2),"History map/GIF gallery absent");await Capture("media-06-history");
        return "PASS actual DOCX 53 questions/map q19, practice map decode/stable answer selection, flashcards both sides, editor preserves attachments, GIF frame advance/pause, mock attempt/history media.";
    }
}
#endif
