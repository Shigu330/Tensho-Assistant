using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.Fashion;

/// <summary>
/// Reads the « FashionCheck » window (Fashion Report) five times a second on the framework thread while it is visible: the hint category of
/// slot n is the string AtkValue 2 + 11 × n (layout learned from the Avant-Garde plugin, AGPL, read only; this code is original) and the
/// window rectangle. Developer mode logs the 11 categories once per change as evidence.
/// </summary>
internal sealed class FashionCheckReader : IDisposable
{
    private readonly FashionCheckService service;
    private readonly IFramework framework;
    private readonly IGameGui gui;
    private readonly IPluginLog log;
    private readonly Func<bool> capture;
    private long nextRead;
    private bool warned;
    private string? lastLogged;

    /// <summary>X-73.4 (0.861): the outfit judged (week theme, slots), sent once per change for the discoveries base; null: not wanted.</summary>
    public Action<uint, IReadOnlyList<FashionEvaluation>>? Judged { get; set; }
    private string? judgedSeen;

    public FashionCheckReader(FashionCheckService service, IFramework framework, IGameGui gui, IPluginLog log, Func<bool> capture)
    {
        this.service = service; this.framework = framework; this.gui = gui; this.log = log; this.capture = capture;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    private unsafe void OnUpdate(IFramework _)
    {
        if (!framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading) return;
        var now = Stopwatch.GetTimestamp();
        if (now < nextRead) return;
        nextRead = now + Stopwatch.Frequency / 5;
        try
        {
            var handle = gui.GetAddonByName("FashionCheck");
            if (handle.IsNull || !handle.IsReady || !handle.IsVisible || gui.GameUiHidden) { if (service.Live is not null) service.Publish(null); return; }
            var unit = (AtkUnitBase*)handle.Address;
            if (unit->RootNode == null) { service.Publish(null); return; }
            var categories = new string[FashionWeek.SlotCount];
            for (var slot = 0; slot < FashionWeek.SlotCount; slot++)
            {
                var index = 2 + slot * 11;
                // R-14 (0.453): a slot value the window does not have is a changed layout (unknown), never « no category this week ».
                if (unit->AtkValues == null || index >= unit->AtkValuesCount) throw new InvalidOperationException($"FashionCheck has {unit->AtkValuesCount} values, slot {slot} needs index {index}.");
                var value = unit->AtkValues[index];
                categories[slot] = value.Type is AtkValueType.String or AtkValueType.ManagedString or AtkValueType.ConstString
                    ? value.GetValueAsString() ?? "" : "";
            }
            // Slot rows (0.219): node 8 + n of the window (layout learned from Avant-Garde, read only), screen position and size.
            var rects = new FashionSlotRect[FashionWeek.SlotCount];
            for (var slot = 0; slot < FashionWeek.SlotCount; slot++)
            {
                var node = unit->GetNodeById((uint)(8 + slot));
                rects[slot] = node == null || !node->IsVisible() ? default
                    : new(node->ScreenX, node->ScreenY, node->Width * unit->Scale, node->Height * unit->Scale);
            }
            // X-73.4 (0.861, user request: learn from the game what the community spreadsheet gives): the game's own categories and theme (FashionCheckManager.Theme), no
            // longer matched by name, and the outfit it judged (HighScore: item, raw evaluation per slot), for the discoveries base.
            uint[]? ids = null; uint weekly = 0;
            var manager = FFXIVClientStructs.FFXIV.Client.Game.FashionCheckManager.Instance();
            if (manager != null)
            {
                weekly = manager->Theme.WeeklyTheme;
                if (weekly != 0)
                {
                    ids = new uint[FashionWeek.SlotCount];
                    for (var slot = 0; slot < FashionWeek.SlotCount && slot < manager->Theme.ItemThemes.Length; slot++) ids[slot] = manager->Theme.ItemThemes[slot];
                }
                if (weekly != 0 && Judged is { } judged && manager->HighScore.Score > 0)
                {
                    var evaluations = new List<FashionEvaluation>();
                    var high = manager->HighScore;
                    for (var slot = 0; slot < FashionWeek.SlotCount && slot < high.ItemIds.Length && slot < high.ItemEvaluations.Length; slot++)
                        if (high.ItemIds[slot] is var item and not 0) evaluations.Add(new(slot, item % 1_000_000, ids is null ? 0 : ids[slot], high.ItemEvaluations[slot]));
                    var signature = weekly + ":" + string.Join(",", evaluations.Select(entry => $"{entry.Slot}:{entry.Item}:{entry.Evaluation}"));
                    if (evaluations.Count > 0 && signature != judgedSeen)
                    {
                        judgedSeen = signature; judged(weekly, evaluations);
                        if (capture()) log.Information("Fashion Report judged: theme {0}, score {1}: {2}", weekly, high.Score, signature);
                    }
                }
            }
            var frame = new FashionCheckFrame(categories, unit->X, unit->Y, unit->RootNode->Width * unit->Scale, unit->RootNode->Height * unit->Scale, DateTimeOffset.UtcNow, rects, ids, weekly);
            warned = false;
            var previous = service.Live;
            if (previous is null || !previous.Categories.SequenceEqual(categories) || !(previous.CategoryIds ?? []).SequenceEqual(ids ?? []) || previous.X != frame.X || previous.Y != frame.Y || previous.Width != frame.Width
                || !(previous.Slots ?? []).SequenceEqual(rects))
                service.Publish(frame);
 var key = string.Join("|", categories) + $" / ids {string.Join(",", ids ?? [])} theme {weekly} / " + string.Join(" ", rects.Select((rect, slot) => $"{slot}:{rect.X:0},{rect.Y:0},{rect.Width:0}x{rect.Height:0}"));
            if (capture() && key != lastLogged) { lastLogged = key; log.Information("Fashion Report categories and slot rectangles: {Categories}", key); }
        }
        catch (Exception error) { service.Publish(null); nextRead = now + Stopwatch.Frequency * 5; if (!warned) { warned = true; log.Warning(error, "Fashion Report window unavailable."); } }
    }

    public void Dispose() { TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate); service.Publish(null); }
}
