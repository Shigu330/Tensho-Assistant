namespace TenshoAssistant.Gathering;

/// <summary>What an effect changes in an ordinary gather; <see cref="Other"/> is shown but not estimated (collectable values, rates of high or low level items…).</summary>
public enum GatherEffectKind {Attempts,Yield,Boon,Rate,Other}
/// <summary>The stat a condition asks for, when Tensho can read it.</summary>
public enum GatherEffectStat {MaxGp,Gathering,Perception,Other}

/// <param name="Condition">The condition as the game words it, with its value.</param>
/// <param name="Text">The effect as the game words it, with its value.</param>
public sealed record GatherNodeEffect(GatherEffectStat Stat,int Needed,GatherEffectKind Kind,int Value,string Condition,string Text);

/// <summary>
/// 0.515 (user request): every effect of the game's « Node effect » box, from the GatheringPointBonus sheet, worded by the game. Ids and meanings: docs/evidence/0.515-node-effects.md.
/// The condition the game names « Max GP » depends on the maximum, so spending GP at the node does not lose it. An active effect on attempts is already in the count the window shows.
/// </summary>
public static class GatherNodeEffects
{
    private const uint ConditionGathering=14,ConditionPerception=15,ConditionMaxGp=19;
    private const uint TypeRate=1,TypeYield=14,TypeYieldUp=17,TypeAttempts=18,TypeAttemptsUp=19,TypeBoon=22,TypeBoonUp=23;

    /// <summary>The effect of a sheet row; null for an empty row: the game has no words for it, and does not show it either.</summary>
    public static GatherNodeEffect? From(uint condition,uint needed,string? conditionText,uint type,int value,string? typeText)
    {
        var words=typeText?.Trim();var asked=conditionText?.Trim().TrimEnd('#',' ');
        if(string.IsNullOrEmpty(words) || string.IsNullOrEmpty(asked) || needed>int.MaxValue)return null;
        var stat=condition switch {ConditionGathering=>GatherEffectStat.Gathering,ConditionPerception=>GatherEffectStat.Perception,ConditionMaxGp=>GatherEffectStat.MaxGp,_=>GatherEffectStat.Other};
        var kind=type switch {TypeRate=>GatherEffectKind.Rate,TypeYield or TypeYieldUp=>GatherEffectKind.Yield,TypeAttempts or TypeAttemptsUp=>GatherEffectKind.Attempts,TypeBoon or TypeBoonUp=>GatherEffectKind.Boon,_=>GatherEffectKind.Other};
        var plus=words.IndexOf('+');
        return new(stat,(int)needed,kind,value,$"{asked} {needed}",plus>=0 ? words.Insert(plus+1,value.ToString(System.Globalization.CultureInfo.InvariantCulture)):$"{words} {value}");
    }

    /// <summary>How much of the stat is missing: 0 when the effect is on; null when Tensho cannot read the stat (0 is never a reading here).</summary>
    public static int? Missing(GatherNodeEffect effect,int maxGp,int gathering,int perception)
    {
        ArgumentNullException.ThrowIfNull(effect);
        if(effect.Stat==GatherEffectStat.Other)return null;
        var have=effect.Stat switch {GatherEffectStat.MaxGp=>maxGp,GatherEffectStat.Gathering=>gathering,_=>perception};
        return have<=0 ? null:Math.Max(0,effect.Needed-have);
    }

    /// <summary>The node as it would be with this effect on, for the « with it » estimate; null when the effect cannot be estimated.</summary>
    public static GpScenario? With(GpScenario scenario,GatherNodeEffect effect)
    {
        ArgumentNullException.ThrowIfNull(scenario);ArgumentNullException.ThrowIfNull(effect);
        return effect.Kind switch
        {
            GatherEffectKind.Attempts=>scenario with {Attempts=Math.Min(12,scenario.Attempts+effect.Value)},
            GatherEffectKind.Yield=>scenario with {BaseYield=Math.Min(20,scenario.BaseYield+effect.Value)},
            GatherEffectKind.Boon=>scenario with {BoonPercent=Math.Min(100,scenario.BoonPercent+effect.Value)},
            GatherEffectKind.Rate=>scenario with {SuccessPercent=Math.Min(100,scenario.SuccessPercent+effect.Value)},
            _=>null,
        };
    }
}
