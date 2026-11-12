using TenshoAssistant.Fields;

// 0.1178 (X-188 panel and X-190 coins, user's request of 12/11 on the Occult Crescent, docs/evidence/0.1178-occult-panel.md): the panel on the island
// (worn job, its experience, what it learns next, knowledge, coins), shown only while the island's director is there; « what it buys » for each coin.
internal static class OccultPanel1178Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026, 11, 12, 13, 57, 33, TimeSpan.Zero);
    private static readonly OccultJob Freelancer = new(0, "Freelance fantôme", 24, [new(5, "A5", "", 0, false)]);
    private static readonly OccultJob Knight = new(1, "Paladin fantôme", 6, [new(1, "A1", "", 0, false), new(3, "A3", "", 0, false), new(6, "A6", "", 0, false)]);
    private static OccultState State(int current = 1, uint needed = 2400, int knightLevel = 2) =>
        new(At, 465, 2800, current, [1, knightLevel, 0, 1, .. new int[20]], [0, 900, 0, 0, .. new uint[20]], needed, 270, 12);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Occult panel 1178: the worn job, its experience out of what it needs, nothing once mastered or unread", () =>
        {
            Check(OccultCrescent.Worn([Freelancer, Knight], State())?.Id == 1, "the knight is worn");
            Check(OccultCrescent.Worn([Freelancer], State()) is null, "a worn job missing from the sheets: none");
            Check(OccultCrescent.Worn([Freelancer, Knight], State(current: -1)) is null, "no job worn");
            Check(OccultCrescent.WornExperience(Knight, State()) is (900, 2400), "experience of the worn job");
            Check(OccultCrescent.WornExperience(Knight, State(needed: 0)) is null, "nothing needed read: no bar");
            Check(OccultCrescent.WornExperience(Knight, State(knightLevel: 6)) is null, "mastered: no bar");
            Check(OccultCrescent.WornExperience(new OccultJob(40, "x", 5, []), State(current: 40)) is null, "a job past the state: no bar, no crash");
        }),
        ("Occult panel 1178: each coin once, in the sheet's order, empty rows left out", () =>
        {
            var coins = OccultCrescent.Coins([new(45043, "Silver"), new(45044, "Gold"), new(0, "none"), new(51975, "North silver"), new(45043, "Silver"), new(51976, "")]);
            Check(coins.Select(coin => coin.ItemId).SequenceEqual([45043u, 45044u, 51975u]), "coins: " + string.Join(",", coins.Select(coin => coin.ItemId)));
        }),
        ("Occult panel 1178: occult records read out of all, other kinds left out", () =>
        {
            var kinds = TenshoAssistant.Collections.CollectionKind.OccultLore;
            var lore = OccultCrescent.Lore([new(kinds, 1, "Ketenramm", 0, true), new(kinds, 2, "Ville fantôme", 0, false), new(kinds, 3, "Archive", 0, true),
                new(TenshoAssistant.Collections.CollectionKind.Mount, 4, "Chocobo", 0, true)]);
            Check(lore == (2, 3), $"read {lore.Read} of {lore.Total}");
            Check(OccultCrescent.Lore([]) == (0, 0), "nothing read yet");
            var collections = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Collections.cs"));
            Check(collections.Contains("focusCollectionKind==extraKind ? ImGuiTabItemFlags.SetSelected", StringComparison.Ordinal), "the extra kinds' tabs follow a jump");
        }),
        ("Occult panel 1178: wiring: the panel only on the island, its switch on, coins from MKDData, « what it buys » opens the currency's tab", () =>
        {
            string Read(string path) => File.ReadAllText(Path.GetFullPath(path));
            var reader = Read("src/TenshoAssistant/Fields/OccultCrescentReader.Dalamud.cs");
            Check(reader.Contains("Volatile.Write(ref onIsland,director!=null);", StringComparison.Ordinal) && reader.Contains("Volatile.Write(ref onIsland,false);", StringComparison.Ordinal), "on the island: the director now; a failed reading: not on the island");
            var page = Read("src/TenshoAssistant/MainWindow.FieldContent.cs");
            Check(page.Contains("if(windowDisposed || OccultOnIsland?.Invoke()!=true || OccultStateRead?.Invoke() is not { } state)return false;", StringComparison.Ordinal), "the panel only on the island, once read");
            Check(page.Contains("()=>configuration.OccultPanel,OccultPanelVisible,DrawOccultPanelContents", StringComparison.Ordinal) && page.Contains("OpenCurrencyValue(coins[index].ItemId)", StringComparison.Ordinal), "panel and coin buttons");
            Check(Read("src/TenshoAssistant/MainWindow.TrackerWindow.cs").Contains("new(this,OccultPanelDefinition)", StringComparison.Ordinal), "the panel is a PanelWindow");
            Check(Read("src/TenshoAssistant/Fields/FieldBoard.Dalamud.cs").Contains("GetExcelSheet<MKDData>()", StringComparison.Ordinal), "coins from MKDData");
            Check(new TenshoAssistant.Configuration().OccultPanel, "on by default");
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("mainWindow.OccultOnIsland=()=>occultReader?.OnIsland==true;", StringComparison.Ordinal), "the reader gives the page whether the island is there");
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.1178-occult-panel.md")), "evidence");
        }),
    ];
}
