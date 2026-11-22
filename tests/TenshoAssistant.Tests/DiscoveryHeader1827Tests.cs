using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

// 0.1827 (X-826): DiscoveryPlayers1801Tests allowed only Format, Version, Issue, Login and Count in a player file's header, while the merge tool
// also writes Gist and Revision (X-804) and Counted (X-808): every merge made with the current tool (sync, the laptop worker, a « note only »
// pull request) failed CI (PR #54). Now one list, PLAYER_HEADER of tools/discoveries-merge.py, read here: a header holds those fields only, each of
// its own shape (an issue number or the name of an anonymous withdrawn file, a GitHub login, a gist id and its revision in hex, a yes/no, a count),
// never a character name, a world, a place nor a day. Checked on the repository's player files (DiscoveryPlayers1801Tests) and on files the current
// tool writes (a share merged with its gist noted, and the anonymous file of a withdrawn player, X-803).
internal static class DiscoveryHeader1827Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    private static readonly string[] Expected=["Format","Version","Issue","Login","Gist","Revision","Counted","Count"];

    /// <summary>The fields a player file's header may hold: PLAYER_HEADER of the merge tool, the one source.</summary>
    public static IReadOnlyList<string> ToolHeader()
    {
        var match=Regex.Match(Read("tools/discoveries-merge.py"),@"^PLAYER_HEADER=\(([^)]*)\)$",RegexOptions.Multiline);
        Check(match.Success,"PLAYER_HEADER in tools/discoveries-merge.py");
        return Regex.Matches(match.Groups[1].Value,"\"([A-Za-z]+)\"").Select(name=>name.Groups[1].Value).ToList();
    }

    /// <summary>A player file's header as the tool writes it: its fields from PLAYER_HEADER only, in that order, each of its own shape; throws otherwise.</summary>
    public static void Header(string file,JsonElement header,int facts,IReadOnlyList<string> allowed)
    {
        Check(header.ValueKind==JsonValueKind.Object,file+": a header object");
        var names=header.EnumerateObject().Select(member=>member.Name).ToList();
        Check(names.All(allowed.Contains),file+": a field the tool never writes in a header (a character name, world, place or day has no place there): "
            +string.Join(", ",names.Where(name=>!allowed.Contains(name))));
        Check(names.SequenceEqual(allowed.Where(names.Contains)),file+": the header's fields in the tool's order: "+string.Join(", ",names));
        Check(header.GetProperty("Format").GetString()=="tensho-discoveries-player" && header.GetProperty("Version").ValueKind==JsonValueKind.Number
            && header.GetProperty("Version").GetInt32()==1,file+": format and version");
        var issue=header.GetProperty("Issue");
        var withdrawn=Regex.IsMatch(file,"^withdrawn-[0-9a-f]{16}\\.jsonl\\.gz$");
        if(withdrawn)
            Check(issue.ValueKind==JsonValueKind.String && issue.GetString()+".jsonl.gz"==file && !header.TryGetProperty("Login",out _)
                && !header.TryGetProperty("Gist",out _),file+": an anonymous file (X-803): its own random name, no login, no gist");
        else
            Check(issue.ValueKind==JsonValueKind.Number && issue.GetInt32()>0 && issue.GetInt32()+".jsonl.gz"==file,file+": its issue number names the file");
        if(header.TryGetProperty("Login",out var login))
            Check(login.ValueKind==JsonValueKind.String && Regex.IsMatch(login.GetString()!,"^[A-Za-z0-9-]{1,39}$"),file+": a GitHub login, nothing else");
        var gist=header.TryGetProperty("Gist",out var id);
        Check(gist==header.TryGetProperty("Revision",out var revision),file+": a gist and its revision together");
        if(gist)
            Check(id.ValueKind==JsonValueKind.String && Regex.IsMatch(id.GetString()!,"^[0-9a-f]{20,40}$") && revision.ValueKind==JsonValueKind.String
                && Regex.IsMatch(revision.GetString()!,"^[0-9a-f]{40}$"),file+": a gist id and a revision, in hex");
        if(header.TryGetProperty("Counted",out var counted))
            Check(counted.ValueKind is JsonValueKind.True or JsonValueKind.False,file+": Counted is a yes or no");
        Check(header.GetProperty("Count").ValueKind==JsonValueKind.Number && header.GetProperty("Count").GetInt32()==facts,file+": the count of its facts");
    }

    private static bool Refused(string json)
    {
        try { Header("43.jsonl.gz",JsonDocument.Parse(json).RootElement,1,Expected); return false; }
        catch(Exception error) when(error.Message.StartsWith("43.jsonl.gz",StringComparison.Ordinal)) { return true; }
    }

    // Writes, with the tool given, the player files of a share merged with its gist noted (43) and of a withdrawn player (from a file of before, 44).
    private const string Maker="""
import importlib.util,json,os,sys
spec=importlib.util.spec_from_file_location("discoveries_merge",sys.argv[1]);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
root=sys.argv[2];m.PLAYERS=os.path.join(root,"players");m.V3_DIR=os.path.join(root,"v3");os.makedirs(m.PLAYERS);os.makedirs(m.V3_DIR)
m.head_v3_keys=lambda:set()
def fact(subject,x,z,day):
    return {"Kind":1,"Subject":subject,"Detail":0,"Territory":128,"Map":1,"CellX":int(x//m.CELL),"CellZ":int(z//m.CELL),"X":x,"Z":z,
            "LevelMin":1,"LevelMax":1,"Seen":0,"Imported":1,"First":day,"Last":day}
old=[fact(1,10.0,10.0,"2026-11-01T00:00:00+00:00"),fact(2,60.0,10.0,"2026-11-02T00:00:00+00:00")]
json.dump({"Issue":44,"Login":"Bo","Facts":old},open(os.path.join(m.PLAYERS,"44.json"),"w",encoding="utf-8"))
shared=[fact(3,110.0,10.0,"2026-11-03T00:00:00+00:00")]
m.rebuild(fresh=[("43.jsonl.gz",f) for f in shared],history=lambda:set(),
          pending={"43.jsonl.gz":(43,"Ann",shared,{"Gist":"0123456789abcdef0123456789abcdef","Revision":"0123456789abcdef0123456789abcdef01234567"})})
m.withdraw("44",False)
""";

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discovery header 1827: wiring: one list of header fields in the tool, written in that order, any other refused on reading",() =>
        {
            Check(ToolHeader().SequenceEqual(Expected),"PLAYER_HEADER: "+string.Join(", ",ToolHeader()));
            var tool=Read("tools/discoveries-merge.py");
            Check(tool.Contains("if list(header)!=[k for k in PLAYER_HEADER if k in header]:raise ValueError(",StringComparison.Ordinal),"player_bytes writes nothing else");
            Check(tool.Contains("if set(header)-set(PLAYER_HEADER):raise ValueError(",StringComparison.Ordinal),"read_player refuses anything else");
            Check(tool.Contains("X-826: a player file's header: exactly PLAYER_HEADER, in its order",StringComparison.Ordinal)
                && tool.Contains("X-826: a header holding {field}: refused",StringComparison.Ordinal),"the tool's selftest cases");
            Check(Read("tests/TenshoAssistant.Tests/DiscoveryPlayers1801Tests.cs").Contains("DiscoveryHeader1827Tests.Header(file,header!.Value,facts.Count,allowed)",StringComparison.Ordinal),
                "the repository's player files checked by the same rule");
        }),
        ("Discovery header 1827: the rule: the tool's fields pass, a player's data or a field of another shape is refused",() =>
        {
            Check(!Refused("""{"Format":"tensho-discoveries-player","Version":1,"Issue":43,"Login":"Shigu330","Gist":"6966b8a6642b8373160064c7fe5b2626","Revision":"95f59bb5f690cdfdc1adcc6412c184b707eeac54","Counted":true,"Count":1}"""),
                "the header PR #54 carries passes");
            Check(!Refused("""{"Format":"tensho-discoveries-player","Version":1,"Issue":43,"Login":"Shigu330","Count":1}"""),"a header of before X-804 passes");
            foreach(var (what,json) in new[]
            {
                ("a character name","""{"Format":"tensho-discoveries-player","Version":1,"Issue":43,"Login":"Ann","Name":"Ann Doe","Count":1}"""),
                ("a world","""{"Format":"tensho-discoveries-player","Version":1,"Issue":43,"World":"Alpha","Count":1}"""),
                ("an exact place","""{"Format":"tensho-discoveries-player","Version":1,"Issue":43,"X":10.5,"Z":3.0,"Count":1}"""),
                ("a day","""{"Format":"tensho-discoveries-player","Version":1,"Issue":43,"First":"2026-11-01T00:00:00+00:00","Count":1}"""),
                ("a login holding a name","""{"Format":"tensho-discoveries-player","Version":1,"Issue":43,"Login":"Ann Doe of Alpha","Count":1}"""),
                ("a gist holding text","""{"Format":"tensho-discoveries-player","Version":1,"Issue":43,"Gist":"Ann Doe","Revision":"95f59bb5f690cdfdc1adcc6412c184b707eeac54","Count":1}"""),
                ("a gist without its revision","""{"Format":"tensho-discoveries-player","Version":1,"Issue":43,"Gist":"6966b8a6642b8373160064c7fe5b2626","Count":1}"""),
                ("Counted holding text","""{"Format":"tensho-discoveries-player","Version":1,"Issue":43,"Counted":"Alpha","Count":1}"""),
                ("fields out of order","""{"Format":"tensho-discoveries-player","Version":1,"Issue":43,"Count":1,"Login":"Ann"}"""),
                ("another issue than the file's","""{"Format":"tensho-discoveries-player","Version":1,"Issue":44,"Count":1}"""),
            })
                Check(Refused(json),"refused: "+what);
        }),
        ("Discovery header 1827: the player files the current tool writes (a share with its gist noted, a withdrawn player) pass the rule",() =>
        {
            string? python=null;
            foreach(var name in new[]{"python","py"})
            {
                try
                {
                    using var probe=Process.Start(new ProcessStartInfo(name,"--version"){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false});
                    if(probe is null)continue;
                    probe.WaitForExit(20000);
                    if(probe.ExitCode==0){python=name;break;}
                }
                catch(Exception){}
            }
            if(python is null)return; // no Python on this machine: the rule and the wiring cases above still hold
            var folder=Path.Combine(Path.GetTempPath(),"tensho-header-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                var script=Path.Combine(folder,"make.py");
                File.WriteAllText(script,Maker);
                var start=new ProcessStartInfo(python){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};
                foreach(var argument in new[]{"-I","-X","utf8",script,Path.GetFullPath("tools/discoveries-merge.py"),Path.Combine(folder,"out")})start.ArgumentList.Add(argument);
                using var run=Process.Start(start)!;
                var output=run.StandardOutput.ReadToEndAsync();
                var error=run.StandardError.ReadToEndAsync();
                Check(run.WaitForExit(120000),"the tool ends");
                Check(run.ExitCode==0,"the tool writes the files ("+run.ExitCode+"): "+output.Result+error.Result);
                var players=RepositoryCommon.Players(Path.Combine(folder,"out","players"));
                Check(players.Count==2 && players.All(player=>player.Header is not null),"two files of the current format: "+string.Join(", ",players.Select(player=>player.File)));
                var allowed=ToolHeader();
                foreach(var (file,header,facts) in players)Header(file,header!.Value,facts.Count,allowed);
                var share=players.Single(player=>player.File=="43.jsonl.gz").Header!.Value;
                Check(share.GetProperty("Login").GetString()=="Ann" && share.GetProperty("Gist").GetString()=="0123456789abcdef0123456789abcdef"
                    && share.GetProperty("Revision").GetString()=="0123456789abcdef0123456789abcdef01234567" && share.GetProperty("Counted").GetBoolean(),
                    "the share's header holds its login, its gist noted and Counted: "+share);
                var gone=players.Single(player=>player.File.StartsWith("withdrawn-",StringComparison.Ordinal));
                Check(gone.Facts.Count==2 && !gone.Header!.Value.TryGetProperty("Login",out _),"the withdrawn player's facts kept without any name: "+gone.Header);
            }
            finally { try { Directory.Delete(folder,true); } catch(IOException){} }
        }),
    ];
}
