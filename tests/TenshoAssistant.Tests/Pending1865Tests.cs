using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32.SafeHandles;
using TenshoAssistant;

// 0.1865 (X-859): the CI stayed red from 0.1864. 1) Replace1864Tests looked for a snippet with « \n » in a file read raw; the Windows
// runner checks the files out with CRLF, so it failed there only. A ratchet over the tests' sources now refuses a file read raw and then
// searched for a « \n » snippet without its « \r\n » form. 2) X-844 failed once on CI: the chat sweep deleted a lock file, which is
// « delete pending » for an instant during the sweep's own File.Delete (longer if another program holds it; not shown here), and the
// client waiting for it got access denied (reproduced under load: 4 failures in 30 on the old code, same message). Behaviour test: a lock file made
// « delete pending » by hand (the old way of deleting, kept open), the client waits, then gets the lock once it is let go.
internal static class Pending1865Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    [DllImport("kernel32.dll",SetLastError=true,CharSet=CharSet.Unicode)]
    private static extern SafeFileHandle CreateFileW(string name,uint access,uint share,IntPtr security,uint disposition,uint flags,IntPtr template);
    [DllImport("kernel32.dll",SetLastError=true)]
    private static extern bool SetFileInformationByHandle(SafeFileHandle handle,int infoClass,ref byte info,int size);
    private const uint Delete=0x10000;
    private const uint ShareAll=7;
    private const uint OpenExisting=3;
    private const int FileDispositionInfo=4;

    // The file opened with the right to delete it and marked deleted the classic way: it stays « delete pending » while this handle lives.
    private static SafeFileHandle DeletePending(string path)
    {
        var handle=CreateFileW(path,Delete,ShareAll,IntPtr.Zero,OpenExisting,0,IntPtr.Zero);
        if(handle.IsInvalid)throw new Exception("open for delete failed: "+Marshal.GetLastPInvokeError());
        byte yes=1;
        if(!SetFileInformationByHandle(handle,FileDispositionInfo,ref yes,1)){handle.Dispose();throw new Exception("mark deleted failed: "+Marshal.GetLastPInvokeError());}
        return handle;
    }

    private static string Folder()=>Directory.CreateDirectory(Path.Combine(Path.GetTempPath(),"tensho-1865-"+Guid.NewGuid().ToString("N"))).FullName;

    private static string FindRepositoryFile(string relative)
    {
        for(var folder=new DirectoryInfo(AppContext.BaseDirectory);folder is not null;folder=folder.Parent)
        {
            var candidate=Path.Combine(folder.FullName,relative);
            if(Directory.Exists(candidate) || File.Exists(candidate))return candidate;
        }
        return Path.GetFullPath(relative);
    }

    // A text read raw (File.ReadAllText, or a helper of the file that returns it raw), then searched for a snippet holding « \n »: the
    // statement (its line and the next) must also accept « \r\n ». X-861 (0.1868, review 0.1866 M1): the read searched in line
    // (« File.ReadAllText(…).Contains("…\n…") »), a « string x= » as well as a « var x= », and a helper of the file (« string Read=>… »,
    // « string Read(…)=>… », « string Read(…){return …;} ») count too; only a normalisation of the line endings right after the read
    // (ReplaceLineEndings, Replace("\r…") or Replace('\r'…)) lets it pass, not any Replace among the arguments; a « \n » that starts the
    // snippet passes too (see NewlineSearch). X-864 (0.1870, review 0.1868 M2): every search that a « \n » snippet breaks on a CRLF text
    // (Contains, IndexOf, LastIndexOf, StartsWith, EndsWith, Split), a read or a variable followed by other calls before the search
    // (« File.ReadAllText(…).Trim().Contains(…) », « x.Trim().IndexOf(…) »; a normalisation anywhere in that chain lets it pass), and a
    // helper of another file (« Other.Read(…) », given by <paramref name="foreign"/>, see RawHelpers) count too.
    internal static IReadOnlyList<string> RawNewlineSearches(string source)=>RawNewlineSearches(source,[]);

    internal static IReadOnlyList<string> RawNewlineSearches(string source,IEnumerable<(string Name,bool Property)> foreign)
    {
        var found=new List<string>();
        var readers=new List<(string Name,bool Property)>{("File.ReadAllText",false)};
        foreach(var helper in RawHelpers(source))readers.Add((helper.Name,helper.Property));
        readers.AddRange(foreign);
        foreach(var (reader,property) in readers)
        {
            var pattern=@"(?<![\w.])"+Regex.Escape(reader)+(property ? @"\b(?!\s*(?:\(|=>|\{))":@"\s*\(");
            foreach(Match use in Regex.Matches(source,pattern))
            {
                if(Regex.IsMatch(source[..use.Index],@"\bstring\s+$"))continue;
                if(source[(source.LastIndexOf('\n',Math.Max(0,use.Index-1))+1)..use.Index].TrimStart().StartsWith("//",StringComparison.Ordinal))continue;
                var end=property ? use.Index+use.Length:CallEnd(source,use.Index+use.Length-1);
                if(end<0)continue;
                var (chained,normalised)=Chain(source,end);
                if(normalised)continue;
                end=chained;
                if(Regex.IsMatch(source[end..],@"^\s*"+NewlineSearch))
                {
                    if(!Statement(source,end).Contains(@"\r\n",StringComparison.Ordinal))found.Add(reader+": "+Line(source,end));
                    continue;
                }
                var assigned=Regex.Match(source[..use.Index],@"\b(?:var|string)\s+(\w+)\s*=\s*$");
                if(!assigned.Success)continue;
                var name=assigned.Groups[1].Value;
                var rest=source[end..];
                var next=Regex.Match(rest,@"\b(?:var|string)\s+"+Regex.Escape(name)+@"\s*=");
                var lines=(next.Success ? rest[..next.Index]:rest).Split('\n');
                for(var i=0;i<lines.Length;i++)
                {
                    if(!Regex.IsMatch(lines[i],@"\b"+Regex.Escape(name)+VariableChain+NewlineSearch))continue;
                    var statement=lines[i]+(i+1<lines.Length ? lines[i+1]:"");
                    if(!statement.Contains(@"\r\n",StringComparison.Ordinal))found.Add(name+": "+lines[i].Trim());
                }
            }
        }
        return found;
    }

    // A search for a snippet with a « \n » after another character of the snippet: « \r\n » breaks it. A « \n » that starts the snippet
    // still matches a CRLF text (the « \r » comes before it), so it is not refused.
    private const string NewlineSearch=@"\s*\.\s*(?:Contains|IndexOf|LastIndexOf|StartsWith|EndsWith|Split)\s*\(\s*""(?:[^""\\]|\\.)*(?:[^""\\]|\\.)\\n";

    // Calls between a variable and its search (« x.Trim().IndexOf(…) »), none of them a normalisation of the line endings; simple
    // arguments only (no parenthesis inside).
    private const string VariableChain=@"(?:\s*\.\s*(?!ReplaceLineEndings\b|Replace\s*\(\s*(?:""\\r|'\\r'))\w+\s*\([^()]*\))*";

    // The line endings made one form right after the read.
    private static bool Normalised(string source,int end)=>
        Regex.IsMatch(source[end..],@"^\s*\.(?:ReplaceLineEndings\s*\(|Replace\s*\(\s*(?:""\\r|'\\r'))");

    private static readonly Regex ChainedCall=new(@"\G\s*\.\s*(\w+)\s*\(");
    private static readonly HashSet<string> Searches=["Contains","IndexOf","LastIndexOf","StartsWith","EndsWith","Split"];

    // X-864: the end of the calls chained after a read (« .Trim() », « .Substring(3) »…), up to a search; normalised when one of them
    // makes the line endings one form.
    private static (int End,bool Normalised) Chain(string source,int end)
    {
        while(true)
        {
            if(Normalised(source,end))return (end,true);
            var call=ChainedCall.Match(source,end);
            if(!call.Success || Searches.Contains(call.Groups[1].Value))return (end,false);
            var close=CallEnd(source,call.Index+call.Length-1);
            if(close<0)return (end,false);
            end=close;
        }
    }

    /// <summary>X-864: the helpers of <paramref name="source"/> that return a file's text raw (« string Read=>File.ReadAllText(…) »,
    /// « string Read(…)=>… », « string Read(…){return …;} », calls chained after the read allowed), with the class that holds each and
    /// whether another file can call it (internal or public), so the ratchet also finds them used from another file (« Class.Read(…) »).</summary>
    internal static IReadOnlyList<(string Name,bool Property,string Class,bool Shared)> RawHelpers(string source)
    {
        var found=new List<(string,bool,string,bool)>();
        foreach(Match helper in Regex.Matches(source,@"\bstring\s+(\w+)\s*(\([^)]*\))?\s*(?:=>|\{\s*return\b)\s*File\.ReadAllText\s*\("))
        {
            var end=CallEnd(source,helper.Index+helper.Length-1);
            if(end<0 || Chain(source,end).Normalised)continue;
            var owner=Regex.Matches(source[..helper.Index],@"\bclass\s+(\w+)").LastOrDefault()?.Groups[1].Value ?? "";
            var start=source.LastIndexOf('\n',Math.Max(0,helper.Index-1))+1;
            var shared=Regex.IsMatch(source[start..helper.Index],@"\b(?:internal|public)\b");
            found.Add((helper.Groups[1].Value,!helper.Groups[2].Success,owner,shared));
        }
        return found;
    }

    // The index just after the parenthesis that closes the one at « open » (string and character literals skipped), or -1.
    private static int CallEnd(string source,int open)
    {
        var depth=0;
        for(var i=open;i<source.Length;i++)
        {
            var c=source[i];
            if(c=='@' && i+1<source.Length && source[i+1]=='"')
            {
                for(i+=2;i<source.Length;i++)
                {
                    if(source[i]!='"')continue;
                    if(i+1<source.Length && source[i+1]=='"'){i++;continue;}
                    break;
                }
            }
            else if(c=='"' || c=='\'')
            {
                for(i++;i<source.Length && source[i]!=c;i++)if(source[i]=='\\')i++;
            }
            else if(c=='(')depth++;
            else if(c==')' && --depth==0)return i+1;
        }
        return -1;
    }

    private static string Line(string source,int at)
    {
        var start=source.LastIndexOf('\n',Math.Max(0,at-1))+1;
        var stop=source.IndexOf('\n',at);
        return source[start..(stop<0 ? source.Length:stop)].Trim();
    }

    // The line of « at » and the next one.
    private static string Statement(string source,int at)
    {
        var stop=source.IndexOf('\n',at);
        if(stop>=0){var after=source.IndexOf('\n',stop+1);stop=after<0 ? source.Length:after;}
        return Line(source,at)+(stop<0 ? "":source[source.IndexOf('\n',at)..stop]);
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Pending 1865: no test reads a file raw then looks for a « \\n » snippet without its « \\r\\n » form (CRLF on the CI runner)",() =>
        {
            Check(RawNewlineSearches("var tool=File.ReadAllText(Path.GetFullPath(\"a.py\"));\nCheck(tool.Contains(\"a:\\n    b\",StringComparison.Ordinal),\"x\");\n").Count==1,
                "the ratchet finds a raw read searched with « \\n »");
            Check(RawNewlineSearches("var tool=File.ReadAllText(Path.GetFullPath(\"a.py\")).ReplaceLineEndings(\"\\n\");\nCheck(tool.Contains(\"a:\\n    b\"),\"x\");\n").Count==0
                && RawNewlineSearches("var tool=File.ReadAllText(Path.GetFullPath(\"a.py\"));\nCheck(tool.Contains(\"a:\\n b\")\n    || tool.Contains(\"a:\\r\\n b\"),\"x\");\n").Count==0,
                "a normalised read, or a search that takes both forms, passes");
            var folder=FindRepositoryFile("tests/TenshoAssistant.Tests");
            var bad=new List<string>();
            // X-864 (0.1870): the raw helpers that other files can call (internal or public), found from them too (« Class.Read(…) »).
            var sources=Directory.EnumerateFiles(folder,"*.cs").ToDictionary(file=>file,File.ReadAllText);
            var helpers=sources.SelectMany(source=>RawHelpers(source.Value).Where(helper=>helper.Shared && helper.Class.Length>0)
                .Select(helper=>(File:source.Key,Name:helper.Class+"."+helper.Name,helper.Property))).ToList();
            foreach(var (file,source) in sources)
                foreach(var line in RawNewlineSearches(source,helpers.Where(helper=>helper.File!=file).Select(helper=>(helper.Name,helper.Property))))
                    bad.Add(Path.GetFileName(file)+": "+line);
            Check(bad.Count==0,"raw reads searched with « \\n »: "+string.Join(" | ",bad));
        }),
        ("Pending 1865: a lock file left « delete pending » by another program: the client waits, then holds the lock once it is let go",() =>
        {
            var folder=Folder();
            try
            {
                var path=Path.Combine(folder,"1.jsonl.lock");
                File.WriteAllText(path,"");
                var pending=DeletePending(path);
                Exception? direct=null;
                try{using(new FileStream(path,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){}}
                catch(Exception error){direct=error;}
                Check(direct is UnauthorizedAccessException,"Windows refuses a « delete pending » file with access denied: "+direct?.GetType().Name);
                var holding=Task.Run(()=>FairFileLock.Hold(path,10_000,error=>error));
                Check(!holding.Wait(300),"the client waits while the file is « delete pending »");
                pending.Dispose();
                Check(holding.Wait(10_000),"the client holds the lock once the other program let the file go");
                holding.Result.Dispose();
            }
            finally{try{Directory.Delete(folder,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
        }),
        ("Pending 1865: a lock file that really cannot be opened still raises access denied, once the wait is over",() =>
        {
            var folder=Folder();
            var path=Path.Combine(folder,"2.jsonl.lock");
            try
            {
                File.WriteAllText(path,"");
                File.SetAttributes(path,FileAttributes.ReadOnly);
                var started=Environment.TickCount64;
                Exception? raised=null;
                try{using(FairFileLock.Hold(path,200,error=>error)){}}
                catch(Exception error){raised=error;}
                Check(raised is UnauthorizedAccessException,"access denied raised: "+raised?.GetType().Name);
                Check(Environment.TickCount64-started>=150,"raised after the wait, not at once");
            }
            finally
            {
                try{File.SetAttributes(path,FileAttributes.Normal);}catch(IOException){}
                try{Directory.Delete(folder,true);}catch(IOException){}catch(UnauthorizedAccessException){}
            }
        }),
    ];
}
