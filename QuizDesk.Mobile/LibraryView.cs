using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace QuizDesk.Mobile;

public partial class MainView {
    readonly MobileBankLibrary library;
    Guid? currentBankId,expandedBankId;
    bool selectingBanks;
    readonly HashSet<Guid> selectedBanks=new();
    readonly Dictionary<Guid,CheckBox> libraryChecks=new();
    Button deleteBanksButton,mergeBanksButton;
    List<LibraryBankSummary> libraryItems=new();
    TextBlock bankSelectionSummary;
    WorkspaceBank BankSnapshot()=>new() {LibraryBankId=currentBankId,Title=title,Bank=bank,Setup=setup,HasUnexportedChanges=dirty};
    void AdoptBank(LibraryBank saved) {
        if(saved.DeletedAt.HasValue)throw new InvalidOperationException("Bộ đề đã bị xóa.");
        currentBankId=saved.Id;bank=saved.Content.Bank;setup=saved.Content.Setup;title=saved.Content.Title;dirty=saved.Content.HasUnexportedChanges;
    }
    void AcceptStoredBank(LibraryBank saved){if(saved.DeletedAt.HasValue)ResetCurrentBank();else AdoptBank(saved);}
    void ResetCurrentBank(){currentBankId=null;expandedBankId=null;bank=new();setup=preferences.CreateSetup(bank);title="Bộ đề chưa đặt tên";dirty=false;questionList=null;}
    static string Stamp(DateTimeOffset value)=>value.ToLocalTime().ToString("dd/MM/yyyy · HH:mm:ss",System.Globalization.CultureInfo.InvariantCulture);
    void Bank() {
        questionList=null;selectionSummary=null;openedBankCount=null;openedBankDates=null;libraryChecks.Clear();
        Heading("Thư viện của bạn","Bộ đề đã lưu","Nhấn vào tên bộ đề để mở hoặc thu gọn phần biên soạn.");
        page.Children.Add(Row(Btn("＋ Nhập file",Import,true),Btn("Tạo bộ đề",NewBank),Btn("Lịch sử nhập",ShowImportHistory)));
        int waiting=Inbox.List().Count;if(waiting>0)page.Children.Add(Btn($"File nhận đang chờ ({waiting})",ReviewDeferredFiles));
        List<LibraryBankSummary> items;
        try{items=library.List(out string warning);if(warning!="")Notify(warning,true);}
        catch(Exception ex){Notify("Không đọc được thư viện: "+ex.Message,true);return;}
        page.Children.Add(Supporting($"{items.Count} bộ đề · Ctrl+A / ⌘A: chọn tất cả · Ngày giờ theo múi giờ thiết bị",12));
        libraryItems=items;selectedBanks.IntersectWith(items.Select(b=>b.Id));
        if(items.Count==0){page.Children.Add(Card(Stack(Text("Thư viện đang trống",22),Supporting("Nhập tài liệu hoặc tạo bộ đề mới. Các bộ đề được giữ riêng trên thiết bị."),Btn("Thử bộ mẫu",LoadSample))));return;}
        page.Children.Add(Btn(selectingBanks?"Xong":"Chọn bộ đề",()=>{if(!PersistBank())return;selectingBanks=!selectingBanks;expandedBankId=null;selectedBanks.Clear();Navigate("bank");}));
        if(selectingBanks) {
            bankSelectionSummary=Supporting("",13);page.Children.Add(bankSelectionSummary);
            mergeBanksButton=Btn("Trích câu / gộp",MergeSelectedBanks,true);mergeBanksButton.IsEnabled=false;
            deleteBanksButton=Btn("Xóa đã chọn",DeleteSelectedBanks);deleteBanksButton.IsEnabled=false;
            page.Children.Add(Row(Btn("Chọn tất cả bộ đề",()=>{foreach(var check in libraryChecks.Values)check.IsChecked=true;}),Btn("Bỏ chọn bộ đề",()=>{foreach(var check in libraryChecks.Values)check.IsChecked=false;}),mergeBanksButton,deleteBanksButton));
        }
        foreach(var item in items) {
            var heading=new Grid {ColumnDefinitions=new ColumnDefinitions("*,Auto"),ColumnSpacing=12};
            var label=Text(item.Title,20);heading.Children.Add(label);var chevron=Icon("arrow");chevron.RenderTransform=new RotateTransform(expandedBankId==item.Id?-90:90);Grid.SetColumn(chevron,1);heading.Children.Add(chevron);
            var open=Btn("",()=>{if(selectingBanks)libraryChecks[item.Id].IsChecked=libraryChecks[item.Id].IsChecked!=true;else OpenLibraryBank(item.Id);});open.Content=heading;open.Name="LibraryBank"+item.Id.ToString("N");open.Classes.Add("text");open.Padding=new Thickness(0,8);open.HorizontalAlignment=HorizontalAlignment.Stretch;open.HorizontalContentAlignment=HorizontalAlignment.Stretch;AutomationProperties.SetName(open,selectingBanks?"Chọn bộ đề "+item.Title:"Mở hoặc thu gọn bộ đề "+item.Title);
            var body=new StackPanel {Spacing=12};body.Children.Add(open);
            if(selectingBanks){var check=new CheckBox {Content="Chọn bộ đề này",IsChecked=selectedBanks.Contains(item.Id)};AutomationProperties.SetName(check,"Chọn bộ đề "+item.Title);check.IsCheckedChanged+=(_,_)=>{if(check.IsChecked==true)selectedBanks.Add(item.Id);else selectedBanks.Remove(item.Id);UpdateBankSelection();};libraryChecks[item.Id]=check;body.Children.Add(check);}
            var countText=Supporting($"{item.Count} câu hỏi"+(currentBankId==item.Id?" · Đang chọn để làm bài":""),12);body.Children.Add(countText);
            var datesText=Supporting((item.ImportedAt.HasValue?"Nhập: "+Stamp(item.ImportedAt.Value):"Tạo trong thư viện: "+Stamp(item.CreatedAt))+"\nSửa gần nhất: "+Stamp(item.UpdatedAt),12);body.Children.Add(datesText);
            if(item.Id==expandedBankId){openedBankCount=countText;openedBankDates=datesText;}
            if(!string.IsNullOrWhiteSpace(item.SourceName))body.Children.Add(Supporting("File nguồn: "+item.SourceName,12));
            var log=Btn("Nhật ký thay đổi",()=>ShowBankActivity(item.Id));log.Classes.Add("text");log.HorizontalAlignment=HorizontalAlignment.Left;log.Padding=new Thickness(0,8);body.Children.Add(log);
            if(expandedBankId==item.Id) {
                var divider=new Border {Height=1};Tone(divider,Border.BackgroundProperty,"OutlineVariant");body.Children.Add(divider);
                var editor=new StackPanel {Spacing=16};editor.Children.Add(Text("Biên soạn · "+item.Title,18));BankEditor(editor);body.Children.Add(editor);
                Dispatcher.UIThread.Post(()=>{var position=editor.TranslatePoint(new Point(0,0),page);if(position.HasValue)pageScroll.Offset=new Vector(0,Math.Max(0,position.Value.Y-12));},DispatcherPriority.Loaded);
            }
            var card=Card(body);card.Padding=new Thickness(16);page.Children.Add(card);
        }
        UpdateBankSelection();
    }
    void UpdateBankSelection(){
        if(!selectingBanks)return;var items=libraryItems.Where(b=>selectedBanks.Contains(b.Id)).ToList();long total=items.Sum(b=>(long)b.Count);
        bankSelectionSummary.Text=$"Đã chọn {items.Count} bộ đề · {total:N0} câu"+(total>5000?" · Chọn số câu trích để tổng không quá 5.000":items.Any(b=>b.Count==0)?" · Bộ đề trống cần được bỏ chọn để gộp":items.Count<2?" · Chọn ít nhất 2 bộ đề để gộp":"");
        mergeBanksButton.Content=$"Trích câu / gộp ({items.Count})";mergeBanksButton.IsEnabled=items.Count>=2&&items.All(b=>b.Count>0);
        deleteBanksButton.Content=$"Xóa đã chọn ({items.Count})";deleteBanksButton.IsEnabled=items.Count>0;
    }
    async void DeleteSelectedBanks() {
        if(active||busy||selectedBanks.Count==0)return;
        var ids=selectedBanks.ToArray();var items=library.List(out _).Where(b=>selectedBanks.Contains(b.Id)).ToList();
        string names=string.Join("\n",items.Take(5).Select(b=>"• "+b.Title))+(items.Count>5?$"\n… và {items.Count-5} bộ đề khác.":"");
        if(!await Ask($"Xóa {ids.Length} bộ đề đã chọn?",names+"\n\nNội dung và nhật ký biên soạn sẽ bị xóa. Giữ lịch sử làm bài và dấu mốc nhập/xóa. Không thể khôi phục thao tác này.","Xóa bộ đề"))return;
        int removed=0;var errors=new List<string>();busy=true;
        try {
            foreach(var id in ids)try{library.Delete(id);selectedBanks.Remove(id);removed++;if(currentBankId==id){ResetCurrentBank();RememberSelectedBank();}}catch(Exception ex){errors.Add(ex.Message);}
        }finally{busy=false;}
        expandedBankId=null;if(errors.Count==0){selectingBanks=false;selectedBanks.Clear();}Navigate("bank");
        Notify($"Đã xóa {removed} bộ đề."+(errors.Count>0?$" Chưa xóa được {errors.Count} bộ đề: {errors[0]}":" Lịch sử làm bài được giữ nguyên."),errors.Count>0);
    }
    void OpenLibraryBank(Guid id) {
        if(active||busy)return;
        notice.IsVisible=false;
        if(expandedBankId==id){expandedBankId=null;Navigate("bank");return;}
        if(!PersistBank())return;
        try{AdoptBank(library.Load(id));expandedBankId=id;RememberSelectedBank();Navigate("bank");}
        catch(Exception ex){Notify("Không mở được bộ đề: "+ex.Message,true);}
    }
    async void RenameBank() {
        if(!currentBankId.HasValue)return;
        var dialog=Dialog("Đổi tên bộ đề");var input=new TextBox {Text=title,MaxLength=200};var error=Supporting("");
        dialog.Content=Stack(Text("Đổi tên bộ đề",23),Field("Tên bộ đề",input),error,Row(Btn("Hủy",()=>dialog.Close(false)),Btn("Lưu tên",()=>{
            string next=input.Text?.Trim()??"";if(next==""){error.Text="Hãy nhập tên bộ đề.";return;}
            string previous=title;bool previousDirty=dirty;title=next;dirty=true;if(PersistBank())dialog.Close(true);else{title=previous;dirty=previousDirty;}
        },true)));
        if(await dialog.ShowDialog<bool>(this))Navigate("bank");
    }
    async void ShowBankActivity(Guid id) {
        try {
            var saved=library.Load(id);var dialog=Dialog("Nhật ký bộ đề");dialog.Height=730;
            var body=Stack(Text(saved.Content.Title,22),Supporting("Nhật ký nhập và thay đổi · Ngày giờ theo múi giờ thiết bị",12));
            foreach(var activity in saved.Activities.AsEnumerable().Reverse()) {
                string label=activity.Kind switch {"imported"=>"NHẬP FILE","created"=>"TẠO BỘ ĐỀ","migrated"=>"CHUYỂN DỮ LIỆU CŨ","merged"=>"GỘP BỘ ĐỀ",_=>"THAY ĐỔI"};
                var details=Stack(Chip(label),Text(Stamp(activity.At),16),Supporting(activity.Description));
                if(activity.Changes.Count>0) {
                    var changes=new ListBox {ItemsSource=activity.Changes,MaxHeight=380,ItemTemplate=new Avalonia.Controls.Templates.FuncDataTemplate<BankFieldChange>((change,_)=>change==null?new Border():Card(Stack(Text(change.Field,16),Supporting("Trước",12),Text(string.IsNullOrEmpty(change.Before)?"(Trống)":change.Before),Supporting("Sau",12),Text(string.IsNullOrEmpty(change.After)?"(Trống)":change.After))))};
                    details.Children.Add(new Expander {Header=$"Xem {activity.Changes.Count} thay đổi",HorizontalAlignment=HorizontalAlignment.Stretch,HorizontalContentAlignment=HorizontalAlignment.Stretch,Content=changes});
                }
                body.Children.Add(Card(details));
            }
            dialog.Content=LogLayout(body,()=>dialog.Close(false));await dialog.ShowDialog<bool>(this);
        }catch(Exception ex){Notify("Không đọc được nhật ký: "+ex.Message,true);}
    }
    async void ShowImportHistory() {
        try {
            var items=library.List(out string warning,true);if(warning!="")Notify(warning,true);
            var body=Stack(Text("Lịch sử nhập và xóa",23),Supporting("Mỗi lần nhập tạo một bộ đề riêng. Ngày giờ theo múi giờ thiết bị.",12));
            var imports=items.Where(b=>b.ImportedAt.HasValue||b.DeletedAt.HasValue).OrderByDescending(b=>b.DeletedAt??b.ImportedAt??b.CreatedAt).ToList();
            if(imports.Count==0)body.Children.Add(Supporting("Chưa có lần nhập được ghi nhận. Bản nháp chuyển từ phiên bản cũ không có thời điểm nhập ban đầu."));
            foreach(var item in imports)body.Children.Add(Card(Stack(Text(item.Title,20),Text((item.ImportedAt.HasValue?"Nhập: "+Stamp(item.ImportedAt.Value):"Tạo: "+Stamp(item.CreatedAt)),16),Supporting("File nguồn: "+(item.SourceName==""?"Tự soạn / dữ liệu cũ":item.SourceName),12),Supporting(item.DeletedAt.HasValue?"Đã xóa: "+Stamp(item.DeletedAt.Value):item.Count+" câu hỏi hiện có",12))));
            var dialog=Dialog("Lịch sử nhập");dialog.Height=730;dialog.Content=LogLayout(body,()=>dialog.Close(false));await dialog.ShowDialog<bool>(this);
        }catch(Exception ex){Notify("Không đọc được lịch sử nhập: "+ex.Message,true);}
    }
    static DockPanel LogLayout(Control body,Action close) {
        var layout=new DockPanel();var button=Btn("Đóng",close);button.HorizontalAlignment=HorizontalAlignment.Right;button.Margin=new Thickness(0,12,0,0);DockPanel.SetDock(button,Dock.Bottom);layout.Children.Add(button);layout.Children.Add(new ScrollViewer {Content=body,HorizontalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled});return layout;
    }
}
