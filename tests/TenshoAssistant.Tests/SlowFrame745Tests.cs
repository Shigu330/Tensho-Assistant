using TenshoAssistant.Diagnostics;

// 0.745 (user report: Tensho feels heavy): which part of a slow frame costs.
internal static class SlowFrame745Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Slow frame: under the limit nothing; above, the costliest parts first, what is left, and the pauses",() =>
        {
            var frames=new SlowFrame();
            frames.Add(7,"Gathering",3);frames.Add(7,"page Items",150);frames.Add(7,"Workshop",20);
            Check(frames.Report(7,99,0) is null,"fast");
            var text=frames.Report(7,200,12)!;
            Check(text.StartsWith("Slow frame: 200 ms; page Items 150 ms, Workshop 20 ms, Gathering 3 ms") && text.Contains("not measured 27 ms") && text.EndsWith("runtime pauses 12 ms"),text);
            Check(frames.Report(8,150,0)!.Contains("not measured 150 ms"),"another frame: its parts are not these");
            frames.Add(8,"Map",1);
            Check(!frames.Report(8,150,0)!.Contains("Items"),"a new frame starts empty");
        }),
        // 0.1019 (test audit #59, was SlowParts801Tests): 0.801 listed ten parts, 0.806 every part of 1 ms or more, 24 at most.
        ("Slow frame: every part of 1 ms or more is listed, smaller ones left out, 24 at most",() =>
        {
            var frames=new SlowFrame();
            for(var i=1;i<=12;i++)frames.Add(3,$"part{i:00}",i+1);
            frames.Add(3,"tiny",0.5);
            var twelve=frames.Report(3,500,0)!;
            Check(Enumerable.Range(1,12).All(i=>twelve.Contains($"part{i:00} {i+1} ms")) && !twelve.Contains("tiny"),"more than ten parts, the small one left out: "+twelve);
            for(var i=13;i<=30;i++)frames.Add(3,$"part{i:00}",i+1);
            var many=frames.Report(3,1000,0)!;
            var listed=System.Text.RegularExpressions.Regex.Matches(many,@"part\d\d ").Count;
            Check(listed==24 && many.Contains("part30 31 ms") && many.Contains("part07 8 ms") && !many.Contains("part06 "),"the 24 costliest: "+many);
        }),
        ("Slow frame: developer mode only, around the whole drawing",() =>
        {
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("if(!mainWindow.DeveloperMode){DrawFrame();return;}") && plugin.Contains("private void DrawFrame()"),"wrapped");
            Check(Read("MainWindow.cs").Contains("SlowFrames.Add(ImGui.GetFrameCount(), \"page \" + drawnPage, drawMs)"),"pages too");
            Check(plugin.Contains("\"setup\"") && plugin.Contains("\"all windows\"") && Read("MainWindow.cs").Contains("\"main window\""),"0.747: the rest named");
            var panel=Read("MainWindow.GatheringPanel.cs");
            Check(panel.Contains("gatheringPanelSolve=(key,Task.Run(()=>") && !panel.Contains("gatheringPanelPlan=GpOptimizer.Solve(scenario,catalog);"),"0.747: the node's plan off the frame");
            Check(panel.Contains("gatheringLeftoverSolve=(known,Task.Run(()=>GpNextAction.Leftover("),"0.747: the end-of-node replan too");
            Check(Read("MainWindow.MapView.cs").Contains("MeasureMap(\"map canvas\",()=>DrawMapCanvas(map,live));"),"0.749: the map's parts");
        }),
    ];
}
