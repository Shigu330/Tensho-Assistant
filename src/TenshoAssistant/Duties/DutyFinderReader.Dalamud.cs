using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.Duties;

/// <summary>Duty selected in the game's Duty Finder (0.185): kind (0 none, 1 roulette, 2 regular) and id, with the window rectangle.</summary>
public sealed record DutyFinderSelection(byte Kind, uint Id, float X, float Y, float Width, float Height);

public sealed class DutyFinderService
{
    private DutyFinderSelection? selection;
    public DutyFinderSelection? Selection => Volatile.Read(ref selection);
    internal void Publish(DutyFinderSelection? value) => Volatile.Write(ref selection, value);
}

/// <summary>
/// Reads AgentContentsFinder.SelectedDuty (FFXIVClientStructs: ContentsId = ContentsType None/Roulette/Regular + Id) while the « ContentsFinder »
/// window is visible, five times a second on the framework thread; the regular id is taken as a ContentFinderCondition row (DutyDefinition.DutyId),
/// which the panel shows by name so that a mismatch is visible. Developer mode logs each new selection as evidence.
/// </summary>
internal sealed class DutyFinderReader : IDisposable
{
    private readonly DutyFinderService service;
    private readonly IFramework framework;
    private readonly IGameGui gui;
    private readonly IPluginLog log;
    private readonly Func<bool> capture;
    private long nextRead;
    private (byte, uint)? lastLogged;

    public DutyFinderReader(DutyFinderService service, IFramework framework, IGameGui gui, IPluginLog log, Func<bool> capture)
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
            var handle = gui.GetAddonByName("ContentsFinder");
            if (handle.IsNull || !handle.IsReady || !handle.IsVisible || gui.GameUiHidden) { if (service.Selection is not null) service.Publish(null); return; }
            var agent = AgentContentsFinder.Instance();
            var unit = (AtkUnitBase*)handle.Address;
            if (agent == null || unit->RootNode == null) { service.Publish(null); return; }
            var selected = agent->SelectedDuty;
            var value = new DutyFinderSelection((byte)selected.ContentType, selected.Id, unit->X, unit->Y, unit->RootNode->Width * unit->Scale, unit->RootNode->Height * unit->Scale);
            if (value != service.Selection) service.Publish(value);
            if (capture() && lastLogged != (value.Kind, value.Id))
            {
                lastLogged = (value.Kind, value.Id);
                log.Information("Duty finder selection: kind {Kind} id {Id}", value.Kind, value.Id);
            }
        }
        catch (Exception error) { service.Publish(null); nextRead = now + Stopwatch.Frequency * 5; log.Warning(error, "Duty finder selection unavailable."); }
    }

    public void Dispose() { framework.Update -= OnUpdate; service.Publish(null); }
}
