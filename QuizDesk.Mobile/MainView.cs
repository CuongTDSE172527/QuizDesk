using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
namespace QuizDesk.Mobile;
public partial class MainView : UserControl {
    readonly Grid shell=new();
    readonly StackPanel page=new() {Spacing=20,Margin=new Thickness(20,16,20,28)};
    readonly StackPanel navigation=new() {Spacing=8,Margin=new Thickness(16,24)};
    readonly Dictionary<string,Button> navButtons=new();
    readonly Border notice=new() {IsVisible=false,CornerRadius=new CornerRadius(8),Padding=new Thickness(16),Margin=new Thickness(16,12,16,0)};
    readonly TextBlock noticeText=new() {TextWrapping=TextWrapping.Wrap};
    readonly ScrollViewer pageScroll;
    readonly Grid appBar,contentContainer,navigationGrid;
    bool? wideLayout;
    readonly TextBlock clock=new() {FontSize=22,FontWeight=FontWeight.SemiBold};
    readonly DispatcherTimer timer=new() {Interval=TimeSpan.FromSeconds(1)};
    readonly PreferencesStore preferencesStore;
    readonly HistoryStore historyStore;
    readonly List<ExamAttempt> unsavedAttempts=new();
    DateTimeOffset startedAt;
    readonly System.Diagnostics.Stopwatch attemptWatch=new();
    AppPreferences preferences;
    List<Question> bank=new(),exam=new();
    QuizSetup setup=new();
    readonly Dictionary<int,string> answers=new();
    bool dirty,active,practice,busy;
    string title="Bộ đề chưa đặt tên";
    int index,examMinutes;
    string examTitle="";
    Guid? examBankId;
    DateTime deadline;
    ListBox questionList;
    bool onlyReviewQuestions;
    TextBlock selectionSummary,preferencesStatus,questionHeading,questionSummary,questionText,feedbackText;
    ProgressBar questionProgress;
    Border questionFeedback;
    Button previousQuestion,nextQuestion;
    readonly Dictionary<string,ToggleButton> answerButtons=new();
    readonly Dictionary<string,TextBlock> answerLabels=new();
    readonly Dictionary<string,TextBlock> answerMarkers=new();
    readonly List<ToggleButton> jumpButtons=new();
    static readonly string[] Extensions={"quiz","doc","docx","pdf","xls","xlsx","csv","tsv","json","txt","md","rtf","html","htm","odt"};

