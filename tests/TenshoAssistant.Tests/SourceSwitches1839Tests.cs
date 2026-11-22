using TenshoAssistant;
using TenshoAssistant.Community;
using TenshoAssistant.Duties;
using TenshoAssistant.Fashion;
using TenshoAssistant.Market;
using TenshoAssistant.Monsters;

// 0.1839 (X-834, security review I6): the desktop application built the network services (Garland Tools, Universalis, the fashion data, duty
// and monster loot) without their switches, set only by Plugin.cs; a service reads « no switch » as allowed: with « game data and what you
// observed only » on in the game, « Fetch » in the application still sent a request. Now one table (CommunitySources.SwitchFor / Wire) sets
// them in the game and in the application (Host.WireSources), and the application turns off a network service the table does not know.
internal static class SourceSwitches1839Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private sealed class Counting:HttpMessageHandler
    {
        public int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Requests);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new StringContent("{}")});
        }
    }

    public static IEnumerable<(string Name,Action Test)> Cases =>
    [
        ("Source switches 1839: behaviour: « game data only » on, a wired Garland client sends nothing; every service of the table follows the settings (X-834)",() =>
        {
            var configuration=new Configuration();
            var handler=new Counting();
            using var garland=new GarlandClient(new HttpClient(handler));
            using var market=new MarketService();
            var services=new object[]{garland,market,new MarketBoardService(),new DutyLootService(garland),new MonsterLootService(garland),new FashionDataService()};
            foreach(var service in services)Check(CommunitySources.Wire(service,configuration),"wired: "+service.GetType().Name);
            bool On(object service)=>((Func<bool>)service.GetType().GetProperty("Enabled")!.GetValue(service)!)();
            Check(services.All(On),"the sources on by default: every service allowed");
            CommunitySources.ApplyGameDataOnly(configuration,true);
            Check(!services.Any(On),"« game data and what you observed only »: every service off, read live from the settings");
            try{garland.ReadDocumentAsync(GarlandResource.Instance,1,CancellationToken.None).GetAwaiter().GetResult();Check(false,"refused");}
            catch(SourceDisabledException){}
            Check(handler.Requests==0,"no request sent: "+handler.Requests);
            CommunitySources.ApplyGameDataOnly(configuration,false);
            configuration.SourceUses.GarlandDutyLoot=false;
            Check(On(garland) && !On(services[3]) && On(services[4]),"a use's own switch: duty loot off, Garland Tools and monster loot on");
            Check(!CommunitySources.Wire(new object(),configuration) && CommunitySources.SwitchFor("TenshoAssistant.Unknown",configuration) is null,"a service the table does not know: not wired (the application turns it off)");
        }),
        ("Source switches 1839: the game and the desktop application set the switches from the same table (X-834)",() =>
        {
            // The game sets them in Plugin.cs, the table says the same for each (kept alike here).
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            var source=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/CommunitySources.cs"));
            foreach(var (line,row) in new[]{
                ("garland.Enabled=()=>configuration.CommunityGarland;","\"TenshoAssistant.Community.GarlandClient\"=>()=>c.CommunityGarland,"),
                ("market.Enabled=()=>configuration.CommunityUniversalis;","\"TenshoAssistant.Market.MarketService\" or \"TenshoAssistant.Market.MarketBoardService\"=>()=>c.CommunityUniversalis,"),
                ("marketBoard.Enabled=()=>configuration.CommunityUniversalis;","\"TenshoAssistant.Market.MarketService\" or \"TenshoAssistant.Market.MarketBoardService\"=>()=>c.CommunityUniversalis,"),
                ("dutyLoot.Enabled=()=>CommunitySources.Allowed(configuration,SourceUse.GarlandDutyLoot);","\"TenshoAssistant.Duties.DutyLootService\"=>()=>Allowed(c,SourceUse.GarlandDutyLoot),"),
                ("monsterLoot.Enabled=()=>CommunitySources.Allowed(configuration,SourceUse.GarlandMonsterLoot);","\"TenshoAssistant.Monsters.MonsterLootService\"=>()=>Allowed(c,SourceUse.GarlandMonsterLoot),"),
                ("fashionData.Enabled=()=>configuration.CommunityFashion;","\"TenshoAssistant.Fashion.FashionDataService\"=>()=>c.CommunityFashion,")})
                Check(plugin.Contains(line,StringComparison.Ordinal) && source.Contains(row,StringComparison.Ordinal),"the game and the table alike: "+line);
            var host=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant.Desktop/Host.cs"));
            Check(host.Contains("WireSources();",StringComparison.Ordinal) && host.Contains("GetMethod(\"Wire\",BindingFlags.Public|BindingFlags.Static)",StringComparison.Ordinal)
                && host.Contains("property.SetValue(service,(Func<bool>)(()=>false));",StringComparison.Ordinal),"the application: the table, and off for a service it does not know");
        }),
    ];
}
