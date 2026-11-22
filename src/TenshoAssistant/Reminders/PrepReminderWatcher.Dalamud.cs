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
/// in a duty, in a cutscene, loading or within <see cref="Settling"/> of it, or in a place without mounts (TerritoryType.Mount, through
/// <see cref="MountAllowed"/>: it can only be out where it can be ridden, HowToPage row 361) is held: not taken for a companion sent away.
/// </summary>
public sealed unsafe class PrepReminderWatcher : IDisposable
{
    public const uint WellFed=Craft.CraftConsumables.WellFed;
    private const uint NoEntity=0xE0000000;
    /// <summary>X-774: after a loading screen or a zone change, the companion takes a few seconds to come back.</summary>
    public static readonly TimeSpan Settling=TimeSpan.FromSeconds(20);
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IObjectTable objects;
    private readonly ICondition condition;
    private readonly Func<PrepReminderSettings> settings;
    private readonly IPluginLog log;
    private readonly PrepTracker meal=new(PrepKind.Meal),chocobo=new(PrepKind.Chocobo);
    private DateTimeOffset nextRead,loadedAt;
    private uint lastTerritory;
    private bool disposed,warned;
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
        if(disposed || now<nextRead)return;
        nextRead=now.AddSeconds(5);
        if(!client.IsLoggedIn){meal.Reset();chocobo.Reset();return;}
        // A loading screen: nothing read, nothing forgotten (a meal that ran out meanwhile is still told after it).
        if(objects.LocalPlayer is not { } me || condition[ConditionFlag.BetweenAreas] || condition[ConditionFlag.BetweenAreas51]){loadedAt=now;return;}
        var territory=client.TerritoryType;
        if(territory!=lastTerritory){lastTerritory=territory;loadedAt=now;}
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

    // X-774: out of sight for a reason that is not the player's choice.
    private bool Held(DateTimeOffset now,uint territory)=>
        condition[ConditionFlag.Mounted] || condition[ConditionFlag.RidingPillion] || condition[ConditionFlag.Mounting]
        || condition[ConditionFlag.Mounting71] || condition[ConditionFlag.MountOrOrnamentTransition] || condition[ConditionFlag.MountImmobile] || condition[ConditionFlag.InFlight]
        || condition[ConditionFlag.Swimming] || condition[ConditionFlag.Diving]
        || condition[ConditionFlag.BoundByDuty] || condition[ConditionFlag.BoundByDuty56] || condition[ConditionFlag.BoundByDuty95]
        || condition[ConditionFlag.WatchingCutscene] || condition[ConditionFlag.WatchingCutscene78] || condition[ConditionFlag.OccupiedInCutSceneEvent]
        || now-loadedAt<Settling || MountAllowed?.Invoke(territory)==false;

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
