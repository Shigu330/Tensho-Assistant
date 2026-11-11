using Dalamud.Bindings.ImGui;
using TenshoAssistant.Inventory;

namespace TenshoAssistant;

// Settings page, in tabs: General (window opening, header clocks), Interface (language, theme, item hover parts, context menu),
// Modules (per-tool options and the opt-in game observations), Map (MainWindow.MapView.cs settings, per-map icon list), Data (Duty
// Finder loot, character panel, automatic storage capture, cache durations, links to saved profiles), Advanced
// (MainWindow.Advanced.cs) and, in developer mode, Debug (translation keys and language report). Every change is saved at once
// and rolled back if the save fails.
internal sealed partial class MainWindow
{
    private string? requestedSettingsTab;
    private void DrawSettings()
    {
        Heading(localization.Get("Settings.Title"),localization.Get("Settings.Description"));
        DrawSettingsSearch();
        if(ImGui.BeginTabBar("SettingsSections"))
        {
            try
            {
                // X-100 (0.999, DEC-048): General, Panels beside the game, Map, Tracking and notifications, Data and sources, Advanced, Debug.
                SettingsTab("General",()=>{DrawGeneralSettings();DrawInterfaceSettings();});
                SettingsTab("Panels",DrawPanelSettings);
                SettingsTab("Map",()=>
                {
                    // The per-map icon list needs a map: the one shown in the Tensho map, else the player's map.
                    var catalog=maps.State.Catalog;
                    var shown=catalog?.Find(mapViewMapId) ?? mapMarkers?.Overview(mapViewMapId) ?? (mapLive?.State is { MapId:>0 } live ? catalog?.Find(live.MapId):null);
                    if(ImGui.Button(localization.Get("MapView.OpenMap")))OpenMapView(0);
                    DrawMapSettingsBody(shown);
                });
                SettingsTab("Tracking",DrawTrackingSettings);
                SettingsTab("Data",DrawDataSettings);
                SettingsTab("Advanced",DrawAdvancedSettings);
                if(developerMode)SettingsTab("Debug",DrawDeveloperSettings);
            }
            finally{ImGui.EndTabBar();}
        }
    }

    // X-76 H4 (0.914): the macro copy settings as saved before the edit not written yet; they come back when saving it fails.
    private TenshoAssistant.Macros.MacroCopySettings? macroCopySaved;
    private void SaveMacroCopy()
    {
        var previous=macroCopySaved!;macroCopySaved=null;
        TrySave("Macro copy settings",()=>configuration.MacroCopy=previous);
    }

    private void SettingsTab(string key,Action draw)
    {
        var selected=requestedSettingsTab==key;
        if(!ImGui.BeginTabItem(localization.Get("SettingsSections."+key)+"###Settings"+key,selected ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None))return;
        if(selected)requestedSettingsTab=null;
        try{draw();}
        finally{ImGui.EndTabItem();}
    }

    private void DrawGeneralSettings()
    {
        ImGui.TextColored(Accent, localization.Get("Settings.Window"));
        // 0.324: shared setting rows (tooltip and « ↺ »).
        // X-76 H6 (0.915): the help is the row's tooltip only (it was also written under the row).
        BoolSetting("Settings.OpenOnLoad","Settings.OpenOnLoadHelp",()=>configuration.OpenOnLoad,value=>configuration.OpenOnLoad=value,c=>c.OpenOnLoad);
        BoolSetting("Settings.OpenOnLogin","Settings.OpenOnLoginHelp",()=>configuration.OpenOnLogin,value=>configuration.OpenOnLogin=value,c=>c.OpenOnLogin);
        BoolSetting("Settings.HelpBanners","SettingTips.HelpBanners",()=>configuration.HelpBanners,value=>configuration.HelpBanners=value,c=>c.HelpBanners);
        // 0.327 (queue step e): opening page, shared row (tooltip and « ↺ »); turning it on remembers the current page at once.
        BoolSetting("Settings.OpenOnLastPage","SettingTips.OpenOnLastPage",()=>configuration.OpenOnLastPage,value=>{configuration.OpenOnLastPage=value;rememberedPage=null;RememberPage();},c=>c.OpenOnLastPage);
        ImGui.Spacing(); ImGui.Separator();
        DrawHeaderSettings();
    }
    private void DrawLanguageSetting()
    {
        var languageCode = localization.CurrentCode;
        var currentLanguage = localization.AvailableLanguages.FirstOrDefault(language => language.Code == languageCode);
        if (ImGui.BeginCombo(localization.Get("Settings.Language") + "###InterfaceLanguage", string.IsNullOrEmpty(currentLanguage.NativeName) ? languageCode : currentLanguage.NativeName))
        {
            try
            {
                foreach (var language in localization.AvailableLanguages)
                    if (ImGui.Selectable(language.NativeName, language.Code == languageCode))
                    {
                        var previousLanguage = configuration.LanguageCode;
                        configuration.LanguageCode = language.Code;
                        localization.SetLanguage(language.Code);
                        // The error text is read after the rollback, in the language put back.
                        TrySave("Language", () => { configuration.LanguageCode = previousLanguage; localization.SetLanguage(languageCode); });
                    }
            }
            finally { ImGui.EndCombo(); }
        }
        Tip("SettingTips.Language");
    }

