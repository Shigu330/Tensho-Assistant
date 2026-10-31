namespace TenshoAssistant.Macros;

public sealed record MacroNativeObservation(ulong CharacterId, bool Available, int Line, bool Locked, uint WaitMs, string Name, DateTimeOffset ObservedAt);
public sealed record MacroObservationState(bool Enabled, MacroNativeObservation? Current, IReadOnlyList<MacroNativeObservation> Trace, string? Error);

public sealed class MacroObservationService
{
    private readonly object gate = new();
    private MacroObservationState state = new(false, null, Array.Empty<MacroNativeObservation>(), null);
    public MacroObservationState State => Volatile.Read(ref state);
    public void SetEnabled(bool enabled)
    { lock (gate) { state = new(enabled, null, Array.Empty<MacroNativeObservation>(), null); } }
    internal void Publish(MacroNativeObservation observation)
    {
        lock (gate)
        {
            if (!state.Enabled) return;
            if (state.Current is { } current && current.CharacterId == observation.CharacterId
                && current.Available == observation.Available && current.Line == observation.Line && current.Locked == observation.Locked
                && current.WaitMs == observation.WaitMs && current.Name == observation.Name && state.Error is null) return;
            var previous = state.Current?.CharacterId == observation.CharacterId ? state.Trace : Array.Empty<MacroNativeObservation>();
            var trace = previous.TakeLast(63).Append(observation with { ObservedAt = observation.ObservedAt.ToUniversalTime() }).ToArray();
            state = new(true, trace[^1], Array.AsReadOnly(trace), null);
        }
    }
    internal void Fail(Exception error)
    { lock (gate) { if (state.Enabled) state = new(true, null, Array.Empty<MacroNativeObservation>(), error.Message); } }
}
