#if QA
using Avalonia.Controls;

namespace QuizDesk.Mobile;

public partial class MainView {
    async Task<string> VerifyLibraryDiscovery() {
        WorkspaceBank Fixture(string name) => new() {
            Title = name,
            Bank = Reader.Parse("Câu 1. Kết quả?\nA. 1\nB. 2\nĐáp án: B"),
            Setup = new QuizSetup { Count = 1, Included = new() { 1 } }
        };
        var first = library.Create(Fixture("Đề khí tượng"), "Khí tượng.docx");
        var second = library.Create(Fixture("Dẫn đường"), "VOR-test.docx");
        AdoptBank(first);
        RememberSelectedBank();
        Navigate("bank");
        var search = Controls<TextBox>(page).Single(control => control.Name == "LibrarySearch");
        search.Text = "vor-TEST";
        await Task.Delay(120);
        Check(!Controls<Button>(page).Any(button => button.Name == "LibraryBank" + first.Id.ToString("N")),
            "Search by source did not hide the other bank");
        Click(Find(page, "Ôn bộ đề"));
        Check(active && practice && currentBankId == second.Id, "Quick practice opened the wrong bank");
        ChooseAnswer("B");
        ExitPractice();
        Navigate("bank");
        Click(Find(page, "Ôn bộ đề"));
        Check(active && answers.GetValueOrDefault(0) == "B", "Quick practice did not resume saved answers");
        ExitPractice();
        Navigate("bank");
        search = Controls<TextBox>(page).Single(control => control.Name == "LibrarySearch");
        search.Text = "de khi tuong";
        await Task.Delay(120);
        Check(Controls<Button>(page).Any(button => button.Name == "LibraryBank" + first.Id.ToString("N")),
            "Unaccented Vietnamese query did not match");
        Click(Find(page, "Thi thử"));
        Check(!active && currentPage == "setup" && currentBankId == first.Id, "Mock setup opened the wrong bank");
        Navigate("bank");
        Click(Find(page, "Chọn bộ đề"));
        Check(librarySelections.Count == 2, "Bulk selection inherited a hidden search filter");
        return "PASS search by source and Vietnamese title, direct practice and resume, mock setup, bulk selection isolation.";
    }
}
#endif
