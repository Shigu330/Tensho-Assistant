// 0.1224 (Verify of 0.1223: « Desktop install 1117 » failed once with « Access to the path is denied » in File.Move): update.json held
// for an instant by another program (an antivirus, the indexer, a reader without delete sharing) just after it was written. The move into
// place is now tried again (DesktopInstall.MoveWhenFree, 6 attempts, 50 ms × the attempt): a short hold is waited for and the change kept;
// a hold that lasts gives the error back, with the old state still whole and no temporary file left.
using TenshoAssistant.Desktop;

internal static class UpdateStateHeld1224Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static void InTemp(Action<string> run)
    {
        var temp = Path.Combine(Path.GetTempPath(),"tensho-held-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try { run(temp); } finally { try { Directory.Delete(temp,true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Update state held 1224: a short hold on update.json is waited for, the change kept",() => InTemp(temp =>
        {
            var own = Path.Combine(temp,"own");
            DesktopInstall.Update(own,_ => new UpdateState(Skipped:"1"));
            var held = new FileStream(DesktopInstall.StatePath(own),FileMode.Open,FileAccess.Read,FileShare.Read);
            var release = Task.Run(() => { Thread.Sleep(150); held.Dispose(); });
            var watch = System.Diagnostics.Stopwatch.StartNew();
            DesktopInstall.Update(own,state => state with { Skipped = "2" });
            release.Wait();
            Check(DesktopInstall.Load(own).Skipped == "2","the change survives the hold");
            Check(watch.ElapsedMilliseconds >= 100,"it waited for the file to be free (" + watch.ElapsedMilliseconds + " ms)");
            Check(!Directory.EnumerateFiles(own,"*.tmp").Any(),"no temporary file left");
        })),
        ("Update state held 1224: a hold that lasts gives the error back, the old state whole, nothing left behind",() => InTemp(temp =>
        {
            var own = Path.Combine(temp,"own");
            DesktopInstall.Update(own,_ => new UpdateState(Skipped:"1"));
            Exception? error = null;
            using (new FileStream(DesktopInstall.StatePath(own),FileMode.Open,FileAccess.Read,FileShare.Read))
            {
                try { DesktopInstall.Update(own,state => state with { Skipped = "2" }); }
                catch (Exception caught) { error = caught; }
            }
            Check(error is IOException or UnauthorizedAccessException,"the error comes back: " + error?.GetType().Name);
            Check(DesktopInstall.Load(own).Skipped == "1","the old state is whole");
            Check(!Directory.EnumerateFiles(own,"*.tmp").Any(),"no temporary file left");
        })),
        ("Update state held 1224: MoveWhenFree tries 6 times with a growing pause, only while the source is there",() =>
        {
            var code = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant.Desktop/DesktopInstall.cs"));
            Check(code.Contains("MoveWhenFree(temp,StatePath(own));",StringComparison.Ordinal) && !code.Contains("File.Move(temp,StatePath(own),true);",StringComparison.Ordinal),"update.json moved through it");
            Check(code.Contains("catch(Exception error) when(attempt<attempts && error is IOException or UnauthorizedAccessException && File.Exists(from))",StringComparison.Ordinal),"only the busy errors, only while the source is there");
        }),
    ];
}
