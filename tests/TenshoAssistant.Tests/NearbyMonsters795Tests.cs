using TenshoAssistant.Monsters;

// 0.795 (X-34, X-35, user request): nearby monsters that drop what the lists still need.
internal static class NearbyMonsters795Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Nearby monsters: only the drops still needed, each once; names per item",() =>
        {
            var drops=new Dictionary<uint,IReadOnlyList<uint>>{[10]=[100,101,100],[11]=[101],[12]=[102]};
            IReadOnlyList<uint> Of(uint id)=>drops.TryGetValue(id,out var list) ? list:[];
            var needed=new HashSet<uint>{100,101};
            Check(NearbyMonsters.ListDrops(10,Of,needed).SequenceEqual(new uint[]{100,101}),"each once");
            Check(NearbyMonsters.ListDrops(12,Of,needed).Count==0 && NearbyMonsters.ListDrops(10,Of,new HashSet<uint>()).Count==0,"nothing needed");
            var by=NearbyMonsters.DroppedNearby([(10,"Wolf"),(11,"Bat"),(10,"Wolf")],Of,needed);
            Check(by[101].SequenceEqual(new[]{"Wolf","Bat"}) && by[100].SequenceEqual(new[]{"Wolf"}) && !by.ContainsKey(102),"names");
        }),
        ("Nearby monsters: drops indexed by monster from MobDrop",() =>
        {
            var csv=new Dictionary<string,string>{["MobDrop"]="ItemId,BNpcNameId\n5000,42\n5001,42\n5000,43\n"};
            var data=new TenshoAssistant.Community.LuminaSupplementalData(csv);
            Check(data.ItemsDroppedBy(42).SequenceEqual(new uint[]{5000,5001}) && data.ItemsDroppedBy(43).SequenceEqual(new uint[]{5000}) && data.ItemsDroppedBy(44).Count==0,"index");
        }),
        ("Nearby monsters: living enemies only, bounded, behind their settings, information only",() =>
        {
            var reader=Read("Maps/MapLiveReader.Dalamud.cs");
            Check(reader.Contains("npc.BattleNpcKind!=Dalamud.Game.ClientState.Objects.Enums.BattleNpcSubKind.Combatant || !npc.IsTargetable") && reader.Contains("if(result.Count>=100)break;"),"enemies, bounded");
            Check(Read("MainWindow.MapView.cs").Contains("if(settings.ShowListMonsters && live?.Monsters is { Count:>0 } monsters"),"map setting");
            Check(Read("MainWindow.NearbyMonsters.cs").Contains("if(now-nearbyMonstersAt<1000)return;"),"once a second");
            Check(Read("MainWindow.ListPanels.cs").Contains("if(nearbyDroppedItems.TryGetValue(itemId,out var droppers))"),"list line");
        }),
    ];
}
