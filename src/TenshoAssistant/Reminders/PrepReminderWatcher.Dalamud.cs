using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace TenshoAssistant.Reminders;

/// <summary>
/// X-226: every 5 seconds on the framework thread, while logged in (cut with the rest of Tensho in PvP and ranked content, CompetitiveGate): the
/// time left of the character's Well Fed status (Status 48, its statuses) and of the chocobo companion out (UIState.Buddy.CompanionInfo:
/// TimeLeft, the companion's EntityId not 0 nor 0xE0000000), each through its <see cref="PrepTracker"/>; a fight (InCombat) or a duty
/// (BoundByDuty, 56, 95) holds the reminders back (R03.1). Read only; an unreadable state says nothing (never a false « has left »).
/// X-774: the companion out of sight while the player is mounted (Mounted, RidingPillion, Mounting…, InFlight), swimming or diving,
/// in a duty, in a cutscene, loading or within <see cref="PrepSettling.Settling"/> of it, or in a place without mounts (TerritoryType.Mount, through
/// <see cref="MountAllowed"/>: it can only be out where it can be ridden, HowToPage row 361) is held: not taken for a companion sent away.
/// X-777: loads, zone changes and the companion going out of sight are seen every frame (a few reads), so that what made it go is known
/// when it goes (<see cref="PrepTracker.Watch"/>); the first few goings and comings after the plugin loads are written to the log with the game's timer
/// (whether the game keeps it while the companion is out of sight is not known: the log is there to see it in the game).
/// X-781 (the review of 0.1789, M1): the settling after a load ends once the companion has been seen back (<see cref="PrepSettling"/>): sent
/// away then, it is not taken for held.
/// </summary>
public sealed unsafe class PrepReminderWatcher : IDisposable
{
    public const uint WellFed=Craft.CraftConsumables.WellFed;
    private const uint NoEntity=0xE0000000;
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IObjectTable objects;
    private readonly ICondition condition;
    private readonly Func<PrepReminderSettings> settings;
    private readonly IPluginLog log;
    private readonly PrepTracker meal=new(PrepKind.Meal),chocobo=new(PrepKind.Chocobo);
    private DateTimeOffset nextRead;
    // X-781: whether a load still explains the companion out of sight (not once it has been seen back).
    private readonly PrepSettling settling=new();
    private uint lastTerritory;
    private bool disposed,warned;
    // X-777: the log lines of the companion's goings and comings, a few per load of the plugin; when it went and the timer then.
    private const int SightLogs=6;
    private int sightLogs;
    private bool watchWarned;
    private DateTimeOffset sightAt;
    private float sightLeft;
    /// <summary>Framework thread: a reminder to say (chat and sound with the alarm options).</summary>
    public Action<PrepNotice>? Due{get;set;}
    /// <summary>X-774: whether mounts are allowed in a territory (TerritoryType.Mount); null: unknown (then it does not hold the companion).</summary>
    public Func<uint,bool?>? MountAllowed{get;set;}

