// 0.1226 (X-217, user request of 12/11, plugin comparison K-30): three optional entries of the server info bar, off by default: the four
// bags' free slots, the worn gear's lowest condition, the zone with its instance. The instance comes from fields of the game's public
// instance (no game function), only while it belongs to the current territory and is 1 to 9 (the game's own instance marks); anything else
// shows the zone alone. Built like the gil entry: framework thread every 2 s, gone during a competitive cut, removed on dispose.
using TenshoAssistant.Alarms;

internal static class InfoBar1226Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Info bar 1226: the instance only when it is the current zone's and 1 to 9",() =>
        {
            Check(InfoBarRules.Instance(132,2,132) == 2,"instance 2 of the zone");
            Check(InfoBarRules.Instance(132,0,132) is null,"no instance");
            Check(InfoBarRules.Instance(133,2,132) is null,"another zone's instance (left behind)");
            Check(InfoBarRules.Instance(132,10,132) is null && InfoBarRules.Instance(0,1,0) is null,"no mark for it, or no zone");
        }),
        ("Info bar 1226: off by default, reset with the settings, wired like the other entries",() =>
        {
            var defaults = new TenshoAssistant.Configuration();
            Check(!defaults.BagsServerBar && !defaults.WearServerBar && !defaults.PlaceServerBar,"off by default");
            var configuration = Read("src/TenshoAssistant/Configuration.cs");
            Check(configuration.Contains("\"BagsServerBar\", \"WearServerBar\", \"PlaceServerBar\"",StringComparison.Ordinal),"in the reset list");
            var entry = Read("src/TenshoAssistant/Alarms/InfoDtrEntries.Dalamud.cs");
            Check(entry.Contains("CompetitiveGate.AddAlways(framework,OnUpdate);",StringComparison.Ordinal) && entry.Contains("if(CompetitiveGate.Cut){bags.Shown=wear.Shown=place.Shown=false;return;}",StringComparison.Ordinal)
                  && entry.Contains("CompetitiveGate.RemoveAlways(framework,OnUpdate);",StringComparison.Ordinal) && entry.Contains("bags.Remove();wear.Remove();place.Remove();",StringComparison.Ordinal),"cut and removed like the others");
            Check(entry.Contains("state->PublicInstance.TerritoryTypeId,state->PublicInstance.InstanceId",StringComparison.Ordinal) && !entry.Contains("IsInstancedArea(",StringComparison.Ordinal),"fields only, no game function");
            Check(entry.Contains("catch(Exception){instanceFailed=true;return null;}",StringComparison.Ordinal),"a failed read turns the instance off (R08)");
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("()=>infoBar?.Dispose());",StringComparison.Ordinal),"owned by the plugin's registrations");
            var settings = Read("src/TenshoAssistant/MainWindow.Advanced.cs");
            foreach (var name in new[] { "BagsServerBar","WearServerBar","PlaceServerBar" })
            {
                Check(settings.Contains($"BoolSetting(\"Advanced.{name}\",\"Advanced.{name}Tip\",()=>configuration.{name},",StringComparison.Ordinal),"setting row " + name);
                foreach (var language in new[] { "en","fr" })
                    Check(Read($"src/TenshoAssistant/Localization/Languages/{language}.json").Contains($"\"{name}Tip\": ",StringComparison.Ordinal),language + ": " + name);
            }
        }),
    ];
}
