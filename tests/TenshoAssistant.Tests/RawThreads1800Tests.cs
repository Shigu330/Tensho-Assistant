// 0.1800 (X-793, review of 0.1799, M3): the ratchet of 0.1796 over the tests' raw threads took any « catch (Exception » in a thread's body,
// so a catch around part of the work passed while the rest could still end the test process, and it never looked whether the kept exception
// was read after the Join. The stricter checks (RawThreads1796Tests.KeptIn and Unchecked) on made sources; the scan of the real test files
// stays in RawThreads1796Tests.
internal static class RawThreads1800Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static string? Why(string source)
    {
        var threads = RawThreads1796Tests.Threads(source);
        Check(threads.Count == 1,"one thread in the made source: " + threads.Count);
        var kept = RawThreads1796Tests.KeptIn(threads[0].Body);
        Check(kept is not null,"its exception kept: " + threads[0].Body);
        return RawThreads1796Tests.Unchecked(source,threads[0].At,kept!);
    }

    private const string NewThread = "new " + "Thread(";

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Raw threads 1800: the whole body guarded and the exception stored: its target, in the forms the tests use",() =>
        {
            var bodies = new (string Body,string Target)[]
            {
                ("(() => { try { Work(); } catch (Exception error) { kept = error; } })","kept"),
                ("(() =>\n{\n    try { Work(); }\n    catch (Exception failure) { error.Value = failure; }\n})","error"),
                ("(() => { try { Work(); } catch (Exception error) { Interlocked.CompareExchange(ref unexpected, \"reader: \" + error, null); } })","unexpected"),
                ("(() => { try { Work(); } catch (IOException io) { other = io; } catch (Exception error) { lock (errors) errors.Add(error); } finally { Done(); } })","errors"),
                ("(() => { try { Work(); } catch (System.Exception error) { kept = error; } }, 1024 * 1024)","kept"),
            };
            foreach (var (body,target) in bodies)
                Check(RawThreads1796Tests.KeptIn(body) == target && RawThreads1796Tests.KeepsItsException(body),"kept in " + target + ": " + body + " → " + RawThreads1796Tests.KeptIn(body));
        }),
        ("Raw threads 1800: the review's case and its kin are no longer kept (part of the work outside the try, filtered, bare, nameless, dropped)",() =>
        {
            var bodies = new[]
            {
                "(() => { Work(); try { X(); } catch (Exception e) { } })",            // the review's example: Work() unguarded
                "(() => { Work(); try { X(); } catch (Exception e) { kept = e; } })",   // the same, the exception stored
                "(() => { try { X(); } catch (Exception e) { kept = e; } Work(); })",   // work after the try
                "(() => { try { X(); } catch (Exception e) when (e is IOException) { kept = e; } })",
                "(() => { try { X(); } catch { } })",
                "(() => { try { X(); } catch (Exception) { failed = true; } })",
                "(() => { try { X(); } catch (Exception e) { Console.WriteLine(\"lost\"); } })",
                "(() => { try { X(); } catch (IOException e) { kept = e; } })",          // another exception still ends the process
                "(() => { try { X(); } finally { Done(); } })",
                "(() => Work())",
                "(Work)",
            };
            foreach (var body in bodies)
                Check(RawThreads1796Tests.KeptIn(body) is null && !RawThreads1796Tests.KeepsItsException(body),"not kept: " + body);
        }),
        ("Raw threads 1800: the kept exception is read by a Check after the thread's Join, in the method or lambda around it",() =>
        {
            const string head = "void Case()\n{\n    Exception? kept = null;\n";
            const string start = "    var t = " + NewThread + "() => { try { Work(); } catch (Exception error) { kept = error; } });\n    t.Start();\n";
            Check(Why(head + start + "    Check(t.Join(1000),\"ended\");\n    Check(kept is null,\"none: \" + kept);\n}\n") is null,"joined, then checked");
            Check(Why("void Case()\n{\n    Exception? kept = null;\n    Thread t;\n    using (var gate = Open())\n    {\n        t = " + NewThread
                + "() => { try { Work(); } catch (Exception error) { kept = error; } });\n        t.Start();\n    }\n    t.Join();\n    Check(kept is null,\"\");\n}\n") is null,
                "a statement block around the thread is passed through");
            var before = Why(head + start + "    Check(kept is null,\"none\");\n    t.Join();\n}\n");
            Check(before is not null && before.Contains("kept is not read by a Check after the thread's Join",StringComparison.Ordinal),"checked before the Join only: " + before);
            Check(Why(head + start + "    Check(kept is null,\"none\");\n}\n") is not null,"never joined");
            Check(Why(head + start + "    var s = string.Join(\",\", items);\n    Check(kept is null,s);\n}\n") is not null,"string.Join is not a thread's Join");
            Check(Why(head + start + "    t.Join();\n}\nvoid Other()\n{\n    Check(kept is null,\"\");\n}\n") is not null,"a Check in another method does not count");
        }),
        ("Raw threads 1800: a helper storing into its parameter (as FairLock1309's Greedy) is followed to each of its calls",() =>
        {
            const string helper = "static Thread Start(int ms,StrongBox<Exception?> box)\n{\n    var t = " + NewThread
                + "() => { try { Work(ms); } catch (Exception failure) { box.Value = failure; } });\n    t.Start();\n    return t;\n}\n";
            const string good = "void Good()\n{\n    var error = new StrongBox<Exception?>();\n    var t = Start(10,error);\n    t.Join();\n    Check(error.Value is null,\"\");\n}\n";
            const string bad = "void Bad()\n{\n    var lost = new StrongBox<Exception?>();\n    Start(10,lost).Join();\n}\n";
            Check(Why(helper + good) is null,"its call reads the box after the Join");
            var missed = Why(helper + good + bad);
            Check(missed is not null && missed.Contains("the call of Start at line 17 does not read lost by a Check after a Join",StringComparison.Ordinal),"a call that never reads it: " + missed);
            var never = Why(helper);
            Check(never is not null && never.Contains("its helper Start is never called",StringComparison.Ordinal),"never called: " + never);
        }),
    ];
}
