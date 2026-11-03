using TenshoAssistant.Community;
using TenshoAssistant.Gathering;

// R-15 (0.417): spearfishing help — GatherBuddy's size and speed per fish, matched to the game's spearfishing window slots.
internal static class Spearfishing417Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
    ];
}
