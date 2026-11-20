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
    private bool borderPushed;
    private bool fadePushed;
    private bool paddingPushed;
    // X-447 D5 (0.1618): style values pushed for the panel's own corners and frame.
    private int framePushed;
    // X-503 M1 (review of 0.1482): the margins before this panel's own, given back to what it opens (popups, menus, tooltips).
    private Vector2 outerPadding;
    // X-467 (0.1504): placed from outside (the chat window over the game's messages, or reduced to its tabs): where it was before, given back once.
    private bool placed;
    private Vector2? beforeAt;
    private Vector2 beforeSize;

    public PanelWindow(MainWindow owner, PanelDefinition panel) : base(panel.Id, panel.Flags)
    {
        this.owner = owner;
        this.panel = panel;
        Position = panel.At;
        PositionCondition = ImGuiCond.FirstUseEver;
        RespectCloseHotkey = false;
        DisableWindowSounds = true;
        ShowCloseButton = panel.Close is not null;
        // X-403 (0.1434): a panel that is resized by hand (the chat window) starts at its size.
        if (panel.Size is { } size) { Size = size; SizeCondition = ImGuiCond.FirstUseEver; }
    }

    public void Follow() => IsOpen = !broken && panel.Wanted();

    public override bool DrawConditions()
    {
        // X-01 (0.1279): not before the panels' code is compiled, 6 s at most after the load since X-653 (Diagnostics/PanelGate.cs).
        if (!owner.PanelsOpen) return false;
        try { return !broken && panel.Visible() && !(panel.StepsAside?.Invoke() != false && lastPosition is { } at && owner.BarBehindGame(at, lastSize, panel.Name)); }
        catch (Exception error) { TurnOff(error); return false; }
    }

    public override void PreDraw()
    {
        if (panel.Title is { } title) WindowName = title() + panel.Id;
        Place();
        PlaceOnce();
        UI.FfxivTheme.Push();
        // X-403 (0.1434): an opacity chosen in the settings is read at each draw.
        if ((panel.Opacity?.Invoke() ?? panel.Alpha) is { } alpha) ImGui.SetNextWindowBgAlpha(alpha);
        // X-448 (0.1454): flags a setting adds at each draw (the chat window: borderless, locked), and no frame line when borderless.
        if (panel.MoreFlags is { } more) Flags = panel.Flags | more();
        borderPushed = panel.NoBorder?.Invoke() == true;
        if (borderPushed) ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        // X-447 D5 (0.1618): corners and frame thickness of its own, read at each draw (no frame line when borderless).
        var frame = panel.Frame?.Invoke();
        if (frame is { } own)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, own.Rounding); framePushed++;
            if (!borderPushed) { ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, own.Border); framePushed++; }
        }
        // X-498 M1 (0.1480): a faded panel (the chat window under a game window) is faded whole: pushed before Begin, its title bar, frame
        // and resize grip are drawn faded too, not only its content.
        var fade = panel.Fade?.Invoke();
        fadePushed = fade is not null;
        if (fade is { } alphaFaded) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, alphaFaded);
        // X-476 (0.1481): margins of its own (the chat window: narrow, as the game's chat).
        // X-447 D5 (0.1618): the margins of its own frame first (read at each draw), else its fixed ones.
        paddingPushed = frame is not null || panel.Padding is not null;
        if (paddingPushed) outerPadding = ImGui.GetStyle().WindowPadding;
        if (frame is { } framed) ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, framed.Padding);
        else if (panel.Padding is { } padding)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, padding);
        }
    }

    /// <summary>X-467 (0.1504): a place given from outside, set each frame (screen pixels, the main viewport's origin added); out of it, the
    /// place and size the window had before are given back once. Dalamud's own Position and Size are cleared first, or its first-use
    /// placement set after PreDraw would replace this one.</summary>
    private void Place()
    {
        if (panel.Place is null) return;
        PanelPlace? place;
        try { place = panel.Place(); }
        catch (Exception error) { TurnOff(error); return; }
        var origin = ImGui.GetMainViewport().Pos;
        if (place is { } now)
        {
            if (!placed) { placed = true; beforeAt = lastPosition; beforeSize = lastSize; }
            Position = null; Size = null;
            if (now.At is { } at) ImGui.SetNextWindowPos(origin + at, ImGuiCond.Always);
            ImGui.SetNextWindowSize(now.Size, ImGuiCond.Always);
            return;
        }
        if (!placed) return;
        placed = false;
        Position = null; Size = null;
        if (beforeAt is { } before) ImGui.SetNextWindowPos(before, ImGuiCond.Always);
        if (beforeSize.X > 0 && beforeSize.Y > 0) ImGui.SetNextWindowSize(beforeSize, ImGuiCond.Always);
    }

    /// <summary>X-447 D7 (0.1618): a place and size asked once (typed in the settings, or the window brought back to the screen's centre);
    /// not while the window is placed from outside.</summary>
    private void PlaceOnce()
    {
        if (placed || panel.Once is null) return;
        PanelPlace? once;
        try { once = panel.Once(); }
        catch (Exception error) { TurnOff(error); return; }
        if (once is not { } asked) return;
        Position = null; Size = null;
        if (asked.At is { } at) ImGui.SetNextWindowPos(ImGui.GetMainViewport().Pos + at, ImGuiCond.Always);
        ImGui.SetNextWindowSize(asked.Size, ImGuiCond.Always);
    }

    public override void PostDraw()
    {
        if (paddingPushed) { ImGui.PopStyleVar(); paddingPushed = false; }
        if (framePushed > 0) { ImGui.PopStyleVar(framePushed); framePushed = 0; }
        if (fadePushed) { ImGui.PopStyleVar(); fadePushed = false; }
        if (borderPushed) { ImGui.PopStyleVar(); borderPushed = false; }
        UI.FfxivTheme.Pop();
        if (!IsOpen) panel.Close?.Invoke();
    }

    public override void Draw()
    {
        lastPosition = ImGui.GetWindowPos();
        lastSize = ImGui.GetWindowSize();
        var started = owner.DeveloperMode ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        // X-503 M1 (review of 0.1482): the window took its margins at Begin; its contents get the usual ones back, so that the gear's panel,
        // the right-click menus and the tooltips it opens are not narrow too (a child without border has no margins of its own anyway).
        var inner = paddingPushed;
        if (inner) ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, outerPadding);
        try { panel.Contents(); }
        catch (Exception error) { TurnOff(error); }
        finally { if (inner) ImGui.PopStyleVar(); }
        if (started != 0) owner.RecordOverlayDraw(panel.Name, System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }

    private void TurnOff(Exception error)
    {
        broken = true;
        owner.PanelFailed(panel.Name, error);
    }
}

