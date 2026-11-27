using TenshoAssistant.Character;

// X-469 (D-174, the owner's choice of 15/11/2026 at about 10:30 pm): the retainers' table shows each retainer's items on sale, read when its
// sale list is open in the game, kept per character and dated; « not read yet » for a retainer never opened. The listing must survive the
// readings at a summoning bell (they carry no listing) and the files (R28.14: no data lost).
internal static class RetainerListing1762Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static readonly DateTimeOffset Then = new(2026, 11, 22, 10, 0, 0, TimeSpan.Zero);

    private static RetainerState Retainer(string name, int market) => RetainerStates.From(name, "BTN", 90, 1000, 12, market, market > 0 ? 1_800_000_000u : 0u, 0, 0, "Limsa", null);

    private static CharacterStatus Status(params RetainerState[] retainers) => new() { ContentId = 42, Name = "Me", SeenAt = Then, Retainers = retainers };

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Retainer listing 1762: behaviour: the sale list's rows kept (an item and a quantity, 20 at most, a price below 0 unknown); the same listing gives the same key", () =>
        {
            var rows = Enumerable.Range(1, 25).Select(index => ((uint)index, index % 2 == 0, index, (long?)(index * 100))).ToList();
            rows.Insert(0, (0u, false, 3, 100L));
            rows.Insert(1, (7u, false, 0, 100L));
            rows.Insert(2, (9u, true, 1, -5L));
            var listed = RetainerStates.Listing(rows);
            Check(listed.Length == 20 && listed[0] == new RetainerSaleItem(9, true, 1, null) && listed[1] == new RetainerSaleItem(1, false, 1, 100), "bounded, the empty rows left out: " + string.Join(" ", listed.Take(3)));
            Check(RetainerStates.ListingKey(5, listed) == RetainerStates.ListingKey(5, RetainerStates.Listing(rows)) && RetainerStates.ListingKey(5, listed) != RetainerStates.ListingKey(6, listed)
                && RetainerStates.ListingKey(5, listed) != RetainerStates.ListingKey(5, listed.Skip(1)), "the key: retainer and listing");
        }),
        ("Retainer listing 1762: behaviour: the listing goes to the retainer of that name, dated; none of that name: nothing kept", () =>
        {
            var retainers = new[] { Retainer("Ada", 2), Retainer("Bob", 0) };
            var listed = new[] { new RetainerSaleItem(5333, false, 99, 120), new RetainerSaleItem(4850, true, 1, 9_000) };
            var next = RetainerStates.WithListing(retainers, "Ada", listed, Then);
            Check(next is not null && next[0].Listed == listed && next[0].ListedAt == Then && next[1].Listed is null && retainers[0].Listed is null, "Ada's, the array read not changed");
            Check(RetainerStates.WithListing(retainers, "Cid", listed, Then) is null, "no retainer of that name (its list not read at a bell): nothing");
        }),
        ("Retainer listing 1762: behaviour: a reading at a summoning bell keeps each listing (nothing to save when nothing changed); nothing on sale: an empty listing dated once; selling again: not read yet", () =>
        {
            var listed = new[] { new RetainerSaleItem(5333, false, 99, 120) };
            var kept = Status(RetainerStates.WithListing([Retainer("Ada", 1), Retainer("Bob", 0), Retainer("Cid", 3)], "Ada", listed, Then)!);
            var bell = Status(Retainer("Ada", 1), Retainer("Bob", 0), Retainer("Cid", 3)) with { SeenAt = Then.AddMinutes(5) };
            var merged = CharacterStatuses.Merge(kept, bell);
            Check(merged.Retainers![0].Listed == listed && merged.Retainers[0].ListedAt == Then, "Ada's listing kept through the bell's reading");
            Check(merged.Retainers[1].Listed is { Length: 0 } && merged.Retainers[1].ListedAt == Then.AddMinutes(5), "Bob sells nothing: nothing on sale, dated");
            Check(merged.Retainers[2].Listed is null, "Cid never opened: not read yet");
            var again = CharacterStatuses.Merge(merged, bell with { SeenAt = Then.AddMinutes(10) });
            Check(!CharacterStatuses.Changed(merged, again) && again.Retainers![1].ListedAt == Then.AddMinutes(5), "read again the same: nothing to save, the date kept");
            var selling = CharacterStatuses.Merge(again, Status(Retainer("Ada", 1), Retainer("Bob", 2), Retainer("Cid", 3)) with { SeenAt = Then.AddMinutes(15) });
            Check(selling.Retainers![1].Listed is null && selling.Retainers[0].Listed == listed, "Bob selling again after an empty listing: not read yet; Ada's still kept");
            Check(CharacterStatuses.Merge(selling, selling with { Retainers = null }).Retainers![0].Listed == listed, "a reading without the retainers' list: all kept");
        }),
        ("Retainer listing 1762: behaviour: the listing is written in the character's file and read back; a file from before it reads « not read yet »", () =>
        {
            var folder = Path.Combine(Path.GetTempPath(), "tensho-listing-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                var listed = new[] { new RetainerSaleItem(5333, false, 99, 120), new RetainerSaleItem(4850, true, 1, null) };
                var status = Status(RetainerStates.WithListing([Retainer("Ada", 2)], "Ada", listed, Then)!);
                CharacterStatuses.Write(folder, status);
                var back = CharacterStatuses.Read(Path.Combine(folder, CharacterStatuses.FileName(42)), 42);
                Check(back.Retainers![0].Listed!.SequenceEqual(listed) && back.Retainers[0].ListedAt == Then, "written and read back");
                var path = Path.Combine(folder, CharacterStatuses.FileName(42));
                var before = File.ReadAllText(path);
                var start = before.IndexOf(",\"Listed\"", StringComparison.Ordinal);
                Check(start > 0, "the file holds the listing: " + before);
                CharacterStatuses.Write(folder, Status(Retainer("Ada", 2)));
                Check(CharacterStatuses.Read(path, 42).Retainers![0] is { Listed: null, ListedAt: null }, "a file without it: not read yet");
            }
            finally { Directory.Delete(folder, true); }
        }),
        ("Retainer listing 1762: wiring: the watcher keeps the open retainer's listing (sale list read, once a second, by the retainer's id named from the game's list); the table shows it under the retainer; both languages", () =>
        {
            var watcher = Read("src/TenshoAssistant/Character/CharacterStatusWatcher.Dalamud.cs");
            Check(watcher.Contains("if(sales() is not { Status:TenshoAssistant.Market.RetainerSalesStatus.Ready,RetainerId:not 0 } open){lastListing=null;return;}", StringComparison.Ordinal)
                && watcher.Contains("RetainerReader.NameOf(open.RetainerId)", StringComparison.Ordinal) && watcher.Contains("RetainerStates.WithListing(retainers,name,listed,now)", StringComparison.Ordinal)
                && watcher.Contains("ReadListing(now);\n        if(now<nextRead)return;", StringComparison.Ordinal), "the watcher");
            Check(Read("src/TenshoAssistant/Plugin.Character.cs").Contains("sales:()=>retainerSales?.State),()=>characterStatus?.Dispose());", StringComparison.Ordinal), "wired");
            Check(Read("src/TenshoAssistant/Character/CharacterStatus.cs").Contains("Retainers=RetainerStates.KeepListings(old.Retainers,fresh.Retainers,fresh.SeenAt) ?? old.Retainers,", StringComparison.Ordinal), "merged");
            var page = Read("src/TenshoAssistant/MainWindow.CharacterStatus.cs");
            Check(page.Contains("DrawRetainerListing(retainer);\n                }", StringComparison.Ordinal) && page.Contains("localization.Get(\"CharacterStatus.ListedNever\")", StringComparison.Ordinal), "under the retainer's row");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                Check(new[] { "ListedNever", "ListedNeverTip", "ListedNone", "ListedAt", "ListedItem" }.All(key => text.Contains($"\"{key}\": ", StringComparison.Ordinal)), "the words, " + language);
            }
        }),
    ];
}
