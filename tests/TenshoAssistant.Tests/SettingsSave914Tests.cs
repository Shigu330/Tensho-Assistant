using System.Text.RegularExpressions;
using TenshoAssistant.Alarms;
using TenshoAssistant.UI;

// 0.914 (X-76 step 2b, harmonisation H3 + H4): one way to save a setting. TrySave (UI/Components/SettingRow.cs, core in UI/SettingSave.cs)
// saves, puts the previous value back when saving fails, logs once and shows the error in the main window's footer; number settings
// follow the edit in memory and are written once, when the slider is released or the field left, not on every frame or key press.
internal static class SettingsSave914Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string Root=Path.GetFullPath("src/TenshoAssistant");
    private static string Source(string file)=>File.ReadAllText(Path.Combine(Root,file));
    private static IEnumerable<(string Name,string Text)> Files()
        =>Directory.EnumerateFiles(Root,"*.cs",SearchOption.AllDirectories)
            .Where(path=>!Path.GetRelativePath(Root,path).Split(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar).Any(part=>part is "obj" or "bin"))
            .Select(path=>(Path.GetRelativePath(Root,path).Replace('\\','/'),File.ReadAllText(path)));
    // The inline copies TrySave replaces: a save that clears the shared error, and the shared error set by hand.
    private static readonly Regex InlineSave=new(@"try\s*\{\s*save\(\);\s*saveError\s*=\s*null;");
    private static readonly Regex InlineError=new(@"saveError\s*=\s*localization\.Get\(""Common\.SaveError""\)");
    private static string Between(string text,string start,string end)
    {
        var from=text.IndexOf(start,StringComparison.Ordinal);
        Check(from>=0,"not found: "+start);
        var to=text.IndexOf(end,from+start.Length,StringComparison.Ordinal);
        Check(to>from,"not found after "+start+": "+end);
        return text[from..to];
    }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Settings save: a failed save of the alarm options brings the saved object back untouched",() =>
        {
            var configuration=new TenshoAssistant.Configuration();
            var saved=configuration.AlarmOptions;
            var next=saved.Copy();
            next.LeadMinutes=30;next.SnoozeMinutes=15;next.RingOnTime=true;
            Check(saved.LeadMinutes==5 && saved.SnoozeMinutes==5 && !saved.RingOnTime,"the copy is edited, not the saved object");
            configuration.AlarmOptions=next;
            var error=SettingSave.Run(()=>throw new IOException("disk full"),()=>configuration.AlarmOptions=saved);
            Check(error is IOException,"the error is returned");
            Check(ReferenceEquals(configuration.AlarmOptions,saved) && configuration.AlarmOptions.LeadMinutes==5 && configuration.AlarmOptions.SnoozeMinutes==5
                && !configuration.AlarmOptions.RingOnTime,"lead, snooze and ring on time are back");
            configuration.AlarmOptions=next;
            Check(SettingSave.Run(()=>{},()=>throw new Exception("no rollback when saved")) is null && configuration.AlarmOptions.LeadMinutes==30,"kept when saved");
            var rolledBack=0;
            SettingSave.Run(()=>throw new IOException(),()=>rolledBack++);
            Check(rolledBack==1,"one rollback per failure");
            SettingSave.Keep();
        }),
        ("Settings save: the alarm options page edits a copy and never writes into the saved options before their save",() =>
        {
            var page=Source("MainWindow.Alarms.cs");
            var body=Between(page,"private void DrawAlarmSettings()","private void SaveAlarmOptions(");
            foreach(var name in new[]{"LeadMinutes","SnoozeMinutes","RingOnTime","Sound","PlaySound","Chat","MuteInDuties","MuteChatInDuties","UseGameAlarms","GameIncludeWeather"})
                Check(!Regex.IsMatch(body,@"\boptions\."+name+@"\s*="),"the saved options are changed before their save: options."+name);
            Check(body.Contains("var next=configuration.AlarmOptions.Copy();",StringComparison.Ordinal) && body.Contains("SaveAlarmOptions(next);",StringComparison.Ordinal),"edited on a copy");
            Check(page.Contains("TrySave(\"Alarm settings\",()=>configuration.AlarmOptions=previous);",StringComparison.Ordinal),"the previous object comes back");
            Check(page.Contains("private (string Id,int Value)? alarmTyped;",StringComparison.Ordinal),"typed numbers wait outside the saved options");
        }),
        ("Settings save: one helper; no inline « try{save();saveError=null;} » or hand-set save error outside SettingRow.cs",() =>
        {
            var found=new List<string>();
            foreach(var (name,text) in Files())
            {
                if(name is "UI/Components/SettingRow.cs" or "UI/SettingSave.cs")continue;
                var lines=text.Split('\n');
                for(var index=0;index<lines.Length;index++)
                    if(InlineSave.IsMatch(lines[index]) || InlineError.IsMatch(lines[index]))found.Add($"{name}:{index+1}");
            }
            Check(found.Count==0,"use TrySave(what, rollback): "+string.Join(", ",found));
            var row=Source("UI/Components/SettingRow.cs");
            Check(row.Contains("private bool TrySave(string what,Action rollback)",StringComparison.Ordinal) && row.Contains("UI.SettingSave.Run(save,rollback)",StringComparison.Ordinal)
                && InlineError.Matches(row).Count==1,"TrySave sets the error, once");
            Check(Between(row,"private void SaveSetting<T>(","\n    }").Contains("TrySave(labelKey,Restore(write,previous));",StringComparison.Ordinal),"SaveSetting goes through TrySave");
            Check(Between(row,"private void IntSetting(","\n    }").Contains("SaveWhenEditEnds(labelKey,write,before);",StringComparison.Ordinal),"IntSetting restores the value it started from");
            Check(Between(row,"private void SaveWhenEditEnds<T>(","\n    }").Contains("if(ImGui.IsItemActivated())editRollback=Restore(write,before);",StringComparison.Ordinal),"the start value is taken when the edit begins");
        }),
        ("Settings save: the sites that saved silently (no restore, no message) now use the shared save",() =>
        {
            foreach(var (file,gone) in new[]
            {
                ("MainWindow.PreparationCost.cs","Could not save the cost scope."),("MainWindow.Plots.cs","Plot watch size could not be saved."),
                ("MainWindow.HuntTrain.cs","Hunt call channel could not be saved."),("MainWindow.Weather.cs","Could not save the weather zones."),
                ("MainWindow.CraftFromStock.cs","Sale days could not be saved."),("MainWindow.CurrencyValue.cs","Sale days could not be saved."),
                ("MainWindow.MarketBoard.cs","Market scope could not be saved."),("MainWindow.TrackerWindow.cs","Tracker window setting could not be saved."),
                ("MainWindow.GatheringPlanSettings.cs","The hotbar highlight setting could not be saved."),("MainWindow.FashionWeek.cs","Fashion options could not be saved."),
                ("MainWindow.Settings.cs","Macro copy settings could not be saved."),("MainWindow.Advanced.cs","Alarm delay could not be saved."),
                ("MainWindow.Alarms.cs","Alarm settings could not be saved."),
            })
                Check(!Source(file).Contains(gone,StringComparison.Ordinal),file+": still saved by hand ("+gone+")");
            Check(Source("MainWindow.PreparationCost.cs").Contains("BoolSetting(\"CostUi.DataCenter\",\"CostUi.DataCenterTip\"",StringComparison.Ordinal),"cost scope: shared row");
            Check(Source("MainWindow.Plots.cs").Contains("SaveSetting(\"PlotWatchSize\",",StringComparison.Ordinal) && Source("MainWindow.HuntTrain.cs").Contains("SaveSetting(\"HuntAnnounceChannel\",",StringComparison.Ordinal)
                && Source("MainWindow.MarketBoard.cs").Contains("SaveSetting(\"MarketBoardScope\",",StringComparison.Ordinal),"combo choices restored when saving fails");
            Check(Source("MainWindow.Weather.cs").Contains("TrySave(\"Weather zones\",()=>{if(kept)favorites[territory]=removed;else favorites.Remove(territory);});",StringComparison.Ordinal),"weather zone undone");
        }),
        ("Settings save: number settings are written when the slider is released or the field left, not on every frame or key press",() =>
        {
            foreach(var (file,keys) in new (string File,string[] Keys)[]
            {
                ("MainWindow.Advanced.cs",["Advanced.AlarmBarTooltipCount","Advanced.CommandDelay"]),
                ("MainWindow.Gear.cs",["Gear.LevelsBelow"]),
                ("MainWindow.GearWear.cs",["GearWear.RepairBelow","GearWear.SpiritbondAt"]),
            })
            {
                var text=Source(file);
                foreach(var key in keys)Check(text.Contains("SliderSetting(\""+key+"\",",StringComparison.Ordinal),file+": "+key+" uses SliderSetting");
                Check(!text.Contains("ImGui.SliderInt(",StringComparison.Ordinal) && !text.Contains("ImGui.SliderFloat(",StringComparison.Ordinal),file+": hand-written slider left");
            }
            Check(!Source("MainWindow.Advanced.cs").Contains("sliderBefore",StringComparison.Ordinal),"the hand-written start value is gone");
            Check(Source("MainWindow.Gear.cs").Contains("IntSetting(\"Gear.MinItemLevel\",",StringComparison.Ordinal),"gear minimum item level: saved when the field is left");
            Check(!Source("MainWindow.GearWear.cs").Contains("SaveGearWear",StringComparison.Ordinal),"no save per drag frame in gear wear");
            var row=Source("UI/Components/SettingRow.cs");
            Check(Regex.Matches(row,@"private void SliderSetting\(").Count==2 && Regex.Matches(row,@"SaveWhenEditEnds\(labelKey,write,before\);").Count==3,"int and decimal sliders, and the whole-number field, save on release");
            var map=Between(Source("MainWindow.MapView.cs"),"private void DrawMapSettingsBody(","private void SaveMapSettings()");
            Check(map.Contains("mapSettingsPending|=changed;",StringComparison.Ordinal) && map.Contains("if(mapSettingsPending && !ImGui.IsAnyItemActive())SaveMapSettings();",StringComparison.Ordinal)
                && !map.Contains("TrySave(",StringComparison.Ordinal),"map settings: live while held, written once nothing is held");
            var settings=Source("MainWindow.Settings.cs");
            Check(settings.Contains("macroCopySaved??=configuration.MacroCopy;",StringComparison.Ordinal) && settings.Contains("if(macroCopySaved is not null && !ImGui.IsAnyItemActive())SaveMacroCopy();",StringComparison.Ordinal),"macro copy: written once the field is left");
        }),
        ("Settings save: the days of sales have one editor for « What can I craft? » and the currency page",() =>
        {
            var currency=Source("MainWindow.CurrencyValue.cs");var stock=Source("MainWindow.CraftFromStock.cs");
            Check(currency.Contains("private void SaleDaysSetting(string id,string tipKey)",StringComparison.Ordinal) && currency.Contains("SaleDaysSetting(\"CurrencyValueDays\",\"CurrencyValue.SaleDaysTip\");",StringComparison.Ordinal)
                && stock.Contains("SaleDaysSetting(\"FromStockDays\",\"CraftStock.SaleDaysTip\");",StringComparison.Ordinal),"one helper, both pages");
            Check(!stock.Contains("localization.Get(\"CraftStock.SaleDays\")",StringComparison.Ordinal) && Regex.Matches(currency,@"localization\.Get\(""CraftStock\.SaleDays""\)").Count==1,"one field drawn by the helper");
            Check(Between(currency,"private void SaleDaysSetting(","private long? CurrencyValueBalance()").Contains("SaveWhenEditEnds(\"CraftStockSaleDays\",writeSaleDays,before);",StringComparison.Ordinal),"saved when the field is left");
        }),
        ("Settings save: the main window shows the save error once, in its footer; the map's layers menu keeps its own line",() =>
        {
            var drawn=Files().Where(file=>Regex.IsMatch(file.Text,@"saveError is (not )?\{ \}")).Select(file=>file.Name).Order(StringComparer.Ordinal).ToArray();
            Check(drawn.SequenceEqual(["MainWindow.MapView.cs","UI/Components/SettingRow.cs"]),"saveError drawn in: "+string.Join(", ",drawn));
            Check(Source("MainWindow.cs").Contains("if(DrawSaveError())return;",StringComparison.Ordinal),"in the footer");
            Check(Between(Source("UI/Components/SettingRow.cs"),"private bool DrawSaveError()","\n    }").Contains("ImGui.TextColored(Error,failed);",StringComparison.Ordinal),"theme error colour");
            Check(!Source("MainWindow.Settings.cs").Contains("saveError",StringComparison.Ordinal),"not on the Settings page any more");
            Check(Between(Source("MainWindow.MapView.cs"),"private void DrawMapLayers()","private void DrawMapSettingsBody(").Contains("if(saveError is { } failed)ImGui.TextColored(Error,failed);",StringComparison.Ordinal),"map window popup");
        }),
    ];
}
