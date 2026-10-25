using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace QuizDesk.Mobile;

public partial class MainView {
    void TrySaveAttempts() {
        foreach(var attempt in unsavedAttempts.ToArray()) {
            try{historyStore.Save(attempt);workspace.Complete(attempt.Id);unsavedAttempts.Remove(attempt);}
            catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException){Notify("Chưa lưu được lịch sử: "+ex.Message+". Bạn có thể thử lại trong mục Lịch sử.",true);}
        }
    }
    static string ScoreText(ExamAttempt a)=>a.Score.HasValue?a.Score.Value.ToString("0.##")+" / 10":"Không tính điểm";
    static string DurationText(ExamAttempt a) {
        var elapsed=TimeSpan.FromSeconds(Math.Min(a.ElapsedSeconds,TimeSpan.MaxValue.TotalSeconds/2));
        return elapsed.TotalHours>=1?$"{(int)elapsed.TotalHours} giờ {elapsed.Minutes} phút {elapsed.Seconds} giây":$"{(int)elapsed.TotalMinutes} phút {elapsed.Seconds} giây";
    }
    static string OutcomeText(AttemptQuestion q)=>q.Outcome switch {"correct"=>"Đúng","wrong"=>"Sai","unanswered"=>"Bỏ trống",_=>"Không tính điểm"};
    static string SummaryText(ExamAttempt a)=>$"{a.Correct} đúng · {a.Wrong} sai · {a.Unanswered} bỏ trống · {a.Unscored} không tính điểm";
    void HistoryPage() {
        Heading("Nhìn lại quá trình học","Lịch sử thi thử","Mỗi lần nộp bài hoặc hết giờ đều được tự động lưu trên máy, kèm câu hỏi và đáp án tại thời điểm thi.");
        if(unsavedAttempts.Count>0)page.Children.Add(Card(Stack(Text($"Có {unsavedAttempts.Count} lần thi chưa lưu được",20),Text("Các lần này vẫn còn trong bộ nhớ. Hãy thử lưu lại trước khi đóng app."),Btn("Thử lưu lại lịch sử",()=>{TrySaveAttempts();Navigate("history");},true))));
        var attempts=historyStore.Load(out string warning);if(warning!="")Notify(warning,true);
        page.Children.Add(Text($"{attempts.Count} lần thi đã lưu",16));
        if(attempts.Count==0) {
            page.Children.Add(Card(Stack(Text("Chưa có lịch sử thi thử",22),Text("Hoàn thành một bài thi thử để xem lại tại đây. Ôn tập và bài thoát giữa chừng không được ghi vào lịch sử. Các lần thi trước khi cập nhật tính năng này không thể khôi phục."),Btn("Thiết lập bài thi",()=>Navigate("setup"),true))));return;
        }
        foreach(var attempt in attempts) {
            var titleText=Text(attempt.Title,21);
            page.Children.Add(Card(Stack(titleText,Text(attempt.FinishedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss")+" · "+DurationText(attempt)+" · "+(attempt.TimedOut?"Tự nộp khi hết giờ":"Đã nộp bài"),13),Text(ScoreText(attempt),26),Text(SummaryText(attempt)),Btn("Xem chi tiết",()=>ShowAttempt(attempt),true))));
        }
    }
    void ShowAttempt(ExamAttempt attempt) {
        if(active)return;page.Children.Clear();SetDestination("history");
        Heading("Chi tiết lần thi",attempt.Title,attempt.FinishedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss")+" · "+DurationText(attempt)+" · "+(attempt.TimedOut?"Tự nộp khi hết giờ":"Đã nộp bài"));
        page.Children.Add(Row(Btn("← Về lịch sử",()=>Navigate("history")),Btn("Lưu kết quả",()=>SaveReport(AttemptReport(attempt)))));
        page.Children.Add(Card(Stack(Text(ScoreText(attempt),36),Text(SummaryText(attempt)),Text("Giữ nguyên câu hỏi, thứ tự lựa chọn, đáp án đã chọn và giải thích của lần thi này. Chỉnh sửa/xóa bộ đề sau đó không làm thay đổi lịch sử.",13))));
        string[] labels={$"Tất cả ({attempt.Questions.Count})",$"Câu sai ({attempt.Wrong})",$"Câu đúng ({attempt.Correct})",$"Bỏ trống ({attempt.Unanswered})",$"Không tính điểm ({attempt.Unscored})"};
        var filter=new ComboBox {Name="HistoryFilter",ItemsSource=labels,SelectedIndex=0,HorizontalAlignment=HorizontalAlignment.Stretch};page.Children.Add(Field("Lọc câu hỏi",filter));
        var questions=new StackPanel {Name="HistoryQuestions",Spacing=12};page.Children.Add(questions);
        void Fill() {
            questions.Children.Clear();string outcome=filter.SelectedIndex switch {1=>"wrong",2=>"correct",3=>"unanswered",4=>"unscored",_=>""};int shown=0;
            for(int i=0;i<attempt.Questions.Count;i++) {
                var q=attempt.Questions[i];if(outcome!=""&&q.Outcome!=outcome)continue;
                string text=q.Text+"\n\n"+string.Join("\n",q.Options.Select(o=>o.Key+". "+o.Value+(o.Key==q.Answer?"  [Đáp án đúng]":"")+(o.Key==q.Picked?"  [Bạn chọn]":"")))+
                    "\n\nBạn chọn: "+(q.Picked==""?"Bỏ trống":q.Picked)+"\nĐáp án đúng: "+(q.Answer==""?"Không có đáp án để tính điểm":q.Answer)+(string.IsNullOrWhiteSpace(q.Note)?"":"\n\nGiải thích / ghi chú:\n"+q.Note);
                var detail=new Expander {Header=$"Câu {i+1} (câu gốc {q.Number}) · {OutcomeText(q)}",IsExpanded=shown==0,HorizontalAlignment=HorizontalAlignment.Stretch,HorizontalContentAlignment=HorizontalAlignment.Stretch};
                void Populate(){detail.Content=detail.IsExpanded?Stack(Text(text,15),Gallery(q.Media)):null;}
                detail.PropertyChanged+=(_,e)=>{if(e.Property==Expander.IsExpandedProperty)Populate();};Populate();questions.Children.Add(detail);shown++;
            }
            if(shown==0)questions.Children.Add(Text("Không có câu hỏi thuộc nhóm này."));
        }
        filter.SelectionChanged+=(_,_)=>Fill();Fill();pageScroll.Offset=default;
    }
    static string AttemptReport(ExamAttempt attempt) {
        var text=new StringBuilder(attempt.Title+"\n"+attempt.FinishedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss")+"\n"+ScoreText(attempt)+"\n"+SummaryText(attempt)+"\n\n");
        for(int i=0;i<attempt.Questions.Count;i++) {
            var q=attempt.Questions[i];text.AppendLine($"Câu {i+1} (câu gốc {q.Number}) · {OutcomeText(q)}\n{q.Text}\n");
            foreach(var option in q.Options)text.AppendLine(option.Key+". "+option.Value);
            text.AppendLine("Bạn chọn: "+(q.Picked==""?"Bỏ trống":q.Picked)+"\nĐáp án đúng: "+(q.Answer==""?"Không tính điểm":q.Answer)+"\n"+q.Note+"\n");
        }return text.ToString();
    }
}
