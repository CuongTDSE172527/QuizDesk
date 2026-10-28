#if QA
using System.Diagnostics;
using System.Text.Json;

namespace QuizDesk.Mobile;
public partial class MainView {
    async Task<string> VerifyPerformance(){
        bank=Enumerable.Range(1,1000).Select(n=>new Question {Number=n,Text="Câu đo hiệu năng "+n,Answer="B",Options=new(){{"A","Một"},{"B","Hai"}}}).ToList();
        setup=new(){Count=1000,Minutes=30,ShuffleQuestions=false,ShuffleOptions=false,Included=new(bank.Select(q=>q.Number))};title="Performance fixture";
        AdoptBank(library.Create(BankSnapshot()));RememberSelectedBank();
        long memory=GC.GetTotalAllocatedBytes(true);var watch=Stopwatch.StartNew();Start(false);page.UpdateLayout();watch.Stop();double start=watch.Elapsed.TotalMilliseconds;
        var heading=questionText;var buttons=answerButtons.ToDictionary(p=>p.Key,p=>p.Value);
        watch.Restart();for(int i=0;i<100;i++){MoveToQuestion((i*7)%exam.Count);ChooseAnswer("A");}watch.Stop();
        Check(ReferenceEquals(heading,questionText)&&buttons.All(p=>ReferenceEquals(p.Value,answerButtons[p.Key])),"Performance path rebuilt answer controls");
        var saved=workspace.LoadActive(out string warning);Check(warning==""&&saved.Index==index&&saved.Answers.Count==answers.Count,"Optimized interaction lost persisted state");
        var result=new {PerformanceMode=preferences.PerformanceMode,Questions=1000,JumpControls=jumpButtons.Count,StartAndLayoutMs=start,Interaction100Ms=watch.Elapsed.TotalMilliseconds,AllocatedMiB=(GC.GetTotalAllocatedBytes(true)-memory)/1048576.0};
        File.WriteAllText(Path.Combine(dataRoot,"performance.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions {WriteIndented=true}));
        timer.Stop();active=false;await Task.Delay(180);
        return "PASS 1,000-question benchmark; 100 persisted moves/answers; stable answer controls and restored final state. Host measurements only; see performance.json.";
    }
}
#endif
