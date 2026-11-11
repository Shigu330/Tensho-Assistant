namespace TenshoAssistant.Discoveries;

/// <summary>0.1096: where a gain of items came from when it is none of the causes of <see cref="ItemGains.Attribute"/>.</summary>
public enum GainSource { None, Venture, AirshipReturn, SubmarineReturn, Squadron, SquadronTraining }

/// <summary>
/// 0.1096 (user report, 11/11 15 h: « Item gain not attributed » for retainer ventures, an airship and a submarine back, and squadron titles):
/// the game's own log messages (LogMessage rows, read in the client's sheet: French texts quoted in docs/evidence/0.1096-gain-sources.md) that
/// say where the items came from, by row, in any language. The voyage lines come a few seconds after the items; the squadron's just before.
/// </summary>
public static class GainLogMessages
{
    public const uint SquadronMission=3937,SquadronTraining=3938;
    public const uint AirshipDone=4161,AirshipNewPoint=4163,AirshipRank=4164;
    public const uint SubmarineDone=6059,SubmarineNewPoint=6061,SubmarineRank=6062;

    public static GainSource SourceOf(uint logMessage)=>logMessage switch
    {
        AirshipDone=>GainSource.AirshipReturn,SubmarineDone=>GainSource.SubmarineReturn,SquadronMission=>GainSource.Squadron,SquadronTraining=>GainSource.SquadronTraining,
        _=>GainSource.None,
    };

    /// <summary>The voyage lines read for the log (new destination, rank), not gains: their name for the developer's log, or null.</summary>
    public static string? VoyageNote(uint logMessage)=>logMessage switch
    {
        AirshipNewPoint=>"airship new destination",SubmarineNewPoint=>"submarine new destination",AirshipRank=>"airship rank",SubmarineRank=>"submarine rank",
        _=>null,
    };
}

/// <summary>
/// 0.1096: gains no cause explained wait <see cref="Hold"/> ms for a source seen just before or just after them (a venture's result window, a
/// voyage's or a squadron's log message); those still alone after that are given back as not attributed. Called on the framework thread.
/// </summary>
public sealed class PendingGains<T>
{
    public const long Hold=6_000;
    private readonly List<(long At,T Gain)> pending=[];
    private readonly List<(long At,GainSource Source,uint Detail)> sources=[];

    public int Waiting=>pending.Count;

    public void Source(GainSource source,uint detail,long now)
    {
        if(source==GainSource.None)return;
        sources.Add((now,source,detail));
        sources.RemoveAll(seen=>now-seen.At>Hold*2);
    }

    public void Add(T gain,long now){if(pending.Count<64)pending.Add((now,gain));}

    /// <summary>The gains a source explains (the source closest in time, within <see cref="Hold"/>), and those that waited for nothing.</summary>
    public (IReadOnlyList<(T Gain,GainSource Source,uint Detail)> Explained,IReadOnlyList<T> Alone) Settle(long now)
    {
        var explained=new List<(T,GainSource,uint)>();var alone=new List<T>();
        for(var index=0;index<pending.Count;index++)
        {
            var (at,gain)=pending[index];
            var near=sources.Where(seen=>Math.Abs(seen.At-at)<=Hold).OrderBy(seen=>Math.Abs(seen.At-at)).Select(seen=>((GainSource,uint)?)(seen.Source,seen.Detail)).FirstOrDefault();
            if(near is { } found)explained.Add((gain,found.Item1,found.Item2));
            else if(now-at>=Hold)alone.Add(gain);
            else continue;
            pending.RemoveAt(index--);
        }
        return (explained,alone);
    }
}
