using System.Diagnostics;

// 0.1569 (X-625): tools/discoveries-merge.py writes its reports in UTF-8 whatever the console. Run with plain `python -I` on Windows (cp1252),
// `sync --dry-run` stopped on the « → » of the recheck report (UnicodeEncodeError). Here: the wiring, and the tool run with a cp1252 output
// printing its help (the help holds « → » and « » »): it must end without error.
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
        ("Merge UTF-8 1569: run with a cp1252 output, the tool prints its « → » help without error",() =>
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
            var start=new ProcessStartInfo(python){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};
            start.ArgumentList.Add("-I");
            start.ArgumentList.Add(Path.GetFullPath("tools/discoveries-merge.py"));
            start.Environment["PYTHONIOENCODING"]="cp1252";
            start.Environment.Remove("PYTHONUTF8");
            using var run=Process.Start(start)!;
            var output=run.StandardOutput.ReadToEndAsync();
            var error=run.StandardError.ReadToEndAsync();
            Check(run.WaitForExit(60000),"the tool ends");
            Check(!error.Result.Contains("UnicodeEncodeError"),"no UnicodeEncodeError: "+error.Result);
            Check(run.ExitCode==2 && output.Result.Contains("sync [--dry-run]"),"its help, exit code 2 ("+run.ExitCode+")");
        }),
    ];
}
