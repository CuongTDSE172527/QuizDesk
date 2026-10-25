using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
namespace QuizDesk;
public class Question {
    public int Number;
    public string Text = "", Note = "", Answer = "";
    public string OriginalAnswer = "", AnswerSource = "";
    public bool NeedsReview;
    public List<string> ImportWarnings = new();
    public List<QuestionMedia> Media = new();
    public Dictionary<string,string> Options = new Dictionary<string,string>();
}
public class QuizSetup {
    public int Count=20, Minutes=20;
    public bool ShuffleQuestions=true, ShuffleOptions=false;
    public HashSet<int> Included=new HashSet<int>();
    public static List<Question> Build(List<Question> bank, QuizSetup setup, bool practice, Random rng) {
        var pool=bank.Where(q=>setup.Included.Contains(q.Number)).ToList();
        if(pool.Count==0) throw new Exception("Hãy chọn ít nhất một câu.");
        if(!practice && setup.Count>pool.Count) throw new Exception("Số câu thi lớn hơn số câu đã chọn.");
        if(setup.ShuffleQuestions) pool=pool.OrderBy(q=>rng.Next()).ToList();
        return pool.Take(practice?pool.Count:setup.Count).Select(q=>{
            var copy=new Question {Number=q.Number,Text=q.Text,Note=q.Note,OriginalAnswer=q.OriginalAnswer,AnswerSource=q.AnswerSource,NeedsReview=q.NeedsReview,ImportWarnings=new(q.ImportWarnings??new())};
            string scoringAnswer=QuestionReview.ScoringAnswer(q);
            copy.Media=q.Media.Where(m=>m.Target=="question"||m.Target=="note").Select(m=>m.Clone()).ToList();
            var options=q.Options.ToList();
            if(setup.ShuffleOptions) options=options.OrderBy(o=>rng.Next()).ToList();
            for(int i=0;i<options.Count;i++) {
                string key=setup.ShuffleOptions?((char)('A'+i)).ToString():options[i].Key;
                copy.Options[key]=options[i].Value;
                copy.Media.AddRange(q.Media.Where(m=>m.Target==options[i].Key).Select(m=>m.Clone(key)));
                if(options[i].Key==scoringAnswer) copy.Answer=key;
            }
            return copy;
        }).ToList();
    }
}
public static class QuizFile {
    public static void Save(string path,List<Question> bank,QuizSetup s) {
        if(bank.Any(q=>q.Media.Count>0)){MediaQuizFile.Save(path,bank,s);return;}
        using var output=File.Create(path);SaveXml(output,bank,s);
    }
    internal static void SaveXml(Stream output,List<Question> bank,QuizSetup s) {
        new XDocument(new XElement("quiz",new XAttribute("version",1),
            new XElement("settings",new XAttribute("count",s.Count),new XAttribute("minutes",s.Minutes),
                new XAttribute("shuffleQuestions",s.ShuffleQuestions),new XAttribute("shuffleOptions",s.ShuffleOptions)),
            bank.Select(q=>new XElement("question",new XAttribute("number",q.Number),new XAttribute("included",s.Included.Contains(q.Number)),
                new XElement("text",q.Text),new XElement("answer",q.Answer),new XElement("note",q.Note),
                new XElement("originalAnswer",q.OriginalAnswer),new XElement("answerSource",q.AnswerSource),new XElement("needsReview",q.NeedsReview),
                new XElement("importWarnings",(q.ImportWarnings??new()).Select(w=>new XElement("warning",w))),
                q.Options.Select(o=>new XElement("option",new XAttribute("key",o.Key),o.Value)))))).Save(output);
    }
    public static List<Question> Load(string path,out QuizSetup setup) {
        using(var signature=InputSafety.OpenFile(path)){if(signature.ReadByte()==80&&signature.ReadByte()==75)return MediaQuizFile.Load(path,out setup);}
        using var input=InputSafety.OpenFile(path);
        return LoadXml(InputSafety.ReadXml(input).Root,out setup);
    }
    internal static List<Question> LoadXml(XElement root,out QuizSetup setup) {
        if(root==null||root.Name!="quiz"||(int?)root.Attribute("version")!=1) throw new Exception("File QuizDesk không hợp lệ.");
        var s=root.Element("settings")??throw new InvalidDataException("Thiếu cấu hình bộ đề.");
        setup=new QuizSetup {Count=(int)s.Attribute("count"),Minutes=(int)s.Attribute("minutes"),
            ShuffleQuestions=(bool)s.Attribute("shuffleQuestions"),ShuffleOptions=(bool)s.Attribute("shuffleOptions")};
        var list=new List<Question>();
        foreach(var el in root.Elements("question")) {
            InputSafety.Require(list.Count<InputSafety.MaxQuestions,"Tối đa 5.000 câu trong một bộ đề.");
            var q=new Question {Number=(int)el.Attribute("number"),Text=(string)el.Element("text")??"",Answer=(string)el.Element("answer")??"",Note=(string)el.Element("note")??"",OriginalAnswer=(string)el.Element("originalAnswer")??"",AnswerSource=(string)el.Element("answerSource")??"",NeedsReview=(bool?)el.Element("needsReview")??false};
            q.ImportWarnings=el.Element("importWarnings")?.Elements("warning").Select(w=>w.Value).ToList()??new();
            InputSafety.CheckQuestion(q);
            foreach(var o in el.Elements("option")) QuestionReview.AddChoice(q,(string)o.Attribute("key"),o.Value);
            QuestionReview.ApplyAnswerClaims(q,el.Elements("answer").Select(a=>a.Value),q.AnswerSource);InputSafety.CheckQuestion(q);
            if(q.Number<1||string.IsNullOrWhiteSpace(q.Text)||q.Options.Count<2||q.Options.Count>8||q.Options.Any(o=>!SafeRegex.IsMatch(o.Key, "^[A-H]$")||string.IsNullOrWhiteSpace(o.Value))||(q.Answer!=""&&!q.Options.ContainsKey(q.Answer))) throw new Exception("Câu hỏi trong file không hợp lệ.");
            list.Add(q); if((bool?)el.Attribute("included")==true)setup.Included.Add(q.Number);
        }
        if(list.GroupBy(q=>q.Number).Any(g=>g.Count()>1)||setup.Count<1||setup.Count>Math.Max(1,list.Count)||setup.Minutes<1||setup.Minutes>300)throw new Exception("Cấu hình bộ đề không hợp lệ.");
        return list;
    }
}
public static class Reader {
    public static string Read(string path) {
        if (Path.GetExtension(path).ToLowerInvariant() == ".txt") return InputSafety.ReadText(path);
        using (var zip = InputSafety.OpenArchive(path)) {
            var entry = zip.GetEntry("word/document.xml");
            if (entry == null) throw new Exception("Không tìm thấy nội dung Word.");
            using (var stream = entry.Open()) {
                var doc = InputSafety.ReadXml(entry);
                XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
                return string.Join("\n", doc.Descendants(w+"p").Select(p => string.Concat(p.Descendants().Where(t=>t.Name==w+"t"||t.Name==w+"br"||t.Name==w+"tab").Select(t=>t.Name==w+"t"?t.Value:t.Name==w+"br"?"\n":"\t"))));
            }
        }
    }
    public static List<Question> Parse(string source) {
        InputSafety.Require(source!=null&&source.Length<=InputSafety.MaxTextCharacters);
        var result = new List<Question>();
        var keys = new Dictionary<int,List<string>>();
        var inline = new Dictionary<Question,List<string>>();
        var reviewed = new Dictionary<Question,ReviewedAnswers>();
        Question current = null;
        string option = null;
        bool keySection = false;
        foreach (string raw in source.Replace("\r","").Split('\n')) {
            string line = raw.Trim();
            if (line.Length == 0) continue;
            if(ReviewedAnswers.IsReferences(line))break;
            if(current!=null&&reviewed[current].Read(line)){option=null;continue;}
            if (SafeRegex.IsMatch(line, @"^(?:BẢNG\s+ĐÁP\s+ÁN\s*:?|ĐÁP\s+ÁN)\s*$", RegexOptions.IgnoreCase)) { keySection = true; continue; }
            if(option=="ANSWER"&&inline[current][^1].Trim()==""&&SafeRegex.IsMatch(line,@"^(?:Câu\s*)?\d+\s*[.:)-]?\s*[A-H]\b",RegexOptions.IgnoreCase)){
                inline[current].RemoveAt(inline[current].Count-1);option=null;keySection=true;
            }
            if (keySection) {
                foreach (Match k in SafeRegex.Matches(line, @"(?:Câu\s*)?(\d+)\s*[\.\:\-\)]?\s*([A-H](?:\s*[,;/&+]\s*[A-H])*)\b", RegexOptions.IgnoreCase))
                { int number=int.Parse(k.Groups[1].Value);if(!keys.ContainsKey(number))keys[number]=new();keys[number].Add(k.Groups[2].Value.ToUpperInvariant()); }
                continue;
            }
            if(current!=null&&QuestionReview.TryReadAnswerLine(line,out string answer)){inline[current].Add(answer);option=QuestionReview.HasResolvedClaim(current,answer)?"ANSWER_DONE":"ANSWER";continue;}
            Match note = SafeRegex.Match(line, @"^(?:Giải thích|Ghi chú)\s*:\s*(.*)$", RegexOptions.IgnoreCase);
            if (note.Success && current != null) { current.Note = note.Groups[1].Value; option = "NOTE"; continue; }
            Match q = SafeRegex.Match(line, @"^(?:Câu\s+(\d+)\s*[\.\:\)]?|(\d+)\s*[\.\)])\s+(.+)$", RegexOptions.IgnoreCase);
            if (q.Success) {
                current = new Question { Number = int.Parse(q.Groups[1].Success ? q.Groups[1].Value : q.Groups[2].Value), Text=q.Groups[3].Value };
                InputSafety.Require(result.Count<InputSafety.MaxQuestions);result.Add(current);inline[current]=new();reviewed[current]=new(); option=null; continue;
            }
            if (current == null) continue;
            if(option=="ANSWER_DONE")continue;
            if(option=="ANSWER"){inline[current][^1]+="\n"+line;if(QuestionReview.HasResolvedClaim(current,inline[current][^1]))option="ANSWER_DONE";continue;}
            Match o = SafeRegex.Match(line, @"^([A-H])[\.\)]\s*(.+)$", RegexOptions.IgnoreCase);
            if (o.Success) {
                option = o.Groups[1].Value.ToUpperInvariant();
                option=QuestionReview.AddChoice(current,option,o.Groups[2].Value);
            } else if (option == "NOTE") current.Note += "\n"+line;
            else if (option != null) current.Options[option] += "\n"+line;
            else current.Text += "\n"+line;
        }
        foreach (var q in result) {
            if(reviewed[q].Apply(q)){if(inline[q].Count>0||keys.ContainsKey(q.Number))QuestionReview.Warn(q,"Có thêm dòng/bảng đáp án ngoài đáp án gốc/rà soát. Hãy kiểm tra lại.");continue;}
            var claims=inline[q].Concat(keys.GetValueOrDefault(q.Number)??new()).ToList();
            QuestionReview.ApplyAnswerClaims(q,claims,keys.ContainsKey(q.Number)?"Dòng/bảng đáp án trong tài liệu":"Dòng đáp án trong câu");
        }
        if (result.GroupBy(q=>q.Number).Any(g=>g.Count()>1)) throw new Exception("Số câu bị trùng. Hãy nhập từng bộ đề riêng.");
        if (result.Count == 0 || result.Any(q=>q.Options.Count<2)) throw new Exception("Chưa đọc được đủ câu hỏi và lựa chọn. Xem file đề mẫu; mỗi lựa chọn A., B., C., D. cần ở một dòng riêng.");
        return result;
    }
}
