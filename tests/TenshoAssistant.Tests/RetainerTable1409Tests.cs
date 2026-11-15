using TenshoAssistant.Character;
using TenshoAssistant.Inventory;

// X-196 slice 1 (0.1409, with X-307): every retainer of the character, read at a summoning bell, with its stock from the game's item finder
// copy, kept in the character file. Town names: the Town sheet's rows are RetainerTown's values (1 Limsa Lominsa … 12 Old Sharlayan, 14
// Tuliyollal; probe of 15/11, English and French clients).
internal static class RetainerTable1409Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file)).Replace("\r\n", "\n");
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 9, 0, 0, TimeSpan.Zero);

    private static RetainerState Sample(long gil = 12345) => RetainerStates.From("Shiru", "BTN", 100, gil, 120, 3, (uint)Now.AddDays(2).ToUnixTimeSeconds(),
        712, (uint)Now.AddHours(1).ToUnixTimeSeconds(), "Limsa Lominsa", (50, 4, 9000));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Retainer table 1409: the game's values as kept: a zero time is none, texts bounded", () =>
        {
            var retainer = Sample();
            Check(retainer is { Name: "Shiru", Job: "BTN", Level: 100, Gil: 12345, Items: 120, MarketItems: 3, Venture: 712, Town: "Limsa Lominsa", Stacks: 50, HqStacks: 4, Crystals: 9000 }, "copied");
            Check(retainer.MarketEnds == Now.AddDays(2) && retainer.VentureDone == Now.AddHours(1), "times");
            var idle = RetainerStates.From(" A ", "", 1, -5, 0, 0, 999, 0, 999, new string('t', 60), null);
            Check(idle.MarketEnds is null && idle.VentureDone is null, "nothing on sale, no venture: no end (the game's leftover times ignored)");
            Check(idle.Name == "A" && idle.Gil == 0 && idle.Town.Length == RetainerStates.MaximumText && idle.Stacks is null && idle.Crystals is null, "trimmed, bounded, stock unknown");
        }),
        ("Retainer table 1409: the stock from the game's copy: stacks, HQ apart, crystals added", () =>
        {
            var items = new (uint, ushort)[RetainerFinderCompare.Slots];
            items[0] = (5057, 99); items[1] = (1_005_057, 3); items[2] = (0, 0); items[3] = (4850, 0);
            var crystals = new ushort[RetainerFinderCompare.Crystals]; crystals[0] = 100; crystals[17] = 2;
            var copy = new FinderRetainer(1, false, items, new uint[RetainerFinderCompare.Equipped], crystals);
            Check(RetainerFinderCompare.Stock(copy) == (2, 1, 102), "two stacks (one HQ), empty slots and zero counts left out, 102 crystals");
        }),
        ("Retainer table 1409: merged like the rest: unknown keeps the last read, a change asks for a save", () =>
        {
            var old = new CharacterStatus { ContentId = 1, SeenAt = Now.AddDays(-1), Retainers = [Sample()] };
            var fresh = new CharacterStatus { ContentId = 1, SeenAt = Now };
            var merged = CharacterStatuses.Merge(old, fresh);
            Check(merged.Retainers is { Length: 1 } kept && kept[0] == Sample(), "away from a bell: the last read stays");
            Check(CharacterStatuses.Merge(old, fresh with { Retainers = [Sample(99)] }).Retainers![0].Gil == 99, "a new read wins");
            Check(!CharacterStatuses.Changed(merged, merged with { SeenAt = Now.AddSeconds(30), Retainers = [Sample()] }), "the same values: no save");
            Check(CharacterStatuses.Changed(merged, merged with { Retainers = [Sample(99)] }), "gil changed: saved");
            Check(CharacterStatuses.Changed(merged with { Retainers = null }, merged), "first read: saved");
        }),
        ("Retainer table 1409: behaviour: the character file keeps them; a file from before reads « unknown »; at most ten", () =>
        {
            var folder = Path.Combine(Path.GetTempPath(), "tensho-retainers-" + Guid.NewGuid().ToString("N"));
            try
            {
                var many = Enumerable.Range(0, 12).Select(index => Sample(index) with { Name = "R" + index + new string('x', 50) }).ToArray();
                CharacterStatuses.Write(folder, new CharacterStatus { ContentId = 5, Name = "A", SeenAt = Now, Retainers = many });
                var back = CharacterStatuses.Read(Path.Combine(folder, CharacterStatuses.FileName(5)), 5);
                Check(back.Retainers is { Length: CharacterStatuses.MaximumVentures } read && read[0].Name.Length == RetainerStates.MaximumText && read[3].Gil == 3
                    && read[0].MarketEnds == Now.AddDays(2), "written and read back, bounded");
                File.WriteAllText(Path.Combine(folder, CharacterStatuses.FileName(6)), "{\"Schema\":1,\"Status\":{\"ContentId\":6,\"Name\":\"B\",\"HuntBills\":2}}");
                var older = CharacterStatuses.Read(Path.Combine(folder, CharacterStatuses.FileName(6)), 6);
                Check(older.Retainers is null && older.HuntBills == 2, "a file of 0.1408 still reads, retainers unknown");
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }),
        ("Retainer table 1409: read at the bell with the game's copy, guarded on its own, named from the sheets", () =>
        {
            var reader = Read("Character/RetainerReader.Dalamud.cs");
            Check(reader.Contains("if(manager==null || !manager->IsReady)return null;", StringComparison.Ordinal), "not read before the list is opened");
            Check(reader.Contains("var copy=finder?.FirstOrDefault(row=>row.RetainerId==id);", StringComparison.Ordinal) && typeof(RetainerState).GetProperties().All(property => !property.Name.Contains("Id", StringComparison.Ordinal)), "joined by id, the id not kept");
            var watcher = Read("Character/CharacterStatusWatcher.Dalamud.cs");
            Check(watcher.Contains("Retainers=ReadRetainers(),", StringComparison.Ordinal) && watcher.Contains("try{return RetainerReader.Read(jobName,townName);}", StringComparison.Ordinal), "guarded on its own");
            var plugin = Read("Plugin.cs");
            Check(plugin.Contains("GetExcelSheet<Lumina.Excel.Sheets.ClassJob>().GetRowOrDefault(job)?.Abbreviation.ExtractText()", StringComparison.Ordinal)
                && plugin.Contains("GetExcelSheet<Lumina.Excel.Sheets.Town>().GetRowOrDefault(town)?.Name.ExtractText()", StringComparison.Ordinal), "job and town named from the sheets");
        }),
    ];
}
