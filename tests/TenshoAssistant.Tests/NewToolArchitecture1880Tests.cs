using System.Diagnostics;
using System.Text.RegularExpressions;

// X-872 / X-873 (0.1880): ArchitectureMap1879Tests asks a row of ARCHITECTURE.md's module map for every code folder of the plugin, and
// scripts/New-Tool.ps1 made a folder without one: the CI job « New tool skeleton passes Verify » (CiNotes/, CiGatherTab/) and a contributor
// following CONTRIBUTING « Adding a tool » failed Verify. Now the script writes the folder's row in both halves. Run on a copy that holds
// the document, and checked with the very pattern of ArchitectureMap1879Tests.
internal static class NewToolArchitecture1880Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}

    private static readonly string[] Files=
    [
        "scripts/New-Tool.ps1","src/TenshoAssistant/MainWindow.cs","src/TenshoAssistant/MainWindow.Pages.cs","src/TenshoAssistant/Guide/GuideFlow.cs",
        "src/TenshoAssistant.Desktop/DesktopPages.cs","src/TenshoAssistant/Localization/Languages/en.json","src/TenshoAssistant/Localization/Languages/fr.json",
        "tests/TenshoAssistant.Tests/TenshoAssistant.Tests.csproj","docs/project/ARCHITECTURE.md",
    ];

    private static string Copy()
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-newtool-map-"+Guid.NewGuid().ToString("N"));
        foreach(var file in Files)
        {
            var target=Path.Combine(root,file);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(Path.GetFullPath(file),target);
        }
        return root;
    }

    private static (int Code,string Output) Run(string root,params string[] arguments)
    {
        foreach(var shell in new[]{"pwsh","pwsh.exe"})
        {
            var start=new ProcessStartInfo(shell){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};
            foreach(var argument in new[]{"-NoProfile","-NonInteractive","-File",Path.Combine(root,"scripts","New-Tool.ps1"),"-Root",root}.Concat(arguments))start.ArgumentList.Add(argument);
            Process? process;
            try{process=Process.Start(start);}catch(System.ComponentModel.Win32Exception){continue;}
            using(process)
            {
                if(process is null)continue;
                var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();
                if(!process.WaitForExit(120_000)){process.Kill(true);throw new Exception("New-Tool.ps1 did not end in two minutes");}
                return (process.ExitCode,output.Result+error.Result);
            }
        }
        throw Skip.Because("PowerShell 7 (pwsh) is not installed here");
    }

    private static (string En,string Fr) Halves(string root)
    {
        var text=File.ReadAllText(Path.Combine(root,"docs/project/ARCHITECTURE.md")).Replace("\r\n","\n");
        var half=text.IndexOf("\n---\n",StringComparison.Ordinal);
        Check(half>0,"ARCHITECTURE has two halves");
        return (text[..half],text[half..]);
    }

    // The module map: from its heading to the next heading.
    private static string Map(string half,string heading)
    {
        var from=half.IndexOf(heading,StringComparison.Ordinal);
        Check(from>=0,"the module map: "+heading);
        var to=half.IndexOf("\n### ",from+heading.Length,StringComparison.Ordinal);
        return half[from..(to<0?half.Length:to)];
    }

    private static void Mapped(string root,string name)
    {
        var (en,fr)=Halves(root);
        foreach(var (half,language,heading) in new[]{(en,"English","### Module map"),(fr,"French","### Carte des modules")})
        {
            // The pattern of ArchitectureMap1879Tests, which the CI job's Verify runs on the new folder.
            Check(Regex.IsMatch(half,$@"^\|[^|\n]*`{name}/`",RegexOptions.Multiline),$"ARCHITECTURE ({language}): the folder {name}/ has no row");
            var map=Map(half,heading);
            Check(Regex.Matches(map,$@"^\| `{name}/` \|",RegexOptions.Multiline).Count==1,$"ARCHITECTURE ({language}): one row for {name}/, in the module map");
            var row=map.Split('\n').Single(line=>line.StartsWith($"| `{name}/` |",StringComparison.Ordinal));
            Check(row.Count(c=>c=='|')==3 && row.Contains($"`MainWindow.{name}.cs`"),$"ARCHITECTURE ({language}): a row of two columns naming the page: "+row);
        }
        Check(en.Contains($"`{name}/` | ") && !en.Contains("modèle pur") && fr.Contains("modèle pur"),"each half in its own language");
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("New tool 1880 (X-872): a page alone and a tab of a family each get their folder's row in the module map, in both halves",()=>
        {
            var root=Copy();
            try
            {
                var raw=File.ReadAllText(Path.Combine(root,"docs/project/ARCHITECTURE.md"));
                foreach(var arguments in new[]{
                    new[]{"-Name","SampleMapNotes","-Category","Objects","-LabelEn","Sample notes","-LabelFr","Notes d'essai"},
                    new[]{"-Name","SampleMapTab","-Category","Production","-Parent","Gathering","-LabelEn","Sample","-LabelFr","Essai"}})
                {
                    var (code,output)=Run(root,arguments);
                    Check(code==0,"New-Tool.ps1 failed: "+output);
                    Mapped(root,arguments[1]);
                }
                var after=File.ReadAllText(Path.Combine(root,"docs/project/ARCHITECTURE.md"));
                Check(after.Contains("\r\n")==raw.Contains("\r\n") && after.Replace("\r\n","\n").Split('\n').Length==raw.Replace("\r\n","\n").Split('\n').Length+4,"four rows added, line ends kept");
                var refused=Run(root,"-Name","SampleBar","-Category","World","-LabelEn","A | B","-LabelFr","A","-DryRun");
                Check(refused.Code!=0 && refused.Output.Contains("must not contain"),"a label that would break the table is refused: "+refused.Output);
            }
            finally{try{Directory.Delete(root,true);}catch(IOException){}}
        }),
        ("New tool 1880 (X-873): a dry run names ARCHITECTURE.md and writes nothing; CONTRIBUTING says it, in both halves",()=>
        {
            var root=Copy();
            try
            {
                var before=File.ReadAllText(Path.Combine(root,"docs/project/ARCHITECTURE.md"));
                var (code,output)=Run(root,"-Name","SampleMapDry","-Category","World","-LabelEn","Dry","-LabelFr","Sec","-DryRun");
                Check(code==0 && output.Contains("docs/project/ARCHITECTURE.md (a row of the module map"),output);
                Check(File.ReadAllText(Path.Combine(root,"docs/project/ARCHITECTURE.md"))==before,"changed by a dry run");
            }
            finally{try{Directory.Delete(root,true);}catch(IOException){}}
            var contributing=File.ReadAllText(Path.GetFullPath("CONTRIBUTING.md"));
            Check(contributing.Contains("a row for its folder `<Name>/` in the module map of `docs/project/ARCHITECTURE.md`")
                && contributing.Contains("une ligne pour son dossier `<Name>/` dans la carte des modules de `docs/project/ARCHITECTURE.md`"),"CONTRIBUTING « Adding a tool », both halves");
            Check(File.ReadAllText(Path.GetFullPath(".github/workflows/build.yml")).Contains("./scripts/New-Tool.ps1 -Name CiGatherTab"),"the CI job still adds its skeletons and runs Verify on them");
        }),
    ];
}
