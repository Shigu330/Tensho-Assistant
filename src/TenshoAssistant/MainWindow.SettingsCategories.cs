using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

// X-449 (the user's request of 15/11, DEC-048 completed): each tab of the Settings page lists its categories on the left and shows the
// chosen one on the right (CategoryPane). Every setting is drawn by exactly one category; the search index (SettingsIndex, made from
// this list by the index generator) says which. A category's label is « SettingsCategories.<Id> ».
internal sealed partial class MainWindow
{
    internal readonly record struct SettingsCategory(string Tab,string Id,Action Draw);

    private SettingsCategory[]? settingsCategories;
    private SettingsCategory[] SettingsCategories=>settingsCategories??=
    [
        new("General","WindowOpening",DrawWindowOpeningSettings),
        new("General","Header",DrawHeaderSettings),
        new("General","Appearance",DrawAppearanceSettings),
        new("General","Fonts",DrawFontSettings),
        new("General","ItemTooltips",DrawItemTooltipSettings),
        new("General","ItemsMarket",DrawItemMarketSettings),
        new("Panels","GamePanels",DrawGamePanelSettings),
        new("Panels","Crafting",DrawCraftHelperSettings),
        new("Panels","CraftMacros",DrawCraftMacroSettings),
        new("Panels","Gathering",DrawGatheringSettings),
        new("Panels","Gear",DrawGearSettings),
        new("Panels","Games",DrawGamesSettings),
        new("Panels","Macros",DrawMacroObservationSettings),
        new("Panels","ChatWindow",DrawChatLookCategory),
        new("Panels","ChatHistory",DrawChatHistoryCategory),
        new("Panels","ChatTabs",DrawChatTabsCategory),
        new("Panels","ChatColours",DrawChatColoursCategory),
        new("Panels","ChatPlayers",DrawChatPlayersCategory),
        new("Map","MapGame",DrawMapGameSettings),
        new("Map","MapQuests",DrawMapQuestSettings),
        new("Map","MapNearby",DrawMapNearbySettings),
        new("Map","MapPlayer",DrawMapPlayerSettings),
        new("Map","MapAppearance",DrawMapAppearanceSettings),
        new("Map","MapAreas",DrawMapAreaSettings),
        new("Map","MapIcons",DrawMapIconSettings),
        new("Map","MapWindow",DrawMapWindowSettings),
        new("Tracking","Alarms",DrawAlarmSettings),
        new("Tracking","AlarmCommands",DrawAlarmCommandSettings),
        new("Tracking","ServerBar",DrawTrackingServerBarSettings),
        new("Data","Storage",DrawStorageSettings),
        new("Data","Cache",DrawCacheSettings),
        new("Data","Sources",DrawSourceSettings),
        new("Data","Profiles",DrawProfileSettings),
        new("Advanced","Restore",DrawRestoreSettings),
        new("Advanced","Developer",DrawDeveloperModeSettings),
        new("Advanced","Desktop",DrawDesktopSettings),
        new("Advanced","Support",DrawSupportSettings),
        new("Debug","DebugTools",DrawDebugToolSettings),
        new("Debug","DebugLanguages",DrawDebugLanguageSettings),
    ];

    // The category chosen in each tab, kept while the plugin is loaded; the search and the tools' « Settings » links set it.
    private readonly Dictionary<string,string> settingsCategoryChosen=[];

    private void DrawSettingsCategories(string tab)
    {
        var categories=SettingsCategories.Where(category=>category.Tab==tab)
            .Select(category=>(category.Id,localization.Get("SettingsCategories."+category.Id),category.Draw)).ToArray();
        var chosen=settingsCategoryChosen.GetValueOrDefault(tab);
        CategoryPane("SettingsCategory"+tab,categories,ref chosen);
        settingsCategoryChosen[tab]=chosen!;
    }

    /// <summary>
    /// Opens a setting's place: its tab and its category (the category of a setting is in <see cref="SettingsIndex.Categories"/>); with the
    /// label key of a setting found by the search (X-507), the page then opens the sections around it, scrolls it into view and highlights it.
    /// </summary>
    private void OpenSettingsCategory(string tab,string category,string? settingKey=null)
    {
        requestedSettingsTab=tab;
        settingsCategoryChosen[tab]=category;
        if(settingKey is not null)settingFocus.Request(settingKey);
    }
}
