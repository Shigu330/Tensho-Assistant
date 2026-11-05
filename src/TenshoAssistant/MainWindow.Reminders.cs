using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.Alarms;
using TenshoAssistant.Reminders;

namespace TenshoAssistant;

// R-08 (0.300, user request: « at least what the game's Calendar offers »): the Calendar's personal timers with their reset, what
// the game tells about them, a « done » box kept until the next reset, and an alarm button for the next time. Counters are read
// from the game once a second (GameTimers); reset times are the Calendar's (Reminders/ResetSchedule.cs). Rows: supply missions,
// custom deliveries, Fashion Report, allied society quests, leves, treasure maps (manual « opened » start), free company voyages
// (0.307, last workshop read; « Details » opens the Free company page) and retainer ventures (known once the list was opened).
internal sealed partial class MainWindow
{
    private GameTimerSnapshot? reminderTimers;
    // 0.321 (review 0.316, point 4): one read a second, ten seconds after a failure, and the failure logged once per message
    // (a lasting failure after a game update read and logged again on every frame).
    private DateTimeOffset reminderTimersTriedAt;
    private string? reminderTimersError;

    private void DrawReminders()
    {
        Heading(localization.Get("Reminders.Title"),localization.Get("Reminders.Description"));
        var now=DateTimeOffset.UtcNow;
        var wait=reminderTimersError is null ? TimeSpan.FromSeconds(1):TimeSpan.FromSeconds(10);
        if(now-reminderTimersTriedAt>=wait)
        {
            reminderTimersTriedAt=now;
            try{reminderTimers=GameTimers.Read(DailyRoulettes?.Invoke(),ChallengeRows?.Invoke());reminderTimersError=null;TickRemindersFromGame(reminderTimers);}
            catch(Exception error)
            {
                if(reminderTimersError!=error.Message)log.Warning(error,"Game timers could not be read.");
                reminderTimersError=error.Message;
            }
        }
        var timers=reminderTimers;
        if(!ImGui.BeginTable("Reminders",6,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH))return;
        try
        {
            foreach(var key in new[]{"Task","State","Next","Done","Bar",""})ImGui.TableSetupColumn(key.Length==0 ? "":localization.Get("Reminders.Column"+key),key.Length==0 ? ImGuiTableColumnFlags.WidthFixed:ImGuiTableColumnFlags.None,key.Length==0 ? 28:0);
            ImGui.TableHeadersRow();
            var supplyReset=ResetSchedule.NextDaily(now,ResetSchedule.SupplyHour);
            Row("Supply",FontAwesomeIcon.Shield,localization.Get("Reminders.SupplyState"),supplyReset,ResetSchedule.PreviousDaily(now,ResetSchedule.SupplyHour));
            var weekly=ResetSchedule.NextWeekly(now,ResetSchedule.WeeklyDay,ResetSchedule.WeeklyHour);var lastWeekly=ResetSchedule.PreviousWeekly(now,ResetSchedule.WeeklyDay,ResetSchedule.WeeklyHour);
            Row("Deliveries",FontAwesomeIcon.Box,timers?.DeliveriesLeft is { } left ? localization.Format("Reminders.DeliveriesLeft",left):localization.Get("Reminders.Unknown"),weekly,lastWeekly);
            Row("Fashion",FontAwesomeIcon.Tshirt,localization.Get("Reminders.FashionState"),weekly,lastWeekly);
            var daily=ResetSchedule.NextDaily(now,ResetSchedule.DailyHour);
            Row("Allied",FontAwesomeIcon.Handshake,timers?.AlliedQuestsLeft is { } allied ? localization.Format("Reminders.AlliedLeft",allied):localization.Get("Reminders.Unknown"),daily,ResetSchedule.PreviousDaily(now,ResetSchedule.DailyHour));
            Row("Leves",FontAwesomeIcon.Scroll,timers?.LeveAllowances is { } leves ? localization.Format("Reminders.LevesLeft",leves):localization.Get("Reminders.Unknown"),timers?.NextLeveAllowances,null);
            // Treasure maps: 0.372 (M-02) the game's next allowance when it is read; otherwise « opened » starts the 18 h wait the game shows.
            var mapAt=configuration.TreasureMapAt;
            var mapReady=timers?.NextMapAllowance ?? mapAt?.AddHours(18);
            Row("Treasure",FontAwesomeIcon.Map,mapReady is { } ready && ready>now ? localization.Get("Reminders.TreasureWaiting"):localization.Get("Reminders.TreasureReady"),mapReady>now ? mapReady:null,null,()=>
            {
                // 0.384 (g, M-14): the deciphered map's spot on the Tensho map.
                if(TreasureMap?.Invoke() is { } treasure && maps.State.Catalog?.Find(treasure.MapId) is { } treasureMap)
                {
                    ImGui.SameLine();
                    if(ImGui.SmallButton(localization.Get("Reminders.TreasureShow")+"##TreasureShow"))ShowOnTenshoMap(treasureMap,treasure.Position);
                    Tip("Reminders.TreasureShowTip");
                }
                if(timers?.NextMapAllowance is not null)return;
                if(ImGui.SmallButton(localization.Get("Reminders.TreasureOpened")+"##TreasureOpened")){configuration.TreasureMapAt=now;SaveReminders();}
                Tip("Reminders.TreasureOpenedTip");
            });
            // 0.372 (M-02): Wondrous Tails and hunt bills, read from the game.
            var bingo=timers?.Bingo;
            Row("Bingo",FontAwesomeIcon.Book,bingo is null ? localization.Get("Reminders.Unknown"):!bingo.Held ? localization.Get("Reminders.BingoNone")
                :localization.Format("Reminders.BingoState",bingo.Stickers,bingo.SecondChance),bingo?.Expires>now ? bingo.Expires:null,null,
                // N-03 (0.439): line odds now and after a shuffle, while fewer than 9 stickers are placed.
                ()=>{
                    // N-02 (0.443): the tasks still to do (open) and those to claim.
                    if(bingo is { Held:true, Tasks:{ } tasks } && BingoTaskName is { } taskName)
                    {
                        foreach(var (order,status) in tasks.Where(task=>task.Status is 0 or 1).DistinctBy(task=>task.Order))
                            ImGui.TextColored(status==1 ? UI.FfxivTheme.Success:Muted,localization.Format(status==1 ? "Reminders.BingoClaim":"Reminders.BingoTask",taskName(order)));
                    }
                    if(bingo is not { Held:true, Placed:{ } placed } || Reminders.WondrousTailsLines.Odds(placed) is not { } odds)return;
                    var shuffled=Reminders.WondrousTailsLines.Shuffled();
                    ImGui.TextDisabled(localization.Format("Reminders.BingoOdds",Percent(odds.AtLeastOne),Percent(odds.AtLeastTwo),Percent(odds.Three)));
                    ImGui.TextDisabled(localization.Format("Reminders.BingoShuffle",Percent(shuffled.AtLeastOne),Percent(shuffled.AtLeastTwo),Percent(shuffled.Three)));
                    if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("Reminders.BingoOddsHelp"));
                    static string Percent(double value)=>(value*100).ToString("0.#");
                });
            Row("HuntBills",FontAwesomeIcon.Crosshairs,timers?.HuntBillsHeld is { } bills ? localization.Format("Reminders.HuntBillsState",bills):localization.Get("Reminders.Unknown"),
                weekly,lastWeekly,()=>DrawHuntBills(timers?.HuntBills));
            // 0.382 (g, M-11): challenge log (once the game loaded it) and Doman Enclave donations, weekly.
            Row("ChallengeLog",FontAwesomeIcon.ClipboardList,timers?.ChallengeLog is { } challenge ? localization.Format("Reminders.ChallengeLogState",challenge.Done,challenge.Total)
                :localization.Get("Reminders.ChallengeLogUnknown"),weekly,lastWeekly);
            Row("DomanEnclave",FontAwesomeIcon.Coins,timers?.DomanEnclave is { } doman ? localization.Format("Reminders.DomanEnclaveState",doman.Donated.ToString("N0"),doman.Allowance.ToString("N0"))
                :localization.Get("Reminders.DomanEnclaveUnknown"),weekly,lastWeekly);
            // 0.307 (R-07): free company voyages, as last read in the workshop; « Details » = the Free company page.
            var voyages=configuration.Voyages;
            Row("Voyages",FontAwesomeIcon.Ship,voyages is not { Count:>0 } ? localization.Get("Reminders.VoyagesUnknown")
                :localization.Format("Reminders.VoyagesState",voyages.Count(vessel=>FreeCompany.Voyages.StateOf(vessel,now)==FreeCompany.VoyageState.Voyaging),voyages.Count(vessel=>FreeCompany.Voyages.StateOf(vessel,now)==FreeCompany.VoyageState.Returned)),
                FreeCompany.Voyages.NextReturn(voyages,now),null,()=>
            {
                if(ImGui.SmallButton(localization.Get("Reminders.VoyagesOpen")+"##VoyagesOpen"))page=Page.FreeCompany;
            });
            // Retainer ventures: known once the retainer list was opened at a summoning bell.
            var ventures=timers?.Ventures;
            var nextVenture=ventures?.Where(venture=>venture.Done>now).Select(venture=>venture.Done).Min();
            Row("Ventures",FontAwesomeIcon.Bell,ventures is null ? localization.Get("Reminders.VenturesUnknown")
                :localization.Format("Reminders.VenturesState",ventures.Count,ventures.Count(venture=>venture.Done is { } done && done<=now)),nextVenture,null,()=>
            {
                // 0.570 (T16): retainer by retainer, with a bell; and one bell for when they are all back.
                if(ventures is not { Count:>0 } || !ImGui.TreeNode(localization.Get("Reminders.VenturesDetail")+"##VenturesDetail"))return;
                try
                {
                    foreach(var (name,end) in ventures)
                    {
                        if(end is not { } at)ImGui.TextDisabled(localization.Format("Reminders.VentureNone",name));
                        else if(at<=now)ImGui.TextColored(Success,localization.Format("Reminders.VentureDone",name));
                        else
                        {
                            ImGui.TextUnformatted(localization.Format("Reminders.VentureAt",name,at.ToLocalTime().ToString("g")));
                            ImGui.SameLine();
                            AlarmButton("Venture"+name,localization.Format("Reminders.AlarmTip",name),()=>[new TenshoAlarm{Label=name,Group=localization.Get("Reminders.Title"),Source=AlarmSource.Reminder,Repeat=AlarmRepeat.Once,At=at,LeadMinutes=0}]);
                        }
                    }
                    if(ventures.Where(venture=>venture.Done>now).Select(venture=>venture.Done).Max() is { } lastBack)
                    {
                        ImGui.TextUnformatted(localization.Format("Reminders.VenturesAllAt",lastBack.ToLocalTime().ToString("g")));
                        ImGui.SameLine();
                        AlarmButton("VenturesAll",localization.Get("Reminders.VenturesAllTip"),()=>[new TenshoAlarm{Label=localization.Get("Reminders.Ventures"),Group=localization.Get("Reminders.Title"),Source=AlarmSource.Reminder,Repeat=AlarmRepeat.Once,At=lastBack,LeadMinutes=0}]);
                    }
                }
                finally{ImGui.TreePop();}
            });
            // 0.333 (DEC-023 6): Grand Company squadron, mission and training, ends read from the game (PlayerState).
            SquadronRow("SquadronMission",FontAwesomeIcon.Flag,timers?.SquadronMission,SquadronMissionName);
            SquadronRow("SquadronTraining",FontAwesomeIcon.Dumbbell,timers?.SquadronTraining,SquadronTrainingName);
        }
        finally{ImGui.EndTable();}
        ImGui.TextDisabled(localization.Get("Reminders.Help"));
        var fromGame=configuration.RemindersFromGame;
        if(ImGui.Checkbox(localization.Get("Reminders.FromGame"),ref fromGame)){configuration.RemindersFromGame=fromGame;SaveReminders();}
        Tip("Reminders.FromGameTip");
        DrawPersonalTasks();
        DrawSquadronPlanner();
        DrawDailyRoulettes(timers);
        DrawCharacterStatuses();

