using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using QuizDesk;
partial class Tests {
    static void MediaChecks(string folder,string original){
        Directory.CreateDirectory(folder);
        byte[] png=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aX1cAAAAASUVORK5CYII=");
        byte[] gif=Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAAAAAAALAAAAAABAAEAAAIBRAA7");
        byte[] mp4=Encoding.ASCII.GetBytes("\0\0\0\u0018ftypisom00000000"); // header fixture, not a playback claim
        Question Q(int n)=>new(){Number=n,Text="Câu media "+n,Answer="B",Note="Giải thích",Options=new(){{"A","Một"},{"B","Hai"}},Media=new(){MediaFiles.FromBytes("map.png",png,"question"),MediaFiles.FromBytes("animation.gif",gif,"B"),MediaFiles.FromBytes("clip.mp4",mp4,"note")}};
        var bank=new List<Question>{Q(19)};var setup=new QuizSetup {Count=1,ShuffleOptions=true,Included=new(){19}};
        Importers.Validate(bank);Check(bank[0].Media.Count==3,"media formats accepted from real signatures");
        for(int i=0;i<15;i++){var exam=QuizSetup.Build(bank,setup,false,new Random(i));Check(exam[0].Media.Single(m=>m.Mime=="image/gif").Target==exam[0].Answer,"choice shuffle remaps attachment "+i);}
        var snapshot=QuizSetup.Build(bank,setup,false,new Random(1));snapshot[0].Media[0].Data[0]=0;Check(bank[0].Media[0].Data[0]==137,"exam attachment is independent snapshot");
        string package=Path.Combine(folder,"media.quiz");QuizFile.Save(package,bank,setup);var loaded=Importers.Load(package,out var config);
        Check(File.ReadAllBytes(package)[0]==80&&loaded[0].Media.Count==3&&loaded[0].Media[1].Data.SequenceEqual(gif)&&loaded[0].Media[1].Target=="B"&&config.ShuffleOptions,"portable media ZIP roundtrip");
        using(var zip=ZipFile.OpenRead(package))Check(zip.Entries.Count==5,"media package contains text manifest and three assets");
        var plain=Q(1);plain.Media.Clear();QuizFile.Save(Path.Combine(folder,"old.quiz"),new(){plain},new(){Count=1,Included=new(){1}});Check(Importers.Load(Path.Combine(folder,"old.quiz"),out _)[0].Media.Count==0,"legacy XML remains readable");
        var library=new MobileBankLibrary(Path.Combine(folder,"library"));var a=library.Create(new(){Title="A",Bank=bank,Setup=setup});var b=library.Create(new(){Title="B",Bank=new(){Q(19)},Setup=setup});var merged=library.Merge(new[]{a.Id,b.Id},"Merged",true,random:new Random(2));
        Check(merged.Content.Bank.Count==2&&merged.Content.Bank.All(q=>q.Media.Count==3),"merge retains independent media and targets");
        var edited=library.Load(a.Id).Content;edited.Bank[0].Media.RemoveAt(1);var saved=library.Save(a.Id,edited);Check(saved.Activities.Last().Changes.Any(c=>c.Field.Contains("Media"))&&library.Load(b.Id).Content.Bank[0].Media.Count==3,"media change audited without altering another source");
        var workspace=new MobileWorkspace(Path.Combine(folder,"workspace"));workspace.SaveBank("Media",bank,setup);Check(workspace.LoadBank(out _).Bank[0].Media.Count==3,"workspace persists media");
        var exam2=QuizSetup.Build(bank,setup,false,new Random(1));workspace.Begin(new(){Id=Guid.NewGuid(),Title="Media",StartedAt=DateTimeOffset.Now,Deadline=DateTimeOffset.Now.AddMinutes(20),Minutes=20,Exam=exam2});Check(workspace.LoadActive(out _).Exam[0].Media.Count==3,"active exam persists media");
        var attempt=ExamAttempt.Capture("Media",DateTimeOffset.Now,DateTimeOffset.Now,TimeSpan.Zero,20,false,exam2,new Dictionary<int,string>());var history=new HistoryStore(Path.Combine(folder,"history"));history.Save(attempt);Check(history.Load(out _).Single().Questions[0].Media.Count==3,"history independent media snapshot persists");
        var cards=FlashcardSession.Create("Media",a.Id,bank,setup);var store=new FlashcardStore(Path.Combine(folder,"study"));store.Save(cards);Check(store.Load(out _).Cards.Single().Media.Count==3&&cards.Current.Answer=="B","flashcards retain question answer and note media");
        void Reject(Action action,string label){bool rejected=false;try{action();}catch(InvalidDataException){rejected=true;}Check(rejected,label);}
        Reject(()=>MediaFiles.FromBytes("bad.exe",Encoding.ASCII.GetBytes("MZprogram")),"executable disguised as media rejected");
        var invalid=Q(1);invalid.Media[0].Mime="video/mp4";Reject(()=>Importers.Validate(new(){invalid}),"MIME mismatch rejected");
        invalid=Q(1);invalid.Media[0].Target="H";Reject(()=>Importers.Validate(new(){invalid}),"missing option target rejected");
        invalid=Q(1);invalid.Media[0].Name="../x.png";Reject(()=>Importers.Validate(new(){invalid}),"media traversal name rejected");
        invalid=Q(1);invalid.Media[0].Data=new byte[MediaFiles.ItemLimit+1];Reject(()=>Importers.Validate(new(){invalid}),"oversize media rejected");
        invalid=Q(1);invalid.Media=Enumerable.Range(0,17).Select(i=>MediaFiles.FromBytes("x.png",png)).ToList();Reject(()=>Importers.Validate(new(){invalid}),"per-question item bound enforced");
        var huge=Q(1);huge.Media[0].Data=new byte[MediaFiles.ImageLimit];png.CopyTo(huge.Media[0].Data,0);huge.Media.RemoveRange(1,2);Reject(()=>MediaFiles.CheckBank(new[]{huge,huge,huge,huge}),"aggregate bank media budget enforced");
        string corrupt=Path.Combine(folder,"corrupt.quiz");File.Copy(package,corrupt,true);using(var zip=ZipFile.Open(corrupt,ZipArchiveMode.Update)){var media=zip.Entries.First(e=>e.FullName.EndsWith(".png"));string name=media.FullName;media.Delete();using var stream=zip.CreateEntry(name).Open();stream.Write(gif);}Reject(()=>Importers.Load(corrupt,out _),"corrupt media package hash/signature rejected");
        string traversal=Path.Combine(folder,"traversal.quiz");File.Copy(package,traversal,true);using(var zip=ZipFile.Open(traversal,ZipArchiveMode.Update)){var entry=zip.GetEntry("media.json");string text;using(var input=entry.Open())using(var reader=new StreamReader(input))text=reader.ReadToEnd();entry.Delete();using var writer=new StreamWriter(zip.CreateEntry("media.json").Open());writer.Write(text.Replace("media/","../"));}Reject(()=>Importers.Load(traversal,out _),"package traversal path rejected without extraction");
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main",rel="http://schemas.openxmlformats.org/officeDocument/2006/relationships",draw="http://schemas.openxmlformats.org/drawingml/2006/main";
        XElement P(string text,string image=null)=>new(w+"p",new XElement(w+"r",new XElement(w+"t",text),image==null?null:new XElement(draw+"blip",new XAttribute(rel+"embed",image))));
        string docx=Path.Combine(folder,"synthetic.docx");
        using(var zip=ZipFile.Open(docx,ZipArchiveMode.Create)){
            using(var output=zip.CreateEntry("word/document.xml").Open())new XDocument(new XElement(w+"document",new XElement(w+"body",P("Câu 1.","pic"),P("A.","pic"),P("B.","gif"),P("Đáp án: B"),P("Giải thích: Xem clip"),new XElement(w+"p",new XElement(draw+"videoFile",new XAttribute(rel+"link","video")))))).Save(output);
            XNamespace pkg="http://schemas.openxmlformats.org/package/2006/relationships";
            using(var output=zip.CreateEntry("word/_rels/document.xml.rels").Open())new XDocument(new XElement(pkg+"Relationships",new[]{("pic","x.png"),("gif","x.gif"),("video","x.mp4")}.Select(x=>new XElement(pkg+"Relationship",new XAttribute("Id",x.Item1),new XAttribute("Target","media/"+x.Item2))))).Save(output);
            foreach(var item in new[]{("x.png",png),("x.gif",gif),("x.mp4",mp4)}){using var output=zip.CreateEntry("word/media/"+item.Item1).Open();output.Write(item.Item2);}
        }
        var imageOnly=Importers.Load(docx,out _).Single();Check(imageOnly.Media.Count==4&&imageOnly.Media.Any(m=>m.Target=="note"&&m.Mime=="video/mp4")&&imageOnly.Text.Contains("Xem hình"),"DOCX media-only stem/choices and video-only note retain slots");
        Check(imageOnly.Answer=="B"&&QuestionReview.DuplicateChoices(imageOnly).Count==0&&QuestionReview.ScoringAnswer(imageOnly)=="B","different media-only choices are not false duplicates");
        string json=Path.Combine(folder,"media.json");File.WriteAllText(json,System.Text.Json.JsonSerializer.Serialize(new[]{new {number=1,text="JSON",options=new{A="a",B="b"},answer="B",media=new[]{new{name="x.png",mime="image/png",target="question",data=Convert.ToBase64String(png)}}}}));Check(Importers.Load(json,out _)[0].Media[0].Data.SequenceEqual(png),"JSON base64 media imports without URL fetch");
        var inbox=new ExternalQuizInbox(Path.Combine(folder,"inbox"));using(var input=File.OpenRead(package))inbox.AddAsync(input,"Shared.quiz").GetAwaiter().GetResult();Check(Importers.Load(inbox.ContentPath(inbox.List().Single()),out _)[0].Media.Count==3,"external file inbox receives media ZIP unchanged");
        if(original!=null){var actual=Importers.Load(original,out var s);Console.WriteLine("ACTUAL questions="+actual.Count+" media="+actual.Sum(q=>q.Media.Count));
            Check(actual.Single(q=>q.Number==19).Media.Count==1&&actual.Single(q=>q.Number==19).Media[0].Mime=="image/png"&&actual.Single(q=>q.Number==19).Media[0].Target=="question","actual meteorology q19 map attached to stem");
            Check(actual.Single(q=>q.Number==19).Answer=="C"&&actual.Single(q=>q.Number==33).Text.Contains("Nhiệt độ đo"),"actual media source answer and split heading preserved");
            Check(actual.Count==53&&actual.Last().Number==53&&actual.All(q=>q.Options.Count>=3),"actual Word auto-numbered supplementary questions retained");
            string result=Path.Combine(folder,"Khi-tuong-53-cau.quiz");QuizFile.Save(result,actual,s);var round=Importers.Load(result,out _);Check(round.Count==actual.Count&&round.Single(q=>q.Number==19).Media[0].Data.SequenceEqual(actual.Single(q=>q.Number==19).Media[0].Data),"actual meteorology portable export retains map bytes");
        }
        Console.WriteLine($"TOTAL {checks} media checks passed");
    }
}
