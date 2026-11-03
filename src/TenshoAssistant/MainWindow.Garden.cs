using Dalamud.Bindings.ImGui;
using TenshoAssistant.Reminders;

namespace TenshoAssistant;

// N-08 (0.448, DEC-026): the garden journal on the Reminders page. The player notes each bed or pot (place, seed, growing and tending
// hours they type: the game's sheets give neither); Tensho shows when it is ripe and when to tend it, and says it once in the chat
// (with the alarms' sound) when the reminders are on. Nothing is read from the game.
// 0.449 (DEC-027): picking a seed fills its growing and tending hours from the embedded Lotlab data (still editable).
internal sealed partial class MainWindow
{
    /// <summary>The embedded garden seeds with their names in the game's language (set by the plugin).</summary>
    internal Func<IReadOnlyList<(GardenSeed Seed,string Name)>>? GardenSeedCatalog { get; set; }
    private string newBedPlace="",newBedSeed="",gardenSeedSearch="";
    private int newBedGrow=1,newBedTend;
    private string? newBedError;
    private DateTimeOffset gardenObservedAt;

    /// <summary>Called every frame: once every 10 s, says the ripe beds and the beds to tend (each once), when the reminders are on.</summary>
    internal void ObserveGarden()
    {
        var now=DateTimeOffset.UtcNow;
        if(now-gardenObservedAt<TimeSpan.FromSeconds(10))return;
        gardenObservedAt=now;
        if(!configuration.GardenAlerts || configuration.GardenBeds is not { Count:>0 } beds)return;
        var due=GardenJournal.Due(beds,now);
        if(due.Count==0)return;
        foreach(var (bed,state) in due)
            ChatNotice?.Invoke(localization.Format(state==GardenState.Ripe ? "Garden.RipeNotice":"Garden.TendNotice",bed.Seed,bed.Place));
        if(configuration.AlarmOptions.PlaySound)Alarms.AlarmService.PlaySound(configuration.AlarmOptions.Sound,log);
        SaveReminders();
    }

