namespace TenshoAssistant.Discoveries;

/// <summary>
/// X-807 M4 (review of 0.1806): the trial base of the « Discoveries-trial » picture of the desktop application (never in the game, never
/// written): monsters seen at places of one map, each with how many players saw it in the common set (Imported) and your own sightings
/// (Seen), so the page's table shows « seen by N players », « seen by one player only » and its order (the most players first).
/// </summary>
public static class DiscoveryTrial
{
    /// <summary>The day of the trial facts (fixed: the picture does not follow the clock).</summary>
    public static readonly DateTimeOffset At=new(2026,11,20,0,0,0,TimeSpan.Zero);
    /// <summary>(players who saw it in the common set, your sightings) of each trial monster, in the order given.</summary>
    public static readonly (int Imported,int Seen)[] Counts=[(1,0),(5,0),(0,4),(3,2),(2,0),(1,1)];

    /// <summary>The trial facts: one per monster given (at most <see cref="Counts"/>), at its own 50-yalm cell of the map.</summary>
    public static DiscoveryFile File(IReadOnlyList<uint> monsters,uint territory,uint map)
    {
        var facts=new List<DiscoveryFact>();
        for(var i=0;i<monsters.Count && i<Counts.Length;i++)
        {
            var x=-160f+61f*i;var z=120f-47f*i;var (cellX,cellZ)=DiscoveryStore.Cell(x,z);
            facts.Add(new DiscoveryFact{Kind=DiscoveryKind.MonsterPlace,Subject=monsters[i],Territory=territory,Map=map,CellX=cellX,CellZ=cellZ,X=x,Z=z,
                LevelMin=8+2*i,LevelMax=9+2*i,Seen=Counts[i].Seen,Imported=Counts[i].Imported,First=At.AddDays(-3-i),Last=At});
        }
        return new DiscoveryFile{Written=At,Facts=facts};
    }
}
