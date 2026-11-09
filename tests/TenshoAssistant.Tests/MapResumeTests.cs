using TenshoAssistant.Maps;

// 0.625 (user report): the map closed by the game during an NPC's menu comes back.
internal static class MapResumeTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map resume: closed during an event, back once free for a moment, once",() =>
        {
            var at=new DateTimeOffset(2026,11,6,12,0,0,TimeSpan.Zero);
            var resume=new MapResume();
            resume.GameClosedMap(true);
            Check(!resume.ShouldReopen(true,false,100,at),"still talking");
            Check(!resume.ShouldReopen(false,false,100,at.AddSeconds(1)),"free for a moment only");
            Check(resume.ShouldReopen(false,false,100,at.AddSeconds(3)),"free again");
            Check(!resume.ShouldReopen(false,false,100,at.AddSeconds(4)),"only once");
        }),
        ("Map resume: closed by the player or opened meanwhile: nothing",() =>
        {
            var at=new DateTimeOffset(2026,11,6,12,0,0,TimeSpan.Zero);
            var resume=new MapResume();
            resume.GameClosedMap(false);
            Check(!resume.ShouldReopen(false,false,100,at) && !resume.ShouldReopen(false,false,100,at.AddSeconds(5)),"the map key: it stays closed");
            Check(!resume.ShouldReopen(false,false,200,at.AddSeconds(9)),"the map key, then another zone: it stays closed");
            resume.GameClosedMap(true);
            Check(!resume.ShouldReopen(true,true,100,at) && !resume.ShouldReopen(false,false,100,at.AddSeconds(5)),"already open again");
            resume.GameClosedMap(true);
            resume.Cancel();
            Check(!resume.ShouldReopen(false,false,100,at.AddSeconds(5)),"cancelled");
        }),
        ("Map resume (0.658, 0.661): a teleport or a duty brings the map back; the gap before the loading screen does not",() =>
        {
            var at=new DateTimeOffset(2026,11,6,12,0,0,TimeSpan.Zero);
            var resume=new MapResume();
            resume.GameClosedMap(true);
            Check(!resume.ShouldReopen(false,false,958,at),"the flags drop");
            Check(!resume.ShouldReopen(false,false,958,at.AddSeconds(1)),"a second later, not yet");
            Check(!resume.ShouldReopen(true,false,958,at.AddSeconds(1.2)),"the loading screen");
            Check(!resume.ShouldReopen(false,false,0,at.AddSeconds(5)),"no territory yet");
            Check(!resume.ShouldReopen(false,false,130,at.AddSeconds(6)),"arrived: the moment starts again");
            Check(resume.ShouldReopen(false,false,130,at.AddSeconds(8)),"arrived in another zone");
            Check(!resume.ShouldReopen(false,false,130,at.AddSeconds(9)),"once");
            resume.GameClosedMap(true);
            Check(!resume.ShouldReopen(true,false,1000,at.AddSeconds(10)),"a duty's opening scene");
            Check(!resume.ShouldReopen(false,false,1000,at.AddSeconds(11)) && resume.ShouldReopen(false,false,1000,at.AddSeconds(13)),"free in the duty: back too");
            Check(!File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/NativeMapReplacer.Dalamud.cs")).Contains("ConditionFlag.Casting"),"the map key during a cast stays the player's");
        }),
        ("Map resume: wired where the game closes its map",() =>
        {
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/NativeMapReplacer.Dalamud.cs"));
            Check(view.Contains("resume.GameClosedMap(ClosedByEvent());"),"noted on closing");
            Check(view.Contains("resume.ShouldReopen(ClosedByEvent(),visible,client.TerritoryType,now)"),"asked at each tick");
            Check(view.Contains("resume.Cancel();"),"not after the competitive cut");
        }),
    ];
}
