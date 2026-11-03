using System.Text.Json;
using System.Text.RegularExpressions;
using TenshoAssistant.Credits;

// R-13 (0.320): the Credits page lists everything Tensho uses; these checks keep the list in step with the code.
internal static class CreditCatalogTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string Source = Path.GetFullPath("src/TenshoAssistant");
    private static IEnumerable<string> CodeFiles => Directory.EnumerateFiles(Source,"*.cs",SearchOption.AllDirectories).Where(file => !file.Contains("\\obj\\") && !file.Contains("\\bin\\") && !file.Contains("/obj/") && !file.Contains("/bin/"));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Credits: unique ids, a licence and a sentence in English and French for every entry",()=>
        {
            var ids = CreditCatalog.All.Select(credit => credit.Id).ToArray();
            Check(ids.Length >= 60 && ids.Distinct().Count() == ids.Length,"unique ids: " + ids.Length);
            Check(CreditCatalog.All.All(credit => credit.Name.Length > 0 && credit.License.Length > 0),"name and licence");
            Check(CreditCatalog.All.All(credit => credit.Url.Length == 0 || credit.Url.StartsWith("https://",StringComparison.Ordinal)) && CreditCatalog.All.All(credit => credit.LicenseUrl.Length == 0 || credit.LicenseUrl.StartsWith("https://",StringComparison.Ordinal)),"HTTPS links only");
            foreach (var kind in Enum.GetValues<CreditKind>()) Check(CreditCatalog.All.Any(credit => credit.Kind == kind),"every section has entries: " + kind);
            foreach (var language in new[] { "en", "fr" })
            {
                using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Source,"Localization","Languages",language + ".json")));
                var items = document.RootElement.GetProperty("Credits").GetProperty("Items");
                var missing = ids.Where(id => !items.TryGetProperty(id,out var text) || text.GetString() is not { Length: > 0 }).ToArray();
                Check(missing.Length == 0,$"{language}.json misses credit texts: {string.Join(", ",missing)}");
                var extra = items.EnumerateObject().Select(item => item.Name).Except(ids).ToArray();
                Check(extra.Length == 0,$"{language}.json has texts for no credit: {string.Join(", ",extra)}");
                foreach (var kind in Enum.GetValues<CreditKind>())
                    Check(document.RootElement.GetProperty("Credits").GetProperty("Kind").TryGetProperty(kind.ToString(),out _),$"{language}: section title {kind}");
            }
            var game = CreditCatalog.All.Single(credit => credit.Kind == CreditKind.Game);
            Check(game.Author.Contains("SQUARE ENIX",StringComparison.Ordinal),"the game credit names SQUARE ENIX");
        }),
        ("Credits: every embedded data file and every web host in the code has its credit",()=>
        {
            var project = File.ReadAllText(Path.Combine(Source,"TenshoAssistant.csproj"));
            var embedded = Regex.Matches(project,"<EmbeddedResource Include=\"([^\"]+)\"").Select(match => match.Groups[1].Value.Replace('\\','/'))
                .Where(path => !path.StartsWith("Localization/",StringComparison.Ordinal)).ToArray();
            Check(embedded.Length >= 2,"embedded data found: " + embedded.Length);
            var credited = CreditCatalog.All.Where(credit => credit.Kind is CreditKind.Embedded or CreditKind.Rule).SelectMany(credit => credit.Files).ToHashSet(StringComparer.Ordinal);
            foreach (var path in embedded) Check(credited.Contains(path),"embedded data without its credit: " + path);
            // Every address written in the code (a string beginning with https://), except Tensho's own repository and the credit list itself.
            var hosts = CodeFiles.Where(file => Path.GetFileName(file) != "CreditCatalog.cs").SelectMany(file => Regex.Matches(File.ReadAllText(file),"\"https?://([A-Za-z0-9.\\-]+)").Select(match => match.Groups[1].Value.ToLowerInvariant()))
                .Where(host => host != "github.com").ToHashSet();
            var known = CreditCatalog.All.SelectMany(credit => credit.Hosts).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var unknown = hosts.Where(host => !known.Contains(host)).ToArray();
            Check(hosts.Count >= 5 && unknown.Length == 0,"web hosts without a credit: " + string.Join(", ",unknown));
            foreach (var credit in CreditCatalog.All.Where(credit => credit.Kind == CreditKind.Online)) Check(credit.Hosts.Count > 0,"an online source names its hosts: " + credit.Id);
        }),
        ("Credits: names and authors are language-neutral; licence links match the licence; copied MIT data carries the full notice",()=>
        {
            var french = new Regex(@"\b(et|non consigné|dépôt|communauté|contributeurs|données|règle|semaine)\b",RegexOptions.IgnoreCase);
            foreach (var credit in CreditCatalog.All)
            {
                Check(!french.IsMatch(credit.Name) && !french.IsMatch(credit.Author),$"{credit.Id}: name and author shown in every language must be proper names: {credit.Name} / {credit.Author}");
                if (credit.License == "none") Check(credit.LicenseUrl.Length == 0,$"{credit.Id}: no published licence, so no licence link");
                if (credit.License is not ("none" or "unknown" or "proprietary")) Check(credit.LicenseUrl.Length > 0,$"{credit.Id}: licence {credit.License} needs its link");
                if (credit.Kind == CreditKind.Embedded && credit.Copied && credit.License == "MIT")
                    Check(credit.Files.Any(file => File.ReadAllText(Path.Combine(Source,file)).Contains("Permission is hereby granted",StringComparison.Ordinal)),$"{credit.Id}: a copied MIT source needs its full notice in a listed file");
            }
            Check(CreditCatalog.All.Single(credit => credit.Kind == CreditKind.Game).License == "proprietary","the game is proprietary, not unlicensed");
            foreach (var language in new[] { "en", "fr" })
            {
                using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Source,"Localization","Languages",language + ".json")));
                var credits = document.RootElement.GetProperty("Credits");
                foreach (var credit in CreditCatalog.All.Where(credit => credit.SettingPartial))
                    Check(credits.GetProperty("SettingScope").TryGetProperty(credit.Id,out _),$"{language}: what the partial setting of {credit.Id} turns off");
                var ids = CreditCatalog.All.Select(credit => credit.Id).ToHashSet();
                foreach (var name in credits.GetProperty("Names").EnumerateObject()) Check(ids.Contains(name.Name),$"{language}: localized name for no credit: {name.Name}");
            }
        }),
        ("Credits: listed files and settings exist; references are read, never copied",()=>
        {
            foreach (var credit in CreditCatalog.All)
            {
                foreach (var file in credit.Files) Check(File.Exists(Path.Combine(Source,file)),$"{credit.Id}: file {file} exists");
                if (credit.Setting.Length > 0)
                {
                    var path = CreditCatalog.SettingPath(typeof(TenshoAssistant.Configuration),credit.Setting);
                    Check(path is { Length: > 0 } && path[^1].PropertyType == typeof(bool),$"{credit.Id}: setting {credit.Setting} is a boolean of Configuration");
                }
                var closed = credit.License is "none" or "unknown" || credit.License.StartsWith("AGPL",StringComparison.Ordinal);
                if (credit.Kind == CreditKind.Reference) Check(!credit.Copied,$"{credit.Id}: a reference is read, not copied");
                if (closed && credit.Kind is CreditKind.Embedded) Check(credit.Id == "SubmersibleLootSpreadsheet","only the upstream sheet (taken over through MIT SubmarineTracker) is embedded without its own licence: " + credit.Id);
            }
            Check(CreditCatalog.SettingPath(typeof(TenshoAssistant.Configuration),"Discord.Enabled") is { Length: 2 },"dotted setting path");
            Check(CreditCatalog.SettingPath(typeof(TenshoAssistant.Configuration),"Nope.Enabled") is null,"missing setting");
        }),
    ];
}
