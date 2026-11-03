using TenshoAssistant.Monsters;

// 0.390 (user request): the blue magic spell's monsters sorted by level — a range counts by its first number, no level stays last.
internal static class BlueMonsterOrder390Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Blue spell monsters: level order, ranges by their first number, rows without a level last, wiki order kept otherwise",() =>
        {
            IReadOnlyList<WikiMonsterRow> rows = [new("Ark Morbol","60","The Void Ark"),new("Stroper","","[[]]"),new("Morbol","43-48","[[]]"),
                new("Capricious Cassie","43","East Shroud"),new("Polluting Morbol","90","Urqopacha")];
            Check(BlueSpellSources.Level("43-48")==43 && BlueSpellSources.Level("")==null && BlueSpellSources.Level("?")==null,"levels");
            Check(BlueSpellSources.SortByLevel(rows,1).Select(row=>row.Name).SequenceEqual(["Morbol","Capricious Cassie","Ark Morbol","Polluting Morbol","Stroper"]),"lowest first, ties in wiki order");
            Check(BlueSpellSources.SortByLevel(rows,2).Select(row=>row.Name).SequenceEqual(["Polluting Morbol","Ark Morbol","Morbol","Capricious Cassie","Stroper"]),"highest first");
            Check(ReferenceEquals(BlueSpellSources.SortByLevel(rows,0),rows),"wiki order");
        }),
    ];
}