        void SquadronRow(string key,FontAwesomeIcon icon,SquadronTimer? timer,Func<ushort,string?>? nameOf)
        {
            string state;DateTimeOffset? next=null;
            if(timer is null)state=localization.Get("Reminders.Unknown");
            else if(timer.Id==0)state=localization.Get("Reminders.SquadronNone");
            else
            {
                var name=SquadronName(nameOf,timer.Id);
                state=timer.Done is { } done && done<=now ? localization.Format("Reminders.SquadronDone",name)
                    :localization.Format("Reminders.SquadronRunning",name);
                if(timer.Done is { } end && end>now)next=end;
            }
            Row(key,icon,state,next,null);
        }

        void Row(string key,FontAwesomeIcon icon,string state,DateTimeOffset? next,DateTimeOffset? lastReset,Action? extra=null)
        {
            ImGui.TableNextRow();ImGui.TableNextColumn();
            ImGui.PushFont(UiBuilder.IconFont);
            try{ImGui.TextColored(Accent,icon.ToIconString());}
            finally{ImGui.PopFont();}
            ImGui.SameLine();ImGui.TextUnformatted(localization.Get("Reminders."+key));
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("Reminders."+key+"Help"));
            ImGui.TableNextColumn();ImGui.TextUnformatted(state);
            extra?.Invoke();
            ImGui.TableNextColumn();
            if(next is { } at)
            {
                var left=at-now;
                ImGui.TextUnformatted(localization.Format("Reminders.In",(int)left.TotalHours,left.Minutes,at.ToLocalTime().ToString("g")));
            }
            else ImGui.TextDisabled("—");
            ImGui.TableNextColumn();
            if(lastReset is { } reset)
            {
                // 0.374 (M-04): the logged-in character's boxes (shared ones when no character is logged in).
                var done=ResetSchedule.DoneSince(ReminderDoneAt(key),reset);
                if(ImGui.Checkbox("##Done"+key,ref done))SetReminderDone(key,done ? now:null);
                Tip("Reminders.DoneTip");
            }
            // 0.458 (user request, in-game campaign): what the Reminders icon of the server bar shows, chosen here.
            ImGui.TableNextColumn();
            if(ReminderCounts.BarRows.Contains(key))
            {
                var inBar=configuration.ReminderBarHidden?.Contains(key)!=true;
                if(ImGui.Checkbox("##Bar"+key,ref inBar))
                {
                    var hidden=configuration.ReminderBarHidden ?? [];
                    configuration.ReminderBarHidden=inBar ? hidden.Where(row=>row!=key).ToList():[..hidden.Where(row=>row!=key),key];
                    SaveReminders();
                }
                Tip("Reminders.BarRowTip");
            }
            ImGui.TableNextColumn();
            if(next is { } alarmAt && alarmAt>now)
                AlarmButton("Reminder"+key,localization.Format("Reminders.AlarmTip",localization.Get("Reminders."+key)),
                    // 0.331: the vessels' return goes to Discord under « Free company », as from the Free company page.
                    ()=>[new TenshoAlarm{Label=localization.Get("Reminders."+key),Group=localization.Get("Reminders.Title"),Source=key=="Voyages" ? AlarmSource.Voyage:AlarmSource.Reminder,Repeat=AlarmRepeat.Once,At=alarmAt,
                        LeadMinutes=AlarmSchedule.ClampLead(AlarmRepeat.Once,DefaultAlarmLead)}]);
        }
    }

    // Names of squadron missions and trainings from the game data (set by Plugin.cs), kept by id; « #id » when the row is unknown.
    /// <summary>0.558 (T17): from the Reminders page and the server bar, so that the bar's count does not wait for the page.</summary>
    internal void TickRemindersFromGame(GameTimerSnapshot timers)
    {
        if(!configuration.RemindersFromGame)return;
        var now=DateTimeOffset.UtcNow;
        foreach(var row in ReminderCounts.DoneByGame(timers.AlliedQuestsLeft,timers.DeliveriesLeft))
            if(!ResetSchedule.DoneSince(ReminderDoneAt(row),ReminderCounts.LastReset(row,now)))SetReminderDone(row,now);
    }
    internal Func<ushort,string?>? SquadronMissionName { get; set; }
    internal Func<ushort,string?>? SquadronTrainingName { get; set; }
    private readonly Dictionary<(Func<ushort,string?>,ushort),string> squadronNames=[];
    private string SquadronName(Func<ushort,string?>? nameOf,ushort id)
    {
        if(nameOf is null)return $"#{id}";
        if(squadronNames.TryGetValue((nameOf,id),out var known))return known;
        string name;
        try{name=nameOf(id) is { Length:>0 } found ? found:$"#{id}";}
        catch(Exception){name=$"#{id}";}
        squadronNames[(nameOf,id)]=name;
        return name;
    }

    /// <summary>0.458: what is left to do for the Reminders icon: the rows the player keeps in the bar, then their own tasks marked « on screen ».</summary>
    internal IReadOnlyList<string> ReminderTodoLines(DateTimeOffset now)
        =>ReminderCounts.Todo(ReminderDoneAt,configuration.ReminderBarHidden,now).Select(row=>localization.Get("Reminders."+row))
            .Concat(PersonalTasks.Pending(configuration.PersonalTasks,now).Select(task=>task.Name)).ToArray();

    private void SaveReminders()
    {
        try{save();saveError=null;}
        catch(Exception error){saveError=localization.Get("Common.SaveError");log.Warning(error,"Reminders could not be saved.");}
    }
}
