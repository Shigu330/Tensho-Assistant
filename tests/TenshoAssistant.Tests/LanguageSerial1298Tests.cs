// 0.1298 (X-281, review of the 0.1278 block, I1, R36.2): the language tests written in Program.cs, moved here unchanged. They select
// French, then Japanese, through LocalizationManager.SetLanguage, which sets TenshoCulture for the whole process: run in parallel with a
// class that formats a number (StatTiers380, LanguageFormats1220…), the other class could read « 12,5 » instead of « 12.5 ». The Program
// group cannot run alone (TestRegistry writes it as parallel), so these tests are a class of their own; each one gives English back.
using TenshoAssistant.Localization;

[SerialTests("LocalizationManager.SetLanguage sets TenshoCulture for the whole process (French, Japanese)")]
internal static class LanguageSerial1298Tests
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

    private static readonly (string Name, Action Run)[] Moved =
    [
        ("An added language is discovered without window changes and safely falls back after its file is removed", () =>
        {
            var directory=Path.Combine(Path.GetTempPath(),"TenshoAddedLanguage-"+Guid.NewGuid().ToString("N"));
            var languages=Path.Combine(directory,"Localization","Languages");
            Directory.CreateDirectory(languages);
            var path=Path.Combine(languages,"es.json");
            try
            {
                File.WriteAllText(path,"""{"_meta":{"code":"ES","name":"Spanish","nativeName":"Español","author":"Test","version":1},"Settings":{"Language":"Idioma"}}""");
                var loc=new TenshoAssistant.Localization.LocalizationManager((error,message)=>throw new InvalidOperationException(message,error),directory);
                Check(loc.AvailableLanguages.Single(language=>language.NativeName=="Español").Code=="es","discovered metadata uses the same normalized code as selection");
                loc.SetLanguage(" ES ");
                Check(loc.CurrentCode=="es" && loc.Get("Settings.Language")=="Idioma","new language can be selected from metadata alone");
                Check(loc.Get("CraftMacro.Copy")=="Copy this block","partial added language uses bundled English");
                File.Delete(path);loc.Reload();
                Check(loc.CurrentCode=="en" && loc.Get("Settings.Language")=="Language","removed language falls back without a missing-key UI");
            }
            finally{if(File.Exists(path))File.Delete(path);Directory.Delete(languages);Directory.Delete(Path.GetDirectoryName(languages)!);Directory.Delete(directory);}
        }),
        ("Invalid external translation formats retain bundled text and corrected files reload without losing the language", () =>
        {
            var directory=Path.Combine(Path.GetTempPath(),"TenshoFormat-"+Guid.NewGuid().ToString("N"));
            var languages=Path.Combine(directory,"Localization","Languages");
            Directory.CreateDirectory(languages);
            var path=Path.Combine(languages,"fr.json");
            try
            {
                File.WriteAllText(path,"""{"_meta":{"code":"fr","name":"French","nativeName":"Français","author":"Test","version":1},"Home":{"Catalogues":"cassé {99}"},"LocalClock":{"Header":"cassé {"}}""");
                var warnings=new List<string>();
                var loc=new TenshoAssistant.Localization.LocalizationManager((error,message)=>warnings.Add(message),directory);
                loc.SetLanguage("fr");
                Check(!loc.Format("Home.Catalogues",12,34).Contains("cassé") && !loc.Format("LocalClock.Header","12:34:56").Contains("cassé"),"bad indices and unmatched braces do not reach rendering");
                Check(warnings.Count==2 && loc.Validate().Errors.Count>0,"invalid editable text remains diagnosed");
                File.WriteAllText(path,"""{"_meta":{"code":"fr","name":"French","nativeName":"Français","author":"Test","version":1},"Home":{"Catalogues":"Recettes {1}, objets {0}"}}""");
                loc.Reload();
                Check(loc.CurrentCode=="fr" && loc.Format("Home.Catalogues",12,34)=="Recettes 34, objets 12","corrected translation and reordered placeholders load with the selection preserved");
            }
            finally{File.Delete(path);Directory.Delete(languages);Directory.Delete(Path.GetDirectoryName(languages)!);Directory.Delete(directory);}
        }),
        ("Embedded languages work without external files and preserve fallback across reload", () =>
        {
            var failures = new List<string>();
            var loc = new TenshoAssistant.Localization.LocalizationManager((error, message) => failures.Add(message), Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
            Check(loc.AvailableLanguages.Count >= 4, "bundled languages discovered without external files");
            Check(loc.Get("Settings.Language") == "Language", "embedded English source loaded");
            loc.SelectInitialLanguage(null,"French");
            Check(loc.CurrentCode=="fr","first launch uses the client language metadata");
            loc.SelectInitialLanguage("en","French");
            Check(loc.CurrentCode=="en","manual saved choice overrides the client language");
            loc.SelectInitialLanguage(null,"UnsupportedLanguage");
            Check(loc.CurrentCode=="en","unsupported client language falls back to English");
            loc.SetLanguage("fr");
            Check(loc.Get("Settings.Language") == "Langue", "embedded French selected");
            var previousRevision=loc.Revision;
            loc.Reload();
            Check(loc.CurrentCode == "fr" && loc.Get("Settings.Language") == "Langue", "reload retains selected language");
            Check(loc.Revision!=previousRevision,"reload invalidates cached localized captions without changing the language");
            loc.SetLanguage("ja");
            Check(loc.Get("CraftMacro.Copy") == "Copy this block", "partial language falls back to English");
            Check(loc.Get("Missing.Example") == "[Missing.Example]", "unknown key safe");
            previousRevision=loc.Revision;
            loc.ShowKeys = true;
            Check(loc.Get("Settings.Language") == "Settings.Language", "developer key mode");
            Check(loc.Revision!=previousRevision,"key display invalidates cached captions");
            Check(failures.Count == 0, "bundled files parse successfully");
        }),
    ];

    /// <summary>Each test gives TenshoCulture back to English, the default the other classes run with.</summary>
    public static IEnumerable<(string Name, Action Run)> Cases =>
        Moved.Select(test => (test.Name, (Action)(() => { try { test.Run(); } finally { TenshoCulture.Use("en"); } })));
}
