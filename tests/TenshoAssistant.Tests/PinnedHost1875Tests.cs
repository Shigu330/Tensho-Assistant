// X-870 (0.1875): since PluginUiTest 0.2.0 the IFramework stand-in kept the work handed to RunOnFrameworkThread, RunOnTick and Run and
// gave a task that completed only when the host ran it; the desktop application never runs it, so the tasks stayed pending for ever and
// the static list only grew. Upstream (Shigu330/PluginUiTest 602838a) keeps it only when FrameworkProxy.KeepWork is set (the test tool);
// the copy is that commit. The behaviour is checked by the desktop application's headless run (Headless.FrameworkCheck, run by Verify):
// these cases fix the wiring the test project can read (it has no Dalamud).
internal static class PinnedHost1875Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath(file)).Replace("\r\n","\n");

    internal static readonly (string Name,Action Run)[] Cases =
    [
        ("X-870: the copy's IFramework stand-in keeps work only with KeepWork; otherwise the task is the default, already complete",() =>
        {
            var standIns = Read("tools/PluginUiTest/src/PluginUiTest.Host/StandIns.cs");
            Check(standIns.Contains("public static bool KeepWork;",StringComparison.Ordinal),"KeepWork exists");
            var off = standIns.IndexOf("case \"RunOnFrameworkThread\" or \"RunOnTick\" or \"Run\" when !KeepWork:",StringComparison.Ordinal);
            var kept = standIns.IndexOf("lock(Queued)Queued.Add(",StringComparison.Ordinal);
            Check(off > 0 && kept > off,"the case without KeepWork comes first, before the queue");
            Check(standIns[off..kept].Contains("return StandInDefaults.For(method.ReturnType);",StringComparison.Ordinal),"without KeepWork: StandInDefaults (Task.CompletedTask, Task.FromResult)");
        }),
        ("X-870: the desktop application never sets KeepWork, and its headless run checks the stand-in's behaviour",() =>
        {
            foreach (var file in Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant.Desktop"),"*.cs"))
                Check(!File.ReadAllText(file).Contains("KeepWork=true",StringComparison.Ordinal) && !File.ReadAllText(file).Contains("KeepWork = true",StringComparison.Ordinal),
                      Path.GetFileName(file)+" sets KeepWork");
            var headless = Read("src/TenshoAssistant.Desktop/Headless.cs");
            var run = headless.IndexOf("public static int Run(Host host",StringComparison.Ordinal);
            Check(run > 0 && headless.IndexOf("if(FrameworkCheck() is { } framework){report.Fail(\"framework: \"+framework);return 1;}",run,StringComparison.Ordinal) > run,
                  "the headless run fails when the check fails");
            foreach (var part in new[]{"!plain.Wait(TimeSpan.Zero)","typed.Result!=0","ran!=0","FrameworkProxy.Queued.Count"})
                Check(headless.Contains(part,StringComparison.Ordinal),"the check looks at "+part);
        }),
    ];
}
