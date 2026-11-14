// 0.1101 (R12): the discoveries of ticket #43 are in the common file; a fact carries only the known fields (never player data).
using System.Text.Json;

internal static class CommunityMerge1101Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static readonly HashSet<string> Fields =
        ["Kind","Subject","Detail","Territory","Map","CellX","CellZ","X","Z","LevelMin","LevelMax","Seen","Imported","First","Last","Values","Hq"];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Community discoveries: ticket #43 merged, known fields and kinds only",() =>
        {
            // X-357 (0.1349): the lines of the v3 set as published (the v2 discoveries.json is gone).
            var lines = RepositoryCommon.FactLines();
            Check(lines.Count >= 1967,"the common set holds the facts of ticket #43 (" + lines.Count + ")");
            foreach (var (_, line) in lines)
            {
                using var document = JsonDocument.Parse(line);
                var fact = document.RootElement;
                foreach (var member in fact.EnumerateObject())
                    Check(Fields.Contains(member.Name),"unknown field in a fact: " + member.Name);
                var kind = fact.GetProperty("Kind").GetInt32();
                Check(kind is >= 1 and <= 38 and not 26 and not 27,"known kind (" + kind + ")");
            }
            using var player = JsonDocument.Parse(File.ReadAllText(Path.GetFullPath("community-discoveries/players/43.json")));
            Check(player.RootElement.GetProperty("Issue").GetInt32() == 43 && player.RootElement.GetProperty("Facts").GetArrayLength() >= 1967,
                  "the player file of ticket #43 holds at least its 1967 facts of 0.1101 (2421 since the merge of 12/11)");
        }),
    ];
}
