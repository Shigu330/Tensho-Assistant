using System.Diagnostics;

// 0.1569 (X-625): tools/discoveries-merge.py writes its reports in UTF-8 whatever the console. Run with plain `python -I` on Windows (cp1252),
// `sync --dry-run` stopped on the « → » of the recheck report (UnicodeEncodeError). Here: the wiring, and (0.1570, X-627) the tool run as
// `__main__` behind a cp1252 stdout, printing an offline recheck report that holds « → ». An environment variable cannot force cp1252
// (`python -I` ignores PYTHONIOENCODING), so a small driver wraps stdout itself. The same driver on a copy of the tool without the UTF-8
// call must stop on a UnicodeEncodeError: proof that the case fails on the code from before the fix.
internal static class MergeUtf81569Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Merge UTF-8 1569: the merge tool turns its output to UTF-8 before writing anything",() =>
        {
            var tool=Read("tools/discoveries-merge.py");
            Check(tool.Contains("stream.reconfigure(encoding=\"utf-8\",errors=\"replace\")"),"stdout and stderr turned to UTF-8");
            Check(tool.Contains("if __name__==\"__main__\":\n    utf8_output(sys.stdout);utf8_output(sys.stderr)\n"),"first thing the tool does");
        }),
        ("Merge UTF-8 1569: behind a cp1252 output, the tool prints its « → » report; without the fix it stops",() =>
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
            var folder=Path.Combine(Path.GetTempPath(),"tensho-merge-utf8-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                var changes=Path.Combine(folder,"changes.json");
                File.WriteAllText(changes,"{\"Old\":\"7.5\",\"New\":\"7.51\",\"PatchDate\":\"2026-11-20\",\"Places\":[]}");
                var tool=Path.GetFullPath("tools/discoveries-merge.py");
                var source=File.ReadAllText(tool);
                const string fix="utf8_output(sys.stdout);utf8_output(sys.stderr)";
                Check(source.Contains(fix),"the fix is in the tool");
                var before=Path.Combine(folder,"before-fix.py");
                File.WriteAllText(before,source.Replace(fix,"pass"));
                (int Code,string Output,string Error) Run(string path)
                {
                    const string driver="import io,sys,runpy;sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding='cp1252');"
                        +"tool=sys.argv[1];sys.argv=[tool,'recheck','--changes',sys.argv[2]];runpy.run_path(tool,run_name='__main__')";
                    var start=new ProcessStartInfo(python){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};
                    foreach(var argument in new[]{"-I","-c",driver,path,changes})start.ArgumentList.Add(argument);
                    using var run=Process.Start(start)!;
                    var output=run.StandardOutput.ReadToEndAsync();
                    var error=run.StandardError.ReadToEndAsync();
                    Check(run.WaitForExit(60000),"the tool ends");
                    return (run.ExitCode,output.Result,error.Result);
                }
                var now=Run(tool);
                Check(now.Code==0 && !now.Error.Contains("UnicodeEncodeError"),"the tool ends without error ("+now.Code+"): "+now.Error);
                Check(now.Output.Contains("7.5 → 7.51"),"its report holds the « → » arrow: "+now.Output);
                var old=Run(before);
                Check(old.Code!=0 && old.Error.Contains("UnicodeEncodeError"),"without the fix, the same run stops on a UnicodeEncodeError ("+old.Code+")");
            }
            finally { try { Directory.Delete(folder,true); } catch(IOException){} }
        }),
    ];
}
