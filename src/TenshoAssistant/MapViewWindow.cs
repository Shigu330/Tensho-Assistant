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
    public override bool DrawConditions() => owner.MapViewVisible() && !aside.Hidden(lastPosition is { } at && owner.MapBehindGame(at, lastSize), System.Diagnostics.Stopwatch.GetElapsedTime(Started));
    private static readonly long Started = System.Diagnostics.Stopwatch.GetTimestamp();
    private readonly UI.StepAside aside = new(TimeSpan.FromSeconds(1));
    private System.Numerics.Vector2? lastPosition;
    private System.Numerics.Vector2 lastSize;
    public override void PreDraw() => owner.PrepareMapView(this);
    public override void PostDraw() { owner.FinishMapView(); if (!IsOpen) owner.CloseMapFromGame(); } // 0.514: Plugin.Draw copies the state into IsOpen before OnClose could run.
    public override void Draw()
    {
        // 0.489 (user report): the map is its own window, drawn with neither a page nor a panel: it has its line in Diagnostics.
        lastPosition = Dalamud.Bindings.ImGui.ImGui.GetWindowPos();
        lastSize = Dalamud.Bindings.ImGui.ImGui.GetWindowSize();
        var started = owner.DeveloperMode ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        owner.DrawMapViewContents();
        if (started != 0) owner.RecordOverlayDraw("Map", System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }
    public override void OnClose() => owner.CloseMapFromGame();
}
