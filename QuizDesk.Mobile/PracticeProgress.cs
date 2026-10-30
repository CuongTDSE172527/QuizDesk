using QuizDesk;

namespace QuizDesk.Mobile;
public partial class MainView {
    MobileWorkspace PracticeWorkspace(Guid id)=>new(Path.Combine(workspace.Folder,"practice",id.ToString("N")));
    void PracticeEntry() {
        if(!currentBankId.HasValue)return;
        var saved=PracticeWorkspace(currentBankId.Value).LoadActive(out string warning);
        if(warning!="")Notify(warning,true);
        if(saved==null||!saved.Practice||saved.LibraryBankId!=currentBankId)return;
        page.Children.Add(Card(Stack(Text("Ôn tập đã lưu",22),
            Text($"{saved.Title} · Đã trả lời {saved.Answers.Count}/{saved.Exam.Count} câu · Đang ở câu {saved.Index+1}",14),
            Supporting("Tiếp tục giữ nguyên câu hỏi, thứ tự trộn và đáp án của lượt đã lưu. Ôn lại từ đầu để áp dụng các sửa đổi mới nhất.",12),
            Row(Btn("Tiếp tục ôn tập",()=>ResumePractice(saved),true)))));
    }
    async void StartNewPractice() {
        if(active||busy||studying)return;
        if(currentBankId.HasValue) {
            var saved=PracticeWorkspace(currentBankId.Value).LoadActive(out _);
            if(saved!=null&&!await Ask("Ôn tập lại từ đầu?","Lượt mới sẽ thay thế tiến trình ôn tập đã lưu của bộ đề này.","Ôn lại từ đầu"))return;
        }
        if(!PersistBank())return;
        Start(true);
    }
    void ResumePractice(ActiveMobileExam saved) {
        if(active||busy||studying||!saved.Practice||saved.LibraryBankId!=currentBankId)return;
        try{workspace.Begin(saved);RestoreExam(saved);pageScroll.Offset=default;}
        catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException){Notify("Chưa tiếp tục được ôn tập: "+ex.Message,true);}
    }
    void SavePracticeProgress() {
        if(!examBankId.HasValue)throw new InvalidDataException("Bộ đề chưa được lưu trong thư viện.");
        PracticeWorkspace(examBankId.Value).Begin(new ActiveMobileExam {
            LibraryBankId=examBankId,Id=activeAttemptId,Title=examTitle,StartedAt=startedAt,
            Deadline=new DateTimeOffset(deadline),Practice=true,Minutes=examMinutes,Exam=exam,
            Index=index,Answers=new(answers)
        });
    }
}
