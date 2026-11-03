namespace TenshoAssistant.Macros;

public sealed record MacroSlot(uint Book, uint Index, string Name);
public sealed record MacroRequest(long Token, ulong CharacterId, MacroSlot Slot);
public enum MacroSequenceStatus { Idle, Ready, AwaitingStart, Running, Complete, Cancelled, Failed, Disposed }
public sealed record MacroSequenceState(MacroSequenceStatus Status, ulong CharacterId, IReadOnlyList<MacroSlot> Plan,
    int Completed, MacroRequest? Pending, string? Message);

/// <summary>
/// Pure scheduling core. An adapter must prove start/end ownership before calling the receipt methods.
/// This type performs no native call and never infers successful completion from elapsed time.
/// </summary>
public sealed class MacroSequence : IDisposable
{
    private readonly object gate = new();
    private long token;
    private MacroSequenceState state = new(MacroSequenceStatus.Idle, 0, Array.Empty<MacroSlot>(), 0, null, null);
    public MacroSequenceState State => Volatile.Read(ref state);
    private bool Active => state.Status is MacroSequenceStatus.Ready or MacroSequenceStatus.AwaitingStart or MacroSequenceStatus.Running;

    public void Start(ulong character, IEnumerable<MacroSlot> plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (character == 0) throw new ArgumentOutOfRangeException(nameof(character));
        var copied = plan.Take(33).ToArray();
        if (copied.Length is < 1 or > 32 || copied.Any(slot => slot is null || slot.Book > 1 || slot.Index > 99))
            throw new ArgumentException("A sequence holds 1 to 32 macros, book 0/1 and index 0–99.", nameof(plan));
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(state.Status == MacroSequenceStatus.Disposed, this);
            if (Active) throw new InvalidOperationException("A sequence is already active.");
            state = new(MacroSequenceStatus.Ready, character, Array.AsReadOnly(copied), 0, null, null);
        }
    }
    public MacroRequest? PrepareNext(ulong character, bool shellAvailable, bool busy, bool locked)
    {
        lock (gate)
        {
            ValidateOwnerCore(character);
            if (state.Status != MacroSequenceStatus.Ready) return null;
            if (!shellAvailable) { StopCore(MacroSequenceStatus.Failed, "Native state unavailable."); return null; }
            if (busy || locked) return null;
            var request = new MacroRequest(checked(++token), character, state.Plan[state.Completed]);
            state = state with { Status = MacroSequenceStatus.AwaitingStart, Pending = request };
            return request;
        }
    }
    public void ObserveStart(MacroRequest receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        lock (gate)
        {
            if (!Active || state.Pending?.Token != receipt.Token) return;
            if (state.Status != MacroSequenceStatus.AwaitingStart || state.Pending != receipt)
            { StopCore(MacroSequenceStatus.Failed, "The start does not match the owned request."); return; }
            state = state with { Status = MacroSequenceStatus.Running };
        }
    }
    public void ObserveEnd(MacroRequest receipt, bool interrupted)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        lock (gate)
        {
            if (!Active || state.Pending?.Token != receipt.Token) return;
            if (state.Pending != receipt || state.Status != MacroSequenceStatus.Running)
            { StopCore(MacroSequenceStatus.Failed, "End received without a confirmed owned start."); return; }
            if (interrupted) { StopCore(MacroSequenceStatus.Cancelled, "Macro interrupted; next steps cancelled."); return; }
            var completed = state.Completed + 1;
            state = state with { Completed = completed, Pending = null,
                Status = completed == state.Plan.Count ? MacroSequenceStatus.Complete : MacroSequenceStatus.Ready };
        }
    }
    public void ObserveExternalStart()
    { lock (gate) { if (Active) StopCore(MacroSequenceStatus.Cancelled, "An external macro took over."); } }
    public void ValidateOwner(ulong character)
    { lock (gate) { ValidateOwnerCore(character); } }
    private void ValidateOwnerCore(ulong character)
    {
        if (Active && (character == 0 || character != state.CharacterId))
            StopCore(MacroSequenceStatus.Cancelled, "Character changed or logged out.");
    }
    public void Fail(string message)
    { lock (gate) { if (Active) StopCore(MacroSequenceStatus.Failed, message); } }
    public void Cancel()
    { lock (gate) { if (Active) StopCore(MacroSequenceStatus.Cancelled, "Next steps cancelled."); } }
    private void StopCore(MacroSequenceStatus status, string message) => state = state with { Status = status, Pending = null, Message = message };
    public void Dispose()
    {
        lock (gate)
            state = new(MacroSequenceStatus.Disposed, 0, Array.Empty<MacroSlot>(), 0, null, null);
    }
}
