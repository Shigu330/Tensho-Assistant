using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace TenshoAssistant;

/// <summary>
/// X-109 (0.981, DEC-047): the team panel beside the game's Beastmaster board, before entering and, in the labyrinth, only while its board is open and
/// out of combat. Its own window system: during a cut it is the only thing Tensho draws, and only then.
/// </summary>
internal sealed class BeastBoardWindow : Window
{
    private readonly MainWindow owner;
    public Func<Beastmaster.BoardWindows> Windows { get; init; } = () => default;
    public BeastBoardWindow(MainWindow owner) : base("Tensho###TenshoBeastBoard", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.owner = owner;
        RespectCloseHotkey = false;
    }
    public override void PreDraw()
    {
        var at = Windows();
        var x = at.Party ? at.PartyX + at.PartyWidth + 8 : at.X + at.Width + 8;
        ImGui.SetNextWindowPos(new Vector2(x, at.Y), ImGuiCond.Always);
        ImGui.SetNextWindowSizeConstraints(new Vector2(300, 0), new Vector2(460, Math.Max(300, at.Height)));
    }
    public override void Draw() => owner.DrawBeastBoardPanel();
}
