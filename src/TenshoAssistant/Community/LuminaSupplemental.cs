using System.Globalization;
using System.Net.Http;
using TenshoAssistant.Caching;
using TenshoAssistant.Duties;

namespace TenshoAssistant.Community;

/// <summary>
/// LuminaSupplemental (Critical-Impact, GPL-3.0, github.com/Critical-Impact/LuminaSupplemental, « Generated » CSVs): data the game sheets do
/// not hold, keyed by game ids (0.188). Used first for duty loot because it covers more duties than Garland (283 duties with boss coffers,
/// 1 373 zone chests with rates, read 2026-10-31): DungeonBossChest (ItemId, ContentFinderConditionId, FightNo, CofferNo), DungeonBossDrop
/// (ContentFinderConditionId, FightNo, ItemId), DungeonChest (RowId = chest, ChestNo, ContentFinderConditionId) and DungeonChestItem
/// (ItemId, ChestId, Probability).
/// </summary>
public sealed class LuminaSupplementalData
{
    private readonly Dictionary<uint, List<DutyLootEntry>> byDuty = [];
    private readonly Dictionary<uint, HashSet<uint>> dutiesOfItem = [];
    public int Duties => byDuty.Count;

    public static readonly string[] Files = ["DungeonBossChest", "DungeonBossDrop", "DungeonChest", "DungeonChestItem", "MobDrop", "FateItem", "RetainerVentureItem", "StoreItem", "ItemSupplement", "ENpcShop", "ENpcPlace", "QuestRequiredItem"];
    // Uses of an item (0.209): what it gives (ItemSupplement read the other way: SourceItemId → ItemId per kind) and the quests that ask
    // for it (QuestRequiredItem: ItemId, QuestId, Quantity, IsHq; quest names resolved from the Quest sheet when the data is built).
    private readonly Dictionary<uint, Dictionary<int, (int Count, List<uint> Items)>> gives = [];
    private readonly Dictionary<uint, List<(uint QuestId, string Name, uint Quantity, bool Hq)>> questsRequiring = [];
    public IReadOnlyList<(int Kind, int Count, IReadOnlyList<uint> Items)> GivesOf(uint sourceItemId) =>
        gives.TryGetValue(sourceItemId, out var kinds)
            ? kinds.OrderBy(pair => pair.Key).Select(pair => (pair.Key, pair.Value.Count, (IReadOnlyList<uint>)pair.Value.Items)).ToArray() : [];
    public IReadOnlyList<(uint QuestId, string Name, uint Quantity, bool Hq)> QuestsRequiring(uint itemId) =>
        questsRequiring.TryGetValue(itemId, out var list) ? list : [];
    // Shops the game sheets do not link to an NPC (0.199): ENpcShop (ENpcResidentId, ShopId) and NPC positions ENpcPlace (ENpcResidentId,
    // TerritoryTypeId, MapId, PlaceNameId, Position « x;y » in map coordinates). NPC names resolved from ENpcResident when the data is built.
    private readonly Dictionary<uint, List<uint>> npcsOfShop = [];
    private readonly Dictionary<uint, LuminaNpcPlace> npcPlaces = [];
    private readonly Dictionary<uint, string> npcNames = [];
    public IReadOnlyList<uint> NpcsOfShop(uint shopId) => npcsOfShop.TryGetValue(shopId, out var list) ? list : [];
    public LuminaNpcPlace? PlaceOf(uint npcId) => npcPlaces.TryGetValue(npcId, out var place) ? place : null;
    public string? NpcName(uint npcId) => npcNames.GetValueOrDefault(npcId);
    // ItemSupplement (0.197): ItemId obtained from SourceItemId by a source kind (1 desynthesis, 2 reduction, 3 loot, 4 gardening, 5 Skybuilders,
    // 6 card packs, 7 coffer, 8 Palace of the Dead, 9 Heaven-on-High, 10 Eureka Orthos, 11–14 Eureka, 15 Bozja, 16 logogram,
    // 17 Pilgrim's Traverse, 18 Oizys, 19 Auxesia — enum LuminaSupplemental.Excel.Model.ItemSupplementSource). Per item and kind: count and
    // the first sources (crystals have thousands of desynthesis sources).
    private readonly Dictionary<uint, Dictionary<int, (int Count, List<uint> Sources)>> supplements = [];
    public IReadOnlyList<(int Kind, int Count, IReadOnlyList<uint> Sources)> SupplementsOf(uint itemId) =>
        supplements.TryGetValue(itemId, out var kinds)
            ? kinds.OrderBy(pair => pair.Key).Select(pair => (pair.Key, pair.Value.Count, (IReadOnlyList<uint>)pair.Value.Sources)).ToArray() : [];
    // Other item sources (0.189): monsters (MobDrop: ItemId, BNpcNameId), FATEs (FateItem: ItemId, FateId), retainer ventures
    // (RetainerVentureItem), online store (StoreItem: ItemId, PriceCentsUSD). Names resolved from the game sheets when the data is built.
    private readonly Dictionary<uint, List<string>> monsters = [], fates = [];
    private readonly HashSet<uint> ventures = [];
    private readonly Dictionary<uint, uint> storeCents = [];
    public IReadOnlyList<string> MonstersOf(uint itemId) => monsters.TryGetValue(itemId, out var list) ? list : [];
    public IReadOnlyList<string> FatesOf(uint itemId) => fates.TryGetValue(itemId, out var list) ? list : [];
    public bool FromVenture(uint itemId) => ventures.Contains(itemId);
    public uint? StorePriceCents(uint itemId) => storeCents.TryGetValue(itemId, out var cents) ? cents : null;

