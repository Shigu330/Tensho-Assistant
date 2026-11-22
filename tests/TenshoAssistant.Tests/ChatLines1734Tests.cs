using TenshoAssistant.Chat;

// X-733 and X-734 (0.1734, T-0.1715-1 and T-0.1720-1 seen in game on 21/11): an action named in a combat line's plain text, where the game's
// template puts it (docs/evidence/0.1734-chat-action-lines.md), and a meal's bonuses in its status tooltip.
internal static class ChatLines1734Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");
    private static ChatPayload Text(string text) => new(ChatPayloadKind.Text, "TextPayload", Text: text);
    private static ChatPayload Other(string name) => new(ChatPayloadKind.Other, name);
    private static readonly ChatPayload End = new(ChatPayloadKind.LinkEnd, "LinkEnd");
    private static string Shape(IEnumerable<ChatSegment> body) => string.Join("|", body.Select(segment => $"{segment.Kind}:{segment.Text}"));

    // The game's templates of 21/11 (LogMessage, macro strings as the client gives them; the subject's macro shortened).
    private const string Subject = "<head(<if([gstr1==gstr2],vous,<string(gstr2)>)>)>";
    private static readonly (int, string)[] French =
    [
        (43, Subject + " <if([gstr1==gstr2],utilisez,utilise)> <string(lstr1)>."),
        (43, Subject + " <if([gstr1==gstr2],préparez,prépare)> <string(lstr1)>."),
        (43, Subject + " <if([gstr1==gstr2],montez sur votre <frnoun(Mount,12,lnum1,1,1)>,monte sur <frnoun(Mount,5,lnum1,1,1)>)>."),
        (44, Subject + " <if([gstr1==gstr2],utilisez,utilise)> <frnoun(Item,1,lnum1,1,1)>."),
        (45, Subject + " <if([gstr1==gstr3],récupérez,récupère)> <kilo(lnum2,<nbsp>)> PV grâce à <string(lstr1)>."),
        (41, "L'action \"<string(lstr1)>\" que <if(gnum8,<sheet(ObjStr,gnum8,0)>,gstr3)> se préparait à lancer a été interrompue."),
    ];

    private static ChatActionLines Lines(bool spaced = true) => new(French, ["Téléportation", "Vous", "Science", "Science des minéraux", "Soin", "Sprint", "portation", "Chocobo destrier"], spaced);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat lines 1734: what follows an action's name in the game's templates (French, English, German of 21/11)", () =>
        {
            Check(ChatActionLines.End(French[0].Item2) == ".", "« … utilisez <string(lstr1)>. »: the full stop");
            Check(ChatActionLines.End("<head(Your)> <string(lstr1)> is interrupted.") == " is interrupted.", "English 540");
            Check(ChatActionLines.End("<head(du)> setzt <string(lstr1)> ein.") == " ein.", "German 533: the name is not last");
            Check(ChatActionLines.End("<string(lstr1)> <if([gstr1==gstr2],von dir,von x)> wurde unterbrochen.") is null, "a macro after the name: no end (never a guess)");
            Check(ChatActionLines.End(French[3].Item2) is null && ChatActionLines.End("Vous lancez <string(lstr1)>") is null, "no action's string, or the name ending the template: none");
            Check(ChatActionLines.End("<string(lstr1)><nbsp>\\(x\\)!") == " (x)!", "a non-breaking space, the game's escapes");
            Check(Lines().Kinds == 2, "kinds 43 and 45 end with a name here (41 puts quotes and a macro after it)");
        }),
        ("Chat lines 1734: the action a line names: before its template's end, the longest name starting a word", () =>
        {
            var lines = Lines();
            Check(lines.Find(43, "Vous utilisez Téléportation.") == (14, 13), "the line of T-0.1720-1: « Téléportation »");
            Check(lines.Find(43, " utilise Téléportation.") == (9, 13), "another player's (after their link)");
            Check(lines.Find(43, "Vous utilisez Science des minéraux.") == (14, 20), "the longest name, not « Science »");
            Check(lines.Find(45, " récupère 1 234 PV grâce à Soin.") == (27, 4), "a heal's line names its action too");
            Check(lines.Find(43, "Vous montez sur votre chocobo destrier.") is null, "a mount (no name of an action: capitals count)");
            Check(lines.Find(44, "Vous utilisez Soin.") is null, "kind 44 names an item: no template with an action there");
            Check(lines.Find(43, "Vous utilisez Téléportation") is null && lines.Find(43, "Soin! Vous utilisez") is null, "a line without the template's end: none");
            Check(lines.Find(43, "Vous utilisez Téléportation.") is { } whole && whole.Length != 9, "never « portation » inside a word");
            Check(Lines(spaced: false).Find(43, "Xportation.") == (1, 9), "a language without spaces: a name anywhere");
            Check(new ChatActionLines(French, [], true).Find(43, "Vous utilisez Téléportation.") is null, "no names read: none");
        }),
        ("Chat lines 1734: the pieces of the lines of 21/11: the action's name in the last text outside a link, a coloured item stays text", () =>
        {
            var lines = Lines();
            Func<string, (int, int)?> finder = text => lines.Find(43, text);
            var own = ChatPayloads.Segments([Text("Vous utilisez Téléportation.")], finder);
            Check(Shape(own) == "Text:Vous utilisez |ActionName:Téléportation|Text:.", "your line (kind 43, TextPayload of 28 characters): " + Shape(own));
            Check(own[1].Link is { Kind: ChatSegmentKind.ActionName, Action: "Téléportation" }, "its name kept for the tooltip");
            var other = ChatPayloads.Segments([Other("PlayerPayload"), Text("Ann Doe"), End, Text(" utilise Téléportation.")], finder);
            Check(Shape(other) == "Text:Ann Doe|Text: utilise |ActionName:Téléportation|Text:.", "a player's line: " + Shape(other));
            var status = new ChatPayload(ChatPayloadKind.Status, "Status", Link: new ChatLink(ChatSegmentKind.StatusLink, StatusId: 225));
            var linked = ChatPayloads.Segments([Text("Vous utilisez "), status, Text("Sprint."), End], finder);
            Check(!linked.Any(segment => segment.Kind == ChatSegmentKind.ActionName), "a text inside a link is never an action: " + Shape(linked));
            var on = new ChatPayload(ChatPayloadKind.Colour, "UIForegroundPayload", Size: 500);
            var off = new ChatPayload(ChatPayloadKind.Colour, "UIForegroundPayload");
            var item = ChatPayloads.Segments([Text("Vous utilisez "), on, Other("UIGlowPayload"), Text("Soin"), Other("UIGlowPayload"), off, Text(".")], text => lines.Find(44, text));
            Check(Shape(item) == "Text:Vous utilisez |Text:Soin|Text:.", "kind 44: the coloured text is an item's name (LogMessage 535): text: " + Shape(item));
            Check(Shape(ChatPayloads.Segments([Text("Vous utilisez Téléportation.")])) == "Text:Vous utilisez Téléportation.", "no finder (not a combat line): text");
            Check(ChatPayloads.LastPlainText([Text("a"), status, Text("in"), End, Text(".")]) == 4 && ChatPayloads.LastPlainText([status, Text("in")]) == -1, "the last text outside a link");
        }),
        ("Chat lines 1734: a player's line takes the action a job learns or a general action; never a copy that differs", () =>
        {
            var tips = ChatActionTips.Build([
                new(120, "Soin", 405, true, false, "Restaure les PV.", 53), new(8895, "Soin", 784, true, false, "Autre.", 0),
                new(3, "Sprint", 104, false, false, "Vous augmentez votre vitesse.", 0, true), new(16933, "Sprint", 405, false, false, ""),
                new(5, "Téléportation", 111, false, false, "Vous utilisez des gils…", 0, true),
                new(163, "Ruine", 501, true, false, "A", 69), new(17869, "Ruine", 501, true, false, "A", 29),
                new(167, "Aspiration", 514, true, false, "B", 29), new(16508, "Aspiration", 514, true, false, "C", 69),
                new(1, "Feu", 10, true, false, "Deals fire damage."), new(2, "Feu", 99, false, false, ""),
            ]);
            Check(ChatActionTips.Find(tips, "Soin", true)?.Icon == 405 && ChatActionTips.Find(tips, "Soin", false) is null, "« Soin » (probe of 21/11: the job's row 120 and a copy, icon 784): the job's for a player, none for others");
            Check(ChatActionTips.Find(tips, "Sprint", true)?.Description == "Vous augmentez votre vitesse." && ChatActionTips.Find(tips, "Sprint", false) is null, "Sprint, a general action: the player's");
            Check(ChatActionTips.Find(tips, "Téléportation", true)?.Icon == 111 && ChatActionTips.Find(tips, "Téléportation", false)?.Icon == 111, "Teleport: one row");
            Check(ChatActionTips.Find(tips, "Ruine", true)?.Icon == 501 && ChatActionTips.Find(tips, "Aspiration", true) is null, "two jobs' rows that agree: shown; that differ: none");
            Check(ChatActionTips.Find(tips, "Feu", true)?.Icon == 10, "no learned row: the player's actions, as in 0.1720");
        }),
        ("Chat lines 1734: a meal's tooltip: the experience line, then each bonus in the game's words, HQ values for an HQ item", () =>
        {
            Check(ChatFoodTip.Holds(48) && ChatFoodTip.Holds(49) && !ChatFoodTip.Holds(50) && !ChatFoodTip.Holds(0), "Well Fed and Medicated only");
            Check(ChatFoodTip.Row(10123) == (123u, true) && ChatFoodTip.Row(123) == (123u, false) && ChatFoodTip.Row(10000) == (0u, true), "the Param: the ItemFood row, plus 10000 for HQ");
            string Exp(int percent) => $"Effets du repas (exp +{percent}%)";
            string Bonus(bool relative, int value, int max) => relative ? $"+{value}% (max {max})" : $"+{value}";
            ChatFoodBonus[] bonuses = [new("Collecte", true, 6, 90, 7, 95), new("Savoir-faire", true, 3, 45, 3, 49), new("", true, 1, 1, 1, 1), new("PV", false, 10, 0, 12, 0)];
            var hq = ChatFoodTip.Lines(48, true, 3, bonuses, Exp, Bonus);
            Check(string.Join("/", hq) == "Effets du repas (exp +3%)/Collecte +7% (max 95)/Savoir-faire +3% (max 49)/PV +12", "the user's capture of 21/11 (HQ): " + string.Join("/", hq));
            Check(ChatFoodTip.Lines(48, false, 3, bonuses, Exp, Bonus)[1] == "Collecte +6% (max 90)", "NQ: its own values");
            Check(ChatFoodTip.Lines(49, false, 0, bonuses, Exp, Bonus) is { Count: 3 } medicine && medicine[0].StartsWith("Collecte", StringComparison.Ordinal), "a medicine: no meal's line");
            Check(ChatFoodTip.Lines(48, false, 3, [new("Contrôle", false, -3, 0, -6, 0)], Exp, Bonus).Count == 0, "a negative value (rows 400 and 402): no line, the description stays");
        }),
        ("Chat lines 1734: the meal's Param kept with its link (one box for the pieces of one link); the history keeps the status as text", () =>
        {
            var box = new ChatStatusParam();
            var link = new ChatLink(ChatSegmentKind.StatusLink, StatusId: 48) with { Param = box };
            Check(link.Param is { Value: 0 }, "0 until read");
            box.Value = 10123;
            Check(link.Param?.Value == 10123, "read later (frames after the line), seen by the link already drawn");
            var record = new ChatRecord(1, DateTimeOffset.FromUnixTimeMilliseconds(1_763_700_000_000), 46, ChatChannels.From(46), "", [new ChatSegment("Repu", ChatSegmentKind.StatusLink, link)]);
            var back = ChatHistory.Decode(ChatHistory.Encode(record), 7);
            Check(back is not null && back.Body.All(segment => segment.Link is null) && back.Body[0].Text == "Repu", "the history: text");
        }),
        ("Chat lines 1734: wiring: names and templates read once, at the first message; the meal's status read at its line and after; the game's words", () =>
        {
            var reader = Read("Chat/ChatReader.Dalamud.cs");
            Check(reader.Contains("lineData?.Prepare();", StringComparison.Ordinal)
                && reader.Contains("var body=ChatPayloads.Segments(payloads,lineData is { } named && ChatChannels.From(logKind)==ChatChannel.Combat ? text=>named.ActionIn(logKind,text):null);", StringComparison.Ordinal)
                && reader.Contains("if(lineData is not null)body=WithStatusParams(body,message,lineData);", StringComparison.Ordinal), "the reader: combat lines only, the statuses");
            Check(reader.Contains("if(player is null && (byte)message.SourceKind!=1 && (byte)message.TargetKind!=1)return body;", StringComparison.Ordinal), "the one who gained it: the linked player, else you only when the line is yours");
            var players = Read("Chat/ChatPlayerActions.Dalamud.cs");
            Check(players.Contains("public void Prepare()=>actionTips??=Task.Run(ActionTips);", StringComparison.Ordinal)
                && players.Contains("ChatChannels.From((int)row.LogKind.RowId)==ChatChannel.Combat", StringComparison.Ordinal)
                && players.Contains("data.Language!=Dalamud.Game.ClientLanguage.Japanese", StringComparison.Ordinal), "the templates of the combat kinds, the client's language");
            Check(players.Contains("framework.RunOnTick(()=>ReadStatus(param,status,name,world,frame+1,atLine,lineAt,fresh),delayTicks:1);", StringComparison.Ordinal)
                && players.Contains("if(disposed || statusFailed || TenshoAssistant.Competitive.CompetitiveGate.Cut || !client.IsLoggedIn)return;", StringComparison.Ordinal), "read again in the next frames; never after unload, during a cut or logged out");
            Check(players.Contains("strings.EvaluateFromAddon(1017,", StringComparison.Ordinal) && players.Contains("strings.EvaluateFromAddon(1016,", StringComparison.Ordinal), "the game's words, evaluated by Dalamud");
            var window = Read("MainWindow.ChatWindow.cs");
            Check(window.Contains("DrawChatStatusTip(statusLink.StatusId,statusLink.Param);", StringComparison.Ordinal)
                && window.Contains("param is { Value:>0 } read ? ChatPlayers.FoodLines(id,read.Value):null", StringComparison.Ordinal), "the tooltip: the bonuses once read");
            var plugin = Read("Plugin.cs");
            Check(plugin.Contains("ISeStringEvaluator seStrings,IAddonLifecycle addonLifecycle)", StringComparison.Ordinal) && Read("Plugin.Chat.cs").Contains("chatReader.LineData=()=>mainWindow.ChatPlayers;", StringComparison.Ordinal), "the plugin's services");
        }),
    ];
}
