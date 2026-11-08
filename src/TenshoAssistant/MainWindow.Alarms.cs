using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.Alarms;
using TenshoAssistant.Community;
using TenshoAssistant.Gathering;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

/// <summary>
/// Tensho alarms (0.201) — their own page since 0.202 — and the bell buttons other tools use: weather periods and searches, fishing windows
/// (GatherBuddy ET window with weather and previous weather, 0.254; Garland conditions), gathering windows, sightseeing vistas (wiki
/// weathers), Reminders (0.300), company voyages (0.307) and manual daily ET alarms. Once or every ET day, grouped by gathering job then by
/// item, saved in the configuration; daily ones can also be written into the game's own alarm list (« /alarm », 0.205). AlarmService rings
/// them and copies the game's alarms on the framework thread; this file only draws (next times cached, at most one computation per second
/// per alarm, 0.291).
/// </summary>
internal sealed partial class MainWindow
{
    // 0.291 (R-06 step 6, measured 9 ms): the next time of an alarm (a search through weather periods for weather alarms) was
    // computed several times per alarm and per frame; it is kept and computed again at most once a second, or when it is passed.
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<TenshoAlarm,NextAlarmCache> nextAlarms=new();
    private sealed class NextAlarmCache{public DateTimeOffset ComputedAt;public DateTimeOffset? Next;public bool Enabled;}
    private DateTimeOffset? NextAlarm(TenshoAlarm alarm,DateTimeOffset now)
    {
        var cache=nextAlarms.GetOrCreateValue(alarm);
        if(cache.ComputedAt==default || now-cache.ComputedAt>=TimeSpan.FromSeconds(1) || cache.Enabled!=alarm.Enabled || cache.Next is { } next && next<=now)
        {cache.ComputedAt=now;cache.Enabled=alarm.Enabled;cache.Next=AlarmSchedule.Next(alarm,now);}
        return cache.Next;
    }

    private AlarmService? alarmService;
    internal void ConfigureAlarms(AlarmService service)=>alarmService=service;
    private string alarmStatus="";private string alarmManualLabel="";private int alarmManualHour=8,alarmManualMinute;
    // 0.325: the group whose « Delete all » waits for its second click (null: none).
    private string? alarmGroupDeleteArmed;private int alarmGroupDeleteSeenFrame;

    private int DefaultAlarmLead=>configuration.AlarmOptions?.LeadMinutes ?? 5;

