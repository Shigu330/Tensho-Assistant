using TenshoAssistant.Chat;

// 0.1688 (X-699, the user's request of 21/11: « is there really no way to keep the player's id for the right-click menu? the game's chat does
// it »): the probe (docs/evidence/0.1688-chat-player-menu-far.md) found no function of the game's library that opens the game's player menu
// for a player far away; the game's chat does it by an unnamed event of its interface, which Tensho does not forge. Near you, friend
// request, blacklist and report open the game's own menu of that character, where they are; far away, greyed with the reason.
internal static class ChatPlayerMenu1688Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");
    private static readonly ChatPlayerAction[] GameOnly = [ChatPlayerAction.Friend, ChatPlayerAction.Blacklist, ChatPlayerAction.Report];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat player menu 1688: behaviour: near you, friend request, blacklist and report are on and open the game's menu; far away, greyed with why", () =>
        {
            var known = new ChatSource("Aya Lune", 73, 1001, 2001);
            foreach (var channel in new[] { ChatChannel.Say, ChatChannel.Yell, ChatChannel.Shout, ChatChannel.TellIn, ChatChannel.Ls1 })
            {
                var near = ChatPlayerMenu.Entries(known, channel, true, false);
                Check(GameOnly.All(action => near.Single(entry => entry.Action == action) is { Enabled: true, Why: ChatPlayerWhy.None }), channel + ": near, on");
                Check(near.Any(entry => entry.Action == ChatPlayerAction.GameMenu && entry.Enabled), channel + ": « Game menu… » near");
                var far = ChatPlayerMenu.Entries(known, channel, false, false);
                Check(GameOnly.All(action => far.Single(entry => entry.Action == action) is { Enabled: false, Why: ChatPlayerWhy.GameOnly }), channel + ": far, greyed, the game only");
                Check(far.All(entry => entry.Action != ChatPlayerAction.GameMenu), channel + ": no « Game menu… » far");
            }
            // A name only (a message from the history): near, the game's menu needs no identifiers (the character is found by name and world).
            var named = ChatPlayerMenu.Entries(new ChatSource("Aya Lune", 73), ChatChannel.Say, true, false);
            Check(GameOnly.All(action => named.Single(entry => entry.Action == action).Enabled), "a name only, near: on");
            var none = ChatPlayerMenu.Entries(null, ChatChannel.Say, false, false);
            Check(GameOnly.All(action => none.Single(entry => entry.Action == action) is { Enabled: false, Why: ChatPlayerWhy.NoSender }), "no sender: greyed, no sender said");
            Check(ChatPlayerMenu.OpensGameMenu(ChatPlayerAction.GameMenu) && GameOnly.All(ChatPlayerMenu.OpensGameMenu)
                  && !new[] { ChatPlayerAction.Tell, ChatPlayerAction.Reply, ChatPlayerAction.Invite, ChatPlayerAction.Plate, ChatPlayerAction.Mute, ChatPlayerAction.Target, ChatPlayerAction.PartyFinder }.Any(ChatPlayerMenu.OpensGameMenu),
                "only those four open the game's menu");
        }),
        ("Chat player menu 1688: wiring: the click opens the game's menu of the character in range; the tooltip says so; nothing else changes", () =>
        {
            var actions = Read("src/TenshoAssistant/Chat/ChatPlayerActions.Dalamud.cs");
            Check(actions.Contains("            case ChatPlayerAction.GameMenu:\n            case ChatPlayerAction.Friend:\n            case ChatPlayerAction.Blacklist:\n            case ChatPlayerAction.Report:\n                if(InRange(source) is { } shown)AgentHUD.Instance()->OpenContextMenuFromTarget(", StringComparison.Ordinal),
                "the same call as « Game menu… », only for the character in range");
            Check(actions.Contains("if(failed || TenshoAssistant.Competitive.CompetitiveGate.Cut)return;", StringComparison.Ordinal) && actions.Contains("framework.RunOnTick(()=>", StringComparison.Ordinal),
                "on the click, at the next tick, never during a cut (as before)");
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("else if(entry.Enabled && entry.Action!=Chat.ChatPlayerAction.GameMenu && Chat.ChatPlayerMenu.OpensGameMenu(entry.Action))Tip(\"ChatWindow.PlayerByGameMenu\");", StringComparison.Ordinal), "the tooltip near you");
            foreach (var code in new[] { "en", "fr" })
            {
                var language = Read($"src/TenshoAssistant/Localization/Languages/{code}.json");
                Check(language.Contains("\"PlayerByGameMenu\": \"", StringComparison.Ordinal) && language.Contains("\"PlayerWhyGameOnly\": \"", StringComparison.Ordinal), code + ": both tooltips");
                Check(language.Contains("\"QuestionChatPlayerFar\": \"", StringComparison.Ordinal) && language.Contains("\"AnswerChatPlayerFar\": \"", StringComparison.Ordinal), code + ": the FAQ's question");
            }
            Check(Read("src/TenshoAssistant/Localization/Languages/fr.json").Contains("pour un joueur loin il ne les ouvre que depuis une ligne de son propre chat", StringComparison.Ordinal), "far away: the reason said");
            Check(TenshoAssistant.Help.Faq.Entries.Any(entry => entry.Key == "ChatPlayerFar"), "in the FAQ's list");
        }),
        ("Chat player menu 1688: the game's chat event is never forged: no call to the chat panel's link handler nor to the interface's external handler", () =>
        {
            var root = Path.GetFullPath("src");
            var forged = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal) && !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                .Where(path => { var text = File.ReadAllText(path); return text.Contains("HandleLinkClick", StringComparison.Ordinal) || text.Contains("AtkExternalInterface", StringComparison.Ordinal); })
                .Select(path => Path.GetRelativePath(root, path)).ToList();
            Check(forged.Count == 0, "the game's chat event is the game's: " + string.Join(", ", forged));
        }),
        ("Chat player menu 1688: the evidence says what was searched and why it cannot be done far away, in both halves", () =>
        {
            var evidence = Read("docs/evidence/0.1688-chat-player-menu-far.md");
            var halves = evidence.Split("\n---\n");
            Check(halves.Length == 2, "two halves");
            Check(halves[0].Contains("`LogViewer.HandleLinkClick(LinkData*)`", StringComparison.Ordinal) && halves[0].Contains("`AtkStage.AtkExternalInterface->CallHandler(handler 2, three values)`", StringComparison.Ordinal)
                  && halves[0].Contains("No function of the game's library opens the game's menu of a player from a name, a world or identifiers.", StringComparison.Ordinal), "English: the route and the conclusion");
            Check(halves[1].Contains("`LogViewer.HandleLinkClick(LinkData*)`", StringComparison.Ordinal) && halves[1].Contains("`AtkStage.AtkExternalInterface->CallHandler(gestionnaire 2, trois valeurs)`", StringComparison.Ordinal)
                  && halves[1].Contains("Aucune fonction de la bibliothèque du jeu n'ouvre le menu du jeu d'un joueur à partir d'un nom, d'un monde ou d'identifiants.", StringComparison.Ordinal), "French: the route and the conclusion");
            Check(Read("src/TenshoAssistant/Chat/ChatSources.cs").Contains("docs/evidence/0.1688-chat-player-menu-far.md", StringComparison.Ordinal), "cited by the code");
        }),
    ];
}
