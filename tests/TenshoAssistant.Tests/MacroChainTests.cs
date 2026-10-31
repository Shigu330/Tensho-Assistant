using TenshoAssistant.Macros;
using TenshoAssistant;
using System.Text.Json;

internal static class MacroChainTests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static MacroSequenceState Idle() => new(MacroSequenceStatus.Idle, 0, Array.Empty<MacroSlot>(), 0, null, null);
    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Macro definitions copy native lines and require one final marker without arguments", () =>
        {
            var lines = new List<string> { "/wait 1", "/tenshonext", "" };
            var definition = MacroDefinition.Copy(new(0, 1, "Test"), 1, lines); lines.Clear();
            Check(definition.Lines.Count == 3 && definition.ValidationError() is null, "native definitions detached from producer");
            Check(MacroDefinition.Copy(definition.Slot, 1, ["/tenshonext"]).ValidationError() is not null, "marker-only macros rejected");
            Check(MacroDefinition.Copy(definition.Slot, 1, ["/tenshonext", "/wait 1"]).ValidationError() is not null, "marker must be last");
            Check(MacroDefinition.Copy(definition.Slot, 1, ["/wait 1", "/tenshonext", "/tenshonext"]).ValidationError() is not null, "duplicate markers rejected");
            Check(MacroDefinition.Copy(definition.Slot, 1, ["/wait 1", "/tenshonext anything"]).ValidationError() is not null, "marker arguments rejected");
            Check(MacroDefinition.Copy(definition.Slot, 1, Enumerable.Repeat("/wait 1", 16)).ValidationError() is not null, "oversized input never silently accepted");
        }),
        ("Macro marker evidence binds owner name native line and footer", () =>
        {
            var pending = new MacroRequest(1, 10, new(0, 1, "Test"));
            var lines = Enumerable.Repeat("", 15).ToArray(); lines[0] = "/wait 1"; lines[1] = "/tenshonext";
            Check(MacroMarkerProtocol.Matches(pending, 10, "Test", 1, lines, "")
                && MacroMarkerProtocol.Matches(pending, 10, "Test", 2, lines, ""), "current or just-consumed native line accepted");
            Check(!MacroMarkerProtocol.Matches(pending, 11, "Test", 1, lines, "")
                && !MacroMarkerProtocol.Matches(pending, 10, "Other", 1, lines, "")
                && !MacroMarkerProtocol.Matches(pending, 10, "Test", 0, lines, "")
                && !MacroMarkerProtocol.Matches(pending, 10, "Test", -1, lines, "")
                && !MacroMarkerProtocol.Matches(pending, 10, "Test", 1, lines, "skip"), "foreign manual or idle evidence rejected");
            lines[2] = "/wait 1";
            Check(!MacroMarkerProtocol.Matches(pending, 10, "Test", 1, lines, ""), "commands after marker rejected");
            lines[2] = "/tenshonext";
            Check(!MacroMarkerProtocol.Matches(pending, 10, "Test", 2, lines, ""), "duplicate native marker rejected");
        }),
        ("Macro requests retain owner and cancel wins before native dispatch", () =>
        {
            var service = new MacroChainService(); service.Publish(10, false, [], Idle());
            var plan = new List<MacroSlot> { new(0, 1, "Test") };
            service.RequestStart(plan); plan.Clear();
            var request = service.TakeStart();
            Check(request?.CharacterId == 10 && request.Plan.Count == 1, "start request copied and owner captured");
            var called = false; service.RequestCancel();
            Check(!service.TryDispatch(10, () => called = true) && !called, "pending cancellation prevents dispatch");
            Check(service.TakeCancel() && service.TryDispatch(10, () => called = true) && called, "explicit cancellation consumption permits a future run");
            Check(!service.TryDispatch(11, () => throw new Exception("wrong owner")), "foreign owner cannot dispatch");
            service.RequestStart([new(0, 1, "Test")]); service.Publish(11, false, [], Idle());
            Check(service.TakeStart() is null, "owner transition discards queued old-character start");
        }),
        ("Macro plan configuration roundtrips without automatic execution defaults", () =>
        {
            var legacy = JsonSerializer.Deserialize<Configuration>("{\"Version\":1}")!;
            Check(legacy.MacroChainPlan.Count == 0, "old configurations have no launchable default plan");
            legacy.MacroChainPlan.Add(new(1, 99, "Shared"));
            var copy = JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(legacy))!;
            Check(copy.MacroChainPlan.Single() == new MacroSlot(1, 99, "Shared"), "book index and name retained");
        }),
    ];
}
