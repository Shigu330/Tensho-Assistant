using System.Diagnostics;

// 0.1824 (X-823, review of 0.1823, I1): the trusted merge (tools/discoveries-trust.py) read only the branch's name: a pull request from a fork
// named travailleur/discoveries-N passed, and the common set it brought was checked by its paths only. Now: a branch of the repository itself
// opened by the worker's account, and the content by value (a sync of that share alone replayed offline must write exactly the pull request's
// players/ and v3/ files). Here: the wiring, then the tool's own selftest run (its end-to-end cases against a made-up GitHub: a fork, another
// author, a file out of scope, a common set or player file changed by value, a file more or missing: never merged), and the same selftest on a
// copy without the origin and content checks, which must fail: proof that the cases fail on the code from before the fix.
internal static class DiscoveryOrigin1824Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    private const string Origin="if pr.get(\"cross\") is not False or (pr.get(\"head_repo\") or \"\").lower()!=REPOSITORY.lower() \\\n        or (pr.get(\"author\") or \"\").lower()!=WORKER.lower():return \"doubt\",\"pr-origin\"";
    private const string Content="if batch.get(\"content\")!=\"same\":return \"doubt\",\"content\"";   // X-830: a batch's content

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Discovery origin 1824: the trusted merge checks where the pull request comes from and replays its content",() =>
        {
            var tool=Read("tools/discoveries-trust.py");
            Check(tool.Contains(Origin,StringComparison.Ordinal),"a branch of this repository, opened by the worker's account");
            Check(tool.Contains("isCrossRepository,headRepository,headRepositoryOwner,author",StringComparison.Ordinal),"the origin asked of GitHub");
            Check(tool.Contains(Content,StringComparison.Ordinal) && tool.Contains("expected,read=replay(base,issues,links,listing,reader)",StringComparison.Ordinal),
                "the content replayed by value");
            foreach(var name in new[]{"X-823, X-830: {name}: a doubt, never merged","\"a fork\"","\"another author\"","\"a file out of scope\"","\"the common set changed by value\"",
                "\"the player file changed by value\"","\"a kind file more\"","\"an index naming other bytes\"","the worker's own batch, replayed whole: merged"})
                Check(tool.Contains(name,StringComparison.Ordinal),"selftest case: "+name);
            Check(Read("tools/README.md").Contains("since 0.1824, X-823: a branch of this repository itself, never a fork",StringComparison.Ordinal)
                && Read("community-discoveries/README.md").Contains("since Tensho 0.1824, among them a pull request from a branch of this repository",StringComparison.Ordinal),
                "said in the READMEs");
        }),
        ("Discovery origin 1824: the tool's selftest passes; without the origin and content checks, it fails",() =>
        {
            string? python=null;
            foreach(var name in new[]{"python","py"})
            {
                try
                {
                    using var probe=Process.Start(new ProcessStartInfo(name,"--version"){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false});
                    if(probe is null)continue;
                    probe.WaitForExit(20000);
                    if(probe.ExitCode==0){python=name;break;}
                }
                catch(Exception){}
            }
            if(python is null)return; // no Python on this machine: the wiring case above still holds
            var folder=Path.Combine(Path.GetTempPath(),"tensho-origin-"+Guid.NewGuid().ToString("N"));
            var tools=Path.Combine(folder,"tools");
            Directory.CreateDirectory(tools);
            try
            {
                foreach(var name in new[]{"discoveries-merge.py","bilingual.py"})File.Copy(Path.GetFullPath("tools/"+name),Path.Combine(tools,name));
                var source=File.ReadAllText(Path.GetFullPath("tools/discoveries-trust.py")).Replace("\r\n","\n");
                Check(source.Contains(Origin,StringComparison.Ordinal) && source.Contains(Content,StringComparison.Ordinal),"the fix is in the tool");
                var before=Path.Combine(tools,"discoveries-trust.py");
                File.WriteAllText(before,source.Replace(Origin,"if False:return \"doubt\",\"pr-origin\"").Replace(Content,"if False:return \"doubt\",\"content\""));
                (int Code,string Output) Run(string path)
                {
                    var start=new ProcessStartInfo(python){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};
                    foreach(var argument in new[]{"-I","-X","utf8",path,"--selftest"})start.ArgumentList.Add(argument);
                    using var run=Process.Start(start)!;
                    var output=run.StandardOutput.ReadToEndAsync();
                    var error=run.StandardError.ReadToEndAsync();
                    Check(run.WaitForExit(120000),"the selftest ends");
                    return (run.ExitCode,output.Result+error.Result);
                }
                var now=Run(Path.GetFullPath("tools/discoveries-trust.py"));
                Check(now.Code==0 && now.Output.Contains("discoveries-trust selftest:",StringComparison.Ordinal) && !now.Output.Contains("FAIL",StringComparison.Ordinal),
                    "the tool's selftest passes ("+now.Code+"): "+now.Output);
                var old=Run(before);
                foreach(var name in new[]{"X-823, X-830: a fork: a doubt, never merged","X-823, X-830: another author: a doubt, never merged","X-823, X-830: the common set changed by value: a doubt, never merged",
                    "X-823, X-830: the player file changed by value: a doubt, never merged"})
                    Check(old.Code!=0 && old.Output.Contains("FAIL "+name,StringComparison.Ordinal),"without the fix, the selftest fails on: "+name+" ("+old.Code+"): "+old.Output);
            }
            finally { try { Directory.Delete(folder,true); } catch(IOException){} }
        }),
    ];
}
