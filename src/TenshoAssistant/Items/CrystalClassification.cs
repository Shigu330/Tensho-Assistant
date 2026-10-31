using System.Globalization;
using System.Text;

namespace TenshoAssistant.Items;

public sealed record CrystalClassification(string Element, string Size);

public static class CrystalClassifier
{
    public static readonly string[] Elements = ["Feu", "Glace", "Vent", "Terre", "Foudre", "Eau"];
    public static readonly string[] Sizes = ["Éclats", "Cristaux", "Agrégats"];

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
