using Dalamud.Bindings.ImGui;
using TenshoAssistant.Inventory;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private string? requestedSettingsTab;
    private void DrawSettings()
    {
        Heading(localization.Get("Settings.Title"),localization.Get("Settings.Description"));
        if(ImGui.BeginTabBar("SettingsSections"))
        {
            try
            {
                SettingsTab("General",DrawGeneralSettings);
                SettingsTab("Interface",DrawInterfaceSettings);
                SettingsTab("Modules",DrawModuleSettings);
                SettingsTab("Data",DrawDataSettings);
                if(developerMode)SettingsTab("Debug",DrawDeveloperSettings);
            }
            finally{ImGui.EndTabBar();}
        }
        if(saveError is not null)ImGui.TextWrapped(saveError);
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
        var openOnLoad = configuration.OpenOnLoad;
        var openOnLogin = configuration.OpenOnLogin;
        var loadChanged = ImGui.Checkbox(localization.Get("Settings.OpenOnLoad"), ref openOnLoad);
        ImGui.TextDisabled(localization.Get("Settings.OpenOnLoadHelp"));
        ImGui.Spacing();
        var loginChanged = ImGui.Checkbox(localization.Get("Settings.OpenOnLogin"), ref openOnLogin);
        ImGui.TextDisabled(localization.Get("Settings.OpenOnLoginHelp"));
        if (loadChanged || loginChanged)
        {
            var previous = configuration.OpenOnLoad;
            var previousLogin = configuration.OpenOnLogin;
            configuration.OpenOnLoad = openOnLoad;
            configuration.OpenOnLogin = openOnLogin;
            try
            {
                save();
                saveError = null;
            }
            catch (Exception error)
            {
                configuration.OpenOnLoad = previous;
                configuration.OpenOnLogin = previousLogin;
                saveError = localization.Get("Common.SaveError");
                log.Error(error, "Could not persist Tensho configuration.");
            }
        }
        ImGui.Spacing(); ImGui.Separator();
    }
    private void DrawInterfaceSettings()
    {
        ImGui.TextColored(Accent, localization.Get("Settings.Interface"));
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
                        try { save(); saveError = null; }
                        catch (Exception error)
                        {
                            configuration.LanguageCode = previousLanguage;
                            localization.SetLanguage(languageCode);
                            saveError = localization.Get("Common.SaveError");
                            log.Warning(error, "Could not save localization preference.");
                        }
                    }
            }
            finally { ImGui.EndCombo(); }
        }
        ImGui.TextDisabled(localization.Format("Localization.LoadedLanguages", localization.AvailableLanguages.Count));
        DrawThemeSetting();
        ImGui.Checkbox(localization.Get("Developer.Enable") + "###DeveloperMode", ref developerMode);
        if (!developerMode) localization.ShowKeys = false;
        ImGui.TextColored(Accent, localization.Get("Settings.ObjectsMarket"));
        var hoverPrices = configuration.MarketPricesOnHover;
        if (ImGui.Checkbox(localization.Get("Settings.MarketHover"), ref hoverPrices))
        {
            var previous = configuration.MarketPricesOnHover;
            configuration.MarketPricesOnHover = hoverPrices;
            try { save(); saveError = null; if (!hoverPrices) market.CancelBackground(); }
            catch (Exception error)
            { configuration.MarketPricesOnHover = previous; saveError = localization.Get("Common.SaveError"); log.Warning(error, "Could not save market hover preference."); }
        }
        ImGui.TextWrapped(localization.Get("InterfaceHelp.PriceHelp"));
        var hoverUsages=configuration.ItemUsagesOnHover;
        if(ImGui.Checkbox(localization.Get("Settings.UsageHover"),ref hoverUsages))
        {
            var previous=configuration.ItemUsagesOnHover;configuration.ItemUsagesOnHover=hoverUsages;
            try{save();saveError=null;}
            catch(Exception error){configuration.ItemUsagesOnHover=previous;saveError=localization.Get("Common.SaveError");log.Warning(error,"Could not save item usage hover preference.");}
        }
        ImGui.TextWrapped(localization.Get("InterfaceHelp.UsageHelp"));
        ImGui.BeginDisabled(!configuration.ItemUsagesOnHover);
        try
        {
            var hoverMonsters=configuration.MonsterSourcesOnHover;
            if(ImGui.Checkbox(localization.Get("Settings.MonsterHover"),ref hoverMonsters))
            {
                var previous=configuration.MonsterSourcesOnHover;configuration.MonsterSourcesOnHover=hoverMonsters;
                try{save();saveError=null;}
                catch(Exception error){configuration.MonsterSourcesOnHover=previous;saveError=localization.Get("Common.SaveError");log.Warning(error,"Could not save monster hover preference.");}
            }
            ImGui.TextWrapped(localization.Get("InterfaceHelp.MonsterHoverHelp"));
        }
        finally{ImGui.EndDisabled();}
        var itemMenu=configuration.ItemContextMenuEnabled;
        if(ImGui.Checkbox(localization.Get("Settings.ContextMenu"),ref itemMenu))
        {
            var previous=configuration.ItemContextMenuEnabled;configuration.ItemContextMenuEnabled=itemMenu;
            try{save();saveError=null;}
            catch(Exception error){configuration.ItemContextMenuEnabled=previous;saveError=localization.Get("Common.SaveError");log.Warning(error,"Could not save item context menu preference.");}
        }
        ImGui.TextWrapped(localization.Get("InterfaceHelp.MenuHelp"));
        ImGui.Spacing(); ImGui.Separator();
    }
    private void DrawModuleSettings()
    {
        ImGui.TextWrapped(localization.Get("Observation.SavedHelp"));
        if(ImGui.CollapsingHeader(localization.Get("ModuleSettings.Craft"),ImGuiTreeNodeFlags.DefaultOpen))
        {
            var logHelper=configuration.CraftLogHelperEnabled;
            if(ImGui.Checkbox(localization.Get("ModuleSettings.CraftLog"),ref logHelper))
            {
                var previous=configuration.CraftLogHelperEnabled;configuration.CraftLogHelperEnabled=logHelper;
                try{save();saveError=null;}
                catch(Exception error){configuration.CraftLogHelperEnabled=previous;saveError=localization.Get("Common.SaveError");log.Warning(error,"Could not save crafting log helper preference.");}
            }
            ImGui.TextWrapped(localization.Get("ModuleSettings.CraftLogHelp"));
            DrawObservationSetting("Craft", localization.Get("ModuleSettings.ObserveCraft"),craftLive.State.Enabled,craftLive.SetEnabled);
            ImGui.TextWrapped(localization.Get("ModuleSettings.CraftHelp"));
            ImGui.TextColored(Accent,localization.Get("ModuleSettings.Search"));
            ImGui.SetNextItemWidth(130);
            if(ImGui.InputInt(localization.Get("ModuleSettings.Steps"),ref craftSolveSteps,0))craftSolveSteps=Math.Clamp(craftSolveSteps,1,100);
            ImGui.SetNextItemWidth(130);
            if(ImGui.InputInt(localization.Get("ModuleSettings.Width"),ref craftSolveWidth,0))craftSolveWidth=Math.Clamp(craftSolveWidth,1,512);
            ImGui.SetNextItemWidth(130);
            if(ImGui.InputInt(localization.Get("ModuleSettings.Budget"),ref craftSolveBudget,0))craftSolveBudget=Math.Clamp(craftSolveBudget,1,500000);
            ValidateCraftSearch();
            ImGui.TextDisabled(localization.Get("ModuleSettings.SearchHelp"));
            ImGui.TextColored(Accent,localization.Get("ModuleSettings.Export"));
            ImGui.SetNextItemWidth(130);
            var exportChanged=ImGui.InputInt(localization.Get("ModuleSettings.Wait"),ref craftMacroExtraWait,0);
            if(exportChanged)craftMacroExtraWait=Math.Clamp(craftMacroExtraWait,0,3);
            exportChanged|=ImGui.Checkbox(localization.Get("ModuleSettings.Echo"),ref craftMacroEcho);
            if(exportChanged){craftMacroExport=null;craftMacroError=null;}
            ImGui.TextDisabled(localization.Get("ModuleSettings.ExportHelp"));
        }
        if(ImGui.CollapsingHeader(localization.Get("ModuleSettings.Gathering"),ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawObservationSetting("Gathering", localization.Get("ModuleSettings.ObserveGathering"),gatheringObservation.State.Enabled,gatheringObservation.SetEnabled);
            ImGui.TextWrapped(localization.Get("ModuleSettings.GatheringHelp"));
        }
        if(ImGui.CollapsingHeader(localization.Get("ModuleSettings.Games"),ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawObservationSetting("Cactpot", localization.Get("ModuleSettings.ObserveCactpot"),cactpotObservation.State.Enabled,enabled=>
            {
                cactpotObservation.SetEnabled(enabled);
                Array.Clear(cactpotBoard);cactpotObservedOwner=0;InvalidateCactpot();
            });
            DrawObservationSetting("Triad", localization.Get("ModuleSettings.ObserveTriad"),triadObservation.State.Enabled,triadObservation.SetEnabled);
            ImGui.TextWrapped(localization.Get("ModuleSettings.GamesHelp"));
        }
        if(ImGui.CollapsingHeader(localization.Get("ModuleSettings.Macros"),ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawObservationSetting("Macros", localization.Get("ModuleSettings.ObserveMacros"),macroObservations.State.Enabled,macroObservations.SetEnabled);
            ImGui.TextWrapped(localization.Get("ModuleSettings.MacroHelp"));
        }
    }
    private void DrawDataSettings()
    {
        DrawCacheSettings();
        if(ImGui.CollapsingHeader(localization.Get("ModuleSettings.Profiles")))
        {
            ImGui.TextWrapped(localization.Get("ModuleSettings.ProfilesHelp"));
            if(ImGui.Button(localization.Get("ModuleSettings.SortProfiles")))page=Page.SortPreview;
            if(ImGui.Button(localization.Get("ModuleSettings.Presets")))page=Page.CraftPresets;
            if(ImGui.Button(localization.Get("ModuleSettings.Bookmarks")))page=Page.Maps;
            if(ImGui.Button(localization.Get("ModuleSettings.Deck")))page=Page.TripleTriad;
            if(ImGui.Button(localization.Get("ModuleSettings.Quests")))page=Page.Quests;
            if(ImGui.Button(localization.Get("ModuleSettings.MacroSequence")))page=Page.MacroChain;
        }
    }
    private void DrawDeveloperSettings()
    {
        if (developerMode && ImGui.CollapsingHeader(localization.Get("Settings.DebugLocalization") + "###LocalizationDebug"))
        {
            var showKeys = localization.ShowKeys;
            if (ImGui.Checkbox(localization.Get("Developer.ShowKeys") + "###ShowTranslationKeys", ref showKeys)) localization.ShowKeys = showKeys;
            if (ImGui.SmallButton(localization.Get("Navigation.Diagnostics"))) page = Page.Diagnostics;
            if (ImGui.SmallButton(localization.Get("Settings.ReloadTranslations"))) localization.Reload();
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

    private void DrawObservationSetting(string id,string label,bool enabled,Action<bool> apply)
    {
        if(!ImGui.Checkbox(label+"###Observe"+id,ref enabled))return;
        configuration.Observations ??= new();
        var hadPrevious=configuration.Observations.TryGetValue(id,out var previous);
        configuration.Observations[id]=enabled;
        try { save();saveError=null; }
        catch(Exception error)
        {
            if(hadPrevious)configuration.Observations[id]=previous;
            else configuration.Observations.Remove(id);
            saveError=localization.Get("Common.SaveError");
            log.Warning(error,"Could not save observation preference {Observation}",id);
            return;
        }
        apply(enabled);
    }

    private void DrawToolSettingsLink(string id)
    {
        if(ImGui.SmallButton(localization.Get("Settings.Title")+"###ToolSettings"+id)){page=Page.Settings;requestedSettingsTab="Modules";IsOpen=true;}
    }
}
