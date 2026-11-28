using TenshoAssistant.Desktop;

// 0.1958 (X-914, review of 0.1956, M2). The counters' icon buttons (0.1954) are in a reference picture: « Reminders-tasks », Reminders ›
// Tasks with two trial tasks (a counter, a daily task), given by the plugin's window in the desktop application only and taken back after.
internal static class CounterPicture1958Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Counter picture 1958: « Reminders-tasks » planned once on the Reminders page, after the retainers' picture",()=>
        {
            var plan=ReferencePlan.Plan(["Home","Reminders","Settings"]);
            Check(plan.Count(shot=>shot.Id=="Reminders-tasks" && shot.Page=="Reminders" && shot.Setup=="tasks-trial" && shot.Width==1280)==1,"planned once");
            var ids=plan.Select(shot=>shot.Id).ToList();
            Check(ids.IndexOf("Reminders-tasks")==ids.IndexOf("Reminders-retainers")+1,"after the retainers");
            Check(!ReferencePlan.Plan(["Home"]).Any(shot=>shot.Setup=="tasks-trial"),"not without the page");
        }),
        ("Counter picture 1958: the trial: a counter task and a daily one, the tasks before given back, desktop only",()=>
        {
            var data=Read("src/TenshoAssistant/MainWindow.DesktopData.cs");
            var start=data.IndexOf("private void DesktopTasksTrial(bool on)",StringComparison.Ordinal);
            Check(start>0,"the trial");
            var body=data[start..data.IndexOf("/// <summary>X-520 (0.1537)",start,StringComparison.Ordinal)];
            Check(body.Contains("if(!inDesktopApp)return;",StringComparison.Ordinal),"never in the game");
            Check(body.Contains("Period=TaskPeriod.Counter,Target=5,Count=2",StringComparison.Ordinal),"a counter");
            Check(body.Contains("Period=TaskPeriod.Daily",StringComparison.Ordinal),"a daily task");
            Check(body.Contains("configuration.PersonalTasks=desktopTasksBefore.Tasks;",StringComparison.Ordinal),"given back");
            Check(!body.Contains("SaveReminders",StringComparison.Ordinal) && !body.Contains("save()",StringComparison.Ordinal),"never saved");
            var tasks=Read("src/TenshoAssistant/MainWindow.PersonalTasks.cs");
            Check(tasks.Contains("ToolButton(Dalamud.Interface.FontAwesomeIcon.Minus,\"PersonalTasks.CountDownTip\",true)",StringComparison.Ordinal),"the page draws the « - » icon of a counter");
        }),
        ("Counter picture 1958: the application gives the trial before the picture and takes it back after; the contract names the method",()=>
        {
            var references=Read("src/TenshoAssistant.Desktop/References.cs");
            Check(references.Contains("if(shot.Setup==\"tasks-trial\" && !Headless.TasksTrial(host,true))",StringComparison.Ordinal),"given");
            Check(references.Contains("if(shot.Setup==\"tasks-trial\")Headless.TasksTrial(host,false);",StringComparison.Ordinal),"taken back");
            var headless=Read("src/TenshoAssistant.Desktop/Headless.cs");
            Check(headless.Contains("GetMethod(\"DesktopTasksTrial\"",StringComparison.Ordinal),"reached by its name");
            Check(headless.Contains("if(on)focus.SetValue(host.Window,\"Tasks\");",StringComparison.Ordinal),"the Tasks tab");
            Check(Read("src/TenshoAssistant.Desktop/DesktopContract.cs").Contains("\"DesktopTasksTrial\",false,[\"System.Boolean\"]",StringComparison.Ordinal),"in the contract");
        }),
    ];
}
