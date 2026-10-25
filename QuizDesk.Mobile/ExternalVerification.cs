#if QA
using Avalonia;
using Avalonia.Controls;
using System.Diagnostics;

namespace QuizDesk.Mobile;
public partial class MainView {
    async Task WaitIncoming() {
        for(int i=0;i<100&&currentDialog==null;i++)await Task.Delay(40);
        Check(currentDialog?.Title=="Nhận bộ đề","External preview did not appear");
    }
    string ExternalFixture(string name="Đề nhận có khoảng trắng.quiz") {
        string path=Path.Combine(dataRoot,name);
        QuizFile.Save(path,new(){new(){Number=8,Text="Câu từ file chia sẻ",Note="Giải thích được giữ",Answer="B",Options=new(){{"A","Một"},{"B","Hai"}}}},new(){Count=1,Minutes=17,Included=new(){8}});return path;
    }
    void PrepareExternalExam() {
        AdoptBank(library.Create(new(){Title="Bộ đề đang thi",Bank=new(){new(){Number=1,Text="Câu đang thi",Answer="A",Options=new(){{"A","Một"},{"B","Hai"}}}},Setup=new(){Count=1,Minutes=30,Included=new(){1},ShuffleQuestions=false}}));RememberSelectedBank();Navigate("setup");Start(false);ChooseAnswer("A");
    }
    async Task<string> VerifyExternal(string phase) {
        if(phase=="external-small") {
            var desktop=(Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime;desktop.MainWindow.Width=360;desktop.MainWindow.Height=640;await Task.Delay(100);
            string file=ExternalFixture();using(var input=File.OpenRead(file))await MobilePlatform.ReceiveQuiz(input,string.Concat(Enumerable.Repeat("Tên bộ đề tiếng Việt rất dài ",6))+".quiz");
            await WaitIncoming();var scroll=Controls<ScrollViewer>(modal).Single(c=>c.Name=="IncomingPreviewScroll");scroll.Offset=new Vector(0,scroll.Extent.Height);await Task.Delay(100);
            var button=Find(modal,"Thêm vào thư viện");var point=button.TranslatePoint(new Point(button.Bounds.Width/2,button.Bounds.Height/2),modal);
            Check(point.HasValue&&point.Value.Y>0&&point.Value.Y<Bounds.Height,"Long filename makes confirmation unreachable at 360x640");await Capture("external-small-preview");Click(button);await Task.Delay(120);
            Check(bank.Count==1&&Inbox.List().Count==0,"Small-screen confirmation failed");return "PASS phone preview 360x640: long Unicode filename scrolls, confirmation remains reachable and imports successfully.";
        }
        if(phase=="external-shell") {
            await WaitIncoming();await Capture("external-shell-01-cold-preview");Click(Find(modal,"Thêm vào thư viện"));await Task.Delay(150);Check(bank.Count==1&&bank[0].Number==8,"Cold command line did not import");
            string second=ExternalFixture("File thứ hai.quiz");var info=new ProcessStartInfo(Environment.ProcessPath){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden};info.ArgumentList.Add(second);
            using var process=Process.Start(info);await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(12));Check(process.ExitCode==0,"Secondary desktop process failed");
            await WaitIncoming();await Capture("external-shell-02-warm-preview");Click(Find(modal,"Thêm vào thư viện"));await Task.Delay(120);
            Check(library.List(out _).Count==2,"Warm shell open did not forward to existing process");
            return "PASS actual desktop executable: Unicode/spaced command-line path on cold launch; second process forwards file to live window; both imports confirmed and kept separately.";
        }
        if(phase=="external-restart") {
            Check(active&&answers[0]=="A"&&Inbox.List().Count==1&&currentDialog==null,"Pending file disrupted restored exam");
            Finish();await WaitIncoming();Click(Find(modal,"Thêm vào thư viện"));await Task.Delay(120);
            Check(bank[0].Number==8&&historyStore.Load(out _).Single().Correct==1,"Pending import lost answer/history on restart");
            return "PASS pending external copy and exam answer survive fresh process; preview waits for submission; confirmed import preserves completed attempt.";
        }
        PrepareExternalExam();var id=currentBankId.Value;var attempt=activeAttemptId;string source=ExternalFixture();byte[] frozen=File.ReadAllBytes(source);
        using(var stream=File.OpenRead(source))await MobilePlatform.ReceiveQuiz(stream,Path.GetFileName(source));await Task.Delay(150);
        Check(active&&activeAttemptId==attempt&&answers[0]=="A"&&currentDialog==null&&Inbox.List().Count==1,"Incoming file replaced active exam");await Capture("external-01-exam-waiting");
        if(phase=="external-hold"){PersistSession();return "PASS active attempt/checkpoint and incoming file kept for restart without displaying import dialog.";}
        Finish();await WaitIncoming();await Capture("external-02-preview");Click(Find(modal,"Để sau"));await Task.Delay(100);Check(Inbox.List().Count==1&&currentBankId==id,"Later imported or discarded file");
        Navigate("bank");Click(Controls<Button>(page).Single(b=>b.Content?.ToString()?.StartsWith("File nhận đang chờ")==true));await WaitIncoming();Click(Find(modal,"Bỏ qua"));await Task.Delay(100);
        Check(Inbox.List().Count==0&&library.List(out _).Count==1,"Reject changed library or retained pending file");
        using(var stream=File.OpenRead(source))await MobilePlatform.ReceiveQuiz(stream,Path.GetFileName(source));await WaitIncoming();Click(Find(modal,"Thêm vào thư viện"));await Task.Delay(150);
        Check(currentBankId!=id&&bank.Single().Number==8&&setup.Minutes==17&&setup.Included.SetEquals(new[]{8}),"Confirmed import lost questions/configuration");
        Check(library.Load(id).Content.Bank[0].Text=="Câu đang thi"&&historyStore.Load(out _).Single().Correct==1,"Import changed existing bank/attempt");
        Check(File.ReadAllBytes(source).SequenceEqual(frozen),"Source document modified");await Capture("external-03-library");
        using(var invalid=new MemoryStream(System.Text.Encoding.UTF8.GetBytes("<broken>")))await MobilePlatform.ReceiveQuiz(invalid,"invalid.quiz");await Task.Delay(200);
        Check(Inbox.List().Count==0&&library.List(out _).Count==2,"Invalid quiz damaged library or left repeating preview");
        return "PASS external file waits during exam; preview, defer/reopen, reject and confirm; independent library copy keeps setup, source file and attempt history; malformed input handled.";
    }
}
#endif