    public LuminaSupplementalData(IReadOnlyDictionary<string, string> csv, Func<uint, string?>? monsterName = null, Func<uint, string?>? fateName = null,
        Func<uint, string?>? npcName = null, Func<uint, string?>? questName = null)
    {
        ArgumentNullException.ThrowIfNull(csv);
        foreach (var row in Rows(csv, "ENpcShop"))
        {
            var npc = Uint(row, "ENpcResidentId"); var shop = Uint(row, "ShopId");
            if (npc == 0 || shop == 0) continue;
            if (!npcsOfShop.TryGetValue(shop, out var list)) npcsOfShop[shop] = list = [];
            if (!list.Contains(npc)) list.Add(npc);
            if (!npcNames.ContainsKey(npc) && npcName?.Invoke(npc) is { Length: > 0 } name) npcNames[npc] = name;
        }
        foreach (var row in Rows(csv, "ENpcPlace"))
        {
            var npc = Uint(row, "ENpcResidentId");
            var position = (row.GetValueOrDefault("Position") ?? "").Split(';');
            if (npc == 0 || npcPlaces.ContainsKey(npc) || position.Length != 2
                || !float.TryParse(position[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                || !float.TryParse(position[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y)) continue;
            npcPlaces[npc] = new(npc, Uint(row, "TerritoryTypeId"), Uint(row, "MapId"), x, y);
        }
        void Named(Dictionary<uint, List<string>> target, uint item, string? name)
        {
            if (item == 0 || string.IsNullOrWhiteSpace(name)) return;
            if (!target.TryGetValue(item, out var list)) target[item] = list = [];
            if (list.Count < 8 && !list.Contains(name)) list.Add(name);
        }
        foreach (var row in Rows(csv, "MobDrop")) Named(monsters, Uint(row, "ItemId"), monsterName?.Invoke(Uint(row, "BNpcNameId")));
        foreach (var row in Rows(csv, "FateItem")) Named(fates, Uint(row, "ItemId"), fateName?.Invoke(Uint(row, "FateId")));
        foreach (var row in Rows(csv, "RetainerVentureItem")) if (Uint(row, "ItemId") is var venture and not 0) ventures.Add(venture);
        foreach (var row in Rows(csv, "ItemSupplement"))
        {
            var item = Uint(row, "ItemId"); var source = Uint(row, "SourceItemId"); var kind = (int)Uint(row, "ItemSupplementSource");
            if (item == 0 || source == 0 || kind == 0) continue;
            if (!supplements.TryGetValue(item, out var kinds)) supplements[item] = kinds = [];
            var entry = kinds.TryGetValue(kind, out var known) ? known : (0, new List<uint>());
            if (entry.Item2.Count < 8 && !entry.Item2.Contains(source)) entry.Item2.Add(source);
            kinds[kind] = (entry.Item1 + 1, entry.Item2);
            if (!gives.TryGetValue(source, out var given)) gives[source] = given = [];
            var out_ = given.TryGetValue(kind, out var knownOut) ? knownOut : (0, new List<uint>());
            if (out_.Item2.Count < 24 && !out_.Item2.Contains(item)) out_.Item2.Add(item);
            given[kind] = (out_.Item1 + 1, out_.Item2);
        }
        foreach (var row in Rows(csv, "QuestRequiredItem"))
        {
            var item = Uint(row, "ItemId"); var quest = Uint(row, "QuestId");
            if (item == 0 || quest == 0) continue;
            if (!questsRequiring.TryGetValue(item, out var list)) questsRequiring[item] = list = [];
            if (list.Count < 16 && !list.Any(known => known.QuestId == quest))
                list.Add((quest, questName?.Invoke(quest) ?? "", Uint(row, "Quantity"), string.Equals(row.GetValueOrDefault("IsHq"), "True", StringComparison.OrdinalIgnoreCase)));
        }
        foreach (var row in Rows(csv, "StoreItem")) if (Uint(row, "ItemId") is var stored and not 0 && Uint(row, "PriceCentsUSD") is var cents and not 0) storeCents.TryAdd(stored, cents);
        var unique = new HashSet<(uint, uint, DutyLootSourceKind, int)>();
        void Add(uint duty, uint item, DutyLootSourceKind kind, int index)
        {
            if (duty == 0 || item == 0 || !unique.Add((duty, item, kind, index))) return;
            if (!byDuty.TryGetValue(duty, out var list)) byDuty[duty] = list = [];
            list.Add(new(item, kind, index, ""));
            if (!dutiesOfItem.TryGetValue(item, out var set)) dutiesOfItem[item] = set = [];
            set.Add(duty);
        }
        foreach (var row in Rows(csv, "DungeonBossChest")) Add(Uint(row, "ContentFinderConditionId"), Uint(row, "ItemId"), DutyLootSourceKind.FightCoffer, (int)Uint(row, "FightNo"));
        foreach (var row in Rows(csv, "DungeonBossDrop")) Add(Uint(row, "ContentFinderConditionId"), Uint(row, "ItemId"), DutyLootSourceKind.FightCoffer, (int)Uint(row, "FightNo"));
        var chests = Rows(csv, "DungeonChest").ToDictionary(row => Uint(row, "RowId"), row => (Duty: Uint(row, "ContentFinderConditionId"), Number: (int)Uint(row, "ChestNo")));
        foreach (var row in Rows(csv, "DungeonChestItem"))
            if (chests.TryGetValue(Uint(row, "ChestId"), out var chest)) Add(chest.Duty, Uint(row, "ItemId"), DutyLootSourceKind.ZoneCoffer, Math.Max(0, chest.Number - 1));
    }

    /// <summary>Boss coffers and drops (FightCoffer, fight number) and chests (ZoneCoffer, chest number) of a duty (ContentFinderCondition id).</summary>
    public IReadOnlyList<DutyLootEntry> ForDuty(uint dutyId) => byDuty.TryGetValue(dutyId, out var list) ? list : [];
    /// <summary>Duties (ContentFinderCondition ids) whose bosses or chests give the item.</summary>
    public IReadOnlyCollection<uint> DutiesOf(uint itemId) => dutiesOfItem.TryGetValue(itemId, out var set) ? set : [];

    private static uint Uint(Dictionary<string, string> row, string column) =>
        row.TryGetValue(column, out var text) && uint.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : 0;
    // Plain CSV: header line, comma separated, no quoted commas in these files (positions use « ; »).
    private static IEnumerable<Dictionary<string, string>> Rows(IReadOnlyDictionary<string, string> csv, string file)
    {
        if (!csv.TryGetValue(file, out var text)) yield break;
        var lines = text.Split('\n');
        if (lines.Length == 0) yield break;
        var header = lines[0].TrimEnd('\r').Split(',');
        for (var index = 1; index < lines.Length; index++)
        {
            var line = lines[index].TrimEnd('\r');
            if (line.Length == 0) continue;
            var cells = line.Split(',');
            var row = new Dictionary<string, string>(header.Length, StringComparer.Ordinal);
            for (var column = 0; column < header.Length && column < cells.Length; column++) row[header[column]] = cells[column];
            yield return row;
        }
    }
}

public sealed record LuminaNpcPlace(uint NpcId, uint TerritoryId, uint MapId, float X, float Y);
public sealed record LuminaSupplementalState(LuminaSupplementalData? Data, bool Loading, string? Error);
public sealed record LuminaCsvCache(string Text);

/// <summary>Downloads the CSVs once on first use (bounded to 4 MB each), kept in the DataCache « lumina-supplemental » for the sources lifetime.</summary>
public sealed class LuminaSupplementalService : IDisposable
{
    private const string Family = "lumina-supplemental", Version = "main-v1"; // one entry per file
    private const string Base = "https://raw.githubusercontent.com/Critical-Impact/LuminaSupplemental/main/src/LuminaSupplemental.Excel/Generated/";
    private readonly HttpClient client = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly CancellationTokenSource cancellation = new();
    private readonly Func<TimeSpan> lifetime;
    private readonly DataCache? cache;
    private LuminaSupplementalState state = new(null, false, null);
    private int started;
    public LuminaSupplementalState State => Enabled?.Invoke() == false ? new(null, false, null) : Volatile.Read(ref state);

    private readonly Func<uint, string?>? monsterName, fateName, npcName, questName;
    public LuminaSupplementalService(Func<TimeSpan> lifetime, DataCache? cache, Func<uint, string?>? monsterName = null, Func<uint, string?>? fateName = null,
        Func<uint, string?>? npcName = null, Func<uint, string?>? questName = null)
    { this.lifetime = lifetime; this.cache = cache; this.monsterName = monsterName; this.fateName = fateName; this.npcName = npcName; this.questName = questName; }

    // Settings › Advanced (0.206): when off, nothing is downloaded and the source is simply absent.
    public Func<bool>? Enabled { get; set; }
    public void Request()
    {
        if (Enabled?.Invoke() == false) return;
        if (Interlocked.Exchange(ref started, 1) == 1) return;
        Volatile.Write(ref state, new(null, true, null));
        var token = cancellation.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                var files = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var file in LuminaSupplementalData.Files)
                {
                    var cached = cache is null ? null : await cache.ReadAsync<LuminaCsvCache>(Family, file, Version, token).ConfigureAwait(false);
                    if (cached is not null && DateTimeOffset.UtcNow - cached.RecordedAt <= lifetime()) { files[file] = cached.Value.Text; continue; }
                    var text = await BoundedHttp.ReadAsync(client, new Uri(Base + file + ".csv"), 4 * 1024 * 1024, token).ConfigureAwait(false);
                    files[file] = text;
                    if (cache is not null) await cache.WriteAsync(Family, file, Version, new LuminaCsvCache(text), DateTimeOffset.UtcNow).ConfigureAwait(false);
                }
                Volatile.Write(ref state, new(new LuminaSupplementalData(files, monsterName, fateName, npcName, questName), false, null));
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { Volatile.Write(ref state, new(null, false, error.Message)); Interlocked.Exchange(ref started, 0); }
        }, token);
    }

    public void Dispose() { cancellation.Cancel(); cancellation.Dispose(); client.Dispose(); }
}
