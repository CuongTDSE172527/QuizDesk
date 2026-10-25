using QuizDesk;
using System.Text;
using System.Text.Json;

partial class Tests {
    static async Task MobileChecks(string root) {
        string folder=Path.GetFullPath(Path.Combine(root,Guid.NewGuid().ToString("N")));var store=new MobileWorkspace(folder);
        Check(store.LoadBank(out var warning)==null&&warning=="","Missing workspace is empty");
        var bank=new List<Question>{new(){Number=7,Text="Nội dung tiếng Việt",Note="Giải thích",Answer="B",OriginalAnswer="A",AnswerSource="Reviewed",NeedsReview=true,Options=new(){{"A","Một"},{"B","Hai"}}}};
        var setup=new QuizSetup {Count=1,Minutes=17,ShuffleOptions=true,Included=new(){7}};store.SaveBank("Bộ đề",bank,setup);
        var restored=new MobileWorkspace(folder).LoadBank(out warning);
        Check(warning==""&&restored.Title=="Bộ đề"&&restored.Bank[0].Text==bank[0].Text&&restored.Bank[0].Answer=="B"&&restored.Bank[0].OriginalAnswer=="A"&&restored.Bank[0].NeedsReview&&restored.Bank[0].Options["B"]=="Hai","Draft restores question fields, provenance, Unicode and choices");
        Check(restored.Setup.Minutes==17&&restored.Setup.ShuffleOptions&&restored.Setup.Included.SetEquals(new[]{7}),"Draft restores exam settings and selection");
        store.SaveBank("Bộ đề",bank,setup,true);Check(store.LoadBank(out _).HasUnexportedChanges,"Unexported-change warning survives process recreation");
        var now=DateTimeOffset.Now;var active=new ActiveMobileExam {Title="Đang thi",StartedAt=now,Deadline=now.AddMinutes(17),Minutes=17,Exam=bank};store.Begin(active);
        byte[] frozen=File.ReadAllBytes(Path.Combine(folder,"active-exam.json"));store.Checkpoint(active.Id,0,new Dictionary<int,string>{{0,"B"}});
        var resumed=new MobileWorkspace(folder).LoadActive(out warning);
        Check(warning==""&&resumed.Id==active.Id&&resumed.Deadline==active.Deadline&&resumed.Answers[0]=="B","Fresh workspace restores attempt ID, deadline and answer");
        Check(frozen.SequenceEqual(File.ReadAllBytes(Path.Combine(folder,"active-exam.json"))),"Answer checkpoint does not rewrite the immutable exam snapshot");
        bank[0].Text="Changed source";Check(store.LoadActive(out _).Exam[0].Text=="Nội dung tiếng Việt","Source edits do not affect in-progress exam snapshot");
        var oldId=active.Id;var next=new ActiveMobileExam {StartedAt=now,Deadline=now.AddMinutes(1),Minutes=1,Exam=bank};store.Begin(next);
        store.Checkpoint(oldId,0,new Dictionary<int,string>{{0,"A"}});Check(store.LoadActive(out _).Answers.Count==0,"Stale checkpoint cannot leak answers into a new attempt");
        store.Checkpoint(next.Id,99,new Dictionary<int,string>{{-1,"B"},{0,"H"},{9,"A"}});resumed=store.LoadActive(out _);
        Check(resumed.Index==0&&resumed.Answers.Count==0,"Invalid saved index and answer keys are sanitized");
        store.Complete(oldId);Check(store.LoadActive(out _)?.Id==next.Id,"Completing an older attempt does not remove the current attempt");store.Complete(next.Id);
        Check(store.LoadActive(out _)==null,"Completing the current attempt clears its checkpoint");
        Check(!Directory.GetFiles(folder,"*.tmp").Any(),"Atomic saves leave no temporary files");
        string cache=Path.Combine(folder,"ImportCache");byte[] data=Encoding.UTF8.GetBytes("Câu 1. Chọn?\nA. Một\nB. Hai\nĐáp án: B");
        using(var stream=new NonSeekableStream(data)) {
            string path=await MobileImportCache.CopyAsync(stream,"../../external.txt",cache);var imported=Importers.Load(path,out _);
            Check(Path.GetDirectoryName(path)==cache&&imported.Single().Answer=="B","Non-seekable document stream imports without a filesystem path or path traversal");File.Delete(path);
        }
        bool rejected=false;using(var tooLarge=new RepeatingStream(MobileImportCache.Limit+1)){try{await MobileImportCache.CopyAsync(tooLarge,"large.txt",cache);}catch(InvalidDataException){rejected=true;}}
        Check(rejected&&!Directory.EnumerateFiles(cache).Any(),"Oversized document rejected and partial cache file removed");
        rejected=false;using(var input=new MemoryStream(data)){try{await MobileImportCache.CopyAsync(input,"file.exe",cache);}catch(InvalidDataException){rejected=true;}}Check(rejected,"Unsupported document extension rejected");
        string draft=Path.Combine(folder,"workspace.json");File.WriteAllText(draft,"{broken");Check(store.LoadBank(out warning)==null&&warning!=""&&File.ReadAllText(draft)=="{broken","Corrupted draft does not crash or get rewritten on load");
        store.Begin(new ActiveMobileExam {StartedAt=now,Deadline=now.AddSeconds(-1),Minutes=1,Exam=bank});Check(store.LoadActive(out _).Deadline<now,"Expired deadline is retained across process recreation");
        Console.WriteLine("TOTAL "+checks+" mobile lifecycle/import checks passed.");
    }
    sealed class NonSeekableStream(byte[] bytes):MemoryStream(bytes) {public override bool CanSeek=>false;public override long Seek(long offset,SeekOrigin loc)=>throw new NotSupportedException();}
    sealed class RepeatingStream(long remaining):Stream {
        public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;
        public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}
        public override int Read(byte[] buffer,int offset,int count){int n=(int)Math.Min(count,remaining);Array.Fill(buffer,(byte)'a',offset,n);remaining-=n;return n;}
        public override ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken token=default){int n=(int)Math.Min(buffer.Length,remaining);buffer.Span[..n].Fill((byte)'a');remaining-=n;return ValueTask.FromResult(n);}
        public override void Flush(){}public override long Seek(long o,SeekOrigin s)=>throw new NotSupportedException();public override void SetLength(long l)=>throw new NotSupportedException();public override void Write(byte[] b,int o,int c)=>throw new NotSupportedException();
    }
}
