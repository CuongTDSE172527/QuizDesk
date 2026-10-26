using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace QuizDesk.Mobile;
public partial class MainView {
    async void MergeSelectedBanks(){
        if(active||studying||busy||selectedBanks.Count<2||!PersistBank())return;
        var items=libraryItems.Where(b=>selectedBanks.Contains(b.Id)).ToArray();var ids=items.Select(b=>b.Id).ToArray();
        var dialog=Dialog("Trích câu & gộp đề");dialog.Height=720;
        var name=new TextBox {Name="MergedBankTitle",Text="Bộ đề tổng hợp",MaxLength=200};
        var shuffle=new ToggleSwitch {Name="MergeShuffle",Content="Trộn thứ tự câu hỏi",IsChecked=true};
        var goExam=new CheckBox {Content="Mở thiết lập thi thử sau khi tạo",IsChecked=true};
        var inputs=new Dictionary<Guid,NumericUpDown>();var rows=new StackPanel {Spacing=12};var total=Supporting("");var error=Supporting("");
        void UpdateTotal()=>total.Text=$"Tổng số câu lấy: {inputs.Values.Sum(v=>v.Value??0):N0} / tối đa 5.000";
        foreach(var item in items){
            var count=new NumericUpDown {Name="TakeFromBank_"+item.Id.ToString("N"),Minimum=0,Maximum=item.Count,Increment=1,FormatString="0",Value=item.Count};
            inputs[item.Id]=count;count.ValueChanged+=(_,_)=>UpdateTotal();rows.Children.Add(Field(item.Title+$" · có {item.Count} câu",count));
        }
        UpdateTotal();LibraryBank result=null;
        var cancel=Btn("Hủy",()=>dialog.Close(false));Button create=null;
        create=Btn("Tạo bộ đề gộp",async()=>{
            if(busy)return;string next=name.Text?.Trim()??"";
            if(next==""){error.Text="Hãy nhập tên bộ đề mới.";return;}
            if(inputs.Values.Any(v=>v.Value==null||v.Value!=decimal.Truncate(v.Value.Value))){error.Text="Số câu phải là số nguyên.";return;}
            var counts=inputs.ToDictionary(p=>p.Key,p=>decimal.ToInt32(p.Value.Value??0));
            if(counts.Values.Sum(v=>(long)v) is <1 or >5000){error.Text="Tổng số câu trích phải từ 1 đến 5.000.";return;}
            busy=true;create.IsEnabled=false;cancel.IsEnabled=false;name.IsEnabled=false;shuffle.IsEnabled=false;rows.IsEnabled=false;goExam.IsEnabled=false;
            error.Text="Đang trích ngẫu nhiên và tạo bộ đề…";
            try{
                bool mix=shuffle.IsChecked==true;
                var defaults=new AppPreferences {QuestionCount=preferences.QuestionCount,Minutes=preferences.Minutes};
                result=await Task.Run(()=>library.Merge(ids,next,mix,defaults,counts:counts));dialog.Close(true);
            }catch(Exception ex){error.Text="Chưa tạo được bộ đề: "+ex.Message;}
            finally{busy=false;create.IsEnabled=true;cancel.IsEnabled=true;name.IsEnabled=true;shuffle.IsEnabled=true;rows.IsEnabled=true;goExam.IsEnabled=true;}
        },true);
        var body=Stack(Text("Trích câu từ từng bộ",23),Field("Tên bộ đề mới",name),
            Supporting("Nhập số câu lấy ở mỗi bộ. Đặt 0 để bỏ qua bộ đó. Nếu lấy ít hơn toàn bộ, app chọn ngẫu nhiên không lặp trong mỗi bộ."),rows,total,shuffle,goExam,
            Supporting("Câu được đánh số lại từ 1; đáp án, giải thích và media đi theo câu. Nhật ký giữ bộ nguồn/số câu gốc. Bộ gốc được giữ nguyên."),error);
        var actions=Row(cancel,create);var layout=new DockPanel();DockPanel.SetDock(actions,Dock.Bottom);layout.Children.Add(actions);layout.Children.Add(new ScrollViewer {Content=body,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});dialog.Content=layout;
        if(!await dialog.ShowDialog<bool>(this)||result==null)return;
        AdoptBank(result);selectedBanks.Clear();selectingBanks=false;expandedBankId=result.Id;RememberSelectedBank();Navigate(goExam.IsChecked==true?"setup":"bank");
        Notify($"Đã tạo “{title}” · {bank.Count} câu. Thi thử mặc định dùng đủ các câu đã trích; nhật ký giữ số câu gốc. Tạo đề mới để bốc một lượt ngẫu nhiên khác.");
    }
}
