namespace TenshoAssistant.MiniGames;

public enum TriadObservationStatus { Disabled, Waiting, Offline, AddonAbsent, NotReady, Observed, Error }
public enum TriadObservedPresence { Empty, Hidden, Known, Incomplete }
public readonly record struct TriadRawCard(bool HasCard, byte Owner, byte Stars, byte Type, byte Top, byte Right, byte Bottom, byte Left);
public sealed record TriadObservedCard(TriadObservedPresence Presence, TriadPlayer? Owner,
    int Stars = 0, uint TypeId = 0, int Top = 0, int Right = 0, int Bottom = 0, int Left = 0)
{
    public static TriadObservedCard Copy(TriadRawCard card)
    {
        if (!card.HasCard) return new(TriadObservedPresence.Empty, null);
        TriadPlayer? owner = card.Owner switch { 1 => TriadPlayer.Blue, 2 => TriadPlayer.Red, _ => null };
        if (card.Owner > 2) return new(TriadObservedPresence.Incomplete, null);
        // Never expose stale rank, rarity or type bytes behind a hidden card.
        if (card.Top == 0 && card.Right == 0 && card.Bottom == 0 && card.Left == 0) return new(TriadObservedPresence.Hidden, owner);
        if (card.Stars is < 1 or > 5 || card.Type > 4 || card.Top is < 1 or > 10 || card.Right is < 1 or > 10
            || card.Bottom is < 1 or > 10 || card.Left is < 1 or > 10) return new(TriadObservedPresence.Incomplete, owner);
        return new(TriadObservedPresence.Known, owner, card.Stars, card.Type, card.Top, card.Right, card.Bottom, card.Left);
    }
}

public sealed class TriadObservedFrame
{
    public byte NativePhase { get; }
    public IReadOnlyList<TriadObservedCard> BlueHand { get; }
    public IReadOnlyList<TriadObservedCard> RedHand { get; }
    public IReadOnlyList<TriadObservedCard> Board { get; }
    public string Source => "FFXIVClientStructs.AddonTripleTriad / faces UI";
    public TriadObservedFrame(byte phase, IReadOnlyList<TriadRawCard> blue, IReadOnlyList<TriadRawCard> red, IReadOnlyList<TriadRawCard> board)
    {
        ArgumentNullException.ThrowIfNull(blue); ArgumentNullException.ThrowIfNull(red); ArgumentNullException.ThrowIfNull(board);
        if (blue.Count != 5 || red.Count != 5 || board.Count != 9) throw new ArgumentException("Observation Triple Triade incomplète.");
        NativePhase = phase;
        BlueHand = Array.AsReadOnly(blue.Select(TriadObservedCard.Copy).ToArray());
        RedHand = Array.AsReadOnly(red.Select(TriadObservedCard.Copy).ToArray());
        Board = Array.AsReadOnly(board.Select(TriadObservedCard.Copy).ToArray());
    }
}

public sealed record TriadObservationState(bool Enabled, ulong CharacterId, TriadObservationStatus Status,
    DateTimeOffset? ObservedAt, TriadObservedFrame? Frame, string? Error);

/// <summary>Pure managed gate; disabling, owner changes and disposal reject in-flight observations.</summary>
public sealed class TriadObservationService : IDisposable
{
    private readonly object gate = new();
    private int generation;
    private bool disposed;
    private TriadObservationState state = new(false,0,TriadObservationStatus.Disabled,null,null,null);
    public TriadObservationState State => Volatile.Read(ref state);
    public void SetEnabled(bool enabled)
    {
        lock (gate)
        {
            if (disposed || state.Enabled == enabled) return;
            generation++;
            state = new(enabled,0,enabled ? TriadObservationStatus.Waiting : TriadObservationStatus.Disabled,null,null,null);
        }
    }
    internal int? BeginRead(ulong character)
    {
        lock (gate)
        {
            if (disposed || !state.Enabled) return null;
            if (state.CharacterId != character || (character == 0 && state.Status != TriadObservationStatus.Offline))
            {
                generation++;
                state = new(true,character,character == 0 ? TriadObservationStatus.Offline : TriadObservationStatus.Waiting,null,null,null);
            }
            return character == 0 ? null : generation;
        }
    }
    private bool Accept(int ticket, ulong character) => !disposed && state.Enabled && character != 0
        && state.CharacterId == character && ticket == generation;
    internal void Publish(int ticket, ulong character, byte phase, IReadOnlyList<TriadRawCard> blue, IReadOnlyList<TriadRawCard> red, IReadOnlyList<TriadRawCard> board)
    {
        lock (gate)
        {
            if (!Accept(ticket,character)) return;
            TriadObservedFrame copied;
            try { copied = new TriadObservedFrame(phase,blue,red,board); }
            catch (ArgumentException error)
            {
                state = new(true,character,TriadObservationStatus.Error,null,null,error.Message);
                throw;
            }
            state = new(true,character,TriadObservationStatus.Observed,DateTimeOffset.UtcNow,copied,null);
        }
    }
    internal void Missing(int ticket, ulong character, TriadObservationStatus reason)
    {
        if (reason is not (TriadObservationStatus.AddonAbsent or TriadObservationStatus.NotReady)) throw new ArgumentOutOfRangeException(nameof(reason));
        lock (gate)
            if (Accept(ticket,character)) state = new(true,character,reason,null,null,null);
    }
    internal void Fail(int ticket, ulong character, Exception error)
    {
        lock (gate)
            if (Accept(ticket,character)) state = new(true,character,TriadObservationStatus.Error,null,null,error.Message);
    }
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true; generation++;
            state = new(false,0,TriadObservationStatus.Disabled,null,null,null);
        }
    }
}
