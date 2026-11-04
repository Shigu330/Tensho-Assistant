using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gathering;

namespace TenshoAssistant;

/// <summary>
/// R-15 (0.417): while the game's spearfishing window is open (setting FishingPanel, as the fishing help), a panel beside it: for each
/// fish in sight, its size and speed and the GatherBuddy fish that match, those of the current zone first (accent), at most three
/// others. Speed names and values from GatherBuddy (to confirm in game: developer mode shows the raw values).
/// </summary>
internal sealed partial class MainWindow
{
    internal SpearfishingReader? Spearfishing { get; set; }

    internal void DrawSpearfishingPanel()
    {
        if(windowDisposed || !configuration.FishingPanel || Spearfishing?.Frame is not { } frame)return;
        Panels?.Show("TenshoSpearfishingPanel",localization.Get("Spearfishing.Title"),ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoMove|ImGuiWindowFlags.AlwaysAutoResize,()=>{ImGui.SetNextWindowPos(new Vector2(frame.X+frame.Width+6,frame.Y),ImGuiCond.Always);ImGui.SetNextWindowSize(new Vector2(340,0),ImGuiCond.Always);},()=>
        {
            gatherBuddyFish?.Request();
            var data=gatherBuddyFish?.State.Data;
            var catalog=gatheringCatalog.State.Catalog;var items=itemCatalog.State.Catalog;
            bool InZone(uint fish)=>catalog?.ForItem(fish).Any(source=>source.TerritoryId==frame.TerritoryId)==true;
            var shown=0;
            foreach(var slot in frame.Slots.Where(slot=>slot.Available))
            {
                shown++;
                if(shown>1)ImGui.Separator();
                var size=localization.Get(slot.Size switch{1=>"Spearfishing.Small",2=>"Spearfishing.Average",3=>"Spearfishing.Large",_=>"Spearfishing.UnknownSize"});
                var speed=Gathering.Spearfishing.SpeedName(slot.Speed) is { } name ? localization.Get("Spearfishing.Speed"+name):slot.Speed.ToString();
                ImGui.TextColored(Muted,localization.Format("Spearfishing.Slot",slot.Index+1,size,speed)+(slot.Large ? " · "+localization.Get("Spearfishing.GuaranteedLarge"):""));
                if(developerMode)ImGui.TextDisabled($"size {slot.Size} · speed {slot.Speed} · reverse {slot.Reverse}");
                if(data is null){ImGui.TextDisabled(localization.Get("Spearfishing.NoData"));continue;}
                var candidates=Gathering.Spearfishing.Candidates(data.Spearfish,slot,InZone);
                if(candidates.InZone.Count+candidates.Elsewhere.Count==0){ImGui.TextDisabled(localization.Get("Spearfishing.NoMatch"));continue;}
                foreach(var fish in candidates.InZone)ImGui.TextColored(Accent,items?.Find(fish)?.Name ?? $"ID {fish}");
                foreach(var fish in candidates.Elsewhere.Take(3))ImGui.TextDisabled(items?.Find(fish)?.Name ?? $"ID {fish}");
            }
            if(shown==0)ImGui.TextDisabled(localization.Get("Spearfishing.NoFish"));
            ImGui.TextDisabled(localization.Get("Spearfishing.Credit"));
        });
    }
}
