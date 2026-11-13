// 0.1225 (X-153, first slice, GPT review of 11/11): the plugin's wiring is split by family, the desktop family first. Each service of the
// family (the local link with the desktop app, its installer) has exactly one install path and one release path across every Plugin*.cs
// file, both in Plugin.Desktop.cs, inside the plugin's registrations (released in reverse order; a failure does not stop
// the others); Plugin.cs calls the family once and creates none of them.
using System.Text.RegularExpressions;

internal static class DesktopFamily1225Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static (string Name,string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"Plugin*.cs")
        .Select(path => (Path.GetFileName(path),File.ReadAllText(path).Replace("\r\n","\n"))).ToArray();

    private static int Count(string text,string pattern) => Regex.Matches(text,pattern).Count;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Desktop family 1224: one install and one release path for each service, in Plugin.Desktop.cs only",() =>
        {
            var files = PluginFiles();
            Check(files.Any(file => file.Name == "Plugin.Desktop.cs") && files.Any(file => file.Name == "Plugin.cs"),"both files");
            var paths = new (string Service,string Install,string Release)[]
            {
                ("local link",@"linkFeed=new Link\.LocalLinkFeed\(",@"linkFeed\?\.Dispose\(\);linkFeed=null;"),
                ("installer",@"mainWindow\.DesktopInstaller=new Link\.DesktopInstaller\(",@"mainWindow\.DesktopInstaller\?\.Dispose\(\);mainWindow\.DesktopInstaller=null;"),
            };
            foreach (var (service,install,release) in paths)
            {
                Check(files.Sum(file => Count(file.Text,install)) == 1 && files.Sum(file => Count(file.Text,release)) == 1,service + ": one install and one release path");
                var home = files.Single(file => file.Name == "Plugin.Desktop.cs").Text;
                Check(Count(home,install) == 1 && Count(home,release) == 1,service + ": in its family's file");
            }
            var desktop = files.Single(file => file.Name == "Plugin.Desktop.cs").Text;
            Check(Count(desktop,@"registrations\.Register\(") == 2,"both through the plugin's registrations");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(Count(plugin,@"RegisterDesktop\(pluginInterface,configuration,framework,client,player,inventory,objects\);") == 1,"Plugin.cs calls the family once");
            Check(!plugin.Contains("Link.LocalLinkFeed(",StringComparison.Ordinal) && !plugin.Contains("new Link.DesktopInstaller(",StringComparison.Ordinal),"and creates none of them");
            Check(plugin.Contains("public sealed partial class Plugin : IDalamudPlugin",StringComparison.Ordinal) && desktop.Contains("public sealed partial class Plugin",StringComparison.Ordinal),"one class in two files");
        }),
    ];
}
