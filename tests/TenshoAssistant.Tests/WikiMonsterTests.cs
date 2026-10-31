using System.Net;
using System.Text.Json;
using TenshoAssistant.Community;
using TenshoAssistant.Monsters;

internal static class WikiMonsterTests
{
    private sealed class Handler(Func<HttpRequestMessage,HttpResponseMessage> respond):HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {Calls++;return Task.FromResult(respond(request));}
    }
    private static string Response(string title="Rroneek Chuck")=>JsonSerializer.Serialize(new{parse=new{title,revid=1463649,text=new Dictionary<string,string>{{"*","<table class=\"item sortable stdt__table_v2\"><tbody><tr><th>Enemy</th><th>Level</th><th>Location\n</th></tr><tr><td><a>Rroneek</a></td><td>96</td><td><a>Shaaloani</a> <small>(21.8,10.2)</small></td></tr></tbody></table>"}}}});
    private static void Check(bool value){if(!value)throw new Exception("Wiki monster assertion failed.");}
    public static void Parse()
    {
        var result=WikiMonsterParser.Parse(Response(),44106,"Rroneek Chuck",DateTimeOffset.UtcNow);
        Check(result.ItemId==44106 && result.Monsters.Count==1 && result.Monsters[0].Name=="Rroneek" && result.Monsters[0].Level=="96" && result.Monsters[0].Location.Contains("21.8,10.2") && result.Revision==1463649);
        try{WikiMonsterParser.Parse(Response("Wrong page"),44106,"Rroneek Chuck",DateTimeOffset.UtcNow);throw new Exception("Mismatch accepted");}catch(InvalidDataException){}
        var unrelated=JsonSerializer.Serialize(new{parse=new{title="Rroneek Chuck",revid=1,text=new Dictionary<string,string>{{"*","<table class=\"npc\"><tr><td>Vendor</td></tr></table>"}}}});
        Check(WikiMonsterParser.Parse(unrelated,44106,"Rroneek Chuck",DateTimeOffset.UtcNow).Monsters.Count==0);
    }
    public static void Cache()
    {
        string? requested=null;
        var wikiHandler=new Handler(request=>{requested=request.RequestUri!.Query;return new(HttpStatusCode.OK){Content=new StringContent(Response())};});
        using var wikiHttp=new HttpClient(wikiHandler);
        // The title comes from the game's English item name; no Garland request exists any more.
        using var service=new WikiMonsterSources(id=>id==44106 ? "Rroneek Chuck":null,()=>TimeSpan.FromDays(1),http:wikiHttp);
        service.Select(44106);
        Check(wikiHandler.Calls==0);
        service.RequestAsync().GetAwaiter().GetResult();
        Check(service.State.Document?.Monsters.Count==1 && service.State.Error is null && requested!.Contains("page=Rroneek_Chuck") && requested.Contains("redirects=1"));
        service.RequestAsync().GetAwaiter().GetResult();
        Check(wikiHandler.Calls==1);
        service.Select(1);
        Check(service.State.Document is null);
        service.Select(44106);
        Check(service.State.Document?.ItemId==44106);
        service.Select(2);service.RequestAsync().GetAwaiter().GetResult();
        Check(service.State.Error is not null && wikiHandler.Calls==1);
        service.Dispose();
        service.RequestAsync().GetAwaiter().GetResult();
        Check(service.State.Document is null && wikiHandler.Calls==1);
    }
    public static void Redirects()
    {
        var redirected=JsonSerializer.Serialize(new{parse=new{title="Rroneek Chuck",revid=7,redirects=new[]{new{from="Rroneek chuck",to="Rroneek Chuck"}},
            text=new Dictionary<string,string>{{"*","<table class=\"item\"><tr><th>Enemy</th><th>Level</th><th>Location</th></tr><tr><td>Rroneek</td><td>96</td><td>Shaaloani</td></tr></table>"}}}});
        var followed=WikiMonsterParser.Parse(redirected,44106,"Rroneek chuck",DateTimeOffset.UtcNow);
        Check(followed.Title=="Rroneek Chuck" && followed.Monsters.Count==1 && followed.Url.EndsWith("Rroneek_Chuck",StringComparison.Ordinal));
        try{WikiMonsterParser.Parse(redirected,44106,"Other item",DateTimeOffset.UtcNow);throw new Exception("Foreign redirect accepted");}catch(InvalidDataException){}
        var missing=WikiMonsterParser.Parse("{\"error\":{\"code\":\"missingtitle\",\"info\":\"The page you specified doesn't exist.\"}}",42,"Nothing Here",DateTimeOffset.UtcNow);
        Check(missing.Monsters.Count==0 && missing.ItemId==42 && missing.Revision==0);
        var drop=new TenshoAssistant.Items.ItemDefinition(44106,"Épaule",false,UiCategoryId:45,EnglishName:"Rroneek Chuck");
        Check(MonsterDropCandidates.Eligible(drop,false,false) && !MonsterDropCandidates.Eligible(drop,true,false) && !MonsterDropCandidates.Eligible(drop,false,true)
            && !MonsterDropCandidates.Eligible(drop with{UiCategoryId=46},false,false) && !MonsterDropCandidates.Eligible(drop with{EnglishName=""},false,false)
            && !MonsterDropCandidates.Eligible(drop with{Equipment=TenshoAssistant.Items.EquipmentSlots.Head},false,false) && !MonsterDropCandidates.Eligible(null,false,false));
    }
}
