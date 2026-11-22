using System.Numerics;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Alarms;
using TenshoAssistant.Chat;
using TenshoAssistant.Collections;
using TenshoAssistant.Discoveries;
using TenshoAssistant.FreeCompany;
using TenshoAssistant.Inventory;

// 0.1793 (X-574, the user's choice of 17/11: the code that converted the old data formats is removed, the user being the only one with data).
// Behaviour tests (R28.14: data loss), real files in a temporary folder of their own, real SQLite: (a) the data as it is today (TenshoData.db
// with bank, gil journal, achievement progress and voyage rows; a settings file holding the chat's former upgrade markers and an alarm with
// its Discord source; a discoveries database) loaded, saved and loaded again by fresh instances: nothing lost; (b) the files of the old
// formats beside a database already converted: never read, moved nor written, the database's values shown, and one log line per family per
// plugin load; the copies set aside by the conversions of before (before-sqlite-*, « .migrated ») left alone and never named; (c) the JSON
// mode (no data file can be made) still reads and writes its files as before.
internal static class OldFormats1793Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026, 11, 20, 9, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Recent = new(DateTimeOffset.UtcNow.AddDays(-2).UtcTicks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond, TimeSpan.Zero);
    private const ulong First = 18014498549678007UL, Second = 0x8000_0000_0000_0002UL, Journal = 0x8000_0000_0000_0042UL;

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-1793-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    // One data file is one plugin load; its X-574 lines are kept in « said ».
    private static TenshoDataFile Data(string root, List<string>? said = null)
        => new(Path.Combine(root, TenshoDataDatabase.FileName), (error, message) => throw new Exception(message, error),
            line => { if (line.EndsWith("(X-574).", StringComparison.Ordinal)) said?.Add(line); });

    private static BankEntry Entry(ulong character, BankStorageKind kind, ulong storage = 0, uint item = 5057, int quantity = 3)
    {
        var owner = new InventoryOwnerId(kind == BankStorageKind.Retainer ? storage : character);
        var ids = BankStorageScope.ContainerIds(kind);
        var containers = ids.Select(id => new InventoryContainerSnapshot(new(owner, new(id)), InventoryCoverage.Complete,
            id == ids[0] ? [new InventoryStack(new(new(item), false), quantity, 0)] : [])).ToArray();
        return new(new(character, kind, storage), kind == BankStorageKind.Retainer ? "Servant « " + storage + " »" : "Name (World)", new(At, "test", containers));
    }
    private static string Text(IEnumerable<BankEntry> entries) => string.Join("\n", FileInventoryBankStore.EntryRows(entries.ToArray()));
    private static PerCharacterBankStore Bank(string root, TenshoDataFile? data)
        => new(Path.Combine(root, "InventoryBank"), Path.Combine(root, "InventoryBank.json"), 10_000, data);

    private static List<GilEntry> Gil(int count, long start)
        => Enumerable.Range(0, count).Select(index => new GilEntry(At.AddMinutes(index), index % 2 == 0 ? 100 + index : -(50 + index), start + index, (GilSource)(index % 3), (GilCategory)(index % 9))).ToList();

    private static Dictionary<ulong, Dictionary<uint, AchievementProgress>> Progress(uint current) => new()
    {
        [First] = new() { [3811] = new(current, 10, At.AddDays(-1)), [1357] = new(200, 200, At) },
        [Second] = new() { [42] = new(7, 10, At) },
    };
    private static bool Same(Dictionary<ulong, Dictionary<uint, AchievementProgress>> left, Dictionary<ulong, Dictionary<uint, AchievementProgress>> right)
        => left.Count == right.Count && left.All(pair => right.TryGetValue(pair.Key, out var other) && pair.Value.Count == other.Count
            && pair.Value.All(item => other.TryGetValue(item.Key, out var value) && value == item.Value));

    private static VoyageRecord Voyage(ulong house, int slot, int hours, params (uint Item, int Count)[] loot)
        => new(house, slot % 2 == 0 ? VesselKind.Submarine : VesselKind.Airship, slot, "Vessel \"" + slot + "\" ×", 50 + slot, Recent.AddHours(hours),
            slot == 1 ? null : Recent.AddHours(hours + 18), [1u, 5u, 9u], ["Deep-sea Site 1", "The Ivory Shoals"], loot.Select(entry => new VoyageLoot(entry.Item, entry.Count)).ToArray());
    private static bool Same(IReadOnlyList<VoyageRecord> left, IReadOnlyList<VoyageRecord> right)
        => left.Count == right.Count && left.Zip(right).All(pair => pair.First.HouseId == pair.Second.HouseId && pair.First.Kind == pair.Second.Kind
            && pair.First.Slot == pair.Second.Slot && pair.First.RegisteredAt == pair.Second.RegisteredAt && VoyageHistory.Same(pair.First, pair.Second));

    private static DiscoveryStore Discoveries(uint subject, int sightings)
    {
        var store = new DiscoveryStore();
        var place = new Vector3(120, 0, -40);
        store.Merge([new DiscoveryFact { Kind = DiscoveryKind.FishCatch, Subject = subject + 1, Detail = 1, Territory = 135, Map = 16, CellX = DiscoveryStore.Cell(place.X, place.Z).X,
            CellZ = DiscoveryStore.Cell(place.X, place.Z).Z, X = place.X, Z = place.Z, LevelMin = 1, LevelMax = 1, Seen = 1, First = At.AddDays(-3), Last = At.AddDays(-2) }]);
        for (var sighting = 0; sighting < sightings; sighting++) store.SeeSource(DiscoveryKind.FishCatch, subject, 1, 3 + sighting, At.AddMinutes(sighting), 135, 16, place);
        store.SeeSource(DiscoveryKind.Desynthesis, subject + 2, 1, 1, At);
        return store;
    }
    // Every fact of a database (yours and shared), as JSON, by key: what a restart would show.
    private static string Facts(DiscoveryDatabase database)
        => string.Join("\n", database.ReadMine().Facts.Concat(database.SharedKinds().SelectMany(kind => database.ReadShared(kind).Facts))
            .OrderBy(fact => fact.Key, StringComparer.Ordinal).Select(fact => JsonSerializer.Serialize(fact, TenshoJson.Plain)));

    // The data as it is today, written through the stores themselves (one plugin load).
    private static void Today(string root)
    {
        var data = Data(root);
        var bank = Bank(root, data);
        bank.Load();
        bank.Save([Entry(First, BankStorageKind.Saddlebag), Entry(First, BankStorageKind.Retainer, 99, item: 2), Entry(Second, BankStorageKind.Currency)]);
        new GilJournalStore(Path.Combine(root, "GilJournal"), data).Save(Journal, Gil(4, 1000));
        var achievements = new AchievementProgressData(Path.Combine(root, "AchievementProgress.json"), data);
        foreach (var (character, progress) in Progress(1)) achievements.Save(character, progress, Progress(1));
        new VoyageHistoryData(Path.Combine(root, "VoyageHistory.json"), data).Save([Voyage(17, 0, 0, (22500, 3)), Voyage(0x8000_0000_0000_0003UL, 1, 1, (10, 2))]);
        var discoveries = Path.Combine(root, "Discoveries");
        Directory.CreateDirectory(discoveries);
        new DiscoveryDatabase(Path.Combine(discoveries, DiscoveryDatabase.FileName)).Write(Discoveries(4869, 2).Snapshot(At).Facts, [], false);
    }

    // Every file of the profile but the databases and the locks, with its bytes (the files of before, compared byte for byte).
    private static (string Name, byte[] Bytes)[] OldFiles(string root) => Directory.GetFiles(root, "*", SearchOption.AllDirectories)
        .Where(path => Path.GetFileName(path) is var name && !name.StartsWith(TenshoDataDatabase.FileName, StringComparison.Ordinal)
            && !name.StartsWith(DiscoveryDatabase.FileName, StringComparison.Ordinal) && !name.EndsWith(".lock", StringComparison.Ordinal))
        .Order(StringComparer.Ordinal).Select(path => (Path.GetRelativePath(root, path), File.ReadAllBytes(path))).ToArray();
    private static bool Same((string Name, byte[] Bytes)[] left, (string Name, byte[] Bytes)[] right)
        => left.Length == right.Length && left.Zip(right).All(pair => pair.First.Name == pair.Second.Name && pair.First.Bytes.SequenceEqual(pair.Second.Bytes));

    private static SettingsFile<Configuration> Settings(string root)
        => new(Path.Combine(root, "TenshoAssistant", SettingsFile<Configuration>.FileName), Path.Combine(root, "TenshoAssistant.json"), (error, message) => throw new Exception(message, error));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Old formats 1793: behaviour: today's data (bank, gil journal, achievements, voyages, discoveries) loaded, saved and loaded again by fresh instances: nothing lost", () => InTemp(root =>
        {
            Today(root);
            var bank = Text(Bank(root, Data(root)).Load());
            var gil = new GilJournalStore(Path.Combine(root, "GilJournal"), Data(root)).Load(Journal, At);
            var achievements = new AchievementProgressData(Path.Combine(root, "AchievementProgress.json"), Data(root)).Load(At);
            var voyages = new VoyageHistoryData(Path.Combine(root, "VoyageHistory.json"), Data(root)).Load(At);
            var discoveryPath = Path.Combine(root, "Discoveries", DiscoveryDatabase.FileName);
            var facts = Facts(new DiscoveryDatabase(discoveryPath));
            Check(bank.Length > 0 && gil.Count == 4 && Same(achievements, Progress(1)) && voyages.Count == 2 && facts.Split('\n').Length == 3, "today's data read");

            // One plugin load: everything read, then saved as read.
            var said = new List<string>();
            var data = Data(root, said);
            var store = Bank(root, data);
            store.Save(store.Load());
            var journal = new GilJournalStore(Path.Combine(root, "GilJournal"), data);
            journal.Save(Journal, journal.Load(Journal, At));
            var progress = new AchievementProgressData(Path.Combine(root, "AchievementProgress.json"), data);
            var all = progress.Load(At);
            foreach (var (character, seen) in all) progress.Save(character, seen, all);
            var history = new VoyageHistoryData(Path.Combine(root, "VoyageHistory.json"), data);
            history.Save(history.Load(At), null, DateTimeOffset.UtcNow);
            var discoveries = new DiscoveryDatabase(discoveryPath);
            var memory = new DiscoveryStore();
            memory.Load(discoveries.ReadMine());
            foreach (var kind in discoveries.SharedKinds()) memory.LoadShared(discoveries.ReadShared(kind));
            discoveries.Write(memory.Snapshot(At).Facts, [], false);
            Check(said.Count == 0, "no file of before: nothing said: " + string.Join(" | ", said));

            // The next plugin load, fresh instances.
            Check(Text(Bank(root, Data(root)).Load()) == bank, "the bank identical");
            Check(new GilJournalStore(Path.Combine(root, "GilJournal"), Data(root)).Load(Journal, At).SequenceEqual(gil), "the gil journal identical (an id above long.MaxValue)");
            Check(Same(new AchievementProgressData(Path.Combine(root, "AchievementProgress.json"), Data(root)).Load(At), achievements), "the achievement progress identical");
            Check(Same(new VoyageHistoryData(Path.Combine(root, "VoyageHistory.json"), Data(root)).Load(At), voyages), "the voyage history identical");
            Check(Facts(new DiscoveryDatabase(discoveryPath)) == facts, "the discoveries identical (yours, shared, provenance)");
            using var open = new TenshoDataDatabase(Path.Combine(root, TenshoDataDatabase.FileName)).Open();
            Check(Convert.ToInt64(TenshoDataDatabase.Scalar(open, "SELECT COUNT(*) FROM converted;")) == 0 && Convert.ToInt64(TenshoDataDatabase.Scalar(open, "SELECT COUNT(*) FROM gil_converted;")) == 0,
                "the tables of the marks kept, never written");
        })),
        ("Old formats 1793: behaviour: the user's settings (chat upgrade markers, GearExactAtCaps, an alarm and its Discord source) load, save and load again the same; the markers stay in the file, ignored", () => InTemp(root =>
        {
            var file = Settings(root);
            var settings = file.Load(out var origin);
            Check(origin == SettingsOrigin.New, "a new file: " + origin);
            settings.GearExactAtCaps = false;
            settings.Chat.Opacity = 40; settings.Chat.TabsAtBottom = false; settings.Chat.WindowOpen = true;
            settings.Chat.Tabs = [new() { Name = "Mine", Channels = [ChatChannel.Say, ChatChannel.Npc], ShowUnread = false }];
            settings.Alarms = [new TenshoAlarm { Label = "Retainers", Group = "Reminders", Source = AlarmSource.Reminder, Discord = true, DiscordMessage = "back" },
                               new TenshoAlarm { Label = "Old", Group = "Rappels" }];
            Check(file.Save(settings), "saved");
            // As the user's file holds them (upgraded long ago): the markers of the chat's former upgrades.
            var path = file.FilePath;
            var node = SettingsJson.Node(File.ReadAllText(path));
            node["Chat"]!["LookVersion"] = 1; node["Chat"]!["TabsVersion"] = 2;
            File.WriteAllText(path, SettingsJson.Text(node));
            var bytes = File.ReadAllBytes(path);

            void Holds(Configuration read, string when)
            {
                Check(!read.GearExactAtCaps, when + ": GearExactAtCaps off");
                Check(read.Chat.Opacity == 40 && !read.Chat.TabsAtBottom && read.Chat.WindowOpen, when + ": the chat's look as saved (no upgrade)");
                var tabs = read.Chat.EnsureTabs("General", "Battle", "Event", "Social");
                Check(tabs.Count == 1 && tabs[0].Name == "Mine" && tabs[0].Channels.SequenceEqual([ChatChannel.Say, ChatChannel.Npc]) && !tabs[0].ShowUnread, when + ": the tabs as saved (no Battle nor Event added)");
                Check(read.Alarms.Count == 2 && read.Alarms[0] is { Source: AlarmSource.Reminder, Discord: true, DiscordMessage: "back" } && read.Alarms[1].Source == AlarmSource.Other,
                    when + ": the alarm's Discord source kept; none guessed from a group");
                Check(DiscordWebhook.SourceOf(read.Alarms[1]) == AlarmSource.Other, when + ": the other stays other");
            }
            var again = Settings(root);
            var loaded = again.Load(out origin);
            Check(origin == SettingsOrigin.Own && File.ReadAllBytes(path).SequenceEqual(bytes), "read as it is, nothing written at load");
            Holds(loaded, "loaded");
            Check(again.Save(loaded), "saved again");
            Holds(Settings(root).Load(out _), "loaded again");
            var saved = SettingsJson.Node(File.ReadAllText(path));
            Check(saved["Chat"]!["TabsVersion"]?.GetValue<int>() == 2 && saved["Chat"]!["LookVersion"]?.GetValue<int>() == 1 && saved["GearExactAtCaps"]?.GetValue<bool>() == false,
                "the markers kept in the file as names this version does not know (nothing lost), read by nobody");
        })),
        ("Old formats 1793: behaviour: files of the old formats beside a converted database: never read, moved nor written; the database's values; one line per family per plugin load", () => InTemp(root =>
        {
            Today(root);
            var bank = Text(Bank(root, Data(root)).Load());
            // The files of before, with other values than the database's, as an older version would have left them.
            new FileInventoryBankStore(Path.Combine(root, "InventoryBank.json")).Save([Entry(First, BankStorageKind.Retainer, 77, quantity: 9)]);
            new FileInventoryBankStore(Path.Combine(root, "InventoryBank", PerCharacterBankStore.FileName(First))).Save([Entry(First, BankStorageKind.Armoury, quantity: 8)]);
            GilJournal.Write(Path.Combine(root, "GilJournal", GilJournalStore.FileName(Journal)), Gil(2, 9000));
            VoyageHistory.Write(Path.Combine(root, "VoyageHistory.json"), [Voyage(17, 2, 5, (7, 7))]);
            File.WriteAllText(Path.Combine(root, "AchievementProgress.json"), AchievementProgressStore.Write(Progress(9)));
            var main = Path.Combine(root, "Discoveries", "Discoveries.json");
            DiscoveryFiles.WriteSplit(main, Discoveries(7000, 5).Snapshot(At), []);
            // What the user's folder holds: the copies the conversions of before set aside.
            File.WriteAllText(Path.Combine(root, "InventoryBank.json.migrated"), "{\"old\":1}");
            File.WriteAllText(Path.Combine(root, "InventoryBank.json.bak.migrated"), "{\"old\":2}");
            var aside = Path.Combine(root, "before-sqlite-20261112-101010", "InventoryBank"); Directory.CreateDirectory(aside);
            new FileInventoryBankStore(Path.Combine(aside, PerCharacterBankStore.FileName(Second))).Save([Entry(Second, BankStorageKind.Bags, quantity: 6)]);
            var gilAside = Path.Combine(root, "GilJournal", "before-sqlite-20261112-101010"); Directory.CreateDirectory(gilAside);
            GilJournal.Write(Path.Combine(gilAside, GilJournalStore.FileName(Journal)), Gil(1, 5));
            var before = OldFiles(root);
            var facts = Facts(new DiscoveryDatabase(Path.Combine(root, "Discoveries", DiscoveryDatabase.FileName)));

            // What each load shows: the database's values, then what its saves wrote there.
            var bankShown = bank;
            var gilShown = Gil(4, 1000);
            var progressShown = Progress(1);
            var voyagesShown = new List<VoyageRecord> { Voyage(17, 0, 0, (22500, 3)), Voyage(0x8000_0000_0000_0003UL, 1, 1, (10, 2)) };
            for (var load = 1; load <= 2; load++)
            {
                var said = new List<string>();
                var data = Data(root, said);
                var store = Bank(root, data);
                for (var twice = 0; twice < 2; twice++)
                {
                    Check(Text(store.Load()) == bankShown, $"load {load}: the bank is the database's");
                    Check(new GilJournalStore(Path.Combine(root, "GilJournal"), data).Load(Journal, At).SequenceEqual(gilShown), $"load {load}: the gil journal is the database's");
                    Check(Same(new AchievementProgressData(Path.Combine(root, "AchievementProgress.json"), data).Load(At), progressShown), $"load {load}: the achievements are the database's");
                    Check(Same(new VoyageHistoryData(Path.Combine(root, "VoyageHistory.json"), data).Load(At), voyagesShown), $"load {load}: the voyages are the database's");
                }
                // Saves of this load: the database written, the files of before never.
                store.Save(store.Load());
                bankShown = Text(store.Apply(new BankChange(Put: Entry(Second, BankStorageKind.Bags, quantity: load)), store.Load()));
                gilShown = [.. gilShown, new GilEntry(At.AddDays(load), 1, 1, GilSource.Character, GilCategory.Other)];
                new GilJournalStore(Path.Combine(root, "GilJournal"), data).Save(Journal, gilShown);
                progressShown[Second][42] = new((uint)(7 + load), 10, At.AddDays(load));
                new AchievementProgressData(Path.Combine(root, "AchievementProgress.json"), data).Save(Second, progressShown[Second], progressShown);
                var voyage = Voyage(17, 4, 2 + load, (3, load));
                voyagesShown.Add(voyage);
                new VoyageHistoryData(Path.Combine(root, "VoyageHistory.json"), data).Save([voyage], null, DateTimeOffset.UtcNow);
                var discoveries = new DiscoveryDatabase(Path.Combine(root, "Discoveries", DiscoveryDatabase.FileName));
                Check(Facts(discoveries) == facts, $"load {load}: the discoveries are the database's");
                Check(DiscoveryFiles.OldFiles(main).SequenceEqual(["Discoveries.json", "Shared/"]), $"load {load}: the discoveries' files of before named");
                Check(said.Order(StringComparer.Ordinal).SequenceEqual(
                [
                    "Achievement progress: a file of an old format (AchievementProgress.json) was left aside, not read (X-574).",
                    "Bank: files of an old format (InventoryBank.json, InventoryBank/) were left aside, not read (X-574).",
                    $"Gil journal: a file of an old format ({Path.Combine("GilJournal", "*.json")}) was left aside, not read (X-574).",
                    "Voyage history: a file of an old format (VoyageHistory.json) was left aside, not read (X-574).",
                ]), $"load {load}: one line per family, never the copies set aside: " + string.Join(" | ", said));
                Check(Same(OldFiles(root), before), $"load {load}: every file of before still there, byte for byte (the copies set aside too)");
                Check(Directory.GetDirectories(root, "before-sqlite-*").Length == 1 && !File.Exists(Path.Combine(root, "InventoryBank.json.migrated.1")), $"load {load}: nothing moved nor renamed");
            }
            // What the saves wrote is in the database, next to what it held; a third load (fresh instances) shows it.
            var saved = Bank(root, Data(root)).Load();
            Check(Text(saved) == bankShown && saved.Length == 4
                && saved.Single(entry => entry.Key == new BankKey(Second, BankStorageKind.Bags, 0)).Snapshot.Containers.Values.Sum(container => container.Stacks.Sum(stack => stack.Quantity)) == 2, "the bank's last change saved");
            Check(new GilJournalStore(Path.Combine(root, "GilJournal"), Data(root)).Load(Journal, At).SequenceEqual(gilShown), "the gil journal saved");
            Check(Same(new AchievementProgressData(Path.Combine(root, "AchievementProgress.json"), Data(root)).Load(At), progressShown), "the achievements saved");
            Check(Same(new VoyageHistoryData(Path.Combine(root, "VoyageHistory.json"), Data(root)).Load(At), voyagesShown), "the voyages saved by key");
        })),
        ("Old formats 1793: behaviour: the JSON mode (no data file can be made) still reads and writes the bank, gil journal, voyage and achievement files; only the former shared bank file is left aside", () => InTemp(root =>
        {
            var said = new List<string>();
            Directory.CreateDirectory(Path.Combine(root, TenshoDataDatabase.FileName));   // a folder in the way: no data file can be made
            var json = new TenshoDataFile(Path.Combine(root, TenshoDataDatabase.FileName), (_, _) => { }, line => { if (line.EndsWith("(X-574).", StringComparison.Ordinal)) said.Add(line); });
            new FileInventoryBankStore(Path.Combine(root, "InventoryBank.json")).Save([Entry(First, BankStorageKind.Retainer, 77, quantity: 9)]);
            var former = File.ReadAllBytes(Path.Combine(root, "InventoryBank.json"));
            var store = Bank(root, json);
            Check(store.Mode == "json" && store.Load().Length == 0, "JSON mode; the former shared file not read nor split");
            var entries = new[] { Entry(First, BankStorageKind.Saddlebag), Entry(Second, BankStorageKind.Bags) };
            store.Save(entries);
            Check(File.Exists(Path.Combine(root, "InventoryBank", PerCharacterBankStore.FileName(First))) && File.Exists(Path.Combine(root, "InventoryBank", PerCharacterBankStore.FileName(Second))), "one file per character, as before");
            var gil = new GilJournalStore(Path.Combine(root, "GilJournal"), json);
            gil.Save(Journal, Gil(3, 100));
            Check(File.Exists(Path.Combine(root, "GilJournal", GilJournalStore.FileName(Journal))), "the gil journal's file");
            var achievements = new AchievementProgressData(Path.Combine(root, "AchievementProgress.json"), json);
            achievements.Save(First, Progress(4)[First], Progress(4));
            Check(File.Exists(Path.Combine(root, "AchievementProgress.json")), "the achievement file");
            var voyages = new VoyageHistoryData(Path.Combine(root, "VoyageHistory.json"), json);
            voyages.Save([Voyage(17, 0, 0, (1, 1)), Voyage(18, 1, 1, (2, 2))]);
            Check(File.Exists(Path.Combine(root, "VoyageHistory.json")), "the voyage file");
            // The next load reads those files as before.
            var next = new TenshoDataFile(Path.Combine(root, TenshoDataDatabase.FileName), (_, _) => { });
            Check(Text(Bank(root, next).Load()) == Text(entries), "the bank read back");
            Check(new GilJournalStore(Path.Combine(root, "GilJournal"), next).Load(Journal, At).SequenceEqual(Gil(3, 100)), "the gil journal read back");
            Check(Same(new AchievementProgressData(Path.Combine(root, "AchievementProgress.json"), next).Load(At), Progress(4)), "the achievements read back");
            Check(Same(new VoyageHistoryData(Path.Combine(root, "VoyageHistory.json"), next).Load(At), [Voyage(17, 0, 0, (1, 1)), Voyage(18, 1, 1, (2, 2))]), "the voyages read back");
            Check(File.ReadAllBytes(Path.Combine(root, "InventoryBank.json")).SequenceEqual(former) && !File.Exists(Path.Combine(root, "InventoryBank.json.migrated")), "the former shared file left as it is");
            Check(said.SequenceEqual(["Bank: a file of an old format (InventoryBank.json) was left aside, not read (X-574)."]), "only the former shared file said, once: " + string.Join(" | ", said));
        })),
        ("Old formats 1793: behaviour: « Erase this character » with the data file also removes that character's files of before left in place (InventoryBank/, GilJournal/), never another's", () => InTemp(root =>
        {
            Today(root);
            var bankFolder = Path.Combine(root, "InventoryBank");
            var gilFolder = Path.Combine(root, "GilJournal");
            var aside = Path.Combine(root, "before-sqlite-20261112-055730");
            foreach (var folder in new[] { bankFolder, gilFolder, aside }) Directory.CreateDirectory(folder);
            var first = PerCharacterBankStore.FileName(First);
            var second = PerCharacterBankStore.FileName(Second);
            foreach (var file in new[] { Path.Combine(bankFolder, first), Path.Combine(bankFolder, first + ".bak"), Path.Combine(gilFolder, first), Path.Combine(aside, first),
                         Path.Combine(bankFolder, second), Path.Combine(gilFolder, second) })
                File.WriteAllText(file, "{}");
            var store = Bank(root, Data(root));
            var left = store.Apply(new BankChange(Erase: First), store.Load());
            Check(left.All(entry => entry.Key.CharacterId != First), "the character's rows erased");
            foreach (var file in new[] { Path.Combine(bankFolder, first), Path.Combine(bankFolder, first + ".bak"), Path.Combine(gilFolder, first), Path.Combine(aside, first) })
                Check(!File.Exists(file), "removed: " + Path.GetRelativePath(root, file));
            Check(File.Exists(Path.Combine(bankFolder, second)) && File.Exists(Path.Combine(gilFolder, second)), "another character's files of before left alone");
        })),
    ];
}
