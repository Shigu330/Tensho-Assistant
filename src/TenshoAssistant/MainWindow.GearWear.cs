using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gear;

namespace TenshoAssistant;

// g, M-06 (0.376): wear and spiritbond of the worn gear on the Gear page (per piece), the alert and bar settings with their thresholds,
// and the optional on-screen bar (off by default) with the lowest condition and the highest spiritbond.
internal sealed partial class MainWindow
{
    private GearWearSettings WearSettings=>configuration.GearWear??=new();

    private void DrawGearWear(GearCatalog catalog)
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("GearWear.Title")+"###GearWear"))return;
        ImGui.TextDisabled(localization.Get("GearWear.Help"));
        var settings=WearSettings;var defaults=new GearWearSettings();
        BoolSetting("GearWear.Alert","GearWear.AlertTip",()=>WearSettings.Alert,value=>WearSettings.Alert=value,c=>(c.GearWear ?? new()).Alert);
        BoolSetting("GearWear.Bar","GearWear.BarTip",()=>WearSettings.Bar,value=>WearSettings.Bar=value,c=>(c.GearWear ?? new()).Bar);
        var repair=settings.Repair;ImGui.SetNextItemWidth(160);
        if(ImGui.SliderInt(localization.Get("GearWear.RepairBelow")+"##RepairBelow",ref repair,1,99,"%d %%")){settings.RepairBelow=repair;SaveGearWear();}
        Tip("GearWear.RepairBelowTip");
        if(DefaultButton("RepairBelow",settings.Repair!=defaults.Repair)){settings.RepairBelow=defaults.RepairBelow;SaveGearWear();}
        var bond=settings.Spiritbond;ImGui.SetNextItemWidth(160);
        if(ImGui.SliderInt(localization.Get("GearWear.SpiritbondAt")+"##SpiritbondAt",ref bond,50,100,"%d %%")){settings.SpiritbondAt=bond;SaveGearWear();}
        Tip("GearWear.SpiritbondAtTip");
        if(DefaultButton("SpiritbondAt",settings.Spiritbond!=defaults.Spiritbond)){settings.SpiritbondAt=defaults.SpiritbondAt;SaveGearWear();}
        var pieces=EquippedGear?.Invoke() ?? [];
        if(pieces.Count==0){ImGui.TextDisabled(localization.Get("GearWear.Unknown"));return;}
        if(!ImGui.BeginTable("GearWearTable",3,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH))return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("GearWear.ColumnPiece"));
            ImGui.TableSetupColumn(localization.Get("GearWear.ColumnCondition"));
            ImGui.TableSetupColumn(localization.Get("GearWear.ColumnSpiritbond"));
            ImGui.TableHeadersRow();
            foreach(var piece in pieces)
            {
                ImGui.TableNextRow();ImGui.TableNextColumn();
                ImGui.TextUnformatted(catalog.Find(piece.ItemId)?.Name ?? $"#{piece.ItemId}");
                ImGui.TableNextColumn();
                if(piece.Condition is { } condition)ImGui.TextColored(condition<settings.Repair ? UI.FfxivTheme.Error:Success,$"{condition} %");
                else ImGui.TextDisabled("—");
                ImGui.TableNextColumn();
                if(GearWear.SpiritbondPercent(piece.Spiritbond) is { } percent)ImGui.TextColored(percent>=settings.Spiritbond ? Accent:Muted,$"{percent:0.0} %");
                else ImGui.TextDisabled("—");
            }
        }
        finally{ImGui.EndTable();}
    }

    private void SaveGearWear()
    {
        try{save();saveError=null;}
        catch(Exception error){saveError=localization.Get("Common.SaveError");log.Warning(error,"Gear wear settings could not be saved.");}
    }

    // Drawn by Plugin.Draw: the on-screen wear bar, only when it is on and the worn gear is known.
    internal void DrawWearBar()
    {
        if(windowDisposed || configuration.GearWear is not { Bar:true } settings)return;
        var state=GearWear.Evaluate(EquippedGear?.Invoke() ?? [],settings);
        if(state.LowestCondition is null && state.HighestSpiritbond is null)return;
        UI.FfxivTheme.Push();
        try
        {
            ImGui.SetNextWindowPos(new Vector2(40,200),ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowBgAlpha(0.6f);
            if(ImGui.Begin("###TenshoWearBar",ImGuiWindowFlags.NoTitleBar|ImGuiWindowFlags.AlwaysAutoResize|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoNav
                   |ImGuiWindowFlags.NoScrollbar))
            {
                if(state.LowestCondition is { } condition)
                {
                    ImGui.TextColored(state.ToRepair>0 ? UI.FfxivTheme.Error:Success,localization.Format("GearWear.BarCondition",condition));
                    if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("GearWear.BarConditionTip"));
                }
                if(state.HighestSpiritbond is { } bond)
                {
                    if(state.LowestCondition is not null)ImGui.SameLine();
                    ImGui.TextColored(state.ToExtract>0 ? Accent:Muted,localization.Format("GearWear.BarSpiritbond",bond.ToString("0")));
                    if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("GearWear.BarSpiritbondTip"));
                }
            }
            ImGui.End();
        }
        finally{UI.FfxivTheme.Pop();}
    }
}
