using TenshoAssistant.Gathering;

internal static class WeatherNamesTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Weather names use exact explicit English metadata without localized or fuzzy fallback",()=>
        {
            var catalog=new WeatherCatalog([new(1,"Pluie",602,"Rain"),new(2,"Rain",603)],[new(10,1,[new(1,100)]),new(20,2,[new(2,100)])],"test");
            Check(catalog.WeatherBySourceName("Rain",10)?.Id==1,"English row not localized label");
            foreach(var name in new[]{"rain"," Rain","Pluie","Rain ","Showers",""})
                Check(catalog.WeatherBySourceName(name,10) is null,"unmatched remains unknown");
            Check(catalog.WeatherBySourceName("Rain",20) is null,"unknown English metadata never falls back to local name");
            Check(catalog.Weather(2)?.EnglishName is null,"old three-field definitions remain usable with unknown English metadata");
        }),
        ("Duplicate native English weather names resolve only within unambiguous territory rates",()=>
        {
            var catalog=new WeatherCatalog([new(1,"Dégagé",1,"Clear Skies"),new(91,"Dégagé",2,"Clear Skies")],
                [new(10,1,[new(1,100)]),new(20,2,[new(91,100)]),new(30,3,[new(1,50),new(91,50)])],"test");
            Check(catalog.WeatherBySourceName("Clear Skies",10)?.Id==1 && catalog.WeatherBySourceName("Clear Skies",20)?.Id==91,"zone identity disambiguates");
            Check(catalog.WeatherBySourceName("Clear Skies",30) is null && catalog.WeatherBySourceName("Clear Skies",99) is null,"ambiguous and missing zone remain unknown");
        }),
        ("Weather alias scopes exclude zero mass unknown identities and invalid native rate tables",()=>
        {
            var catalog=new WeatherCatalog([new(1,"Pluie",1,"Rain"),new(2,"Pluie",2,"Rain"),new(3,"Inconnue",3)],
                [new(10,1,[new(1,50),new(1,20),new(2,0)]),new(20,0,[new(1,100)]),new(30,3,[new(1,101)]),
                 new(40,4,[new(1,50),new(2,-1)]),new(50,5,[new(99,100)]),new(60,6,[]),new(70,7,[new(1,50),new(3,50)]),new(80,8,[new(1,50),new(99,50)])],"test");
            Check(catalog.WeatherBySourceName("Rain",10)?.Id==1,"positive distinct weather identities, partial table okay for identity only");
            foreach(var territory in new uint[]{20,30,40,50,60,70,80})Check(catalog.WeatherBySourceName("Rain",territory) is null,"invalid or incomplete scope cannot prove name uniqueness");
        }),
        ("Weather aliases and scoped identities detach from mutable producer collections",()=>
        {
            var definitions=new List<WeatherDefinition>{new(1,"Pluie",1,"Rain")};
            var shares=new List<WeatherShare>{new(1,100)};
            var zones=new List<ZoneWeather>{new(10,1,shares)};
            var catalog=new WeatherCatalog(definitions,zones,"version");definitions.Clear();shares.Clear();zones.Clear();
            Check(catalog.WeatherBySourceName("Rain",10)?.Id==1 && catalog.Forecast(10,DateTimeOffset.UnixEpoch,1)[0].Weather?.Id==1,"immutable alias and forecast views share owned definitions");
        }),
    };
}