    private Guid? alarmEditing;
    private string alarmEditDate="";private Guid? alarmEditDateFor;
    private void DrawAlarmEdit(TenshoAlarm alarm,DateTimeOffset now,ref bool changed)
    {
        ImGui.Indent();
        var label=alarm.Label;ImGui.SetNextItemWidth(220);
        if(ImGui.InputText(localization.Get("Alarms.EditName")+"##AlarmEditName",ref label,100) && label.Trim().Length>0)alarm.Label=label;
        if(ImGui.IsItemDeactivatedAfterEdit())changed=true;
        Tip("Alarms.EditNameTip");
        if(alarm.Repeat==AlarmRepeat.Once)
        {
            if(alarmEditDateFor!=alarm.Id){alarmEditDateFor=alarm.Id;alarmEditDate=alarm.At.ToLocalTime().ToString("yyyy-MM-dd HH:mm",System.Globalization.CultureInfo.InvariantCulture);}
            ImGui.SetNextItemWidth(160);
            ImGui.InputText(localization.Get("Alarms.EditDate")+"##AlarmEditDate",ref alarmEditDate,20);
            Tip("Alarms.EditDateTip");
            if(WorldClocks.ParseLocal(alarmEditDate,now) is { } moved)
            {
                if(ImGui.IsItemDeactivatedAfterEdit()){alarm.At=moved;alarm.LastOccurrence=default;alarm.LastRung=default;alarm.LastOnTime=default;changed=true;}
            }
            else ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("Alarms.EditDateBad"));
        }
        if(alarm.GameName.Length>0)ImGui.TextColored(Accent,localization.Get("Alarms.EditInGame"));
        ImGui.Unindent();
    }
    /// <summary>0.832 (X-62): a list alarm whose item is held in the quantity the list needed.</summary>
    internal bool AlarmGathered(TenshoAlarm alarm)
    {
        if(alarm.Needed<=0)return false;
        RefreshRecipeHoldingsCache();
        return GetRecipeHoldings(alarm.ItemId) is { HasObservation:true } held && AlarmSchedule.Gathered(alarm,held.Total);
    }
    private void AddAlarms(IReadOnlyList<TenshoAlarm> added)
    {
        configuration.Alarms??=[];
        if(added.Count==0){alarmStatus=localization.Get("Alarms.NoWindow");return;}
        // R-12: an alarm joining an existing group takes the group's Discord switch and message.
        foreach(var alarm in added)
            if(configuration.Alarms.FirstOrDefault(known=>AlarmGroupKey(known)==AlarmGroupKey(alarm)) is { } member)
            {alarm.Discord=member.Discord;alarm.DiscordMessage=member.DiscordMessage;}
        var before=configuration.Alarms.Count;
        if(!AlarmSchedule.Add(configuration.Alarms,added)){alarmStatus=localization.Format("Alarms.Full",AlarmSchedule.Maximum);return;}
        try
        {
            save();alarmStatus=localization.Format("Alarms.Added",configuration.Alarms.Count-before);
            if(configuration.AlarmOptions is { UseGameAlarms:true } options)SendAlarmsToGame(configuration.Alarms.Skip(before).ToArray(),options);
        }
        catch(Exception error){configuration.Alarms.RemoveRange(before,configuration.Alarms.Count-before);log.Warning(error,"Alarms could not be saved.");alarmStatus=localization.Get("Alarms.SaveFailed");}
    }

    // Daily ET alarms into the game's list (0.205); weather-conditioned ones only when the setting allows it.
    private void SendAlarmsToGame(IReadOnlyList<TenshoAlarm> alarms,AlarmSettings options)
    {
        var eligible=alarms.Where(alarm=>alarm.Repeat==AlarmRepeat.EorzeaDaily && alarm.GameName.Length==0
            && (options.GameIncludeWeather || alarm.Weathers.Count+alarm.PreviousWeathers.Count==0)).ToArray();
        var queued=alarmService?.SendToGame(eligible,options.Sound,options.MuteInDuties) ?? 0;
        var skipped=alarms.Count(alarm=>alarm.Repeat==AlarmRepeat.EorzeaDaily && alarm.GameName.Length==0)-eligible.Length;
        alarmStatus+=" "+localization.Format("Alarms.GameQueued",queued)+(skipped>0 ? " "+localization.Format("Alarms.GameSkippedWeather",skipped):"");
    }

    // Bell button that creates the alarms; the tooltip says what will be created. 0.331: daily = alarms every ET day, whose lead is
    // bounded to 60 min (ClampLead): the tooltip gives the lead they really get.
    private void AlarmButton(string id,string tooltip,Func<IReadOnlyList<TenshoAlarm>> create,bool daily=false)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        bool clicked;
        try{clicked=ImGui.SmallButton(FontAwesomeIcon.Bell.ToIconString()+"##Alarm"+id);}
        finally{ImGui.PopFont();}
        if(ImGui.IsItemHovered())WrappedTooltip(tooltip+"\n"+localization.Format("Alarms.LeadHint",AlarmSchedule.ClampLead(daily ? AlarmRepeat.EorzeaDaily:AlarmRepeat.Once,DefaultAlarmLead)));
        if(clicked)AddAlarms(create());
    }
    // 0.331: a copy in the game keeps ringing there as it was sent; turning the alarm off or changing its lead here does not reach it.
    private void NoteGameCopy(IEnumerable<TenshoAlarm> edited){if(edited.Any(alarm=>alarm.GameName.Length>0))alarmStatus=localization.Get("Alarms.GameCopyUnchanged");}
    // 0.330: empty space the size of the bell (SmallButton: text plus the frame's side padding) where no alarm can be made, so a list stays aligned.
    private static void AlarmButtonSpace()
    {
        ImGui.PushFont(UiBuilder.IconFont);
        try{ImGui.Dummy(ImGui.CalcTextSize(FontAwesomeIcon.Bell.ToIconString())+new Vector2(ImGui.GetStyle().FramePadding.X*2,0));}
        finally{ImGui.PopFont();}
    }

    // Weather ids for community weather names of a zone; null when one name is not proven in this zone (condition then not checked).
    private static IReadOnlyList<uint>? WeatherIds(WeatherCatalog catalog,IReadOnlyList<string>? names,uint territory)
    {
        if(names is not { Count:>0 })return [];
        var ids=names.Select(name=>catalog.WeatherBySourceName(name,territory)?.Id).ToArray();
        return ids.All(id=>id is not null) ? ids.Select(id=>id!.Value).Distinct().ToArray():null;
    }

    // 0.254: GatherBuddy fish — one alarm per ET window start, with the fish's weather and previous weather conditions; with weather
    // but no time window, the starts of the three weather periods (00, 08, 16 ET). Shared by « How to fish it » and the gathering rows.
    private bool GatherBuddyFishAlarmButton(string id,Community.GatherBuddyFish fish,uint territory,string label)
    {
        var starts=fish.StartMinute is { } minute ? [minute]:(fish.Weathers.Count+fish.Transitions.Count>0 ? new[]{0,480,960}:[]);
        if(starts.Length==0)return false;
        AlarmButton(id,localization.Format("Alarms.FishTooltip",starts.Length),
            ()=>AlarmSchedule.ForWindows(label,starts,DefaultAlarmLead,territory,fish.FishId,fish.Weathers,fish.Transitions)
                .Select(created=>{created.JobId=18;return created;}).ToArray(),daily:true);
        return true;
    }

    // Fishing: one daily alarm per ET window start; with weather but no time window, the starts of the three weather periods (00, 08, 16 ET).
    private void DrawFishingAlarmButton(string id,FishingConditions conditions,uint territory)
    {
        if(weather.State.Catalog is not { } catalog)return;
        var weathers=WeatherIds(catalog,conditions.Weather,territory);var previous=WeatherIds(catalog,conditions.PreviousWeather,territory);
        // 0.330: one start per window, a window crossing midnight included (OpeningMinutes, no extra alarm at 00:00).
        var starts=conditions.Timing.Kind==GatheringScheduleKind.Limited ? conditions.Timing.OpeningMinutes.ToArray()
            :(weathers is { Count:>0 } || previous is { Count:>0 }) ? [0,480,960]:[];
        if(starts.Length==0)return;
        var unproven=weathers is null || previous is null;
        var label=SelectedItem?.Name ?? itemId.ToString();
        AlarmButton(id,localization.Format("Alarms.FishTooltip",starts.Length)+(unproven ? "\n"+localization.Get("Alarms.WeatherUnproven"):""),
            ()=>AlarmSchedule.ForWindows(label,starts,DefaultAlarmLead,territory,(uint)itemId,weathers ?? [],previous ?? []).Select(created=>{created.JobId=18;return created;}).ToArray(),daily:true);
        ImGui.SameLine();ImGui.TextDisabled(localization.Format("Alarms.Windows",starts.Length));
    }

    private void DrawGatheringAlarmButton(string id,GatheringSource source)
    {
        var timing=source.Timing;
        if(timing.Kind!=GatheringScheduleKind.Limited || timing.OpeningMinutes.Count==0)return;
        var place=string.IsNullOrWhiteSpace(source.Place) ? source.Zone:source.Place;
        var label=$"{SelectedItem?.Name ?? itemId.ToString()} · {place}";
        var job=AlarmSchedule.JobOf(source.TypeId,source.IsFishing);
        AlarmButton(id,localization.Format("Alarms.GatherTooltip",timing.OpeningMinutes.Count)+(job!=0 ? "\n"+JobName(job):""),
            ()=>AlarmSchedule.ForWindows(label,timing.OpeningMinutes,DefaultAlarmLead,source.TerritoryId,(uint)itemId)
                .Select(created=>{created.JobId=job;return created;}).ToArray(),daily:true);
    }

    // Gathering job names from the game (ClassJob), given by the plugin.
    private Func<uint,string?>? jobName;
    internal void ConfigureJobNames(Func<uint,string?> names)=>jobName=names;
    private string JobName(uint job)=>jobName?.Invoke(job) is { Length:>0 } name ? char.ToUpper(name[0],System.Globalization.CultureInfo.CurrentCulture)+name[1..]:job.ToString();
    // Job of an alarm: the one stored, else the item's gathering sources (alarms created before 0.203); 0 when none or several.
    private uint AlarmJob(TenshoAlarm alarm)=>AlarmSchedule.JobOfAlarm(alarm,item=>gatheringCatalog.State.Catalog?.ForItem(item));

    // Vista (sightseeing log): daily alarm at the start of its ET window, with the weathers the wiki gives when they are proven in the zone.
    // 0.328: weatherNote is null when the weather is known, else the key saying why it is not checked: no wiki row (wiki off, loading,
    // failed or row not matched) or a wiki weather not linked to the zone; the alarm then has the ET window only.
    private TenshoAlarm? VistaAlarm(MapLocalPosition vista,uint territory,out string? weatherNote)
    {
        weatherNote=null;
        if(weather.State.Catalog is not { } catalog)return null;
        var wiki=WikiVistaFor(vista);var proven=WeatherIds(catalog,wiki?.Weathers,territory);
        weatherNote=wiki is null ? "Alarms.VistaWeatherUnknown":proven is null ? "Alarms.WeatherUnproven":null;
        var weathers=proven ?? [];
        var hasTime=vista.MinTime!=0 || vista.MaxTime!=0;
        if(!hasTime && weathers.Count==0)return null;
        var start=vista.MinTime/100*60+vista.MinTime%100;
        return new TenshoAlarm{Label=localization.Format("MapView.Vista",vista.Name),Group=localization.Format("MapView.Vista",vista.Name),Repeat=AlarmRepeat.EorzeaDaily,EtMinute=hasTime ? start:0,
            LeadMinutes=AlarmSchedule.ClampLead(AlarmRepeat.EorzeaDaily,DefaultAlarmLead),TerritoryId=territory,Weathers=[..weathers]};
    }
    // Next real time the vista's ET window opens with a matching weather (within 40 ET days), or null, also when the weather is not
    // known (0.328: a window computed without it is not the « ET time and weather » the text says); cached per vista, minute, wiki row
    // and weather catalogue.
    private (uint Id,long Minute,object? Wiki,object? Weather,DateTimeOffset? At) vistaNextCache;
    private DateTimeOffset? NextVistaWindow(MapLocalPosition vista,uint territory)
    {
        var minute=DateTimeOffset.UtcNow.ToUnixTimeSeconds()/60;
        var wiki=WikiVistaFor(vista);var catalog=weather.State.Catalog;
        if(vistaNextCache.Id==vista.Id && vistaNextCache.Minute==minute && ReferenceEquals(vistaNextCache.Wiki,wiki) && ReferenceEquals(vistaNextCache.Weather,catalog))
            return vistaNextCache.At;
        DateTimeOffset? found=null;
        if(VistaAlarm(vista,territory,out var weatherNote) is { } alarm && weatherNote is null)
        {
            var starts=alarm.Weathers.Count>0 && vista.MinTime==0 && vista.MaxTime==0 ? [0,480,960]:new[]{alarm.EtMinute};
            found=starts.SelectMany(start=>AlarmSchedule.Occurrences(new TenshoAlarm{Repeat=AlarmRepeat.EorzeaDaily,EtMinute=start},DateTimeOffset.UtcNow.AddMinutes(-1),40))
                .Where(time=>alarm.Weathers.Count==0 || AlarmService.WeatherAt(catalog,territory,time) is { } id && alarm.Weathers.Contains(id))
                .Order().Select(time=>(DateTimeOffset?)time).FirstOrDefault();
        }
        vistaNextCache=(vista.Id,minute,wiki,catalog,found);
        return found;
    }
    private void DrawVistaAlarmEntry(MapLocalPosition vista,uint territory)
    {
        if(VistaAlarm(vista,territory,out var weatherNote) is not { } alarm){ImGui.TextDisabled(localization.Get("Alarms.VistaNone"));return;}
        var starts=alarm.Weathers.Count>0 && vista.MinTime==0 && vista.MaxTime==0 ? new[]{0,480,960}:new[]{alarm.EtMinute};
        if(ImGui.Selectable(localization.Format(weatherNote is null ? "Alarms.VistaAdd":"Alarms.VistaAddWindow",vista.Name)+"##VistaAlarm"))
            AddAlarms(AlarmSchedule.ForWindows(alarm.Label,starts,alarm.LeadMinutes,territory,0,alarm.Weathers).Select(created=>{created.Group=alarm.Group;return created;}).ToArray());
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("Alarms.LeadHint",alarm.LeadMinutes)+(weatherNote is null ? "":"\n"+localization.Get(weatherNote)));
    }

    private string AlarmWhen(TenshoAlarm alarm)=>alarm.Repeat switch
    {
        AlarmRepeat.EorzeaDaily=>localization.Format("Alarms.Daily",GatheringTime.Clock(alarm.EtMinute)),
        AlarmRepeat.RealDaily=>localization.Format("Alarms.RealDaily",alarm.At.ToLocalTime().ToString("t")),
        AlarmRepeat.RealWeekly=>localization.Format("Alarms.RealWeekly",alarm.At.ToLocalTime().ToString("dddd t")),
        AlarmRepeat.DailyReset=>localization.Get("Alarms.AtDailyReset"),
        AlarmRepeat.WeeklyReset=>localization.Get("Alarms.AtWeeklyReset"),
        _=>alarm.At.ToLocalTime().ToString("g"),
    };

    // Alarm group (0.202): the item, or the weather / vista / name the alarm was created for, with its icon.
    private static string AlarmGroupKey(TenshoAlarm alarm)=>alarm.ItemId!=0 ? $"item:{alarm.ItemId}":alarm.Group.Length>0 ? alarm.Group
        :System.Text.RegularExpressions.Regex.Replace(alarm.Label,@" · \d\d:\d\d ET$","");
    private void DrawAlarmGroupIcon(TenshoAlarm first,string title)
    {
        var size=new Vector2(32);
        if(first.ItemId!=0 && itemCatalog.State.Catalog?.Find(first.ItemId) is { } item)
        {DrawItemIcon(item,size);return;}
        if(first.IconId!=0){DrawItemIcon(new Items.ItemDefinition(0,title,false,first.IconId),size);if(ImGui.IsItemHovered())WrappedTooltip(title);return;}
        ImGui.PushFont(UiBuilder.IconFont);
        try{ImGui.Button(FontAwesomeIcon.Bell.ToIconString()+"##GroupIcon",size);}
        finally{ImGui.PopFont();}
        if(ImGui.IsItemHovered())WrappedTooltip(title);
    }

    // Alarms module (0.202): its own page, alarms grouped by item with icon and tooltip, settings and the game's alarms.
    private void DrawAlarmsPage()
    {
        configuration.Alarms??=[];
        var alarms=configuration.Alarms;
        Heading(localization.Get("Alarms.PageTitle"),localization.Format("Alarms.Title",alarms.Count));
        DrawGatheringClock();
        DrawWorldClocks();
        ImGui.TextWrapped(localization.Get("Alarms.Help"));
        if(alarmStatus.Length>0)ImGui.TextColored(Accent,alarmStatus);
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("Alarms.SettingsTitle")+"###AlarmSettingsPage"))DrawAlarmSettings();
        var now=DateTimeOffset.UtcNow;var changed=false;var remove=new List<TenshoAlarm>();var added=new List<TenshoAlarm>();
        // 0.326: an armed « Delete all » not drawn on the previous frame (page left, header folded, group gone) is dropped.
        if(alarmGroupDeleteArmed is not null && ImGui.GetFrameCount()>alarmGroupDeleteSeenFrame+1)alarmGroupDeleteArmed=null;
        if(alarms.Count==0)ImGui.TextDisabled(localization.Get("Alarms.Empty"));
        // Sections by gathering job (0.203: miner, botanist, fisher, then the other alarms), groups ordered by their next ringing time.
        foreach(var section in alarms.GroupBy(AlarmJob).OrderBy(section=>section.Key==0 ? uint.MaxValue:section.Key))
        {
            var sectionTitle=section.Key==0 ? localization.Get("Alarms.OtherSection"):JobName(section.Key);
            ImGui.Spacing();
            // ClassJob icon 62100 + job (62116 miner, 62117 botanist, 62118 fisher), as the gathering catalogue uses for fishing.
            if(section.Key!=0){DrawItemIcon(new Items.ItemDefinition(0,sectionTitle,false,62100+section.Key),new Vector2(ImGui.GetFrameHeight()));ImGui.SameLine();}
            if(!TenshoAssistant.UI.UiComponents.Header($"{sectionTitle} ({section.Count()})###JobSection{section.Key}",ImGuiTreeNodeFlags.DefaultOpen))continue;
            foreach(var group in section.GroupBy(AlarmGroupKey).OrderBy(group=>group.Min(alarm=>alarm.Enabled ? NextAlarm(alarm,now) ?? DateTimeOffset.MaxValue:DateTimeOffset.MaxValue)))
            {
                var members=group.ToArray();var first=members[0];
                var title=first.ItemId!=0 ? itemCatalog.State.Catalog?.Find(first.ItemId)?.Name ?? first.ItemId.ToString()
                    :first.Group.Length>0 ? first.Group:group.Key;
                ImGui.PushID($"{section.Key}/{group.Key}");
                try
                {
                    ImGui.Separator();
                    DrawAlarmGroupIcon(first,title);
                    ImGui.SameLine();
                    ImGui.BeginGroup();
                    try
                    {
                        var next=members.Where(alarm=>alarm.Enabled).Select(alarm=>NextAlarm(alarm,now)).OfType<DateTimeOffset>().OrderBy(time=>time).FirstOrDefault();
                        ImGui.TextColored(members.Any(alarm=>alarm.Enabled) ? Accent:Muted,title);
                        ImGui.TextDisabled(localization.Format("Alarms.GroupLine",members.Length)
                            +(next!=default ? " · "+localization.Format("Alarms.Next",next.ToLocalTime().ToString("t"),Math.Max(0,(int)(next-now).TotalMinutes)):"")
                            +(members.Any(alarm=>alarm.Weathers.Count+alarm.PreviousWeathers.Count>0) ? " · "+localization.Get("Alarms.WeatherCondition"):"")
                            +(members.Any(AlarmGathered) ? " · "+localization.Get("Alarms.GatheredSilent"):""));
                        var all=members.All(alarm=>alarm.Enabled);
                        if(ImGui.Checkbox(localization.Get("Alarms.GroupOn")+"##GroupOn",ref all)){foreach(var alarm in members)alarm.Enabled=all;changed=true;NoteGameCopy(members);}
                        Tip("SettingTips.AlarmGroupOn");
                        ImGui.SameLine();ImGui.SetNextItemWidth(90);
                        var groupLead=first.LeadMinutes;
                        if(ImGui.InputInt(localization.Get("Alarms.Lead")+"##GroupLead",ref groupLead,1)){foreach(var alarm in members)alarm.LeadMinutes=AlarmSchedule.ClampLead(alarm.Repeat,groupLead);changed=true;NoteGameCopy(members);}
                        Tip("SettingTips.AlarmLead");
                        ImGui.SameLine();
                        // 0.325: « Delete all » asks a second click, as « Restore all defaults » does.
                        var armKey=$"{section.Key}/{group.Key}";
                        if(alarmGroupDeleteArmed!=armKey)
                        {
                            if(ImGui.SmallButton(localization.Get("Alarms.DeleteGroup"))){alarmGroupDeleteArmed=armKey;alarmGroupDeleteSeenFrame=ImGui.GetFrameCount();}
                            Tip("Alarms.DeleteGroupTip");
                        }
                        else
                        {
                            alarmGroupDeleteSeenFrame=ImGui.GetFrameCount();
                            ImGui.PushStyleColor(ImGuiCol.Text,UI.FfxivTheme.Error);
                            bool confirmed;
                            try{confirmed=ImGui.SmallButton(localization.Format("Alarms.DeleteGroupConfirm",members.Length));}
                            finally{ImGui.PopStyleColor();}
                            if(confirmed){remove.AddRange(members);alarmGroupDeleteArmed=null;}
                            ImGui.SameLine();
                            if(ImGui.SmallButton(localization.Get("Settings.ResetAllNo")+"##CancelGroupDelete"))alarmGroupDeleteArmed=null;
                        }
                    if(members.Any(alarm=>alarm.Repeat==AlarmRepeat.EorzeaDaily && alarm.GameName.Length==0))
                    {
                        ImGui.SameLine();
                        if(ImGui.SmallButton(localization.Get("Alarms.SendGroup"))){alarmStatus="";SendAlarmsToGame(members,(configuration.AlarmOptions ?? new()) is var o ? new AlarmSettings{Sound=o.Sound,MuteInDuties=o.MuteInDuties,GameIncludeWeather=true}:new());}
                        Tip("Alarms.SendGroupHelp");
                    }
                        // R-12 (0.319): this group's alarms on Discord, shown while the webhook is on.
                        if(configuration.Discord is { Enabled:true })
                        {
                            ImGui.SameLine();
                            // 0.584 (user request): a reminder's alarms are set in Reminders only, no second box here.
                            if(members.All(alarm=>DiscordWebhook.ReminderRowOf(alarm).Length>0))
                            {
                                ImGui.TextDisabled(localization.Get("Discord.SetInReminders"));
                            }
                            else
                            {
                                var toDiscord=members.All(alarm=>alarm.Discord);
                                if(ImGui.Checkbox(localization.Get("Discord.GroupOn")+"##GroupDiscord",ref toDiscord)){foreach(var alarm in members)alarm.Discord=toDiscord;changed=true;}
                                Tip("Discord.GroupOnTip");
                            }
                        }
                        if(first.ItemId!=0){ImGui.SameLine();if(ImGui.SmallButton(localization.Get("Alarms.OpenItem"))){SelectItem(first.ItemId);OpenItemSheet();}}
                        if(first.TerritoryId!=0){ImGui.SameLine();if(ImGui.SmallButton(localization.Get("Navigation.Weather"))){OpenWeather(first.TerritoryId,maps.State.Catalog?.Entries.FirstOrDefault(map=>map.TerritoryId==first.TerritoryId)?.Zone ?? "");}}
                        if(ImGui.TreeNode(localization.Format("Alarms.Slots",members.Length)+"###Slots"))
                        {
                            try
                            {
                                // R-12: the group's own Discord message (empty = the template of the settings), saved when the field is left.
                                if(configuration.Discord is { Enabled:true })
                                {
                                    // Typed into a buffer; the alarms take it only when the field is left (or no longer edited),
                                    // so a ringing alarm never posts a half-typed message.
                                    // 0.331: keyed by section and group (armKey), as the same item can have a group in two job sections.
                                    var message=discordGroupEdits.TryGetValue(armKey,out var editing) ? editing:first.DiscordMessage;
                                    ImGui.SetNextItemWidth(Math.Min(360,ImGui.GetContentRegionAvail().X));
                                    if(ImGui.InputTextWithHint(localization.Get("Discord.GroupMessage")+"##GroupDiscordMessage",localization.Get("Discord.GroupMessageHint"),ref message,300))
                                        discordGroupEdits[armKey]=message;
                                    if((ImGui.IsItemDeactivatedAfterEdit() || !ImGui.IsItemActive()) && discordGroupEdits.Remove(armKey,out var final))
                                    {foreach(var alarm in members)alarm.DiscordMessage=final.Trim();changed=true;}
                                    Tip("Discord.GroupMessageTip");
                                }
                                foreach(var alarm in members.OrderBy(alarm=>alarm.Repeat==AlarmRepeat.EorzeaDaily ? alarm.EtMinute:0).ThenBy(alarm=>alarm.At))
                                {
                                    ImGui.PushID(alarm.Id.ToString());
                                    try
                                    {
                                        var enabled=alarm.Enabled;
                                        if(ImGui.Checkbox("##On",ref enabled)){alarm.Enabled=enabled;changed=true;NoteGameCopy([alarm]);}
                                        Tip("SettingTips.AlarmOn");
                                        ImGui.SameLine();
                                        var at=NextAlarm(alarm,now);
                                        ImGui.TextColored(alarm.Enabled ? Success:Muted,AlarmWhen(alarm)+(at is { } when ? " · "+localization.Format("Alarms.Next",when.ToLocalTime().ToString("t"),Math.Max(0,(int)(when-now).TotalMinutes)):""));
                                        if(ImGui.IsItemHovered())WrappedTooltip(alarm.Label+"\n"+string.Join("\n",AlarmSchedule.Occurrences(alarm,now,5).Select(time=>time.ToLocalTime().ToString("g"))));
                                        ImGui.SameLine();ImGui.SetNextItemWidth(90);
                                        var lead=alarm.LeadMinutes;
                                        if(ImGui.InputInt(localization.Get("Alarms.Lead")+"##Lead",ref lead,1)){alarm.LeadMinutes=AlarmSchedule.ClampLead(alarm.Repeat,lead);changed=true;NoteGameCopy([alarm]);}
                                        Tip("SettingTips.AlarmLead");
                                        if(alarm.GameName.Length>0)
                                        {
                                            ImGui.SameLine();
                                            var gameList=alarmService?.GameAlarms;
                                            var present=gameList?.Any(game=>game.Name==alarm.GameName)==true;
                                            ImGui.TextColored(present ? Success:Accent,localization.Get(present ? "Alarms.InGame":"Alarms.SentNotSeen"));
                                            if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("Alarms.InGameHelp",alarm.GameName));
                                            // 0.331: the copy is not in the game's list as read (deleted there, or the command ignored): on the player's click,
                                            // Tensho rings the alarm again and « Send to the game » comes back (it offers alarms without a game name).
                                            if(gameList is not null && !present)
                                            {
                                                ImGui.SameLine();
                                                if(ImGui.SmallButton(localization.Get("Alarms.RingInTensho")+"##RingInTensho")){alarm.GameName="";changed=true;}
                                                Tip("Alarms.RingInTenshoTip");
                                            }
                                        }
                                        ImGui.SameLine();
                                        // 0.557 (T19): name and date on the player's own alarms (an automatic alarm follows its deadline), and « remind me later ».
                                        if(alarm.AutoKey.Length==0 && ImGui.SmallButton(localization.Get("Alarms.Edit")+"##AlarmEdit"))alarmEditing=alarmEditing==alarm.Id ? null:alarm.Id;
                                        if(alarm.AutoKey.Length==0){Tip("Alarms.EditTip");ImGui.SameLine();}
                                        var snooze=configuration.AlarmOptions?.SnoozeMinutes ?? 5;
                                        if(ImGui.SmallButton(localization.Format("Alarms.Snooze",snooze)+"##AlarmSnooze"))added.Add(AlarmSchedule.Snooze(alarm,now,snooze));
                                        Tip("Alarms.SnoozeTip");
                                        ImGui.SameLine();
                                        if(ImGui.SmallButton(localization.Get("Alarms.Delete")))remove.Add(alarm);
                                        if(alarmEditing==alarm.Id)DrawAlarmEdit(alarm,now,ref changed);
                                        Tip("Alarms.DeleteTip");
                                    }
                                    finally{ImGui.PopID();}
                                }
                            }
                            finally{ImGui.TreePop();}
                        }
                    }
                    finally{ImGui.EndGroup();}
                }
                finally{ImGui.PopID();}
            }
        }
        if(remove.Any(alarm=>alarm.GameName.Length>0))alarmStatus=localization.Get("Alarms.DeleteInGame");
        foreach(var alarm in remove){alarms.Remove(alarm);changed=true;}
        if(alarmService?.GameStatus is { } gameState)
            ImGui.TextColored(gameState is "Done" or "Sending" ? Success:Accent,localization.Format("Alarms.GameState"+gameState,alarmService.Pending,GameAlarmCommand.GameMaximum));
        // Manual daily alarm at an ET time.
        ImGui.Separator();
        ImGui.TextColored(Accent,localization.Get("Alarms.ManualTitle"));
        ImGui.SetNextItemWidth(160);ImGui.InputTextWithHint("##AlarmLabel",localization.Get("Alarms.ManualLabel"),ref alarmManualLabel,40);
        Tip("Alarms.ManualLabelTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(70);ImGui.InputInt("##AlarmHour",ref alarmManualHour,0);
        Tip("Alarms.ManualHourTip");
        ImGui.SameLine();ImGui.TextUnformatted(":");ImGui.SameLine();ImGui.SetNextItemWidth(70);ImGui.InputInt("ET##AlarmMinute",ref alarmManualMinute,0);
        Tip("Alarms.ManualMinuteTip");
        alarmManualHour=Math.Clamp(alarmManualHour,0,23);alarmManualMinute=Math.Clamp(alarmManualMinute,0,59);
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("Alarms.AddDaily")))
            AddAlarms(AlarmSchedule.ForWindows(alarmManualLabel.Trim().Length>0 ? alarmManualLabel.Trim():localization.Get("Alarms.ManualDefault"),[alarmManualHour*60+alarmManualMinute],DefaultAlarmLead,0,0));
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("Alarms.AddDailyTip",AlarmSchedule.ClampLead(AlarmRepeat.EorzeaDaily,DefaultAlarmLead)));
        // 0.386 (g, X-10): the same label at a real local date, once.
        DrawManualDateAlarm();
        if(added.Count>0)AddAlarms(added);
        if(changed)
            try{save();}catch(Exception error){log.Warning(error,"Alarms could not be saved.");alarmStatus=localization.Get("Alarms.SaveFailed");}
        DrawGameAlarms();
    }

    // Short link to the Alarms module, where alarms are created.
    private void DrawAlarmsLink()
    {
        var count=configuration.Alarms?.Count ?? 0;
        if(ImGui.SmallButton(localization.Format("Alarms.Title",count)+"##AlarmsLink"))page=Page.Alarms;
        if(alarmStatus.Length>0){ImGui.SameLine();ImGui.TextColored(Accent,alarmStatus);}
    }

    private void DrawGameAlarms()
    {
        if(!ImGui.TreeNode(localization.Get("Alarms.GameTitle")+"###GameAlarms"))return;
        try
        {
            ImGui.TextWrapped(localization.Get("Alarms.GameHelp"));
            if(ImGui.SmallButton(localization.Get("Alarms.OpenGame")))alarmService?.OpenGameWindow();
            if(alarmService?.GameAlarms is not { } game){ImGui.TextDisabled(localization.Get("Alarms.GameUnavailable"));return;}
            if(game.Count==0)ImGui.TextDisabled(localization.Get("Alarms.GameNone"));
            foreach(var alarm in game)
                ImGui.TextUnformatted($"{alarm.Name} · {alarm.Hour:00}:{alarm.Minute:00} {localization.Get("Alarms.Clock"+Math.Clamp(alarm.ClockTime,0,2))}"
                    +(alarm.Repeat ? " · "+localization.Get("Alarms.Repeat"):"")+(alarm.Reminder>0 ? " · "+localization.Format("Alarms.Reminder",alarm.Reminder):""));
            // Native fields needed before Tensho may write a game alarm (0.202): copy them for the developer.
            if(game.Count>0 && ImGui.SmallButton(localization.Get("Alarms.CopyRaw")))
                ImGui.SetClipboardText(string.Join(Environment.NewLine,game.Select(alarm=>$"name={alarm.Name} index={alarm.Index} clock={alarm.ClockTime} {alarm.Hour:00}:{alarm.Minute:00} reminder={alarm.Reminder} sound={alarm.Sound} timestamp={alarm.Timestamp} flags={alarm.Flags} captured={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}")));
        }
        finally{ImGui.TreePop();}
    }

    // Settings › Modules › Alarms.
    private void DrawAlarmSettings()
    {
        var options=configuration.AlarmOptions??=new();
        var lead=options.LeadMinutes;var sound=options.Sound;var play=options.PlaySound;var chat=options.Chat;var mute=options.MuteInDuties;var changed=false;
        var useGame=options.UseGameAlarms;var gameWeather=options.GameIncludeWeather;var muteChat=options.MuteChatInDuties;
        // 0.324: « ↺ » after each option.
        var defaults=DefaultConfiguration.AlarmOptions;
        // X-76 H3 (0.914): nothing is written into the saved options before their save (a failed save could not bring them back).
        changed|=AlarmNumber(localization.Get("Alarms.DefaultLead"),"AlarmLeadMinutes","Advanced.LeadTip",lead,0,1440,out lead);
        if(DefaultButton("AlarmLead",lead!=defaults.LeadMinutes)){lead=defaults.LeadMinutes;changed=true;}
        changed|=ImGui.Checkbox(localization.Get("Alarms.PlaySound"),ref play);Tip("Advanced.PlaySoundTip");
        if(DefaultButton("AlarmPlaySound",play!=defaults.PlaySound)){play=defaults.PlaySound;changed=true;}
        ImGui.SetNextItemWidth(160);
        if(ImGui.BeginCombo(localization.Get("Alarms.Sound"),localization.Get("Alarms.Sound"+Math.Clamp(sound,0,5))))
        {
            try{for(var index=0;index<6;index++)if(ImGui.Selectable(localization.Get("Alarms.Sound"+index),sound==index)){sound=index;changed=true;}}
            finally{ImGui.EndCombo();}
        }
        Tip("Advanced.SoundTip");
        ImGui.SameLine();if(ImGui.SmallButton(localization.Get("Alarms.Try")))alarmService?.PreviewSound(sound);Tip("Advanced.TryTip");
        if(DefaultButton("AlarmSound",sound!=defaults.Sound)){sound=defaults.Sound;changed=true;}
        changed|=ImGui.Checkbox(localization.Get("Alarms.Chat"),ref chat);Tip("Advanced.ChatTip");
        if(DefaultButton("AlarmChat",chat!=defaults.Chat)){chat=defaults.Chat;changed=true;}
        changed|=AlarmNumber(localization.Get("Alarms.SnoozeMinutes"),"AlarmSnoozeMinutes","Advanced.SnoozeMinutesTip",options.SnoozeMinutes,1,1440,out var snooze);
        if(DefaultButton("AlarmSnooze",snooze!=defaults.SnoozeMinutes)){snooze=defaults.SnoozeMinutes;changed=true;}
        var onTime=options.RingOnTime;
        changed|=ImGui.Checkbox(localization.Get("Alarms.RingOnTime"),ref onTime);
        Tip("Advanced.RingOnTimeTip");
        if(DefaultButton("AlarmRingOnTime",onTime!=defaults.RingOnTime)){onTime=defaults.RingOnTime;changed=true;}
        changed|=ImGui.Checkbox(localization.Get("Alarms.MuteInDuties"),ref mute);Tip("Advanced.MuteTip");
        if(DefaultButton("AlarmMute",mute!=defaults.MuteInDuties)){mute=defaults.MuteInDuties;changed=true;}
        // Queue step e (audit 2026-11-02): no [Tensho] chat line in a duty either; greyed while it can have no effect (no silence, no chat line).
        var muteChatOff=!mute || !chat;
        ImGui.Indent();
        if(muteChatOff)ImGui.BeginDisabled();
        try
        {
            changed|=ImGui.Checkbox(localization.Get("Alarms.MuteChatInDuties")+"###AlarmMuteChat",ref muteChat);Tip("Advanced.MuteChatTip");
            if(DefaultButton("AlarmMuteChat",muteChat!=defaults.MuteChatInDuties)){muteChat=defaults.MuteChatInDuties;changed=true;}
        }
        finally{if(muteChatOff)ImGui.EndDisabled();ImGui.Unindent();}
        // Game alarms through the game's « /alarm » command (0.205).
        changed|=ImGui.Checkbox(localization.Get("Alarms.UseGame"),ref useGame);
        Tip("Alarms.UseGameHelp");
        if(DefaultButton("AlarmUseGame",useGame!=defaults.UseGameAlarms)){useGame=defaults.UseGameAlarms;changed=true;}
        if(useGame)
        {
            ImGui.Indent();
            changed|=ImGui.Checkbox(localization.Get("Alarms.GameWeather"),ref gameWeather);Tip("Advanced.GameWeatherTip");
            if(DefaultButton("AlarmGameWeather",gameWeather!=defaults.GameIncludeWeather)){gameWeather=defaults.GameIncludeWeather;changed=true;}
            ImGui.Unindent();
        }
        // 0.366 (g, C-10): duty ready alert, saved on its own (kept when the options above are saved).
        BoolSetting("Alarms.DutyPop","Alarms.DutyPopTip",()=>configuration.AlarmOptions?.DutyPopAlert==true,
            value=>(configuration.AlarmOptions??=new()).DutyPopAlert=value,c=>c.AlarmOptions?.DutyPopAlert==true);
        // 0.371 (g, M-01): automatic alarms, saved on its own (kept when the options above are saved).
        BoolSetting("AutoAlarms.Setting","AutoAlarms.SettingTip",()=>configuration.AlarmOptions?.AutoAlarms!=false,
            value=>(configuration.AlarmOptions??=new()).AutoAlarms=value,c=>c.AlarmOptions?.AutoAlarms!=false);
        // R-12 (0.319): Discord webhook, saved on its own.
        DrawDiscordSettings();
        if(page!=Page.Alarms && ImGui.SmallButton(localization.Get("Alarms.Open"))){page=Page.Alarms;}
        ImGui.TextDisabled(localization.Get("Alarms.SettingsHelp"));
        if(!changed)return;
        // 0.319: the command delay (Settings › Advanced) is kept; it went back to 500 ms whenever an alarm setting changed. X-76 H3 (0.914):
        // a copy keeps it with every other field (duty ready alert, automatic alarms, and any field added later).
        var next=configuration.AlarmOptions.Copy();
        next.LeadMinutes=Math.Clamp(lead,0,1440);next.Sound=Math.Clamp(sound,0,5);next.PlaySound=play;next.Chat=chat;next.MuteInDuties=mute;next.MuteChatInDuties=muteChat;
        next.UseGameAlarms=useGame;next.GameIncludeWeather=gameWeather;next.SnoozeMinutes=Math.Clamp(snooze,1,1440);next.RingOnTime=onTime;
        SaveAlarmOptions(next);
    }

    /// <summary>X-76 H3 (0.914): the edited copy replaces the saved options; when saving fails the untouched previous object comes back.</summary>
    private void SaveAlarmOptions(AlarmSettings next)
    {
        var previous=configuration.AlarmOptions;
        configuration.AlarmOptions=next;
        TrySave("Alarm settings",()=>configuration.AlarmOptions=previous);
    }

    // X-76 H3 (0.914): a number typed in the alarm options waits here until its field is left, so the saved options are never changed
    // before their save; one field is active at a time.
    private (string Id,int Value)? alarmTyped;

    /// <summary>An alarm option number field (+/- buttons) with its tooltip: the value shown (typed or saved), true once when the field is left after an edit.</summary>
    private bool AlarmNumber(string label,string id,string tipKey,int saved,int min,int max,out int value)
    {
        value=alarmTyped is { } typed && typed.Id==id ? typed.Value:saved;
        ImGui.SetNextItemWidth(100);
        if(ImGui.InputInt(label+"###"+id,ref value,1)){value=Math.Clamp(value,min,max);alarmTyped=(id,value);}
        Tip(tipKey);
        var done=ImGui.IsItemDeactivatedAfterEdit();
        if((done || !ImGui.IsItemActive()) && alarmTyped?.Id==id)alarmTyped=null;
        return done;
    }
}
