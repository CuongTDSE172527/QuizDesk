using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Media.Transformation;
using Avalonia.Platform;

namespace QuizDesk.Mobile;
public partial class MainView {
    static readonly Lazy<Bitmap> BrandLogo=new(()=>new Bitmap(AssetLoader.Open(new Uri("avares://QuizDesk.Mobile/Assets/QuizDesk-logo.png"))));
    static readonly TransformOperations RestTransform=TransformOperations.Parse("translate(0px, 0px) scale(1, 1)");
    // Content and persistence always update synchronously. These compositor transitions never defer an action.
    void Animate(Control target,int milliseconds=140,bool flip=false) {
        target.Transitions=null;
        target.Opacity=preferences.ReducedMotion?1:0.78;
        target.RenderTransform=preferences.ReducedMotion?RestTransform:TransformOperations.Parse(flip?"translate(0px, 0px) scale(0.96, 1)":"translate(0px, 6px) scale(1, 1)");
        if(preferences.ReducedMotion)return;
        target.Transitions=new Transitions {
            new DoubleTransition {Property=Visual.OpacityProperty,Duration=TimeSpan.FromMilliseconds(milliseconds),Easing=new CubicEaseOut()},
            new TransformOperationsTransition {Property=Visual.RenderTransformProperty,Duration=TimeSpan.FromMilliseconds(milliseconds),Easing=new CubicEaseOut()}
        };
        target.Opacity=1;target.RenderTransform=RestTransform;
    }
    void StopMotion(Control target){target.Transitions=null;target.Opacity=1;target.RenderTransform=RestTransform;}
}
