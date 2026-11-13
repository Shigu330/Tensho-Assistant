using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Fields;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

// W-14 (0.717): Eureka and Bozja, from the game's files: where things happen, the field records, the lost actions. What the character holds there is not read: docs/evidence/0.717-eureka-bozja.md.
internal sealed partial class MainWindow
{
    internal Func<FieldCatalog?>? FieldCatalogLoad { get; set; }
    private Task<FieldCatalog?>? fieldLoad;
    private FieldCatalog? fieldCatalog;
    private bool fieldFailed;
    private string? fieldTab,fieldZoneTab;
    private string fieldQuery="";
    /// <summary>X-188 (0.1176): the Occult Crescent's state last read on the island (null: not read this session, or the desktop application).</summary>
    internal Func<OccultState?>? OccultStateRead { get; set; }
    /// <summary>X-188 (0.1178): the island's director is there now (the panel on the island).</summary>
    internal Func<bool>? OccultOnIsland { get; set; }

    // The game's files for the page, and for the panel on the island (0.1178): read once, off the framework thread.
    private FieldCatalog? FieldCatalogReady()
    {
        if(fieldCatalog is null && !fieldFailed && fieldLoad is null && FieldCatalogLoad is { } load)fieldLoad=Task.Run(load);
        if(fieldLoad is { IsCompleted:true } done)
        {
            fieldLoad=null;
            fieldCatalog=done.IsCompletedSuccessfully ? done.Result:null;
            fieldFailed=fieldCatalog is null;
        }
        return fieldCatalog;
    }

