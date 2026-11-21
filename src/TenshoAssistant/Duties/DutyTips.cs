namespace TenshoAssistant.Duties;

/// <summary>X-684 (0.1691): what a duty's tooltip says: its icon, its name, its kind and level, its zone, where you stand with it (completed,
/// unlocked, locked: the game's duty progress), the quest that unlocks it when the game names one, and its ids in developer mode.</summary>
public sealed record DutyTipFacts(string Name,uint Icon,string? Kind,string? Zone,string? Status,string? Unlock,string? Developer);

public static class DutyTips
{
    /// <summary>The facts with blanks left out (nothing said rather than an empty line).</summary>
    public static DutyTipFacts Build(string name,uint icon,string? kind,string? zone,string? status,string? unlock,string? developer)
    {
        static string? Kept(string? text)=>text is { } value && value.Trim() is { Length:>0 } trimmed ? trimmed:null;
        return new(name.Trim(),icon,Kept(kind),Kept(zone),Kept(status),Kept(unlock),Kept(developer));
    }

    /// <summary>The ids a contributor needs (developer mode only), as the Duties page wrote them before.</summary>
    public static string Ids(DutyDefinition duty)=>$"Duty {duty.DutyId} · instance {duty.InstanceId} · territory {duty.TerritoryId}";
}
