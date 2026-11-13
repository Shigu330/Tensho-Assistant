using TenshoAssistant;
using TenshoAssistant.Gear;

// X-277 (0.1290), user decision of 13/11 about 23 h: « the 50-60-70-80-90-100 caps must show only 50-60-70-80-90-100, but every other
// level must show the gear close to it; it seems to do the opposite ». The rule of 0.430 (exact level outside the caps) is turned round,
// and the setting renamed GearExactAtCaps, the saved choice kept.
internal static class GearCaps1290Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static GearItem Head(uint id, int level, ulong jobs) =>
        new(id, $"I{id}", 0, level, level * 5, GearSlot.Head, false, jobs, new Dictionary<int, int>(), new Dictionary<int, int>(), 0, false, false);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("X-277: Beastmaster 45 sees its pieces of levels 40 to 45; at 50 only level 50; at 51 from 46 to 51", () =>
        {
            const uint beastmaster = 43;
            var catalog = new GearCatalog([.. new[] { 38, 40, 41, 45, 46, 49, 50, 51, 52 }.Select(level => Head((uint)level, level, 1UL << 43)), Head(999, 45, 1UL << 19)]);
            var filter = new GearFilter(5, true);
            Check(GearRules.LevelWindow(45, filter) == (40, 45), "45: window");
            Check(GearRules.LevelWindow(50, filter) == (50, 50), "50: cap, exact");
            Check(GearRules.LevelWindow(51, filter) == (46, 51), "51: window");
            Check(GearRules.LevelWindow(100, filter) == (100, 100) && GearRules.LevelWindow(99, filter) == (94, 99), "100 cap, 99 window");
            uint[] Levels(int level) => [.. GearRules.Candidates(catalog, GearSlot.Head, beastmaster, level, filter).Select(item => item.Id).OrderBy(id => id)];
            Check(Levels(45).SequenceEqual(new uint[] { 40, 41, 45 }), "45: " + string.Join(",", Levels(45)));
            Check(Levels(50).SequenceEqual(new uint[] { 50 }), "50: " + string.Join(",", Levels(50)));
            Check(Levels(51).SequenceEqual(new uint[] { 46, 49, 50, 51 }), "51: " + string.Join(",", Levels(51)));
            Check(GearRules.LevelWindow(50, filter with { ExactAtCaps = false }) == (45, 50), "option off: the window everywhere");
        }),
        ("X-277: the saved choice is kept across the rename, and the old name is not written again", () =>
        {
            // Dalamud saves with Newtonsoft: it reads a property through its setter and writes only the properties it can read. The old
            // name is a public property with a setter and no getter: read from a file saved before 0.1290, never saved again.
            var old = typeof(Configuration).GetProperty("GearExactOutsideCaps")!;
            Check(old.CanWrite && !old.CanRead && old.SetMethod!.IsPublic, "old name: public, write only");
            var off = new Configuration();
            Check(off.GearExactAtCaps, "default on");
            old.SetValue(off, false);
            Check(!off.GearExactAtCaps, "a player who had turned it off keeps it off");
            var on = new Configuration { GearExactAtCaps = false };
            old.SetValue(on, true);
            Check(on.GearExactAtCaps, "on stays on");
            var current = typeof(Configuration).GetProperty("GearExactAtCaps")!;
            Check(current.CanRead && current.CanWrite && ConfigurationReset.Reset.Contains("GearExactAtCaps") && !ConfigurationReset.Reset.Contains("GearExactOutsideCaps"), "the new name is the saved setting");
        }),
    ];
}
