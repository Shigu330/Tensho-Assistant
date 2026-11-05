using Dalamud.Interface.Windowing;

namespace TenshoAssistant;

/// <summary>
/// The Tensho map as a Dalamud window (WindowSystem, as Dalamud asks for regular windows; PUBLISH-018). Content and open state stay in
/// MainWindow.MapView: the plugin copies the state into IsOpen before each draw, and the close button writes it back.
/// </summary>
internal sealed class MapViewWindow : Window
{
    private readonly MainWindow owner;

    public MapViewWindow(MainWindow owner) : base("###TenshoMapView")
    {
        this.owner = owner;
        // As before the window system: Escape does not close the map (the game key M toggles it).
        RespectCloseHotkey = false;
    }

    // 0.537 (user request): on its setting, the map steps aside when a game window comes in front; its open state does not change.
    // 0.539 (user request): it comes back a moment after.
    public override bool DrawConditions()
    {
        // 0.617: asked again after a frame without it: what is open now does not hide it, and the moment's delay starts over.
        var frame=Dalamud.Bindings.ImGui.ImGui.GetFrameCount();
        if(frame!=lastConditionFrame+1){owner.MapOpened();aside=new(TimeSpan.FromSeconds(1));}
        lastConditionFrame=frame;
        return owner.MapViewVisible() && !aside.Hidden(lastPosition is { } at && owner.MapBehindGame(at, lastSize), System.Diagnostics.Stopwatch.GetElapsedTime(Started));
    }
    private static readonly long Started = System.Diagnostics.Stopwatch.GetTimestamp();
    private UI.StepAside aside = new(TimeSpan.FromSeconds(1));
    private int lastConditionFrame=-10;
    private System.Numerics.Vector2? lastPosition;
    private System.Numerics.Vector2 lastSize;
    public override void PreDraw() => owner.PrepareMapView(this);
    public override void PostDraw() { owner.FinishMapView(); if (!IsOpen) owner.CloseMapFromGame(); } // 0.514: Plugin.Draw copies the state into IsOpen before OnClose could run.
    public override void Draw()
    {
        // 0.489 (user report): the map is its own window, drawn with neither a page nor a panel: it has its line in Diagnostics.
        var position = Dalamud.Bindings.ImGui.ImGui.GetWindowPos();
        var size = Dalamud.Bindings.ImGui.ImGui.GetWindowSize();
        // 0.617 (user report): « Enlarge », or moving or resizing it by hand: what it now touches was there first and must not make it vanish. Hidden, it could not be moved back.
        if (lastPosition is { } before && (before != position || lastSize != size)) owner.MapOpened();
        lastPosition = position;
        lastSize = size;
        var started = owner.DeveloperMode ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        owner.DrawMapViewContents();
        if (started != 0) owner.RecordOverlayDraw("Map", System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }
    public override void OnClose() => owner.CloseMapFromGame();
}
