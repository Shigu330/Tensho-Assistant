using TenshoAssistant.Diagnostics;

// 0.724: the page sweep of the self-check.
internal static class PageSweep724Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static void Run(PageSweep sweep,double milliseconds=1,string? error=null)
    {
        while(!sweep.Done){if(sweep.Due())sweep.Drawn(milliseconds,error);}
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Page sweep: one draw every few frames, every page twice over, and it ends",() =>
        {
            var sweep=new PageSweep(["A","B"]);
            for(var frame=1;frame<PageSweep.FramesBetweenDraws;frame++)Check(!sweep.Due(),"spaced out");
            Check(sweep.Due(),"then due");
            Check(sweep.Current=="A","first page");
            sweep.Drawn(1,null);
            Run(sweep);
            Check(sweep.Done && sweep.Current is null && !sweep.Due(),"ends");
            Check(sweep.Lines.All(line=>line.Detail.StartsWith($"{PageSweep.FirstPassDraws+PageSweep.SecondPassDraws} draws")),"both passes");
            Check(new PageSweep([]).Done,"nothing to draw");
        }),
        ("Page sweep: an error is kept and fails the page, slow is only for a settled page, and the summary counts them",() =>
        {
            var sweep=new PageSweep(["Broken"]);
            Run(sweep,1,"NullReferenceException: x");
            Check(sweep.Lines.Single().Status==SelfCheckStatus.Failed && sweep.Lines.Single().Detail.Contains("NullReference"),"error");
            var loading=new PageSweep(["First"]);
            while(!loading.Done){if(loading.Due())loading.Drawn(loading.Lines.Single().Detail.StartsWith("0 draws") ? 500:1,null);}
            Check(loading.Lines.Single().Status==SelfCheckStatus.Ok && loading.Lines.Single().Detail.Contains("first 500.0 ms, settled 1.0 ms"),"a slow first draw is not slow");
            var slow=new PageSweep(["Slow"]);
            Run(slow,PageSweep.SlowMs);
            Check(slow.Lines.Single().Status==SelfCheckStatus.Suspect,"settled and slow");
            var text=PageSweep.Summary([..sweep.Lines,..slow.Lines],1,2,"a.test 2");
            Check(text.Contains("0 ok, 1 slow, 1 failed, 1 not drawn") && text.Contains("requests meanwhile: 2 (a.test 2)"),text);
            Check(PageSweep.Summary([],0,0,"").EndsWith("requests meanwhile: 0."),"no host when none");
        }),
        ("Page sweep: out of sight, no input, developer mode only, never while busy or cut, the market left out, the page kept",() =>
        {
            var window=Read("Diagnostics/PageSweepWindow.Dalamud.cs");
            foreach(var part in new[]{"ImGuiWindowFlags.NoInputs","HiddenFramesCannotSkipItems=2","ImGuiP.GetCurrentWindow().Hidden","Position=ImGui.GetMainViewport().Pos","MaximumSilentFrames","ImGuiCond.Always","IsOpen=developerMode","RespectCloseHotkey=false","DisableWindowSounds=true","CompetitiveGate.Cut || busy()",",true)"})
                Check(window.Contains(part),part);
            var plugin=Read("Plugin.cs");
            var wired=plugin[plugin.IndexOf("new Diagnostics.PageSweepWindow(")..];
            wired=wired[..wired.IndexOf("windows.RemoveWindow(pageSweep)")];
            foreach(var flag in new[]{"InCombat","BoundByDuty","Crafting","Gathering","Fishing","BetweenAreas","WatchingCutscene"})Check(wired.Contains("ConditionFlag."+flag),flag);
            Check(wired.Contains("configuration.DeveloperMode") && wired.Contains("selfCheck?.LastLines.Count>0"),"after the self-check");
            // A hidden window is never hovered: only a raw test of the mouse could react to a click, and the pages have none.
            foreach(var file in Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"MainWindow*.cs"))Check(!File.ReadAllText(file).Contains("IsMouseHoveringRect"),file);
            var pages=Read("MainWindow.PageSweep.cs");
            Check(pages.Contains("entry.Page!=Page.Market") && pages.Contains("page=kept"),"market and page");
            Check(Read("MainWindow.cs").Contains("        OpenGuideOnce();\n        ValidateDrawContext();") || Read("MainWindow.cs").Contains("        OpenGuideOnce();\r\n        ValidateDrawContext();"),"same preparation as the window");
        }),
    ];
}
