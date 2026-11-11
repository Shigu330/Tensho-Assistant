using TenshoAssistant.Link;

// 0.1114 (X-119 step 2 suite, part 1): protocol 2 of the local link: small live states sent whole, the names of the plugin's saved files that
// changed (never their content), the application reading its settings file again on such a notice; bounds and refusals.
internal static class LinkLive1114Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static LinkSnapshot Sample(LinkLive? live=null,IReadOnlyList<LinkChange>? changes=null)
        => new(LocalLinkProtocol.Version,"0.1114.0",true,false,132,"New Gridania",7,live,changes);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Link 1114: the live states both ways; out-of-bounds values refused (X-149, 0.1119: change notices are categories, LinkCategories1119Tests)",() =>
        {
            Check(LocalLinkProtocol.Version>=2 && LocalLinkProtocol.MaxLine==8192,"protocol 2 or later, 8 KB lines");
            var line=LocalLinkProtocol.Serialize(Sample(new("MIN",100,650,700,20,140)));
            Check(LocalLinkProtocol.TryParse(line,out var back,out var error) && back!.Live==new LinkLive("MIN",100,650,700,20,140),"round trip: "+error);
            Check(LocalLinkProtocol.TryParse(LocalLinkProtocol.Serialize(Sample()),out var bare,out _) && bare!.Live is null && bare.Changes is null,"both parts are optional");
            foreach(var bad in new[]{new LinkLive(new string('J',17),1,1,1,1,1),new LinkLive("MIN",1001,1,1,1,1),new LinkLive("MIN",1,1,1,141,140),new LinkLive("MIN",1,-2,1,1,1),new LinkLive("MIN",1,1,1,1,1001)})
                Check(!LocalLinkProtocol.TryParse(LocalLinkProtocol.Serialize(Sample(bad)),out _,out var why) && why=="live state out of bounds","refused: "+bad);
            Check(!LocalLinkProtocol.TryParse(LocalLinkProtocol.Serialize(Sample(changes:[new LinkChange("settings",0)])),out _,out _),"generation 0 refused");
        }),
        ("Link 1114: the change log and the reader (one entry per category, newest last, bounded, each taken once, a new plugin start counted again)",() =>
        {
            var log=new LinkChangeLog();
            Check(log.Snapshot() is null,"nothing at first");
            log.Add("settings");log.Add("bank");log.Add("settings");
            var first=log.Snapshot()!;
            Check(first.Count==2 && first[0].Category=="bank" && first[1]==new LinkChange("settings",3),"one entry per category, its newest generation last");
            for(var i=0;i<40;i++)log.Add($"c{i}");
            Check(log.Snapshot()!.Count==LocalLinkProtocol.MaxChanges && log.Snapshot()![^1].Category=="c39","bounded, oldest leave first");
            var reader=new LinkChangeReader();
            Check(reader.Take(null).Count==0 && reader.Take(Sample()).Count==0,"no notice, nothing to read");
            var snapshot=Sample(changes:first);
            Check(reader.Take(snapshot).SequenceEqual(["bank","settings"]),"every category at first");
            Check(reader.Take(snapshot).Count==0,"each once");
            Check(reader.Take(Sample(changes:[..first,new LinkChange("voyages",4)])).SequenceEqual(["voyages"]),"only the new one");
            Check(reader.Take(Sample(changes:[new LinkChange("settings",1)])).SequenceEqual(["settings"]),"the plugin started again: counted from zero");
        }),
        ("Link 1114: wiring (feed reads the live states on the framework thread, watchers only while the link is on, app reads its settings again between frames)",() =>
        {
            var feed=Read("src/TenshoAssistant/Link/LocalLinkFeed.Dalamud.cs");
            Check(feed.Contains("ReadLive(loggedIn),changes.Snapshot()") && feed.Contains("if(server is null){server=new LocalLinkServer(LocalLinkProtocol.PipeName(),message=>log.Information(message));Watch();}"),"live states and notices in the snapshot; watching starts with the link");
            Check(feed.Split("Unwatch();").Length>=3 && feed.Contains("liveFailed=true;"),"watchers stopped with the link and at unload; a failed reading left out (R08)");
            Check(feed.Contains("private void OnFile(object? sender,FileSystemEventArgs change)\n    {\n        if(LocalLinkProtocol.ChangeCategory(settingsFile,folder,change.FullPath) is { } category)changes.Add(category);\n    }"),"a watcher's event only adds a category (X-149)");
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("new Link.LocalLinkFeed(framework,client,player,inventory,objects,pluginInterface.ConfigFile.FullName,pluginInterface.GetPluginConfigDirectory(),configuration,"),"the plugin's own files watched");
            var host=Read("src/TenshoAssistant.Desktop/Host.cs");
            Check(host.Contains("Newtonsoft.Json.JsonConvert.PopulateObject(File.ReadAllText(settingsPath),target,settings);") && host.Contains("ObjectCreationHandling=Newtonsoft.Json.ObjectCreationHandling.Replace"),"settings read again into the same object, lists replaced");
            Check(!host.Contains("File.WriteAllText") && !host.Contains("File.Write("),"the app never writes the plugin's settings");
            var program=Read("src/TenshoAssistant.Desktop/Program.cs");
            Check(program.IndexOf("status.Poll();",StringComparison.Ordinal) is var poll and >0 && poll<program.IndexOf("Ui.Frame(host,status",StringComparison.Ordinal),"between frames, before drawing");
            Check(Read("src/TenshoAssistant.Desktop/StatusBar.cs").Contains("if(!categories.Contains(TenshoAssistant.Link.LocalLinkProtocol.SettingsCategory))return;"),"the settings are what is read again");
            Check(Read("src/TenshoAssistant.Desktop/Headless.cs").Contains("new(\"MIN\",100,650,700,20,140)"),"Verify's headless run draws the live line");
        }),
    ];
}
