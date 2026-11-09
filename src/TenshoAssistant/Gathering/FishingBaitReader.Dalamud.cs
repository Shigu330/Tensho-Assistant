using Dalamud.Plugin.Services;

namespace TenshoAssistant.Gathering;

/// <summary>
/// g, R-04 (0.398): the bait in hand (PlayerState.FishingBait, an item id; 0 = none), copied every half second on the framework thread
/// while the character is a fisher. Field read only; null when not a fisher, logged out or not read.
/// </summary>
public sealed unsafe class FishingBaitReader : IDisposable
{
    private readonly IFramework framework;
    private readonly IObjectTable objects;
    private readonly IPluginLog log;
    private long bait=-1;
    private DateTimeOffset nextRead;
    private bool disposed,warned;
    public uint? Bait=>Volatile.Read(ref bait) is >= 0 and var value ? (uint)value:null;
    /// <summary>C28 (0.663): seconds left of Fisher's Intuition (Status 568, the row GatherBuddy reads too); null when not on.</summary>
    private const uint IntuitionStatus=568;
    private double intuitionLeft=-1;
    public double? IntuitionLeft=>Volatile.Read(ref intuitionLeft) is >= 0 and var left ? left:null;
    /// <summary>
    /// C10 (0.1011): when the last cast started, for the stopwatch of the fishing help. The line's state (FishingEventHandler, through
    /// EventFramework.EventHandlerModule) turns to CastingOut (1) at each cast, mooch included; lures do not change it (docs/evidence/0.887-fishing-bite.md).
    /// Read every frame (one field), only the change is kept; null before the first cast.
    /// </summary>
    private long castAt=-1;private int lineState=-1;private bool lineWarned;
    public DateTimeOffset? CastAt=>Volatile.Read(ref castAt) is >= 0 and var ticks ? new DateTimeOffset(ticks,TimeSpan.Zero):null;

    public FishingBaitReader(IFramework framework,IObjectTable objects,IPluginLog log)
    {
        this.framework=framework;this.objects=objects;this.log=log;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    private void OnUpdate(IFramework _)
    {
        if(disposed)return;
        ReadLine();
        var now=DateTimeOffset.UtcNow;
        if(now<nextRead)return;
        nextRead=now.AddMilliseconds(500);
        try
        {
            var state=FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState.Instance();
            var local=objects.LocalPlayer;
            Volatile.Write(ref bait,local?.ClassJob.RowId==18 && state!=null && state->IsLoaded ? state->FishingBait:-1);
            double left=-1;
            if(local?.ClassJob.RowId==18)
                foreach(var status in local.StatusList)
                    if(status.StatusId==IntuitionStatus && status.RemainingTime>0){left=status.RemainingTime;break;}
            Volatile.Write(ref intuitionLeft,left);
            warned=false;
        }
        catch(Exception error)
        {
            Volatile.Write(ref bait,-1);Volatile.Write(ref intuitionLeft,-1d);
            if(!warned){warned=true;log.Warning(error,"The bait in hand could not be read.");}
        }
    }

    private void ReadLine()
    {
        try
        {
            var events=FFXIVClientStructs.FFXIV.Client.Game.Event.EventFramework.Instance();
            var handler=events==null ? null:events->EventHandlerModule.FishingEventHandler;
            var state=handler==null ? -1:(int)handler->State;
            if(state==lineState)return;
            lineState=state;
            if(state==1)Volatile.Write(ref castAt,DateTimeOffset.UtcNow.UtcTicks);
        }
        catch(Exception error)
        {
            if(!lineWarned){lineWarned=true;log.Warning(error,"The fishing line could not be read.");}
        }
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
    }
}
