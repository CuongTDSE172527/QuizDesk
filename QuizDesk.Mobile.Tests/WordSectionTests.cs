using QuizDesk;
using System.IO.Compression;
using System.Xml.Linq;

partial class Tests {
    static void WordSectionChecks(string root,string realFile){
        string folder=Path.Combine(root,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";int serial=0;
        XElement P(string text,string style=null,string color=null)=>new(w+"p",style==null?null:new XElement(w+"pPr",new XElement(w+"pStyle",new XAttribute(w+"val",style))),new XElement(w+"r",color==null?null:new XElement(w+"rPr",new XElement(w+"color",new XAttribute(w+"val",color))),new XElement(w+"t",text)));
        XElement[] Q(int n,string answer)=>new[]{P($"Câu {n}: Question {answer}?"),P("A. One"),P("B. Two"),P("Đáp án: "+answer)};
        XElement Table(string answer)=>new(w+"tbl",new XElement(w+"tr",new XElement(w+"tc",P("Câu")),new XElement(w+"tc",P("Đáp án"))),new XElement(w+"tr",new XElement(w+"tc",P("1")),new XElement(w+"tc",P(answer))));
        string FileFor(IEnumerable<XElement> elements,XElement numbering=null,bool media=false){
            string file=Path.Combine(folder,$"source-{serial++}.docx");using var zip=ZipFile.Open(file,ZipArchiveMode.Create);
            using(var output=zip.CreateEntry("word/document.xml").Open())new XDocument(new XElement(w+"document",new XElement(w+"body",elements))).Save(output);
            using(var output=zip.CreateEntry("word/styles.xml").Open())new XDocument(new XElement(w+"styles",new XElement(w+"style",new XAttribute(w+"styleId","Subject"),new XElement(w+"name",new XAttribute(w+"val","heading 1"))))).Save(output);
            if(numbering!=null)using(var output=zip.CreateEntry("word/numbering.xml").Open())new XDocument(numbering).Save(output);
            if(media){
                XNamespace rel="http://schemas.openxmlformats.org/package/2006/relationships";
                using(var output=zip.CreateEntry("word/_rels/document.xml.rels").Open())new XDocument(new XElement(rel+"Relationships",new XElement(rel+"Relationship",new XAttribute("Id","image1"),new XAttribute("Target","media/image.png")))).Save(output);
                using(var output=zip.CreateEntry("word/media/image.png").Open())output.Write(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aX1cAAAAASUVORK5CYII="));
            }
            return file;
        }
        var toc=P("1. Fake question 99","TOC1");
        var input=new[]{P("Contents"),toc,P("Môn Một","Subject")}.Concat(Q(1,"A")).Concat(new[]{P("Môn Hai","Heading1")}).Concat(Q(1,"B"));
        string file=FileFor(input);var banks=Importers.LoadBanks(file,"Combined.docx");
        Check(banks.Count==2&&banks.Select(b=>b.Content.Title).SequenceEqual(new[]{"Môn Một","Môn Hai"}),"Subjects become separately named banks; TOC excluded");
        Check(banks.All(b=>b.Content.Bank.Single().Number==1)&&banks.Select(b=>b.Content.Bank.Single().Answer).SequenceEqual(new[]{"A","B"}),"Repeated numbers across subjects keep independent answers");
        Check(banks.All(b=>b.SourceName.StartsWith("Combined.docx")&&b.Content.Setup.Included.SetEquals(new[]{1})),"Each subject retains file provenance and a valid selection");
        var flat=Importers.Load(file,out var setup);
        Check(flat.Select(q=>q.Number).SequenceEqual(new[]{1,2})&&flat[1].Text.Contains("Môn Hai")&&setup.Included.SetEquals(new[]{1,2}),"Flat API preserves subjects and gives globally unique numbers");
        string tables=FileFor(new[]{P("First","Heading1")}.Concat(Q(1,"A").Take(3)).Append(Table("A")).Append(P("Second","Heading1")).Concat(Q(1,"B").Take(3)).Append(Table("B")));
        Check(Importers.LoadBanks(tables).Select(b=>b.Content.Bank.Single().Answer).SequenceEqual(new[]{"A","B"}),"Answer tables are scoped to their subject");
        string repeats=FileFor(new[]{P("First","Heading1")}.Concat(Q(1,"A")).Append(P("Second","Heading1")).Concat(Q(1,"A")).Concat(Q(1,"B")));
        var repeated=Importers.LoadBanks(repeats)[1].Content.Bank;
        Check(repeated.Select(q=>q.Number).SequenceEqual(new[]{1,2})&&repeated.Select(q=>q.Answer).SequenceEqual(new[]{"A","B"})&&repeated[1].Text.Contains("Câu gốc 1"),"Supplementary repeated numbers are renumbered only after matching inline answers");
        string colored=FileFor(new[]{P("First","Heading1")}.Concat(Q(1,"A")).Concat(new[]{P("Second","Heading1"),P("Câu 1. Red A?"),P("A. One",color:"EE0000"),P("B. Two"),P("Câu 1. Red B?"),P("A. One"),P("B. Two",color:"EE0000")}));
        Check(Importers.LoadBanks(colored)[1].Content.Bank.Select(q=>q.Answer).SequenceEqual(new[]{"A","B"}),"Repeated source numbers do not share formatting candidates");
        string ambiguous=FileFor(new[]{P("First","Heading1")}.Concat(Q(1,"A")).Append(P("Second","Heading1")).Concat(Q(1,"A").Take(3)).Concat(Q(1,"B").Take(3)).Append(Table("A")));
        Check(Importers.LoadBanks(ambiguous)[1].Content.Bank.All(q=>q.Answer==""&&QuestionReview.RequiresReview(q)),"Repeated numbers cannot silently reuse one table answer");
        var fieldStart=new XElement(w+"p",new XElement(w+"r",new XElement(w+"fldChar",new XAttribute(w+"fldCharType","begin")),new XElement(w+"instrText"," TOC \\o \"1-3\" ")));
        var fieldEnd=new XElement(w+"p",new XElement(w+"r",new XElement(w+"fldChar",new XAttribute(w+"fldCharType","end"))));
        string fields=FileFor(new[]{fieldStart,P("1. Fake TOC question"),fieldEnd}.Concat(Q(1,"B")));
        Check(Importers.LoadBanks(fields).Single().Content.Bank.Single().Answer=="B","Complex TOC fields without TOC paragraph styles are excluded");
        string single=FileFor(Q(1,"A"));
        Check(Importers.LoadBanks(single,"Original.docx").Single().Content.Title=="Original","Single-subject documents retain filename title");
        var numbering=new XElement(w+"numbering");
        foreach(var (id,format) in new[]{("1","decimal"),("2","lowerLetter")}){
            numbering.Add(new XElement(w+"abstractNum",new XAttribute(w+"abstractNumId",id),new XElement(w+"lvl",new XAttribute(w+"ilvl","0"),new XElement(w+"numFmt",new XAttribute(w+"val",format)))));
            numbering.Add(new XElement(w+"num",new XAttribute(w+"numId",id),new XElement(w+"abstractNumId",new XAttribute(w+"val",id))));
        }
        XElement Listed(string text,string id){var p=P(text);p.AddFirst(new XElement(w+"pPr",new XElement(w+"numPr",new XElement(w+"numId",new XAttribute(w+"val",id)))));return p;}
        var auto=Importers.LoadBanks(FileFor(new[]{P("First","Heading1"),Listed("Question one?","1"),Listed("One","2"),Listed("Two","2"),P("Đáp án: A"),P("Second","Heading1"),Listed("Question two?","1"),Listed("One","2"),Listed("Two","2"),P("Đáp án: B")},numbering));
        Check(auto.All(b=>b.Content.Bank.Single().Number==1)&&auto.Select(b=>b.Content.Bank.Single().Answer).SequenceEqual(new[]{"A","B"}),"Automatic Word question and option numbering stays local to each subject");
        var compact=Importers.LoadBanks(FileFor(new[]{P("First","Heading1")}.Concat(Q(1,"A").Take(3)).Concat(new[]{P("ĐÁP ÁN:"),P("1 A"),P("Second","Heading1")}).Concat(Q(1,"B").Take(3)).Concat(new[]{P("ĐÁP ÁN:"),P("1 B")})));
        Check(compact.Select(b=>b.Content.Bank.Single().Answer).SequenceEqual(new[]{"A","B"}),"Compact answer sections are resolved independently after splitting");
        XElement PictureQuestion(){var p=P("Câu 1. Picture?");p.Add(new XElement(w+"r",new XElement(w+"drawing",new XElement(XName.Get("blip","http://schemas.openxmlformats.org/drawingml/2006/main"),new XAttribute(XName.Get("embed","http://schemas.openxmlformats.org/officeDocument/2006/relationships"),"image1")))));return p;}
        var pictures=Importers.LoadBanks(FileFor(new[]{P("First","Heading1"),PictureQuestion()}.Concat(Q(1,"A").Skip(1)).Concat(new[]{P("Second","Heading1"),PictureQuestion()}).Concat(Q(1,"B").Skip(1)),media:true));
        Check(pictures.All(b=>b.Content.Bank.Single().Media.Single().Target=="question"),"Images and their relationships remain attached in each subject");
        string exported=Path.Combine(folder,"subject.quiz");QuizFile.Save(exported,pictures[1].Content.Bank,pictures[1].Content.Setup);
        Check(Importers.Load(exported,out _).Single().Media.Single().Data.SequenceEqual(pictures[1].Content.Bank.Single().Media.Single().Data),"Split subject exports and reimports its original image");
        bool rejected=false;try{Importers.LoadBanks(FileFor(new[]{P("Good","Heading1")}.Concat(Q(1,"A")).Concat(new[]{P("Broken","Heading1"),P("Câu 1. Missing choices")})));}catch(Exception){rejected=true;}
        Check(rejected,"An invalid subject rejects the import instead of silently losing it");
        var library=new MobileBankLibrary(Path.Combine(folder,"library"));
        foreach(var b in banks){var saved=library.Create(b.Content,b.SourceName,true);var restored=library.Load(saved.Id);Check(restored.Content.Title==b.Content.Title&&restored.Content.Bank.Single().Answer==b.Content.Bank.Single().Answer,"Subject persists independently: "+b.Content.Title);}
        Check(library.ImportCombined(banks,"Merged",false).Content.Bank.Select(q=>q.Answer).SequenceEqual(new[]{"A","B"}),"Batch merge consumes independently parsed subjects");
        if(realFile!=null){
            var real=Importers.LoadBanks(realFile);
            Check(real.Count==10&&real.Select(b=>b.Content.Bank.Count).SequenceEqual(new[]{30,25,30,35,35,40,35,32,42,42}),"Supplied combined document contains all 346 questions in ten subjects");
            Check(real.All(b=>b.Content.Bank.Select(q=>q.Number).Distinct().Count()==b.Content.Bank.Count),"Every real subject has unique question numbers");
            Check(real.Sum(b=>b.Content.Bank.Count(q=>q.Answer!=""))==344,"Supplied document retains unresolved source claims for review");
            Check(real.Last().Content.Bank.Skip(34).Select(q=>q.Answer).SequenceEqual(new[]{"A","B","C","C","A","D","C","D"}),"All eight supplementary FMS answers stay with their questions");
            foreach(var b in real)Console.WriteLine($"SUBJECT {b.Content.Title}: {b.Content.Bank.Count} questions, {b.Content.Bank.Count(QuestionReview.RequiresReview)} review");
        }
        Console.WriteLine($"TOTAL {checks} Word section checks passed.");
    }
}
