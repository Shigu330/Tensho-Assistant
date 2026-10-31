namespace TenshoAssistant.MiniGames;

public enum MiniCactpotObservationStatus {Disabled,Waiting,Offline,AddonAbsent,NotReady,Observed,Error}
public sealed class MiniCactpotObservedFrame
{
    public IReadOnlyList<int> Board {get;}
    public int Revealed {get;}
    public bool Playable=>Revealed is >=1 and <=4;
    public string Source=>"FFXIVClientStructs.AddonLotteryDaily.GameNumbers · ordre ligne/colonne";
    public MiniCactpotObservedFrame(IReadOnlyList<int> board)
    {
        ArgumentNullException.ThrowIfNull(board);
        var values=board.ToArray();
        if(values.Length!=9 || values.Any(value=>value is <0 or >9) || values.Where(value=>value!=0).Distinct().Count()!=values.Count(value=>value!=0))
            throw new ArgumentException("Plateau observé incomplet, hors bornes ou chiffres répétés.",nameof(board));
        Board=Array.AsReadOnly(values);Revealed=values.Count(value=>value!=0);
    }
}
public sealed record MiniCactpotObservationState(bool Enabled,ulong Owner,MiniCactpotObservationStatus Status,DateTimeOffset? ObservedAt,MiniCactpotObservedFrame? Frame,string? Error)
{
    public bool CanSolve(DateTimeOffset now)=>Enabled && Owner!=0 && Status==MiniCactpotObservationStatus.Observed && Frame is {Playable:true}
        && ObservedAt is { } date && now>=date && now-date<=TimeSpan.FromSeconds(2);
}

/// <summary>Owns immutable observations; lifetime and owner changes revoke read leases.</summary>
public sealed class MiniCactpotObservationService : IDisposable
{
    private readonly object gate=new();private int generation;private bool disposed;
    private MiniCactpotObservationState state=new(false,0,MiniCactpotObservationStatus.Disabled,null,null,null);
    public MiniCactpotObservationState State=>Volatile.Read(ref state);
    public void SetEnabled(bool enabled)
    {
        lock(gate)
        {
            if(disposed || state.Enabled==enabled)return;
            generation++;state=new(enabled,0,enabled ? MiniCactpotObservationStatus.Waiting:MiniCactpotObservationStatus.Disabled,null,null,null);
        }
    }
    internal int? BeginRead(ulong owner)
    {
        lock(gate)
        {
            if(disposed || !state.Enabled)return null;
            if(state.Owner!=owner || owner==0 && state.Status!=MiniCactpotObservationStatus.Offline)
            {generation++;state=new(true,owner,owner==0 ? MiniCactpotObservationStatus.Offline:MiniCactpotObservationStatus.Waiting,null,null,null);}
            return owner==0 ? null:generation;
        }
    }
    private bool Accept(int ticket,ulong owner)=>!disposed && state.Enabled && owner!=0 && state.Owner==owner && generation==ticket;
    internal void Publish(int ticket,ulong owner,IReadOnlyList<int> board)
    {
        lock(gate)
        {
            if(!Accept(ticket,owner))return;
            try{state=new(true,owner,MiniCactpotObservationStatus.Observed,DateTimeOffset.UtcNow,new(board),null);}
            catch(ArgumentException error){generation++;state=new(true,owner,MiniCactpotObservationStatus.Error,null,null,error.Message);}
        }
    }
    internal void Missing(int ticket,ulong owner,MiniCactpotObservationStatus reason)
    {
        if(reason is not (MiniCactpotObservationStatus.AddonAbsent or MiniCactpotObservationStatus.NotReady))throw new ArgumentOutOfRangeException(nameof(reason));
        lock(gate)if(Accept(ticket,owner)){generation++;state=new(true,owner,reason,null,null,null);}
    }
    internal void Fail(int ticket,ulong owner,Exception error)
    {lock(gate)if(Accept(ticket,owner)){generation++;state=new(true,owner,MiniCactpotObservationStatus.Error,null,null,error.Message);}}
    public void Dispose()
    {lock(gate){if(disposed)return;disposed=true;generation++;state=new(false,0,MiniCactpotObservationStatus.Disabled,null,null,null);}}
}
