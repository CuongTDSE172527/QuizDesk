using QuizDesk;
using System.IO.Compression;
using System.Xml.Linq;

partial class Tests {
    static void ReviewChecks(string folder) {
        Directory.CreateDirectory(folder);
        List<Question> Load(string name,string content){string path=Path.Combine(folder,name);File.WriteAllText(path,content);return Importers.Load(path,out _);}
        var duplicate=Load("duplicate.txt","Câu 1. Chọn quả?\nA. Táo\nA. Lê\nC. Nho\nĐáp án: A\nGiải thích: Giữ giải thích gốc.");
        Check(duplicate[0].Options.Count==3&&duplicate[0].Options.Values.SequenceEqual(new[]{"Táo","Lê","Nho"}),"Duplicate labels import without losing any choice");
        Check(QuestionReview.Messages(duplicate[0]).Any(s=>s.Contains("Trùng nhãn"))&&duplicate[0].Note.Contains("gốc"),"Duplicate-label receipt and explanation retained");
        Check(QuestionReview.ScoringAnswer(duplicate[0])=="","Duplicate-label answer is not guessed or graded");
        var missing=Load("missing.txt","Câu 1. Chọn số?\nA. Một\nB. Hai");
        Check(missing[0].Answer==""&&QuestionReview.RequiresReview(missing[0]),"Text without any answer imports and requests editing");
        var blank=Load("blank.txt","Câu 1. Chọn số?\nA. Một\nB. Hai\nĐáp án:");
        Check(blank[0].Answer==""&&blank[0].Options["B"]=="Hai","Blank answer placeholder is not appended to last choice");
        var conflict=Load("conflict.txt","Câu 1. Chọn số?\nA. Một\nB. Hai\nĐáp án: A\nĐáp án: B");
        Check(conflict[0].Answer==""&&QuestionReview.Messages(conflict[0]).Any(s=>s.Contains("A; B")),"Conflicting inline answers preserved for review");
        var repeated=Load("repeated.txt","Câu 1. Chọn số?\nA. Một\nB. Hai\nĐáp án: A\nĐáp án: A");
        Check(repeated[0].ImportWarnings.Count>0,"Repeated identical answer lines are highlighted too");
        var multiple=Load("multiple.txt","Câu 1. Chọn số?\nA. Một\nB. Hai\nĐáp án: A, B");
        Check(multiple[0].Answer==""&&multiple[0].ImportWarnings.Any(s=>s.Contains("A, B")),"Multiple answers on one line are not silently reduced to first letter");
        var keys=Load("keys.txt","Câu 1. Chọn số?\nA. Một\nB. Hai\nĐÁP ÁN\n1 A\n1 B");
        Check(keys[0].Answer==""&&keys[0].ImportWarnings.Count>0,"Repeated compact answer-key rows import");
        var same=Load("same.json","[{\"text\":\"Chọn?\",\"options\":{\"A\":\"  Cà  phê\",\"B\":\"CÀ PHÊ\"},\"answer\":\"B\"}]");
        Check(QuestionReview.DuplicateChoices(same[0]).Single()=="A, B"&&QuestionReview.ScoringAnswer(same[0])=="","Duplicate choice content ignores case and whitespace");
        Check(QuestionReview.RequiresReview(new Question{Number=1,Text="q",Answer="A",Options=new(){{"A","café"},{"B","cafe\u0301"}}}),"Unicode equivalent duplicate content detected");
        var json=Load("missing.json","[{\"text\":\"Chọn?\",\"options\":{\"A\":\"Một\",\"B\":\"Hai\"}}]");
        Check(json[0].Answer=="","JSON answer property is optional");
        var validSameKeys=Load("same-keys.txt","Câu 1. Một?\nA. Sai\nB. Đúng\nĐáp án: B\nCâu 2. Hai?\nA. Sai\nB. Đúng\nĐáp án: B");
        Check(validSameKeys.All(q=>!QuestionReview.RequiresReview(q)),"Different questions may legitimately share the same answer letter");
        var jsonDuplicate=Load("duplicate.json","[{\"text\":\"Chọn?\",\"options\":{\"A\":\"Một\",\"a\":\"Hai\"},\"answer\":\"A\"}]");
        Check(jsonDuplicate[0].Options.Count==2&&jsonDuplicate[0].ImportWarnings.Count>0,"JSON duplicate labels preserve both values");
        var csv=Load("missing.csv","text,A,B,note\nChọn?,Một,Hai,Giải thích");
        Check(csv[0].Answer==""&&csv[0].Note=="Giải thích","CSV without answer column retains question and explanation");
        var csvDup=Load("duplicate.csv","text,A,A,answer,answer\nChọn?,Một,Hai,A,B");
        Check(csvDup[0].Options.Count==2&&csvDup[0].Answer==""&&csvDup[0].ImportWarnings.Count>=2,"Repeated CSV columns preserve choices and competing keys");
        var invalid=Load("invalid.json","[{\"text\":\"Chọn?\",\"options\":{\"A\":\"Một\",\"B\":\"Hai\"},\"answer\":\"H\"}]");
        Check(invalid[0].Answer==""&&invalid[0].ImportWarnings.Count>0,"Out-of-range source answer becomes editable warning");
        string Word(string name,bool table) {
            XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            XElement P(string s)=>new(w+"p",new XElement(w+"r",new XElement(w+"t",s)));
            XElement Row(params string[] values)=>new(w+"tr",values.Select(s=>new XElement(w+"tc",P(s))));
            var body=new XElement(w+"body",P("Câu 1. Chọn số?"),P("A. Một"),P(table?"B. Hai":"A. Hai"));
            if(table)body.Add(new XElement(w+"tbl",Row("Câu","Đáp án","Giải thích"),Row("1","A","Lý do A"),Row("1","B","Lý do B")));
            string path=Path.Combine(folder,name);using(var z=ZipFile.Open(path,ZipArchiveMode.Create)){using var s=z.CreateEntry("word/document.xml").Open();new XDocument(new XElement(w+"document",body)).Save(s);}return path;
        }
        var word=Importers.Load(Word("duplicate.docx",false),out _);
        Check(word[0].Options.Count==2&&word[0].ImportWarnings.Count>0&&word[0].Answer=="","DOCX duplicate choices without key are kept");
        var table=Importers.Load(Word("table.docx",true),out _);
        Check(table[0].Answer==""&&table[0].Note.Contains("Lý do A")&&table[0].Note.Contains("Lý do B"),"DOCX repeated answer-table rows keep every explanation");
        var setup=new QuizSetup{Count=1,Included=new(){1},ShuffleOptions=true};
        for(int i=0;i<10;i++)Check(QuizSetup.Build(duplicate,setup,false,new Random(i))[0].Answer=="","Ambiguous question stays unscored after shuffle "+i);
        string quiz=Path.Combine(folder,"review.quiz");QuizFile.Save(quiz,duplicate,setup);var restored=Importers.Load(quiz,out _);
        Check(restored[0].ImportWarnings.SequenceEqual(duplicate[0].ImportWarnings)&&restored[0].Options.Count==3,"Quiz export/import keeps unresolved warning and all content");
        string noKeyQuiz=Path.Combine(folder,"missing.quiz");QuizFile.Save(noKeyQuiz,missing,setup);
        Check(Importers.Load(noKeyQuiz,out _)[0].Answer=="","Quiz export/import keeps missing answer");
        var library=new MobileBankLibrary(Path.Combine(folder,"Library"));var saved=library.Create(new(){Title="Review",Bank=duplicate,Setup=setup},"duplicate.txt",true);
        Check(new MobileBankLibrary(Path.Combine(folder,"Library")).Load(saved.Id).Content.Bank[0].ImportWarnings.Count>0,"Bank warning survives library restart");
        var fixedQuestion=new Question {Number=1,Text=duplicate[0].Text,Answer="B",Note=duplicate[0].Note,Options=new(duplicate[0].Options)};
        var changed=new WorkspaceBank{Title="Review",Bank=new(){fixedQuestion},Setup=setup};library.Save(saved.Id,changed);
        Check(!QuestionReview.RequiresReview(fixedQuestion)&&QuizSetup.Build(changed.Bank,setup,false,new Random())[0].Answer!="","Confirmed edit clears source warning and enables grading");
        Check(library.Load(saved.Id).Activities.Last().Changes.Any(c=>c.Field.Contains("Cảnh báo")),"Resolving import warning is recorded in change history");
        var attempt=ExamAttempt.Capture("Unscored",DateTimeOffset.Now,DateTimeOffset.Now,TimeSpan.Zero,1,false,QuizSetup.Build(same,setup,false,new Random()),new Dictionary<int,string>{{0,"A"}});
        Check(attempt.Unscored==1&&attempt.Wrong==0&&attempt.Score==null,"Duplicate-content attempt is unscored rather than wrong");
        Console.WriteLine("TOTAL "+checks+" import-review checks passed.");
    }
}
