namespace TenshoAssistant.Craft;

public sealed record CraftSolveInput(ulong Owner,int Revision,RecipeDefinition Recipe,CraftStats Stats,int StartingQuality,CraftSolveOptions Options);
public sealed record CraftSolveState(CraftSolveInput? Input,bool Loading,CraftSolveResult? Result,DateTimeOffset? CalculatedAt,string? Error);

public sealed class CraftSolveSession : IDisposable
{
    private readonly object gate=new();
    private readonly Func<CraftSolveInput,CancellationToken,CraftSolveResult> solve;
    private CancellationTokenSource? cancellation;
    private int generation;
    private bool disposed;
    private CraftSolveState state=new(null,false,null,null,null);
    public CraftSolveState State=>Volatile.Read(ref state);
    public CraftSolveSession(Func<CraftSolveInput,CancellationToken,CraftSolveResult>? solve=null)
    { this.solve=solve ?? ((input,token)=>CraftSolver.Solve(input.Recipe,input.Stats,input.StartingQuality,input.Options,token)); }
    public Task StartAsync(CraftSolveInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        input=input with {Recipe=input.Recipe with {Ingredients=Array.AsReadOnly(input.Recipe.Ingredients.ToArray())}};
        lock(gate)
        {
            if(disposed) return Task.CompletedTask;
            cancellation?.Cancel(); cancellation?.Dispose(); cancellation=new(); var token=cancellation.Token; var ticket=++generation;
            state=new(input,true,null,null,null);
            return Task.Run(()=>
            {
                try
                {
                    var result=solve(input,token);
                    lock(gate) if(!disposed && ticket==generation) state=new(input,false,result,DateTimeOffset.UtcNow,null);
                }
                catch(Exception error)
                {
                    lock(gate) if(!disposed && ticket==generation) state=new(input,false,null,null,error is OperationCanceledException ? "Calcul annulé.":error.Message);
                }
            });
        }
    }
    public void ValidateContext(ulong owner,int revision,uint recipe)
    {
        lock(gate) if(!disposed && state.Input is { } input && (input.Owner!=owner || input.Revision!=revision || input.Recipe.RecipeId!=recipe)) Clear();
    }
    private void Clear() { generation++; cancellation?.Cancel(); state=new(null,false,null,null,null); }
    public void Cancel() {lock(gate) if(!disposed) Clear();}
    public void Dispose() {lock(gate) {if(disposed)return; Clear(); disposed=true; cancellation?.Dispose();}}
}
