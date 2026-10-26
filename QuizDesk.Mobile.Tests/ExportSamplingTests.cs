using QuizDesk;
using System.Security.Cryptography;
using System.Text;
partial class Tests {
    static void ExportSamplingChecks(string folder){
        Directory.CreateDirectory(folder);
        var png=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aX1cAAAAASUVORK5CYII=");
        Question Q(int i,string prefix)=>new(){Number=i,Text=prefix+i,Answer=i%2==0?"B":"A",Note="Explain "+prefix+i,Options=new(){{"A","One"},{"B","Two"}}};
        var source=new List<Question>{Q(7,"<script>alert(1)</script>\n\"Quoted\", "),Q(9,"Second ")};
        source[0].Media.Add(MediaFiles.FromBytes("map.png",png,"question"));source[0].OriginalAnswer="Original A";source[0].AnswerSource="Reviewed";source[1].Answer="";source[1].NeedsReview=true;source[1].ImportWarnings.Add("Confirm manually");
        string json=Path.Combine(folder,"export.json");File.WriteAllBytes(json,QuizExports.Write("json",source,"Title"));var restored=Importers.Load(json,out _);
        Check(restored.Count==2&&restored[0].Text==source[0].Text&&restored[0].Answer=="A"&&restored[0].Media[0].Data.SequenceEqual(png),"JSON export reimports text, answer and embedded media");
        Check(restored[0].OriginalAnswer=="Original A"&&restored[0].AnswerSource=="Reviewed"&&restored[1].NeedsReview&&restored[1].ImportWarnings.Contains("Confirm manually"),"JSON preserves review state and answer provenance");
        foreach(string format in new[]{"csv","tsv"}){
            string file=Path.Combine(folder,"export."+format);File.WriteAllBytes(file,QuizExports.Write(format,source,"Title"));var rows=Importers.Load(file,out _);
            Check(rows.Count==2&&rows[0].Text==source[0].Text&&rows[0].Answer=="A"&&rows[0].Note==source[0].Note,format+" roundtrip preserves multiline quoted text and answers");
        }
        var formula=Q(1,"=SUM(A1:A2)");formula.Options["A"]="@danger";formula.Note="  +formula";
        var csv=Encoding.UTF8.GetString(QuizExports.Write("csv",new[]{formula},"Title"));
        Check(csv.Contains("'=SUM")&&csv.Contains("'@danger")&&csv.Contains("'  +formula"),"Spreadsheet formula cells receive a safe apostrophe prefix");
        var html=Encoding.UTF8.GetString(QuizExports.Write("html",source,"<Title>"));
        Check(!html.Contains("<script>")&&html.Contains("&lt;script&gt;")&&html.Contains("&lt;Title&gt;")&&html.Contains("data:image/png;base64,"),"HTML escapes content and embeds media without external links");
        Check(html.Contains("Content-Security-Policy")&&!html.Contains("autoplay"),"HTML disables external resources and does not autoplay video");
        File.WriteAllBytes(json,QuizExports.Write("json",source,"Title",false));var hidden=Importers.Load(json,out _);
        Check(hidden.All(q=>q.Answer==""&&q.Note==""&&q.OriginalAnswer==""&&q.AnswerSource==""),"Question-only JSON does not disclose answers or explanations");
        var text=Encoding.UTF8.GetString(QuizExports.Write("txt",source,"Title"));Check(text.Contains("Câu 7.")&&text.Contains("Đáp án: A")&&text.Contains("Giải thích:"),"TXT includes question labels, answers and explanation");
        Check(!Encoding.UTF8.GetString(QuizExports.Write("txt",source,"Title",false)).Contains("Đáp án:"),"Question-only TXT omits answer section");
        bool failed=false;try{QuizExports.Write("exe",source,"Title");}catch(InvalidDataException){failed=true;}Check(failed,"Unknown export format rejected");
        var library=new MobileBankLibrary(Path.Combine(folder,"library"));
        WorkspaceBank Bank(string prefix,int n)=>new(){Title=prefix,Bank=Enumerable.Range(1,n).Select(i=>Q(i,prefix)).ToList(),Setup=new(){Included=new(){1}}};
        var a=library.Create(Bank("Alpha",30));var b=library.Create(Bank("Beta",25));var ids=new[]{a.Id,b.Id};
        string Hash(Guid id)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(folder,"library",id.ToString("N")+".json"))));
        string ah=Hash(a.Id),bh=Hash(b.Id);var quotas=new Dictionary<Guid,int>{{a.Id,7},{b.Id,4}};
        var sampled=library.Merge(ids,"Sampled",true,random:new Random(17),counts:quotas);
        Check(sampled.Content.Bank.Count==11&&sampled.Content.Bank.Count(q=>q.Text.StartsWith("Alpha"))==7&&sampled.Content.Bank.Count(q=>q.Text.StartsWith("Beta"))==4,"Sample obeys per-bank quotas exactly");
        Check(sampled.Content.Bank.Select(q=>q.Text).Distinct().Count()==11,"Sampling is without replacement within sources");
        Check(sampled.Content.Bank.Select(q=>q.Number).SequenceEqual(Enumerable.Range(1,11)),"Sample receives fresh consecutive numbers");
        Check(sampled.Content.Setup.Count==11&&sampled.Content.Setup.Included.Count==11,"Mock exam defaults to the full allocated sample");
        Check(sampled.Content.Bank.All(q=>q.Note=="Explain "+q.Text&&q.Answer==(int.Parse(new string(q.Text.Where(char.IsDigit).ToArray()))%2==0?"B":"A")),"Answers and notes stay associated with sampled questions");
        Check(Hash(a.Id)==ah&&Hash(b.Id)==bh,"Sampling leaves original persisted banks unchanged");
        Check(sampled.Activities.Single(e=>e.Kind=="merged").Changes.All(c=>c.Before.Contains("Câu gốc")),"Sample audit retains source question numbers");
        var again=library.Merge(ids,"Again",true,random:new Random(17),counts:quotas);Check(again.Content.Bank.Select(q=>q.Text).SequenceEqual(sampled.Content.Bank.Select(q=>q.Text)),"Fixed seed reproduces allocation and shuffle");
        var skip=library.Merge(ids,"Skip",true,counts:new Dictionary<Guid,int>{{a.Id,0},{b.Id,3}});Check(skip.Content.Bank.Count==3&&skip.Content.Bank.All(q=>q.Text.StartsWith("Beta")),"Zero quota skips a selected source");
        void Reject(Dictionary<Guid,int> counts,string label){int before=library.List(out _).Count;bool rejected=false;try{library.Merge(ids,"Bad",true,counts:counts);}catch(InvalidDataException){rejected=true;}Check(rejected&&library.List(out _).Count==before,label);}
        Reject(new(){{a.Id,31}},"Cannot take more questions than a source contains");Reject(new(){{a.Id,-1}},"Negative quota rejected");Reject(new(){{a.Id,0},{b.Id,0}},"Empty combined sample rejected");Reject(new(){{Guid.NewGuid(),1}},"Quota for an unselected source rejected");
        var large1=library.Create(Bank("Large1",2600));var large2=library.Create(Bank("Large2",2600));
        Check(library.Merge(new[]{large1.Id,large2.Id},"Small sample",true,counts:new Dictionary<Guid,int>{{large1.Id,5},{large2.Id,5}}).Content.Bank.Count==10,"Can sample from sources whose full combined size exceeds 5000");
        Console.WriteLine("TOTAL "+checks+" export and sampling checks passed.");
    }
}
