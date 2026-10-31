using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using TripleTriad = Lumina.Excel.Sheets.TripleTriad;

namespace TenshoAssistant.MiniGames;

/// <summary>
/// Builds the Triple Triad NPC catalogue from the game sheets off the framework thread (sheet files only), then reads every 5 s on the
/// framework thread which NPCs the character has beaten (UIState.IsTripleTriadNpcBeaten with the TripleTriad row id).
/// </summary>
internal sealed class TriadNpcLoader : IDisposable
{
    private const uint CardItemAction = 3357, PackAcquisition = 8;
    private readonly TriadNpcService service;
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IPlayerState player;
    private readonly CancellationTokenSource cancellation = new();
    private long nextBeaten;
    private ulong beatenOwner;

    public TriadNpcLoader(TriadNpcService service, IDataManager data, IFramework framework, IClientState client, IPlayerState player, IPluginLog log)
    {
        this.service = service; this.framework = framework; this.client = client; this.player = player;
        var token = cancellation.Token;
        _ = Task.Run(() =>
        {
            try { service.Publish(Build(data, token)); }
            catch (OperationCanceledException) { }
            catch (Exception error) { log.Warning(error, "Triple Triad NPC catalogue unavailable."); service.Fail(error); }
        }, token);
        framework.Update += OnUpdate;
    }

