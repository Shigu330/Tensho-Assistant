using TenshoAssistant.Beastmaster;

// 0.1619 (X-667, review of the block 0.1613 to 0.1618): M1, the Beastmaster result's bonus group never mixed from two readings; M3, a place
// asked while the chat window is placed from outside is dropped; M4, invented names only (ChatCopy1617Tests); M5, the New-Tool skeleton built
// and tested by the CI (job « new-tool » of build.yml). M2 is said in PrivateValues1607Tests' header.
internal static class ReviewFindings1619Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath(name)).Replace("\r\n","\n");
    private static readonly DateTimeOffset First=new(2026,11,20,16,0,0,TimeSpan.Zero);
    private static BoardResult Result(DateTimeOffset at,int? bonus=null,int? bonuses=null,int? total=null,BoardParts? parts=null,IReadOnlyList<int>? points=null)
        =>new(at,900,[new BeastGain(1,1,0,1,50)],bonus,bonuses,total,parts,points);
    private static bool AddsUp(BoardResult result)=>result.Bonus is null
        ? result.Bonuses is null && result.Total is null && result.BonusPoints is null
        : result.BonusPoints is { } points && points.Count==result.Bonuses && points.Sum()==result.Bonus && result.Total==result.Score+result.Bonus;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Review findings 1619: M1, a new bonus group is taken whole: bonus, number, total and points add up",()=>
        {
            var kept=Result(First,bonus:120,bonuses:2,total:1020,points:[60,60]);
            var changed=BeastResults.Keep(kept,Result(First.AddSeconds(2),bonus:180,bonuses:3,total:1080,points:[60,60,60]));
            Check(changed is { Bonus:180, Bonuses:3, Total:1080 } && changed.BonusPoints!.SequenceEqual([60,60,60]) && AddsUp(changed),"the new group whole: "+changed);
        }),
        ("Review findings 1619: M1, a reading without the bonus keeps the first group whole and adds its lines",()=>
        {
            var kept=Result(First,bonus:120,bonuses:2,total:1020,points:[60,60]);
            var merged=BeastResults.Keep(kept,Result(First.AddSeconds(2),parts:new(300,400,100,100,0,77)));
            Check(merged is { Bonus:120, Bonuses:2, Total:1020, Parts.HpLeft:77 } && merged.BonusPoints!.SequenceEqual([60,60]) && AddsUp(merged) && merged.At==First,"kept group, new lines: "+merged);
            Check(BeastResults.Keep(merged,Result(First.AddSeconds(3))) is null,"nothing new: nothing changes");
        }),
        ("Review findings 1619: M1, a group without points in the first reading is never filled with another reading's points",()=>
        {
            var kept=Result(First,bonus:120,bonuses:2,total:1020);
            var merged=BeastResults.Keep(kept,Result(First.AddSeconds(2),bonus:180,bonuses:3,total:1080,points:[60,60,60]));
            Check(merged is { Bonus:180, Bonuses:3 } && merged.BonusPoints!.Count==3,"the new group whole");
            var none=BeastResults.Keep(Result(First,bonus:180,bonuses:3,total:1080,points:[60,60,60]),Result(First.AddSeconds(2),parts:new(300,400,100,100,0,null)));
            Check(none is { Bonus:180 } && none.BonusPoints!.Count==3,"the first group whole");
        }),
        ("Review findings 1619: M3, a place asked while placed from outside is taken and dropped, and the settings say so",()=>
        {
            var panel=Read("src/TenshoAssistant/PanelWindow.cs");
            var from=panel.IndexOf("private void PlaceOnce()",StringComparison.Ordinal);
            var body=panel[from..panel.IndexOf("ImGui.SetNextWindowSize(asked.Size",from,StringComparison.Ordinal)];
            Check(body.IndexOf("once = panel.Once();",StringComparison.Ordinal)<body.IndexOf("if (placed || once is not { } asked) return;",StringComparison.Ordinal)
                && !body.Contains("if (placed || panel.Once is null)",StringComparison.Ordinal),"taken first, then dropped while placed");
            var chat=Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(chat.Contains("if(chatFollowing||chatReduced)ImGui.TextColored(Warning,localization.Get(\"ChatWindow.PlaceFollowing\"));"),"said in the settings");
        }),
        ("Review findings 1619: M4, the chat copy test uses the repository's invented names",()=>
        {
            var test=Read("tests/TenshoAssistant.Tests/ChatCopy1617Tests.cs");
            Check(test.Contains("Ann Doe@Moogle") && !test.Contains("Aya"),"invented names");
        }),
        ("Review findings 1619: M5, the CI adds two skeleton pages and runs the whole Verify on them",()=>
        {
            var ci=Read(".github/workflows/build.yml");
            var from=ci.IndexOf("\n  new-tool:",StringComparison.Ordinal);
            Check(from>0,"the job new-tool");
            var job=ci[from..];
            var alone=job.IndexOf("./scripts/New-Tool.ps1 -Name CiNotes -Category Objects",StringComparison.Ordinal);
            var tab=job.IndexOf("./scripts/New-Tool.ps1 -Name CiGatherTab -Category Production -Parent Gathering",StringComparison.Ordinal);
            var verify=job.IndexOf("run: ./scripts/Verify.ps1 -Force -DotNet dotnet",StringComparison.Ordinal);
            Check(alone>0 && tab>alone && verify>tab,"a page alone, a tab of a family, then Verify");
            Check(job.Contains("runs-on: windows-latest") && job.Contains("./scripts/Get-Dalamud.ps1"),"on Windows, with Dalamud");
        }),
    ];
}
