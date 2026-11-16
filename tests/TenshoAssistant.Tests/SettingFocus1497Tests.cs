using System.Text.RegularExpressions;
using TenshoAssistant.UI;

// X-507 (the user's bug of 16/11): choosing a result of the settings search opens its page, unfolds its section, scrolls to the setting and
// highlights it for a moment. The journey's state is SettingFocus (behaviour tests, a clock and a frame number given by hand); the wiring tests
// check that every setting the search can find is drawn through a row that reports itself, and that no fold on the way keeps it hidden.
internal static class SettingFocus1497Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Setting focus 1497: nothing asked, nothing scrolled or highlighted; a row of another key is left alone",() =>
        {
            var focus=new SettingFocus();
            Check(!focus.Opening && focus.Reached("Settings.OpenOnLoad",1,0.0)==(false,0f),"nothing asked");
            focus.Request("Settings.OpenOnLoad");
            Check(focus.Opening,"the sections open while it is looked for");
            Check(focus.Reached("Settings.OpenOnLogin",1,0.0)==(false,0f),"another setting is not highlighted");
            Check(focus.Opening,"still looking");
        }),
        ("Setting focus 1497: the first row of the key is scrolled into view once and highlighted, the highlight fades out in two seconds, then nothing",() =>
        {
            var focus=new SettingFocus();
            focus.Request("Advanced.Wiki");
            var first=focus.Reached("Advanced.Wiki",10,100.0);
            Check(first==(true,1f),"scrolled once, fully highlighted when first drawn");
            Check(!focus.Opening,"found: the sections stay as they are");
            var later=focus.Reached("Advanced.Wiki",11,100.5);
            Check(!later.Scroll && Math.Abs(later.Strength-0.75f)<0.001f,"not scrolled again, 75 % after half a second: "+later);
            Check(focus.Reached("Advanced.Wiki",12,101.99).Strength>0 && focus.Reached("Advanced.Wiki",13,102.0)==(false,0f),"over after two seconds");
            Check(focus.Reached("Advanced.Wiki",14,102.1)==(false,0f),"and stays over");
        }),
        ("Setting focus 1497: two rows with the same key (one per page, one per chat tab): only the first counts, in every frame",() =>
        {
            var focus=new SettingFocus();
            focus.Request("Advanced.PagePlace");
            Check(focus.Reached("Advanced.PagePlace",5,1.0)==(true,1f),"first row of the frame");
            Check(focus.Reached("Advanced.PagePlace",5,1.0)==(false,0f) && focus.Reached("Advanced.PagePlace",5,1.0)==(false,0f),"the others of the frame are left alone");
            Check(focus.Reached("Advanced.PagePlace",6,1.2).Strength>0 && focus.Reached("Advanced.PagePlace",6,1.2)==(false,0f),"next frame: the first again, the second not");
        }),
        ("Setting focus 1497: a setting that is never drawn (its parent option is off) is given up after eight frames, and a new request starts over",() =>
        {
            var focus=new SettingFocus();
            focus.Request("Settings.HoverVendorsMetOnly");
            for(var frame=0;frame<SettingFocus.SearchFrames-1;frame++){focus.Tick();Check(focus.Opening,"still looking at frame "+frame);}
            focus.Tick();
            Check(!focus.Opening && focus.Reached("Settings.HoverVendorsMetOnly",50,0.0)==(false,0f),"given up: the sections are no longer forced open, a late row is left alone");
            focus.Request("Settings.HoverVendors");
            Check(focus.Opening && focus.Reached("Settings.HoverVendors",51,0.0)==(true,1f),"a new request works");
            for(var frame=0;frame<20;frame++)focus.Tick();
            Check(focus.Reached("Settings.HoverVendors",52,0.1).Strength>0,"ticks after it is found do not end the highlight");
            focus.Request("Settings.UsageHover");
            Check(focus.Reached("Settings.HoverVendors",53,0.2)==(false,0f) && focus.Reached("Settings.UsageHover",53,0.2)==(true,1f),"the previous setting is forgotten");
        }),
        ("Setting focus 1497: every row the search can find reports itself, every fold on the way opens, the page asks and gives up",() =>
        {
            var row=Read("src/TenshoAssistant/UI/Components/SettingRow.cs");
            foreach(var component in new[]{"BoolSetting","TextSetting","CheckSetting","SettingButton","ComboSetting<T>","IntSetting","SliderSetting"})
            {
                var start=row.IndexOf("private "+(component is "TextSetting" or "CheckSetting" or "SettingButton" ? "bool ":"void ")+component+"(",StringComparison.Ordinal);
                Check(start>=0,"component "+component);
                var end=row.IndexOf("\n    }\n",start,StringComparison.Ordinal);
                Check(row[start..end].Contains("ReachSetting(labelKey);",StringComparison.Ordinal),component+" reports its row (a second slider is checked below)");
            }
            Check(Regex.Matches(row,@"ReachSetting\(labelKey\);").Count>=8,"the two sliders too");
            Check(Read("src/TenshoAssistant/MainWindow.MapView.cs").Contains("ReachSetting(key);",StringComparison.Ordinal),"the map's boxes (SettingBox)");
            var ui=Read("src/TenshoAssistant/UI/UiComponents.cs");
            Check(ui.Contains("if (OpenSections) ImGui.SetNextItemOpen(true);",StringComparison.Ordinal) && Regex.Matches(ui,@"if \(OpenSections\) ImGui\.SetNextItemOpen\(true\);").Count==2,"headers and tree nodes open");
            // No fold on a setting's way escapes it: a method a category reaches that holds a setting does not call ImGui's own tree node or header.
            var raw=SettingsTabs999Tests.Walk().Where(step=>step.Body.Contains("ImGui.TreeNode(",StringComparison.Ordinal) || step.Body.Contains("ImGui.CollapsingHeader(",StringComparison.Ordinal))
                .Select(step=>step.Method).Distinct().ToArray();
            Check(raw.Length==0,"a fold on a setting's way does not open for the search: "+string.Join(",",raw));
            var page=Read("src/TenshoAssistant/MainWindow.Settings.cs");
            Check(page.Contains("settingFocus.Tick();",StringComparison.Ordinal) && page.Contains("OpenSections=settingFocus.Opening;",StringComparison.Ordinal) && page.Contains("OpenSections=false;",StringComparison.Ordinal),"the page ticks, opens the folds, and closes them again");
            Check(page.Contains("OpenSettingsCategory(tab,category,key);",StringComparison.Ordinal) && Read("src/TenshoAssistant/MainWindow.SettingsCategories.cs").Contains("settingFocus.Request(settingKey);",StringComparison.Ordinal),"a search result asks for its setting");
        }),
        ("Setting focus 1497: every setting of the search index is drawn by a row component and lies in a category, so choosing it can reach it",() =>
        {
            var index=TenshoAssistant.SettingsIndex.Entries;
            Check(index.Length>=224 && index.All(entry=>TenshoAssistant.SettingsIndex.Categories.ContainsKey(entry.Key)),"every entry has its tab and its category");
            var calls=SettingsTabs999Tests.Walk().SelectMany(step=>Regex.Matches(step.Body,@"\b(BoolSetting|IntSetting|SliderSetting|ComboSetting|SettingBox|TextSetting|CheckSetting|SettingButton)(?:<[^>()]*>)?\(\s*""([\w.]+)""")
                .Select(match=>(Component:match.Groups[1].Value,Key:match.Groups[2].Value))).ToArray();
            foreach(var entry in index)Check(calls.Any(call=>call.Key==entry.Key),$"{entry.Key}: no row component draws it");
            Check(calls.Select(call=>call.Component).Distinct().All(component=>new[]{"BoolSetting","IntSetting","SliderSetting","ComboSetting","SettingBox","TextSetting","CheckSetting","SettingButton"}.Contains(component)),"only components that report");
        }),
    ];
}
