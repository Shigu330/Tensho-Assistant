using System.Text.Json;
using TenshoAssistant.Chat;

// 0.1565 (X-560, X-565): the chat settings fit the window. The grids of boxes take as many columns as the width holds (X-560, 0.1528, tested
// here), each linkshell's number stays in its name; the line of each tab (name, unread, colour, remove) goes to the next line when narrow.
internal static class ChatSettingsFit1565Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static JsonElement Section(string code, string name)
    {
        var document = JsonDocument.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{code}.json")));
        return document.RootElement.GetProperty(name);
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat settings fit 1565: the grid's columns follow the width, at least one, at most one per box", () =>
        {
            Check(ChatFormat.GridColumns(760, 250, 36) == 3, "760 px, cells of 250: 3 columns");
            Check(ChatFormat.GridColumns(300, 250, 36) == 1, "300 px, cells of 250: 1 column");
            Check(ChatFormat.GridColumns(100, 250, 36) == 1, "narrower than one cell: still 1 column");
            Check(ChatFormat.GridColumns(5000, 250, 4) == 4, "never more columns than boxes");
            Check(ChatFormat.GridColumns(float.NaN, 250, 36) == 1 && ChatFormat.GridColumns(760, 0, 36) == 1, "unknown width: 1 column");
            // Three columns of 250 never go past 760 px: the sum stays within the width given (the scroll bar is outside ImGui's content region).
            for (var width = 200f; width <= 2000f; width += 37f)
                Check(ChatFormat.GridColumns(width, 250, 36) * 250 <= Math.Max(width, 250), $"{width} px: the columns go past the width");
        }),
        ("Chat settings fit 1565: each linkshell's name keeps its number, in en and fr", () =>
        {
            foreach (var code in new[] { "en", "fr" })
            {
                var names = Section(code, "ChatChannels");
                for (var slot = 1; slot <= 8; slot++)
                    foreach (var key in new[] { "Ls" + slot, "Cwls" + slot })
                        Check(names.GetProperty(key).GetString()!.Contains("[" + slot + "]"), $"{code}: {key} has lost its number");
            }
        }),
        ("Chat settings fit 1565: a control goes to the next line when the width left does not hold it", () =>
        {
            Check(ChatFormat.FitsOnLine(200, 150), "150 in 200 fits");
            Check(ChatFormat.FitsOnLine(150, 150), "150 in 150 fits");
            Check(!ChatFormat.FitsOnLine(120, 150), "150 in 120 does not fit");
            Check(!ChatFormat.FitsOnLine(float.NaN, 10) && !ChatFormat.FitsOnLine(-5, 10), "unknown or negative width left: next line");
        }),
        ("Chat settings fit 1565: the tab's line wraps each control (unread, colour, remove)", () =>
        {
            var code = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            var start = code.IndexOf("private void DrawChatTabSettings(", StringComparison.Ordinal);
            var end = code.IndexOf("private static void SameLineIfFits(", start, StringComparison.Ordinal);
            Check(start > 0 && end > start, "DrawChatTabSettings not found");
            var body = code[start..end];
            Check(body.Split("SameLineIfFits(").Length - 1 >= 3, "the tab's line does not wrap its three controls");
            Check(!body.Contains("ImGui.SameLine();"), "a control of the tab's line is still put on the same line whatever the width");
        }),
    ];
}
