using TenshoAssistant.Reminders;
using TenshoAssistant.UI;

// 0.1256 (X-178, version 1: data and computations of the garden's icon view). The seed's item is noted with a bed (chat detection, seed
// picked in the list); growth, time left, marks and the wait for a missing icon are pure and tested here. Nothing visible yet.
internal static class GardenView1256Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static readonly DateTimeOffset Now=new(2026,11,13,12,0,0,TimeSpan.Zero);
    private static GardenBed Bed(double plantedHoursAgo,int grow,int tend=0,double? tendedHoursAgo=null)
        =>new(){Place="Mist · 1",Seed="Seed",PlantedAt=Now.AddHours(-plantedHoursAgo),GrowHours=grow,TendHours=tend,TendedAt=tendedHoursAgo is { } hours ? Now.AddHours(-hours):null};

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Garden view 1256: growth from 0 to 1, never beyond, 0 when planted in the future",() =>
        {
            Check(GardenView.Growth(Bed(0,48),Now)==0,"0 at planting");
            Check(Math.Abs(GardenView.Growth(Bed(24,48),Now)-0.5f)<0.0001f,"half way");
            Check(GardenView.Growth(Bed(48,48),Now)==1 && GardenView.Growth(Bed(500,48),Now)==1,"1 when ripe and after");
            Check(GardenView.Growth(Bed(-3,48),Now)==0,"planted in the future (clock change)");
        }),
        ("Garden view 1256: the time left is rounded down and null once ripe; its text key follows its size",() =>
        {
            Check(GardenView.Left(Bed(0,55),Now)==(2,7,0),"55 h: 2 d 7 h");
            Check(GardenView.Left(Bed(0,24),Now)==(1,0,0),"24 h exactly: 1 d 0 h");
            var bed=Bed(0,1);
            Check(GardenView.Left(bed,Now.AddMinutes(1))==(0,0,59),"59 min");
            Check(GardenView.Left(bed,Now.AddMinutes(1).AddSeconds(30))==(0,0,58),"58 min 30 s: rounded down, never ripe too early");
            Check(GardenView.Left(bed,Now.AddHours(1)) is null && GardenView.Left(bed,Now.AddHours(5)) is null,"ripe: no time left");
            Check(GardenView.LeftText((2,7,0))==("Common.LeftDays",2,7) && GardenView.LeftText((0,5,12))==("Common.LeftHours",5,12) && GardenView.LeftText((0,0,59))==("Common.LeftMinutes",59,0),"days, hours, minutes");
            foreach(var file in new[]{"en","fr"})
            {
                var text=Read($"src/TenshoAssistant/Localization/Languages/{file}.json");
                foreach(var key in new[]{"LeftDays","LeftHours","LeftMinutes"})Check(text.Contains($"    \"{key}\": \"{{0}}"),$"{file}: Common.{key}");
            }
        }),
        ("Garden view 1256: the mark of a bed (none, tending later, tending due, ripe)",() =>
        {
            Check(GardenView.Badge(Bed(1,48),Now)==GardenBadge.None,"no tending interval");
            Check(GardenView.Badge(Bed(1,48,24),Now)==GardenBadge.Later,"before the tending is due");
            Check(GardenView.Badge(Bed(30,48,24),Now)==GardenBadge.Due,"tending due");
            Check(GardenView.Badge(Bed(30,48,24,10),Now)==GardenBadge.Later,"tended since: later again");
            Check(GardenView.Badge(Bed(30,48,24,2),Now)==GardenBadge.None,"tended, the next tending would come after the harvest: none");
            Check(GardenView.Badge(Bed(50,48,24),Now)==GardenBadge.Ripe,"ripe wins");
        }),
        ("Garden view 1256: a crossbreed is never « not owned » without a reading of the inventories",() =>
        {
            Check(GardenView.Mark(true,true,3)==GardenCrossMark.Wished,"wished and owned");
            Check(GardenView.Mark(false,true,0)==GardenCrossMark.NotOwned,"not wished, read, none held");
            Check(GardenView.Mark(true,true,0)==GardenCrossMark.WishedNotOwned,"both");
            Check(GardenView.Mark(false,false,0)==GardenCrossMark.Unknown,"not read: unknown, never « not owned »");
            Check(GardenView.Mark(true,false,0)==GardenCrossMark.Wished,"wished, not read: wished only");
            Check(GardenView.Mark(false,true,12)==GardenCrossMark.None,"owned, not wished: no mark");
        }),
        ("Garden view 1256: the seed's item by its noted id, else by its name, else none; a narrow cell stacks",() =>
        {
            var bed=new GardenBed{Seed="Krakka Root Seeds",SeedId=7719};
            Check(GardenView.SeedIdOf(bed,_=>1)==7719,"the noted id wins");
            bed.SeedId=0;
            Check(GardenView.SeedIdOf(bed,name=>name=="Krakka Root Seeds" ? 7719u:null)==7719,"by the name");
            Check(GardenView.SeedIdOf(bed,_=>null) is null && GardenView.SeedIdOf(bed,_=>0u) is null,"unknown name: none");
            Check(GardenView.Stacked(99,23,8) && !GardenView.Stacked(116,23,8) && GardenView.Stacked(115.9f,23,8),"under four frame heights and three spacings: stacked");
        }),
        ("Garden view 1256: a missing icon shows an empty square for 60 frames, then the name",() =>
        {
            var wait=new IconWait();
            Check(wait.ShowText(0,false,10),"no icon id: the name at once");
            for(var frame=100;frame<160;frame++)Check(!wait.ShowText(5,false,frame),$"frame {frame-100}: still waiting");
            Check(wait.ShowText(5,false,160) && wait.ShowText(5,false,500),"from the 60th frame: the name");
            Check(!wait.ShowText(5,true,501) && !wait.ShowText(5,false,502) && wait.Count==1,"drawn once: the wait starts again");
            Check(!wait.ShowText(5,false,3),"a frame counter that went back: no name yet");
            for(uint id=10;id<10+IconWait.Capacity*3;id++)wait.ShowText(id,false,1000);
            Check(wait.Count<=IconWait.Capacity,"bounded memory");
        }),
        ("Garden view 1256: the journal keeps the seed's item; a bed noted before reads with none",() =>
        {
            var planted=GardenJournal.Plant("Pot","Mandrake Seeds",48,24,Now,7737);
            Check(planted.SeedId==7737 && GardenJournal.Plant("Pot","Typed",48,24,Now).SeedId==0,"Plant keeps it, 0 by default");
            var detected=GardenJournal.Detected([planted],"Mist 1-2 · patch 1, bed 3","Krakka Root Seeds",120,24,Now,7719);
            Check(detected is { Count:2 } && detected[1].SeedId==7719 && detected[0].SeedId==7737,"Detected keeps it and leaves the others");
            var json=System.Text.Json.JsonSerializer.Serialize(detected);
            var back=System.Text.Json.JsonSerializer.Deserialize<List<GardenBed>>(json)!;
            Check(back.Count==2 && back[0].SeedId==7737 && back[1].SeedId==7719 && back[1].Seed=="Krakka Root Seeds" && back[1].Id==detected![1].Id,"saved and read back");
            var old=System.Text.Json.JsonSerializer.Deserialize<List<GardenBed>>("[{\"Id\":\"0d1f3c5e-0000-4000-8000-000000000001\",\"Place\":\"Pot\",\"Seed\":\"Old Seed\",\"PlantedAt\":\"2026-11-01T10:00:00+00:00\",\"GrowHours\":24,\"TendHours\":0}]")!;
            Check(old.Count==1 && old[0].SeedId==0 && old[0].Seed=="Old Seed" && old[0].GrowHours==24,"a bed saved before 0.1256: read whole, no item");
            var page=Read("src/TenshoAssistant/MainWindow.Garden.cs");
            Check(page.Contains("DateTimeOffset.UtcNow,known.Seed.SeedId);"),"the chat detection notes the seed's item");
            Check(page.Contains("newBedSeedId=seed.SeedId;") && page.Contains("ref newBedSeed,GardenJournal.MaximumText))newBedSeedId=0;") && page.Contains("DateTimeOffset.UtcNow,newBedSeedId)];"),"the picked seed's item is noted, and dropped when another name is typed");
        }),
    ];
}
