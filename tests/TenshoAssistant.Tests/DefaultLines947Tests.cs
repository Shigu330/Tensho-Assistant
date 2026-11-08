using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using TenshoAssistant;

// X-76 step 4 (0.947): the « Default: on / off » line of every on/off setting's tooltip says its real default (read on a new Configuration), in English
// and in French. A ratchet: a new BoolSetting with a wrong line fails here.
internal static class DefaultLines947Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static string? Text(JsonElement root,string key)
    {
        if(root.TryGetProperty(key,out var flat) && flat.ValueKind==JsonValueKind.String)return flat.GetString();
        var dot=key.IndexOf('.');
        return dot>0 && root.TryGetProperty(key[..dot],out var section) && section.ValueKind==JsonValueKind.Object
            && section.TryGetProperty(key[(dot+1)..],out var value) && value.ValueKind==JsonValueKind.String ? value.GetString():null;
    }

    private static object? Value(object start,string path)
    {
        object? current=start;
        foreach(var part in path.Split('.'))
        {
            if(current is null)return null;
            var property=current.GetType().GetProperty(part,BindingFlags.Public|BindingFlags.Instance);
            if(property is null)return null;
            current=property.GetValue(current);
        }
        return current;
    }

    internal static IReadOnlyList<string> Mismatches()
    {
        var english=JsonDocument.Parse(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json"))).RootElement;
        var french=JsonDocument.Parse(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json"))).RootElement;
        var fresh=new Configuration();
        var found=new List<string>();
        foreach(var path in Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"*.cs",SearchOption.AllDirectories))
        {
            if(path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))continue;
            foreach(Match match in Regex.Matches(File.ReadAllText(path),@"BoolSetting\(""[\w.]+"",""([\w.]+)"",.*?,c=>c\.([\w.]+)\)"))
            {
                var tip=match.Groups[1].Value;var setting=match.Groups[2].Value;
                if(Value(fresh,setting) is not bool on){found.Add($"{setting}: default not read");continue;}
                var en=Text(english,tip);var fr=Text(french,tip);
                if(en is null || fr is null){found.Add($"{tip}: tooltip missing");continue;}
                if(!en.Contains(on ? "Default: on":"Default: off",StringComparison.Ordinal))found.Add($"{setting} ({tip}) en: default {(on ? "on":"off")}");
                if(!fr.Contains(on ? "Par défaut : activé":"Par défaut : désactivé",StringComparison.Ordinal))found.Add($"{setting} ({tip}) fr: default {(on ? "on":"off")}");
            }
        }
        return found;
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Default lines 947: every on/off setting's tooltip says its real default, in English and French",() =>
        {
            var found=Mismatches();
            Check(found.Count==0,"wrong or missing « Default » line: "+string.Join("; ",found.Take(12)));
        }),
    ];
}
