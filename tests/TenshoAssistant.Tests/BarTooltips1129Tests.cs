using System.Numerics;
using TenshoAssistant.UI;

// 0.1129 (X-162, user request of 12/11): Tensho's server bar tooltips (alarms, reminders, currencies, gil) all open on the same side of
// their entry, under it by default or above it with the setting, and never off the screen.
internal static class BarTooltips1129Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file)).Replace("\r\n","\n");
    private static readonly Vector2 Screen=new(1920,1080);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Server bar tooltips: under by default, above with the setting, the other side without room, never off the screen",() =>
        {
            // The server bar at the top right of the screen, as the game puts it.
            var (under,pivot)=BarTooltipPlace.For(new(1500,2),new(1560,20),Screen,false);
            Check(under==new Vector2(1560,24) && pivot==new Vector2(1,0),"under, right edge on the entry's right edge: "+under+" "+pivot);
            var (top,topPivot)=BarTooltipPlace.For(new(1500,2),new(1560,20),Screen,true);
            Check(top==new Vector2(1560,24) && topPivot==new Vector2(1,0),"above asked at the top of the screen: no room, so under");
            // The bar moved to the bottom of the screen.
            var (low,lowPivot)=BarTooltipPlace.For(new(100,1060),new(160,1078),Screen,false);
            Check(low==new Vector2(100,1056) && lowPivot==new Vector2(0,1),"under asked at the bottom: above, left edge on the entry's left edge");
            var (middle,middlePivot)=BarTooltipPlace.For(new(100,500),new(160,518),Screen,true);
            Check(middle==new Vector2(100,496) && middlePivot==new Vector2(0,1),"above when there is room");
            var (edge,_)=BarTooltipPlace.For(new(1900,2),new(1930,20),Screen,false);
            Check(edge.X==1920,"an entry past the right edge: the tooltip's right edge stays on the screen");
        }),
        ("Server bar tooltips: the four entries draw Tensho's tooltip on the chosen side; the setting is saved and reset",() =>
        {
            foreach(var file in new[]{"Alarms/AlarmDtrEntry.Dalamud.cs","Alarms/ReminderDtrEntry.Dalamud.cs","Alarms/CurrencyDtrEntry.Dalamud.cs","Inventory/GilDtrEntry.Dalamud.cs"})
                Check(Read(file).Contains("TooltipScope.UnderBox(bounds,configuration.ServerBarTooltipAbove)"),file);
            foreach(var file in new[]{"Alarms/CurrencyDtrEntry.Dalamud.cs","Inventory/GilDtrEntry.Dalamud.cs"})
                Check(Read(file).Contains("entry.Tooltip=configuration.AlarmBarTooltip && bounds is not null ? null:"),"the game's tooltip only when Tensho's cannot be placed: "+file);
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("(\"CurrencyBar\",()=>currencyBar?.Invoke()?.DrawTooltip())") && plugin.Contains("(\"GilBar\",()=>gilBar?.Invoke()?.DrawTooltip())"),"drawn by the plugin");
            Check(Read("UI/Components/Tooltip.cs").Contains("ImGui.SetNextWindowPos(position,ImGuiCond.Always,pivot);"),"placed with its pivot");
            var configuration=Read("Configuration.cs");
            Check(configuration.Contains("public bool ServerBarTooltipAbove { get; set; }") && configuration.Contains("\"ServerBarTooltipAbove\","),"setting, under by default, in the reset list");
        }),
    ];
}
