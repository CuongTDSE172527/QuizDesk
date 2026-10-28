using Avalonia.Controls;

namespace QuizDesk.Mobile;
public partial class MainView {
    Control glassSettingsPanel;
    ToggleSwitch motionSetting;
    bool ReduceVisualMotion=>preferences.ReducedMotion||preferences.PerformanceMode;

    Control PerformanceSettings(){
        var toggle=new ToggleSwitch {Name="PerformanceMode",Content="Performance Mode",IsChecked=preferences.PerformanceMode};
        toggle.IsCheckedChanged+=(_,_)=>{
            preferences.PerformanceMode=toggle.IsChecked==true;
            ApplyPerformanceMode();SavePreferences();
        };
        return Card(Stack(toggle,Supporting("Dành cho máy cấu hình yếu: ảnh được xử lý lần lượt, tạm tắt kính/blur và chuyển động; đồng hồ trên thanh đầu hiển thị giờ/phút. Đồng hồ thi vẫn chạy bình thường. Tắt để khôi phục các tùy chỉnh giao diện đã lưu.")));
    }

    void ApplyPerformanceMode(){
        // Performance is an override, never a destructive rewrite of visual preferences.
        FlushGlassSettings();
        UpdateAero();UpdateHeaderDateTime();
        if(glassSettingsPanel!=null)glassSettingsPanel.IsEnabled=!preferences.PerformanceMode;
        if(motionSetting!=null)motionSetting.IsEnabled=!preferences.PerformanceMode;
        if(ReduceVisualMotion){
            StopMotion(page);
            if(questionText!=null)StopMotion(questionText);
            if(flashCard!=null)StopMotion(flashCard);
            foreach(var marker in answerMarkers.Values)StopMotion(marker);
        }
    }
}
