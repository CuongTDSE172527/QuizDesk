using System.Text.Json;

namespace QuizDesk;

public sealed class BankFieldChange {
    public string Field {get;set;}="";
    public string Before {get;set;}="";
    public string After {get;set;}="";
}
public sealed class BankActivity {
    public DateTimeOffset At {get;set;}
    public string Kind {get;set;}="";
    public string Description {get;set;}="";
    public List<BankFieldChange> Changes {get;set;}=new();
}
public sealed class LibraryBank {
    public DateTimeOffset? DeletedAt {get;set;}
    public int DeletedQuestionCount {get;set;}
    public int Version {get;set;}=1;
    public Guid Id {get;set;}
    public DateTimeOffset CreatedAt {get;set;}
    public DateTimeOffset? ImportedAt {get;set;}
    public DateTimeOffset UpdatedAt {get;set;}
    public string SourceName {get;set;}="";
    public WorkspaceBank Content {get;set;}=new();
    public List<BankActivity> Activities {get;set;}=new();
}
public sealed record ImportedBank(string SourceName,WorkspaceBank Content);

public sealed record LibraryBankSummary(Guid Id,string Title,int Count,DateTimeOffset CreatedAt,DateTimeOffset? ImportedAt,DateTimeOffset UpdatedAt,string SourceName,DateTimeOffset? DeletedAt=null);

