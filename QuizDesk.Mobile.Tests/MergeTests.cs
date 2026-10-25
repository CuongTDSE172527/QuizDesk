using QuizDesk;
using System.Security.Cryptography;
partial class Tests {
    static void MergeChecks(string folder){
        var store=new MobileBankLibrary(folder);
        Question Q(int n,string text,string answer="B")=>new(){Number=n,Text=text,Note="Giải thích "+text,Answer=answer,OriginalAnswer="B. Hai",AnswerSource="Dòng sau rà soát",Options=new(){{"A","Một"},{"B","Hai"}}};
        WorkspaceBank Bank(string name,params Question[] questions)=>new(){Title=name,Bank=questions.ToList(),Setup=new(){Count=1,Minutes=20,ShuffleQuestions=false,Included=new(){questions[0].Number}}};
        var pending=Q(18,"A2","");pending.NeedsReview=true;pending.ImportWarnings.Add("Chưa chốt từ nguồn");
        var a=store.Create(Bank("Nguồn A",Q(7,"A1"),pending),"a.docx",true);var b=store.Create(Bank("Nguồn B",Q(7,"B1"),Q(27,"B2")),"b.docx",true);
        string Hash(Guid id)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(folder,id.ToString("N")+".json"))));
        string aHash=Hash(a.Id),bHash=Hash(b.Id);var ids=new[]{a.Id,b.Id};
        var merged=store.Merge(ids,"  Đề tổng hợp  ",false,new AppPreferences {QuestionCount=3,Minutes=45});
        Check(merged.Id!=a.Id&&merged.Id!=b.Id&&merged.Content.Title=="Đề tổng hợp","Merge creates an independent named bank");
        Check(merged.Content.Bank.Select(q=>q.Text).SequenceEqual(new[]{"A1","A2","B1","B2"}),"Unshuffled merge follows selected source order");
        Check(merged.Content.Bank.Select(q=>q.Number).SequenceEqual(Enumerable.Range(1,4)),"Overlapping source question numbers are renumbered uniquely");
        Check(merged.Content.Setup.Included.SetEquals(new[]{1,2,3,4}),"All source questions included even if source setup excludes some");
        Check(merged.Content.Setup.Count==3&&merged.Content.Setup.Minutes==45&&!merged.Content.Setup.ShuffleQuestions&&!merged.Content.Setup.ShuffleOptions,"New setup honors defaults and question-only shuffle option");
        var q2=merged.Content.Bank[1];Check(q2.Answer==""&&q2.Note==pending.Note&&q2.NeedsReview&&q2.ImportWarnings.SequenceEqual(pending.ImportWarnings)&&q2.OriginalAnswer==pending.OriginalAnswer&&q2.AnswerSource==pending.AnswerSource,"Unresolved answers, explanations and provenance remain unchanged");
        Check(Hash(a.Id)==aHash&&Hash(b.Id)==bHash,"Source bank bytes and timestamps unchanged by merge");
        var receipt=merged.Activities.Single(e=>e.Kind=="merged");Check(receipt.At==merged.CreatedAt&&receipt.Description.Contains("Nguồn A")&&receipt.Description.Contains("Nguồn B")&&receipt.Changes.Count==4,"Merge audit and source mapping committed with creation timestamp");
        Check(receipt.Changes[3].Before=="Nguồn B · Câu gốc 27"&&receipt.Changes[3].After=="Câu 4","Audit retains original source numbers after renumbering");
        var restored=new MobileBankLibrary(folder).Load(merged.Id);Check(restored.Content.Bank.Count==4&&restored.Activities.Any(e=>e.Kind=="merged"),"Merged bank and receipt survive a fresh store");
        var s1=store.Merge(ids,"Trộn một",true,random:new Random(123));var s2=store.Merge(ids,"Trộn hai",true,random:new Random(123));
        Check(s1.Content.Bank.Select(q=>q.Text).SequenceEqual(s2.Content.Bank.Select(q=>q.Text)),"Fixed seed gives reproducible test shuffle");
        Check(s1.Content.Bank.Select(q=>q.Text).Order().SequenceEqual(merged.Content.Bank.Select(q=>q.Text).Order()),"Shuffling loses or duplicates no questions");
        Check(s1.Content.Bank.All(q=>q.Note=="Giải thích "+q.Text&&q.Options["B"]=="Hai"&&q.Answer==(q.Text=="A2"?"":"B")),"Answer and explanation stay attached to each shuffled question");
        Check(s1.Content.Setup.ShuffleQuestions&&!s1.Content.Setup.ShuffleOptions,"Question shuffle remains configurable for study/exam");
        bool different=false;for(int seed=0;seed<5;seed++)different|=!store.Merge(ids,"Thử trộn "+seed,true,random:new Random(seed)).Content.Bank.Select(q=>q.Text).SequenceEqual(merged.Content.Bank.Select(q=>q.Text));Check(different,"Question shuffle changes order across seeds");
        string quiz=Path.Combine(folder,"merged.quiz");QuizFile.Save(quiz,s1.Content.Bank,s1.Content.Setup);var roundtrip=Importers.Load(quiz,out var config);
        Check(roundtrip.Select(q=>(q.Text,q.Answer,q.Note)).SequenceEqual(s1.Content.Bank.Select(q=>(q.Text,q.Answer,q.Note)))&&config.ShuffleQuestions,"Merged quiz exports and reimports with correct associations");
        restored.Content.Bank[0].Options["A"]="Chỉnh sửa riêng";restored.Content.Bank[1].ImportWarnings.Clear();store.Save(restored.Id,restored.Content);
        Check(Hash(a.Id)==aHash&&Hash(b.Id)==bHash&&store.Load(a.Id).Content.Bank[0].Options["A"]=="Một","Editing merged options/warnings cannot edit a source bank");
        void Reject(Action action,string label){int count=Directory.GetFiles(folder,"*.json").Length;bool rejected=false;try{action();}catch(Exception ex) when(ex is InvalidDataException||ex is IOException){rejected=true;}Check(rejected&&Directory.GetFiles(folder,"*.json").Length==count,label);}
        Reject(()=>store.Merge(new[]{a.Id},"Một",true),"One source rejected without creating bank");
        Reject(()=>store.Merge(new[]{a.Id,a.Id},"Trùng chọn",true),"Duplicate IDs cannot masquerade as two sources");
        Reject(()=>store.Merge(ids," ",true),"Empty merged title rejected atomically");Reject(()=>store.Merge(ids,new string('x',201),true),"Oversized merged title rejected atomically");
        Reject(()=>store.Merge(new[]{a.Id,Guid.NewGuid()},"Thiếu",true),"Missing source causes no partial merged bank");
        var empty=store.Create(new(){Title="Trống"});Reject(()=>store.Merge(new[]{a.Id,empty.Id},"Trống",false),"Empty source rejected clearly");
        var duplicate=store.Create(Bank("Bản sao",Q(1,"A1")));Check(store.Merge(new[]{a.Id,duplicate.Id},"Giữ đủ câu",false).Content.Bank.Count(q=>q.Text=="A1")==2,"Equal question content across sources is retained");
        store.Delete(a.Id);Check(store.Load(merged.Id).Content.Bank.Count==4,"Deleting source cannot remove merged question snapshots");
        Reject(()=>store.Merge(ids,"Đã xóa",true),"Deleted source rejected atomically");
        WorkspaceBank Many(string name,int n)=>new(){Title=name,Bank=Enumerable.Range(1,n).Select(i=>Q(i,name+i)).ToList(),Setup=new(){Included=new(){1}}};
        var big1=store.Create(Many("Lớn A",2500));var big2=store.Create(Many("Lớn B",2500));
        Check(store.Merge(new[]{big1.Id,big2.Id},"5.000 câu",true,random:new Random(1)).Content.Bank.Count==5000,"Exactly 5000 merged questions are allowed");
        var extra=store.Create(Bank("Thêm",Q(1,"Extra")));Reject(()=>store.Merge(new[]{big1.Id,big2.Id,extra.Id},"Quá lớn",true),"5001 questions rejected without truncation or partial write");
        string longText=new string('x',65530);WorkspaceBank Payload(string name)=>new(){Title=name,Bank=Enumerable.Range(1,130).Select(i=>new Question {Number=i,Text=longText,Answer="A",Options=new(){{"A","Một"},{"B","Hai"}}}).ToList(),Setup=new(){Included=new(){1}}};
        var payload1=store.Create(Payload("Dung lượng A"));var payload2=store.Create(Payload("Dung lượng B"));Reject(()=>store.Merge(new[]{payload1.Id,payload2.Id},"Quá dung lượng",false),"Combined text budget rejected before snapshot expansion");
        Check(!Directory.EnumerateFiles(folder,"*.tmp").Any(),"Merge operations leave no temporary commits");
        Console.WriteLine("TOTAL "+checks+" merge checks passed.");
    }
}
