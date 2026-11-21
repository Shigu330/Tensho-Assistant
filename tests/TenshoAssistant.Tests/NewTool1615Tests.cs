using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

// 0.1615 (X-664, relay 142): scripts/New-Tool.ps1 made a page the tests refused: no stop of the guide's visit, no kind in the desktop
// application, and a line of the list of pages that could not be a tab of a family (Tab, About). Now it registers all of them. A dry run:
// the script runs on a copy of the files it changes (a page alone and a tab of the Gathering family), and the copy is checked with the rules of
// PageCoverageTests, GuideVisit1045Tests, PageHeader1573Tests, HelpTests and DesktopPages. The whole skeleton built and tested: see the
// CHANGELOG of 0.1615 (a copy of the repository, built, tested and drawn by the desktop application).
internal static class NewTool1615Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}

    private static readonly string[] Files=
    [
        "scripts/New-Tool.ps1","src/TenshoAssistant/MainWindow.cs","src/TenshoAssistant/MainWindow.Pages.cs","src/TenshoAssistant/Guide/GuideFlow.cs",
        "src/TenshoAssistant.Desktop/DesktopPages.cs","src/TenshoAssistant/Localization/Languages/en.json","src/TenshoAssistant/Localization/Languages/fr.json",
        "tests/TenshoAssistant.Tests/TenshoAssistant.Tests.csproj",
    ];

    private static string Copy()
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-newtool-"+Guid.NewGuid().ToString("N"));
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

    private static string Text(string root,string file)=>File.ReadAllText(Path.Combine(root,file)).Replace("\r\n","\n");
    private static string[] Pages(string root)=>Regex.Match(Text(root,"src/TenshoAssistant/MainWindow.cs"),@"enum Page \{([^}]*)\}").Groups[1].Value
        .Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
    private static string Line(string root,string page)=>Text(root,"src/TenshoAssistant/MainWindow.Pages.cs").Split('\n').Single(line=>line.TrimStart().StartsWith($"new(Page.{page},",StringComparison.Ordinal));

    private static void Registered(string root,string name)
    {
        Check(Pages(root).Contains(name),"enum Page: "+name);
        Check(Regex.IsMatch(Text(root,"src/TenshoAssistant/Guide/GuideFlow.cs"),$@"new\(""{name}""\)"),"a stop of the guide's visit (PageCoverageTests, GuideVisit1045Tests)");
        var desktop=Text(root,"src/TenshoAssistant.Desktop/DesktopPages.cs");
        var from=desktop.IndexOf("IReadOnlyList<string> AsIs =",StringComparison.Ordinal);var asIs=desktop[from..desktop.IndexOf("];",from,StringComparison.Ordinal)];
        Check(asIs.Contains($"\"{name}\","),"the desktop kind AsIs (DesktopPages.Unclassified empty)");
        foreach(var language in new[]{"en","fr"})
        {
            using var document=JsonDocument.Parse(Text(root,$"src/TenshoAssistant/Localization/Languages/{language}.json").TrimStart('﻿'));
            var json=document.RootElement;
            Check(json.GetProperty("Navigation").TryGetProperty(name,out _),language+": its name");
            foreach(var key in new[]{"Title","Purpose","Start","Empty"})Check(json.GetProperty("PageHelp").TryGetProperty(name+key,out _),$"{language}: PageHelp.{name}{key} (HelpTests)");
            foreach(var key in new[]{"Title","Help","Count"})Check(json.GetProperty(name+"Ui").TryGetProperty(key,out _),$"{language}: {name}Ui.{key}");
        }
        Check(Text(root,"tests/TenshoAssistant.Tests/TenshoAssistant.Tests.csproj").Contains($"../../src/TenshoAssistant/{name}/*.cs"),"the model compiled by the tests");
        Check(File.Exists(Path.Combine(root,"tests","TenshoAssistant.Tests",name+"Tests.cs")) && File.Exists(Path.Combine(root,"src","TenshoAssistant",name,name+"Model.cs")),"its test and its model");
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("New tool 1615: a page alone is registered everywhere the tests ask, and draws its own title",()=>
        {
            var root=Copy();
            try
            {
                var (code,output)=Run(root,"-Name","SampleNotes","-Category","Objects","-LabelEn","Sample notes","-LabelFr","Notes d'essai");
                Check(code==0,"New-Tool.ps1 failed: "+output);
                Registered(root,"SampleNotes");
                var line=Line(root,"SampleNotes");
                Check(line.Contains("PageLabel(\"Navigation.SampleNotes\")") && !line.Contains(",Tab:") && !line.Contains(",About:"),"a page alone: its name, no tab (PageHeader1573Tests): "+line);
                Check(Text(root,"src/TenshoAssistant/MainWindow.SampleNotes.cs").Contains("Heading(localization.Get(\"SampleNotesUi.Title\"),localization.Get(\"SampleNotesUi.Help\"));"),"its title");
            }
            finally{try{Directory.Delete(root,true);}catch(IOException){}}
        }),
        ("New tool 1615: a tab of a family names its family, its tab and its description, and draws no heading",()=>
        {
            var root=Copy();
            try
            {
                var (code,output)=Run(root,"-Name","SampleGatherTab","-Category","Production","-Parent","Gathering","-LabelEn","Sample","-LabelFr","Essai");
                Check(code==0,"New-Tool.ps1 failed: "+output);
                Registered(root,"SampleGatherTab");
                var line=Line(root,"SampleGatherTab");
                Check(line.Contains(",null,DrawSampleGatherTab,") && line.Contains(",Page.Gathering,Tab:PageLabel(\"SampleGatherTabUi.Title\"),About:PageLabel(\"SampleGatherTabUi.Help\"))"),"Parent, Tab and About: "+line);
                Check(!Text(root,"src/TenshoAssistant/MainWindow.SampleGatherTab.cs").Contains("Heading(",StringComparison.Ordinal),"the page header draws the title");
                var refused=Run(root,"-Name","SampleOther","-Category","Production","-Parent","GatheringGp","-LabelEn","Other","-LabelFr","Autre");
                Check(refused.Code!=0 && refused.Output.Contains("itself a tab of a family"),"a tab is not a family: "+refused.Output);
                Check(!Pages(root).Contains("SampleOther"),"nothing written when refused");
            }
            finally{try{Directory.Delete(root,true);}catch(IOException){}}
        }),
        ("New tool 1682: a page alone of « Tracking » goes after the last page of the category, so it stays one group of the side bar",()=>
        {
            // X-704 (0.1682): the side bar has one category left (D-199); at the end of the list such a page would draw « Tracking » twice.
            var root=Copy();
            try
            {
                var (code,output)=Run(root,"-Name","SampleTimers","-Category","Tracking","-LabelEn","Timers","-LabelFr","Minuteurs");
                Check(code==0,"New-Tool.ps1 failed: "+output);
                Registered(root,"SampleTimers");
                var entries=Text(root,"src/TenshoAssistant/MainWindow.Pages.cs").Split('\n').Select(line=>Regex.Match(line,@"^\s*new\(Page\.(\w+),""(\w+)"",(?!null,)"))
                    .Where(match=>match.Success).Select(match=>(Page:match.Groups[1].Value,Category:match.Groups[2].Value)).ToArray();
                var groups=TenshoAssistant.UI.PageFamilies.Runs(entries,entry=>TenshoAssistant.UI.PageFamilies.SideBarCategory(entry.Category));
                var tracking=groups.Where(group=>group.Key=="Tracking").ToArray();
                Check(tracking.Length==1 && tracking[0].Items[^1].Page=="SampleTimers","one « Tracking » group, the new page last: "+string.Join(" | ",tracking.Select(group=>string.Join(",",group.Items.Select(entry=>entry.Page)))));
            }
            finally{try{Directory.Delete(root,true);}catch(IOException){}}
        }),
        ("New tool 1615: a dry run writes nothing",()=>
        {
            var root=Copy();
            try
            {
                var before=Files.ToDictionary(file=>file,file=>Text(root,file));
                var (code,output)=Run(root,"-Name","SampleDry","-Category","World","-LabelEn","Dry","-LabelFr","Sec","-DryRun");
                Check(code==0 && output.Contains("DRY RUN: nothing was written."),output);
                foreach(var (file,text) in before)Check(Text(root,file)==text,"changed by a dry run: "+file);
                Check(!File.Exists(Path.Combine(root,"src","TenshoAssistant","MainWindow.SampleDry.cs")),"no page created");
            }
            finally{try{Directory.Delete(root,true);}catch(IOException){}}
        }),
    ];
}
