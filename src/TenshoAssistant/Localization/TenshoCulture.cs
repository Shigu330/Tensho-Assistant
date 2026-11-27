using System.Globalization;

namespace TenshoAssistant.Localization;

/// <summary>
/// 0.1220 (X-261, first-player review G11): numbers shown by Tensho follow the language of Tensho, not the culture of Windows.
/// English (and the partial languages, whose texts are mostly English) use the invariant culture (1,234.5); French uses
/// French separators (1 234,5) with a no-break space U+00A0, which every game font has (the narrow U+202F of .NET's French is
/// not sure to be there). Set by <see cref="LocalizationManager.SetLanguage"/>; read on any thread (a culture is immutable).
/// Never for files, macros, URLs or coordinates: those stay invariant.
/// </summary>
internal static class TenshoCulture
{
    private static readonly CultureInfo English = BuildEnglish();
    private static readonly CultureInfo French = BuildFrench();
    private static volatile CultureInfo current = English;

    public static CultureInfo Current => current;

    public static CultureInfo For(string? code)
        => string.Equals(code, "fr", StringComparison.OrdinalIgnoreCase) ? French : English;

    internal static void Use(string? code) => current = For(code);

    private static bool IsFrench => ReferenceEquals(current, French);

    /// <summary>A percentage as the language writes it: « 12.5% » in English, « 12,5 % » in French (no-break space).</summary>
    public static string Percent(double value, string format = "0")
        => value.ToString(format, current) + (IsFrench ? " %" : "%");

    /// <summary>X-658 (0.1604): a gain or a loss with its sign, grouped as the language writes numbers: « +1,234 » / « +1 234 », « 0 » for none.</summary>
    public static string Signed(long value) => value.ToString("+#,0;-#,0;0", current);

    /// <summary>The colon after a label: « : » in English, « : » after a no-break space in French.</summary>
    public static string Colon => IsFrench ? " :" : ":";

    private static CultureInfo BuildEnglish()
    {
        // The invariant culture, with « 12.5% » instead of its « 12.50 % » for the P format.
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.PercentPositivePattern = 1;
        culture.NumberFormat.PercentNegativePattern = 1;
        return CultureInfo.ReadOnly(culture);
    }

    private static CultureInfo BuildFrench()
    {
        // Built from the invariant culture: the same result with or without the system's culture data (desktop app, tests).
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NumberGroupSeparator = " ";
        culture.NumberFormat.CurrencyGroupSeparator = " ";
        culture.NumberFormat.PercentGroupSeparator = " ";
        culture.NumberFormat.NumberDecimalSeparator = ",";
        culture.NumberFormat.PercentDecimalSeparator = ",";
        culture.NumberFormat.CurrencyDecimalSeparator = ",";
        return CultureInfo.ReadOnly(culture);
    }
}
