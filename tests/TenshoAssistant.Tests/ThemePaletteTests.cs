using TenshoAssistant.UI;

internal static class ThemePaletteTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Game theme values map to palettes and an explicit setting wins",()=>
        {
            Check(ThemePalettes.FromGame(0)==PluginTheme.Dark && ThemePalettes.FromGame(1)==PluginTheme.Light && ThemePalettes.FromGame(2)==PluginTheme.ClassicFF
                && ThemePalettes.FromGame(3)==PluginTheme.ClearBlue,"long-standing themes");
            Check(ThemePalettes.FromGame(null)==PluginTheme.Dark && ThemePalettes.FromGame(99)==PluginTheme.Dark,"unknown falls back to dark");
            Check(ThemePalettes.Resolve(0,2)==PluginTheme.ClassicFF && ThemePalettes.Resolve(2,2)==PluginTheme.Light && ThemePalettes.Resolve(42,3)==PluginTheme.ClearBlue,"setting 0 follows, 1..6 force, out of range follows");
            Check(Enum.GetValues<PluginTheme>().All(theme=>ThemePalettes.All.ContainsKey(theme)),"every theme has a palette");
        }),
        ("Every palette keeps text readable on its backgrounds",()=>
        {
            foreach(var (theme,palette) in ThemePalettes.All)
            {
                Check(ThemePalettes.Contrast(palette.Text,palette.Background)>=7 && ThemePalettes.Contrast(palette.Text,palette.Panel)>=7,$"{theme}: body text");
                Check(ThemePalettes.Contrast(palette.TextMuted,palette.Panel)>=4.5 && ThemePalettes.Contrast(palette.Accent,palette.Panel)>=4.5,$"{theme}: muted and accent text");
                Check(ThemePalettes.Contrast(palette.Text,palette.Selected)>=4.5 && ThemePalettes.Contrast(palette.Text,palette.Hovered)>=4.5,$"{theme}: text on selection and hover");
            }
        }),
    };
}
