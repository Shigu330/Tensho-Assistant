using System.Reflection;
using System.Text.Json;

namespace TenshoAssistant.Localization;

internal sealed class LocalizationManager
{
    private readonly Action<Exception, string> warn;
    private readonly string languagesDirectory;
    private readonly Dictionary<string, Dictionary<string, string>> languages = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string> selected = [];
    private Dictionary<string, string> english = [];

    public LocalizationManager(Action<Exception, string> warn, string? pluginDirectory = null)
    {
        this.warn = warn;
        languagesDirectory = Path.Combine(pluginDirectory ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory, "Localization", "Languages");
        Reload();
    }

    public IReadOnlyList<LocalizationLanguage> AvailableLanguages { get; private set; } = Array.Empty<LocalizationLanguage>();
    private LocalizationValidationReport validation = new(0, 0, Array.Empty<string>(), Array.Empty<LocalizationLanguageValidation>());

    public LocalizationValidationReport Validate() => validation;

    public string CurrentCode { get; private set; } = "en";
    public int Revision {get;private set;}
    private bool showKeys;
    public bool ShowKeys
    {
        get=>showKeys;
        set{if(showKeys==value)return;showKeys=value;Revision++;}
    }

    public void SelectInitialLanguage(string? savedCode, string clientLanguage)
    {
        if (!string.IsNullOrWhiteSpace(savedCode)) { SetLanguage(savedCode); return; }
        foreach (var language in AvailableLanguages)
            if (string.Equals(language.Name, clientLanguage, StringComparison.OrdinalIgnoreCase)
                || string.Equals(language.Code, clientLanguage, StringComparison.OrdinalIgnoreCase))
            { SetLanguage(language.Code); return; }
        SetLanguage("en");
    }

    public void SetLanguage(string? code)
    {
        var normalized = string.IsNullOrWhiteSpace(code) ? "en" : code.Trim().ToLowerInvariant();
        CurrentCode = languages.ContainsKey(normalized) ? normalized : "en";
        selected = languages.GetValueOrDefault(CurrentCode) ?? english;
        Revision++;
    }

    public string Get(string key)
        => ShowKeys ? key : selected.GetValueOrDefault(key) ?? english.GetValueOrDefault(key) ?? $"[{key}]";

    public string Format(string key, params object[] args)
    {
        try { return string.Format(System.Globalization.CultureInfo.InvariantCulture, Get(key), args); }
        catch (FormatException) { return Get(key); }
    }

    public void Reload()
    {
        languages.Clear();
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var resource in assembly.GetManifestResourceNames())
        {
            if (!resource.StartsWith("TenshoAssistant.Localization.Languages.", StringComparison.Ordinal) || !resource.EndsWith(".json", StringComparison.Ordinal)) continue;
            try
            {
                using var stream = assembly.GetManifestResourceStream(resource);
                if (stream is null) continue;
                using var document = JsonDocument.Parse(stream);
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Flatten(document.RootElement, string.Empty, values);
                if (values.TryGetValue("_meta.code", out var code)) languages[code] = values;
            }
            catch (Exception error) { warn(error, $"Could not load embedded language {resource}."); }
        }
        var bundledReference=languages.GetValueOrDefault("en");
        if (Directory.Exists(languagesDirectory))
            foreach (var file in Directory.EnumerateFiles(languagesDirectory, "*.json"))
            {
                try
                {
                    using var document = JsonDocument.Parse(File.ReadAllText(file));
                    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    Flatten(document.RootElement, string.Empty, values);
                    if (values.TryGetValue("_meta.code", out var code) && !string.IsNullOrWhiteSpace(code))
                    {
                        // Validate once on reload; a broken override keeps the bundled
                        // translation, or English fallback for a newly added language.
                        var valid=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
                        foreach(var pair in values)
                        {
                            if(!pair.Key.StartsWith("_meta.",StringComparison.OrdinalIgnoreCase)
                                && bundledReference?.TryGetValue(pair.Key,out var sourceText)==true
                                && !LocalizationValidator.CompatibleFormats(sourceText,pair.Value))
                            {warn(new FormatException($"Incompatible placeholders: {pair.Key}"),$"Ignoring invalid translation {code}:{pair.Key}.");continue;}
                            valid[pair.Key]=pair.Value;
                        }
                        if (languages.TryGetValue(code, out var bundled))
                            foreach (var pair in valid) bundled[pair.Key] = pair.Value;
                        else languages[code] = valid;
                    }
                }
                catch (Exception error) { warn(error, $"Could not load localization file {file}."); }
            }

        english = languages.GetValueOrDefault("en") ?? [];
        AvailableLanguages = Array.AsReadOnly(languages.Values
            .Select(values => new LocalizationLanguage(values["_meta.code"].Trim().ToLowerInvariant(), values.GetValueOrDefault("_meta.name", values["_meta.code"]), values.GetValueOrDefault("_meta.nativeName", values["_meta.code"])))
            .OrderBy(language => language.Code == "en" ? 0 : 1).ThenBy(language => language.NativeName)
            .ToArray());
        // The validator describes editable external files; embedded defaults remain usable
        // even when a development install copied only the DLL.
        validation = Directory.Exists(languagesDirectory)
            ? LocalizationValidator.Validate(languagesDirectory)
            : new LocalizationValidationReport(english.Count, languages.Count,
                new[] { $"Using embedded translations. External language directory not found: {languagesDirectory}" },
                Array.Empty<LocalizationLanguageValidation>());
        SetLanguage(CurrentCode);
    }

    private static void Flatten(JsonElement element, string prefix, Dictionary<string, string> values)
    {
        if (element.ValueKind == JsonValueKind.Object)
            foreach (var property in element.EnumerateObject()) Flatten(property.Value, string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}", values);
        else if (element.ValueKind == JsonValueKind.String) values[prefix] = element.GetString() ?? string.Empty;
    }
}

internal readonly record struct LocalizationLanguage(string Code, string Name, string NativeName);
