using System.Text.Json;

namespace QuizDesk;

public sealed class WorkspaceBank {
    public Guid? LibraryBankId {get;set;}
    public int Version {get;set;}=1;
    public string Title {get;set;}="Bộ đề chưa đặt tên";
    public bool HasUnexportedChanges {get;set;}
    public List<Question> Bank {get;set;}=new();
    public QuizSetup Setup {get;set;}=new();
}
public sealed class ActiveMobileExam {
    public Guid? LibraryBankId {get;set;}
    public int Version {get;set;}=1;
    public Guid Id {get;set;}=Guid.NewGuid();
    public string Title {get;set;}="";
    public DateTimeOffset StartedAt {get;set;}
    public DateTimeOffset Deadline {get;set;}
    public bool Practice {get;set;}
    public int Minutes {get;set;}
    public List<Question> Exam {get;set;}=new();
    public int Index {get;set;}
    public Dictionary<int,string> Answers {get;set;}=new();
    public HashSet<int> Flagged {get;set;}=new();
}
public sealed class MobileExamProgress {
    public Guid Id {get;set;}
    public int Index {get;set;}
    public Dictionary<int,string> Answers {get;set;}=new();
    public HashSet<int> Flagged {get;set;}=new();
}
public sealed class MobileWorkspace {
    static readonly JsonSerializerOptions Json=new() {IncludeFields=true};
    public string Folder {get;}
    public MobileWorkspace(string folder)=>Folder=Path.GetFullPath(folder);
    void Write<T>(string name,T value) {
        Directory.CreateDirectory(Folder);string path=Path.Combine(Folder,name),temp=path+".tmp";
        try {using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){JsonSerializer.Serialize(stream,value,Json);stream.Flush(true);}File.Move(temp,path,true);}
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
    T Read<T>(string name) {string path=Path.Combine(Folder,name);if(!File.Exists(path))return default;using var stream=InputSafety.OpenFile(path,128L*1024*1024);return JsonSerializer.Deserialize<T>(stream,Json);}
    public void SaveBank(string title,List<Question> bank,QuizSetup setup,bool hasUnexportedChanges=false,Guid? libraryBankId=null)=>Write("workspace.json",new WorkspaceBank {Title=title,Bank=bank,Setup=setup,HasUnexportedChanges=hasUnexportedChanges,LibraryBankId=libraryBankId});
    public WorkspaceBank LoadBank(out string warning) {
        warning="";
        try {
            var state=Read<WorkspaceBank>("workspace.json");if(state==null)return null;
            if(state.Version!=1||state.Bank==null||state.Setup==null||state.Title==null)throw new InvalidDataException();
            if(state.Bank.Count>0)Importers.Validate(state.Bank);
            state.Setup.Included??=new();state.Setup.Included.IntersectWith(state.Bank.Select(q=>q.Number));
            state.Setup.Count=Math.Clamp(state.Setup.Count,1,Math.Max(1,state.Bank.Count));state.Setup.Minutes=Math.Clamp(state.Setup.Minutes,1,300);return state;
        }catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException||ex is JsonException||ex is InvalidDataException){warning="Không đọc được bản nháp. File được giữ nguyên: "+ex.Message;return null;}
    }
    public void Begin(ActiveMobileExam exam){Write("active-exam.json",exam);Checkpoint(exam.Id,exam.Index,exam.Answers,exam.Flagged);}
    public void Checkpoint(Guid id,int index,IReadOnlyDictionary<int,string> answers,IEnumerable<int> flagged=null)=>Write("exam-progress.json",new MobileExamProgress {Id=id,Index=index,Answers=new(answers),Flagged=new(flagged??Array.Empty<int>())});
    public ActiveMobileExam LoadActive(out string warning) {
        warning="";
        try {
            var state=Read<ActiveMobileExam>("active-exam.json");if(state==null)return null;
            if(state.Version!=1||state.Id==Guid.Empty||state.Exam==null||state.Exam.Count==0||state.StartedAt==default||state.Deadline==default||state.Minutes<1||state.Minutes>300)throw new InvalidDataException();
            Importers.Validate(state.Exam);
            var progress=Read<MobileExamProgress>("exam-progress.json");
            if(progress?.Id==state.Id){state.Index=progress.Index;state.Answers=progress.Answers??new();state.Flagged=progress.Flagged??new();}
            state.Index=Math.Clamp(state.Index,0,state.Exam.Count-1);state.Answers??=new();
            foreach(var pair in state.Answers.ToArray())if(pair.Key<0||pair.Key>=state.Exam.Count||(state.Exam[pair.Key].IsEssay?(pair.Value==null||pair.Value.Length>32768):!state.Exam[pair.Key].Options.ContainsKey(pair.Value??"")))state.Answers.Remove(pair.Key);
            return state;
        }catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException||ex is JsonException||ex is InvalidDataException){warning="Không đọc được bài đang làm. File được giữ nguyên: "+ex.Message;return null;}
    }
    public void Complete(Guid id) {
        if(Read<ActiveMobileExam>("active-exam.json")?.Id!=id)return;
        File.Delete(Path.Combine(Folder,"active-exam.json"));File.Delete(Path.Combine(Folder,"exam-progress.json"));
    }
}
public static class MobileImportCache {
    public const long Limit=50L*1024*1024;
    public static async Task<string> CopyAsync(Stream input,string fileName,string folder) {
        string extension=Path.GetExtension(fileName).ToLowerInvariant();
        if(!new[]{".quiz",".doc",".docx",".pdf",".xls",".xlsx",".csv",".tsv",".json",".txt",".md",".rtf",".html",".htm",".odt"}.Contains(extension))throw new InvalidDataException("Định dạng file chưa hỗ trợ.");
        Directory.CreateDirectory(folder);string path=Path.Combine(folder,Guid.NewGuid().ToString("N")+extension);
        try {
            await using(var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {
                byte[] buffer=new byte[65536];long total=0;int size;
                while((size=await input.ReadAsync(buffer))>0){total+=size;if(total>Limit)throw new InvalidDataException("File vượt quá 50 MB.");await output.WriteAsync(buffer.AsMemory(0,size));}
            }return path;
        }catch{if(File.Exists(path))File.Delete(path);throw;}
    }
}
