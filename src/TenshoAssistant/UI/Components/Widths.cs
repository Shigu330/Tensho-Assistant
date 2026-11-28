namespace TenshoAssistant;

// X-607 (0.1948, user request of 18/11): the widths the pages give most often to a control (SetNextItemWidth), named in one place with the
// value each page wrote by hand before, so the pages are unchanged. Numbers: a number field (InputInt) of a few digits; the others: lists,
// sliders and searches, from the shortest to the widest.
internal static class Widths
{
    /// <summary>A number field of one or two digits (70).</summary>
    public const float Tiny=70;
    /// <summary>A number field (InputInt) of a few digits (90).</summary>
    public const float Number=90;
    /// <summary>A number field of a larger number (gil, quantities) (110).</summary>
    public const float NumberWide=110;
    /// <summary>A short list, slider or search (160).</summary>
    public const float Short=160;
    /// <summary>A list or search of middle width (220).</summary>
    public const float Medium=220;
    /// <summary>A long list or search (260).</summary>
    public const float Long=260;
    /// <summary>The widest list or field of a line (320).</summary>
    public const float Wide=320;
}
