using System.Text.RegularExpressions;
using TenshoAssistant.UI;

// X-521 (review of 0.1497, minor findings): the search opens only the sections around the setting it chose and folds the others back (M1);
// the category chosen in each tab is kept in the configuration (M2, D-180); no setting is drawn by two categories and every category of the
// map and of the chat window writes what it changed (M6); the category's title is the shared subtitle and the map's « open a map » line is
// no longer a category label (M7).
internal static class SettingsReview1498Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Settings review 1498 M1: found, only the header and the tree nodes holding the setting stay open; the others folded before fold back once",() =>
        {
            var focus=new SettingFocus();
            focus.Request("ChatWindow.FiltersUse");
            // Frame 5: header A (folded before), its node A1 (folded); header B (open before) holds node B1 (folded) holding node B2 (folded),
            // then the setting at x=40 inside B2; node B3 (folded) is drawn after at x=20.
            Check(focus.Open(1,false,true,0,5) && focus.Open(2,false,false,10,5),"opened while looked for");
            focus.Open(3,true,true,0,5);focus.Open(4,false,false,10,5);focus.Open(5,false,false,20,5);
            Check(focus.Reached("ChatWindow.FiltersUse",5,1.0,40)==(true,1f),"found");
            focus.Open(6,false,false,20,5);
            Check(!focus.FoldBack(3) && !focus.FoldBack(4) && !focus.FoldBack(5),"the path (header B, nodes B1 and B2) stays open");
            Check(focus.FoldBack(1) && focus.FoldBack(2),"header A and node A1, folded before, fold back");
            Check(!focus.FoldBack(1),"once only");
            Check(!focus.FoldBack(6),"a node drawn after the setting is not opened by the search (no longer looking)");
        }),
        ("Settings review 1498 M1: a section open before stays open; a setting given up folds back all the search opened; a new request forgets it",() =>
        {
            var path=SettingFocus.PathTo([(1,true,0),(2,false,10),(3,false,10),(4,false,20)],30);
            Check(path.SetEquals([1u,3u,4u]),"path: the last header, then the nodes left of the row going back: "+string.Join(",",path));
            Check(SettingFocus.PathTo([(2,false,10)],10).Count==0,"a node at the row's own edge does not hold it");
            var focus=new SettingFocus();
            focus.Request("Advanced.Wiki");
            focus.Open(1,true,true,0,1);focus.Open(2,false,true,0,1);
            for(var frame=0;frame<SettingFocus.SearchFrames;frame++)focus.Tick();
            Check(!focus.Opening && focus.FoldBack(2) && !focus.FoldBack(1),"given up: the folded one folds back, the open one stays");
            focus.Request("Advanced.Wiki");
            focus.Open(7,false,true,0,1);
            focus.Request("Advanced.Wiki");
            for(var frame=0;frame<SettingFocus.SearchFrames;frame++)focus.Tick();
            Check(!focus.FoldBack(7),"a new request starts afresh");
            Check(!new SettingFocus().Open(1,false,true,0,1),"nothing asked: nothing opened");
        }),
        ("Settings review 1498 M1: the page hands the focus to the headers and tree nodes in a try/finally; they fold back before opening",() =>
        {
            var page=Read("src/TenshoAssistant/MainWindow.Settings.cs");
            Check(Regex.IsMatch(page,@"UiComponents\.Focus=settingFocus;\s*try\{DrawSettingsTabs\(\);\}\s*finally\{TenshoAssistant\.UI\.UiComponents\.Focus=null;\}"),"the focus is given back even when a tab throws");
            var ui=Read("src/TenshoAssistant/UI/UiComponents.cs");
            Check(ui.IndexOf("focus.FoldBack(id)",StringComparison.Ordinal) is > 0 and var fold && fold<ui.IndexOf("focus.Opening",StringComparison.Ordinal),"fold back first");
            Check(Read("src/TenshoAssistant/UI/Components/SettingRow.cs").Contains("ImGui.GetItemRectMin().X)",StringComparison.Ordinal),"the row gives its left edge");
        }),
        ("Settings review 1498 M2: the category chosen is kept in the configuration, written once per new choice, never in the desktop app",() =>
        {
            var chosen=new Dictionary<string,string>();
            Check(SettingsCategoryChoice.Set(chosen,"Map","MapIcons") && chosen["Map"]=="MapIcons","a new choice is written");
            Check(!SettingsCategoryChoice.Set(chosen,"Map","MapIcons"),"the same choice again: nothing to save (not every frame)");
            Check(!SettingsCategoryChoice.Set(chosen,"Map","") && chosen["Map"]=="MapIcons","no empty choice");
            Check(SettingsCategoryChoice.Set(chosen,"Map","MapGame") && chosen.Count==1,"one choice per tab");
            var config=new TenshoAssistant.Configuration();
            Check(config.SettingsCategories is { Count:0 },"empty by default");
            var json=System.Text.Json.JsonSerializer.Serialize(new TenshoAssistant.Configuration{SettingsCategories=new(){["Data"]="Cache"}});
            Check(System.Text.Json.JsonSerializer.Deserialize<TenshoAssistant.Configuration>(json)!.SettingsCategories["Data"]=="Cache","kept through a save and a load");
            var source=Read("src/TenshoAssistant/MainWindow.SettingsCategories.cs");
            Check(source.Contains("configuration.SettingsCategories.GetValueOrDefault(tab)",StringComparison.Ordinal)
                  && source.Contains("UI.SettingsCategoryChoice.Set(configuration.SettingsCategories,tab,category) || inDesktopApp)return;",StringComparison.Ordinal)
                  && source.Contains("TrySave(\"Settings category chosen\")",StringComparison.Ordinal) && !source.Contains("settingsCategoryChosen",StringComparison.Ordinal),"read and saved there");
        }),
        ("Settings review 1498 M6: no setting is drawn by two categories; every map category ends with FinishMapSettings, every chat one saves",() =>
        {
            var places=SettingsTabs999Tests.Walk().SelectMany(step=>Regex.Matches(step.Body,@"\b(?:BoolSetting|IntSetting|SliderSetting|ComboSetting|SettingBox|TextSetting|CheckSetting|SettingButton)(?:<[^>()]*>)?\(\s*""([\w.]+)""")
                .Select(match=>(Key:match.Groups[1].Value,Place:step.Tab+"/"+step.Category))).GroupBy(row=>row.Key)
                .Where(group=>group.Select(row=>row.Place).Distinct().Count()>1).Select(group=>group.Key+": "+string.Join(", ",group.Select(row=>row.Place).Distinct())).ToArray();
            Check(places.Length==0,"a setting in two categories: "+string.Join(" ; ",places));
            var own=SettingsTabs999Tests.Walk().GroupBy(step=>step.Category).ToDictionary(group=>group.Key,group=>group.First());
            var map=SettingsTabs999Tests.CategoryList().Where(category=>category.Tab=="Map").ToArray();
            Check(map.Length==8 && map.All(category=>own[category.Id].Body.Contains("FinishMapSettings(",StringComparison.Ordinal)),"a map category does not write what it changed: "
                  +string.Join(",",map.Where(category=>!own[category.Id].Body.Contains("FinishMapSettings(",StringComparison.Ordinal)).Select(category=>category.Id)));
            var chat=SettingsTabs999Tests.CategoryList().Where(category=>category.Id.StartsWith("Chat",StringComparison.Ordinal)).ToArray();
            Check(chat.Length==5 && chat.All(category=>own[category.Id].Body.Contains("SaveChatSettingsWhenReleased();",StringComparison.Ordinal)),"a chat category does not write what it changed: "
                  +string.Join(",",chat.Where(category=>!own[category.Id].Body.Contains("SaveChatSettingsWhenReleased();",StringComparison.Ordinal)).Select(category=>category.Id)));
        }),
        ("Settings review 1498 M7: the category's title is the shared subtitle; the map's « open a map » line lives with the map's texts",() =>
        {
            var pane=Read("src/TenshoAssistant/UI/Components/CategoryPane.cs");
            Check(pane.Contains("UiComponents.SubTitle(current.Label);",StringComparison.Ordinal) && !pane.Contains("TextColored(",StringComparison.Ordinal),"subtitle");
            Check(Read("src/TenshoAssistant/MainWindow.MapView.cs").Contains("localization.Get(\"MapView.IconsNoMap\")",StringComparison.Ordinal),"map text");
            foreach(var language in new[]{"en","fr"})
            {
                var text=Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                Check(!text.Contains("\"MapNone\"",StringComparison.Ordinal) && text.Contains("\"IconsNoMap\"",StringComparison.Ordinal),language+": moved");
                foreach(var dead in new[]{"SectionGame","SectionQuests","SectionNearby","SectionPlayer","SectionAreas","SectionIcons"})Check(!text.Contains($"\"{dead}\"",StringComparison.Ordinal),$"{language}: {dead} left (M5)");
            }
        }),
    ];
}
