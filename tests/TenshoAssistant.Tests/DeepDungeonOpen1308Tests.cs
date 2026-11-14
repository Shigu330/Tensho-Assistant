using TenshoAssistant.Competitive;

// 0.1308 (X-262, user's choice of 13/11 after the Palace of the Dead 1-10, « actif mais si une carte de l'instance specifique il faut coupe
// la map tant qu'on est dedans »): deep dungeons are no longer cut (CompetitiveCut610 and LabyrinthOpen1115 check the zones); Tensho's map
// gives way to the game's in every instance whose map the game draws itself, found in the sheets (docs/evidence/0.1308-own-map-instances.md).
// CompetitiveZones.Build is left to those two classes: it sets the labyrinth's list for the whole process.
internal static class DeepDungeonOpen1308Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Deep dungeon open 1308: an instance whose territory no map of the sheets covers has the game's own map; one with a map does not",() =>
        {
            // Duties: 561 (Palace of the Dead, Map 0, no Map row), 1044 (a dungeon with TerritoryType.Map), 1050 (Map 0 but a Map row names it), 0 (none).
            var own=CompetitiveZones.OwnMapInstances([561,1044,1050,0,561],[(561,0),(1044,800),(1050,0),(132,2)],[1050,0,132]);
            Check(own.SetEquals([561u]),"only 561: "+string.Join(",",own));
            Check(!own.Contains(132),"a field without a duty is never one");
            Check(CompetitiveZones.OwnMapInstances([],[],[]).Count==0,"nothing read: none");
            Check(CompetitiveZones.DeepDungeonContentType==21,"the content type stays known");
        }),
        ("Deep dungeon open 1308: no longer cut in the code; the map window hides and the game's map stays there; DEC-038, ARCHITECTURE and FAQ say it",() =>
        {
            var zones=Read("src/TenshoAssistant/Competitive/CompetitiveZones.cs");
            Check(zones.Contains("foreach(var (territory,pvp,_) in content)if(territory!=0 && pvp)zones.Add(territory);",StringComparison.Ordinal)
                && !zones.Contains("type==DeepDungeonContentType",StringComparison.Ordinal),"zones: content flagged PvP only");
            var gate=Read("src/TenshoAssistant/Competitive/CompetitiveGate.Dalamud.cs");
            Check(gate.Contains("CompetitiveGate.SetOwnMap(own);",StringComparison.Ordinal) && gate.Contains("public static bool HasOwnMap(uint territory)=>territory!=0 && ownMap.Contains(territory);",StringComparison.Ordinal),"gate");
            var map=Read("src/TenshoAssistant/MainWindow.MapView.cs");
            Check(map.Contains("if(InOwnMapInstance?.Invoke()==true)return false;",StringComparison.Ordinal),"MapViewVisible: hidden, so the replacer keeps the game's map (canShow)");
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("mainWindow.InOwnMapInstance=()=>Competitive.CompetitiveGate.HasOwnMap(client.TerritoryType);",StringComparison.Ordinal)
                && Read("src/TenshoAssistant/Plugin.cs").Contains("()=>mainWindow.MapViewVisible(),mainWindow.CloseMapFromGame",StringComparison.Ordinal),"plugin");
            var decisions=Read("docs/project/DECISIONS.md");
            Check(decisions.Contains("deep dungeons (Palace of the Dead, Heaven-on-High, Eureka Orthos, Pilgrim's Traverse, content type 21) are no longer cut",StringComparison.Ordinal)
                && decisions.Contains("les donjons profonds (Palais des morts, Pilier des cieux, Eurêka Orthos, Sanctuaire des pèlerins, type de contenu 21) ne sont plus coupés",StringComparison.Ordinal),"DEC-038, both halves");
            var architecture=Read("docs/project/ARCHITECTURE.md");
            Check(architecture.Contains("Instances with the game's own map (X-262, 0.1308)",StringComparison.Ordinal) && architecture.Contains("Instances à carte propre (X-262, 0.1308)",StringComparison.Ordinal),"ARCHITECTURE, both halves");
            Check(Read("src/TenshoAssistant/Localization/Languages/en.json").Contains("In deep dungeons Tensho works as in any dungeon",StringComparison.Ordinal)
                && Read("src/TenshoAssistant/Localization/Languages/fr.json").Contains("En donjon sans fond, Tensho marche comme dans tout donjon",StringComparison.Ordinal),"FAQ, en and fr");
        }),
    ];
}
