using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Views;
using Android.Content.PM;
namespace QuizDesk.Android;
[Activity(Label="Video · QuizDesk",Exported=false,ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.ScreenSize)]
public sealed class VideoActivity : Activity {
    VideoView video;
    protected override void OnCreate(Bundle state){
        base.OnCreate(state);
        string path=Intent?.GetStringExtra("path");string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"QuizDeskMedia"))+System.IO.Path.DirectorySeparatorChar;
        if(path==null||!System.IO.Path.GetFullPath(path).StartsWith(folder,StringComparison.Ordinal)||!System.IO.File.Exists(path)){Finish();return;}
        var body=new LinearLayout(this){Orientation=Orientation.Vertical};body.SetBackgroundColor(global::Android.Graphics.Color.Black);
        var close=new Button(this){Text="← Trở lại bài học"};close.Click+=(_,_)=>Finish();body.AddView(close);
        video=new VideoView(this);body.AddView(video,new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent,0,1));SetContentView(body);
        var controls=new MediaController(this);controls.SetAnchorView(video);video.SetMediaController(controls);
        video.Error+=(_,e)=>{e.Handled=true;Toast.MakeText(this,"Thiết bị không giải mã được video này. Hãy đổi sang MP4 H.264/AAC.",ToastLength.Long)?.Show();Finish();};
        video.SetVideoPath(path);video.Prepared+=(_,_)=>{int at=state?.GetInt("position",0)??0;if(at>0)video.SeekTo(at);video.Start();};
    }
    protected override void OnSaveInstanceState(Bundle state){state.PutInt("position",video?.CurrentPosition??0);base.OnSaveInstanceState(state);}
    protected override void OnPause(){video?.Pause();base.OnPause();}
    protected override void OnDestroy(){video?.StopPlayback();video?.Dispose();video=null;base.OnDestroy();}
}
