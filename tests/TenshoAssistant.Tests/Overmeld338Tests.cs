using TenshoAssistant.Gear;

// 0.338 (DEC-023, point 8): overmeld success rates from MateriaJoinRate / MateriaJoinRateGatherCraft, and the corrected grade rule.
internal static class Overmeld338Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Overmeld: rate by grade, advanced socket and quality; combat and crafting sheets; unknown when the data has none",() =>
        {
            var combat = new Dictionary<int,OvermeldRate> { [0] = new([80,40,20,10],[90,48,28,16]),[9] = new([12,0,0,0],[17,0,0,0]) };
            var craft = new Dictionary<int,OvermeldRate> { [0] = new([70,35,15,5],[75,40,20,10]) };
            var crit = new MateriaKind(15,27,[]);
            var cp = new MateriaKind(30,11,[]);
            var catalog = new GearCatalog([],[crit,cp],combat,craft);
            Check(catalog.OvermeldRate(crit,0,0,false) == (80f,false) && catalog.OvermeldRate(crit,0,3,true) == (16f,false),"combat: NQ first, HQ fourth");
            Check(catalog.OvermeldRate(cp,0,1,false) == (35f,false),"crafting stat: crafting sheet");
            Check(catalog.OvermeldRate(crit,10,1,true) == (10f,true) && catalog.OvermeldRate(crit,11,0,false) == (12f,true) && catalog.OvermeldRate(crit,11,1,true) == (0f,true),
                "XI and XII: Teamcraft's rates, said so");
            Check(catalog.OvermeldRate(crit,0,4,false) is null && catalog.OvermeldRate(crit,3,0,false) is null,"no column, or a grade nowhere: unknown");
            Check(new GearCatalog([],[crit]).OvermeldRate(crit,0,0,false) is null,"no rates loaded: unknown");
        }),
        ("Overmeld: VI, VIII, X and XII only in the first advanced socket, as the game data and the wiki say",() =>
        {
            foreach (var grade in new[] { 5,7,9,11 })
                Check(GearRules.OvermeldAllowed(grade,0) && !GearRules.OvermeldAllowed(grade,1) && !GearRules.OvermeldAllowed(grade,3),$"grade {grade + 1}: first only");
            Check(GearRules.OvermeldAllowed(10,3) && GearRules.OvermeldAllowed(8,2),"other grades in every advanced socket");
            Check(File.ReadAllText(Path.GetFullPath("docs/evidence/0.338-overmeld-rates.md")).Contains("| 9 (X) | 12 0 0 0 | 17 0 0 0 |",StringComparison.Ordinal),"evidence of the probe");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Gear.cs"));
            Check(page.Contains("GearRules.OvermeldAllowed(grade,socket-item.MateriaSlots)",StringComparison.Ordinal) && page.Contains("Tip(\"Gear.OvermeldRateTip\");",StringComparison.Ordinal),"editor uses the socket's rank and explains the rate");
        }),
    ];
}
