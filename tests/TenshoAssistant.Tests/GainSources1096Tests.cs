using TenshoAssistant.Discoveries;

// 0.1096 (user report, 11/11 15 h): gains of retainer ventures, an airship and a submarine back and squadron titles were logged as « not
// attributed »; the game's log messages (by row) and the venture window now name their source. docs/evidence/0.1096-gain-sources.md.
internal static class GainSources1096Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gain sources 1096: log message rows",() =>
        {
            Check(GainLogMessages.SourceOf(4161)==GainSource.AirshipReturn && GainLogMessages.SourceOf(6059)==GainSource.SubmarineReturn,"voyages back");
            Check(GainLogMessages.SourceOf(3937)==GainSource.Squadron && GainLogMessages.SourceOf(3938)==GainSource.SquadronTraining,"squadron");
            Check(GainLogMessages.SourceOf(4163)==GainSource.None && GainLogMessages.VoyageNote(4163)=="airship new destination" && GainLogMessages.VoyageNote(6062)=="submarine rank","notes are not sources");
            Check(GainLogMessages.SourceOf(1)==GainSource.None && GainLogMessages.VoyageNote(1) is null,"anything else");
        }),
        ("Gain sources 1096: a gain waits for its source, before or after it",() =>
        {
            var pending=new PendingGains<string>();
            // The squadron's message comes just before its titles; the airship's three seconds after its items (dalamud.log of 11/11).
            pending.Source(GainSource.Squadron,12,1_000);
            pending.Add("14954x5",1_060);
            pending.Add("12923x13",10_000);
            var first=pending.Settle(10_100);
            Check(first.Explained.Single() is ("14954x5",GainSource.Squadron,12u) && first.Alone.Count==0 && pending.Waiting==1,"squadron explained, the airship's gain still waits");
            pending.Source(GainSource.AirshipReturn,0,13_200);
            var second=pending.Settle(13_300);
            Check(second.Explained.Single() is ("12923x13",GainSource.AirshipReturn,0u) && pending.Waiting==0,"airship explained after");
            pending.Add("36177x4",40_000);
            Check(pending.Settle(45_000).Alone.Count==0 && pending.Waiting==1,"waits the hold");
            var late=pending.Settle(46_000);
            Check(late.Alone.SequenceEqual(["36177x4"]) && late.Explained.Count==0,"alone after the hold: not attributed");
            pending.Source(GainSource.Venture,584,50_000);pending.Source(GainSource.SubmarineReturn,0,53_500);
            pending.Add("12923x24",53_000);
            Check(pending.Settle(53_600).Explained.Single().Source==GainSource.SubmarineReturn,"the closest source wins");
            // 0.1099: a window explains a gain only when nothing else does (the venture's result window is a window too).
            pending.Source(GainSource.Window,0,70_000);pending.Source(GainSource.Venture,584,70_000);pending.Add("5825x7",70_000);
            Check(pending.Settle(70_100).Explained.Single().Source==GainSource.Venture,"venture over its window");
            pending.Source(GainSource.Window,0,90_000);pending.Add("44106x212",90_400);
            Check(pending.Settle(90_500).Explained.Single().Source==GainSource.Window,"taken out of the saddlebag: a window, never noted");
        }),
        ("Gain sources 1096: wiring",() =>
        {
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("chat.LogMessage+=OnLogMessage") && watcher.Contains("chat.LogMessage-=OnLogMessage"),"log messages subscribed and released");
            Check(watcher.Contains("unexplained.Source(GainSource.Venture,task,"),"the venture result is a source");
            Check(watcher.Contains("SettleGains(now);") && watcher.Contains("\"Item gain attributed: {0} from {1} {2}.\"") && watcher.Contains("\"Item gain not attributed: {0}; {1}.\""),"settled every pass, both lines in the developer log");
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.1096-gain-sources.md")),"evidence");
            Check(watcher.Contains("unexplained.Source(crafting ? GainSource.Craft:gathering ? GainSource.Gathering:windowOpen ? GainSource.Window:GainSource.None,0,now);")
                  && watcher.Contains("if(source!=GainSource.Window)log.Information(\"Item gain attributed"),"0.1099: craft, gathering and windows; windows silent");
        }),
    ];
}
