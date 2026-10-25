using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuizDesk;

public sealed class AttemptQuestion {
    public int Number {get;set;}
    public string Text {get;set;}="";
    public Dictionary<string,string> Options {get;set;}=new();
    public string Picked {get;set;}="";
    public string Answer {get;set;}="";
    public string Note {get;set;}="";
    public List<QuestionMedia> Media {get;set;}=new();
    [JsonIgnore] public string Outcome=>Answer==""?"unscored":Picked==""?"unanswered":Picked==Answer?"correct":"wrong";
}

public sealed class ExamAttempt {
    public int Version {get;set;}=1;
    public Guid Id {get;set;}=Guid.NewGuid();
    public string Title {get;set;}="";
    public DateTimeOffset StartedAt {get;set;}
    public DateTimeOffset FinishedAt {get;set;}
    public double ElapsedSeconds {get;set;}
    public int Minutes {get;set;}
    public bool TimedOut {get;set;}
    public List<AttemptQuestion> Questions {get;set;}=new();
    [JsonIgnore] public int Correct=>Questions.Count(q=>q.Outcome=="correct");
    [JsonIgnore] public int Wrong=>Questions.Count(q=>q.Outcome=="wrong");
    [JsonIgnore] public int Unanswered=>Questions.Count(q=>q.Outcome=="unanswered");
    [JsonIgnore] public int Unscored=>Questions.Count(q=>q.Outcome=="unscored");
    [JsonIgnore] public int Graded=>Questions.Count-Unscored;
    [JsonIgnore] public double? Score=>Graded==0?null:10.0*Correct/Graded;

    public static ExamAttempt Capture(string title,DateTimeOffset started,DateTimeOffset finished,TimeSpan elapsed,int minutes,bool timedOut,IReadOnlyList<Question> exam,IReadOnlyDictionary<int,string> answers) {
        var attempt=new ExamAttempt {Title=title,StartedAt=started,FinishedAt=finished,ElapsedSeconds=Math.Max(0,elapsed.TotalSeconds),Minutes=minutes,TimedOut=timedOut};
        for(int i=0;i<exam.Count;i++) {
            var q=exam[i];attempt.Questions.Add(new AttemptQuestion {Number=q.Number,Text=q.Text,Options=new(q.Options),Answer=q.Answer,Note=q.Note,Media=q.Media.Select(m=>m.Clone()).ToList(),Picked=answers.TryGetValue(i,out string picked)?picked:""});
        }
        attempt.Validate();return attempt;
    }
    public void Validate() {
        if(Questions!=null)MediaFiles.Check(Questions.SelectMany(q=>q?.Media??throw new InvalidDataException("Thiếu media lịch sử.")));
        if(Version!=1||Id==Guid.Empty||Title==null||StartedAt==default||FinishedAt==default||!double.IsFinite(ElapsedSeconds)||ElapsedSeconds<0||Minutes<1||Minutes>300||Questions==null||Questions.Count<1||Questions.Count>5000)
            throw new InvalidDataException("Lịch sử có cấu trúc chưa được hỗ trợ.");
        if(Questions.Any(q=>q==null||q.Number<1||string.IsNullOrWhiteSpace(q.Text)||q.Note==null||q.Options==null||q.Options.Count<2||q.Options.Count>8||q.Options.Any(p=>p.Key.Length!=1||p.Key[0]<'A'||p.Key[0]>'H'||string.IsNullOrWhiteSpace(p.Value))||q.Answer==null||q.Picked==null||(q.Answer!=""&&!q.Options.ContainsKey(q.Answer))||(q.Picked!=""&&!q.Options.ContainsKey(q.Picked))))
            throw new InvalidDataException("Nội dung câu hỏi trong lịch sử không hợp lệ.");
    }
}

public sealed class HistoryStore {
    public string DirectoryPath {get;}
    public HistoryStore(string folder=null)=>DirectoryPath=folder??Path.Combine(Path.GetDirectoryName(new PreferencesStore().FilePath),"History");
    public void Save(ExamAttempt attempt) {
        attempt.Validate();Directory.CreateDirectory(DirectoryPath);
        string path=Path.Combine(DirectoryPath,attempt.Id.ToString("N")+".json"),temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try {
            using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {
                JsonSerializer.Serialize(stream,attempt,new JsonSerializerOptions {WriteIndented=true});stream.Flush(true);
            }
            File.Move(temp,path,true);
        }finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public List<ExamAttempt> Load(out string warning) {
        warning="";var attempts=new List<ExamAttempt>();int skipped=0;
        try {
            if(!Directory.Exists(DirectoryPath))return attempts;
            foreach(string path in Directory.EnumerateFiles(DirectoryPath,"*.json")) {
                try {
                    using var stream=InputSafety.OpenFile(path,128L*1024*1024);
                    var item=JsonSerializer.Deserialize<ExamAttempt>(stream)??throw new InvalidDataException();item.Validate();
                    // A filename identifies an attempt, so a duplicate/copy cannot double-count it.
                    if(Path.GetFileNameWithoutExtension(path)!=item.Id.ToString("N"))throw new InvalidDataException();
                    attempts.Add(item);
                }catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException||ex is JsonException||ex is InvalidDataException){skipped++;}
            }
            if(skipped>0)warning=$"Không đọc được {skipped} bản ghi lịch sử. Các file này được giữ nguyên; những lần thi khác vẫn xem được.";
        }catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException){warning="Không đọc được thư mục lịch sử: "+ex.Message;}
        return attempts.OrderByDescending(a=>a.FinishedAt).ToList();
    }
}
