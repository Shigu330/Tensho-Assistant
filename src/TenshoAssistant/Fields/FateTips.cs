namespace TenshoAssistant.Fields;

/// <summary>X-684 (0.1676): what a FATE's tooltip says: icon, name, level, kind (FATE, skirmish, critical engagement), place, its state
/// now when it is under way (progress, time left, experience bonus), the game's description, the players it takes.</summary>
public sealed record FateTipFacts(string Name,uint Icon,int? Level,string? Kind,string? Place,string? Now,string? Description,int Participants);

public static class FateTips
{
    /// <summary>The facts with blanks left out (nothing said rather than an empty line).</summary>
    public static FateTipFacts Build(string name,uint icon,int? level,string? kind,string? place,string? now,string? description,int participants)
    {
        static string? Kept(string? text)=>text is { } value && value.Trim() is { Length:>0 } trimmed ? trimmed:null;
        return new(name.Trim(),icon,level is > 0 ? level:null,Kept(kind),Kept(place),Kept(now),Kept(description),Math.Max(0,participants));
    }
}
