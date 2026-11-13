// 0.1249 (X-153, fourth slice, GPT review of 11/11): the panel family out of Plugin.cs, after the desktop (0.1225), persistence (0.1239)
// and server bar (0.1240) families. The attached panels and the panel windows each have exactly one install path and one release path
// across every Plugin*.cs file, both in Plugin.Panels.cs, inside the plugin's registrations, in the order of before; Plugin.cs calls the
// family once, after the main window is added (so the panels are released before it), creates none of them and keeps their frame.
using System.Text.RegularExpressions;

internal static class PanelFamily1249Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static (string Name,string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"Plugin*.cs")
        .Select(path => (Path.GetFileName(path),File.ReadAllText(path).Replace("\r\n","\n"))).ToArray();

    private static int Count(string text,string pattern) => Regex.Matches(text,pattern).Count;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Panel family 1249: one install and one release path for each, in Plugin.Panels.cs only, in the order of before; called once after the main window",() =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Panels.cs").Text;
            var parts = new (string Part,string Install,string Release)[]
            {
                ("attached panels",@"attachedPanels = new AttachedPanels\(windows, mainWindow\);",@"attachedPanels\?\.Dispose\(\); attachedPanels = null;"),
                ("panel windows",@"panelWindows = mainWindow\.CreatePanelWindows\(\);",@"foreach \(var panel in panelWindows\) windows\.RemoveWindow\(panel\); panelWindows = \[\];"),
            };
            var last = -1;
            foreach (var (part,install,release) in parts)
            {
                Check(files.Sum(file => Count(file.Text,install)) == 1 && files.Sum(file => Count(file.Text,release)) == 1,part + ": one install and one release path");
                Check(Count(home,install) == 1 && Count(home,release) == 1,part + ": in its family's file");
                var at = Regex.Match(home,install).Index;
                Check(at > last,part + ": created in the order of before");
                last = at;
            }
            Check(Count(home,@"registrations\.Register\(") == 2,"through the plugin's registrations");
            Check(files.Sum(file => Count(file.Text,@"new AttachedPanels\(")) == 1 && files.Sum(file => Count(file.Text,@"CreatePanelWindows\(\)")) == 1,"created nowhere else");
            Check(files.Sum(file => Count(file.Text,@"private PanelWindow\[\] panelWindows")) == 1 && home.Contains("private PanelWindow[] panelWindows = [];") && home.Contains("private AttachedPanels? attachedPanels;"),"their fields live with them");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            var main = plugin.IndexOf("registrations.Register(() => windows.AddWindow(mainWindow), () => windows.RemoveWindow(mainWindow));",StringComparison.Ordinal);
            var call = plugin.IndexOf("RegisterPanels();",StringComparison.Ordinal);
            Check(main > 0 && call > main && Count(plugin,@"RegisterPanels\(\);") == 1,"Plugin.cs calls the family once, after the main window (released before it)");
            Check(plugin.IndexOf("mapWindow = new MapViewWindow(mainWindow)",StringComparison.Ordinal) is > 0 and var map && map < call,"after the map window, as before");
            var follow = plugin.IndexOf("foreach(var panel in panelWindows)panel.Follow();",StringComparison.Ordinal);
            var apply = plugin.IndexOf("attachedPanels?.Apply();",StringComparison.Ordinal);var draw = plugin.IndexOf("windows.Draw();",StringComparison.Ordinal);var end = plugin.IndexOf("attachedPanels?.EndFrame();",StringComparison.Ordinal);
            Check(follow > 0 && follow < apply && apply < draw && draw < end,"their frame stays in the plugin's draw, in the same order");
        }),
    ];
}
