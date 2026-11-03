namespace TenshoAssistant.Gear;

/// <summary>
/// g, M-06 (0.376): wear settings (user's choice: a Tensho alert and an on-screen bar, each optional, thresholds adjustable). Values
/// are clamped on use: repair below 1 to 99 %, materia extraction from 50 to 100 %.
/// </summary>
public sealed class GearWearSettings
{
    public bool Alert { get; set; } = true;
    public bool Bar { get; set; }
    public int RepairBelow { get; set; } = 10;
    public int SpiritbondAt { get; set; } = 100;
    public int Repair=>Math.Clamp(RepairBelow,1,99);
    public int Spiritbond=>Math.Clamp(SpiritbondAt,50,100);
}

/// <summary>The worn gear's state against the thresholds: pieces to repair, pieces ready for extraction, lowest condition, highest bond.</summary>
public sealed record GearWearState(int ToRepair,int ToExtract,int? LowestCondition,double? HighestSpiritbond);

public static class GearWear
{
    /// <summary>
    /// Spiritbond percent from the item's raw value: 10 000 points = 100 % (The Balance, consolegameswiki « Spiritbond »; to confirm in
    /// game, docs/evidence/0.376-gear-wear.md). Null when unknown or out of range.
    /// </summary>
    public static double? SpiritbondPercent(int? raw)=>raw is >= 0 and <= 10000 ? raw.Value/100.0:null;

    public static GearWearState Evaluate(IEnumerable<EquippedPiece> pieces,GearWearSettings settings)
    {
        int repair=0,extract=0;int? lowest=null;double? highest=null;
        foreach(var piece in pieces)
        {
            if(piece.Condition is { } condition)
            {
                lowest=lowest is { } low ? Math.Min(low,condition):condition;
                if(condition<settings.Repair)repair++;
            }
            if(SpiritbondPercent(piece.Spiritbond) is { } bond)
            {
                highest=highest is { } high ? Math.Max(high,bond):bond;
                if(bond>=settings.Spiritbond)extract++;
            }
        }
        return new(repair,extract,lowest,highest);
    }
}

/// <summary>
/// Says once when the worn gear starts needing a repair or holds a piece ready for extraction; armed again once it no longer does
/// (repaired, materia extracted). Unknown readings change nothing.
/// </summary>
public sealed class GearWearAlertTracker
{
    private bool repairing,extracting;
    public (bool Repair,bool Extract) Evaluate(GearWearState state)
    {
        var repair=state.ToRepair>0 && !repairing;var extract=state.ToExtract>0 && !extracting;
        if(state.LowestCondition is not null)repairing=state.ToRepair>0;
        if(state.HighestSpiritbond is not null)extracting=state.ToExtract>0;
        return (repair,extract);
    }
    public void Reset(){repairing=false;extracting=false;}
}