    private static TriadNpcCatalog Build(IDataManager data, CancellationToken token)
    {
        var matches = data.GetExcelSheet<TripleTriad>();
        var residents = data.GetExcelSheet<ENpcResident>();
        var cardOfItem = new Dictionary<uint, uint>();
        foreach (var item in data.GetExcelSheet<Item>())
            if (item.ItemAction.RowId != 0 && item.ItemAction.IsValid && item.ItemAction.Value.Action.RowId == CardItemAction && item.ItemAction.Value.Data[0] != 0)
                cardOfItem[item.RowId] = item.ItemAction.Value.Data[0];
        token.ThrowIfCancellationRequested();
        var npcOfMatch = new Dictionary<uint, uint>();
        foreach (var npc in data.GetExcelSheet<ENpcBase>())
            foreach (var value in npc.ENpcData)
                if (value.RowId != 0 && matches.HasRow(value.RowId) && !npcOfMatch.ContainsKey(value.RowId)) { npcOfMatch[value.RowId] = npc.RowId; break; }
        token.ThrowIfCancellationRequested();
        var wanted = npcOfMatch.Values.ToHashSet();
        var places = new Dictionary<uint, (uint Map, string Zone, float X, float Y)>();
        foreach (var level in data.GetExcelSheet<Level>())
            if (level.Type == 8 && wanted.Contains(level.Object.RowId) && !places.ContainsKey(level.Object.RowId) && level.Map.RowId != 0 && level.Map.IsValid)
            {
                var map = level.Map.Value;
                places[level.Object.RowId] = (map.RowId, map.PlaceName.ValueNullable?.Name.ExtractText() ?? "",
                    Npcs.NpcShopLinks.MapCoordinate(level.X, map.OffsetX, map.SizeFactor), Npcs.NpcShopLinks.MapCoordinate(level.Z, map.OffsetY, map.SizeFactor));
            }
        token.ThrowIfCancellationRequested();
        // NPCs absent from the Level sheet (Gold Saucer players among others) are placed by the territory layouts, read as the NPC vendor
        // loader does (planevent/planner, LGB header checked first: docs/evidence/0.98.0-lgb-probe.log).
        var missing = wanted.Where(id => !places.ContainsKey(id)).ToHashSet();
        if (missing.Count > 0)
            foreach (var territory in data.GetExcelSheet<TerritoryType>())
            foreach (var kind in new[] { "planevent", "planner" })
            {
                token.ThrowIfCancellationRequested();
                if (missing.Count == 0 || territory.Map.RowId == 0 || !territory.Map.IsValid) continue;
                var bg = territory.Bg.ExtractText(); var cut = bg.IndexOf("/level/", StringComparison.Ordinal);
                if (cut < 0) continue;
                var path = $"bg/{bg[..(cut + 1)]}level/{kind}.lgb";
                Lumina.Data.Files.LgbFile? file;
                try { file = data.FileExists(path) && data.GetFile(path)?.Data is { } bytes && Npcs.NpcShopLinks.ValidLayout(bytes) ? data.GetFile<Lumina.Data.Files.LgbFile>(path) : null; }
                catch { file = null; }
                if (file is null) continue;
                var map = territory.Map.Value;
                foreach (var layer in file.Layers)
                foreach (var instance in layer.InstanceObjects)
                {
                    if (instance.AssetType != Lumina.Data.Parsing.Layer.LayerEntryType.EventNPC
                        || instance.Object is not Lumina.Data.Parsing.Layer.LayerCommon.ENPCInstanceObject npc
                        || !missing.Remove(npc.ParentData.ParentData.BaseId)) continue;
                    var position = instance.Transform.Translation;
                    places[npc.ParentData.ParentData.BaseId] = (map.RowId, map.PlaceName.ValueNullable?.Name.ExtractText() ?? "",
                        Npcs.NpcShopLinks.MapCoordinate(position.X, map.OffsetX, map.SizeFactor), Npcs.NpcShopLinks.MapCoordinate(position.Z, map.OffsetY, map.SizeFactor));
                }
            }
        var npcs = new List<TriadNpc>();
        var npcsOfCard = new Dictionary<uint, List<uint>>();
        foreach (var (matchId, npcId) in npcOfMatch)
        {
            var match = matches.GetRow(matchId);
            var rewards = match.ItemPossibleReward.Select(item => cardOfItem.GetValueOrDefault(item.RowId)).Where(card => card != 0).Distinct().ToArray();
            foreach (var card in rewards) { if (!npcsOfCard.TryGetValue(card, out var list)) npcsOfCard[card] = list = []; list.Add(matchId); }
            var name = residents.TryGetRow(npcId, out var resident) ? resident.Singular.ExtractText() : "";
            places.TryGetValue(npcId, out var place);
            npcs.Add(new(matchId, npcId, name, place.Map, place.Zone ?? "", place.X, place.Y, Array.AsReadOnly(rewards),
                Array.AsReadOnly(match.TripleTriadRule.Select(rule => rule.RowId).Where(rule => rule != 0).Distinct().ToArray()), match.Fee));
        }
        var cards = new Dictionary<uint, TriadCardSource>();
        var itemOfCard = cardOfItem.GroupBy(pair => pair.Value).ToDictionary(group => group.Key, group => group.Min(pair => pair.Key));
        var items = data.GetExcelSheet<Item>();
        foreach (var card in data.GetExcelSheet<TripleTriadCardResident>())
        {
            if (card.RowId == 0) continue;
            var pack = card.AcquisitionType.RowId == PackAcquisition && items.TryGetRow(card.Acquisition.RowId, out var packItem) ? packItem : (Item?)null;
            cards[card.RowId] = new(card.RowId, itemOfCard.GetValueOrDefault(card.RowId),
                Array.AsReadOnly((npcsOfCard.GetValueOrDefault(card.RowId) ?? []).ToArray()), pack?.RowId ?? 0, pack?.Name.ExtractText() ?? "");
        }
        return new(npcs.AsReadOnly(), cards);
    }

    private unsafe void OnUpdate(IFramework _)
    {
        if (!framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading) return;
        var now = Stopwatch.GetTimestamp();
        var owner = client.IsLoggedIn && player.IsLoaded ? player.ContentId : 0;
        if (owner == beatenOwner && now < nextBeaten) return;
        nextBeaten = now + Stopwatch.Frequency * 5; beatenOwner = owner;
        if (owner == 0 || service.State.Catalog is not { } catalog) { service.SetBeaten(null); return; }
        var ui = UIState.Instance();
        if (ui == null) { service.SetBeaten(null); return; }
        var beaten = new HashSet<uint>();
        foreach (var npc in catalog.Npcs) if (ui->IsTripleTriadNpcBeaten(npc.MatchId)) beaten.Add(npc.MatchId);
        service.SetBeaten(beaten);
    }

    public void Dispose() { framework.Update -= OnUpdate; cancellation.Cancel(); cancellation.Dispose(); }
}
