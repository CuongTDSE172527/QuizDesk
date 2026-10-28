using QuizDesk;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
partial class Tests {
    static int checks;
    static void Check(bool condition,string name) {if(!condition)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
    [STAThread] static void Main(string[] args) {
        if(args.Length==0){Console.WriteLine("Usage: QuizDesk.Tests <samples-folder> [original.docx reviewed.docx] OR --settings <test-folder>");return;}
        if(args[0]=="--office-export"){OfficeExportChecks(args[1]);return;}
        if(args[0]=="--export-sampling"){ExportSamplingChecks(args[1]);return;}
        if(args[0]=="--media"){MediaChecks(args[1],args.Length>2?args[2]:null);return;}
        if(args[0]=="--batch-import"){BatchImportChecks(args[1]);return;}
        if(args[0]=="--merge"){MergeChecks(args[1]);return;}
        if(args[0]=="--word-symbols"){WordSymbolChecks(args[1],args.Length>2?args[2]:null);return;}
        if(args[0]=="--word-sections"){WordSectionChecks(args[1],args.Length>2?args[2]:null);return;}
        if(args[0]=="--font-color-file"){FontColorFileChecks(args[1]);return;}
        if(args[0]=="--recognition"){RecognitionChecks(args[1],args.Length>2?args[2]:null);return;}
        if(args[0]=="--security"){SecurityChecks(args[1]);return;}
        if(args[0]=="--mobile"){MobileChecks(args[1]).GetAwaiter().GetResult();return;}
        if(args[0]=="--external"){ExternalChecks(args[1]).GetAwaiter().GetResult();return;}
        if(args[0]=="--answer-text"){AnswerTextChecks(args[1],args.Length>2?args[2]:null);return;}
        if(args[0]=="--study"){StudyChecks(args[1],args.Length>2?args[2]:null);return;}
        if(args[0]=="--review"){ReviewChecks(args[1]);return;}
        if(args[0]=="--library"){LibraryChecks(args[1]);return;}
        if(args[0]=="--history"){HistoryChecks(args[1]);return;}
        if(args[0]=="--settings"){PreferenceChecks(args[1]);return;}
        string samples=Path.Combine(Path.GetTempPath(),"QuizDeskTests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(samples);
        foreach(string fixture in Directory.GetFiles(args[0]))File.Copy(fixture,Path.Combine(samples,Path.GetFileName(fixture)));
        foreach(string path in Directory.GetFiles(samples).Where(p=>!p.EndsWith(".quiz")&&!Path.GetFileName(p).StartsWith("legacy-test")&&!Path.GetFileName(p).StartsWith("Review-"))) {
            var bank=Importers.Load(path,out var setup);
            Check(bank.Count==2,Path.GetExtension(path)+" count");
            Check(bank[0].Answer=="B"&&bank[1].Answer=="A",Path.GetExtension(path)+" answer keys");
            Check(setup.Included.Count==2,Path.GetExtension(path)+" selection");
        }
        var original=Importers.Load(Path.Combine(samples,"De-mau.json"),out var settings);
        settings.ShuffleOptions=true;settings.Count=2;
        for(int seed=0;seed<30;seed++) {
            var exam=QuizSetup.Build(original,settings,false,new Random(seed));
            Check(exam.All(q=>q.Options[q.Answer]==original.Single(o=>o.Number==q.Number).Options[original.Single(o=>o.Number==q.Number).Answer]),"shuffle "+seed);
        }
        var temp=Path.Combine(samples,"roundtrip.quiz");
        QuizFile.Save(temp,original,settings);var restored=Importers.Load(temp,out var config);
        Check(restored.Count==2&&restored[0].Text==original[0].Text&&config.ShuffleOptions,"old .quiz roundtrip");
        settings.Included.Clear();bool invalid=false;try{QuizSetup.Build(original,settings,false,new Random());}catch{invalid=true;}Check(invalid,"empty selection rejected");
        var csv=Importers.Csv("text,A,B,answer,note\n\"Line 1\nLine 2\",one,two,B,\"A \"\"quote\"\"\"",',');
        Check(csv[1][0]=="Line 1\nLine 2"&&csv[1][4]=="A \"quote\"","CSV multiline/quotes");
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        string ansiRtf=Path.Combine(samples,"ansi-test.rtf");
        File.WriteAllBytes(ansiRtf,Encoding.GetEncoding(1252).GetBytes(@"{\rtf1\ansi\ansicpg1252 Câu 1. Chon café?\par A. Café\par B. Tea\par Answer: B\par}"));
        var rtfBank=Importers.Load(ansiRtf,out _);File.Delete(ansiRtf);
        Check(rtfBank.Count==1&&rtfBank[0].Text.Contains("café")&&rtfBank[0].Answer=="B","RTF stream preserves raw ANSI code page and answer");
        invalid=false;try{Importers.Validate(new List<Question>{new Question {Number=1,Text="Bad",Answer="C",Options=new Dictionary<string,string>{{"A","x"},{"B","y"}}}});}catch{invalid=true;}Check(invalid,"invalid answer rejected");
        if(args.Length>2) {
            var old=Importers.Load(args[1],out var oldSetup);var reviewed=Importers.Load(args[2],out var reviewedSetup);
            Check(old.Count==40&&reviewed.Count==40,"Both supplied VOR files: 40 questions");
            Check(old.All(q=>q.Options.Count==4)&&reviewed.All(q=>q.Options.Count==4),"Word automatic letter numbering retained");
            Check(old[9].Answer=="A"&&reviewed[9].Answer=="D"&&reviewed[9].OriginalAnswer=="a","Question 10: revised answer takes precedence");
            Check(reviewed[16].Answer==""&&reviewed[36].Answer=="","Questions 17 and 37 excluded from scoring");
            Check(reviewed[16].Note.Contains("Morse")&&reviewed[36].Note.Contains("107"),"Invalid-question explanations retained");
            Check(reviewed[39].Answer=="B"&&reviewed[39].Options["B"].Contains("28%"),"Question 40 automatic choice matches table key");
            Check(old.All(q=>q.Note==""),"No fabricated explanations in original file");
            Check(reviewed.Count(q=>q.Note!="")==15,"15 supplied review notes retained");
            foreach(var q in reviewed)Console.WriteLine("VOR "+q.Number+" | "+q.Answer+" | old "+q.OriginalAnswer+" | "+q.AnswerSource+" | "+q.Note.Replace("\n"," "));
            string export=Path.Combine(samples,"VOR-40-da-ra-soat.quiz");QuizFile.Save(export,reviewed,reviewedSetup);
            var check=Importers.Load(export,out _);Check(check[9].OriginalAnswer=="a"&&check[16].NeedsReview,"Provenance and review flags persist");
        }
        File.Copy(Path.Combine(samples,"De-mau.xls"),Path.Combine(samples,"legacy-test.xls"),true);
        LegacyChecks("",Path.Combine(samples,"De-mau.xlsx"),samples);
        Console.WriteLine("TOTAL "+checks+" checks passed.");
        Directory.Delete(samples,true);
    }
    static void LegacyChecks(string docx,string xlsx,string folder) {
        string converted=Path.GetFullPath(Path.Combine(folder,"legacy-converted.docx"));
        LegacyOffice.ConvertDoc(Path.Combine(AppContext.BaseDirectory,"Fixtures","simple.doc"),converted);
        string text=Reader.Read(converted);
        Check(text.Length>10,"Binary DOC converts to readable DOCX without Office");
        Console.WriteLine("LEGACY DOC CONTENT: "+text.Substring(0,Math.Min(120,text.Length)));
        File.Delete(converted);
        var docQuestions=Importers.Load(Path.Combine(AppContext.BaseDirectory,"Fixtures","quiz.doc"),out _);
        Check(docQuestions.Count==1&&docQuestions[0].Options.Count==2&&docQuestions[0].Answer=="B","Binary DOC end-to-end quiz import");
        var sheet=Importers.Load(Path.Combine(folder,"legacy-test.xls"),out _);
        Check(sheet.Count==2&&sheet[0].Answer=="B"&&sheet[1].Answer=="A","Binary XLS imports questions and answers without Office");
    }
    static void PreferenceChecks(string folder) {
        folder=Path.Combine(folder,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        string path=Path.Combine(folder,"settings.json");var store=new PreferencesStore(path);
        var settings=store.Load(out var warning);Check(warning==""&&settings.QuestionCount==20&&settings.Minutes==20,"Missing settings uses defaults");
        Check(settings.AeroGlass,"Missing settings enables desktop Aero preference by default");
        Check(settings.GlassBrightness==0&&settings.GlassTransparency==50,"Legacy/default settings preserve the original glass appearance");
        settings.GlassBrightness=-23;settings.GlassTransparency=85;
        settings.AeroGlass=false;
        settings.Theme="dark";settings.QuestionCount=17;settings.Minutes=42;settings.ShuffleQuestions=false;settings.ShuffleOptions=true;store.Save(settings);
        var restored=new PreferencesStore(path).Load(out warning);
        Check(warning==""&&restored.Theme=="dark"&&restored.QuestionCount==17&&restored.Minutes==42&&!restored.ShuffleQuestions&&restored.ShuffleOptions,"New settings store restores all preferences");
        Check(!restored.AeroGlass,"Disabled Aero preference survives a settings reload");
        Check(restored.GlassBrightness==-23&&restored.GlassTransparency==85,"Both glass slider values survive a settings reload");
        settings.AeroGlass=true;store.Save(settings);Check(store.Load(out warning).AeroGlass,"Aero preference can be re-enabled and persisted");
        settings.Theme="light";store.Save(settings);Check(File.ReadAllText(path+".bak").Contains("dark"),"Atomic save preserves previous settings backup");
        Check(!Directory.EnumerateFiles(folder,"*.tmp").Any(),"Atomic save leaves no incomplete temporary files");
        File.WriteAllText(path,"{broken");restored=store.Load(out warning);Check(warning!=""&&restored.Theme=="light","Corrupt settings does not crash startup");
        store.Save(settings);Check(File.ReadAllText(path+".bak")=="{broken","Corrupt file retained in backup when replaced");
        File.WriteAllText(path,"{\"Version\":99}");restored=store.Load(out warning);Check(warning!=""&&restored.QuestionCount==20,"Unsupported settings version falls back safely");
        File.WriteAllText(path,"{\"Theme\":\"system\"}");restored=store.Load(out warning);Check(warning==""&&restored.Theme=="system"&&restored.QuestionCount==20&&restored.AeroGlass&&restored.GlassBrightness==0&&restored.GlassTransparency==50,"Partial settings retain defaults for new fields");
        File.WriteAllText(path,"{\"Theme\":\"invalid\",\"QuestionCount\":-5,\"Minutes\":900}");restored=store.Load(out warning);Check(restored.Theme=="light"&&restored.QuestionCount==1&&restored.Minutes==300,"Out-of-range settings are normalized");
        File.WriteAllText(path,"{\"GlassBrightness\":-900,\"GlassTransparency\":900}");restored=store.Load(out warning);
        Check(warning==""&&restored.GlassBrightness==-50&&restored.GlassTransparency==100,"Out-of-range glass slider values are safely clamped");
        settings.QuestionCount=17;var small=settings.CreateSetup(new List<Question>{new Question {Number=7},new Question {Number=9}});
        Check(small.Count==2&&settings.QuestionCount==17&&small.Included.SetEquals(new[]{7,9}),"Small bank clamps exam count without changing global preference");
        Console.WriteLine("TOTAL "+checks+" settings checks passed.");
    }
}

