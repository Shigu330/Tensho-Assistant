using System.Diagnostics;
using System.Text.Json.Nodes;
using TenshoAssistant.Desktop;

// 0.1120 (X-150, GPT review of 11/11, audit A15): the desktop app reads the plugin's settings off the frame, bounded, without the Discord
// addresses, with a closed list of types, groups the link's notices into one reading and applies a valid reading at once (a failed one
// keeps the last valid settings). Behaviour tests (R28): a real settings file holding secrets, a file over the bound, a file being written
// by another handle, a burst of notices, a reading that fails between two good ones.
internal static class SettingsRead1120Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private const string Hook="https://discord.com/api/webhooks/123456789/SECRET-TOKEN-abc";
    private const string Other="https://discord.com/api/webhooks/987/OTHER-SECRET";

    private static string Settings(int version=7)=>$$$"""
        {"$type":"TenshoAssistant.Configuration, TenshoAssistant","Version":{{{version}}},"Ratio":0.30000000000000004,
         "Discord":{"$type":"TenshoAssistant.Alarms.DiscordSettings, TenshoAssistant","Enabled":true,"WebhookUrl":"{{{Hook}}}","CategoryUrls":{"Gathering":"{{{Other}}}"},"Username":"Tensho"},
         "Lists":[{"$type":"TenshoAssistant.Alarms.TenshoAlarm, TenshoAssistant","Label":"a","Nested":{"WebhookUrl":"{{{Hook}}}"}}]}
        """;

    private static void InTemp(Action<string> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"tensho-settings-1120-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try{run(root);}
        finally{try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Settings 1120: a real settings file with Discord addresses: the app's copy holds none of them, the rest kept as written (type names first, numbers exact)",() => InTemp(root =>
        {
            var path=Path.Combine(root,"TenshoAssistant.json");
            File.WriteAllText(path,Settings());
            var projected=SettingsGuard.Project(SettingsGuard.ReadBounded(path));
            Check(!projected.Contains("SECRET",StringComparison.Ordinal) && !projected.Contains("webhooks",StringComparison.Ordinal) && !projected.Contains("WebhookUrl",StringComparison.Ordinal) && !projected.Contains("CategoryUrls",StringComparison.Ordinal),"no secret at any depth: "+projected);
            var node=JsonNode.Parse(projected)!.AsObject();
            Check(node.First().Key=="$type" && node["Discord"]!.AsObject().First().Key=="$type" && (bool)node["Discord"]!["Enabled"]! && ((string?)node["Discord"]!["Username"])=="Tensho","type names stay first, other fields kept");
            Check(projected.Contains("0.30000000000000004",StringComparison.Ordinal) && ((int)node["Version"]!)==7,"numbers exactly as written");
            var refused=false;
            try{SettingsGuard.Project("[1,2]");}catch(System.Text.Json.JsonException){refused=true;}
            Check(refused,"not an object: refused");
        })),
        ("Settings 1120: reading is bounded and shares the file the game is writing",() => InTemp(root =>
        {
            var big=Path.Combine(root,"big.json");
            using(var file=new FileStream(big,FileMode.Create)){file.SetLength(SettingsGuard.MaxBytes+1);}
            var refused=false;
            try{SettingsGuard.ReadBounded(big);}catch(InvalidDataException){refused=true;}
            Check(refused,"over 8 MB: refused before parsing");
            var path=Path.Combine(root,"TenshoAssistant.json");
            File.WriteAllText(path,Settings());
            using(new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.ReadWrite))
                Check(SettingsGuard.ReadBounded(path).Contains("TenshoAssistant.Configuration",StringComparison.Ordinal),"read while another handle writes it");
        })),
        ("Settings 1120: the closed list of types (the plugin's, primitives, collections of those; nothing else)",() =>
        {
            var plugin=typeof(TenshoAssistant.Configuration).Assembly;
            foreach(var good in new[]{typeof(TenshoAssistant.Configuration),typeof(TenshoAssistant.Alarms.DiscordSettings),typeof(int),typeof(string),typeof(System.Numerics.Vector4),
                    typeof(Dictionary<uint,List<uint>>),typeof(Dictionary<ulong,Dictionary<uint,int>>),typeof(List<TenshoAssistant.Alarms.TenshoAlarm>),typeof(HashSet<string>),
                    typeof((string,int,string,int,string,int)),typeof(uint?),typeof(TenshoAssistant.Alarms.AlarmSource),typeof(int[])})
                Check(SettingsGuard.AllowedType(good,plugin),"allowed: "+good);
            foreach(var bad in new[]{typeof(Process),typeof(FileInfo),typeof(List<Process>),typeof(Dictionary<string,FileStream>),typeof(System.Reflection.Assembly),typeof(Action),
                    typeof(System.Collections.ObjectModel.ObservableCollection<int>),typeof(int[,]),typeof(Func<string>),typeof(StringComparison)})
                Check(!SettingsGuard.AllowedType(bad,plugin),"refused: "+bad);
        }),
        ("Settings 1120: a burst of notices is one reading off the frame; a notice during a reading asks one more; a failed reading keeps the last valid settings",() =>
        {
            var reads=0;var fail=false;var gate=new ManualResetEventSlim(true);
            var grouped=new Grouped<int>(()=>{gate.Wait(5000);var n=Interlocked.Increment(ref reads);if(fail)throw new IOException("being written");return n;},100);
            var thread=Environment.CurrentManagedThreadId;
            for(var i=0;i<20;i++)grouped.Request();
            Check(SpinWait.SpinUntil(()=>!grouped.Busy,5000) && reads==1,"twenty notices: one reading, got "+reads);
            var applied=0;
            void Poll(){if(grouped.Take() is { } result && result.Error is null)applied=result.Value;}
            Poll();
            Check(applied==1 && grouped.Take() is null,"taken once and applied");
            gate.Reset();grouped.Request();
            Check(SpinWait.SpinUntil(()=>Volatile.Read(ref reads)==1 && grouped.Busy,1000),"a reading started");
            Thread.Sleep(200);
            grouped.Request();grouped.Request();
            gate.Set();
            Check(SpinWait.SpinUntil(()=>!grouped.Busy,5000) && reads==3,"notices during a reading: one more, got "+reads);
            Poll();
            Check(applied==3,"the newest reading applied");
            fail=true;grouped.Request();
            Check(SpinWait.SpinUntil(()=>!grouped.Busy,5000),"the failed reading ends");
            var failed=grouped.Take();
            Check(failed is { Error: IOException } && applied==3,"a failed reading is said and the last valid settings stay");
            Check(thread==Environment.CurrentManagedThreadId,"the caller never waited on the reading");
        }),
        ("Settings 1120: wiring (start and link read through the guard, the binder refuses other types, the live settings changed only between frames, never written)",() =>
        {
            var host=Read("src/TenshoAssistant.Desktop/Host.cs");
            Check(host.Contains("Newtonsoft.Json.JsonConvert.DeserializeObject(SettingsGuard.Project(SettingsGuard.ReadBounded(path)),configurationType,settings);") && !host.Contains("PopulateObject") && !host.Contains("File.ReadAllText("),"one bounded, projected reading");
            Check(host.Contains("return SettingsGuard.AllowedType(type,plugin) ? type : throw new Newtonsoft.Json.JsonSerializationException("),"the binder enforces the closed list");
            Check(host.Contains("var value=Parse(path);"),"the start reads through it too");
            var status=Read("src/TenshoAssistant.Desktop/StatusBar.cs");
            Check(status.Contains("(reload??=new(host.ReadSettingsAgain)).Request();") && status.Contains(": result.Value is { } fresh ? host.ApplySettings(fresh)"),"read off the frame, applied at once when valid");
            Check(!host.Contains("File.Write") && !status.Contains("File.Write"),"the app never writes the plugin's settings");
        }),
    ];
}
