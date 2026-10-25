using QuizDesk;

partial class Tests {
    static void HistoryChecks(string folder) {
        folder=Path.GetFullPath(Path.Combine(folder,Guid.NewGuid().ToString("N")));var store=new HistoryStore(folder);
        Check(store.Load(out var warning).Count==0&&warning=="","Missing history is empty");
        var exam=Enumerable.Range(1,4).Select(i=>new Question {Number=i,Text="Câu hỏi "+i,Note="Giải thích tiếng Việt",Answer=i==4?"":"B",Options=new(){{"A","Một"},{"B","Hai"}}}).ToList();
        var answers=new Dictionary<int,string>{{0,"B"},{1,"A"},{3,"A"}};var now=DateTimeOffset.Now;
        var a=ExamAttempt.Capture("VOR · Lần thi đầu",now.AddMinutes(-3),now,TimeSpan.FromSeconds(123),20,false,exam,answers);
        Check(a.Correct==1&&a.Wrong==1&&a.Unanswered==1&&a.Unscored==1&&a.Graded==3&&Math.Abs(a.Score.Value-10.0/3)<0.001,"Correct/wrong/blank/unscored grading");
        exam[0].Text="Edited";exam[0].Options["B"]="Changed";exam[0].Note="Changed";exam[0].Answer="A";answers.Clear();
        Check(a.Questions[0].Text=="Câu hỏi 1"&&a.Questions[0].Options["B"]=="Hai"&&a.Questions[0].Answer=="B"&&a.Questions[0].Picked=="B"&&a.Questions[0].Note.Contains("tiếng Việt"),"Snapshot unaffected by source edits or cleared answers");
        store.Save(a);var read=new HistoryStore(folder).Load(out warning).Single();
        Check(warning==""&&read.Id==a.Id&&read.Title==a.Title&&read.StartedAt==a.StartedAt&&read.FinishedAt==a.FinishedAt&&read.ElapsedSeconds==123&&read.Minutes==20&&!read.TimedOut,"Fresh store restores metadata and Unicode");
        Check(read.Questions.Select(q=>q.Outcome).SequenceEqual(new[]{"correct","wrong","unanswered","unscored"})&&read.Questions[0].Note==a.Questions[0].Note,"Fresh store restores per-question answers and explanations");
        store.Save(a);Check(store.Load(out _).Count==1,"Retry save does not duplicate attempt");
        Check(!Directory.GetFiles(folder,"*.tmp").Any(),"Atomic history save leaves no temp file");
        string corrupt=Path.Combine(folder,"corrupt.json");File.WriteAllText(corrupt,"{broken");
        Check(store.Load(out warning).Count==1&&warning!=""&&File.ReadAllText(corrupt)=="{broken","Corrupt record skipped without losing good attempts");
        string future=Path.Combine(folder,"future.json");File.WriteAllText(future,"{\"Version\":99}");Check(store.Load(out warning).Count==1&&warning!="","Future history schema skipped safely");
        var unscored=ExamAttempt.Capture("Không chấm điểm",now,now.AddSeconds(1),TimeSpan.FromSeconds(1),1,true,new[]{exam[3]},new Dictionary<int,string>());store.Save(unscored);
        Check(unscored.Score==null&&unscored.Unscored==1&&unscored.Unanswered==0,"All-unscored exam has no score or graded blanks");
        Check(store.Load(out _).First().Id==unscored.Id,"Newest attempt listed first");
        var original=new Question {Number=9,Text="Trộn lựa chọn",Answer="B",Options=new(){{"A","Sai"},{"B","Đúng"},{"C","Khác"}}};
        var setup=new QuizSetup {Count=1,ShuffleOptions=true,Included=new(){9}};var shuffled=QuizSetup.Build(new(){original},setup,false,new Random(42));
        var shuffledRecord=ExamAttempt.Capture("Trộn",now,now,TimeSpan.Zero,1,false,shuffled,new Dictionary<int,string>{{0,shuffled[0].Answer}});
        Check(shuffledRecord.Correct==1&&shuffledRecord.Questions[0].Options[shuffledRecord.Questions[0].Picked]=="Đúng","History retains remapped option labels and correct answer after shuffle");
        var invalid=ExamAttempt.Capture("Invalid",now,now,TimeSpan.Zero,1,false,new[]{original},new Dictionary<int,string>());invalid.Questions[0].Picked="H";
        bool rejected=false;try{store.Save(invalid);}catch(InvalidDataException){rejected=true;}Check(rejected,"Invalid history answer rejected before write");
        string blocked=Path.Combine(folder,"not-a-directory");File.WriteAllText(blocked,"keep");bool failed=false;try{new HistoryStore(blocked).Save(a);}catch(IOException){failed=true;}
        Check(failed&&File.ReadAllText(blocked)=="keep"&&store.Load(out _).Count==2,"Failed save preserves existing history and unrelated file");
        Console.WriteLine("TOTAL "+checks+" history checks passed.");
    }
}
