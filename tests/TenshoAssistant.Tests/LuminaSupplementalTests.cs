using TenshoAssistant.Community;
using TenshoAssistant.Duties;

internal static class LuminaSupplementalTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    // First rows of the real files (Critical-Impact/LuminaSupplemental, Generated, read 2026-10-31): Sastasha = ContentFinderCondition 4.
    private static readonly Dictionary<string,string> Csv = new()
    {
        ["DungeonBossChest"]="RowId,ItemId,ContentFinderConditionId,Quantity,FightNo,CofferNo\r\n1,2684,4,1,0,0\r\n2,2683,4,1,0,0\r\n3,2683,4,1,0,1\r\n",
        ["DungeonBossDrop"]="RowId,ContentFinderConditionId,FightNo,ItemId,Quantity\n1,4,2,9789,1\n",
        ["DungeonChest"]="RowId,ChestNo,ContentFinderConditionId,MapId,TerritoryTypeId,TreasureId,DungeonBossId,Position\n1,1,4,31,1036,93,3,-324.51367;5.569519;348.989\n",
        ["DungeonChestItem"]="RowId,ItemId,ChestId,Min,Max,Probability\n1,31423,1,1,1,12.42\n2,38467,9,1,1,11.28\n",
    };
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("LuminaSupplemental gives boss coffers, boss drops and chests of a duty by its ContentFinderCondition id",()=>
        {
            var data=new LuminaSupplementalData(Csv);
            var sastasha=data.ForDuty(4);
            Check(sastasha.Count(entry=>entry.Kind==DutyLootSourceKind.FightCoffer && entry.SourceIndex==0)==2,"two items in the first boss coffer, the repeat merged");
            Check(sastasha.Any(entry=>entry.ItemId==9789 && entry.Kind==DutyLootSourceKind.FightCoffer && entry.SourceIndex==2),"boss drop of the third fight");
            Check(sastasha.Any(entry=>entry.ItemId==31423 && entry.Kind==DutyLootSourceKind.ZoneCoffer && entry.SourceIndex==0),"chest 1 item");
            Check(!sastasha.Any(entry=>entry.ItemId==38467),"item of an unknown chest ignored");
            Check(data.DutiesOf(2683).SequenceEqual([4u]) && data.DutiesOf(1).Count==0 && data.ForDuty(5).Count==0,"item to duties index");
        }),
        ("LuminaSupplemental item uses: what an item gives, the quests asking for it, and the NPCs of unlinked shops",()=>
        {
            // QuestRequiredItem first row of the real file (read 2026-10-31); ItemSupplement and ENpcShop rows in the files' column order.
            var csv=new Dictionary<string,string>
            {
                ["ItemSupplement"]="ItemId,SourceItemId,ItemSupplementSource,Min,Max,Probability\n2,5106,1,1,3,0\n41650,41667,7,1,1,0\n41650,41667,7,1,1,0\n",
                ["QuestRequiredItem"]="ItemId,QuestId,Quantity,IsHq\n5352,65541,10,False\n",
                ["ENpcShop"]="ENpcResidentId,ShopId\n1016289,1769635\n",
                ["ENpcPlace"]="ENpcResidentId,TerritoryTypeId,MapId,PlaceNameId,Position\n1016289,132,2,52,11.7;13.4\n",
            };
            var data=new LuminaSupplementalData(csv,npcName:_=>"Vendor",questName:id=>id==65541 ? "Quest":null);
            Check(data.GivesOf(5106) is [(1,1,[2u])],"desynthesis of 5106 gives 2");
            Check(data.GivesOf(41667) is [(7,2,[41650u])] && data.SupplementsOf(41650) is [(7,2,[41667u])],"coffer read both ways, duplicates counted once in the list");
            Check(data.QuestsRequiring(5352) is [(65541u,"Quest",10u,false)] && data.QuestsRequiring(1).Count==0,"quest asking for an item");
            Check(data.NpcsOfShop(1769635) is [1016289u] && data.NpcName(1016289)=="Vendor" && data.PlaceOf(1016289) is { MapId:2, X:11.7f, Y:13.4f },"unlinked shop NPC and place");
        }),
    ];
}
