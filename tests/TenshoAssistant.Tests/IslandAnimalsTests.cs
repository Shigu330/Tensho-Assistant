// Test audit of 2026-11-09, batch 2 (0.1021): IslandAnimals685Tests, IslandAnimals901Tests, IslandAnimals932Tests merged into IslandAnimalsTests; every case is kept.
using TenshoAssistant.Island;
using TenshoAssistant;
internal static class IslandAnimalsTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static readonly DateTimeOffset At=new(2026,11,6,12,0,0,TimeSpan.Zero);
    // The markup of the page, 2026-11-06, shortened.
    private const string Page=@"<table class=""mech1""><tr><td>not this one</td></tr></table>
<table class=""npc sortable stdt__table_v2""><tbody><tr><th>Animal</th></tr>
<tr>
<td><span><a href=""x"" title=""Adamantoise (Island Sanctuary)""><img alt=""Adamantoise""></a></span> <a href=""x"" title=""Adamantoise (Island Sanctuary)"">Adamantoise</a></span></td>
<td><a href=""u"" title=""Unnamed Island"">Unnamed Island</a> (<b><small>X:</small></b>31.8, <b><small>Y:</small></b>12.2)</td>
<td style=""text-align:center;"">[L]<br><span><a href=""m"" title=""Makeshift Soporific""><img></a></span></td>
<td><span><a href=""c"" title=""Sanctuary Carapace""><img></a></span> <a href=""c"" title=""Sanctuary Carapace"">Carapace</a></td>
<td><span><a href=""b"" title=""Sanctuary Claw""><img></a></span></td>
<td>12PM-3PM
</td>
<td style=""text-align:center;""><span><a href=""f"" title=""Fog""><img></a></span>
</td></tr>
<tr>
<td><a href=""x"" title=""Aurochs"">Aurochs</a></td>
<td>Unnamed Island (<b><small>X:</small></b>12, <b><small>Y:</small></b>17)</td>
<td>[L]<br><a href=""m"" title=""Makeshift Soporific""></a></td>
<td><a title=""Sanctuary Milk""></a></td>
<td><a title=""Sanctuary Horn""></a></td>
<td>
</td>
<td style=""text-align:center;"">
</td></tr>
<tr><td>a broken row</td></tr>
</tbody></table>";
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From IslandAnimals685Tests: W-04 (0.685): the island's animals: the wiki's tables read, in the game's words, and the pasture only read.
        ("Island animals: the wiki's rows are read, a row with a condition and one without, and a broken one left out",() =>
        {
            var rows=IslandAnimalBook.Parse(Page,At).Rows;
            Check(rows.Count==2,"two rows, got "+rows.Count);
            var rare=rows[0];
            Check(rare.Name=="Adamantoise" && Math.Abs(rare.X-31.8f)<0.01f && Math.Abs(rare.Y-12.2f)<0.01f,"name and place");
            Check(rare.Size=="L" && rare.Restraint=="Makeshift Soporific" && rare.Common=="Sanctuary Carapace" && rare.Bonus=="Sanctuary Claw","size, capture, leavings");
            Check(rare.Time=="12PM-3PM" && rare.Weather=="Fog","time and weather");
            Check(rows[1].Time.Length==0 && rows[1].Weather.Length==0,"no condition is empty, not made up");
        }),
        ("Island animals: a page with no table is an error, not an empty list",() =>
        {
            var failed=false;
            try{IslandAnimalBook.Parse("<html></html>",At);}catch(InvalidDataException){failed=true;}
            Check(failed,"refused");
        }),
        ("Island animals: Eorzea hours read across noon and midnight",() =>
        {
            Check(IslandAnimalBook.EorzeaHours("12PM-3PM")=="12:00 – 15:00","noon");
            Check(IslandAnimalBook.EorzeaHours("9PM-12AM")=="21:00 – 00:00","midnight");
            Check(IslandAnimalBook.EorzeaHours("12AM-3AM")=="00:00 – 03:00","night");
            Check(IslandAnimalBook.EorzeaHours("")=="","empty stays empty");
            Check(IslandAnimalBook.EorzeaHours("dawn")=="dawn","unreadable stays as written");
        }),
        ("Island animals: words the game knows change language, others stay in the wiki's English",() =>
        {
            var rows=IslandAnimalBook.Localized(IslandAnimalBook.Parse(Page,At).Rows,name=>name=="Aurochs" ? "aurochs fr":null,item=>item=="Sanctuary Milk" ? "lait":null,weather=>weather=="Fog" ? "brouillard":null);
            Check(rows[0].Name=="Adamantoise" && rows[0].Weather=="brouillard" && rows[0].Time=="12:00 – 15:00","first");
            Check(rows[1].Name=="aurochs fr" && rows[1].Common=="lait" && rows[1].Bonus=="Sanctuary Horn","second");
        }),
        ("Island animals: the pasture only reads, and the wiki is asked on a click",() =>
        {
            var reader=Read("Island/IslandAnimals.Dalamud.cs");
            foreach(var call in new[]{"CollectLeavingsAll","DispatchEvent","ProcessYield","HandleStartOrEndCommand"})Check(!reader.Contains(call),call);
            Check(Read("Plugin.cs").Contains("mainWindow.ReadPasture=()=>framework.RunOnFrameworkThread(()=>"),"framework thread");
            var page=Read("MainWindow.IslandPasture.cs");
            Check(page.Contains("if(ImGui.Button(localization.Get(\"Island.AnimalListRead\")))source.Request();"),"on click");
            Check(System.Text.RegularExpressions.Regex.Matches(page,@"source\.Request\(").Count==1,"nowhere else");
        }),
        ("Island page: the workshop and the pasture are two tabs",() =>
        {
            var page=Read("MainWindow.IslandSanctuary.cs");
            Check(page.Contains("###IslandTabWorkshop") && page.Contains("###IslandTabPasture") && page.Contains("private void DrawIslandWorkshop()"),"tabs");
        }),
        // From IslandAnimals901Tests: 0.901 (X-83): the island's wild animals on the map, from MJIAnimals (BNpcBase → icon).
        ("Island animals: MJIAnimals by BNpcBase, living ones only, their own icon, a setting on by default",() =>
        {
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs"));
            Check(reader.Contains("if(animal.BNpcBase.RowId!=0 && animal.Icon>0)") && reader.Contains("islandAnimals.TryAdd(animal.BNpcBase.RowId,(uint)animal.Icon);"),"sheet");
            Check(reader.Contains("BattleNpc when islandAnimals.ContainsKey(item.BaseId)"),"by base");
            Check(new MapViewSettings().ShowIslandAnimals,"default on");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(view.Contains("MapObjectKind.IslandAnimal=>!settings.ShowIslandAnimals"),"setting used");
        }),
        // From IslandAnimals932Tests: X-76 P5 (0.932): island animals on the map show their size and leavings, read from MJIAnimals.
        ("Island animals 932: size and leavings from the game data on the map hover",() =>
        {
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs")).Contains("islandAnimalInfo.TryAdd(animal.BNpcBase.RowId,(animal.Size,animal.Reward.Select("),"read");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs")).Contains(":item.Kind==MapObjectKind.IslandAnimal ? IslandAnimalLabel(item):item.Name;"),"shown");
        }),
    ];
}
