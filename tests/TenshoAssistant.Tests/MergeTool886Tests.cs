// 0.886 (X-77): the common file is rebuilt from one file per player, so an updated gist is never counted twice.
using TenshoAssistant.Discoveries;
using System.Text.Json;
internal static class MergeTool886Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Merge tool: one file per player; each common fact counted by the players who have it",() =>
        {
            var players=Directory.GetFiles(Path.GetFullPath("community-discoveries/players"),"*.json");
            Check(players.Length>0,"player files");
            var counts=new Dictionary<string,int>();
            foreach(var path in players)
                foreach(var fact in JsonDocument.Parse(File.ReadAllText(path)).RootElement.GetProperty("Facts").EnumerateArray())
                {
                    var key=string.Join(":",new[]{"Kind","Subject","Detail","Territory","Map","CellX","CellZ"}.Select(name=>fact.GetProperty(name).GetRawText()));
                    counts[key]=counts.GetValueOrDefault(key)+1;
                }
            var common=CommunityDiscoveries.Parse(File.ReadAllText(Path.GetFullPath("community-discoveries/discoveries.json")))!;
            Check(common.Facts.Count==counts.Count && common.Facts.All(fact=>counts[fact.Key.Replace(((int)fact.Kind).ToString()+":",((int)fact.Kind).ToString()+":")]==fact.Imported),"rebuilt");
            var tool=File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py"));
            Check(tool.Contains("def sync(dry)") && tool.Contains("FLAG: refusals over 5 %"),"sync with its check");
        }),
    ];
}
