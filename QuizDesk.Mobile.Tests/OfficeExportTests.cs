using QuizDesk;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
partial class Tests {
    static void OfficeExportChecks(string folder){
        Directory.CreateDirectory(folder);byte[] png=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aX1cAAAAASUVORK5CYII=");
        var first=new Question {Number=7,Text="Độ cao < 10 & > 2?\nDòng hai",Answer="B",Note="Giải thích tiếng Việt\nDòng tiếp",Options=new(){{"A","Một"},{"B","Hai"},{"C","Ba"},{"D","Bốn"}}};
        first.Media.Add(MediaFiles.FromBytes("question.png",png));first.Media.Add(MediaFiles.FromBytes("choice.png",png,"B"));first.Media.Add(MediaFiles.FromBytes("note.png",png,"note"));
        var second=new Question {Number=19,Text="=SUM(A1:A2)",Options=new(){{"A","@nội dung"},{"B","+1"}},NeedsReview=true};
        var bank=new List<Question>{first,second};
        XDocument Read(string file,string entry){using var zip=ZipFile.OpenRead(file);using var input=zip.GetEntry(entry).Open();return XDocument.Load(input);}
        string docx=Path.Combine(folder,"QuizDesk-Office.docx"),xlsx=Path.Combine(folder,"QuizDesk-Office.xlsx");
        File.WriteAllBytes(docx,QuizExports.Write("docx",bank,"Đề kiểm tra <Office>"));File.WriteAllBytes(xlsx,QuizExports.Write("xlsx",bank,"Đề kiểm tra <Office>"));
        foreach(var file in new[]{docx,xlsx}){
            Check(File.ReadAllBytes(file).Take(2).SequenceEqual(new byte[]{80,75}),Path.GetExtension(file)+" is a real ZIP/OOXML package");
            using var zip=ZipFile.OpenRead(file);Check(zip.GetEntry("[Content_Types].xml")!=null&&zip.GetEntry("_rels/.rels")!=null&&zip.GetEntry("docProps/core.xml")!=null,Path.GetExtension(file)+" has package relationships, content types and metadata");
            XNamespace rel="http://schemas.openxmlformats.org/package/2006/relationships";
            foreach(var entry in zip.Entries.Where(e=>e.FullName.EndsWith(".rels"))){using var input=entry.Open();var relationships=XDocument.Load(input);Check(relationships.Root.Elements(rel+"Relationship").All(e=>((string)e.Attribute("Type")).StartsWith("http")&&e.Attribute("TargetMode")==null),entry.FullName+" uses standard internal relationship URIs");}
        }
        var word=Importers.Load(docx,out _);Check(word.Count==2&&word.Select(q=>q.Number).SequenceEqual(new[]{7,19}),"DOCX independently reimports all original question numbers");
        Check(word[0].Text==first.Text&&word[0].Options["B"]=="Hai"&&word[0].Answer=="B"&&word[0].Note.Contains("Giải thích tiếng Việt"),"DOCX roundtrip preserves Vietnamese, markup characters, choices, answer and explanation");
        Check(word[0].Media.Count==3&&word[0].Media.Any(m=>m.Target=="question")&&word[0].Media.Any(m=>m.Target=="B")&&word[0].Media.Any(m=>m.Target=="note"),"DOCX embedded images retain stem, choice and note targets");
        Check(word[0].Media.All(m=>m.Data.SequenceEqual(png)),"DOCX image bytes preserved without conversion");
        Check(word[1].Answer=="","DOCX never invents an answer for an unresolved question");
        var sheet=Importers.Load(xlsx,out _);Check(sheet.Count==2&&sheet[0].Text==first.Text&&sheet[0].Answer=="B"&&sheet[0].Note==first.Note,"XLSX roundtrip preserves multiline Vietnamese text and answer associations");
        Check(sheet[1].Text=="=SUM(A1:A2)"&&sheet[1].Options["A"]=="@nội dung","XLSX stores formula-looking input as literal text without modifying it");
        XNamespace ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main";var xml=Read(xlsx,"xl/worksheets/sheet1.xml");
        Check(!xml.Descendants(ns+"f").Any()&&xml.Descendants(ns+"c").All(c=>(string)c.Attribute("t")=="inlineStr"),"XLSX contains no executable formulas");
        Check(xml.Descendants(ns+"pane").Single().Attribute("state").Value=="frozen"&&xml.Descendants(ns+"autoFilter").Any(),"XLSX freezes header and provides an autofilter");
        string noAnswers=Path.Combine(folder,"questions-only.docx");File.WriteAllBytes(noAnswers,QuizExports.Write("docx",bank,"Questions only",false));
        var noWord=Importers.Load(noAnswers,out _);Check(noWord.All(q=>q.Answer==""&&q.Note==""),"Question-only DOCX omits answers and explanations");
        Check(noWord[0].Media.Count==2&&!noWord[0].Media.Any(m=>m.Target=="note"),"Question-only DOCX omits explanation media while retaining question/choices");
        string noSheet=Path.Combine(folder,"questions-only.xlsx");File.WriteAllBytes(noSheet,QuizExports.Write("xlsx",bank,"Questions only",false));Check(Importers.Load(noSheet,out _).All(q=>q.Answer==""&&q.Note==""),"Question-only XLSX omits answers and explanations");
        var longQuestion=new Question {Number=1,Text=new string('x',32767),Options=new(){{"A","One"},{"B","Two"}}};
        Check(QuizExports.Write("xlsx",new[]{longQuestion},"Limit").Length>0,"Excel maximum 32767-character cell is accepted");
        longQuestion.Text+="x";bool rejected=false;try{QuizExports.Write("xlsx",new[]{longQuestion},"Too long");}catch(InvalidDataException){rejected=true;}Check(rejected,"Oversize Excel cell is rejected without silent truncation");
        Check(QuizExports.Write("docx",new[]{longQuestion},"Long document").Length>0,"DOCX supports text exceeding the Excel cell limit");
        first.Media.Add(MediaFiles.FromBytes("clip.mp4",Encoding.ASCII.GetBytes("\0\0\0\u0018ftypisom00000000"),"question"));
        string videoDoc=Path.Combine(folder,"video-notice.docx");File.WriteAllBytes(videoDoc,QuizExports.Write("docx",bank,"Video notice"));
        using(var zip=ZipFile.OpenRead(videoDoc)){Check(!zip.Entries.Any(e=>e.FullName.EndsWith(".mp4")),"DOCX does not embed unsupported video payloads");using var input=zip.GetEntry("word/document.xml").Open();Check(XDocument.Load(input).ToString().Contains("clip.mp4"),"DOCX identifies unsupported attachment instead of silently dropping it");}
        Console.WriteLine("TOTAL "+checks+" Office export checks passed.");
    }
}
