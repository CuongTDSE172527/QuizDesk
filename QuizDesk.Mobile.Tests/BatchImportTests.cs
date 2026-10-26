using QuizDesk;
partial class Tests {
    static void BatchImportChecks(string folder) {
        Directory.CreateDirectory(folder);var library=new MobileBankLibrary(Path.Combine(folder,"library"));
        Question Q(int n,string text,string answer)=>new(){Number=n,Text=text,Answer=answer,Note="Note "+text,Options=new(){{"A","One"},{"B","Two"}}};
        WorkspaceBank Read(string file){var questions=Importers.Load(file,out var setup);return new(){Title=Path.GetFileNameWithoutExtension(file),Bank=questions,Setup=setup};}
        string first=Path.Combine(folder,"first.quiz"),second=Path.Combine(folder,"second.quiz");
        var qa=new List<Question>{Q(1,"First 1","A"),Q(2,"First 2","B")};
        var qb=new List<Question>{Q(1,"Second 1","B"),Q(2,"Second 2","")};qb[1].NeedsReview=true;qb[1].ImportWarnings.Add("Missing answer");
        QuizFile.Save(first,qa,new(){Included=new(){1},Count=1});QuizFile.Save(second,qb,new(){Included=new(){1,2},Count=2});
        var imports=new[]{new ImportedBank("first.quiz",Read(first)),new ImportedBank("second.quiz",Read(second))};
        var merged=library.ImportCombined(imports,"Combined",false);
        Check(merged.Content.Bank.Select(q=>q.Number).SequenceEqual(new[]{1,2,3,4}),"Batch renumbers overlapping source numbers after independent parsing");
        Check(merged.Content.Bank.Select(q=>q.Answer).SequenceEqual(new[]{"A","B","B",""}),"Same source question numbers never cross-assign answers");
        Check(merged.Content.Bank.Select(q=>q.Note).SequenceEqual(new[]{"Note First 1","Note First 2","Note Second 1","Note Second 2"}),"Explanations stay attached when renumbering");
        Check(merged.Content.Bank[3].NeedsReview&&merged.Content.Bank[3].ImportWarnings.Contains("Missing answer"),"Unanswered questions remain importable and flagged");
        Check(merged.ImportedAt.HasValue&&merged.SourceName=="Nhập nhiều file","Combined import appears in import history");
        Check(merged.Content.Setup.Included.SetEquals(new[]{1,2,3,4}),"Combined import includes every question regardless of source exam selection");
        Check(imports[1].Content.Bank[0].Number==1&&imports[1].Content.Bank[0].Answer=="B","Combining leaves source numbers and answers unchanged");
        var receipt=merged.Activities.Single(a=>a.Kind=="merged");
        Check(receipt.Changes[2].Before.Contains("second.quiz")&&receipt.Changes[2].Before.Contains("Câu gốc 1")&&receipt.Changes[2].After=="Câu 3","Audit links new number to filename and original number");
        var restored=library.Load(merged.Id);Check(restored.Activities.Single(a=>a.Kind=="merged").Changes.Count==4,"Import source mapping survives restart");
        var shuffled=library.ImportCombined(imports,"Mixed",true,random:new Random(17));
        Check(shuffled.Content.Bank.All(q=>q.Answer==merged.Content.Bank.Single(o=>o.Text==q.Text).Answer&&q.Note=="Note "+q.Text),"Batch shuffle preserves correct answers and notes");
        Check(shuffled.Content.Bank.Select(q=>q.Number).SequenceEqual(new[]{1,2,3,4}),"Shuffled imports have unique consecutive numbers");
        var sameNames=library.ImportCombined(new[]{new ImportedBank("same.quiz",Read(first)),new ImportedBank("same.quiz",Read(second))},"Same filename",false);
        Check(sameNames.Activities.Single(a=>a.Kind=="merged").Changes[2].Before.Contains("[2: same.quiz]"),"Same filenames in different locations remain distinct origins");
        string text1=Path.Combine(folder,"answer-table-1.txt"),text2=Path.Combine(folder,"answer-table-2.txt");
        File.WriteAllText(text1,"Câu 1. First table?\nA. One\nB. Two\nCâu 2. First second?\nA. Three\nB. Four\nĐÁP ÁN:\n1 B\n2 A");
        File.WriteAllText(text2,"Câu 1. Second table?\nA. Five\nB. Six\nCâu 2. Second second?\nA. Seven\nB. Eight\nĐÁP ÁN:\n1 A\n2 B");
        var tables=library.ImportCombined(new[]{new ImportedBank("answer-table-1.txt",Read(text1)),new ImportedBank("answer-table-2.txt",Read(text2))},"Separate keys",false);
        Check(tables.Content.Bank.Count==4&&tables.Content.Bank.Select(q=>q.Answer).SequenceEqual(new[]{"B","A","A","B"}),"Each file's trailing answer table is matched before combining overlapping numbers");
        var separate=imports.Select(i=>library.Create(i.Content,i.SourceName,true)).ToArray();
        Check(separate.Length==2&&separate.All(b=>b.Content.Bank.Select(q=>q.Number).SequenceEqual(new[]{1,2})),"Separate banks preserve original numbering independently");
        void Reject(Action action,string label){int before=Directory.GetFiles(Path.Combine(folder,"library"),"*.json").Length;bool rejected=false;try{action();}catch(InvalidDataException){rejected=true;}Check(rejected&&before==Directory.GetFiles(Path.Combine(folder,"library"),"*.json").Length,label);}
        var invalid=new WorkspaceBank {Title="Invalid",Bank=new(){Q(1,"A","A"),Q(1,"B","B")}};
        Reject(()=>library.ImportCombined(new[]{imports[0],new ImportedBank("bad.quiz",invalid)},"Invalid",false),"Invalid source creates no partial combined bank");
        Reject(()=>library.ImportCombined(imports," ",false),"Blank combined title rejected before writing");
        Reject(()=>library.ImportCombined(new[]{imports[0]},"Only one",false),"One file cannot masquerade as a combined import");
        Check(library.ImportCombined(Enumerable.Repeat(imports[0],21),"Many files",false).Content.Bank.Count==42,"Batch accepts more than twenty files within content budgets");
        merged.Content.Bank[0].Options["A"]="Changed";Check(imports[0].Content.Bank[0].Options["A"]=="One","Merged choices are independent deep copies");
        Console.WriteLine("TOTAL "+checks+" batch import checks passed.");
    }
}
