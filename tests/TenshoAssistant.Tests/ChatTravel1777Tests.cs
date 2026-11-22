using TenshoAssistant.Chat;

// X-484 (0.1777, beyond the game; the user's request of 15/11 « hide the others' mounts and teleports », reading R-X-484 of 20/11): a tab can
// leave out the combat lines in which another player readies or uses Teleport or Return (ChatTravel). docs/evidence/0.1777-chat-others-teleport.md.
internal static class ChatTravel1777Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static readonly DateTimeOffset Now = new(2026, 11, 22, 5, 0, 0, TimeSpan.Zero);

    // The French client's names of Action rows 5 and 6 (docs/evidence/0.1734-chat-action-lines.md).
    private static readonly IReadOnlySet<string> Names = ChatTravel.From(["Téléportation", "Retour", null, " "]);

    // A combat line as the reader keeps it: the player's name, the template's words, the action's name marked (made-up names).
    private static ChatRecord Line(string action, byte source, int kind = 43, string verb = " utilise ") =>
        new(0, Now, kind, ChatChannels.From(kind), "", [new ChatSegment("Moonie Example"), new ChatSegment(verb), new ChatSegment(action, ChatSegmentKind.ActionName), new ChatSegment(".")], source);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat travel 1777: behaviour: another player's Teleport and Return lines (readies, uses) are found; yours, other actions, other lines are not", () =>
        {
            Check(Names.Count == 2 && Names.Contains("Téléportation") && Names.Contains("Retour"), "the two names, empty ones left out");
            Check(ChatTravel.Actions.SequenceEqual([5u, 6u]), "Action rows 5 and 6");
            foreach (var source in new byte[] { 2, 3, 4 })
            {
                Check(ChatTravel.ByOthers(Line("Téléportation", source), Names), "uses Teleport, subject " + source);
                Check(ChatTravel.ByOthers(Line("Téléportation", source, verb: " prépare "), Names), "readies Teleport, subject " + source);
                Check(ChatTravel.ByOthers(Line("Retour", source), Names), "uses Return, subject " + source);
            }
            Check(!ChatTravel.ByOthers(Line("Téléportation", 1), Names), "you: never hidden");
            foreach (var source in new byte[] { 0, 5, 6, 7, 8, 9, 11 }) Check(!ChatTravel.ByOthers(Line("Téléportation", source), Names), "not a player other than you: " + source);
            Check(!ChatTravel.ByOthers(Line("Sprint", 4), Names), "another action");
            var plain = new ChatRecord(0, Now, 43, ChatChannel.Combat, "", [new ChatSegment("Moonie Example utilise Téléportation.")], 4);
            Check(!ChatTravel.ByOthers(plain, Names), "a name not marked as an action (read back as text): not taken for one");
            var said = new ChatRecord(0, Now, 10, ChatChannel.Say, "Moonie Example", [new ChatSegment("Téléportation", ChatSegmentKind.ActionName)], 4);
            Check(!ChatTravel.ByOthers(said, Names), "not a combat line");
            Check(!ChatTravel.ByOthers(Line("Téléportation", 4), new HashSet<string>()), "names not read: nothing hidden");
        }),
        ("Chat travel 1777: behaviour: a tab with « Hide others' teleports » leaves those lines out, the other tabs keep them; off by default", () =>
        {
            var store = new ChatStore();
            void Add(string action, byte source) => store.Add(Now, 43, "", [new ChatSegment("Moonie Example"), new ChatSegment(" utilise "), new ChatSegment(action, ChatSegmentKind.ActionName), new ChatSegment(".")], 2000, source);
            ChatTabSettings hiding = new() { Name = "Battle", Channels = [ChatChannel.Combat], HideOthersTravel = true }, keeping = new() { Name = "All", Channels = [ChatChannel.Combat] };
            Check(!new ChatTabSettings().HideOthersTravel, "off by default");
            Add("Téléportation", 4); Add("Téléportation", 1); Add("Retour", 2); Add("Sprint", 4);
            var filter = new ChatTabFilter(hiding, null, null, Names);
            var shown = store.After(0).Where(filter.Shows).Select(record => (record.Body[2].Text, record.Source)).ToArray();
            Check(shown.Length == 2 && shown.Contains(("Téléportation", (byte)1)) && shown.Contains(("Sprint", (byte)4)), "your teleport and another's sprint stay: " + string.Join(", ", shown));
            Check(store.After(0).Count(new ChatTabFilter(keeping, null, null, Names).Shows) == 4, "the other tab keeps all four");
            Check(store.After(0).Count(new ChatTabFilter(hiding, null, null, new HashSet<string>()).Shows) == 4, "the names not read: nothing hidden");
        }),
        ("Chat travel 1777: the names read once from the Action sheet's rows; the tab's setting with its tooltip; the view's filter takes the names read", () =>
        {
            var plugin = Read("src/TenshoAssistant/Plugin.cs");
            Check(plugin.Contains("Chat.ChatTravel.Names=Chat.ChatTravel.From(Chat.ChatTravel.Actions.Select(id=>data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRowOrDefault(id)?.Name.ExtractText()));", StringComparison.Ordinal), "the names in the client's language");
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("CheckSetting(\"ChatWindow.HideOthersTravel\",\"ChatWindow.HideOthersTravelTip\",()=>tab.HideOthersTravel,value=>{tab.HideOthersTravel=value;ChatTabChanged(tab);});", StringComparison.Ordinal), "the tab's setting, the tab built again when changed");
            var filters = Read("src/TenshoAssistant/Chat/ChatFilters.cs");
            Check(filters.Contains("this.travel=travel ?? ChatTravel.Names;", StringComparison.Ordinal) && filters.Contains("if(tab.HideOthersTravel && ChatTravel.ByOthers(record,travel))return false;", StringComparison.Ordinal), "the filter");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                Check(text.Contains("\"HideOthersTravel\": ", StringComparison.Ordinal) && text.Contains("\"HideOthersTravelTip\": ", StringComparison.Ordinal), "both texts in " + language);
            }
            Check(SettingsIndexHas("ChatWindow.HideOthersTravel"), "in the settings' search");
        }),
    ];

    private static bool SettingsIndexHas(string key) => Read("src/TenshoAssistant/SettingsIndex.cs").Contains($"(\"{key}\",\"Panels\")", StringComparison.Ordinal);
}
