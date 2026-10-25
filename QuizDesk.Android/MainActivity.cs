using Android.App;
using Android.Content.PM;
using Android.Content;
using AndroidIntent=Android.Content.Intent;
using Android.OS;
using Android.Provider;
using Android.Views;
using Avalonia.Android;
using QuizDesk.Mobile;

namespace QuizDesk.Android;
[Activity(Label="QuizDesk",Theme="@style/MyTheme.NoActionBar",Icon="@mipmap/ic_launcher",MainLauncher=true,Exported=true,
    LaunchMode=LaunchMode.SingleTask,
    WindowSoftInputMode=SoftInput.AdjustResize,
    ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.ScreenSize|ConfigChanges.UiMode|ConfigChanges.SmallestScreenSize|ConfigChanges.Density)]
// Content providers can use vendor-specific MIME types for unknown extensions.
// A separate type-less filter is required: */* still does not match a null type.
// Paths can be opaque IDs, so validate the display name/content after selection.
[IntentFilter(new[]{Intent.ActionView},Categories=new[]{Intent.CategoryDefault,Intent.CategoryBrowsable,Intent.CategoryOpenable},DataSchemes=new[]{"content","file"},DataMimeType="*/*")]
[IntentFilter(new[]{Intent.ActionView},Categories=new[]{Intent.CategoryDefault,Intent.CategoryBrowsable,Intent.CategoryOpenable},DataSchemes=new[]{"content","file"})]
[IntentFilter(new[]{Intent.ActionSend},Categories=new[]{Intent.CategoryDefault},DataMimeType="*/*")]
public class MainActivity : AvaloniaMainActivity {
    protected override void OnCreate(Bundle savedInstanceState){base.OnCreate(savedInstanceState);MobilePlatform.PlayVideo=path=>{StartActivity(new Intent(this,typeof(VideoActivity)).PutExtra("path",path));return Task.CompletedTask;};if(savedInstanceState==null)ReceiveIntent(Intent);}
    protected override void OnNewIntent(AndroidIntent intent){base.OnNewIntent(intent);Intent=intent;ReceiveIntent(intent);}
    async void ReceiveIntent(AndroidIntent intent) {
        if(intent?.Action!=AndroidIntent.ActionView&&intent?.Action!=AndroidIntent.ActionSend)return;
        try {
#pragma warning disable CS0618
            var uri=intent.Action==AndroidIntent.ActionSend?intent.GetParcelableExtra(AndroidIntent.ExtraStream) as global::Android.Net.Uri:intent.Data;
#pragma warning restore CS0618
            uri??=intent.ClipData?.ItemCount>0?intent.ClipData.GetItemAt(0)?.Uri:null;
            if(uri!=null&&uri.Scheme!="content"&&uri.Scheme!="file")throw new IOException("Chỉ mở file từ bộ nhớ hoặc ứng dụng cung cấp file.");
            if(uri==null)throw new IOException("File đính kèm không có địa chỉ để đọc.");
            string name=null;
            try{using var cursor=ContentResolver.Query(uri,new[]{IOpenableColumns.DisplayName},null,null,null);if(cursor?.MoveToFirst()==true){int col=cursor.GetColumnIndex(IOpenableColumns.DisplayName);if(col>=0)name=cursor.GetString(col);}}
            catch(global::Java.Lang.Exception){} // Some providers offer streams without metadata queries.
            name=IncomingQuizFileName.Resolve(name,uri.LastPathSegment);
            using var stream=ContentResolver.OpenInputStream(uri)??throw new IOException("Không có quyền đọc file. Hãy lưu về Files rồi mở lại.");
            await MobilePlatform.ReceiveQuiz(stream,name);
        }catch(Exception ex){MobilePlatform.IncomingError="Không mở được file: "+ex.Message;MobilePlatform.SignalIncoming();}
    }
    protected override void OnPause(){(Avalonia.Application.Current as App)?.CurrentView?.PersistSession();base.OnPause();}
    protected override void OnResume(){base.OnResume();(Avalonia.Application.Current as App)?.CurrentView?.ResumeSession();}
}
