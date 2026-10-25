using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using b2xtranslator.StructuredStorage.Reader;
using b2xtranslator.DocFileFormat;
using b2xtranslator.OpenXmlLib.WordprocessingML;
using b2xtranslator.WordprocessingMLMapping;
namespace QuizDesk;
public static class LegacyOffice {
    public static void ConvertDoc(string source,string target) {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using(var input=InputSafety.OpenFile(source)){}
        using var storage=new StructuredStorageReader(source);
        var document=new WordDocument(storage);
        var type=Converter.DetectOutputType(document);
        using(var output=WordprocessingDocument.Create(target,type))Converter.Convert(document,output);
        InputSafety.NormalizeConvertedArchive(target);
    }
    public static T Convert<T>(string path,Func<string,T> reader) {
        string folder=Path.Combine(Path.GetTempPath(),"QuizDesk-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try {string target=Path.Combine(folder,"converted.docx");ConvertDoc(path,target);return reader(target);}
        catch(Exception ex) {throw new Exception("Không đọc được DOC Word 97–2003. File có thể bị hỏng, mã hóa, hoặc thuộc phiên bản cũ hơn Word 97. Hãy lưu lại thành DOCX.\n"+ex.Message,ex);}
        finally {try{Directory.Delete(folder,true);}catch{}}
    }
    public static List<Question> ReadXls(string path) {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var stream=File.OpenRead(path);using var reader=ExcelDataReader.ExcelReaderFactory.CreateBinaryReader(stream);
        var rows=new List<List<string>>();
        while(reader.Read()) {
            InputSafety.Require(rows.Count<InputSafety.MaxRows&&reader.FieldCount<=InputSafety.MaxColumns);
            var row=new List<string>();for(int i=0;i<reader.FieldCount;i++)row.Add(System.Convert.ToString(reader.GetValue(i),System.Globalization.CultureInfo.InvariantCulture)??"");rows.Add(row);
        }
        return Importers.FromRows(rows);
    }
}
