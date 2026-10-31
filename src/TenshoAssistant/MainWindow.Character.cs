using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Character;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    // Combat jobs (ClassJob category 30 war, 31 magic) get real values; gathering and crafting their own three attributes (0.182).
    private static bool CombatCategory(int category)=>category is 30 or 31;

    private void DrawCharacterStats()
    {
        Heading(localization.Get("CharacterUi.Title"), localization.Get("CharacterUi.Description"));
        var state = characterStats.State;
        if (state.CharacterId == 0) { ImGui.TextWrapped(localization.Get("CharacterUi.Offline")); return; }
        ImGui.TextColored(Accent,localization.Format("CharacterDates.Summary",state.Name,state.Job,state.Level));
        ImGui.TextDisabled(localization.Format("CharacterDates.Observed",state.ObservedAt?.ToLocalTime().ToString("HH:mm:ss") ?? "—"));
        if(CombatCategory(state.Category))
        {
            if(state.Modifiers.Div<=0){ImGui.TextWrapped(localization.Get("CharacterStats.NoModifiers"));return;}
            DrawDerivedStats(state,true);
            if(state.Modifiers.Main==0)ImGui.TextDisabled(localization.Format("CharacterStats.NoMain",state.Level));
        }
        else if(StatFormulas.Useful(state.Category) is { Count:>0 } useful && ImGui.BeginTable("CharacterUseful",2,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp))
        {
            try
            {
                foreach(var key in useful)
                {
                    ImGui.TableNextRow();ImGui.TableNextColumn();ImGui.TextUnformatted(localization.Get("CharacterStats.Attr"+key));
                    ImGui.TableNextColumn();ImGui.TextUnformatted(state.Values.TryGetValue(key,out var value) ? value.ToString("N0"):localization.Get("CharacterUi.Unknown"));
                }
            }
            finally{ImGui.EndTable();}
        }
        ImGui.TextDisabled(localization.Get("CharacterStats.Sources"));
    }

    // Table of real values; with the raw attribute in the Tensho page, without it beside the game window (the game shows it).
    private void DrawDerivedStats(CharacterStatsState state,bool withRaw)
    {
        var rows=StatFormulas.Combat(state.Values,state.Category,state.Role,state.Modifiers);
        if(!ImGui.BeginTable("CharacterDerived",withRaw ? 3:2,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp))return;
        try
        {
            foreach(var row in rows)
            {
                ImGui.TableNextRow();ImGui.TableNextColumn();ImGui.TextUnformatted(localization.Get("CharacterStats."+row.Key));
                if(ImGui.IsItemHovered())DrawStatHelp(row,state.Modifiers);
                ImGui.TableNextColumn();ImGui.TextColored(Success,row.Value);
                if(ImGui.IsItemHovered())DrawStatHelp(row,state.Modifiers);
                if(withRaw){ImGui.TableNextColumn();ImGui.TextDisabled(row.Raw.ToString("N0"));}
            }
        }
        finally{ImGui.EndTable();}
    }
    private void DrawStatHelp(DerivedStat row,LevelModifiers modifiers)
    {
        ImGui.BeginTooltip();
        try
        {
            ImGui.PushTextWrapPos(ImGui.GetFontSize()*26);
            ImGui.TextColored(Accent,localization.Get("CharacterStats."+row.Key));
            ImGui.TextWrapped(localization.Get("CharacterStats."+row.Key+"Help"));
            ImGui.TextDisabled(localization.Get("CharacterStats."+row.Key+"Formula"));
            ImGui.TextDisabled(localization.Format("CharacterStats.Modifiers",modifiers.Main,modifiers.Sub,modifiers.Div,row.Raw));
            ImGui.PopTextWrapPos();
        }
        finally{ImGui.EndTooltip();}
    }

    // Beside the game's Character window: only the real values the game does not show (0.182).
    internal void DrawCharacterPanel()
    {
        if(windowDisposed || !configuration.ShowCharacterPanel || characterStats.Window is not { } window)return;
        var state=characterStats.State;
        if(state.CharacterId==0 || !CombatCategory(state.Category) || state.Modifiers.Div<=0)return;
        using var theme=UI.FfxivTheme.Apply();
        ImGui.SetNextWindowPos(new Vector2(window.X+window.Width+6,window.Y),ImGuiCond.Always);
        var visible=ImGui.Begin(localization.Get("CharacterStats.Panel")+"###TenshoCharacterPanel",
            ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.AlwaysAutoResize|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse);
        try
        {
            if(!visible)return;
            ImGui.TextColored(Accent,localization.Format("CharacterStats.PanelJob",state.Job,state.Level));
            DrawDerivedStats(state,false);
            if(state.Modifiers.Main==0)ImGui.TextColored(Accent,localization.Format("CharacterStats.NoMain",state.Level));
            ImGui.TextDisabled(localization.Get("CharacterStats.HoverHint"));
        }
        finally{ImGui.End();}
    }
}
