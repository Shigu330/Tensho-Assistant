using TenshoAssistant.Maps;

// 0.625 (user report): the map closed by the game during an NPC's menu comes back.
internal static class MapResume625Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map resume: closed during an event, back once free, once",() =>
        {
            var resume=new MapResume();
            resume.GameClosedMap(true,100);
            Check(!resume.ShouldReopen(true,false,100),"still talking");
            Check(resume.ShouldReopen(false,false,100),"free again");
            Check(!resume.ShouldReopen(false,false,100),"only once");
        }),
        ("Map resume: closed by the player, another zone, or opened meanwhile: nothing",() =>
        {
            var resume=new MapResume();
            resume.GameClosedMap(false,100);
            Check(!resume.ShouldReopen(false,false,100),"the map key: it stays closed");
            resume.GameClosedMap(true,100);
            Check(!resume.ShouldReopen(false,false,200),"another zone");
            Check(!resume.ShouldReopen(false,false,100),"and it does not come back later");
            resume.GameClosedMap(true,100);
            Check(!resume.ShouldReopen(true,true,100) && !resume.ShouldReopen(false,false,100),"already open again");
        }),
        ("Job progress (T21): one row per experience slot, the job standing for its class, in the game's order",() =>
        {
            // ClassJob rows: 1 gladiator and 19 paladin share slot 1; 8 carpenter alone in slot 8.
            var jobs=TenshoAssistant.Gear.JobProgressList.Jobs(new Dictionary<uint,int>{[1]=1,[19]=1,[8]=8},new Dictionary<uint,int>{[19]=1,[8]=30});
            Check(string.Join(",",jobs.Select(job=>job.Job))=="19,8","paladin first, then carpenter");
            Check(new TenshoAssistant.Gear.JobProgressRow(19,0,0,0) is { Unlocked:false },"level 0: not unlocked");
            Check(Math.Abs(new TenshoAssistant.Gear.JobProgressRow(19,90,50,200).Fraction-0.25f)<0.001f,"share of the level");
            Check(new TenshoAssistant.Gear.JobProgressRow(19,100,0,0).Fraction==1,"nothing to earn");
            Check(new TenshoAssistant.Gear.JobProgressRow(19,90,500,200).Fraction==1,"never past the bar");
        }),
        ("Map resume: wired where the game closes its map",() =>
        {
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/NativeMapReplacer.Dalamud.cs"));
            Check(view.Contains("resume.GameClosedMap(Occupied(),client.TerritoryType);"),"noted on closing");
            Check(view.Contains("if(resume.ShouldReopen(Occupied(),visible,client.TerritoryType)"),"asked at each tick");
        }),
    ];
}
