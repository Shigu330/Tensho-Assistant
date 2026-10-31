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

/// <summary>Screen rectangle of a card slot or board square (pixels, same space as ImGui).</summary>
public readonly record struct TriadRect(float X, float Y, float Width, float Height)
{
    public bool Valid => float.IsFinite(X) && float.IsFinite(Y) && float.IsFinite(Width) && float.IsFinite(Height) && Width > 0 && Height > 0;
}
/// <summary>
/// What the match needs beyond the card faces (TRIAD-003, capture docs/evidence/0.160.0-triad-match-capture.log): rules shown in the
/// match window (TripleTriadRule ids, matched by name), the card the game imposes on a constrained move (Chaos/Order), the NPC's deck
/// from the TripleTriad sheet (docs/evidence/0.159.0-triad-npc-probe.log) and the slots' screen rectangles.
/// </summary>
public sealed record TriadMatchContext(IReadOnlyList<uint> RuleIds, int? ForcedSlot, uint NpcId, IReadOnlyList<uint> NpcFixed,
    IReadOnlyList<uint> NpcVariable, IReadOnlyList<TriadRect>? BlueRects, IReadOnlyList<TriadRect>? BoardRects);

/// <summary>A deck saved in the game (GoldSaucerModule.Decks): name and card ids (0 = empty slot).</summary>
public sealed record TriadGameDeck(int Index, string Name, IReadOnlyList<uint> Cards);
/// <summary>
/// The NPC challenge window « TripleTriadRequest » (TRIAD-003, capture docs/evidence/0.160.0-triad-match-capture.log): values 102–105 hold
/// the match rules as TripleTriadRule ids (10, 12, 4, 9 = the rules then shown in the match), the NPC is the target; plus the window's
/// screen rectangle and the player's saved decks.
/// </summary>
public sealed record TriadRequestInfo(uint NpcId, IReadOnlyList<uint> RuleIds, IReadOnlyList<uint> NpcFixed, IReadOnlyList<uint> NpcVariable,
    TriadRect Window, IReadOnlyList<TriadGameDeck> Decks, DateTimeOffset ObservedAt, string NpcName = "", IReadOnlyList<string>? RuleNames = null);

public sealed class TriadObservedFrame
{
    public byte NativePhase { get; }
    public IReadOnlyList<TriadObservedCard> BlueHand { get; }
    public IReadOnlyList<TriadObservedCard> RedHand { get; }
    public IReadOnlyList<TriadObservedCard> Board { get; }
    public string Source => "FFXIVClientStructs.AddonTripleTriad / faces UI";
    public TriadMatchContext? Context { get; }
    // TurnState (FFXIVClientStructs): 0 waiting, 1 normal move, 2 masked move (card imposed by Chaos/Order); the player moves on 1 and 2.
    public bool BlueToMove => NativePhase is 1 or 2;
    public TriadObservedFrame(byte phase, IReadOnlyList<TriadRawCard> blue, IReadOnlyList<TriadRawCard> red, IReadOnlyList<TriadRawCard> board,
        TriadMatchContext? context = null)
    {
        Context = context is null ? null : context with
        {
            RuleIds = Array.AsReadOnly(context.RuleIds.ToArray()), NpcFixed = Array.AsReadOnly(context.NpcFixed.ToArray()),
            NpcVariable = Array.AsReadOnly(context.NpcVariable.ToArray()),
            ForcedSlot = context.ForcedSlot is >= 0 and <= 4 ? context.ForcedSlot : null,
            BlueRects = context.BlueRects is { Count: 5 } blueRects && blueRects.All(rect => rect.Valid) ? Array.AsReadOnly(blueRects.ToArray()) : null,
            BoardRects = context.BoardRects is { Count: 9 } boardRects && boardRects.All(rect => rect.Valid) ? Array.AsReadOnly(boardRects.ToArray()) : null,
        };
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
    private TriadRequestInfo? request;
    public TriadRequestInfo? Request => Volatile.Read(ref request);
    // Screen rectangle of the game's deck editor « GSInfoEditDeck » while it is open (0.162), for the deck reminder panel.
    private TriadRect? editDeck;
    public TriadRect? EditDeckWindow { get { lock (gate) return editDeck; } }
    // Cards drawn in the editor's list (0.164, capture docs/evidence/0.163.0-triad-editdeck-capture.log): icon texture
    // ui/icon/088000/088NNN = card NNN, with its screen rectangle.
    private IReadOnlyList<(uint CardId, TriadRect Rect)> editDeckCards = [];
    public IReadOnlyList<(uint CardId, TriadRect Rect)> EditDeckCards { get { lock (gate) return editDeckCards; } }
    // Deck rows of the game's deck choice window « TripleTriadSelDeck » (0.165): the card ids of each row and its screen rectangle.
    private IReadOnlyList<(IReadOnlyList<uint> Cards, TriadRect Row)> selDeck = [];
    public IReadOnlyList<(IReadOnlyList<uint> Cards, TriadRect Row)> SelDeckRows { get { lock (gate) return selDeck; } }
    internal void PublishSelDeck(int ticket, ulong character, IReadOnlyList<(IReadOnlyList<uint> Cards, TriadRect Row)> rows)
    { lock (gate) if (Accept(ticket,character) || rows.Count == 0) selDeck = rows; }
    internal void PublishEditDeck(int ticket, ulong character, TriadRect? rect, IReadOnlyList<(uint CardId, TriadRect Rect)>? cards = null)
    { lock (gate) if (Accept(ticket,character) || rect is null) { editDeck = rect; editDeckCards = rect is null ? [] : cards ?? []; } }
    internal void PublishRequest(int ticket, ulong character, TriadRequestInfo? info)
    { lock (gate) if (Accept(ticket,character) || info is null) request = info; }
    public void SetEnabled(bool enabled)
    {
        lock (gate)
        {
            if (disposed || state.Enabled == enabled) return;
            generation++; request = null; editDeck = null;
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
                generation++; request = null; editDeck = null;
                state = new(true,character,character == 0 ? TriadObservationStatus.Offline : TriadObservationStatus.Waiting,null,null,null);
            }
            return character == 0 ? null : generation;
        }
    }
    private bool Accept(int ticket, ulong character) => !disposed && state.Enabled && character != 0
        && state.CharacterId == character && ticket == generation;
    internal void Publish(int ticket, ulong character, byte phase, IReadOnlyList<TriadRawCard> blue, IReadOnlyList<TriadRawCard> red, IReadOnlyList<TriadRawCard> board,
        TriadMatchContext? context = null)
    {
        lock (gate)
        {
            if (!Accept(ticket,character)) return;
            TriadObservedFrame copied;
            try { copied = new TriadObservedFrame(phase,blue,red,board,context); }
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
            disposed = true; generation++; request = null; editDeck = null;
            state = new(false,0,TriadObservationStatus.Disabled,null,null,null);
        }
    }
}
