using TenshoAssistant;

// 0.1262 (X-161, user request of 12/11): on Tensho's map only, the sightseeing marks a little smaller by default (80 %) and the monsters'
// marks half their size; they join the icon size table, so the player changes them like any icon. A size the player set stays; the new
// defaults reach the players whose saved table does not name these icons (behaviour test with a table saved before).
internal static class MapIconSizes1262Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map icon sizes 1262: smaller sightseeing and monster marks by default, a size the player set kept",() =>
        {
            var fresh=new MapViewSettings();
            Check(fresh.SizeOf(MapViewSettings.VistaGlyph)==0.8f && fresh.SizeOf(MapViewSettings.MonsterGlyph)==0.5f && fresh.SizeOf(MapViewSettings.PartyGlyph)==0.5f,"new player: 80 %, 50 %, and the party as before");
            Check(fresh.SizeOf(60453)==1 && fresh.SizeOf(MapViewSettings.VistaGlyph,696)==0.8f,"another icon stays at 100 %; a map without its own table follows");
            // A table saved before 0.1262: it names the party only (the default of then) and what the player changed.
            var before=System.Text.Json.JsonSerializer.Deserialize<MapViewSettings>("{\"IconSizes\":{\"4294901764\":0.5,\"60453\":1.5}}")!;
            Check(before.SizeOf(MapViewSettings.VistaGlyph)==0.8f && before.SizeOf(MapViewSettings.MonsterGlyph)==0.5f,"a player from before gets the new defaults");
            Check(before.SizeOf(60453)==1.5f && before.SizeOf(MapViewSettings.PartyGlyph)==0.5f,"what was saved is untouched");
            var chosen=System.Text.Json.JsonSerializer.Deserialize<MapViewSettings>("{\"IconSizes\":{\"4294901761\":1,\"4294901768\":0}}")!;
            Check(chosen.SizeOf(MapViewSettings.VistaGlyph)==1 && chosen.SizeOf(MapViewSettings.MonsterGlyph)==0,"a size the player set stays (100 % kept, 0 hides)");
            fresh.SetSize(MapViewSettings.MonsterGlyph,0,1f);
            var back=System.Text.Json.JsonSerializer.Deserialize<MapViewSettings>(System.Text.Json.JsonSerializer.Serialize(fresh))!;
            Check(back.SizeOf(MapViewSettings.MonsterGlyph)==1 && back.SizeOf(MapViewSettings.VistaGlyph)==0.8f,"set, saved and read back");
            fresh.SetSize(MapViewSettings.MonsterGlyph,696,1.5f);
            Check(fresh.SizeOf(MapViewSettings.MonsterGlyph,696)==1.5f && fresh.SizeOf(MapViewSettings.MonsterGlyph,1)==1,"a map's own size wins on that map");
        }),
        ("Map icon sizes 1262: the monsters' marks follow the table and are listed with the other icons",() =>
        {
            Check(MapViewSettings.VistaGlyph==0xFFFF0001 && MapViewSettings.MonsterGlyph==0xFFFF0008,"the keys of Tensho's own layers");
            var view=Read("src/TenshoAssistant/MainWindow.MapView.cs");
            Check(view.Contains("var monsterSize=settings.SizeOf(GlyphMonster,map.MapId);") && view.Contains("var glyph=Math.Max(4,baseIcon*0.6f*monsterSize);") && view.Contains("if(monsterSize<=0)break;"),"drawn at the table's size; 0 hides them");
            Check(view.Contains("settings.ShowWishMonsters)icons.Add(GlyphMonster);") && view.Contains("GlyphMonster=>localization.Get(\"MapView.IconMonsters\"),"),"listed in the map's icon sizes with a name");
            Check(view.Contains("GlyphVista=MapViewSettings.VistaGlyph,") && view.Contains("GlyphMonster=MapViewSettings.MonsterGlyph;"),"one key each, shared with the settings");
            foreach(var file in new[]{"en","fr"})Check(Read($"src/TenshoAssistant/Localization/Languages/{file}.json").Contains("    \"IconMonsters\": \""),file+": MapView.IconMonsters");
        }),
    ];
}
