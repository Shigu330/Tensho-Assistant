using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

// 0.1607 (X-663, review of 0.1606): the user's own gil balances, times and retainer names stay out of the repository.
// 0.1613 (X-665 M1, review of 0.1612): the first ratchet kept unkeyed SHA-256 hashes of these values, which a few seconds of guessing turn
// back into the values (amounts of four to six digits). No form of the values is in the repository any more; two checks instead:
// 1. The values themselves are read from a list outside the repository (one per line, « # » for a comment): the file named by the
//    TENSHO_PRIVATE_VALUES variable, otherwise %LOCALAPPDATA%\TenshoAssistant-dev\private-values.txt. Absent (a CI runner, another
//    contributor): the check is skipped, never passed in silence.
// 2. A ratchet on a shape, without any value: lines that name gil beside an amount of five digits or more, counted per tracked file and
//    frozen. A new such line fails until its amount is checked to be invented (docs/evidence/0.1604-gil-retainer-sales.md) and the count
//    raised here.
// Every file git tracks or would add is read (README, CONTRIBUTING, SECURITY, repo.json and the .json files included), binaries aside.
internal static class PrivateValues1607Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}

    private static IEnumerable<string> Tokens(string text)
    {
        // Numbers with their thousands separators (space, no-break space, narrow no-break space, comma, dot) joined; words with their hyphens.
        foreach(Match match in Regex.Matches(text,@"\d{1,3}(?:[\u0020\u00a0\u202f,.]\d{3})+|\d+"))yield return Regex.Replace(match.Value,@"[^\d]","");
        foreach(Match match in Regex.Matches(text,@"\p{L}+(?:-\p{L}+)*"))yield return match.Value.ToLowerInvariant();
    }

    internal static string? ListPath()
    {
        var named=Environment.GetEnvironmentVariable("TENSHO_PRIVATE_VALUES");
        if(!string.IsNullOrWhiteSpace(named))return named;
        var local=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return string.IsNullOrEmpty(local) ? null:Path.Combine(local,"TenshoAssistant-dev","private-values.txt");
    }

    internal static HashSet<string> ReadList(string text)=>
        text.Split('\n').Select(line=>line.Trim()).Where(line=>line.Length>0 && !line.StartsWith('#'))
            .SelectMany(Tokens).ToHashSet(StringComparer.Ordinal);

    /// <summary>How many of the values a text holds. A number of fewer than five digits counts only with « gil » less than 40 characters
    /// away: alone, it is as often the game's data (an item and its quantity in submarine-loot.json, written on one line).</summary>
    internal static int Found(string text,HashSet<string> values)
    {
        var hits=new HashSet<string>(StringComparer.Ordinal);
        foreach(var token in Tokens(text).Where(token=>token.Length>=5 || !token.All(char.IsAsciiDigit)))if(values.Contains(token))hits.Add(token);
        var gils=Gil.Matches(text).Select(match=>match.Index).ToArray();
        if(gils.Length>0)
            foreach(Match match in Regex.Matches(text,@"\d{1,3}(?:[   ,.]\d{3})+|\d+"))
            {
                var token=Regex.Replace(match.Value,@"[^\d]","");
                if(token.Length<5 && values.Contains(token) && gils.Any(index=>Math.Abs(index-match.Index)<40))hits.Add(token);
            }
        return hits.Count;
    }

    private static readonly Regex Gil=new(@"\bgils?\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    private static readonly Regex Amount=new(@"(?<![\d.,:#x])(?:\d{1,3}(?:[\u0020\u00a0\u202f,.]\d{3})+|\d{5,})(?![\d])",RegexOptions.CultureInvariant);

    /// <summary>Whether a line names gil beside an amount of five digits or more (the shape of a balance).</summary>
    internal static bool AmountLine(string line)=>Gil.IsMatch(line) && Amount.Matches(line).Any(match=>Regex.Replace(match.Value,@"[^\d]","").Length>=5);

    // Counts of such lines on 20/11 (0.1613), each read: the amounts are invented, the game's own, or limits of the code.
    private static readonly Dictionary<string,int> Frozen=new(StringComparer.Ordinal)
    {
        ["SECURITY.md"]=1,
        ["docs/changelog/CHANGELOG-0.1200-0.1299.md"]=4,
        ["docs/changelog/CHANGELOG-0.1300-0.1399.md"]=2,
        ["docs/changelog/CHANGELOG-0.1600-0.1699.md"]=2,
        ["docs/changelog/CHANGELOG-0.300-0.399.md"]=1,
        ["docs/changelog/CHANGELOG-0.700-0.799.md"]=4,
        ["docs/evidence/0.1319-gil-other.md"]=2,
        ["docs/evidence/0.1434-chat-logfilter.md"]=2,
        ["docs/evidence/0.1604-gil-retainer-sales.md"]=2,
        ["docs/evidence/0.318-plugin-gaps.json"]=1,
        ["docs/evidence/0.519-crystal-actions.md"]=1,
        ["docs/evidence/0.767-retainer-gil-crystals.md"]=1,
        ["docs/project/GLOBAL-PLAN.md"]=2,
        ["src/TenshoAssistant/Inventory/BankModels.cs"]=2,
        ["src/TenshoAssistant/Inventory/GilRecorder.cs"]=1,
        ["src/TenshoAssistant/Inventory/InventoryDisplayLayout.cs"]=2,
        ["tests/TenshoAssistant.Tests/AfterError1551Tests.cs"]=2,
        ["tests/TenshoAssistant.Tests/Currencies344Tests.cs"]=1,
        ["tests/TenshoAssistant.Tests/DeliveryRewards730Tests.cs"]=1,
        ["tests/TenshoAssistant.Tests/GilJournalTests.cs"]=1,
        ["tests/TenshoAssistant.Tests/NpcVendorTests.cs"]=1,
        ["tests/TenshoAssistant.Tests/RetainerExtras767Tests.cs"]=1,
        ["tests/TenshoAssistant.Tests/RetainerGil1293Tests.cs"]=6,
        ["tests/TenshoAssistant.Tests/RetainerPages1285Tests.cs"]=1,
        ["tests/TenshoAssistant.Tests/RetainerSalesTests.cs"]=1,
        ["tests/TenshoAssistant.Tests/RetainerTable1409Tests.cs"]=2,
        ["tests/TenshoAssistant.Tests/ReviewFindings1606Tests.cs"]=1,
        ["tests/TenshoAssistant.Tests/TriadCardTests.cs"]=1,
    };

    private static string[] TrackedFiles()
    {
        var start=new ProcessStartInfo("git"){WorkingDirectory=Path.GetFullPath("."),RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};
        foreach(var argument in new[]{"-c","core.quotepath=off","ls-files","-z","--cached","--others","--exclude-standard"})start.ArgumentList.Add(argument);
        using var git=Process.Start(start) ?? throw new Exception("git could not be started");
        var output=git.StandardOutput.ReadToEnd();
        git.WaitForExit();
        Check(git.ExitCode==0,"git ls-files failed: "+git.StandardError.ReadToEnd());
        return output.Split('\0',StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.Ordinal).ToArray();
    }

    private static IEnumerable<(string Name,string Text)> Texts()
    {
        foreach(var name in TrackedFiles())
        {
            var path=Path.GetFullPath(name);
            if(!File.Exists(path))continue;
            var bytes=File.ReadAllBytes(path);
            if(bytes.Take(8000).Contains((byte)0))continue; // binary
            yield return (name.Replace('\\','/'),Encoding.UTF8.GetString(bytes));
        }
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Private values 1613: the tokenizer joins thousands separators and keeps hyphenated words; the list skips comments",()=>
        {
            var tokens=Tokens("« 1 234 567 » beside 12,345 and 9.876 in Ul-dah, 2026").ToList();
            Check(tokens.Contains("1234567") && tokens.Contains("12345") && tokens.Contains("9876") && tokens.Contains("ul-dah") && tokens.Contains("2026"),string.Join("|",tokens));
            var list=ReadList("# a comment, 99999\r\n12 345\r\nSome-Name\r\n\r\n");
            Check(list.SetEquals(["12345","some-name"]),string.Join("|",list));
            Check(Found("[[43998,4553],[43997,1234]] and far away, after many more words of text: gil",["1234"])==0 && Found("sold for 1 234 gil",["1234"])==1 && Found("Some-Name, 12 345",["some-name","12345"])==2,"short numbers only beside gil");
        }),
        ("Private values 1613: an amount line is gil beside five digits or more, not a time, an id or a year",()=>
        {
            Check(AmountLine("the retainer kept 12 345 gil"),"spaced amount");
            Check(AmountLine("Gils: 123456"),"plain amount");
            Check(!AmountLine("3 555 gil in seven days"),"four digits");
            Check(!AmountLine("gil at 12:34:56 in 2026"),"time and year");
            Check(!AmountLine("item 0x12345 for gil"),"hexadecimal");
            Check(!AmountLine("an amount of 123456 crystals"),"no gil");
        }),
        ("Private values 1613: no hash, digest or other form of the user's values is in this file",()=>
        {
            var text=File.ReadAllText(Path.GetFullPath("tests/TenshoAssistant.Tests/PrivateValues1607Tests.cs"));
            Check(!Regex.IsMatch(text,@"""[0-9a-f]{16,}"""),"a hexadecimal digest is written here");
            Check(!text.Contains("SHA256"+".HashData",StringComparison.Ordinal) && !text.Contains("HM"+"AC",StringComparison.Ordinal),"a hash of the values is computed here");
        }),
        ("Private values 1613: none of the user's values (the list outside the repository) is in a tracked file",()=>
        {
            var path=ListPath();
            if(path is null || !File.Exists(path))throw Skip.Because("no list of private values on this computer (TENSHO_PRIVATE_VALUES or %LOCALAPPDATA%\\TenshoAssistant-dev\\private-values.txt)");
            var values=ReadList(File.ReadAllText(path));
            Check(values.Count>0,"the list of private values is empty");
            var found=new List<string>();
            foreach(var (name,text) in Texts())
            {
                // The number of matches only, never the value: the failure message is printed by the CI and kept in logs.
                var count=Found(text,values);
                if(count>0)found.Add($"{name} ({count})");
            }
            Check(found.Count==0,"private values found (file, how many): "+string.Join(", ",found.Take(20)));
        }),
        ("Private values 1613: lines naming gil beside an amount of five digits or more do not grow (ratchet, R28.14)",()=>
        {
            var counts=new SortedDictionary<string,int>(StringComparer.Ordinal);
            foreach(var (name,text) in Texts())
            {
                if(name=="tests/TenshoAssistant.Tests/PrivateValues1607Tests.cs")continue;
                var count=text.Split('\n').Count(AmountLine);
                if(count>0)counts[name]=count;
            }
            var grown=counts.Where(pair=>pair.Value>Frozen.GetValueOrDefault(pair.Key)).Select(pair=>$"{pair.Key}: {Frozen.GetValueOrDefault(pair.Key)} -> {pair.Value}").ToList();
            Check(grown.Count==0,"new lines with a gil amount (check the amount is invented, then raise the count): "+string.Join("; ",grown)
                +"\nfrozen now:\n"+string.Join("\n",counts.Select(pair=>$"        [\"{pair.Key}\"]={pair.Value},")));
        }),
    ];
}
