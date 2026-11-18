using TenshoAssistant.Chat;

// 0.1521 (X-554 and X-555, review of 0.1520): the alert words never fire on what you say yourself (any channel) nor on the combat lines (DEC-053).
internal static class ChatAlertsOwn1521Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static readonly DateTimeOffset Start = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat alerts own 1521: what you say yourself is marked as yours (the game gives no subject) and never alerts, on every spoken channel", () =>
        {
            var words = ChatTextFilter.Words("raid");
            const string own = "Ann Doe";
            foreach (var (kind, channel) in new[] { (10, ChatChannel.Say), (11, ChatChannel.Shout), (30, ChatChannel.Yell), (14, ChatChannel.Party), (24, ChatChannel.FreeCompany), (16, ChatChannel.Ls1), (37, ChatChannel.Cwls1), (28, ChatChannel.Emote) })
            {
                Check(ChatChannels.From(kind) == channel, $"kind {kind}");
                var source = ChatAlert.Source(channel, 0, own, own);
                var said = new ChatRecord(1, Start, kind, channel, own, [new ChatSegment("raid tonight")], source);
                Check(source == ChatAlert.You && ChatAlert.Sent(said) && !ChatAlert.Holds(said, words), $"your own {channel} line does not alert");
                var other = said with { Sender = "Cloud Strife", Source = ChatAlert.Source(channel, 0, "Cloud Strife", own) };
                Check(other.Source == 0 && ChatAlert.Holds(other, words), $"another player's {channel} line alerts");
            }
            // The party number in front of the name (a glyph of the game's font), capitals ignored.
            Check(ChatAlert.Source(ChatChannel.Party, 0, "ann doe", own) == ChatAlert.You, "party line with its number");
            // Someone of another world with the same name, an unknown name, a subject the game gave, a system line: unchanged.
            Check(ChatAlert.Source(ChatChannel.Shout, 0, "Ann Doe@Ragnarok", own) == 0, "another world");
            Check(ChatAlert.Source(ChatChannel.Say, 0, own, "") == 0 && ChatAlert.Source(ChatChannel.Say, 0, own, null) == 0, "name not known");
            Check(ChatAlert.Source(ChatChannel.Say, 4, own, own) == 4, "a subject given by the game is kept");
            Check(ChatAlert.Source(ChatChannel.System, 0, own, own) == 0, "a system line is not spoken");
        }),
        ("Chat alerts own 1521: a combat line holding an alert word is never read for it: no band, no « ! » on the Combat tab", () =>
        {
            var words = ChatTextFilter.Words("raid, Shadow");
            var hit = new ChatRecord(1, Start, 41, ChatChannel.Combat, "", [new ChatSegment("The Shadow Lord hits you for 9999 damage.")], 32, 2);
            Check(ChatChannels.From(41) == ChatChannel.Combat && !ChatAlert.Holds(hit, words), "a combat line does not alert");
            var store = new ChatStore();
            var tab = new ChatTabSettings { Name = "Combat", Channels = [ChatChannel.Combat, ChatChannel.Say] };
            var settings = new ChatSettings { AlertWords = "raid, shadow" };
            var view = new ChatTabView();
            view.Refresh(store, tab, 100, null, settings);
            store.Add(Start, 41, "", [new ChatSegment("The Shadow Lord hits you for 9999 damage.")], 100, 32, 2);
            store.Add(Start.AddSeconds(1), 10, "Ann Doe", [new ChatSegment("raid now")], 100, ChatAlert.You);
            store.Add(Start.AddSeconds(2), 10, "Cloud Strife", [new ChatSegment("raid now")], 100);
            view.Refresh(store, tab, 100, null, settings);
            Check(view.Alerts == 1, $"only the other player's line alerts: {view.Alerts}");
            Check(view.Unread == 1, $"your own line and the combat line are not unread (X-559, DEC-053), the other player's is: {view.Unread}");
        }),
        ("Chat alerts own 1521: the reader marks your lines with your character's name; the tooltip says it", () =>
        {
            var reader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/ChatReader.Dalamud.cs"));
            Check(reader.Contains("var source=ChatAlert.Source(ChatChannels.From((int)message.LogKind),(byte)message.SourceKind,sender,ownName?.Invoke());")
                  && reader.Contains("current.BoundedMaxLines,source,(byte)message.TargetKind);"), "the reader keeps the subject it marked");
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Chat.cs"));
            Check(plugin.Contains("history,()=>player.CharacterName);"), "the plugin passes the character's name");
            var en = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json"));
            var fr = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json"));
            Check(en.Contains("Your own messages and the combat lines never alert.") && fr.Contains("Vos propres messages et les lignes de combat n'alertent jamais."), "tooltip");
        }),
    ];
}
