using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.Alarms;
using TenshoAssistant.Community;
using TenshoAssistant.Gathering;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

/// <summary>
/// Tensho alarms (0.201): created from the weather page, fishing windows and gathering windows — once, or every ET day at each window start
/// with the weather the period must have — listed and edited on the weather page; they ring with the game's alarm sound (AlarmService).
/// </summary>
internal sealed partial class MainWindow
{
    private AlarmService? alarmService;
    internal void ConfigureAlarms(AlarmService service)=>alarmService=service;
    private string alarmStatus="";private string alarmManualLabel="";private int alarmManualHour=8,alarmManualMinute;

    private int DefaultAlarmLead=>configuration.AlarmOptions?.LeadMinutes ?? 5;

    private void AddAlarms(IReadOnlyList<TenshoAlarm> added)
    {
        configuration.Alarms??=[];
        if(added.Count==0){alarmStatus=localization.Get("Alarms.NoWindow");return;}
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

    // Bell button that creates the alarms; the tooltip says what will be created.
    private void AlarmButton(string id,string tooltip,Func<IReadOnlyList<TenshoAlarm>> create)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        bool clicked;
        try{clicked=ImGui.SmallButton(FontAwesomeIcon.Bell.ToIconString()+"##Alarm"+id);}
        finally{ImGui.PopFont();}
        if(ImGui.IsItemHovered())WrappedTooltip(tooltip+"\n"+localization.Format("Alarms.LeadHint",DefaultAlarmLead));
        if(clicked)AddAlarms(create());
    }

    // Weather ids for community weather names of a zone; null when one name is not proven in this zone (condition then not checked).
    private static IReadOnlyList<uint>? WeatherIds(WeatherCatalog catalog,IReadOnlyList<string>? names,uint territory)
    {
        if(names is not { Count:>0 })return [];
        var ids=names.Select(name=>catalog.WeatherBySourceName(name,territory)?.Id).ToArray();
        return ids.All(id=>id is not null) ? ids.Select(id=>id!.Value).Distinct().ToArray():null;
    }

    // Fishing: one daily alarm per ET window start; with weather but no time window, the starts of the three weather periods (00, 08, 16 ET).
    private void DrawFishingAlarmButton(string id,FishingConditions conditions,uint territory)
    {
        if(weather.State.Catalog is not { } catalog)return;
        var weathers=WeatherIds(catalog,conditions.Weather,territory);var previous=WeatherIds(catalog,conditions.PreviousWeather,territory);
        var starts=conditions.Timing.Kind==GatheringScheduleKind.Limited ? conditions.Timing.Windows.Select(window=>window.StartMinute).ToArray()
            :(weathers is { Count:>0 } || previous is { Count:>0 }) ? [0,480,960]:[];
        if(starts.Length==0)return;
        var unproven=weathers is null || previous is null;
        var label=SelectedItem?.Name ?? itemId.ToString();
        AlarmButton(id,localization.Format("Alarms.FishTooltip",starts.Length)+(unproven ? "\n"+localization.Get("Alarms.WeatherUnproven"):""),
            ()=>AlarmSchedule.ForWindows(label,starts,DefaultAlarmLead,territory,(uint)itemId,weathers ?? [],previous ?? []).Select(created=>{created.JobId=18;return created;}).ToArray());
        ImGui.SameLine();ImGui.TextDisabled(localization.Format("Alarms.Windows",starts.Length));
    }

    private void DrawGatheringAlarmButton(string id,GatheringSource source)
    {
        var timing=source.Timing;
        if(timing.Kind!=GatheringScheduleKind.Limited || timing.Windows.Count==0)return;
        var place=string.IsNullOrWhiteSpace(source.Place) ? source.Zone:source.Place;
        var label=$"{SelectedItem?.Name ?? itemId.ToString()} · {place}";
        var job=AlarmSchedule.JobOf(source.TypeId,source.IsFishing);
        AlarmButton(id,localization.Format("Alarms.GatherTooltip",timing.Windows.Count)+(job!=0 ? "\n"+JobName(job):""),
            ()=>AlarmSchedule.ForWindows(label,timing.Windows.Select(window=>window.StartMinute),DefaultAlarmLead,source.TerritoryId,(uint)itemId)
                .Select(created=>{created.JobId=job;return created;}).ToArray());
    }

