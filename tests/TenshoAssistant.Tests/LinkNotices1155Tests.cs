using System.IO.Pipes;
using System.Text;
using TenshoAssistant.Alarms;
using TenshoAssistant.Link;

// 0.1155 (X-163, user request of 12/11): the plugin's alerts that ring in the game but are not Tensho alarms (currencies, duty ready, GP,
// gear wear, important items, hunt marks) go to the desktop application by the link and ring there once, with the alarms' options.
// Behaviour test: alerts sent through a real pipe and read by the application's own reader: each rings once across heartbeats, an old one
// is not rung again when the link comes back, a plugin started again rings its new ones; the worst line stays under the link's bound.
internal static class LinkNotices1155Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private const long T0=1_800_000_000_000;
    private static LinkSnapshot Sample(long sequence,IReadOnlyList<LinkNotice>? notices)
        => new(LocalLinkProtocol.Version,"0.1155.0",true,false,132,"New Gridania",sequence,null,null,notices);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Link notices 1155: the plugin's log keeps the newest four for two minutes, texts bounded",() =>
        {
            var log=new LinkNoticeLog();
            Check(log.Snapshot(T0) is null,"nothing yet");
            log.Add("  ",T0);
            Check(log.Snapshot(T0) is null,"a blank alert is left out");
            for(var n=1;n<=6;n++)log.Add($"Alert {n}",T0+n);
            var kept=log.Snapshot(T0+10)!;
            Check(kept.Count==LocalLinkProtocol.MaxNotices && kept[0].Text=="Alert 3" && kept[^1].Text=="Alert 6" && kept[^1].Id==6,"the newest four, oldest first: "+string.Join(",",kept.Select(k=>k.Text)));
            Check(log.Snapshot(T0+6+(long)LinkNoticeLog.Keep.TotalMilliseconds+1) is null,"gone after two minutes");
            log.Add(new string('x',500),T0);
            var cut=log.Snapshot(T0)!.Single().Text;
            Check(cut.Length==LocalLinkProtocol.MaxNoticeText && cut.EndsWith('…'),"cut at the bound: "+cut.Length);
        }),
        ("Link notices 1155: the application's reader rings each alert once, not an old one when the link comes, again after a plugin restart",() =>
        {
            var reader=new LinkNoticeReader();
            LinkNotice N(long id,long at,string text)=>new(id,at,text);
            var now=T0+600_000;
            // The link comes: an alert of five minutes ago is history, one of ten seconds ago rings.
            var first=reader.Take(Sample(10,[N(1,now-300_000,"old"),N(2,now-10_000,"duty ready")]),now);
            Check(first.SequenceEqual(["duty ready"]),"first sight: only the fresh: "+string.Join(",",first));
            Check(reader.Take(Sample(11,[N(1,now-300_000,"old"),N(2,now-10_000,"duty ready")]),now+1000).Count==0,"heartbeat: nothing again");
            var next=reader.Take(Sample(12,[N(2,now-10_000,"duty ready"),N(3,now+2000,"GP 600"),N(4,now+2000,"repair")]),now+2000);
            Check(next.SequenceEqual(["GP 600","repair"]),"new numbers, in order: "+string.Join(",",next));
            // An alert added long after (the app was busy): still rung, it is new by its number.
            Check(reader.Take(Sample(13,[N(5,now-100_000,"late")]),now+3000).SequenceEqual(["late"]),"a new number rings whatever its time");
            // The plugin starts again: its sequence and numbers count from 1 again.
            Check(reader.Take(Sample(1,[N(1,now+4000,"after restart")]),now+4000).SequenceEqual(["after restart"]),"plugin restarted: its fresh alert rings");
            Check(reader.Take(Sample(2,[N(1,now+4000,"after restart")]),now+5000).Count==0,"and once only");
            // The link goes and comes back: the alert already rung is not rung again.
            Check(reader.Take(null,now+6000).Count==0,"link lost");
            Check(reader.Take(Sample(3,[N(1,now+4000,"after restart")]),now+7000).Count==0,"link back: the alert already rung is not rung again");
            Check(reader.Take(Sample(4,[N(1,now+4000,"after restart"),N(2,now+8000,"next")]),now+8000).SequenceEqual(["next"]),"then the next one");
        }),
        ("Link notices 1155: through a real pipe, the alerts reach the application's reader; refused when out of bounds; the worst line fits",() =>
        {
            Check(LocalLinkProtocol.Version==3,"same protocol: an older application ignores the field");
            var worst=new LinkSnapshot(LocalLinkProtocol.Version,new string('9',LocalLinkProtocol.MaxPlugin),true,false,uint.MaxValue,new string('漢',LocalLinkProtocol.MaxZone),long.MaxValue,
                new LinkLive(new string('漢',LocalLinkProtocol.MaxJob),1000,100_000,100_000,1000,1000),
                [.. LocalLinkProtocol.Categories.Values.Append(LocalLinkProtocol.SettingsCategory).Take(LocalLinkProtocol.MaxChanges).Select(c=>new LinkChange(c,long.MaxValue))],
                [.. Enumerable.Range(1,LocalLinkProtocol.MaxNotices).Select(n=>new LinkNotice(long.MaxValue-n,long.MaxValue,new string('«',LocalLinkProtocol.MaxNoticeText)))]);
            var line=LocalLinkProtocol.Serialize(worst);
            Check(Encoding.UTF8.GetByteCount(line)<=LocalLinkProtocol.MaxLine,"worst line "+Encoding.UTF8.GetByteCount(line)+" bytes");
            Check(LocalLinkProtocol.TryParse(line,out _,out var why),"the worst line is read: "+why);
            foreach(var bad in new IReadOnlyList<LinkNotice>[]{[.. Enumerable.Range(1,5).Select(n=>new LinkNotice(n,T0,"x"))],[new LinkNotice(0,T0,"x")],[new LinkNotice(1,T0,new string('x',LocalLinkProtocol.MaxNoticeText+1))],[new LinkNotice(1,T0,null!)]})
                Check(!LocalLinkProtocol.TryParse(LocalLinkProtocol.Serialize(Sample(1,bad)),out _,out var refused) && refused=="notice list refused","refused: "+refused);
            Check(LocalLinkProtocol.TryParse("{\"Protocol\":3,\"Plugin\":\"0.1154.0\",\"LoggedIn\":true,\"Paused\":false,\"Territory\":1,\"Zone\":\"\",\"Sequence\":1}",out var older,out _) && older!.Notices is null,"a plugin of before sends none");
            if(!OperatingSystem.IsWindows())return;
            var log=new LinkNoticeLog();
            var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            log.Add("Duty ready: Sastasha",now);log.Add("Allagan tomestones: 1,900 of 2,000",now);
            var name="TenshoAssistant.Link.Test."+Guid.NewGuid().ToString("N")[..12];
            var server=new LocalLinkServer(name);
            try
            {
                server.Publish(Sample(1,log.Snapshot(now)));
                using var pipe=new NamedPipeClientStream(".",name,PipeDirection.In,PipeOptions.CurrentUserOnly);
                pipe.Connect(5000);
                using var reader=new StreamReader(pipe,Encoding.UTF8);
                Check(LocalLinkProtocol.TryParse(reader.ReadLine(),out var received,out var error),"read: "+error);
                var app=new LinkNoticeReader();
                Check(app.Take(received,now+500).SequenceEqual(["Duty ready: Sastasha","Allagan tomestones: 1,900 of 2,000"]),"both alerts ring in the application");
                Check(app.Take(received,now+1500).Count==0,"once");
            }
            finally{server.Dispose();}
        }),
        ("Link notices 1155: wired where the alerts ring, rung with the alarms' options, never the Tensho alarms or the garden twice",() =>
        {
            var plugin=Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Split("if(options.ChatAllowed(inDuty) || options.SoundAllowed(inDuty))linkNotices.Add(message);").Length==4,"currencies, duty ready, GP");
            Check(plugin.Contains("if(repairText is not null)linkNotices.Add(repairText);") && plugin.Contains("if(extractText is not null)linkNotices.Add(extractText);"),"gear wear");
            Check(plugin.Contains("if(configuration.ItemNoteImportantSound){Alarms.AlarmService.PlaySound((configuration.AlarmOptions ?? new()).Sound,log);linkNotices.Add(message);}"),"important items, when they ring");
            Check(Read("src/TenshoAssistant/Plugin.Desktop.cs").Contains("log,linkNotices){Held=mainWindow.AlarmHeldItems};mainWindow.LinkNotice=linkNotices.Add;"),"the feed sends them; the hunt marks too");
            Check(Read("src/TenshoAssistant/MainWindow.HuntTrain.cs").Contains("LinkNotice?.Invoke(alert);"),"hunt marks");
            Check(!Read("src/TenshoAssistant/Alarms/AlarmService.Dalamud.cs").Contains("linkNotices") && !Read("src/TenshoAssistant/MainWindow.Garden.cs").Contains("LinkNotice"),
                "the Tensho alarms and the garden ring in the app by their own rule (AlarmBell, GardenBell): not sent twice");
            Check(Read("src/TenshoAssistant/Link/LocalLinkFeed.Dalamud.cs").Contains("notices.Snapshot(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())"),"in each snapshot");
            Check(Read("src/TenshoAssistant/MainWindow.DesktopData.cs").Contains("if(texts.Length==0 || options.DesktopOnlyGameBehind && gameInFront)return ([],false);\n        return (texts,options.DesktopSound);"),"same options as the alarms");
            Check(Read("src/TenshoAssistant/MainWindow.DesktopData.cs").Contains("LinkNotices=DesktopLinkNotices,") && Read("src/TenshoAssistant.Desktop/Host.cs").Contains("=>Surface.LinkNotices(texts,gameInFront);"),"called through the typed contract (X-151, 0.1238)");
            Check(Read("src/TenshoAssistant.Desktop/StatusBar.cs").Contains("linkNotices.Take(linked,now.ToUnixTimeMilliseconds())"),"read once a second with the bell");
            var options=new AlarmSettings();
            Check(options.DesktopSound && options.DesktopOnlyGameBehind,"defaults");
        }),
    ];
}
