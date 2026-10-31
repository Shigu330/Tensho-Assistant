namespace TenshoAssistant.Macros;

public sealed record MacroDefinition(MacroSlot Slot, uint IconId, IReadOnlyList<string> Lines)
{
    public const string Marker = "/tenshonext";
    public static MacroDefinition Copy(MacroSlot slot, uint icon, IEnumerable<string> lines) =>
        new(slot, icon, Array.AsReadOnly(lines.Take(16).ToArray()));
    public string? ValidationError()
    {
        if (Lines.Count is < 1 or > 15 || Lines.All(string.IsNullOrWhiteSpace)) return "Macro vide ou trop longue.";
        var used = Lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
        if (used.Length < 2 || used[^1].Trim() != Marker || used.Count(line => line.Trim() == Marker) != 1)
            return $"Terminez la macro par une seule ligne {Marker}, après ses commandes.";
        if (used.Any(line => line.Trim().StartsWith(Marker + " ", StringComparison.OrdinalIgnoreCase)))
            return "Le marqueur ne prend aucun argument.";
        return null;
    }
}

public sealed record MacroChainState(ulong CharacterId, bool Loading, IReadOnlyList<MacroDefinition> Catalog,
    MacroSequenceState Sequence, string? Error);
public sealed record MacroStartRequest(ulong CharacterId, IReadOnlyList<MacroSlot> Plan);

public static class MacroMarkerProtocol
{
    public static bool Matches(MacroRequest pending, ulong character, string nativeName, int nativeLine,
        IReadOnlyList<string> nativeLines, string arguments)
    {
        if (character == 0 || character != pending.CharacterId || nativeName != pending.Slot.Name || arguments.Length != 0
            || nativeLines.Count != 15) return false;
        var marker = -1;
        for (var index = 0; index < nativeLines.Count; index++)
        {
            if (nativeLines[index].Trim() == MacroDefinition.Marker)
            { if (marker >= 0) return false; marker = index; }
        }
        if (marker < 0 || (nativeLine != marker && nativeLine != marker + 1)) return false;
        return nativeLines.Skip(marker + 1).All(string.IsNullOrWhiteSpace);
    }
}

public sealed class MacroChainService
{
    private readonly object gate = new();
    private int refresh;
    private bool cancel;
    private MacroStartRequest? start;
    private MacroChainState state = new(0, false, Array.Empty<MacroDefinition>(),
        new(MacroSequenceStatus.Idle, 0, Array.Empty<MacroSlot>(), 0, null, null), null);
    public MacroChainState State => Volatile.Read(ref state);
    public void RequestRefresh() => Interlocked.Exchange(ref refresh, 1);
    internal bool TakeRefresh() => Interlocked.Exchange(ref refresh, 0) != 0;
    public void RequestStart(IEnumerable<MacroSlot> plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var copied = plan.Take(33).ToArray();
        lock (gate)
        {
            if (state.CharacterId == 0) { ErrorCore("Connectez un personnage avant de lancer une séquence."); return; }
            if (start is not null || state.Sequence.Status is MacroSequenceStatus.Ready or MacroSequenceStatus.AwaitingStart or MacroSequenceStatus.Running)
            { ErrorCore("Une séquence ou demande est déjà en cours."); return; }
            if (copied.Length is < 1 or > 32 || copied.Any(slot => slot is null || slot.Book > 1 || slot.Index > 99))
            { ErrorCore("Choisissez entre 1 et 32 macros valides."); return; }
            start = new(state.CharacterId, Array.AsReadOnly(copied));
            state = state with { Error = null };
        }
    }
    public void RequestCancel() { lock (gate) { cancel = true; start = null; } }
    internal bool TakeCancel() { lock (gate) { var value = cancel; cancel = false; return value; } }
    internal MacroStartRequest? TakeStart() { lock (gate) { var value = start; start = null; return value; } }
    internal void SetError(string message) { lock (gate) { ErrorCore(message); } }
    private void ErrorCore(string message) => state = state with { Error = message };
    internal void Publish(ulong owner, bool loading, IEnumerable<MacroDefinition>? catalog, MacroSequenceState sequence)
    {
        lock (gate)
        {
            var entries = owner == state.CharacterId ? state.Catalog : Array.Empty<MacroDefinition>();
            if (catalog is not null) entries = Array.AsReadOnly(catalog.Select(row => MacroDefinition.Copy(row.Slot, row.IconId, row.Lines)).ToArray());
            if (owner != state.CharacterId) { start = null; cancel = false; }
            state = new(owner, loading, entries, sequence, owner == state.CharacterId ? state.Error : null);
        }
    }
    internal bool TryDispatch(ulong character, Action action)
    {
        lock (gate)
        {
            if (cancel || character == 0 || state.CharacterId != character) return false;
            action(); return true;
        }
    }
    internal void Clear()
    {
        lock (gate)
        {
            start = null; cancel = false;
            state = new(0, false, Array.Empty<MacroDefinition>(), new(MacroSequenceStatus.Disposed, 0, Array.Empty<MacroSlot>(), 0, null, null), null);
        }
    }
}
