using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace TenshoAssistant.Maps;

/// <summary>
/// X-747 (0.1772, the user's idea I-019 of 21/11: « the summoning bells we placed in our house are not on the map, nor on the official map »):
/// a probe before any code (R06). FFXIVClientStructs gives, inside a house, the furniture placed (HousingManager.IndoorTerritory →
/// FurnitureManager.FurnitureVector: each piece's Id, Position, Rotation, Index; tools/ApiProbe, 22/11), and the game spawns interactive pieces
/// as event objects (object table). Developer mode only, read only (fields and the object table, no call but HousingManager.IsInside), every
/// five seconds while inside a house, said in the log only when it changes: the number of pieces, each kind's Id and count, and the event
/// objects with their row, the game's name and position, so that a reading in the user's house tells which Id or object is the bell
/// (docs/evidence/0.1772-housing-furniture.md). Nothing is shown yet. A failure stops it until the plugin reloads.
/// </summary>
public sealed unsafe class HousingFurnitureProbe : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IObjectTable objects;
    private readonly IPluginLog log;
    private readonly Func<bool> developer;
    private long next = -100_000;
    private string? said;
    private bool failed;

    public HousingFurnitureProbe(IFramework framework, IClientState client, IObjectTable objects, IPluginLog log, Func<bool> developer)
    {
        this.framework = framework; this.client = client; this.objects = objects; this.log = log; this.developer = developer;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework, OnUpdate);
    }

    private void OnUpdate(IFramework _)
    {
        var now = Environment.TickCount64;
        if (failed || !developer() || !client.IsLoggedIn || now - next < 5000) return;
        next = now;
        try
        {
            var manager = HousingManager.Instance();
            if (manager == null || manager->IndoorTerritory == null || !manager->IsInside()) { said = null; return; }
            var furniture = &manager->IndoorTerritory->FurnitureManager;
            var count = furniture->FurnitureVector.LongCount;
            if (count is < 0 or > 5000) { said = null; return; }
            var kinds = new Dictionary<uint, int>();
            foreach (var piece in furniture->FurnitureVector)
                if (piece.Value != null) kinds[piece.Value->Id] = kinds.GetValueOrDefault(piece.Value->Id) + 1;
            var events = objects.Where(thing => thing.ObjectKind == ObjectKind.EventObj).Take(40)
                .Select(thing => $"{thing.BaseId} « {thing.Name.TextValue} » ({thing.Position.X:F1}, {thing.Position.Z:F1})").ToArray();
            var text = $"territory {client.TerritoryType}, {count} pieces of {kinds.Count} kinds ({string.Join(", ", kinds.OrderBy(kind => kind.Key).Take(60).Select(kind => $"{kind.Key}x{kind.Value}"))}); "
                + $"{events.Length} event objects: {string.Join("; ", events)}";
            if (text == said) return;
            said = text;
            log.Information("Housing furniture probe (X-747, developer mode): {0}.", text);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            failed = true;
            log.Warning(error, "Housing furniture probe: unavailable (maybe after a game update), stopped until the plugin reloads.");
        }
    }

    public void Dispose() => TenshoAssistant.Competitive.CompetitiveGate.Remove(framework, OnUpdate);
}