    readonly MobileWorkspace workspace;
    readonly string dataRoot;
    readonly Grid outer=new();
    readonly Grid modal=new() {IsVisible=false,Background=new SolidColorBrush(Color.Parse("#52000000"))};
    OverlayDialog currentDialog;
    Guid activeAttemptId;
    ActiveMobileExam restoredExam;
    string currentPage="home";
    bool restoring;
    TopLevel Host=>TopLevel.GetTopLevel(this);
    IStorageProvider Storage=>Host?.StorageProvider??throw new InvalidOperationException("Chưa sẵn sàng chọn file.");
    public MainView(string folder=null) {
        dataRoot=folder??MobilePlatform.DataDirectory??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QuizDesk");
        studyStore=new FlashcardStore(Path.Combine(dataRoot,"Study"));restoredStudy=studyStore.Load(out string studyWarning);
        AddHandler(KeyDownEvent,LibrarySelectKey,Avalonia.Interactivity.RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent,StudyKey,Avalonia.Interactivity.RoutingStrategies.Tunnel);
        preferencesStore=new PreferencesStore(Path.Combine(dataRoot,"settings.json"));preferences=preferencesStore.Load(out string settingsWarning);
        historyStore=new HistoryStore(Path.Combine(dataRoot,"History"));workspace=new MobileWorkspace(dataRoot);library=new MobileBankLibrary(Path.Combine(dataRoot,"Library"));
        var saved=workspace.LoadBank(out string draftWarning);
        if(saved!=null){bank=saved.Bank;setup=saved.Setup;title=saved.Title;dirty=saved.HasUnexportedChanges;}else setup=preferences.CreateSetup(bank);
        string libraryWarning="";
        try {
            if(saved?.LibraryBankId is Guid savedId){AcceptStoredBank(library.Load(savedId));RememberSelectedBank();}
            else if(saved!=null&&(saved.Bank.Count>0||saved.HasUnexportedChanges||saved.Title!="Bộ đề chưa đặt tên")) {AcceptStoredBank(library.MigrateLegacy(saved));RememberSelectedBank();}
        }catch(Exception ex){libraryWarning="Không tải được bộ đề thư viện. Giữ bản nháp khôi phục: "+ex.Message;}
        restoredExam=workspace.LoadActive(out string examWarning);
        if(restoredExam!=null&&historyStore.Load(out _).Any(a=>a.Id==restoredExam.Id)){try{workspace.Complete(restoredExam.Id);}catch(IOException){}restoredExam=null;}
        ApplyTheme();Tone(this,BackgroundProperty,"Surface");Tone(this,ForegroundProperty,"OnSurface");
        shell.RowDefinitions=new RowDefinitions("Auto,*,Auto");
        var headerSurface=appBar=new Grid {MinHeight=72};
        var header=new Grid {ColumnDefinitions=new ColumnDefinitions("*,Auto,Auto,Auto"),Margin=new Thickness(20,4,12,4),MinHeight=64};
        headerSurface.Children.Add(header);
        var brand=brandName=Text("QuizDesk",22);brand.FontWeight=FontWeight.SemiBold;
        var mark=new Image {Source=BrandLogo.Value,Width=36,Height=36};
        var identity=new StackPanel {Orientation=Orientation.Horizontal,Spacing=10,VerticalAlignment=VerticalAlignment.Center,Children={mark,brand}};header.Children.Add(identity);
        Grid.SetColumn(headerDateTime,1);ToolTip.SetTip(headerDateTime,"Ngày giờ thiết bị · định dạng 24 giờ");header.Children.Add(headerDateTime);UpdateHeaderDateTime();
        var help=IconButton("Trợ giúp","help",()=>Navigate("help"));Grid.SetColumn(help,2);header.Children.Add(help);navButtons["help"]=help;
        var settings=IconButton("Cài đặt","preferences",()=>Navigate("preferences"));Grid.SetColumn(settings,3);header.Children.Add(settings);navButtons["preferences"]=settings;
        shell.Children.Add(headerSurface);
        var container=contentContainer=new Grid {RowDefinitions=new RowDefinitions("Auto,*")};notice.Child=noticeText;container.Children.Add(notice);
        pageScroll=new ScrollViewer {Content=page,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};Grid.SetRow(pageScroll,1);container.Children.Add(pageScroll);Grid.SetRow(container,1);shell.Children.Add(container);
        var bottom=navigationGrid=new Grid {ColumnDefinitions=new ColumnDefinitions("*,*,*,*"),ColumnSpacing=0};
        var navs=new[]{("home","Trang chủ"),("bank","Bộ đề"),("setup","Học & thi"),("history","Lịch sử")};
        for(int i=0;i<navs.Length;i++){var (key,label)=navs[i];var button=NavButton(key,label);Grid.SetColumn(button,i);bottom.Children.Add(button);}
        Tone(navigation,Panel.BackgroundProperty,"Container");navigation.Children.Add(bottom);Grid.SetRow(navigation,2);navigation.Margin=default;shell.Children.Add(navigation);
        outer.Children.Add(glassBackdrop);outer.Children.Add(shell);outer.Children.Add(modal);Content=outer;TopLevel.SetAutoSafeAreaPadding(this,true);
        page.MaxWidth=1040;SizeChanged+=(_,_)=>AdaptLayout();
        timer.Tick+=(_,_)=>Tick();
        headerDateTimeTimer.Tick+=(_,_)=>UpdateHeaderDateTime();
        AttachedToVisualTree+=(_,_)=>{
            UpdateHeaderDateTime();headerDateTimeTimer.Start();AttachAero();
            MobilePlatform.IncomingQuiz+=IncomingChanged;IncomingChanged();
            if(Host!=null)Host.BackRequested+=HandleBack;
            if(restoredExam!=null){var state=restoredExam;restoredExam=null;RestoreExam(state);}else if(restoredStudy?.InProgress==true){var state=restoredStudy;restoredStudy=null;OpenStudy(state);}else if(active&&!practice)timer.Start();
#if QA
            if(!qaStarted){qaStarted=true;Dispatcher.UIThread.Post(async()=>await VerifyMobile());}
#endif
        };
        DetachedFromVisualTree+=(_,_)=>{DetachAero();headerDateTimeTimer.Stop();MobilePlatform.IncomingQuiz-=IncomingChanged;PersistSession();timer.Stop();if(Host!=null)Host.BackRequested-=HandleBack;};
        Navigate("home");string warning=string.Join(" ",new[]{settingsWarning,draftWarning,examWarning,libraryWarning,studyWarning}.Where(s=>s!=""));if(warning!="")Notify(warning,true);
    }
    static TextBlock Text(string value,double size=14)=>new() {Text=value,FontSize=size,TextWrapping=TextWrapping.Wrap,LineHeight=size*1.4};
    static StackPanel Stack(params Control[] controls) {var s=new StackPanel {Spacing=12};foreach(var c in controls)s.Children.Add(c);return s;}
    static WrapPanel Row(params Control[] controls) {var s=new WrapPanel {Orientation=Orientation.Horizontal};foreach(var c in controls){c.Margin=new Thickness(0,0,10,8);s.Children.Add(c);}return s;}
    static Control Field(string label,Control input)=>new StackPanel {Spacing=8,Children={Supporting(label,12),input}};
    Border Card(Control child) {
        var card=new Border {Child=child,Padding=new Thickness(20),CornerRadius=new CornerRadius(14),BorderThickness=new Thickness(1)};
        Tone(card,Border.BackgroundProperty,"ContainerLow");Tone(card,Border.BorderBrushProperty,"OutlineVariant");return card;
    }
    Border ReviewCard(Control child) {
        var card=Card(child);Tone(card,Border.BackgroundProperty,"WarningContainer");card.BorderThickness=new Thickness(1);Tone(card,Border.BorderBrushProperty,"WarningOutline");return card;
    }
    static Button Btn(string label,Action action,bool primary=false) {
        var b=new Button {Content=label,HorizontalContentAlignment=HorizontalAlignment.Center};b.Classes.Add("qd");if(primary)b.Classes.Add("accent");if(label.StartsWith("Xóa"))b.Classes.Add("danger");b.Click+=(_,_)=>action();return b;
    }
    void Heading(string eyebrow,string heading,string description) {var caption=Text(eyebrow,12);caption.Classes.Add("primary");caption.FontWeight=FontWeight.SemiBold;page.Children.Add(new StackPanel {Spacing=8,Children={caption,new TextBlock {Text=heading,FontSize=28,FontWeight=FontWeight.SemiBold,TextWrapping=TextWrapping.Wrap,LineHeight=36},Supporting(description)}});}
    void Notify(string message,bool error=false) {noticeText.Text=message;Tone(notice,Border.BackgroundProperty,error?"ErrorContainer":"SecondaryContainer");Tone(noticeText,TextBlock.ForegroundProperty,error?"OnErrorContainer":"OnSecondaryContainer");notice.IsVisible=true;}
    void Navigate(string key) {
        if(studying){Notify("Nhấn Lưu & thoát để rời flashcards. Tiến độ học được giữ lại.");return;}
        if(active){Notify(practice?"Nhấn Thoát ôn tập để chuyển sang trang khác.":"Hãy nộp bài trước khi chuyển sang trang khác.");return;}
        if(key!="bank"){selectingBanks=false;selectedBanks.Clear();}
        if(key!="bank"||currentPage!="bank")expandedBankId=null;
        currentPage=key;page.Children.Clear();preferencesStatus=null;
        SetDestination(key);
        switch(key){case "bank":Bank();break;case "setup":ExamSettings();break;case "history":HistoryPage();break;case "help":Help();break;case "preferences":PreferencesPage();break;default:Home();break;}
        pageScroll.Offset=default;Animate(page);
    }
    async Task<bool> Ask(string heading,string message,string yes="Đồng ý") {
        var dialog=Dialog(heading);dialog.Content=Stack(Text(heading,23),Text(message,15),Row(Btn("Hủy",()=>dialog.Close(false)),Btn(yes,()=>dialog.Close(true),true)));return await dialog.ShowDialog<bool>(this);
    }
    OverlayDialog Dialog(string heading)=>new(this,heading);
    Task<bool> ConfirmLeave()=>Task.FromResult(!active&&!studying&&PersistBank());
    void Home() {
        Heading("Học theo cách của bạn","Sẵn sàng cho bài thi?","Từ tài liệu có sẵn đến một bài kiểm tra của riêng bạn.");
        var heroTitle=Text("Mang tài liệu.\nBắt đầu luyện tập.",28);heroTitle.Classes.Add("onHero");
        var heroBody=Text("Nhập đề có hoặc chưa có đáp án, biên soạn và học từng câu theo nhịp của bạn.",15);heroBody.Classes.Add("onHero");
        var import=Btn("＋ Nhập file đề",Import,true);import.HorizontalAlignment=HorizontalAlignment.Left;
        var hero=new Border {Padding=new Thickness(24),CornerRadius=new CornerRadius(16),Child=new StackPanel {Spacing=16,Children={Chip("KHÔNG GIAN ÔN TẬP"),heroTitle,heroBody,import}}};
        hero.Classes.Add("hero");hero.BorderThickness=new Thickness(1);Tone(hero,Border.BorderBrushProperty,"OutlineVariant");Tone(hero,Border.BackgroundProperty,"PrimaryContainer");page.Children.Add(hero);
        var metrics=new Grid {ColumnDefinitions=new ColumnDefinitions("*,*,*"),ColumnSpacing=8};
        string[] labels={"Câu hỏi","Đã chọn","Thiếu đáp án"};int[] counts={bank.Count,setup.Included.Count,bank.Count(q=>q.Answer=="")};
        for(int i=0;i<3;i++){var metric=Metric(counts[i].ToString(),labels[i]);Grid.SetColumn(metric,i);metrics.Children.Add(metric);}page.Children.Add(metrics);
        page.Children.Add(Text("Tiếp tục học",22));
        var open=Btn(bank.Count==0?"Tự soạn bộ đề":"Mở bộ câu hỏi",bank.Count==0?NewBank:()=>Navigate("bank"));open.Classes.Add("tonal");
        page.Children.Add(Card(Stack(Chip(bank.Count==0?"CHƯA CÓ BỘ ĐỀ":"BỘ ĐỀ HIỆN TẠI"),Text(title,22),Supporting(bank.Count==0?"Thêm câu hỏi theo ý bạn hoặc khám phá bộ mẫu trước.":"Bản nháp được tự động lưu. Chọn một chế độ để tiếp tục."),Row(open,Btn("Thử bộ mẫu",LoadSample)))));
        page.Children.Add(Supporting("Word · PDF · Excel · Văn bản · .quiz",12));
    }
    async void NewBank(){
        if(busy||!await ConfirmLeave())return;
        try {var empty=new List<Question>();AdoptBank(library.Create(new WorkspaceBank {Title="Bộ đề mới",Bank=empty,Setup=preferences.CreateSetup(empty),HasUnexportedChanges=true}));selectingBanks=false;selectedBanks.Clear();RememberSelectedBank();Navigate("bank");expandedBankId=currentBankId;Navigate("bank");}
        catch(Exception ex){Notify("Không tạo được bộ đề: "+ex.Message,true);}
    }
    async void LoadSample() {
        if(busy||!await ConfirmLeave())return;
        try {using var input=AssetLoader.Open(new Uri("avares://QuizDesk.Mobile/Assets/De-mau.txt"));string path=await MobileImportCache.CopyAsync(input,"De-mau.txt",Path.Combine(dataRoot,"ImportCache"));try{await Load(path,"Đề mẫu");}finally{File.Delete(path);}}
        catch(System.Text.RegularExpressions.RegexMatchTimeoutException){Notify("Tài liệu có cấu trúc văn bản xử lý quá lâu. Hãy chia nhỏ hoặc lưu lại file trước khi nhập.",true);}catch(Exception ex){Notify(ex.Message,true);}
    }
    async void Import() {
        if(busy||!await ConfirmLeave())return;
        try {
            var files=await Storage.OpenFilePickerAsync(new FilePickerOpenOptions {Title="Nhập một hoặc nhiều file đề",AllowMultiple=true,FileTypeFilter=new[]{new FilePickerFileType("Bộ đề") {Patterns=Extensions.Select(x=>"*."+x).ToArray(),MimeTypes=new[]{"*/*"}},FilePickerFileTypes.All}});
            if(files.Count==0)return;
            if(files.Count>1){await ImportMany(files);return;}
            busy=true;shell.IsEnabled=false;await using var input=await files[0].OpenReadAsync();string path;
            try{path=await MobileImportCache.CopyAsync(input,files[0].Name,Path.Combine(dataRoot,"ImportCache"));}finally{busy=false;}
            try{await Load(path,files[0].Name);}finally{File.Delete(path);}
        }catch(Exception ex){busy=false;Notify(ex.Message,true);}finally{if(!active)shell.IsEnabled=true;}
    }
    async Task Load(string path,string displayName=null) {
        if(busy)return;busy=true;shell.IsEnabled=false;navigation.IsEnabled=false;Notify("Đang đọc file: "+(displayName??Path.GetFileName(path)));
        try {
            var imports=await Task.Run(()=>Importers.LoadBanks(path,displayName??Path.GetFileName(path)));
            if(!PersistBank())return;
            LibraryBank document=null;
            foreach(var imported in imports){
                if(!Path.GetExtension(path).Equals(".quiz",StringComparison.OrdinalIgnoreCase))imported.Content.Setup=preferences.CreateSetup(imported.Content.Bank);
                var saved=await Task.Run(()=>library.Create(imported.Content,imported.SourceName,true));document??=saved;
            }
            AdoptBank(document);selectingBanks=false;selectedBanks.Clear();RememberSelectedBank();expandedBankId=null;Navigate("bank");
            var all=imports.SelectMany(i=>i.Content.Bank).ToList();
            Notify($"Đã nhập {all.Count} câu vào {imports.Count} bộ đề{(imports.Count>1?" theo từng môn":"")} • {all.Count(q=>q.Note!="")} câu có giải thích/ghi chú • {all.Count(QuestionReview.RequiresReview)} câu cần sửa/bổ sung • {all.Count(q=>QuestionReview.ScoringAnswer(q)=="")} câu chưa tính điểm. Mở bộ đề và chọn Chỉ hiện câu cần sửa để kiểm tra.");
        }catch(System.Text.RegularExpressions.RegexMatchTimeoutException){Notify("Tài liệu có cấu trúc văn bản xử lý quá lâu. Hãy chia nhỏ hoặc lưu lại file trước khi nhập.",true);}catch(Exception ex){Notify(ex.Message,true);}finally{busy=false;navigation.IsEnabled=true;shell.IsEnabled=true;IncomingChanged();}
    }
    async void Save() {
        string temp=Path.Combine(dataRoot,"export-"+Guid.NewGuid().ToString("N")+".quiz");
        try {Directory.CreateDirectory(dataRoot);QuizFile.Save(temp,bank,setup);if(await ExportFile(SafeName(title)+".quiz","application/vnd.quizdesk.quiz",await File.ReadAllBytesAsync(temp))){dirty=false;PersistBank();Notify("Đã xuất bộ đề.");}}
        catch(System.Text.RegularExpressions.RegexMatchTimeoutException){Notify("Tài liệu có cấu trúc văn bản xử lý quá lâu. Hãy chia nhỏ hoặc lưu lại file trước khi nhập.",true);}catch(Exception ex){Notify(ex.Message,true);}finally{if(File.Exists(temp))File.Delete(temp);}
    }
    static string SafeName(string name)=>string.Concat(name.Select(c=>Path.GetInvalidFileNameChars().Contains(c)?'_':c));
    void BankEditor(StackPanel editor) {
        var page=editor;
        page.Children.Add(Row(Btn("Đổi tên bộ đề",RenameBank),Btn("Thu gọn",()=>{expandedBankId=null;Navigate("bank");}),Btn("Flashcards",StartStudy)));
        var clear=clearQuestionsButton=Btn("Xóa tất cả",DeleteAllQuestions);clear.IsEnabled=bank.Count>0;
        var tools=new Grid {ColumnDefinitions=new ColumnDefinitions("*,*,*"),ColumnSpacing=8};
        var actions=new[]{Btn("Thêm câu",()=>Edit(null),true),Btn("Xuất bộ đề",ChooseExport),Btn("Làm bài",()=>Navigate("setup"))};
        for(int i=0;i<actions.Length;i++){actions[i].Padding=new Thickness(8,10);actions[i].HorizontalAlignment=HorizontalAlignment.Stretch;Grid.SetColumn(actions[i],i);tools.Children.Add(actions[i]);}page.Children.Add(tools);
        var search=questionSearch=new TextBox {PlaceholderText="Tìm câu hỏi theo nội dung hoặc số câu…"};page.Children.Add(search);
        reviewSummaryText=Text("",13);reviewSummaryCard=ReviewCard(reviewSummaryText);page.Children.Add(reviewSummaryCard);
        onlyReviewQuestions=false;var reviewFilter=new CheckBox {Name="OnlyReviewQuestions",Content=Text("Chỉ hiện câu cần sửa / bổ sung đáp án")};Avalonia.Automation.AutomationProperties.SetName(reviewFilter,"Chỉ hiện câu cần sửa / bổ sung đáp án");page.Children.Add(reviewFilter);
        reviewFilter.IsCheckedChanged+=(_,_)=>{onlyReviewQuestions=reviewFilter.IsChecked==true;FillQuestions(search.Text);};
        selectionSummary=Supporting("",12);selectionSummary.VerticalAlignment=VerticalAlignment.Center;
        var selection=new Grid {ColumnDefinitions=new ColumnDefinitions("*,Auto,Auto"),ColumnSpacing=4};selection.Children.Add(selectionSummary);
        var all=Btn("Chọn tất cả",()=>{setup.Included=new(bank.Select(q=>q.Number));dirty=true;PersistBank();FillQuestions(search.Text);});
        var none=Btn("Bỏ chọn",()=>{setup.Included.Clear();dirty=true;PersistBank();FillQuestions(search.Text);});
        all.Classes.Add("text");none.Classes.Add("text");all.Padding=none.Padding=new Thickness(8,10);Grid.SetColumn(all,1);Grid.SetColumn(none,2);selection.Children.Add(all);selection.Children.Add(none);page.Children.Add(selection);
        questionList=new ListBox {MaxHeight=480,ItemTemplate=new Avalonia.Controls.Templates.FuncDataTemplate<Question>((q,_)=>QuestionRow(q))};
        questionList.Styles.Add(new Style(s=>s.OfType<ListBoxItem>()) {Setters={new Setter(ContentControl.HorizontalContentAlignmentProperty,HorizontalAlignment.Stretch)}});page.Children.Add(questionList);
        FillQuestions("");search.TextChanged+=(_,_)=>FillQuestions(search.Text);
        emptyBankCard=Card(Stack(Text("Bộ đề đang trống",22),Text("Thêm câu hỏi đầu tiên hoặc nhập tài liệu có sẵn.")));page.Children.Add(emptyBankCard);RefreshEditorSummary();
        page.Children.Add(Btn("Tiếp tục → Thiết lập bài thi",()=>Navigate("setup"),true));
        page.Children.Add(Supporting("Bộ đề tự lưu trong thư viện. Nhấn Xuất .quiz để sao lưu hoặc chuyển sang máy khác.",12));page.Children.Add(clear);
    }
    void FillQuestions(string query) {
        if(questionList==null)return;query??="";
        questionItems=new(bank.Where(q=>MatchesEditor(q,query)));questionList.ItemsSource=questionItems;UpdateSelection();
    }
    Control QuestionRow(Question q) {
            if(q==null)return new Border();
            var grid=new Grid {ColumnDefinitions=new ColumnDefinitions("Auto,*"),RowDefinitions=new RowDefinitions("Auto,Auto"),ColumnSpacing=12,Margin=new Thickness(0,6)};
            var check=new CheckBox {IsChecked=setup.Included.Contains(q.Number),VerticalAlignment=VerticalAlignment.Center};
            check.IsCheckedChanged+=(_,_)=>{if(check.IsChecked==true)setup.Included.Add(q.Number);else setup.Included.Remove(q.Number);dirty=true;PersistBank();UpdateSelection();};grid.Children.Add(check);
            var heading=Text("Câu "+q.Number+" · "+q.Text,15);heading.MaxLines=2;heading.TextTrimming=TextTrimming.CharacterEllipsis;
            var issues=QuestionReview.Messages(q);
            var center=Stack(heading,Text($"{q.Options.Count} lựa chọn · {(q.Answer==""?"Chưa có đáp án":"Đáp án đúng: "+q.Answer)}{(QuestionReview.ScoringAnswer(q)==""?" · Chưa tính điểm":"")}{(q.Note!=""?" · Có giải thích/ghi chú":"")}{(q.Media.Count>0?$" · {q.Media.Count} media":"")}",12));
            if(q.Answer!="") {
                var answerContent=q.Options.GetValueOrDefault(q.Answer);
                var detail=Text(string.IsNullOrWhiteSpace(answerContent)?"Không tìm thấy nội dung lựa chọn — cần kiểm tra lại.":answerContent,14);
                detail.Name="CorrectAnswerDetail";center.Children.Add(detail);
                if(q.Media.Any(m=>m.Target==q.Answer))center.Children.Add(Supporting("Đáp án có hình ảnh / media · Nhấn Sửa để xem chi tiết.",12));
            }
            if(issues.Count>0){var warning=Text("⚠ "+string.Join("\n",issues),13);Tone(warning,TextBlock.ForegroundProperty,"OnWarningContainer");center.Children.Add(warning);}
            Grid.SetColumn(center,1);grid.Children.Add(center);
            var edit=Btn("Sửa",()=>Edit(q));var remove=Btn("Xóa",()=>DeleteQuestion(q));edit.Classes.Add("text");remove.Classes.Add("text");var actions=Row(edit,remove);Grid.SetColumn(actions,1);Grid.SetRow(actions,1);grid.Children.Add(actions);var card=issues.Count>0?ReviewCard(grid):Card(grid);card.Name="QuestionRow"+q.Number;if(issues.Count==0)Tone(card,Border.BackgroundProperty,"Surface");return card;
    }
    void UpdateSelection(){if(selectionSummary!=null)selectionSummary.Text=$"{setup.Included.Count}/{bank.Count} đã chọn";}
    async void Edit(Question original) {
        var viewport=CaptureEditorViewport(original);Question edited=null;
        int number=original?.Number??(bank.Count==0?1:bank.Max(q=>q.Number)+1);
        var dialog=Dialog(original==null?"Thêm câu hỏi":"Sửa câu "+number);dialog.Height=730;
        var text=new TextBox {Name="QuestionText",Text=original?.Text??"",AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,MinHeight=90};
        var body=Stack(Text(original==null?"Thêm câu hỏi":"Sửa câu "+number,22),Field("Nội dung câu hỏi",text),Text("Nhập ít nhất 2 lựa chọn. Bỏ trống các ô không dùng.",13));var options=new Dictionary<string,TextBox>();
        if(original!=null){body.Children.Add(Text("Nguồn: "+original.AnswerSource+"\nĐáp án gốc: "+original.OriginalAnswer,13));var issues=QuestionReview.Messages(original);if(issues.Count>0)body.Children.Add(ReviewCard(Text(string.Join("\n",issues)+"\n\nLưu sau khi kiểm tra để xác nhận các nhãn và đáp án. Nội dung lựa chọn còn trùng sẽ tiếp tục được đánh dấu.",13)));}
        foreach(char c in "ABCDEFGH") {string key=c.ToString();var box=new TextBox {Name="Option"+key,Text=original?.Options.GetValueOrDefault(key)??"",AcceptsReturn=true,TextWrapping=TextWrapping.Wrap};options[key]=box;body.Children.Add(Field("Lựa chọn "+key,box));}
        var answer=new ComboBox {Name="CorrectAnswer",ItemsSource=new[]{"Chưa có đáp án (chưa tính điểm)","A","B","C","D","E","F","G","H"},HorizontalAlignment=HorizontalAlignment.Stretch,SelectedIndex=original==null||original.Answer==""?0:original.Answer[0]-'A'+1};body.Children.Add(Field("Đáp án đúng — có thể bổ sung sau",answer));
        var note=new TextBox {Name="QuestionNote",Text=original?.Note??"",AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,MinHeight=80};body.Children.Add(Field("Giải thích / ghi chú",note));
        var attachments=(original?.Media??new()).Select(m=>m.Clone()).ToList();AddMediaEditor(body,attachments,original);
        var error=Text("");body.Children.Add(error);
        var save=Btn("Lưu câu hỏi",()=>{
            var result=new Question {Number=number,Media=attachments.Select(m=>m.Clone()).ToList(),Text=text.Text?.Trim()??"",Answer=answer.SelectedIndex<=0?"":answer.SelectedItem.ToString(),Note=note.Text?.Trim()??"",OriginalAnswer=original?.OriginalAnswer??"",AnswerSource="Người dùng đã kiểm tra / biên soạn"};
            foreach(var o in options){if(!string.IsNullOrWhiteSpace(o.Value.Text))result.Options[o.Key]=o.Value.Text.Trim();else if(attachments.Any(m=>m.Target==o.Key))result.Options[o.Key]="(Xem media)";}
            if(string.IsNullOrWhiteSpace(result.Text)&&attachments.Any(m=>m.Target=="question"))result.Text="Xem hình ảnh / video và chọn đáp án.";
            try{CommitQuestion(original,result);edited=result;dialog.Close(true);}catch(Exception ex){error.Text=ex.Message;}
        },true);
        var cancel=Btn("Hủy",()=>dialog.Close(false));cancel.IsCancel=true;
        var layout=new DockPanel();var footer=Row(cancel,save);DockPanel.SetDock(footer,Dock.Bottom);layout.Children.Add(footer);layout.Children.Add(new ScrollViewer {Content=body,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});dialog.Content=layout;
        if(await dialog.ShowDialog<bool>(this))RefreshEditedQuestion(original,edited,viewport);
    }
    void CommitQuestion(Question original,Question result) {
        Importers.Validate(bank.Where(q=>!ReferenceEquals(q,original)).Append(result).ToList());if(bank.Any(q=>q.Number==result.Number&&!ReferenceEquals(q,original)))throw new InvalidOperationException("Số câu bị trùng.");
        if(original==null){bank.Add(result);setup.Included.Add(result.Number);}else{int at=bank.IndexOf(original);if(at<0)throw new InvalidOperationException("Câu hỏi không còn trong bộ đề.");bank[at]=result;}dirty=true;PersistBank();
    }
    void RemoveQuestion(Question q){if(!bank.Remove(q))return;setup.Included.Remove(q.Number);setup.Count=Math.Min(setup.Count,Math.Max(1,bank.Count));dirty=true;PersistBank();}
    void ClearQuestions(){bank.Clear();setup.Included.Clear();setup.Count=1;dirty=true;PersistBank();}
    async void DeleteQuestion(Question q){if(q==null||!bank.Contains(q))return;if(await Ask("Xóa câu "+q.Number+"?","Câu hỏi sẽ bị xóa khỏi bộ đề trong thư viện. Nhật ký giữ nội dung đã xóa; file nguồn không thay đổi.","Xóa câu")){RemoveQuestion(q);Navigate("bank");}}
    async void DeleteAllQuestions(){if(bank.Count==0)return;if(await Ask("Xóa tất cả "+bank.Count+" câu?","Xóa toàn bộ câu hỏi trong bộ đề này, kể cả câu đang ẩn bởi tìm kiếm? Các bộ đề khác và file nguồn không thay đổi.","Xóa tất cả")){ClearQuestions();Navigate("bank");Notify("Đã xóa câu hỏi trong bộ đề này và ghi nhật ký thay đổi.");}}
    void ApplyTheme()=>Application.Current.RequestedThemeVariant=preferences.Theme=="dark"?ThemeVariant.Dark:preferences.Theme=="light"?ThemeVariant.Light:ThemeVariant.Default;
    void SavePreferences(){try{preferencesStore.Save(preferences);if(preferencesStatus!=null)preferencesStatus.Text="Đã tự động lưu cài đặt.";}catch(Exception ex){Notify("Không lưu được cài đặt: "+ex.Message,true);}}
    void RememberExamSettings(){preferences.QuestionCount=setup.Count;preferences.Minutes=setup.Minutes;preferences.ShuffleQuestions=setup.ShuffleQuestions;preferences.ShuffleOptions=setup.ShuffleOptions;SavePreferences();}
    void ApplyPreferencesToBank(){setup.Count=Math.Min(preferences.QuestionCount,Math.Max(1,bank.Count));setup.Minutes=preferences.Minutes;setup.ShuffleQuestions=preferences.ShuffleQuestions;setup.ShuffleOptions=preferences.ShuffleOptions;dirty=true;PersistBank();}
    static NumericUpDown Number(int value,int max)=>new() {Minimum=1,Maximum=max,Value=value,Increment=1,FormatString="0",HorizontalAlignment=HorizontalAlignment.Stretch};
    static int Value(NumericUpDown input)=>Math.Clamp((int)Math.Round(input.Value??1),(int)input.Minimum,(int)input.Maximum);
    void PreferencesPage() {
        Heading("Tùy chọn ứng dụng","Cài đặt","Thay đổi được tự động lưu và khôi phục khi mở lại app.");
        preferencesStatus=Text("Lưu trên thiết bị này. Không cần tài khoản.",13);
        var theme=new ComboBox {ItemsSource=new[]{"Sáng","Tối","Theo hệ thống"},SelectedIndex=preferences.Theme=="dark"?1:preferences.Theme=="system"?2:0,HorizontalAlignment=HorizontalAlignment.Stretch};
        theme.SelectionChanged+=(_,_)=>{preferences.Theme=theme.SelectedIndex==1?"dark":theme.SelectedIndex==2?"system":"light";ApplyTheme();SavePreferences();};
        page.Children.Add(Card(Field("Giao diện",theme)));
        page.Children.Add(PerformanceSettings());
        glassSettingsPanel=GlassSettings();glassSettingsPanel.IsEnabled=!preferences.PerformanceMode;page.Children.Add(glassSettingsPanel);
        var motion=motionSetting=new ToggleSwitch {Content="Giảm chuyển động",IsChecked=preferences.ReducedMotion,IsEnabled=!preferences.PerformanceMode};
        motion.IsCheckedChanged+=(_,_)=>{preferences.ReducedMotion=motion.IsChecked==true;StopMotion(page);SavePreferences();};
        page.Children.Add(Card(Stack(motion,Supporting("Chuyển tiếp nhanh 90–140 ms. Bật để hiển thị nội dung tức thì."))));
        var count=Number(preferences.QuestionCount,5000);var minutes=Number(preferences.Minutes,300);
        var shuffle=new ToggleSwitch {Content="Trộn câu hỏi",IsChecked=preferences.ShuffleQuestions};var options=new ToggleSwitch {Content="Trộn lựa chọn",IsChecked=preferences.ShuffleOptions};
        count.ValueChanged+=(_,_)=>{preferences.QuestionCount=Value(count);SavePreferences();};minutes.ValueChanged+=(_,_)=>{preferences.Minutes=Value(minutes);SavePreferences();};
        shuffle.IsCheckedChanged+=(_,_)=>{preferences.ShuffleQuestions=shuffle.IsChecked==true;SavePreferences();};options.IsCheckedChanged+=(_,_)=>{preferences.ShuffleOptions=options.IsChecked==true;SavePreferences();};
        var apply=Btn("Áp dụng cho bộ đề đang mở",()=>{ApplyPreferencesToBank();Notify("Đã áp dụng cài đặt mặc định cho bộ đề.");});apply.IsEnabled=bank.Count>0;
        page.Children.Add(Card(Stack(Text("Cấu hình thi mặc định",21),Text("Dùng cho bộ đề mới nhập. File .quiz ưu tiên cấu hình đã lưu trong file.",13),Field("Số câu mặc định",count),Field("Thời gian mặc định (phút)",minutes),shuffle,options,apply)));
        page.Children.Add(preferencesStatus);page.Children.Add(Text("Thiết lập bài thi cũng được nhớ làm mặc định. Nội dung câu hỏi cần nhấn Lưu bộ đề để lưu thành .quiz.",13));
        page.Children.Add(Text("QuizDesk "+AppVersion.Display+" · "+(OperatingSystem.IsIOS()?"iOS":OperatingSystem.IsAndroid()?"Android":MobilePlatform.DesktopHost?(OperatingSystem.IsMacOS()?"macOS":"Windows"):"Preview"),12));
    }
    void ExamSettings() {
        setup.Count=Math.Clamp(setup.Count,1,Math.Max(1,bank.Count));Heading("Bài kiểm tra của bạn","Chọn nhịp học phù hợp",$"{setup.Included.Count} câu được chọn từ bộ “{title}”.");
        int unscored=bank.Count(q=>setup.Included.Contains(q.Number)&&QuestionReview.ScoringAnswer(q)=="");if(unscored>0)page.Children.Add(ReviewCard(Text($"{unscored} câu đã chọn chưa có đáp án hoặc còn trùng/mơ hồ. Bạn vẫn có thể làm bài; các câu này chưa tính điểm. Mở Bộ đề → Sửa để bổ sung.",14)));
        var count=Number(setup.Count,Math.Max(1,bank.Count));var minutes=Number(setup.Minutes,300);
        count.Classes.Add("examSetting");minutes.Classes.Add("examSetting");
        var shuffle=new ToggleSwitch {Content="Trộn thứ tự câu hỏi",IsChecked=setup.ShuffleQuestions};var options=new ToggleSwitch {Content="Trộn các lựa chọn",IsChecked=setup.ShuffleOptions};
        count.ValueChanged+=(_,_)=>{setup.Count=Value(count);dirty=true;PersistBank();RememberExamSettings();};minutes.ValueChanged+=(_,_)=>{setup.Minutes=Value(minutes);dirty=true;PersistBank();RememberExamSettings();};
        shuffle.IsCheckedChanged+=(_,_)=>{setup.ShuffleQuestions=shuffle.IsChecked==true;dirty=true;PersistBank();RememberExamSettings();};options.IsCheckedChanged+=(_,_)=>{setup.ShuffleOptions=options.IsChecked==true;dirty=true;PersistBank();RememberExamSettings();};
        page.Children.Add(Card(Stack(Field("Số câu thi thử",count),Field("Thời gian (phút)",minutes),shuffle,options)));
        page.Children.Add(Card(Stack(Text("Ôn tập hay thi thử?",22),Text("Ôn tập: dùng tất cả câu đã chọn. Xem kết quả và giải thích sau mỗi câu.\n\nThi thử: lấy số câu đã đặt. Có thể đổi đáp án đến khi nộp hoặc hết giờ."),Text("Tắt trộn lựa chọn nếu nội dung câu/giải thích nhắc đến nhãn A/B/C/D. Ghi chú giữ nguyên nội dung gốc.",13))));
        page.Children.Add(Row(Btn("Bắt đầu thi thử",()=>Start(false),true),Btn("Ôn tập",StartNewPractice),Btn("Lưu cấu hình",Save)));
        PracticeEntry();
        StudyEntry();
    }
    void Start(bool isPractice) {
        try{exam=QuizSetup.Build(bank,setup,isPractice,new Random());}catch(Exception ex){Notify(ex.Message,true);return;}
        examTitle=title;examMinutes=setup.Minutes;examBankId=currentBankId;
        answers.Clear();index=0;practice=isPractice;active=true;activeAttemptId=Guid.NewGuid();deadline=DateTime.Now.AddMinutes(setup.Minutes);notice.IsVisible=false;navigation.IsEnabled=false;
        startedAt=DateTimeOffset.Now;attemptWatch.Restart();BeginCheckpoint();
        clock.Text=TimeSpan.FromMinutes(setup.Minutes).ToString(@"hh\:mm\:ss");CreateQuestionView();ShowQuestion();if(!practice)timer.Start();pageScroll.Offset=default;Animate(page);
    }
    // Keep these controls alive throughout an attempt, including every answer and navigation event.
    void CreateQuestionView() {
        page.Children.Clear();answerButtons.Clear();answerLabels.Clear();answerMarkers.Clear();jumpButtons.Clear();page.Children.Add(Text(practice?"ÔN TẬP":"THI THỬ",12));
        questionHeading=Text("",24);page.Children.Add(questionHeading);questionSummary=Text("");page.Children.Add(questionSummary);if(!practice)page.Children.Add(clock);
        questionProgress=new ProgressBar {Minimum=0,Maximum=exam.Count,Height=6};page.Children.Add(questionProgress);
        questionText=Text("",22);questionText.FontWeight=FontWeight.Medium;var choices=Stack(questionText);questionMedia=Gallery();choices.Children.Add(questionMedia);choiceMedia.Clear();
        foreach(char c in "ABCDEFGH") {
            string key=c.ToString();var label=Text("",16);var marker=Text("",16);marker.FontWeight=FontWeight.SemiBold;marker.VerticalAlignment=VerticalAlignment.Center;
            var content=new Grid {ColumnDefinitions=new ColumnDefinitions("24,*"),ColumnSpacing=8};content.Children.Add(marker);var optionBody=Stack(label);var gallery=Gallery();choiceMedia[key]=gallery;optionBody.Children.Add(gallery);Grid.SetColumn(optionBody,1);content.Children.Add(optionBody);
            var button=new ToggleButton {Content=content,Padding=new Thickness(16,14),HorizontalAlignment=HorizontalAlignment.Stretch,HorizontalContentAlignment=HorizontalAlignment.Stretch,MinHeight=64};
            button.Classes.Add("choice");button.Click+=(_,_)=>ChooseAnswer(key);answerButtons[key]=button;answerLabels[key]=label;answerMarkers[key]=marker;choices.Children.Add(button);
        }
        feedbackText=Text("");noteMedia=Gallery();questionFeedback=new Border {Child=Stack(feedbackText,noteMedia),IsVisible=false,Padding=new Thickness(16),CornerRadius=new CornerRadius(14)};choices.Children.Add(questionFeedback);page.Children.Add(Card(choices));
        previousQuestion=Btn("← Câu trước",()=>MoveToQuestion(index-1));nextQuestion=Btn("Câu tiếp →",()=>MoveToQuestion(index+1));
        var end=practice?Btn("Thoát ôn tập",ExitPractice):Btn("Nộp bài",async()=>{if(await Ask("Nộp bài?",$"Bạn đã trả lời {answers.Count}/{exam.Count} câu.","Nộp bài"))Finish();},true);
        if(practice)page.Children.Add(Supporting("Tiến trình tự lưu khi trả lời, chuyển câu hoặc thoát ôn tập. Có thể tiếp tục riêng từng bộ đề trong Học & thi.",12));
        page.Children.Add(Row(previousQuestion,nextQuestion,end));
        CreateJumpNavigator();
    }
    void MoveToQuestion(int target){if(!active||target<0||target>=exam.Count)return;if(index==target){UpdateJumpState();return;}index=target;ShowQuestion();Checkpoint();Animate(questionText,110);}
    void ChooseAnswer(string key){if(!active)return;bool changed=(!practice||!answers.ContainsKey(index))&&exam[index].Options.ContainsKey(key)&&answers.GetValueOrDefault(index)!=key;if(changed)answers[index]=key;UpdateAnswerState();if(changed){Checkpoint();Animate(answerMarkers[key],90,true);}}
    void ShowQuestion(){var q=exam[index];questionHeading.Text=$"Câu {index+1} / {exam.Count}";questionText.Text=q.Text;questionMedia.Set(q.Media.Where(m=>m.Target=="question"));foreach(var pair in answerButtons){bool exists=q.Options.TryGetValue(pair.Key,out string value);pair.Value.IsVisible=exists;if(exists)answerLabels[pair.Key].Text=pair.Key+".  "+value;choiceMedia[pair.Key].Set(q.Media.Where(m=>m.Target==pair.Key));}previousQuestion.IsEnabled=index>0;nextQuestion.IsEnabled=index<exam.Count-1;UpdateAnswerState();}
    void UpdateAnswerState() {
        var q=exam[index];answers.TryGetValue(index,out string picked);questionSummary.Text=$"Đã trả lời {answers.Count} câu · Câu gốc {q.Number}";questionProgress.Value=answers.Count;
        foreach(var p in answerButtons){
            bool correct=practice&&picked!=null&&p.Key==q.Answer,wrong=practice&&picked!=null&&q.Answer!=""&&p.Key==picked&&picked!=q.Answer;
            p.Value.IsChecked=p.Key==picked;p.Value.Classes.Set("correct",correct);p.Value.Classes.Set("wrong",wrong);
            answerMarkers[p.Key].Text=correct?"✓":wrong?"×":p.Key==picked?"✓":"";
            Avalonia.Automation.AutomationProperties.SetName(p.Value,answerLabels[p.Key].Text+(correct?" · Đáp án đúng":wrong?" · Bạn chọn, chưa đúng":p.Key==picked?" · Đã chọn":""));
        }
        UpdateJumpState();
        bool show=practice&&picked!=null;if(show){feedbackText.Text=(q.Answer==""?"Chưa có đáp án đã xác nhận để chấm điểm. Bạn có thể bổ sung trong Bộ đề → Sửa.":(picked==q.Answer?"Chính xác!":"Chưa đúng.")+" Đáp án: "+q.Answer)+"\n"+q.Note;Tone(questionFeedback,Border.BackgroundProperty,q.Answer==""?"WarningContainer":picked==q.Answer?"SuccessContainer":"ErrorContainer");Tone(feedbackText,TextBlock.ForegroundProperty,q.Answer==""?"OnWarningContainer":picked==q.Answer?"OnSuccessContainer":"OnErrorContainer");}noteMedia.Set(show?q.Media.Where(m=>m.Target=="note"):Array.Empty<QuestionMedia>());questionFeedback.IsVisible=show;
    }
    void ExitPractice(){
        if(!active||!practice)return;
        try{SavePracticeProgress();workspace.Complete(activeAttemptId);}catch(Exception ex) when(ex is IOException||ex is InvalidDataException||ex is UnauthorizedAccessException){Notify("Chưa lưu được ôn tập: "+ex.Message,true);return;}
        active=false;timer.Stop();attemptWatch.Stop();currentDialog?.Close(false);navigation.IsEnabled=true;Navigate("setup");IncomingChanged();
    }
    void Tick(){if(!active)return;var left=deadline-DateTime.Now;clock.Text=left.TotalSeconds>0?left.ToString(@"hh\:mm\:ss"):"00:00:00";if(left.TotalSeconds<=0)Finish(true);}
    void Finish(bool timedOut=false) {
        if(!active)return;if(practice){ExitPractice();return;}active=false;timer.Stop();currentDialog?.Close(false);navigation.IsEnabled=true;page.Children.Clear();
        attemptWatch.Stop();ExamAttempt completed=null;
        if(!practice){completed=ExamAttempt.Capture(examTitle,startedAt,timedOut?new DateTimeOffset(deadline):DateTimeOffset.Now,TimeSpan.FromSeconds(Math.Clamp((DateTimeOffset.Now-startedAt).TotalSeconds,0,examMinutes*60)),examMinutes,timedOut,exam,answers);completed.Id=activeAttemptId;unsavedAttempts.Add(completed);TrySaveAttempts();}else{workspace.Complete(activeAttemptId);}
        int graded=exam.Count(q=>q.Answer!=""),correct=Enumerable.Range(0,exam.Count).Count(i=>exam[i].Answer!=""&&answers.TryGetValue(i,out string a)&&a==exam[i].Answer);
        Heading("Hoàn thành","Bạn đã hoàn thành bài làm",graded==0?"Bài chưa có câu đủ đáp án để tính điểm.":$"Đúng {correct} / {graded} câu được tính điểm");
        page.Children.Add(Card(Stack(Text(graded==0?"—":(10.0*correct/graded).ToString("0.##")+" / 10",42),Text($"Không tính điểm: {exam.Count(q=>q.Answer=="")} câu. Điểm dựa trên đáp án trong bộ đề."))));
        var report=new StringBuilder(examTitle+"\n"+DateTime.Now.ToString("dd/MM/yyyy HH:mm")+$"\nĐúng {correct}/{graded}\n\n");
        page.Children.Add(Row(Btn("Lưu kết quả",()=>SaveReport(report.ToString()),true),Btn("Làm lại",()=>Navigate("setup")),Btn("Về bộ đề",()=>Navigate("bank"))));
        if(completed!=null){var snapshot=completed;page.Children.Add(Row(Btn("Xem lại lần thi này",()=>ShowAttempt(snapshot)),Btn("Mở lịch sử",()=>Navigate("history"))));page.Children.Add(Text(unsavedAttempts.Contains(completed)?"Chưa lưu được lịch sử. Mở Lịch sử để thử lưu lại.":"Đã tự động lưu lần thi vào Lịch sử.",13));}
        foreach(int i in Enumerable.Range(0,exam.Count).OrderBy(i=>exam[i].Answer==""?2:answers.TryGetValue(i,out var a)&&a==exam[i].Answer?1:0)) {
            var q=exam[i];string a=answers.GetValueOrDefault(i,"Chưa trả lời");string text=q.Text+"\n\n"+string.Join("\n",q.Options.Select(o=>o.Key+". "+o.Value))+"\n\nBạn chọn: "+a+"\nĐáp án: "+(q.Answer==""?"Không tính điểm":q.Answer)+"\n"+q.Note;report.AppendLine("Câu "+q.Number+"\n"+text+"\n");
            var detail=new Expander {Header="Câu "+q.Number+" · "+(q.Answer==""?"Không tính điểm":a==q.Answer?"Đúng":a=="Chưa trả lời"?"Chưa trả lời":"Sai"),HorizontalAlignment=HorizontalAlignment.Stretch,HorizontalContentAlignment=HorizontalAlignment.Stretch};
            detail.PropertyChanged+=(_,e)=>{if(e.Property==Expander.IsExpandedProperty)detail.Content=detail.IsExpanded?Stack(Text(text),Gallery(q.Media)):null;};page.Children.Add(detail);
        }pageScroll.Offset=default;IncomingChanged();
    }
    async void SaveReport(string text) {
        try{if(await ExportFile("Ket-qua-"+DateTime.Now.ToString("yyyyMMdd-HHmm")+".txt","text/plain",Encoding.UTF8.GetBytes(text)))Notify("Đã xuất kết quả.");}catch(System.Text.RegularExpressions.RegexMatchTimeoutException){Notify("Tài liệu có cấu trúc văn bản xử lý quá lâu. Hãy chia nhỏ hoặc lưu lại file trước khi nhập.",true);}catch(Exception ex){Notify(ex.Message,true);}
    }
    void Help() {
        Heading("Nhập đề","Tài liệu của bạn, sẵn sàng để luyện tập","App đọc câu hỏi và đáp án có sẵn. Không tự xác minh kiến thức hoặc tự đoán đáp án.");
        page.Children.Add(Card(Stack(Text("Tài liệu văn bản",22),Text("DOC/DOCX, PDF có chữ, TXT, Markdown, RTF, HTML, ODT. Mỗi câu bắt đầu bằng Câu 1. hoặc 1. Mỗi lựa chọn nằm trên một dòng.\nĐáp án có dạng Đáp án: B hoặc bảng đáp án cuối đề."),Text("Câu 1. Kết quả của 2 + 3?\nA. 4\nB. 5\nĐáp án: B\nGiải thích: 2 + 3 = 5.",16))));
        page.Children.Add(Card(Stack(Text("Bảng tính & dữ liệu",22),Text("XLS/XLSX: đọc sheet đầu tiên. CSV/TSV hỗ trợ ô nhiều dòng có ngoặc kép.\nCác cột: number, text, A, B, C, D, answer, note. Có thể thêm E–H.\nJSON: [{number, text, options: {A, B, ...}, answer, note}].\n.quiz có media cần QuizDesk 5.11.0 trở lên; file chỉ có chữ vẫn dùng định dạng cũ."))));
        page.Children.Add(Card(Stack(Text("Đề chưa có đáp án hoặc có đáp án trùng",22),Text("Có thể nhập đề chưa có đáp án: bỏ dòng Đáp án hoặc để trống/bỏ cột answer. Câu đó được tô vàng; chọn Sửa để thêm đáp án và giải thích.\nNhãn lựa chọn bị lặp được giữ đủ nội dung ở nhãn còn trống. Nhiều dòng đáp án hoặc lựa chọn trùng nội dung cũng được đánh dấu. Dùng bộ lọc Chỉ hiện câu cần sửa; lưu câu sau khi kiểm tra. Các câu chưa có đáp án hoặc còn trùng/mơ hồ chưa được tính điểm."))));
        page.Children.Add(Card(Stack(Text("Ảnh, GIF & video",22),Text("DOC/DOCX: giữ media nhúng theo câu hỏi, lựa chọn hoặc giải thích. Trong Sửa câu hỏi có nút Thêm ảnh / GIF / video. GIF có nút phát/dừng; video phát native trên Android/iOS hoặc qua ứng dụng mặc định trên PC. Đồng hồ thi vẫn tiếp tục.\nPNG/JPEG/WebP/GIF tối đa 8 MB/file, MP4/WebM tối đa 16 MB/file, tổng media 24 MB/bộ và 16 media/câu. Bộ đề chia sẻ .quiz chứa cả media để dùng offline.\nPDF/bảng tính/HTML/ODT hiện chỉ nhập phần chữ; thêm media thủ công sau. Không tải media từ liên kết bên ngoài hoặc chạy đối tượng OLE."))));
        page.Children.Add(Card(Stack(Text("Cần chuẩn hóa trước khi nhập",22),Text("PDF ảnh/scan cần OCR bên ngoài. Chưa hỗ trợ Word trước 97, file mật khẩu, công thức toán dạng đối tượng hay câu nhiều đáp án đúng. Tài liệu nhiều cột có thể cần sửa thứ tự sau khi nhập.\nTối đa 50 MB/file và 5.000 câu. Ô công thức XLSX cần chuyển thành giá trị trước khi nhập."),Btn("Thử đề mẫu",LoadSample))));
        page.Children.Add(Btn("Giấy phép thư viện",ShowLicenses));
    }
}
