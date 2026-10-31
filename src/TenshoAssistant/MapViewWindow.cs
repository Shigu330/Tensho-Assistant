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

    public override bool DrawConditions() => owner.MapViewVisible();
    public override void PreDraw() => owner.PrepareMapView(this);
    public override void PostDraw() => owner.FinishMapView();
    public override void Draw() => owner.DrawMapViewContents();
    public override void OnClose() => owner.CloseMapFromGame();
}
