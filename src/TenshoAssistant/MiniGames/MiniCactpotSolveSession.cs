namespace TenshoAssistant.MiniGames;

public sealed record MiniCactpotSolveState(IReadOnlyList<int>? Board,ulong Owner,int Revision,MiniCactpotResult? Result,bool Loading,string? Error);

/// <summary>Owns solver workers; copied input and generation prevent late results from another ticket or owner.</summary>
public sealed class MiniCactpotSolveSession : IDisposable
{
    private readonly object gate=new();private readonly Func<IReadOnlyList<int>,CancellationToken,MiniCactpotResult> solve;
    private CancellationTokenSource? cancellation;private int generation;private bool disposed;
    private MiniCactpotSolveState state=new(null,0,0,null,false,null);
    public MiniCactpotSolveSession():this(MiniCactpotSolver.Solve){}
    internal MiniCactpotSolveSession(Func<IReadOnlyList<int>,CancellationToken,MiniCactpotResult> solve){this.solve=solve;}
    public MiniCactpotSolveState State=>Volatile.Read(ref state);
    public Task StartAsync(IReadOnlyList<int> board,ulong owner,int revision)
    {
        ArgumentNullException.ThrowIfNull(board);if(board.Count!=9)throw new ArgumentException("Neuf cases requises.",nameof(board));
        var copied=Array.AsReadOnly(board.ToArray());
        lock(gate)
        {
            if(disposed)return Task.CompletedTask;
            cancellation?.Cancel();cancellation?.Dispose();cancellation=new();var token=cancellation.Token;var ticket=++generation;
            state=new(copied,owner,revision,null,true,null);
            return Task.Run(()=>
            {
                try
                {
                    var result=solve(copied,token);
                    lock(gate)if(!disposed && ticket==generation)state=new(copied,owner,revision,result,false,null);
                }
                catch(Exception error)
                {lock(gate)if(!disposed && ticket==generation)state=new(copied,owner,revision,null,false,error is OperationCanceledException ? "Calcul annulé.":error.Message);}
            });
        }
    }
    public void ValidateContext(ulong owner,int revision)
    {lock(gate)if(!disposed && state.Board is not null && (owner!=state.Owner || revision!=state.Revision))Clear();}
    public void Cancel(){lock(gate)if(!disposed)Clear();}
    private void Clear(){generation++;cancellation?.Cancel();state=new(null,0,0,null,false,null);}
    public void Dispose(){lock(gate){if(disposed)return;Clear();disposed=true;cancellation?.Dispose();}}
}
