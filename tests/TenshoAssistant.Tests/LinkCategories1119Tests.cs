using System.IO.Pipes;
using System.Text;
using TenshoAssistant.Link;

// 0.1119 (X-149, GPT review of 11/11): the local link's change notices are categories of a closed list (protocol 3), never a file name
// nor a character's id; another protocol is refused and the application says when the game offers another one. Behaviour test (R28,
// confidentiality): real saved files of a plugin folder (the bank's and the characters' named by a character id) changed, their notices
// sent through a real pipe, and the bytes the application receives searched for any name or id.
internal static class LinkCategories1119Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static LinkSnapshot Sample(IReadOnlyList<LinkChange>? changes=null)
        => new(LocalLinkProtocol.Version,"0.1119.0",true,false,132,"New Gridania",7,null,changes);
    private static bool Wait(Func<bool> done)
    {
        var until=Environment.TickCount64+10_000;
        while(Environment.TickCount64<until){if(done())return true;Thread.Sleep(50);}
        return done();
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Link 1119: real saved files changed, their notices through a real pipe: the bytes received hold categories only, no file name, no character id",() =>
        {
            if(!OperatingSystem.IsWindows())return;
            var root=Path.Combine(Path.GetTempPath(),"tensho-link-1119-"+Guid.NewGuid().ToString("N"));
            var folder=Path.Combine(root,"TenshoAssistant");var settings=Path.Combine(root,"TenshoAssistant.json");
            const string id="18014398509481984";
            var paths=new[]{settings,Path.Combine(folder,"InventoryBank",id+".json"),Path.Combine(folder,"InventoryBank",id+".json.bak"),Path.Combine(folder,"Characters",id+".json"),
                Path.Combine(folder,"GilJournal",id+".json"),Path.Combine(folder,"CraftLists.json"),Path.Combine(folder,"VoyageHistory.json"),
                Path.Combine(folder,"InventoryBank.lock"),Path.Combine(folder,"MyTools.json."+Guid.NewGuid().ToString("N")+".tmp"),Path.Combine(folder,"PlayState.json"),
                Path.Combine(folder,"DataCache-v1","items.json"),Path.Combine(folder,"SelfCheckSeen.json"),Path.Combine(folder,"Unknown","x.json")};
            try
            {
                var log=new LinkChangeLog();
                foreach(var path in paths)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllText(path,"{}");
                    if(LocalLinkProtocol.ChangeCategory(settings,folder,path) is { } category)log.Add(category);
                }
                var sent=log.Snapshot()!;
                Check(sent.Select(change=>change.Category).SequenceEqual(["settings","bank","characters","gil-journal","craft-lists","voyages"]),"categories: "+string.Join(", ",sent.Select(c=>c.Category)));
                var name="TenshoAssistant.Link.Test."+Guid.NewGuid().ToString("N")[..12];
                var server=new LocalLinkServer(name);
                try
                {
                    server.Publish(Sample(sent));
                    using var pipe=new NamedPipeClientStream(".",name,PipeDirection.In,PipeOptions.CurrentUserOnly);
                    pipe.Connect(5000);
                    using var reader=new StreamReader(pipe,Encoding.UTF8);
                    var line=reader.ReadLine()!;
                    Check(!line.Contains(id,StringComparison.Ordinal) && !line.Contains(".json",StringComparison.OrdinalIgnoreCase) && !line.Contains("TenshoAssistant/",StringComparison.Ordinal)
                        && !line.Contains("InventoryBank",StringComparison.Ordinal) && !line.Contains("Characters",StringComparison.Ordinal),"no file name, no id on the wire: "+line);
                    Check(LocalLinkProtocol.TryParse(line,out var received,out var error) && received!.Changes!.Count==6,"the application reads the categories: "+error);
                    Check(new LinkChangeReader().Take(received).Contains(LocalLinkProtocol.SettingsCategory),"the settings notice is what makes the app read them again");
                }
                finally{server.Dispose();}
            }
            finally{try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
        }),
        // 0.1207 (X-184 N6): such a notice is left out (never passed on) instead of refusing the whole snapshot (Link1207Tests).
        ("Link 1119: a notice out of the closed list (a file name, an id, an unknown word) is never passed on; protocol 2 is refused",() =>
        {
            Check(LocalLinkProtocol.Version==3,"protocol 3");
            foreach(var bad in new[]{"TenshoAssistant.json","TenshoAssistant/InventoryBank/18014398509481984.json","18014398509481984","Bank","other","","../x"})
                Check(LocalLinkProtocol.TryParse(LocalLinkProtocol.Serialize(Sample([new LinkChange(bad,1)])),out var kept,out _) && kept!.Changes!.Count==0,"left out: "+bad);
            foreach(var good in LocalLinkProtocol.Categories.Values.Append(LocalLinkProtocol.SettingsCategory))
                Check(LocalLinkProtocol.TryParse(LocalLinkProtocol.Serialize(Sample([new LinkChange(good,1)])),out _,out _),"accepted: "+good);
            Check(!LocalLinkProtocol.TryParse(LocalLinkProtocol.Serialize(Sample() with {Protocol=2}),out _,out var old) && old!.Contains("protocol 2"),"protocol 2 refused: "+old);
            Check(LocalLinkProtocol.Serialize(Sample([new LinkChange("bank",1)])).Contains("\"Category\":\"bank\"",StringComparison.Ordinal),"the field is named Category");
        }),
        ("Link 1119: the app finds a link of another protocol for its user (and only its user) and says so",() =>
        {
            var own=LocalLinkProtocol.PipeName("PC\\Alice");
            var hash=own[(own.LastIndexOf('.')+1)..];
            var bob=LocalLinkProtocol.PipeName("PC\\Bob");
            Check(LocalLinkProtocol.OtherProtocol([@"\\.\pipe\TenshoAssistant.Link.2."+hash,@"\\.\pipe\other"],"PC\\Alice")==2,"protocol 2 of the same user found");
            Check(LocalLinkProtocol.OtherProtocol([@"\\.\pipe\"+own],"PC\\Alice") is null,"its own protocol is not « other »");
            Check(LocalLinkProtocol.OtherProtocol([@"\\.\pipe\TenshoAssistant.Link.2."+bob[(bob.LastIndexOf('.')+1)..]],"PC\\Alice") is null,"another user's link ignored");
            Check(LocalLinkProtocol.OtherProtocol([@"\\.\pipe\TenshoAssistant.Link.Test.abc."+hash],"PC\\Alice") is null,"a non-number ignored");
            var status=Read("src/TenshoAssistant.Desktop/StatusBar.cs");
            Check(status.Contains("else if(OtherProtocol?.Invoke() is { } other)lines.Add((host.Text(\"Desktop.LinkOtherProtocol\",other,TenshoAssistant.Link.LocalLinkProtocol.Version),true));"),"the status bar says it");
            Check(Read("src/TenshoAssistant.Desktop/Program.cs").Contains("status.OtherProtocol=()=>link.OtherProtocol;"),"wired to the client");
            foreach(var lang in new[]{"en","fr"})Check(Read($"src/TenshoAssistant/Localization/Languages/{lang}.json").Contains("\"LinkOtherProtocol\": "),"text in "+lang);
            var security=Read("SECURITY.md");
            Check(security.Contains("categories of a closed list") && security.Contains("catégories d'une liste fermée") && !security.Contains("the zone's name, nothing else"),"SECURITY says exactly what passes, both halves");
        }),
    ];
}
