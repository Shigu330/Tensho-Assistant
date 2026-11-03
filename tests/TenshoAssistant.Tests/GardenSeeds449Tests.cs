using TenshoAssistant.Reminders;

// 0.449 (DEC-027, point 3): the garden journal's hours filled in from the embedded Lotlab seeds (GPL-3.0), still editable.
internal static class GardenSeeds449Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Garden seeds: the embedded Lotlab table is read with its notice, hours bounded, wilting hours become the tending reminder",() =>
        {
            var csv=Read("Reminders/Data/garden-seeds.csv");
            Check(csv.Contains("# Source: Lotlab/FFXIV-Gardening-Tracker") && csv.Contains("GNU General Public License v3.0"),"source and licence in the header");
            var seeds=GardenSeeds.Read(new StringReader(csv));
            Check(seeds.Count==107 && seeds.Select(seed=>seed.SeedId).Distinct().Count()==107,"107 seeds");
            var pepper=seeds.Single(seed=>seed.SeedId==7715);
            Check(pepper.HarvestId==4785 && GardenSeeds.GrowHours(pepper)==120 && GardenSeeds.TendHours(pepper)==48,"paprika seeds 120 h, wilt after 48 h");
            Check(seeds.Any(seed=>seed.WiltHours==0) && seeds.Where(seed=>seed.WiltHours==0).All(seed=>GardenSeeds.TendHours(seed)==0),"no wilting: no tending reminder");
            Check(seeds.All(seed=>GardenSeeds.GrowHours(seed) is >= 1 and <= GardenJournal.MaximumGrowHours),"growing hours within the journal's bounds");
        }),
        ("Garden seeds: malformed and repeated rows skipped; names kept within the journal's 40 characters",() =>
        {
            var seeds=GardenSeeds.Read(new StringReader("# note\nseed,harvest,grow,wilt\n1,2,24,0\n1,2,48,0\nx,2,3,4\n3,4,0,0\n5,6,24,-1\n7,8,24\n9,10,30,12\n"));
            Check(seeds.Count==2 && seeds[0]==new GardenSeed(1,2,24,0) && seeds[1]==new GardenSeed(9,10,30,12),"two valid rows");
            Check(GardenSeeds.JournalName("  Graines de poivron  ")=="Graines de poivron" && GardenSeeds.JournalName(new string('x',50))==new string('x',40)
                && GardenSeeds.JournalName("")==null && GardenSeeds.JournalName(null)==null,"journal names");
        }),
        ("Garden seeds: picked from a list in the game's language, the hours stay editable, embedded and credited",() =>
        {
            var ui=Read("MainWindow.Garden.cs");
            Check(ui.Contains("newBedGrow=GardenSeeds.GrowHours(seed);") && ui.Contains("newBedTend=GardenSeeds.TendHours(seed);")
                && ui.Contains("InputInt(localization.Get(\"Garden.Grow\")"),"picker fills, inputs remain");
            Check(Read("TenshoAssistant.csproj").Contains("<EmbeddedResource Include=\"Reminders\\Data\\garden-seeds.csv\" />"),"embedded");
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("\"TenshoAssistant.Reminders.Data.garden-seeds.csv\"") && plugin.Contains("data.GetExcelSheet<Lumina.Excel.Sheets.Item>();"),"names from the game's Item sheet");
            Check(Read("Credits/CreditCatalog.cs").Contains("new(\"LotlabGardening\", CreditKind.Embedded"),"credited");
        }),
    ];
}
