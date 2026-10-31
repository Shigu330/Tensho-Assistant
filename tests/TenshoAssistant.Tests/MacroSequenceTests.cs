using TenshoAssistant.Macros;

internal static class MacroSequenceTests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Throws<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}"); }
    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Passive macro observations are opt-in coalesced bounded and immutable", () =>
        {
            var service = new MacroObservationService();
            var row = new MacroNativeObservation(10, true, 0, false, 0, "Test", DateTimeOffset.Now);
            service.Publish(row); Check(service.State.Current is null, "disabled observer cannot publish");
            service.SetEnabled(true); service.Publish(row);
            service.Publish(row with { ObservedAt = row.ObservedAt.AddSeconds(1) });
            Check(service.State.Trace.Count == 1 && service.State.Current!.ObservedAt.Offset == TimeSpan.Zero,
                "unchanged values coalesced and timestamp normalized");
            for (var line = 1; line <= 100; line++) service.Publish(row with { Line = line });
            Check(service.State.Trace.Count == 64 && service.State.Trace[0].Line == 37 && service.State.Current!.Line == 100,
                "trace bounded to sixty-four latest transitions");
            Throws<NotSupportedException>(() => ((IList<MacroNativeObservation>)service.State.Trace).Clear());
        }),
        ("Passive macro observations clear foreign owner errors and disabled publications", () =>
        {
            var service = new MacroObservationService(); service.SetEnabled(true);
            var row = new MacroNativeObservation(10, true, 0, false, 0, "A", DateTimeOffset.UtcNow);
            service.Publish(row); service.Publish(row with { CharacterId = 11, Name = "B" });
            Check(service.State.Trace.Count == 1 && service.State.Trace.All(entry => entry.CharacterId == 11), "old character trace removed");
            service.Fail(new InvalidOperationException("unavailable"));
            Check(service.State.Current is null && service.State.Trace.Count == 0 && service.State.Error == "unavailable", "failed reads are not idle proof");
            service.Publish(row); Check(service.State.Error is null && service.State.Current is not null, "successful observation recovers error");
            service.SetEnabled(false); service.Publish(row);
            Check(!service.State.Enabled && service.State.Current is null && service.State.Trace.Count == 0, "late callback cannot restore disabled trace");
        }),
        ("Macro sequence advances only after matching start and successful end receipts", () =>
        {
            using var sequence = new MacroSequence();
            var source = new List<MacroSlot> { new(0, 0, "First"), new(1, 99, "Second") };
            sequence.Start(10, source); source.Clear();
            Throws<NotSupportedException>(() => ((IList<MacroSlot>)sequence.State.Plan).Clear());
            Check(sequence.PrepareNext(10, true, true, false) is null && sequence.PrepareNext(10, true, false, true) is null, "busy or locked native shell never replaced");
            var first = sequence.PrepareNext(10, true, false, false)!;
            Check(sequence.PrepareNext(10, true, false, false) is null, "only one outstanding request");
            sequence.ObserveStart(first);
            Check(sequence.State.Status == MacroSequenceStatus.Running && sequence.PrepareNext(10, true, false, false) is null, "idle-looking shell is not completion evidence");
            sequence.ObserveEnd(first, false);
            var second = sequence.PrepareNext(10, true, false, false)!;
            Check(second.Slot.Book == 1 && second.Slot.Index == 99 && second.Token != first.Token, "copied order and distinct receipts");
            sequence.ObserveStart(second); sequence.ObserveEnd(second, false);
            Check(sequence.State.Status == MacroSequenceStatus.Complete && sequence.State.Completed == 2, "all explicit receipts accounted for");
        }),
        ("Macro sequence rejects unproven endings foreign receipts and external takeovers", () =>
        {
            using var sequence = new MacroSequence();
            var plan = new[] { new MacroSlot(0, 5, "A") };
            sequence.Start(1, plan);
            var request = sequence.PrepareNext(1, true, false, false)!;
            sequence.ObserveEnd(request, false);
            Check(sequence.State.Status == MacroSequenceStatus.Failed && sequence.State.Completed == 0, "void native execution is not proof of start or completion");
            sequence.Start(1, plan); request = sequence.PrepareNext(1, true, false, false)!;
            sequence.ObserveStart(request with { CharacterId = 2 });
            Check(sequence.State.Status == MacroSequenceStatus.Failed, "foreign owner cannot acknowledge own token");
            sequence.Start(1, plan); sequence.ObserveExternalStart();
            Check(sequence.State.Status == MacroSequenceStatus.Cancelled, "external execution cancels future steps");
        }),
        ("Macro sequence cancels on interruption owner change and manual stop and ignores old receipts", () =>
        {
            using var sequence = new MacroSequence(); var plan = new[] { new MacroSlot(0, 5, "A") };
            sequence.Start(1, plan); var old = sequence.PrepareNext(1, true, false, false)!;
            sequence.ObserveStart(old); sequence.ObserveEnd(old, true);
            Check(sequence.State.Status == MacroSequenceStatus.Cancelled && sequence.State.Completed == 0, "interruption never counted as successful completion");
            sequence.Start(1, plan); var current = sequence.PrepareNext(1, true, false, false)!;
            sequence.ObserveStart(old); sequence.ObserveEnd(old, false);
            Check(sequence.State.Status == MacroSequenceStatus.AwaitingStart && sequence.State.Pending == current, "late prior-run receipts cannot change new run");
            sequence.ValidateOwner(2); Check(sequence.State.Status == MacroSequenceStatus.Cancelled, "owner boundary cancels pending execution");
            sequence.Start(1, plan); sequence.ValidateOwner(0); Check(sequence.State.Status == MacroSequenceStatus.Cancelled, "logout cancels plan");
            sequence.Start(1, plan); sequence.Cancel(); Check(sequence.PrepareNext(1, true, false, false) is null, "stop cannot schedule another action");
        }),
        ("Macro sequence validates bounded plans and native availability and is inert after disposal", () =>
        {
            var sequence = new MacroSequence(); var plan = new[] { new MacroSlot(0, 5, "A") };
            Throws<ArgumentOutOfRangeException>(() => sequence.Start(0, plan));
            Throws<ArgumentException>(() => sequence.Start(1, []));
            Throws<ArgumentException>(() => sequence.Start(1, Enumerable.Repeat(plan[0], 33)));
            Throws<ArgumentException>(() => sequence.Start(1, [new(2, 0, "Wrong book")]));
            Throws<ArgumentException>(() => sequence.Start(1, [new(0, 100, "Wrong index")]));
            sequence.Start(1, plan); Throws<InvalidOperationException>(() => sequence.Start(1, plan));
            Check(sequence.PrepareNext(1, false, false, false) is null && sequence.State.Status == MacroSequenceStatus.Failed, "unavailable native shell is explicit failure");
            sequence.Start(1, plan); var pending = sequence.PrepareNext(1, true, false, false)!;
            sequence.Dispose(); sequence.ObserveStart(pending); sequence.ObserveEnd(pending, false); sequence.Dispose();
            Check(sequence.State.Status == MacroSequenceStatus.Disposed && sequence.State.Plan.Count == 0, "no late publication after dispose");
            Throws<ObjectDisposedException>(() => sequence.Start(1, plan));
        }),
    ];
}
