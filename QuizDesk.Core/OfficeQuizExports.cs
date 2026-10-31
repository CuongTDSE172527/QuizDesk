using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;
namespace QuizDesk;

// Minimal Office Open XML packages. No Office installation, COM, native converter or network access.
internal static class OfficeQuizExports {
    static readonly XNamespace R="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    static readonly XNamespace P="http://schemas.openxmlformats.org/package/2006/relationships";
    static readonly XNamespace C="http://schemas.openxmlformats.org/package/2006/content-types";
    static readonly XNamespace W="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    static readonly XNamespace S="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    static readonly XNamespace A="http://schemas.openxmlformats.org/drawingml/2006/main";
    static readonly XNamespace WP="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
    static readonly XNamespace PIC="http://schemas.openxmlformats.org/drawingml/2006/picture";
    static XAttribute Attr(string name,object value)=>new(W+name,value);
    static XElement Relationship(string id,string type,string target)=>new(P+"Relationship",new XAttribute("Id",id),new XAttribute("Type",R.NamespaceName+"/"+type),new XAttribute("Target",target));
    static XElement Override(string part,string type)=>new(C+"Override",new XAttribute("PartName",part),new XAttribute("ContentType",type));
    static XElement ContentTypes(params object[] parts)=>new(C+"Types",new XElement(C+"Default",new XAttribute("Extension","rels"),new XAttribute("ContentType","application/vnd.openxmlformats-package.relationships+xml")),new XElement(C+"Default",new XAttribute("Extension","xml"),new XAttribute("ContentType","application/xml")),parts);
    static void Xml(ZipArchive zip,string path,XElement root){
        using var output=zip.CreateEntry(path,CompressionLevel.Optimal).Open();
        using var writer=XmlWriter.Create(output,new XmlWriterSettings {Encoding=new UTF8Encoding(false),Indent=false});
        new XDocument(new XDeclaration("1.0","utf-8","yes"),root).Save(writer);
    }
    static void Metadata(ZipArchive zip,string title){
        XNamespace cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties",dc="http://purl.org/dc/elements/1.1/";
        Xml(zip,"docProps/core.xml",new XElement(cp+"coreProperties",new XAttribute(XNamespace.Xmlns+"dc",dc),new XElement(dc+"title",title),new XElement(dc+"creator","QuizDesk")));
    }
    static XElement PackageRelationships(string main,string type)=>new(P+"Relationships",Relationship("rId1",type,main),new XElement(P+"Relationship",new XAttribute("Id","rId2"),new XAttribute("Type","http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties"),new XAttribute("Target","docProps/core.xml")));
    const string CoreType="application/vnd.openxmlformats-package.core-properties+xml";
    public static byte[] Spreadsheet(IReadOnlyList<Question> bank,string title,bool answers){
        using var output=new MemoryStream();
        using(var zip=new ZipArchive(output,ZipArchiveMode.Create,true)){
            Xml(zip,"[Content_Types].xml",ContentTypes(Override("/xl/workbook.xml","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"),Override("/xl/worksheets/sheet1.xml","application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"),Override("/xl/styles.xml","application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"),Override("/docProps/core.xml",CoreType)));
            Xml(zip,"_rels/.rels",PackageRelationships("xl/workbook.xml","officeDocument"));Metadata(zip,title);
            Xml(zip,"xl/workbook.xml",new XElement(S+"workbook",new XAttribute(XNamespace.Xmlns+"r",R),new XElement(S+"sheets",new XElement(S+"sheet",new XAttribute("name","Cau hoi"),new XAttribute("sheetId",1),new XAttribute(R+"id","rId1"))),new XElement(S+"definedNames",new XElement(S+"definedName",new XAttribute("name","_xlnm.Print_Titles"),new XAttribute("localSheetId",0),"'Cau hoi'!$1:$1"))));
            Xml(zip,"xl/_rels/workbook.xml.rels",new XElement(P+"Relationships",Relationship("rId1","worksheet","worksheets/sheet1.xml"),Relationship("rId2","styles","styles.xml")));
            XElement Font(bool bold)=>new(S+"font",new XElement(S+"sz",new XAttribute("val",11)),new XElement(S+"name",new XAttribute("val","Calibri")),bold?new XElement(S+"b"):null);
            Xml(zip,"xl/styles.xml",new XElement(S+"styleSheet",new XElement(S+"fonts",new XAttribute("count",2),Font(false),Font(true)),new XElement(S+"fills",new XAttribute("count",2),new XElement(S+"fill",new XElement(S+"patternFill",new XAttribute("patternType","none"))),new XElement(S+"fill",new XElement(S+"patternFill",new XAttribute("patternType","gray125")))),new XElement(S+"borders",new XAttribute("count",1),new XElement(S+"border",new XElement(S+"left"),new XElement(S+"right"),new XElement(S+"top"),new XElement(S+"bottom"),new XElement(S+"diagonal"))),new XElement(S+"cellStyleXfs",new XAttribute("count",1),new XElement(S+"xf",new XAttribute("numFmtId",0),new XAttribute("fontId",0),new XAttribute("fillId",0),new XAttribute("borderId",0))),new XElement(S+"cellXfs",new XAttribute("count",2),Enumerable.Range(0,2).Select(i=>new XElement(S+"xf",new XAttribute("numFmtId",0),new XAttribute("fontId",i),new XAttribute("fillId",0),new XAttribute("borderId",0),new XAttribute("xfId",0),new XAttribute("applyAlignment",1),new XElement(S+"alignment",new XAttribute("wrapText",1),new XAttribute("vertical","top"))))),new XElement(S+"cellStyles",new XAttribute("count",1),new XElement(S+"cellStyle",new XAttribute("name","Normal"),new XAttribute("xfId",0),new XAttribute("builtinId",0)))));
            var data=new XElement(S+"sheetData");
            void Row(int index,IEnumerable<string> fields,bool header=false){
                var cells=fields.Select((text,column)=>{
                    if(text.Length>32767)throw new InvalidDataException($"XLSX: dòng {index}, cột {(char)('A'+column)} vượt giới hạn 32.767 ký tự/ô. Hãy dùng DOCX, JSON hoặc .quiz.");
                    return new XElement(S+"c",new XAttribute("r",$"{(char)('A'+column)}{index}"),new XAttribute("t","inlineStr"),new XAttribute("s",header?1:0),new XElement(S+"is",new XElement(S+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text)));
                });
                data.Add(new XElement(S+"row",new XAttribute("r",index),cells));
            }
            Row(1,new[]{"number","text","A","B","C","D","E","F","G","H","answer","note"},true);
            for(int i=0;i<bank.Count;i++){var q=bank[i];Row(i+2,new[]{q.Number.ToString(System.Globalization.CultureInfo.InvariantCulture),(q.IsEssay?"[Tự luận] ":"")+q.Text}.Concat(Enumerable.Range(0,8).Select(n=>q.Options.GetValueOrDefault(((char)('A'+n)).ToString(),""))).Concat(new[]{answers?(q.IsEssay?q.Answer:QuestionReview.ScoringAnswer(q)):"",answers?q.Note:""}));}
            Xml(zip,"xl/worksheets/sheet1.xml",new XElement(S+"worksheet",new XElement(S+"dimension",new XAttribute("ref",$"A1:L{bank.Count+1}")),new XElement(S+"sheetViews",new XElement(S+"sheetView",new XAttribute("workbookViewId",0),new XElement(S+"pane",new XAttribute("ySplit",1),new XAttribute("topLeftCell","A2"),new XAttribute("activePane","bottomLeft"),new XAttribute("state","frozen")))),new XElement(S+"sheetFormatPr",new XAttribute("defaultRowHeight",30)),new XElement(S+"cols",Enumerable.Range(1,12).Select(i=>new XElement(S+"col",new XAttribute("min",i),new XAttribute("max",i),new XAttribute("width",i==1?8:i==2||i==12?65:i==11?12:32),new XAttribute("customWidth",1)))),data,new XElement(S+"autoFilter",new XAttribute("ref",$"A1:L{bank.Count+1}")),new XElement(S+"pageMargins",new XAttribute("left",0.3),new XAttribute("right",0.3),new XAttribute("top",0.5),new XAttribute("bottom",0.5),new XAttribute("header",0.2),new XAttribute("footer",0.2)),new XElement(S+"pageSetup",new XAttribute("orientation","landscape"),new XAttribute("paperSize",9))));
        }
        return output.ToArray();
    }
    static XElement Paragraph(string text,bool bold=false,int size=22){
        var run=new XElement(W+"r",new XElement(W+"rPr",bold?new XElement(W+"b"):null,new XElement(W+"sz",Attr("val",size))));
        var lines=text.Replace("\r\n","\n").Replace('\r','\n').Split('\n');
        for(int i=0;i<lines.Length;i++){if(i>0)run.Add(new XElement(W+"br"));run.Add(new XElement(W+"t",new XAttribute(XNamespace.Xml+"space","preserve"),lines[i]));}
        return new XElement(W+"p",new XElement(W+"pPr",new XElement(W+"spacing",Attr("after",120))),run);
    }
    static (int width,int height) ImageSize(QuestionMedia media){
        var b=media.Data;
        if(media.Mime=="image/png"&&b.Length>=24)return (BinaryPrimitives.ReadInt32BigEndian(b.AsSpan(16,4)),BinaryPrimitives.ReadInt32BigEndian(b.AsSpan(20,4)));
        if(media.Mime=="image/gif"&&b.Length>=10)return (BinaryPrimitives.ReadUInt16LittleEndian(b.AsSpan(6,2)),BinaryPrimitives.ReadUInt16LittleEndian(b.AsSpan(8,2)));
        if(media.Mime=="image/jpeg")for(int i=2;i+9<b.Length;){
            if(b[i++]!=255)continue;byte marker=b[i++];if(marker==255){i--;continue;}if(marker==0||marker==216||marker==217||marker is >=208 and <=215)continue;
            if(i+2>b.Length)break;int length=BinaryPrimitives.ReadUInt16BigEndian(b.AsSpan(i,2));if(length<2||length>b.Length-i)break;
            if(marker is >=192 and <=195 or >=197 and <=199 or >=201 and <=203 or >=205 and <=207)return (BinaryPrimitives.ReadUInt16BigEndian(b.AsSpan(i+5,2)),BinaryPrimitives.ReadUInt16BigEndian(b.AsSpan(i+3,2)));i+=length;
        }
        return (576,384);
    }
    static XElement Picture(QuestionMedia media,string relation,int index){
        var (width,height)=ImageSize(media);if(width<1||height<1||width>8192||height>8192||(long)width*height>12_000_000)throw new InvalidDataException("Ảnh trong DOCX có kích thước không hợp lệ hoặc quá lớn.");
        double scale=Math.Min(1,Math.Min(5486400d/(width*9525d),5486400d/(height*9525d)));long cx=Math.Max(1,(long)(width*9525d*scale)),cy=Math.Max(1,(long)(height*9525d*scale));
        return new XElement(W+"p",new XElement(W+"r",new XElement(W+"drawing",new XElement(WP+"inline",new XAttribute("distT",0),new XAttribute("distB",0),new XAttribute("distL",0),new XAttribute("distR",0),new XElement(WP+"extent",new XAttribute("cx",cx),new XAttribute("cy",cy)),new XElement(WP+"docPr",new XAttribute("id",index),new XAttribute("name",media.Name)),new XElement(WP+"cNvGraphicFramePr",new XElement(A+"graphicFrameLocks",new XAttribute("noChangeAspect",1))),new XElement(A+"graphic",new XElement(A+"graphicData",new XAttribute("uri",PIC.NamespaceName),new XElement(PIC+"pic",new XElement(PIC+"nvPicPr",new XElement(PIC+"cNvPr",new XAttribute("id",index),new XAttribute("name",media.Name)),new XElement(PIC+"cNvPicPr")),new XElement(PIC+"blipFill",new XElement(A+"blip",new XAttribute(R+"embed",relation)),new XElement(A+"stretch",new XElement(A+"fillRect"))),new XElement(PIC+"spPr",new XElement(A+"xfrm",new XElement(A+"off",new XAttribute("x",0),new XAttribute("y",0)),new XElement(A+"ext",new XAttribute("cx",cx),new XAttribute("cy",cy))),new XElement(A+"prstGeom",new XAttribute("prst","rect"),new XElement(A+"avLst"))))))))));
    }
    public static byte[] Document(IReadOnlyList<Question> bank,string title,bool answers){
        using var output=new MemoryStream();
        using(var zip=new ZipArchive(output,ZipArchiveMode.Create,true)){
            var types=ContentTypes(Override("/word/document.xml","application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"),Override("/word/styles.xml","application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"),Override("/docProps/core.xml",CoreType));
            var relationships=new XElement(P+"Relationships",Relationship("rIdStyles","styles","styles.xml"));
            var body=new XElement(W+"body",Paragraph(title,true,32));int image=0;var extensions=new HashSet<string>();
            void Media(Question q,string target){foreach(var media in q.Media.Where(m=>m.Target==target)){
                if(media.Mime is not ("image/png" or "image/jpeg" or "image/gif")){body.Add(Paragraph("Đính kèm không nhúng: "+media.Name+" ("+media.Mime+"). Dùng .quiz hoặc HTML để giữ media này."));continue;}
                image++;string extension=MediaFiles.Extension(media.Mime).TrimStart('.'),path=$"media/image{image}.{extension}",id="rIdImage"+image;
                if(extensions.Add(extension))types.AddFirst(new XElement(C+"Default",new XAttribute("Extension",extension),new XAttribute("ContentType",media.Mime)));
                relationships.Add(Relationship(id,"image",path));using(var stream=zip.CreateEntry("word/"+path,CompressionLevel.Optimal).Open())stream.Write(media.Data);
                body.Add(Picture(media,id,image));
            }}
            void AddLines(string text,bool bold=false,int size=22){foreach(string line in text.Replace("\r\n","\n").Replace('\r','\n').Split('\n'))body.Add(Paragraph(line,bold,size));}
            foreach(var q in bank){AddLines($"Câu {q.Number}. {(q.IsEssay?"[Tự luận] ":"")}{q.Text}",true,26);Media(q,"question");foreach(var option in q.Options){AddLines(option.Key+". "+option.Value);Media(q,option.Key);}if(answers){body.Add(Paragraph("Đáp án: "+(q.IsEssay?q.Answer:QuestionReview.ScoringAnswer(q)==""?"Chưa xác nhận":QuestionReview.ScoringAnswer(q)),true));if(q.Note!="")AddLines("Giải thích: "+q.Note);Media(q,"note");}body.Add(Paragraph(""));}
            body.Add(new XElement(W+"sectPr",new XElement(W+"pgSz",Attr("w",11906),Attr("h",16838)),new XElement(W+"pgMar",Attr("top",1134),Attr("right",1134),Attr("bottom",1134),Attr("left",1134),Attr("header",567),Attr("footer",567),Attr("gutter",0))));
            Xml(zip,"word/document.xml",new XElement(W+"document",new XAttribute(XNamespace.Xmlns+"w",W),new XAttribute(XNamespace.Xmlns+"r",R),new XAttribute(XNamespace.Xmlns+"wp",WP),new XAttribute(XNamespace.Xmlns+"a",A),new XAttribute(XNamespace.Xmlns+"pic",PIC),body));
            Xml(zip,"word/styles.xml",new XElement(W+"styles",new XElement(W+"docDefaults",new XElement(W+"rPrDefault",new XElement(W+"rPr",new XElement(W+"rFonts",Attr("ascii","Calibri"),Attr("hAnsi","Calibri")),new XElement(W+"sz",Attr("val",22)))))));
            Xml(zip,"word/_rels/document.xml.rels",relationships);Xml(zip,"_rels/.rels",PackageRelationships("word/document.xml","officeDocument"));Xml(zip,"[Content_Types].xml",types);Metadata(zip,title);
        }
        return output.ToArray();
    }
}
