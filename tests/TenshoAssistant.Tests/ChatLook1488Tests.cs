using TenshoAssistant.Chat;

// 0.1488 (X-505, the user's captures of 11/16: Tensho's chat window and the game's side by side, « ours is still too visible »): each
// difference seen is a setting with the game's value by default: tabs under the lines, a background nearly invisible, a thin scroll bar, the
// hour without a leading zero ([9:21]), links with the game's orange arrow and not underlined, rounded tabs with a dot on the one shown, and
// wrapped lines indented. A configuration saved before gets the new look once (the background only when it was still the old default).
internal static class ChatLook1488Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static readonly ChatLink Potion = new(ChatSegmentKind.ItemLink, ItemId: 4554);
    private static readonly DateTimeOffset Morning = new(2026, 11, 16, 9, 21, 0, TimeSpan.Zero);

    private static List<ChatRun> Runs(ChatSettings settings, List<ChatPiece> pieces)
        => [.. ChatFormat.Runs(new ChatRecord(1, Morning, 10, ChatChannel.Say, "Ann", ChatPieces.Segments(pieces)), settings, _ => "", _ => "Hi-Potion")];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat look 1488: by default the window looks like the game's chat (each difference a setting, the game's value)", () =>
        {
            var settings = new ChatSettings();
            Check(settings.TabsAtBottom, "tabs under the lines");
            Check(settings.BoundedOpacity == 15 && ChatSettings.DefaultOpacity == 15, "a background nearly invisible: " + settings.BoundedOpacity);
            Check(settings.ThinScrollbar && settings.GameLinks && settings.GameTabs && settings.WrapIndent && !settings.HourLeadingZero, "thin bar, game links, game tabs, indent, no leading zero");
            Check(settings.TextOutline, "the text keeps its outline (readable on the light background)");
            Check(Math.Abs(ChatLook.ScrollbarThin - 5f) < 0.01f, "a thin bar (5 px): " + ChatLook.ScrollbarThin);
        }),
        ("Chat look 1488: the hour as the game writes it ([9:21]), with a leading zero on request; 12-hour clock unchanged", () =>
        {
            var local = Morning.ToLocalTime();
            var game = ChatFormat.Time(Morning, new ChatSettings());
            Check(game == local.ToString("H:mm", System.Globalization.CultureInfo.InvariantCulture), "the game's format: " + game);
            var nine = new DateTimeOffset(local.Year, local.Month, local.Day, 9, 5, 0, local.Offset);
            Check(ChatFormat.Time(nine, new ChatSettings()) == "9:05", "no zero before the hour: " + ChatFormat.Time(nine, new ChatSettings()));
            Check(ChatFormat.Time(nine, new ChatSettings { HourLeadingZero = true }) == "09:05", "with a zero on request");
            Check(ChatFormat.Time(nine, new ChatSettings { Clock24 = false }).StartsWith("9:05", StringComparison.Ordinal), "12-hour clock: « 9:05 AM »");
            var record = new ChatRecord(1, nine, 10, ChatChannel.Say, "Ann", [new ChatSegment("hi")]);
            Check(string.Concat(ChatFormat.Runs(record, new ChatSettings { ShowChannel = false }, _ => "").Select(run => run.Text)) == "[9:05] Ann: hi", "in brackets, as the game's chat");
            Check(ChatFormat.LayoutKey(new ChatSettings()) != ChatFormat.LayoutKey(new ChatSettings { HourLeadingZero = true }), "a change lays the lines out again");
        }),
        ("Chat look 1488: a link as the game's: the arrow where the game's glyph is (its own run, part of the link), the name after it, not joined", () =>
        {
            var settings = new ChatSettings { ShowTimestamps = false, ShowChannel = false };
            var runs = Runs(settings, [new("test "), new(null, Potion), new(ChatGlyphs.LinkMarker.ToString()), new("Hi-Potion"), new(null, End: true), new(" test")]);
            Check(string.Concat(runs.Select(run => run.Text)) == "Ann: test →Hi-Potion test", "the message: " + string.Concat(runs.Select(run => run.Text)));
            Check(runs.Single(run => run.Role == ChatRole.LinkMark) is { Text: "→" } mark && mark.Link == Potion, "the arrow, clickable as the link");
            Check(runs.Single(run => run.Role == ChatRole.Link) is { Text: "Hi-Potion" } name && name.Link == Potion, "the name, the link");
            var wrapped = ChatFormat.Wrap(runs, 1000, text => text.Length * 7f).SelectMany(line => line).ToList();
            Check(wrapped.Count(run => run.Link == Potion) == 2, "laid out, the arrow and the name stay two runs (drawn in two colours)");
            var old = Runs(new ChatSettings { ShowTimestamps = false, ShowChannel = false, GameLinks = false }, [new(null, Potion), new(ChatGlyphs.LinkMarker.ToString()), new("Hi-Potion"), new(null, End: true)]);
            Check(!old.Any(run => run.Role == ChatRole.LinkMark) && old.Single(run => run.Role == ChatRole.Link).Text == "Hi-Potion", "setting off: no arrow, as before");
            var web = Runs(settings, [new("see https://example.org now")]);
            Check(!web.Any(run => run.Role == ChatRole.LinkMark), "a web address (no glyph from the game) gets no arrow");
            var bare = Runs(settings, [new("go "), new(null, Potion), new(ChatGlyphs.LinkMarker.ToString()), new(null, End: true), new(" now")]);
            Check(string.Concat(bare.Select(run => run.Text)) == "Ann: go → now" && bare.Single(run => run.Link == Potion).Role == ChatRole.LinkMark, "the glyph only (X-506): the arrow alone");
        }),
        ("Chat look 1488: wrapped lines indented, narrower by the indent; the first line keeps the whole width", () =>
        {
            var runs = new List<ChatRun> { new(string.Join(" ", Enumerable.Repeat("word", 30)), ChatRole.Body) };
            static float Measure(string text) => text.Length * 10f;
            var flat = ChatFormat.Wrap(runs, 200, Measure);
            var indented = ChatFormat.Wrap(runs, 200, Measure, indent: 40);
            Check(flat[0].Sum(run => Measure(run.Text.TrimEnd())) <= 200 && indented[0].Sum(run => Measure(run.Text.TrimEnd())) > 160, "the first line, whole width");
            Check(indented.Skip(1).All(line => line.Sum(run => Measure(run.Text.TrimEnd())) <= 160), "the next lines, 40 narrower");
            Check(indented.Count >= flat.Count, "as many lines or more");
            Check(string.Concat(indented.SelectMany(line => line).Select(run => run.Text)) == runs[0].Text, "nothing lost");
            Check(ChatLook.Indent(16, true) == 16 && ChatLook.Indent(16, false) == 0, "about one letter, or nothing");
            var narrow = ChatFormat.Wrap(runs, 60, Measure, indent: 500);
            Check(narrow.Count > 1 && string.Concat(narrow.SelectMany(line => line).Select(run => run.Text)) == runs[0].Text, "an indent wider than the window keeps half of it");
        }),
        ("Chat look 1488: the game's look by default; a configuration saved keeps its own look (X-574: no upgrade of settings saved before 0.1488)", () =>
        {
            var fresh = new ChatSettings();
            Check(fresh.BoundedOpacity == 15 && fresh.TabsAtBottom, "a new player: the light background, the tabs under");
            var saved = new ChatSettings { Opacity = 40, TabsAtBottom = false };
            saved.EnsureTabs("General", "Battle", "Event", "Social");
            Check(saved.BoundedOpacity == 40 && !saved.TabsAtBottom, "reading the tabs changes nothing of the saved look");
            var chosen = new ChatSettings { Opacity = 70 };
            Check(chosen.BoundedOpacity == 70, "a background the player chose is kept");
            Check(!typeof(ChatSettings).GetProperties().Any(property => property.Name is "LookVersion" or "TabsVersion"), "no upgrade marker left in the settings");
        }),
        ("Chat look 1488: the game's tabs: the one shown marked by a dot (a letter of the game font), the others as they are", () =>
        {
            Check(ChatLook.TabLabel("General", true, true) == "· General" && ChatLook.TabLabel("Battle", false, true) == "Battle", "the dot on the tab shown");
            Check(ChatLook.TabLabel("General", true, false) == "General", "setting off: no dot");
        }),
        ("Chat look 1488: the window uses the settings (thin bar pushed around the lines' area, game tabs, links without underline, indent)", () =>
        {
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            var thin = window.IndexOf("ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize,ChatLook.ScrollbarThin);", StringComparison.Ordinal);
            var area = window.IndexOf("try{visible=ImGui.BeginChild(ChatFormat.LinesId(tab)", StringComparison.Ordinal);
            Check(thin > 0 && area > thin && window.Contains("if(thin){ImGui.PopStyleColor(4);ImGui.PopStyleVar();}}", StringComparison.Ordinal), "the bar's size and colours around the area's start");
            Check(window.Contains("ImGui.PushStyleVar(ImGuiStyleVar.TabRounding,ChatLook.TabRounding);", StringComparison.Ordinal)
                  && window.Contains("ChatLook.TabLabel(ChatAlert.Mark(ChatFormat.TabLabel(ChatConversations.Label(tab,options.CaptureNames),tab.ShowUnread,view.Unread),view.Alerts),index==chatTabShown,options.GameTabs)", StringComparison.Ordinal), "the game's tabs");
            // 0.1491 (X-510, review of 0.1490): with the game's look, a link without the game's arrow is underlined too (ChatReview1491Tests).
            Check(window.Contains("if(underline)\n        {\n            var min=ImGui.GetItemRectMin();", StringComparison.Ordinal), "underlined without the game's look, or a link without the game's arrow");
            Check(window.Contains("ChatLook.Indent(ImGui.GetFontSize(),options.WrapIndent),ChatLook.IconWidth(ImGui.GetTextLineHeight())));", StringComparison.Ordinal)
                  && window.Contains("if(line>0 && indent>0)ImGui.SetCursorPosX(left+indent);", StringComparison.Ordinal), "laid out and drawn with the same indent");
            foreach (var key in new[] { "HourLeadingZero", "ThinScrollbar", "GameLinks", "GameTabs", "WrapIndent" })
                Check(window.Contains($"BoolSetting(\"ChatWindow.{key}\",\"ChatWindow.{key}Tip\"", StringComparison.Ordinal), key + ": a setting");
        }),
    ];
}
