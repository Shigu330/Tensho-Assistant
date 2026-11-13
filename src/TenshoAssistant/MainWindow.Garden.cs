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

    /// <summary>
    /// 0.460 (user request, in-game campaign): a planting read from the chat (Reminders/GardenChatWatcher): noted in the journal with the seed's hours. An unknown seed is
    /// never guessed: the player is told to note it.
    /// </summary>
    internal void GardenPlanted(GardenChatEvent found,uint? seedId,string? zone)
    {
        if(found.Kind is GardenChatKind.Tended or GardenChatKind.Uprooted or GardenChatKind.Harvested){GardenCared(found,zone);return;}
        var seeds=GardenSeedCatalog?.Invoke() ?? [];
        var known=seedId is { } id ? seeds.FirstOrDefault(row=>row.Seed.SeedId==id):default;
        if(known.Seed is null){ChatNotice?.Invoke(localization.Get("Garden.DetectedUnknown"));return;}
        var at=zone ?? localization.Get("Garden.DetectedHere");
        var place=found.Kind==GardenChatKind.PlantedBed ? localization.Format("Garden.PlaceBed",at,found.Patch ?? 0,found.Bed ?? 0):localization.Format("Garden.PlacePot",at);
        var beds=GardenJournal.Detected(configuration.GardenBeds,place,known.Name,GardenSeeds.GrowHours(known.Seed),GardenSeeds.TendHours(known.Seed),DateTimeOffset.UtcNow);
        if(beds is null){ChatNotice?.Invoke(localization.Get("Garden.Full"));return;}
        configuration.GardenBeds=beds;
        ChatNotice?.Invoke(localization.Format("Garden.DetectedNotice",known.Name,place));
        SaveReminders();
    }

    /// <summary>
    /// 0.616: tending or uprooting read from the chat. The line does not say which bed: with one bed noted here it is that one, with several nothing is guessed.
    /// X-179 (0.1165): the bed named by the game's menu just before (found.Patch, found.Bed) is that bed alone; not noted: nothing changes.
    /// </summary>
    private void GardenCared(GardenChatEvent found,string? zone)
    {
        var kind=found.Kind;
        var menu=found.Patch is { } patch && found.Bed is { } number ? (patch,number):((int,int)?)null;
        var here=GardenJournal.CareTargets(configuration.GardenBeds,zone,menu);
        if(here.Length==0)return;
        // #36 (0.978): a harvest is of a ripe bed: the only one ripe here is that one; none ripe (0.997, review): nothing is removed, the player is told.
        if(kind==GardenChatKind.Harvested)
        {
            var ripe=here.Where(row=>GardenJournal.RipeAt(row)<=DateTimeOffset.UtcNow).ToArray();
            if(ripe.Length==0){ChatNotice?.Invoke(localization.Get("Garden.HarvestNoneRipe"));return;}
            here=ripe;
        }
        if(here.Length>1){ChatNotice?.Invoke(localization.Get("Garden.CareSeveral"));return;}
        var bed=here[0];
        if(kind==GardenChatKind.Tended)GardenJournal.Tend(bed,DateTimeOffset.UtcNow);
        else if(kind==GardenChatKind.Harvested)
        {
            configuration.GardenBeds=(configuration.GardenBeds ?? []).Where(row=>row.Id!=bed.Id).ToList();
            ChatNotice?.Invoke(localization.Format("Garden.HarvestedNotice",bed.Seed,bed.Place));
        }
        else
        {
            configuration.GardenBeds=(configuration.GardenBeds ?? []).Where(row=>row.Id!=bed.Id).ToList();
            ChatNotice?.Invoke(localization.Format("Garden.UprootedNotice",bed.Seed,bed.Place));
        }
        SaveReminders();
    }

    // 0.459 (user request, in-game campaign): the garden has its own page, out of Reminders.
    private void DrawGardenPage()
    {
        Heading(localization.Get("Garden.PageTitle"),localization.Get("Garden.PageDescription"));
        // X-132 (0.1160, user request of 11/11): the garden's alarms in their own tab, their number beside « Garden » in the tools' menu.
        DetailTabs("GardenTabs",
        [
            ("Garden",localization.Get("Garden.TabGarden"),()=>{DrawGardenJournal();DrawGardenCrosses();}),
            ("Alarms",localization.Format("Garden.TabAlarms",GardenAlarmCount),()=>DrawGardenAlarms()),
        ],ref gardenTabFocus);
    }

    private string? gardenTabFocus;

    /// <summary>X-132 (0.1160): the garden's alarms still to ring while the garden reminders are on (0 when they are off).</summary>
    private int GardenAlarmCount=>configuration.GardenAlerts ? GardenJournal.Alarms(configuration.GardenBeds,DateTimeOffset.UtcNow).Count:0;

    /// <summary>X-132 (0.1160): the « Alarms » tab: each coming tending and harvest of the journal's beds, with its time.</summary>
    private void DrawGardenAlarms()
    {
        ImGui.TextWrapped(localization.Get("Garden.AlarmsHelp"));
        if(!configuration.GardenAlerts){ImGui.TextColored(Warning,localization.Get("Garden.AlarmsOff"));return;}
        var now=DateTimeOffset.UtcNow;
        var alarms=GardenJournal.Alarms(configuration.GardenBeds,now);
        if(alarms.Count==0){EmptyState("Garden.AlarmsEmpty");return;}
        if(!ImGui.BeginTable("GardenAlarms",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerH|ImGuiTableFlags.SizingStretchProp))return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("Garden.AlarmsWhen"));
            ImGui.TableSetupColumn(localization.Get("Garden.AlarmsWhat"));
            ImGui.TableSetupColumn(localization.Get("Garden.AlarmsSeed"));
            ImGui.TableSetupColumn(localization.Get("Garden.AlarmsPlace"));
            ImGui.TableHeadersRow();
            foreach(var (bed,kind,at) in alarms)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(InText(at,now));
                ImGui.TableNextColumn();
                ImGui.TextColored(kind==GardenState.Ripe ? Success:Accent,localization.Get(kind==GardenState.Ripe ? "Garden.AlarmRipe":"Garden.AlarmTend"));
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(bed.Seed);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(bed.Place);
            }
        }
        finally{ImGui.EndTable();}
    }

    private void DrawGardenJournal()
    {
        var beds=configuration.GardenBeds ?? [];
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("Garden.Title",beds.Count)+"###GardenJournal"))return;
        ImGui.TextDisabled(localization.Get("Garden.Help"));
        BoolSetting("Garden.Alerts","Garden.AlertsTip",()=>configuration.GardenAlerts,value=>configuration.GardenAlerts=value,c=>c.GardenAlerts);
        BoolSetting("Garden.Detect","Garden.DetectTip",()=>configuration.GardenDetect,value=>configuration.GardenDetect=value,c=>c.GardenDetect);
        var now=DateTimeOffset.UtcNow;GardenBed? removed=null;var changed=false;
        // 0.461 (user request, in-game campaign): each patch as a grid; the rest (pots, places typed by hand) stays a list.
        var forecastShown=false;
        foreach(var (zone,patch,planted) in GardenJournal.Grids(beds))
        {
            TenshoAssistant.UI.UiComponents.SubTitle(localization.Format("Garden.GridTitle",zone,patch));
            // X-179 (0.1165): the whole patch tended at once, when the game's menu was not read (several beds, one tending line each).
            if(planted.Any(entry=>GardenJournal.TendAt(entry.Bed) is not null))
            {
                ImGui.SameLine();
                ImGui.PushID($"TendPatch{zone}{patch}");
                DesktopEditBegin();
                try{if(ImGui.SmallButton(localization.Get("Garden.TendPatch"))){foreach(var (_,bed) in planted)if(GardenJournal.TendAt(bed) is not null)GardenJournal.Tend(bed,now);changed=true;}}
                finally{DesktopEditEnd();ImGui.PopID();}
                Tip("Garden.TendPatchTip");
            }
            // X-130 (0.1109): what each planted bed may give, by the wiki's rule and the community pairs (null until the pairs are read).
            var forecasts=GardenGridForecasts(planted);
            forecastShown|=forecasts is not null;
            if(!ImGui.BeginTable($"GardenGrid{zone}{patch}",3,ImGuiTableFlags.Borders|ImGuiTableFlags.SizingStretchSame))continue;
            try
            {
                for(var row=0;row<3;row++)
                {
                    ImGui.TableNextRow();
                    for(var column=0;column<3;column++)
                    {
                        ImGui.TableNextColumn();
                        if(GardenCrosses.BedAt(row,column) is not { } number){ImGui.TextDisabled("—");continue;}
                        var (_,bed)=planted.FirstOrDefault(entry=>entry.Number==number);
                        if(bed is null){ImGui.TextDisabled(localization.Format("Garden.GridEmpty",number));continue;}
                        ImGui.PushID(bed.Id.ToString());
                        try
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text,GardenColor(GardenJournal.State(bed,now)));
                            try{ImGui.TextWrapped($"{number}. {bed.Seed}");}
                            finally{ImGui.PopStyleColor();}
                            ImGui.TextDisabled(GardenTimes(bed,now));
                            DrawBedButtons(bed,now,ref changed,ref removed);
                            if(forecasts?.FirstOrDefault(forecast=>forecast.Bed==number) is { } forecast)DrawGardenForecast(forecast,planted);
                        }
                        finally{ImGui.PopID();}
                    }
                }
            }
            finally{ImGui.EndTable();}
        }
        if(forecastShown)ImGui.TextDisabled(localization.Get("Garden.ForecastSource"));
        foreach(var bed in beds.Where(bed=>GardenJournal.Slot(bed) is null))
        {
            ImGui.PushID(bed.Id.ToString());
            try
            {
                ImGui.TextColored(GardenColor(GardenJournal.State(bed,now)),bed.Seed);
                ImGui.SameLine();
                ImGui.TextUnformatted(bed.Place);
                ImGui.SameLine();
                ImGui.TextDisabled(GardenTimes(bed,now));
                ImGui.SameLine();
                DrawBedButtons(bed,now,ref changed,ref removed);
            }
            finally{ImGui.PopID();}
        }
        if(removed is not null){configuration.GardenBeds=beds.Where(bed=>bed!=removed).ToList();changed=true;}
        if(changed)SaveReminders();
        DrawNewBed(beds);
    }

    private static System.Numerics.Vector4 GardenColor(GardenState state)=>state switch{GardenState.Ripe=>Success,GardenState.ToTend=>UI.FfxivTheme.Warning,_=>Accent};

    private string GardenTimes(GardenBed bed,DateTimeOffset now)
    {
        var state=GardenJournal.State(bed,now);
        var ripe=GardenJournal.RipeAt(bed).ToLocalTime().ToString("g");
        var tend=GardenJournal.TendAt(bed) is { } due ? " · "+localization.Format(state==GardenState.ToTend ? "Garden.TendNow":"Garden.TendAt",due.ToLocalTime().ToString("g")):"";
        return (state==GardenState.Ripe ? localization.Get("Garden.Ripe"):localization.Format("Garden.RipeAt",ripe))+tend;
    }

    private void DrawBedButtons(GardenBed bed,DateTimeOffset now,ref bool changed,ref GardenBed? removed)
    {
        // X-180 (0.1168): greyed in the desktop application (nothing is saved there).
        DesktopEditBegin();
        try{DrawBedButtonsCore(bed,now,ref changed,ref removed);}
        finally{DesktopEditEnd();}
    }

    private void DrawBedButtonsCore(GardenBed bed,DateTimeOffset now,ref bool changed,ref GardenBed? removed)
    {
        ImGui.BeginDisabled(GardenJournal.TendAt(bed) is null);
        try{if(ImGui.SmallButton(localization.Get("Garden.Tend"))){GardenJournal.Tend(bed,now);changed=true;}}
        finally{ImGui.EndDisabled();}
        Tip("Garden.TendTip");
        ImGui.SameLine();
        if(DeleteButton("Garden.Remove"))removed=bed;
        Tip("Garden.RemoveTip");
    }

    private void DrawNewBed(List<GardenBed> beds)
    {
        // X-180 (0.1168): greyed in the desktop application (nothing is saved there).
        DesktopEditBegin();
        try{DrawNewBedCore(beds);}
        finally{DesktopEditEnd();}
    }

    private void DrawNewBedCore(List<GardenBed> beds)
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
        // X-154 (0.1254): the form's main action (PrimaryButton, greyed when the journal is full).
        var full=beds.Count>=GardenJournal.Capacity;
        if(PrimaryButton("Garden.Plant",full ? "Garden.Full":"Garden.PlantTip",!full))
        {
            if(GardenJournal.CleanText(newBedPlace) is not { } place || GardenJournal.CleanText(newBedSeed) is not { } seed)newBedError=localization.Get("Garden.Invalid");
            else
            {
                configuration.GardenBeds=[..beds,GardenJournal.Plant(place,seed,newBedGrow,newBedTend,DateTimeOffset.UtcNow)];
                newBedSeed="";newBedError=null;SaveReminders();
            }
        }
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
            Tip(seeds.Count==0 ? "Garden.SeedPickNone":"Garden.SeedPickTip");
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