    private void DrawGardenJournal()
    {
        var beds=configuration.GardenBeds ?? [];
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("Garden.Title",beds.Count)+"###GardenJournal"))return;
        ImGui.TextDisabled(localization.Get("Garden.Help"));
        BoolSetting("Garden.Alerts","Garden.AlertsTip",()=>configuration.GardenAlerts,value=>configuration.GardenAlerts=value,c=>c.GardenAlerts);
        var now=DateTimeOffset.UtcNow;GardenBed? removed=null;var changed=false;
        foreach(var bed in beds)
        {
            ImGui.PushID(bed.Id.ToString());
            try
            {
                var state=GardenJournal.State(bed,now);
                ImGui.TextColored(state switch{GardenState.Ripe=>Success,GardenState.ToTend=>UI.FfxivTheme.Warning,_=>Accent},bed.Seed);
                ImGui.SameLine();
                ImGui.TextUnformatted(bed.Place);
                ImGui.SameLine();
                var ripe=GardenJournal.RipeAt(bed).ToLocalTime().ToString("g");
                var tend=GardenJournal.TendAt(bed) is { } due ? " · "+localization.Format(state==GardenState.ToTend ? "Garden.TendNow":"Garden.TendAt",due.ToLocalTime().ToString("g")):"";
                ImGui.TextDisabled((state==GardenState.Ripe ? localization.Get("Garden.Ripe"):localization.Format("Garden.RipeAt",ripe))+tend);
                ImGui.SameLine();
                ImGui.BeginDisabled(GardenJournal.TendAt(bed) is null);
                try{if(ImGui.SmallButton(localization.Get("Garden.Tend"))){GardenJournal.Tend(bed,now);changed=true;}}
                finally{ImGui.EndDisabled();}
                if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get("Garden.TendTip"));
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("Garden.Replant"))){GardenJournal.Replant(bed,now);changed=true;}
                Tip("Garden.ReplantTip");
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("Garden.Remove")))removed=bed;
                Tip("Garden.RemoveTip");
            }
            finally{ImGui.PopID();}
        }
        if(removed is not null){configuration.GardenBeds=beds.Where(bed=>bed!=removed).ToList();changed=true;}
        if(changed)SaveReminders();
        DrawNewBed(beds);
        DrawGardenCrosses();
    }

    private void DrawNewBed(List<GardenBed> beds)
    {
        ImGui.SetNextItemWidth(160);
        ImGui.InputTextWithHint("##NewBedPlace",localization.Get("Garden.PlaceHint"),ref newBedPlace,GardenJournal.MaximumText);
        Tip("Garden.PlaceTip");
        ImGui.SameLine();
        DrawSeedPicker();
        ImGui.SameLine();
        ImGui.SetNextItemWidth(160);
        ImGui.InputTextWithHint("##NewBedSeed",localization.Get("Garden.SeedHint"),ref newBedSeed,GardenJournal.MaximumText);
        Tip("Garden.SeedTip");
        ImGui.SetNextItemWidth(110);
        if(ImGui.InputInt(localization.Get("Garden.Grow")+"##NewBedGrow",ref newBedGrow))newBedGrow=GardenJournal.ClampGrow(newBedGrow);
        Tip("Garden.GrowTip");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(110);
        if(ImGui.InputInt(localization.Get("Garden.TendHours")+"##NewBedTend",ref newBedTend))newBedTend=GardenJournal.ClampTend(newBedTend);
        Tip("Garden.TendHoursTip");
        ImGui.SameLine();
        ImGui.BeginDisabled(beds.Count>=GardenJournal.Capacity);
        try
        {
            if(ImGui.Button(localization.Get("Garden.Plant")))
            {
                if(GardenJournal.CleanText(newBedPlace) is not { } place || GardenJournal.CleanText(newBedSeed) is not { } seed)newBedError=localization.Get("Garden.Invalid");
                else
                {
                    configuration.GardenBeds=[..beds,GardenJournal.Plant(place,seed,newBedGrow,newBedTend,DateTimeOffset.UtcNow)];
                    newBedSeed="";newBedError=null;SaveReminders();
                }
            }
        }
        finally{ImGui.EndDisabled();}
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get(beds.Count>=GardenJournal.Capacity ? "Garden.Full":"Garden.PlantTip"));
        if(newBedError is { } error)ImGui.TextColored(UI.FfxivTheme.Error,error);
    }

    private void DrawSeedPicker()
    {
        var seeds=GardenSeedCatalog?.Invoke() ?? [];
        ImGui.BeginDisabled(seeds.Count==0);
        try
        {
            ImGui.SetNextItemWidth(160);
            var open=ImGui.BeginCombo("##NewBedSeedPick",localization.Get("Garden.SeedPick"));
            if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get(seeds.Count==0 ? "Garden.SeedPickNone":"Garden.SeedPickTip"));
            if(open)
            {
                try
                {
                    ImGui.SetNextItemWidth(-1);
                    ImGui.InputTextWithHint("##GardenSeedSearch",localization.Get("Garden.SeedSearch"),ref gardenSeedSearch,40);
                    Tip("Garden.SeedSearchTip");
                    foreach(var (seed,name) in seeds)
                    {
                        if(gardenSeedSearch.Length>0 && !name.Contains(gardenSeedSearch,StringComparison.CurrentCultureIgnoreCase))continue;
                        if(!ImGui.Selectable(name+"##GardenSeed"+seed.SeedId))continue;
                        newBedSeed=GardenSeeds.JournalName(name) ?? newBedSeed;
                        newBedGrow=GardenSeeds.GrowHours(seed);
                        newBedTend=GardenSeeds.TendHours(seed);
                    }
                }
                finally{ImGui.EndCombo();}
            }
        }
        finally{ImGui.EndDisabled();}
    }
}
