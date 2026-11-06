using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace TenshoAssistant;

/// <summary>
/// 0.514 (issue #38): a free-standing on-screen panel as a Dalamud window, as Dalamud asks of anything that looks like a window. The setting
/// keeps deciding: Follow copies it into IsOpen before each draw, and the close button writes it back in PostDraw, in the same frame, or the next
/// Follow would undo it. Escape does not close a panel: that would turn its setting off. A panel that throws turns off alone, as the others do in Plugin.Draw.
/// </summary>
internal sealed class PanelWindow : Window
{
    private readonly MainWindow owner;
    private readonly PanelDefinition panel;
    private bool broken;
    private Vector2? lastPosition;
    private Vector2 lastSize;

    public PanelWindow(MainWindow owner, PanelDefinition panel) : base(panel.Id, panel.Flags)
    {
        this.owner = owner;
        this.panel = panel;
        Position = panel.At;
        PositionCondition = ImGuiCond.FirstUseEver;
        RespectCloseHotkey = false;
        DisableWindowSounds = true;
        ShowCloseButton = panel.Close is not null;
    }

    public void Follow() => IsOpen = !broken && panel.Wanted();

    public override bool DrawConditions()
    {
        try { return !broken && panel.Visible() && !(lastPosition is { } at && owner.BarBehindGame(at, lastSize, panel.Name)); }
        catch (Exception error) { TurnOff(error); return false; }
    }

    public override void PreDraw()
    {
        if (panel.Title is { } title) WindowName = title() + panel.Id;
        UI.FfxivTheme.Push();
        if (panel.Alpha is { } alpha) ImGui.SetNextWindowBgAlpha(alpha);
    }

    public override void PostDraw()
    {
        UI.FfxivTheme.Pop();
        if (!IsOpen) panel.Close?.Invoke();
    }

    public override void Draw()
    {
        lastPosition = ImGui.GetWindowPos();
        lastSize = ImGui.GetWindowSize();
        var started = owner.DeveloperMode ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        try { panel.Contents(); }
        catch (Exception error) { TurnOff(error); }
        if (started != 0) owner.RecordOverlayDraw(panel.Name, System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }

    private void TurnOff(Exception error)
    {
        broken = true;
        owner.PanelFailed(panel.Name, error);
    }
}

/// <summary>What a panel window shows: <c>Wanted</c> is its setting, <c>Visible</c> the state of the moment; no <c>Close</c>, no close button.</summary>
internal sealed record PanelDefinition(string Name, string Id, Vector2 At, ImGuiWindowFlags Flags, Func<bool> Wanted, Func<bool> Visible, Action Contents, Action? Close = null, Func<string>? Title = null, float? Alpha = null);
