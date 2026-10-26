using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace QuizDesk.Mobile;
public partial class MainView {
    async void ShowLicenses() {
        var dialog=Dialog("Giấy phép thư viện");dialog.Height=700;
        var body=Stack(Text("Thư viện mã nguồn mở",22));
        foreach(var uri in Avalonia.Platform.AssetLoader.GetAssets(new Uri("avares://QuizDesk.Mobile/Assets/Licenses/"),null)) {
            using var input=Avalonia.Platform.AssetLoader.Open(uri);using var reader=new StreamReader(input);body.Children.Add(Text(Path.GetFileName(uri.AbsolutePath),18));body.Children.Add(Text(reader.ReadToEnd(),12));
        }
        var layout=new DockPanel();var close=Btn("Đóng",()=>dialog.Close(false));DockPanel.SetDock(close,Dock.Bottom);layout.Children.Add(close);layout.Children.Add(new ScrollViewer {Content=body,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});dialog.Content=layout;await dialog.ShowDialog<bool>(this);
    }
    bool PersistBank() {
        if(restoring)return true;
        try {
            if(currentBankId.HasValue)library.Save(currentBankId.Value,BankSnapshot());
            else if(bank.Count>0||dirty||title!="Bộ đề chưa đặt tên") {var saved=library.Create(BankSnapshot());currentBankId=saved.Id;}
            RememberSelectedBank();return true;
        }catch(Exception ex){Notify("Chưa lưu được bộ đề: "+ex.Message,true);return false;}
    }
    void RememberSelectedBank() {
        try{workspace.SaveBank(title,bank,setup,dirty,currentBankId);}
        catch(Exception ex){Notify("Bộ đề đã lưu trong thư viện; chưa ghi được trạng thái đang mở: "+ex.Message,true);}
    }
    public void PersistSession(){PersistBank();if(active)Checkpoint();if(studying)SaveStudy();}
    public void ResumeSession(){UpdateHeaderDateTime();if(active&&!practice)Tick();IncomingChanged();}
    void BeginCheckpoint() {
        try{workspace.Begin(new ActiveMobileExam {LibraryBankId=examBankId,Id=activeAttemptId,Title=examTitle,StartedAt=startedAt,Deadline=new DateTimeOffset(deadline),Practice=practice,Minutes=examMinutes,Exam=exam});}
        catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException){Notify("Chưa lưu được bài đang làm: "+ex.Message,true);}
    }
    void Checkpoint() {
        try{workspace.Checkpoint(activeAttemptId,index,answers);}catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException){Notify("Chưa lưu được lựa chọn: "+ex.Message,true);}
    }
    void RestoreExam(ActiveMobileExam state) {
        restoring=true;
        try {
            examBankId=state.LibraryBankId;examTitle=state.Title;
            activeAttemptId=state.Id;exam=state.Exam;index=state.Index;practice=state.Practice;startedAt=state.StartedAt;deadline=state.Deadline.LocalDateTime;
            examMinutes=state.Minutes;answers.Clear();foreach(var pair in state.Answers)answers[pair.Key]=pair.Value;
            active=true;navigation.IsEnabled=false;CreateQuestionView();ShowQuestion();
            if(!practice){timer.Start();Tick();}Notify(active?"Đã khôi phục bài đang làm.":"Bài đã hết giờ khi app ở nền; kết quả đã được nộp.");
        }finally{restoring=false;}
    }
    async void HandleBack(object sender,RoutedEventArgs args) {
        if(busy){args.Handled=true;return;}
        if(currentDialog!=null){args.Handled=true;currentDialog.Close(false);return;}
        if(studying){args.Handled=true;ExitStudy();return;}
        if(active&&practice){args.Handled=true;ExitPractice();return;}
        if(active){args.Handled=true;if(await Ask("Nộp bài trước khi rời màn hình?","Bài đang làm được lưu trên thiết bị. Bạn có thể hủy để tiếp tục làm bài.","Nộp bài"))Finish();return;}
        if(currentPage=="bank"&&expandedBankId.HasValue){args.Handled=true;expandedBankId=null;Navigate("bank");}
        else if(currentPage!="home"){args.Handled=true;Navigate("home");}else PersistSession();
    }
    async Task<bool> ExportFile(string name,string mime,byte[] bytes) {
        if(OperatingSystem.IsIOS()&&MobilePlatform.ShareFile!=null)return await MobilePlatform.ShareFile(name,mime,bytes);
        if(!Storage.CanSave){if(MobilePlatform.ShareFile!=null)return await MobilePlatform.ShareFile(name,mime,bytes);throw new NotSupportedException("Trình chọn file trên thiết bị chưa hỗ trợ xuất file.");}
        var file=await Storage.SaveFilePickerAsync(new FilePickerSaveOptions {Title="Xuất file",SuggestedFileName=name,DefaultExtension=Path.GetExtension(name).TrimStart('.'),ShowOverwritePrompt=true,FileTypeChoices=new[]{new FilePickerFileType("QuizDesk") {Patterns=new[]{"*"+Path.GetExtension(name)},MimeTypes=new[]{mime}}}});
        if(file==null)return false;
        await using var output=await file.OpenWriteAsync();if(output.CanSeek)output.SetLength(0);await output.WriteAsync(bytes);await output.FlushAsync();return true;
    }
    sealed class OverlayDialog {
        readonly MainView owner;
        readonly TaskCompletionSource<bool> completion=new();
        public object Content {get;set;}
        public double Height {get;set;}=double.NaN;
        public string Title {get;}
        public OverlayDialog(MainView owner,string title){this.owner=owner;Title=title;}
        public void Close(bool result)=>completion.TrySetResult(result);
        public async Task<T> ShowDialog<T>(MainView _) {
            if(owner.currentDialog!=null)throw new InvalidOperationException("Đã có hộp thoại đang mở.");
            var body=new Border {Child=Content as Control,Padding=new Thickness(20),CornerRadius=new CornerRadius(16),BorderThickness=new Thickness(1),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
            Tone(body,Border.BackgroundProperty,"ContainerLow");Tone(body,Border.BorderBrushProperty,"OutlineVariant");
            void Resize(){body.Width=Math.Max(260,Math.Min(560,owner.Bounds.Width-24));body.MaxHeight=Math.Max(180,owner.Bounds.Height-24);if(!double.IsNaN(Height))body.Height=Math.Min(Height,body.MaxHeight);}
            void OnSize(object sender,SizeChangedEventArgs args)=>Resize();
            owner.SizeChanged+=OnSize;Resize();owner.currentDialog=this;owner.modal.Children.Add(body);owner.modal.IsVisible=true;owner.shell.IsEnabled=false;owner.Animate(body,120);
            try{return (T)(object)(await completion.Task);}
            finally {owner.SizeChanged-=OnSize;owner.modal.Children.Clear();owner.modal.IsVisible=false;owner.shell.IsEnabled=true;owner.currentDialog=null;owner.IncomingChanged();}
        }
    }
}
