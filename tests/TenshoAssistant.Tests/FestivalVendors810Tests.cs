using TenshoAssistant.Npcs;

// 0.810, 0.811 (X-45, user reports): event and quest NPCs shown on the map while absent in game.
internal static class FestivalVendors810Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static NpcLocation Spot(int condition,uint festival=0)=>new(128,11,"",10,12,0,0,0,condition,festival);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
            Check(loader.Contains("list[index]=list[index] with {Condition=placed.Condition,Festival=placed.Festival};"),"0.823: Level positions too");
        }),
        ("Conditional NPCs: on the map, always, or while its festival runs; never a quest's or the opening's",() =>
        {
            var running=new HashSet<uint>{114};
            Check(NpcShopLinks.Present(Spot(0),running) && NpcShopLinks.Present(Spot(1,114),running),"always, running festival");
            Check(!NpcShopLinks.Present(Spot(1,27),running) && !NpcShopLinks.Present(Spot(2),running),"other festival, quest");
            var map=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(map.Contains("same.Contains(spot.MapId) && Npcs.NpcShopLinks.Present(spot,running)") && map.Contains("|| mapVendorCache.Festivals!=festivalKey)"),"filtered, rebuilt");
        }),
    ];
}
