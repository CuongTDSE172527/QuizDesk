using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace QuizDesk;

public static class QuestionReview {
    static string Normalize(string text)=>SafeRegex.Replace(text.Normalize(NormalizationForm.FormC).Trim(),@"\s+"," ");
    public static List<string> DuplicateChoices(Question q)=>q.Options
        .GroupBy(o=>Normalize(o.Value)+"\n"+string.Join(";",(q.Media??new()).Where(m=>m.Target==o.Key).Select(MediaFiles.Fingerprint)),StringComparer.OrdinalIgnoreCase)
        .Where(g=>g.Count()>1).Select(g=>string.Join(", ",g.Select(o=>o.Key))).ToList();
    public static List<string> Messages(Question q) {
        var messages=new List<string>(q.ImportWarnings??new());
        foreach(string keys in DuplicateChoices(q))messages.Add("Lựa chọn "+keys+" trùng nội dung. Hãy sửa các lựa chọn.");
        if(string.IsNullOrEmpty(q.Answer))messages.Add("Chưa có đáp án đúng. Nhấn Sửa để bổ sung.");
        if(q.NeedsReview&&messages.Count==0)messages.Add("Đáp án/nội dung từ tài liệu cần được kiểm tra lại.");
        return messages;
    }
    public static bool RequiresReview(Question q)=>Messages(q).Count>0;
    public static string ScoringAnswer(Question q)=>q.ImportWarnings?.Count>0||DuplicateChoices(q).Count>0?"":q.Answer;
    public static void Warn(Question q,string message) {
        q.ImportWarnings??=new();if(!q.ImportWarnings.Contains(message))q.ImportWarnings.Add(message);
    }
    // Preserve every source choice. Repeated labels receive a free label and an
    // explicit receipt so the editor can resolve the ambiguity without data loss.
    public static string AddChoice(Question q,string label,string value) {
        InputSafety.Require(label!=null&&label.Length<=8&&value!=null&&value.Length<=32768,"Lựa chọn không hợp lệ hoặc quá dài.");
        string key=label.Trim().ToUpperInvariant();
        if(q.Options.ContainsKey(key)) {
            string next="ABCDEFGH".Select(c=>c.ToString()).FirstOrDefault(k=>!q.Options.ContainsKey(k));
            if(next==null)throw new Exception("Câu "+q.Number+" có hơn 8 lựa chọn. Hãy chia câu hỏi trước khi nhập.");
            Warn(q,$"Trùng nhãn lựa chọn {key}; nội dung thứ hai được giữ ở {next}. Hãy kiểm tra lại các nhãn và chọn đáp án đúng.");
            key=next;
        }
        q.Options.Add(key,value);return key;
    }
    public static void ApplyAnswerClaims(Question q,IEnumerable<string> claims,string source) {
        var values=claims.Select(s=>(s??"").Trim()).Where(s=>s!="").ToList();
        InputSafety.Require(values.Count<=32&&values.All(v=>v.Length<=32768),"Có quá nhiều dòng đáp án hoặc nội dung đáp án quá dài.");
        if(values.Count==0){q.Answer="";return;}
        if(values.Count>1)Warn(q,"Có nhiều dòng/ô đáp án ("+string.Join("; ",values)+"). Hãy chọn lại một đáp án đúng.");
        string resolved=values.Count==1?ResolveAnswer(q,values[0]):"";
        if(resolved!="")q.Answer=resolved;
        else {
            q.Answer="";
            if(values.Count==1)Warn(q,"Đáp án trong tài liệu không xác định được một lựa chọn: "+values[0]+". Hãy bổ sung đáp án.");
        }
        q.AnswerSource=source;
        if(q.ImportWarnings?.Count>0&&q.OriginalAnswer=="")q.OriginalAnswer=string.Join("; ",values);
    }
    // Compare complete text only. Keep accents, numbers, units, negations and
    // mathematical operators; approximate similarity must never assign a key.
    static string AnswerText(string text){
        text=Normalize(text);var pairs=new[]{("\"","\""),("“","”"),("‘","’"),("«","»")};
        foreach(var pair in pairs)if(text.Length>=2&&text.StartsWith(pair.Item1)&&text.EndsWith(pair.Item2)){text=text[1..^1].Trim();break;}
        return text.TrimEnd('.', '!', '?').Trim();
    }
    public static bool TryReadAnswerLine(string line,out string value){
        var match=SafeRegex.Match(line,@"^(?:Đáp\s*án(?:\s*đúng)?|Câu\s*trả\s*lời(?:\s*đúng)?|(?:Correct\s+)?Answer)(?:\s+là\s*[:：]?\s*|\s*[:：—–-]\s*)(.*)$",RegexOptions.IgnoreCase|RegexOptions.Singleline);
        value=match.Success?match.Groups[1].Value.Trim():null;return match.Success;
    }
    internal static bool HasResolvedClaim(Question q,string claim)=>ResolveAnswer(q,claim)!="";
    static string ResolveAnswer(Question q,string claim) {
        var letter=SafeRegex.Match(claim,@"^([A-H])[.)：:]?$",RegexOptions.IgnoreCase);
        if(letter.Success) {
            string key=letter.Groups[1].Value.ToUpperInvariant();
            return q.Options.ContainsKey(key)?key:"";
        }
        var labeled=SafeRegex.Match(claim,@"^([A-H])(?:[.)：:]|\s+[—–-])\s*(.+)$",RegexOptions.IgnoreCase|RegexOptions.Singleline);
        string text=labeled.Success?labeled.Groups[2].Value:claim;
        var matches=q.Options.Where(o=>string.Equals(AnswerText(o.Value),AnswerText(text),StringComparison.OrdinalIgnoreCase)).Select(o=>o.Key).ToList();
        if(matches.Count!=1)return "";
        if(labeled.Success&&matches[0]!=labeled.Groups[1].Value.ToUpperInvariant())return "";
        return matches[0];
    }
}
