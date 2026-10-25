using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
namespace QuizDesk;
// Shared bounds apply before materializing documents, including direct reader entry points.
public static class InputSafety {
    public const int MaxQuestions=5000,MaxRows=10002,MaxColumns=256,MaxDepth=64;
    public const long MaxFileBytes=50L*1024*1024,MaxXmlBytes=16L*1024*1024;
    public const int MaxTextCharacters=16*1024*1024;
    public static void Require(bool condition,string message="Tài liệu vượt giới hạn xử lý an toàn. Hãy chia thành file nhỏ hơn."){if(!condition)throw new InvalidDataException(message);}
    public static Stream OpenFile(string path,long limit=MaxFileBytes){var stream=File.OpenRead(path);try{Require(stream.Length<=limit);return new CappedStream(stream,limit);}catch{stream.Dispose();throw;}}
    public static string ReadText(string path,long limit=MaxFileBytes){using var input=OpenFile(path,limit);using var reader=new StreamReader(input,Encoding.UTF8,true);var result=new StringBuilder();var buffer=new char[8192];int count;while((count=reader.Read(buffer,0,buffer.Length))>0){Require(result.Length<=MaxTextCharacters-count);result.Append(buffer,0,count);}return result.ToString();}
    public static ZipArchive OpenArchive(string path){ValidateArchiveHeader(path);var zip=ZipFile.OpenRead(path);try{Require(zip.Entries.Count<=2048);long total=0;var names=new HashSet<string>(StringComparer.Ordinal);foreach(var entry in zip.Entries){Require(names.Add(entry.FullName),"Tài liệu có mục nén trùng tên.");Require(entry.Length<=128L*1024*1024-total);total+=entry.Length;}return zip;}catch{zip.Dispose();throw;}}
    // Check central-directory records before ZipArchive allocates an object per entry.
    // EOCD counts alone are untrusted: a forged count must not bypass this budget.
    static void ValidateArchiveHeader(string path){
        using var file=File.OpenRead(path);Require(file.Length<=MaxFileBytes&&file.Length>=22);
        byte[] tail=new byte[(int)Math.Min(file.Length,65557)];file.Position=file.Length-tail.Length;file.ReadExactly(tail);
        int end=-1;
        for(int i=tail.Length-22;i>=0;i--)if(System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(tail.AsSpan(i))==0x06054b50&&i+22+System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(tail.AsSpan(i+20))==tail.Length){end=i;break;}
        Require(end>=0,"Không tìm thấy cấu trúc ZIP hợp lệ.");
        long endOffset=file.Length-tail.Length+end;
        ushort U16(int offset)=>System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(tail.AsSpan(end+offset));
        uint U32(int offset)=>System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(tail.AsSpan(end+offset));
        Require(U16(4)==0&&U16(6)==0,"Không hỗ trợ ZIP nhiều phần.");
        ulong count=U16(10),size=U32(12),offset=U32(16);
        if(count==65535||size==uint.MaxValue||offset==uint.MaxValue){
            Require(endOffset>=20);file.Position=endOffset-20;byte[] locator=new byte[20];file.ReadExactly(locator);
            Require(System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(locator)==0x07064b50);
            ulong location=System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(locator.AsSpan(8));Require(location<=(ulong)(file.Length-56));
            file.Position=(long)location;byte[] z64=new byte[56];file.ReadExactly(z64);
            Require(System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(z64)==0x06064b50&&System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(z64.AsSpan(16))==0&&System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(z64.AsSpan(20))==0);
            count=System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(z64.AsSpan(32));size=System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(z64.AsSpan(40));offset=System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(z64.AsSpan(48));
        }
        Require(count<=2048&&size<=4*1024*1024&&offset<=(ulong)endOffset&&size<=(ulong)endOffset-offset);
        long stop=(long)(offset+size);file.Position=(long)offset;int actual=0;byte[] header=new byte[46];
        while(file.Position<stop){
            Require(++actual<=2048&&stop-file.Position>=46);file.ReadExactly(header);
            Require(System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(header)==0x02014b50);
            int name=System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(28));
            int extra=System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(30)),comment=System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(32));
            Require(name<=2048&&name+extra+comment<=stop-file.Position);file.Position+=name+extra+comment;
        }
        Require((ulong)actual==count,"Số mục ZIP không khớp cấu trúc.");
    }
    // b2xtranslator writes identical parts twice when its output is closed. Normalize
    // only our own generated DOC conversion; external archives retain strict rejection.
    internal static void NormalizeConvertedArchive(string path){
        ValidateArchiveHeader(path);using var zip=ZipFile.Open(path,ZipArchiveMode.Update);
        Require(zip.Entries.Count<=2048);long total=0;
        foreach(var entry in zip.Entries){Require(entry.Length<=128L*1024*1024-total);total+=entry.Length;}
        var remove=new List<ZipArchiveEntry>();
        foreach(var group in zip.Entries.GroupBy(e=>e.FullName,StringComparer.Ordinal).Where(g=>g.Count()>1)){
            byte[] expected=null;
            foreach(var entry in group){
                Require(entry.Length<=MaxXmlBytes);
                using var bounded=new CappedStream(entry.Open(),MaxXmlBytes);
                byte[] hash=System.Security.Cryptography.SHA256.HashData(bounded);
                if(expected==null)expected=hash;
                else{Require(expected.AsSpan().SequenceEqual(hash),"Bộ chuyển DOC tạo mục trùng có nội dung khác nhau.");remove.Add(entry);}
            }
        }
        foreach(var entry in remove)entry.Delete();
    }
    public static XDocument ReadXml(ZipArchiveEntry entry){Require(entry.Length<=MaxXmlBytes);using var stream=new CappedStream(entry.Open(),MaxXmlBytes);return ReadXml(stream);}
    public static XDocument ReadXml(Stream input){using var xml=XmlReader.Create(input,new XmlReaderSettings {DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=MaxXmlBytes,MaxCharactersFromEntities=1024,CloseInput=false});using var guarded=new DepthReader(xml);return XDocument.Load(guarded);}
    public static void CheckQuestion(Question q){Require(q!=null&&q.Options!=null&&q.Text!=null&&q.Note!=null&&q.Answer!=null,"Nội dung câu hỏi không hợp lệ.");Require(q.Text.Length<=65536&&q.Note.Length<=131072&&q.Answer.Length<=8);Require((q.OriginalAnswer?.Length??0)<=131072&&(q.AnswerSource?.Length??0)<=8192);Require(q.Options.Count<=8&&q.Options.All(o=>o.Key.Length<=8&&o.Value!=null&&o.Value.Length<=32768));Require((q.ImportWarnings?.Count??0)<=32&&(q.ImportWarnings?.All(w=>w!=null&&w.Length<=262144)??true));Require(q.Media!=null&&q.Media.Count<=16,"Mỗi câu tối đa 16 media.");MediaFiles.Check(q.Media);Require(q.Media.All(m=>m.Target=="question"||m.Target=="note"||q.Options.ContainsKey(m.Target)),"Media tham chiếu lựa chọn không tồn tại.");}
    sealed class CappedStream(Stream input,long limit):Stream {
        long read;
        public override int Read(byte[] buffer,int offset,int count)=>Read(buffer.AsSpan(offset,count));
        public override int Read(Span<byte> buffer){int count=input.Read(buffer[..(int)Math.Min(buffer.Length,Math.Max(1,limit-read+1))]);read+=count;Require(read<=limit);return count;}
        public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;
        public override long Length=>input.Length;public override long Position {get=>read;set=>throw new NotSupportedException();}
        public override void Flush(){}public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException();public override void SetLength(long value)=>throw new NotSupportedException();public override void Write(byte[] buffer,int offset,int count)=>throw new NotSupportedException();
        protected override void Dispose(bool disposing){if(disposing)input.Dispose();base.Dispose(disposing);}
    }
    sealed class DepthReader(XmlReader inner):XmlReader {
        int nodes;
        public override bool Read(){bool read=inner.Read();Require(!read||(inner.Depth<=MaxDepth&&++nodes<=1000000&&inner.AttributeCount<=128),"XML lồng quá sâu hoặc có quá nhiều mục.");return read;}
        public override int AttributeCount=>inner.AttributeCount;public override string BaseURI=>inner.BaseURI;public override int Depth=>inner.Depth;
        public override bool EOF=>inner.EOF;public override bool HasValue=>inner.HasValue;public override bool IsEmptyElement=>inner.IsEmptyElement;
        public override string LocalName=>inner.LocalName;public override XmlNameTable NameTable=>inner.NameTable;public override string NamespaceURI=>inner.NamespaceURI;
        public override XmlNodeType NodeType=>inner.NodeType;public override string Prefix=>inner.Prefix;public override ReadState ReadState=>inner.ReadState;public override string Value=>inner.Value;
        public override string GetAttribute(string name)=>inner.GetAttribute(name);public override string GetAttribute(string name,string ns)=>inner.GetAttribute(name,ns);public override string GetAttribute(int i)=>inner.GetAttribute(i);
        public override string LookupNamespace(string prefix)=>inner.LookupNamespace(prefix);public override bool MoveToAttribute(string name)=>inner.MoveToAttribute(name);public override bool MoveToAttribute(string name,string ns)=>inner.MoveToAttribute(name,ns);public override void MoveToAttribute(int i)=>inner.MoveToAttribute(i);
        public override bool MoveToElement()=>inner.MoveToElement();public override bool MoveToFirstAttribute()=>inner.MoveToFirstAttribute();public override bool MoveToNextAttribute()=>inner.MoveToNextAttribute();public override bool ReadAttributeValue()=>inner.ReadAttributeValue();
        public override void ResolveEntity()=>throw new XmlException("DTD/entity không được hỗ trợ.");public override void Close()=>inner.Close();
    }
}
// Per operation deadline; third-party parsers are not process-sandboxed by this timeout.
public static class SafeRegex {
    public static readonly TimeSpan Timeout=TimeSpan.FromMilliseconds(250);
    public static bool IsMatch(string input,string pattern,RegexOptions options=RegexOptions.None)=>Regex.IsMatch(input,pattern,options,Timeout);
    public static Match Match(string input,string pattern,RegexOptions options=RegexOptions.None)=>Regex.Match(input,pattern,options,Timeout);
    public static MatchCollection Matches(string input,string pattern,RegexOptions options=RegexOptions.None)=>Regex.Matches(input,pattern,options,Timeout);
    public static string Replace(string input,string pattern,string replacement,RegexOptions options=RegexOptions.None)=>Regex.Replace(input,pattern,replacement,options,Timeout);
}
