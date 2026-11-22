using TenshoAssistant.Discoveries;

// 0.1814 (X-807, review of the block 0.1803 to 0.1806, M1 to M4). The merge tool's behaviour (a stop in the rebuild of sync leaves every player
// file as it was; the file bounded() keeps is the size v3_build writes; a kind file over the bound refused; the old plugins' bound said) is in
// its selftest (tools/discoveries-merge.py --selftest, run before each version); here, the wiring, the documents and the trial base of the
// « Discoveries-trial » picture.
internal static class DiscoveryReview1814Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");
    private static string Between(string text,string from,string to)
    {
        var start=text.IndexOf(from,StringComparison.Ordinal);var end=start<0 ? -1:text.IndexOf(to,start,StringComparison.Ordinal);
        return start<0 || end<0 ? "":text[start..end];
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discovery review 1814 M1: sync writes no player file itself: they go to rebuild, written after the common set",() =>
        {
            var tool=Read("tools/discoveries-merge.py");
            var sync=Between(tool,"def sync(dry):","def withdraw(");
            Check(sync.Length>0 && !sync.Contains("write_player(",StringComparison.Ordinal),"no write_player in sync");
            Check(sync.Contains("if not dry:rebuild(fresh=fresh,pending=pending)",StringComparison.Ordinal),"the pending files given to rebuild");
            var rebuild=Between(tool,"def rebuild(","def write_v3(");
            Check(rebuild.Contains("later.append(lambda issue=issue,author=author,given=given,noted=noted:write_player(issue,author,given,noted))",StringComparison.Ordinal)
                && rebuild.Contains("changed=write_v3(bounded(common[\"Facts\"],say=say),V3_DIR,say)\n    for write in later:write()",StringComparison.Ordinal),
                "written in later, after the common set");
            Check(tool.Contains("X-807 M1: a stop in the rebuild of sync leaves every player file as it was",StringComparison.Ordinal),"the tool's behaviour case");
        }),
        ("Discovery review 1814 M2 and M3: one producer, a kind file over the bound refused, the old plugins' bound said",() =>
        {
            var tool=Read("tools/discoveries-merge.py");
            Check(!tool.Contains("v3_kind_bytes(kind,keep,\"t\")",StringComparison.Ordinal) && tool.Contains("v3_kind_bytes(kind,keep,PRODUCER)",StringComparison.Ordinal),"fit_bytes measures with the producer written");
            Check(tool.Contains("v3_build(facts,PRODUCER)",StringComparison.Ordinal) && tool.Contains("\"Producer\":PRODUCER",StringComparison.Ordinal)
                && !tool.Contains("\"Tensho Assistant community\");files=",StringComparison.Ordinal),"the same producer at the three places");
            Check(tool.Contains("if len(data)>MAX_COMPRESSED:raise ValueError(",StringComparison.Ordinal),"v3_build refuses a kind file over the compressed bound");
            Check(tool.Contains("OLD_PLUGIN_FACTS=200_000",StringComparison.Ordinal),"the old plugins' bound");
            foreach(var name in new[]{"X-807 M2: v3_build refuses a kind file over the compressed bound","X-807 M3: a kind past the bound of the plugins up to 0.1804 is said"})
                Check(tool.Contains(name,StringComparison.Ordinal),"the tool's case: "+name);
            var readme=Read("community-discoveries/README.md");
            Check(!readme.Contains("64 KB per line, 200,000 facts per kind)",StringComparison.Ordinal) && readme.Contains("Tensho 0.1804 and older read at",StringComparison.Ordinal)
                && readme.Contains("Tensho 0.1804 et avant lisent au plus 200 000 faits par genre",StringComparison.Ordinal),"the README says both bounds, in both halves");
            var decisions=Read("docs/project/DECISIONS.md");
            Check(decisions.Contains("Completed in 0.1814 (X-807, review of 0.1806)",StringComparison.Ordinal) && decisions.Contains("Complétée en 0.1814 (X-807, relecture de 0.1806)",StringComparison.Ordinal),"DEC-052 completed in both halves");
        }),
        ("Discovery review 1814 M4: the trial base: valid facts, « seen by N players » and « by one only », the most players first",() =>
        {
            var file=DiscoveryTrial.File([11u,12u,13u,14u,15u,16u,17u],148,4);
            Check(file.Facts.Count==DiscoveryTrial.Counts.Length && file.Facts.All(DiscoveryStore.Valid),"one valid fact per monster, at most the counts given");
            var store=new DiscoveryStore();store.Load(file);
            var rows=store.Find(DiscoveryKind.MonsterPlace,_=>true,200);
            var witnesses=rows.Select(DiscoveryFact.WitnessesOf).ToArray();
            Check(rows.Count==6 && witnesses.SequenceEqual(witnesses.OrderByDescending(n=>n)),"rows ordered by players: "+string.Join(",",witnesses));
            Check(witnesses.Count(n=>n>1)>=3 && witnesses.Count(n=>n==1)>=2,"both « seen by N players » and « by one player only » shown");
            Check(rows.Select(fact=>fact.Key).Distinct().Count()==6,"each at its own cell");
        }),
        ("Discovery review 1815 M4: the trial facts are dated before the fixed clock of the reference images, so the picture holds them",() =>
        {
            var references=Read("src/TenshoAssistant.Desktop/References.cs");
            var found=System.Text.RegularExpressions.Regex.Match(references,@"public static readonly DateTimeOffset Clock=new\((\d+),(\d+),(\d+),");
            Check(found.Success,"the fixed clock found");
            var clock=new DateTimeOffset(int.Parse(found.Groups[1].Value),int.Parse(found.Groups[2].Value),int.Parse(found.Groups[3].Value),0,0,0,TimeSpan.Zero);
            Check(DiscoveryTrial.At<=clock,"the trial day "+DiscoveryTrial.At+" is not after the pictures' clock "+clock);
        }),
        ("Discovery review 1814 M4: the picture planned once, given then taken back, never in the game",() =>
        {
            var plan=TenshoAssistant.Desktop.ReferencePlan.Plan(TenshoAssistant.Desktop.DesktopPages.AsIs.Concat(TenshoAssistant.Desktop.DesktopPages.NeedsLink));
            Check(plan.Count(shot=>shot.Id=="Discoveries-trial" && shot.Page=="Discoveries" && shot.Setup=="discoveries-trial")==1,"planned once");
            var references=Read("src/TenshoAssistant.Desktop/References.cs");
            Check(references.Contains("Headless.DiscoveriesTrial(host,true)",StringComparison.Ordinal) && references.Contains("if(shot.Setup==\"discoveries-trial\")Headless.DiscoveriesTrial(host,false);",StringComparison.Ordinal),"given, then taken back");
            var data=Read("src/TenshoAssistant/MainWindow.DesktopData.cs");
            var trial=Between(data,"private void DesktopDiscoveriesTrial(","// X-606 (0.1710, R37)");
            Check(trial.Contains("if(!inDesktopApp)return;",StringComparison.Ordinal) && trial.Contains("DiscoveriesWriteBlocked=true;",StringComparison.Ordinal),"never in the game, never written");
        }),
    ];
}
