using QuizDesk;
using System.IO.Compression;
using System.Xml.Linq;

partial class Tests {
    static void WordSymbolChecks(string root,string realFile){
        string folder=Path.Combine(root,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);int serial=0;
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement R(string text)=>new(w+"r",new XElement(w+"t",text));
        XElement Sym(string code,string font="Symbol")=>new(w+"r",new XElement(w+"sym",new XAttribute(w+"font",font),new XAttribute(w+"char",code)));
        XElement P(params XElement[] runs)=>new(w+"p",runs);
        string Fixture(params XElement[] paragraphs){
            string path=Path.Combine(folder,$"symbols-{serial++}.docx");
            using var zip=ZipFile.Open(path,ZipArchiveMode.Create);
            using var output=zip.CreateEntry("word/document.xml").Open();
            new XDocument(new XElement(w+"document",new XElement(w+"body",paragraphs))).Save(output);return path;
        }
        List<Question> Read(XElement content)=>Importers.Load(Fixture(P(R("1. Unit?")),content,P(R("B. 12ms.")),P(R("Đáp án: A"))),out _);
        var bank=Read(P(R("A. 12"),Sym("F06D"),R("s.")));
        Check(bank.Single().Options["A"]=="12µs."&&bank.Single().Answer=="A","Word Symbol F06D preserves microseconds and answer association");
        Check(Read(P(R("A. 12"),Sym("006D"),R("s."))).Single().Options["A"]=="12µs.","Non-private Symbol code maps identically");
        Check(Read(P(R("A. 12"),Sym("f06d","symbolmt"),R("s."))).Single().Options["A"]=="12µs.","Symbol font aliases and lowercase hex are accepted");
        var formatted=new XElement(w+"r",new XElement(w+"rPr",new XElement(w+"rFonts",new XAttribute(w+"ascii","Symbol"))),new XElement(w+"t","m"));
        Check(Read(P(R("A. 12"),formatted,R("s."))).Single().Options["A"]=="12µs.","Symbol-encoded text runs preserve their glyphs");
        Check(Read(P(R("A. µ μ ± × ÷ ≤ ≥ ∑ √ Ω π"))).Single().Options["A"]=="µ μ ± × ÷ ≤ ≥ ∑ √ Ω π","Existing Unicode mathematical text is unchanged");
        foreach(var pair in new[]{("F0B0","°"),("F0B1","±"),("F0B4","×"),("F0B8","÷"),("F0A3","≤"),("F0B3","≥"),("F0B9","≠"),("F0A5","∞"),("F0D6","√"),("F0E5","∑"),("F0F2","∫"),("F057","Ω"),("F070","π")})
            Check(Read(P(R("A. "),Sym(pair.Item1))).Single().Options["A"]==pair.Item2,"Symbol math glyph preserved: "+pair.Item2);
        var unknown=Read(P(R("A. 12"),Sym("F06D","Unknown Font"),R("s."))).Single();
        Check(unknown.Options["A"].Contains("⟦ký hiệu")&&unknown.ImportWarnings.Count>0,"Unknown symbol fonts remain visible and require review instead of silently dropping units");
        Check(Read(P(R("A. "),Sym("ZZZZ"))).Single().Options["A"].Contains("⟦ký hiệu"),"Malformed symbol codes do not crash or vanish");
        Check(Read(P(R("A. "),Sym("FFFF"))).Single().Options["A"].Contains("⟦ký hiệu"),"Non-XML symbol codes are preserved as review labels, not invalid Unicode");
        var inline=Importers.Load(Fixture(P(R("1. Unit?")),P(R("A. 12"),Sym("F06D"),R("s.")),P(R("B. 18"),Sym("F06D"),R("s.")),P(R("Đáp án: 12"),Sym("F06D"),R("s.")),P(R("Giải thích: 1"),Sym("F06D"),R("s = 10^-6 s."))),out var setup);
        Check(inline.Single().Answer=="A"&&inline.Single().Note.Contains("1µs"),"Text answer matching and explanation retain symbols");
        var study=FlashcardSession.Create("Symbols",null,inline,setup);
        Check(study.Cards.Single().Back=="A. 12µs."&&study.Cards.Single().Choices.Contains("18µs")&&study.Cards.Single().Note.Contains("1µs"),"Flashcard answer, choices and note retain the imported micro sign");
        string package=Path.Combine(folder,"roundtrip.quiz");QuizFile.Save(package,inline,setup);
        Check(Importers.Load(package,out _).Single().Options["A"]=="12µs.","Quiz save/reimport preserves decoded symbols");
        if(realFile!=null){
            var dme=Importers.Load(realFile,out var config);
            Check(dme.Count==32,"Actual DME file imports all 32 questions");
            Check(dme[0].Options["A"]=="12µs."&&dme[1].Options["A"]=="50µs.","Actual DME 12/50 microsecond values are restored");
            var glyphs=string.Concat(dme.SelectMany(q=>q.Options.Values));
            Check(glyphs.Count(c=>c=='µ')==8&&glyphs.Count(c=>c=='±')==4&&glyphs.Count(c=>c=='τ')==4,"Actual DME retains all 16 Symbol glyphs: micro, plus-minus and tau");
            // The original file has no answer keys. Confirm one in memory only,
            // as a user would in the editor, before checking flashcard output.
            dme[0].Answer="A";
            var cards=FlashcardSession.Create("DME",null,dme,config);
            Check(cards.Cards.First(c=>c.Number==dme[0].Number).Back=="A. 12µs.","Actual DME flashcard shows microseconds after an answer is confirmed");
        }
        Console.WriteLine("TOTAL "+checks+" Word symbol checks passed.");
    }
}
