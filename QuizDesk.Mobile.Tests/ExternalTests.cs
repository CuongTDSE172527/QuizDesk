using QuizDesk;
using System.Text;

partial class Tests {
    static async Task ExternalChecks(string root) {
        Directory.CreateDirectory(root);var inbox=new ExternalQuizInbox(root);
        string path=Path.Combine(root,"Đề chia sẻ có khoảng trắng.quiz");
        var questions=new List<Question>{new(){Number=1,Text="Câu không có đáp án",Options=new(){{"A","Một"},{"B","Hai"}}}};
        var setup=new QuizSetup{Count=1,Minutes=17,Included=new(){1}};QuizFile.Save(path,questions,setup);
        byte[] original=File.ReadAllBytes(path);IncomingQuiz item;
        using(var stream=new NonSeekableStream(original))item=await inbox.AddAsync(stream,"Đề nhận.Quiz");
        Check(item.Name=="Đề nhận.Quiz"&&inbox.List().Count==1,"Unicode/uppercase extension and nonseekable external stream accepted");
        File.Delete(path);Check(File.ReadAllBytes(inbox.ContentPath(item)).SequenceEqual(original),"Inbox owns copy independent of source file lifetime");
        using(var stream=new MemoryStream(original)){var again=await inbox.AddAsync(stream,"Different name.quiz");Check(again.Id==item.Id&&inbox.List().Count==1,"Duplicate OS delivery does not create second pending file");}
        Check(Directory.GetFiles(Path.GetDirectoryName(inbox.ContentPath(item)),"*.quiz").Length==1,"Duplicate delivery temporary copy is removed");
        var restored=new ExternalQuizInbox(root).List();Check(restored.Single().Id==item.Id&&restored[0].ReceivedAt!=default,"Pending file and receipt survive restart");
        var loaded=Importers.Load(inbox.ContentPath(item),out var config);Check(loaded[0].Answer==""&&config.Minutes==17,"Received quiz keeps unscored questions and configuration");
        var library=new MobileBankLibrary(Path.Combine(root,"Library"));var content=new WorkspaceBank{Title="Nhận",Bank=loaded,Setup=config};
        var saved=library.CreateIncoming(item.Id,content,item.Name);saved.Content.Bank[0].Text="Đã sửa tại máy nhận";library.Save(saved.Id,saved.Content);
        var retried=library.CreateIncoming(item.Id,content,item.Name);Check(retried.Content.Bank[0].Text=="Đã sửa tại máy nhận"&&library.List(out _).Count==1,"Retry after interruption cannot duplicate or overwrite imported bank");
        library.Delete(item.Id);Check(library.CreateIncoming(item.Id,content,item.Name).DeletedAt.HasValue,"Retry does not resurrect a bank deleted by user");
        inbox.Remove(item);Check(inbox.List().Count==0&&!File.Exists(inbox.ContentPath(item)),"Accept/reject removes both pending receipt and content");
        bool rejected=false;try{using var stream=new MemoryStream(original);await inbox.AddAsync(stream,"not-a-quiz.xml");}catch(InvalidDataException){rejected=true;}Check(rejected&&inbox.List().Count==0,"Non-quiz file cannot enter external inbox");
        rejected=false;try{using var stream=new RepeatingStream(MobileImportCache.Limit+1);await inbox.AddAsync(stream,"large.quiz");}catch(InvalidDataException){rejected=true;}
        Check(rejected&&Directory.GetFiles(Path.Combine(root,"IncomingQuiz"),"*.quiz").Length==0,"Oversize nonseekable input is rejected and partial copy removed");
        string dtd=Path.Combine(root,"entity.quiz");File.WriteAllText(dtd,"<!DOCTYPE quiz [<!ENTITY secret SYSTEM 'file:///should-not-be-read'>]><quiz version='1'>&secret;</quiz>");
        rejected=false;try{Importers.Load(dtd,out _);}catch(System.Xml.XmlException){rejected=true;}Check(rejected,"External quiz rejects DTD/entity expansion");
        var ids=new List<IncomingQuiz>();for(int i=0;i<10;i++){using var input=new MemoryStream(Encoding.UTF8.GetBytes("unique "+i));ids.Add(await inbox.AddAsync(input,i+".quiz"));}
        rejected=false;try{using var input=new MemoryStream(original);await inbox.AddAsync(input,"eleven.quiz");}catch(InvalidOperationException){rejected=true;}
        Check(rejected&&inbox.List().Count==10,"Pending queue is bounded at ten files");
        foreach(var queued in ids)inbox.Remove(queued);
        Check(QuizDesk.Android.IncomingQuizFileName.Resolve("Đề nhận.QUIZ","12345")=="Đề nhận.QUIZ","Provider display name takes precedence over opaque URI");
        Check(QuizDesk.Android.IncomingQuizFileName.Resolve(null,"primary:Download/Đề.quiz")=="Đề.quiz","Quiz extension recovered from URI when metadata unavailable");
        Check(QuizDesk.Android.IncomingQuizFileName.Resolve("   ","12345")=="Bo-de.quiz","Missing metadata uses a neutral filename for strict content validation");
        Check(QuizDesk.Android.IncomingQuizFileName.Resolve("download","opaque-token")=="Bo-de.quiz","Extensionless provider names do not block a valid quiz");
        rejected=false;try{QuizDesk.Android.IncomingQuizFileName.Resolve("document.pdf","fake.quiz");}catch(InvalidDataException){rejected=true;}
        Check(rejected,"Explicit non-quiz display name cannot be overridden by URI or MIME");
        Console.WriteLine("TOTAL "+checks+" external-file checks passed.");
    }
}
