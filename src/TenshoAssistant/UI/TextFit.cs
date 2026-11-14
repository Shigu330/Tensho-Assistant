namespace TenshoAssistant.UI;

/// <summary>
/// X-286 (review of block 0.1278, M5; the Home picture seen on 13/11): a text drawn in a fixed width (a home tile) was cut by the clip
/// rectangle in the middle of a word (« simulateur, carnet, r »). It now ends with « … » where it does not fit, cut after the last whole
/// word when that keeps most of the line, and the caller shows the whole text in a tooltip. Pure: the width of a text is given by the
/// caller (ImGui.CalcTextSize in the window, a made-up measure in the tests).
/// </summary>
public static class TextFit
{
    public const string Ellipsis = "…";

    /// <summary>The text as it fits in <paramref name="width"/>: itself, or a shorter start ending with « … » (« … » alone when even one
    /// letter does not fit, empty for an empty text). A word is not cut when the cut at the last space keeps two thirds of what fits.</summary>
    public static string Fit(string text, float width, Func<string, float> measure)
    {
        ArgumentNullException.ThrowIfNull(measure);
        if (string.IsNullOrEmpty(text) || measure(text) <= width) return text ?? "";
        // The longest start that fits with « … » (the measure grows with the length: a binary search).
        int low = 0, high = text.Length;
        while (low < high)
        {
            var middle = (low + high + 1) / 2;
            if (measure(text[..middle].TrimEnd() + Ellipsis) <= width) low = middle; else high = middle - 1;
        }
        if (low == 0) return Ellipsis;
        var start = text[..low];
        var space = start.LastIndexOf(' ');
        if (space > 0 && low < text.Length && text[low] != ' ' && space >= low * 2 / 3) start = start[..space];
        return start.TrimEnd(' ', ',', ';', ':', '·', '-') + Ellipsis;
    }

    /// <summary>True when <see cref="Fit"/> shortened the text (the caller then shows it whole in a tooltip).</summary>
    public static bool Shortened(string text, string shown) => !string.Equals(text ?? "", shown, StringComparison.Ordinal);
}
