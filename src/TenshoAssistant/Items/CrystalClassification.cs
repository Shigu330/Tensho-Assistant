using System.Globalization;
using System.Text;

namespace TenshoAssistant.Items;

public sealed record CrystalClassification(string Element, string Size);

public static class CrystalClassifier
{
    // 0.909: the result is the English key itself (language keys CrystalElements.* and CrystalSizes.*), in the page's column and row order.
    public static readonly string[] Elements = ["Fire", "Ice", "Wind", "Earth", "Lightning", "Water"];
    public static readonly string[] Sizes = ["Shards", "Crystals", "Clusters"];

    // 0.322: the English item name first (the catalogue keeps it for every client language), the French name as a fallback. A German
    // or Japanese client used to leave every crystal unclassified.
    private static readonly string[] EnglishElements = ["FIRE", "ICE", "WIND", "EARTH", "LIGHTNING", "WATER"];
    private static readonly string[] EnglishSizes = ["SHARD", "CRYSTAL", "CLUSTER"];

    // Game data: the French client's element words and size stems (accents removed), same order as Elements and Sizes.
    private static readonly string[] FrenchElements = ["FEU", "GLACE", "VENT", "TERRE", "FOUDRE", "EAU"];
    private static readonly string[] FrenchSizes = ["ECLAT", "CRISTAL", "AGREGAT"];

    public static CrystalClassification? Classify(string name, string? englishName)
    {
        if (!string.IsNullOrWhiteSpace(englishName))
        {
            var upper = englishName.ToUpperInvariant();
            var element = Array.FindIndex(EnglishElements, value => upper.StartsWith(value + " ", StringComparison.Ordinal));
            var size = Array.FindIndex(EnglishSizes, value => upper.EndsWith(" " + value, StringComparison.Ordinal));
            if (element >= 0 && size >= 0) return new(Elements[element], Sizes[size]);
        }
        return Classify(name);
    }

    public static CrystalClassification? Classify(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var normalized = RemoveMarks(name).ToUpperInvariant().Replace('’', '\'');
        var element = Array.FindIndex(FrenchElements, upper =>
            normalized.Contains($"DE {upper}", StringComparison.Ordinal)
            || normalized.Contains($"D'{upper}", StringComparison.Ordinal));
        if (element < 0) return null;
        var size = Array.FindIndex(FrenchSizes, stem => normalized.Contains(stem, StringComparison.Ordinal));
        return size < 0 ? null : new(Elements[element], Sizes[size]);
    }

    private static string RemoveMarks(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        return builder.ToString();
    }
}
