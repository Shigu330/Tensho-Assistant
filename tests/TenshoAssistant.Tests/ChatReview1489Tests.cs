using TenshoAssistant.Chat;

// 0.1489 (X-504, review of 0.1486). M1: the outline is drawn in four copies that overlap; faded to 30 %, each copy was at 30 % of its own
// opacity and where the four met the halo was 69 % dark, darker than the text. Each copy is now chosen so that the copies together are faded
// as the text is. M2: the lines' area was named after the tab's place: after « Remove » a tab, the next ones took the area, and the scroll, of
// the tab before. It is now named after the tab's view (one per tab, its own number).
internal static class ChatReview1489Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    /// <summary>How dark the copies are together where all of them overlap, from the colour each is drawn with.</summary>
    private static double Overlap(uint colour) => ChatLook.Together((colour >> 24) / 255d);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat review 1489: faded to 30 %, the four copies of the outline together are no darker than 30 % of the outline at full (M1)", () =>
        {
            var full = Overlap(ChatLook.Outline(1f));
            // X-569 (0.1884): the same check is kept once, in ChatOutline1485Tests.
            foreach (var alpha in new[] { 0.3f, ChatCover.FadedAlpha, 0.5f, 0.8f })
            {
                var together = Overlap(ChatLook.Outline(alpha));
                Check(Math.Abs(together - alpha * full) <= 0.01, $"{alpha}: the copies together {together:0.000}, expected {alpha * full:0.000}");
                Check(together <= alpha + 0.005, $"{alpha}: the halo no darker than the text ({together:0.000})");
            }
            var before = ChatLook.Together(ChatLook.OutlineOpacity * 0.3);   // 0.1485: each copy at 30 % of its own opacity
            Check(before > 0.6 && Overlap(ChatLook.Outline(0.3f)) < 0.31, $"the case of the review: {before:0.00} before, now under 0.31");
            Check(ChatLook.Outline(0f) >> 24 == 0 && ChatLook.Outline(float.NaN) == ChatLook.OutlineColor && ChatLook.Outline(2f) == ChatLook.OutlineColor, "bounded");
            Check((ChatLook.Outline(0.3f) & 0x00FFFFFF) == (ChatLook.OutlineColor & 0x00FFFFFF), "still black");
            Check(Math.Abs(ChatLook.Copy(ChatLook.Together(0.4)) - 0.4) < 1e-9, "one copy's opacity and the copies together, each other's inverse");
        }),
        ("Chat review 1489: removing a tab leaves the next tabs their own lines' area, so their own scroll (M2)", () =>
        {
            var store = new ChatStore();
            var tabs = new List<ChatTabSettings> { new() { Name = "General" }, new() { Name = "Battle" }, new() { Name = "Event" } };
            var before = tabs.Select(tab => ChatFormat.LinesId(store.ViewOf(tab).Serial)).ToList();
            Check(before.Distinct().Count() == 3, "one area per tab");
            tabs.RemoveAt(0);   // « Remove » the first tab: the others slide one place
            var after = tabs.Select(tab => ChatFormat.LinesId(store.ViewOf(tab).Serial)).ToList();
            Check(after[0] == before[1] && after[1] == before[2], "each tab keeps its own area (named by place, Battle would have taken General's)");
            Check(ChatFormat.LinesId(store.ViewOf(tabs[0]).Serial) == ChatFormat.LinesId(store.ViewOf(tabs[0]).Serial), "found again frame after frame");
            var added = new ChatTabSettings { Name = "Social" };
            tabs.Add(added);
            Check(!before.Contains(ChatFormat.LinesId(store.ViewOf(added).Serial)), "a new tab never gets a removed tab's area");
            var window = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("DrawChatLines(view,options,height,view.Serial);", StringComparison.Ordinal) && !window.Contains("DrawChatLines(ChatMessages.ViewOf(tabs[shown]),options,height,shown);", StringComparison.Ordinal),
                  "the window names the area after the view, not the place");
        }),
    ];
}