    // Gathering job names from the game (ClassJob), given by the plugin.
    private Func<uint,string?>? jobName;
    internal void ConfigureJobNames(Func<uint,string?> names)=>jobName=names;
    private string JobName(uint job)=>jobName?.Invoke(job) is { Length:>0 } name ? char.ToUpper(name[0],System.Globalization.CultureInfo.CurrentCulture)+name[1..]:job.ToString();
    // Job of an alarm: the one stored, else the item's gathering sources (alarms created before 0.203); 0 when none or several.
    private uint AlarmJob(TenshoAlarm alarm)
    {
        if(alarm.JobId!=0)return alarm.JobId;
        if(alarm.ItemId==0 || gatheringCatalog.State.Catalog?.ForItem(alarm.ItemId) is not { Count:>0 } sources)return 0;
        var jobs=sources.Select(source=>AlarmSchedule.JobOf(source.TypeId,source.IsFishing)).Where(job=>job!=0).Distinct().ToArray();
        return jobs.Length==1 ? jobs[0]:0;
    }

    // Vista (sightseeing log): daily alarm at the start of its ET window, with the weathers the wiki gives when they are proven in the zone.
    private TenshoAlarm? VistaAlarm(MapLocalPosition vista,uint territory)
    {
        if(weather.State.Catalog is not { } catalog)return null;
        var weathers=WeatherIds(catalog,WikiVistaFor(vista)?.Weathers,territory) ?? [];
        var hasTime=vista.MinTime!=0 || vista.MaxTime!=0;
        if(!hasTime && weathers.Count==0)return null;
        var start=vista.MinTime/100*60+vista.MinTime%100;
        return new TenshoAlarm{Label=localization.Format("MapView.Vista",vista.Name),Group=localization.Format("MapView.Vista",vista.Name),Repeat=AlarmRepeat.EorzeaDaily,EtMinute=hasTime ? start:0,
            LeadMinutes=AlarmSchedule.ClampLead(AlarmRepeat.EorzeaDaily,DefaultAlarmLead),TerritoryId=territory,Weathers=[..weathers]};
    }
    // Next real time the vista's ET window opens with a matching weather (within 40 ET days), or null; cached per vista and minute.
    private (uint Id,long Minute,DateTimeOffset? At) vistaNextCache;
    private DateTimeOffset? NextVistaWindow(MapLocalPosition vista,uint territory)
    {
        var minute=DateTimeOffset.UtcNow.ToUnixTimeSeconds()/60;
        if(vistaNextCache.Id==vista.Id && vistaNextCache.Minute==minute)return vistaNextCache.At;
        DateTimeOffset? found=null;
        if(VistaAlarm(vista,territory) is { } alarm)
        {
            var catalog=weather.State.Catalog;
            var starts=alarm.Weathers.Count>0 && vista.MinTime==0 && vista.MaxTime==0 ? [0,480,960]:new[]{alarm.EtMinute};
            found=starts.SelectMany(start=>AlarmSchedule.Occurrences(new TenshoAlarm{Repeat=AlarmRepeat.EorzeaDaily,EtMinute=start},DateTimeOffset.UtcNow.AddMinutes(-1),40))
                .Where(time=>alarm.Weathers.Count==0 || AlarmService.WeatherAt(catalog,territory,time) is { } id && alarm.Weathers.Contains(id))
                .Order().Select(time=>(DateTimeOffset?)time).FirstOrDefault();
        }
        vistaNextCache=(vista.Id,minute,found);
        return found;
    }
    private void DrawVistaAlarmEntry(MapLocalPosition vista,uint territory)
    {
        if(VistaAlarm(vista,territory) is not { } alarm){ImGui.TextDisabled(localization.Get("Alarms.VistaNone"));return;}
        var starts=alarm.Weathers.Count>0 && vista.MinTime==0 && vista.MaxTime==0 ? new[]{0,480,960}:new[]{alarm.EtMinute};
        if(ImGui.Selectable(localization.Format("Alarms.VistaAdd",vista.Name)+"##VistaAlarm"))
            AddAlarms(AlarmSchedule.ForWindows(alarm.Label,starts,alarm.LeadMinutes,territory,0,alarm.Weathers).Select(created=>{created.Group=alarm.Group;return created;}).ToArray());
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("Alarms.LeadHint",DefaultAlarmLead));
    }

    private string AlarmWhen(TenshoAlarm alarm)=>alarm.Repeat==AlarmRepeat.EorzeaDaily
        ? localization.Format("Alarms.Daily",$"{alarm.EtMinute/60:00}:{alarm.EtMinute%60:00}")
        : alarm.At.ToLocalTime().ToString("g");

    // Alarm group (0.202): the item, or the weather / vista / name the alarm was created for, with its icon.
    private static string AlarmGroupKey(TenshoAlarm alarm)=>alarm.ItemId!=0 ? $"item:{alarm.ItemId}":alarm.Group.Length>0 ? alarm.Group
        :System.Text.RegularExpressions.Regex.Replace(alarm.Label,@" · \d\d:\d\d ET$","");
    private void DrawAlarmGroupIcon(TenshoAlarm first,string title)
    {
        var size=new Vector2(32);
        if(first.ItemId!=0 && itemCatalog.State.Catalog?.Find(first.ItemId) is { } item)
        {DrawItemIcon(item,size);if(ImGui.IsItemHovered())DrawItemInfoTooltip(item.Id);return;}
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
        ImGui.TextWrapped(localization.Get("Alarms.Help"));
        if(alarmStatus.Length>0)ImGui.TextColored(Accent,alarmStatus);
        if(ImGui.CollapsingHeader(localization.Get("Alarms.SettingsTitle")+"###AlarmSettingsPage"))DrawAlarmSettings();
        var now=DateTimeOffset.UtcNow;var changed=false;var remove=new List<TenshoAlarm>();
        if(alarms.Count==0)ImGui.TextDisabled(localization.Get("Alarms.Empty"));
        // Sections by gathering job (0.203: miner, botanist, fisher, then the other alarms), groups ordered by their next ringing time.
        foreach(var section in alarms.GroupBy(AlarmJob).OrderBy(section=>section.Key==0 ? uint.MaxValue:section.Key))
        {
            var sectionTitle=section.Key==0 ? localization.Get("Alarms.OtherSection"):JobName(section.Key);
            ImGui.Spacing();
            // ClassJob icon 62100 + job (62116 miner, 62117 botanist, 62118 fisher), as the gathering catalogue uses for fishing.
            if(section.Key!=0){DrawItemIcon(new Items.ItemDefinition(0,sectionTitle,false,62100+section.Key),new Vector2(ImGui.GetFrameHeight()));ImGui.SameLine();}
            if(!ImGui.CollapsingHeader($"{sectionTitle} ({section.Count()})###JobSection{section.Key}",ImGuiTreeNodeFlags.DefaultOpen))continue;
            foreach(var group in section.GroupBy(AlarmGroupKey).OrderBy(group=>group.Min(alarm=>alarm.Enabled ? AlarmSchedule.Next(alarm,now) ?? DateTimeOffset.MaxValue:DateTimeOffset.MaxValue)))
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
                        var next=members.Where(alarm=>alarm.Enabled).Select(alarm=>AlarmSchedule.Next(alarm,now)).OfType<DateTimeOffset>().OrderBy(time=>time).FirstOrDefault();
                        ImGui.TextColored(members.Any(alarm=>alarm.Enabled) ? Accent:Muted,title);
                        ImGui.TextDisabled(localization.Format("Alarms.GroupLine",members.Length)
                            +(next!=default ? " · "+localization.Format("Alarms.Next",next.ToLocalTime().ToString("t"),Math.Max(0,(int)(next-now).TotalMinutes)):"")
                            +(members.Any(alarm=>alarm.Weathers.Count+alarm.PreviousWeathers.Count>0) ? " · "+localization.Get("Alarms.WeatherCondition"):""));
                        var all=members.All(alarm=>alarm.Enabled);
                        if(ImGui.Checkbox(localization.Get("Alarms.GroupOn")+"##GroupOn",ref all)){foreach(var alarm in members)alarm.Enabled=all;changed=true;}
                        ImGui.SameLine();ImGui.SetNextItemWidth(90);
                        var groupLead=first.LeadMinutes;
                        if(ImGui.InputInt(localization.Get("Alarms.Lead")+"##GroupLead",ref groupLead,1)){foreach(var alarm in members)alarm.LeadMinutes=AlarmSchedule.ClampLead(alarm.Repeat,groupLead);changed=true;}
                        ImGui.SameLine();
                        if(ImGui.SmallButton(localization.Get("Alarms.DeleteGroup")))remove.AddRange(members);
                    if(members.Any(alarm=>alarm.Repeat==AlarmRepeat.EorzeaDaily && alarm.GameName.Length==0))
                    {
                        ImGui.SameLine();
                        if(ImGui.SmallButton(localization.Get("Alarms.SendGroup"))){alarmStatus="";SendAlarmsToGame(members,(configuration.AlarmOptions ?? new()) is var o ? new AlarmSettings{Sound=o.Sound,MuteInDuties=o.MuteInDuties,GameIncludeWeather=true}:new());}
                        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("Alarms.SendGroupHelp"));
                    }
                        if(first.ItemId!=0){ImGui.SameLine();if(ImGui.SmallButton(localization.Get("Alarms.OpenItem"))){SelectItem(first.ItemId);page=Page.Items;}}
                        if(first.TerritoryId!=0){ImGui.SameLine();if(ImGui.SmallButton(localization.Get("Navigation.Weather"))){OpenWeather(first.TerritoryId,maps.State.Catalog?.Entries.FirstOrDefault(map=>map.TerritoryId==first.TerritoryId)?.Zone ?? "");}}
                        if(ImGui.TreeNode(localization.Format("Alarms.Slots",members.Length)+"###Slots"))
                        {
                            try
                            {
                                foreach(var alarm in members.OrderBy(alarm=>alarm.Repeat==AlarmRepeat.EorzeaDaily ? alarm.EtMinute:0).ThenBy(alarm=>alarm.At))
                                {
                                    ImGui.PushID(alarm.Id.ToString());
                                    try
                                    {
                                        var enabled=alarm.Enabled;
                                        if(ImGui.Checkbox("##On",ref enabled)){alarm.Enabled=enabled;changed=true;}
                                        ImGui.SameLine();
                                        var at=AlarmSchedule.Next(alarm,now);
                                        ImGui.TextColored(alarm.Enabled ? Success:Muted,AlarmWhen(alarm)+(at is { } when ? " · "+localization.Format("Alarms.Next",when.ToLocalTime().ToString("t"),Math.Max(0,(int)(when-now).TotalMinutes)):""));
                                        if(ImGui.IsItemHovered())WrappedTooltip(alarm.Label+"\n"+string.Join("\n",AlarmSchedule.Occurrences(alarm,now,5).Select(time=>time.ToLocalTime().ToString("g"))));
                                        ImGui.SameLine();ImGui.SetNextItemWidth(90);
                                        var lead=alarm.LeadMinutes;
                                        if(ImGui.InputInt(localization.Get("Alarms.Lead")+"##Lead",ref lead,1)){alarm.LeadMinutes=AlarmSchedule.ClampLead(alarm.Repeat,lead);changed=true;}
                                        if(alarm.GameName.Length>0)
                                        {
                                            ImGui.SameLine();
                                            var present=alarmService?.GameAlarms?.Any(game=>game.Name==alarm.GameName)==true;
                                            ImGui.TextColored(present ? Success:Accent,localization.Get(present ? "Alarms.InGame":"Alarms.SentNotSeen"));
                                            if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("Alarms.InGameHelp",alarm.GameName));
                                        }
                                        ImGui.SameLine();
                                        if(ImGui.SmallButton(localization.Get("Alarms.Delete")))remove.Add(alarm);
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
        ImGui.SameLine();ImGui.SetNextItemWidth(70);ImGui.InputInt("##AlarmHour",ref alarmManualHour,0);
        ImGui.SameLine();ImGui.TextUnformatted(":");ImGui.SameLine();ImGui.SetNextItemWidth(70);ImGui.InputInt("ET##AlarmMinute",ref alarmManualMinute,0);
        alarmManualHour=Math.Clamp(alarmManualHour,0,23);alarmManualMinute=Math.Clamp(alarmManualMinute,0,59);
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("Alarms.AddDaily")))
            AddAlarms(AlarmSchedule.ForWindows(alarmManualLabel.Trim().Length>0 ? alarmManualLabel.Trim():localization.Get("Alarms.ManualDefault"),[alarmManualHour*60+alarmManualMinute],DefaultAlarmLead,0,0));
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
        var useGame=options.UseGameAlarms;var gameWeather=options.GameIncludeWeather;
        ImGui.SetNextItemWidth(100);changed|=ImGui.InputInt(localization.Get("Alarms.DefaultLead"),ref lead,1);Tip("Advanced.LeadTip");
        changed|=ImGui.Checkbox(localization.Get("Alarms.PlaySound"),ref play);Tip("Advanced.PlaySoundTip");
        ImGui.SetNextItemWidth(160);
        if(ImGui.BeginCombo(localization.Get("Alarms.Sound"),localization.Get("Alarms.Sound"+Math.Clamp(sound,0,5))))
        {
            try{for(var index=0;index<6;index++)if(ImGui.Selectable(localization.Get("Alarms.Sound"+index),sound==index)){sound=index;changed=true;}}
            finally{ImGui.EndCombo();}
        }
        Tip("Advanced.SoundTip");
        ImGui.SameLine();if(ImGui.SmallButton(localization.Get("Alarms.Try")))alarmService?.PreviewSound(sound);Tip("Advanced.TryTip");
        changed|=ImGui.Checkbox(localization.Get("Alarms.Chat"),ref chat);Tip("Advanced.ChatTip");
        changed|=ImGui.Checkbox(localization.Get("Alarms.MuteInDuties"),ref mute);Tip("Advanced.MuteTip");
        // Game alarms through the game's « /alarm » command (0.205).
        changed|=ImGui.Checkbox(localization.Get("Alarms.UseGame"),ref useGame);
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("Alarms.UseGameHelp"));
        if(useGame){ImGui.Indent();changed|=ImGui.Checkbox(localization.Get("Alarms.GameWeather"),ref gameWeather);Tip("Advanced.GameWeatherTip");ImGui.Unindent();}
        if(page!=Page.Alarms && ImGui.SmallButton(localization.Get("Alarms.Open"))){page=Page.Alarms;}
        ImGui.TextDisabled(localization.Get("Alarms.SettingsHelp"));
        if(!changed)return;
        var previous=configuration.AlarmOptions;
        configuration.AlarmOptions=new(){LeadMinutes=Math.Clamp(lead,0,1440),Sound=Math.Clamp(sound,0,5),PlaySound=play,Chat=chat,MuteInDuties=mute,
            UseGameAlarms=useGame,GameIncludeWeather=gameWeather};
        try{save();}catch(Exception error){configuration.AlarmOptions=previous;log.Warning(error,"Alarm settings could not be saved.");}
    }
}
