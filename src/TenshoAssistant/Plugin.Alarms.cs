using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1932, GPT review of 11/11, accepted by the user): the alarms' family out of Plugin.cs, after the free company's family (0.1931). The
// Discord notifier, the chat link handler's release, the alarm service and the automatic alarms' watcher are each created in one install path
// and released in one release path of the plugin's registrations, in this file; Plugin.cs calls the two blocks where they stood (their saves
// given in), so the order of the registrations is the one of before: the notifier, registered before the alarm service, is released after
// it. AlarmsFamily1932Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The Discord notifier, then the alarm service (its chat line, its Discord post); <paramref name="save"/> keeps the alarms. Returns the notifier.</summary>
    private Alarms.DiscordNotifier RegisterAlarms(Configuration configuration,IFramework framework,ICondition condition,IChatGui chat,IObjectTable objects,Action save)
    {
        // R-12 (0.319): Discord webhook posts on their own worker; registered before the alarm service so it is released after it.
        var discord=new Alarms.DiscordNotifier();
        registrations.Register(()=>{},discord.Dispose);
        mainWindow.ConfigureDiscord(discord);
        // X-574 (0.1793, the user's choice of 17/11): the alarms made before 0.319 (no stored source) are no longer given one by their group.
        Alarms.AlarmService? alarms=null;
        registrations.Register(()=>{},()=>{chat.RemoveChatLinkHandler();alarmLinks.Clear();});
        registrations.Register(()=>alarms=new Alarms.AlarmService(framework,condition,log,configuration,save,
            ()=>weather.State.Catalog,(alarm,line)=>
            {
                // « label|minutes|local time » from the service, localized here.
                var parts=line.Split('|');
                if(parts.Length<3)return;
                // R-12 (0.414): the game's map link to the alarm's gathering point, and « Open » (Tensho on the item), when known.
                var message=new Dalamud.Game.Text.SeStringHandling.SeStringBuilder()
                    .AddText("[Tensho] "+localization.Format("Alarms.Ring",string.Join('|',parts[..^2]),
                        // X-312 (0.1316): the minutes left in the common format (« 1 h 30 », « 17 j 2 h »), not « 90 min ».
                        int.TryParse(parts[^2],System.Globalization.NumberStyles.Integer,System.Globalization.CultureInfo.InvariantCulture,out var ringMinutes)
                            ? UI.TimeLeft.Write(TimeSpan.FromMinutes(Math.Max(0,ringMinutes)),(key,first,second)=>localization.Format(key,first,second)):parts[^2],
                        parts[^1]));
                // X-509 (review of 0.1490): the whole place link (arrow, place and coordinates, end), not AddMapLink's bare start.
                if(mainWindow.AlarmPlace(alarm) is { } place && Alarms.AlarmPlaceLink.Build(place.TerritoryId,place.MapId,place.X,place.Y,log) is { } placeLink)
                    message.AddText(" ").Append(placeLink);
                // 0.774 (X-03, user report and Dalamud's console in verbose): the game never handed the « Open in Tensho » link to Dalamud; it took it for an
                // item tooltip. The ring now carries the game's own item link: hovering shows the item, a right click opens its menu, where Tensho's entry is.
                if(alarm.ItemId is > 0 and < uint.MaxValue)
                    // 0.780 (X-03, user report: the right click did nothing on the grey hint, which was not part of the link): the hint is inside the link, so the
                    // whole line opens the item's menu.
                    // 0.812 (X-03, user report 21:01: no menu at all): the game's own item link, its own name, as in 0.774 (validated at 14:29). The 0.780 link
                    // with another text was no longer taken by the game as an item (no menu opened: docs/evidence/0.812-alarm-link.md). The hint stays beside it.
                    message.AddText(" ").AddItemLink(alarm.ItemId,false).AddUiForeground(" "+localization.Get("Alarms.RightClickTensho"),3);
                chat.Print(message.Build());
            },alarm=>
            {
                // An item alarm without its job (fish forecast, alarms before 0.203) takes it from the gathering catalogue, as the
                // Alarms page sorts it, so the Discord category matches; stored on the alarm, saved with its next change.
                if(alarm.JobId==0 && alarm.ItemId!=0)alarm.JobId=Alarms.AlarmSchedule.JobOfAlarm(alarm,item=>gatheringCatalog.State.Catalog?.ForItem(item));
                return Alarms.DiscordWebhook.ShouldSend(configuration.Discord,alarm);
            },(alarm,minutes,occurrence)=>
            {
                // Framework thread: the character's name is read here, only when the player allowed it.
                var settings=configuration.Discord;
                var character=settings.IncludeCharacter ? objects.LocalPlayer?.Name.TextValue:null;
                var content=Alarms.DiscordWebhook.Format(settings,localization.Get("Discord.DefaultTemplate"),alarm.DiscordMessage,alarm.Label,minutes,occurrence.ToLocalTime(),character,mainWindow.AlarmZoneName(alarm.TerritoryId),mainWindow.AlarmItemName(alarm.ItemId),
                    UI.TimeLeft.Write(TimeSpan.FromMinutes(Math.Max(0,minutes)),(key,first,second)=>localization.Format(key,first,second)));
                discord.Enqueue(new(Alarms.DiscordWebhook.UrlFor(settings,Alarms.DiscordWebhook.SourceOf(alarm)),Alarms.DiscordWebhook.Payload(settings,content)));
            }),()=>alarms?.Dispose());
        mainWindow.ConfigureAlarms(alarms!);
        if(alarms is not null)alarms.Gathered=mainWindow.AlarmGathered;
        return discord;
    }

    /// <summary>The automatic alarms at the ventures' end and the vessels' return; <paramref name="save"/> keeps them.</summary>
    private void RegisterAutoAlarms(IFramework framework,IClientState client,Configuration configuration,Action save)
    {
        Alarms.AutoAlarmWatcher? autoAlarms=null;
        registrations.Register(()=>autoAlarms=new Alarms.AutoAlarmWatcher(framework,client,log,configuration,save,localization.Get),
            ()=>autoAlarms?.Dispose());
    }
}
