using Avalonia.Controls;
using QuizDesk;
namespace QuizDesk.Mobile;
public partial class MainView {
    async void ChooseExport(){
        if(busy||active||studying)return;
        var dialog=Dialog("Xuất bộ đề");var formats=QuizExports.Formats;
        var choice=new ComboBox {Name="QuizExportFormat",ItemsSource=formats.Select(f=>f.Label).ToArray(),SelectedIndex=0,HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Stretch};
        var answers=new CheckBox {Content="Kèm đáp án và giải thích",IsChecked=true,IsEnabled=false};var notice=Supporting("");
        void Update(){var f=formats[choice.SelectedIndex];answers.IsEnabled=f.Id!="quiz";notice.Text=f.Id=="quiz"?"Giữ đầy đủ nội dung và cấu hình bài thi để mở lại trong QuizDesk.":f.Id=="docx"?"Word: kèm ảnh PNG/JPEG/GIF. GIF có thể hiển thị ảnh tĩnh; video/WebP được ghi chú tên file, không nhúng. Dùng .quiz để sao lưu đủ media.":f.Id=="xlsx"?"Excel: chỉ xuất chữ, đáp án và giải thích; không kèm ảnh/video. Mỗi ô tối đa 32.767 ký tự. Các ô là văn bản, không chạy công thức.":f.Media?"Giữ media nhúng. JSON phù hợp nhập lại; HTML để xem và in offline.":"Chỉ xuất chữ, không kèm ảnh/video. CSV/TSV bảo vệ ô có thể bị Excel hiểu thành công thức; nội dung ô đó sẽ có dấu nháy đơn ở đầu.";}
        choice.SelectionChanged+=(_,_)=>Update();Update();
        var body=Stack(Text("Xuất bộ đề",23),Field("Định dạng",choice),answers,notice,Row(Btn("Hủy",()=>dialog.Close(false)),Btn("Chọn nơi lưu",()=>dialog.Close(true),true)));dialog.Content=body;
        if(!await dialog.ShowDialog<bool>(this))return;
        var format=formats[choice.SelectedIndex];if(format.Id=="quiz"){Save();return;}
        busy=true;shell.IsEnabled=false;
        try{
            bool include=answers.IsChecked==true;var snapshot=bank.ToList();string name=title;
            var bytes=await Task.Run(()=>QuizExports.Write(format.Id,snapshot,name,include));
            if(await ExportFile(SafeName(title)+"."+format.Extension,format.Mime,bytes))Notify("Đã xuất "+format.Label+". Dùng .quiz để sao lưu đầy đủ cấu hình bài thi.");
        }catch(Exception ex){Notify("Chưa xuất được: "+ex.Message,true);}finally{busy=false;shell.IsEnabled=true;IncomingChanged();}
    }
}
