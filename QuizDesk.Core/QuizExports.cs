using System.Net;
using System.Text;
using System.Text.Json;
namespace QuizDesk;
public sealed record QuizExportFormat(string Id,string Extension,string Mime,string Label,bool Media);
public static class QuizExports {
    public static IReadOnlyList<QuizExportFormat> Formats {get;}=new[]{
        new QuizExportFormat("quiz","quiz","application/vnd.quizdesk.quiz","QuizDesk (.quiz)",true),
        new QuizExportFormat("docx","docx","application/vnd.openxmlformats-officedocument.wordprocessingml.document","Word (.docx)",true),
        new QuizExportFormat("xlsx","xlsx","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","Excel (.xlsx)",false),
        new QuizExportFormat("json","json","application/json","JSON (.json)",true),
        new QuizExportFormat("csv","csv","text/csv","CSV — Excel (.csv)",false),
        new QuizExportFormat("tsv","tsv","text/tab-separated-values","TSV (.tsv)",false),
        new QuizExportFormat("txt","txt","text/plain","Văn bản (.txt)",false),
        new QuizExportFormat("html","html","text/html","Trang HTML — xem/in offline (.html)",true)
    };
    public static byte[] Write(string format,IReadOnlyList<Question> bank,string title,bool answers=true){
        if(bank.Count==0)throw new InvalidDataException("Bộ đề chưa có câu hỏi để xuất.");
        Importers.Validate(bank.ToList());
        if(format=="docx")return OfficeQuizExports.Document(bank,title,answers);
        if(format=="xlsx")return OfficeQuizExports.Spreadsheet(bank,title,answers);
        string Answer(Question q)=>answers?(q.IsEssay?q.Answer:QuestionReview.ScoringAnswer(q)):"";
        string Note(Question q)=>answers?q.Note:"";
        if(format=="json")return JsonSerializer.SerializeToUtf8Bytes(new {title,questions=bank.Select(q=>new {
            isEssay=q.IsEssay,number=q.Number,text=q.Text,options=q.Options,answer=Answer(q),note=Note(q),
            originalAnswer=answers?q.OriginalAnswer:"",answerSource=answers?q.AnswerSource:"",needsReview=q.NeedsReview,
            importWarnings=answers?q.ImportWarnings:new List<string>(),media=q.Media.Where(m=>answers||m.Target!="note")
        })},new JsonSerializerOptions {WriteIndented=true});
        if(format is "csv" or "tsv"){
            char separator=format=="csv"?',':'\t';
            string Cell(string text){
                text??="";
                // Quoting alone does not stop spreadsheet formula execution.
                if(text.TrimStart().Length>0&&"=+-@".Contains(text.TrimStart()[0])||text.StartsWith('\t')||text.StartsWith('\r'))text="'"+text;
                return "\""+text.Replace("\"","\"\"")+"\"";
            }
            var rows=new List<string>{string.Join(separator,new[]{"number","text","A","B","C","D","E","F","G","H","answer","note"}.Select(Cell))};
            foreach(var q in bank)rows.Add(string.Join(separator,new[]{q.Number.ToString(System.Globalization.CultureInfo.InvariantCulture),(q.IsEssay?"[Tự luận] ":"")+q.Text}.Concat(Enumerable.Range(0,8).Select(i=>q.Options.GetValueOrDefault(((char)('A'+i)).ToString(),""))).Concat(new[]{Answer(q),Note(q)}).Select(Cell)));
            return new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(string.Join("\r\n",rows)+"\r\n")).ToArray();
        }
        if(format=="txt"){
            var text=new StringBuilder();text.AppendLine(title).AppendLine();
            foreach(var q in bank){text.AppendLine($"Câu {q.Number}. {(q.IsEssay?"[Tự luận] ":"")}{q.Text}");foreach(var option in q.Options)text.AppendLine($"{option.Key}. {option.Value}");if(answers){text.AppendLine("Đáp án: "+(Answer(q)==""?"Chưa xác nhận":Answer(q)));if(q.Note!="")text.AppendLine("Giải thích: "+q.Note);}text.AppendLine();}
            return Encoding.UTF8.GetBytes(text.ToString());
        }
        if(format=="html"){
            string E(string text)=>WebUtility.HtmlEncode(text??"");
            string Lines(string text)=>E(text).Replace("\r\n","\n").Replace("\n","<br>");
            var html=new StringBuilder("<!doctype html><html lang=\"vi\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><meta http-equiv=\"Content-Security-Policy\" content=\"default-src 'none'; img-src data:; media-src data:; style-src 'unsafe-inline'\"><title>");
            html.Append(E(title)).Append("</title><style>body{font:17px/1.6 system-ui;max-width:900px;margin:32px auto;padding:0 20px;color:#202430}article{border:1px solid #ddd;border-radius:16px;padding:20px;margin:20px 0;break-inside:avoid}img,video{display:block;max-width:100%;max-height:500px;margin:12px 0}.answer{background:#f0f2ff;padding:12px} @media print{video{display:none}}</style></head><body><h1>").Append(E(title)).Append("</h1>");
            void Media(Question q,string target){foreach(var m in q.Media.Where(m=>m.Target==target)){
                var uri="data:"+m.Mime+";base64,"+Convert.ToBase64String(m.Data);
                if(m.Mime.StartsWith("image/",StringComparison.Ordinal))html.Append("<img alt=\"").Append(E(m.Name)).Append("\" src=\"").Append(uri).Append("\">");
                else html.Append("<video controls preload=\"none\" src=\"").Append(uri).Append("\"></video>");
            }}
            foreach(var q in bank){html.Append("<article><h2>Câu ").Append(q.Number).Append(". ").Append(Lines(q.Text)).Append("</h2>");Media(q,"question");foreach(var option in q.Options){html.Append("<p><b>").Append(E(option.Key)).Append(".</b> ").Append(Lines(option.Value)).Append("</p>");Media(q,option.Key);}if(answers){html.Append("<div class=\"answer\">Đáp án: ").Append(E(Answer(q)==""?"Chưa xác nhận":Answer(q))).Append("<br>").Append(Lines(q.Note));Media(q,"note");html.Append("</div>");}html.Append("</article>");}
            return Encoding.UTF8.GetBytes(html.Append("</body></html>").ToString());
        }
        throw new InvalidDataException("Định dạng xuất chưa được hỗ trợ.");
    }
}
