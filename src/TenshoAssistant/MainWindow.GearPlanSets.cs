using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gear;

namespace TenshoAssistant;

// T25 (0.593): several named plans per job on the Gear page, above the planner. Taking a plan moves it into GearPlans and GearMateria, where
// the planner and the share code read it, so each one is shared by code once taken.
internal sealed partial class MainWindow
{
    private enum GearPlanEdit {None,New,Rename}
    private GearPlanEdit gearPlanEdit;
    private string gearPlanName="";
    private bool gearPlanNameRefused;

    private void LoadGearPlan(uint job,GearNamedPlan plan)
    {
        var plans=configuration.GearPlans??=[];
        if(plan.Pieces.Count>0)plans[job]=plan.Pieces;else plans.Remove(job);
        if(plan.Materia.Count>0)(configuration.GearMateria??=[])[job]=plan.Materia;else configuration.GearMateria?.Remove(job);
    }

    private void DrawGearPlanPicker(uint job)
    {
        var defaultName=localization.Get("Gear.PlanDefault");
        var sets=configuration.GearPlanSets;
        var set=sets?.GetValueOrDefault(job);
        ImGui.SetNextItemWidth(220);
        if(ImGui.BeginCombo(localization.Get("Gear.PlanPick")+"###GearPlanPick",set?.Active ?? defaultName))
        {
            try
            {
                ImGui.Selectable(set?.Active ?? defaultName,true);
                foreach(var name in (set?.Others ?? []).Select(plan=>plan.Name).Order(StringComparer.CurrentCultureIgnoreCase).ToArray())
                {
                    if(!ImGui.Selectable(name+"###"+name))continue;
                    if(GearPlanSets.Switch(set!,name,configuration.GearPlans?.GetValueOrDefault(job),configuration.GearMateria?.GetValueOrDefault(job)) is { } taken)
                    {LoadGearPlan(job,taken);gearPlanEdit=GearPlanEdit.None;SaveGearPlan();}
                }
            }
            finally{ImGui.EndCombo();}
        }
        Tip("Gear.PlanPickTip");
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("Gear.PlanNew"))){gearPlanEdit=GearPlanEdit.New;gearPlanName="";gearPlanNameRefused=false;}
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("Gear.PlanNewTip"));
        ImGui.SameLine();
        ImGui.BeginDisabled(set is null);
        if(ImGui.SmallButton(localization.Get("Gear.PlanRename"))){gearPlanEdit=GearPlanEdit.Rename;gearPlanName=set?.Active ?? "";gearPlanNameRefused=false;}
        ImGui.EndDisabled();
        ImGui.SameLine();
        ImGui.BeginDisabled(set is not {Others.Count:>0});
        if(ImGui.SmallButton(localization.Get("Gear.PlanDelete")) && set is not null && GearPlanSets.Delete(set) is { } next){LoadGearPlan(job,next);gearPlanEdit=GearPlanEdit.None;SaveGearPlan();}
        ImGui.EndDisabled();
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get("Gear.PlanDeleteTip"));
        if(gearPlanEdit==GearPlanEdit.None)return;
        ImGui.SetNextItemWidth(220);
        var confirmed=ImGui.InputText(localization.Get("Gear.PlanName")+"###GearPlanName",ref gearPlanName,GearPlanSets.MaxName,ImGuiInputTextFlags.EnterReturnsTrue);Tip("Gear.PlanNameTip");
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("Gear.PlanOk")) || confirmed)
        {
            var created=gearPlanEdit==GearPlanEdit.New;
            var done=created
                ? GearPlanSets.New(ref set,defaultName,gearPlanName,configuration.GearPlans?.GetValueOrDefault(job),configuration.GearMateria?.GetValueOrDefault(job))
                : set is not null && GearPlanSets.Rename(set,gearPlanName);
            gearPlanNameRefused=!done;
            if(done)
            {
                (configuration.GearPlanSets??=[])[job]=set!;
                gearPlanEdit=GearPlanEdit.None;
                SaveGearPlan();
            }
        }
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("Gear.PlanCancel")))gearPlanEdit=GearPlanEdit.None;
        if(gearPlanNameRefused)ImGui.TextColored(Accent,localization.Get("Gear.PlanRefused"));
    }
}
