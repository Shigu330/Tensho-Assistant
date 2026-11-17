using System.Text.Json;
using TenshoAssistant.Chat;

// 0.1518 (X-484, the user's request of 17/11: the combat filters by kind of line were missing): the game's combat filters (LogFilter, 176 rows:
// 16 kinds of line × 11 subjects, probed on 17/11, docs/evidence/0.1518-chat-filter-genres.md) grouped by kind of line across every subject.
internal static class ChatGenres1518Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    // Rows of the evidence (id, kind, caster, target, category, order, preset, name).
    private static readonly ChatFilterRow ByYou = new(51, 41, 2, 65535, 4, 1, 2, "Damage dealt by you.");
    private static readonly ChatFilterRow MissedByYou = new(52, 42, 2, 65535, 4, 2, 2, "Failed attacks by you.");
    private static readonly ChatFilterRow OnYou = new(58, 41, 65535, 2, 4, 8, 2, "Damage you are dealt.");
    private static readonly ChatFilterRow EndOnYou = new(65, 48, 65535, 2, 4, 15, 2, "Beneficial effects on you ending.");
    private static readonly ChatFilterRow ByParty = new(71, 41, 4, 65535, 5, 1, 2, "Damage dealt by party members.");
    private static readonly ChatFilterRow OnParty = new(78, 41, 65535, 4, 5, 8, 2, "Damage party members are dealt.");
    private static readonly ChatFilterRow ByOthersPets = new(251, 41, 2048, 65535, 14, 1, 0, "Damage dealt by others' pets and companions.");
    private static readonly ChatFilterRow Say = new(1, 10, 65535, 65535, 1, 1, 1, "Say");
    private static readonly ChatFilterRow OwnBattle = new(37, 58, 1807, 65535, 2, 2, 2, "Own Battle System Messages");

    private static Dictionary<string, string> Flat(string code)
    {
        var values = new Dictionary<string, string>();
        void Walk(JsonElement node, string prefix)
        {
            foreach (var property in node.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Object) Walk(property.Value, prefix + property.Name + ".");
                else if (property.Value.ValueKind == JsonValueKind.String) values[prefix + property.Name] = property.Value.GetString()!;
            }
        }
        using var document = JsonDocument.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{code}.json")));
        Walk(document.RootElement, "");
        return values;
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat genres 1518: a combat filter's kind of line is its kind and its side; the other filters have none", () =>
        {
            Check(ChatFilterTable.Genre(ByYou) == (41, false) && ChatFilterTable.Genre(OnYou) == (41, true) && ChatFilterTable.Genre(EndOnYou) == (48, true),
                "kind and side of the combat rows");
            Check(ChatFilterTable.Genre(Say) is null && ChatFilterTable.Genre(OwnBattle) is null, "Discussion and Announcements have no kind of line");
            Check(ChatFilterTable.GenreKey((41, false)) == "ChatWindow.Genre41By" && ChatFilterTable.GenreKey((49, true)) == "ChatWindow.Genre49On", "keys");
        }),
        ("Chat genres 1518: the kinds of line group every subject, the side done by the subject first, in the game's order", () =>
        {
            var table = new ChatFilterTable([OnParty, Say, ByOthersPets, EndOnYou, MissedByYou, ByParty, OnYou, ByYou, OwnBattle]);
            var genres = table.Genres();
            Check(genres.Select(g => g.Genre).SequenceEqual(new[] { (41, false), (42, false), (41, true), (48, true) }),
                "order: " + string.Join(", ", genres.Select(g => g.Genre)));
            Check(genres[0].Rows.Select(row => row.Id).SequenceEqual(new uint[] { 51, 71, 251 }), "damage dealt: by you, by party members, by others' pets");
            Check(genres[2].Rows.Select(row => row.Id).SequenceEqual(new uint[] { 58, 78 }), "damage taken: you, party members");
            Check(genres.Sum(g => g.Rows.Count) == 7, "only the combat rows");
        }),
        ("Chat genres 1518: the sixteen kinds of line of the game are named in en and fr, each its own name", () =>
        {
            var kinds = new[] { 41, 42, 43, 44, 45, 46, 47 }.Select(kind => (kind, false))
                .Concat(new[] { 41, 42, 43, 44, 45, 46, 47, 48, 49 }.Select(kind => (kind, true))).ToList();
            foreach (var code in new[] { "en", "fr" })
            {
                var values = Flat(code);
                var names = kinds.Select(genre => values.GetValueOrDefault(ChatFilterTable.GenreKey(genre)) ?? "").ToList();
                Check(names.All(name => name.Length > 0) && names.Distinct().Count() == 16, $"{code}: {string.Join(" | ", names)}");
                Check(values["ChatWindow.GenreTip"].Contains("{0}") && values["ChatWindow.GenreTip"].Contains("{1}"), $"{code}: GenreTip counts");
                Check(values.ContainsKey("ChatWindow.FilterBySubject") && values.ContainsKey("ChatWindow.FilterByGenre"), $"{code}: the two ways in");
            }
        }),
        ("Chat genres 1518: the chat settings show the combat filters by subject and by kind of line", () =>
        {
            var code = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(code.Contains("###ChatFilterBySubject") && code.Contains("###ChatFilterByGenre") && code.Contains("DrawChatFilterGenres(tab,filters,table);"),
                "the two trees");
            Check(code.Contains("filters.RemoveAll(chosen=>rows.Any(row=>row.Id==chosen));") && code.Contains("if(all)filters.AddRange(rows.Select(row=>row.Id));"),
                "a kind of line turns all its subjects on or off");
        }),
    ];
}
