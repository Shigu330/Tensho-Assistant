// 0.993: the gil journal is read off the frame (it took 128 ms on the frame at each load); nothing is recorded before it is there.
internal static class GilLoad993Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gil load 993: read in a task, waited for before any change is recorded, a failed read never written over",() =>
        {
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Inventory/GilWatcher.Dalamud.cs"));
            Check(watcher.Contains("loading=id==0 ? null:Task.Run(()=>GilJournal.Prune(GilJournal.Read(path),now));"),"task");
            Check(watcher.Contains("if(!read.IsCompleted)return;") && watcher.Contains("else{loadFailed=true;"),"waited, failure kept");
        }),
    ];
}
