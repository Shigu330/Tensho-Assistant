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
        ("GatherBuddy fish data: bait, mooch route, bite, ET window, weathers, lure and predators read from its source files",()=>
        {
            // Statements in GatherBuddy's own form (Data7.0.cs, read 2026-11-01), the last one combining the other calls.
            const string source = @"
        data.Apply(43691, Patch.Dawntrail) // Bait fish
            .Bait(data, 43850)
            .Bite(data, HookSet.Precise, BiteType.Weak);
        data.Apply(43692, Patch.Dawntrail) // Pleated Matamata
            .Mooch(data, 43691)
            .Bite(data, HookSet.Powerful, BiteType.Strong);
        data.Apply(43749, Patch.Dawntrail) // Chain Shark
            .Mooch(data, 43692)
            .Time(1080, 1200)
            .Weather(data, 7, 8)
            .Transition(data, 1, 2)
            .Predators(data, 20, (38939, 4), (36449, 5))
            .Lure(Enums.Lure.Modest)
            .Bite(data, HookSet.Powerful, BiteType.Legendary);";
            var data = GatherBuddyFishData.Parse([source]);
            Check(data.Count == 3, "three fish");
            Check(data.Find(43691) is { Baits: [43850u], HookSet: "Precise", Bite: "Weak" }, "bait and bite");
            Check(data.Route(43749).SequenceEqual([43850u, 43691u, 43692u]), "bait then mooched fish, in order");
            var shark = data.Find(43749)!;
            Check(shark is { StartMinute: 1080, EndMinute: 1200, Lure: "Modest", Bite: "Legendary" } && shark.Weathers.SequenceEqual([7u, 8u])
                && shark.Transitions.SequenceEqual([1u, 2u]) && shark.Predators.SequenceEqual([(38939u, 4), (36449u, 5)]), "window, weathers, lure, predators");
            Check(data.Find(1) is null && data.Route(1).Count == 0, "unknown fish");
            // Older files (Data2.0.cs, read 2026-11-01): spaces before the parenthesis and the whole mooch chain in one call.
            const string older = @"
        data.Apply     (4869, Patch.ARealmReborn) // Merlthor Goby
            .Bait      (data, 2585)
            .Bite      (data, HookSet.Precise, BiteType.Weak)
            .Snag      (data, Snagging.None);
        data.Apply     (4904, Patch.ARealmReborn) // Moocher
            .Mooch     (data, 2585, 4869)
            .Bite      (data, HookSet.Powerful, BiteType.Strong);";
            var arr = GatherBuddyFishData.Parse([older]);
            Check(arr.Count == 2 && arr.Find(4869) is { Baits: [2585u], Snag: false }, "aligned calls read");
            Check(arr.Route(4904).SequenceEqual([2585u, 4869u]) && arr.Route(4869).SequenceEqual([2585u]), "whole chain kept, not doubled");
        }),
        ("Teamcraft bite times: range of the well reported seconds, usual second, few stray reports ignored",()=>
        {
            // Rows of the real answer for spot 35 (gubal, 2026-11-01): fish 4885 has 462 reports at 13 s, 10 at 9 s and 4 at 26 s.
            var times = FishBiteTimeSummary.Summarize([(4885u,13,462),(4885u,9,10),(4885u,26,4),(4870u,18,769),(4874u,17,446),(4874u,19,314),(4874u,23,8)]);
            Check(times[4885] is { Minimum:9, Maximum:13, Usual:13, Reports:476 },"4 reports at 26 s are under 2 % and dropped");
            Check(times[4874] is { Minimum:17, Maximum:19, Usual:17 } && times[4870] is { Minimum:18, Maximum:18 },"range and usual second (8 reports of 768 at 23 s dropped)");
            Check(!FishBiteTimeSummary.Summarize([(1u,5,2)]).ContainsKey(1),"too few reports");
        }),
    ];
}
