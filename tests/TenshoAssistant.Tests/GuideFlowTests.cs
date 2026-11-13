using System.Text.Json;
using TenshoAssistant.Guide;

// ONBOARD-001, lots 4 to 6 (DEC-031): the guide's order, what the interests keep, and its red lines: never imposed, the player acts, nothing is sent.
internal static class GuideFlowTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Guide: forward and back through every step and stop, never past the ends",() =>
        {
            var at=new GuidePosition(GuideStep.Welcome,0);
            Check(GuideFlow.Previous(at,3)==at,"nothing before the first");
            var seen=new List<GuidePosition>{at};
            while(at.Step!=GuideStep.End){at=GuideFlow.Next(at,3);seen.Add(at);}
            Check(seen.Count==7+3,"seven steps (Panels since 0.1222) and the three stops");
            Check(seen.Count(position=>position.Step==GuideStep.Visit)==3 && seen.Where(position=>position.Step==GuideStep.Visit).Select(position=>position.Stop).SequenceEqual([0,1,2]),"the stops in order");
            Check(GuideFlow.Next(at,3)==at,"nothing after the last");
            foreach(var position in Enumerable.Reverse(seen).Skip(1)){at=GuideFlow.Previous(at,3);Check(at==position,"back: "+position);}
            Check(GuideFlow.Next(new(GuideStep.Windows,0),0)==new GuidePosition(GuideStep.End,0),"no stop: the visit is skipped");
            Check(GuideFlow.Previous(new(GuideStep.End,0),0)==new GuidePosition(GuideStep.Windows,0),"and on the way back");
        }),
        ("Guide: progress grows from 0 to 1; a saved position that no longer exists comes back inside",() =>
        {
            Check(GuideFlow.Progress(new(GuideStep.Welcome,0),3)==0 && GuideFlow.Progress(new(GuideStep.End,0),3)==1,"the ends");
            Check(GuideFlow.Progress(new(GuideStep.Visit,1),3)>GuideFlow.Progress(new(GuideStep.Visit,0),3),"the stops count");
            Check(GuideFlow.Valid(99,0,3).Step==GuideStep.End && GuideFlow.Valid(-4,0,3).Step==GuideStep.Welcome,"out of range");
            Check(GuideFlow.Valid((int)GuideStep.Visit,9,3)==new GuidePosition(GuideStep.Visit,2),"fewer stops");
            Check(GuideFlow.Valid((int)GuideStep.Visit,0,0).Step==GuideStep.End,"no stop left");
        }),
        ("Guide: interests keep what matches; nothing chosen keeps the pages for everyone (0.1222)",() =>
        {
            var everyone=GuideFlow.Stops(null);
            Check(everyone.Count==GuideFlow.Stops(new HashSet<GuideInterest>()).Count && everyone.All(stop=>stop.Interest is null) && everyone.Count==GuideFlow.EveryStop.Count(stop=>stop.Interest is null) && everyone.All(stop=>stop.Page!="TripleTriad"),"nothing chosen");
            var craft=GuideFlow.Stops(new HashSet<GuideInterest>{GuideInterest.Crafting});
            Check(craft.Any(stop=>stop.Page=="CraftSimulation") && craft.Any(stop=>stop.Page=="Maps") && craft.All(stop=>stop.Page!="TripleTriad"),"its stops and the common ones");
            var windows=GuideFlow.Windows(new HashSet<GuideInterest>{GuideInterest.FreeCompany});
            Check(windows.Any(task=>task.Key=="Workshop") && windows.Any(task=>task.Key=="Retainer") && windows.All(task=>task.Key!="CraftingLog"),"windows too");
            Check(GuideFlow.ParseInterests(["Crafting","Unknown"]).Single()==GuideInterest.Crafting && GuideFlow.ParseInterests(null).Count==0,"saved names");
        }),
        ("Guide: opens once for a new install, is only offered to anyone else, never twice",() =>
        {
            Check(GuideFlow.OpensByItself(true,false,false) && !GuideFlow.OpensByItself(false,false,false) && !GuideFlow.OpensByItself(true,true,false) && !GuideFlow.OpensByItself(true,false,true),"opens");
            Check(GuideFlow.Offered(false,false,false) && !GuideFlow.Offered(true,false,false) && !GuideFlow.Offered(false,true,false) && !GuideFlow.Offered(false,false,true),"offered");
        }),
        ("Guide: every step, stop, window and interest has its texts in English and French",() =>
        {
            foreach(var language in new[]{"en","fr"})
            {
                using var document=JsonDocument.Parse(Read($"src/TenshoAssistant/Localization/Languages/{language}.json").TrimStart('﻿'));
                var guide=document.RootElement.GetProperty("Guide");
                foreach(var step in Enum.GetValues<GuideStep>())Check(guide.TryGetProperty($"Step{step}Title",out _),$"{language} step title {step}");
                foreach(var step in Enum.GetValues<GuideStep>().Where(step=>step!=GuideStep.Visit))Check(guide.TryGetProperty($"Step{step}Text",out _),$"{language} step text {step}");
                foreach(var task in GuideFlow.EveryWindow)Check(guide.TryGetProperty($"Window{task.Key}",out _) && guide.TryGetProperty($"Window{task.Key}Where",out _),$"{language} window {task.Key}");
                foreach(var interest in Enum.GetValues<GuideInterest>())Check(guide.TryGetProperty($"Interest{interest}",out _),$"{language} interest {interest}");
            }
            var pages=Read("src/TenshoAssistant/MainWindow.cs");
            foreach(var stop in GuideFlow.EveryStop)Check(pages.Contains(" "+stop.Page+",") || pages.Contains(" "+stop.Page+" }"),"a stop that is not a page: "+stop.Page);
        }),
        ("Guide and help: what runs in Draw stays light and follows the language",() =>
        {
            var guide=Read("src/TenshoAssistant/MainWindow.Guide.cs");
            Check(guide.Contains("DrawGuideWindows(tasks, false);"),"the Home list reads what is saved (every two seconds: DrawCost956Tests)");
            var help=Read("src/TenshoAssistant/MainWindow.Help.cs");
            Check(help.Contains("var language = localization.CurrentCode + \"|\" + helpQuery;"),"help rebuilt on a language change");
        }),
        ("Guide: its red lines in the code",() =>
        {
            var code=Read("src/TenshoAssistant/MainWindow.Guide.cs");
            Check(!code.Contains("HttpClient") && !code.Contains("OpenRecipe") && !code.Contains("ExecuteCommand") && !code.Contains("OpenLink"),"it opens nothing of the game and sends nothing");
            Check(code.Contains("InCombat: true") && code.Contains("InDuty: true"),"hidden in combat and duties");
            var window=Read("src/TenshoAssistant/MainWindow.cs");
            Check(window.Contains("OpenGuideOnce();"),"opened with Tensho itself, not when the plugin loads");
            var plugin=Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("windows.AddWindow(guideWindow = new GuideWindow(mainWindow))") && plugin.Contains("guideWindow.IsOpen=mainWindow.GuideOpen"),"a Dalamud window");
            Check(plugin.Contains("mainWindow.GuideNewInstall=newInstall"),"new install read before the file is written");
            Check(Read("src/TenshoAssistant/MainWindow.Help.cs").Contains("StartGuide()"),"started again from Help");
        }),
    ];
}
