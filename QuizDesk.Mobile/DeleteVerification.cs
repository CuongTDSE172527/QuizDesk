#if QA
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Text.Json;

namespace QuizDesk.Mobile;
public partial class MainView {
    static WorkspaceBank DeleteSample(string name)=>new(){Title=name,Bank=new(){new Question {Number=1,Text="Nội dung cần giữ trong lịch sử thi",Answer="B",Note="Giải thích gốc",Options=new(){{"A","Một"},{"B","Hai"}}}},Setup=new(){Count=1,Minutes=30,ShuffleQuestions=false,Included=new(){1}}};
    async Task<string> VerifyDelete(string phase) {
        if(phase=="delete-restart") {
            Guid keep=Guid.Parse(File.ReadAllText(Path.Combine(dataRoot,"delete-kept-id.txt")));
            Check(currentBankId==null&&bank.Count==0&&workspace.LoadBank(out _).LibraryBankId==null,"Deleted current bank restored from stale workspace");
            Check(library.List(out _).Single().Id==keep&&historyStore.Load(out _).Count==1,"Restart changed remaining bank or attempt history");
            PersistSession();Check(library.List(out _).Count==1,"Autosave resurrected deleted bank");Navigate("bank");await Capture("delete-06-restart");
            return "PASS: crash-window stale workspace referencing deleted bank resets safely on startup; no resurrection; unselected bank and attempt history survive.";
        }
        var a=library.Create(DeleteSample("Bộ đề nhập để xóa"),"de-a.docx",true);var b=library.Create(DeleteSample("Bộ đề tự tạo để xóa"));var keepBank=library.Create(DeleteSample("Bộ đề được giữ lại"));
        AdoptBank(a);RememberSelectedBank();Start(false);ChooseAnswer("B");Finish();Check(historyStore.Load(out _).Count==1,"Attempt fixture missing");
        AdoptBank(b);RememberSelectedBank();Navigate("bank");await Capture("delete-01-library");Click(Find(page,"Chọn bộ đề"));await Task.Delay(100);
        Check(!deleteBanksButton.IsEnabled&&libraryChecks.Count==3,"Selection should begin empty");libraryChecks[a.Id].IsChecked=true;libraryChecks[b.Id].IsChecked=true;
        Check(selectedBanks.Count==2&&deleteBanksButton.IsEnabled,"Checkbox selection did not enable delete");await Capture("delete-02-selected");
        Click(deleteBanksButton);await Task.Delay(100);Check(currentDialog!=null,"Delete confirmation missing");await Capture("delete-03-confirm");Click(Find(modal,"Hủy"));await Task.Delay(100);
        Check(library.List(out _).Count==3&&selectedBanks.Count==2,"Cancel deleted a bank or lost selection");
        Click(deleteBanksButton);await Task.Delay(100);Click(Find(modal,"Xóa bộ đề"));await Task.Delay(120);
        Check(library.List(out _).Single().Id==keepBank.Id&&currentBankId==null&&bank.Count==0,"Confirmed delete removed wrong bank or kept current draft");
        Check(historyStore.Load(out _).Single().Title==a.Content.Title&&historyStore.Load(out _).Single().Questions[0].Text==a.Content.Bank[0].Text,"Bank deletion changed saved attempt snapshot");
        Check(!selectingBanks&&selectedBanks.Count==0&&!library.Load(a.Id).Content.Bank.Any()&&!library.Load(b.Id).Content.Bank.Any(),"Delete did not purge content or leave selection mode");await Capture("delete-04-remaining");
        ShowImportHistory();await Task.Delay(100);Check(Controls<TextBlock>(modal).Count(t=>(t.Text??"").StartsWith("Đã xóa:"))==2,"Deletion history missing timestamped imported/created bank receipts");await Capture("delete-05-history");currentDialog.Close(false);await Task.Delay(80);
        File.WriteAllText(Path.Combine(dataRoot,"delete-kept-id.txt"),keepBank.Id.ToString());
        var stale=b.Content;stale.LibraryBankId=b.Id;File.WriteAllText(Path.Combine(dataRoot,"delete-stale-workspace.json"),JsonSerializer.Serialize(stale,new JsonSerializerOptions {IncludeFields=true}));
        return "PASS: select two banks, cancel, confirm, purge only selected banks including current draft, preserve unselected bank and attempt snapshots, retain timestamped import/deletion receipts.";
    }
    async Task<string> VerifyDesktopLayout() {
        Check(MobilePlatform.DesktopHost,"Test must use actual desktop host");
        var lifetime=(IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime;var window=lifetime.MainWindow;
        window.Width=1280;window.Height=860;Navigate("home");await Capture("desktop-01-home");Check(wideLayout==true&&shell.ColumnDefinitions.Count==2,"Desktop navigation rail missing");
        AdoptBank(library.Create(DeleteSample("Bộ đề dùng chung · Windows / macOS")));RememberSelectedBank();Navigate("bank");await Capture("desktop-02-library");OpenLibraryBank(currentBankId.Value);await Capture("desktop-03-editor");
        Navigate("setup");Start(true);ChooseAnswer("B");await Capture("desktop-04-practice");Finish();
        preferences.Theme="dark";ApplyTheme();Navigate("preferences");await Capture("desktop-05-settings-dark");
        window.Width=760;await Task.Delay(100);Check(wideLayout==false&&shell.ColumnDefinitions.Count==1,"Narrow desktop did not switch to bottom navigation");await Capture("desktop-06-narrow");
        window.Width=1280;await Task.Delay(100);Check(wideLayout==true,"Desktop rail did not restore after resize");
        return "PASS: actual desktop host shares all Material 3 pages; rail at 1280dp, bottom navigation at 760dp, library editor/practice/theme and resize work.";
    }
}
#endif