    private void DrawInterfaceSettings()
    {
        ImGui.TextColored(Accent, localization.Get("Settings.Interface"));
        DrawFontSettings();
        DrawLanguageSetting();
        ImGui.TextDisabled(localization.Format("Localization.LoadedLanguages", localization.AvailableLanguages.Count));
        DrawThemeSetting();
        ImGui.TextColored(Accent, localization.Get("Settings.ObjectsMarket"));
        ImGui.TextDisabled(localization.Get("Advanced.MarketMoved"));
        // 0.324: shared setting rows (tooltip and « ↺ »). 0.447: « monsters on hover » removed (it no longer did anything).
        BoolSetting("Settings.UsageHover","InterfaceHelp.UsageHelp",()=>configuration.ItemUsagesOnHover,value=>configuration.ItemUsagesOnHover=value,c=>c.ItemUsagesOnHover);
        // e) (audit 2026-11-02): the four parts of the panel under the game's item tooltip, under « item uses » that turns that panel on
        // (greyed while it is off). Tensho's own item tooltips and the item sheet always show every part.
        ImGui.Indent();
        ImGui.BeginDisabled(!configuration.ItemUsagesOnHover);
        try
        {
            BoolSetting("Settings.HoverCollection","Settings.HoverCollectionTip",()=>configuration.HoverShowCollection,value=>configuration.HoverShowCollection=value,c=>c.HoverShowCollection);
            BoolSetting("Settings.HoverStock","Settings.HoverStockTip",()=>configuration.HoverShowStock,value=>configuration.HoverShowStock=value,c=>c.HoverShowStock);
            BoolSetting("Settings.HoverUsages","Settings.HoverUsagesTip",()=>configuration.HoverShowUsages,value=>configuration.HoverShowUsages=value,c=>c.HoverShowUsages);
            BoolSetting("Settings.HoverSources","Settings.HoverSourcesTip",()=>configuration.HoverShowSources,value=>configuration.HoverShowSources=value,c=>c.HoverShowSources);
            // 0.377 (g, M-07): comparison with the worn piece.
            BoolSetting("Settings.HoverCompare","Settings.HoverCompareTip",()=>configuration.HoverShowCompare,value=>configuration.HoverShowCompare=value,c=>c.HoverShowCompare);
        }
        finally{ImGui.EndDisabled();ImGui.Unindent();}
        // 0.705 (user request): NPC vendors in item tooltips, set as the list panels are. X-76 step 3 (0.952): out of the « item uses » block,
        // Tensho's own item tooltips (How to get, collection) use it whether that panel is on or not.
        BoolSetting("Settings.HoverVendors","Settings.HoverVendorsTip",()=>configuration.HoverVendors,value=>configuration.HoverVendors=value,c=>c.HoverVendors);
        if(configuration.HoverVendors)
        {
            ImGui.Indent();
            BoolSetting("Settings.HoverVendorsMetOnly","Settings.HoverVendorsMetOnlyTip",()=>configuration.HoverVendorsMetOnly,value=>configuration.HoverVendorsMetOnly=value,c=>c.HoverVendorsMetOnly);
            BoolSetting("Settings.HoverVendorsByCurrency","Settings.HoverVendorsByCurrencyTip",()=>configuration.HoverVendorsByCurrency,value=>configuration.HoverVendorsByCurrency=value,c=>c.HoverVendorsByCurrency);
            if(configuration.HoverVendorsByCurrency)DrawCurrencyTicks("HoverVendor",()=>configuration.HoverVendorCurrenciesOn,value=>configuration.HoverVendorCurrenciesOn=value);
            ImGui.Unindent();
        }
        // e) (audit 2026-11-02): the whole panel under the game's item tooltip (prices included) is left out while bound by duty.
        BoolSetting("Settings.HoverHideInDuty","Settings.HoverHideInDutyTip",()=>configuration.HoverHideInDuty,value=>configuration.HoverHideInDuty=value,c=>c.HoverHideInDuty);
        BoolSetting("Settings.ContextMenu","InterfaceHelp.MenuHelp",()=>configuration.ItemContextMenuEnabled,value=>configuration.ItemContextMenuEnabled=value,c=>c.ItemContextMenuEnabled);
        // X-76 step 3 (0.952): the item sheet and Tensho's item tooltips, not the crafting log helper it was drawn under.
        BoolSetting("ModuleSettings.ShowEnglishItemName","ModuleSettings.ShowEnglishItemNameTip",()=>configuration.ShowEnglishItemName,value=>configuration.ShowEnglishItemName=value,c=>c.ShowEnglishItemName);
        // 0.327 (queue step e): number of item search results, shared row (tooltip and « ↺ »).
        IntSetting("Settings.ItemSearchLimit","SettingTips.ItemSearchLimit",()=>Configuration.BoundedItemSearchLimit(configuration.ItemSearchLimit),value=>configuration.ItemSearchLimit=value,Configuration.MinimumItemSearchLimit,Configuration.MaximumItemSearchLimit,DefaultConfiguration.ItemSearchLimit);
        // e) (audit 2026-11-02): scope the Market page opens on; a choice made on that page is saved here too. Plain names: the region
        // would need the Universalis data centre table, which this page must not ask for.
        // X-76 H5 (0.915): shared combo row; an unknown saved value is shown as the data centre (MarketScopeShown).
        ComboSetting("Settings.MarketScope","Settings.MarketScopeTip",()=>MarketScopeShown,value=>configuration.MarketBoardScope=value,
            Enum.GetValues<TenshoAssistant.Market.MarketScope>().Select(scope=>(scope,localization.Get("Settings.MarketScope"+scope))).ToArray(),DefaultConfiguration.MarketBoardScope,200);
        ImGui.Spacing(); ImGui.Separator();
    }
    // X-100 (0.999, DEC-048): the panels beside the game's windows, then each tool's aids (they were « Modules »).
    private void DrawPanelSettings()
    {
        BoolSetting("DutyFinder.Setting","Advanced.DutyFinderTip",()=>configuration.ShowDutyFinderLoot,value=>configuration.ShowDutyFinderLoot=value,c=>c.ShowDutyFinderLoot);
        BoolSetting("CharacterStats.Setting","Advanced.CharacterPanelTip",()=>configuration.ShowCharacterPanel,value=>configuration.ShowCharacterPanel=value,c=>c.ShowCharacterPanel);
        BoolSetting("Advanced.HidePanelsBehindGame","Advanced.HidePanelsBehindGameTip",()=>configuration.HidePanelsBehindGame,value=>configuration.HidePanelsBehindGame=value,c=>c.HidePanelsBehindGame);
        BoolSetting("Advanced.HideBarsBehindGame","Advanced.HideBarsBehindGameTip",()=>configuration.HideBarsBehindGame,value=>configuration.HideBarsBehindGame=value,c=>c.HideBarsBehindGame);
        ImGui.Spacing();ImGui.Separator();
        DrawModuleSettings();
    }

