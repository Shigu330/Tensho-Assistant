using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gathering;
using TenshoAssistant.Items;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private uint gpJob=16;
    private int gpLevel=100,gpBudget=600,gpAttempts=4,gpBaseYield=1,gpSuccess=100,gpBoon=15,gpBountiful=3;
    private bool gpYieldActive,gpGift1Active,gpGift2Active,gpTidingsActive,gpOtherEffects;
    private int gpFirstAttemptBonus;
    private ulong gpCopiedOwner;
    private DateTimeOffset? gpCopiedAt;
    private GpOptimization? gpResult;
    private string? gpError;
    private void ValidateGpCopy()
    {
        if(gpCopiedOwner==0 || gpCopiedOwner==characterStats.State.CharacterId) return;
        gpCopiedOwner=0; gpCopiedAt=null; gpBudget=0; gpLevel=1; gpResult=null;
        gpError=localization.Get("GpUi.ChangedCharacter");
    }
    private void DrawGatheringGp()
    {
        Heading(localization.Get("GpUi.Title"),localization.Get("GpUi.Subtitle"));
        DrawGatheringObservation();
        if(gpActions.State.Catalog is not { } catalog)
        { ImGui.TextWrapped(gpActions.State.Error ?? localization.Get("GpUi.Loading")); return; }
        ImGui.TextWrapped(localization.Get("GpUi.Help"));
        var stats=characterStats.State;
        var canCopy=stats.CharacterId!=0 && stats.JobId is 16 or 17 && stats.CurrentGp is not null
            && TenshoAssistant.Caching.CacheAge.Fresh(stats.ObservedAt,DateTimeOffset.UtcNow,TimeSpan.FromSeconds(5));
        ImGui.BeginDisabled(!canCopy);
        try
        {
            if(ImGui.SmallButton(localization.Get("GpUi.Copy")))
            {
                gpJob=stats.JobId; gpLevel=stats.Level; gpBudget=(int)stats.CurrentGp!.Value;
                gpCopiedOwner=stats.CharacterId; gpCopiedAt=stats.ObservedAt; gpResult=null; gpError=null;
            }
        }
        finally { ImGui.EndDisabled(); }
        if(canCopy) { ImGui.SameLine(); ImGui.TextDisabled(localization.Format("GpUi.Observed",stats.Job,stats.CurrentGp!,stats.MaximumGp!)); }
        if(gpCopiedAt is { } date) ImGui.TextDisabled(localization.Format("GpUi.Copied",date.ToLocalTime().ToString("T")));
        var changed=false; var resourceChanged=false;
        if(ImGui.RadioButton(localization.Get("GpUi.Miner"),gpJob==16)) { gpJob=16; changed=resourceChanged=true; }
        ImGui.SameLine(); if(ImGui.RadioButton(localization.Get("GpUi.Botanist"),gpJob==17)) { gpJob=17; changed=resourceChanged=true; }
        if(ImGui.CollapsingHeader(localization.Get("GpUi.Scenario"),ImGuiTreeNodeFlags.DefaultOpen))
        {
            if(ImGui.BeginTable("GpInputs",ImGui.GetContentRegionAvail().X<760 ? 1:2,ImGuiTableFlags.SizingStretchSame))
            {
                try
                {
                    ImGui.TableNextRow(); ImGui.TableNextColumn(); ImGui.SetNextItemWidth(110);
                    resourceChanged|=ImGui.InputInt(localization.Get("GpUi.Level"),ref gpLevel,0);
                    ImGui.TableNextColumn(); ImGui.SetNextItemWidth(110); resourceChanged|=ImGui.InputInt(localization.Get("GpUi.Budget"),ref gpBudget,0);
                    ImGui.TableNextRow(); ImGui.TableNextColumn(); ImGui.SetNextItemWidth(110);
                    changed|=ImGui.InputInt(localization.Get("GpUi.Attempts"),ref gpAttempts,0);
                    ImGui.TableNextColumn(); ImGui.SetNextItemWidth(110); changed|=ImGui.InputInt(localization.Get("GpUi.Yield"),ref gpBaseYield,0);
                    if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("GpUi.YieldHelp"));
                    ImGui.TableNextRow(); ImGui.TableNextColumn(); ImGui.SetNextItemWidth(110);
                    changed|=ImGui.InputInt(localization.Get("GpUi.Success"),ref gpSuccess,0);
                    ImGui.TableNextColumn(); ImGui.SetNextItemWidth(110); changed|=ImGui.InputInt(localization.Get("GpUi.Boon"),ref gpBoon,0);
                    ImGui.TableNextRow(); ImGui.TableNextColumn(); ImGui.SetNextItemWidth(110);
                    changed|=ImGui.InputInt(localization.Get("GpUi.Bountiful"),ref gpBountiful,0);
                    if(ImGui.IsItemHovered()) WrappedTooltip(localization.Get("GpUi.BountifulHelp"));
                    ImGui.TableNextColumn();
                    if(DrawBountifulEstimate() is { } estimate && estimate!=gpBountiful){gpBountiful=estimate;changed=true;}
                }
                finally { ImGui.EndTable(); }
            }
        }
        if(ImGui.CollapsingHeader(localization.Get("GpUi.Effects")))
        {
            ImGui.TextWrapped(localization.Get("GpUi.EffectsHelp"));
            changed|=DrawGpDeclaredEffect(catalog,GpEffect.Yield,localization.Get("GpUi.YieldActive"),ref gpYieldActive);
            changed|=DrawGpDeclaredEffect(catalog,GpEffect.BoonChance1,localization.Get("GpUi.Gift1"),ref gpGift1Active);
            changed|=DrawGpDeclaredEffect(catalog,GpEffect.BoonChance2,localization.Get("GpUi.Gift2"),ref gpGift2Active);
            changed|=DrawGpDeclaredEffect(catalog,GpEffect.BoonYield,localization.Get("GpUi.Tidings"),ref gpTidingsActive);
            ImGui.SetNextItemWidth(110);changed|=ImGui.InputInt(localization.Get("GpUi.FirstBonus"),ref gpFirstAttemptBonus,0);
            ImGui.TextWrapped(localization.Get("GpUi.FirstHelp"));
            changed|=ImGui.Checkbox(localization.Get("GpUi.OtherEffect"),ref gpOtherEffects);
        }
        if(resourceChanged) { gpCopiedOwner=0; gpCopiedAt=null; }
        if(changed || resourceChanged) { gpResult=null; gpError=null; }
        if(ImGui.Button(localization.Get("GpUi.Compare")))
        {
            try { gpResult=GpOptimizer.Solve(new(gpJob,gpLevel,gpBudget,gpAttempts,gpBaseYield,gpSuccess,gpBoon,gpBountiful,new(gpYieldActive,gpGift1Active,gpGift2Active,gpTidingsActive,gpFirstAttemptBonus,gpOtherEffects)),catalog); gpError=null; }
            catch(ArgumentException) { gpResult=null; gpError=gpOtherEffects ? localization.Get("GpUi.Unsupported"):localization.Get("GpUi.Invalid"); }
        }
        if(gpError is { } error) ImGui.TextWrapped(error);
        if(gpResult is not { } result) return;
        var best=result.Best;
        ImGui.Separator();
        ImGui.TextColored(Accent,localization.Format("GpUi.Best",best.ExpectedYield.ToString("0.00"),best.Cost));
        ImGui.TextDisabled(localization.Format("GpUi.Baseline",result.Baseline.ToString("0.00"),(best.ExpectedYield-result.Baseline).ToString("0.00"),result.Scenario.Budget-best.Cost));
        if(result.Scenario.Effects.FirstAttemptBonus>0)ImGui.TextWrapped(localization.Format("GpUi.Paid",result.Scenario.Effects.FirstAttemptBonus));
        ImGui.TextWrapped(localization.Get("GpUi.Expectation"));
        if(best.Actions.Count==0) ImGui.TextUnformatted(localization.Get("GpUi.Keep"));
        foreach(var use in best.Actions)
        {
            DrawItemIcon(new ItemDefinition(0,use.Action.Name,false,use.Action.Icon),new Vector2(28)); ImGui.SameLine();
            var repeat=use.Action.Spec.Effect is GpEffect.Bountiful1 or GpEffect.Bountiful2;
            ImGui.TextWrapped(localization.Format("GpUi.Use",use.Action.Name,use.Count,use.Action.Spec.Cost*use.Count) + (repeat ? result.Scenario.Effects.FirstAttemptBonus>0 ? localization.Get("GpUi.AfterFirst"):localization.Get("GpUi.BeforeEach") : localization.Get("GpUi.BeforeAttempts")));
            if(ImGui.IsItemHovered()) WrappedTooltip(use.Action.Description);
        }
        if(result.NextUpgrade is { } next)
            ImGui.TextColored(Accent,localization.Format("GpUi.Next",next.Cost,next.Cost-result.Scenario.Budget,next.ExpectedYield.ToString("0.00")));
        if(ImGui.CollapsingHeader(localization.Get("GpUi.Thresholds")))
        {
            if(ImGui.BeginTable("GpFrontier",3,ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                try
                {
                    ImGui.TableSetupColumn(localization.Get("GpUi.Required")); ImGui.TableSetupColumn(localization.Get("GpUi.Average")); ImGui.TableSetupColumn(localization.Get("GpUi.Gain")); ImGui.TableHeadersRow();
                    foreach(var plan in result.Frontier)
                    {
                        ImGui.TableNextRow(); ImGui.TableNextColumn(); ImGui.TextUnformatted(plan.Cost.ToString());
                        ImGui.TableNextColumn(); ImGui.TextUnformatted($"{plan.ExpectedYield:0.00}");
                        ImGui.TableNextColumn(); ImGui.TextUnformatted($"{plan.ExpectedYield-result.Baseline:0.00}");
                    }
                }
                finally { ImGui.EndTable(); }
            }
        }
    }
    private bool DrawGpDeclaredEffect(GpActionCatalog catalog,GpEffect effect,string label,ref bool active)
    {
        var metadata=catalog.Actions.First(action=>action.Spec.JobId==gpJob && action.Spec.Effect==effect);
        DrawItemIcon(new ItemDefinition(0,metadata.Name,false,metadata.Icon),new Vector2(24));
        if(ImGui.IsItemHovered())WrappedTooltip(metadata.Description);
        ImGui.SameLine();return ImGui.Checkbox(label,ref active);
    }

    // Bountiful II estimate (0.224): gathering rating of the current gathering job against the context item's base value; « Use » fills
    // the field, which stays editable. Community rule (docs/evidence/0.224-bountiful-base.md).
    private Func<uint,int?>? bountifulBase;
    internal void ConfigureBountifulBase(Func<uint,int?> baseOf)=>bountifulBase=baseOf;
    private int? DrawBountifulEstimate()
    {
        var stats=characterStats.State;
        var baseValue=itemId>0 ? bountifulBase?.Invoke((uint)itemId):null;
        var rating=stats.JobId is 16 or 17 && stats.Values.TryGetValue("Gathering",out var gathering) ? gathering:0;
        if(baseValue is not { } valueBase || rating<=0 || BountifulEstimate.Bonus(rating,valueBase) is not { } bonus)
        {
            ImGui.TextDisabled(localization.Get("GpUi.BountifulEstimateNone"));
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("GpUi.BountifulEstimateNoneHelp"));
            return null;
        }
        ImGui.TextColored(Accent,localization.Format("GpUi.BountifulEstimate",bonus));
        var next=BountifulEstimate.NextStep(rating,valueBase);
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("GpUi.BountifulEstimateHelp",rating,valueBase,(int)Math.Floor(valueBase*0.9),(int)Math.Floor(valueBase*1.1))
            +(next is { } step ? "\n"+localization.Format("GpUi.BountifulNext",step-rating):""));
        ImGui.SameLine();
        return ImGui.SmallButton(localization.Get("GpUi.BountifulUse")+"##BountifulUse") ? bonus:null;
    }
}
