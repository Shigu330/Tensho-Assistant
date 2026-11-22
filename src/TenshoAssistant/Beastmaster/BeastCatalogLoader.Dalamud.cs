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
    // X-746 (0.1766): Addon 17749 « Assimilation (Nv 22) » / « Borrow (Lv. 22) », right after the eight families.
    private const uint BorrowLabelAddon = 17749;
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
        // X-551 (0.1574): XBMElement's name with its macro codes (« <icon(56)>Feu »): the icon the game draws before the word.
        var elementMacros = new Dictionary<uint, string>();
        foreach (var row in data.Excel.GetSheet<RawRow>(null, "XBMElement"))
            if (row.ReadColumn(0) is ReadOnlySeString name) elementMacros[row.RowId] = name.ToMacroString();
        // X-746 (0.1766): the actions Borrow gives, one per family: their condition is the familiar's Kinship (Action.PrimaryCostType 125,
        // PrimaryCostValue the family); their effect from ActionTransient (docs/evidence/0.1766-beast-borrow.md).
        var transient = data.GetExcelSheet<ActionTransient>();
        var borrowActions = actions.Where(action => action.PrimaryCostType == BeastCatalog.KinshipCost)
            .Select(action => (action.RowId, (int)action.PrimaryCostType, (int)action.PrimaryCostValue, action.Name.ExtractText(), (uint)action.Icon,
                transient.TryGetRow(action.RowId, out var text) ? text.Description.ExtractText() : "")).ToArray();
        var catalog = BeastCatalog.Build(sheets,
            id => pets.TryGetRow(id, out var pet) ? pet.Name.ExtractText() : "",
            family => addon.TryGetRow(FamilyAddonBase + (uint)Math.Max(family, 0), out var text) ? text.Text.ExtractText() : "",
            id => actions.TryGetRow(id, out var action) ? (int)action.AttackType.RowId : 0,
            id => enemies.TryGetRow(id, out var enemy) ? enemy.Singular.ExtractText() : "",
            id => actions.TryGetRow(id, out var action) ? action.Name.ExtractText() : "",
            id => contents.TryGetRow(id, out var content) ? (content.Name.ExtractText(), (int)content.ClassJobLevelRequired, (int)content.ItemLevelRequired) : ("", 0, 0),
            // 0.688: 1 a zone (PlaceName), 2 a duty (ContentFinderCondition); 0 is the beast the job starts with.
            catchPlace: (kind, id) => kind switch
            {
                1 => data.GetExcelSheet<PlaceName>().GetRowOrDefault(id)?.Name.ExtractText() ?? "",
                2 => data.GetExcelSheet<ContentFinderCondition>().GetRowOrDefault(id)?.Name.ExtractText() ?? "",
                _ => "",
            },
            // X-553 (0.1523): a magical automatic attack's element.
            aspect: id => actions.TryGetRow(id, out var action) ? action.Aspect : 0,
            // X-486 (0.1559): the status an enemy's action inflicts, in the client's language.
            statusName: id => data.GetExcelSheet<Lumina.Excel.Sheets.Status>().GetRowOrDefault(id)?.Name.ExtractText() ?? "",
            // X-622 M1 (review of 0.1560): 1 helpful, 2 harmful (docs/evidence/0.1562-beast-effect-category.md).
            statusCategory: id => data.GetExcelSheet<Lumina.Excel.Sheets.Status>().GetRowOrDefault(id)?.StatusCategory ?? 0,
            // X-755 (0.1750): the names of the effects a beast inflicts, the game's own (Addon 12472 to 12481 and 17755).
            effectName: row => addon.TryGetRow(row, out var text) ? text.Text.ExtractText() : "",
            // X-746 (0.1766): « Assimilation (Nv 22) », the game's label beside the families' names (Addon 17741 to 17748).
            borrowActions: borrowActions,
            borrowLabel: addon.TryGetRow(BorrowLabelAddon, out var borrow) ? borrow.Text.ExtractText() : "",
            elementMacro: row => elementMacros.GetValueOrDefault(row, ""));
        // X-744 (0.1768): the Crucible's ephemeral items (XBMItem, read raw: 14 columns, else none, R08), their kinds' names (XBMItemType)
        // and the game's guide rules of the board's spaces and items (DescriptionString): docs/evidence/0.1768-crucible-items.md.
        // A failure here leaves the items and rules out (said on the page), never the rest of the Beastmaster's data.
        try
        {
            var familyNames = Enumerable.Range(1, 8).ToDictionary(family => family, family => addon.TryGetRow(FamilyAddonBase + (uint)family, out var text) ? text.Text.ExtractText() : "");
            var items = new List<BeastItem>();
            var itemSheet = data.Excel.GetRawSheet("XBMItem");
            if (itemSheet.Columns.Count == BeastItems.Columns)
                foreach (var row in data.Excel.GetSheet<RawRow>(null, "XBMItem"))
                    if (BeastItems.Read(new XbmRow(row.RowId, 0, Enumerable.Range(0, BeastItems.Columns).Select(index => Cell(row.ReadColumn(index))).ToArray()), familyNames) is { } item) items.Add(item);
            var kinds = new Dictionary<int, string>();
            foreach (var row in data.Excel.GetSheet<RawRow>(null, "XBMItemType"))
                if (row.RowId is >= 1 and <= 3 && Cell(row.ReadColumn(0)) is string kind && kind.Length > 0) kinds[(int)row.RowId] = kind;
            var guide = data.Excel.GetSheet<RawRow>(null, "DescriptionString");
            string GuideText(uint id) => guide.TryGetRow(id, out var row) && Cell(row.ReadColumn(0)) is string text ? text.Trim() : "";
            var rules = BeastItems.Guide.Select(rule => new BeastGuideRule(rule.Key, GuideText(rule.Title), GuideText(rule.Text))).Where(rule => rule.Title.Length > 0 && rule.Text.Length > 0).ToArray();
            return catalog with { Items = items, ItemKinds = kinds, Rules = rules };
        }
        catch (Exception error) when (error is not OperationCanceledException and not OutOfMemoryException) { return catalog; }
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
