using Dalamud.Bindings.ImGui;
using TenshoAssistant.UI;

namespace TenshoAssistant;

// Plugin theme: follow the game's interface theme (read by UI/GameThemeReader.Dalamud.cs) or pick one of the Tensho palettes
// (UI/ThemePalettes.cs: approximations of the game themes, not extracted assets). Applied once per frame before the plugin
// windows; the setting (Settings › Interface) has a tooltip and « ↺ » back to the default (follow the game).
internal sealed partial class MainWindow
{
    private Func<uint?> gameTheme=()=>null;
    internal void ConfigureTheme(Func<uint?> reader)=>gameTheme=reader;
    // Called once per frame before any plugin window: the game option or the explicit setting picks the palette.
    internal void ApplyTheme()
    {
        // X-157 (0.1272): « Follow Dalamud's style » (off by default; in the game only: the desktop application has no Dalamud style).
        // Read here, before any Tensho window pushes anything: the colours are the style Dalamud has made active.
        if(configuration.FollowDalamudStyle && !inDesktopApp)
        {
            static System.Numerics.Vector4 Of(ImGuiCol colour)=>ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(colour));
            FfxivTheme.Follow(ThemePalettes.Follow(Of(ImGuiCol.WindowBg),Of(ImGuiCol.ChildBg),Of(ImGuiCol.Border),Of(ImGuiCol.Text),Of(ImGuiCol.TextDisabled),Of(ImGuiCol.Header),Of(ImGuiCol.HeaderHovered)));
            return;
        }
        FfxivTheme.Select(ThemePalettes.Resolve(configuration.ThemeMode,gameTheme()));
    }

    private void DrawThemeSetting()
    {
        var detected=gameTheme();
        var follow=localization.Format("Settings.ThemeFollow",detected is { } value
            ? localization.Format("Settings.ThemeDetected",localization.Get("Theme."+ThemePalettes.FromGame(value)),value):localization.Get("Settings.ThemeUnknown"));
        var mode=Math.Clamp(configuration.ThemeMode,0,Enum.GetValues<PluginTheme>().Length);
        ImGui.SetNextItemWidth(320);
        if(ImGui.BeginCombo(localization.Get("Settings.Theme"),mode==0 ? follow:localization.Get("Theme."+(PluginTheme)(mode-1))))
        {
            try
            {
                if(ImGui.Selectable(follow,mode==0))SaveTheme(0);
                foreach(var theme in Enum.GetValues<PluginTheme>())
                    if(ImGui.Selectable(localization.Get("Theme."+theme),mode==(int)theme+1))SaveTheme((int)theme+1);
            }
            finally{ImGui.EndCombo();}
        }
        Tip("SettingTips.Theme");
        // 0.261: « ↺ » back to the default (follow the game).
        if(DefaultButton("Theme",configuration.ThemeMode!=DefaultConfiguration.ThemeMode))SaveTheme(DefaultConfiguration.ThemeMode);
    }
    /// <param name="before">0.477: the theme to come back to, when a tool's theme is chosen; any other choice forgets it.</param>
    private void SaveTheme(int mode,int? before=null)
    {
        var previous=configuration.ThemeMode;var previousBefore=configuration.ThemeModeBeforeTool;
        configuration.ThemeMode=mode;configuration.ThemeModeBeforeTool=before;
        TrySave("Theme",()=>{configuration.ThemeMode=previous;configuration.ThemeModeBeforeTool=previousBefore;});
    }
}
