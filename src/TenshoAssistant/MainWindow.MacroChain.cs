using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Macros;
using TenshoAssistant.Items;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private readonly List<MacroSlot> macroPlan = [];
    private bool macroPlanLoaded;
    private int macroBook;
    private bool macroPlanSaved;
    private string? macroPlanError;
    private void DrawMacroChain()
    {
        Heading(localization.Get("MacroUi.Title"), localization.Get("MacroUi.Description"));
        if (!macroPlanLoaded)
        {
            macroPlan.AddRange((configuration.MacroChainPlan ?? []).Where(slot => slot is not null && slot.Book <= 1 && slot.Index < 100).Take(32));
            macroPlanLoaded = true; macroChain.RequestRefresh();
        }
        var state = macroChain.State;
        var active = state.Sequence.Status is MacroSequenceStatus.Ready or MacroSequenceStatus.AwaitingStart or MacroSequenceStatus.Running;
        ImGui.TextWrapped(localization.Get("MacroUi.Help"));
        if (ImGui.SmallButton(localization.Get("MacroUi.Copy"))) ImGui.SetClipboardText(MacroDefinition.Marker);
        ImGui.SameLine();
        if (ImGui.SmallButton(localization.Get("MacroUi.Refresh"))) macroChain.RequestRefresh();
        if (state.Loading) ImGui.TextDisabled(localization.Get("MacroUi.Loading"));
        if (state.CharacterId == 0) ImGui.TextDisabled(localization.Get("MacroUi.Offline"));
        if (state.Error is { } error) {DrawMacroWrappedText(localization.Get("MacroCounts.Error"));if(developerMode)DrawMacroWrappedText(error);}
        ImGui.TextColored(active ? Accent : Muted,localization.Format("MacroCounts.Progress",MacroStatus(state.Sequence.Status),state.Sequence.Completed,state.Sequence.Plan.Count));
        if (developerMode && state.Sequence.Message is { } message) DrawMacroWrappedText(message);
        ImGui.TextDisabled(localization.Get("MacroUi.Experimental"));
        ImGui.BeginDisabled(active);
        try
        {
            if (ImGui.RadioButton(localization.Get("MacroUi.Personal"), macroBook == 0)) macroBook = 0;
            ImGui.SameLine(); if (ImGui.RadioButton(localization.Get("MacroUi.Shared"), macroBook == 1)) macroBook = 1;
            var visible = ImGui.BeginChild("MacroCatalog", new Vector2(0, Math.Clamp(ImGui.GetContentRegionAvail().Y*.35f,90,190)), true);
            try
            {
                if (visible)
                foreach (var macro in state.Catalog.Where(row => row.Slot.Book == macroBook))
                {
                    ImGui.PushID($"Macro{macro.Slot.Book}/{macro.Slot.Index}");
                    try
                    {
                        DrawItemIcon(new ItemDefinition(0, macro.Slot.Name, false, macro.IconId), new Vector2(24));
                        ImGui.SameLine();
                        if (ImGui.SmallButton(localization.Get("MacroCounts.Add")+$"###AddMacro{macro.Slot.Index}") && macroPlan.Count < 32)
                        { macroPlan.Add(macro.Slot); macroPlanSaved = false; }
                        ImGui.SameLine(); ImGui.TextUnformatted($"{macro.Slot.Index} · {macro.Slot.Name}");
                        if (macro.ValidationError() is { } validation)
                        { ImGui.SameLine(); ImGui.TextColored(Accent, localization.Get("MacroUi.Marker")); if (ImGui.IsItemHovered()) WrappedTooltip(validation); }
                    }
                    finally { ImGui.PopID(); }
                }
            }
            finally { ImGui.EndChild(); }
            ImGui.TextColored(Accent,localization.Format("MacroCounts.Plan",macroPlan.Count));
            var showPlan = ImGui.BeginChild("MacroPlan", new Vector2(0, 160), true);
            try
            {
                if (showPlan)
                for (var index = 0; index < macroPlan.Count; index++)
                {
                    var slot = macroPlan[index];
                    ImGui.PushID(index);
                    var remove = false;
                    try
                    {
                        if (ImGui.SmallButton(localization.Get("MacroUi.Remove"))) remove = true;
                        ImGui.SameLine();
                        if (index > 0 && ImGui.SmallButton("↑"))
                        { (macroPlan[index - 1], macroPlan[index]) = (macroPlan[index], macroPlan[index - 1]); macroPlanSaved = false; }
                        ImGui.SameLine(); ImGui.TextUnformatted($"{index + 1}. {(slot.Book == 0 ? localization.Get("MacroUi.PersonalSlot") : localization.Get("MacroUi.SharedSlot"))} {slot.Index} · {slot.Name}");
                    }
                    finally { ImGui.PopID(); }
                    if (remove) { macroPlan.RemoveAt(index); index--; macroPlanSaved = false; }
                }
            }
            finally { ImGui.EndChild(); }
            if (ImGui.Button(localization.Get("MacroUi.Save")))
            {
                var previous = configuration.MacroChainPlan ?? [];
                configuration.MacroChainPlan = macroPlan.ToList();
                try { save(); macroPlanSaved = true; macroPlanError = null; }
                catch (Exception exception)
                { configuration.MacroChainPlan = previous; macroPlanError = localization.Get("MacroUi.SaveError"); log.Warning(exception, "Could not save macro chain plan."); }
            }
            ImGui.SameLine();
            ImGui.BeginDisabled(macroPlan.Count == 0 || state.CharacterId == 0 || state.Loading);
            try { if (ImGui.Button(localization.Get("MacroUi.Start"))) macroChain.RequestStart(macroPlan); }
            finally { ImGui.EndDisabled(); }
        }
        finally { ImGui.EndDisabled(); }
        if (active && ImGui.Button(localization.Get("MacroUi.Stop"))) macroChain.RequestCancel();
        ImGui.TextWrapped(localization.Get("MacroUi.StopHelp"));
        if (macroPlanSaved) ImGui.TextColored(Success, localization.Get("MacroUi.Saved"));
        if (macroPlanError is { } saveMessage) ImGui.TextWrapped(saveMessage);
    }
    private string MacroStatus(MacroSequenceStatus status) => status switch
    {
        MacroSequenceStatus.Ready => localization.Get("MacroUi.Ready"),
        MacroSequenceStatus.AwaitingStart => localization.Get("MacroUi.Awaiting"),
        MacroSequenceStatus.Running => localization.Get("MacroUi.Running"),
        MacroSequenceStatus.Complete => localization.Get("MacroUi.Complete"),
        MacroSequenceStatus.Cancelled => localization.Get("MacroUi.Cancelled"),
        MacroSequenceStatus.Failed => localization.Get("MacroUi.Failed"),
        _ => localization.Get("MacroUi.Idle"),
    };
    private static void DrawMacroWrappedText(string text)
    {
        ImGui.PushTextWrapPos(0);
        try { ImGui.TextUnformatted(text); }
        finally { ImGui.PopTextWrapPos(); }
    }
}
