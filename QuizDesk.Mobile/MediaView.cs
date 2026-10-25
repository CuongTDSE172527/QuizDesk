using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using SkiaSharp;
namespace QuizDesk.Mobile;

// Decode only visible attachments, release native buffers when leaving a screen.
// GIF playback is explicit, so a bank with many animations never starts them all.
public sealed class MediaImage : UserControl {
    readonly QuestionMedia media;
    readonly Image image=new(){Stretch=Stretch.Uniform,MaxHeight=360,HorizontalAlignment=HorizontalAlignment.Center};
    readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap,FontSize=12};
    readonly Button play=new(){Content="▶ Phát GIF"};
    readonly DispatcherTimer timer=new();
    static long reservedPixels;long ownedPixels;
    SKCodec codec;SKBitmap pixels;SKData encoded;WriteableBitmap bitmap;int frame,generation;bool playing;
    public bool IsImageLoaded=>bitmap!=null;
    public int CurrentFrame=>frame;
    public MediaImage(QuestionMedia media){
        this.media=media;var body=new StackPanel {Spacing=6};body.Children.Add(image);body.Children.Add(status);
        if(media.Mime=="image/gif"){body.Children.Add(play);play.Click+=(_,e)=>{e.Handled=true;if(codec==null)return;playing=!playing;play.Content=playing?"Ⅱ Dừng GIF":"▶ Phát GIF";if(playing)timer.Start();else timer.Stop();};}
        Content=body;timer.Tick+=(_,_)=>{if(codec==null||!IsEffectivelyVisible){timer.Stop();playing=false;play.Content="▶ Phát GIF";return;}frame=(frame+1)%Math.Max(1,codec.FrameCount);DrawFrame();};
        AttachedToVisualTree+=async(_,_)=>await Load();DetachedFromVisualTree+=(_,_)=>Release();
    }
    async Task Load(){
        int request=++generation;status.Text="Đang đọc ảnh…";
        try{
            // Header parsing precedes pixel allocation, including decompression bombs.
            var result=await Task.Run(()=>{
                SKData data=null;SKCodec decoder=null;SKBitmap buffer=null;long reserved=0;
                try{data=SKData.CreateCopy(media.Data);decoder=SKCodec.Create(data)??throw new InvalidDataException("Không giải mã được ảnh.");var info=decoder.Info;
                    InputSafety.Require(info.Width>0&&info.Height>0&&info.Width<=8192&&info.Height<=8192&&(long)info.Width*info.Height<=12_000_000,"Ảnh quá lớn (tối đa 12 triệu pixel). Hãy giảm kích thước.");
                    InputSafety.Require(decoder.FrameCount<=500,"GIF có quá nhiều khung hình (tối đa 500).");
                    reserved=(long)info.Width*info.Height;
                    long inUse=Interlocked.Add(ref reservedPixels,reserved);InputSafety.Require(inUse<=12_000_000,"Nhiều ảnh lớn đang mở. Hãy đóng bớt các câu khác hoặc giảm kích thước ảnh.");
                    buffer=new SKBitmap(new SKImageInfo(info.Width,info.Height,SKColorType.Bgra8888,SKAlphaType.Premul));return (data,decoder,buffer,reserved);
                }catch{if(reserved>0)Interlocked.Add(ref reservedPixels,-reserved);buffer?.Dispose();decoder?.Dispose();data?.Dispose();throw;}
            });
            if(request!=generation){result.buffer.Dispose();result.decoder.Dispose();result.data.Dispose();Interlocked.Add(ref reservedPixels,-result.reserved);return;}
            ownedPixels=result.reserved;encoded=result.data;codec=result.decoder;pixels=result.buffer;frame=0;
            bitmap=new WriteableBitmap(new PixelSize(pixels.Width,pixels.Height),new Vector(96,96),Avalonia.Platform.PixelFormat.Bgra8888,Avalonia.Platform.AlphaFormat.Premul);
            image.Source=bitmap;DrawFrame();status.Text=media.Name;play.IsVisible=media.Mime=="image/gif"&&codec.FrameCount>1;
        }catch(Exception ex){if(request==generation){Release();status.Text="Không hiển thị được ảnh: "+ex.Message;}}
    }
    void DrawFrame(){
        if(codec==null||pixels==null||bitmap==null)return;
        var result=codec.GetPixels(pixels.Info,pixels.GetPixels(),new SKCodecOptions(frame));
        if(result!=SKCodecResult.Success&&result!=SKCodecResult.IncompleteInput){timer.Stop();status.Text="Không đọc được khung hình.";return;}
        byte[] row=new byte[pixels.Width*4];
        using(var target=bitmap.Lock())for(int y=0;y<pixels.Height;y++){
            System.Runtime.InteropServices.Marshal.Copy(pixels.GetPixels()+y*pixels.RowBytes,row,0,row.Length);System.Runtime.InteropServices.Marshal.Copy(row,0,target.Address+y*target.RowBytes,row.Length);
        }
        image.InvalidateVisual();if(codec.FrameCount>1)timer.Interval=TimeSpan.FromMilliseconds(Math.Clamp(codec.FrameInfo[frame].Duration,30,10000));
    }
    void Release(){generation++;if(ownedPixels>0){Interlocked.Add(ref reservedPixels,-ownedPixels);ownedPixels=0;}timer.Stop();playing=false;play.Content="▶ Phát GIF";image.Source=null;bitmap?.Dispose();bitmap=null;pixels?.Dispose();pixels=null;codec?.Dispose();codec=null;encoded?.Dispose();encoded=null;}
}
public sealed class MediaGallery : StackPanel {
    readonly Func<QuestionMedia,Task> open;
    QuestionMedia[] current=Array.Empty<QuestionMedia>();
    public MediaGallery(Func<QuestionMedia,Task> open){this.open=open;Spacing=8;}
    public void Set(IEnumerable<QuestionMedia> attachments){
        var next=attachments.ToArray();if(current.SequenceEqual(next))return;current=next;Children.Clear();IsVisible=next.Length>0;
        foreach(var media in next){
            if(media.Mime.StartsWith("image/"))Children.Add(new MediaImage(media));
            else {var button=new Button {Content="▶ Xem video · "+media.Name,HorizontalAlignment=HorizontalAlignment.Stretch,HorizontalContentAlignment=HorizontalAlignment.Left};button.Click+=async(_,e)=>{e.Handled=true;await open(media);};Children.Add(button);}
        }
    }
}
public partial class MainView {
    MediaGallery questionMedia,noteMedia;
    readonly Dictionary<string,MediaGallery> choiceMedia=new();
    MediaGallery Gallery(IEnumerable<QuestionMedia> media=null){var view=new MediaGallery(OpenMedia);view.Set(media??Array.Empty<QuestionMedia>());return view;}
    async Task OpenMedia(QuestionMedia media){
        try{
            MediaFiles.Check(new[]{media});string folder=Path.Combine(Path.GetTempPath(),"QuizDeskMedia");Directory.CreateDirectory(folder);
            // Only content-derived names are written; document names never become paths.
            foreach(string old in Directory.EnumerateFiles(folder))if(File.GetLastWriteTimeUtc(old)<DateTime.UtcNow.AddDays(-1))try{File.Delete(old);}catch(IOException){}
            string path=Path.Combine(folder,MediaFiles.Fingerprint(media)+MediaFiles.Extension(media.Mime));if(!File.Exists(path))await File.WriteAllBytesAsync(path,media.Data);
            if(MobilePlatform.PlayVideo!=null){await MobilePlatform.PlayVideo(path);return;}
            var file=await Storage.TryGetFileFromPathAsync(new Uri(path));if(file==null||!await Host.Launcher.LaunchFileAsync(file))Notify("Không mở được video. Hãy cài trình phát hỗ trợ "+media.Mime+".",true);
        }catch(Exception ex){Notify("Không mở được video: "+ex.Message,true);}
    }
    void AddMediaEditor(StackPanel body,List<QuestionMedia> attachments,Question original){
        var target=new ComboBox {ItemsSource=new[]{"Câu hỏi","Giải thích / đáp án","Lựa chọn A","Lựa chọn B","Lựa chọn C","Lựa chọn D","Lựa chọn E","Lựa chọn F","Lựa chọn G","Lựa chọn H"},SelectedIndex=0,HorizontalAlignment=HorizontalAlignment.Stretch};
        var list=new StackPanel {Spacing=8};var error=Text("",13);
        void Fill(){list.Children.Clear();foreach(var m in attachments.ToArray()){
            string location=m.Target=="question"?"Câu hỏi":m.Target=="note"?"Giải thích":"Lựa chọn "+m.Target;
            list.Children.Add(Row(Text($"{location} · {m.Name} · {m.Data.Length/1024.0:0.#} KB",13),Btn("Xóa đính kèm",()=>{attachments.Remove(m);Fill();})));
        }}
        var add=Btn("＋ Thêm ảnh / GIF / video",async()=>{
            try{
                var files=await Storage.OpenFilePickerAsync(new FilePickerOpenOptions {Title="Đính kèm media",AllowMultiple=true,FileTypeFilter=new[]{new FilePickerFileType("Ảnh / GIF / video"){Patterns=new[]{"*.png","*.jpg","*.jpeg","*.webp","*.gif","*.mp4","*.webm"},MimeTypes=new[]{"image/png","image/jpeg","image/webp","image/gif","video/mp4","video/webm"}}}});
                string slot=target.SelectedIndex==0?"question":target.SelectedIndex==1?"note":((char)('A'+target.SelectedIndex-2)).ToString();var pending=new List<QuestionMedia>();
                foreach(var file in files){InputSafety.Require(attachments.Count+pending.Count<16,"Mỗi câu tối đa 16 media.");await using var input=await file.OpenReadAsync();pending.Add(await MediaFiles.ReadAsync(input,file.Name,slot));}
                MediaFiles.Check(bank.Where(q=>!ReferenceEquals(q,original)).SelectMany(q=>q.Media).Concat(attachments).Concat(pending));attachments.AddRange(pending);error.Text="";Fill();
            }catch(Exception ex){error.Text=ex.Message;}
        });
        body.Children.Add(Text("Ảnh, GIF & video",19));body.Children.Add(Supporting("PNG/JPEG/WebP/GIF: tối đa 8 MB/file. MP4/WebM: tối đa 16 MB/file; tổng 24 MB/bộ. Video phát bằng trình phát trên thiết bị; bộ đếm giờ thi vẫn tiếp tục.",12));body.Children.Add(Field("Vị trí đính kèm",target));body.Children.Add(add);body.Children.Add(error);body.Children.Add(list);Fill();
    }
}
