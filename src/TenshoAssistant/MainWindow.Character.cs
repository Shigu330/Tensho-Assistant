using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Character;

namespace TenshoAssistant;

// The panel beside the game's Character window (0.182, setting ShowCharacterPanel); the Character page was removed in 0.341 (user request:
// the Gear page shows the same real values, with the same table and help, DrawDerivedStats):
// combat jobs get derived values (critical rate and damage, direct hit, determination, speed, tenacity, piety, defenses) computed by
// Character/StatFormulas.cs from the game's attributes and level modifiers (ParamGrow; main-stat table from Character Panel Refined, MIT);
// gatherers and crafters show their own three attributes. Character/CharacterStatsReader copies the values on the framework thread.
internal sealed partial class MainWindow
{
    // Combat jobs (ClassJob category 30 war, 31 magic) get real values; gathering and crafting their own three attributes (0.182).
    private static bool CombatCategory(int category)=>category is 30 or 31;

    // Table of real values; with the raw attribute in the Tensho page, without it beside the game window (the game shows it).
    private void DrawDerivedStats(CharacterStatsState state,bool withRaw)
    {
        var rows=StatFormulas.Combat(state.Values,state.Category,state.Role,state.Modifiers);
        // 0.380 (g, M-09): one more column, the points to the next tier (wasted points in the help).
        if(!ImGui.BeginTable("CharacterDerived",withRaw ? 4:3,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp))return;
        try
        {
            foreach(var row in rows)
            {
                ImGui.TableNextRow();ImGui.TableNextColumn();ImGui.TextUnformatted(localization.Get("CharacterStats."+row.Key));
                if(ImGui.IsItemHovered())DrawStatHelp(row,state.Modifiers);
                ImGui.TableNextColumn();ImGui.TextColored(Success,row.Value);
                if(ImGui.IsItemHovered())DrawStatHelp(row,state.Modifiers);
                if(withRaw){ImGui.TableNextColumn();ImGui.TextDisabled(row.Raw.ToString("N0"));}
                ImGui.TableNextColumn();
                if(StatFormulas.Tier(row.Key,row.Raw,state.Modifiers) is { } tier)
                {
                    ImGui.TextDisabled(localization.Format("CharacterStats.TierShort",tier.ToNext));
                    if(ImGui.IsItemHovered())DrawStatHelp(row,state.Modifiers);
                }
            }
        }
        finally{ImGui.EndTable();}
    }
    private void DrawStatHelp(DerivedStat row,LevelModifiers modifiers)
    {
        using(TooltipScope.Begin())
        {
            ImGui.TextColored(Accent,localization.Get("CharacterStats."+row.Key));
            ImGui.TextWrapped(localization.Get("CharacterStats."+row.Key+"Help"));
            ImGui.TextDisabled(localization.Get("CharacterStats."+row.Key+"Formula"));
            ImGui.TextDisabled(localization.Format("CharacterStats.Modifiers",modifiers.Main,modifiers.Sub,modifiers.Div,row.Raw));
            // 0.380 (g, M-09): stat tier.
            if(StatFormulas.Tier(row.Key,row.Raw,modifiers) is { } tier)ImGui.TextColored(Accent,localization.Format("CharacterStats.Tier",tier.ToNext,tier.Wasted));
        }
    }

    // Beside the game's Character window: only the real values the game does not show (0.182).
    internal void DrawCharacterPanel()
    {
        if(windowDisposed || !configuration.ShowCharacterPanel || characterStats.Window is not { } window)return;
        var state=characterStats.State;
        if(state.CharacterId==0 || !CombatCategory(state.Category) || state.Modifiers.Div<=0)return;
        Panels?.Show("TenshoCharacterPanel",localization.Get("CharacterStats.Panel"),ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.AlwaysAutoResize|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse,()=>{ImGui.SetNextWindowPos(new Vector2(window.X+window.Width+6,window.Y)+WindowMoved("Character",window.X,window.Y),ImGuiCond.Always);},()=>
        {
            ImGui.TextColored(Accent,localization.Format("CharacterStats.PanelJob",state.Job,state.Level));
            DrawDerivedStats(state,false);
            if(state.Modifiers.Main==0)ImGui.TextColored(Accent,localization.Format("CharacterStats.NoMain",state.Level));
            ImGui.TextDisabled(localization.Get("CharacterStats.HoverHint"));
        });
    }
}
