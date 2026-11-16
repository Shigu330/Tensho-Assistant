using System.Text.Json;
using System.Text.RegularExpressions;

// X-449 (the user's request of 15/11): every tab of the Settings page lists its categories on the left and shows the chosen one on the right.
// Here: the categories are the one list the page, the search and the tools' « Settings » links share; every setting lies in exactly one
// category of its own tab; every category has its label in both languages.
internal static class SettingsCategories1496Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static Dictionary<string,string> Labels(string language)
    {
        using var document=JsonDocument.Parse(Read($"src/TenshoAssistant/Localization/Languages/{language}.json").TrimStart('﻿'));
        return document.RootElement.GetProperty("SettingsCategories").EnumerateObject().ToDictionary(property=>property.Name,property=>property.Value.GetString() ?? "");
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Settings categories 1496: every tab of the page lists at least two categories, the page draws them through the shared pane, ids are unique",() =>
        {
            var tabs=Regex.Matches(Read("src/TenshoAssistant/MainWindow.Settings.cs"),@"SettingsTab\(""(\w+)""").Select(match=>match.Groups[1].Value).ToArray();
            var categories=SettingsTabs999Tests.CategoryList();
            Check(categories.Select(category=>category.Id).Distinct().Count()==categories.Count,"a category id twice");
            Check(categories.Select(category=>category.Tab).Distinct().Order().SequenceEqual(tabs.Order()),"the tabs of the categories are the tabs of the page");
            foreach(var tab in tabs)Check(categories.Count(category=>category.Tab==tab)>=2,$"{tab}: fewer than two categories");
            var page=Read("src/TenshoAssistant/MainWindow.Settings.cs");
            Check(page.Contains("DrawSettingsCategories(key)") && Read("src/TenshoAssistant/MainWindow.SettingsCategories.cs").Contains("CategoryPane(\"SettingsCategory\"+tab,categories,ref chosen)")
                  && Read("src/TenshoAssistant/MainWindow.SettingsCategories.cs").Contains("ChooseSettingsCategory(tab,chosen!)"),"each tab draws its categories and keeps the chosen one");
        }),
        ("Settings categories 1496: every category has its label in English and French",() =>
        {
            var ids=SettingsTabs999Tests.CategoryList().Select(category=>category.Id).ToArray();
            foreach(var language in new[]{"en","fr"})
            {
                var labels=Labels(language);
                Check(ids.All(id=>labels.GetValueOrDefault(id) is { Length:>0 }),$"{language}: a category without label: {string.Join(",",ids.Where(id=>!labels.ContainsKey(id)))}");
                Check(labels.Keys.All(key=>ids.Contains(key)),$"{language}: a label of no category: {string.Join(",",labels.Keys.Where(key=>!ids.Contains(key)))}");
            }
        }),
        ("Settings categories 1496: every setting is in one category of its tab (none left out, none lost), the search index says which",() =>
        {
            var scanned=SettingsTabs999Tests.Scan();
            var categories=SettingsTabs999Tests.CategoryList();
            Check(scanned.Count>=224,$"{scanned.Count} settings found, 224 before the categories: a setting left out of every category");
            var index=TenshoAssistant.SettingsIndex.Entries.ToDictionary(entry=>entry.Key,entry=>entry.Tab);
            Check(TenshoAssistant.SettingsIndex.Categories.Count==index.Count && index.Keys.All(TenshoAssistant.SettingsIndex.Categories.ContainsKey),"the index lists a category for every setting");
            foreach(var (key,place) in scanned)
            {
                Check(TenshoAssistant.SettingsIndex.Categories.GetValueOrDefault(key)==place.Category,$"{key}: the index says {TenshoAssistant.SettingsIndex.Categories.GetValueOrDefault(key)}, the code {place.Category} (run the settings index generator)");
                Check(categories.Any(category=>category.Id==place.Category && category.Tab==place.Tab && category.Tab==index[key]),$"{key}: its category is not in its tab");
            }
        }),
        ("Settings categories 1496: a search result opens its tab and its category; the tools' « Settings » links name an existing category of the Panels tab",() =>
        {
            var source=Read("src/TenshoAssistant/MainWindow.Settings.cs");
            Check(source.Contains("OpenSettingsCategory(tab,category,key)") && source.Contains("SettingsIndex.Categories[key]"),"the search opens the category of the setting found");
            var panels=SettingsTabs999Tests.CategoryList().Where(category=>category.Tab=="Panels").Select(category=>category.Id).ToHashSet();
            var linked=Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"MainWindow*.cs").SelectMany(file=>Regex.Matches(File.ReadAllText(file),@"DrawToolSettingsLink\(""\w+"",""(\w+)""\)").Select(match=>match.Groups[1].Value)).ToArray();
            Check(linked.Length>=8 && linked.All(panels.Contains),"a tool link names a category that does not exist: "+string.Join(",",linked.Where(id=>!panels.Contains(id))));
            Check(Read("src/TenshoAssistant/MainWindow.MapView.cs").Contains("requestedSettingsTab=\"Map\";") && SettingsTabs999Tests.CategoryList().Any(category=>category.Tab=="Map"),"the map's cog opens the Map tab, on the category the player chose last");
        }),
    ];
}
