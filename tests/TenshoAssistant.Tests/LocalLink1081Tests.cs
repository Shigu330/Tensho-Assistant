using System.Text.RegularExpressions;
using TenshoAssistant.Link;

// X-119 step 2, part 1 (0.1081): the local link between the plugin and the desktop application (src/TenshoAssistant/Link/LocalLink.cs,
// one source for both ends). The protocol refuses what it does not know; a real pipe carries a snapshot from the plugin's server to the
// application's client; a second server of the same name waits (Busy); the wiring keeps the link off by default, one way, current user
// only, behind the competitive gate, and the application reads it without writing.
internal static class LocalLink1081Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath(file));

    private static bool Wait(Func<bool> condition,int milliseconds=8000)
    {
        var until=Environment.TickCount64+milliseconds;
        while(Environment.TickCount64<until){if(condition())return true;Thread.Sleep(50);}
        return condition();
    }

    private static LinkSnapshot Sample(long sequence=1) => new(LocalLinkProtocol.Version,"0.1081.0",true,false,132,"New Gridania",sequence);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Local link 1081: a snapshot goes through the wire format unchanged; the pipe name is per Windows user and stable",() =>
        {
            var line=LocalLinkProtocol.Serialize(Sample());
            Check(!line.Contains('\n'),"one line");
            Check(LocalLinkProtocol.TryParse(line,out var back,out var error) && back==Sample(),"round trip: "+error);
            Check(LocalLinkProtocol.PipeName("PC\\Alice")==LocalLinkProtocol.PipeName("pc\\alice"),"case does not matter");
            Check(LocalLinkProtocol.PipeName("PC\\Alice")!=LocalLinkProtocol.PipeName("PC\\Bob"),"two users, two pipes");
            Check(Regex.IsMatch(LocalLinkProtocol.PipeName("PC\\Alice"),@"^TenshoAssistant\.Link\."+LocalLinkProtocol.Version+@"\.[0-9A-F]{16}$"),"name shape: "+LocalLinkProtocol.PipeName("PC\\Alice"));
        }),
        ("Local link 1081: the protocol refuses an empty, too long, malformed, other-protocol or oversized line",() =>
        {
            Check(!LocalLinkProtocol.TryParse("",out _,out _),"empty");
            Check(!LocalLinkProtocol.TryParse(new string(' ',10)+"{"+new string('a',LocalLinkProtocol.MaxLine)+"}",out _,out var tooLong) && tooLong=="line too long","too long: "+tooLong);
            Check(!LocalLinkProtocol.TryParse("{not json",out _,out _),"malformed");
            Check(!LocalLinkProtocol.TryParse("[1,2]",out _,out _),"not an object");
            Check(!LocalLinkProtocol.TryParse("{\"Protocol\":1}",out _,out _),"missing fields");
            Check(!LocalLinkProtocol.TryParse(LocalLinkProtocol.Serialize(Sample() with {Protocol=1}),out _,out var other) && other!.Contains("protocol 1"),"other protocol: "+other);
            Check(!LocalLinkProtocol.TryParse(LocalLinkProtocol.Serialize(Sample() with {Zone=new string('z',LocalLinkProtocol.MaxZone+1)}),out _,out _),"zone too long");
            Check(!LocalLinkProtocol.TryParse(LocalLinkProtocol.Serialize(Sample() with {Plugin=new string('9',LocalLinkProtocol.MaxPlugin+1)}),out _,out _),"version too long");
        }),
        ("Local link 1081: a real pipe carries the plugin's snapshots to the application; a second server waits; closing the server ends the link",() =>
        {
            if(!OperatingSystem.IsWindows())return;// the link is Windows only (named pipes with an access list)
            var name="TenshoAssistant.Link.Test."+Guid.NewGuid().ToString("N")[..12];
            var log=new List<string>();
            using(var client=new LocalLinkClient(name))
            {
                var server=new LocalLinkServer(name,message=>{lock(log)log.Add(message);});
                try
                {
                    server.Publish(Sample(1));
                    Check(Wait(()=>client.Current?.Sequence==1),$"first snapshot received (server {server.State} {server.Problem}, client {client.Problem})");
                    Check(server.State==LocalLinkState.Linked,"server linked: "+server.State);
                    server.Publish(Sample(2) with {LoggedIn=false,Territory=0,Zone=""});
                    Check(Wait(()=>client.Current?.Sequence==2),"a change is sent at once");
                    Check(client.Current is { LoggedIn:false, Zone:"" },"the new state");
                    using var second=new LocalLinkServer(name);
                    Check(Wait(()=>second.State==LocalLinkState.Busy),"a second server of the same name is busy: "+second.State+" "+second.Problem);
                }
                finally{server.Dispose();}
                Check(server.State==LocalLinkState.Off,"disposed: off");
                lock(log)Check(log.Any(line=>line.Contains("Linked",StringComparison.Ordinal)),"the server logs its state: "+string.Join(" | ",log));
            }
        }),
        ("Local link 1081: wiring: off by default and reset, one way, current user only, network denied, behind the competitive gate, read-only app",() =>
        {
            var configuration=Read("src/TenshoAssistant/Configuration.cs");
            Check(configuration.Contains("public bool DesktopLink { get; set; }\n") || configuration.Contains("public bool DesktopLink { get; set; }\r\n"),"DesktopLink off by default (no initialiser)");
            Check(configuration.Contains("\"DesktopLink\", "),"reset with the other settings");
            var link=Read("src/TenshoAssistant/Link/LocalLink.cs");
            Check(link.Contains("PipeDirection.Out,1,") && link.Contains("PipeOptions.FirstPipeInstance"),"server: out only, one instance");
            Check(link.Contains("WellKnownSidType.NetworkSid") && link.Contains("AccessControlType.Deny"),"network logons denied");
            Check(link.Contains("PipeDirection.In,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly"),"client: in only, current user only");
            Check(!Regex.IsMatch(link,@"\b(Socket|TcpListener|HttpListener|HttpClient)\b"),"no network port");
            var feed=Read("src/TenshoAssistant/Link/LocalLinkFeed.Dalamud.cs");
            Check(feed.Contains("CompetitiveGate.Add(framework,OnUpdate)") && feed.Contains("CompetitiveGate.Remove(framework,OnUpdate)"),"behind the competitive gate, removed on dispose");
            Check(feed.Contains("if(!configuration.DesktopLink){Stop();return;}"),"the setting opens and closes the pipe");
            var plugin=Read("src/TenshoAssistant/Plugin.Desktop.cs");
            Check(plugin.Contains("registrations.Register(()=>{linkFeed=new Link.LocalLinkFeed(") && plugin.Contains("linkFeed?.Dispose();linkFeed=null;"),"owned by the plugin's registrations");
            Check(Read("src/TenshoAssistant/MainWindow.Advanced.cs").Contains("BoolSetting(\"Advanced.DesktopLink\""),"the setting is in Settings › Advanced");
            var desktop=Read("src/TenshoAssistant.Desktop/TenshoAssistant.Desktop.csproj");
            Check(desktop.Contains(@"<Compile Include=""..\TenshoAssistant\Link\LocalLink.cs"""),"the app compiles the same source");
            var program=Read("src/TenshoAssistant.Desktop/Program.cs");
            Check(program.Contains("new TenshoAssistant.Link.LocalLinkClient(TenshoAssistant.Link.LocalLinkProtocol.PipeName())") && program.Contains("--no-link"),"the app reads the link");
            Check(Read("src/TenshoAssistant.Desktop/Headless.cs").Contains("status bar link text missing"),"the headless run checks the link's texts");
            Check(TenshoAssistant.Help.Faq.PageOf("DesktopLink")=="Settings","FAQ entry");
        }),
    ];
}