    private void DrawFieldContent()
    {
        Heading(localization.Get("FieldContent.Title"),localization.Get("FieldContent.Subtitle"));
        FieldCatalogReady();
        if(fieldFailed){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("FieldContent.Unavailable"));return;}
        if(fieldCatalog is not { } catalog){ImGui.TextDisabled(localization.Get("FieldContent.Loading"));return;}
        ImGui.SetNextItemWidth(240);
        ImGui.InputTextWithHint("###FieldQuery",localization.Get("FieldContent.Search"),ref fieldQuery,100);
        Tip("FieldContent.SearchTip");
        var tabs=new List<(string,string,Action)>
        {
            ("Zones",localization.Get("FieldContent.TabZones"),()=>DrawFieldZones(catalog)),
            ("Records",localization.Get("FieldContent.TabRecords"),()=>DrawFieldRecords(catalog)),
            ("Actions",localization.Get("FieldContent.TabActions"),()=>DrawFieldActions(catalog)),
        };
        // X-188 (0.1176): the Occult Crescent's phantom jobs, under the island's name (game data).
        if(catalog.OccultJobs.Count>0 && catalog.OccultLabels is { } labels && catalog.Zones.FirstOrDefault(zone=>zone.Use==OccultUse) is { } island)
            tabs.Add(("Occult",island.Name,()=>DrawFieldOccult(catalog.OccultJobs,labels)));
        DetailTabs("FieldTabs",tabs,ref fieldTab);
    }

    /// <summary>X-188 (0.1176): TerritoryIntendedUse 61, the Occult Crescent (FieldBoard.FieldZoneUses).</summary>
    private const uint OccultUse=61;

    // X-188 (0.1176): knowledge, coins and each phantom job: level, what it learns next and at which level, the worn job's experience.
    private void DrawFieldOccult(IReadOnlyList<OccultJob> jobs,OccultLabels labels)
    {
        ImGui.TextDisabled(localization.Get("FieldContent.OccultHint"));
        BoolSetting("FieldContent.OccultPanel","FieldContent.OccultPanelTip",()=>configuration.OccultPanel,value=>configuration.OccultPanel=value,c=>c.OccultPanel);
        BoolSetting("FieldContent.OccultAlerts","FieldContent.OccultAlertsTip",()=>configuration.OccultAlerts,value=>configuration.OccultAlerts=value,c=>c.OccultAlerts);
        DrawOccultCoinButtons();
        DrawOccultLore();
        if(OccultStateRead?.Invoke() is not { } state){ImGui.TextDisabled(localization.Get("FieldContent.OccultNotRead"));return;}
        ImGui.TextDisabled(localization.Format("FieldContent.OccultReadAt",state.At.ToLocalTime().ToString("t")));
        ImGui.TextUnformatted(labels.Knowledge);
        ImGui.SameLine();
        ImGui.ProgressBar(OccultCrescent.Share(state.Knowledge,state.KnowledgeNeeded),new Vector2(-1,ImGui.GetFrameHeight()),$"{(state.Knowledge).ToString("N0",Numbers)} / {(state.KnowledgeNeeded).ToString("N0",Numbers)}");
        ImGui.TextUnformatted(localization.Format("FieldContent.OccultCoins",state.Silver.ToString("N0",Numbers),state.Gold.ToString("N0",Numbers)));
        if(!ImGui.BeginTable("###FieldOccult",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.Resizable))return;
        try
        {
            ImGui.TableSetupColumn(labels.PhantomJob);ImGui.TableSetupColumn(localization.Get("FieldContent.ColumnLevel"));
            ImGui.TableSetupColumn(localization.Get("FieldContent.ColumnNext"));ImGui.TableSetupColumn(labels.Experience);
            ImGui.TableHeadersRow();
            foreach(var job in OccultCrescent.Ordered(jobs,state))
            {
                var level=OccultCrescent.LevelOf(state,job.Id);var worn=job.Id==(uint)state.CurrentJob;
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                if(worn)ImGui.TextColored(UI.FfxivTheme.Gold,job.Name);else ImGui.TextUnformatted(job.Name);
                ImGui.TableNextColumn();
                if(level>0)ImGui.TextUnformatted($"{level} / {job.MaxLevel}");else ImGui.TextDisabled("—");
                ImGui.TableNextColumn();
                if(OccultCrescent.Next(job,level) is { } next)
                {
                    DrawSmallGameIcon(next.Icon);ImGui.TextUnformatted(localization.Format("FieldContent.OccultNext",next.Name,next.Level));
                    if(ImGui.IsItemHovered() && next.Description.Length>0)WrappedTooltip(next.Description);
                }
                else if(level>=job.MaxLevel && level>0)ImGui.TextColored(UI.FfxivTheme.Success,labels.Mastered);
                else ImGui.TextDisabled("—");
                ImGui.TableNextColumn();
                if(worn && state.NeededJobExperience>0 && job.Id<state.Experience.Count)ImGui.TextUnformatted($"{(state.Experience[(int)job.Id]).ToString("N0",Numbers)} / {(state.NeededJobExperience).ToString("N0",Numbers)}");
                else ImGui.TextDisabled("—");
            }
        }
        finally{ImGui.EndTable();}
    }

    // X-190 (0.1178): « what it buys » for each coin of the island: Storage › What to do with it (vendors, prices, collection items).
    private void DrawOccultCoinButtons()
    {
        var coins=fieldCatalog?.OccultCoins ?? [];
        for(var index=0;index<coins.Count;index++)
        {
            if(index>0)ImGui.SameLine();
            if(ImGui.SmallButton(localization.Format("FieldContent.OccultCoinBuys",coins[index].Name)+$"##OccultCoin{coins[index].ItemId}"))OpenCurrencyValue(coins[index].ItemId);
            Tip("FieldContent.OccultCoinBuysTip");
        }
    }

    // X-190 (0.1178): the occult records read, out of all, counted when the collections' list changes; the button opens what is missing.
    private object? occultLoreFrom;
    private (int Read,int Total) occultLore;
    private void DrawOccultLore()
    {
        var entries=ExtraCollections?.Invoke();
        if(entries is null)return;
        if(!ReferenceEquals(occultLoreFrom,entries)){occultLoreFrom=entries;occultLore=OccultCrescent.Lore(entries);}
        if(occultLore.Total==0)return;
        ImGui.TextUnformatted(localization.Format("FieldContent.OccultLore",occultLore.Read,occultLore.Total));
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("FieldContent.OccultLoreOpen")+"##OccultLoreOpen"))
        {
            page=Page.Collections;collectionKind=Collections.CollectionKind.OccultLore;focusCollectionKind=Collections.CollectionKind.OccultLore;collectionFilter=2;
        }
        Tip("FieldContent.OccultLoreOpenTip");
    }

    // X-188 (0.1178, user request of 12/11): the panel on the island: the worn phantom job, its level and experience, what it learns next,
    // knowledge and coins. Shown only while the island's director is there, once its state and the game's files are read.
    private OccultState? occultPanelState;
    private PanelDefinition OccultPanelDefinition=>new("OccultPanel","###TenshoOccultPanel",new Vector2(40,500),
        ImGuiWindowFlags.NoTitleBar|ImGuiWindowFlags.AlwaysAutoResize|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoNav|ImGuiWindowFlags.NoScrollbar,
        ()=>configuration.OccultPanel,OccultPanelVisible,DrawOccultPanelContents,Alpha:0.7f);

    private bool OccultPanelVisible()
    {
        occultPanelState=null;
        if(windowDisposed || OccultOnIsland?.Invoke()!=true || OccultStateRead?.Invoke() is not { } state)return false;
        // DEC-041: nothing shown during a fight.
        if(mapLive?.State is { InCombat:true })return false;
        if(FieldCatalogReady() is not { OccultJobs.Count:>0,OccultLabels:not null })return false;
        occultPanelState=state;
        return true;
    }

    private void DrawOccultPanelContents()
    {
        if(occultPanelState is not { } state || fieldCatalog is not { OccultLabels:{ } labels } catalog)return;
        var width=220*Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale;
        if(OccultCrescent.Worn(catalog.OccultJobs,state) is { } job)
        {
            var level=OccultCrescent.LevelOf(state,job.Id);
            ImGui.TextColored(UI.FfxivTheme.Gold,job.Name);
            ImGui.SameLine();
            ImGui.TextUnformatted($"{level} / {job.MaxLevel}");
            if(OccultCrescent.WornExperience(job,state) is { } experience)
            {
                ImGui.ProgressBar(OccultCrescent.Share(experience.Have,experience.Need),new Vector2(width,ImGui.GetFrameHeight()),$"{(experience.Have).ToString("N0",Numbers)} / {(experience.Need).ToString("N0",Numbers)}");
                TipText(labels.Experience);
            }
            else if(level>=job.MaxLevel && level>0)ImGui.TextColored(UI.FfxivTheme.Success,labels.Mastered);
            if(OccultCrescent.Next(job,level) is { } next)
            {
                DrawSmallGameIcon(next.Icon);ImGui.TextUnformatted(localization.Format("FieldContent.OccultPanelNext",next.Name,next.Level));
                if(next.Description.Length>0)TipText(next.Description);
            }
        }
        ImGui.ProgressBar(OccultCrescent.Share(state.Knowledge,state.KnowledgeNeeded),new Vector2(width,ImGui.GetFrameHeight()),$"{(state.Knowledge).ToString("N0",Numbers)} / {(state.KnowledgeNeeded).ToString("N0",Numbers)}");
        TipText(labels.Knowledge);
        ImGui.TextUnformatted(localization.Format("FieldContent.OccultCoins",state.Silver.ToString("N0",Numbers),state.Gold.ToString("N0",Numbers)));
        Tip("FieldContent.OccultPanelTip");
    }

    // X-189 (0.1179, user request of 12/11): on the island, a critical engagement that opens its registration and a FATE that starts are said
    // in the chat and sent to the desktop application, with the place and the time left; the alarms' sound if it is on. DEC-041: nothing during a
    // fight: what opened then is said once the fight is over, if it is still open. Checked once a second.
    private readonly HashSet<int> occultEngagementsSaid=[];
    private readonly HashSet<uint> occultFatesSaid=[];
    private DateTimeOffset occultAlertsAt;
    internal void ObserveOccultAlerts()
    {
        if(windowDisposed || !configuration.OccultAlerts)return;
        var now=DateTimeOffset.UtcNow;
        if(now<occultAlertsAt)return;
        occultAlertsAt=now.AddSeconds(1);
        if(OccultOnIsland?.Invoke()!=true){occultEngagementsSaid.Clear();occultFatesSaid.Clear();return;}
        var live=mapLive?.State;
        if(live is { InCombat:true })return;
        var catalog=maps.State.Catalog;
        if(OccultStateRead?.Invoke() is { } state)
            foreach(var engagement in OccultAlerts.Engagements(state.Events,occultEngagementsSaid))
            {
                var map=catalog?.Find(engagement.MapId);
                var at=map is null ? (Vector2?)null:MapProjection.CoordinateFromWorld(new Vector3(engagement.X,0,engagement.Z),map);
                SayOccultAlert(localization.Format("FieldContent.OccultEngagementAlert",engagement.Name,OccultAlerts.Clock(engagement.SecondsLeft),map?.Zone ?? "",
                    at is { } place ? MapProjection.CoordinateNumber(place.X):"?",at is { } same ? MapProjection.CoordinateNumber(same.Y):"?"));
            }
        if(live is { Fates:{ } fates } && catalog?.Find(live.MapId) is { } fateMap)
            foreach(var fate in OccultAlerts.Fates(fates,fate=>fate.Id,occultFatesSaid))
            {
                var at=MapProjection.CoordinateFromWorld(fate.Position,fateMap);
                SayOccultAlert(localization.Format("FieldContent.OccultFateAlert",fate.Name,fate.Level,OccultAlerts.Clock(fate.SecondsLeft),fateMap.Zone,
                    MapProjection.CoordinateNumber(at.X),MapProjection.CoordinateNumber(at.Y)));
            }
    }

    private void SayOccultAlert(string alert)
    {
        ChatNotice?.Invoke(alert);
        if(configuration.AlarmOptions.PlaySound)Alarms.AlarmService.PlaySound(configuration.AlarmOptions.Sound,log);
        LinkNotice?.Invoke(alert);
    }

    private void DrawFieldZones(FieldCatalog catalog)
    {
        var tabs=catalog.Zones.Where(zone=>zone.Events.Count>0).Select(zone=>(zone.Territory.ToString(),$"{zone.Name} ({zone.Events.Count})",(Action)(()=>DrawFieldZone(zone)))).ToList();
        if(tabs.Count==0){ImGui.TextDisabled(localization.Get("FieldContent.NoZones"));return;}
        DetailTabs("FieldZoneTabs",tabs,ref fieldZoneTab);
    }

    private void DrawFieldZone(FieldZone zone)
    {
        var map=maps.State.Catalog?.Find(zone.Map);
        ImGui.TextDisabled(localization.Get("FieldContent.ZoneHint"));
        var rows=zone.Events.Where(row=>FieldBoard.Matches(fieldQuery,row.Name,row.Description)).ToArray();
        if(rows.Length==0){ImGui.TextDisabled(localization.Get("FieldContent.NoMatch"));return;}
        if(!ImGui.BeginTable($"###FieldZone{zone.Territory}",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.Resizable))return;
        try
        {
            foreach(var column in new[]{"ColumnName","ColumnKind","ColumnLevel","ColumnPlace"})ImGui.TableSetupColumn(localization.Get("FieldContent."+column));
            ImGui.TableHeadersRow();
            foreach(var row in rows)
            {
                var coordinate=map is null ? (Vector2?)null:CoordinateOf(map,new Vector3(row.X,0,row.Z));
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                if(ImGui.Selectable($"{row.Name}##FieldEvent{row.Kind}{row.Id}") && map is not null && coordinate is { } target)OpenMapPointFromPage(map,target.X,target.Y);
                if(ImGui.IsItemHovered())WrappedTooltip(FieldEventTip(row));
                if(map is not null && coordinate is { } place)PlaceMenu($"FieldPlace{row.Kind}{row.Id}",map,place,row.Name);
                ImGui.TableNextColumn();ImGui.TextUnformatted(localization.Get(row.Kind==FieldEventKind.Fate ? (zone.Use==FieldBoard.BozjaUse ? "FieldContent.KindSkirmish":"FieldContent.KindFate"):"FieldContent.KindEngagement"));
                ImGui.TableNextColumn();
                if(row.Level is { } level)ImGui.TextUnformatted(level.ToString());else ImGui.TextDisabled("—");
                ImGui.TableNextColumn();
                if(coordinate is { } at)ImGui.TextUnformatted(MapProjection.CoordinateText(at));else ImGui.TextDisabled("—");
            }
        }
        finally{ImGui.EndTable();}
    }

    private string FieldEventTip(FieldEvent row)
    {
        var text=row.Description.Length>0 ? row.Description:localization.Get("FieldContent.NoDescription");
        return row.Participants>0 ? text+"\n\n"+localization.Format("FieldContent.Participants",row.Participants):text;
    }

    private void DrawFieldRecords(FieldCatalog catalog)
    {
        ImGui.TextDisabled(localization.Get("FieldContent.RecordsHint"));
        var rows=catalog.Records.Where(row=>FieldBoard.Matches(fieldQuery,row.Name,row.Description,row.Quest)).ToArray();
        if(rows.Length==0){ImGui.TextDisabled(localization.Get("FieldContent.NoMatch"));return;}
        if(!ImGui.BeginTable("###FieldRecords",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.Resizable))return;
        try
        {
            foreach(var column in new[]{"ColumnNumber","ColumnName","ColumnRarity","ColumnQuest"})ImGui.TableSetupColumn(localization.Get("FieldContent."+column));
            ImGui.TableHeadersRow();
            foreach(var row in rows)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();ImGui.TextUnformatted(row.Number.ToString());
                ImGui.TableNextColumn();DrawSmallGameIcon(row.Icon);ImGui.TextUnformatted(row.Name);
                if(ImGui.IsItemHovered() && row.Description.Length>0)WrappedTooltip(row.Description);
                ImGui.TableNextColumn();ImGui.TextUnformatted(new string('★',Math.Clamp(row.Rarity,0,5)));
                ImGui.TableNextColumn();ImGui.TextUnformatted(row.Quest);
            }
        }
        finally{ImGui.EndTable();}
    }

    private void DrawFieldActions(FieldCatalog catalog)
    {
        ImGui.TextDisabled(localization.Get("FieldContent.ActionsHint"));
        var rows=catalog.Actions.Where(row=>FieldBoard.Matches(fieldQuery,row.Name,row.Description,row.Category)).ToArray();
        if(rows.Length==0){ImGui.TextDisabled(localization.Get("FieldContent.NoMatch"));return;}
        if(!ImGui.BeginTable("###FieldActions",3,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.Resizable))return;
        try
        {
            foreach(var column in new[]{"ColumnName","ColumnCategory","ColumnMost"})ImGui.TableSetupColumn(localization.Get("FieldContent."+column));
            ImGui.TableHeadersRow();
            foreach(var row in rows)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();DrawSmallGameIcon(row.Icon);ImGui.TextUnformatted(row.Name);
                if(ImGui.IsItemHovered() && row.Description.Length>0)WrappedTooltip(row.Description);
                ImGui.TableNextColumn();ImGui.TextUnformatted(row.Category);
                ImGui.TableNextColumn();ImGui.TextUnformatted(row.Most.ToString());
            }
        }
        finally{ImGui.EndTable();}
    }
}
