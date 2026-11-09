using TenshoAssistant.Discoveries;

// 0.1005: QuestItemAsked facts last seen before 0.911's fix are refused when read, imported or merged, and the base is written again without them.
internal static class QuestAskedFix1005Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Quest asked fix 1005: a fact from before 0.911 refused and the base marked to be written; one after it kept",() =>
        {
            var before=new DateTimeOffset(2026,11,8,15,20,27,TimeSpan.Zero);var after=QuestItemAsks.Fixed.AddMinutes(1);
            var old=new DiscoveryFact{Kind=DiscoveryKind.QuestItemAsked,Subject=2169,Detail=68631,LevelMin=1,LevelMax=1,Seen=1,First=before,Last=before};
            var good=new DiscoveryFact{Kind=DiscoveryKind.QuestItemAsked,Subject=2002169,Detail=68631,LevelMin=1,LevelMax=1,Seen=1,First=after,Last=after};
            Check(!DiscoveryStore.Valid(old) && DiscoveryStore.Valid(good),"rule");
            var store=new DiscoveryStore();
            store.Load(new DiscoveryFile{Facts=[old,good]});
            Check(store.Summary().Single().Total==1 && store.Dirty,"loaded without it, written again");
            Check(new DiscoveryStore().Merge([new DiscoveryFact{Kind=DiscoveryKind.QuestItemAsked,Subject=2169,Detail=68631,LevelMin=1,LevelMax=1,Imported=1,First=before,Last=before}]).Refused==1,"import refused");
        }),
        ("Quest asked fix 1005: the merge tool follows the same rule, in rebuild too",() =>
        {
            var tool=File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py"));
            Check(tool.Contains("QUEST_ASKED_FIXED=datetime(2026,11,8,15,42,29,tzinfo=timezone.utc)") && tool.Contains("if f[\"Kind\"]==25 and when(f[\"Last\"])<(QUEST_ASKED_FIXED_DAY if day(f[\"Last\"])==f[\"Last\"] else QUEST_ASKED_FIXED):return False"),"valid");
            Check(tool.Contains("kept=[f for f in player[\"Facts\"] if valid(dict(f))]"),"rebuild");
        }),
    ];
}