/// <summary>What a panel window shows: <c>Wanted</c> is its setting, <c>Visible</c> the state of the moment; no <c>Close</c>, no close button.</summary>
internal sealed record PanelDefinition(string Name, string Id, Vector2 At, ImGuiWindowFlags Flags, Func<bool> Wanted, Func<bool> Visible, Action Contents, Action? Close = null, Func<string>? Title = null, float? Alpha = null, Func<bool>? StepsAside = null, Func<float>? Opacity = null, Vector2? Size = null,
    Func<ImGuiWindowFlags>? MoreFlags = null, Func<bool>? NoBorder = null, Func<float?>? Fade = null, Vector2? Padding = null, Func<PanelPlace?>? Place = null,
    Func<PanelFrame>? Frame = null, Func<PanelPlace?>? Once = null);

/// <summary>X-447 D5 (0.1618): a panel's margins, corners and frame thickness read at each draw (the chat window's settings); they replace
/// <see cref="PanelDefinition.Padding"/>.</summary>
internal readonly record struct PanelFrame(Vector2 Padding, float Rounding, float Border);

/// <summary>X-467 (0.1504): a place given to a panel from outside, in screen pixels from the main viewport's origin: <c>At</c> null keeps
/// where the window is (only its size is set).</summary>
internal readonly record struct PanelPlace(Vector2? At, Vector2 Size);
