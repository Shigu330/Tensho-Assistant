using TenshoAssistant.Inventory;

// X-557 (0.1774, the user's request of 17/11 « check the parsers to see whether more categories can come out of « Other » »): what surrounds a
// gil change classified « Other » (the game's messages and log rows, the windows open, 3 s before and after) written once in the log without
// any player data (R25), and the new category Duty from the « Other » lines of dalamud.log of 21/11 (the end of duties 171 and 238).
// docs/evidence/0.1774-gil-duty.md.
internal static class GilOther1774Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static readonly DateTimeOffset At = new(2026, 11, 21, 12, 22, 58, TimeSpan.Zero);

    // Made-up names standing for the character's own (never a real one in the repository).
    private static readonly IReadOnlyList<string> Own = GilOtherContext.PersonalNames(["Lune Dorée", "Moogle", "Cerberus", "TAG", "Pipette"]);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Gil other 1774: behaviour (R25): a message with a sender, a player payload or any of the character's own names is left out whole; the game's own lines kept", () =>
        {
            Check(Own.Contains("Lune Dorée") && Own.Contains("Lune") && Own.Contains("Dorée") && Own.Contains("Pipette") && !Own.Contains(""), "the full name and each word: " + string.Join(",", Own));
            Check(GilOtherContext.Safe(false, false, "Vous obtenez 1200 gils.", Own) == "Vous obtenez 1200 gils.", "the game speaking about you: kept");
            Check(GilOtherContext.Safe(true, false, "Vous obtenez 1200 gils.", Own) is null, "a sender (a player or an NPC speaking): left out");
            Check(GilOtherContext.Safe(false, true, "Vous obtenez 1200 gils.", Own) is null, "a player payload: left out, never masked");
            foreach (var text in new[] { "Lune Dorée obtient 10 gils.", "lune obtient", "DORÉE vous fait signe", "DOREE sans accent", "Bienvenue sur Moogle.", "cerberus", "Votre servant Pipette a vendu", "[TAG] a déposé" })
                Check(GilOtherContext.Safe(false, false, text, Own) is null, "one of the character's own names, any case: left out: " + text);
            Check(GilOtherContext.Safe(false, false, "   ", Own) is null, "an empty text: nothing");
            Check(GilOtherContext.Safe(false, false, new string('a', 500), Own) is { Length: 201 } cut && cut.EndsWith('…'), "a long text cut at 200");
            var context = new GilOtherContext();
            Check(context.Message(At.AddSeconds(-1), 62, false, false, "Vous obtenez 1200 gils.", Own), "kept");
            Check(!context.Message(At.AddSeconds(-1), 62, false, false, "Lune Dorée obtient 1200 gils.", Own) && !context.Message(At, 10, true, false, "Bonjour", Own) && !context.Message(At, 62, false, true, "X obtient", Own), "left out");
            context.Other(At, 1200);
            var line = context.Due(At.AddSeconds(3)).Single();
            foreach (var name in Own) Check(!line.Contains(name, StringComparison.OrdinalIgnoreCase), "no own name in the line: " + name);
            Check(line.Contains("kind 62: Vous obtenez 1200 gils.", StringComparison.Ordinal), "the kept message in the line: " + line);
        }),
        ("Gil other 1774: behaviour: the line is written once, 3 s after the change, with what was heard and open from 3 s before to 3 s after", () =>
        {
            var context = new GilOtherContext();
            context.Message(At.AddSeconds(-3.5), 57, false, false, "Too early.", Own);
            context.Message(At.AddSeconds(-2.5), 64, false, false, "Vous gagnez des points d'expérience.", Own);
            context.Row(At.AddSeconds(-0.5), 557);
            context.Windows(At.AddSeconds(-4), ["Old"]);
            context.Windows(At.AddSeconds(-1), ["NowLoading", "Tooltip"]);
            context.Other(At, 1200);
            Check(context.Due(At.AddSeconds(2)).Count == 0 && context.Pending == 1, "not before its 3 s after");
            context.Message(At.AddSeconds(2.5), 62, false, false, "Vous obtenez [item 18172] une paire.", Own);
            context.Windows(At.AddSeconds(2), ["NeedGreed", "Tooltip"]);
            var lines = context.Due(At.AddSeconds(3));
            Check(lines.Count == 1 && context.Pending == 0, "one line at 3 s");
            var line = lines[0];
            Check(line.StartsWith("Gil other context: +1,200; game messages within 3 s: ", StringComparison.Ordinal), line);
            Check(line.Contains("[-2.5 s] kind 64: Vous gagnez des points d'expérience.", StringComparison.Ordinal) && line.Contains("[+2.5 s] kind 62: Vous obtenez [item 18172] une paire.", StringComparison.Ordinal), "before and after, with their offset");
            Check(!line.Contains("Too early", StringComparison.Ordinal) && !line.Contains("Old", StringComparison.Ordinal), "nothing beyond 3 s");
            Check(line.Contains("log message rows: 557 (-0.5 s)", StringComparison.Ordinal) && line.Contains("windows open within 3 s: NowLoading, Tooltip, NeedGreed.", StringComparison.Ordinal), "rows and windows, each window once: " + line);
            Check(context.Due(At.AddSeconds(10)).Count == 0, "written once");
        }),
        ("Gil other 1774: behaviour: two changes each get their line; nothing heard says « none »; at most 20 waiting; old readings dropped", () =>
        {
            var context = new GilOtherContext();
            context.Other(At, 1200);
            context.Other(At.AddSeconds(5), 300);
            var first = context.Due(At.AddSeconds(4));
            Check(first.Count == 1 && first[0].Contains("+1,200", StringComparison.Ordinal) && first[0].Contains("game messages within 3 s: none; log message rows: none; windows open within 3 s: none.", StringComparison.Ordinal), "the first, with nothing heard: " + first[0]);
            Check(context.Due(At.AddSeconds(8)) is [var second] && second.Contains("+300", StringComparison.Ordinal), "the second at its own time");
            var full = new GilOtherContext();
            for (var index = 0; index < 30; index++) full.Other(At.AddSeconds(index), index + 1);
            Check(full.Pending == GilOtherContext.MaxPending, "at most 20 waiting");
            var quiet = new GilOtherContext();
            quiet.Message(At, 57, false, false, "Old line.", Own);
            quiet.Due(At.AddSeconds(10));
            quiet.Other(At.AddSeconds(10), -50);
            Check(quiet.Due(At.AddSeconds(13)).Single().Contains("game messages within 3 s: none", StringComparison.Ordinal), "a message older than 3 s before is gone");
        }),
        ("Gil other 1774: behaviour: the « Other » gains of 21/11 at the end of duties 171 and 238 become Duty; a spending and a gain outside a duty stay « Other »", () =>
        {
            // 13:22:58 « Gil other: …, windows seen in the last 15 s: NowLoading, ActionDetail, Tooltip »; 13:23:03 « … MiniTalkPlayer, NowLoading; log messages: 557 » (made-up amounts below).
            var windows = new Dictionary<string, DateTimeOffset> { ["NowLoading"] = At.AddSeconds(-6) };
            var defeat = new Dictionary<uint, DateTimeOffset> { [557] = At.AddSeconds(-1) };
            GilCategory Category(long delta, bool duty, IReadOnlyDictionary<uint, DateTimeOffset>? heard = null) => GilJournal.InDuty(GilJournal.ForDelta(GilJournal.Classify(windows, At, heard), delta), delta, duty);
            Check(Category(1200, true) == GilCategory.Duty && Category(300, true, defeat) == GilCategory.Duty, "the two gains of each duty end: Duty (the loading screen's teleport only counts a spending)");
            Check(Category(1200, false) == GilCategory.Other, "outside a duty: still « Other »");
            Check(Category(-500, true) == GilCategory.Teleport, "a spending after the loading screen: still the teleport");
            Check(GilJournal.InDuty(GilJournal.ForDelta(GilJournal.Classify(new Dictionary<string, DateTimeOffset>(), At), -500), -500, true) == GilCategory.Other, "a spending in a duty with nothing else: still « Other »");
            Check(GilJournal.InDuty(GilCategory.Repair, -500, true) == GilCategory.Repair && GilJournal.InDuty(GilCategory.Fate, 84, true) == GilCategory.Fate, "a category already found is kept");
            Check((int)GilCategory.Beastmaster == 14 && (int)GilCategory.Duty == 15 && !GilJournal.IsTransfer(GilCategory.Duty), "added last: the saved numbers stay");
        }),
        ("Gil other 1774: wiring: developer mode only, never while cut, the line as a value, the duty and own names from the plugin, unsubscribed; page and texts; evidence", () =>
        {
            var watcher = Read("src/TenshoAssistant/Inventory/GilWatcher.Dalamud.cs");
            Check(watcher.Contains("return GilJournal.InDuty(GilJournal.ForDelta(GilJournal.Classify(seen,now,heard),delta),delta,InDuty?.Invoke()==true);", StringComparison.Ordinal), "the duty after the windows and messages");
            Check(watcher.Contains("if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;\n        if(disposed || !developerMode())return;", StringComparison.Ordinal) && watcher.Contains("if(own.Count==0)return;", StringComparison.Ordinal), "messages: not cut, developer mode, own names known first");
            Check(watcher.Contains("if(category==GilCategory.Other && developerMode())lock(context)context.Other(at,change);", StringComparison.Ordinal) && watcher.Contains("foreach(var line in due)log.Information(\"{0}\",line);", StringComparison.Ordinal), "the change noted and its line written as a value");
            Check(watcher.Contains("if(chat is not null)chat.ChatMessageUnhandled+=OnChatMessage;", StringComparison.Ordinal) && watcher.Contains("if(chat is not null)chat.ChatMessageUnhandled-=OnChatMessage;", StringComparison.Ordinal), "subscribed and removed");
            // X-153 (0.1898): wired by the inventory family.
            var plugin = Read("src/TenshoAssistant/Plugin.Inventory.cs");
            Check(plugin.Contains("gilWatcher.InDuty=()=>condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty];", StringComparison.Ordinal) && plugin.Contains("gilWatcher.OwnNames=()=>[player.CharacterName,", StringComparison.Ordinal), "the plugin's duty and own names");
            Check(Read("src/TenshoAssistant/MainWindow.GilJournal.cs").Contains("GilCategory.Duty=>Dalamud.Interface.FontAwesomeIcon.Dungeon,", StringComparison.Ordinal), "an icon");
            foreach (var language in new[] { "en", "fr" })
                Check(Read($"src/TenshoAssistant/Localization/Languages/{language}.json").Contains("    \"CategoryDuty\": ", StringComparison.Ordinal), "named, " + language);
            var evidence = Read("docs/evidence/0.1774-gil-duty.md");
            Check(evidence.Contains("13:22:58", StringComparison.Ordinal) && evidence.Contains("15:36:34", StringComparison.Ordinal) && evidence.Contains("\n---\n", StringComparison.Ordinal), "the evidence, both halves");
        }),
    ];
}
