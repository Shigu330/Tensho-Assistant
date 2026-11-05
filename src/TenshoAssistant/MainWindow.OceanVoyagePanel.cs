using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gathering;

namespace TenshoAssistant;

/// <summary>
/// R-07 (0.418, user's choice in GAPS: all at once): during an ocean fishing voyage (setting FishingPanel, as the fishing help), a
/// panel with the stop (place, moment, spectral current), its fish with the GatherBuddy bait and bite, the next stop, the three
/// missions (progress / target) and the points. Game data and GatherBuddy (Apache-2.0); fish not documented by GatherBuddy show « ? ».
/// </summary>
internal sealed partial class MainWindow
{
    internal OceanVoyageReader? OceanVoyage { get; set; }
    internal Func<OceanVoyageData?>? OceanVoyageData { get; set; }

    internal void DrawOceanVoyagePanel()
    {
        if(windowDisposed || !configuration.FishingPanel || OceanVoyage?.Frame is not { } frame || frame.Status==4 || OceanVoyageData?.Invoke() is not { } data)return;
        Panels?.Show("TenshoOceanVoyage",localization.Get("OceanVoyage.Title"),ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.AlwaysAutoResize,()=>{ImGui.SetNextWindowPos(new Vector2(60,300),ImGuiCond.FirstUseEver);},()=>
        {
            if(OceanVoyages.Current(data,frame) is not { } stop){ImGui.TextDisabled(localization.Get("OceanVoyage.Unknown"));return;}
            ImGui.TextColored(Accent,localization.Format("OceanVoyage.Stop",stop.Index+1,stop.Spot.Place,localization.Get("OceanFishing.Time"+stop.Time)));
            if(stop.Spectral)ImGui.TextColored(UI.FfxivTheme.Success,localization.Get("OceanVoyage.Spectral"));
            if(stop.NextSpot is { } next)ImGui.TextDisabled(localization.Format("OceanVoyage.Next",next.Place,localization.Get("OceanFishing.Time"+stop.NextTime)));
            gatherBuddyFish?.Request();
            var guide=gatherBuddyFish?.State.Data;var items=itemCatalog.State.Catalog;
            if(stop.Fish.Count>0 && ImGui.BeginTable("OceanVoyageFish",3,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingFixedFit))
            {
                try
                {
                    ImGui.TableSetupColumn(localization.Get("OceanVoyage.Fish"));ImGui.TableSetupColumn(localization.Get("OceanVoyage.Bait"));ImGui.TableSetupColumn(localization.Get("OceanVoyage.Bite"));
                    ImGui.TableHeadersRow();
                    foreach(var fish in stop.Fish)
                    {
                        ImGui.TableNextRow();ImGui.TableNextColumn();
                        ImGui.TextUnformatted(items?.Find(fish)?.Name ?? $"ID {fish}");
                        ImGui.TableNextColumn();
                        var route=guide?.Route(fish) ?? [];
                        ImGui.TextUnformatted(route.Count>0 ? string.Join(" › ",route.Select(step=>items?.Find(step)?.Name ?? $"ID {step}")):"?");
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(guide?.Find(fish)?.Bite switch{"Weak"=>"!","Strong"=>"!!","Legendary"=>"!!!",_=>"?"});
                    }
                }
                finally{ImGui.EndTable();}
            }
            var missions=OceanVoyages.Missions(data,frame);
            if(missions.Count>0)
            {
                ImGui.Separator();
                foreach(var (text,progress,target) in missions)
                    ImGui.TextColored(progress>=target && target>0 ? UI.FfxivTheme.Success:Muted,localization.Format("OceanVoyage.Mission",text,progress,target));
            }
            ImGui.Separator();
            ImGui.TextUnformatted(localization.Format("OceanVoyage.Points",frame.Points.ToString("N0"),frame.Caught));
            ImGui.TextDisabled(localization.Get("OceanVoyage.Credit"));
        });
    }
}
