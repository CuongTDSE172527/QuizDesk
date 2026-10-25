namespace QuizDesk.Android;

// Kept independent of Android APIs so provider metadata cases can be tested
// without a device. No MIME string is treated as evidence of valid quiz content.
public static class IncomingQuizFileName {
    public static string Resolve(string displayName,string lastPathSegment) {
        string Leaf(string name)=>Path.GetFileName((name??"").Replace('\\','/')).Trim();
        if(!string.IsNullOrWhiteSpace(displayName)) {
            string name=Leaf(displayName);
            if(Path.GetExtension(name).Equals(".quiz",StringComparison.OrdinalIgnoreCase))return name;
            if(Path.GetExtension(name)!="")throw new InvalidDataException("Chỉ mở file .quiz theo cách này. Các định dạng khác dùng nút Nhập file.");
        }
        string fallback=Leaf(lastPathSegment);
        if(Path.GetExtension(fallback).Equals(".quiz",StringComparison.OrdinalIgnoreCase))return fallback;
        // When the provider exposes only an opaque identifier, the strict quiz
        // parser will determine whether the copied file is actually a quiz.
        return "Bo-de.quiz";
    }
}
