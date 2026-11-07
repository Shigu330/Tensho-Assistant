using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace TenshoAssistant;

// R-06 step 1 (0.249): the shared setting row. A setting always has its tooltip, and once it differs from the default
// a small « ↺ » button puts the default back. The default is read from a fresh Configuration, so it never drifts from the
// property initialisers.
internal sealed partial class MainWindow
{
    private static readonly Configuration DefaultConfiguration=new();

    /// <summary>A saved boolean setting: checkbox, tooltip, reset to default; the value is restored when saving fails.</summary>
    private void BoolSetting(string labelKey,string tipKey,Func<bool> read,Action<bool> write,Func<Configuration,bool>? defaultOf=null)
    {
        var value=read();
        if(ImGui.Checkbox(localization.Get(labelKey)+"###"+labelKey,ref value))SaveSetting(labelKey,read,write,value);
        Tip(tipKey);
        if(defaultOf?.Invoke(DefaultConfiguration) is { } fallback && BoolDefaultButton(labelKey,fallback,read()))SaveSetting(labelKey,read,write,fallback);
    }

    /// <summary>The « ↺ » of an on/off setting (BoolSetting, observation rows, 0.330), naming the default; shown only when the value differs, true when clicked.</summary>
    private bool BoolDefaultButton(string id,bool fallback,bool value)
    {
        if(fallback==value)return false;
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        bool reset;
        try{reset=ImGui.SmallButton(FontAwesomeIcon.Undo.ToIconString()+"###Default"+id);}
        finally{ImGui.PopFont();}
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("Settings.ResetOne",localization.Get(fallback ? "Settings.On":"Settings.Off")));
        return reset;
    }

    /// <summary>
    /// A saved whole-number setting (0.321): field without +/- buttons, value bounded as typed and kept in memory, saved once when the
    /// field is left after an edit (no disk write per key press), tooltip and « ↺ » to the default.
    /// </summary>
    private void IntSetting(string labelKey,string tipKey,Func<int> read,Action<int> write,int min,int max,int fallback,float width=130)
    {
        var value=read();
        ImGui.SetNextItemWidth(width);
        if(ImGui.InputInt(localization.Get(labelKey)+"###"+labelKey,ref value,0))write(Math.Clamp(value,min,max));
        if(ImGui.IsItemDeactivatedAfterEdit())
            try{save();saveError=null;}
            catch(Exception error){saveError=localization.Get("Common.SaveError");log.Warning(error,"Setting {Setting} could not be saved.",labelKey);}
        Tip(tipKey);
        if(DefaultButton(labelKey,read()!=fallback))SaveSetting(labelKey,read,write,fallback);
    }

    private static readonly MapViewSettings MapDefaults=new();

    /// <summary>The « ↺ » button after any setting control, shown only when the value differs from its default; true when clicked.</summary>
    private bool DefaultButton(string id,bool differs)
    {
        if(!differs)return false;
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        bool clicked;
        try{clicked=ImGui.SmallButton(FontAwesomeIcon.Undo.ToIconString()+"###Default"+id);}
        finally{ImGui.PopFont();}
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("Settings.ResetValue"));
        return clicked;
    }

    /// <summary>
    /// 0.324: a button that asks a community source. While that source is turned off (Settings › Advanced) it is greyed, explains why
    /// on hover and never reports a click.
    /// </summary>
    private bool SourceButton(string label,bool sourceOn,bool small=false)
    {
        if(!sourceOn)ImGui.BeginDisabled();
        bool clicked;
        try{clicked=small ? ImGui.SmallButton(label):ImGui.Button(label);}
        finally{if(!sourceOn)ImGui.EndDisabled();}
        if(!sourceOn)Tip("Advanced.SourceOffTip");
        return clicked && sourceOn;
    }

    private void SaveSetting<T>(string labelKey,Func<T> read,Action<T> write,T value)
    {
        var previous=read();write(value);
        try{save();saveError=null;}
        catch(Exception error){write(previous);saveError=localization.Get("Common.SaveError");log.Warning(error,"Setting {Setting} could not be saved.",labelKey);}
    }
}

internal sealed partial class MainWindow
{
    // 0.258 (user request): « restore every setting ». Saved data and the language are kept; the settings listed in ConfigurationReset
    // take the value of a fresh Configuration (0.321: both lists in Configuration.cs, a test puts every property in one of them).
    private bool resetAllArmed;

    private void DrawResetAllSettings()
    {
        if(!resetAllArmed)
        {
            if(ImGui.Button(localization.Get("Settings.ResetAll")))resetAllArmed=true;
            Tip("Settings.ResetAllTip");
            return;
        }
        ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("Settings.ResetAllConfirm"));
        if(ImGui.Button(localization.Get("Settings.ResetAllYes")))
        {
            resetAllArmed=false;
            var fresh=new Configuration();
            // R-12 (0.319): the Discord settings go back to their defaults (sending off) but the webhook address the player typed is kept.
            var webhook=configuration.Discord?.WebhookUrl ?? "";
            var categoryWebhooks=configuration.Discord?.CategoryUrls;
            var properties=typeof(Configuration).GetProperties(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance)
                .Where(property=>property.CanRead && property.CanWrite && property.GetIndexParameters().Length==0 && ConfigurationReset.Reset.Contains(property.Name)).ToArray();
            // 0.321: the previous values come back when saving fails (they stayed reset in memory).
            var previous=properties.Select(property=>(property,property.GetValue(configuration))).ToArray();
            foreach(var property in properties)property.SetValue(configuration,property.GetValue(fresh));
            (configuration.Discord??=new()).WebhookUrl=webhook;
            configuration.Discord.CategoryUrls=categoryWebhooks;
            try
            {
                save();saveError=null;
                discordUrlEdit=null;discordNameEdit=null;discordTemplateEdit=null;discordRoleEdit=null;discordGroupEdits.Clear();
                discordNotifier?.Clear();
                // 0.330 (audit): macro text prepared with the old export options (echo, wait, chaining) is dropped, as the single rows do,
                // and the gathering reader follows the restored gathering help.
                craftMacroExport=null;craftMacroError=null;
                ApplyGatheringObservation();
                // 0.331 (audit): the running developer mode follows the restored setting (Diagnostics, IDs, capture logs, translation keys),
                // as the developer checkbox does; it was cached once at start.
                developerMode=configuration.DeveloperMode;if(!developerMode)localization.ShowKeys=false;
            }
            catch(Exception error)
            {
                foreach(var (property,value) in previous)property.SetValue(configuration,value);
                saveError=localization.Get("Common.SaveError");log.Warning(error,"Default settings could not be saved.");
            }
        }
        ImGui.SameLine();
        if(ImGui.Button(localization.Get("Settings.ResetAllNo")))resetAllArmed=false;
    }
}
