using TenshoAssistant.Alarms;

// 0.1132 (X-163, user request of 12/11): the desktop application rings the alarms by the plugin's own rule (AlarmSchedule.Check) with its
// own memory: an occurrence the plugin already marked handled (it rang in the game) still rings in the application, once; alarms copied
// into the game's alarms and weather conditions without a weather catalogue are left alone; a reading again of the settings (new objects)
// does not ring twice.
internal static class DesktopBell1132Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static readonly DateTimeOffset Now=new(2026,11,12,2,0,0,TimeSpan.Zero);
    private static TenshoAlarm Once(string label,DateTimeOffset at)=>new(){Label=label,Repeat=AlarmRepeat.Once,At=at,LeadMinutes=5};

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Desktop bell: the plugin's rule, its own memory, once per occurrence",() =>
        {
            var bell=new AlarmBell();
            var due=Once("Garden",Now.AddMinutes(3));
            var handled=Once("Ventures",Now.AddMinutes(2));handled.LastOccurrence=handled.At;
            var later=Once("Later",Now.AddMinutes(30));
            var inGame=Once("Copied",Now.AddMinutes(1));inGame.GameName="Tensho 1";
            var weather=Once("Weather",Now.AddMinutes(1));weather.Weathers=[2];
            var first=bell.Due([due,handled,later,inGame,weather],Now,null);
            Check(first.Count==2 && first.Any(item=>item.Alarm.Label=="Garden") && first.Any(item=>item.Alarm.Label=="Ventures"),"due ones, the plugin's handled one included: "+string.Join(",",first.Select(item=>item.Alarm.Label)));
            Check(bell.Due([due,handled,later],Now.AddSeconds(1),null).Count==0,"never twice");
            Check(bell.Due([Once("Garden",Now.AddMinutes(3))],Now.AddSeconds(2),null).Count==0,"the settings read again into new objects: still once");
            var withWeather=bell.Due([weather],Now,(alarm,time)=>2u);
            Check(withWeather.Count==1,"a weather condition rings once the weather is known and matches");
            var wrong=Once("Wrong weather",Now.AddMinutes(1));wrong.Weathers=[3];
            Check(bell.Due([wrong],Now,(alarm,time)=>2u).Count==0,"another weather: not rung");
            Check(bell.Due([later],Now.AddMinutes(26),null).Count==1,"a later one when its lead comes");
            var off=Once("Off",Now.AddMinutes(1));off.Enabled=false;
            Check(new AlarmBell().Due([off],Now,null).Count==0,"a disabled alarm never");
            Check(AlarmSchedule.Check(handled,Now).Check==AlarmCheck.None,"the plugin's own check is unchanged");
        }),
        ("Desktop bell: wired in the app once a second, only when the game is behind by default, never headless",() =>
        {
            var data=Read("src/TenshoAssistant/MainWindow.DesktopData.cs");
            Check(data.Contains("internal (string[] Lines,bool Sound) DesktopDueAlarms(DateTimeOffset now,bool gameInFront)")
                && data.Contains("if(due.Count==0 || options.DesktopOnlyGameBehind && gameInFront)return ([],false);"),"the plugin's entry");
            Check(Read("src/TenshoAssistant.Desktop/Host.cs").Contains("GetMethod(\"DesktopDueAlarms\",BindingFlags.Instance|BindingFlags.NonPublic,[typeof(DateTimeOffset),typeof(bool)])"),"called by that signature");
            var program=Read("src/TenshoAssistant.Desktop/Program.cs");
            Check(program.IndexOf("status.Bell=true;",StringComparison.Ordinal)>program.IndexOf("if(headless)return Headless.Run(",StringComparison.Ordinal),"never in a headless run");
            var options=new AlarmSettings();
            Check(options.DesktopSound && options.DesktopOnlyGameBehind && options.Copy().DesktopOnlyGameBehind,"defaults on, kept by Copy");
        }),
    ];
}
