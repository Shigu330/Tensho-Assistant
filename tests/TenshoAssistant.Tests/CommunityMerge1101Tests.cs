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
            // 0.1801 (X-772): the player file of ticket #43 read in its new format (43.jsonl.gz, without place nor day) or the one of before.
            // X-830 (D-226 a): the player files are anonymous (a random id, no issue in the header): ticket #43's facts are in one of them.
            var players = RepositoryCommon.Players();
            Check(players.Count >= 1 && players.All(file => System.Text.RegularExpressions.Regex.IsMatch(file.File, @"^[0-9a-f]{16}\.jsonl\.gz$") && file.Header is { } header && !header.TryGetProperty("Issue", out _))
                  && players.Max(file => file.Facts.Count) >= 1967,
                  "an anonymous player file holds at least ticket #43's 1967 facts of 0.1101 (2421 since the merge of 12/11)");
        }),
    ];
}