// A bank and its audit trail are committed together: a failed write cannot leave
// a new audit entry attached to older question contents (or vice versa).
public sealed class MobileBankLibrary {
    static readonly JsonSerializerOptions Json=new() {IncludeFields=true};
    static readonly Guid LegacyId=new("9ba50c90-cf67-49d2-988b-a4b761af1aba");
    readonly string folder;
    public MobileBankLibrary(string folder)=>this.folder=Path.GetFullPath(folder);
    string FileName(Guid id) {if(id==Guid.Empty)throw new InvalidDataException("Mã bộ đề không hợp lệ.");return Path.Combine(folder,id.ToString("N")+".json");}
    static WorkspaceBank Clone(WorkspaceBank bank)=>JsonSerializer.Deserialize<WorkspaceBank>(JsonSerializer.Serialize(bank,Json),Json);
    static void Validate(WorkspaceBank bank) {
        if(bank==null||bank.Version!=1||bank.Bank==null||bank.Setup==null||string.IsNullOrWhiteSpace(bank.Title))throw new InvalidDataException("Dữ liệu bộ đề không hợp lệ.");
        if(bank.Bank.Any(q=>q==null||q.Options==null||q.Answer==null||q.Note==null))throw new InvalidDataException("Nội dung câu hỏi không hợp lệ.");
        try{if(bank.Bank.Count>0)Importers.Validate(bank.Bank);}catch(Exception ex){throw new InvalidDataException("Nội dung câu hỏi không hợp lệ: "+ex.Message,ex);}
        bank.Setup.Included??=new();bank.Setup.Included.IntersectWith(bank.Bank.Select(q=>q.Number));
        bank.Setup.Count=Math.Clamp(bank.Setup.Count,1,Math.Max(1,bank.Bank.Count));bank.Setup.Minutes=Math.Clamp(bank.Setup.Minutes,1,300);
    }
    void Write(LibraryBank bank) {
        Directory.CreateDirectory(folder);string path=FileName(bank.Id),temp=path+".tmp";
        try {using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){JsonSerializer.Serialize(stream,bank,Json);stream.Flush(true);}File.Move(temp,path,true);}
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public LibraryBank Load(Guid id) {
        using var stream=InputSafety.OpenFile(FileName(id),128L*1024*1024);var bank=JsonSerializer.Deserialize<LibraryBank>(stream,Json);
        if(bank==null||bank.Id!=id||bank.Version!=1||bank.CreatedAt==default||bank.UpdatedAt==default||bank.Activities==null||bank.Activities.Any(a=>a==null||a.Changes==null||a.At==default))throw new InvalidDataException("Không đọc được bộ đề trong thư viện.");
        Validate(bank.Content);bank.Content.LibraryBankId=id;return bank;
    }
    public List<LibraryBankSummary> List(out string warning,bool includeDeleted=false) {
        warning="";var items=new List<LibraryBankSummary>();if(!Directory.Exists(folder))return items;
        var failures=new List<string>();
        foreach(string file in Directory.EnumerateFiles(folder,"*.json")) {
            if(!Guid.TryParseExact(Path.GetFileNameWithoutExtension(file),"N",out var id))continue;
            try {var bank=Load(id);if(!includeDeleted&&bank.DeletedAt.HasValue)continue;items.Add(new(id,bank.Content.Title,bank.DeletedAt.HasValue?bank.DeletedQuestionCount:bank.Content.Bank.Count,bank.CreatedAt,bank.ImportedAt,bank.UpdatedAt,bank.SourceName,bank.DeletedAt));}
            catch(Exception ex) when(ex is IOException||ex is InvalidDataException||ex is UnauthorizedAccessException||ex is JsonException){failures.Add(Path.GetFileName(file));}
        }
        if(failures.Count>0)warning=$"Không đọc được {failures.Count} bộ đề. File gốc vẫn được giữ nguyên.";
        return items.OrderByDescending(b=>b.UpdatedAt).ThenBy(b=>b.Id).ToList();
    }
    public LibraryBank Create(WorkspaceBank content,string sourceName="",bool imported=false)=>Create(content,sourceName,imported,Guid.NewGuid(),false);
    public bool Contains(Guid id)=>File.Exists(FileName(id));
    public LibraryBank CreateIncoming(Guid id,WorkspaceBank content,string sourceName) {
        if(Contains(id))return Load(id);
        return Create(content,sourceName,true,id,false);
    }
    public LibraryBank Merge(IEnumerable<Guid> ids,string title,bool shuffle,AppPreferences defaults=null,Random random=null) {
        if(ids==null)throw new InvalidDataException("Hãy chọn ít nhất 2 bộ đề.");
        var selected=ids.Distinct().ToArray();
        InputSafety.Require(selected.Length>=2&&selected.Length<=InputSafety.MaxQuestions,"Hãy chọn ít nhất 2 bộ đề khác nhau.");
        title=title?.Trim();InputSafety.Require(!string.IsNullOrWhiteSpace(title)&&title.Length<=200,"Tên bộ đề cần từ 1 đến 200 ký tự.");
        var sources=selected.Select(Load).ToArray();
        return MergeSources(sources,title,shuffle,defaults,random,false);
    }
    // Parse every file independently before this call: answer tables are scoped to their source.
    public LibraryBank ImportCombined(IEnumerable<ImportedBank> imports,string title,bool shuffle,AppPreferences defaults=null,Random random=null) {
        var files=imports?.ToArray()??throw new InvalidDataException("Chưa chọn file đề.");
        InputSafety.Require(files.Length>=2&&files.Length<=20,"Chọn từ 2 đến 20 file để gộp.");
        title=title?.Trim();InputSafety.Require(!string.IsNullOrWhiteSpace(title)&&title.Length<=200,"Tên bộ đề cần từ 1 đến 200 ký tự.");
        foreach(var file in files){InputSafety.Require(file!=null&&file.Content!=null,"File đề không hợp lệ.");Validate(file.Content);}
        var sources=files.Select(f=>new LibraryBank {Content=f.Content,SourceName=Path.GetFileName(f.SourceName??"")}).ToArray();
        return MergeSources(sources,title,shuffle,defaults,random,true);
    }
    LibraryBank MergeSources(LibraryBank[] sources,string title,bool shuffle,AppPreferences defaults,Random random,bool imported) {
        MediaFiles.Check(sources.SelectMany(b=>b.Content.Bank).SelectMany(q=>q.Media));
        InputSafety.Require(sources.All(b=>!b.DeletedAt.HasValue&&b.Content.Bank.Count>0),"Chỉ gộp các bộ đề còn tồn tại và có câu hỏi.");
        InputSafety.Require(sources.Sum(b=>(long)b.Content.Bank.Count)<=InputSafety.MaxQuestions,"Bộ đề gộp tối đa 5.000 câu. Hãy chọn ít bộ đề hơn.");
        long characters=sources.Sum(b=>b.Content.Bank.Sum(q=>(long)q.Text.Length+q.Note.Length+q.Answer.Length+(q.OriginalAnswer?.Length??0)+(q.AnswerSource?.Length??0)+q.Options.Sum(o=>(long)o.Key.Length+o.Value.Length)+(q.ImportWarnings?.Sum(w=>(long)w.Length)??0)));
        InputSafety.Require(characters<=InputSafety.MaxTextCharacters,"Nội dung bộ đề gộp quá lớn. Hãy chọn ít bộ đề hơn.");
        static string OriginTitle(LibraryBank b)=>b.Content.Title.Length>200?b.Content.Title[..197]+"…":b.Content.Title;
        var pool=new List<(Question question,string origin)>();
        foreach(var source in sources)foreach(var q in source.Content.Bank){
            var copy=new Question {Text=q.Text,Note=q.Note,Answer=q.Answer,OriginalAnswer=q.OriginalAnswer,AnswerSource=q.AnswerSource,NeedsReview=q.NeedsReview,Options=new(q.Options),Media=q.Media.Select(m=>m.Clone()).ToList(),ImportWarnings=new(q.ImportWarnings??new())};
            pool.Add((copy,$"{OriginTitle(source)}"+(imported?$" [{Array.IndexOf(sources,source)+1}: {source.SourceName}]":"")+$" · Câu gốc {q.Number}"));
        }
        if(shuffle){random??=Random.Shared;for(int i=pool.Count-1;i>0;i--){int j=random.Next(i+1);(pool[i],pool[j])=(pool[j],pool[i]);}}
        var origins=new List<BankFieldChange>();
        for(int i=0;i<pool.Count;i++){pool[i].question.Number=i+1;origins.Add(new(){Field=$"Câu {i+1} · Nguồn",Before=pool[i].origin,After=$"Câu {i+1}"});}
        var questions=pool.Select(p=>p.question).ToList();var setup=(defaults??new AppPreferences()).CreateSetup(questions);
        setup.ShuffleQuestions=shuffle;setup.ShuffleOptions=false;
        var content=new WorkspaceBank {Title=title,Bank=questions,Setup=setup,HasUnexportedChanges=true};
        var activity=new BankActivity {Kind="merged",Description=$"Gộp {sources.Length} bộ đề · {questions.Count} câu · "+(shuffle?"Trộn thứ tự câu hỏi.":"Giữ thứ tự các bộ nguồn.")+"\n"+string.Join("\n",sources.Select(b=>$"• {OriginTitle(b)}: {b.Content.Bank.Count} câu")),Changes=origins};
        return Create(content,imported?"Nhập nhiều file":"",imported,Guid.NewGuid(),false,activity);
    }
    LibraryBank Create(WorkspaceBank content,string sourceName,bool imported,Guid id,bool migrated,BankActivity extraActivity=null) {
        var snapshot=Clone(content);Validate(snapshot);snapshot.LibraryBankId=id;var now=DateTimeOffset.UtcNow;
        var bank=new LibraryBank {Id=id,Content=snapshot,CreatedAt=now,UpdatedAt=now,ImportedAt=imported?now:null,SourceName=Path.GetFileName(sourceName??"")};
        bank.Activities.Add(new() {At=now,Kind=migrated?"migrated":imported?"imported":"created",Description=migrated?"Chuyển bản nháp từ phiên bản cũ. Không có dữ liệu về thời điểm nhập ban đầu.":imported?$"Nhập file “{bank.SourceName}” · {snapshot.Bank.Count} câu hỏi.":"Tạo bộ đề mới."});
        if(extraActivity!=null){extraActivity.At=now;bank.Activities.Add(extraActivity);}
        Write(bank);return bank;
    }
    public LibraryBank MigrateLegacy(WorkspaceBank legacy) {
        if(File.Exists(FileName(LegacyId)))return Load(LegacyId);
        return Create(legacy,"",false,LegacyId,true);
    }
    public LibraryBank Save(Guid id,WorkspaceBank content) {
        var snapshot=Clone(content);Validate(snapshot);snapshot.LibraryBankId=id;
        var bank=Load(id);if(bank.DeletedAt.HasValue)throw new InvalidOperationException("Bộ đề đã bị xóa. Không thể tự lưu lại nội dung cũ.");var changes=Diff(bank.Content,snapshot);
        if(changes.Count==0&&bank.Content.HasUnexportedChanges==snapshot.HasUnexportedChanges)return bank;
        if(changes.Count>0) {
            var now=DateTimeOffset.UtcNow;bank.UpdatedAt=now;
            bank.Activities.Add(new() {At=now,Kind="changed",Description=Describe(bank.Content,snapshot),Changes=changes});
        }
        bank.Content=snapshot;Write(bank);return bank;
    }
    public void Delete(Guid id) {
        var bank=Load(id);if(bank.DeletedAt.HasValue)return;
        var now=DateTimeOffset.UtcNow;bank.DeletedAt=now;bank.UpdatedAt=now;bank.DeletedQuestionCount=bank.Content.Bank.Count;
        // Keep only metadata needed for import/deletion history, never deleted
        // question text or old before/after snapshots. Commit atomically.
        bank.Content.Bank=new();bank.Content.Setup=new(){Count=1};bank.Content.HasUnexportedChanges=false;
        bank.Activities=bank.Activities.Where(a=>a.Kind is "imported" or "created" or "migrated").ToList();
        bank.Activities.Add(new(){At=now,Kind="deleted",Description=$"Xóa bộ đề “{bank.Content.Title}” · {bank.DeletedQuestionCount} câu hỏi."});Write(bank);
    }
    static string Describe(WorkspaceBank before,WorkspaceBank after) {
        var oldIds=before.Bank.Select(q=>q.Number).ToHashSet();var newIds=after.Bank.Select(q=>q.Number).ToHashSet();
        int added=newIds.Except(oldIds).Count(),removed=oldIds.Except(newIds).Count();
        if(before.Bank.Count>0&&after.Bank.Count==0)return $"Xóa tất cả {before.Bank.Count} câu hỏi trong bộ đề.";
        if(added>0||removed>0)return $"Cập nhật câu hỏi · thêm {added}, xóa {removed}.";
        if(before.Title!=after.Title)return "Đổi tên bộ đề.";
        if(before.Bank.Where((q,i)=>QuestionText(q)!=QuestionText(after.Bank[i])).Any())return "Chỉnh sửa nội dung câu hỏi, đáp án hoặc giải thích.";
        return "Thay đổi câu được chọn hoặc cấu hình bài thi.";
    }
    static string QuestionText(Question q)=>q.Text+"\n"+string.Join("\n",q.Options.OrderBy(o=>o.Key).Select(o=>o.Key+". "+o.Value))+"\nĐáp án: "+q.Answer+"\nGiải thích: "+q.Note+"\nMedia: "+MediaFiles.Describe(q.Media);
    static List<BankFieldChange> Diff(WorkspaceBank before,WorkspaceBank after) {
        var result=new List<BankFieldChange>();
        void Change(string field,object oldValue,object newValue){string old=oldValue?.ToString()??"",next=newValue?.ToString()??"";if(old!=next)result.Add(new(){Field=field,Before=old,After=next});}
        Change("Tên bộ đề",before.Title,after.Title);
        var oldQuestions=before.Bank.ToDictionary(q=>q.Number);var newQuestions=after.Bank.ToDictionary(q=>q.Number);
        foreach(var q in before.Bank.Where(q=>!newQuestions.ContainsKey(q.Number)))Change("Xóa câu "+q.Number,QuestionText(q),"");
        foreach(var q in after.Bank) {
            if(!oldQuestions.TryGetValue(q.Number,out var old)){Change("Thêm câu "+q.Number,"",QuestionText(q));continue;}
            Change($"Câu {q.Number} · Nội dung",old.Text,q.Text);
            foreach(string key in old.Options.Keys.Union(q.Options.Keys).Order())Change($"Câu {q.Number} · Lựa chọn {key}",old.Options.GetValueOrDefault(key),q.Options.GetValueOrDefault(key));
            Change($"Câu {q.Number} · Media",MediaFiles.Describe(old.Media),MediaFiles.Describe(q.Media));
            Change($"Câu {q.Number} · Đáp án đúng",old.Answer,q.Answer);Change($"Câu {q.Number} · Giải thích / ghi chú",old.Note,q.Note);
            Change($"Câu {q.Number} · Nguồn đáp án",old.AnswerSource,q.AnswerSource);Change($"Câu {q.Number} · Đáp án gốc",old.OriginalAnswer,q.OriginalAnswer);Change($"Câu {q.Number} · Cần xem lại",old.NeedsReview,q.NeedsReview);
            Change($"Câu {q.Number} · Cảnh báo khi nhập",string.Join("\n",old.ImportWarnings??new()),string.Join("\n",q.ImportWarnings??new()));
        }
        Change("Thứ tự câu hỏi",string.Join(", ",before.Bank.Select(q=>q.Number)),string.Join(", ",after.Bank.Select(q=>q.Number)));
        Change("Câu được chọn",string.Join(", ",before.Setup.Included.Order()),string.Join(", ",after.Setup.Included.Order()));
        Change("Số câu thi thử",before.Setup.Count,after.Setup.Count);Change("Thời gian (phút)",before.Setup.Minutes,after.Setup.Minutes);
        Change("Trộn câu hỏi",before.Setup.ShuffleQuestions,after.Setup.ShuffleQuestions);Change("Trộn lựa chọn",before.Setup.ShuffleOptions,after.Setup.ShuffleOptions);return result;
    }
}
