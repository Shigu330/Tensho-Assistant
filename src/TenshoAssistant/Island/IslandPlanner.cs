namespace TenshoAssistant.Island;

public sealed record IslandStep(IslandCraft Craft,int Start,bool Efficient,double Value);
public sealed record IslandChain(int Workshop,int Level,IReadOnlyList<IslandStep> Steps);
public sealed record IslandAgenda(IReadOnlyList<IslandChain> Chains,double Total);

/// <summary>
/// N-10 (0.672): a proposed agenda for one cycle. The best chain of 24 hours for each workshop in turn, the highest level first, each one seeing the supply the
/// chains before it have used. An efficient craft (a theme shared with the previous craft of its workshop, and another item) makes 2 units: community rule,
/// WorkshopOptimizerPlugin (MIT), written again here. Bounded, deterministic, and not the best agenda possible: docs/evidence/0.672-island-agenda.md.
/// </summary>
public static class IslandPlanner
{
    public const int CycleHours=24;

    public static bool Efficient(IslandCraft previous,IslandCraft next)=>previous.Id!=next.Id && previous.Themes.Any(next.Themes.Contains);

    public static IslandAgenda Propose(IslandCatalog catalog,IslandSnapshot island)
    {
        var crafts=catalog.Crafts.Where(craft=>craft.LevelRequired<=island.Rank && craft.Hours<=CycleHours).OrderBy(craft=>craft.Id).ToArray();
        var produced=new Dictionary<uint,int>();
        var chosen=new List<(IslandWorkshop Workshop,IReadOnlyList<IslandCraft> Crafts)>();
        foreach(var workshop in island.Workshops.Where(item=>!item.UnderConstruction).OrderByDescending(item=>item.Level).ThenBy(item=>item.Index))
        {
            var chain=BestChain(crafts,craft=>IslandValue.Unit(craft,catalog,island,workshop.Level,produced.GetValueOrDefault(craft.Id)));
            chosen.Add((workshop,chain));
            for(var index=0;index<chain.Count;index++)
                produced[chain[index].Id]=produced.GetValueOrDefault(chain[index].Id)+Units(chain,index);
        }
        return Timeline(catalog,island,chosen);
    }

    /// <summary>The chain of at most 24 hours worth the most at these unit values; a craft without a value is left out. The first found wins a tie.</summary>
    public static IReadOnlyList<IslandCraft> BestChain(IReadOnlyList<IslandCraft> crafts,Func<IslandCraft,double?> unit)
    {
        var usable=crafts.Select(craft=>(Craft:craft,Value:unit(craft))).Where(item=>item.Value is not null).Select(item=>(item.Craft,Value:item.Value!.Value)).ToArray();
        if(usable.Length==0)return [];
        var score=new double[CycleHours+1,usable.Length];
        var previous=new int[CycleHours+1,usable.Length];
        for(var hour=0;hour<=CycleHours;hour++)
            for(var index=0;index<usable.Length;index++){score[hour,index]=-1;previous[hour,index]=-1;}
        for(var index=0;index<usable.Length;index++)score[usable[index].Craft.Hours,index]=usable[index].Value;
        var bestScore=-1d;var bestHour=-1;var bestIndex=-1;
        for(var hour=1;hour<=CycleHours;hour++)
            for(var index=0;index<usable.Length;index++)
            {
                if(score[hour,index]<0)continue;
                if(score[hour,index]>bestScore){bestScore=score[hour,index];bestHour=hour;bestIndex=index;}
                for(var next=0;next<usable.Length;next++)
                {
                    var end=hour+usable[next].Craft.Hours;
                    if(end>CycleHours)continue;
                    var worth=score[hour,index]+(Efficient(usable[index].Craft,usable[next].Craft) ? 2:1)*usable[next].Value;
                    if(worth>score[end,next]){score[end,next]=worth;previous[end,next]=index;}
                }
            }
        var chain=new List<IslandCraft>();
        while(bestIndex>=0)
        {
            chain.Add(usable[bestIndex].Craft);
            var before=previous[bestHour,bestIndex];
            bestHour-=usable[bestIndex].Craft.Hours;
            bestIndex=before;
        }
        chain.Reverse();
        return chain;
    }

    private static int Units(IReadOnlyList<IslandCraft> chain,int index)=>index>0 && Efficient(chain[index-1],chain[index]) ? 2:1;

    /// <summary>The worth of the chains as they finish, in hour order: what is made first lowers the supply of what follows. The groove stays as read.</summary>
    private static IslandAgenda Timeline(IslandCatalog catalog,IslandSnapshot island,List<(IslandWorkshop Workshop,IReadOnlyList<IslandCraft> Crafts)> chosen)
    {
        var events=new List<(int End,int Chain,int Step,int Start)>();
        for(var chain=0;chain<chosen.Count;chain++)
        {
            var hour=0;
            for(var step=0;step<chosen[chain].Crafts.Count;step++)
            {
                events.Add((hour+chosen[chain].Crafts[step].Hours,chain,step,hour));
                hour+=chosen[chain].Crafts[step].Hours;
            }
        }
        var produced=new Dictionary<uint,int>();
        var steps=chosen.Select(item=>new IslandStep[item.Crafts.Count]).ToArray();
        var total=0d;
        foreach(var item in events.OrderBy(item=>item.End).ThenBy(item=>item.Chain))
        {
            var crafts=chosen[item.Chain].Crafts;
            var craft=crafts[item.Step];
            var units=Units(crafts,item.Step);
            var worth=units*(IslandValue.Unit(craft,catalog,island,chosen[item.Chain].Workshop.Level,produced.GetValueOrDefault(craft.Id)) ?? 0);
            produced[craft.Id]=produced.GetValueOrDefault(craft.Id)+units;
            steps[item.Chain][item.Step]=new(craft,item.Start,units==2,worth);
            total+=worth;
        }
        return new(chosen.Select((item,index)=>new IslandChain(item.Workshop.Index,item.Workshop.Level,steps[index])).ToArray(),total);
    }
}
