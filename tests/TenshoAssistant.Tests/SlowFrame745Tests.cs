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
        ("Slow frame: developer mode only, around the whole drawing",() =>
        {
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("if(!mainWindow.DeveloperMode){DrawFrame();return;}") && plugin.Contains("private void DrawFrame()"),"wrapped");
            Check(Read("MainWindow.cs").Contains("SlowFrames.Add(ImGui.GetFrameCount(), \"page \" + drawnPage, drawMs)"),"pages too");
            Check(plugin.Contains("\"setup\"") && plugin.Contains("\"all windows\"") && Read("MainWindow.cs").Contains("\"main window\""),"0.747: the rest named");
            var panel=Read("MainWindow.GatheringPanel.cs");
            Check(panel.Contains("gatheringPanelSolve=(key,Task.Run(()=>") && !panel.Contains("gatheringPanelPlan=GpOptimizer.Solve(scenario,catalog);"),"0.747: the node's plan off the frame");
            Check(panel.Contains("gatheringLeftoverSolve=(known,Task.Run(()=>GpNextAction.Leftover("),"0.747: the end-of-node replan too");
        }),
    ];
}
