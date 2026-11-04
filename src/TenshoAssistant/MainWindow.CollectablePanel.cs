using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gathering;

namespace TenshoAssistant;

/// <summary>
/// R-06 (0.409): while the game's Collectable window is open, the gathering panel (setting GatheringPanel) gives way to this one,
/// beside it: the item's collectability thresholds and scrip rewards from the game's sheets, GP and integrity as shown. The current
/// collectability is not read yet (no named field; developer mode lists the window's raw values to find it in game), so no next
/// action is advised.
/// </summary>
internal sealed partial class MainWindow
{
    internal CollectableReader? Collectables { get; set; }
    private string? collectableRawLogged;

    internal void DrawCollectablePanel()
    {
        if(windowDisposed || !configuration.GatheringPanel || Collectables?.Frame is not { } frame)return;
        using var theme=UI.FfxivTheme.Apply();
        ImGui.SetNextWindowPos(new Vector2(frame.X+frame.Width+6,frame.Y),ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(360,0),ImGuiCond.Always);
        var visible=ImGui.Begin(localization.Get("CollectablePanel.Title")+"###TenshoCollectablePanel",
            ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoMove|ImGuiWindowFlags.AlwaysAutoResize);
        try
        {
            if(!visible)return;
            // 0.478: the game's window cuts long names; the rules hold the whole one.
            var named=Collectables.Catalog?.Named(frame.ItemName);
            ImGui.TextColored(Accent,named is { Count:>0 } ? named[0].Name:frame.ItemName);
            ImGui.TextDisabled(localization.Format("CollectablePanel.Resources",Shown(frame.Gp),Shown(frame.MaxGp),Shown(frame.Integrity),Shown(frame.MaxIntegrity)));
            if(Collectables.Catalog is not { } catalog)ImGui.TextDisabled(localization.Get(Collectables.CatalogFailed ? "CollectablePanel.Unavailable":"CollectablePanel.Loading"));
            else if(catalog.Named(frame.ItemName) is not { Count:>0 } rules)ImGui.TextWrapped(localization.Get("CollectablePanel.NoRule"));
            else
                foreach(var rule in rules.DistinctBy(rule=>(rule.Low,rule.Mid,rule.High,rule.RewardLow,rule.RewardMid,rule.RewardHigh,rule.Scrips)).Take(4))
                {
                    ImGui.Separator();
                    ImGui.TextUnformatted(localization.Format("CollectablePanel.Thresholds",rule.Low,rule.Mid,rule.High));
                    if(rule.Scrips && rule.RewardHigh>0)ImGui.TextUnformatted(localization.Format("CollectablePanel.Scrips",rule.RewardLow,rule.RewardMid,rule.RewardHigh));
                    if(rule.LevelMin>0)ImGui.TextDisabled(localization.Format("CollectablePanel.Levels",rule.LevelMin,rule.LevelMax));
                }
            ImGui.Separator();
            ImGui.TextWrapped(localization.Get("CollectablePanel.Unknown"));
            // Developer mode: what the window holds goes to the log at each change, to find which value follows the collectability.
            if(developerMode && string.Join(" | ",frame.RawValues) is var raw && raw!=collectableRawLogged){collectableRawLogged=raw;log.Information("Collectable window values: {Values}",raw);}
            if(developerMode && ImGui.CollapsingHeader(localization.Get("CollectablePanel.Raw")+"###CollectableRaw"))
                foreach(var value in frame.RawValues)ImGui.TextDisabled(value);
        }
        finally { ImGui.End(); }
        static string Shown(int? value)=>value?.ToString("N0") ?? "?";
    }
}
