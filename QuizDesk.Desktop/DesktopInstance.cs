namespace QuizDesk;

// Each launch stages files durably before signalling the single UI process.
internal sealed class DesktopInstance : IDisposable {
    readonly string root;
    readonly FileStream lease;
    FileSystemWatcher watcher;
    public bool IsPrimary=>lease!=null;
    public DesktopInstance(string dataRoot) {
        root=Path.GetFullPath(dataRoot);Directory.CreateDirectory(root);
        try{lease=new FileStream(Path.Combine(root,"desktop-instance.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
        catch(IOException){}
    }
    public void Listen(Action receive) {
        watcher=new FileSystemWatcher(root,"desktop-open.signal") {NotifyFilter=NotifyFilters.LastWrite|NotifyFilters.FileName};
        watcher.Created+=(_,_)=>receive();watcher.Changed+=(_,_)=>receive();
        watcher.Error+=(_,_)=>receive();watcher.EnableRaisingEvents=true;
    }
    public void NotifyPrimary()=>File.WriteAllText(Path.Combine(root,"desktop-open.signal"),Guid.NewGuid().ToString("N"));
    public void Dispose(){watcher?.Dispose();lease?.Dispose();}
}
