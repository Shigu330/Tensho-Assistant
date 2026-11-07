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

    public FishingBaitReader(IFramework framework,IObjectTable objects,IPluginLog log)
    {
        this.framework=framework;this.objects=objects;this.log=log;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    private void OnUpdate(IFramework _)
    {
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextRead)return;
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

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
    }
}
