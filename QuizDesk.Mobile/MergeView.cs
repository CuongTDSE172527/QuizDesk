using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace QuizDesk.Mobile;
public partial class MainView {
    async void MergeSelectedBanks(){
        if(active||studying||busy||selectedBanks.Count<2||!PersistBank())return;
        var items=libraryItems.Where(b=>selectedBanks.Contains(b.Id)).ToArray();var ids=items.Select(b=>b.Id).ToArray();
        var dialog=Dialog("Gộp bộ đề");dialog.Height=680;
        var name=new TextBox {Name="MergedBankTitle",Text="Bộ đề gộp",MaxLength=200};
        var shuffle=new ToggleSwitch {Name="MergeShuffle",Content="Trộn thứ tự câu hỏi",IsChecked=true};var error=Supporting("");
        LibraryBank result=null;
        var cancel=Btn("Hủy",()=>dialog.Close(false));Button create=null;
        create=Btn("Tạo bộ đề gộp",async()=>{
            if(busy)return;string next=name.Text?.Trim()??"";
            if(next==""){error.Text="Hãy nhập tên bộ đề mới.";return;}
            busy=true;create.IsEnabled=false;cancel.IsEnabled=false;name.IsEnabled=false;shuffle.IsEnabled=false;
            error.Text="Đang tạo bộ đề…";
            try{
                bool mix=shuffle.IsChecked==true;
                var defaults=new AppPreferences {QuestionCount=preferences.QuestionCount,Minutes=preferences.Minutes};
                result=await Task.Run(()=>library.Merge(ids,next,mix,defaults));dialog.Close(true);
            }catch(Exception ex){error.Text="Chưa gộp được bộ đề: "+ex.Message;}
            finally{busy=false;create.IsEnabled=true;cancel.IsEnabled=true;name.IsEnabled=true;shuffle.IsEnabled=true;}
        },true);
        var actions=Row(cancel,create);
        var body=Stack(Text("Gộp bộ đề",23),Supporting($"{items.Length} bộ đề · {items.Sum(b=>(long)b.Count):N0} câu hỏi"),
            Supporting(string.Join("\n",items.Select(b=>$"• {b.Title}: {b.Count} câu"))),Field("Tên bộ đề mới",name),shuffle,
            Supporting("Lấy toàn bộ câu hỏi và đánh số lại từ 1. Đáp án, giải thích và cảnh báo đi theo từng câu. Bộ gốc được giữ nguyên."),error);
        var layout=new DockPanel();DockPanel.SetDock(actions,Dock.Bottom);layout.Children.Add(actions);layout.Children.Add(new ScrollViewer {Content=body,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});dialog.Content=layout;
        if(!await dialog.ShowDialog<bool>(this)||result==null)return;
        AdoptBank(result);selectedBanks.Clear();selectingBanks=false;expandedBankId=result.Id;RememberSelectedBank();Navigate("bank");
        Notify($"Đã tạo “{title}” từ {items.Length} bộ đề · {bank.Count} câu. Mở Học & thi để ôn tập hoặc làm bài; nhật ký đã lưu nguồn và thứ tự câu.");
    }
}
