using QuizDesk;
using System.Text.Json;

partial class Tests {
    static void SnapshotIsolationChecks(string root){
        var store=new MobileBankLibrary(Path.Combine(root,Guid.NewGuid().ToString("N")));
        byte[] image=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aX1cAAAAASUVORK5CYII=");
        var first=new Question {Number=1,Text="Snapshot µ",Note="Explanation",Answer="B",OriginalAnswer="A",AnswerSource="Reviewed",NeedsReview=true,ImportWarnings=new(){"Review"},Options=new(){{"A","One"},{"B","Two"}},Media=new(){new QuestionMedia {Name="pixel.png",Mime="image/png",Target="B",Data=image}}};
        var second=new Question {Number=2,Text="Other",Answer="A",Options=new(){{"A","Alpha"},{"B","Beta"}}};
        var source=new WorkspaceBank {LibraryBankId=Guid.NewGuid(),Version=1,Title="Isolated",HasUnexportedChanges=true,Bank=new(){first,second},Setup=new(){Count=2,Minutes=45,ShuffleQuestions=false,ShuffleOptions=true,Included=new(){1,2}}};
        var json=new JsonSerializerOptions {IncludeFields=true};
        var oracle=JsonSerializer.Deserialize<WorkspaceBank>(JsonSerializer.Serialize(source,json),json);
        var saved=store.Create(source);oracle.LibraryBankId=saved.Id;
        Check(JsonSerializer.Serialize(saved.Content,json)==JsonSerializer.Serialize(oracle,json),"Direct snapshot matches the previous independent JSON roundtrip for every model field");
        first.Options["A"]="Changed";first.ImportWarnings.Add("Later");first.Media[0].Data[0]^=0xFF;source.Setup.Included.Clear();source.Bank.Clear();
        Check(saved.Content.Bank.Count==2&&saved.Content.Setup.Included.SetEquals(new[]{1,2}),"Question list and selection are isolated from the source");
        Check(saved.Content.Bank[0].Options["A"]=="One"&&saved.Content.Bank[0].ImportWarnings.Count==1,"Choices and review state are isolated");
        Check(saved.Content.Bank[0].Media[0].Data[0]==137,"Media bytes are deep-copied without a base64/JSON intermediary");
        saved.Content.Bank[0].Options["B"]="Unsaved";
        Check(store.Load(saved.Id).Content.Bank[0].Options["B"]=="Two","Mutating a returned snapshot cannot mutate the saved file");
        var limits=new WorkspaceBank {Title="Limits",Bank=new(){second},Setup=new(){Count=9000,Minutes=9000,Included=new(){2,999}}};
        var normalized=store.Create(limits);
        Check(normalized.Content.Setup.Count==1&&normalized.Content.Setup.Minutes==300&&normalized.Content.Setup.Included.SetEquals(new[]{2}),"Snapshot validation still normalizes settings");
        Check(limits.Setup.Count==9000&&limits.Setup.Minutes==9000&&limits.Setup.Included.Contains(999),"Normalization does not mutate the caller's setup");
        bool rejected=false;try{store.Create(new WorkspaceBank {Title="Invalid",Bank=new(){null}});}catch(InvalidDataException){rejected=true;}
        Check(rejected,"Invalid question snapshots are still rejected");
        var bank=Enumerable.Range(1,100).Select(n=>new Question {Number=n,Text="Shuffle "+n,Answer="B",Options=new(){{"A","One"},{"B","Two"},{"C","Three"}},Media=n==1?new(){new QuestionMedia {Name="pixel.png",Mime="image/png",Target="B",Data=(byte[])image.Clone()}}:new()}).ToList();
        var setup=new QuizSetup {Count=100,ShuffleQuestions=true,ShuffleOptions=true,Included=new(bank.Select(q=>q.Number))};
        var a=QuizSetup.Build(bank,setup,false,new Random(42));var b=QuizSetup.Build(bank,setup,false,new Random(42));
        Check(a.Select(q=>q.Number).Order().SequenceEqual(Enumerable.Range(1,100)),"Linear shuffle retains every selected question once");
        Check(a.All(q=>q.Options[q.Answer]=="Two"),"Linear choice shuffle retains the correct answer association");
        Check(a.Select(q=>q.Number).SequenceEqual(b.Select(q=>q.Number))&&a.Zip(b).All(p=>p.First.Options.SequenceEqual(p.Second.Options)),"A fixed random seed remains reproducible");
        var mapped=a.Single(q=>q.Number==1);
        Check(mapped.Media.Single().Target==mapped.Answer&&!ReferenceEquals(mapped.Media.Single().Data,bank[0].Media[0].Data),"Choice media follows the shuffled answer and retains snapshot isolation");
        Check(bank.Select(q=>q.Number).SequenceEqual(Enumerable.Range(1,100))&&bank[0].Answer=="B","Shuffling does not mutate the source bank");
        var catalogFolder=Path.Combine(root,Guid.NewGuid().ToString("N"));var catalog=new MobileBankLibrary(catalogFolder);
        var large=Enumerable.Range(1,1000).Select(n=>new Question {Number=n,Text="Metadata "+n+new string('x',1024),Answer="A",Options=new(){{"A","One"},{"B","Two"}}}).ToList();
        var document=catalog.Create(new WorkspaceBank {Title="Cached catalog",Bank=large,Setup=new(){Included=new(large.Select(q=>q.Number))}});
        var watch=new System.Diagnostics.Stopwatch();long allocated=GC.GetAllocatedBytesForCurrentThread();watch.Start();
        var cold=catalog.List(out var coldWarning);watch.Stop();double coldMs=watch.Elapsed.TotalMilliseconds;long coldBytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
        allocated=GC.GetAllocatedBytesForCurrentThread();watch.Restart();var warm=catalog.List(out var warmWarning);watch.Stop();double warmMs=watch.Elapsed.TotalMilliseconds;long warmBytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
        Check(coldWarning==""&&warmWarning==""&&cold.SequenceEqual(warm),"Cached metadata matches a full verified library read");
        Check(warmBytes<coldBytes/3,"Repeated catalog rendering no longer deserializes full question banks");
        document.Content.Title="Saved rename";catalog.Save(document.Id,document.Content);
        Check(catalog.List(out _).Single().Title=="Saved rename","Saving a bank invalidates cached metadata");
        string catalogPath=Path.Combine(catalogFolder,document.Id.ToString("N")+".json");
        var external=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(catalogPath));external["Content"]["Title"]="External rename";
        File.WriteAllText(catalogPath,external.ToJsonString());File.SetLastWriteTimeUtc(catalogPath,DateTime.UtcNow.AddSeconds(2));
        Check(catalog.List(out _).Single().Title=="External rename","External file changes invalidate cached metadata");
        File.WriteAllText(catalogPath,"{broken");File.SetLastWriteTimeUtc(catalogPath,DateTime.UtcNow.AddSeconds(4));
        Check(catalog.List(out var corruptWarning).Count==0&&corruptWarning!="","Cache does not mask a changed corrupt library file");
        File.WriteAllText(catalogPath,external.ToJsonString());File.SetLastWriteTimeUtc(catalogPath,DateTime.UtcNow.AddSeconds(6));
        Check(catalog.List(out _).Single().Title=="External rename","A repaired library file recovers its metadata");
        catalog.Delete(document.Id);
        Check(catalog.List(out _).Count==0&&catalog.List(out _,true).Single().DeletedAt.HasValue,"Deletion and includeDeleted retain their semantics with a cache");
        File.WriteAllText(Path.Combine(root,"summary-cache-benchmark.json"),JsonSerializer.Serialize(new {Questions=1000,ColdMs=coldMs,WarmMs=warmMs,ColdAllocatedBytes=coldBytes,WarmAllocatedBytes=warmBytes},new JsonSerializerOptions {WriteIndented=true}));
        Console.WriteLine("TOTAL "+checks+" snapshot and shuffle checks passed.");
    }
}
