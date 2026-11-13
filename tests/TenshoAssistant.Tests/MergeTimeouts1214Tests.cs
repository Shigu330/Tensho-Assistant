// 0.1214 (X-146, GPT audit of 11/11, A10): the merge tool's remote calls have a deadline. The gists were already read bounded (bytes,
// decompression, facts: X-174) and a gist that cannot be read was already reported for its issue while the batch went on (X-168); what was
// missing: no deadline on a gist file (a server sending a byte at a time held the batch for good) nor on the GitHub CLI calls. The behaviour
// is in the tool's selftest (a fake answer that trickles past the deadline, run by check.sh); here, the wiring.
internal static class MergeTimeouts1214Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Merge timeouts 1214: every remote call of the merge tool has a deadline; a gist too slow is skipped, the batch goes on",() =>
        {
            var tool=Read("tools/discoveries-merge.py");
            Check(tool.Contains("REMOTE_TIMEOUT=60") && tool.Contains("with (opener or urllib.request.urlopen)(url,timeout=deadline) as answer:")
                && tool.Contains("if time.monotonic()>end:raise TimeoutError(\"the gist file came too slowly\")"),"a gist file: deadline in all, trickle included");
            var calls=tool.Split("subprocess.run([\"gh\"").Length-1;
            var timed=tool.Split("timeout=REMOTE_TIMEOUT)").Length-1;
            Check(calls==4 && timed==4,$"each GitHub CLI call has the deadline ({timed} of {calls})");
            Check(tool.Contains("except Exception as error:print(f\"#{issue['number']} {issue['title']}: gist unreadable ({type(error).__name__}), skipped\");continue"),"reported for its issue, the batch goes on");
            Check(tool.Contains("X-146: a byte at a time past the deadline: stopped"),"selftest case");
        }),
    ];
}
