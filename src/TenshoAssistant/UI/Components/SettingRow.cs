using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace TenshoAssistant;

// R-06 step 1 (0.249): the shared setting row. A setting always has its tooltip, and once it differs from the default
// a small « ↺ » button puts the default back. The default is read from a fresh Configuration, so it never drifts from the
// property initialisers.
internal sealed partial class MainWindow
{
    private static readonly Configuration DefaultConfiguration=new();

    /// <summary>
    /// A saved boolean setting: checkbox, tooltip, reset to default; the value is restored when saving fails. X-76 H5 (0.915): the default
    /// is required, so every on/off row has its « ↺ ».
    /// </summary>
    private void BoolSetting(string labelKey,string tipKey,Func<bool> read,Action<bool> write,Func<Configuration,bool> defaultOf)
    {
        var value=read();
        if(ImGui.Checkbox(localization.Get(labelKey)+"###"+labelKey,ref value))SaveSetting(labelKey,read,write,value);
        Tip(tipKey);
        var fallback=defaultOf(DefaultConfiguration);
        if(DefaultButton(labelKey,read()!=fallback,OnOff(fallback)))SaveSetting(labelKey,read,write,fallback);
    }

    /// <summary>The name of an on/off default, for the « ↺ » tooltip.</summary>
    private string OnOff(bool value)=>localization.Get(value ? "Settings.On":"Settings.Off");

    /// <summary>
    /// X-76 H5 (0.915): a saved choice in a list: combo, tooltip and « ↺ » naming the default option; the previous choice comes back when
    /// saving fails. Options are (value, text shown); without label (showLabel false) the combo keeps its id.
    /// </summary>
    private void ComboSetting<T>(string labelKey,string tipKey,Func<T> read,Action<T> write,IReadOnlyList<(T Value,string Label)> options,T fallback,float width=160,bool showLabel=true)
    {
        var current=read();
        var comparer=EqualityComparer<T>.Default;
        string? shown=null,defaultName=null;
        foreach(var (value,label) in options)
        {
            if(comparer.Equals(value,current))shown=label;
            if(comparer.Equals(value,fallback))defaultName=label;
        }
        ImGui.SetNextItemWidth(width);
        if(ImGui.BeginCombo((showLabel ? localization.Get(labelKey):"")+"###"+labelKey,shown ?? current?.ToString() ?? ""))
        {
            try
            {
                foreach(var (value,label) in options)
                    if(ImGui.Selectable(label,comparer.Equals(value,current)) && !comparer.Equals(value,current))SaveSetting(labelKey,read,write,value);
            }
            finally{ImGui.EndCombo();}
        }
        Tip(tipKey);
        if(DefaultButton(labelKey,!comparer.Equals(read(),fallback),defaultName))SaveSetting(labelKey,read,write,fallback);
    }

    /// <summary>
    /// A saved whole-number setting (0.321): field without +/- buttons, value bounded as typed and kept in memory, saved once when the
    /// field is left after an edit (no disk write per key press), tooltip and « ↺ » to the default. X-76 H3 (0.914): the value the field
    /// had when the edit began comes back when saving fails.
    /// </summary>
    private void IntSetting(string labelKey,string tipKey,Func<int> read,Action<int> write,int min,int max,int fallback,float width=130)
    {
        var before=read();var value=before;
        ImGui.SetNextItemWidth(width);
        if(ImGui.InputInt(localization.Get(labelKey)+"###"+labelKey,ref value,0))write(Math.Clamp(value,min,max));
        SaveWhenEditEnds(labelKey,write,before);
        Tip(tipKey);
        if(DefaultButton(labelKey,read()!=fallback))SaveSetting(labelKey,read,write,fallback);
    }

    /// <summary>
    /// X-76 H4 (0.914): a saved whole-number setting on a slider. The value follows the drag in memory (what it changes is seen at once)
    /// and the settings file is written once, when the slider is released after a change (no disk write per frame); tooltip and « ↺ ».
    /// </summary>
    private void SliderSetting(string labelKey,string tipKey,Func<int> read,Action<int> write,int min,int max,int fallback,string format="%d",float width=160)
    {
        var before=read();var value=Math.Clamp(before,min,max);
        ImGui.SetNextItemWidth(width);
        if(ImGui.SliderInt(localization.Get(labelKey)+"###"+labelKey,ref value,min,max,format))write(value);
        SaveWhenEditEnds(labelKey,write,before);
        Tip(tipKey);
        if(DefaultButton(labelKey,read()!=fallback))SaveSetting(labelKey,read,write,fallback);
    }

