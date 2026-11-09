// Test audit of 2026-11-09, batch 2 (0.1021): Spearfishing417Tests, Spearfish890Tests, SpearMulti898Tests merged into SpearfishingTests; every case is kept.
using TenshoAssistant.Community;
using TenshoAssistant.Gathering;
using TenshoAssistant.Discoveries;
using TenshoAssistant.Maps;
internal static class SpearfishingTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From Spearfishing417Tests: R-15 (0.417): spearfishing help — GatherBuddy's size and speed per fish, matched to the game's spearfishing window slots.
        ("Spearfishing: GatherBuddy « .Spear » read as the game's size and speed values",() =>
        {
            // Statement shape of GatherBuddy.GameData/Data/Fish/Data7.0.cs (docs/evidence/0.417-spearfishing.md).
            var data=GatherBuddyFishData.Parse([
                "data.Apply(43803, Patch.Dawntrail) // Turali Loach\n            .Spear(data, SpearfishSize.Small, SpearfishSpeed.ExtremelySlow);",
                "data.Apply(43810, Patch.Dawntrail)\n            .Spear(data, SpearfishSize.Large, SpearfishSpeed.VeryFast);",
                "data.Apply(43811, Patch.Dawntrail)\n            .Spear(data, SpearfishSize.Unknown, SpearfishSpeed.Unknown);",
                "data.Apply(43664, Patch.Dawntrail)  // Mosquito Fish\n            .Bait(data, 43852)\n            .Bite(data, HookSet.Precise, BiteType.Weak);",
            ]);
            Check(data.Find(43803) is { SpearSize:1, SpearSpeed:150 } && data.Find(43810) is { SpearSize:3, SpearSpeed:400 },"size and speed");
            Check(data.Find(43811) is { SpearSize:null, SpearSpeed:null } && data.Find(43664) is { SpearSize:null, Baits.Count:1 },"unknown and rod fish");
            Check(data.Spearfish.Select(fish=>fish.FishId).Order().SequenceEqual([43803u,43810u]),"spearfish only");
            Check(Spearfishing.Speed("LynFast")==600 && Spearfishing.Speed("SuperSlow")==100 && Spearfishing.Speed("Unknown") is null && Spearfishing.SpeedName(350)=="Fast" && Spearfishing.SpeedName(123) is null,"speed names");
        }),
        ("Spearfishing: candidates by size and speed, the zone's first; nothing for an empty slot",() =>
        {
            GatherBuddyFish Fish(uint id,int size,int speed)=>new(id,[],[],null,null,null,null,[],[],null,[],false,size,speed);
            var fish=new[]{Fish(1,2,300),Fish(2,2,300),Fish(3,2,350),Fish(4,3,300)};
            var result=Spearfishing.Candidates(fish,new SpearfishSlot(0,true,2,300,false,false),id=>id==2);
            Check(result.InZone.SequenceEqual([2u]) && result.Elsewhere.SequenceEqual([1u]),"match and zone");
            Check(Spearfishing.Candidates(fish,new SpearfishSlot(1,false,2,300,false,false),_=>true).InZone.Count==0
                && Spearfishing.Candidates(fish,new SpearfishSlot(1,true,-1,300,false,false),_=>true).Elsewhere.Count==0,"empty or unknown slot");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/SpearfishingReader.Dalamud.cs"));
            Check(reader.Contains("GetAddonByName(\"SpearFishing\")") && reader.Contains("framework.IsInFrameworkUpdateThread") && !reader.Contains("FireCallback"),"read only, framework thread");
        }),
        // From Spearfish890Tests: 0.890 (X-73.18), replaced in 0.891: the speared slot is the one that leaves the window with its size and speed kept (docs/evidence/0.891-spearfishing-slots.md).
        // 0.891 (X-79): the panel's fixed list; (X-78) the fishing spot's map icon.
        ("Spearfishing: the slot that leaves with its size kept is the fish speared; one kind gives it, several give nothing",() =>
        {
            Check(ItemGains.SpearShadow([(2,200)])==(2,200) && ItemGains.SpearShadow([(2,200),(2,200)])==(2,200),"alike");
            Check(ItemGains.SpearShadow([(2,200),(1,150)]) is null && ItemGains.SpearShadow([]) is null && ItemGains.SpearShadow([(0,200)]) is null,"unknown");
            Check(DiscoveryStore.HasPlace(DiscoveryKind.SpearShadow) && (int)DiscoveryKind.SpearShadow==28 && !Enum.IsDefined(typeof(DiscoveryKind),26) && !Enum.IsDefined(typeof(DiscoveryKind),27),"kind 28, 26 and 27 refused");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("if(fish.Available && !spearShown[index])spearFirst[index]=(size,fish.Speed);"),"speed when it appeared");
            Check(watcher.Contains("if(spearPending is { } pending && (item>0 && item!=pending.Item || now>=pending.Until))"),"item lag");
            Check(Spearfishing.Shadows(new SpearfishSlot(0,true,3,800,false,false)).SequenceEqual(new[]{(3,800),(3,400)}),"doubled speed");
            Check(watcher.Contains("var speared=spearGone.Where(gone=>now-gone.At<=1_500)"),"1.5 s");
        }),
        ("Spearfishing panel: every fish of the node in a fixed order, yours first, in sight highlighted; fishing spot icon 60465 on the map",() =>
        {
            var slots=new[]{new SpearfishSlot(0,true,2,200,false,false),new SpearfishSlot(1,false,3,300,false,false)};
            var rows=Spearfishing.Rows([20146,20155,20164,99],fish=>fish==20155 ? (2,200):null,fish=>fish switch{20146=>(3,300),20164=>(1,200),20155=>(1,150),_=>null},slots);
            Check(rows.Select(row=>row.Fish).SequenceEqual(new uint[]{20164,20155,20146,99}),"order");
            Check(rows[1] is { Seen:true, InSight:true, Size:2 } && rows[2] is { InSight:false } && rows[3] is { Size:null, InSight:false },"seen first, in sight, unknown");
            Check(TenshoAssistant.Maps.MapLocalLists.FishingSpotIcon==60465,"icon");
            // 0.899: a capture line limit per window.
            var capture=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs"));
            Check(capture.Contains("Line(\"addons\",160,$\"{name}:{all}\",\"addons/\"+name);"),"limit per window");
            // 0.897: S/M/L and chevrons.
            Check(Spearfishing.ShortShadow(1,150)=="S >>" && Spearfishing.ShortShadow(2,200)=="M >>>" && Spearfishing.ShortShadow(3,400)=="L >>>>>>>" && Spearfishing.ShortShadow(3,0)=="L","short shadow");
            // 0.892: a fixed layout and the node kept once found.
            var panel=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.SpearfishingPanel.cs"));
            Check(panel.Contains("for(var index=0;index<3;index++)") && panel.Contains("var listed=ofPoint ? rows:rows.Where(row=>row.Seen || row.InSight).ToArray();"),"fixed layout");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/SpearfishingReader.Dalamud.cs"));
            Check(reader.Contains("if(lastPoint==0 && point() is var target and not 0)"),"node kept");
            // 0.894: the node by its base; fishing spots with an area.
            var factor=TenshoAssistant.Maps.MapLocalIndexCache.FishingSpotRadiusFactor;
            Check(panel.Contains("source.BaseId==point && source.TypeId is 4 or 5") && factor>0.14f && factor<0.17f,"base, area");
            var loader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapLocalIndexLoader.Dalamud.cs"));
            Check(loader.Contains("spot.Radius*MapLocalIndexCache.FishingSpotRadiusFactor/scale"),"fishing spot radius");
            Check(panel.Contains("DrawItemIcon(items?.Find(row.Fish),new Vector2(side));") && panel.Contains("DrawItemIcon(items?.Find(match.Fish),new Vector2(ImGui.GetTextLineHeight()));"),"icons");
        }),
        // From SpearMulti898Tests: 0.898 (X-73.18): the shadows of a multiple spearfishing catch, from the fish the chat names and the slots the window shows speared.
        ("Spearfishing multiple catch: alike slots give all, a known fish takes its shadow, the last one the rest, ambiguous left out",() =>
        {
            (int,int)? None(uint item)=>null;
            Check(ItemGains.SpearAssign([10,11],[(1,200),(1,200)],None).SequenceEqual(new[]{(10u,1,200),(11u,1,200)}),"alike");
            var known=ItemGains.SpearAssign([10,11],[(3,300),(1,200)],item=>item==11 ? (1,200):null);
            Check(known.Count==2 && known.Contains((11u,1,200)) && known.Contains((10u,3,300)),"elimination");
            Check(ItemGains.SpearAssign([10,11],[(3,300),(1,200)],None).Count==0,"ambiguous");
            Check(ItemGains.SpearAssign([10,11,12],[(3,300),(1,200)],None).Count==0 && ItemGains.SpearAssign([],[],None).Count==0,"counts differ");
            var three=ItemGains.SpearAssign([10,11,12],[(3,300),(1,200),(1,200)],item=>item==10 ? (3,300):null);
            Check(three.Count==3 && three.Contains((11u,1,200)) && three.Contains((12u,1,200)),"rest alike");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("if((int)message.LogKind!=67 || message.Sender.TextValue.Trim().Length!=0)return;") && watcher.Contains("()=>chat.ChatMessageUnhandled-=OnSpearChat)"),"chat, owned");
        }),
        ("Gathering sources (C29): spearfishing points name their fish, and the cache follows",() =>
        {
            var loader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/GatheringCatalogLoader.Dalamud.cs"));
            Check(loader.Contains("reference.RowType == typeof(SpearfishingItem)"),"read before the gathering items are filtered");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/GatheringCatalogCache.cs")).Contains("CopyRules=2"),"an old cache without them is rebuilt");
        }),
    ];
}
