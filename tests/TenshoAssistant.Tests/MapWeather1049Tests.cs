using System.Numerics;
using TenshoAssistant.Gathering;
using TenshoAssistant.Maps;

// 0.1049 (X-114): the current weather of each zone on Tensho's map, only while the mouse is over it.
internal static class MapWeather1049Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly WeatherDefinition Fair=new(2,"Fair Skies",60202);
    private static readonly WeatherDefinition Rain=new(7,"Rain",60207);
    private static WeatherDefinition? WeatherOf(uint territory)=>territory switch{132=>Fair,133=>Rain,148=>new(1,"No icon",0),_=>null};
    private static uint TerritoryOf(uint map)=>map switch{2=>132,3=>133,4=>132,5=>148,6=>999,_=>0};

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map weather 1049: a zone map shows its own weather once, a zone without a known weather none",() =>
        {
            var own=MapWeatherIcons.Build(132,[(new Vector2(5,5),3)],TerritoryOf,WeatherOf);
            Check(own.Count==1 && own[0] is { TerritoryId:132,Icon:60202,Name:"Fair Skies",At:null },"zone map: one icon, its own zone, in the corner");
            Check(MapWeatherIcons.Build(999,[],TerritoryOf,WeatherOf).Count==0,"individual or unknown weather: no icon");
            Check(MapWeatherIcons.Build(148,[],TerritoryOf,WeatherOf).Count==0,"weather without icon: no icon");
        }),
        ("Map weather 1049: a region map shows one icon per linked zone with a known weather, at the zone's place",() =>
        {
            var marks=MapWeatherIcons.Build(0,[(new Vector2(10,20),2),(new Vector2(30,40),3),(new Vector2(50,60),4),(new Vector2(1,1),0),(new Vector2(2,2),6),(new Vector2(3,3),7),(new Vector2(4,4),5)],TerritoryOf,WeatherOf);
            Check(marks.Count==2,$"two zones with a weather, got {marks.Count}");
            Check(marks[0] is { TerritoryId:132,At:{ X:10,Y:20 } } && marks[1] is { TerritoryId:133,Name:"Rain",At:{ X:30,Y:40 } },"the zones' places kept, once per zone");
        }),
        ("Map weather 1049: the list is built once per map and weather period, never kept while the catalogues are missing",() =>
        {
            var cache=new MapWeatherIcons();var builds=0;
            IReadOnlyList<MapWeatherMark> Build(){builds++;return [new(132,60202,"Fair Skies",null)];}
            Check(cache.For(1,2800,false,Build).Count==0 && builds==0,"not ready: nothing built");
            cache.For(1,2800,true,Build);cache.For(1,4199,true,Build);
            Check(builds==1,"same map and period: built once");
            cache.For(1,4200,true,Build);Check(builds==2,"next period: built again");
            cache.For(2,4200,true,Build);Check(builds==3,"other map: built again");
            cache.For(2,4200,false,Build);cache.For(2,4200,true,Build);Check(builds==4,"after a missing catalogue: built again");
        }),
        ("Map weather 1049: drawn on Tensho's map only while hovered, with the zone weather tooltip",() =>
        {
            var code=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs")).Replace("\r\n","\n");
            var start=code.IndexOf("// X-114 (0.1049",StringComparison.Ordinal);
            Check(start>0,"weather block present");
            var block=code.Substring(start,Math.Min(2400,code.Length-start));
            Check(block.Contains("if(hovered)",StringComparison.Ordinal),"only while the mouse is over the map");
            Check(block.Contains("mapWeather.For(",StringComparison.Ordinal) && block.Contains("MapWeatherIcons.Build(",StringComparison.Ordinal),"built through the cache");
            Check(block.Contains(".Forecast(zone,weatherTime,1)",StringComparison.Ordinal),"weather from the game's forecast");
            Check(block.Contains("\"MapView.VistaWeatherNow\"",StringComparison.Ordinal),"tooltip with the zone weather text");
        }),
    ];
}