    /// <summary>X-76 H4 (0.914): the same slider for a decimal setting.</summary>
    private void SliderSetting(string labelKey,string tipKey,Func<float> read,Action<float> write,float min,float max,float fallback,string format="%.2f",float width=160)
    {
        var before=read();var value=Math.Clamp(float.IsFinite(before) ? before:fallback,min,max);
        ImGui.SetNextItemWidth(width);
        if(ImGui.SliderFloat(localization.Get(labelKey)+"###"+labelKey,ref value,min,max,format))write(value);
        SaveWhenEditEnds(labelKey,write,before);
        Tip(tipKey);
        if(DefaultButton(labelKey,read()!=fallback))SaveSetting(labelKey,read,write,fallback);
    }

    // X-76 H3/H4 (0.914): how to put back the value a number field or slider had when its edit began (one control is active at a
    // time); used when saving the finished edit fails.
    private Action? editRollback;

    /// <summary>
    /// Called right after a number control that writes its value in memory as it changes: remembers the value it started from when it
    /// becomes active, and saves once when it is left after an edit, putting that value back if saving fails.
    /// </summary>
    private void SaveWhenEditEnds<T>(string what,Action<T> write,T before)
    {
        if(ImGui.IsItemActivated())editRollback=Restore(write,before);
        if(!ImGui.IsItemDeactivatedAfterEdit())return;
        var rollback=editRollback ?? UI.SettingSave.Keep;editRollback=null;
        TrySave(what,rollback);
    }

    // Built only when an edit begins or a value is saved, not every frame.
    private static Action Restore<T>(Action<T> write,T value)=>()=>write(value);

    /// <summary>
    /// X-76 H3 (0.914): every save made from this window goes through here. When saving fails the rollback puts the previous value
    /// back in memory, the warning names what was saved (once per failure) and the window's footer shows the error; true when saved.
    /// </summary>
    private bool TrySave(string what,Action rollback)
    {
        if(UI.SettingSave.Run(save,rollback) is not { } error){saveError=null;return true;}
        saveError=localization.Get("Common.SaveError");
        log.Warning(error,"{What} could not be saved.",what);
        return false;
    }

    /// <summary>The same when nothing is to be put back: the value stays in memory and the next save that works writes it.</summary>
    private bool TrySave(string what)=>TrySave(what,UI.SettingSave.Keep);

    /// <summary>
    /// X-76 H3 (0.914): the one place the main window shows saveError, in its footer whatever the page, with a button that hides it;
    /// false when there is nothing to show (the footer then shows its usual line).
    /// </summary>
    private bool DrawSaveError()
    {
        if(saveError is not { } failed)return false;
        ImGui.PushFont(UiBuilder.IconFont);
        bool hide;
        try{hide=ImGui.SmallButton(FontAwesomeIcon.Times.ToIconString()+"###HideSaveError");}
        finally{ImGui.PopFont();}
        Tip("Common.Dismiss");
        ImGui.SameLine();
        ImGui.TextColored(Error,failed);
        if(hide)saveError=null;
        return true;
    }

    private static readonly MapViewSettings MapDefaults=new();

    /// <summary>
    /// The « ↺ » button after any setting control, shown only when the value differs from its default; true when clicked. Its tooltip
    /// names the default when one is given (on/off rows, combo options). X-76 H5 (0.915): the one reset button (the on/off one merged in).
    /// </summary>
    private bool DefaultButton(string id,bool differs,string? defaultName=null)
    {
        if(!differs)return false;
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        bool clicked;
        try{clicked=ImGui.SmallButton(FontAwesomeIcon.Undo.ToIconString()+"###Default"+id);}
        finally{ImGui.PopFont();}
        if(defaultName is null)Tip("Settings.ResetValue");
        else if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Format("Settings.ResetOne",defaultName));
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
        TrySave(labelKey,Restore(write,previous));
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
            if(TrySave("Default settings",()=>{foreach(var (property,value) in previous)property.SetValue(configuration,value);}))
            {
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
        }
        ImGui.SameLine();
        if(ImGui.Button(localization.Get("Settings.ResetAllNo")))resetAllArmed=false;
    }
}
