using TenshoAssistant.Island;

// W-04 (0.685): the island's animals: the wiki's tables read, in the game's words, and the pasture only read.
internal static class IslandAnimals685Tests
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
    ];
}
