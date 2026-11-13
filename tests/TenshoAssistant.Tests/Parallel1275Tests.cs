using System.Text.RegularExpressions;
using TenshoAssistant;
using TenshoAssistant.Caching;

// X-267 (0.1275, R36): the tests run in parallel (TestRunner.cs) and the flaky cache test is fixed at its cause (a file held an instant by a
// scanner: the replace is tried again, in AtomicFile.OverwriteWhenFree, used by DataCache and AtomicFile.Write).
internal static class Parallel1275Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");
    private static void InTemp(Action<string> run)
    {
        var folder = Path.Combine(Path.GetTempPath(), "tensho-1275-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try { run(folder); }
        finally { try { Directory.Delete(folder, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }
    private static TestGroup Group(string name, bool serial, params (string Name, Action Run)[] tests) => new(name, tests, serial);
    private static readonly DateOnly Today = new(2026, 11, 13);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Parallel 1275: behaviour: the file replaced is held an instant by another program: the replace is tried again and the new file is in place", () => InTemp(folder =>
        {
            var path = Path.Combine(folder, "a.json"); var next = path + ".new.tmp";
            File.WriteAllText(path, "old"); File.WriteAllText(next, "new");
            var holder = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); // no FileShare.Delete: as a scanner reading it
            var failed = new List<int>();
            try { AtomicFile.OverwriteWhenFree(next, path, wait: attempt => { failed.Add(attempt); holder.Dispose(); }); }
            finally { holder.Dispose(); }
            Check(failed.SequenceEqual([1]), "refused once while held, then done: " + string.Join(",", failed));
            Check(File.ReadAllText(path) == "new" && !File.Exists(next), "the new file in place, nothing left behind");
            // Held for every attempt: an error at the last one, the old file as it was, the new one still there (the caller removes it).
            File.WriteAllText(next, "newer");
            var tries = new List<int>();
            using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var refused = false;
                try { AtomicFile.OverwriteWhenFree(next, path, attempts: 3, wait: tries.Add); }
                catch (IOException) { refused = true; }
                catch (UnauthorizedAccessException) { refused = true; }
                Check(refused && tries.SequenceEqual([1, 2]), "given up after the last attempt: " + string.Join(",", tries));
            }
            Check(File.ReadAllText(path) == "new" && File.ReadAllText(next) == "newer", "nothing half done");
        })),
        ("Parallel 1275: behaviour: the disk cache writes over a file another program holds an instant (a lost write on the CI of 0.1267)", () => InTemp(folder =>
        {
            using var cache = new DataCache(folder);
            var date = DateTimeOffset.UtcNow.AddHours(-1);
            Check(cache.WriteAsync("prices", "71:42", "v1", 1, date).WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult(), "first write");
            var file = Directory.GetFiles(folder, "*.json").Single();
            var holder = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var release = Task.Run(async () =>
            {
                // Released once the new version is written beside (its move is refused meanwhile), well within the attempts (750 ms).
                for (var wait = 0; wait < 200 && Directory.GetFiles(folder, "*.tmp").Length == 0; wait++) await Task.Delay(5);
                await Task.Delay(30); holder.Dispose();
            });
            var written = cache.WriteAsync("prices", "71:42", "v1", 2, date.AddMinutes(1)).WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
            release.WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult(); holder.Dispose();
            Check(written && cache.Error is null, "written once the file is free: " + cache.Error);
            Check(cache.ReadAsync<int>("prices", "71:42", "v1").WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult()?.Value == 2, "the new value read back");
            Check(Directory.GetFiles(folder, "*.tmp").Length == 0, "no temporary file left");
            Wait(cache.FlushAsync());
        })),
        ("Parallel 1275: the classes run at the same time, the tests of a class in order, a [SerialTests] class alone after; results in the order of the list", () =>
        {
            using var both = new Barrier(2);
            var order = new List<string>(); var running = 0; var serialAlone = false;
            void Step(string name) { lock (order) order.Add(name); }
            // Each of A and B waits for the other: only a parallel run gets past the barrier (5 s, then it fails).
            var groups = new[]
            {
                Group("A", false, ("a1", () => { Interlocked.Increment(ref running); Check(both.SignalAndWait(TimeSpan.FromSeconds(5)), "B ran at the same time"); Step("a1"); }),
                    ("a2", () => { Step("a2"); Interlocked.Decrement(ref running); })),
                Group("S", true, ("s1", () => { serialAlone = Volatile.Read(ref running) == 0; Step("s1"); })),
                Group("B", false, ("b1", () => { Interlocked.Increment(ref running); Check(both.SignalAndWait(TimeSpan.FromSeconds(5)), "A ran at the same time"); Step("b1"); Interlocked.Decrement(ref running); }),
                    ("b2", () => throw new InvalidOperationException("broken"))),
            };
            var run = ParallelTests.Run(groups, 4, [], Today);
            Check(run.Results.Select(result => result.Name).SequenceEqual(["a1", "a2", "s1", "b1", "b2"]), "said in the order of the list");
            Check(run.Results.Take(4).All(result => result.Passed) && !run.Results[4].Passed && run.Results[4].Error!.Contains("broken"), "a failure is kept with its error: " + run.Results[0].Error);
            Check(order.IndexOf("a1") < order.IndexOf("a2") && order[^1] == "s1" && serialAlone, "a class in order; the serial one last and alone: " + string.Join(",", order));
            Check(run.Total == 5 && run.Passed == 4 && run.Failed == 1, "counted");
            Check(ParallelTests.Line(run, groups).StartsWith("PARALLEL 3 classes on 4 threads in ", StringComparison.Ordinal) && ParallelTests.Line(run, groups).Contains("alone after: S;"), ParallelTests.Line(run, groups));
        }),
        ("Parallel 1275: a quarantined test still runs and counts; its failure is said, not counted; past its day, without a reason or naming no test, the entry fails", () =>
        {
            var groups = new[] { Group("A", false, ("flaky", () => throw new IOException("held")), ("fine", () => { })) };
            var held = ParallelTests.Run(groups, 2, [("flaky", Today, "a file held by a scanner, cause looked for")], Today);
            Check(held.Total == 2 && held.Passed == 2 && held.Failed == 0, "in the count, not a failure");
            Check(held.Results[0].Note!.StartsWith("QUARANTINE flaky (until 2026-11-13: a file held by a scanner, cause looked for) failed this run", StringComparison.Ordinal), held.Results[0].Note!);
            var ended = ParallelTests.Run(groups, 2, [("flaky", Today.AddDays(-1), "old")], Today);
            Check(ended.Failed == 2 && ended.Notes.Single().StartsWith("FAIL quarantine ended on 2026-11-12: flaky", StringComparison.Ordinal), "past its day: the test fails, and the entry");
            Check(ParallelTests.Run(groups, 2, [("flaky", Today, " ")], Today).Notes.Single() == "FAIL quarantine without a reason: flaky", "a reason is written");
            Check(ParallelTests.Run(groups, 2, [("gone", Today, "x")], Today).Notes.Single() == "FAIL quarantine names no test: gone", "a full run checks the name");
            Check(ParallelTests.Run(groups, 2, [("gone", Today, "x")], Today, fullRun: false).Notes.Length == 0, "« --only » runs part of the list: not checked");
            Check(Quarantine.Entries.All(entry => entry.Reason.Trim().Length > 0), "every entry of the project has its reason");
        }),
        ("Parallel 1275: the registry finds every static class with a static « Cases », property or field, public or internal; « --only » filters each class", () =>
        {
            var found = TestRegistry.Classes(typeof(Parallel1275Tests).Assembly).Select(type => type.Name).ToHashSet();
            Check(found.Contains(nameof(Parallel1275Tests)) && found.Contains("CacheTests") && found.Contains("VerifySpeed1202Tests"), "properties");
            var fields = typeof(Parallel1275Tests).Assembly.GetTypes().Where(type => type.GetField("Cases", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static) is not null).ToArray();
            Check(fields.Length >= 25 && fields.All(type => found.Contains(type.Name)), "fields too: " + fields.Length);
            var groups = TestRegistry.Groups([("inline", () => { })], typeof(Parallel1275Tests).Assembly);
            Check(groups[0].Name == TestRegistry.ProgramGroup && groups.Length == found.Count + 1 && groups.Select(group => group.Name).Distinct().Count() == groups.Length, "Program.cs first, each class once");
            var only = GroupFilter.Only(groups, ["--only", "Parallel 1275: the registry"]);
            Check(only.Length == 1 && only[0].Name == nameof(Parallel1275Tests) && only[0].Tests.Length == 1, "one class left, one test");
            Check(GroupFilter.Only(groups, []).Length == groups.Length && GroupFilter.Full([]) && !GroupFilter.Full(["--only", "x"]) && GroupFilter.Full(["--only", ""]), "a full run keeps them all");
        }),
        ("Parallel 1275: each test makes its own temporary folder: no fixed name under the temporary folder is created by a test", () =>
        {
            // Two tests running at the same time on one fixed folder would clean up each other's files. The names listed are only strings
            // given to a function that does not touch the disk (LocalLinkProtocol.ChangeCategory).
            var allowed = new[] { "DesktopData1147Tests.cs:\"tensho-1147-link\"", "DesktopData1147Tests.cs:\"TenshoAssistant.json\"" };
            var fixedNames = Directory.EnumerateFiles(Path.GetFullPath("tests/TenshoAssistant.Tests"), "*.cs")
                .SelectMany(file => Regex.Matches(File.ReadAllText(file), @"Path\.Combine\(\s*Path\.GetTempPath\(\)\s*,\s*(""[^""]*"")\s*\)").Select(match => Path.GetFileName(file) + ":" + match.Groups[1].Value))
                .Where(name => !allowed.Contains(name)).ToArray();
            Check(fixedNames.Length == 0, "a fixed name under the temporary folder (add a Guid): " + string.Join(", ", fixedNames));
        }),
        ("Parallel 1275: wiring: Program.cs runs the groups of the registry in parallel; New-Tool.ps1 and the patch helper add no test to a list any more", () =>
        {
            var program = Read("tests/TenshoAssistant.Tests/Program.cs");
            Check(program.Contains("var groups = TestRegistry.Groups(tests, typeof(TestRegistry).Assembly);") && program.Contains("ParallelTests.Run(groups, Environment.ProcessorCount, Quarantine.Entries,"), "registry, then the parallel run");
            Check(!program.Contains("allTests") && !Regex.IsMatch(program, @"\.Concat\(\w+\.Cases\)"), "no hand list");
            Check(!Read("scripts/New-Tool.ps1").Contains(".Concat($last.Cases)"), "New-Tool.ps1 writes no registration");
            var verify = Read("scripts/Verify.ps1");
            var started = verify.IndexOf("$taskTests = Start-Process -FilePath $DotNet -ArgumentList @('run', '--project', 'tests/TenshoAssistant.Tests'", StringComparison.Ordinal);
            var waited = verify.IndexOf("$taskTests.WaitForExit()", StringComparison.Ordinal);
            Check(started > verify.IndexOf("Add-TaskTime 'build'", StringComparison.Ordinal) && started < verify.IndexOf("Add-TaskTime 'languages'", StringComparison.Ordinal)
                && waited > verify.IndexOf("Add-TaskTime 'desktop update'", StringComparison.Ordinal) && waited < verify.IndexOf("Write-Output (\"TIMES \"", StringComparison.Ordinal)
                && verify.Contains("if ($taskTests.ExitCode -ne 0) { throw 'Managed tests failed.' }") && verify.Contains("& taskkill.exe /T /F /PID $taskTests.Id"),
                "Verify: the tests start after the build, alongside the rest, waited for before the times; a failure fails; never left running");
            Check(Read("src/TenshoAssistant/Caching/DataCache.cs").Contains("AtomicFile.OverwriteWhenFree(temporary,path);temporary=null;")
                && Read("src/TenshoAssistant/AtomicFile.cs").Contains("OverwriteWhenFree(temporary,path);"), "the cache and every atomic write try again");
        }),
    ];

    private static void Wait(Task task) => task.WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
}
