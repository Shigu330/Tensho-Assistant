// 0.883 (X-77.1): exchange through GitHub: device flow, token protected by Windows, own sightings only, stable payload, opt-in automatic exchange.
using TenshoAssistant.Discoveries;
internal static class GitHubExchange883Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("GitHub exchange: payload round-trips and is stable; automatic exchange off by default and due once a day; token never in clear",() =>
        {
            var at=new DateTimeOffset(2026,11,8,12,0,0,TimeSpan.Zero);
            var store=new DiscoveryStore();
            store.SeeSource(DiscoveryKind.Venture,5,7,2,at);
            var first=CommunityDiscoveries.Mine(store.Snapshot(at));var second=CommunityDiscoveries.Mine(store.Snapshot(at.AddHours(1)));
            // X-574 (0.1793): the payload is the v3 set the exchange sends (ExchangeV3, since 0.1137); the v1/v2 Encode and Decode are gone.
            var payload=ExchangeV3.GistFiles(ExchangeV3.Build(first));
            Check(CommunityDiscoveries.Hash(payload[ExchangeV3.IndexName])==CommunityDiscoveries.Hash(ExchangeV3.GistFiles(ExchangeV3.Build(second))[ExchangeV3.IndexName]),"same facts, same payload");
            var index=ExchangeV3.ParseIndex(payload[ExchangeV3.IndexName])!;
            Check(index.Files.Sum(entry=>ExchangeV3.ReadKind(new MemoryStream(Convert.FromBase64String(payload[entry.Name+ExchangeV3.GistSuffix])),entry).Facts.Count)==1,"round trip");
            Check(!new DiscoverySettings().AutoExchange,"off by default");
            var folder=Path.Combine(Path.GetTempPath(),"tensho-exchange-"+Guid.NewGuid().ToString("N"));
            try
            {
                using var exchange=new GitHubExchange(folder);
                Check(!exchange.Connected && exchange.Due(at),"not connected, due");
            }
            finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
            var code=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/GitHubExchange.cs"));
            Check(code.Contains("AtomicFile.Write(TokenPath,UserSecret.Protect(") && code.Contains("AtomicFile.Write(RefreshPath,UserSecret.Protect(") && !code.Contains("client_secret"),"token and refresh token protected, no secret");
            Check(code.Contains("if(upload && Connected)") && code.Contains("CommunityDiscoveries.Mine("),"own sightings, connected only");
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Discoveries.cs"));
            Check(page.Contains("if(settings.Enabled && settings.AutoExchange && DiscoveryExchange is { } exchange && exchange.Due(DateTimeOffset.UtcNow))"),"automatic only when turned on");
        }),
    ];
}
