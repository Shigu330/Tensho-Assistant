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
    private string? lastLogged;

    public FashionCheckReader(FashionCheckService service, IFramework framework, IGameGui gui, IPluginLog log, Func<bool> capture)
    {
        this.service = service; this.framework = framework; this.gui = gui; this.log = log; this.capture = capture;
        framework.Update += OnUpdate;
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
                if (index >= unit->AtkValuesCount) { categories[slot] = ""; continue; }
                var value = unit->AtkValues[index];
                categories[slot] = value.Type is AtkValueType.String or AtkValueType.ManagedString or AtkValueType.ConstString
                    ? value.GetValueAsString() ?? "" : "";
            }
            var frame = new FashionCheckFrame(categories, unit->X, unit->Y, unit->RootNode->Width * unit->Scale, unit->RootNode->Height * unit->Scale, DateTimeOffset.UtcNow);
            var previous = service.Live;
            if (previous is null || !previous.Categories.SequenceEqual(categories) || previous.X != frame.X || previous.Y != frame.Y || previous.Width != frame.Width)
                service.Publish(frame);
            var key = string.Join("|", categories);
            if (capture() && key != lastLogged) { lastLogged = key; log.Information("Fashion Report categories: {Categories}", key); }
        }
        catch (Exception error) { service.Publish(null); nextRead = now + Stopwatch.Frequency * 5; log.Warning(error, "Fashion Report window unavailable."); }
    }

    public void Dispose() { framework.Update -= OnUpdate; service.Publish(null); }
}
