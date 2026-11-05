using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;

namespace TenshoAssistant.Beastmaster;

/// <summary>
/// Reads the Beastmaster sheets raw on a worker (their typed rows do not match the game), and on the framework thread which beasts the
/// character has (XBMManager, read-only, once it has received the list). A failure gives a state, never an empty list taken for true.
/// </summary>
internal sealed class BeastCatalogLoader : IDisposable
{
    // Addon rows of the eight families: 17741 « Thériens » for family 1 (the game's sheet of Cu sith).
    private const uint FamilyAddonBase = 17740;
    private readonly BeastService service;
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IPlayerState player;
    private readonly IPluginLog log;
    private readonly CancellationTokenSource cancellation = new();
    private long nextRead;
    private ulong owner;
    private string? failure;

    public BeastCatalogLoader(BeastService service, IDataManager data, IFramework framework, IClientState client, IPlayerState player, IPluginLog log)
    {
        this.service = service; this.framework = framework; this.client = client; this.player = player; this.log = log;
        var token = cancellation.Token;
        _ = Task.Run(() =>
        {
            try { service.Publish(Build(data, token)); }
            catch (OperationCanceledException) { }
            catch (Exception error) { log.Warning(error, "Beastmaster data unavailable."); service.Fail(error.Message); }
        }, token);
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    private static object? Cell(object? value) => value is ReadOnlySeString text ? text.ExtractText() : value;

    private static BeastCatalog Build(IDataManager data, CancellationToken token)
    {
        var sheets = new Dictionary<string, IReadOnlyList<XbmRow>>();
        foreach (var (name, columns) in BeastCatalog.Columns)
        {
            token.ThrowIfCancellationRequested();
            var raw = data.Excel.GetRawSheet(name);
            if (raw.Columns.Count != columns) throw new InvalidDataException($"{name}: {raw.Columns.Count} columns, {columns} expected.");
            var rows = new List<XbmRow>();
            if (raw is RawSubrowExcelSheet)
            {
                foreach (var group in data.Excel.GetSubrowSheet<RawSubrow>(null, name))
                    foreach (var row in group) rows.Add(new(row.RowId, row.SubrowId, Enumerable.Range(0, columns).Select(index => Cell(row.ReadColumn(index))).ToArray()));
            }
            else
            {
                foreach (var row in data.Excel.GetSheet<RawRow>(null, name)) rows.Add(new(row.RowId, 0, Enumerable.Range(0, columns).Select(index => Cell(row.ReadColumn(index))).ToArray()));
            }
            sheets[name] = rows;
        }
        var pets = data.GetExcelSheet<Pet>();
        var addon = data.GetExcelSheet<Addon>();
        var actions = data.GetExcelSheet<Lumina.Excel.Sheets.Action>();
        var enemies = data.GetExcelSheet<BNpcName>();
        var contents = data.GetExcelSheet<ContentFinderCondition>();
        return BeastCatalog.Build(sheets,
            id => pets.TryGetRow(id, out var pet) ? pet.Name.ExtractText() : "",
            family => addon.TryGetRow(FamilyAddonBase + (uint)Math.Max(family, 0), out var text) ? text.Text.ExtractText() : "",
            id => actions.TryGetRow(id, out var action) ? (int)action.AttackType.RowId : 0,
            id => enemies.TryGetRow(id, out var enemy) ? enemy.Singular.ExtractText() : "",
            id => actions.TryGetRow(id, out var action) ? action.Name.ExtractText() : "",
            id => contents.TryGetRow(id, out var content) ? (content.Name.ExtractText(), (int)content.ClassJobLevelRequired, (int)content.ItemLevelRequired) : ("", 0, 0));
    }

    private unsafe void OnUpdate(IFramework _)
    {
        if (!framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading) return;
        var now = Stopwatch.GetTimestamp();
        var current = client.IsLoggedIn && player.IsLoaded ? player.ContentId : 0;
        if (current == owner && now < nextRead) return;
        nextRead = now + Stopwatch.Frequency * 5; owner = current;
        if (current == 0 || service.State.Catalog is not { } catalog) { service.SetObtained(null); return; }
        try
        {
            var manager = XBMManager.Instance();
            if (manager == null || manager->State != XBMManager.DataState.Received) { service.SetObtained(null); return; }
            var obtained = new HashSet<uint>();
            foreach (var beast in catalog.Beasts) if (manager->IsPetUnlocked(beast.Id)) obtained.Add(beast.Id);
            service.SetObtained(obtained); failure = null;
        }
        catch (Exception error)
        {
            service.SetObtained(null, true);
            if (failure != error.Message) { failure = error.Message; log.Warning(error, "Beasts obtained unavailable; retry in five seconds."); }
        }
    }

    public void Dispose() { TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate); cancellation.Cancel(); cancellation.Dispose(); }
}
