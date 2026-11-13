using System.Numerics;
using TenshoAssistant.UI;

// 0.1272 (X-157): « Follow Dalamud's style », off by default: the standard controls keep Dalamud's active style, Tensho pushes only its
// identity (accent) and reads the style's own neutral colours for what it draws by hand.
internal static class FollowStyle1272Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Follow style 1272: neutral roles from the style, identity from the Tensho palette that reads better on it",() =>
        {
            var darkWindow=new Vector4(0.06f,0.06f,0.06f,0.93f);var lightWindow=new Vector4(0.94f,0.94f,0.94f,1);
            var border=new Vector4(0.43f,0.43f,0.5f,0.5f);var text=new Vector4(1,1,1,1);var muted=new Vector4(0.5f,0.5f,0.5f,1);
            var header=new Vector4(0.26f,0.59f,0.98f,0.31f);var hovered=new Vector4(0.26f,0.59f,0.98f,0.8f);
            var dark=ThemePalettes.Follow(darkWindow,new Vector4(0,0,0,0),border,text,muted,header,hovered);
            Check(dark.Background==darkWindow && dark.Border==border && dark.Text==text && dark.TextMuted==muted && dark.Selected==header && dark.Hovered==hovered,"the style's own colours, untouched");
            Check(dark.Panel==darkWindow,"a see-through child background takes the window's");
            var child=new Vector4(0.1f,0.1f,0.12f,1);
            Check(ThemePalettes.Follow(darkWindow,child,border,text,muted,header,hovered).Panel==child,"a child background of the style is kept");
            var identityDark=ThemePalettes.All[PluginTheme.Dark];var identityLight=ThemePalettes.All[PluginTheme.Light];
            Check(ThemePalettes.FollowIdentity(darkWindow)==PluginTheme.Dark && dark.Accent==identityDark.Accent && dark.Success==identityDark.Success && dark.Warning==identityDark.Warning && dark.Error==identityDark.Error,"dark style: Tensho's dark identity");
            var light=ThemePalettes.Follow(lightWindow,lightWindow,border,new Vector4(0,0,0,1),muted,header,hovered);
            Check(ThemePalettes.FollowIdentity(lightWindow)==PluginTheme.Light && light.Accent==identityLight.Accent && light.Error==identityLight.Error,"light style: Tensho's light identity");
            // Whatever the style's background, the accent kept is readable on it as a mark (3:1, non-text) and as a title (4.5:1) at both ends.
            Check(ThemePalettes.Contrast(dark.Accent,darkWindow)>=4.5 && ThemePalettes.Contrast(light.Accent,lightWindow)>=4.5,"the accent reads on the followed background");
            // On the mid greys neither Tensho accent is a visible mark (3:1): the style's own text colour stands in, never an unreadable accent.
            var fallbacks=0;
            for(var grey=0f;grey<=1.001f;grey+=0.05f)
            {
                var window=new Vector4(grey,grey,grey,1);
                var styleText=grey<0.5f ? new Vector4(1,1,1,1):new Vector4(0,0,0,1);
                var followed=ThemePalettes.Follow(window,window,border,styleText,muted,header,hovered);
                var own=ThemePalettes.All[ThemePalettes.FollowIdentity(window)].Accent;
                if(ThemePalettes.Contrast(own,window)>=3)Check(followed.Accent==own,$"grey {grey:0.00}: Tensho's accent kept when it is a visible mark");
                else{Check(followed.Accent==styleText,$"grey {grey:0.00}: the style's text colour stands in");fallbacks++;}
                Check(ThemePalettes.Contrast(followed.Accent,window)>=3,$"grey {grey:0.00}: the accent used is a visible mark");
            }
            Check(fallbacks is >0 and <8,"the stand-in is used on the mid greys only ("+fallbacks+")");
        }),
        ("Follow style 1272: wiring: off by default, read before the windows, only the identity pushed, each pop takes back its own push",() =>
        {
            Check(!new TenshoAssistant.Configuration().FollowDalamudStyle,"off by default");
            var configuration=Read("src/TenshoAssistant/Configuration.cs");
            Check(configuration.Contains("\"NavigationMode\", \"FollowDalamudStyle\","),"given back by « reset everything »");
            var theme=Read("src/TenshoAssistant/UI/FfxivTheme.cs");
            Check(theme.Contains("pushed.Push(Following);\n        if(Following){PushIdentity();return;}"),"following: only the identity is pushed");
            Check(theme.Contains("if(pushed.Count>0 && pushed.Pop()){PopIdentity();return;}"),"a pop takes back what its own push pushed, even if the mode changed since");
            var identity=theme[theme.IndexOf("private static void PushIdentity()",StringComparison.Ordinal)..];
            var count=identity.Split("ImGui.PushStyleColor(").Length-1;
            Check(count==3 && identity.Contains($"private static void PopIdentity()=>ImGui.PopStyleColor({count});") && !identity.Contains("PushStyleVar"),"as many identity colours popped as pushed, no spacing touched");
            Check(theme.Contains("public static void Select(PluginTheme theme){Theme=theme;current=ThemePalettes.All[theme];Following=false;}"),"choosing a Tensho theme leaves the mode");
            var window=Read("src/TenshoAssistant/MainWindow.Theme.cs");
            Check(window.Contains("if(configuration.FollowDalamudStyle && !inDesktopApp)") && window.Contains("FfxivTheme.Follow(ThemePalettes.Follow(Of(ImGuiCol.WindowBg),Of(ImGuiCol.ChildBg),"),"the game only; the style read once per frame before the windows");
            Check(Read("src/TenshoAssistant/MainWindow.Settings.cs").Contains("BoolSetting(\"Settings.FollowDalamud\",\"Settings.FollowDalamudTip\",()=>configuration.FollowDalamudStyle,"),"the setting, under the theme");
            Check(Read("src/TenshoAssistant/SettingsIndex.cs").Contains("(\"Settings.FollowDalamud\",\"General\")"),"found by the settings search");
            foreach(var file in new[]{"en","fr"})
            {
                var text=Read($"src/TenshoAssistant/Localization/Languages/{file}.json");
                foreach(var key in new[]{"\"FollowDalamud\": ","\"FollowDalamudTip\": "})Check(text.Contains("\n    "+key),$"{file}: {key}");
            }
        }),
    ];
}
