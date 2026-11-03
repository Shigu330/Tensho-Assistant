using System.Globalization;
using System.Text;

namespace TenshoAssistant.Items;

public sealed record CrystalClassification(string Element, string Size);

public static class CrystalClassifier
{
    public static readonly string[] Elements = ["Feu", "Glace", "Vent", "Terre", "Foudre", "Eau"];
    public static readonly string[] Sizes = ["Éclats", "Cristaux", "Agrégats"];

    // 0.322: the English item name first (the catalogue keeps it for every client language), the French name as a fallback; the result
    // keeps the French canonical values above, which the page maps to its translated keys. A German or Japanese client used to leave
    // every crystal unclassified.
    private static readonly string[] EnglishElements = ["FIRE", "ICE", "WIND", "EARTH", "LIGHTNING", "WATER"];
    private static readonly string[] EnglishSizes = ["SHARD", "CRYSTAL", "CLUSTER"];

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
        var element = Elements.FirstOrDefault(value =>
        {
            var upper = value.ToUpperInvariant();
            return normalized.Contains($"DE {upper}", StringComparison.Ordinal)
                || normalized.Contains($"D'{upper}", StringComparison.Ordinal);
        });
        if (element is null) return null;
        var size = normalized.Contains("ECLAT", StringComparison.Ordinal) ? "Éclats"
            : normalized.Contains("CRISTAL", StringComparison.Ordinal) ? "Cristaux"
            : normalized.Contains("AGREGAT", StringComparison.Ordinal) ? "Agrégats" : null;
        return size is null ? null : new(element, size);
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
