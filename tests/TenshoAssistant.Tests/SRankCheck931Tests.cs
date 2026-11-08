using TenshoAssistant.Hunting;

// X-76 P5 (0.931): every S rank of Tensho's table is an S rank (NotoriousMonster.Rank 3) of its territory in the game data, from the probe output
// kept in docs/evidence/0.931-srank-check.md; a new mark must be checked the same way.
internal static class SRankCheck931Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("S rank check 931: table against the game's notorious monsters",() =>
        {
            var game=new HashSet<(uint Territory,uint Name)>();
            foreach(var line in File.ReadAllLines(Path.GetFullPath("docs/evidence/0.931-srank-check.md")))
            {
                var match=System.Text.RegularExpressions.Regex.Match(line,@"^NM territory (\d+) monster \d+ name (\d+) '.*' rank 3$");
                if(match.Success)game.Add((uint.Parse(match.Groups[1].Value),uint.Parse(match.Groups[2].Value)));
            }
            Check(game.Count>=60,"probe lines kept: "+game.Count);
            var wrong=SRankConditions.Marks.Where(mark=>mark.Territory==0 ? !game.Any(pair=>pair.Name==mark.Name):!game.Contains((mark.Territory,mark.Name))).ToArray();
            Check(wrong.Length==0,"not an S rank of its territory: "+string.Join(", ",wrong.Select(mark=>$"{mark.Name}@{mark.Territory}")));
            var missing=game.Select(pair=>pair.Name).Distinct().Where(name=>SRankConditions.Marks.All(mark=>mark.Name!=name)).ToArray();
            Check(missing.Length==0,"S rank of the game not in the table: "+string.Join(", ",missing));
        }),
    ];
}
