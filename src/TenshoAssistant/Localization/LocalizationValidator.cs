using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace TenshoAssistant.Localization;

internal static class LocalizationValidator
{
    public static LocalizationValidationReport Validate(string directory)
    {
        var files = Directory.Exists(directory) ? Directory.EnumerateFiles(directory, "*.json") : [];
        var parsed = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<string>();
        var formats = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file));
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    errors.Add($"{Path.GetFileName(file)}: root must be an object");
                    continue;
                }
                var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Inspect(document.RootElement, string.Empty, values, errors, Path.GetFileName(file));
                var code = Path.GetFileNameWithoutExtension(file);
                if (!values.TryGetValue("_meta.code", out var declaredCode) || !string.Equals(code, declaredCode, StringComparison.OrdinalIgnoreCase))
                    errors.Add($"{code}: _meta.code must match the file name");
                foreach (var field in new[] { "name", "nativeName", "author" })
                    if (!values.TryGetValue($"_meta.{field}", out var value) || string.IsNullOrWhiteSpace(value)) errors.Add($"{code}: missing _meta.{field}");
                if (!document.RootElement.TryGetProperty("_meta", out var meta) || meta.ValueKind != JsonValueKind.Object || !meta.TryGetProperty("version", out var version) || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var number) || number < 1)
                    errors.Add($"{code}: _meta.version must be a positive integer");
                formats[code] = values;
                Flatten(document.RootElement, string.Empty, keys);
                parsed[Path.GetFileNameWithoutExtension(file)] = keys;
            }
            catch (JsonException error) { errors.Add($"{Path.GetFileName(file)}: invalid JSON ({error.Message})"); }
            catch (IOException error) { errors.Add($"{Path.GetFileName(file)}: {error.Message}"); }
            catch (UnauthorizedAccessException error) { errors.Add($"{Path.GetFileName(file)}: {error.Message}"); }
        }

        if (!parsed.TryGetValue("en", out var reference))
            return new LocalizationValidationReport(0, 0, errors.Append("en.json is missing").ToArray(), []);

        var languages = new List<LocalizationLanguageValidation>();
        foreach (var (code, keys) in parsed)
        {
            var missing = reference.Except(keys, StringComparer.OrdinalIgnoreCase).OrderBy(key => key).ToArray();
            var unknown = keys.Except(reference, StringComparer.OrdinalIgnoreCase).OrderBy(key => key).ToArray();
            languages.Add(new LocalizationLanguageValidation(code, reference.Count, reference.Count - missing.Length, missing, unknown));
            foreach (var key in keys.Intersect(reference))
            {
                if (!formats["en"].TryGetValue(key, out var sourceText) || !formats[code].TryGetValue(key, out var translatedText)) continue;
                try
                {
                    var source = CompositeFormat.Parse(sourceText);
                    var translated = CompositeFormat.Parse(translatedText);
                    if (source.MinimumArgumentCount != translated.MinimumArgumentCount || !Arguments(sourceText).SetEquals(Arguments(translatedText))) errors.Add($"{code}: incompatible placeholders in {key}");
                }
                catch (FormatException) { errors.Add($"{code}: invalid format in {key}"); }
            }
        }
        return new LocalizationValidationReport(reference.Count, languages.Count, errors, languages);
    }

    internal static bool CompatibleFormats(string sourceText,string translatedText)
    {
        try
        {
            return CompositeFormat.Parse(sourceText).MinimumArgumentCount==CompositeFormat.Parse(translatedText).MinimumArgumentCount
                && Arguments(sourceText).SetEquals(Arguments(translatedText));
        }
        catch(FormatException){return false;}
    }

    private static HashSet<string> Arguments(string text)
    {
        // Remove escaped braces before inspecting the validated composite format.
        var unescaped = text.Replace("{{", string.Empty).Replace("}}", string.Empty);
        return Regex.Matches(unescaped, @"\{([0-9]+)").Select(match => int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture).ToString(System.Globalization.CultureInfo.InvariantCulture)).ToHashSet(StringComparer.Ordinal);
    }

    private static void Inspect(JsonElement element, string prefix, Dictionary<string, string> values, List<string> errors, string file)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject())
            {
                var key = prefix.Length == 0 ? property.Name : $"{prefix}.{property.Name}";
                if (!names.Add(property.Name)) errors.Add($"{file}: duplicate key {key}");
                Inspect(property.Value, key, values, errors, file);
            }
        }
        else if (element.ValueKind == JsonValueKind.String) values[prefix] = element.GetString() ?? string.Empty;
        else if (!prefix.StartsWith("_meta.", StringComparison.OrdinalIgnoreCase)) errors.Add($"{file}: {prefix} must contain text");
    }

    private static void Flatten(JsonElement element, string prefix, HashSet<string> keys)
    {
        if (element.ValueKind == JsonValueKind.Object)
            foreach (var property in element.EnumerateObject()) Flatten(property.Value, string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}", keys);
        else if (!prefix.StartsWith("_meta.", StringComparison.OrdinalIgnoreCase)) keys.Add(prefix);
    }
}

internal sealed record LocalizationValidationReport(int ReferenceKeyCount, int LanguageCount, IReadOnlyList<string> Errors, IReadOnlyList<LocalizationLanguageValidation> Languages);
internal sealed record LocalizationLanguageValidation(string Code, int TotalKeys, int TranslatedKeys, IReadOnlyList<string> MissingKeys, IReadOnlyList<string> UnknownKeys)
{
    public double Completion => TotalKeys == 0 ? 1 : (double)TranslatedKeys / TotalKeys;
}
