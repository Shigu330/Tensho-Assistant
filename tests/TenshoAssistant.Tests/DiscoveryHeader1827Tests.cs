using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

// 0.1827 (X-826): DiscoveryPlayers1801Tests allowed only Format, Version, Issue, Login and Count in a player file's header, while the merge tool
// also writes Gist and Revision (X-804) and Counted (X-808): every merge made with the current tool (sync, the laptop worker, a « note only »
// pull request) failed CI (PR #54). Now one list, PLAYER_HEADER of tools/discoveries-merge.py, read here: a header holds those fields only, each of
// its own shape (an issue number or the name of an anonymous withdrawn file, a GitHub login, a gist id and its revision in hex, a yes/no, a count),
// never a character name, a world, a place nor a day. Checked on the repository's player files (DiscoveryPlayers1801Tests) and on files the current
// tool writes (a share merged with its gist noted, and the anonymous file of a withdrawn player, X-803).
// X-830 (D-226 a, user's choice of 22/11): version 2, anonymous: PLAYER_HEADER is Format, Version, Counted and Count; the file is named by a
// random id (16 hex digits); no issue, login, gist nor revision (the link is the worker's, outside the repository; the gists merged are in
// merged.txt, hashed).
internal static class DiscoveryHeader1827Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    private static readonly string[] Expected=["Format","Version","Counted","Count"];

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
            && header.GetProperty("Version").GetInt32()==2,file+": format and version");
        // X-830: named by a random id, never by an issue; nothing in the header ties it to an account
        Check(Regex.IsMatch(file,"^[0-9a-f]{16}\\.jsonl\\.gz$"),file+": named by a random id (16 hex digits), never by an issue");
        foreach(var name in new[]{"Issue","Login","Gist","Revision"})Check(!header.TryGetProperty(name,out _),file+": no "+name+" (X-830)");
        if(header.TryGetProperty("Counted",out var counted))
            Check(counted.ValueKind is JsonValueKind.True or JsonValueKind.False,file+": Counted is a yes or no");
        Check(header.GetProperty("Count").ValueKind==JsonValueKind.Number && header.GetProperty("Count").GetInt32()==facts,file+": the count of its facts");
    }

    private static bool Refused(string json)
    {
        try { Header("0123456789abcdef.jsonl.gz",JsonDocument.Parse(json).RootElement,1,Expected); return false; }
        catch(Exception error) when(error.Message.StartsWith("0123456789abcdef.jsonl.gz",StringComparison.Ordinal)) { return true; }
    }

    // Writes, with the tool given, the player files of a share merged with its gist listed as merged (43, through the private link) and of a
    // withdrawn player (from a file of before, 44); X-830: the numbered file of before anonymized first.
    private const string Maker="""
import importlib.util,json,os,sys
spec=importlib.util.spec_from_file_location("discoveries_merge",sys.argv[1]);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
root=sys.argv[2];m.PLAYERS=os.path.join(root,"players");m.V3_DIR=os.path.join(root,"v3");m.MERGED_PATH=os.path.join(root,"merged.txt");os.makedirs(m.PLAYERS);os.makedirs(m.V3_DIR)
links=os.path.join(root,"..","private","links.json")
m.head_v3_keys=lambda:set()
def fact(subject,x,z,day):
    return {"Kind":1,"Subject":subject,"Detail":0,"Territory":128,"Map":1,"CellX":int(x//m.CELL),"CellZ":int(z//m.CELL),"X":x,"Z":z,
            "LevelMin":1,"LevelMax":1,"Seen":0,"Imported":1,"First":day,"Last":day}
old=[fact(1,10.0,10.0,"2026-11-01T00:00:00+00:00"),fact(2,60.0,10.0,"2026-11-02T00:00:00+00:00")]
json.dump({"Issue":44,"Login":"Bo","Facts":old},open(os.path.join(m.PLAYERS,"44.json"),"w",encoding="utf-8"))
m.anonymize(False,links)
shared=[fact(3,110.0,10.0,"2026-11-03T00:00:00+00:00")]
mine=m.read_links(links);mine["43"]={"File":m.new_file_id(),"Login":"Ann"};m.write_links(mine,links)
name=mine["43"]["File"]+m.PLAYER_SUFFIX
m.rebuild(fresh=[(name,f) for f in shared],history=lambda:set(),pending={name:(mine["43"]["File"],shared)},owners=m.owners_of(mine),
          notes={m.note_hash("0123456789abcdef0123456789abcdef","0123456789abcdef0123456789abcdef01234567")})
m.withdraw("44",False,links)
print(json.dumps({"share":name}))
""";

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discovery header 1827: wiring: one list of header fields in the tool, written in that order, any other refused on reading",() =>
        {
            Check(ToolHeader().SequenceEqual(Expected),"PLAYER_HEADER: "+string.Join(", ",ToolHeader()));
            var tool=Read("tools/discoveries-merge.py");
            Check(tool.Contains("if list(header)!=[k for k in PLAYER_HEADER if k in header]:raise ValueError(",StringComparison.Ordinal),"player_bytes writes nothing else");
            Check(tool.Contains("fields=PLAYER_HEADER if header.get(\"Version\")==PLAYER_VERSION else OLD_HEADER",StringComparison.Ordinal)
                && tool.Contains("if set(header)-set(fields):raise ValueError(",StringComparison.Ordinal),"read_player refuses anything else (version 1: read to be anonymized only)");
            Check(tool.Contains("X-826: a player file's header: exactly PLAYER_HEADER, in its order",StringComparison.Ordinal)
                && tool.Contains("X-826: a header holding {field}: refused",StringComparison.Ordinal),"the tool's selftest cases");
            Check(Read("tests/TenshoAssistant.Tests/DiscoveryPlayers1801Tests.cs").Contains("DiscoveryHeader1827Tests.Header(file,header!.Value,facts.Count,allowed)",StringComparison.Ordinal),
                "the repository's player files checked by the same rule");
        }),
        ("Discovery header 1827: the rule: the tool's fields pass, a player's data or a field of another shape is refused",() =>
        {
            Check(!Refused("""{"Format":"tensho-discoveries-player","Version":2,"Counted":true,"Count":1}""") && !Refused("""{"Format":"tensho-discoveries-player","Version":2,"Count":1}"""),
                "the header the tool writes passes");
            foreach(var (what,json) in new[]
            {
                ("an issue (X-830)","""{"Format":"tensho-discoveries-player","Version":2,"Issue":43,"Count":1}"""),
                ("a login (X-830)","""{"Format":"tensho-discoveries-player","Version":2,"Login":"Shigu330","Count":1}"""),
                ("a gist and its revision (X-830)","""{"Format":"tensho-discoveries-player","Version":2,"Gist":"6966b8a6642b8373160064c7fe5b2626","Revision":"95f59bb5f690cdfdc1adcc6412c184b707eeac54","Count":1}"""),
                ("the header of before X-830 (PR #54)","""{"Format":"tensho-discoveries-player","Version":1,"Issue":43,"Login":"Shigu330","Gist":"6966b8a6642b8373160064c7fe5b2626","Revision":"95f59bb5f690cdfdc1adcc6412c184b707eeac54","Counted":true,"Count":1}"""),
                ("a character name","""{"Format":"tensho-discoveries-player","Version":2,"Name":"Ann Doe","Count":1}"""),
                ("a world","""{"Format":"tensho-discoveries-player","Version":2,"World":"Alpha","Count":1}"""),
                ("an exact place","""{"Format":"tensho-discoveries-player","Version":2,"X":10.5,"Z":3.0,"Count":1}"""),
                ("a day","""{"Format":"tensho-discoveries-player","Version":2,"First":"2026-11-01T00:00:00+00:00","Count":1}"""),
                ("Counted holding text","""{"Format":"tensho-discoveries-player","Version":2,"Counted":"Alpha","Count":1}"""),
                ("fields out of order","""{"Format":"tensho-discoveries-player","Version":2,"Count":1,"Counted":true}"""),
                ("another count","""{"Format":"tensho-discoveries-player","Version":2,"Count":2}"""),
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
                var shareName=JsonDocument.Parse(output.Result.Trim().Split('\n').Last()).RootElement.GetProperty("share").GetString();
                var share=players.Single(player=>player.File==shareName);
                Check(share.Header!.Value.GetProperty("Counted").GetBoolean() && share.Facts.Count==1,"the share's file: counted, its facts, no login nor gist: "+share.Header);
                var gone=players.Single(player=>player.File!=shareName);
                Check(gone.Facts.Count==2,"the withdrawn player's facts kept without any name: "+gone.Header);
                var merged=File.ReadAllText(Path.Combine(folder,"out","merged.txt"));
                Check(merged.StartsWith("# tensho-discoveries-merged 1\n",StringComparison.Ordinal) && !merged.Contains("0123456789abcdef0123456789abcdef",StringComparison.Ordinal)
                    && merged.Contains(TenshoAssistant.Discoveries.GistRotation.NoteHash("0123456789abcdef0123456789abcdef","0123456789abcdef0123456789abcdef01234567"),StringComparison.Ordinal),
                    "the gist merged listed apart, hashed as the plugin computes it: "+merged);
                var link=File.ReadAllText(Path.Combine(folder,"private","links.json"));
                Check(link.Contains("\"Ann\"",StringComparison.Ordinal) && !link.Contains("\"44\"",StringComparison.Ordinal),"the private link outside the repository: 43 linked, 44 forgotten once withdrawn");
            }
            finally { try { Directory.Delete(folder,true); } catch(IOException){} }
        }),
    ];
}
