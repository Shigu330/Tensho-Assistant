// 0.1428, X-436 and X-437 (review of block 0.1420-0.1427): the public text of 0.1427 no longer calls the cause of X-433 proven (CHANGELOG
// with its players' line, code comment, test header, evidence); the warm-up and the readers' compiler time their compilations only when asked
// (developer mode, M2); the evidence of X-434 keeps the probe's raw output, and the place of [retainer+0x34] among the line's parameters is
// counted on it (M1).
using System.Reflection;
using System.Text.RegularExpressions;
using TenshoAssistant.Diagnostics;

// A reader's class of its own (the compiler compiles a handler's outermost class).
internal sealed class Reader1428 { public void OnUpdate() { } }

internal static class Review1428Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static readonly TimeSpan Long = TimeSpan.FromSeconds(30);

    /// <summary>The section of a version, in CHANGELOG.md or, once archived, in docs/changelog/.</summary>
    private static string Section(string version)
    {
        foreach (var file in new[] { Path.GetFullPath("CHANGELOG.md") }.Concat(Directory.GetFiles(Path.GetFullPath("docs/changelog"), "*.md")))
        {
            var text = Read(file);
            var start = text.IndexOf("\n## " + version + "\n", StringComparison.Ordinal);
            if (start < 0) continue;
            var end = text.IndexOf("\n## ", start + 4, StringComparison.Ordinal);
            return end < 0 ? text[start..] : text[start..end];
        }
        throw new Exception("no section " + version);
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review 1428 (X-436): the 0.1427 section says the cause not proven, and no longer promises the players fewer freezes", () =>
        {
            var section = Section("0.1427.0");
            foreach (var claim in new[] { "and fixed", "et corrigée", "was preempted in the middle of a compilation", "était préempté au milieu d'une compilation", "Fewer short freezes", "moins de petits gels", "- For players:", "- Pour les joueurs :" })
                Check(!section.Contains(claim, StringComparison.Ordinal), "still in 0.1427: " + claim);
            Check(section.Contains("cause non prouvée", StringComparison.Ordinal) && section.Contains("cause not proven", StringComparison.Ordinal), "said in both halves");
            Check(section.Contains("Ni gain ni dommage mesurés", StringComparison.Ordinal) && section.Contains("Neither gain nor harm measured", StringComparison.Ordinal), "nor the change's effect");
        }),
        ("Review 1428 (X-436, X-437 M3): the code comment, the test header and the evidence no longer call the cause proven, nor the change harmless", () =>
        {
            var warmup = Read("src/TenshoAssistant/Diagnostics/JitWarmup.cs");
            Check(!warmup.Contains("Below normal, the thread was preempted", StringComparison.Ordinal) && warmup.Contains("Not proven to be the cause", StringComparison.Ordinal), "the comment");
            Check(!Regex.IsMatch(warmup, @"background thread below\s*(///\s*)?normal priority"), "the summary no longer says below normal");
            var header = Read("tests/TenshoAssistant.Tests/WarmupPriority1427Tests.cs").Split('\n')[0];
            Check(!header.Contains("(fix,", StringComparison.Ordinal) && header.Contains("cause not proven", StringComparison.Ordinal), header);
            var evidence = Read("docs/evidence/0.1427-warmup-priority.md");
            Check(!Regex.IsMatch(evidence, @"il ne\s+nuit pas") && evidence.Contains("Ni gain ni dommage n'est mesuré", StringComparison.Ordinal) && evidence.Contains("neither gain nor harm is measured", StringComparison.Ordinal), "the evidence");
            Check(!evidence.Contains("à corriger à la prochaine version", StringComparison.Ordinal), "the comment said corrected");
        }),
        ("Review 1428 (X-437 M2): a warm-up times its compilations only when asked; the plugin asks the developer mode", () =>
        {
            using var quiet = JitWarmup.Start(() => [typeof(SlowCompilations)], TimeSpan.Zero);
            Check(quiet.Wait(Long) && quiet.Prepared > 0, "compiled");
            Check(quiet.Slowest == "", "not asked: nothing timed: " + quiet.Slowest);
            var off = 0;
            using var refused = JitWarmup.Start(() => [typeof(SlowCompilations)], TimeSpan.Zero, measure: () => { Interlocked.Increment(ref off); return false; });
            Check(refused.Wait(Long) && refused.Prepared > 0 && refused.Slowest == "" && Volatile.Read(ref off) > 0, "asked, the answer no: nothing timed");
            using var measured = JitWarmup.Start(() => [typeof(SlowCompilations)], TimeSpan.Zero, measure: static () => true);
            Check(measured.Wait(Long) && measured.Slowest.Contains(" ms (", StringComparison.Ordinal), "asked: timed: " + measured.Slowest);
            Check(Read("src/TenshoAssistant/MainWindow.cs").Contains("measure: static ()=>TenshoAssistant.Competitive.CompetitiveGate.Developer?.Invoke()==true,", StringComparison.Ordinal), "the window's warm-up asks the developer mode");
        }),
        ("Review 1428 (X-437 M2): the readers' compiler times its compilations only when asked; the plugin asks the developer mode", () =>
        {
            var handler = ((Action)new Reader1428().OnUpdate).Method;
            using (var quiet = new ReaderCompiler(prepare: _ => Thread.Sleep(1)))
            {
                var ticket = quiet.Add(handler);
                Check(SpinWait.SpinUntil(() => quiet.Idle && ticket.Ready, Long), "compiled");
                Check(quiet.Slowest == "", "not asked: nothing timed: " + quiet.Slowest);
            }
            using (var measured = new ReaderCompiler(prepare: _ => Thread.Sleep(1)) { Measure = static () => true })
            {
                var ticket = measured.Add(handler);
                Check(SpinWait.SpinUntil(() => measured.Idle && ticket.Ready, Long), "compiled");
                Check(measured.Slowest.Contains("Reader1428.", StringComparison.Ordinal), "asked: timed: " + measured.Slowest);
            }
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("CompetitiveGate.Compiler.Measure=static ()=>TenshoAssistant.Competitive.CompetitiveGate.Developer?.Invoke()==true;", StringComparison.Ordinal), "the plugin gives the developer mode");
        }),
        ("Review 1428 (X-437 M1): on the probe's raw output, the line's parameters are the name, the town, the count, then [rsi+34h], MarketExpire", () =>
        {
            const string log = "docs/evidence/0.1420-retainer-sales-end-probe.log";
            var lines = Read(log).Split('\n');
            Check(lines.Contains("  0x34 UInt32 MarketExpire") && lines.Contains("  0x31 Byte MarketItemCount") && lines.Contains("  0x30 RetainerTown Town") && lines.Contains("  0x08 FixedSizeArray32`1 _name"), "the offsets, from the probe");
            var block = lines.SkipWhile(line => !line.StartsWith("=== row 2315 at 0x140E377E5", StringComparison.Ordinal)).Skip(1).TakeWhile(line => !line.StartsWith("===", StringComparison.Ordinal)).ToList();
            var code = block.Select(line => Regex.Match(line, @"^[ >] ([0-9A-F]{8}) (.+)$")).Where(match => match.Success).Select(match => (At: Convert.ToInt64(match.Groups[1].Value, 16), Text: match.Groups[2].Value)).ToList();
            Check(code.Count > 400 && code.Zip(code.Skip(1)).All(pair => pair.First.At < pair.Second.At), "the function's code, in address order");
            // The parameter list: made empty, then each value appended at its end, inline (its size + 1) or by the append call with the list.
            var made = code.FindIndex(line => line.Text == "movdqu [rsp+48h],xmm0");
            var choice = code.FindIndex(line => line.Text == "cmp byte [rsi+28h],0");
            Check(made > 0 && choice > made && code.Take(made).Any(line => line.Text == "mov [rsp+68h],rdi"), "the list made empty before the values, the line chosen after");
            var appended = new List<string>();
            string? source = null;
            for (var index = made + 1; index < choice; index++)
            {
                var text = code[index].Text;
                if (Regex.Match(text, @"^(?:lea rax,|movzx eax,byte |mov eax,)\[rsi\+(8|30h|31h|34h)\]$") is { Success: true } load) source = "rsi+" + load.Groups[1].Value;
                else if (text is "mul r15d" or "mov [rbp-70h],r15d") source = "r15d";
                var inline = text == "inc qword [rsp+68h]";
                var call = text == "call 00000001401EAFC0h" && code.Skip(index - 10).Take(10).Any(line => line.Text == "lea rcx,[rsp+48h]");
                if (!inline && !call) continue;
                appended.Add(source ?? "?");
                source = null;
            }
            Check(string.Join(" ", appended) == "rsi+8 rsi+30h rsi+31h rsi+34h r15d r15d", "the values appended, in order: " + string.Join(" ", appended));
            // So lstr1 the name, lnum2 the town, lnum3 the count, lnum4 [rsi+34h] = MarketExpire; the evidence says so and cites the raw output.
            var evidence = Read("docs/evidence/0.1420-retainer-sales-end-code.md");
            Check(evidence.Split("0.1420-retainer-sales-end-probe.log").Length >= 4, "the evidence cites the raw output in both halves");
            foreach (var quoted in new[] { "movzx eax,byte [rsi+30h]", "movzx eax,byte [rsi+31h]", "mov eax,[rsi+34h]", "cmp byte [rsi+31h],0", "movdqu [rsp+48h],xmm0", "lea rax,[rsi+8]", "call 00000001401EAFC0h" })
                Check(code.Any(line => line.Text == quoted), "what the evidence quotes is in the raw output: " + quoted);
        }),
    ];
}
