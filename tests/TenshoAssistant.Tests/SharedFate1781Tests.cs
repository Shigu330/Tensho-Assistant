using TenshoAssistant.Fields;

// X-213 (the user's request of 12/11, K-23): the shared FATE ranks, read from the game's window once the player opened it (AgentFateProgress,
// read only), kept per character; the gemstone trader's items still to come by rank, from the sheets (FateShop shops rank by rank, the last
// rank's items behind the zone's rank achievement). Unknown until the window was opened, never 0 (R06).
internal static class SharedFate1781Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static readonly DateTimeOffset Start = new(2026, 11, 22, 7, 0, 0, TimeSpan.Zero);

    private static SharedFateShopEntry E(uint item, int cost, uint gate = 0) => new(item, 26807, cost, gate);

    // Labyrinthos as probed on 22/11 (FateShop 1037484: shops 1770458, 1770459, 1770459; achievement 3022 on the last rank's items).
    private static IReadOnlyList<IReadOnlyList<SharedFateShopEntry>> Labyrinthos()
    {
        IReadOnlyList<SharedFateShopEntry> first = [E(36243, 2), E(36254, 2), E(36261, 2)];
        IReadOnlyList<SharedFateShopEntry> second = [E(36243, 2), E(36254, 2), E(36261, 2), E(35962, 70), E(36361, 350, 3022), E(35799, 100, 3022), E(37424, 350, 3022)];
        return [first, second, second];
    }

    // Urqopacha as probed on 22/11 (FateShop 1048628: shops 1770737, 1770738, 1770739; achievement 3559).
    private static IReadOnlyList<IReadOnlyList<SharedFateShopEntry>> Urqopacha()
    {
        IReadOnlyList<SharedFateShopEntry> materials = [E(44053, 3), E(44063, 3), E(44067, 3)];
        return [materials, [.. materials, E(43607, 90)], [.. materials, E(43607, 90), E(44114, 150), E(44312, 450, 3559), E(43571, 700, 3559), E(45009, 450, 3559)]];
    }

    private static SharedFateZone Zone(int maxRank, IReadOnlyList<IReadOnlyList<SharedFateShopEntry>> shops) =>
        new(956, "Labyrinthos", 11, maxRank, [6, 60], [.. SharedFates.Ranks(maxRank, shops).Select(pair => new SharedFateReward(pair.Entry.Item, "#" + pair.Entry.Item, 0, pair.Entry.CostItem, "gem", pair.Entry.Cost, pair.Rank))], "Free Market Friend", true);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Shared FATE 1781: the trader's items by rank: shop i at rank i + 1, the rank achievement's items at the highest rank (Endwalker and Dawntrail as probed)", () =>
        {
            Check(SharedFates.MaxRank([6, 60, 0]) == 3 && SharedFates.MaxRank([6, 20, 40]) == 4 && SharedFates.MaxRank([0, 0, 0]) == 1, "highest ranks 3 and 4 from FateProgressUI");
            var labyrinthos = SharedFates.Ranks(3, Labyrinthos()).ToDictionary(pair => pair.Entry.Item, pair => pair.Rank);
            Check(labyrinthos.Count == 7, "each item once: " + labyrinthos.Count);
            Check(labyrinthos[36243] == 1 && labyrinthos[35962] == 2 && labyrinthos[36361] == 3 && labyrinthos[35799] == 3 && labyrinthos[37424] == 3,
                "Labyrinthos: materials at 1, the map at 2, the rolls and the sign at 3: " + string.Join(", ", labyrinthos));
            var urqopacha = SharedFates.Ranks(4, Urqopacha());
            var ranks = urqopacha.ToDictionary(pair => pair.Entry.Item, pair => pair.Rank);
            Check(ranks[44053] == 1 && ranks[43607] == 2 && ranks[44114] == 3 && ranks[44312] == 4 && ranks[43571] == 4, "Urqopacha: map 2, partition 3, rolls and minion 4: " + string.Join(", ", ranks));
            Check(urqopacha.Select(pair => pair.Rank).SequenceEqual(urqopacha.Select(pair => pair.Rank).Order()), "listed by rank");
            Check(urqopacha.First(pair => pair.Entry.Item == 43607).Entry.Cost == 90, "the price kept");
        }),
        ("Shared FATE 1781: behaviour: unknown until the window was read (never 0); the items still to come follow the rank read; none at the highest rank", () =>
        {
            var zone = Zone(3, Labyrinthos());
            Check(SharedFates.Remaining(zone, null) is null, "the window never opened: unknown, not « none » nor 0");
            var rankOne = SharedFates.Remaining(zone, new SharedFateRead { Rank = 1, MaxRank = 3 })!;
            Check(rankOne.Count == 4 && rankOne.All(reward => reward.Rank > 1), "rank 1: the map and the three rank-3 items to come: " + rankOne.Count);
            Check(SharedFates.Remaining(zone, new SharedFateRead { Rank = 2, MaxRank = 3 })!.Count == 3, "rank 2: the three items of rank 3");
            Check(SharedFates.Remaining(zone, new SharedFateRead { Rank = 3, MaxRank = 3 })!.Count == 0, "highest rank: nothing more");
            Check(SharedFates.LastRead(null) is null && SharedFates.LastRead(new Dictionary<uint, SharedFateRead>()) is null, "never read: no date");
        }),
        ("Shared FATE 1781: behaviour: what the window held is kept per character, zone by zone; values that cannot be are left out; the kept copy never changed in place", () =>
        {
            var store = new Dictionary<ulong, Dictionary<uint, SharedFateRead>>();
            const ulong first = 1001, second = 2002;
            // The window not filled yet (ranks 0) and a value that cannot be: nothing kept.
            Check(SharedFates.Merged(null, [new(956, 0, 0, 0, 0), new(957, 4, 3, 1, 6), new(958, 1, 3, -1, 6), new(0, 1, 3, 0, 6)], Start, true) is null, "nothing valid: nothing kept (never a false rank)");
            store[first] = SharedFates.Merged(null, [new(956, 2, 3, 12, 60), new(1187, 1, 4, 3, 6)], Start, true)!;
            Check(store[first].Count == 2 && store[first][956] is { Rank: 2, MaxRank: 3, Progress: 12, Needed: 60 } && store[first][956].ReadAt == Start, "the first character's two zones");
            store[second] = SharedFates.Merged(store.GetValueOrDefault(second), [new(956, 3, 3, 0, 0)], Start.AddHours(1), true)!;
            Check(store[first][956].Rank == 2 && store[second][956].Rank == 3 && !store[second].ContainsKey(1187), "each character its own copy");
            // The first character opens the window again: one zone moved, the other still shown as before; a zone not shown stays.
            var kept = store[first];
            var later = Start.AddDays(1);
            Check(SharedFates.Merged(kept, [new(956, 2, 3, 12, 60)], later, false) is null, "the same values, not the first reading of an opening: nothing to save");
            var merged = SharedFates.Merged(kept, [new(956, 2, 3, 13, 60)], later, false)!;
            Check(merged[956].Progress == 13 && merged[956].ReadAt == later && merged[1187].ReadAt == Start, "the zone that moved, with its time; the other as it was");
            Check(kept[956].Progress == 12, "the kept table not changed in place (a save may be writing it)");
            var stamped = SharedFates.Merged(kept, [new(956, 2, 3, 12, 60)], later, true)!;
            Check(stamped[956].ReadAt == later && SharedFates.LastRead(stamped) == later, "the first reading of an opening: the time read even unchanged");
        }),
        ("Shared FATE 1781: read only while the game's window is open, per character, saved on change; the page section, the settings, the texts", () =>
        {
            var reader = Read("src/TenshoAssistant/Fields/SharedFateReader.Dalamud.cs");
            Check(reader.Contains("var handle=gui.GetAddonByName(SharedFates.Window);", StringComparison.Ordinal)
                && reader.Contains("if(handle.IsNull || !handle.IsReady || !handle.IsVisible){stamped=false;return;}", StringComparison.Ordinal), "the window open, or nothing read");
            Check(reader.Contains("seen.Add(new(zone.TerritoryTypeId,zone.CurrentRank,zone.MaxRank,zone.FateProgress,zone.NeededFates));", StringComparison.Ordinal), "the agent's fields, read only");
            Check(reader.Contains("all[character]=merged;", StringComparison.Ordinal) && reader.Contains("save();", StringComparison.Ordinal) && reader.Contains("CompetitiveGate.Add(framework,OnUpdate);", StringComparison.Ordinal), "per character, saved, cut in competitive content");
            // X-153 (0.1895): made by the field content family, its save passed in by the plugin's constructor.
            Check(Read("src/TenshoAssistant/Plugin.Fields.cs").Contains("new Fields.SharedFateReader(framework,client,player,gameGui,log,()=>configuration.SharedFates??=[],save)", StringComparison.Ordinal)
                && Read("src/TenshoAssistant/Plugin.cs").Contains("RegisterSharedFates(configuration,framework,client,player,gameGui,()=>SaveTimed(\"shared FATEs\"));", StringComparison.Ordinal), "wired");
            Check(Read("src/TenshoAssistant/Configuration.cs").Contains("\"SharedFates\",", StringComparison.Ordinal), "the player's data, kept by « reset all »");
            Check(Read("src/TenshoAssistant/MainWindow.Fates.cs").Contains("DrawSharedFates();", StringComparison.Ordinal)
                && Read("src/TenshoAssistant/MainWindow.SheetLoaders.cs").Contains("SharedFatesLoad=()=>Read(\"Shared FATE sheets\",()=>Fields.SharedFateLoader.Read(data));", StringComparison.Ordinal), "the section on the FATE page, the sheets on both sides");
            Check(Read("docs/evidence/0.1008-game-update-inventory.md").Contains("`FateProgress`", StringComparison.Ordinal), "the window in the game update inventory");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                foreach (var key in new[] { "SharedTitle", "SharedHeader", "SharedUnknown", "SharedUnknownTip", "SharedNext", "SharedRewards", "SharedRewardsNoTraderTip", "SharedAchievement", "SharedReadAt", "SharedUnavailable" })
                    Check(text.Contains($"\"{key}\": ", StringComparison.Ordinal), $"{language}: {key}");
            }
        }),
    ];
}
