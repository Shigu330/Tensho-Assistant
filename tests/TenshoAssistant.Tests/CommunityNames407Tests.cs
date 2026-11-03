using TenshoAssistant.Community;

// 0.407 (user rule, 2026-11-03): monster, place and duty names from the English wiki and Garland are shown in the game's language when
// the game knows them; anything else stays as the page wrote it.
internal static class CommunityNames407Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Community names: the game's name replaces the page's, capitalised like the page; unknown names are kept",() =>
        {
            Check(CommunityNames.Name("Coeurl","coeurl")=="Coeurl","lower-case game name capitalised");
            Check(CommunityNames.Name("Wild Boar","sanglier sauvage")=="Sanglier sauvage","French monster");
            Check(CommunityNames.Name("Some Monster",null)=="Some Monster","unknown kept");
            Check(CommunityNames.Name("Some Monster","  ")=="Some Monster","empty kept");
            Check(CommunityNames.Name("Ahriman","Ahriman[a]")=="Ahriman","grammar marker removed");
            Check(CommunityNames.Name("Coeurl","クァール")=="クァール","Japanese kept as is");
        }),
        ("Community names: a wiki location keeps its coordinates and translates its place",() =>
        {
            string? Place(string name)=>name=="Western Thanalan" ? "Thanalan occidental":name=="The Aurum Vale" ? "le Val d'Aurum":null;
            Check(CommunityNames.Location("Western Thanalan (12.3, 7)",Place)=="Thanalan occidental (12.3, 7)","coordinates kept as written");
            Check(CommunityNames.Location("The Aurum Vale",Place)=="Le Val d'Aurum","duty without coordinates");
            Check(CommunityNames.Location("Somewhere new (1, 2)",Place)=="Somewhere new (1, 2)","unknown place kept");
            Check(CommunityNames.Location("",Place)=="","empty");
        }),
        ("Community names: every English wiki and Garland name shown goes through the game's names",() =>
        {
            string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
            Check(Read("MainWindow.Monsters.cs").Contains("{CommunityMonster(row.Name)} · {row.Level} · {CommunityPlace(row.Location)}"),"wiki monsters page");
            Check(Read("MainWindow.BlueSpellSources.cs").Contains("{CommunityMonster(row.Name)} · {row.Level} · {CommunityPlace(row.Location)}"),"blue spell monsters");
            var hover=Read("MainWindow.MarketHover.cs");
            Check(hover.Contains("group.Select(CommunityGarland)"),"hover");
            var names=Read("Items/GameNames.Lumina.cs");
            Check(names.Contains("ClientLanguage.English") && names.Contains("Task.Run(Build)") && names.Contains("string.Equals(gameEnglish.Trim(),english.Trim()"),
                "built off the framework thread from the English sheets; Garland ids kept only when the English names match");
            Check(Read("Plugin.cs").Contains("mainWindow.GameNames=new Items.GameNames(data,log);"),"wired");
        }),
    ];
}
