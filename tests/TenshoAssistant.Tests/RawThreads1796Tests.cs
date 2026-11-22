// 0.1796 (X-783): the tests' process fell once (« tests exit 127 », a trace ending in Thread.StartCallback, 22/11 during relay 177): an
// exception on a raw thread (new Thread) ends the whole process, every test with it, where the runner would have kept it for its test.
// Every raw thread a test starts keeps its exception for the test (a catch (Exception …) in its body, checked after the join), as
// FairLock1309's busy store did since 0.1794; the runner's own threads (TestRunner.cs) catch per test. A ratchet over the test files.
internal static class RawThreads1796Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static readonly string Needle = "new " + "Thread(";

    /// <summary>The raw threads of a source: (line, body) for each « new Thread( » in code (not in a string or a comment), its body the text
    /// up to the matching parenthesis.</summary>
    internal static List<(int Line,string Body)> Threads(string source)
    {
        var found = new List<(int,string)>();
        var code = Code(source);
        for (var at = code.IndexOf(Needle,StringComparison.Ordinal); at >= 0; at = code.IndexOf(Needle,at + 1,StringComparison.Ordinal))
        {
            var open = at + Needle.Length - 1;
            var depth = 0;
            var end = open;
            for (; end < code.Length; end++)
            {
                if (code[end] == '(') depth++;
                else if (code[end] == ')' && --depth == 0) break;
            }
            var line = 1 + source.AsSpan(0,at).Count('\n');
            found.Add((line,source.Substring(open,Math.Min(end,source.Length - 1) - open + 1)));
        }
        return found;
    }

    /// <summary>The source with its strings, characters and comments blanked out (same length, same lines), so that only code is searched.</summary>
    internal static string Code(string source)
    {
        var text = source.ToCharArray();
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
            {
                while (i < text.Length && text[i] != '\n') text[i++] = ' ';
            }
            else if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
            {
                var close = source.IndexOf("*/",i + 2,StringComparison.Ordinal);
                var stop = close < 0 ? text.Length : close + 2;
                for (; i < stop; i++) if (text[i] != '\n') text[i] = ' ';
            }
            else if (c == '"' || c == '\'')
            {
                var verbatim = c == '"' && i > 0 && (source[i - 1] == '@' || (i > 1 && source[i - 1] == '$' && source[i - 2] == '@'));
                i++;
                while (i < text.Length)
                {
                    if (!verbatim && text[i] == '\\') { text[i] = ' '; if (i + 1 < text.Length && text[i + 1] != '\n') text[i + 1] = ' '; i += 2; continue; }
                    if (text[i] == c)
                    {
                        if (verbatim && i + 1 < text.Length && text[i + 1] == '"') { text[i] = ' '; text[i + 1] = ' '; i += 2; continue; }
                        break;
                    }
                    if (text[i] == '\n' && !verbatim) break;
                    text[i] = ' ';
                    i++;
                }
                i++;
            }
            else i++;
        }
        return new string(text);
    }

    internal static bool KeepsItsException(string body) =>
        body.Contains("catch (Exception",StringComparison.Ordinal) || body.Contains("catch(Exception",StringComparison.Ordinal);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Raw threads 1796: the scan finds a raw thread in code only, with its whole body, and tells one that keeps its exception",() =>
        {
            var source = "var a = new Thread(() => { Work(\"(\"); });\n"
                + "// var b = new Thread(() => Crash());\n"
                + "Check(text.Contains(\"var worker=new Thread(\"),\"wiring\");\n"
                + "var c = new Thread(() =>\n{\n    try { Work(')'); }\n    catch (Exception error) { kept = error; }\n}) { IsBackground = true };\n";
            var threads = Threads(source);
            Check(threads.Count == 2,"two threads in code (not the comment nor the string): " + threads.Count);
            Check(threads[0].Line == 1 && threads[0].Body == "(() => { Work(\"(\"); })" && !KeepsItsException(threads[0].Body),"the first, unguarded: " + threads[0].Body);
            Check(threads[1].Line == 4 && threads[1].Body.EndsWith("catch (Exception error) { kept = error; }\n})",StringComparison.Ordinal)
                && KeepsItsException(threads[1].Body),"the second, guarded: " + threads[1].Body);
        }),
        ("Raw threads 1796: behaviour: a raw thread's exception kept for the test reaches the test, the process goes on",() =>
        {
            Exception? kept = null;
            var thread = new Thread(() =>
            {
                try { throw new IOException("the lock was not given in time"); }
                catch (Exception error) { kept = error; }
            }) { IsBackground = true };
            thread.Start();
            Check(thread.Join(TimeSpan.FromSeconds(10)),"ended");
            Check(kept is IOException { Message: "the lock was not given in time" },"kept for the test: " + kept);
        }),
        ("Raw threads 1796: every raw thread a test starts keeps its exception for the test (a crash on it ends every test: X-783)",() =>
        {
            var folder = Path.GetFullPath("tests/TenshoAssistant.Tests");
            var bare = new List<string>();
            var count = 0;
            foreach (var file in Directory.GetFiles(folder,"*.cs").Order(StringComparer.Ordinal))
            {
                if (Path.GetFileName(file) == "TestRunner.cs") continue;   // its threads run the tests, each one caught (RunGroup)
                foreach (var (line,body) in Threads(File.ReadAllText(file)))
                {
                    count++;
                    if (!KeepsItsException(body)) bare.Add(Path.GetFileName(file) + ":" + line);
                }
            }
            Check(count >= 7,"the test threads found (" + count + "): the scan reads the files");
            Check(bare.Count == 0,"raw threads whose exception would end the test process: " + string.Join(", ",bare));
            var runner = File.ReadAllText(Path.Combine(folder,"TestRunner.cs")).ReplaceLineEndings("\n");
            Check(runner.Contains("try { run(); }\n            catch (SkipTestException skip) { skipped = skip.Message; }\n            catch (Exception failure) { error = failure.ToString(); }",StringComparison.Ordinal),
                "the runner's threads catch each test's exception");
        }),
    ];
}