    public PrepReminderWatcher(IFramework framework,IClientState client,IObjectTable objects,ICondition condition,Func<PrepReminderSettings> settings,IPluginLog log)
    {
        this.framework=framework;this.client=client;this.objects=objects;this.condition=condition;this.settings=settings;this.log=log;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    private void OnUpdate(IFramework _)
    {
        var now=DateTimeOffset.UtcNow;
        if(disposed)return;
        // X-777: every frame, a few reads: a load or a zone change seen at once, and the moment the companion goes out of sight.
        var loading=objects.LocalPlayer is null || condition[ConditionFlag.BetweenAreas] || condition[ConditionFlag.BetweenAreas51];
        var territory=client.TerritoryType;
        if(loading)settling.Loaded(now);
        else if(territory!=lastTerritory){lastTerritory=territory;settling.Loaded(now);}
        if(client.IsLoggedIn)WatchCompanion(now,territory,loading);
        if(now<nextRead)return;
        nextRead=now.AddSeconds(5);
        if(!client.IsLoggedIn){meal.Reset();chocobo.Reset();return;}
        // A loading screen: nothing read, nothing forgotten (a meal that ran out meanwhile is still told after it).
        if(objects.LocalPlayer is not { } me || loading)return;
        try
        {
            var options=settings();
            var busy=condition[ConditionFlag.InCombat] || condition[ConditionFlag.BoundByDuty] || condition[ConditionFlag.BoundByDuty56] || condition[ConditionFlag.BoundByDuty95];
            if(options.Meal)
            {
                double? left=null;
                foreach(var status in me.StatusList)
                    if(status.StatusId==WellFed){left=status.RemainingTime;break;}
                Say(meal.Read(now,left,options.MealThreshold,busy));
            }
            else meal.Reset();
            var ui=UIState.Instance();
            if(options.Chocobo && ui!=null)
            {
                var info=ui->Buddy.CompanionInfo;
                var outNow=info.Companion!=null && info.Companion->EntityId is not (0 or NoEntity);
                Say(chocobo.Read(now,outNow ? info.TimeLeft:null,options.ChocoboThreshold,busy,!outNow && Held(now,territory)));
            }
            else if(!options.Chocobo)chocobo.Reset();
            warned=false;
        }
        catch(Exception error)
        {
            if(!warned){warned=true;log.Warning(error,"Preparation reminder: the meal or the chocobo companion could not be read.");}
        }
    }

    // X-777: every frame: the companion out or not; whether something holds it only the frame it goes and just after (PrepTracker.WantsHeld).
    private void WatchCompanion(DateTimeOffset now,uint territory,bool loading)
    {
        try
        {
            if(!settings().Chocobo)return;
            var ui=UIState.Instance();
            if(ui==null)return;
            var info=ui->Buddy.CompanionInfo;
            var outNow=info.Companion!=null && info.Companion->EntityId is not (0 or NoEntity);
            settling.Seen(now,outNow);
            var held=chocobo.WantsHeld(now,outNow) && (loading || Held(now,territory));
            var sight=chocobo.Watch(now,outNow,held);
            if(sight==PrepSight.Same || sightLogs>=SightLogs)return;
            sightLogs++;
            if(sight==PrepSight.Went)
            {
                sightAt=now;sightLeft=info.TimeLeft;
                log.Information("Chocobo companion out of sight (X-777): {0}, the game's timer then {1:0} s.",held ? "held (mounted, loading, a duty…)":"nothing held it",info.TimeLeft);
            }
            else log.Information("Chocobo companion back (X-777) after {0:0} s: the game's timer {1:0} s (it was {2:0} s when it went).",(now-sightAt).TotalSeconds,info.TimeLeft,sightLeft);
        }
        catch(Exception error)
        {
            if(!watchWarned){watchWarned=true;log.Warning(error,"Preparation reminder: the chocobo companion could not be watched.");}
        }
    }

    // X-774: out of sight for a reason that is not the player's choice.
    private bool Held(DateTimeOffset now,uint territory)=>
        condition[ConditionFlag.Mounted] || condition[ConditionFlag.RidingPillion] || condition[ConditionFlag.Mounting]
        || condition[ConditionFlag.Mounting71] || condition[ConditionFlag.MountOrOrnamentTransition] || condition[ConditionFlag.MountImmobile] || condition[ConditionFlag.InFlight]
        || condition[ConditionFlag.Swimming] || condition[ConditionFlag.Diving]
        || condition[ConditionFlag.BoundByDuty] || condition[ConditionFlag.BoundByDuty56] || condition[ConditionFlag.BoundByDuty95]
        || condition[ConditionFlag.WatchingCutscene] || condition[ConditionFlag.WatchingCutscene78] || condition[ConditionFlag.OccupiedInCutSceneEvent]
        || settling.Holds(now) || MountAllowed?.Invoke(territory)==false;

    private void Say(PrepNotice? notice)
    {
        if(notice is not null)Due?.Invoke(notice);
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
    }
}
