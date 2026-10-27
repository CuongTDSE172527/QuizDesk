using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
namespace QuizDesk;

public sealed record WordSection(string Title,List<Question> Questions);

public static partial class WordReader {
    static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    static string Plain(XElement e)=>string.Concat(e.Descendants().Where(x=>x.Name==W+"t"||x.Name==W+"tab"||x.Name==W+"br").Select(x=>x.Name==W+"t"?x.Value:x.Name==W+"tab"?"\t":"\n")).Trim();
    static bool On(XElement e)=>e!=null&&!new[]{"0","false","off"}.Contains((string)e.Attribute(W+"val"));
    public static List<Question> Read(string path) {
        var sections=ReadSections(path);
        if(sections.Count==1)return sections[0].Questions;
        var result=new List<Question>();
        foreach(var section in sections)foreach(var q in section.Questions){
            q.Text=$"[{section.Title} — Câu {q.Number}]\n{q.Text}";q.Number=result.Count+1;result.Add(q);
        }
        Importers.Validate(result);return result;
    }
    public static List<WordSection> ReadSections(string path) {
        using var zip=InputSafety.OpenArchive(path);
        XDocument Xml(string name){var entry=zip.GetEntry(name);if(entry==null)return null;return InputSafety.ReadXml(entry);}
        var doc=Xml("word/document.xml")??throw new Exception("File Word thiếu nội dung.");
        var styles=Xml("word/styles.xml");var numbering=Xml("word/numbering.xml");
        var sections=SplitSections(doc,styles,Path.GetFileNameWithoutExtension(path));
        var result=new List<WordSection>();
        foreach(var section in sections){
            try {result.Add(new(section.title,ReadDocument(section.doc,styles,numbering,zip,Xml,sections.Count>1)));}
            catch(Exception ex) when(ex is InvalidDataException||ex.GetType()==typeof(Exception)){
                throw new InvalidDataException($"{section.title}: {ex.Message}",ex);
            }
        }
        InputSafety.Require(result.Sum(s=>s.Questions.Count)<=InputSafety.MaxQuestions);
        MediaFiles.CheckBank(result.SelectMany(s=>s.Questions).ToList());
        return result;
    }
    static List<Question> ReadDocument(XDocument doc,XDocument styles,XDocument numbering,ZipArchive zip,Func<string,XDocument> Xml,bool allowRepeatedNumbers) {
        var styleIndex=new Dictionary<string,XElement>(StringComparer.Ordinal);
        foreach(var style in styles?.Root?.Elements(W+"style")??Enumerable.Empty<XElement>()) {
            string id=(string)style.Attribute(W+"styleId");
            InputSafety.Require(!string.IsNullOrEmpty(id)&&id.Length<=1024&&styleIndex.TryAdd(id,style),"Mã kiểu Word thiếu hoặc trùng.");
        }
        var styleCache=new Dictionary<(string id,string prop),XElement>();
        var tableKeys=new Dictionary<int,List<(string answer,string old,string note,string source)>>();
        var answerTables=new HashSet<XElement>();
        foreach(var table in doc.Descendants(W+"tbl")) {
            var rows=table.Elements(W+"tr").ToList();if(rows.Count<2)continue;
            var headers=rows[0].Elements(W+"tc").Select(c=>Plain(c).ToLowerInvariant()).ToList();
            int num=headers.FindIndex(s=>s=="câu"||s=="stt"||s=="số câu");
            int revised=headers.FindIndex(s=>s.Contains("rà soát")||s.Contains("đề xuất")||s.Contains("đáp án mới")||s.Contains("đáp án đúng"));
            int old=headers.FindIndex(s=>s.Contains("cũ")||s.Contains("gốc"));
            int generic=headers.FindIndex(s=>s=="đáp án"||s=="answer");
            int answer=revised>=0?revised:generic;
            int note=headers.FindIndex(s=>s.Contains("ghi chú")||s.Contains("giải thích")||s.Contains("lưu ý"));
            if(num<0||answer<0)continue;answerTables.Add(table);
            foreach(var row in rows.Skip(1)) {
                var cells=row.Elements(W+"tc").Select(Plain).ToList();
                string Cell(int i)=>i>=0&&i<cells.Count?cells[i]:"";
                if(!int.TryParse(Cell(num),out int n))continue;
                if(!tableKeys.ContainsKey(n))tableKeys[n]=new();
                tableKeys[n].Add((Cell(answer),Cell(old),Cell(note),revised>=0?"Bảng đáp án rà soát":"Bảng đáp án"));
            }
        }
        XElement StyleProp(string id,string prop,bool paragraph=false) {
            if(string.IsNullOrEmpty(id))return null;var cacheKey=(id,paragraph?"p:"+prop:prop);if(styleCache.TryGetValue(cacheKey,out var cached))return cached;
            var seen=new HashSet<string>();
            while(!string.IsNullOrEmpty(id)&&seen.Add(id)) {
                InputSafety.Require(seen.Count<=64,"Chuỗi kế thừa kiểu Word quá sâu.");
                styleIndex.TryGetValue(id,out var style);
                var value=style?.Element(W+(paragraph?"pPr":"rPr"))?.Element(W+prop);if(value!=null){styleCache[cacheKey]=value;return value;}
                id=(string)style?.Element(W+"basedOn")?.Attribute(W+"val");
            }styleCache[cacheKey]=null;return null;
        }
        double Ratio(XElement p,Func<XElement,bool> marked) {
            int total=0,yes=0;foreach(var run in p.Descendants(W+"r")) {int len=SafeRegex.Replace(Plain(run),@"\s","").Length;total+=len;if(marked(run))yes+=len;}
            return total==0?0:(double)yes/total;
        }
        bool Bold(XElement p)=>Ratio(p,r=>{
            var own=r.Element(W+"rPr");var b=own?.Element(W+"b")??StyleProp((string)own?.Element(W+"rStyle")?.Attribute(W+"val"),"b")??StyleProp((string)p.Element(W+"pPr")?.Element(W+"pStyle")?.Attribute(W+"val"),"b");return On(b);
        })>=0.8;
        XElement RunProp(XElement run,XElement paragraph,string property)=>run.Element(W+"rPr")?.Element(W+property)
            ??StyleProp((string)run.Element(W+"rPr")?.Element(W+"rStyle")?.Attribute(W+"val"),property)
            ??StyleProp((string)paragraph.Element(W+"pPr")?.Element(W+"pStyle")?.Attribute(W+"val"),property);
        bool Shaded(XElement shading){
            if(shading==null||(string)shading.Attribute(W+"val")=="nil")return false;
            string theme=(string)shading.Attribute(W+"themeFill"),fill=(string)shading.Attribute(W+"fill");
            return !string.IsNullOrEmpty(theme)&&theme!="background1"&&theme!="background2"||!string.IsNullOrEmpty(fill)&&!new[]{"auto","FFFFFF","ffffff","none"}.Contains(fill);
        }
        bool Colored(XElement color){
            if(color==null)return false;
            string theme=(string)color.Attribute(W+"themeColor");
            if(!string.IsNullOrEmpty(theme)&&theme!="none")return theme.StartsWith("accent",StringComparison.OrdinalIgnoreCase);
            string rgb=(string)color.Attribute(W+"val");
            // Automatic, black, white and neutral gray text are ordinary text colors.
            return rgb!=null&&SafeRegex.IsMatch(rgb,@"^[0-9a-fA-F]{6}$")
                &&!(rgb[..2].Equals(rgb[2..4],StringComparison.OrdinalIgnoreCase)&&rgb[..2].Equals(rgb[4..],StringComparison.OrdinalIgnoreCase));
        }
        bool Highlighted(XElement paragraph,string label){
            string style=(string)paragraph.Element(W+"pPr")?.Element(W+"pStyle")?.Attribute(W+"val");
            if(Shaded(paragraph.Element(W+"pPr")?.Element(W+"shd")??StyleProp(style,"shd",true)))return true;
            bool Marked(XElement run){
                string color=(string)RunProp(run,paragraph,"highlight")?.Attribute(W+"val");
                return !string.IsNullOrEmpty(color)&&color!="none"||Shaded(RunProp(run,paragraph,"shd"))||Colored(RunProp(run,paragraph,"color"));
            }
            if(Ratio(paragraph,Marked)>=0.8)return true;
            // An intentionally highlighted A./B./… label is also a source marker.
            // A highlighted word inside a long answer is not sufficient evidence.
            string prefix="";bool marked=false;
            foreach(var run in paragraph.Descendants(W+"r")){
                string text=Plain(run);if(string.IsNullOrWhiteSpace(text))continue;
                prefix+=text.Trim();marked|=Marked(run);
                if(prefix.Length>3)return false;
                if(SafeRegex.IsMatch(prefix,@"^[A-H][.)]$",RegexOptions.IgnoreCase))return marked&&prefix[0].ToString().Equals(label,StringComparison.OrdinalIgnoreCase);
            }return false;
        }
        var result=new List<Question>();Question current=null;string option=null;bool noteMode=false,answerMode=false,answerFinished=false;
        var inline=new Dictionary<Question,List<string>>();
        var reviewed=new Dictionary<Question,ReviewedAnswers>();
        var bold=new Dictionary<Question,HashSet<string>>();var highlighted=new Dictionary<Question,HashSet<string>>();
        string ListFormat(XElement p) {
            var props=p.Element(W+"pPr")?.Element(W+"numPr");if(props==null)return "";
            string id=(string)props.Element(W+"numId")?.Attribute(W+"val"),level=(string)props.Element(W+"ilvl")?.Attribute(W+"val")??"0";
            string abstractId=(string)numbering?.Descendants(W+"num").FirstOrDefault(n=>(string)n.Attribute(W+"numId")==id)?.Element(W+"abstractNumId")?.Attribute(W+"val");
            string format=(string)numbering?.Descendants(W+"abstractNum").FirstOrDefault(n=>(string)n.Attribute(W+"abstractNumId")==abstractId)?.Elements(W+"lvl").FirstOrDefault(l=>(string)l.Attribute(W+"ilvl")==level)?.Element(W+"numFmt")?.Attribute(W+"val");
            return format??"";
        }
        bool IsLetterList(XElement p)=>ListFormat(p) is "lowerLetter" or "upperLetter";
        XNamespace rel="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        var relationships=Xml("word/_rels/document.xml.rels")?.Root?.Elements().ToDictionary(e=>(string)e.Attribute("Id"),StringComparer.Ordinal)??new();
        long mediaBytes=0;
        void Attach(XElement paragraph,string target){
            if(current==null)return;
            var ids=paragraph.Descendants().Where(e=>e.Name.LocalName is "blip" or "imagedata" or "videoFile").SelectMany(e=>new[]{(string)e.Attribute(rel+"embed"),(string)e.Attribute(rel+"id"),(string)e.Attribute(rel+"link")}).Where(id=>!string.IsNullOrEmpty(id)).Distinct();
            foreach(string id in ids){
                if(!relationships.TryGetValue(id,out var relationship)){QuestionReview.Warn(current,"Thiếu liên kết media trong Word.");continue;}
                string address=(string)relationship.Attribute("Target");
                if((string)relationship.Attribute("TargetMode")=="External"){QuestionReview.Warn(current,"Media liên kết bên ngoài chưa được tải: hãy đính kèm file gốc trong màn hình Sửa.");continue;}
                try{
                    var uri=new Uri(new Uri("https://quizdesk.local/word/document.xml"),address);
                    InputSafety.Require(uri.Scheme=="https"&&uri.Host=="quizdesk.local"&&uri.Query==""&&uri.Fragment==""&&uri.AbsolutePath.StartsWith("/word/",StringComparison.Ordinal),"Địa chỉ media Word không hợp lệ.");
                    var part=zip.GetEntry(Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')))??throw new InvalidDataException("Thiếu file media Word.");
                    InputSafety.Require(part.Length<=MediaFiles.ItemLimit&&mediaBytes+part.Length<=MediaFiles.BankLimit,"Media Word vượt giới hạn.");
                    using var input=part.Open();byte[] bytes=new byte[(int)part.Length];input.ReadExactly(bytes);var media=MediaFiles.FromBytes(Path.GetFileName(part.FullName),bytes,target);
                    InputSafety.Require(current.Media.Count<16,"Mỗi câu tối đa 16 media.");current.Media.Add(media);mediaBytes+=bytes.Length;
                }catch(InvalidDataException ex){throw new InvalidDataException($"Câu {current.Number}: {ex.Message}",ex);}
            }
            if(paragraph.Descendants().Any(e=>e.Name.LocalName=="OLEObject"))QuestionReview.Warn(current,"Đối tượng OLE nhúng chưa hỗ trợ. Hãy thêm file ảnh/GIF/video gốc trong màn hình Sửa.");
        }
        var paragraphs=doc.Descendants(W+"p").Where(p=>!p.Ancestors(W+"tbl").Any(t=>answerTables.Contains(t))).ToList();
        for(int index=0;index<paragraphs.Count;index++) {
            var p=paragraphs[index];
            string line=Plain(p);if(line==""){Attach(p,noteMode||answerMode||answerFinished?"note":option??"question");continue;}
            if(SafeRegex.IsMatch(line,@"^(?:BẢNG\s+)?ĐÁP\s+ÁN\s*[:：]?\s*$",RegexOptions.IgnoreCase)){
                int next=index+1;while(next<paragraphs.Count&&Plain(paragraphs[next])=="")next++;
                bool found=false;
                while(next<paragraphs.Count){
                    var key=SafeRegex.Match(Plain(paragraphs[next]),@"^(?:Câu\s*)?(\d+)\s*[.:)-]?\s*([A-H])\s*$",RegexOptions.IgnoreCase);
                    if(!key.Success)break;
                    int number=int.Parse(key.Groups[1].Value);
                    if(!tableKeys.ContainsKey(number))tableKeys[number]=new();
                    tableKeys[number].Add((key.Groups[2].Value.ToUpperInvariant(),"","","Bảng đáp án cuối môn"));
                    found=true;index=next;next++;while(next<paragraphs.Count&&Plain(paragraphs[next])=="")next++;
                }
                if(found){option=null;noteMode=answerMode=false;answerFinished=true;continue;}
            }
            if(ReviewedAnswers.IsReferences(line)||SafeRegex.IsMatch(line,@"^(ĐỐI CHIẾU ĐÁP ÁN|Tài liệu đối chiếu|Các câu cần đặc biệt|Màu xanh lá:)",RegexOptions.IgnoreCase))break;
            var q=SafeRegex.Match(line,@"^(?:Câu\s+(\d+)\s*[.:)]?|(\d+)\s*[.)])\s*(.*)$",RegexOptions.IgnoreCase);
            bool autoQuestion=ListFormat(p) is "decimal" or "decimalZero";
            if((q.Success&&!IsLetterList(p))||autoQuestion) {
                InputSafety.Require(result.Count<InputSafety.MaxQuestions);current=new Question {Number=autoQuestion?(result.Count==0?1:result.Max(x=>x.Number)+1):int.Parse(q.Groups[1].Success?q.Groups[1].Value:q.Groups[2].Value),Text=autoQuestion?line:q.Groups[3].Value};result.Add(current);option=null;noteMode=false;answerMode=false;answerFinished=false;
                bold[current]=new HashSet<string>();highlighted[current]=new HashSet<string>();inline[current]=new();reviewed[current]=new();Attach(p,"question");continue;
            }
            if(current==null)continue;
            if(reviewed[current].Read(line)){Attach(p,"note");option=null;noteMode=false;answerMode=false;answerFinished=false;continue;}
            if(QuestionReview.TryReadAnswerLine(line,out string answer)){Attach(p,"note");inline[current].Add(answer);option=null;noteMode=false;answerMode=!QuestionReview.HasResolvedClaim(current,answer);answerFinished=!answerMode;continue;}
            var note=SafeRegex.Match(line,@"^(?:Giải thích|Ghi chú|Lời giải|Lưu ý)\s*:\s*(.*)$",RegexOptions.IgnoreCase);
            if(note.Success){Attach(p,"note");current.Note=note.Groups[1].Value;noteMode=true;answerMode=false;option=null;continue;}
            if(noteMode){Attach(p,"note");current.Note+="\n"+line;continue;}
            if(answerFinished){Attach(p,"note");continue;}
            if(answerMode){Attach(p,"note");inline[current][^1]+="\n"+line;answerFinished=QuestionReview.HasResolvedClaim(current,inline[current][^1]);answerMode=!answerFinished;continue;}
            var o=SafeRegex.Match(line,@"^([A-H])[.)]\s*(.*)$",RegexOptions.IgnoreCase|RegexOptions.Singleline);
            if(o.Success||IsLetterList(p)) {
                option=o.Success?o.Groups[1].Value.ToUpperInvariant():((char)('A'+current.Options.Count)).ToString();
                option=QuestionReview.AddChoice(current,option,o.Success?o.Groups[2].Value:line);
                if(string.IsNullOrWhiteSpace(current.Options[option])&&p.Descendants().Any(e=>e.Name.LocalName is "blip" or "imagedata" or "videoFile"))current.Options[option]="(Xem media)";
                Attach(p,option);if(Bold(p))bold[current].Add(option);if(Highlighted(p,option))highlighted[current].Add(option);
            } else if(option!=null){Attach(p,option);current.Options[option]+="\n"+line;if(Highlighted(p,option))highlighted[current].Add(option);}
            else {Attach(p,"question");current.Text+="\n"+line;}
        }
        var repeated=result.GroupBy(q=>q.Number).Where(g=>g.Count()>1).Select(g=>g.Key).ToHashSet();
        foreach(var q in result) {
            if(string.IsNullOrWhiteSpace(q.Text)&&q.Media.Any(m=>m.Target=="question"))q.Text="Xem hình ảnh / video và chọn đáp án.";
            if(bold[q].Count==1)q.OriginalAnswer=bold[q].Single();
            if(tableKeys.ContainsKey(q.Number)&&repeated.Contains(q.Number)){
                QuestionReview.Warn(q,"Số câu gốc bị trùng nên không thể ghép chắc chắn với bảng đáp án. Hãy xác nhận đáp án đúng.");
            } else if(tableKeys.TryGetValue(q.Number,out var rows)) {
                if(rows.Count>1)QuestionReview.Warn(q,"Có "+rows.Count+" dòng đáp án cho cùng câu trong bảng. Hãy kiểm tra và chọn lại đáp án.");
                q.OriginalAnswer=string.Join("; ",rows.Select(r=>r.old).Where(s=>s!=""));
                // A reviewed table intentionally supersedes the inline/original key.
                // Repeated table rows, however, must be resolved by the user.
                QuestionReview.ApplyAnswerClaims(q,rows.Select(r=>r.answer),rows[0].source);
                q.Note=string.Join("\n",new[]{q.Note}.Concat(rows.Select(r=>r.note)).Append(q.Answer==""?"Kết luận trong bảng: "+string.Join("; ",rows.Select(r=>r.answer)):"").Where(s=>s!=""));
                if(q.Answer=="")q.NeedsReview=true;
            } else if(reviewed[q].Apply(q)) {
                // Additional inline claims are compared with this key below.
            } else if(inline[q].Count>0) {
                QuestionReview.ApplyAnswerClaims(q,inline[q],"Dòng đáp án trong câu");
            } else {
                var candidates=highlighted[q].Union(bold[q]).ToHashSet();
                if(candidates.Count==1) {q.Answer=candidates.Single();q.AnswerSource=highlighted[q].Count>0?"Lựa chọn được highlight/tô nền/tô màu chữ":"Lựa chọn in đậm";}
                else {q.AnswerSource=candidates.Count>1?"Nhiều lựa chọn cùng định dạng — cần xác nhận":"Chưa tìm thấy đáp án";q.NeedsReview=true;}
            }
            var marked=highlighted[q].Union(bold[q]).ToHashSet();
            if(marked.Count>1)QuestionReview.Warn(q,"Nhiều lựa chọn cùng được đánh dấu: "+string.Join(", ",marked.OrderBy(x=>x))+". Hãy xác nhận đáp án đúng.");
            else if(marked.Count==1&&q.Answer!=""&&!marked.Contains(q.Answer))
                QuestionReview.Warn(q,"Đáp án được đánh dấu khác với đáp án ghi trong tài liệu. Hãy xác nhận đáp án đúng.");
            // Compare additional explicit sources without overriding the selected key.
            if(tableKeys.ContainsKey(q.Number)||reviewed[q].Original.Count>0||reviewed[q].Revised.Count>0)
                foreach(string claim in inline[q]){
                    var other=new Question {Options=q.Options};
                    QuestionReview.ApplyAnswerClaims(other,new[]{claim},"Đối chiếu");
                    if(other.Answer==""||other.Answer!=q.Answer)QuestionReview.Warn(q,"Các nguồn đáp án trong tài liệu mâu thuẫn hoặc không xác định được. Hãy xác nhận đáp án đúng.");
                }
        }
        // Text-only documents may use a compact answer section; retain the established parser for those.
        if(tableKeys.Count==0&&result.Any(q=>q.Options.Count<2)&&result.All(q=>q.Media.Count==0))return Reader.Parse(string.Join("\n",doc.Descendants(W+"p").Select(Plain)));
        if(allowRepeatedNumbers&&repeated.Count>0)for(int i=0;i<result.Count;i++){
            var q=result[i];if(q.Number!=i+1)q.Text=$"[Câu gốc {q.Number}]\n{q.Text}";q.Number=i+1;
        }
        Importers.Validate(result);return result;
    }
}
