using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Platform.Storage;
using QuizDesk;

namespace QuizDesk.Mobile;
public partial class MainView {
    async Task ImportMany(IReadOnlyList<IStorageFile> files) {
        var dialog=Dialog("Nhập nhiều file");dialog.Height=640;
        var mode=new ComboBox {Name="BatchImportMode",ItemsSource=new[]{"Tách riêng — theo file và môn trong Word","Gộp tất cả thành một bộ đề"},SelectedIndex=0,HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Stretch};
        var name=new TextBox {Name="BatchImportTitle",Text="Bộ đề tổng hợp",MaxLength=200,IsEnabled=false};
        var shuffle=new ToggleSwitch {Name="BatchImportShuffle",Content="Trộn thứ tự câu hỏi sau khi gộp",IsChecked=false,IsEnabled=false};
        mode.SelectionChanged+=(_,_)=>{name.IsEnabled=shuffle.IsEnabled=mode.SelectedIndex==1;};
        var error=Supporting("");bool combine=false,mix=false;string mergedTitle="";
        var cancel=Btn("Hủy",()=>dialog.Close(false));
        var start=Btn("Nhập các file",()=>{
            combine=mode.SelectedIndex==1;mix=shuffle.IsChecked==true;mergedTitle=name.Text?.Trim()??"";
            if(combine&&mergedTitle.Length==0){error.Text="Hãy nhập tên bộ đề tổng hợp.";return;}
            dialog.Close(true);
        },true);
        var body=Stack(Text("Nhập nhiều file",23),Supporting($"Đã chọn {files.Count} file"),
            Supporting(string.Join("\n",files.Select((f,i)=>$"{i+1}. {f.Name}"))),Field("Cách tạo bộ đề",mode),Field("Tên bộ đề tổng hợp",name),shuffle,
            Supporting("Word tổng hợp được tách theo môn và bỏ qua mục lục. Đọc và ghép đáp án trong từng môn trước, sau đó mới gộp. Bộ tổng hợp đánh số từ 1; nhật ký giữ tên nguồn và số câu gốc. Không tự xóa các câu có nội dung giống nhau."),
            Supporting("Nếu gộp: chỉ tạo bộ đề khi mọi file đọc thành công (tối đa 5.000 câu). Nếu tách: file lỗi được báo riêng, các file hợp lệ vẫn được lưu."),error);
        var layout=new DockPanel();var actions=Row(cancel,start);DockPanel.SetDock(actions,Dock.Bottom);layout.Children.Add(actions);
        layout.Children.Add(new ScrollViewer {Content=body,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});dialog.Content=layout;
        if(!await dialog.ShowDialog<bool>(this)||!PersistBank())return;
        busy=true;shell.IsEnabled=false;navigation.IsEnabled=false;
        var staged=new List<ImportedBank>();var report=new List<string>();LibraryBank latest=null;int savedCount=0;
        var defaults=new AppPreferences {QuestionCount=preferences.QuestionCount,Minutes=preferences.Minutes};
        long characters=0;int count=0,completedFiles=0;
        try {
            for(int i=0;i<files.Count;i++) {
                string path=null;var file=files[i];Notify($"Đang đọc {i+1}/{files.Count}: {file.Name}");
                try {
                    await using(var stream=await file.OpenReadAsync())path=await MobileImportCache.CopyAsync(stream,file.Name,Path.Combine(dataRoot,"ImportCache"));
                    var imports=await Task.Run(()=>{
                        var items=Importers.LoadBanks(path,file.Name);
                        foreach(var item in items)if(!Path.GetExtension(file.Name).Equals(".quiz",StringComparison.OrdinalIgnoreCase))item.Content.Setup=defaults.CreateSetup(item.Content.Bank);
                        return items;
                    });
                    foreach(var imported in imports){
                        var content=imported.Content;
                        if(combine) {
                            count+=content.Bank.Count;
                            characters+=content.Bank.Sum(q=>(long)q.Text.Length+q.Note.Length+q.Options.Sum(o=>(long)o.Value.Length)+q.Answer.Length+(q.OriginalAnswer?.Length??0)+(q.AnswerSource?.Length??0)+(q.ImportWarnings?.Sum(w=>(long)w.Length)??0));
                            InputSafety.Require(count<=InputSafety.MaxQuestions,"Bộ tổng hợp vượt quá 5.000 câu; chưa tạo bộ đề. Hãy chọn ít file hơn.");
                            InputSafety.Require(characters<=InputSafety.MaxTextCharacters,"Nội dung bộ tổng hợp quá lớn; chưa tạo bộ đề.");
                            MediaFiles.Check(staged.SelectMany(b=>b.Content.Bank).Concat(content.Bank).SelectMany(q=>q.Media));
                            staged.Add(imported);
                        } else {
                            var document=await Task.Run(()=>library.Create(content,imported.SourceName,true));latest=document;savedCount++;
                        }
                        report.Add($"✓ {imported.SourceName}: {content.Bank.Count} câu · {content.Bank.Count(QuestionReview.RequiresReview)} câu cần sửa/bổ sung");
                    }
                    completedFiles++;
                } catch(Exception ex) {
                    report.Add($"✕ {file.Name}: {ex.Message}");
                    if(combine){if(i+1<files.Count)report.Add($"Chưa đọc {files.Count-i-1} file còn lại do lỗi ở file trên.");break;} // Never silently omit a failed source.
                } finally {if(path!=null&&File.Exists(path))File.Delete(path);}
            }
            if(combine&&completedFiles==files.Count) {
                var document=await Task.Run(()=>library.ImportCombined(staged,mergedTitle,mix,defaults));latest=document;savedCount++;
            }
        } catch(Exception ex){report.Add("Chưa hoàn tất: "+ex.Message);}
        finally{busy=false;navigation.IsEnabled=true;shell.IsEnabled=true;IncomingChanged();}
        if(savedCount>0){AdoptBank(latest);selectedBanks.Clear();selectingBanks=false;expandedBankId=null;RememberSelectedBank();Navigate("bank");}
        string summary=combine?(savedCount==1?$"Đã tạo bộ đề tổng hợp: {latest.Content.Bank.Count} câu.":"Chưa tạo bộ đề tổng hợp. Các bộ đề đã có được giữ nguyên."):$"Đã lưu {savedCount} bộ đề từ {completedFiles}/{files.Count} file vào thư viện.";
        Notify(summary,savedCount==0);
        var resultDialog=Dialog("Kết quả nhập file");resultDialog.Height=600;
        var close=Btn("Đóng",()=>resultDialog.Close(true),true);var resultLayout=new DockPanel();DockPanel.SetDock(close,Dock.Bottom);resultLayout.Children.Add(close);
        resultLayout.Children.Add(new ScrollViewer {Content=Stack(Text(summary,20),Supporting(string.Join("\n\n",report))),HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});
        resultDialog.Content=resultLayout;await resultDialog.ShowDialog<bool>(this);
    }
}
