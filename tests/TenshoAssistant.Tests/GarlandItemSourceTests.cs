using TenshoAssistant.Community;

internal static class GarlandItemSourceTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    // Shape of Garland Tools item/fr/3/16574.json (read 2026-10-31): sources are « partials » (string ids, obj.n names).
    private const string Alexander="{\"item\":{\"name\":\"Alexander articulé\",\"id\":16574,\"instances\":[30042,30046]},\"partials\":["
        +"{\"type\":\"instance\",\"id\":\"30042\",\"obj\":{\"i\":30042,\"n\":\"Alexander - L'Âme du Créateur\"}},"
        +"{\"type\":\"instance\",\"id\":\"30046\",\"obj\":{\"i\":30046,\"n\":\"Alexander - L'Âme du Créateur (sadique)\"}},"
        +"{\"type\":\"instance\",\"id\":\"30042\",\"obj\":{\"n\":\"duplicate\"}},"
        +"{\"type\":\"item\",\"id\":\"5\",\"obj\":{\"n\":\"Ingredient\"}},{\"type\":\"mob\",\"id\":7,\"obj\":{\"n\":\"Monstre\"}},{\"type\":\"quest\",\"id\":\"0\",\"obj\":{\"n\":\"Sans id\"}}]}";
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Garland item sources keep duties, monsters and other source partials once each",()=>
        {
            var list=GarlandItemSourceParser.Parse(Alexander,16574,DateTimeOffset.UnixEpoch);
            Check(list.Sources.Count==3,"two duties and one monster; item, duplicate and zero id dropped");
            Check(list.Sources[0]==new GarlandItemSource("instance",30042,"Alexander - L'Âme du Créateur") && list.Sources[2]==new GarlandItemSource("mob",7,"Monstre"),"string and number ids");
            Check(GarlandItemSourceParser.Parse("{\"item\":{\"id\":3}}",3,DateTimeOffset.UnixEpoch).Sources.Count==0,"no partials = no known source");
        }),
        ("Garland item sources read exchanges from tradeShops even without an NPC",()=>
        {
            // Shape of item/fr/3/40382.json (Cor buffle insulaire, read 2026-10-31).
            const string Buffalo="{\"item\":{\"id\":40382,\"tradeShops\":[{\"shop\":\"Shop\",\"npcs\":[],\"listings\":[{\"item\":[{\"id\":\"40382\",\"amount\":1}],\"currency\":[{\"id\":\"37549\",\"amount\":50000}]},"
                +"{\"item\":[{\"id\":\"9\",\"amount\":1}],\"currency\":[{\"id\":\"1\",\"amount\":5}]}]}]},\"partials\":[{\"type\":\"item\",\"id\":\"37549\",\"obj\":{\"n\":\"Assignat insulaire azur\"}}]}";
            var list=GarlandItemSourceParser.Parse(Buffalo,40382,DateTimeOffset.UnixEpoch);
            Check(list.Sources.Count==0 && list.Trades is [ { CurrencyId:37549,Amount:50000 } ],"only the listing selling this item");
        }),
        ("Garland item sources reject another item",()=>
        {
            var failed=false;
            try{GarlandItemSourceParser.Parse(Alexander,1,DateTimeOffset.UnixEpoch);}catch(InvalidDataException){failed=true;}
            Check(failed,"rejected");
        }),
    };
}
