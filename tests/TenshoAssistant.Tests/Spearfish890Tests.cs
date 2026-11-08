// 0.890 (X-73.18), replaced in 0.891: the speared slot is the one that leaves the window with its size and speed kept (docs/evidence/0.891-spearfishing-slots.md).
// 0.891 (X-79): the panel's fixed list; (X-78) the fishing spot's map icon.
using TenshoAssistant.Discoveries;
using TenshoAssistant.Gathering;
internal static class Spearfish890Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Spearfishing: the slot that leaves with its size kept is the fish speared; one kind gives it, several give nothing",() =>
        {
            Check(ItemGains.SpearShadow([(2,200)])==(2,200) && ItemGains.SpearShadow([(2,200),(2,200)])==(2,200),"alike");
            Check(ItemGains.SpearShadow([(2,200),(1,150)]) is null && ItemGains.SpearShadow([]) is null && ItemGains.SpearShadow([(0,200)]) is null,"unknown");
            Check(DiscoveryStore.HasPlace(DiscoveryKind.SpearShadow) && !Enum.IsDefined(typeof(DiscoveryKind),26),"kind 27, 26 refused");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("if(spearShown[index] && !fish.Available && (int)fish.Size is >= 1 and <= 3 && fish.Speed>0)spearGone.Add(((int)fish.Size,fish.Speed,now));"),"slot left");
            Check(watcher.Contains("var speared=spearGone.Where(gone=>now-gone.At<=1_500)"),"1.5 s");
        }),
        ("Spearfishing panel: every fish of the node in a fixed order, yours first, in sight highlighted; fishing spot icon 60465 on the map",() =>
        {
            var slots=new[]{new SpearfishSlot(0,true,2,200,false,false),new SpearfishSlot(1,false,3,300,false,false)};
            var rows=Spearfishing.Rows([20146,20155,20164,99],fish=>fish==20155 ? (2,200):null,fish=>fish switch{20146=>(3,300),20164=>(1,200),20155=>(1,150),_=>null},slots);
            Check(rows.Select(row=>row.Fish).SequenceEqual(new uint[]{20164,20155,20146,99}),"order");
            Check(rows[1] is { Seen:true, InSight:true, Size:2 } && rows[2] is { InSight:false } && rows[3] is { Size:null, InSight:false },"seen first, in sight, unknown");
            Check(TenshoAssistant.Maps.MapLocalLists.FishingSpotIcon==60465,"icon");
        }),
    ];
}
