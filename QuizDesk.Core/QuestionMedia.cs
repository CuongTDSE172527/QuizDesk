using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
namespace QuizDesk;

public sealed class QuestionMedia {
    public string Name {get;set;}="";
    public string Mime {get;set;}="";
    // question, note, or a choice label A–H. Never a filesystem path or URL.
    public string Target {get;set;}="question";
    public byte[] Data {get;set;}=Array.Empty<byte>();
    public QuestionMedia Clone(string target=null)=>new(){Name=Name,Mime=Mime,Target=target??Target,Data=(byte[])Data.Clone()};
}
public static class MediaFiles {
    public const int ImageLimit=8*1024*1024,ItemLimit=16*1024*1024,BankLimit=24*1024*1024,MaxItems=1000;
    public static string Detect(ReadOnlySpan<byte> b){
        if(b.Length>=24&&b[..8].SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}))return "image/png";
        if(b.Length>=4&&b[0]==255&&b[1]==216&&b[2]==255)return "image/jpeg";
        if(b.Length>=13&&(b[..6].SequenceEqual("GIF89a"u8)||b[..6].SequenceEqual("GIF87a"u8)))return "image/gif";
        if(b.Length>=16&&b[..4].SequenceEqual("RIFF"u8)&&b.Slice(8,4).SequenceEqual("WEBP"u8))return "image/webp";
        if(b.Length>=16&&b.Slice(4,4).SequenceEqual("ftyp"u8))return "video/mp4";
        if(b.Length>=16&&b[..4].SequenceEqual(new byte[]{26,69,223,163}))return "video/webm";
        throw new InvalidDataException("Chỉ hỗ trợ ảnh PNG/JPEG/WebP, GIF và video MP4/WebM. File không đúng định dạng hoặc bị hỏng.");
    }
    public static string Extension(string mime)=>mime switch {"image/png"=>".png","image/jpeg"=>".jpg","image/gif"=>".gif","image/webp"=>".webp","video/mp4"=>".mp4","video/webm"=>".webm",_=>throw new InvalidDataException("Media không hỗ trợ.")};
    public static QuestionMedia FromBytes(string name,byte[] bytes,string target="question"){
        InputSafety.Require(bytes!=null&&bytes.Length<=ItemLimit,"Mỗi video tối đa 16 MB, mỗi ảnh/GIF tối đa 8 MB.");
        var media=new QuestionMedia {Name=Path.GetFileName(name).Replace('\\','_'),Data=bytes,Mime=Detect(bytes),Target=target};Check(new[]{media});return media;
    }
    public static async Task<QuestionMedia> ReadAsync(Stream input,string name,string target){
        using var memory=new MemoryStream();byte[] buffer=new byte[65536];int count;
        while((count=await input.ReadAsync(buffer))>0){InputSafety.Require(memory.Length+count<=ItemLimit,"Mỗi file đính kèm tối đa 16 MB.");await memory.WriteAsync(buffer.AsMemory(0,count));}
        return FromBytes(name,memory.ToArray(),target);
    }
    public static void Check(IEnumerable<QuestionMedia> media){
        InputSafety.Require(media!=null,"Danh sách media không hợp lệ.");long total=0;int count=0;
        foreach(var m in media){
            InputSafety.Require(++count<=MaxItems&&m!=null&&m.Name!=null&&m.Name.Length is >0 and <=200&&m.Name.IndexOfAny(new[]{'/','\\','\0'})<0&&m.Mime!=null&&m.Data!=null&&m.Data.Length<=ItemLimit,"Media không hợp lệ hoặc quá lớn.");
            InputSafety.Require(m.Target=="question"||m.Target=="note"||(m.Target?.Length==1&&m.Target[0]>='A'&&m.Target[0]<='H'),"Vị trí media không hợp lệ.");
            InputSafety.Require(Detect(m.Data)==m.Mime,"Nội dung media không khớp định dạng.");
            InputSafety.Require(!m.Mime.StartsWith("image/")||m.Data.Length<=ImageLimit,"Mỗi ảnh/GIF tối đa 8 MB.");
            total+=m.Data.Length;InputSafety.Require(total<=BankLimit,"Tổng media mỗi bộ đề tối đa 24 MB. Hãy giảm dung lượng hoặc chia bộ đề.");
        }
    }
    public static void CheckBank(IEnumerable<Question> bank)=>Check(bank.SelectMany(q=>q.Media??throw new InvalidDataException("Thiếu danh sách media.")));
    public static string Fingerprint(QuestionMedia m)=>Convert.ToHexStringLower(SHA256.HashData(m.Data));
    public static string Describe(IEnumerable<QuestionMedia> media)=>string.Join("\n",media.Select(m=>$"{m.Target} · {m.Name} · {m.Mime} · {m.Data.Length} bytes · {Fingerprint(m)}"));
}
// A portable .quiz with media is a bounded ZIP, never extracted to caller paths.
// Text-only files retain the legacy XML format for backwards compatibility.
public static class MediaQuizFile {
    sealed class Manifest {public int Version {get;set;}=2;public List<Entry> Items {get;set;}=new();}
    sealed class Entry {public int Number {get;set;}public string Name {get;set;}public string Mime {get;set;}public string Target {get;set;}public string Path {get;set;}}
    public static void Save(string path,List<Question> bank,QuizSetup setup){
        Importers.Validate(bank);MediaFiles.CheckBank(bank);string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{
            using(var zip=ZipFile.Open(temp,ZipArchiveMode.Create)){
                using(var text=zip.CreateEntry("quiz.xml").Open())QuizFile.SaveXml(text,bank,setup);
                var manifest=new Manifest();var written=new HashSet<string>();
                foreach(var q in bank)foreach(var m in q.Media){string entry="media/"+MediaFiles.Fingerprint(m)+MediaFiles.Extension(m.Mime);manifest.Items.Add(new(){Number=q.Number,Name=m.Name,Mime=m.Mime,Target=m.Target,Path=entry});if(written.Add(entry)){using var output=zip.CreateEntry(entry,CompressionLevel.NoCompression).Open();output.Write(m.Data);}}
                using var meta=zip.CreateEntry("media.json").Open();JsonSerializer.Serialize(meta,manifest);
            }
            using(var check=InputSafety.OpenFile(temp)){}File.Move(temp,path,true);
        }finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public static List<Question> Load(string path,out QuizSetup setup){
        using var zip=InputSafety.OpenArchive(path);var text=zip.GetEntry("quiz.xml")??throw new InvalidDataException("Thiếu nội dung .quiz.");
        var list=QuizFile.LoadXml(InputSafety.ReadXml(text).Root,out setup);
        var entry=zip.GetEntry("media.json")??throw new InvalidDataException("Thiếu danh sách media.");InputSafety.Require(entry.Length<=1024*1024);
        using var input=entry.Open();var meta=JsonSerializer.Deserialize<Manifest>(input);
        InputSafety.Require(meta?.Version==2&&meta.Items!=null&&meta.Items.Count<=MediaFiles.MaxItems,"Phiên bản .quiz chưa hỗ trợ hoặc quá nhiều media.");
        var questions=list.ToDictionary(q=>q.Number);long total=0;
        foreach(var item in meta.Items){
            InputSafety.Require(item!=null&&questions.ContainsKey(item.Number)&&item.Path!=null&&SafeRegex.IsMatch(item.Path,@"^media/[a-f0-9]{64}\.(png|jpg|gif|webp|mp4|webm)$"),"Đường dẫn media không hợp lệ.");
            var asset=zip.GetEntry(item.Path)??throw new InvalidDataException("Thiếu file media.");total+=asset.Length;InputSafety.Require(asset.Length<=MediaFiles.ItemLimit&&total<=MediaFiles.BankLimit,"Media quá lớn.");
            using var stream=asset.Open();byte[] data=new byte[(int)asset.Length];stream.ReadExactly(data);
            var m=new QuestionMedia {Name=item.Name,Mime=item.Mime,Target=item.Target,Data=data};MediaFiles.Check(new[]{m});
            InputSafety.Require(item.Path=="media/"+MediaFiles.Fingerprint(m)+MediaFiles.Extension(m.Mime),"Media bị thay đổi hoặc không đúng mã kiểm tra.");questions[item.Number].Media.Add(m);
        }
        Importers.Validate(list);return list;
    }
}
