using System.IO.Pipes;
using System.Text;
using TenshoAssistant.Alarms;
using TenshoAssistant.Link;

// 0.1156 (X-163, user request of 12/11): an alarm whose item is already gathered stays silent in the game (X-62, 0.832); the desktop
// application now stays silent too. The plugin sends by the link how many of each alarm item the character holds (bags included, only when
// seen); the application's bell marks such an occurrence rung without ringing. Unknown counts (not linked, game closed) still ring.
// Behaviour test: counts sent through a real pipe decide what the application's bell rings.
internal static class AlarmHeld1156Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static readonly DateTimeOffset Now=new(2026,11,12,12,0,0,TimeSpan.Zero);
    private static TenshoAlarm Gather(string label,uint item,long needed)=>new(){Label=label,Repeat=AlarmRepeat.Once,At=Now.AddMinutes(3),LeadMinutes=5,ItemId=item,Needed=needed};
    private static LinkSnapshot Sample(IReadOnlyList<LinkHeld>? held)=>new(LocalLinkProtocol.Version,"0.1156.0",true,false,132,"New Gridania",1,null,null,null,held);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Alarm held 1156: the application's bell stays silent when the quantity is met, rings when it is not or unknown",() =>
        {
            var bell=new AlarmBell();
            var counts=new Dictionary<uint,long>{[100]=5,[200]=4};
            long? Held(uint item)=>counts.TryGetValue(item,out var count) ? count:null;
            var met=Gather("Met",100,5);var short_=Gather("Short",200,5);var unknown=Gather("Unknown",300,5);var plain=Gather("Plain",100,0);
            var due=bell.Due([met,short_,unknown,plain],Now,null,Held);
            Check(due.Select(item=>item.Alarm.Label).OrderBy(x=>x).SequenceEqual(["Plain","Short","Unknown"]),"rung: "+string.Join(",",due.Select(item=>item.Alarm.Label)));
            counts[100]=0;
            Check(bell.Due([Gather("Met",100,5)],Now.AddSeconds(1),null,Held).Count==0,"a silent occurrence is handled: it does not ring later when the count drops");
            Check(new AlarmBell().Due([Gather("Met",100,5)],Now,null).Count==1,"without counts (not linked): it rings, as before");
            Check(AlarmSchedule.Gathered(met,5) && !AlarmSchedule.Gathered(met,4),"the game's own rule");
        }),
        ("Alarm held 1156: the counts through a real pipe decide the bell; bounded; the worst line fits",() =>
        {
            var worst=new LinkSnapshot(LocalLinkProtocol.Version,new string('9',LocalLinkProtocol.MaxPlugin),true,false,uint.MaxValue,new string('漢',LocalLinkProtocol.MaxZone),long.MaxValue,
                new LinkLive(new string('漢',LocalLinkProtocol.MaxJob),1000,100_000,100_000,1000,1000),
                [.. LocalLinkProtocol.Categories.Values.Append(LocalLinkProtocol.SettingsCategory).Take(LocalLinkProtocol.MaxChanges).Select(c=>new LinkChange(c,long.MaxValue))],
                [.. Enumerable.Range(1,LocalLinkProtocol.MaxNotices).Select(n=>new LinkNotice(long.MaxValue-n,long.MaxValue,new string('«',LocalLinkProtocol.MaxNoticeText)))],
                [.. Enumerable.Range(1,LocalLinkProtocol.MaxHeld).Select(n=>new LinkHeld(uint.MaxValue-(uint)n,LocalLinkProtocol.MaxHeldCount))]);
            var line=LocalLinkProtocol.Serialize(worst);
            Check(Encoding.UTF8.GetByteCount(line)<=LocalLinkProtocol.MaxLine,"worst line "+Encoding.UTF8.GetByteCount(line)+" bytes");
            Check(LocalLinkProtocol.TryParse(line,out _,out var why),"the worst line is read: "+why);
            foreach(var bad in new IReadOnlyList<LinkHeld>[]{[.. Enumerable.Range(1,LocalLinkProtocol.MaxHeld+1).Select(n=>new LinkHeld((uint)n,1))],[new LinkHeld(0,1)],[new LinkHeld(1,-1)],[new LinkHeld(1,LocalLinkProtocol.MaxHeldCount+1)],[null!]})
                Check(!LocalLinkProtocol.TryParse(LocalLinkProtocol.Serialize(Sample(bad)),out _,out var refused) && refused=="held list refused","refused: "+refused);
            if(!OperatingSystem.IsWindows())return;
            var name="TenshoAssistant.Link.Test."+Guid.NewGuid().ToString("N")[..12];
            var server=new LocalLinkServer(name);
            try
            {
                server.Publish(Sample([new LinkHeld(100,7),new LinkHeld(200,1)]));
                using var pipe=new NamedPipeClientStream(".",name,PipeDirection.In,PipeOptions.CurrentUserOnly);
                pipe.Connect(5000);
                using var reader=new StreamReader(pipe,Encoding.UTF8);
                Check(LocalLinkProtocol.TryParse(reader.ReadLine(),out var received,out var error),"read: "+error);
                var counts=received!.Held!.ToDictionary(item=>item.Item,item=>item.Count);
                var due=new AlarmBell().Due([Gather("Copper ore",100,5),Gather("Tin ore",200,5)],Now,null,item=>counts.TryGetValue(item,out var count) ? count:null);
                Check(due.Count==1 && due[0].Alarm.Label=="Tin ore","only the alarm not yet gathered rings: "+string.Join(",",due.Select(item=>item.Alarm.Label)));
            }
            finally{server.Dispose();}
        }),
        ("Alarm held 1156: wired from the game's count to the application's bell",() =>
        {
            var alarms=Read("src/TenshoAssistant/MainWindow.Alarms.cs");
            Check(alarms.Contains("internal IReadOnlyList<Link.LinkHeld> AlarmHeldItems()") && alarms.Contains("Where(pair=>pair.held.HasObservation)"),"the plugin's count, only when seen");
            Check(Read("src/TenshoAssistant/Plugin.Desktop.cs").Contains("{Held=mainWindow.AlarmHeldItems,"),"given to the feed");
            Check(Read("src/TenshoAssistant/Link/LocalLinkFeed.Dalamud.cs").Contains("loggedIn ? ReadHeld():null"),"sent with a character only");
            var data=Read("src/TenshoAssistant/MainWindow.DesktopData.cs");
            Check(data.Contains("internal void DesktopAlarmHeld(uint[] items,long[] counts)") && data.Contains("item=>held.TryGetValue(item,out var count) ? count:null);"),"the bell uses the counts");
            Check(Read("src/TenshoAssistant/MainWindow.DesktopData.cs").Contains("AlarmHeld=DesktopAlarmHeld,") && Read("src/TenshoAssistant.Desktop/Host.cs").Contains("public void AlarmHeld(uint[] items,long[] counts)=>Surface.AlarmHeld(items,counts);"),"called through the typed contract (X-151, 0.1238)");
            var status=Read("src/TenshoAssistant.Desktop/StatusBar.cs");
            Check(status.IndexOf("host.AlarmHeld(",StringComparison.Ordinal) is > 0 and var at && at<status.IndexOf("host.DueAlarms(now,front)",StringComparison.Ordinal),"counts given before the bell decides");
        }),
    ];
}
