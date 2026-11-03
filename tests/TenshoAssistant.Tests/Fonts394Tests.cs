using TenshoAssistant.UI;

// 0.394 (user request): fonts of the Tensho window — families checked (Dalamud for the text only), sizes 10 to 40, defaults kept,
// rebuilt only when the used values change, reset by « restore every setting ».
internal static class Fonts394Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Fonts: defaults, families checked, sizes clamped, one key per used value, rebuilt at Draw, reset list",() =>
        {
            var defaults = new FontSettings();
            Check(defaults.Key==("TrumpGothic",24,"TrumpGothic",19,FontSettings.DalamudFont,17),"defaults as before");
            var odd = new FontSettings{Title="Comic",TitleSize=99,Section=FontSettings.DalamudFont,Body="Axis",BodySize=3};
            Check(odd.Key==("TrumpGothic",40,"TrumpGothic",19,"Axis",10),"unknown family falls back, Dalamud only for text, sizes clamped");
            Check(Read("Plugin.cs").Contains("fonts?.Apply(fontSettings() ?? new());",StringComparison.Ordinal),"checked at each Draw");
            Check(TenshoAssistant.ConfigurationReset.Reset.Contains("Fonts"),"reset list");
        }),
    ];
}
