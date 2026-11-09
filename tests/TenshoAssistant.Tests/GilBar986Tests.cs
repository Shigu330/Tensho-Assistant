using TenshoAssistant.Inventory;

// X-107 (0.986): the character's gil of the day, transfers left out, in the server bar when chosen.
internal static class GilBar986Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gil bar 986: since local midnight, character only, transfers out; off by default",() =>
        {
            var now=DateTimeOffset.Now;var midnight=new DateTimeOffset(now.Date,now.Offset);
            var entries=new[]{
                new GilEntry(midnight.AddMinutes(5),1000,0,GilSource.Character,GilCategory.Market),
                new GilEntry(midnight.AddMinutes(6),-300,0,GilSource.Character,GilCategory.Teleport),
                new GilEntry(midnight.AddMinutes(7),5000,0,GilSource.Character,GilCategory.Retainer),
                new GilEntry(midnight.AddMinutes(-5),777,0,GilSource.Character,GilCategory.Market),
                new GilEntry(midnight.AddMinutes(8),900,0,GilSource.Retainers,GilCategory.Market)};
            Check(GilJournal.Today(entries,now)==(1000,300),"today");
            Check(!new TenshoAssistant.Configuration().GilServerBar,"off by default");
        }),
    ];
}
