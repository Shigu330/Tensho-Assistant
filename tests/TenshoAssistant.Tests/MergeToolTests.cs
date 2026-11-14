// Test audit of 2026-11-09, batch 2 (0.1021): MergeTool884Tests, MergeTool886Tests merged into MergeToolTests; every case is kept.
using TenshoAssistant.Discoveries;
using System.Text.Json;
internal static class MergeToolTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From MergeTool884Tests: 0.884 (X-77.1): the merge tool reads gists given as gist:ID and ignores the plugin's computed Key property.
        ("Merge tool: gist prefix removed, computed Key accepted then dropped",() =>
        {
            var tool=File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py"));
            Check(tool.Contains("reference.removeprefix(\"gist:\")") && tool.Contains("f.pop(\"Key\",None)"),"fixed");
        }),
        // From MergeTool886Tests: 0.886 (X-77): the common file is rebuilt from one file per player, so an updated gist is never counted twice.
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
            // X-357 (0.1349): the common facts read from the v3 set (the v2 discoveries.json is gone).
            var common=new DiscoveryFile{Facts=RepositoryCommon.Facts()};
            Check(common.Facts.Count==counts.Count && common.Facts.All(fact=>counts[fact.Key.Replace(((int)fact.Kind).ToString()+":",((int)fact.Kind).ToString()+":")]==fact.Imported),"rebuilt");
            var tool=File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py"));
            Check(tool.Contains("def sync(dry)") && tool.Contains("FLAG: refusals over 5 %"),"sync with its check");
        }),
    ];
}
