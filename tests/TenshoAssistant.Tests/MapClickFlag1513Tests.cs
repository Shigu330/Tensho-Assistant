using Newtonsoft.Json;
using TenshoAssistant;
using TenshoAssistant.Maps;

// 0.1513 (X-535, test T-0.546-1, user report of 17/11: « a click on the map places no flag »): the setting « Left click places the flag »
// was off by default since 0.546 and off in the user's settings. It is on by default now, as on the game's map, and renamed so a value
// saved off is not read back; the click's rule moved to Maps.MapClickFlag.
internal static class MapClickFlag1513Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static readonly JsonSerializerSettings Settings = new() { TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple, TypeNameHandling = TypeNameHandling.Objects };

    // A short click on an empty spot of a zone map, setting on: the case the user tried.
    private static bool Click(bool enabled=true,bool hovered=true,bool icon=false,bool link=false,bool bookmark=false,bool point=false,bool vista=false,uint territory=140,bool released=true,bool dragged=false)
        =>MapClickFlag.Places(enabled,hovered,icon,link,bookmark,point,vista,territory,released,dragged);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map click flag 1513: behaviour: a click on an empty spot places the flag; a drag, an icon, a link or a region map do not",() =>
        {
            Check(Click(),"empty spot of a zone map: flag");
            Check(!Click(dragged:true),"a drag moves the map, no flag");
            Check(!Click(released:false),"only on the release");
            Check(!Click(icon:true) && !Click(link:true) && !Click(bookmark:true) && !Click(point:true) && !Click(vista:true),"an icon keeps its own click");
            Check(!Click(territory:0),"region or world map: no flag");
            Check(!Click(hovered:false),"over a panel drawn on the map: no flag");
            Check(!Click(enabled:false),"the setting off: no flag");
        }),
        ("Map click flag 1513: behaviour: on by default, and a value saved off before (the old name) is not read back",() =>
        {
            Check(new MapViewSettings().FlagOnLeftClick,"on by default");
            var saved=JsonConvert.SerializeObject(new Configuration(),Settings);
            Check(saved.Contains("\"FlagOnLeftClick\":true",StringComparison.Ordinal),"saved under its new name");
            var before=saved.Replace("\"FlagOnLeftClick\":true","\"FlagOnClick\":false",StringComparison.Ordinal);
            var read=JsonConvert.DeserializeObject<Configuration>(before,Settings) ?? throw new Exception("not read back");
            Check(read.MapView.FlagOnLeftClick,"a file of 0.1512 with the setting off: on after the update");
            var off=JsonConvert.DeserializeObject<Configuration>(saved.Replace("\"FlagOnLeftClick\":true","\"FlagOnLeftClick\":false",StringComparison.Ordinal),Settings)!;
            Check(!off.MapView.FlagOnLeftClick,"turned off again by the player: kept off");
        }),
    ];
}
