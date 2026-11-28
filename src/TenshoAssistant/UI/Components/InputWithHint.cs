using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

// X-607 (0.1944, user request of 18/11): one form for a text field with a translated hint (a search box, a name to type). The same call as
// the fields written by hand before it, so the pages are unchanged; its tooltip, when it has one, follows with Tip(key) as before.
internal sealed partial class MainWindow
{
    /// <summary>A text field whose hint is the translated text of <paramref name="hintKey"/>; true when the text changed.</summary>
    private bool InputWithHint(string id,string hintKey,ref string text,int maximum)
        =>ImGui.InputTextWithHint(id,localization.Get(hintKey),ref text,maximum);
}
