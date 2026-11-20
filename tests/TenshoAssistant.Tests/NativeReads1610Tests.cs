using TenshoAssistant.Beastmaster;
using TenshoAssistant.Diagnostics;

// 0.1610 (X-590, external audit of 18/11, « native reads », R08). Behaviour tests: A05, a count out of bounds is a changed structure (thrown,
// the map keeps its last copy and says the part unavailable after a game update), never an empty list like a zone that is really empty;
// A06, the board result read again with fuller numbers replaces the first reading, a poorer reading never replaces a fuller one.
internal static class NativeReads1610Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset First=new(2026,1,5,20,0,0,TimeSpan.Zero);
    private static readonly BeastGain[] Team=[new(3,1,20,2,10),new(7,4,0,4,60)];
    private static BoardResult Result(DateTimeOffset at,int score=900,int? bonus=null,int? bonuses=null,int? total=null,BoardParts? parts=null,int[]? points=null)
        =>new(at,score,Team,bonus,bonuses,total,parts,points);
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Native reads 1610: A05, a count within its bound is read, 0 is an empty list",()=>
        {
            Check(NativeShape.Count(0,1024,"markers")==0 && NativeShape.Count(1024,1024,"markers")==1024 && NativeShape.Count(17,1024,"markers")==17,"within bounds");
        }),
        ("Native reads 1610: A05, a count out of bounds is a changed structure, never an empty list",()=>
        {
            foreach(var count in new long[]{-1,1025,long.MaxValue,long.MinValue})
            {
                try{NativeShape.Count(count,1024,"the map window's event markers");Check(false,"no list for "+count);}
                catch(UnexpectedGameStructureException error){Check(error.Message.Contains("after a game update") && error.Message.Contains(count.ToString()),error.Message);}
            }
        }),
        ("Native reads 1610: A05 wiring, the map's events keep their last copy and the part is said unavailable after a game update, once",()=>
        {
            var map=Read("Maps/MapLiveReader.Dalamud.cs");
            Check(map.Contains("NativeShape.Count(count,1024,\"the map window's event markers\");") && !map.Contains("if(count is <0 or >1024)return [];") && !map.Contains("if(count is <=0 or >1024)return [];"),"no empty list for a bad count");
            Check(map.Contains("try{events=ReadEvents();Read(\"events\");}catch(Exception error){Failed(\"events\",error);}"),"the last copy kept on a failure");
            Check(map.Contains("if(!failedReads.Add(part))return;") && map.Contains("unavailable after a game update"),"logged once, said after a game update");
        }),
        ("Native reads 1610: A06, a new board's result is kept; the same numbers read again change nothing",()=>
        {
            var kept=Result(First,bonus:120,bonuses:2,total:1020);
            Check(ReferenceEquals(BeastResults.Keep(null,kept),kept),"the first result");
            var other=Result(First.AddHours(1),score:950);
            Check(ReferenceEquals(BeastResults.Keep(kept,other),other),"another board");
            Check(BeastResults.Keep(kept,Result(First.AddSeconds(5),bonus:120,bonuses:2,total:1020)) is null,"the same window shown again");
        }),
        ("Native reads 1610: A06, the window filled after a first reading: the fuller second pass replaces it, at the first time",()=>
        {
            var early=Result(First);
            var full=Result(First.AddSeconds(2),bonus:120,bonuses:2,total:1020,parts:new(300,400,100,100,0,77),points:[60,60]);
            var kept=BeastResults.Keep(early,full);
            Check(kept is { Bonus:120, Bonuses:2, Total:1020, Parts.HpLeft:77 } && kept.BonusPoints!.SequenceEqual(new[]{60,60}),"bonus, total, lines and points kept");
            Check(kept!.At==First,"at the first reading's time");
        }),
        ("Native reads 1610: A06, a poorer reading never replaces a fuller one; changed numbers as complete replace them",()=>
        {
            var full=Result(First,bonus:120,bonuses:2,total:1020,parts:new(300,400,100,100,0,77),points:[60,60]);
            Check(BeastResults.Keep(full,Result(First.AddSeconds(3))) is null,"bonus and total unknown again: the fuller stays");
            var changed=BeastResults.Keep(full,Result(First.AddSeconds(3),bonus:180,bonuses:3,total:1080,parts:new(300,400,100,100,0,77),points:[60,60,60]));
            Check(changed is { Bonus:180, Total:1080 } && changed.At==First,"the window's numbers changed, as complete: the later one");
        }),
        ("Native reads 1610: A06 wiring, the page keeps the result Keep chooses",()=>
        {
            var page=Read("MainWindow.Beastmaster.cs");
            Check(page.Contains("if (BeastResults.Keep(plans.Result, result) is not { } kept) return;") && page.Contains("result = kept;") && !page.Contains("if (BeastResults.Same(plans.Result, result)) return;"),"Keep used");
        }),
    ];
}
