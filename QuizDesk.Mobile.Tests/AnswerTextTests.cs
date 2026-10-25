using QuizDesk;
using System.IO.Compression;
using System.Xml.Linq;

partial class Tests {
    static void AnswerTextChecks(string folder,string ils) {
        Directory.CreateDirectory(folder);
        var cases=new (string claim,string expected)[]{
            ("b. Cà phê.","B"),("B) Cà phê","B"),("CÀ  PHÊ", "B"),
            ("Ca\u0300 phe\u0302.","B"),("b.","B"),("B","B"),
            ("A. Cà phê.",""),("H. Cà phê.",""),("Cà phê khác", ""),
            ("Ca phe", ""),("A, B", ""),("B. Không có trong lựa chọn", "")
        };
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement P(string s)=>new(w+"p",new XElement(w+"r",new XElement(w+"t",s)));
        int n=0;
        foreach(var c in cases) {
            string[] lines={"Câu 1. Chọn?","A. Trà","B. Cà phê.","Đáp án: "+c.claim,"Giải thích: Lý do riêng."};
            string txt=Path.Combine(folder,$"case-{n}.txt"),docx=Path.Combine(folder,$"case-{n++}.docx");
            File.WriteAllLines(txt,lines);
            using(var z=ZipFile.Open(docx,ZipArchiveMode.Create)){
                using var s=z.CreateEntry("word/document.xml").Open();
                new XDocument(new XElement(w+"document",new XElement(w+"body",lines.Select(P)))).Save(s);
            }
            foreach(var path in new[]{txt,docx}) {
                var q=Importers.Load(path,out _).Single();
                Check(q.Answer==c.expected,Path.GetExtension(path)+" answer: "+c.claim);
                Check(q.Note=="Lý do riêng."&&q.Text=="Chọn?","Answer text kept separate from explanation/stem");
                Check(c.expected!=""?!QuestionReview.RequiresReview(q):QuestionReview.RequiresReview(q)&&q.OriginalAnswer==c.claim,"Unresolved claims retained and highlighted");
            }
        }
        var duplicate=new Question{Options=new(){{"A","Một"},{"B","Một."}}};
        QuestionReview.ApplyAnswerClaims(duplicate,new[]{"B. Một"},"Test");
        Check(duplicate.Answer==""&&duplicate.ImportWarnings.Count>0,"Ambiguous full text stays unscored even with a label");
        foreach(var value in new[]{"DDM > 0","DDM = 0","DDM < 0"}) {
            var q=new Question{Options=new(){{"A","DDM < 0"},{"B","DDM > 0"}}};
            QuestionReview.ApplyAnswerClaims(q,new[]{value},"Test");
            Check(q.Answer==(value=="DDM < 0"?"A":value=="DDM > 0"?"B":""),"Mathematical operators preserved: "+value);
        }
        var json=Path.Combine(folder,"full.json");File.WriteAllText(json,"[{\"text\":\"Chọn?\",\"options\":{\"A\":\"Trà\",\"B\":\"Cà phê\"},\"answer\":\"Cà phê\"}]");
        Check(Importers.Load(json,out _).Single().Answer=="B","JSON uses shared full-text answer resolver");
        var csv=Path.Combine(folder,"full.csv");File.WriteAllText(csv,"text,A,B,answer\nChọn?,Trà,Cà phê,B. Cà phê");
        Check(Importers.Load(csv,out _).Single().Answer=="B","CSV uses shared full-text answer resolver");
        if(ils!=null) {
            var bank=Importers.Load(ils,out var setup);
            string expected="BCABABCAACACABABABA CAACBDDCABBDBABC".Replace(" ","");
            Check(bank.Count==35,"Real ILS DOCX retains all 35 questions");
            Check(string.Concat(bank.Select(q=>q.Answer))==expected,"All 35 ILS answer keys match document");
            Check(bank.All(q=>!QuestionReview.RequiresReview(q)&&q.Note==""),"ILS answer sentences are not fabricated explanations or warnings");
            string quiz=Path.Combine(folder,"ILS.quiz");QuizFile.Save(quiz,bank,setup);
            Check(Importers.Load(quiz,out _).Select(q=>q.Answer).SequenceEqual(bank.Select(q=>q.Answer)),"ILS quiz export/import retains answers");
        }
        Console.WriteLine("TOTAL "+checks+" answer-text checks passed.");
    }
}
