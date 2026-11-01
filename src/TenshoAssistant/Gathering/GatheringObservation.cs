using System.Globalization;

namespace TenshoAssistant.Gathering;

public enum GatheringObservationStatus {Disabled,Waiting,Offline,AddonAbsent,NotReady,Observed,Error}
public static class GatheringUiLayout
{
    public const uint Success=10,Boon=16,Icon=31,Quantity=7;
    public const int MaxNodes=256;
}
public sealed record GatheringRawSlot(int Index,uint ItemId,uint Flags,bool Visible,string? Success,string? Boon,string? Quantity,uint BaseItemId=0,bool ContextResolved=false);
public sealed record GatheringObservedSlot(int Index,uint ItemId,uint RawItemId,uint Flags,int? SuccessPercent,int? BoonPercent,int? DisplayedQuantity);
public sealed record GatheringResourceCopy(ulong Owner,uint JobId,int Level,int Budget,int Attempts,DateTimeOffset ObservedAt);
public sealed class GatheringObservedFrame
{
    public uint JobId {get;}public int Level {get;}public uint Gp {get;}public uint MaxGp {get;}public byte NativeStatus {get;}
    public int? Attempts {get;}public int? TotalAttempts {get;}
    public IReadOnlyList<GatheringObservedSlot> Slots {get;}
    public string Source=>"FFXIVClientStructs.AddonGathering · valeurs UI affichées, non normalisées";
    internal static int? Number(string? text,int minimum,int maximum,bool percent=false)
    {
        if(text is null || text.Length>64)return null;
        var value=text.Trim();if(percent && value.EndsWith('%'))value=value[..^1].TrimEnd();
        if(value.Length==0 || !value.All(character=>character is >= '0' and <= '9')
            || !int.TryParse(value,NumberStyles.None,CultureInfo.InvariantCulture,out var number) || number<minimum || number>maximum)return null;
        return number;
    }
    public GatheringObservedFrame(uint job,int level,uint gp,uint maxGp,byte status,string? attempts,string? total,IReadOnlyList<GatheringRawSlot> slots)
    {
        ArgumentNullException.ThrowIfNull(slots);
        if(job is not (16 or 17) || level is <1 or >100 || gp>maxGp || maxGp>10000 || status is not (1 or 2)
            || slots.Count!=8 || slots.Any(slot=>slot is null || slot.Index is <0 or >7) || slots.Select(slot=>slot.Index).Distinct().Count()!=8)
            throw new ArgumentException("Observation du point incomplète ou incompatible.");
        JobId=job;Level=level;Gp=gp;MaxGp=maxGp;NativeStatus=status;
        var left=Number(attempts,0,12);var maximum=Number(total,1,12);
        Attempts=left is { } count && maximum is { } cap && count<=cap ? count:null;TotalAttempts=maximum;
        Slots=Array.AsReadOnly(slots.Where(slot=>slot.Visible && slot.ItemId!=0).OrderBy(slot=>slot.Index)
            .Select(slot=>new GatheringObservedSlot(slot.Index,slot.ContextResolved ? slot.BaseItemId:0,slot.ItemId,slot.Flags,Number(slot.Success,0,100,true),Number(slot.Boon,0,100,true),Number(slot.Quantity,1,20))).ToArray());
    }
}
public sealed record GatheringObservationState(bool Enabled,ulong Owner,GatheringObservationStatus Status,DateTimeOffset? ObservedAt,GatheringObservedFrame? Frame,string? Error)
{
    public bool Fresh(DateTimeOffset now)=>Enabled && Owner!=0 && Status==GatheringObservationStatus.Observed && Frame is not null
        && ObservedAt is { } date && now>=date && now-date<=TimeSpan.FromSeconds(2);
    public GatheringResourceCopy? CopyResources(DateTimeOffset now)=>Fresh(now) && Frame is {NativeStatus:1,Attempts:>=1 and <=12} frame
        ? new(Owner,frame.JobId,frame.Level,(int)frame.Gp,frame.Attempts.Value,ObservedAt!.Value):null;
}

public sealed class GatheringObservationService : IDisposable
{
    private readonly object gate=new();private int generation;private bool disposed;
    private GatheringObservationState state=new(false,0,GatheringObservationStatus.Disabled,null,null,null);
    public GatheringObservationState State=>Volatile.Read(ref state);
    public void SetEnabled(bool enabled)
    {lock(gate){if(disposed || state.Enabled==enabled)return;generation++;state=new(enabled,0,enabled ? GatheringObservationStatus.Waiting:GatheringObservationStatus.Disabled,null,null,null);}}
    internal int? BeginRead(ulong owner)
    {
        lock(gate)
        {
            if(disposed || !state.Enabled)return null;
            if(state.Owner!=owner || owner==0 && state.Status!=GatheringObservationStatus.Offline)
            {generation++;state=new(true,owner,owner==0 ? GatheringObservationStatus.Offline:GatheringObservationStatus.Waiting,null,null,null);}
            return owner==0 ? null:generation;
        }
    }
    private bool Accept(int ticket,ulong owner)=>!disposed && state.Enabled && owner!=0 && state.Owner==owner && generation==ticket;
    internal void Publish(int ticket,ulong owner,uint job,int level,uint gp,uint maxGp,byte status,string? attempts,string? total,IReadOnlyList<GatheringRawSlot> slots)
    {
        lock(gate)
        {
            if(!Accept(ticket,owner))return;
            try{state=new(true,owner,GatheringObservationStatus.Observed,DateTimeOffset.UtcNow,new(job,level,gp,maxGp,status,attempts,total,slots),null);}
            catch(ArgumentException error){generation++;state=new(true,owner,GatheringObservationStatus.Error,null,null,error.Message);}
        }
    }
    // Screen rectangle of the game's gathering window (0.225), for the in-game help panel; null when the window is not observed.
    private (float X,float Y,float Width,float Height)? window;
    public (float X,float Y,float Width,float Height)? Window{get{lock(gate)return State.Status==GatheringObservationStatus.Observed ? window:null;}}
    internal void SetWindow((float X,float Y,float Width,float Height)? rect){lock(gate)window=rect;}
    internal void Missing(int ticket,ulong owner,GatheringObservationStatus reason)
    {
        if(reason is not (GatheringObservationStatus.AddonAbsent or GatheringObservationStatus.NotReady))throw new ArgumentOutOfRangeException(nameof(reason));
        lock(gate)if(Accept(ticket,owner)){generation++;state=new(true,owner,reason,null,null,null);}
    }
    internal void Fail(int ticket,ulong owner,Exception error)
    {lock(gate)if(Accept(ticket,owner)){generation++;state=new(true,owner,GatheringObservationStatus.Error,null,null,error.Message);}}
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;generation++;state=new(false,0,GatheringObservationStatus.Disabled,null,null,null);}}
}
