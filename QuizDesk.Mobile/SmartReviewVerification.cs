#if QA
using Avalonia.Controls;

namespace QuizDesk.Mobile;
public partial class MainView {
    async Task<string> VerifySmartReview() {
        var source = library.Create(new WorkspaceBank {
            Title = "Lịch ôn QA",
            Bank = Reader.Parse("Câu 1. Một câu?\nA. Một\nB. Hai\nĐáp án: B"),
            Setup = new QuizSetup { Count = 1, Included = new() { 1 } }
        });
        AdoptBank(source);
        Navigate("home");
        async Task<Button> WaitButton(string name) {
            for (int i = 0; i < 80; i++) {
                var button = Controls<Button>(page).FirstOrDefault(item => Equals(item.Content, name));
                if (button != null) return button;
                await Task.Delay(40);
            }
            throw new InvalidOperationException("Missing " + name);
        }
        async Task WaitSession() {
            for (int i = 0; i < 80 && smartSession == null; i++) await Task.Delay(40);
            Check(smartSession != null, "Smart review did not open");
        }
        Click(await WaitButton("Ôn theo lịch"));
        await WaitSession();
        Click(Controls<Button>(page).Single(item => item.Name == "SmartChoiceA"));
        Click(Find(page, "Thoát lượt ôn"));
        Check(new ReviewStore(Path.Combine(dataRoot,"Reviews")).Load().Single().Rating == RecallRating.Again, "Review lost on exit");
        Click(await WaitButton("Sổ câu sai (1)"));
        await WaitSession();
        Click(Controls<Button>(page).Single(item => item.Name == "SmartChoiceB"));
        Click(Find(page,"Xong lượt ôn"));
        Click(await WaitButton("Sổ câu sai (1)"));
        await WaitSession();
        Click(Controls<Button>(page).Single(item => item.Name == "SmartChoiceB"));
        Click(Find(page,"Xong lượt ôn"));
        Check(!(await WaitButton("Sổ câu sai (0)")).IsEnabled, "Learned item remains in wrong book");
        return "PASS today card, scheduled practice, save on exit, two passes remove wrong question.";
    }
}
#endif
