using TenshoAssistant.Gathering;

// 0.1653 (X-674): the collectable advisor's work, measured instead of a time: a test that holds under any load.
internal static class CollectableWork1653Tests
{
    /// <summary>The most calls of the search a hard case may take: 1 412 726 measured on 20/11 for « Collectable plan always ends », with room.</summary>
    public const long Budget=2_000_000;
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    // The rule and the window's values of CollectablesTests (docs/evidence/0.479-collectable-window-values.md).
    private static CollectableRule Ore()=>new CollectableRule(36291,"Ore",80,81,500,750,1000,1,2,20,true,"G") with {Low=400,Mid=700,High=1000,RewardLow=3,RewardMid=7,RewardHigh=20};
    private static CollectableTurn Turn(int current,int scour,int low,int high,int meticulous,int chance)
    {
        var raw=Enumerable.Range(0,59).Select(index=>$"{index}:uint 0").ToArray();
        raw[2]="2:uint 36291";raw[13]=$"13:uint {current}";raw[14]="14:uint 1000";
        raw[26]="26:uint 22182";raw[29]="29:uint 22183";raw[32]="32:uint 22184";raw[35]="35:uint 240";raw[38]="38:uint 22185";
        raw[48]=$"48:uint {scour}";raw[49]=$"49:uint {low}";raw[50]=$"50:uint {high}";raw[51]=$"51:uint {meticulous}";raw[52]="52:text 8 %";raw[54]="54:bool False";raw[56]="56:bool False";raw[41]="41:uint 21205";
        return CollectableTurn.Parse(raw)! with {MeticulousChance=chance};
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Collectable work 1653: the work is the turn's, the same each time (never the load's), within the budget, and the advice is the same with or without it",()=>
        {
            var turn=Turn(0,177,88,265,132,8) with {IntuitionChance=13};
            var bonuses=new CollectableBonuses(175,88,2000,200,100,false,true,300,6);
            var first=CollectableAdvisor.Advise(turn,6,Ore(),bonuses,out var work)!;
            var again=CollectableAdvisor.Advise(turn,6,Ore(),bonuses,out var workAgain)!;
            var plain=CollectableAdvisor.Advise(turn,6,Ore(),bonuses)!;
            Check(work>0 && work==workAgain && work<=Budget,$"{work} then {workAgain}");
            Check(first==plain && first==again,"the same advice: "+first+" / "+plain);
            Check(CollectableAdvisor.Advise(turn,0,Ore(),bonuses,out var none) is null && none==0 && CollectableAdvisor.Advise(turn,3,null,bonuses,out var noRule) is null && noRule==0,"nothing searched: no work");
            CollectableAdvisor.Advise(Turn(1000,177,88,265,132,0),3,Ore(),bonuses,out var done);
            Check(done>0 && done<work,$"an easy turn does less: {done} against {work}");
        }),
        ("Collectable work 1653: the plan's tests measure the work, never a time",()=>
        {
            var tests=File.ReadAllText(Path.GetFullPath("tests/TenshoAssistant.Tests/CollectablesTests.cs"));
            Check(!tests.Contains("Stopwatch",StringComparison.Ordinal) && !tests.Contains("ElapsedMilliseconds",StringComparison.Ordinal)
                && tests.Contains("work<=CollectableWork1653Tests.Budget",StringComparison.Ordinal),"no clock in the collectable tests");
        }),
    ];
}
