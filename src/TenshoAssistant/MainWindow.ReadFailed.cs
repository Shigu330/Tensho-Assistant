using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

// C36 (0.601): after a game update, a panel whose reading fails says so where it stands, instead of disappearing: a short line beside the game's
// window, and in Diagnostics which reading failed. The cause stays in the log.
internal sealed partial class MainWindow
{
    /// <summary>Language keys of the readings that fail now.</summary>
    internal IReadOnlyList<string> FailedReadings()
    {
        var parts=new List<string>();
        if(configuration.CraftLogHelperEnabled && craftLog.Error is not null)parts.Add("CraftLog");
        if(CraftLiveWanted(configuration) && craftLive.State.Status==Craft.CraftLiveStatusKind.Error)parts.Add("CraftLive");
        if(configuration.GatheringPanel && gatheringObservation.State.Status==Gathering.GatheringObservationStatus.Error)parts.Add("Gathering");
        return parts;
    }

    private void DrawReadFailedPanel(string id,string addonName,string part)
    {
        var addon=gameGui.GetAddonByName(addonName);
        if(addon.IsNull || !addon.IsVisible)return;
        var viewport=ImGui.GetMainViewport();var width=Math.Min(320,viewport.WorkSize.X);
        var position=viewport.Pos+addon.Position+new Vector2(addon.ScaledWidth+6,0);
        if(position.X+width>viewport.WorkPos.X+viewport.WorkSize.X)position.X=viewport.Pos.X+addon.Position.X-width-6;
        var text=localization.Format("ReadFailed.Panel",localization.Get("ReadFailed."+part));
        Panels?.Show(id,localization.Get("ReadFailed."+part),ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoCollapse,
            ()=>{ImGui.SetNextWindowPos(position,ImGuiCond.Always);ImGui.SetNextWindowSize(new Vector2(width,0),ImGuiCond.Always);},
            ()=>{ImGui.PushStyleColor(ImGuiCol.Text,UI.FfxivTheme.Warning);try{ImGui.TextWrapped(text);}finally{ImGui.PopStyleColor();}});
    }
}
