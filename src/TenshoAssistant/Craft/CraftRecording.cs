namespace TenshoAssistant.Craft;

/// <summary>
/// A-14 (0.436): a crafting rotation recorded while the player crafts by hand — the crafting actions the game accepted, in order
/// (100 at most). Only actions of the model are kept; anything else is counted apart, never guessed.
/// </summary>
public sealed class CraftRecording
{
    public const int MaxActions=100;
    private readonly object gate=new();
    private readonly List<CraftActionKind> actions=[];
    private int ignored;
    public bool Recording { get; private set; }

    public void Start(){lock(gate){actions.Clear();ignored=0;Recording=true;}}
    public void Stop(){lock(gate)Recording=false;}

    /// <summary>One action the game accepted (null: not a crafting action of the model).</summary>
    public void Observe(CraftActionKind? kind)
    {
        lock(gate)
        {
            if(!Recording)return;
            if(kind is { } known && actions.Count<MaxActions)actions.Add(known);else ignored++;
        }
    }

    public (IReadOnlyList<CraftActionKind> Actions,int Ignored) Snapshot(){lock(gate)return (actions.ToArray(),ignored);}
}
