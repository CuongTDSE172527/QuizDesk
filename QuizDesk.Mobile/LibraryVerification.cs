#if QA
using Avalonia.Controls;
using Avalonia.Platform;

namespace QuizDesk.Mobile;
public partial class MainView {
    async Task<string> VerifyLibrary(string phase) {
        var culture=System.Globalization.CultureInfo.CurrentCulture;
        try{System.Globalization.CultureInfo.CurrentCulture=new System.Globalization.CultureInfo("th-TH");Check(Stamp(new DateTimeOffset(new DateTime(2026,10,21,12,34,56,DateTimeKind.Local)))=="21/10/2026 · 12:34:56","Library dates must keep Gregorian year and full time across device locales");}
        finally{System.Globalization.CultureInfo.CurrentCulture=culture;}
        if(phase=="library-migration") {
            var legacy=System.Text.Json.JsonSerializer.Deserialize<WorkspaceBank>(File.ReadAllText(Path.Combine(dataRoot,"legacy-source.json")),new System.Text.Json.JsonSerializerOptions {IncludeFields=true});
            Check(currentBankId.HasValue&&library.List(out _).Count==1,"Startup did not migrate legacy workspace exactly once");
            var migrated=library.Load(currentBankId.Value);
            Check(migrated.ImportedAt==null&&migrated.Activities.Count==1&&migrated.Activities[0].Kind=="migrated","Migration fabricated import time/history");
            Check(bank.Count==legacy.Bank.Count&&bank[0].Text==legacy.Bank[0].Text&&bank[0].Note==legacy.Bank[0].Note&&title==legacy.Title&&setup.Minutes==legacy.Setup.Minutes,"Migration lost legacy contents/settings");
            string identity=Path.Combine(dataRoot,"migration-identity.txt");if(File.Exists(identity))Check(Guid.Parse(File.ReadAllText(identity))==currentBankId.Value,"Restart migrated the same draft twice");File.WriteAllText(identity,currentBankId.Value.ToString());
            Navigate("bank");Check(questionList==null,"Migrated editor must start collapsed");await Capture("library-migrated");
            return "PASS: the actual app startup migrates the 5.1 workspace once; title/questions/explanations/settings survive; original import time stays unknown; fresh process keeps the same bank identity and collapsed library.";
        }
        if(phase=="library-restart") {
            Guid expected=Guid.Parse(File.ReadAllText(Path.Combine(dataRoot,"library-expected-id.txt")));
            Check(currentBankId==expected&&bank[0].Text=="Câu hỏi đã biên soạn trong thư viện","Selected library/edited content not restored in fresh process");
            Check(library.List(out _).Count==2&&library.Load(expected).Content.Title=="VOR đã biên soạn","Library not restored");
            int expectedCount=int.Parse(File.ReadAllText(Path.Combine(dataRoot,"library-event-count.txt")));
            Check(library.Load(expected).Activities.Count==expectedCount,"Restart created a false modification record");
            Navigate("bank");Check(questionList==null&&expandedBankId==null,"Library editor automatically expanded on restart");await Capture("library-06-restored-collapsed");
            Click(Controls<Button>(page).Single(b=>b.Name=="LibraryBank"+expected.ToString("N")));Check(questionList!=null&&expandedBankId==expected,"Tapping bank did not expand restored editor");
            return "PASS: a new process restores both banks, selected bank identity, edited questions, name and exact audit history; library remains collapsed until tapped.";
        }
        preferences.Theme="light";ApplyTheme();
        async Task ImportSample() {
            using var input=AssetLoader.Open(new Uri("avares://QuizDesk.Mobile/Assets/De-mau.txt"));string path=await MobileImportCache.CopyAsync(input,"De-mau.txt",Path.Combine(dataRoot,"ImportCache"));
            try{await Load(path,"VOR.docx");}finally{File.Delete(path);}
        }
        await ImportSample();Guid first=currentBankId.Value;string oldNote=bank[0].Note;await ImportSample();Guid second=currentBankId.Value;
        Check(first!=second&&library.List(out _).Count==2,"Second import replaced the first bank");
        Check(questionList==null&&expandedBankId==null&&!Controls<TextBox>(page).Any(),"Imported library should be collapsed");await Capture("library-01-collapsed");
        Click(Controls<Button>(page).Single(b=>b.Name=="LibraryBank"+first.ToString("N")));
        await Task.Delay(180);
        Check(currentBankId==first&&expandedBankId==first&&questionList!=null,"Bank tap did not select and expand editor");
        Click(Find(page,"Sửa"));await Task.Delay(100);Controls<TextBox>(modal).Single(b=>b.Name=="QuestionText").Text="Câu hỏi đã biên soạn trong thư viện";Controls<TextBox>(modal).Single(b=>b.Name=="QuestionNote").Text="Giải thích đã cập nhật và lưu riêng";
        Click(Find(modal,"Lưu câu hỏi"));await Task.Delay(100);
        var saved=library.Load(first);Check(saved.Activities[^1].Changes.Any(c=>c.Field.Contains("Giải thích")&&c.Before==oldNote&&c.After.Contains("lưu riêng")),"UI edit did not append precise audit detail");await Capture("library-02-expanded");
        OpenLibraryBank(second);await Task.Delay(180);Check(bank[0].Note==oldNote&&Controls<ListBox>(page).Count()==1,"Switching bank lost independence or left two editors open");
        OpenLibraryBank(first);RenameBank();await Task.Delay(100);Controls<TextBox>(modal).Single().Text="VOR đã biên soạn";Click(Find(modal,"Lưu tên"));await Task.Delay(100);
        ShowBankActivity(first);await Task.Delay(150);Check(currentDialog!=null&&Controls<TextBlock>(modal).Any(t=>t.Text=="Đổi tên bộ đề."),"Audit UI did not show rename");
        var changePanel=Controls<Expander>(modal).First();changePanel.IsExpanded=true;await Capture("library-03-change-history");currentDialog.Close(false);await Task.Delay(80);
        ShowImportHistory();await Task.Delay(150);Check(Controls<TextBlock>(modal).Count(t=>t.Text=="File nguồn: VOR.docx")==2,"Import history did not retain both source filenames");await Capture("library-04-import-history");currentDialog.Close(false);await Task.Delay(80);
        OpenLibraryBank(first);Check(expandedBankId==null&&questionList==null,"Second tap did not collapse editor");await Capture("library-05-collapsed-edited");PersistSession();
        File.WriteAllText(Path.Combine(dataRoot,"library-expected-id.txt"),first.ToString());File.WriteAllText(Path.Combine(dataRoot,"library-event-count.txt"),library.Load(first).Activities.Count.ToString());
        return "PASS: same-filename imports remain independent; library initially collapsed; tapping selects/expands one editor; edits and rename persist with exact before/after and timestamped audit; global import history retains both entries; second tap collapses without losing data.";
    }
}
#endif
