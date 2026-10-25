using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;

namespace QuizDesk;

public sealed class IncomingQuiz {
    public Guid Id {get;set;}
    public string Name {get;set;}="";
    public string Hash {get;set;}="";
    public DateTimeOffset ReceivedAt {get;set;}
}

// Copy incoming streams while the OS still grants access. The durable receipt is
// committed last, so an interrupted copy can never appear as a pending import.
public sealed class ExternalQuizInbox {
    static readonly ConcurrentDictionary<string,SemaphoreSlim> Gates=new(StringComparer.OrdinalIgnoreCase);
    readonly string folder;
    SemaphoreSlim Gate=>Gates.GetOrAdd(folder,_=>new(1,1));
    public ExternalQuizInbox(string dataRoot)=>folder=Path.GetFullPath(Path.Combine(dataRoot,"IncomingQuiz"));
    string Receipt(Guid id)=>Path.Combine(folder,id.ToString("N")+".json");
    public string ContentPath(IncomingQuiz item)=>Path.Combine(folder,item.Id.ToString("N")+".quiz");
    public List<IncomingQuiz> List() {
        if(!Directory.Exists(folder))return new();
        var items=new List<IncomingQuiz>();
        foreach(string file in Directory.GetFiles(folder,"*.json")) {
            try {
                var item=JsonSerializer.Deserialize<IncomingQuiz>(InputSafety.ReadText(file,64*1024));
                if(item!=null&&item.Id!=Guid.Empty&&Path.GetFileNameWithoutExtension(file)==item.Id.ToString("N")&&File.Exists(ContentPath(item)))items.Add(item);
            }catch(Exception ex) when(ex is IOException||ex is InvalidDataException||ex is JsonException){}
        }
        return items.OrderBy(i=>i.ReceivedAt).ThenBy(i=>i.Id).ToList();
    }
    public async Task<IncomingQuiz> AddAsync(Stream stream,string name) {
        if(!Path.GetExtension(name).Equals(".quiz",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Chỉ mở file .quiz theo cách này. Các định dạng khác dùng nút Nhập file.");
        await Gate.WaitAsync();string copy=null;
        try {
            Directory.CreateDirectory(folder);
            using var lease=await AcquireLease();
            var pending=List();if(pending.Count>=10)throw new InvalidOperationException("Đã có 10 file chờ nhập. Hãy xử lý các file đang chờ trước.");
            copy=await MobileImportCache.CopyAsync(stream,"incoming.quiz",folder);
            string hash;await using(var input=File.OpenRead(copy))hash=Convert.ToHexString(await SHA256.HashDataAsync(input));
            var existing=pending.FirstOrDefault(i=>i.Hash==hash);if(existing!=null)return existing;
            string clean=Path.GetFileName(name.Replace('\\','/'));clean=new string(clean.Where(c=>!char.IsControl(c)).ToArray());if(clean.Length>200)clean=clean[..195]+".quiz";
            var item=new IncomingQuiz {Id=Guid.NewGuid(),Name=clean,Hash=hash,ReceivedAt=DateTimeOffset.UtcNow};
            File.Move(copy,ContentPath(item));copy=null;
            string receipt=Receipt(item.Id),temp=receipt+".tmp";
            try {await File.WriteAllTextAsync(temp,JsonSerializer.Serialize(item));File.Move(temp,receipt,true);}
            catch {File.Delete(ContentPath(item));throw;}
            finally {if(File.Exists(temp))File.Delete(temp);}
            return item;
        }finally{if(copy!=null&&File.Exists(copy))File.Delete(copy);Gate.Release();}
    }
    async Task<FileStream> AcquireLease() {
        var until=DateTime.UtcNow.AddSeconds(15);
        while(true)try{return new FileStream(Path.Combine(folder,"receive.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
        catch(IOException) when(DateTime.UtcNow<until){await Task.Delay(50);}
    }
    public void Remove(IncomingQuiz item) {
        // Receipt disappears first: a crash cannot re-offer an accepted import.
        File.Delete(Receipt(item.Id));File.Delete(ContentPath(item));
    }
}
