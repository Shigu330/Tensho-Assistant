// 0.874 (X-75.1, user request « un test pour chaque sonde »): every in-game probe and parser has a test. The capture's parts are a fixed list: a part added or
// removed fails here until the list (and docs/evidence/0.874-probe-coverage.md) says so.
using System.Text.RegularExpressions;
internal static class ProbeSet874Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    // 0.881 (X-75.7): addons, fashion, islandobjects added.
    private static readonly string[] Parts=["addons","airship","eventMarkers","fashion","fields","fishing","fishline","island","islandAgenda","islandobjects","leves","mapMarkers","messages","monsterEvents","npctrade",
        "purify","questMarkers","retainer","statuses","synthesis","workshop"];
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Probe set: the capture writes exactly the listed parts, each with a line limit",() =>
        {
            var capture=Read("Diagnostics/ReadingCapture.Dalamud.cs");
            var found=Regex.Matches(capture,@"Line\(""([A-Za-z]+)"",(\d+),").Select(match=>match.Groups[1].Value).Distinct().Order(StringComparer.Ordinal).ToArray();
            Check(found.SequenceEqual(Parts.Order(StringComparer.Ordinal)),"parts: "+string.Join(",",found));
            Check(Regex.Matches(capture,@"Line\(""[A-Za-z]+"",(\d+),").All(match=>int.Parse(match.Groups[1].Value) is > 0 and <= 400),"every part bounded");
        }),
        ("Probe set: gatherer statuses, fishing flag and chat, island, synthesis and airship parts keep their guards",() =>
        {
            var capture=Read("Diagnostics/ReadingCapture.Dalamud.cs");
            Check(capture.Contains("if(job is 16 or 17 or 18)ReadStatuses(local);") && capture.Contains("Line(\"statuses\",200,"),"statuses: gatherers only");
            Check(capture.Contains("if(flag!=fishing){fishing=flag;Line(\"fishing\",200,") && capture.Contains("if(objects.LocalPlayer?.ClassJob.RowId==18)Line(\"fishing\",200,"),"fishing: on change, fishers only");
            Check(capture.Contains("if(manager==null || !manager->IsPlayerInSanctuary)return;") && capture.Contains("Line(\"islandAgenda\",200,"),"island: in the sanctuary only");
            Check(capture.Contains("if(text==synthesis)return;") && capture.Contains("Line(\"synthesis\",150,"),"synthesis: on change");
            Check(capture.Contains("if(housing==null || housing->WorkshopTerritory==null){airship=\"\";return;}") && capture.Contains("Line(\"airship\",12,"),"airship: in the workshop only");
        }),
        ("Probe set: the item watcher keeps its busy windows, its 16-gain bound and each reader's guard",() =>
        {
            var watcher=Read("Discoveries/ItemDiscoveryWatcher.Dalamud.cs");
            foreach(var window in new[]{"\"Shop\"","\"Trade\"","\"RetainerSellList\"","\"ItemSearchResult\"","\"LetterViewer\"","\"SalvageResult\"","\"PurifyResult\"","\"RetainerTaskResult\""})
                Check(watcher.Contains(window),"busy window "+window);
            Check(watcher.Contains("if(gains.Count==0 || gains.Count>16)return;"),"16 gains at most");
            Check(watcher.Contains("if(gui.GetAddonByName(\"RetainerTaskResult\") is not { IsNull:false,IsVisible:true }){ventureSeen=\"\";return;}")
                && watcher.Contains("store.SeeSource(DiscoveryKind.Venture,item,task,count,at,hq:hq);"),"ventures: result window only");
            Check(watcher.Contains("if(territory!=lootTerritory){lootSeen.Clear();chestsSeen.Clear();lootTerritory=territory;}") && watcher.Contains("if(duty==0)return;"),"duty chests: in a duty, reset per zone");
            Check(watcher.Contains("if(fate->State is FateState.Ending or FateState.Ended)fateEndedAt=now;"),"FATE end");
            Check(watcher.Contains("if(targets.Target is not IBattleNpc { BattleNpcKind:BattleNpcSubKind.Combatant } npc || npc.CurrentHp!=0 || npc.NameId==0)return;"),"own target, a monster, dead");
            Check(watcher.Contains("festivalDay=day;") && watcher.Contains("registered.Where(id=>!known.Contains(id)).Take(4)"),"festivals once a day, new vistas only");
        }),
        ("Probe set: vistas keep no place and Eorzea minutes; hunt marks, FATEs, market history and the outfit judged stay behind their settings",() =>
        {
            var store=new TenshoAssistant.Discoveries.DiscoveryStore();
            Check(store.SeeSource(TenshoAssistant.Discoveries.DiscoveryKind.VistaSeen,12,3,1439,DateTimeOffset.UtcNow),"vista kept");
            var vista=store.Find(TenshoAssistant.Discoveries.DiscoveryKind.VistaSeen,_=>true,1).Single();
            Check(vista.Map==0 && vista.LevelMax==1439,"no place, minute kept");
            var page=Read("MainWindow.Discoveries.cs");
            Check(page.Contains("if(settings.HuntMarks)") && page.Contains("store.SeeHuntMark(") && page.Contains("if(settings.Fates)") && page.Contains("store.SeeFate("),"marks and FATEs behind settings");
            var reader=Read("Market/SeenPriceReader.Dalamud.cs");
            Check(reader.Contains("board.HistoryReceived+=OnHistory;") && reader.Contains("board.HistoryReceived-=OnHistory;"),"history subscribed and released");
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("if(!configuration.Discovery.Enabled || !configuration.Discovery.FashionReport || mainWindow.DiscoveryStore is not { } store)return;"),"outfit behind its setting");
        }),
    ];
}
