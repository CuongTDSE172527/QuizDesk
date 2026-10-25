using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
namespace QuizDesk;

public class AppPreferences {
    public int Version {get;set;}=1;
    public string Theme {get;set;}="light";
    public int QuestionCount {get;set;}=20;
    public int Minutes {get;set;}=20;
    public bool ShuffleQuestions {get;set;}=true;
    public bool ShuffleOptions {get;set;}=false;
    public bool ReducedMotion {get;set;}=false;
    public void Normalize() {
        if(Version!=1)throw new InvalidDataException("Phiên bản file cài đặt chưa được hỗ trợ.");
        if(Theme!="light"&&Theme!="dark"&&Theme!="system")Theme="light";
        QuestionCount=Math.Clamp(QuestionCount,1,5000);Minutes=Math.Clamp(Minutes,1,300);
    }
    public QuizSetup CreateSetup(List<Question> bank)=>new QuizSetup {
        Count=Math.Min(QuestionCount,Math.Max(1,bank.Count)),Minutes=Minutes,
        ShuffleQuestions=ShuffleQuestions,ShuffleOptions=ShuffleOptions,
        Included=new HashSet<int>(bank.Select(q=>q.Number))
    };
}

public class PreferencesStore {
    public string FilePath {get;}
    public PreferencesStore(string path=null) {
        // Keep the Windows 3.x path so existing preferences survive the UI migration.
        string root=OperatingSystem.IsMacOS()
            ?Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Library","Application Support")
            :Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        FilePath=path??Path.Combine(root,"QuizDesk","settings.json");
    }
    public AppPreferences Load(out string warning) {
        warning="";
        if(!File.Exists(FilePath))return new AppPreferences();
        try {
            var settings=JsonSerializer.Deserialize<AppPreferences>(InputSafety.ReadText(FilePath,1024*1024))??throw new InvalidDataException("File cài đặt trống.");
            settings.Normalize();return settings;
        }catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException||ex is JsonException||ex is InvalidDataException) {
            warning="Không đọc được cài đặt đã lưu. App đang dùng giá trị mặc định. "+ex.Message;return new AppPreferences();
        }
    }
    public void Save(AppPreferences settings) {
        settings.Normalize();string path=Path.GetFullPath(FilePath),folder=Path.GetDirectoryName(path);
        Directory.CreateDirectory(folder);string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try {
            File.WriteAllText(temp,JsonSerializer.Serialize(settings,new JsonSerializerOptions {WriteIndented=true}),new UTF8Encoding(false));
            if(File.Exists(path))File.Replace(temp,path,path+".bak",true);else File.Move(temp,path);
        }finally {if(File.Exists(temp))File.Delete(temp);}
    }
}
