using QuizDesk;
using System.IO.Compression;
using System.Xml.Linq;
partial class Tests {
    static void RecognitionChecks(string folder,string mon15){
        Directory.CreateDirectory(folder);XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";int serial=0;
        XElement Run(string text,params XElement[] props)=>new(w+"r",new XElement(w+"rPr",props),new XElement(w+"t",text));
        XElement P(string text,params XElement[] props)=>new(w+"p",Run(text,props));
        XElement Highlight(string color)=>new(w+"highlight",new XAttribute(w+"val",color));
        XElement Shading(string color)=>new(w+"shd",new XAttribute(w+"fill",color));
        Question Word(IEnumerable<XElement> paragraphs,XElement styles=null){
            string path=Path.Combine(folder,"doc-"+serial+++".docx");using(var zip=ZipFile.Open(path,ZipArchiveMode.Create)){
                using(var output=zip.CreateEntry("word/document.xml").Open())new XDocument(new XElement(w+"document",new XElement(w+"body",paragraphs))).Save(output);
                if(styles!=null)using(var output=zip.CreateEntry("word/styles.xml").Open())new XDocument(styles).Save(output);
            }return Importers.Load(path,out _).Single();
        }
        XElement[] Base(XElement a,XElement b,params XElement[] tail)=>new[]{P("Câu 1. Chọn?"),a,b}.Concat(tail).ToArray();
        foreach(string color in new[]{"yellow","cyan","magenta","red","blue","green","darkBlue","darkCyan","darkGreen","darkMagenta","darkRed","darkYellow","darkGray","lightGray","black"}){
            var q=Word(Base(P("A. Trà"),P("B. Cà phê",Highlight(color))));Check(q.Answer=="B"&&q.NeedsReview&&q.AnswerSource.Contains("highlight"),"Unique highlight: "+color);
        }
        Check(Word(Base(P("A. Trà"),P("B. Cà phê",Highlight("none")))).Answer=="","No-color marker is not an answer");
        Check(Word(Base(P("A. Trà",Highlight("cyan")),P("B. Cà phê",Highlight("yellow")))).Answer=="","Different highlight colors on multiple choices stay ambiguous");
        Check(Word(Base(P("A. Trà",Highlight("yellow")),P("B. Cà phê",Highlight("yellow")))).Answer=="","Uniform background never guesses one choice");
        Check(Word(Base(P("A. Trà",Highlight("red")),P("B. Cà phê",Highlight("cyan")),P("Đáp án đúng là: Cà phê"))).Answer=="B","Explicit answer supersedes decorative colors");
        var review=Word(Base(P("A. Trà",Highlight("cyan")),P("B. Cà phê",Highlight("yellow")),P("Đáp án gốc: A. Trà"),P("Sau rà soát: B — sửa theo tài liệu.")));
        Check(review.Answer=="B"&&review.OriginalAnswer=="A. Trà","Reviewed key supersedes original key and all colors");
        var undecided=Word(Base(P("A. Trà",Highlight("yellow")),P("B. Cà phê"),P("Đáp án gốc: A"),P("Sau rà soát: CHƯA CHỐT — kiểm tra lại.")));
        Check(undecided.Answer=="","Unresolved verdict never falls back to color");
        Check(Word(Base(P("A. Trà"),P("B. Cà phê",Shading("FFD966")))).Answer=="B","Run background shading recognized");
        var paragraph=P("B. Cà phê");paragraph.AddFirst(new XElement(w+"pPr",Shading("00FFFF")));
        Check(Word(Base(P("A. Trà"),paragraph)).Answer=="B","Paragraph background shading recognized");
        Check(Word(Base(P("A. Trà"),P("B. Cà phê",Shading("FFFFFF")))).Answer=="","Plain white shading is not a cue");
        var styles=new XElement(w+"styles",new XElement(w+"style",new XAttribute(w+"styleId","Base"),new XElement(w+"rPr",Highlight("cyan"))),new XElement(w+"style",new XAttribute(w+"styleId","Child"),new XElement(w+"basedOn",new XAttribute(w+"val","Base"))));
        XElement Styled(bool off=false)=>P("B. Cà phê",new XElement(w+"rStyle",new XAttribute(w+"val","Child")),off?Highlight("none"):new XElement(w+"i"));
        Check(Word(Base(P("A. Trà"),Styled()),styles).Answer=="B","Inherited character-style highlighting recognized");
        Check(Word(Base(P("A. Trà"),Styled(true)),styles).Answer=="","Direct none overrides inherited highlight");
        Check(Word(Base(P("A. Trà"),new XElement(w+"p",Run("B.",Highlight("yellow")),Run(" Cà phê")))).Answer=="B","Highlight on the option label alone recognized");
        Check(Word(Base(P("A. Trà"),new XElement(w+"p",Run("B. Cà phê rất "),Run("ngon",Highlight("yellow"))))).Answer=="","Emphasized keyword alone does not invent an answer");
        string[] claims={"B — Cà phê.","b – Cà phê","B: Cà phê","B： Cà phê","“Cà phê.”","B. “Cà phê.”"};
        foreach(string claim in claims){var q=new Question {Options=new(){{"A","Trà"},{"B","Cà phê."}}};QuestionReview.ApplyAnswerClaims(q,new[]{claim},"Test");Check(q.Answer=="B","Full sentence punctuation: "+claim);}
        var conflict=new Question {Options=new(){{"A","Trà"},{"B","Cà phê"}}};QuestionReview.ApplyAnswerClaims(conflict,new[]{"A — Cà phê"},"Test");Check(conflict.Answer==""&&conflict.ImportWarnings.Count>0,"Label/content conflict not repaired by fuzzy matching");
        foreach(string heading in new[]{"Đáp án là Cà phê","Đáp án đúng là: Cà phê","Câu trả lời đúng: Cà phê","Correct answer: Cà phê","Đáp án：Cà phê"}){
            var q=Word(Base(P("A. Trà"),P("B. Cà phê"),P(heading),P("Giải thích: Lý do riêng.")));Check(q.Answer=="B"&&q.Note=="Lý do riêng."&&q.Options["B"]=="Cà phê","DOCX answer heading: "+heading);
            var txt=Reader.Parse("Câu 1. Chọn?\nA. Trà\nB. Cà phê\n"+heading+"\nGiải thích: Lý do riêng.").Single();Check(txt.Answer=="B"&&txt.Note=="Lý do riêng.","Text answer heading: "+heading);
        }
        Check(Word(Base(P("A. Trà"),P("B. Cà phê"),P("Đáp án:"),P("B. Cà phê"),P("Giải thích: Lý do."))).Answer=="B","DOCX answer on next paragraph not mistaken for duplicate option");
        var next=Reader.Parse("Câu 1. Chọn?\nA. Trà\nB. Cà phê\nĐáp án:\nB. Cà phê\nGiải thích: Lý do.").Single();Check(next.Answer=="B"&&next.Options.Count==2&&next.Note=="Lý do.","Text answer on next line is separate from options");
        var compact=Reader.Parse("Câu 1. Chọn?\nA. Trà\nB. Cà phê\nĐÁP ÁN:\n1 B").Single();Check(compact.Answer=="B","Compact key table with colon still recognized");
        var manual=new XElement(w+"p",Run("Đáp án: B. Cà"),new XElement(w+"r",new XElement(w+"br"),new XElement(w+"t","phê")));
        Check(Word(Base(P("A. Trà"),P("B. Cà phê"),manual)).Answer=="B","Soft line break inside full answer sentence preserved");
        var footer=Word(Base(P("A. Trà"),P("B. Cà phê"),P("Đáp án: B"),P("Phụ lục không thuộc câu hỏi.")));
        Check(footer.Answer=="B"&&footer.Text=="Chọn?","Resolved DOCX key stops before unrelated footer paragraphs");
        var textFooter=Reader.Parse("Câu 1. Chọn?\nA. Trà\nB. Cà phê\nĐáp án: B\nPhụ lục không thuộc câu hỏi.").Single();
        Check(textFooter.Answer=="B"&&textFooter.Text=="Chọn?","Resolved text key stops before unrelated footer lines");
        if(mon15!=null){var bank=Importers.Load(mon15,out var setup);var q=bank.Single(q=>q.Number==27);Check(q.Answer=="A"&&q.OriginalAnswer.Contains("quy định")&&q.Options["A"].Contains("quy trình"),"Real screenshot question 27 uses reviewed A despite wording difference in original");QuizFile.Save(Path.Combine(folder,"Mon-15-da-nhan-dien.quiz"),bank,setup);Check(Importers.Load(Path.Combine(folder,"Mon-15-da-nhan-dien.quiz"),out _).Single(q=>q.Number==27).Answer=="A","Question 27 reviewed answer retained in shareable .quiz");}
        Console.WriteLine("TOTAL "+checks+" recognition checks passed.");
    }
}
