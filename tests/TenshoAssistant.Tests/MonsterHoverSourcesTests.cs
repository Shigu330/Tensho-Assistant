using System.Net;
using System.Text.Json;
using TenshoAssistant.Caching;
using TenshoAssistant.Monsters;

internal static class MonsterHoverSourcesTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private sealed class Handler(Func<HttpStatusCode> status):HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {Interlocked.Increment(ref Calls);return Task.FromResult(new HttpResponseMessage(status()){Content=new StringContent(Page())});}
    }
    private static string Page()=>JsonSerializer.Serialize(new{parse=new{title="Rroneek Chuck",revid=1,text=new Dictionary<string,string>{{"*",
        "<table class=\"item\"><tr><th>Enemy</th><th>Level</th><th>Location</th></tr><tr><td>Rroneek</td><td>96</td><td>Shaaloani (21.8,10.2)</td></tr></table>"}}}});
    private static WikiMonsterDocument Document(uint id,DateTimeOffset at)=>new(id,"Rroneek Chuck",WikiMonsterParser.Url("Rroneek Chuck"),1,at,[new("Rroneek","96","Shaaloani")]);
    private sealed class Fixture:IDisposable
    {
        public readonly string Root=Path.Combine(Path.GetTempPath(),"Tensho-MonsterHoverTests-"+Guid.NewGuid().ToString("N"));
        public readonly DataCache Cache;public readonly Handler Http;public readonly WikiMonsterSources Wiki;public readonly MonsterHoverSources Hover;
        public Fixture(HttpStatusCode status=HttpStatusCode.OK,TimeSpan? lifetime=null)
        {
            Cache=new(Root);Http=new(()=>status);var ttl=lifetime ?? TimeSpan.FromDays(1);
            Wiki=new(_=>"Rroneek Chuck",()=>ttl,Cache,new HttpClient(Http));Hover=new(Wiki);
        }
        public void Dispose(){Hover.Dispose();Wiki.Dispose();Cache.FlushAsync().Wait(5000);Cache.Dispose();if(Directory.Exists(Root))Directory.Delete(Root,true);}
    }
    // Observes until the status leaves the transient ones, with a bounded wait.
    private static MonsterHoverView Settle(MonsterHoverSources hover,uint id,TimeSpan hovered,bool network=true)
    {
        var deadline=DateTime.UtcNow.AddSeconds(10);MonsterHoverView view;
        do{view=hover.Observe(id,hovered,network);if(view.Status is not (MonsterHoverStatus.Reading or MonsterHoverStatus.Loading))return view;Thread.Sleep(10);}
        while(DateTime.UtcNow<deadline);
        throw new Exception("hover lookup did not settle");
    }
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Hover monsters come from the wiki disk copy at once without any network request",()=>
        {
            using var fixture=new Fixture();
            fixture.Cache.WriteAsync("wiki-monsters","42","wiki-parse-v1",Document(42,DateTimeOffset.UtcNow),DateTimeOffset.UtcNow).Wait(5000);
            Check(fixture.Hover.Observe(42,TimeSpan.Zero,true).Status==MonsterHoverStatus.Reading,"disk read starts first");
            var view=Settle(fixture.Hover,42,TimeSpan.Zero);
            Check(view.Status==MonsterHoverStatus.Ready && !view.Stale && view.Document!.Monsters.Single().Name=="Rroneek" && fixture.Http.Calls==0,"cached rows shown, no HTTP");
        }),
        ("Without a copy the wiki request waits for a stable hover then caches the answer",()=>
        {
            using var fixture=new Fixture();
            var early=Settle(fixture.Hover,43,TimeSpan.FromMilliseconds(500));
            Check(early.Status==MonsterHoverStatus.Waiting && early.Document is null && fixture.Http.Calls==0,"no request before 1.5 s");
            Check(fixture.Hover.Observe(43,TimeSpan.FromSeconds(2),true).Status==MonsterHoverStatus.Loading,"request after a stable hover");
            Check(fixture.Hover.Observe(43,TimeSpan.FromSeconds(2),true).Status==MonsterHoverStatus.Loading && fixture.Http.Calls<=1,"single request in flight");
            var view=Settle(fixture.Hover,43,TimeSpan.FromSeconds(2));
            Check(view.Status==MonsterHoverStatus.Ready && view.Document!.Monsters[0].Location.Contains("21.8") && fixture.Http.Calls==1,"answer published");
            fixture.Cache.FlushAsync().Wait(5000);
            using var reopened=new WikiMonsterSources(_=>null,()=>TimeSpan.FromDays(1),fixture.Cache);
            Check(reopened.ReadCachedAsync(43,default).Result?.Monsters.Count==1,"answer written to the shared wiki cache");
        }),
        ("Cache-only mode and failures never send repeated wiki requests",()=>
        {
            using(var cacheOnly=new Fixture())
            {
                var view=Settle(cacheOnly.Hover,44,TimeSpan.FromSeconds(5),network:false);
                Check(view.Status==MonsterHoverStatus.None && cacheOnly.Http.Calls==0,"no network when disabled");
            }
            using var failing=new Fixture(HttpStatusCode.InternalServerError);
            Settle(failing.Hover,45,TimeSpan.Zero);
            Check(failing.Hover.Observe(45,TimeSpan.FromSeconds(2),true).Status==MonsterHoverStatus.Loading,"first attempt");
            var failed=Settle(failing.Hover,45,TimeSpan.FromSeconds(2));
            Check(failed.Status==MonsterHoverStatus.Unavailable && failing.Http.Calls==1,"failure reported");
            Check(failing.Hover.Observe(45,TimeSpan.FromSeconds(10),true).Status==MonsterHoverStatus.Unavailable && failing.Http.Calls==1,"backoff prevents a second request");
        }),
        ("An expired wiki copy stays visible while a refresh is fetched after the stable hover",()=>
        {
            using var fixture=new Fixture(lifetime:TimeSpan.FromMinutes(1));
            var old=DateTimeOffset.UtcNow.AddHours(-2);
            fixture.Cache.WriteAsync("wiki-monsters","46","wiki-parse-v1",Document(46,old),old).Wait(5000);
            var stale=Settle(fixture.Hover,46,TimeSpan.Zero);
            Check(stale.Status==MonsterHoverStatus.Waiting && stale.Stale && stale.Document is not null && fixture.Http.Calls==0,"old copy shown first");
            fixture.Hover.Observe(46,TimeSpan.FromSeconds(2),true);
            var fresh=Settle(fixture.Hover,46,TimeSpan.FromSeconds(2));
            Check(fresh.Status==MonsterHoverStatus.Ready && !fresh.Stale && fixture.Http.Calls==1,"refreshed once");
        }),
    };
}
