// 0.1842 (X-840 M1, review of 0.1831 to 0.1832): EorzeaHour1831Tests said that the v3 lines put in the gist hold no minute but only checked
// that files were built. Here the lines really built are read back (gzip, JSON lines): the times of a vista and of a fish's weathers are whole
// Eorzea hours, and another kind keeps its values (so the reading sees a minute when there is one).
using System.IO.Compression;
using System.Text.Json;
using TenshoAssistant.Discoveries;
internal static class ExchangeHour1842Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}

    private static List<JsonElement> Lines(byte[] gz)
    {
        using var input=new GZipStream(new MemoryStream(gz),CompressionMode.Decompress);
        using var reader=new StreamReader(input);
        var lines=new List<JsonElement>();
        string? line;
        while((line=reader.ReadLine()) is not null)
            if(line.Length>0)lines.Add(JsonDocument.Parse(line).RootElement.Clone());
        return lines;
    }

    private static IEnumerable<int> Times(JsonElement fact)
    {
        foreach(var name in new[]{"LevelMin","LevelMax"})
            if(fact.TryGetProperty(name,out var value))yield return value.GetInt32();
        if(fact.TryGetProperty("Values",out var values) && values.ValueKind==JsonValueKind.Object)
            foreach(var entry in values.EnumerateObject())yield return int.Parse(entry.Name,System.Globalization.CultureInfo.InvariantCulture);
    }

    public static IEnumerable<(string Name,Action Run)> Cases=>
    [
        ("X-840 M1: the v3 lines that go into the gist hold the Eorzea hour only for a vista and a fish's weathers",()=>
        {
            var day=new DateTimeOffset(2026,11,20,0,0,0,TimeSpan.Zero);
            var store=new DiscoveryStore();
            foreach(var minute in new[]{1023,1050,1079})
            {
                var at=day.AddMinutes(minute);
                store.SeeSource(DiscoveryKind.FishCondition,5001,2,minute,at,132);
                store.SeeSource(DiscoveryKind.FishTransition,5001,3,minute,at,132);
                store.SeeSource(DiscoveryKind.VistaSeen,7,2,minute,at);
                store.SeeSource(DiscoveryKind.Venture,9,1,minute,at);
            }
            var set=ExchangeV3.Build(CommunityDiscoveries.Mine(store.Snapshot(day.AddDays(1))));
            foreach(var kind in new[]{DiscoveryKind.FishCondition,DiscoveryKind.FishTransition,DiscoveryKind.VistaSeen})
            {
                Check(set.Files.TryGetValue(ExchangeV3.FileName(kind),out var bytes),$"{kind}: its file built");
                var facts=Lines(bytes!).Skip(1).ToList();   // the first line is the file's header
                Check(facts.Count==1,$"{kind}: one fact line ({facts.Count})");
                var times=Times(facts[0]).ToList();
                Check(times.Count>0 && times.All(time=>time==1020),$"{kind}: only the hour 17:00 ET (1020) leaves: {string.Join(",",times)}");
            }
            Check(set.Files.TryGetValue(ExchangeV3.FileName(DiscoveryKind.Venture),out var venture),"venture file built");
            var ventureTimes=Times(Lines(venture!).Skip(1).Single()).ToList();
            Check(ventureTimes.Contains(1023) && ventureTimes.Contains(1079),$"another kind keeps its values, read the same way: {string.Join(",",ventureTimes)}");
        }),
    ];
}
