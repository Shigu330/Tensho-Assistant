using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

// X-607 (0.1947, user request of 18/11): one name for a framed region of a page (a list, a pane): a child with a border. The same call as
// the regions written by hand before it, so the pages are unchanged; the caller still closes it with ImGui.EndChild(), whatever it returns.
internal sealed partial class MainWindow
{
    /// <summary>Opens a child region with a border; true when it is visible. Always closed by ImGui.EndChild().</summary>
    private static bool BeginFramed(string id,Vector2 size)=>ImGui.BeginChild(id,size,true);
}
