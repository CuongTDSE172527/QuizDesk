#if QA
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Avalonia.Threading;

namespace QuizDesk.Mobile;
public partial class MainView {
    static void LegacyPixelCopy(IntPtr source,int sourceStride,IntPtr destination,int destinationStride,int width,int height){
        byte[] row=new byte[width*4];
        for(int y=0;y<height;y++){
            Marshal.Copy(source+y*sourceStride,row,0,row.Length);
            Marshal.Copy(row,0,destination+y*destinationStride,row.Length);
        }
    }
    async Task<string> VerifyRenderCopy(){
        foreach(bool padded in new[]{false,true}){
            int width=7,height=3,bytes=width*4,sourceStride=bytes+(padded?4:0),destinationStride=bytes+(padded?12:0);
            byte[] input=Enumerable.Range(0,sourceStride*height).Select(i=>(byte)(i%251)).ToArray();
            byte[] initial=Enumerable.Repeat((byte)205,destinationStride*height).ToArray();
            IntPtr source=Marshal.AllocHGlobal(input.Length),destination=Marshal.AllocHGlobal(initial.Length);
            try {
                Marshal.Copy(input,0,source,input.Length);Marshal.Copy(initial,0,destination,initial.Length);
                MediaImage.CopyPixelRows(source,sourceStride,destination,destinationStride,width,height);
                var actual=new byte[initial.Length];Marshal.Copy(destination,actual,0,actual.Length);
                for(int y=0;y<height;y++){
                    Check(actual.AsSpan(y*destinationStride,bytes).SequenceEqual(input.AsSpan(y*sourceStride,bytes)),"Pixel content changed at row "+y);
                    Check(actual.AsSpan(y*destinationStride+bytes,destinationStride-bytes).ToArray().All(b=>b==205),"Pixel copy changed row padding");
                }
                bool rejected=false;try{MediaImage.CopyPixelRows(source,1,destination,destinationStride,width,height);}catch(InvalidDataException){rejected=true;}
                Check(rejected,"Invalid pixel stride was not rejected");
            } finally {Marshal.FreeHGlobal(source);Marshal.FreeHGlobal(destination);}
        }
        const int w=1024,h=768,loops=100;int stride=w*4,length=stride*h;
        IntPtr src=Marshal.AllocHGlobal(length),dst=Marshal.AllocHGlobal(length);
        double oldMs,fastMs;long oldBytes,fastBytes;
        try {
            Marshal.Copy(new byte[length],0,src,length);
            LegacyPixelCopy(src,stride,dst,stride,w,h);MediaImage.CopyPixelRows(src,stride,dst,stride,w,h);
            var watch=new Stopwatch();long allocated=GC.GetAllocatedBytesForCurrentThread();watch.Start();
            for(int i=0;i<loops;i++)LegacyPixelCopy(src,stride,dst,stride,w,h);
            watch.Stop();oldMs=watch.Elapsed.TotalMilliseconds;oldBytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
            allocated=GC.GetAllocatedBytesForCurrentThread();watch.Restart();
            for(int i=0;i<loops;i++)MediaImage.CopyPixelRows(src,stride,dst,stride,w,h);
            watch.Stop();fastMs=watch.Elapsed.TotalMilliseconds;fastBytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
            Check(fastBytes<oldBytes/10,"Direct pixel copy still allocates a row buffer per frame");
        } finally {Marshal.FreeHGlobal(src);Marshal.FreeHGlobal(dst);}
        int uiThread=Environment.CurrentManagedThreadId,beats=0;
        var heartbeat=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(10)};heartbeat.Tick+=(_,_)=>beats++;heartbeat.Start();
        foreach(bool lean in new[]{true,false}){
            int active=0,peak=0;
            var jobs=Enumerable.Range(0,8).Select(_=>MediaImage.DecodeAsync(()=>{
                int count=Interlocked.Increment(ref active),seen;
                do{seen=Volatile.Read(ref peak);}while(count>seen&&Interlocked.CompareExchange(ref peak,count,seen)!=seen);
                bool background=Environment.CurrentManagedThreadId!=uiThread;
                Thread.Sleep(25);Interlocked.Decrement(ref active);return background;
            },lean)).ToArray();
            Check((await Task.WhenAll(jobs)).All(background=>background),"Image decoding ran on the UI thread");
            Check(peak<=(lean?1:MediaImage.DecodeWorkerLimit),"Decode worker budget exceeded");
        }
        heartbeat.Stop();Check(beats>0,"Image workers blocked UI message processing");
        try{await MediaImage.DecodeAsync<int>(()=>throw new InvalidOperationException("fixture"),true);}catch(InvalidOperationException){}
        Check(await MediaImage.DecodeAsync(()=>42,true)==42,"A decoder failure leaked its worker slot");
        byte[] gif=Convert.FromBase64String("R0lGODlhCAAIAIEAAP8AAAAAAAAAAAAAACH/C05FVFNDQVBFMi4wAwEAAAAh+QQACAAAACwAAAAACAAIAAAIDwABCBxIsKDBgwgTKkwYEAAh+QQBDAABACwAAAAACAAIAIEA/wAAAAAAAAAAAAAIDwABCBxIsKDBgwgTKkwYEAA7");
        var image=new MediaImage(new QuizDesk.QuestionMedia {Name="worker.gif",Mime="image/gif",Data=gif},()=>true);
        page.Children.Clear();page.Children.Add(image);
        for(int i=0;i<50&&!image.IsImageLoaded;i++)await Task.Delay(30);
        Check(image.IsImageLoaded&&image.CurrentFrame==0,"Worker-decoded first GIF frame was not displayed");
        Click(Find(image,"▶ Phát GIF"));
        for(int i=0;i<50&&image.CurrentFrame==0;i++)await Task.Delay(20);
        Check(image.CurrentFrame>0,"GIF animation no longer advances");
        Click(Find(image,"Ⅱ Dừng GIF"));int frame=image.CurrentFrame;await Task.Delay(160);
        Check(image.CurrentFrame==frame,"GIF pause did not stop playback");
        page.Children.Clear();Check(!image.IsImageLoaded,"Image buffers did not release on detach");
        var metrics=new {Width=w,Height=h,Frames=loops,LegacyCopyMs=oldMs,DirectCopyMs=fastMs,LegacyAllocatedBytes=oldBytes,DirectAllocatedBytes=fastBytes,DecodeWorkerLimit=MediaImage.DecodeWorkerLimit};
        File.WriteAllText(Path.Combine(dataRoot,"render-copy.json"),JsonSerializer.Serialize(metrics,new JsonSerializerOptions {WriteIndented=true}));
        return "PASS native pixel copy, padded strides, no frame buffer allocation, bounded off-thread decoding, UI heartbeat, worker failure recovery, GIF first frame/play/pause/detach. Host timings only; see render-copy.json.";
    }
}
#endif
