using QuizDesk;
using System.Text;
partial class Tests {
    static void EssayChecks(string folder) {
        Directory.CreateDirectory(folder);
        var essay=new Question {Number=1,IsEssay=true,Text="Giải thích µs?",Answer="Một phần triệu giây.\nA. Ví dụ: 12µs.",Note="Đủ ý, đúng đơn vị."};
        var mc=new Question {Number=2,Text="2+3?",Answer="B",Options=new(){{"A","4"},{"B","5"}}};
        var bank=new List<Question>{essay,mc};var setup=new QuizSetup {Count=2,Minutes=30,Included=new(){1,2},ShuffleOptions=true};
        Importers.Validate(bank);Check(QuestionReview.ScoringAnswer(essay)=="","essay excluded from automatic grading");
        foreach(string format in new[]{"quiz","json","txt","csv","tsv","docx","xlsx"}) {
            string path=Path.Combine(folder,"essay."+format);
            if(format=="quiz")QuizFile.Save(path,bank,setup);else File.WriteAllBytes(path,QuizExports.Write(format,bank,"Mixed essay"));
            var round=Importers.Load(path,out _);
            Check(round.Count==2&&round[0].IsEssay&&round[0].Options.Count==0&&round[0].Answer==essay.Answer,format+" essay type/model roundtrip");
            Check(!round[1].IsEssay&&round[1].Answer=="B",format+" mixed MC remains intact");
        }
        var library=new MobileBankLibrary(Path.Combine(folder,"library"));
        var saved=library.Create(new(){Title="Essay",Bank=bank,Setup=setup});
        Check(library.Load(saved.Id).Content.Bank[0].IsEssay,"library snapshot preserves essay type");
        var exam=QuizSetup.Build(bank,setup,false,new Random(9));
        var q=exam.Single(q=>q.IsEssay);Check(q.Answer==essay.Answer&&q.Options.Count==0,"shuffle preserves essay model");
        int e=exam.IndexOf(q),m=1-e;var responses=new Dictionary<int,string>{{e,"Bài làm nhiều dòng\n12µs"},{m,exam[m].Answer}};
        var workspace=new MobileWorkspace(Path.Combine(folder,"workspace"));
        workspace.Begin(new(){Exam=exam,Answers=responses,Index=e,StartedAt=DateTimeOffset.Now,Deadline=DateTimeOffset.Now.AddMinutes(30),Minutes=30});
        Check(workspace.LoadActive(out _).Answers[e]==responses[e],"essay checkpoint retains full response");
        var attempt=ExamAttempt.Capture("Essay",DateTimeOffset.Now,DateTimeOffset.Now,TimeSpan.Zero,30,false,exam,responses);
        Check(attempt.Correct==1&&attempt.Unscored==1&&attempt.Score==10,"essay is not compared as MC label");
        var history=new HistoryStore(Path.Combine(folder,"history"));history.Save(attempt);
        var restored=history.Load(out _).Single();var a=restored.Questions.Single(q=>q.IsEssay);
        Check(a.Picked==responses[e]&&a.Answer==essay.Answer,"history retains response and model");
        a.ManualCorrect=false;history.Save(restored);Check(history.Load(out _).Single().Score==5,"manual incorrect affects score explicitly");
        a.ManualCorrect=true;history.Save(restored);Check(history.Load(out _).Single().Score==10,"manual correct saved");
        a.ManualCorrect=null;Check(restored.Unscored==1,"reset manual grading");
        var invalid=new Question {Number=1,Text="Malformed MC"};bool rejected=false;try{Importers.Validate(new(){invalid});}catch{rejected=true;}Check(rejected,"missing choices not silently converted to essay");
        essay.Answer=new string('x',32769);rejected=false;try{Importers.Validate(new(){essay});}catch{rejected=true;}Check(rejected,"essay model length limit enforced");
        Console.WriteLine($"TOTAL {checks} essay checks passed");
    }
}
