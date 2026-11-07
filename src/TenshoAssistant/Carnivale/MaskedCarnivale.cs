namespace TenshoAssistant.Carnivale;

/// <summary>W-02 (0.683): a stage of the Masked Carnivale, from AOZContent. Times in seconds. Information only, no advice on the fight (DEC-033): docs/evidence/0.683-masked-carnivale.md.</summary>
public sealed record CarnivaleStage(uint Row,int Order,IReadOnlyList<string> Enemies,int StandardSeconds,int IdealSeconds);

public enum CarnivaleReadStatus { Loaded,NotLoaded,Unavailable }

/// <summary>What the game holds, copied on the framework thread: one value per bit of the game's list, a stage's bit being its row minus one.</summary>
public sealed record CarnivaleSnapshot(CarnivaleReadStatus Status,IReadOnlyList<bool> Completed)
{
    public static CarnivaleSnapshot Of(CarnivaleReadStatus status)=>new(status,[]);
}

public sealed record CarnivaleRow(CarnivaleStage Stage,bool Done);

public static class CarnivaleBoard
{
    /// <summary>A stage beyond the game's list means the list has changed: nothing is shown rather than a wrong answer.</summary>
    public static CarnivaleReadStatus Check(IReadOnlyList<CarnivaleStage> stages,int bits)=>
        stages.All(stage=>stage.Row>=1 && stage.Row<=bits) ? CarnivaleReadStatus.Loaded:CarnivaleReadStatus.Unavailable;

    public static IReadOnlyList<CarnivaleRow> Rows(IReadOnlyList<CarnivaleStage> stages,CarnivaleSnapshot snapshot)
    {
        if(snapshot.Status!=CarnivaleReadStatus.Loaded)return [];
        return stages.OrderBy(stage=>stage.Order).Select(stage=>new CarnivaleRow(stage,snapshot.Completed[(int)stage.Row-1])).ToArray();
    }

    /// <summary>Minutes and seconds; zero is unknown, not no time at all.</summary>
    public static string Time(int seconds)=>seconds<=0 ? "":$"{seconds/60}:{seconds%60:00}";

    /// <summary>The enemies of a stage, each once, in the order of its acts; an unnamed row is left out.</summary>
    public static IReadOnlyList<string> Enemies(IEnumerable<string> names)=>names.Where(name=>name.Length>0).Distinct(StringComparer.Ordinal).ToArray();

    /// <summary>An act's fight type: 1 is an arrangement of several enemies, any other a single row of the bosses' sheet.</summary>
    public static bool IsArrangement(int fightType)=>fightType==1;
}
