using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace QuizDesk;
public static partial class WordReader {
    static List<Question> WarnWordSymbols(List<Question> questions){
        foreach(var question in questions)
            if(new[]{question.Text,question.Note}.Concat(question.Options.Values).Any(s=>s.Contains(UnknownWordSymbol,StringComparison.Ordinal)))
                QuestionReview.Warn(question,"Có ký hiệu Word/font chưa hỗ trợ được giữ bằng nhãn trong nội dung. Hãy đối chiếu file gốc trước khi học/thi.");
        return questions;
    }
    const string UnknownWordSymbol="⟦ký hiệu ";
    // Symbol character codes use the font's encoding (often offset by F000),
    // not Unicode. These common mathematical mappings require no installed font.
    // References: Microsoft OpenXML SymbolChar; unicode.org/Public/MAPPINGS/VENDORS/ADOBE/symbol.txt
    static readonly Dictionary<int,char> MathSymbolCodes=new(){
        [0x22]='∀',[0x24]='∃',[0x27]='∋',[0x2A]='∗',[0x2D]='−',[0x40]='≅',
        [0x5C]='∴',[0x5E]='⊥',[0x7E]='∼',[0xA0]='€',[0xA2]='′',[0xA3]='≤',
        [0xA4]='⁄',[0xA5]='∞',[0xAB]='↔',[0xAC]='←',[0xAD]='↑',[0xAE]='→',[0xAF]='↓',
        [0xB0]='°',[0xB1]='±',[0xB2]='″',[0xB3]='≥',[0xB4]='×',[0xB5]='∝',
        [0xB6]='∂',[0xB7]='•',[0xB8]='÷',[0xB9]='≠',[0xBA]='≡',[0xBB]='≈',[0xBC]='…',
        [0xC4]='⊗',[0xC5]='⊕',[0xC6]='∅',[0xC7]='∩',[0xC8]='∪',[0xC9]='⊃',
        [0xCA]='⊇',[0xCB]='⊄',[0xCC]='⊂',[0xCD]='⊆',[0xCE]='∈',[0xCF]='∉',
        [0xD0]='∠',[0xD1]='∇',[0xD5]='∏',[0xD6]='√',[0xD7]='⋅',[0xD8]='¬',
        [0xD9]='∧',[0xDA]='∨',[0xDB]='⇔',[0xDC]='⇐',[0xDD]='⇑',[0xDE]='⇒',[0xDF]='⇓',
        [0xE5]='∑',[0xF2]='∫'
    };
    static bool IsSymbolFont(string font)=>font!=null&&(font.Equals("Symbol",StringComparison.OrdinalIgnoreCase)||font.Equals("SymbolMT",StringComparison.OrdinalIgnoreCase)||font.Equals("Symbol MT",StringComparison.OrdinalIgnoreCase));
    static string MissingSymbol(string font,string code)=>UnknownWordSymbol+(font??"?")[..Math.Min(font?.Length??1,64)]+": "+(code??"?")[..Math.Min(code?.Length??1,16)]+"⟧";
    static string SymbolGlyph(int value,string font){
        int code=value is >=0xF000 and <=0xF0FF?value-0xF000:value;
        if(code is >=0x41 and <=0x5A)return "ΑΒΧΔΕΦΓΗΙϑΚΛΜΝΟΠΘΡΣΤΥςΩΞΨΖ"[code-0x41].ToString();
        if(code is >=0x61 and <=0x7A)return "αβχδεφγηιϕκλµνοπθρστυϖωξψζ"[code-0x61].ToString();
        if(MathSymbolCodes.TryGetValue(code,out char mapped))return mapped.ToString();
        if(code is >=0x20 and <=0x7E&&code!=0x60)return ((char)code).ToString();
        if(value>0xFF&&!(value is >=0xE000 and <=0xF8FF)&&!char.IsSurrogate((char)value)&&System.Xml.XmlConvert.IsXmlChar((char)value))return ((char)value).ToString();
        return MissingSymbol(font,value.ToString("X4",CultureInfo.InvariantCulture));
    }
    static string ReadWordSymbol(XElement symbol){
        string font=(string)symbol.Attribute(W+"font"),raw=(string)symbol.Attribute(W+"char");
        if(!ushort.TryParse(raw,NumberStyles.AllowHexSpecifier,CultureInfo.InvariantCulture,out ushort value))return MissingSymbol(font,raw);
        if(IsSymbolFont(font))return SymbolGlyph(value,font);
        // Unknown legacy symbol fonts cannot be decoded as arbitrary Unicode.
        bool unicodeFont=string.IsNullOrWhiteSpace(font)||new[]{"Cambria Math","Cambria","Calibri","Arial","Times New Roman","Segoe UI Symbol"}.Contains(font,StringComparer.OrdinalIgnoreCase);
        if(unicodeFont&&value>=0x20&&!(value is >=0xE000 and <=0xF8FF)&&!char.IsSurrogate((char)value)&&System.Xml.XmlConvert.IsXmlChar((char)value))return ((char)value).ToString();
        return MissingSymbol(font,raw);
    }
    static string ReadWordText(XElement text){
        var fonts=text.Ancestors(W+"r").FirstOrDefault()?.Element(W+"rPr")?.Element(W+"rFonts");
        string font=(string)fonts?.Attribute(W+"ascii")??(string)fonts?.Attribute(W+"hAnsi");
        if(!IsSymbolFont(font))return text.Value;
        var result=new StringBuilder();
        foreach(char character in text.Value)result.Append(character<=0xFF||character is >=(char)0xF000 and <=(char)0xF0FF?SymbolGlyph(character,font):character.ToString());
        return result.ToString();
    }
}
