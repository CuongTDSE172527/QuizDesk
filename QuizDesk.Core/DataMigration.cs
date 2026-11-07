namespace QuizDesk;

internal static class DataMigration {
    internal static void BackupV1(string path) {
        string backup = path + ".v1.bak";
        if (!File.Exists(path) || File.Exists(backup)) return;
        string temporary = backup + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try {
            using (var input = File.OpenRead(path))
            using (var output = File.Create(temporary)) {
                input.CopyTo(output);
                output.Flush(true);
            }
            try { File.Move(temporary, backup); }
            catch (IOException) when (File.Exists(backup)) { }
        } finally {
            File.Delete(temporary);
        }
    }
}
