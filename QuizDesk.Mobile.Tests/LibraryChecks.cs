using QuizDesk;
partial class Tests {
    static WorkspaceBank LibrarySample(string title="VOR")=>new() {Title=title,Bank=new(){new Question {Number=1,Text="Chọn phương vị",Answer="A",Note="Giải thích ban đầu",Options=new(){{"A","Phương vị"},{"B","Độ cao"}}}},Setup=new(){Count=1,Minutes=30,Included=new(){1}}};
    static void LibraryChecks(string root) {
        string folder=Path.Combine(root,Guid.NewGuid().ToString("N"));var store=new MobileBankLibrary(folder);
        Check(store.List(out var warning).Count==0&&warning=="","Missing library is empty");
        var before=DateTimeOffset.UtcNow;var input=LibrarySample();var first=store.Create(input,"VOR.docx",true);var second=store.Create(LibrarySample("VOR lần hai"),"VOR.docx",true);
        Check(first.Id!=second.Id&&store.List(out _).Count==2,"Repeated imports with identical filenames preserve two independent banks");
        Check(first.ImportedAt>=before&&first.CreatedAt.Offset==TimeSpan.Zero&&first.Activities[0].At==first.ImportedAt&&first.SourceName=="VOR.docx","Import stores UTC timestamp and source filename");
        input.Bank[0].Text="Changed outside store";Check(store.Load(first.Id).Content.Bank[0].Text=="Chọn phương vị","Creation snapshots mutable questions");
        var edited=store.Load(first.Id);edited.Content.Bank[0].Answer="B";edited.Content.Bank[0].Note="Giải thích đã sửa";edited.Content.Bank[0].Options["B"]="Độ cao mới";
        var updated=store.Save(first.Id,edited.Content);var changes=updated.Activities[^1].Changes;
        Check(changes.Any(c=>c.Field.Contains("Đáp án đúng")&&c.Before=="A"&&c.After=="B")&&changes.Any(c=>c.Field.Contains("Giải thích")&&c.Before=="Giải thích ban đầu"&&c.After=="Giải thích đã sửa"),"Answer and explanation history retains exact before/after");
        Check(changes.Any(c=>c.Field.Contains("Lựa chọn B")&&c.Before=="Độ cao"&&c.After=="Độ cao mới"),"Option edits retain before/after");
        Check(store.Load(second.Id).Content.Bank[0].Answer=="A"&&store.Load(second.Id).Activities.Count==1,"Editing first bank does not touch second bank or its history");
        var date=updated.UpdatedAt;int events=updated.Activities.Count;store.Save(first.Id,updated.Content);updated.Content.HasUnexportedChanges=true;store.Save(first.Id,updated.Content);updated.Content.HasUnexportedChanges=false;updated=store.Save(first.Id,updated.Content);
        Check(updated.Activities.Count==events&&updated.UpdatedAt==date,"Autosave and export flags do not create false modification dates/history");
        updated.Content.Title="VOR đổi tên";updated=store.Save(first.Id,updated.Content);
        Check(updated.Activities[^1].Description=="Đổi tên bộ đề."&&updated.Activities[^1].Changes.Single().Before=="VOR","Rename is audited");
        updated.Content.Setup.Included.Clear();updated.Content.Setup.Minutes=45;updated=store.Save(first.Id,updated.Content);
        Check(updated.Activities[^1].Changes.Any(c=>c.Field=="Câu được chọn")&&updated.Activities[^1].Changes.Any(c=>c.Field=="Thời gian (phút)"&&c.After=="45"),"Selection and exam settings are audited");
        updated.Content.Bank.Add(new Question {Number=2,Text="Câu mới",Answer="B",Options=new(){{"A","Một"},{"B","Hai"}}});updated=store.Save(first.Id,updated.Content);
        Check(updated.Activities[^1].Changes.Any(c=>c.Field=="Thêm câu 2"&&c.After.Contains("Câu mới")),"Added question is audited");
        updated.Content.Bank.RemoveAt(0);updated=store.Save(first.Id,updated.Content);
        Check(updated.Activities[^1].Changes.Any(c=>c.Field=="Xóa câu 1"&&c.Before.Contains("Giải thích đã sửa")),"Deleted question history retains removed contents");
        updated.Content.Bank.Clear();updated=store.Save(first.Id,updated.Content);
        Check(updated.Activities[^1].Description.Contains("Xóa tất cả 1")&&store.Load(second.Id).Content.Bank.Count==1,"Clear all affects current bank only and is audited");
        var restarted=new MobileBankLibrary(folder).Load(first.Id);
        Check(restarted.Content.Title=="VOR đổi tên"&&restarted.Content.Bank.Count==0&&restarted.Activities.Count==updated.Activities.Count,"Fresh library instance restores all banks and audit entries");
        var workspace=new MobileWorkspace(Path.Combine(folder,"workspace"));workspace.SaveBank(second.Content.Title,second.Content.Bank,second.Content.Setup,false,second.Id);
        Check(workspace.LoadBank(out _).LibraryBankId==second.Id,"Workspace preserves selected library bank identity");
        workspace.Begin(new ActiveMobileExam {LibraryBankId=second.Id,Title=second.Content.Title,Exam=second.Content.Bank,Minutes=30,StartedAt=DateTimeOffset.UtcNow,Deadline=DateTimeOffset.UtcNow.AddMinutes(30)});
        Check(workspace.LoadActive(out _).LibraryBankId==second.Id,"Active exam preserves source library identity");
        string blocked=Path.Combine(folder,second.Id.ToString("N")+".json.tmp");Directory.CreateDirectory(blocked);var pending=store.Load(second.Id);pending.Content.Title="Should not commit";bool failed=false;
        try{store.Save(second.Id,pending.Content);}catch(IOException){failed=true;}catch(UnauthorizedAccessException){failed=true;}
        Check(failed&&store.Load(second.Id).Content.Title=="VOR lần hai"&&store.Load(second.Id).Activities.Count==1,"Failed atomic write preserves previous questions and audit together");Directory.Delete(blocked);
        string corrupt=Path.Combine(folder,Guid.NewGuid().ToString("N")+".json");File.WriteAllText(corrupt,"{broken");
        Check(store.List(out warning).Count==2&&warning!=""&&File.ReadAllText(corrupt)=="{broken","Corrupt bank is reported and preserved without hiding healthy banks");
        var legacy=LibrarySample("Bản nháp cũ");legacy.HasUnexportedChanges=true;var migrated=store.MigrateLegacy(legacy);legacy.Title="Do not remigrate";var again=store.MigrateLegacy(legacy);
        Check(again.Id==migrated.Id&&again.Content.Title=="Bản nháp cũ"&&again.Activities.Count==1,"Legacy migration is idempotent after interrupted workspace update");
        Check(again.ImportedAt==null&&again.Activities[0].Kind=="migrated"&&again.Content.HasUnexportedChanges,"Migration preserves dirty draft without fabricating original import date");
        var invalid=LibrarySample();invalid.Bank[0].Answer="H";failed=false;try{store.Create(invalid,"bad.docx",true);}catch(InvalidDataException){failed=true;}
        Check(failed&&store.List(out _).Count==3,"Invalid import does not create an empty library entry");
        Check(!Directory.EnumerateFiles(folder,"*.tmp").Any(),"Committed library leaves no partial temporary files");
        var deletions=new MobileBankLibrary(Path.Combine(folder,"delete-case"));var deleting=deletions.Create(LibrarySample("Đề xóa"),"de.docx",true);var kept=deletions.Create(LibrarySample("Đề giữ"));
        var stale=deletions.Load(deleting.Id).Content;stale.Bank[0].Note="Private old explanation";deletions.Save(deleting.Id,stale);deletions.Delete(deleting.Id);
        Check(deletions.List(out _).Single().Id==kept.Id,"Deleting a bank preserves every unselected bank");
        var receipt=deletions.Load(deleting.Id);Check(receipt.DeletedAt.HasValue&&receipt.DeletedAt.Value.Offset==TimeSpan.Zero&&receipt.ImportedAt.HasValue&&receipt.SourceName=="de.docx"&&receipt.DeletedQuestionCount==1,"Deletion retains timestamp and original import metadata");
        Check(receipt.Content.Bank.Count==0&&receipt.Content.Setup.Included.Count==0&&receipt.Activities.Last().Kind=="deleted"&&!receipt.Activities.Any(a=>a.Kind=="changed"),"Deleted bank purges questions, selection and old question audit payloads");
        string payload=File.ReadAllText(Path.Combine(folder,"delete-case",deleting.Id.ToString("N")+".json"));Check(!payload.Contains("Private old explanation"),"Deleted explanation does not remain in file");
        Check(deletions.List(out _,true).Count==2,"Import/deletion history remains available separately from active library");
        int deletionEvents=receipt.Activities.Count;deletions.Delete(deleting.Id);Check(deletions.Load(deleting.Id).Activities.Count==deletionEvents,"Repeated delete is idempotent");
        failed=false;try{deletions.Save(deleting.Id,stale);}catch(InvalidOperationException){failed=true;}Check(failed&&deletions.Load(deleting.Id).Content.Bank.Count==0,"Stale autosave cannot resurrect a deleted bank");
        string failedDelete=Path.Combine(folder,"delete-case",kept.Id.ToString("N")+".json.tmp");Directory.CreateDirectory(failedDelete);failed=false;
        try{deletions.Delete(kept.Id);}catch(IOException){failed=true;}catch(UnauthorizedAccessException){failed=true;}Directory.Delete(failedDelete);
        Check(failed&&deletions.List(out _).Single().Id==kept.Id&&deletions.Load(kept.Id).Content.Bank.Count==1,"Failed delete preserves bank contents and visibility");
        var oldDraft=deletions.MigrateLegacy(LibrarySample("Legacy deleted"));deletions.Delete(oldDraft.Id);Check(deletions.MigrateLegacy(LibrarySample()).DeletedAt.HasValue,"Legacy migration respects deleted bank marker");
        Console.WriteLine("TOTAL "+checks+" library checks passed.");
    }
}