    // X-100 (0.999, DEC-048): alarms, their game commands, and the server bar entries (also on their own pages).
    private void DrawTrackingSettings()
    {
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("Alarms.SettingsTitle"),ImGuiTreeNodeFlags.DefaultOpen))DrawAlarmSettings();
        DrawAlarmCommandSettings();
        ImGui.Separator();
        BoolSetting("Currencies.ServerBar","Currencies.ServerBarTip",()=>configuration.CurrencyServerBar,value=>configuration.CurrencyServerBar=value,c=>c.CurrencyServerBar);
        BoolSetting("GilJournal.ServerBar","GilJournal.ServerBarTip",()=>configuration.GilServerBar,value=>configuration.GilServerBar=value,c=>c.GilServerBar);
    }

    // X-100 (0.999, DEC-048): a search among every setting of this page (SettingsIndex, kept in step with the code by a test); a click opens its tab.
    private string settingsQuery="";
    private void DrawSettingsSearch()
    {
        ImGui.SetNextItemWidth(280);
        ImGui.InputTextWithHint("##SettingsSearch",localization.Get("Settings.SearchHint"),ref settingsQuery,64);
        Tip("Settings.SearchTip");
        var query=settingsQuery.Trim();
        if(query.Length<2)return;
        var hits=SettingsIndex.Entries.Where(entry=>localization.Get(entry.Key).Contains(query,StringComparison.CurrentCultureIgnoreCase)).Take(15).ToArray();
        if(hits.Length==0){ImGui.TextDisabled(localization.Get("Settings.SearchNone"));return;}
        foreach(var (key,tab) in hits)
            if(ImGui.Selectable($"{localization.Get(key)}  ·  {localization.Get("SettingsSections."+tab)}##SettingHit{key}")){requestedSettingsTab=tab;settingsQuery="";}
        ImGui.Separator();
    }

    private void DrawModuleSettings()
    {
        ImGui.TextWrapped(localization.Get("Observation.SavedHelp"));
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("ModuleSettings.Craft"),ImGuiTreeNodeFlags.DefaultOpen))
        {
            // 0.321: the checkbox saved the configuration on every frame (its « if » only guarded the tooltip); the shared row saves on click.
            BoolSetting("ModuleSettings.CraftLog","SettingTips.CraftLog",()=>configuration.CraftLogHelperEnabled,value=>configuration.CraftLogHelperEnabled=value,c=>c.CraftLogHelperEnabled);
            ImGui.TextWrapped(localization.Get("ModuleSettings.CraftLogHelp"));
            // 0.327 (audit 2026-11-02, queue step e): compute as soon as a recipe is selected (default, as before) or only on request.
            ImGui.Indent();
            ImGui.BeginDisabled(!configuration.CraftLogHelperEnabled);
            try{BoolSetting("ModuleSettings.CraftLogTryAllPresets","ModuleSettings.CraftLogTryAllPresetsTip",()=>configuration.CraftLogTryAllPresets,value=>configuration.CraftLogTryAllPresets=value,c=>c.CraftLogTryAllPresets);BoolSetting("ModuleSettings.CraftLogAutoSolve","ModuleSettings.CraftLogAutoSolveTip",()=>configuration.CraftLogAutoSolve,value=>configuration.CraftLogAutoSolve=value,c=>c.CraftLogAutoSolve);}
            finally{ImGui.EndDisabled();ImGui.Unindent();}
            DrawObservationSetting("Craft", localization.Get("ModuleSettings.ObserveCraft"),craftLive.State.Enabled,craftLive.SetEnabled);
            ImGui.TextWrapped(localization.Get("ModuleSettings.CraftHelp"));
            ImGui.TextColored(Accent,localization.Get("ModuleSettings.Search"));
            // 0.321 (review 0.316, point 6): saved, with « ↺ »; bounds unchanged.
            var search=CraftSearchOptions;var searchDefaults=DefaultConfiguration.CraftSearch;
            // A-03 (0.465, DEC-028): the optional Raphael solver.
            // 0.468 (user request, in-game campaign): the projection window beside the synthesis, also here.
            DrawCraftLiveSettings();
            BoolSetting("CraftSearch.SettingRaphael","CraftSearch.SolverRaphaelTip",()=>search.Raphael,value=>search.Raphael=value,c=>c.CraftSearch?.Raphael==true);
            // X-76 step 3 (0.952): Tensho's own search limits, under Raphael and greyed while it is on (Raphael reads none of them; the
            // crafting log still falls back to Tensho's search when Raphael fails, their tips say so).
            ImGui.Indent();
            ImGui.BeginDisabled(search.Raphael);
            try
            {
                IntSetting("ModuleSettings.Steps","SettingTips.Steps",()=>search.Steps,value=>search.Steps=value,1,100,searchDefaults.Steps);
                IntSetting("ModuleSettings.Width","SettingTips.Width",()=>search.Width,value=>search.Width=value,1,512,searchDefaults.Width);
                IntSetting("ModuleSettings.Budget","SettingTips.Budget",()=>search.Budget,value=>search.Budget=value,1,500000,searchDefaults.Budget);
            }
            finally{ImGui.EndDisabled();ImGui.Unindent();}
            ValidateCraftSearch();
            ImGui.TextDisabled(localization.Get("ModuleSettings.SearchHelp"));
            // 0.438 (user request): actions the player forbids, everywhere crafting actions are proposed.
            DrawForbiddenActions(search);
            // X-76 step 3 (0.952): a simulator option, above the Export heading.
            BoolSetting("ModuleSettings.AutoSimulate","ModuleSettings.AutoSimulateTip",()=>configuration.CraftSearch.AutoSimulate,value=>configuration.CraftSearch.AutoSimulate=value,c=>c.CraftSearch.AutoSimulate);
            ImGui.TextColored(Accent,localization.Get("ModuleSettings.Export"));
            var exportBefore=(search.ExtraWait,search.Echo,search.BuffExtraWait,search.EchoSound,search.EchoLastOnly);
            IntSetting("ModuleSettings.Wait","SettingTips.Wait",()=>search.ExtraWait,value=>search.ExtraWait=value,0,3,searchDefaults.ExtraWait);
            // X-12 (0.419): the buffs' extra wait apart (shown as the actions' one until changed).
            IntSetting("ModuleSettings.BuffWait","SettingTips.BuffWait",()=>search.BuffExtraWait ?? search.ExtraWait,value=>search.BuffExtraWait=value==search.ExtraWait ? null:value,0,3,search.ExtraWait);
            BoolSetting("ModuleSettings.Echo","SettingTips.Echo",()=>configuration.CraftSearch.Echo,value=>configuration.CraftSearch.Echo=value,c=>c.CraftSearch.Echo);
            // X-76 step 3 (0.952): the echo's sound and « last only » are read only with the echo (CraftMacroExport), greyed while it is off.
            ImGui.Indent();
            ImGui.BeginDisabled(!configuration.CraftSearch.Echo);
            try
            {
                IntSetting("ModuleSettings.EchoSound","ModuleSettings.EchoSoundTip",()=>search.EchoSound,value=>search.EchoSound=value,0,16,searchDefaults.EchoSound);
                // 0.1097 (user request): try the sound, as the alarms' « Listen » button (same service, same texts).
                ImGui.SameLine();
                ImGui.BeginDisabled(!Alarms.MacroSound.Valid(search.EchoSound));
                try{if(ImGui.SmallButton(localization.Get("Alarms.Try")+"##EchoSoundTry"))alarmService?.PreviewChatSound(search.EchoSound);}
                finally{ImGui.EndDisabled();}
                Tip("Advanced.TryTip");
                BoolSetting("ModuleSettings.EchoLastOnly","ModuleSettings.EchoLastOnlyTip",()=>configuration.CraftSearch.EchoLastOnly,value=>configuration.CraftSearch.EchoLastOnly=value,c=>c.CraftSearch.EchoLastOnly);
            }
            finally{ImGui.EndDisabled();ImGui.Unindent();}
            if(exportBefore!=(search.ExtraWait,search.Echo,search.BuffExtraWait,search.EchoSound,search.EchoLastOnly)){craftMacroExport=null;craftMacroError=null;}
            ImGui.TextDisabled(localization.Get("ModuleSettings.ExportHelp"));
            // Game macros used by « Copy into the game's macros » (0.191).
            var copy=configuration.MacroCopy ??= new();
            var start=copy.Start;var end=copy.End;var prefix=copy.Prefix;
            var copyChanged=false;
            var macroDefaults=DefaultConfiguration.MacroCopy;
            // X-76 H5 (0.915): the book and the two switches are shared rows, saved on click; the slot range and the name prefix below
            // are typed, bound to each other and written once no field is being typed in any more.
            ComboSetting("ModuleSettings.MacroBook","SettingTips.MacroBook",()=>copy.Book,value=>copy.Book=value,
                [(0,localization.Get("CraftMacro.Individual")),(1,localization.Get("CraftMacro.Shared"))],macroDefaults.Book);
            ImGui.SetNextItemWidth(100);copyChanged|=ImGui.InputInt(localization.Get("ModuleSettings.MacroStart"),ref start,0);
            Tip("SettingTips.MacroStart");
            if(DefaultButton("MacroStart",start!=macroDefaults.Start)){start=macroDefaults.Start;if(end<start)end=macroDefaults.End;copyChanged=true;}
            ImGui.SetNextItemWidth(100);copyChanged|=ImGui.InputInt(localization.Get("ModuleSettings.MacroEnd"),ref end,0);
            Tip("SettingTips.MacroEnd");
            if(DefaultButton("MacroEnd",end!=macroDefaults.End)){end=macroDefaults.End;if(start>end)start=macroDefaults.Start;copyChanged=true;}
            ImGui.SetNextItemWidth(160);copyChanged|=ImGui.InputText(localization.Get("ModuleSettings.MacroPrefix"),ref prefix,12);
            Tip("SettingTips.MacroPrefix");
            if(DefaultButton("MacroPrefix",prefix!=macroDefaults.Prefix)){prefix=macroDefaults.Prefix;copyChanged=true;}
            BoolSetting("ModuleSettings.MacroOverwrite","SettingTips.MacroOverwrite",()=>copy.OverwriteAny,value=>copy.OverwriteAny=value,c=>c.MacroCopy.OverwriteAny);
            BoolSetting("ModuleSettings.MacroChain","SettingTips.MacroChain",()=>copy.Chain,value=>{copy.Chain=value;craftMacroExport=null;},c=>c.MacroCopy.Chain);
            if(copyChanged)
            {
                start=Math.Clamp(start,0,99);end=Math.Clamp(end,start,99);
                // X-76 H4 (0.914): applied at once, written once no field is being typed in any more (it was written on every key press).
                macroCopySaved??=configuration.MacroCopy;
                configuration.MacroCopy=new(){Book=copy.Book,Start=start,End=end,Prefix=prefix.Trim(),OverwriteAny=copy.OverwriteAny,Chain=copy.Chain};
            }
            if(macroCopySaved is not null && !ImGui.IsAnyItemActive())SaveMacroCopy();
            ImGui.TextDisabled(localization.Get("ModuleSettings.MacroCopyHelp"));
        }
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("ModuleSettings.Gathering"),ImGuiTreeNodeFlags.DefaultOpen))
        {
            // 0.330 (audit): the gathering help needs the node observation, both rows go through SaveGatheringChoice. « Observe » shows the
            // reader's state, on by default through the help (rule applied at load).
            if(ObservationRow("Gathering",localization.Get("ModuleSettings.ObserveGathering"),gatheringObservation.State.Enabled,GatheringObservationWanted(DefaultConfiguration),"ModuleSettings.ObserveGatheringTip") is { } observe)
                SaveGatheringChoice(observe,observe ? null:false);
            BoolSetting("FishingPanel.Setting","FishingPanel.SettingTip",()=>configuration.FishingPanel,value=>configuration.FishingPanel=value,c=>c.FishingPanel);
            // 0.399 (user request): « Go there » teleports at once.
            BoolSetting("GatheringDetails.GoThereSetting","GatheringDetails.GoThereSettingTip",()=>configuration.GoThereTeleport,value=>configuration.GoThereTeleport=value,c=>c.GoThereTeleport);
            // 0.412 (g, R-10): « Go there » also wears the job's gear set.
            BoolSetting("GatheringDetails.GoThereEquipSetting","GatheringDetails.GoThereEquipSettingTip",()=>configuration.GoThereEquip,value=>configuration.GoThereEquip=value,c=>c.GoThereEquip);
            var gatheringPanel=configuration.GatheringPanel;
            if(ImGui.Checkbox(localization.Get("GatheringPanel.Setting")+"###GatheringPanel.Setting",ref gatheringPanel))SaveGatheringChoice(gatheringPanel ? true:null,gatheringPanel);
            Tip("GatheringPanel.SettingTip");
            if(DefaultButton("GatheringPanel.Setting",configuration.GatheringPanel!=DefaultConfiguration.GatheringPanel,OnOff(DefaultConfiguration.GatheringPanel)))
                SaveGatheringChoice(DefaultConfiguration.GatheringPanel ? true:null,DefaultConfiguration.GatheringPanel);
            ImGui.TextWrapped(localization.Get("ModuleSettings.GatheringHelp"));
            DrawGpPlanSettings();
        }
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("Gear.Title"),ImGuiTreeNodeFlags.DefaultOpen))DrawGearSettings();
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("ModuleSettings.Games"),ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawObservationSetting("Cactpot", localization.Get("ModuleSettings.ObserveCactpot"),cactpotObservation.State.Enabled,enabled=>
            {
                cactpotObservation.SetEnabled(enabled);
                Array.Clear(cactpotBoard);cactpotObservedOwner=0;InvalidateCactpot();
            });
            DrawObservationSetting("Triad", localization.Get("ModuleSettings.ObserveTriad"),triadObservation.State.Enabled,triadObservation.SetEnabled);
            // 0.327 (audit 2026-11-02, queue step e): one switch per in-game aid, all on as before; they act only while the observation is on.
            ImGui.Indent();
            ImGui.BeginDisabled(!triadObservation.State.Enabled);
            try
            {
                BoolSetting("ModuleSettings.TriadMatchOverlay","ModuleSettings.TriadMatchOverlayTip",()=>configuration.TriadMatchOverlay,value=>configuration.TriadMatchOverlay=value,c=>c.TriadMatchOverlay);
                BoolSetting("ModuleSettings.TriadChallengePanel","ModuleSettings.TriadChallengePanelTip",()=>configuration.TriadChallengePanel,value=>configuration.TriadChallengePanel=value,c=>c.TriadChallengePanel);
                BoolSetting("ModuleSettings.TriadDeckEditor","ModuleSettings.TriadDeckEditorTip",()=>configuration.TriadDeckEditorHighlight,value=>configuration.TriadDeckEditorHighlight=value,c=>c.TriadDeckEditorHighlight);
            }
            finally{ImGui.EndDisabled();ImGui.Unindent();}
            ImGui.TextWrapped(localization.Get("ModuleSettings.GamesHelp"));
        }
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("ModuleSettings.Macros"),ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawObservationSetting("Macros", localization.Get("ModuleSettings.ObserveMacros"),macroObservations.State.Enabled,macroObservations.SetEnabled);
            ImGui.TextWrapped(localization.Get("ModuleSettings.MacroHelp"));
        }
    }
    private void DrawDataSettings()
    {
        // 0.324: shared setting rows (tooltip and « ↺ »).
        BoolSetting("BankUi.AutoSetting","BankUi.AutoSettingHelp",()=>configuration.AutoBankCapture,value=>configuration.AutoBankCapture=value,c=>c.AutoBankCapture);
        DrawCacheSettings();
        DrawSourceSettings();
        if(TenshoAssistant.UI.UiComponents.Header(localization.Get("ModuleSettings.Profiles")))
        {
            ImGui.TextWrapped(localization.Get("ModuleSettings.ProfilesHelp"));
            if(ImGui.Button(localization.Get("ModuleSettings.SortProfiles")))page=Page.SortPreview;
            if(ImGui.Button(localization.Get("ModuleSettings.Presets")))page=Page.CraftPresets;
            if(ImGui.Button(localization.Get("ModuleSettings.Bookmarks"))){page=Page.Maps;mapsTabRequest=1;}
            if(ImGui.Button(localization.Get("ModuleSettings.Deck")))OpenTriadTab("Deck");
            if(ImGui.Button(localization.Get("ModuleSettings.Quests")))page=Page.Quests;
            if(ImGui.Button(localization.Get("ModuleSettings.MacroSequence")))page=Page.MacroChain;
        }
    }
    private void DrawDeveloperSettings()
    {
        if (developerMode && TenshoAssistant.UI.UiComponents.Header(localization.Get("Settings.DebugLocalization") + "###LocalizationDebug"))
        {
            // X-76 H6 (0.915): « Show translation keys » is in one place, Settings › Advanced › Localization, where contributors are sent
            // and which is shown without developer mode; this developer tab keeps the reload and the full report.
            if (ImGui.SmallButton(localization.Get("Navigation.Diagnostics"))) page = Page.Diagnostics;
            if (ImGui.SmallButton(localization.Get("Settings.ReloadTranslations"))) localization.Reload();
            Tip("Advanced.ReloadTip");
            var report = localization.Validate();
            ImGui.TextDisabled(localization.Format("Localization.ReferenceSummary", report.ReferenceKeyCount, report.LanguageCount));
            foreach (var language in report.Languages)
            {
                ImGui.TextUnformatted($"{language.Code}: {language.TranslatedKeys}/{language.TotalKeys} ({language.Completion:P0})");
                foreach (var key in language.MissingKeys) ImGui.TextDisabled($"- {key}");
            }
            foreach (var error in report.Errors) ImGui.TextColored(TenshoAssistant.UI.FfxivTheme.Error, error);
        }
        ImGui.Spacing(); ImGui.Separator();
    }

    // 0.330 (audit): « ↺ » like every setting row; the default is the one applied at load (Plugin.cs), off for these observations.
    private void DrawObservationSetting(string id,string label,bool enabled,Action<bool> apply)
    {
        if(ObservationRow(id,label,enabled,false,"Advanced.ObservationTip") is not { } value)return;
        configuration.Observations ??= new();
        var hadPrevious=configuration.Observations.TryGetValue(id,out var previous);
        configuration.Observations[id]=value;
        if(!TrySave("Observation "+id,()=>{if(hadPrevious)configuration.Observations[id]=previous;else configuration.Observations.Remove(id);}))return;
        apply(value);
    }

    /// <summary>An observation checkbox with its tooltip and « ↺ »; the value to save when it is ticked, unticked or reset, else null.</summary>
    private bool? ObservationRow(string id,string label,bool enabled,bool fallback,string tipKey)
    {
        var clicked=ImGui.Checkbox(label+"###Observe"+id,ref enabled);
        Tip(tipKey);
        if(DefaultButton("Observe"+id,enabled!=fallback,OnOff(fallback))){enabled=fallback;clicked=true;}
        return clicked ? enabled:null;
    }

    // 0.330 (audit): one rule for the gathering observation and the gathering help, which reads it. Unticking the observation also turns the
    // help off, ticking the help also saves the observation (null = unchanged), in one save rolled back together; the reader then follows
    // GatheringObservationWanted, as at load and after « Restore all defaults ».
    private void SaveGatheringChoice(bool? observe,bool? panel)
    {
        var observations=configuration.Observations ??= new();
        var hadPrevious=observations.TryGetValue("Gathering",out var previous);var previousPanel=configuration.GatheringPanel;
        if(observe is { } wanted)observations["Gathering"]=wanted;
        if(panel is { } shown)configuration.GatheringPanel=shown;
        if(!TrySave("Gathering observation and help",()=>
        {
            if(hadPrevious)observations["Gathering"]=previous;else observations.Remove("Gathering");
            configuration.GatheringPanel=previousPanel;
        }))return;
        ApplyGatheringObservation();
    }

    /// <summary>The gathering node is read while its observation is chosen or the gathering help is on (also the rule at load, Plugin.cs).</summary>
    internal static bool GatheringObservationWanted(Configuration settings)=>settings.Observations?.GetValueOrDefault("Gathering")==true || settings.GatheringPanel;
    private void ApplyGatheringObservation()=>gatheringObservation.SetEnabled(GatheringObservationWanted(configuration));

    private void DrawToolSettingsLink(string id)
    {
        if(ImGui.SmallButton(localization.Get("Settings.Title")+"###ToolSettings"+id)){page=Page.Settings;requestedSettingsTab="Panels";IsOpen=true;}
    }
}
