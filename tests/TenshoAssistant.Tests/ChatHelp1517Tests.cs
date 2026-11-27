using System.Text.Json;
using TenshoAssistant.Chat;

// 0.1517 (X-545, the user's request of 17/11: « the tooltip must be a consistent explanation of the channel »): each channel box and each game
// filter of the chat settings has its own explanation, drawn from the game's help (TextCommand, LogFilter; docs/evidence/0.1517-chat-channel-help.md).
internal static class ChatHelp1517Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static Dictionary<string, string> Flat(string code)
    {
        var values = new Dictionary<string, string>();
        void Walk(JsonElement node, string prefix)
        {
            foreach (var property in node.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Object) Walk(property.Value, prefix + property.Name + ".");
                else if (property.Value.ValueKind == JsonValueKind.String) values[prefix + property.Name] = property.Value.GetString()!;
            }
        }
        using var document = JsonDocument.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{code}.json")));
        Walk(document.RootElement, "");
        return values;
    }

    private static string Text(Dictionary<string, string> values, IEnumerable<(string Key, object[] Args)> lines)
        => string.Join("\n", lines.Select(line =>
        {
            Check(values.TryGetValue(line.Key, out var value), $"key « {line.Key} » missing");
            return line.Args.Length == 0 ? value! : string.Format(value!, line.Args);
        }));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat help 1517: every channel has its own explanation in en and fr, full name included, none cut nor shared", () =>
        {
            foreach (var code in new[] { "en", "fr" })
            {
                var values = Flat(code);
                var seen = new Dictionary<string, ChatChannel>();
                foreach (var channel in ChatChannels.All)
                {
                    var explanation = Text(values, [ChatHelp.Explanation(channel)]);
                    Check(!seen.TryGetValue(explanation, out var other), $"{code}: {channel} and {other} share « {explanation} »");
                    seen[explanation] = channel;
                    Check(explanation.Length >= 40 && !explanation.Contains('{') && !explanation.TrimEnd().EndsWith('…'),
                        $"{code}: {channel}: explanation too short, unformatted or cut: « {explanation} »");
                    var tip = Text(values, ChatHelp.ChannelTip(channel));
                    var name = values["ChatChannels." + channel];
                    Check(tip.StartsWith(name + "\n", StringComparison.Ordinal), $"{code}: {channel}: the tooltip does not start with the full name « {name} »");
                    Check(tip.Contains(explanation, StringComparison.Ordinal) && tip.EndsWith(values["ChatWindow.ChannelBoxTip"], StringComparison.Ordinal),
                        $"{code}: {channel}: the tooltip lacks its explanation or what ticking does");
                }
            }
        }),
        ("Chat help 1517: a numbered linkshell names its own number and its own command", () =>
        {
            var en = Flat("en");
            var fr = Flat("fr");
            for (var slot = 1; slot <= 8; slot++)
            {
                var ls = Text(en, [ChatHelp.Explanation(ChatChannel.Ls1 + slot - 1)]);
                var cw = Text(en, [ChatHelp.Explanation(ChatChannel.Cwls1 + slot - 1)]);
                Check(ls.Contains($"/linkshell{slot} (/l{slot})") && ls.Contains($"[{slot}]"), $"en Linkshell [{slot}]: « {ls} »");
                Check(cw.Contains($"/cwlinkshell{slot} (/cwl{slot})") && cw.Contains($"[CWLS{slot}]"), $"en Cross-world Linkshell [{slot}]: « {cw} »");
                var cwFr = Text(fr, [ChatHelp.Explanation(ChatChannel.Cwls1 + slot - 1)]);
                Check(cwFr.Contains($"/linkshellim{slot} (/lsim{slot})") && cwFr.Contains($"[LSIM {slot}]"), $"fr Linkshell inter-Monde [{slot}]: « {cwFr} »");
            }
            Check(Text(en, [ChatHelp.Explanation(ChatChannel.Echo)]).Contains("only you"), "Echo: only you see it");
            Check(Text(fr, [ChatHelp.Explanation(ChatChannel.Novice)]).Contains("entraide"), "Novice: help between new players and mentors");
        }),
        ("Chat help 1517: a game filter's tooltip gives its name, the game's example and its default tabs", () =>
        {
            foreach (var code in new[] { "en", "fr" })
            {
                var values = Flat(code);
                string Get(string key) => values[key];
                var say = new ChatFilterRow(1, 10, 65535, 65535, 1, 1, 1, "Say", "Cloud Strife: Never mind...");
                var dialogue = new ChatFilterRow(26, 61, 65535, 65535, 2, 16, 4, "NPC Dialogue", "Baderon: Now off with ye.");
                var others = new ChatFilterRow(91, 41, 16, 65535, 7, 1, 0, "Damage dealt by others.");
                var both = new ChatFilterRow(37, 58, 1807, 65535, 2, 2, 3, "Own Battle System Messages", "You defeat the Shadow Lord.");
                var sayTip = Text(values, ChatHelp.FilterTip(say, Get));
                var dialogueTip = Text(values, ChatHelp.FilterTip(dialogue, Get));
                var othersTip = Text(values, ChatHelp.FilterTip(others, Get));
                var bothTip = Text(values, ChatHelp.FilterTip(both, Get));
                Check(sayTip.Contains("« Say »") && sayTip.Contains("Cloud Strife: Never mind...") && sayTip.Contains(values["ChatWindow.TabAll"]),
                    $"{code}: Say: « {sayTip} »");
                Check(dialogueTip.Contains(values["ChatWindow.TabEvent"]) && !dialogueTip.Contains(values["ChatWindow.TabAll"] + ","),
                    $"{code}: NPC Dialogue in the Event tab only: « {dialogueTip} »");
                Check(othersTip.Contains(values["ChatWindow.FilterTipNoTab"]) && !othersTip.Contains(Get("ChatWindow.FilterTipExample").Split('{')[0]),
                    $"{code}: a filter without example nor tab: « {othersTip} »");
                Check(bothTip.Contains(values["ChatWindow.TabAll"] + ", " + values["ChatWindow.TabBattle"]), $"{code}: two default tabs: « {bothTip} »");
                Check(new[] { sayTip, dialogueTip, othersTip, bothTip }.Distinct().Count() == 4, $"{code}: two filters share a tooltip");
            }
        }),
        ("Chat help 1517: the settings use the channel's and the filter's own tooltip, not a shared sentence", () =>
        {
            var code = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(code.Contains("if(TipHovered())TipText(ChatHelpText(Chat.ChatHelp.ChannelTip(channel)));")
                  && code.Contains("if(TipHovered())TipText(ChatHelpText(Chat.ChatHelp.FilterTip(row,localization.Get)));"), "the chat settings' boxes use ChatHelp");
            Check(!code.Contains("Tip(\"ChatWindow.ChannelBoxTip\")") && !code.Contains("Tip(\"ChatWindow.FilterTip\")"), "a shared tooltip is still drawn");
        }),
    ];
}
