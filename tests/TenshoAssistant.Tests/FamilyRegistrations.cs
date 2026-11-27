// 0.1906 (X-885 M2, review of 0.1903): what the family tests of X-153 (0.1893 to 0.1903) check in their family's own file instead of the case
// « behaviour » each one had, the same RegistrationScope test copied eleven times with its names changed, which touched nothing of the family.
// Each service's install and its release are given to one same call of the plugin's registrations: a release given to another registration
// (another service's, or none) would release this service with the wrong one, or never.
internal static class FamilyRegistrations
{
    /// <summary>The text of each « registrations.Register( … ) » call of <paramref name="text"/>, its parentheses balanced (strings and characters skipped).</summary>
    public static List<(int At, string Text)> Calls(string text)
    {
        const string head = "registrations.Register(";
        var calls = new List<(int At, string Text)>();
        for (var at = text.IndexOf(head, StringComparison.Ordinal); at >= 0; at = text.IndexOf(head, at + head.Length, StringComparison.Ordinal))
        {
            var depth = 0;
            var end = -1;
            for (var i = at + head.Length - 1; i < text.Length && end < 0; i++)
            {
                var c = text[i];
                // 0.1930 (X-153, the character's family): a line comment is skipped, so an apostrophe in it (« the retainers' job ») opens no character.
                if (c == '/' && i + 1 < text.Length && text[i + 1] == '/') { var line = text.IndexOf('\n', i); i = line < 0 ? text.Length : line; continue; }
                if (c is '"' or '\'')
                {
                    var verbatim = c == '"' && i > 0 && (text[i - 1] == '@' || (text[i - 1] == '$' && i > 1 && text[i - 2] == '@'));
                    for (i++; i < text.Length; i++)
                    {
                        if (!verbatim && text[i] == '\\') { i++; continue; }
                        if (text[i] == c) { if (verbatim && i + 1 < text.Length && text[i + 1] == '"') { i++; continue; } break; }
                    }
                    continue;
                }
                if (c == '(') depth++;
                else if (c == ')' && --depth == 0) end = i;
            }
            if (end < 0) throw new Exception("a registration not closed at " + at);
            calls.Add((at, text[at..(end + 1)]));
        }
        return calls;
    }

    /// <summary>Throws unless each install is in exactly one registration, which also holds its release and no other service's install. A service
    /// made just before its registration, on the same line, with an empty install (« registrations.Register(()=>{},service.Dispose) », the Duty
    /// Finder's loot) is accepted: that registration must hold its release.</summary>
    public static void Paired(string family, string home, IEnumerable<(string Service, string Install, string Release)> paths)
    {
        var calls = Calls(home);
        var all = paths.ToArray();
        foreach (var (service, install, release) in all)
        {
            var mine = calls.Where(call => System.Text.RegularExpressions.Regex.IsMatch(call.Text, install)).Select(call => call.Text).ToList();
            if (mine.Count == 0 && System.Text.RegularExpressions.Regex.Match(home, install) is { Success: true } made)
            {
                var next = calls.FirstOrDefault(call => call.At > made.Index);
                var sameLine = next.Text is not null && !home[made.Index..next.At].Contains('\n');
                var empty = next.Text is not null && System.Text.RegularExpressions.Regex.IsMatch(next.Text, @"^registrations\.Register\(\s*\(\)\s*=>\s*\{\s*\}\s*,");
                if (sameLine && empty) mine.Add(next.Text!);
            }
            if (mine.Count != 1) throw new Exception(family + ": " + service + "'s install in " + mine.Count + " registrations (one expected)");
            if (!System.Text.RegularExpressions.Regex.IsMatch(mine[0], release)) throw new Exception(family + ": " + service + "'s release not given with its install");
            var others = all.Where(other => other.Service != service && System.Text.RegularExpressions.Regex.IsMatch(mine[0], other.Install)).Select(other => other.Service).ToArray();
            if (others.Length > 0) throw new Exception(family + ": " + service + "'s registration also installs " + string.Join(", ", others));
        }
    }
}
