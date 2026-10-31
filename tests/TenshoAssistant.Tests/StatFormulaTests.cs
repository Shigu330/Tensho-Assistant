using TenshoAssistant.Character;

internal static class StatFormulaTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    // Level 100: Sub 420 and Div 2780 read from ParamGrow (docs/evidence/0.182.0-paramgrow-probe.log), Main 440 from the community table.
    private static readonly LevelModifiers Level100 = new(440,420,2780);
    private static string Value(IReadOnlyList<DerivedStat> rows,string key) => rows.First(row => row.Key == key).Value;
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Combat stats give real values with the game rounding and only the stats of the job",()=>
        {
            var base100 = new Dictionary<string,int>{["CriticalHit"]=420,["DirectHitRate"]=420,["Determination"]=440,["SkillSpeed"]=420,["SpellSpeed"]=1000,["Tenacity"]=420,["Piety"]=440};
            var tank = StatFormulas.Combat(base100,30,1,Level100);
            Check(Value(tank,"CritRate").StartsWith("5") && Value(tank,"CritDamage").StartsWith("140") && Value(tank,"DirectRate").StartsWith("0"),"base values: 5 % / 140 % / 0 %");
            Check(Value(tank,"Gcd").StartsWith("2") && Value(tank,"Gcd").Contains("50"),"skill speed at sub: 2.50 s");
            Check(tank.Any(row => row.Key=="Tenacity") && tank.All(row => row.Key!="Piety"),"tank: tenacity, no piety");
            // Spell Speed 1000 for a caster (category 31): -130 × 580 / 2780 = -27.1 → ceil -27 → 2.43 s.
            var caster = StatFormulas.Combat(base100,31,3,Level100);
            Check(Value(caster,"Gcd").StartsWith("2") && Value(caster,"Gcd").Contains("43") && caster.All(row => row.Key is not ("Tenacity" or "Piety")),"caster: spell speed 2.43 s, no tank or healer stat");
            Check(StatFormulas.Combat(new Dictionary<string,int>{["CriticalHit"]=3000},30,2,Level100).First(row=>row.Key=="CritRate").Value.StartsWith("23"),"crit 3000: 23.5 %");
            Check(StatFormulas.Combat(base100,31,4,Level100).Any(row=>row.Key=="Piety"),"healer: piety");
            Check(StatFormulas.Combat(base100,30,1,Level100 with { Main=0 }).All(row=>row.Key!="Determination"),"unknown main: no determination");
            Check(StatFormulas.Useful(32).SequenceEqual(["Gathering","Perception","GatheringPoints"]) && StatFormulas.Useful(30).Count==0,"gatherer stats only");
            Check(StatFormulas.MainOf(100)==440 && StatFormulas.MainOf(71)==296 && StatFormulas.MainOf(1)==20 && StatFormulas.MainOf(101) is null,"main table, levels 1 to 100");
        }),
    ];
}
