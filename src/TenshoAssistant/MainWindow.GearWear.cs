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
        // X-76 H4 (0.914): written once, when the slider is released (it was written on every frame of a drag).
        SliderSetting("GearWear.RepairBelow","GearWear.RepairBelowTip",()=>WearSettings.Repair,value=>WearSettings.RepairBelow=value,1,99,defaults.Repair,"%d %%");
        SliderSetting("GearWear.SpiritbondAt","GearWear.SpiritbondAtTip",()=>WearSettings.Spiritbond,value=>WearSettings.SpiritbondAt=value,50,100,defaults.Spiritbond,"%d %%");
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
                if(piece.Condition is { } condition)ImGui.TextColored(condition<settings.Repair ? UI.FfxivTheme.Error:Success,TenshoAssistant.Localization.TenshoCulture.Percent(condition));
                else ImGui.TextDisabled("—");
                ImGui.TableNextColumn();
                if(GearWear.SpiritbondPercent(piece.Spiritbond) is { } percent)
                {
                    // X-191 (0.1177): the whole percent as the game shows it; the exact value in the tip.
                    ImGui.TextColored(percent>=settings.Spiritbond ? Accent:Muted,TenshoAssistant.Localization.TenshoCulture.Percent(GearWear.ShownSpiritbond(percent)));
                    TipText(TenshoAssistant.Localization.TenshoCulture.Percent(percent,"0.00"));
                }
                else ImGui.TextDisabled("—");
            }
        }
        finally{ImGui.EndTable();}
    }

    // The on-screen wear bar (a PanelWindow since 0.514), only when it is on and the worn gear is known.
    private GearWearState wearBarState=new(0,0,null,null);
    private PanelDefinition WearPanel=>new("WearBar","###TenshoWearBar",new Vector2(40,200),
        ImGuiWindowFlags.NoTitleBar|ImGuiWindowFlags.AlwaysAutoResize|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoNav|ImGuiWindowFlags.NoScrollbar,
        ()=>configuration.GearWear is { Bar:true },WearBarVisible,DrawWearBarContents,Alpha:0.6f);

    private bool WearBarVisible()
    {
        if(windowDisposed || configuration.GearWear is not { Bar:true } settings)return false;
        wearBarState=GearWear.Evaluate(EquippedGear?.Invoke() ?? [],settings);
        return wearBarState.LowestCondition is not null || wearBarState.HighestSpiritbond is not null;
    }

    private void DrawWearBarContents()
    {
        var state=wearBarState;
        if(state.LowestCondition is { } condition)
        {
            ImGui.TextColored(state.ToRepair>0 ? UI.FfxivTheme.Error:Success,localization.Format("GearWear.BarCondition",condition));
            Tip("GearWear.BarConditionTip");
        }
        if(state.HighestSpiritbond is { } bond)
        {
            if(state.LowestCondition is not null)ImGui.SameLine();
            ImGui.TextColored(state.ToExtract>0 ? Accent:Muted,localization.Format("GearWear.BarSpiritbond",GearWear.ShownSpiritbond(bond)));
            Tip("GearWear.BarSpiritbondTip");
        }
    }
}
