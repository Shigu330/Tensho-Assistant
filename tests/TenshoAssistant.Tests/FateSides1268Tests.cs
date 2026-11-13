using TenshoAssistant.Monsters;

// 0.1268 (X-160, user's request of 12/11): on Tensho's map and above heads, an enemy of a running FATE carries the game's own icon of that
// FATE (its type) instead of a star, and an ally of the FATE (an escort, guards: a combatant the game does not flag hostile) its own mark.
internal static class FateSides1268Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("FATE sides 1268: the side comes from the FATE and the game's hostile flag",() =>
        {
            Check(NearbyMonsters.SideOf(0,true)==FateSide.None && NearbyMonsters.SideOf(0,false)==FateSide.None,"outside a FATE: no side, hostile or not");
            Check(NearbyMonsters.SideOf(120,true)==FateSide.Enemy,"hostile in a FATE: its enemy");
            Check(NearbyMonsters.SideOf(120,false)==FateSide.Ally,"not hostile in a FATE: an ally");
        }),
        ("FATE sides 1268: the flag is read with the monster, and a record built without it stays an enemy",() =>
        {
            var reader=Read("Maps/MapLiveReader.Dalamud.cs");
            Check(reader.Contains("float Height=0,bool Hostile=true);"),"default: hostile (what every monster was before)");
            Check(reader.Contains("var hostile=(npc.StatusFlags & Dalamud.Game.ClientState.Objects.Enums.StatusFlags.Hostile)!=0;") && reader.Contains("EventId.Id,height,hostile));"),"the game's own flag, copied with the monster");
        }),
        ("FATE sides 1268: an enemy carries the FATE's icon, an ally its own mark, on the map and above heads, under the same switches",() =>
        {
            var notes=Read("MainWindow.NearbyMonsters.cs");
            Check(notes.Contains("if(NearbyMonsters.SideOf(monster.FateId,monster.Hostile)==FateSide.Ally)"),"the pure rule decides");
            Check(notes.Contains("new MonsterNote(MonsterNoteKind.FateAlly,localization.Format(\"MapView.MonsterFateAlly\""),"ally: its own note");
            Check(notes.Contains("new MonsterNote(MonsterNoteKind.Fate,localization.Format(\"MapView.MonsterFate\",fate?.Name ?? monster.FateId.ToString()),fate?.Icon ?? 0)"),"enemy: the icon of its FATE, 0 when the FATE is not listed");
            var map=Read("MainWindow.MapView.cs");
            Check(map.Contains("if(first.Icon!=0 && textures.TryGetFromGameIcon(new GameIconLookup(first.Icon),out var fateShared)") && map.Contains("MonsterNoteKind.Fate or MonsterNoteKind.FateAlly=>settings.ShowFateMonsters"),"map: the game's icon, the glyph otherwise; one switch for both sides");
            var above=Read("MainWindow.MonsterIcons.cs");
            Check(above.Contains("if(first.Icon!=0 && textures.TryGetFromGameIcon(new GameIconLookup(first.Icon),out var fateShared)") && above.Contains("MonsterNoteKind.Fate or MonsterNoteKind.FateAlly=>settings.AboveFate"),"above heads: the same");
            Check(above.Contains("MonsterNoteKind.FateAlly=>(FontAwesomeIcon.HandsHelping,"),"the ally's glyph");
            foreach(var file in new[]{"en","fr"})
            {
                var text=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{file}.json"));
                Check(text.Contains("\"MonsterFateAlly\": \""),file+": MapView.MonsterFateAlly");
                var help=text[text.IndexOf("\"ShowFateMonstersHelp\"",StringComparison.Ordinal)..];help=help[..help.IndexOf('\n')];
                Check(!help.Contains("(a star)") && !help.Contains("(une étoile)"),file+": the help no longer promises a star");
            }
        }),
    ];
}
