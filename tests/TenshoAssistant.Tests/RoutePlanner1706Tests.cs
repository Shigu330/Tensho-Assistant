using TenshoAssistant.FreeCompany;

// X-604 (0.1706, the user's request of 18/11: one route planner for both kinds of vessels): the search class shared by the planner's three
// searches (PlannerSearch), and the wiring of the one planner (the kind at its top, the common part, the airship sectors tab opening it).
internal static class RoutePlanner1706Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");

    private static T Wait<T>(PlannerSearch<T> search, out Exception? error)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(10))
        {
            if (search.Take(out var result, out error)) return result;
            if (error is not null) return default!;
            Thread.Sleep(5);
        }
        throw new Exception("the search did not end");
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Route planner 1706: a search runs once per set of inputs, its result read once; new inputs cancel the running one", () =>
        {
            using var search = new PlannerSearch<int>();
            Check(search.Needs("a") && !search.Searching, "nothing yet: due");
            using var gate = new ManualResetEventSlim(false);
            using var started = new ManualResetEventSlim(false);
            var firstToken = CancellationToken.None;
            search.Start("a", token => { firstToken = token; started.Set(); gate.Wait(TimeSpan.FromSeconds(10)); return 1; });
            Check(started.Wait(TimeSpan.FromSeconds(10)), "the first search started on its worker");
            Check(!search.Needs("a") && search.Searching, "same inputs: not due again, running");
            Check(!search.Take(out _, out var none) && none is null, "running: nothing to read yet");
            search.Start("b", token => 2);
            gate.Set();
            Check(Wait(search, out var error) == 2 && error is null, "the new inputs' result");
            Check(firstToken.IsCancellationRequested, "the first search was cancelled");
            Check(!search.Searching && !search.Take(out _, out _), "read once");
            Check(!search.Needs("b") && search.Needs("c"), "due only when the inputs change");
        }),
        ("Route planner 1706: a failure is given once; Again and Cancel make the same inputs due again", () =>
        {
            using var search = new PlannerSearch<int>();
            search.Start("a", token => throw new InvalidOperationException("broken"));
            Wait(search, out var error);
            Check(error is AggregateException { InnerException: InvalidOperationException }, "the failure: " + error);
            Check(!search.Take(out _, out var again) && again is null, "given once");
            Check(!search.Needs("a"), "the failed inputs are not run again by themselves");
            search.Again();
            Check(search.Needs("a"), "Again: due with the same inputs (prices fetched since)");
            search.Start("a", token => { token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)); token.ThrowIfCancellationRequested(); return 3; });
            search.Cancel();
            Check(!search.Searching && search.Needs("a"), "Cancel (the window closed): nothing running, due again when drawn");
            Check(!search.Take(out _, out var cancelled) && cancelled is null, "a cancelled search gives neither a result nor a failure");
        }),
        ("Route planner 1706: one planner in the Routes tab: the kind at its top, the common part for both kinds, the three searches", () =>
        {
            var planner = Read("MainWindow.Planner.cs");
            Check(planner.Contains("if(plannerKind==VesselKind.Airship)DrawAirshipRoutes();", StringComparison.Ordinal) && planner.Contains("else DrawSubmarineRoutes();", StringComparison.Ordinal), "the kind chooses the part");
            Check(planner.Contains("VesselBuilds.EffectivePlan(VesselBuild.FromList(kind,", StringComparison.Ordinal), "the planned build for both kinds");
            Check(Read("MainWindow.FreeCompany.cs").Contains("new(\"Routes\",localization.Get(\"FreeCompany.TabRoutes\"),DrawRoutePlanner,request==1),", StringComparison.Ordinal), "in the Routes tab");
            var submarines = Read("MainWindow.RoutePlanner.cs");
            Check(submarines.Contains("var chosen=DrawPlannerVessel(VesselKind.Submarine,", StringComparison.Ordinal) && submarines.Contains("DrawPlannerStats(VesselKind.Submarine);", StringComparison.Ordinal), "submarines: the common part");
            Check(submarines.Contains("submarineSearch.Start(key,token=>VoyagePlanner.Plan(", StringComparison.Ordinal), "submarines: PlannerSearch");
            var airships = Read("MainWindow.AirshipPlanner.cs");
            Check(airships.Contains("DrawPlannerVessel(VesselKind.Airship);", StringComparison.Ordinal) && airships.Contains("DrawPlannerStats(VesselKind.Airship);", StringComparison.Ordinal), "airships: the common part");
            Check(!airships.Contains("Task.Run(", StringComparison.Ordinal) && !submarines.Contains("Task.Run(", StringComparison.Ordinal), "no search written by hand any more");
            var sectors = Read("MainWindow.AirshipSectors.cs");
            Check(!sectors.Contains("DrawAirshipPlanner(catalog);", StringComparison.Ordinal) && sectors.Contains("{plannerKind=VesselKind.Airship;freeCompanyTabRequest=1;}", StringComparison.Ordinal), "the airship sectors tab opens the planner");
            Check(Read("MainWindow.Lifetime.cs").Contains("submarineSearch.Cancel();", StringComparison.Ordinal), "the searches stop with the window");
            var en = Read("Localization/Languages/en.json"); var fr = Read("Localization/Languages/fr.json");
            foreach (var key in new[] { "KindTip", "PlanRoutes", "PlanRoutesTip", "ChosenCount" })
                Check(en.Contains($"    \"{key}\": ", StringComparison.Ordinal) && fr.Contains($"    \"{key}\": ", StringComparison.Ordinal), "text: " + key);
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.1706-route-planners.md")), "the comparison, public");
        }),
    ];
}
