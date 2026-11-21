using System.Text;
using System.Text.RegularExpressions;

// X-710 (0.1704, review of 0.1699 to 0.1703): an order check on the code's text (« this comes before that ») is only worth something
// while both texts exist: IndexOf gives -1 for a text gone, and -1 is before everything, so the check stays true for ever.
// Not test classes (no Cases): the helpers of the tests that look at the order of the code.
internal static class SourceOrder
{
    /// <summary>True when both texts are in <paramref name="text"/> and the first one starts before the second one; false when either is missing.</summary>
    public static bool Before(string text,string first,string second)
    {
        var a=text.IndexOf(first,StringComparison.Ordinal);
        var b=text.IndexOf(second,StringComparison.Ordinal);
        return a>=0 && b>=0 && a<b;
    }
}

/// <summary>
/// X-710 (0.1704): every text a test looks for with IndexOf or LastIndexOf in a file it reads must still be in that file. The file is the
/// one named where the variable was last given a value (« var page=Read("MainWindow.CraftSimulation.cs"); »), found by the end of its
/// path among the repository's files; a name that fits several files, a text built at run time and a file not found are left alone.
/// The folders the .gitignore leaves out (files kept on the author's disk only, absent on the CI) are left out too, so the sweep
/// reads the same way everywhere.
/// </summary>
internal static class StaleIndexOf
{
    private static readonly Regex Assign=new(@"\b([A-Za-z_]\w*)\s*=\s*[\w.]*\(\s*""([^""]+\.(?:cs|json|ps1|py|md|yml|sh|csproj|csv))""",RegexOptions.Compiled);
    private static readonly Regex Use=new(@"\b([A-Za-z_]\w*)\.(?:IndexOf|LastIndexOf)\(\s*""((?:[^""\\]|\\.)*)""\s*[,)]",RegexOptions.Compiled);
    private static readonly string[] Roots=["src","tests","tools","docs",".github","scripts","community-discoveries"];
    private static readonly string[] SkippedFolders=["bin","obj",".git","artifacts","checkpoints"];

    /// <summary>The folders the .gitignore names (« folder/ » lines, no pattern), as relative prefixes.</summary>
    private static string[] Ignored()
    {
        var file=Path.GetFullPath(".gitignore");
        if(!File.Exists(file))return [];
        return File.ReadAllLines(file).Select(line=>line.Trim()).Where(line=>line.Length>1 && !line.StartsWith('#') && line.EndsWith('/') && line.IndexOfAny(['*','?','[','!'])<0)
            .Select(line=>line.TrimStart('/')).ToArray();
    }

    /// <summary>Every stale text of the test sources (tests/TenshoAssistant.Tests/*.cs), as « file:line: variable &lt;- path: text ».</summary>
    public static List<string> Find()
    {
        var found=new List<string>();
        foreach(var file in Directory.GetFiles(Path.GetFullPath("tests/TenshoAssistant.Tests"),"*.cs").OrderBy(name=>name,StringComparer.Ordinal))
            found.AddRange(Scan(Path.GetFileName(file),File.ReadAllText(file)));
        return found;
    }

    /// <summary>The stale texts of one test source.</summary>
    public static List<string> Scan(string name,string source)
    {
        var found=new List<string>();
        var lines=source.Replace("\r\n","\n").Split('\n');
        for(var i=0;i<lines.Length;i++)
            foreach(Match use in Use.Matches(lines[i]))
            {
                var variable=use.Groups[1].Value;
                var text=Unescape(use.Groups[2].Value).Replace("\r\n","\n");
                if(text.Length<3)continue;
                var target=Target(lines,i,variable);
                if(target is null || Resolve(target) is not { } path)continue;
                if(!Content(path).Contains(text,StringComparison.Ordinal))found.Add($"{name}:{i+1}: {variable} <- {target}: {text}");
            }
        return found;
    }

    private static string? Target(string[] lines,int at,string variable)
    {
        for(var j=at;j>=0;j--)
            foreach(Match assign in Assign.Matches(lines[j]))
                if(assign.Groups[1].Value==variable)return assign.Groups[2].Value;
        return null;
    }

    private static readonly Lazy<Dictionary<string,string>> Files=new(ListFiles);
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string,string> Contents=new();

    private static string Content(string path)=>Contents.GetOrAdd(path,file=>File.ReadAllText(file).Replace("\r\n","\n"));

    private static string? Resolve(string name)
    {
        name=name.Replace('\\','/').TrimStart('.','/');
        string? match=null;
        foreach(var (relative,path) in Files.Value)
            if(relative==name || relative.EndsWith("/"+name,StringComparison.Ordinal))
            {
                if(match is not null)return null;
                match=path;
            }
        return match;
    }

    private static Dictionary<string,string> ListFiles()
    {
        var root=Path.GetFullPath(".");
        var ignored=Ignored();
        var files=new Dictionary<string,string>(StringComparer.Ordinal);
        foreach(var file in Directory.GetFiles(root))files[Path.GetFileName(file)]=file;
        var stack=new Stack<string>(Roots.Select(folder=>Path.Combine(root,folder)).Where(Directory.Exists));
        while(stack.Count>0)
        {
            var folder=stack.Pop();
            foreach(var file in Directory.GetFiles(folder))
            {
                var relative=Path.GetRelativePath(root,file).Replace('\\','/');
                if(!ignored.Any(prefix=>relative.StartsWith(prefix,StringComparison.Ordinal)))files[relative]=file;
            }
            foreach(var sub in Directory.GetDirectories(folder))
                if(!SkippedFolders.Contains(Path.GetFileName(sub),StringComparer.OrdinalIgnoreCase))stack.Push(sub);
        }
        return files;
    }

    private static string Unescape(string text)
    {
        var built=new StringBuilder(text.Length);
        for(var i=0;i<text.Length;i++)
        {
            if(text[i]!='\\' || i+1>=text.Length) { built.Append(text[i]); continue; }
            var next=text[++i];
            if(next=='u' && i+4<text.Length && int.TryParse(text.AsSpan(i+1,4),System.Globalization.NumberStyles.HexNumber,null,out var code)) { built.Append((char)code); i+=4; continue; }
            built.Append(next switch { 'n'=>'\n', 'r'=>'\r', 't'=>'\t', '0'=>'\0', _=>next });
        }
        return built.ToString();
    }
}
