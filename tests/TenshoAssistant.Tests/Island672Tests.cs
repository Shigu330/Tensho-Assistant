using TenshoAssistant.Island;

// N-10 (0.672): the proposed agenda: the efficient rule, the search against every chain, the supply seen by the next workshops, its bounds.
internal static class Island672Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static IslandCraft Craft(uint id,int hours,int value,int level,params uint[] themes)=>Island671Tests.Craft(id,hours,value,level,themes);

    // Every chain of at most 24 hours, for the comparison.
    private static double Exhaustive(IReadOnlyList<IslandCraft> crafts,Func<IslandCraft,double> unit,IslandCraft? last,int hours)
    {
        var best=0d;
        foreach(var craft in crafts)
        {
            if(hours+craft.Hours>24)continue;
            var worth=(last is not null && IslandPlanner.Efficient(last,craft) ? 2:1)*unit(craft)+Exhaustive(crafts,unit,craft,hours+craft.Hours);
            if(worth>best)best=worth;
        }
        return best;
    }

    private static double Worth(IReadOnlyList<IslandCraft> chain,Func<IslandCraft,double> unit)
        =>chain.Select((craft,index)=>(index>0 && IslandPlanner.Efficient(chain[index-1],craft) ? 2:1)*unit(craft)).Sum();

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Island agenda: efficient means a shared theme and another item",() =>
        {
            var a=Craft(1,4,10,1,1,2);var b=Craft(2,4,10,1,2);var c=Craft(3,4,10,1,3);
            Check(IslandPlanner.Efficient(a,b) && IslandPlanner.Efficient(b,a),"shared theme");
            Check(!IslandPlanner.Efficient(a,c),"no shared theme");
            Check(!IslandPlanner.Efficient(a,a),"the same item");
        }),
        ("Island agenda: the search finds what trying every chain finds, within 24 hours",() =>
        {
            var crafts=new[]{Craft(1,4,30,1,1),Craft(2,6,50,1,1,2),Craft(3,8,70,1,2),Craft(4,4,20,1,3),Craft(5,8,90,1,3),Craft(6,6,40,1,2,3)};
            double Unit(IslandCraft craft)=>craft.Value;
            var chain=IslandPlanner.BestChain(crafts,craft=>Unit(craft));
            Check(Math.Abs(Worth(chain,Unit)-Exhaustive(crafts,Unit,null,0))<1e-9,"same worth");
            Check(chain.Sum(craft=>craft.Hours)<=24,"within the cycle");
            Check(IslandPlanner.BestChain(crafts,craft=>craft.Id==1 ? Unit(craft):null).All(craft=>craft.Id==1),"a craft without a value is left out");
            Check(IslandPlanner.BestChain(crafts,craft=>null).Count==0 && IslandPlanner.BestChain([],craft=>1d).Count==0,"nothing to choose");
        }),
        ("Island agenda: the second workshop sees the supply the first used",() =>
        {
            // Two crafts of equal worth when alone: the second workshop must not repeat the first without a reason.
            var crafts=new[]{Craft(1,8,100,1,1),Craft(2,8,100,1,1),Craft(3,8,100,1,2),Craft(4,8,100,1,2)};
            var catalog=Island671Tests.Sample(crafts);
            var one=IslandPlanner.Propose(catalog,Island671Tests.Snapshot(workshops:[new(0,1,false)]));
            var two=IslandPlanner.Propose(catalog,Island671Tests.Snapshot(workshops:[new(0,1,false),new(1,1,false)]));
            Check(two.Chains.Count==2 && two.Total>one.Total,"more workshops, more worth");
            Check(two.Total<one.Total*2+1e-6 && two.Chains[0].Steps.Count==3,"supply falls, three crafts of 8 hours");
            Check(two.Chains.All(chain=>chain.Steps[0].Start==0 && chain.Steps[1].Start==8 && !chain.Steps[0].Efficient),"the first craft of a chain makes one");
            var sum=two.Chains.SelectMany(chain=>chain.Steps).Sum(step=>step.Value);
            Check(Math.Abs(sum-two.Total)<1e-9,"the total is the sum of its steps");
        }),
        ("Island agenda: only ready workshops, the highest level first, crafts of the rank",() =>
        {
            var crafts=new[]{Craft(1,8,100,1,1),Craft(2,8,500,10,1)};
            var catalog=Island671Tests.Sample(crafts);
            var agenda=IslandPlanner.Propose(catalog,Island671Tests.Snapshot(rank:5,workshops:[new(0,1,false),new(1,3,false),new(2,4,true)]));
            Check(agenda.Chains.Count==2 && agenda.Chains[0].Workshop==1 && agenda.Chains[0].Level==3 && agenda.Chains[1].Workshop==0,"order, no workshop under work");
            Check(agenda.Chains.SelectMany(chain=>chain.Steps).All(step=>step.Craft.Id==1),"rank 5 hides the second craft");
            Check(IslandPlanner.Propose(catalog,Island671Tests.Snapshot()).Chains.Count==0,"no workshop");
        }),
        ("Island agenda: 81 crafts and 4 workshops stay short and give the same answer",() =>
        {
            var crafts=Enumerable.Range(1,81).Select(index=>Craft((uint)index,4+index%3*2,20+index,1,(uint)(index%16+1),(uint)((index+5)%16+1))).ToArray();
            var popularity=new byte[91];Array.Fill(popularity,(byte)3);
            var catalog=new IslandCatalog(91,crafts,[popularity],[0,140,120,100,80],[160,130,100,80,60],[0,100,110,120,130,140],new Dictionary<uint,string>());
            var bytes=new byte[91];Array.Fill(bytes,(byte)0x22);
            var island=new IslandSnapshot(IslandReadStatus.Loaded,DateTimeOffset.UnixEpoch,20,[new(0,4,false),new(1,4,false),new(2,4,false),new(3,4,false)],0,null,0,0,0,bytes);
            var watch=System.Diagnostics.Stopwatch.StartNew();
            var first=IslandPlanner.Propose(catalog,island);
            watch.Stop();
            var second=IslandPlanner.Propose(catalog,island);
            Check(watch.Elapsed.TotalSeconds<5,"bounded: "+watch.Elapsed);
            Check(first.Chains.SelectMany(chain=>chain.Steps).Select(step=>step.Craft.Id).SequenceEqual(second.Chains.SelectMany(chain=>chain.Steps).Select(step=>step.Craft.Id)),"deterministic");
            Check(first.Chains.All(chain=>chain.Steps.Sum(step=>step.Craft.Hours)<=24),"within the cycle");
        }),
        ("Island agenda: nothing goes to the game and nothing is computed on Draw",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.IslandSanctuary.cs"));
            Check(page.Contains("islandPlanning=Task.Run(()=>IslandPlanner.Propose(catalog,current));"),"off Draw, on a click");
            Check(!File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Island/IslandPlanner.cs")).Contains("FFXIVClientStructs",StringComparison.Ordinal),"pure");
        }),
    ];
}
