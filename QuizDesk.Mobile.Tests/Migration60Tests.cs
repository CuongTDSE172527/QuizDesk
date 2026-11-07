using QuizDesk;
using System.Text.Json;
using System.Text.Json.Nodes;

partial class Tests {
    static void Migration60Checks(string folder) {
        Directory.CreateDirectory(folder);
        var options = new JsonSerializerOptions { IncludeFields = true };
        var id = Guid.NewGuid();
        var now = DateTimeOffset.Parse("2026-10-31T12:00:00+07:00");
        var questions = Reader.Parse("Câu 1. Nội dung?\nA. Một\nB. Hai\nĐáp án: B");
        var setup = new QuizSetup { Count = 1, Included = new() { 1 } };
        var bank = new LibraryBank {
            Id = id, Version = 1, CreatedAt = now, UpdatedAt = now,
            Content = new WorkspaceBank { Title = "5.x", Bank = questions, Setup = setup }
        };
        var document = JsonSerializer.SerializeToNode(bank, options);
        document["Content"]["Bank"][0].AsObject().Remove("StudyId");
        string legacy = document.ToJsonString();
        var bankPath = Path.Combine(folder, id.ToString("N") + ".json");
        File.WriteAllText(bankPath, legacy);
        var library = new MobileBankLibrary(folder);
        var migrated = library.Load(id);
        Guid identity = migrated.Content.Bank[0].StudyId;
        Check(migrated.Version == 2 && identity != Guid.Empty, "5.x bank gets stable ID");
        Check(File.ReadAllText(bankPath + ".v1.bak") == legacy, "Original bank backed up byte-for-byte");
        migrated.Content.Bank[0].Text = "Edited";
        library.Save(id, migrated.Content);
        Check(new MobileBankLibrary(folder).Load(id).Content.Bank[0].StudyId == identity, "ID survives edit/restart");
        var copy = library.Create(migrated.Content);
        Check(copy.Content.Bank[0].StudyId != identity, "Independent bank has independent review identity");
        Check(QuizSetup.Build(migrated.Content.Bank, setup, true, new Random(1))[0].StudyId == identity, "Exam retains ID");

        var active = new ActiveMobileExam {
            Version = 1, LibraryBankId = id, Exam = questions, StartedAt = now,
            Deadline = now.AddMinutes(30), Minutes = 30
        };
        File.WriteAllText(Path.Combine(folder, "active-exam.json"), JsonSerializer.Serialize(active, options));
        File.WriteAllText(Path.Combine(folder, "exam-progress.json"), JsonSerializer.Serialize(new { active.Id, Index = 0, Answers = new Dictionary<int,string> { [0] = "B" } }));
        var workspace = new MobileWorkspace(folder);
        var resumed = workspace.LoadActive(out var warning);
        Check(warning == "" && resumed.Version == 2 && resumed.Answers[0] == "B", "Old active checkpoint and answer survive upgrade");
        Check(File.Exists(Path.Combine(folder, "exam-progress.json.v1.bak")), "Original progress backed up");

        var historyDir = Path.Combine(folder, "History");
        var history = new HistoryStore(historyDir);
        var attempt = ExamAttempt.Capture("5.x", now, now.AddSeconds(30), TimeSpan.FromSeconds(30), 30, false, questions, resumed.Answers);
        attempt.Version = 1;
        history.Save(attempt);
        var upgraded = history.Load(out warning).Single();
        Check(warning == "" && upgraded.Version == 2 && upgraded.Correct == 1 && upgraded.LibraryBankId == null, "Old history upgrades without fabricated bank IDs");
        Check(File.Exists(Path.Combine(historyDir, attempt.Id.ToString("N") + ".json.v1.bak")), "Original history backed up");
        Console.WriteLine($"TOTAL {checks} migration checks passed");
    }
}
