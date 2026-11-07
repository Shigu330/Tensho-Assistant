using TenshoAssistant.Maps;
using TenshoAssistant.Monsters;

namespace TenshoAssistant;

// 0.795 (X-34, X-35, user request): nearby monsters that drop what the tracking lists still need; worked out once a second, drawn every frame.
internal sealed partial class MainWindow
{
    private long nearbyMonstersAt=long.MinValue;
    private IReadOnlyDictionary<uint,IReadOnlyList<uint>> nearbyMonsterDrops=new Dictionary<uint,IReadOnlyList<uint>>();
    private IReadOnlyDictionary<uint,IReadOnlyList<string>> nearbyDroppedItems=new Dictionary<uint,IReadOnlyList<string>>();
    private void RefreshNearbyMonsters()
    {
        var now=Environment.TickCount64;
        if(now-nearbyMonstersAt<1000)return;
        nearbyMonstersAt=now;
        var monsters=mapLive?.State?.Monsters ?? [];
        if(!configuration.ListPanelsNearbyMonsters || monsters.Count==0 || lumina?.State.Data is not { } data)
        {nearbyMonsterDrops=new Dictionary<uint,IReadOnlyList<uint>>();nearbyDroppedItems=new Dictionary<uint,IReadOnlyList<string>>();return;}
        RefreshRecipeHoldingsCache();
        var needed=new HashSet<uint>();
        foreach(var key in new[]{"CraftList","Workshop","Preparation"})
            if(ListPanelLinesFor(key) is { } known)
                foreach(var (item,amount) in known.Lines)
                    if(GetRecipeHoldings(item) is not { HasObservation:true } held || held.Total<amount)needed.Add(item);
        nearbyMonsterDrops=monsters.Select(monster=>monster.NameId).Distinct().ToDictionary(id=>id,id=>NearbyMonsters.ListDrops(id,data.ItemsDroppedBy,needed));
        nearbyDroppedItems=NearbyMonsters.DroppedNearby(monsters.Select(monster=>(monster.NameId,monster.Name)),data.ItemsDroppedBy,needed);
    }
    private IReadOnlyList<uint> MonsterListDrops(MapMonster monster)=>nearbyMonsterDrops.TryGetValue(monster.NameId,out var drops) ? drops:[];
}
