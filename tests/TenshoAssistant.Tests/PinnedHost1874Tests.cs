using PluginUiTest.Host;

// X-309 (0.1874): the pinned copy of PluginUiTest.Host had received stamp (X-271) and clock/HostClock (X-282) here first. They are now in
// Shigu330/PluginUiTest (main, commit e52a707, after v0.2.0) and the copy is that library again: HostClock in its own file, the stamp and
// the clock as optional arguments after 0.2.0's libraries, the desktop application calling them by name.
internal static class PinnedHost1874Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath(file)).Replace("\r\n","\n");
    private const string Host = "tools/PluginUiTest/src/PluginUiTest.Host/";

    internal static readonly (string Name,Action Run)[] Cases =
    [
        ("X-309: the copy's clock (HostClock) stands still at the instant given, and is the computer's clock without one",() =>
        {
            var at = new DateTimeOffset(2026,11,27,4,30,0,TimeSpan.FromHours(9));
            lock (typeof(HostClock))
            {
                try
                {
                    HostClock.Fixed = at;
                    Check(HostClock.UtcNow == at && HostClock.UtcNow.Offset == TimeSpan.Zero && HostClock.DateTimeUtcNow == at.UtcDateTime,"UTC reads the fixed instant");
                    Check(HostClock.Now == at && HostClock.DateTimeNow == at.UtcDateTime.ToLocalTime() && HostClock.Today == at.UtcDateTime.ToLocalTime().Date,
                          "local time is the same instant in the computer's time zone");
                    Check(HostClock.Provider.GetUtcNow() == at,"TimeProvider.System stands for the same clock");
                    HostClock.Fixed = null;
                    Check(Math.Abs((HostClock.UtcNow - DateTimeOffset.UtcNow).TotalSeconds) < 5,"without an instant, the computer's clock");
                }
                finally { HostClock.Fixed = null; }
            }
        }),
        ("X-309: the stamp and the clock are upstream's optional arguments, called by name by the desktop application; the README names the commit",() =>
        {
            var rewrite = Read(Host+"Rewrite.cs");
            Check(rewrite.Contains("IReadOnlyList<string>? libraries=null,\n        Version? stamp=null,DateTimeOffset? clock=null)",StringComparison.Ordinal),"stamp and clock after 0.2.0's libraries");
            Check(rewrite.Contains("module.Assembly.Name.Version=stamp",StringComparison.Ordinal) && rewrite.Contains("[\"System.TimeProvider::get_System\"]",StringComparison.Ordinal)
                  && !rewrite.Contains("class HostClock",StringComparison.Ordinal) && File.Exists(Path.GetFullPath(Host+"HostClock.cs")),"stamp, clock reads rewritten, HostClock in its own file");
            Check(Read("src/TenshoAssistant.Desktop/Program.cs").Contains("stamp:References.Stamp,clock:References.Clock",StringComparison.Ordinal),"the desktop application names its arguments");
            var readme = Read("tools/PluginUiTest/README.md");
            Check(readme.Split("\n---\n").All(half=>half.Contains("v0.2.0",StringComparison.Ordinal) && half.Contains("`e52a707`",StringComparison.Ordinal)),"each half gives v0.2.0 and commit e52a707");
            Check(Read(Host+"PluginUiTest.Host.csproj").Contains("v0.2.0 plus commit e52a707",StringComparison.Ordinal),"the project's comment gives the same");
        }),
    ];
}
