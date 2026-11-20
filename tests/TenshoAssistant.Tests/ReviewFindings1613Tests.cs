using TenshoAssistant.Beastmaster;
using TenshoAssistant.Diagnostics;

// 0.1613 (X-665, review of the block 0.1607 to 0.1612): M2, the map's quests not accepted read apart from the rest of the events; M3, the
// Beastmaster result kept number by number. M1 (no form of the user's values in the repository) is PrivateValues1607Tests.
internal static class ReviewFindings1613Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath(Path.Combine("src","TenshoAssistant",name)));
    private static readonly DateTimeOffset First=new(2026,11,20,14,0,0,TimeSpan.Zero);
    private static BoardResult Result(DateTimeOffset at,int? bonus=null,int? bonuses=null,int? total=null,BoardParts? parts=null,IReadOnlyList<int>? points=null)
        =>new(at,900,[new BeastGain(1,1,0,1,500)],bonus,bonuses,total,parts,points);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Review findings 1613: M2, a list read apart that fails is empty and its failure reported, the rest goes on",()=>
        {
            var bad=NativeShape.Apart<int>(()=>[NativeShape.Count(5000,1024,"markers")],out var failure);
            Check(bad.Count==0 && failure is UnexpectedGameStructureException,"empty, failure kept: "+failure);
            var good=NativeShape.Apart<int>(()=>[1,2,3],out var none);
            Check(good.SequenceEqual([1,2,3]) && none is null,"read, no failure");
        }),
        ("Review findings 1613: M2 wiring, the quests not accepted no longer throw out of the events part",()=>
        {
            var map=Read(Path.Combine("Maps","MapLiveReader.Dalamud.cs"));
            Check(map.Contains("var unaccepted=TenshoAssistant.Diagnostics.NativeShape.Apart(ReadGameUnaccepted,out unacceptedFailure);") && !map.Contains("var unaccepted=ReadGameUnaccepted();"),"read apart");
            Check(map.Contains("if(unacceptedDone){if(unacceptedFailure is { } missed)Failed(\"quests not accepted\",missed);else Read(\"quests not accepted\");}"),"said unavailable on their own");
        }),
        ("Review findings 1613: M3, a reading that knows other numbers adds them and keeps the bonus and total",()=>
        {
            // The review's case: the kept result knows the bonus, their number and the total (3); the new one the lines, the HP left and the points (3).
            // X-667 M1 (0.1619): the bonus group comes whole from one reading (the points with their bonus), as the window's reading gives it.
            var kept=Result(First,bonus:120,bonuses:2,total:1020,points:[60,60]);
            var merged=BeastResults.Keep(kept,Result(First.AddSeconds(2),parts:new(300,400,100,100,0,77)));
            Check(merged is { Bonus:120, Bonuses:2, Total:1020, Parts.HpLeft:77 } && merged.BonusPoints!.SequenceEqual([60,60]) && merged.At==First,"every number known kept: "+merged);
            Check(BeastResults.Keep(merged,Result(First.AddSeconds(4),parts:new(300,400,100,100,0,null))) is null,"the HP left unknown again: nothing changes");
            Check(BeastResults.Keep(merged,Result(First.AddSeconds(4))) is null,"nothing known: nothing changes");
            var changed=BeastResults.Keep(merged,Result(First.AddSeconds(5),bonus:180,bonuses:3,total:1080,points:[60,60,60]));
            Check(changed is { Bonus:180, Total:1080, Parts.HpLeft:77 } && changed.BonusPoints!.SequenceEqual([60,60,60]),"changed numbers taken, the others kept");
        }),
    ];
}
