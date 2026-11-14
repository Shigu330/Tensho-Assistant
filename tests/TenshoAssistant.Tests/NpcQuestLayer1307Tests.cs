using TenshoAssistant.Npcs;

// 0.1307 (X-266, user report of 13/11 at Mor Dhona, relais diagnosis of 13/11): Tensho's map showed vendors absent in the game: a Level
// position « always » of an NPC placed in its territory only by a quest's layer (Cristiana 1016290, layer « QST_JobRel101 », quest 67748
// not taken; 31 such places in the game). Such a place now shows while one of its layer's quests is under way or done, never otherwise;
// « QST_ENpc_001 » (lower case, territory 180) stays permanent like « QST_ENPC01 ».
internal static class NpcQuestLayer1307Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static NpcLocation Spot(int condition,IReadOnlyList<uint>? quests=null) => new(156,25,"Mor Dhona",21.4f,5.5f,0,0,0,condition,0,quests);
    private static readonly (uint,string)[] Scripts=[(67747,"JobRel101_02211"),(67748,"JobRel101_02212"),(67900,"JobRel1010_01234"),(70199,"AktKdd_00001"),(70200,"AktKdd_00002"),(65581,"SubLak001_00123"),(1,"")];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("NPC quest layer 1307: « QST_ENPC » whatever its case is permanent; a quest layer's key; the quests whose script follows it",() =>
        {
            Check(NpcShopLinks.LayerCondition("QST_ENpc_001",0).Condition==NpcShopLinks.Always && NpcShopLinks.LayerCondition("qst_enpc02",0).Condition==NpcShopLinks.Always,"QST_ENPC, any case");
            Check(NpcShopLinks.LayerCondition("QST_JobRel101",0).Condition==NpcShopLinks.DuringQuest,"a quest layer");
            Check(NpcShopLinks.LayerQuestKey("QST_JobRel101")=="JobRel101" && NpcShopLinks.LayerQuestKey("QST_AktKdd_PUB")=="AktKdd" && NpcShopLinks.LayerQuestKey("QST_SubLak001")=="SubLak001"
                && NpcShopLinks.LayerQuestKey("QST_ClsAcn250_000")=="ClsAcn250" && NpcShopLinks.LayerQuestKey("QST_LucKra_PUB_HANDLER")=="LucKra","keys");
            Check(NpcShopLinks.LayerQuestKey("QST_ENPC01") is null && NpcShopLinks.LayerQuestKey("QST_ENpc_001") is null && NpcShopLinks.LayerQuestKey("EVT_OP_ONLY_ENPC") is null
                && NpcShopLinks.LayerQuestKey("QB_JobAst401_001") is null && NpcShopLinks.LayerQuestKey(null) is null && NpcShopLinks.LayerQuestKey("QST_") is null,"no key");
            Check(NpcShopLinks.QuestsOf("JobRel101",Scripts) is [67747,67748],"JobRel101: its two quests, not JobRel1010");
            Check(NpcShopLinks.QuestsOf("aktkdd",Scripts) is [70199,70200] && NpcShopLinks.QuestsOf("SubLak001",Scripts) is [65581] && NpcShopLinks.QuestsOf("Nothing",Scripts).Count==0,"others");
        }),
        ("NPC quest layer 1307: a Level position takes « quest » only when every layer placing it there is a quest's",() =>
        {
            Check(NpcShopLinks.LevelCondition((NpcShopLinks.DuringQuest,0),true)==NpcShopLinks.DuringQuest,"only QST_JobRel101: quest");
            var withPermanent=NpcShopLinks.MorePresent(NpcShopLinks.LayerCondition("QST_JobRel101",0),NpcShopLinks.LayerCondition("QST_ENPC_002",0));
            Check(NpcShopLinks.LevelCondition(withPermanent,true)==NpcShopLinks.Always,"with QST_ENPC_002: always");
            Check(NpcShopLinks.LevelCondition(NpcShopLinks.MorePresent((NpcShopLinks.DuringQuest,0),(NpcShopLinks.DuringFestival,114)),true)==NpcShopLinks.DuringFestival,"a festival layer: festival");
            Check(NpcShopLinks.LevelCondition(null,false)==NpcShopLinks.Unplaced && NpcShopLinks.LevelCondition(null,true)==NpcShopLinks.Always,"no layer anywhere: unplaced; elsewhere only: always");
        }),
        ("NPC quest layer 1307: a quest's place shows while a linked quest is under way or done; not taken, or no quest linked: hidden",() =>
        {
            var running=new HashSet<uint>();
            var cristiana=Spot(NpcShopLinks.DuringQuest,[67747,67748]);
            Check(!NpcShopLinks.Present(cristiana,running) && !NpcShopLinks.Present(cristiana,running,_=>false),"not taken: hidden (Mor Dhona, 13/11)");
            Check(NpcShopLinks.Present(cristiana,running,quest=>quest==67748) && NpcShopLinks.Present(cristiana,running,quest=>quest==67747),"one under way or done: shown");
            Check(!NpcShopLinks.Present(Spot(NpcShopLinks.DuringQuest),running,_=>true) && !NpcShopLinks.Present(Spot(NpcShopLinks.DuringQuest,[]),running,_=>true),"no quest linked: hidden");
            Check(NpcShopLinks.Present(Spot(NpcShopLinks.Always),running,_=>false) && !NpcShopLinks.Present(Spot(NpcShopLinks.Unplaced,[67748]),running,_=>true),"always stays; unplaced stays hidden");
        }),
        ("NPC quest layer 1307: the loader links the places, the map asks the game and is built again when a quest changes, the cache is rebuilt",() =>
        {
            var loader=Read("src/TenshoAssistant/Npcs/NpcVendorLoader.Dalamud.cs");
            Check(loader.Contains("if(layerCondition.Condition==NpcShopLinks.DuringQuest && NpcShopLinks.LayerQuestKey(layer.Name) is { } questKey)",StringComparison.Ordinal)
                && loader.Contains("list[index]=list[index] with {Condition=NpcShopLinks.DuringQuest,Quests=quests.Length>0 ? quests:null};",StringComparison.Ordinal)
                && loader.Contains("Level place(s) only on quest layers",StringComparison.Ordinal),"loader");
            var map=Read("src/TenshoAssistant/MainWindow.MapView.cs");
            Check(map.Contains("Npcs.NpcShopLinks.Present(spot,running,seen)",StringComparison.Ordinal) && map.Contains("mapVendorQuests=(waited,MapVendorQuestKey(waited));",StringComparison.Ordinal),"map");
            var plugin=Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("mainWindow.QuestSeen=id=>Quests.QuestReader.SeenNow(client,player,unlocks,questRows.Value,id);",StringComparison.Ordinal),"plugin");
            var reader=Read("src/TenshoAssistant/Quests/QuestReader.Dalamud.cs");
            Check(reader.Contains("if (!client.IsLoggedIn || !player.IsLoaded) return false;",StringComparison.Ordinal),"nothing read before the character is in");
            Check(Read("src/TenshoAssistant/Npcs/NpcVendorCatalog.cs").Contains("private const int CopyRules=16;",StringComparison.Ordinal),"the cached catalogue is built again");
        }),
    ];
}
