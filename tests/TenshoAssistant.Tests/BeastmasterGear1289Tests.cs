using TenshoAssistant.Gear;

// X-277 (0.1289): as Beastmaster, the weapon and off hand slots stayed empty and the gear search found nothing for the job: the gear
// catalogue matched ClassJobCategory columns by job abbreviation, and the typed sheet has no BST column (« Unknown0 », patch 7.58).
// Rows probed in the game sheets on 13/11 (docs/evidence/0.1289-beastmaster-classjobcategory.md).
internal static class BeastmasterGear1289Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    // ClassJob rows 0 to 45 (Beastmaster 43); 44 and 45 are empty and have no abbreviation, so they are not jobs.
    private static readonly uint[] Jobs = [.. Enumerable.Range(0, 44).Select(job => (uint)job)];

    // A raw ClassJobCategory row: column 0 the name, then one boolean per ClassJob row (column = job + 1), 47 columns.
    private static Func<int, object?> Row(string name, params int[] jobs) => column => column == 0 ? name : column is > 0 and < 47 ? jobs.Contains(column - 1) : null;

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("X-277: the Beastmaster's own category (row 203) gives its gear to the Beastmaster", () =>
        {
            var mask = GearJobMask.Of(Jobs, Row("BST", 43));
            Check(mask == 1UL << 43, "Beastmaster only: " + mask);
            var axe = new GearItem(50742, "beastmaster's hand axe", 0, 50, 0, GearSlot.MainHand, false, mask, new Dictionary<int, int>(), new Dictionary<int, int>(), 0, false, false);
            var shield = axe with { Id = 50743, Name = "beastmaster's hoplon", Slot = GearSlot.OffHand };
            Check(axe.WearableBy(43) && shield.WearableBy(43) && !axe.WearableBy(19), "weapon and off hand wearable by the Beastmaster, not the paladin");
        }),
        ("X-277: shared categories keep every job and now include the Beastmaster (rows 56, 65, 84)", () =>
        {
            var mask = GearJobMask.Of(Jobs, Row("GLA CNJ THM PLD WHM BLM BST", 1, 6, 7, 19, 24, 25, 43));
            Check(mask == (1UL << 1 | 1UL << 6 | 1UL << 7 | 1UL << 19 | 1UL << 24 | 1UL << 25 | 1UL << 43), "row 56");
            var maiming = GearJobMask.Of(Jobs, Row("PGL LNC MNK DRG SAM RPR BST", 2, 4, 20, 22, 34, 39, 43));
            Check((maiming >> 43 & 1) == 1 && (maiming >> 39 & 1) == 1 && (maiming >> 41 & 1) == 0, "row 84: reaper and Beastmaster, not viper");
        }),
        ("X-277: a missing or unreadable column is not a job (no gear given by mistake)", () =>
        {
            Check(GearJobMask.Of([43, 70], column => column == 44 ? (object?)1 : null) == 0, "not a boolean, or a job out of range");
            Check(GearJobMask.Of([], Row("BST", 43)) == 0, "no job known");
        }),
        ("X-277: the gear loader reads the category raw, by column", () =>
        {
            var loader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gear/GearLoader.Dalamud.cs"));
            Check(loader.Contains("GetSheet<Lumina.Excel.RawRow>(null,\"ClassJobCategory\")") && loader.Contains("GearJobMask.Of(jobs,index=>category.ReadColumn(index))")
                && !loader.Contains("typeof(ClassJobCategory).GetProperties()"), "raw read, no name matching");
        }),
    ];
}
