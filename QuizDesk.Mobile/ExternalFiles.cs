using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;

namespace QuizDesk.Mobile;

public partial class MainView {
    bool reviewingIncoming,incomingAgain;
    readonly HashSet<Guid> deferredIncoming=new();
    ExternalQuizInbox Inbox=>new(dataRoot);
    void IncomingChanged()=>Dispatcher.UIThread.Post(async()=>{
        bool activate=MobilePlatform.BringToFront;MobilePlatform.BringToFront=false;
        if(activate&&Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop&&desktop.MainWindow is Window window){if(window.WindowState==WindowState.Minimized)window.WindowState=WindowState.Normal;window.Activate();}
        try{await ReviewIncomingFiles();}catch(Exception ex){Notify("Không đọc được file đang chờ: "+ex.Message,true);}
    });
    public async Task ReceiveExternalQuiz(Stream input,string name) {
        try{await Inbox.AddAsync(input,name);IncomingChanged();}
        catch(Exception ex){Notify("Không nhận được file: "+ex.Message,true);}
    }
    async Task ReviewIncomingFiles() {
        if(reviewingIncoming){incomingAgain=true;return;}if(Host==null)return;
        if(MobilePlatform.IncomingError is string error){MobilePlatform.IncomingError=null;Notify(error,true);}
        if(active||studying||busy||currentDialog!=null){if(Inbox.List().Count>0)Notify("Đã nhận file .quiz. File được giữ chờ; bạn có thể tiếp tục bài đang làm.");return;}
        reviewingIncoming=true;
        try {
            foreach(var item in Inbox.List().Where(i=>!deferredIncoming.Contains(i.Id))) {
                List<Question> incoming;QuizSetup importedSetup;
                try{var parsed=await Task.Run(()=>{var list=Importers.Load(Inbox.ContentPath(item),out var config);Importers.Validate(list);return (list,config);});incoming=parsed.list;importedSetup=parsed.config;}
                catch(Exception ex){Inbox.Remove(item);Notify("Không mở được “"+item.Name+"”: "+ex.Message,true);continue;}
                // The file may have arrived while an exam/dialog was starting.
                if(active||studying||busy||currentDialog!=null)break;
                bool duplicate=library.List(out _).Any(b=>b.SourceName==item.Name);
                var dialog=Dialog("Nhận bộ đề");bool later=false;
                dialog.Content=new ScrollViewer {Name="IncomingPreviewScroll",HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Content=Stack(Text("Nhận bộ đề",24),Text(Path.GetFileNameWithoutExtension(item.Name),21),
                    Supporting($"{incoming.Count} câu hỏi · {incoming.Count(q=>q.Note!="")} câu có giải thích · {incoming.Count(QuestionReview.RequiresReview)} câu cần kiểm tra"),
                    Supporting($"Cấu hình: {importedSetup.Count} câu / {importedSetup.Minutes} phút.\nThêm một bộ riêng vào thư viện; các bộ hiện có và file nguồn được giữ nguyên."),
                    Supporting(duplicate?"Đã có bộ nhập từ file cùng tên. Nếu tiếp tục sẽ tạo thêm một bản riêng.":"Bạn có thể ôn tập và thi offline sau khi thêm."),
                    Row(Btn("Bỏ qua",()=>dialog.Close(false)),Btn("Để sau",()=>{later=true;dialog.Close(false);}),Btn("Thêm vào thư viện",()=>dialog.Close(true),true)))};
                bool accepted=await dialog.ShowDialog<bool>(this);
                if(later){deferredIncoming.Add(item.Id);continue;}
                if(!accepted){Inbox.Remove(item);continue;}
                if(!PersistBank()){deferredIncoming.Add(item.Id);continue;}
                try {
                    // Stable identity makes a retry after interrupted receipt cleanup
                    // idempotent. Never overwrite a matching existing local bank.
                    LibraryBank saved;
                    if(library.Contains(item.Id))saved=library.Load(item.Id);
                    else saved=library.CreateIncoming(item.Id,new(){Title=Path.GetFileNameWithoutExtension(item.Name),Bank=incoming,Setup=importedSetup},item.Name);
                    if(!saved.DeletedAt.HasValue){AdoptBank(saved);RememberSelectedBank();selectingBanks=false;selectedBanks.Clear();expandedBankId=null;Navigate("bank");}
                    Inbox.Remove(item);Notify("Đã thêm bộ đề vào thư viện. Mở bộ đề để ôn tập hoặc thi thử.");
                }catch(Exception ex){deferredIncoming.Add(item.Id);Notify("Chưa nhập được file; file vẫn được giữ chờ: "+ex.Message,true);}
            }
        }catch(Exception ex){Notify("Không xử lý được file chờ: "+ex.Message,true);}
        finally{reviewingIncoming=false;if(incomingAgain){incomingAgain=false;IncomingChanged();}}
    }
    void ReviewDeferredFiles(){deferredIncoming.Clear();IncomingChanged();}
}

public partial class App {
    async void ActivatedFromOutside(object sender,ActivatedEventArgs args) {
        if(args is not FileActivatedEventArgs files){CurrentView?.ResumeSession();return;}
        foreach(var file in files.Files.OfType<Avalonia.Platform.Storage.IStorageFile>()) {
            try{await using var input=await file.OpenReadAsync();await MobilePlatform.ReceiveQuiz(input,file.Name);}
            catch(Exception ex){MobilePlatform.IncomingError="Không mở được file: "+ex.Message;MobilePlatform.SignalIncoming();}
        }
    }
}
