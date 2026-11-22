using System.Diagnostics;
using System.IO.Compression;
using System.Text;

// 0.1832 (X-830, security review X-810 I2; D-226 a and D-227 B+C, user's choices of 22/11): the public player files tied each fact's key to the
// GitHub login of their issue, and the common set gives the exact place and days of a fact only one player shares. Now the player files are
// anonymous (a random name, no issue, login, gist nor revision), the link issue ↔ file is the worker's private file outside the repository, the
// gists merged are listed hashed in merged.txt, and a pull request merges a batch of shares naming no issue nor account. The precision of the
// common set is kept (the user's choice), the limit said in the warning, the issue and the README. Here: the behaviour cases of both tools'
// selftests (run by Verify, so by CI too) and the repository's own files.
internal static class Anonymous1832Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static (int Code,string Output)? Selftest(string tool)
    {
        foreach(var name in new[]{"python","py"})
        {
            try
            {
                var start=new ProcessStartInfo(name){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};
                foreach(var argument in new[]{"-I","-X","utf8",Path.GetFullPath(tool),"--selftest"})start.ArgumentList.Add(argument);
                using var run=Process.Start(start);
                if(run is null)continue;
                var output=run.StandardOutput.ReadToEndAsync();var error=run.StandardError.ReadToEndAsync();
                if(!run.WaitForExit(240000)){try{run.Kill(true);}catch(InvalidOperationException){}return (-1,"timed out");}
                return (run.ExitCode,output.Result+error.Result);
            }
            catch(System.ComponentModel.Win32Exception){}
        }
        return null;   // no Python on this machine
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Anonymous 1832: the merge tool's behaviour: anonymize keeps the common set, a batch of shares in one sync, one file per account and withdraw through the private link",() =>
        {
            if(Selftest("tools/discoveries-merge.py") is not { } run)return;
            Check(run.Code==0 && !run.Output.Contains("FAIL",StringComparison.Ordinal),"the merge tool's selftest passes ("+run.Code+"): "+run.Output[^Math.Min(run.Output.Length,1500)..]);
            var tool=File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py"));
            foreach(var name in new[]{"X-830: anonymize: every file under a random id, the common set the same bytes","X-830: behaviour: no login, issue nor gist in any player file",
                "X-830: one sync, a batch of two shares","X-830: behaviour: the batch's pull request names no issue nor account",
                "X-830: a newer issue of the same account takes over its file (one file per account)","X-830: withdraw through the private link",
                "X-830: the link never inside the repository","X-829: a share keeps the Eorzea hour only"})
                Check(tool.Contains(name,StringComparison.Ordinal),"the tool's behaviour case: "+name);
        }),
        ("Anonymous 1832: the trusted merge: a batch, naming no issue nor account, replayed with the private link; anything else waits",() =>
        {
            if(Selftest("tools/discoveries-trust.py") is not { } run)return;
            Check(run.Code==0 && !run.Output.Contains("FAIL",StringComparison.Ordinal),"the trusted merge's selftest passes ("+run.Code+"): "+run.Output[^Math.Min(run.Output.Length,1500)..]);
            var tool=File.ReadAllText(Path.GetFullPath("tools/discoveries-trust.py"));
            foreach(var name in new[]{"X-830: a batch of two shares: merged only when every author is trusted","\"pr-names (its title)\"","\"pr-names (another open share's account)\"",
                "X-830: the merge commit names no issue nor account","X-830: no private link file: a doubt","X-830: only a batch's branch, naming no issue"})
                Check(tool.Contains(name,StringComparison.Ordinal),"the trusted merge's case: "+name);
        }),
        ("Anonymous 1832: the repository holds anonymous player files only, the merged gists hashed, no link file",() =>
        {
            var players=Path.GetFullPath("community-discoveries/players");
            var files=Directory.GetFiles(players);
            Check(files.Length>0 && files.All(path=>System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(path),"^[0-9a-f]{16}\\.jsonl\\.gz$")),
                "named by a random id only: "+string.Join(", ",files.Select(Path.GetFileName)));
            foreach(var path in files)
            {
                using var zip=new GZipStream(File.OpenRead(path),CompressionMode.Decompress);
                using var reader=new StreamReader(zip,Encoding.UTF8);
                var header=reader.ReadLine() ?? "";
                Check(header.StartsWith("{\"Format\":\"tensho-discoveries-player\",\"Version\":2,",StringComparison.Ordinal)
                    && !header.Contains("Issue",StringComparison.Ordinal) && !header.Contains("Login",StringComparison.Ordinal) && !header.Contains("Gist",StringComparison.Ordinal),
                    Path.GetFileName(path)+": a header of version 2, naming no issue, login nor gist: "+header);
                Check(!reader.ReadToEnd().Contains("Shigu330",StringComparison.OrdinalIgnoreCase),Path.GetFileName(path)+": no login in its facts");
            }
            Check(File.ReadAllText(Path.GetFullPath("community-discoveries/merged.txt")).StartsWith(TenshoAssistant.Discoveries.GistRotation.NotesHeader,StringComparison.Ordinal),"merged.txt");
            Check(File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py")).Contains("LINKS_PATH=os.path.join(os.path.expanduser(\"~\"),\".tensho\",\"discoveries-links.json\")",StringComparison.Ordinal)
                && File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py")).Contains("if inside_repository(path):raise ValueError(",StringComparison.Ordinal),"the private link: outside the repository, refused inside it");
            var exchange=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/GitHubExchange.cs"));
            Check(exchange.Contains("{IssueBatch}\\n{IssueBatchFrench}",StringComparison.Ordinal),"the issue says the batches and their limit, in English then French");
            foreach(var language in new[]{"en","fr"})
            {
                var text=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                Check(text.Contains(language=="en" ? "with few players, a batch can still be tied to you by its date":"avec peu de joueurs, un lot peut encore être relié à vous par sa date",StringComparison.Ordinal),
                    language+": the first-share warning says the limit");
            }
            var readme=File.ReadAllText(Path.GetFullPath("community-discoveries/README.md"));
            Check(readme.Contains("**Anonymous player files and batches (since Tensho 0.1832).**",StringComparison.Ordinal)
                && readme.Contains("**Fichiers de joueur anonymes et lots (depuis Tensho 0.1832).**",StringComparison.Ordinal),"the README, in English and French");
        }),
    ];
}
