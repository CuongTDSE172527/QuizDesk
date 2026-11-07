#if QA
using System.Text.Json;
using Avalonia.Controls;

namespace QuizDesk.Mobile;

public partial class MainView {
    async Task<string> VerifyMigration60() {
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();
        var legacy = new LibraryBank {
            Version = 1, Id = id, CreatedAt = now, UpdatedAt = now,
            Content = new WorkspaceBank {
                Title = "Bộ đề từ 5.x",
                Bank = Reader.Parse("Câu 1. Câu cũ?\nA. Một\nB. Hai\nĐáp án: B"),
                Setup = new QuizSetup { Count = 1, Included = new() { 1 } }
            }
        };
        string folder = Path.Combine(dataRoot, "Library");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, id.ToString("N") + ".json");
        File.WriteAllText(path, JsonSerializer.Serialize(legacy, new JsonSerializerOptions { IncludeFields = true }));
        Navigate("bank");
        await Task.Delay(100);
        var tile = Controls<Button>(page).Single(control => control.Name == "LibraryBank" + id.ToString("N"));
        Click(tile);
        Check(currentBankId == id && bank[0].StudyId != Guid.Empty && File.Exists(path + ".v1.bak"), "Opening a legacy library did not migrate and back up");
        Start(true);
        ChooseAnswer("B");
        Check(questionFeedback.IsVisible && exam[0].StudyId == bank[0].StudyId, "Migrated question is not usable in practice");
        ExitPractice();
        return "PASS desktop opens legacy bank, backs up, assigns stable ID and practices migrated question.";
    }
}
#endif
