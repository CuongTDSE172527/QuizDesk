using Foundation;
using UIKit;
using Avalonia;
using Avalonia.iOS;
using QuizDesk.Mobile;

namespace QuizDesk.iOS;
[Register("AppDelegate")]
public partial class AppDelegate : AvaloniaAppDelegate<App> {
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) {
        MobilePlatform.DataDirectory=Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"..","Library","Application Support","QuizDesk"));
        MobilePlatform.ShareFile=ShareFile;
        MobilePlatform.PlayVideo=PlayVideo;
        return base.CustomizeAppBuilder(builder).WithInterFont();
    }
    static Task PlayVideo(string path){
        var window=UIApplication.SharedApplication.ConnectedScenes.OfType<UIWindowScene>().SelectMany(s=>s.Windows).FirstOrDefault(w=>w.IsKeyWindow);
        var owner=window?.RootViewController??throw new InvalidOperationException("Không tìm thấy màn hình phát video.");while(owner.PresentedViewController!=null)owner=owner.PresentedViewController;
        var view=new AVKit.AVPlayerViewController {Player=AVFoundation.AVPlayer.FromUrl(NSUrl.FromFilename(path))};owner.PresentViewController(view,true,()=>view.Player.Play());return Task.CompletedTask;
    }
    static async Task<bool> ShareFile(string name,string mime,byte[] bytes) {
        string folder=Path.Combine(Path.GetTempPath(),"QuizDeskExport",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);string path=Path.Combine(folder,Path.GetFileName(name));await File.WriteAllBytesAsync(path,bytes);
        var window=UIApplication.SharedApplication.ConnectedScenes.OfType<UIWindowScene>().SelectMany(s=>s.Windows).FirstOrDefault(w=>w.IsKeyWindow);
        var controller=window?.RootViewController??throw new InvalidOperationException("Không tìm thấy màn hình để xuất file.");
        while(controller.PresentedViewController!=null)controller=controller.PresentedViewController;
        var result=new TaskCompletionSource<bool>();var activity=new UIActivityViewController(new NSObject[]{NSUrl.FromFilename(path)},null);
        activity.CompletionWithItemsHandler=(_,completed,_,error)=>{if(error!=null)result.TrySetException(new IOException(error.LocalizedDescription));else result.TrySetResult(completed);};
        if(activity.PopoverPresentationController!=null){activity.PopoverPresentationController.SourceView=controller.View;activity.PopoverPresentationController.SourceRect=new CoreGraphics.CGRect(controller.View.Bounds.Width/2,controller.View.Bounds.Height/2,1,1);}
        controller.PresentViewController(activity,true,null);return await result.Task;
    }
}
