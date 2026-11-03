using TenshoAssistant.Monsters;

// 0.395 (user request): every blue magic spell sorted by the level of its teaching monster, from the wiki's spellbook table (one page
// for every spell, read once on opening, copy first, wiki switch honoured); unknown levels last.
internal static class BlueSpellIndex395Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Blue spellbook: table parsed by number, monster level = lower of world and duty, whole list sorted, read once with a copy",() =>
        {
            var html = "<table class=\\\"sortable\\\"><tr><th>Spell</th><th>No.</th><th>Rank</th><th>Type</th><th>Min Lv</th><th>World Mob L</th><th>Duty L</th><th>Common Acquisition Methods</th></tr>"
                + "<tr><td>Water Cannon</td><td>1</td><td>x</td><td>Default</td><td>1</td><td>~</td><td>~</td><td>Default</td></tr>"
                + "<tr><td>Drill Cannons</td><td>5</td><td>x</td><td>Open World Dungeon</td><td>1</td><td>46</td><td>50</td><td>Magitek</td></tr>"
                + "<tr><td>Flame Thrower</td><td>2</td><td>x</td><td>Dungeon</td><td>50</td><td>~</td><td>50</td><td>Gobmachine</td></tr></table>";
            var json = "{\"parse\":{\"title\":\"Blue Magic Spellbook\",\"revid\":1548695,\"text\":{\"*\":\"" + html + "\"}}}";
            var book = BlueSpellbookParser.Parse(json,DateTimeOffset.UtcNow);
            Check(book.Rows.Count==3 && book.Rows[5].MonsterLevel==46 && book.Rows[2].MonsterLevel==50 && book.Rows[1].MonsterLevel is null && book.Revision==1548695,"parsed");
            int[] numbers=[1,2,5];
            Check(BlueSpellSources.SortByMonsterLevel(numbers,number=>book.Rows[number].MonsterLevel,1).SequenceEqual([5,2,1]),"lowest first, unknown last");
            Check(BlueSpellSources.SortByMonsterLevel(numbers,number=>book.Rows[number].MonsterLevel,2).SequenceEqual([2,5,1]),"highest first");
            var index = Read("Monsters/BlueSpellWikiIndex.cs");
            Check(index.Contains("disk.ReadAsync<BlueSpellbook>(Family,\"all\",Version,token)",StringComparison.Ordinal) && index.Contains("if(!enabled())",StringComparison.Ordinal),"copy first, wiki switch");
        }),
    ];
}
