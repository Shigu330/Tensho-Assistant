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

    public static readonly string[] Files = ["DungeonBossChest", "DungeonBossDrop", "DungeonChest", "DungeonChestItem", "MobDrop", "FateItem", "RetainerVentureItem", "StoreItem"];
    // Other item sources (0.189): monsters (MobDrop: ItemId, BNpcNameId), FATEs (FateItem: ItemId, FateId), retainer ventures
    // (RetainerVentureItem), online store (StoreItem: ItemId, PriceCentsUSD). Names resolved from the game sheets when the data is built.
    private readonly Dictionary<uint, List<string>> monsters = [], fates = [];
    private readonly HashSet<uint> ventures = [];
    private readonly Dictionary<uint, uint> storeCents = [];
    public IReadOnlyList<string> MonstersOf(uint itemId) => monsters.TryGetValue(itemId, out var list) ? list : [];
    public IReadOnlyList<string> FatesOf(uint itemId) => fates.TryGetValue(itemId, out var list) ? list : [];
    public bool FromVenture(uint itemId) => ventures.Contains(itemId);
    public uint? StorePriceCents(uint itemId) => storeCents.TryGetValue(itemId, out var cents) ? cents : null;

    public LuminaSupplementalData(IReadOnlyDictionary<string, string> csv, Func<uint, string?>? monsterName = null, Func<uint, string?>? fateName = null)
    {
        ArgumentNullException.ThrowIfNull(csv);
        void Named(Dictionary<uint, List<string>> target, uint item, string? name)
        {
            if (item == 0 || string.IsNullOrWhiteSpace(name)) return;
            if (!target.TryGetValue(item, out var list)) target[item] = list = [];
            if (list.Count < 8 && !list.Contains(name)) list.Add(name);
        }
        foreach (var row in Rows(csv, "MobDrop")) Named(monsters, Uint(row, "ItemId"), monsterName?.Invoke(Uint(row, "BNpcNameId")));
        foreach (var row in Rows(csv, "FateItem")) Named(fates, Uint(row, "ItemId"), fateName?.Invoke(Uint(row, "FateId")));
        foreach (var row in Rows(csv, "RetainerVentureItem")) if (Uint(row, "ItemId") is var venture and not 0) ventures.Add(venture);
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
    public LuminaSupplementalState State => Volatile.Read(ref state);

    private readonly Func<uint, string?>? monsterName, fateName;
    public LuminaSupplementalService(Func<TimeSpan> lifetime, DataCache? cache, Func<uint, string?>? monsterName = null, Func<uint, string?>? fateName = null)
    { this.lifetime = lifetime; this.cache = cache; this.monsterName = monsterName; this.fateName = fateName; }

    public void Request()
    {
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
                Volatile.Write(ref state, new(new LuminaSupplementalData(files, monsterName, fateName), false, null));
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { Volatile.Write(ref state, new(null, false, error.Message)); Interlocked.Exchange(ref started, 0); }
        }, token);
    }

    public void Dispose() { cancellation.Cancel(); cancellation.Dispose(); client.Dispose(); }
}
