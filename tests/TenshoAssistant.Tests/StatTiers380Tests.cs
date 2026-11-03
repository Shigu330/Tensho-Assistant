using TenshoAssistant.Character;

// 0.380 (g, M-09): stat tiers — points to the next value and wasted points, solved exactly for the floor formulas and checked against
// a step-by-step search; the GCD searched; nothing without modifiers.
internal static class StatTiers380Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly LevelModifiers Level100 = new(440,420,2780);

    private static double Value(string key,int x)
    {
        var rows = StatFormulas.Combat(new Dictionary<string,int>{["CriticalHit"]=x,["DirectHitRate"]=x,["Determination"]=x,["SkillSpeed"]=x,["Tenacity"]=x,
            ["Piety"]=x,["Defense"]=x,["MagicDefense"]=x},30,1,Level100);
        return double.Parse(rows.Single(row=>row.Key==key).Value.Split(' ')[0],System.Globalization.CultureInfo.CurrentCulture);
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Stat tiers: exact thresholds match a step-by-step search of the shown values",() =>
        {
            Check(StatFormulas.Tier("CritRate",420,Level100)==(14,0),"first critical tier at 434");
            foreach (var key in new[]{"CritRate","DirectRate","Determination","Tenacity","SpeedBonus","Defense","Gcd"})
                foreach (var raw in new[]{420,433,434,1000,2513,3100})
                {
                    var tier = StatFormulas.Tier(key,raw,Level100) ?? throw new Exception("no tier "+key);
                    var now = Value(key,raw);
                    Check(Value(key,raw+tier.ToNext)!=now && (tier.ToNext==1 || Value(key,raw+tier.ToNext-1)==now),$"{key} {raw}: next");
                    Check(Value(key,raw-tier.Wasted)==now && Value(key,raw-tier.Wasted-1)!=now,$"{key} {raw}: wasted");
                }
        }),
        ("Stat tiers: no tier without modifiers or for an unknown key",() =>
        {
            Check(StatFormulas.Tier("CritRate",1000,new(0,0,0)) is null && StatFormulas.Tier("Strength",1000,Level100) is null,"none");
            Check(StatFormulas.Tier("Determination",1000,new(0,420,2780)) is null,"no main modifier, no determination tier");
        }),
    ];
}
