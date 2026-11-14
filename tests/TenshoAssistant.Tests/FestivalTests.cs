// Test audit of 2026-11-09, batch 2 (0.1021): Events342Tests, FestivalFlicker950Tests, FestivalVendors810Tests merged into FestivalTests; every case is kept.
using TenshoAssistant.Events;
using TenshoAssistant.Npcs;
internal static class FestivalTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
    private static NpcLocation Spot(int condition,uint festival=0)=>new(128,11,"",10,12,0,0,0,condition,festival);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From Events342Tests: 0.342 (R-09, DEC-023 point 7; user requests: event shops, the right names, check what other plugins do): running festivals from the
        // game, their quests, names from the guide NPC (French client) or LuminaSupplemental, game flags hidden, event shops from the directory.
        ("Festivals: names file, running slots, French name from the guide NPC (earliest separator), game flags hidden",() =>
        {
            using var reader = new StringReader("# notice\nFestivalId,Name\n0,None\n120,Special Event Flag\n176,All Saint's Wake (2026)\n257,Unknown\n");
            var names = Festivals.ReadNames(reader);
            Check(names.Count == 2 && names[176] == "All Saint's Wake (2026)" && !names.ContainsKey(257) && !names.ContainsKey(0),"None and Unknown are no name");
            Check(Festivals.Running([(0,0),(176,1),(176,1),(120,0)]).Select(festival => festival.Id).SequenceEqual([176u,120u]),"empty slots and duplicates dropped");
            Check(Festivals.FrenchNameFromGuide("démarcheur de la Veillée des saints") == "Veillée des saints","de la");
            Check(Festivals.FrenchNameFromGuide("démarcheuse des Feux de la Mort") == "Feux de la Mort","des, before a later « de la »");
            Check(Festivals.FrenchNameFromGuide("organisatrice de la Commémoration") == "Commémoration","organiser");
            Check(Festivals.FrenchNameFromGuide("démarcheur de la fête des étoiles") == "Fête des étoiles","capitalised");
            Check(Festivals.FrenchNameFromGuide("Cait Sith") is null,"no separator: no name");
            var quests = new Dictionary<uint,IReadOnlyList<uint>> { [176] = [71003u] };
            var layouts = new FestivalLayouts(new Dictionary<uint,IReadOnlyList<(uint,uint)>> { [256] = [(1058700u,133u)] },new Dictionary<uint,string>());
            Check(Festivals.Technical(120,names,quests,layouts) && Festivals.Technical(137,names,quests,layouts),"flag by name, and no quest nor NPC");
            Check(!Festivals.Technical(176,names,quests,layouts) && !Festivals.Technical(256,names,quests,layouts),"quests or NPCs: an event");
            Check(!Festivals.Technical(137,names,null,null),"not hidden before the data is read");
        }),
        ("Festivals: read on the framework thread while shown, layouts checked before parsing, shops cached",() =>
        {
            var reader = Read("Events/FestivalReader.Dalamud.cs");
            Check(reader.Contains("TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);",StringComparison.Ordinal) && reader.Contains("TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);",StringComparison.Ordinal),"framework thread, removed on dispose");
            Check(reader.Contains("client.IsLoggedIn",StringComparison.Ordinal) && reader.Contains("catch(Exception error)",StringComparison.Ordinal),"logged in, guarded");
            var check = reader.IndexOf("NpcShopLinks.ValidLayout(bytes)",StringComparison.Ordinal);
            Check(check > 0 && reader.IndexOf("data.GetFile<Lumina.Data.Files.LgbFile>(path)",StringComparison.Ordinal) > check,"layout checked before Lumina parses it");
            Check(reader.Contains("Interlocked.Exchange(ref layoutsStarted,1)",StringComparison.Ordinal),"layouts read once, after the page asked");
            var page = Read("MainWindow.Events.cs");
            Check(page.Contains("Events.Festivals.Technical(",StringComparison.Ordinal) && page.Contains("Events.NameFromGuide",StringComparison.Ordinal),"flags hidden, name source said");
            Check(page.Contains("eventShops.TryGetValue(festival,out var vendors)",StringComparison.Ordinal),"shops computed once per festival");
        }),
        // From FestivalFlicker950Tests: X-95 (0.950): the running festivals no longer flicker to « none » for one read, and the map keeps the last known list while it is not read.
        ("Festival flicker 950: one empty read after a running list is ignored; the map keeps the last list",() =>
        {
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Events/FestivalReader.Dalamud.cs"));
            Check(reader.Contains("if(slots.Count==0 && Volatile.Read(ref state) is { Active.Count:>0 } && !emptyOnce){emptyOnce=true;return;}"),"reader");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs")).Contains("mapVendorCache.Festivals is { Length:>0 } kept"),"map");
        }),
        // From FestivalVendors810Tests: 0.810, 0.811 (X-45, user reports): event and quest NPCs shown on the map while absent in game.
        ("Conditional NPCs: the layer tells always, festival, or quest and opening (Limsa's planevent.lgb)",() =>
        {
            Check(NpcShopLinks.LayerCondition("QST_Easter2021_Sea001",114)==(NpcShopLinks.DuringFestival,114u),"festival first");
            Check(NpcShopLinks.LayerCondition("QST_ClsArm530",0)==(NpcShopLinks.DuringQuest,0u) && NpcShopLinks.LayerCondition("QB_JobAst401_001",0).Condition==NpcShopLinks.DuringQuest
                && NpcShopLinks.LayerCondition("EVT_OP_ONLY_ENPC",0).Condition==NpcShopLinks.DuringQuest,"quest, opening");
            Check(NpcShopLinks.LayerCondition("PUB_ENPC_HANDLER",0).Condition==NpcShopLinks.Always && NpcShopLinks.LayerCondition(null,0).Condition==NpcShopLinks.Always,"other layers: always");
            Check(NpcShopLinks.LayerCondition("QST_ENPC06",0).Condition==NpcShopLinks.Always && NpcShopLinks.LayerCondition("QST_ClsAcn250_000",0).Condition==NpcShopLinks.DuringQuest,"0.829: QST_ENPC permanent");
            Check(!NpcShopLinks.Present(new NpcLocation(128,11,"",1,1,0,0,0,NpcShopLinks.Unplaced),new HashSet<uint>()),"0.829: a Level-only vendor not on the map");
            Check(NpcShopLinks.MorePresent((2,0),(1,27))==(1,27u) && NpcShopLinks.MorePresent((1,27),(0,0))==(0,0u) && NpcShopLinks.MorePresent((2,0),(2,0))==(2,0u),"0.823: the layer that keeps it most");
            var loader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Npcs/NpcVendorLoader.Dalamud.cs"));
            // 0.1307 (X-266): the Level position's condition comes from NpcShopLinks.LevelCondition (festival, or quest when only quest layers place it).
            Check(loader.Contains("case NpcShopLinks.DuringFestival:list[index]=list[index] with {Condition=NpcShopLinks.DuringFestival,Festival=placed!.Value.Festival};"),"0.823: Level positions too");
        }),
        ("Conditional NPCs: on the map, always, or while its festival runs; never a quest's or the opening's",() =>
        {
            var running=new HashSet<uint>{114};
            Check(NpcShopLinks.Present(Spot(0),running) && NpcShopLinks.Present(Spot(1,114),running),"always, running festival");
            Check(!NpcShopLinks.Present(Spot(1,27),running) && !NpcShopLinks.Present(Spot(2),running),"other festival, quest");
            var map=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            // 0.1307 (X-266): with the quests seen; rebuilt too when one of the quests its vendors wait for changes.
            Check(map.Contains("same.Contains(spot.MapId) && Npcs.NpcShopLinks.Present(spot,running,seen)") && map.Contains("|| mapVendorCache.Festivals!=festivalKey")
                && map.Contains("|| mapVendorQuests.Quests.Length>0 && MapVendorQuestKey(mapVendorQuests.Quests)!=mapVendorQuests.Seen)"),"filtered, rebuilt");
        }),
    ];
}
