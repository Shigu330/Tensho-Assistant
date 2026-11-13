using TenshoAssistant.FreeCompany;

// X-288 (0.1286): an airship and a submarine both out, but only one shown: each workshop list is filled by the game when its panel is
// opened, and a read of one kind replaced every vessel of the house. Behaviour tests on Voyages.Merge in both orders, with the 13/11 vessels.
internal static class VesselKinds1286Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static readonly DateTimeOffset Now = new(2026, 11, 13, 21, 46, 0, TimeSpan.FromHours(1));
    private const ulong House = 19987480711331856;
    private static readonly VoyageVessel Submarine = new() { HouseId = House, Kind = VesselKind.Submarine, Slot = 1, Name = "Submarine-1", Rank = 17,
        RegisteredAt = Now.AddHours(-1), ReturnAt = Now.AddHours(4).AddMinutes(23), Points = [9, 6], ReadAt = Now.AddMinutes(-10) };
    private static readonly VoyageVessel Airship = new() { HouseId = House, Kind = VesselKind.Airship, Slot = 1, Name = "Airship-1", Rank = 7,
        RegisteredAt = Now.AddMinutes(-5), ReturnAt = Now.AddHours(23).AddMinutes(36), ReadAt = Now };

    private static void BothOut(List<VoyageVessel> merged, string order)
    {
        Check(merged.Count == 2, order + ": two vessels, got " + merged.Count);
        Check(merged.Any(vessel => vessel.Kind == VesselKind.Submarine && vessel.ReturnAt == Submarine.ReturnAt), order + ": submarine kept");
        Check(merged.Any(vessel => vessel.Kind == VesselKind.Airship && vessel.ReturnAt == Airship.ReturnAt), order + ": airship kept");
        Check(Voyages.NextReturn(merged, Now) == Submarine.ReturnAt, order + ": next return is the submarine's");
        Check(merged.Count(vessel => Voyages.StateOf(vessel, Now) == VoyageState.Voyaging) == 2, order + ": both voyaging");
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("X-288: submarines read, then airships alone: both vessels kept (page and alarms)", () =>
        {
            var first = Voyages.Merge(null, House, [Submarine]);
            var second = Voyages.Merge(first, House, [Airship]);
            BothOut(second, "subs then airships");
            Check(Voyages.KeptKinds(first, House, [Airship]).SequenceEqual([VesselKind.Submarine]), "the kept kind is named");
        }),
        ("X-288: airships read, then submarines alone: both vessels kept", () =>
        {
            var first = Voyages.Merge(null, House, [Airship]);
            BothOut(Voyages.Merge(first, House, [Submarine]), "airships then subs");
        }),
        ("X-288: a new read of a kind replaces that kind only (new departure, vessel removed in the game)", () =>
        {
            var saved = Voyages.Merge(null, House, [Submarine, Submarine with { Slot = 2, Name = "Submarine-2" }, Airship]);
            var back = Submarine with { ReturnAt = Now.AddHours(30), RegisteredAt = Now };
            var merged = Voyages.Merge(saved, House, [back]);
            Check(merged.Count == 2 && merged.Single(vessel => vessel.Kind == VesselKind.Submarine).ReturnAt == back.ReturnAt, "slot 2 gone, slot 1 updated");
            Check(merged.Single(vessel => vessel.Kind == VesselKind.Airship) == Airship, "airship untouched");
            Check(Voyages.KeptKinds(saved, House, [Submarine, Airship]).Count == 0, "nothing kept when both kinds are read");
        }),
        ("X-288: another house's read keeps this house whole", () =>
        {
            var saved = Voyages.Merge(null, House, [Submarine, Airship]);
            var merged = Voyages.Merge(saved, 42, [Submarine with { HouseId = 42 }]);
            Check(merged.Count == 3 && merged.Count(vessel => vessel.HouseId == House) == 2, "both kept");
        }),
        ("X-288: the watcher logs the kept kinds and merges through Voyages.Merge", () =>
        {
            var watcher = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/FreeCompany/VoyageWatcher.Dalamud.cs"));
            Check(watcher.Contains("Voyages.KeptKinds(configuration.Voyages,houseId,read)") && watcher.Contains("var merged=Voyages.Merge(configuration.Voyages,houseId,read);"), "wiring");
        }),
    ];
}
