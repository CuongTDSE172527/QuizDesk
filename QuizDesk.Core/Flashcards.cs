using System.Text.Json;

namespace QuizDesk;

public sealed class Flashcard {
    public int Number {get;set;}
    public string Front {get;set;}="";
    public string Back {get;set;}="";
    public string Note {get;set;}="";
    public string Choices {get;set;}="";
    public string OriginalAnswer {get;set;}="";
    public string Source {get;set;}="";
    public string Answer {get;set;}="";
    public List<QuestionMedia> Media {get;set;}=new();
}
public sealed class FlashcardSession {
    public int Version {get;set;}=1;
    public Guid Id {get;set;}=Guid.NewGuid();
    public Guid? BankId {get;set;}
    public string Title {get;set;}="";
    public List<Flashcard> Cards {get;set;}=new();
    public List<int> Order {get;set;}=new();
    public Dictionary<int,bool> Known {get;set;}=new();
    public int Position {get;set;}
    public int Excluded {get;set;}
    public bool Revealed {get;set;}
    public bool Summary {get;set;}
    public bool InProgress {get;set;}=true;
    public DateTimeOffset UpdatedAt {get;set;}=DateTimeOffset.UtcNow;
    [System.Text.Json.Serialization.JsonIgnore] public Flashcard Current=>Cards[Order[Position]];
    [System.Text.Json.Serialization.JsonIgnore] public int Remembered=>Known.Count(p=>p.Value);
    public static FlashcardSession Create(string title,Guid? bankId,IEnumerable<Question> bank,QuizSetup setup,Random random=null) {
        var selected=bank.Where(q=>setup.Included.Contains(q.Number)).ToList();
        var cards=selected.Where(q=>QuestionReview.ScoringAnswer(q)!=""&&q.Options.ContainsKey(q.Answer))
            .Select(q=>new Flashcard {Number=q.Number,Front=q.Text,Back=q.Answer+". "+q.Options[q.Answer],Note=q.Note,Choices=string.Join("\n",q.Options.Select(o=>o.Key+". "+o.Value)),OriginalAnswer=q.OriginalAnswer,Source=q.AnswerSource,Answer=q.Answer,Media=q.Media.Select(m=>m.Clone()).ToList()}).ToList();
        if(cards.Count==0)throw new InvalidOperationException("Chưa có câu đã chọn nào có đáp án xác nhận để tạo flashcards. Mở Bộ đề → Sửa để bổ sung.");
        var state=new FlashcardSession {Title=title,BankId=bankId,Cards=cards,Excluded=selected.Count-cards.Count,Order=Enumerable.Range(0,cards.Count).ToList()};
        if(setup.ShuffleQuestions){random??=new Random();random.Shuffle(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(state.Order));}
        return state;
    }
    public void Flip(){if(!Summary)Revealed=!Revealed;}
    public void Move(int position){Position=Math.Clamp(position,0,Order.Count-1);Revealed=false;Summary=false;}
    public void Rate(bool known){
        if(!Revealed||Summary)return;
        Known[Order[Position]]=known;
        if(Position<Order.Count-1)Move(Position+1);else{Summary=true;Revealed=false;}
    }
    public void Repeat(bool all){
        if(all)Known.Clear();
        var next=Enumerable.Range(0,Cards.Count).Where(i=>all||!Known.GetValueOrDefault(i)).ToList();
        if(next.Count==0){Summary=true;return;}
        Order=next;
        Position=0;Summary=false;Revealed=false;InProgress=true;
    }
    public void Validate(){
        if(Cards!=null)MediaFiles.Check(Cards.SelectMany(c=>c?.Media??throw new InvalidDataException("Thiếu media flashcards.")));
        if(Version!=1||Cards==null||Cards.Count<1||Cards.Count>5000||Order==null||Order.Count<1||Order.Count>Cards.Count||Order.Distinct().Count()!=Order.Count||Order.Any(i=>i<0||i>=Cards.Count)||Position<0||Position>=Order.Count||Known==null||Known.Keys.Any(i=>i<0||i>=Cards.Count)||Cards.Any(c=>c==null||c.Number<1||string.IsNullOrWhiteSpace(c.Front)||string.IsNullOrWhiteSpace(c.Back)))throw new InvalidDataException("Tiến độ flashcards không hợp lệ.");
    }
}
public sealed class FlashcardStore {
    readonly string path;
    Guid savedId;
    sealed class Progress {
        public Progress(){}
        public Guid Id {get;set;}
        public List<int> Order {get;set;}=new();
        public Dictionary<int,bool> Known {get;set;}=new();
        public int Position {get;set;}
        public bool Revealed {get;set;}
        public bool Summary {get;set;}
        public bool InProgress {get;set;}
        public DateTimeOffset UpdatedAt {get;set;}
    }
    public FlashcardStore(string folder)=>path=Path.Combine(folder,"flashcards.json");
    static void Write<T>(string destination,T value){
        string temp=destination+".tmp";
        try{using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){JsonSerializer.Serialize(stream,value);stream.Flush(true);}File.Move(temp,destination,true);}
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public void Save(FlashcardSession session){
        session.Validate();Directory.CreateDirectory(Path.GetDirectoryName(path));session.UpdatedAt=DateTimeOffset.UtcNow;
        // The immutable question snapshot is written once per session. Flips
        // and ratings checkpoint only small progress, even for 5,000 cards.
        if(savedId!=session.Id||!File.Exists(path)){Write(path,session);savedId=session.Id;}
        Write(path+".progress",new Progress {Id=session.Id,Order=session.Order,Known=session.Known,Position=session.Position,Revealed=session.Revealed,Summary=session.Summary,InProgress=session.InProgress,UpdatedAt=session.UpdatedAt});
    }
    public FlashcardSession Load(out string warning){
        warning="";if(!File.Exists(path))return null;
        try{
            if(new FileInfo(path).Length>128L*1024*1024)throw new InvalidDataException("File tiến độ vượt giới hạn.");
            using var stream=File.OpenRead(path);var session=JsonSerializer.Deserialize<FlashcardSession>(stream)??throw new InvalidDataException();
            if(File.Exists(path+".progress")){
                if(new FileInfo(path+".progress").Length>2*1024*1024)throw new InvalidDataException();
                using var input=File.OpenRead(path+".progress");var progress=JsonSerializer.Deserialize<Progress>(input)??throw new InvalidDataException();
                if(progress.Id==session.Id){session.Order=progress.Order;session.Known=progress.Known;session.Position=progress.Position;session.Revealed=progress.Revealed;session.Summary=progress.Summary;session.InProgress=progress.InProgress;session.UpdatedAt=progress.UpdatedAt;}
            }
            session.Validate();savedId=session.Id;return session;
        }
        catch(Exception ex) when(ex is IOException||ex is InvalidDataException||ex is UnauthorizedAccessException||ex is JsonException){warning="Không đọc được tiến độ flashcards; file gốc được giữ nguyên: "+ex.Message;return null;}
    }
}
