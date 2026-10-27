using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
namespace QuizDesk;

public static class Importers {
    // Read each subject independently so repeated source numbers cannot share answer keys.
    public static List<ImportedBank> LoadBanks(string path,string displayName=null){
        displayName??=Path.GetFileName(path);
        string ext=Path.GetExtension(path).ToLowerInvariant();
        if(ext==".doc")return LegacyOffice.Convert(path,p=>LoadBanks(p,displayName));
        if(ext==".docx"){
            var sections=WordReader.ReadSections(path);
            return sections.Select(s=>new ImportedBank(sections.Count>1?$"{displayName} — {s.Title}":displayName,
                new WorkspaceBank {Title=sections.Count>1?s.Title:Path.GetFileNameWithoutExtension(displayName),Bank=s.Questions,
                    Setup=new QuizSetup {Count=Math.Min(20,s.Questions.Count),Included=new(s.Questions.Select(q=>q.Number))}})).ToList();
        }
        var bank=Load(path,out var setup);
        return new(){new(displayName,new WorkspaceBank {Title=Path.GetFileNameWithoutExtension(displayName),Bank=bank,Setup=setup})};
    }
    public const string Filter="Tất cả định dạng đề|*.quiz;*.doc;*.docx;*.pdf;*.xls;*.xlsx;*.csv;*.tsv;*.json;*.txt;*.md;*.rtf;*.html;*.htm;*.odt|QuizDesk|*.quiz|Word|*.doc;*.docx|PDF có chữ|*.pdf|Excel|*.xls;*.xlsx|CSV / TSV|*.csv;*.tsv|JSON|*.json|Văn bản|*.txt;*.md;*.rtf;*.html;*.htm;*.odt";
    public static List<Question> Load(string path,out QuizSetup setup) {
        setup=null;
        using(var check=InputSafety.OpenFile(path)){}
        string ext=Path.GetExtension(path).ToLowerInvariant();
        if(ext==".quiz")return QuizFile.Load(path,out setup);
        if(ext==".doc") {
            QuizSetup convertedSetup=null;
            var converted=LegacyOffice.Convert(path,p=>Load(p,out convertedSetup));setup=convertedSetup;return converted;
        }
        List<Question> questions;
        if(ext==".docx")questions=WordReader.Read(path);
        else if(ext==".json") {
            using var doc=JsonDocument.Parse(InputSafety.ReadText(path));
            var array=doc.RootElement.ValueKind==JsonValueKind.Array?doc.RootElement:doc.RootElement.GetProperty("questions");
            questions=new List<Question>();
            foreach(var item in array.EnumerateArray()) {
                InputSafety.Require(questions.Count<InputSafety.MaxQuestions);
                var q=new Question {Number=questions.Count+1,Text=item.GetProperty("text").GetString()??""};
                if(item.TryGetProperty("number",out var number))q.Number=number.GetInt32();
                if(item.TryGetProperty("answer",out var answer))q.Answer=(answer.GetString()??"").Trim().ToUpperInvariant();
                if(item.TryGetProperty("note",out var note))q.Note=note.GetString()??"";
                if(item.TryGetProperty("media",out var media))q.Media=JsonSerializer.Deserialize<List<QuestionMedia>>(media.GetRawText(),new JsonSerializerOptions {PropertyNameCaseInsensitive=true})??throw new InvalidDataException("Media JSON không hợp lệ.");
                foreach(var o in item.GetProperty("options").EnumerateObject())QuestionReview.AddChoice(q,o.Name.ToUpperInvariant(),o.Value.GetString()??"");
                QuestionReview.ApplyAnswerClaims(q,item.EnumerateObject().Where(p=>p.Name=="answer").Select(p=>p.Value.GetString()),"Dữ liệu JSON");
                if(item.TryGetProperty("originalAnswer",out var original))q.OriginalAnswer=original.GetString()??"";
                if(item.TryGetProperty("answerSource",out var source))q.AnswerSource=source.GetString()??"";
                if(item.TryGetProperty("needsReview",out var review))q.NeedsReview|=review.GetBoolean();
                if(item.TryGetProperty("importWarnings",out var warnings))foreach(var warning in warnings.EnumerateArray())q.ImportWarnings.Add(warning.GetString()??"");
                questions.Add(q);
            }
        } else if(ext==".xlsx") questions=ReadSpreadsheet(path);
        else if(ext==".xls") questions=LegacyOffice.ReadXls(path);
        else if(ext==".csv"||ext==".tsv") {
            var text=InputSafety.ReadText(path);
            char delimiter=ext==".tsv"?'\t':(text.Split('\n')[0].Count(c=>c==';')>text.Split('\n')[0].Count(c=>c==',')?';':',');
            questions=FromRows(Csv(text,delimiter));
        } else {
            string text;
            switch(ext) {
                case ".docx": text=Reader.Read(path);break;
                case ".pdf":
                    using(var doc=PdfDocument.Open(path)) {
                        InputSafety.Require(doc.NumberOfPages<=1000);
                        var pages=new List<string>();long total=0;foreach(var page in doc.GetPages()){string content=ContentOrderTextExtractor.GetText(page);total+=content.Length;InputSafety.Require(total<=InputSafety.MaxTextCharacters);pages.Add(content);}
                        if(pages.Any(p=>string.IsNullOrWhiteSpace(p)))throw new Exception("Có trang PDF không đọc được chữ (có thể là ảnh scan). Hãy OCR toàn bộ file hoặc chuyển sang Word có chữ trước khi nhập.");
                        text=string.Join("\n",pages);
                    }
                    break;
                case ".rtf": {
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    using var rtf=File.OpenRead(path);
                    var html=RtfPipe.Rtf.ToHtml(rtf);
                    html=SafeRegex.Replace(html,@"</(?:p|div|li|tr|h[1-6])\s*>|<br\s*/?>","\n",RegexOptions.IgnoreCase);
                    text=WebUtility.HtmlDecode(SafeRegex.Replace(html,"<[^>]+>",""));
                    break;
                }
                case ".odt":
                    using(var zip=InputSafety.OpenArchive(path)) {
                        var entry=zip.GetEntry("content.xml")??throw new Exception("File ODT thiếu nội dung.");
                        var xml=InputSafety.ReadXml(entry);
                        XNamespace ns="urn:oasis:names:tc:opendocument:xmlns:text:1.0";
                        text=string.Join("\n",xml.Descendants().Where(e=>e.Name==ns+"p"||e.Name==ns+"h").Select(e=>e.Value));
                    }
                    break;
                case ".html": case ".htm":
                    text=InputSafety.ReadText(path);
                    text=SafeRegex.Replace(text,@"<(script|style)\b[^>]*>[\s\S]*?</\1>","",RegexOptions.IgnoreCase);
                    text=SafeRegex.Replace(text,@"</(?:p|div|li|tr|h[1-6])\s*>|<br\s*/?>","\n",RegexOptions.IgnoreCase);
                    text=WebUtility.HtmlDecode(SafeRegex.Replace(text,"<[^>]+>",""));
                    break;
                case ".txt": case ".md":
                    text=InputSafety.ReadText(path);
                    if(ext==".md") {
                        text=SafeRegex.Replace(text,@"(?m)^\s{0,3}#{1,6}\s+","");
                        text=text.Replace("**","").Replace("__","");
                        text=SafeRegex.Replace(text,@"(?m)^\s*[-*]\s+(?=[A-H][.)])","");
                    }
                    break;
                default:throw new Exception("Định dạng chưa hỗ trợ. Hãy dùng DOCX, PDF có chữ, XLSX, CSV/TSV, JSON, TXT, Markdown, RTF, HTML, ODT hoặc .quiz.");
            }
            // Common inline answer layouts: split choices separated by tabs or multiple spaces.
            text=SafeRegex.Replace(text,@"[ \t]{2,}(?=[A-H][.)]\s)","\n");
            questions=Reader.Parse(text);
        }
        Validate(questions);
        setup=new QuizSetup {Count=Math.Min(20,questions.Count),Included=new HashSet<int>(questions.Select(q=>q.Number))};
        return questions;
    }
    public static void Validate(List<Question> questions) {
        if(questions==null||questions.Count==0)throw new InvalidDataException("Không tìm thấy câu hỏi.");
        if(questions.Count>5000)throw new InvalidDataException("Tối đa 5.000 câu trong một bộ đề.");
        foreach(var q in questions)InputSafety.CheckQuestion(q);
        MediaFiles.CheckBank(questions);
        if(questions.GroupBy(q=>q.Number).Any(g=>g.Count()>1))throw new InvalidDataException("Số câu bị trùng.");
        foreach(var q in questions) {
            if(q.Number<1||string.IsNullOrWhiteSpace(q.Text)||q.Options.Count<2||q.Options.Count>8||q.Options.Any(o=>!SafeRegex.IsMatch(o.Key,"^[A-H]$")||string.IsNullOrWhiteSpace(o.Value)))
                throw new InvalidDataException("Câu "+q.Number+" cần nội dung và 2–8 lựa chọn A–H.");
            if(q.Answer!=""&&!q.Options.ContainsKey(q.Answer))throw new InvalidDataException("Đáp án câu "+q.Number+" không nằm trong các lựa chọn.");
        }
    }
    public static List<List<string>> Csv(string text,char delimiter) {
        var rows=new List<List<string>>();var row=new List<string>();var field=new StringBuilder();bool quoted=false;
        for(int i=0;i<text.Length;i++) {
            InputSafety.Require(rows.Count<InputSafety.MaxRows&&row.Count<InputSafety.MaxColumns);
            char c=text[i];
            if(c=='"') {if(quoted&&i+1<text.Length&&text[i+1]=='"'){field.Append('"');i++;}else quoted=!quoted;}
            else if(c==delimiter&&!quoted) {row.Add(field.ToString());field.Clear();}
            else if((c=='\r'||c=='\n')&&!quoted) {if(c=='\r'&&i+1<text.Length&&text[i+1]=='\n')i++;row.Add(field.ToString());field.Clear();rows.Add(row);row=new List<string>();}
            else field.Append(c);
        }
        if(quoted)throw new Exception("CSV có dấu ngoặc kép chưa đóng.");
        if(field.Length>0||row.Count>0){row.Add(field.ToString());rows.Add(row);}
        return rows;
    }
    public static List<Question> FromRows(List<List<string>> rows) {
        rows=rows.Where(r=>r.Any(c=>!string.IsNullOrWhiteSpace(c))).ToList();
        if(rows.Count<2)throw new Exception("Bảng cần dòng tiêu đề và ít nhất một câu.");
        InputSafety.Require(rows.Count<=InputSafety.MaxRows&&rows.All(r=>r.Count<=InputSafety.MaxColumns));
        var headers=rows[0].Select(s=>s.Trim().Trim('\uFEFF').ToLowerInvariant()).ToList();
        int Find(params string[] aliases)=>headers.FindIndex(h=>aliases.Contains(h));
        int textCol=Find("text","question","câu hỏi","nội dung"),answerCol=Find("answer","đáp án"),noteCol=Find("note","explanation","giải thích"),numberCol=Find("number","id","câu","stt");
        if(textCol<0)throw new Exception("Thiếu cột text (hoặc Câu hỏi). Xem mẫu bảng tính trong thư mục Samples.");
        string Cell(List<string> row,int col)=>col>=0&&col<row.Count?row[col].Trim():"";
        var result=new List<Question>();
        foreach(var row in rows.Skip(1)) {
            InputSafety.Require(result.Count<InputSafety.MaxQuestions);
            int n=result.Count+1;string number=Cell(row,numberCol);
            if(number!=""&&!int.TryParse(number,out n))throw new Exception("Số câu không hợp lệ ở dòng "+(result.Count+2));
            var q=new Question {Number=n,Text=Cell(row,textCol),Note=Cell(row,noteCol)};
            for(int col=0;col<headers.Count;col++)if(SafeRegex.IsMatch(headers[col],"^[a-h]$")){string value=Cell(row,col);if(value!="")QuestionReview.AddChoice(q,headers[col],value);}
            QuestionReview.ApplyAnswerClaims(q,headers.Select((h,i)=>(h,i)).Where(p=>p.h=="answer"||p.h=="đáp án").Select(p=>Cell(row,p.i)),"Bảng dữ liệu");
            result.Add(q);
        }
        return result;
    }
    static List<Question> ReadSpreadsheet(string path) {
        using var zip=InputSafety.OpenArchive(path);
        XDocument ReadXml(string name)=>InputSafety.ReadXml(zip.GetEntry(name)??throw new InvalidDataException("XLSX thiếu "+name));
        XNamespace ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var strings=zip.GetEntry("xl/sharedStrings.xml")==null?new List<string>():ReadXml("xl/sharedStrings.xml").Descendants(ns+"si").Select(si=>string.Concat(si.Descendants(ns+"t").Select(t=>t.Value))).ToList();
        var sheet=ReadXml("xl/workbook.xml").Descendants(ns+"sheet").FirstOrDefault()??throw new Exception("Workbook không có sheet.");
        XNamespace rel="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        var target=ReadXml("xl/_rels/workbook.xml.rels").Root.Elements().Single(e=>(string)e.Attribute("Id")== (string)sheet.Attribute(rel+"id")).Attribute("Target").Value;
        var sheetUri=new Uri(new Uri("https://local/xl/workbook.xml"),target);
        InputSafety.Require(sheetUri.Scheme=="https"&&sheetUri.Host=="local"&&sheetUri.Query==""&&sheetUri.Fragment=="","Liên kết sheet bên ngoài không được hỗ trợ.");
        string sheetPath=sheetUri.AbsolutePath.TrimStart('/');
        var rows=new List<List<string>>();
        foreach(var row in ReadXml(sheetPath).Descendants(ns+"row")) {
            InputSafety.Require(rows.Count<InputSafety.MaxRows);
            var values=new List<string>();
            foreach(var cell in row.Elements(ns+"c")) {
                string reference=(string)cell.Attribute("r")??"A1";int index=0;
                int letters=0;foreach(char c in reference.TakeWhile(char.IsLetter)){InputSafety.Require(c>='A'&&c<='Z'&&++letters<=3);index=checked(index*26+c-'A'+1);}
                if(index<1||index>InputSafety.MaxColumns)throw new Exception("Địa chỉ ô Excel không hợp lệ.");
                while(values.Count<index)values.Add("");
                string v=cell.Element(ns+"v")?.Value??"",type=(string)cell.Attribute("t");
                if(cell.Element(ns+"f")!=null)throw new Exception("Bảng có công thức. Hãy sao chép và dán dạng giá trị trước khi nhập.");
                if(type=="s"){InputSafety.Require(int.TryParse(v,out int shared)&&shared>=0&&shared<strings.Count,"Chỉ mục chuỗi Excel không hợp lệ.");v=strings[shared];}
                else if(type=="inlineStr")v=string.Concat(cell.Descendants(ns+"t").Select(t=>t.Value));
                values[index-1]=v;
            }
            rows.Add(values);
        }
        return FromRows(rows);
    }
}
