using System.Text.RegularExpressions;

namespace QuizDesk;

// Source declarations are document data, not instructions. Only explicitly
// labeled review fields can supersede original keys; never infer a verdict.
public sealed class ReviewedAnswers {
    public List<string> Original {get;}=new();
    public List<string> Revised {get;}=new();
    public bool Read(string line) {
        var old=SafeRegex.Match(line,@"^Đáp\s*án\s*gốc\s*[:\-]\s*(.*)$",RegexOptions.IgnoreCase);
        if(old.Success){Original.Add(old.Groups[1].Value);return true;}
        var reviewed=SafeRegex.Match(line,@"^(?:Sau\s*rà\s*soát|Đáp\s*án\s*(?:sau\s*)?rà\s*soát)\s*[:\-]\s*(.*)$",RegexOptions.IgnoreCase);
        if(reviewed.Success){Revised.Add(reviewed.Groups[1].Value);return true;}
        return false;
    }
    public bool Apply(Question question) {
        if(Original.Count==0&&Revised.Count==0)return false;
        question.OriginalAnswer=string.Join("; ",Original);
        if(Revised.Count==0){QuestionReview.ApplyAnswerClaims(question,Original,"Dòng đáp án gốc");return true;}
        var claims=Revised.Select(value=>{
            // A dash introduces the review commentary, not a choice's text.
            var key=SafeRegex.Match(value,@"^([A-H])\s*[—–]\s*((?:giữ|sửa|đổi|chọn|phù hợp|đáp án|kết luận)\b.*)$",RegexOptions.IgnoreCase);
            return key.Success?key.Groups[1].Value.ToUpperInvariant():value;
        }).ToList();
        QuestionReview.ApplyAnswerClaims(question,claims,"Dòng sau rà soát");
        question.Note=string.Join("\n",new[]{question.Note,"Sau rà soát: "+string.Join("; ",Revised)}.Where(s=>!string.IsNullOrWhiteSpace(s)));
        // An intentionally unresolved review MUST NOT fall back to the old key.
        if(question.Answer=="")question.NeedsReview=true;
        return true;
    }
    public static bool IsReferences(string line)=>SafeRegex.IsMatch(line,@"^(?:Nguồn đối chiếu|Tài liệu tham khảo|Nguồn tham khảo)\s*:?\s*$",RegexOptions.IgnoreCase);
}
