// 0.1146 (X-168, GPT audit of 12/11, R04): GitHub drops the « discoveries » label of an issue opened by a player without rights on the repository.
// The merge tool (tools/discoveries-merge.py) now lists every open issue and finds the shares by label, title or content; it reads a gist only when
// the issue holds one gist link, the CC0-1.0 sharing ticked, a title naming its author and a gist owned by that author; otherwise manual triage,
// with a bilingual notice to the player. The behaviour itself is checked by the tool's selftest (made-up issues, run by check.sh); here, the
// wiring, and that the tool still recognises the very issue the plugin opens (title, « Shared from », gist link, ticked CC0 line).
internal static class MergeTriage1146Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Merge triage: shares found without their label, checked before anything is read, the player told otherwise",() =>
        {
            var tool=Read("tools/discoveries-merge.py");
            var sync=tool.IndexOf("def sync(dry):",StringComparison.Ordinal);
            Check(sync>0 && !tool.Contains("\"--label\",\"discoveries\""),"no label filter: a player's issue loses its label");
            Check(tool.Contains("\"--json\",\"number,title,body,author,labels\"") && tool.Contains("if not is_share(issue):continue"),"every open issue listed, shares picked");
            var triage=tool.IndexOf("gist,doubt=triage(issue)",sync,StringComparison.Ordinal);
            var listing=tool.IndexOf("owner,files=gist_listing(gist)",sync,StringComparison.Ordinal);
            var owner=tool.IndexOf("doubt=owner_doubt(owner,issue)",sync,StringComparison.Ordinal);
            var read=tool.IndexOf("clean(from_gist_files(files,fetch_gist_raw),player=True)",sync,StringComparison.Ordinal);
            Check(triage>sync && listing>triage && owner>listing && read>owner,"triage, then the owner, and only then the gist's content");
            Check(tool.Contains("MANUAL ({DOUBTS[doubt]}): not merged; tell the player: ") && tool.Contains("notice {issue['number']} manual {doubt}"),"manual triage says how to tell the player");
            foreach(var doubt in new[]{"\"gist\":(","\"cc0\":(","\"title\":(","\"owner\":("})Check(tool.Contains(doubt),"notice "+doubt);
            Check(tool.Contains("def notice_manual(issue,doubt,dry):") && tool.Contains("body=compose(*MANUAL_NOTICES[doubt])"),"English then French (tools/bilingual.py)");
            Check(tool.Contains("# X-168: shares found without their label, gist owner and CC0 checked, doubt left to manual triage."),"selftest cases");
        }),
        ("Merge triage: the tool still recognises the issue the plugin opens",() =>
        {
            var tool=Read("tools/discoveries-merge.py");
            var exchange=Read("src/TenshoAssistant/Discoveries/GitHubExchange.cs");
            Check(exchange.Contains("[\"title\"]=$\"Discoveries: {Login}\"") && tool.Contains("DISCOVERIES_TITLE=re.compile(r\"^\\s*Discoveries:\\s*(\\S+)\\s*$\",re.I)"),"title");
            Check(exchange.Contains("[\"body\"]=$\"Shared from Tensho Assistant {version}") && tool.Contains("SHARED_FROM=\"Shared from Tensho Assistant\""),"content");
            Check(exchange.Contains("Gist: https://gist.github.com/{gist}") && tool.Contains("GIST_LINK=re.compile(r\"https://gist\\.github\\.com/"),"gist link");
            Check(exchange.Contains("\\n\\n- [x] I share these discoveries under CC0-1.0 (public domain") && tool.Contains("I share these discoveries under CC0-1\\.0\\b\",re.M)"),"ticked CC0 line");
        }),
    ];
}
