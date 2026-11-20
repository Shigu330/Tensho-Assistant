using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

// 0.1607 (X-663, review of 0.1606): the user's own gil balances, times and retainer names stay out of the repository's code, tests and
// documents (X-662 M3 had left some in comments). Ratchet (R10, the user's data, R28.14): every number and word of the tracked sources and
// documents is hashed and compared with the hashes of the values taken from the user's journal and captures; the values themselves are never
// written here. Amounts used in their place are invented, of the same shape (docs/evidence/0.1604-gil-retainer-sales.md).
internal static class PrivateValues1607Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}

    // SHA-256 (first 16 hex digits) of the user's values, written without separators (numbers) or in lower case (names).
    private static readonly HashSet<string> Known=
    [
        "78d4444c3cc702aa","9c7be863c0a75745","f8ddab29627e2e46","b21de7acd7385c82","ac1e0a41fac40d33",
        "fb6be6437b143c29","4525c4f65f8517af","74c5b13f6690b9f2","e6c334e8458af8c1","7c618927153c6a3a",
    ];

    internal static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()[..16];

    private static IEnumerable<string> Tokens(string text)
    {
        // Numbers with their thousands separators (space, no-break space, comma, dot) joined; words with their hyphens.
        foreach(Match match in Regex.Matches(text,@"\d{1,3}(?:[   ,.]\d{3})+|\d+"))yield return Regex.Replace(match.Value,@"[^\d]","");
        foreach(Match match in Regex.Matches(text,@"\p{L}+(?:-\p{L}+)*"))yield return match.Value.ToLowerInvariant();
    }

    private static IEnumerable<string> Files()
    {
        foreach(var folder in new[]{"src/TenshoAssistant","src/TenshoAssistant.Desktop","tests/TenshoAssistant.Tests","docs"})
        {
            var root=Path.GetFullPath(folder);
            if(!Directory.Exists(root))continue;
            foreach(var file in Directory.EnumerateFiles(root,"*",SearchOption.AllDirectories))
            {
                var extension=Path.GetExtension(file);
                if(extension is not (".cs" or ".md"))continue;
                var separator=Path.DirectorySeparatorChar;
                // bin and obj: build output; docs/ai: the agent's own files, never in the repository (they may quote the user's values).
                if(file.Contains($"{separator}obj{separator}") || file.Contains($"{separator}bin{separator}") || file.Contains($"{separator}docs{separator}ai{separator}"))continue;
                yield return file;
            }
        }
        yield return Path.GetFullPath("CHANGELOG.md");
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Private values 1607: the tokenizer joins thousands separators and keeps hyphenated words",()=>
        {
            var tokens=Tokens("« 1 234 567 » beside 12,345 and 9.876 in Ul-dah, 2026").ToList();
            Check(tokens.Contains("1234567") && tokens.Contains("12345") && tokens.Contains("9876") && tokens.Contains("ul-dah") && tokens.Contains("2026"),string.Join("|",tokens));
            Check(Hash("abc")=="ba7816bf8f01cfea","SHA-256 of « abc »");
        }),
        ("Private values 1607: none of the user's journal amounts, times or retainer names is in the code, the tests or the documents",()=>
        {
            var found=new List<string>();
            foreach(var file in Files())
            {
                if(!File.Exists(file))continue;
                foreach(var token in Tokens(File.ReadAllText(file)).Distinct())
                    if(Known.Contains(Hash(token)))found.Add($"{Path.GetFileName(file)} ({Hash(token)})");
            }
            Check(found.Count==0,"the user's values found: "+string.Join(", ",found.Distinct().Take(10)));
        }),
    ];
}
