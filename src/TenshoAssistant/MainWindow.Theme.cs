using Dalamud.Bindings.ImGui;
using TenshoAssistant.UI;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private Func<uint?> gameTheme=()=>null;
    internal void ConfigureTheme(Func<uint?> reader)=>gameTheme=reader;
    // Called once per frame before any plugin window: the game option or the explicit setting picks the palette.
    internal void ApplyTheme()=>FfxivTheme.Select(ThemePalettes.Resolve(configuration.ThemeMode,gameTheme()));

    private void DrawThemeSetting()
    {
        var detected=gameTheme();
        var follow=localization.Format("Settings.ThemeFollow",detected is { } value
            ? localization.Format("Settings.ThemeDetected",localization.Get("Theme."+ThemePalettes.FromGame(value)),value):localization.Get("Settings.ThemeUnknown"));
        var mode=Math.Clamp(configuration.ThemeMode,0,Enum.GetValues<PluginTheme>().Length);
        ImGui.SetNextItemWidth(320);
        if(!ImGui.BeginCombo(localization.Get("Settings.Theme"),mode==0 ? follow:localization.Get("Theme."+(PluginTheme)(mode-1))))return;
        try
        {
            if(ImGui.Selectable(follow,mode==0))SaveTheme(0);
            foreach(var theme in Enum.GetValues<PluginTheme>())
                if(ImGui.Selectable(localization.Get("Theme."+theme),mode==(int)theme+1))SaveTheme((int)theme+1);
        }
        finally{ImGui.EndCombo();}
    }
    private void SaveTheme(int mode)
    {
        var previous=configuration.ThemeMode;configuration.ThemeMode=mode;
        try{save();saveError=null;}
        catch(Exception error){configuration.ThemeMode=previous;saveError=localization.Get("Common.SaveError");log.Warning(error,"Could not save theme preference.");}
    }
}
