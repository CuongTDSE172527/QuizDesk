using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace QuizDesk;

public static partial class WordReader {
    static List<(string title,XDocument doc)> SplitSections(XDocument source,XDocument styles,string defaultTitle){
        var doc=new XDocument(source);
        var styleIndex=(styles?.Root?.Elements(W+"style")??Enumerable.Empty<XElement>())
            .Where(s=>s.Attribute(W+"styleId")!=null).GroupBy(s=>(string)s.Attribute(W+"styleId"))
            .ToDictionary(g=>g.Key,g=>g.First());
        IEnumerable<XElement> StyleChain(XElement p){
            string id=(string)p.Element(W+"pPr")?.Element(W+"pStyle")?.Attribute(W+"val");var seen=new HashSet<string>();
            while(id!=null&&seen.Add(id)&&styleIndex.TryGetValue(id,out var style)){
                InputSafety.Require(seen.Count<=64,"Chuỗi kế thừa kiểu Word quá sâu.");yield return style;
                id=(string)style.Element(W+"basedOn")?.Attribute(W+"val");
            }
        }
        bool Toc(XElement p){
            string id=(string)p.Element(W+"pPr")?.Element(W+"pStyle")?.Attribute(W+"val")??"";
            return SafeRegex.IsMatch(id,@"^TOC",RegexOptions.IgnoreCase)
                ||StyleChain(p).Any(s=>SafeRegex.IsMatch((string)s.Element(W+"name")?.Attribute(W+"val")??"",@"^toc(?:\s|$)",RegexOptions.IgnoreCase))
                ||p.Descendants(W+"hyperlink").Any(h=>((string)h.Attribute(W+"anchor")??"").StartsWith("_Toc",StringComparison.OrdinalIgnoreCase))
                ||p.Ancestors(W+"sdt").Any(s=>s.Element(W+"sdtPr")?.Descendants(W+"docPartGallery").Any(g=>((string)g.Attribute(W+"val")??"").Contains("Contents",StringComparison.OrdinalIgnoreCase))==true)
                ||p.Descendants(W+"fldSimple").Any(f=>SafeRegex.IsMatch((string)f.Attribute(W+"instr")??"",@"^\s*TOC\b",RegexOptions.IgnoreCase));
        }
        // Track nested complex fields so TOC results without TOC styles are skipped too.
        var fields=new Stack<bool>();
        foreach(var p in doc.Descendants(W+"p").ToList()){
            bool toc=Toc(p)||fields.Contains(true);
            foreach(var e in p.Descendants()){
                if(e.Name==W+"fldChar"){
                    string type=(string)e.Attribute(W+"fldCharType");
                    if(type=="begin")fields.Push(false);
                    else if(type=="end"&&fields.Count>0)fields.Pop();
                }else if(e.Name==W+"instrText"&&SafeRegex.IsMatch(e.Value,@"^\s*TOC\b",RegexOptions.IgnoreCase)){
                    if(fields.Count>0){fields.Pop();fields.Push(true);}toc=true;
                }
            }
            if(toc||SafeRegex.IsMatch(Plain(p),@"^(?:Contents|Mục lục)(?:\s*\(\d+\))?$",RegexOptions.IgnoreCase))p.Remove();
        }
        bool Heading(XElement p){
            string text=Plain(p);
            if(text.Length==0||text.Length>200||ReviewedAnswers.IsReferences(text)||SafeRegex.IsMatch(text,@"^(?:Câu\s*\d+|[A-H][.)]\s|Đáp án|Giải thích|Ghi chú)",RegexOptions.IgnoreCase))return false;
            var props=p.Element(W+"pPr");string id=(string)props?.Element(W+"pStyle")?.Attribute(W+"val");
            string outline=(string)(props?.Element(W+"outlineLvl")??StyleChain(p).Select(s=>s.Element(W+"pPr")?.Element(W+"outlineLvl")).FirstOrDefault(e=>e!=null))?.Attribute(W+"val");
            return id=="Heading1"||outline=="0"||StyleChain(p).Any(s=>string.Equals((string)s.Element(W+"name")?.Attribute(W+"val"),"heading 1",StringComparison.OrdinalIgnoreCase))
                ||SafeRegex.IsMatch(text,@"^(?:Môn|Phần|Chương)\s+.+",RegexOptions.IgnoreCase)&&!text.EndsWith("?");
        }
        var body=doc.Root?.Element(W+"body")??throw new InvalidDataException("File Word thiếu nội dung.");
        var elements=body.Elements().ToList();
        var headings=elements.Where(e=>e.Name==W+"p"&&Heading(e)).ToHashSet();
        if(headings.Count==0)return new(){(defaultTitle,doc)};
        var groups=new List<(string title,List<XElement> nodes)>();string title=defaultTitle;var nodes=new List<XElement>();bool started=false;
        foreach(var element in elements){
            if(headings.Contains(element)){
                if(started||nodes.Any(e=>e.DescendantsAndSelf(W+"p").Any(p=>SafeRegex.IsMatch(Plain(p),@"^(?:Câu\s+\d+|\d+[.)])",RegexOptions.IgnoreCase)||p.Element(W+"pPr")?.Element(W+"numPr")!=null)))groups.Add((title,nodes));
                title=Plain(element);nodes=new();started=true;
            }else nodes.Add(element);
        }
        if(nodes.Any(e=>!string.IsNullOrWhiteSpace(Plain(e))))groups.Add((title,nodes));
        if(groups.Count==0)return new(){(defaultTitle,doc)};
        return groups.Select(g=>(g.title,new XDocument(new XElement(W+"document",new XElement(W+"body",g.nodes.Select(e=>new XElement(e))))))).ToList();
    }
}
