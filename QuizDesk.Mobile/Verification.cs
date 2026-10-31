#if QA
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace QuizDesk.Mobile;
public partial class MainView {
    bool qaStarted;
    static void Check(bool value,string label){if(!value)throw new Exception(label);}
    static IEnumerable<T> Controls<T>(Visual root) where T:Control=>root.GetVisualDescendants().OfType<T>();
    static void Click(Button button)=>button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    static Button Find(Visual root,string label)=>Controls<Button>(root).First(b=>Equals(b.Content,label));
    async Task Capture(string name) {
        await Task.Delay(180);using var bitmap=new RenderTargetBitmap(new PixelSize((int)Bounds.Width,(int)Bounds.Height),new Vector(96,96));bitmap.Render(this);bitmap.Save(Path.Combine(dataRoot,name+".png"));
    }
    async Task VerifyMobile() {
        string phase=Environment.GetEnvironmentVariable("QUIZDESK_MOBILE_QA_PHASE")??"first";
        string result=Path.Combine(dataRoot,phase+"-result.txt");Directory.CreateDirectory(dataRoot);
        try {
            if(phase.StartsWith("flags")){File.WriteAllText(result,await VerifyFlags(phase));return;}
            if(phase=="word-symbols"){File.WriteAllText(result,await VerifyWordSymbols());return;}
            if(phase=="aero"){File.WriteAllText(result,await VerifyAero());return;}
            if(phase.StartsWith("motion")){File.WriteAllText(result,await VerifyMotion(phase));return;}
            if(phase.StartsWith("practice-colors")){File.WriteAllText(result,await VerifyPracticeColors(phase));return;}
            if(phase.StartsWith("practice")){File.WriteAllText(result,await VerifyPractice(phase));return;}
            if(phase=="recognition"){File.WriteAllText(result,await VerifyRecognition());return;}
            if(phase.StartsWith("merge")){File.WriteAllText(result,await VerifyMerge(phase));return;}
            if(phase=="media"){File.WriteAllText(result,await VerifyMedia());return;}
            if(phase=="render-copy"){File.WriteAllText(result,await VerifyRenderCopy());return;}
            if(phase.StartsWith("performance-mode")){File.WriteAllText(result,await VerifyPerformanceMode(phase));return;}
            if(phase=="performance"){File.WriteAllText(result,await VerifyPerformance());return;}
            if(phase=="edit-position"){File.WriteAllText(result,await VerifyEditPosition());return;}
            if(phase=="study"||phase=="study-restart"){File.WriteAllText(result,await VerifyStudy(phase));return;}
            if(phase.StartsWith("external")){File.WriteAllText(result,await VerifyExternal(phase));return;}
            if(phase=="review"||phase=="review-restart"){File.WriteAllText(result,await VerifyImportReview(phase));return;}
            if(phase=="delete"||phase=="delete-restart"){File.WriteAllText(result,await VerifyDelete(phase));return;}
            if(phase=="desktop-layout"){File.WriteAllText(result,await VerifyDesktopLayout());return;}
            if(phase=="library"||phase=="library-restart"||phase=="library-migration"){File.WriteAllText(result,await VerifyLibrary(phase));return;}
            if(phase=="material"||phase=="design") {
                preferences.Theme="light";ApplyTheme();Navigate("home");await Capture("qd-home-light");
                if(Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime wideHost){wideHost.MainWindow.MinWidth=320;wideHost.MainWindow.Width=1280;wideHost.MainWindow.Height=860;await Task.Delay(150);await Capture("qd-home-desktop-light");}
                Check(navButtons["home"].Classes.Contains("selected"),"Home destination indicator missing");
                var primary=Find(page,"＋ Nhập file đề");
                var foreground=Controls<TextBlock>(primary).First().Foreground as Avalonia.Media.ISolidColorBrush;
                Check(foreground?.Color==Avalonia.Media.Color.Parse("#FFFFFF"),"Filled button text has insufficient contrast");
                if(Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime host) {
                    host.MainWindow.Width=360;await Task.Delay(150);await Capture("qd-home-360");
                    Check(navButtons.Values.Where(b=>b.Classes.Contains("nav")).All(b=>b.Bounds.Width>=48&&b.Bounds.Height>=48),"Navigation touch targets below 48dp");
                }
                bank=new(){new Question {Number=1,Text="Tín hiệu VOR cung cấp thông tin nào cho máy bay?",Answer="A",Note="VOR giúp xác định phương vị của máy bay so với đài phát.",Options=new(){{"A","Phương vị so với đài VOR"},{"B","Độ cao tuyệt đối của máy bay"},{"C","Tốc độ bay so với mặt đất"},{"D","Khoảng cách đến sân bay gần nhất"}}}};
                setup=new(){Count=1,Minutes=30,Included=new(){1}};title="Dẫn đường hàng không · VOR";
                PersistBank();Navigate("bank");OpenLibraryBank(currentBankId.Value);await Capture("qd-bank-360");
                Click(Find(page,"Xóa tất cả"));await Task.Delay(100);await Capture("qd-delete-dialog-360");
                var destructiveText=Controls<TextBlock>(Find(modal,"Xóa tất cả")).First().Foreground as Avalonia.Media.ISolidColorBrush;
                Check(destructiveText?.Color==Avalonia.Media.Color.Parse("#FFFFFF"),"Destructive filled button contrast failed");currentDialog.Close(false);await Task.Delay(80);
                Click(Find(page,"Sửa"));await Task.Delay(100);await Capture("qd-editor-360");currentDialog.Close(false);await Task.Delay(80);
                Navigate("setup");await Capture("qd-setup-360");
                Start(false);await Task.Delay(100);var stableBounds=answerButtons["A"].Bounds;ChooseAnswer("A");await Task.Delay(100);
                Check(answerMarkers["A"].Text=="✓"&&!answerButtons["A"].Classes.Contains("correct"),"Exam selection must not reveal grading");
                Check(stableBounds==answerButtons["A"].Bounds,"Choosing an answer shifted its bounds");await Capture("qd-exam-selected-360");Finish();
                Navigate("setup");Start(true);ChooseAnswer("B");await Capture("qd-practice-wrong-360");
                Check(answerMarkers["B"].Text=="×"&&answerMarkers["A"].Text=="✓","Practice feedback needs non-color markers");Finish();
                Navigate("setup");Start(true);ChooseAnswer("A");await Capture("qd-practice-correct-360");
                Check(answerButtons["A"].Classes.Contains("correct"),"Correct answer state missing");
                Finish();Navigate("preferences");preferences.Theme="dark";ApplyTheme();Navigate("preferences");await Capture("qd-settings-dark-360");
                Navigate("home");await Capture("qd-home-dark-360");
                if(Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime tablet) {tablet.MainWindow.Width=1280;tablet.MainWindow.Height=860;await Task.Delay(100);await Capture("qd-home-desktop-dark");}
                File.WriteAllText(result,"PASS: QuizDesk light/dark palettes; white text on filled primary button; selected navigation; 48dp navigation touch targets at 360dp; question bank/editor/setup/practice states at 360dp; 1280dp desktop rendering; selected-answer marker and stable bounds; exam selection does not reveal grading; practice has correct/wrong markers. These are host preview checks, not native Android/iOS runtime tests.");return;
            }
            if(phase=="restart") {
                Check(active&&answers.Count==1&&index==1,"Incomplete exam/answer/index not restored");
                Check(preferences.Theme=="dark"&&preferences.Minutes==42,"Settings not restored");
                Check(dirty,"Unexported draft-change flag was not restored");
                Check(historyStore.Load(out _).Count==1,"History not restored");
                await Capture("09-restored-exam");Guid id=activeAttemptId;deadline=DateTime.Now.AddSeconds(-1);ResumeSession();
                var restoredAttempts=historyStore.Load(out _);Check(!active&&restoredAttempts.Count==2&&restoredAttempts.Any(a=>a.Id==id&&a.TimedOut),"Resumed expired exam did not submit once");
                Check(workspace.LoadActive(out _)==null,"Submitted exam was not cleared");Finish();Check(historyStore.Load(out _).Count==2,"Duplicate submission after restore");
                File.WriteAllText(result,"PASS: a fresh process restores draft, Settings, History, incomplete exam, question index and chosen answer; expiry submits once and clears the checkpoint.");return;
            }
            await Capture("01-home");
            using(var input=AssetLoader.Open(new Uri("avares://QuizDesk.Mobile/Assets/De-mau.txt"))) {
                string path=await MobileImportCache.CopyAsync(input,"De-mau.txt",Path.Combine(dataRoot,"ImportCache"));try{await Load(path,"Đề mẫu mobile");}finally{File.Delete(path);}
            }
            Check(bank.Count==2,"Embedded sample import failed");Check(questionList==null,"Imported bank must start collapsed");OpenLibraryBank(currentBankId.Value);await Capture("02-bank");
            Click(Find(page,"Sửa"));await Task.Delay(100);Check(currentDialog!=null&&modal.IsVisible,"Touch editor did not open");
            var text=Controls<TextBox>(modal).Single(b=>b.Name=="QuestionText");text.Text="Câu hỏi đã sửa trên điện thoại";
            Click(Find(modal,"Lưu câu hỏi"));await Task.Delay(80);Check(bank[0].Text.Contains("điện thoại"),"Touch editor did not save");
            Check(workspace.LoadBank(out _).Bank[0].Text==bank[0].Text,"Edited draft not persisted");
            Click(Find(page,"Xóa tất cả"));await Task.Delay(60);Click(Find(modal,"Hủy"));await Task.Delay(60);Check(bank.Count==2,"Cancel cleared the bank");
            Click(Find(page,"Xóa tất cả"));await Task.Delay(60);Click(Find(modal,"Xóa tất cả"));await Task.Delay(60);Check(bank.Count==0,"Confirm did not clear bank");
            bank=new(){new Question {Number=1,Text="Chọn kết quả của 2 + 3",Answer="B",Note="Hai cộng ba bằng năm.",Options=new(){{"A","Bốn"},{"B","Năm"}}},new Question {Number=2,Text="Chọn số nhỏ hơn",Answer="A",Options=new(){{"A","Một"},{"B","Hai"}}}};
            setup=new(){Count=2,Minutes=1,ShuffleQuestions=false,ShuffleOptions=false,Included=new(){1,2}};title="Bài mobile QA";PersistBank();Navigate("setup");await Capture("03-setup");
            Start(false);var tree=page.Children.ToArray();var original=answerButtons.ToDictionary(p=>p.Key,p=>p.Value);int detached=0;foreach(var c in tree)c.DetachedFromVisualTree+=(_,_)=>detached++;
            for(int i=0;i<100;i++){MoveToQuestion(i%2);Click(answerButtons["A"]);Click(answerButtons["B"]);}
            Check(detached==0&&tree.SequenceEqual(page.Children)&&original.All(p=>ReferenceEquals(p.Value,answerButtons[p.Key])),"Exam controls rebuilt during touch/navigation");
            await Capture("04-exam");Click(Find(page,"Nộp bài"));await Task.Delay(60);Click(Find(modal,"Nộp bài"));await Task.Delay(80);
            var attempts=historyStore.Load(out _);Check(attempts.Count==1&&attempts[0].Correct==1&&attempts[0].Wrong==1,"Manual attempt/history incorrect");await Capture("05-results");
            Click(navButtons["history"]);await Capture("06-history");Click(Find(page,"Xem chi tiết"));await Task.Delay(60);
            Controls<ComboBox>(page).Single(c=>c.Name=="HistoryFilter").SelectedIndex=1;Check(Controls<Expander>(page).Count()==1,"Wrong-question filter failed");await Capture("07-wrong-answer");
            Navigate("preferences");Controls<ComboBox>(page).Single().SelectedIndex=1;var numbers=Controls<NumericUpDown>(page).ToArray();numbers[0].Value=17;numbers[1].Value=42;await Capture("08-settings");
            var keptBank=bank;var keptSetup=setup;var keptId=currentBankId;
            bank=Enumerable.Range(1,5000).Select(n=>new Question {Number=n,Text="Câu hỏi lớn "+n,Answer="B",Options=new(){{"A","Một"},{"B","Hai"}}}).ToList();setup=new(){Count=20,Minutes=30,Included=new(bank.Select(q=>q.Number))};
            AdoptBank(library.Create(BankSnapshot()));Navigate("bank");OpenLibraryBank(currentBankId.Value);questionList.BringIntoView();await Task.Delay(150);questionList.ScrollIntoView(bank[^1]);await Task.Delay(150);
            int realized=Controls<ListBoxItem>(page).Count();Check(realized>0&&realized<100,"Question bank is not virtualized: "+realized);
            AdoptBank(library.Load(keptId.Value));bank=keptBank;setup=keptSetup;setup.Count=2;setup.Minutes=30;PersistBank();Start(false);ChooseAnswer("B");MoveToQuestion(1);PersistSession();
            var checkpoint=new MobileWorkspace(dataRoot).LoadActive(out string warning);Check(warning==""&&checkpoint.Index==1&&checkpoint.Answers[0]=="B","Lifecycle checkpoint lost answer/index");
            File.WriteAllText(result,"PASS: phone-size real Avalonia UI renders all pages; embedded import, touch edit, cancel/confirm clear-all, autosaved draft, 100 answer/navigation cycles without rebuilding controls, manual submission, History/filter, Settings and incomplete-exam checkpoint.");
        }catch(Exception ex){File.WriteAllText(result,"FAIL: "+ex);}
        finally {timer.Stop();if(Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)desktop.Shutdown();}
    }
}
#endif
