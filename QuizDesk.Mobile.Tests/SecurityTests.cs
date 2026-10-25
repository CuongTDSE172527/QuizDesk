using QuizDesk;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;
partial class Tests {
    static void SecurityChecks(string folder){
        Directory.CreateDirectory(folder);
        string Text(string name,string value){string path=Path.Combine(folder,name);File.WriteAllText(path,value);return path;}
        string Zip(string name,params (string name,string content)[] entries){string path=Path.Combine(folder,name);using(var zip=ZipFile.Open(path,ZipArchiveMode.Create))foreach(var e in entries){using var writer=new StreamWriter(zip.CreateEntry(e.name).Open());writer.Write(e.content);}return path;}
        void Block(Action action,string name){bool blocked=false;try{action();}catch(Exception ex) when(ex is InvalidDataException||ex is IOException||ex is XmlException||ex is RegexMatchTimeoutException){blocked=true;}Check(blocked,name);}
        string question="<question number='1' included='true'><text>Chọn?</text><answer>B</answer><note/><option key='A'>Một</option><option key='B'>Hai</option></question>";
        string Quiz(string body)=>"<quiz version='1'><settings count='1' minutes='20' shuffleQuestions='false' shuffleOptions='false'/>"+body+"</quiz>";
        string valid=Text("valid.quiz",Quiz(question));Check(QuizFile.Load(valid,out _).Single().Answer=="B","Bounded .quiz preserves valid content");
        string entity="<!DOCTYPE quiz [<!ENTITY x SYSTEM 'file:///does-not-exist-quizdesk-secret'>]>"+Quiz(question.Replace("Chọn?","&x;"));
        Block(()=>QuizFile.Load(Text("entity.quiz",entity),out _),"Explicitly rejects external entities in .quiz");
        Block(()=>QuizFile.Load(Text("deep.quiz",Quiz(string.Concat(Enumerable.Repeat("<x>",70))+string.Concat(Enumerable.Repeat("</x>",70)))),out _),"Deep XML is rejected before building its full tree");
        string tooMany=string.Concat(Enumerable.Range(1,5001).Select(i=>question.Replace("number='1'","number='"+i+"'")));
        Block(()=>Importers.Load(Text("5001.quiz",Quiz(tooMany)),out _),"Direct .quiz import enforces 5000-question limit");
        Block(()=>QuizFile.Load(Text("huge-question.quiz",Quiz(question.Replace("Chọn?",new string('a',65537)))),out _),"Oversized question fields rejected");
        string over=new string('x',(int)InputSafety.MaxXmlBytes+1);
        foreach(var f in new[]{("oversize.docx","word/document.xml"),("oversize.odt","content.xml"),("oversize.xlsx","xl/sharedStrings.xml")}){
            string path=Zip(f.Item1,(f.Item2,over));var watch=Stopwatch.StartNew();Block(()=>Importers.Load(path,out _),f.Item1+" compressed XML bomb rejected");Check(watch.ElapsedMilliseconds<3000,"Compressed payload rejected before expansion: "+f.Item1);
        }
        string many=Path.Combine(folder,"too-many-entries.docx");using(var zip=ZipFile.Open(many,ZipArchiveMode.Create))for(int i=0;i<2049;i++)zip.CreateEntry("e"+i);
        Block(()=>WordReader.Read(many),"Archive entry count bounded");
        string forged=Path.Combine(folder,"forged-count.docx");File.Copy(many,forged);using(var stream=File.Open(forged,FileMode.Open,FileAccess.ReadWrite)){stream.Position=stream.Length-14;stream.Write(new byte[]{1,0,1,0});}
        Block(()=>WordReader.Read(forged),"Forged ZIP entry count cannot bypass pre-allocation directory scan");
        string largeName=Zip("long-entry.docx",(new string('a',3000),"x"));Block(()=>WordReader.Read(largeName),"Oversized ZIP metadata name rejected before entry allocation");
        string duplicate=Zip("duplicate.docx",("word/document.xml","<x/>"),("word/document.xml","<x/>"));Block(()=>WordReader.Read(duplicate),"Duplicate archive part names rejected");
        string total=Path.Combine(folder,"expanded-total.docx");using(var zip=ZipFile.Open(total,ZipArchiveMode.Create)){
            byte[] chunk=new byte[65536];for(int i=0;i<9;i++){using var entry=zip.CreateEntry("part"+i).Open();for(int j=0;j<256;j++)entry.Write(chunk);}
        }
        Block(()=>WordReader.Read(total),"Total expanded archive size bounded");
        XNamespace w="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        string doc=new XDocument(new XElement(w+"document",new XElement(w+"body",new XElement(w+"p",new XElement(w+"r",new XElement(w+"rPr",new XElement(w+"rStyle",new XAttribute(w+"val","s0"))),new XElement(w+"t","Câu 1. Chọn?"))),new XElement(w+"p",new XElement(w+"r",new XElement(w+"rPr",new XElement(w+"rStyle",new XAttribute(w+"val","s0"))),new XElement(w+"t","A. Một")))))).ToString();
        string styles=new XDocument(new XElement(w+"styles",Enumerable.Range(0,100).Select(i=>new XElement(w+"style",new XAttribute(w+"styleId","s"+i),new XElement(w+"basedOn",new XAttribute(w+"val","s"+(i+1))))))).ToString();
        Block(()=>WordReader.Read(Zip("style-chain.docx",("word/document.xml",doc),("word/styles.xml",styles))),"Word style inheritance bounded without recursive stack growth");
        string dtd="<!DOCTYPE x [<!ENTITY a 'expanded'>]><x>&a;</x>";
        Block(()=>WordReader.Read(Zip("dtd.docx",("word/document.xml",dtd))),"DOCX disallows DTD declarations");
        Block(()=>Importers.Load(Zip("dtd.odt",("content.xml",dtd)),out _),"ODT disallows DTD declarations");
        const string ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        string workbook=$"<workbook xmlns='{ns}' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships'><sheets><sheet r:id='r1'/></sheets></workbook>";
        string rels="<Relationships><Relationship Id='r1' Target='worksheets/sheet1.xml'/></Relationships>";
        string sheet=$"<worksheet xmlns='{ns}'><sheetData><row><c r='ZZZZZZZZZZZZZZZZZZZZZZZ1'><v>1</v></c></row></sheetData></worksheet>";
        Block(()=>Importers.Load(Zip("bad-cell.xlsx",("xl/workbook.xml",workbook),("xl/_rels/workbook.xml.rels",rels),("xl/worksheets/sheet1.xml",sheet)),out _),"Excel column arithmetic cannot wrap around");
        Block(()=>Importers.Load(Zip("external-sheet.xlsx",("xl/workbook.xml",workbook),("xl/_rels/workbook.xml.rels",rels.Replace("worksheets/sheet1.xml","https://invalid.example/sheet.xml"))),out _),"External XLSX worksheet targets rejected without requests");
        Block(()=>Importers.Csv(new string(',',300),','),"CSV column count bounded");
        string hostile=Text("unclosed.html",string.Concat(Enumerable.Repeat("<script>",20000)));var timeout=Stopwatch.StartNew();Block(()=>Importers.Load(hostile,out _),"Actual malformed HTML aborts regex backtracking");Check(timeout.ElapsedMilliseconds<3000,"Actual HTML regex deadline returns promptly");
        Block(()=>SafeRegex.IsMatch(new string('a',20000)+"!","^(a+)+$"),"Regex operations enforce a finite deadline");
        string oversize=Path.Combine(folder,"oversize.txt");using(var s=File.Create(oversize))s.SetLength(InputSafety.MaxFileBytes+1);
        Block(()=>Reader.Read(oversize),"Direct text reader enforces input size");
        string state=Path.Combine(folder,"workspace");Directory.CreateDirectory(state);Text("workspace/workspace.json","{\"Bank\":[null],\"Setup\":{},\"Title\":\"Invalid\"}");
        Check(new MobileWorkspace(state).LoadBank(out string warning)==null&&warning!="","Malformed saved draft warns rather than crashing startup");
        string libraryPath=Path.Combine(folder,"library");Directory.CreateDirectory(libraryPath);Guid id=Guid.NewGuid();File.WriteAllText(Path.Combine(libraryPath,id.ToString("N")+".json"),JsonSerializer.Serialize(new {Version=1,Id=id,CreatedAt=DateTimeOffset.UtcNow,UpdatedAt=DateTimeOffset.UtcNow,Content=new {Bank=new object[]{null},Setup=new {},Title="bad"},Activities=Array.Empty<object>()}));
        Check(new MobileBankLibrary(libraryPath).List(out warning).Count==0&&warning!="","Invalid saved library bank is isolated with warning");
        string studyFolder=Path.Combine(folder,"study");Directory.CreateDirectory(studyFolder);using(var s=File.Create(Path.Combine(studyFolder,"flashcards.json")))s.SetLength(InputSafety.MaxFileBytes+1);
        Check(new FlashcardStore(studyFolder).Load(out warning)==null&&warning!="","Oversized saved study returns warning without startup crash");
        string preferences=Text("settings.json",new string(' ',1024*1024+1));Check(new PreferencesStore(preferences).Load(out warning).Theme=="light"&&warning!="","Oversized settings fall back safely");
        var normal=Importers.Load(valid,out var setup);string roundtrip=Path.Combine(folder,"roundtrip.quiz");QuizFile.Save(roundtrip,normal,setup);Check(Importers.Load(roundtrip,out _).Single().Answer=="B","Valid data still exports and imports after hardening");
        Console.WriteLine("TOTAL "+checks+" security checks passed.");
    }
}
