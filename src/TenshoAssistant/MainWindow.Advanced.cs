using System.Text;
using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

/// <summary>
/// Settings › General header options (PC clock, Eorzea time, home tool list, alarm entry of the game's server bar and its tooltip with
/// 1–10 alarms, 0.300–0.302) and Settings › Advanced (0.206): « Restore all defaults » (0.258), developer mode and IDs, community sources
/// on / off (LuminaSupplemental, Garland, wiki, GatherBuddy, Teamcraft bite times, market prices on hover), game alarm command delay,
/// clear alarms, bug report text (no character data). Also the contributor help at the top of Diagnostics (docs, source file of the
/// page, data sources, probes, language completion). Every setting has its tooltip. Drawing only: nothing native is read here.
/// </summary>
internal sealed partial class MainWindow
{
    private const string RepositoryUrl = "https://github.com/Shigu330/Tensho-Assistant";
    private bool clearAlarmsArmed;private int clearAlarmsSeenFrame;

    // WrappedTooltip, Tip, TipText and HelpMarker: UI/Components/Tooltip.cs (X-76 H1).

    // Header and home options (Settings › General).
    private void DrawHeaderSettings()
    {
        ImGui.TextColored(Accent,localization.Get("Advanced.HeaderTitle"));
        BoolSetting("Advanced.HeaderClock","Advanced.HeaderClockTip",()=>configuration.ShowHeaderClock,value=>configuration.ShowHeaderClock=value,c=>c.ShowHeaderClock);
        BoolSetting("Advanced.HeaderEt","Advanced.HeaderEtTip",()=>configuration.ShowHeaderEorzeaTime,value=>configuration.ShowHeaderEorzeaTime=value,c=>c.ShowHeaderEorzeaTime);
        BoolSetting("Advanced.HomeTools","Advanced.HomeToolsTip",()=>configuration.HomeShowTools,value=>configuration.HomeShowTools=value,c=>c.HomeShowTools);
        BoolSetting("Advanced.AlarmServerBar","Advanced.AlarmServerBarTip",()=>configuration.AlarmServerBar,value=>configuration.AlarmServerBar=value,c=>c.AlarmServerBar);
        BoolSetting("Advanced.ReminderServerBar","Advanced.ReminderServerBarTip",()=>configuration.ReminderServerBar,value=>configuration.ReminderServerBar=value,c=>c.ReminderServerBar);
        BoolSetting("Advanced.ReminderFinishedServerBar","Advanced.ReminderFinishedServerBarTip",()=>configuration.ReminderFinishedServerBar,value=>configuration.ReminderFinishedServerBar=value,c=>c.ReminderFinishedServerBar);
        // X-76 step 3 (0.952): the server bar tooltip belongs to the three bar entries above (not to the tracking window it sat under):
        // greyed while none of them is on; its count is read by the alarm entry only, greyed while that entry or the tooltip is off.
        ImGui.Indent();
        ImGui.BeginDisabled(!configuration.AlarmServerBar && !configuration.ReminderServerBar && !configuration.ReminderFinishedServerBar);
        try
        {
            BoolSetting("Advanced.AlarmBarTooltip","Advanced.AlarmBarTooltipTip",()=>configuration.AlarmBarTooltip,value=>configuration.AlarmBarTooltip=value,c=>c.AlarmBarTooltip);
            ImGui.Indent();
            ImGui.BeginDisabled(!configuration.AlarmBarTooltip || !configuration.AlarmServerBar);
            // 0.331 (audit): the sliders follow the drag in memory and save once, when released (no disk write per frame); X-76 H4 (0.914): SliderSetting.
            try{SliderSetting("Advanced.AlarmBarTooltipCount","Advanced.AlarmBarTooltipCountTip",()=>configuration.AlarmBarTooltipCount,value=>configuration.AlarmBarTooltipCount=value,
                1,10,DefaultConfiguration.AlarmBarTooltipCount,width:120);}
            finally{ImGui.EndDisabled();ImGui.Unindent();}
        }
        finally{ImGui.EndDisabled();ImGui.Unindent();}
        // 0.426 (user request): close Tensho when it opens a game window.
        BoolSetting("Advanced.CloseOnGameWindow","Advanced.CloseOnGameWindowTip",()=>configuration.CloseOnGameWindow,value=>configuration.CloseOnGameWindow=value,c=>c.CloseOnGameWindow);
        // 0.415 (g, R-13): pinned tracking window.
        BoolSetting("Tracker.Setting","Tracker.SettingTip",()=>configuration.TrackerWindow,value=>configuration.TrackerWindow=value,c=>c.TrackerWindow);
    }

    private void DrawAdvancedSettings()
    {
        ImGui.TextWrapped(localization.Get("Advanced.Help"));
        ImGui.Separator();
        DrawResetAllSettings();
        ImGui.Separator();
        ImGui.TextColored(Accent,localization.Get("Advanced.DeveloperTitle"));
        var developer=developerMode;
        if(ImGui.Checkbox(localization.Get("Developer.Enable")+"###DeveloperMode",ref developer))
        {
            developerMode=developer;if(!developer)localization.ShowKeys=false;
            configuration.DeveloperMode=developer;
            TrySave("Developer mode",()=>developerMode=configuration.DeveloperMode=!developer);
        }
        Tip("Advanced.DeveloperTip");
        if(DefaultButton("DeveloperMode",developerMode!=DefaultConfiguration.DeveloperMode,OnOff(DefaultConfiguration.DeveloperMode)))
        {
            developerMode=configuration.DeveloperMode=false;localization.ShowKeys=false;
            TrySave("Developer mode",()=>developerMode=configuration.DeveloperMode=true);
        }
        // X-76 step 3 (0.954): developer mode already shows the ids, so the box is greyed then.
        ImGui.BeginDisabled(developerMode);
        try{BoolSetting("Advanced.ShowIds","Advanced.ShowIdsTip",()=>configuration.ShowIds,value=>configuration.ShowIds=value,c=>c.ShowIds);}
        finally{ImGui.EndDisabled();}
        if(developerMode && ImGui.SmallButton(localization.Get("Navigation.Diagnostics")))page=Page.Diagnostics;

        ImGui.Separator();
        ImGui.TextColored(Accent,localization.Get("Advanced.SupportTitle"));
        CopyButton(localization.Get("Advanced.CopyReport"),()=>BugReport());
        Tip("Advanced.CopyReportTip");
        ImGui.SameLine();
        CopyButton(localization.Get("Advanced.CopyRepository"),()=>RepositoryUrl);
        Tip("Advanced.CopyRepositoryTip");
    }

    // X-122, D-101 (user's decision of 2026-11-10): one switch per use of Garland Tools, under its switch, greyed while it is off.
    private void DrawGarlandUses()
    {
        ImGui.Indent();
        var off=!configuration.CommunityGarland;
        if(off)ImGui.BeginDisabled();
        try
        {
            BoolSetting("Advanced.UseGarlandDutyLoot","Advanced.UseGarlandDutyLootTip",()=>configuration.SourceUses.GarlandDutyLoot,value=>configuration.SourceUses.GarlandDutyLoot=value,c=>c.SourceUses.GarlandDutyLoot);
            BoolSetting("Advanced.UseGarlandMonsterLoot","Advanced.UseGarlandMonsterLootTip",()=>configuration.SourceUses.GarlandMonsterLoot,value=>configuration.SourceUses.GarlandMonsterLoot=value,c=>c.SourceUses.GarlandMonsterLoot);
            BoolSetting("Advanced.UseGarlandItemSources","Advanced.UseGarlandItemSourcesTip",()=>configuration.SourceUses.GarlandItemSources,value=>configuration.SourceUses.GarlandItemSources=value,c=>c.SourceUses.GarlandItemSources);
        }
        finally{if(off)ImGui.EndDisabled();ImGui.Unindent();}
    }

    // X-122, D-101: one switch per use of the wiki, under its switch, greyed while it is off.
    private void DrawWikiUses()
    {
        ImGui.Indent();
        var off=!configuration.CommunityWiki;
        if(off)ImGui.BeginDisabled();
        try
        {
            BoolSetting("Advanced.UseWikiMonsters","Advanced.UseWikiMonstersTip",()=>configuration.SourceUses.WikiMonsters,value=>configuration.SourceUses.WikiMonsters=value,c=>c.SourceUses.WikiMonsters);
            BoolSetting("Advanced.UseWikiDutyGuides","Advanced.UseWikiDutyGuidesTip",()=>configuration.SourceUses.WikiDutyGuides,value=>configuration.SourceUses.WikiDutyGuides=value,c=>c.SourceUses.WikiDutyGuides);
            BoolSetting("Advanced.UseWikiBlueMagic","Advanced.UseWikiBlueMagicTip",()=>configuration.SourceUses.WikiBlueMagic,value=>configuration.SourceUses.WikiBlueMagic=value,c=>c.SourceUses.WikiBlueMagic);
            BoolSetting("Advanced.UseWikiBeastmaster","Advanced.UseWikiBeastmasterTip",()=>configuration.SourceUses.WikiBeastmaster,value=>configuration.SourceUses.WikiBeastmaster=value,c=>c.SourceUses.WikiBeastmaster);
            BoolSetting("Advanced.UseWikiItemAcquisition","Advanced.UseWikiItemAcquisitionTip",()=>configuration.SourceUses.WikiItemAcquisition,value=>configuration.SourceUses.WikiItemAcquisition=value,c=>c.SourceUses.WikiItemAcquisition);
            BoolSetting("Advanced.UseWikiVistas","Advanced.UseWikiVistasTip",()=>configuration.SourceUses.WikiVistas,value=>configuration.SourceUses.WikiVistas=value,c=>c.SourceUses.WikiVistas);
            BoolSetting("Advanced.UseWikiIslandAnimals","Advanced.UseWikiIslandAnimalsTip",()=>configuration.SourceUses.WikiIslandAnimals,value=>configuration.SourceUses.WikiIslandAnimals=value,c=>c.SourceUses.WikiIslandAnimals);
        }
        finally{if(off)ImGui.EndDisabled();ImGui.Unindent();}
    }

    // X-100 (0.999, DEC-048): the community sources, now in « Data and sources ».
    private void DrawSourceSettings()
    {
        ImGui.Separator();
        ImGui.TextColored(Accent,localization.Get("Advanced.SourcesTitle"));
        ImGui.TextDisabled(localization.Get("Advanced.SourcesHelp"));
        DrawGameDataOnly();
        if(configuration.GameDataOnly)ImGui.BeginDisabled();
        BoolSetting("Advanced.Lumina","Advanced.LuminaTip",()=>configuration.CommunityLumina,value=>configuration.CommunityLumina=value,c=>c.CommunityLumina);
        BoolSetting("Advanced.Garland","Advanced.GarlandTip",()=>configuration.CommunityGarland,value=>configuration.CommunityGarland=value,c=>c.CommunityGarland);
        DrawGarlandUses();
        BoolSetting("Advanced.Wiki","Advanced.WikiTip",()=>configuration.CommunityWiki,value=>configuration.CommunityWiki=value,c=>c.CommunityWiki);
        DrawWikiUses();
        BoolSetting("Advanced.GatherBuddy","Advanced.GatherBuddyTip",()=>configuration.CommunityGatherBuddy,value=>configuration.CommunityGatherBuddy=value,c=>c.CommunityGatherBuddy);
        BoolSetting("Advanced.Teamcraft","Advanced.TeamcraftTip",()=>configuration.CommunityTeamcraftBites,value=>configuration.CommunityTeamcraftBites=value,c=>c.CommunityTeamcraftBites);
        // 0.321 (review 0.316, point 2): the Fashion Report source had no switch.
        BoolSetting("Advanced.Fashion","Advanced.FashionTip",()=>configuration.CommunityFashion,value=>configuration.CommunityFashion=value,c=>c.CommunityFashion);
        // X-76 step 5 (0.949): PaissaDB (plots for sale), with its switch.
        BoolSetting("Advanced.Paissa","Advanced.PaissaTip",()=>configuration.CommunityPaissa,value=>configuration.CommunityPaissa=value,c=>c.CommunityPaissa);
        // 0.324: Universalis as a whole; « prices on hover » under it, greyed while Universalis is off.
        BoolSetting("Advanced.Universalis","Advanced.UniversalisTip",()=>configuration.CommunityUniversalis,value=>{configuration.CommunityUniversalis=value;if(!value)market.CancelBackground();},c=>c.CommunityUniversalis);
        if(configuration.GameDataOnly)ImGui.EndDisabled();
        ImGui.Indent();
        if(!configuration.CommunityUniversalis)ImGui.BeginDisabled();
        try
        {
            BoolSetting("Settings.MarketHover","Advanced.MarketTip",()=>configuration.MarketPricesOnHover,value=>{configuration.MarketPricesOnHover=value;if(!value)market.CancelBackground();},c=>c.MarketPricesOnHover);
            // 0.343 (I-01): refresh key for the hovered price. X-76 step 3 (0.952): under « prices on hover », greyed while it is off (the
            // hover reader drops the refresh with the prices, ItemHoverCoordinator).
            ImGui.Indent();
            ImGui.BeginDisabled(!configuration.MarketPricesOnHover);
            try{BoolSetting("Advanced.HoverRefresh","Advanced.HoverRefreshTip",()=>configuration.HoverPriceRefresh,value=>configuration.HoverPriceRefresh=value,c=>c.HoverPriceRefresh);}
            finally{ImGui.EndDisabled();ImGui.Unindent();}
            // X-122, D-101: the other uses of Universalis, each with its switch.
            BoolSetting("Advanced.UseUniversalisRetainerCompare","Advanced.UseUniversalisRetainerCompareTip",()=>configuration.SourceUses.UniversalisRetainerCompare,value=>configuration.SourceUses.UniversalisRetainerCompare=value,c=>c.SourceUses.UniversalisRetainerCompare);
            BoolSetting("Advanced.UseUniversalisToolPrices","Advanced.UseUniversalisToolPricesTip",()=>configuration.SourceUses.UniversalisToolPrices,value=>configuration.SourceUses.UniversalisToolPrices=value,c=>c.SourceUses.UniversalisToolPrices);
        }
        finally{if(!configuration.CommunityUniversalis)ImGui.EndDisabled();ImGui.Unindent();}
        // 0.346 (g, I-07): not tied to Universalis: it uses the offers the game shows when you open them.
        BoolSetting("Advanced.Undercut","Advanced.UndercutTip",()=>configuration.UndercutClipboard,value=>configuration.UndercutClipboard=value,c=>c.UndercutClipboard);
        BoolSetting("Advanced.RetainerSales","Advanced.RetainerSalesTip",()=>configuration.RetainerSalesPanel,value=>configuration.RetainerSalesPanel=value,c=>c.RetainerSalesPanel);

    }

    // X-100 (0.999, DEC-048): the game command delay and « remove all alarms », now in « Tracking and notifications ».
    private void DrawAlarmCommandSettings()
    {
        ImGui.Separator();
        ImGui.TextColored(Accent,localization.Get("Advanced.AlarmsTitle"));
        configuration.AlarmOptions??=new();
        // 0.331 (audit): saved once, when the slider is released (it saved the whole configuration on every frame of a drag); 0.260: « ↺ »
        // back to the default delay (read from a fresh Configuration). X-76 H4 (0.914): SliderSetting.
        SliderSetting("Advanced.CommandDelay","Advanced.CommandDelayTip",()=>configuration.AlarmOptions.GameCommandDelayMs,value=>configuration.AlarmOptions.GameCommandDelayMs=value,
            250,5000,DefaultConfiguration.AlarmOptions.GameCommandDelayMs,"%d ms",200);
        // Two clicks: the first arms the button, the second deletes. 0.331 (audit): an arming not drawn on the previous frame (page or
        // tab left) is dropped, as for the alarm groups (0.326).
        if(clearAlarmsArmed && ImGui.GetFrameCount()>clearAlarmsSeenFrame+1)clearAlarmsArmed=false;
        if(ImGui.SmallButton(localization.Get(clearAlarmsArmed ? "Advanced.ClearAlarmsConfirm":"Advanced.ClearAlarms")+"###ClearAlarms"))
        {
            if(!clearAlarmsArmed)clearAlarmsArmed=true;
            else
            {
                clearAlarmsArmed=false;var previous=configuration.Alarms;configuration.Alarms=[];
                try{save();alarmStatus=localization.Get("Advanced.AlarmsCleared");}catch(Exception error){configuration.Alarms=previous;log.Warning(error,"Alarms could not be cleared.");}
            }
        }
        if(clearAlarmsArmed)clearAlarmsSeenFrame=ImGui.GetFrameCount();
        Tip("Advanced.ClearAlarmsTip");

    }

    /// <summary>Text for an issue: version, language, data states and errors, settings that change behaviour; no character data.</summary>
    private string BugReport()
    {
        var text=new StringBuilder();
        text.AppendLine($"Tensho Assistant {BuildVersion} · {localization.CurrentCode} · page {page}");
        text.AppendLine($"Items {itemCatalog.State.Catalog?.Count.ToString() ?? "loading"} {itemCatalog.State.Error}");
        text.AppendLine($"Recipes {recipeCatalog.State.Catalog?.Count.ToString() ?? "loading"} {recipeCatalog.State.Error}");
        text.AppendLine($"Gathering {gatheringCatalog.State.Catalog?.Count.ToString() ?? "loading"} {gatheringCatalog.State.Error}");
        text.AppendLine($"Maps {maps.State.Catalog?.Entries.Count.ToString() ?? "loading"} {maps.State.Error}");
        text.AppendLine($"Weather {weather.State.Catalog?.ZoneCount.ToString() ?? "loading"} {weather.State.Error}");
        text.AppendLine($"Vendors {npcVendors?.State.Catalog?.VendorCount.ToString() ?? "loading"} {npcVendors?.State.Error}");
        text.AppendLine($"Duties {duties.State.Catalog?.Entries.Count.ToString() ?? "loading"}");
        text.AppendLine($"Lumina {(configuration.CommunityLumina ? "on":"off")} {(lumina?.State.Data is { } data ? data.Duties+" duties":lumina?.State.Error ?? "not loaded")}");
        text.AppendLine($"Garland {(configuration.CommunityGarland ? "on":"off")} · Wiki {(configuration.CommunityWiki ? "on":"off")} · Universalis {(configuration.CommunityUniversalis ? "on":"off")} · Market hover {configuration.MarketPricesOnHover} ({market.State.WorldName}) {market.State.Error}");
        text.AppendLine($"Quests {quests.State.Entries.Count} {quests.State.Error} · Collections {collections.State.Entries.Count} {collections.State.Error}");
        text.AppendLine($"Alarms {configuration.Alarms?.Count ?? 0} · in game {configuration.Alarms?.Count(alarm=>alarm.GameName.Length>0) ?? 0} · game list {alarmService?.GameAlarms?.Count.ToString() ?? "?"} · status {alarmService?.GameStatus}");
        // R-12: the webhook address is a secret and never appears here, only whether one is set.
        var discordState=discordNotifier?.State;
        text.AppendLine($"Discord {(configuration.Discord?.Enabled==true ? "on":"off")} · webhook {(string.IsNullOrEmpty(configuration.Discord?.WebhookUrl) ? "empty":DiscordWebhookShape())} · sent {discordState?.Sent ?? 0} failed {discordState?.Failed ?? 0} dropped {discordState?.Dropped ?? 0} · last {discordState?.Status} {discordState?.Code}");
        text.AppendLine($"Context item {itemId}");
        return text.ToString();
    }

    // Developer mode: help for contributors (open source), at the top of Diagnostics.
    private void DrawContributorHelp()
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("Contributor.Title")+"###ContributorHelp",ImGuiTreeNodeFlags.DefaultOpen))return;
        ImGui.TextWrapped(localization.Get("Contributor.Intro"));
        CopyButton(localization.Get("Advanced.CopyRepository")+"##ContribRepo",()=>RepositoryUrl);
        Tip("Advanced.CopyRepositoryTip");
        ImGui.SameLine();
        CopyButton(localization.Get("Advanced.CopyReport")+"##ContribReport",()=>BugReport());
        Tip("Advanced.CopyReportTip");
        ImGui.SameLine();
        CopyButton(localization.Get("Contributor.CopyBuild"),()=>@".\scripts\Verify.ps1");
        Tip("Contributor.CopyBuildTip");
        ImGui.TextDisabled(localization.Get("Contributor.License"));
        if(ImGui.TreeNode(localization.Get("Contributor.DocsTitle")+"###ContribDocs"))
        {
            try
            {
                foreach(var (file,key) in new[]{("README.md","Readme"),("CONTRIBUTING.md","Contributing"),("CLAUDE.md","Agent"),("docs/project/ARCHITECTURE.md","Architecture"),
                    ("docs/project/REFERENCES.md","References"),("docs/evidence/","Evidence"),("CHANGELOG.md","Changelog")})
                {
                    ImGui.TextUnformatted(file);Tip("Contributor.Doc"+key);
                    ImGui.SameLine();ImGui.TextDisabled(localization.Get("Contributor.Doc"+key));
                }
            }
            finally{ImGui.TreePop();}
        }
        if(ImGui.TreeNode(localization.Format("Contributor.CodeTitle",page)+"###ContribCode"))
        {
            try
            {
                ImGui.TextWrapped(localization.Format("Contributor.CodeHere",PageEntryFor(page)?.Source ?? $"MainWindow.{page}.cs"));
                ImGui.TextWrapped(localization.Get("Contributor.CodeRules"));
                foreach(var folder in new[]{"Alarms","Caching","Character","Collections","Community","Credits","Craft","Duties","Fashion","FreeCompany","Gathering","Gear","Inventory","Items","Localization","Macros","Maps","Market","MiniGames","Monsters","Npcs","Quests","Reminders","UI"})
                {ImGui.BulletText(folder+"/");Tip("Contributor.Folder"+folder);}
            }
            finally{ImGui.TreePop();}
        }
        if(ImGui.TreeNode(localization.Get("Contributor.SourcesTitle")+"###ContribSources"))
        {
            try
            {
                ImGui.TextWrapped(localization.Get("Contributor.SourcesRule"));
                SourceLine("Lumina / sqpack","Contributor.SourceGame",true);
                SourceLine("LuminaSupplemental (GPL-3.0)","Contributor.SourceLumina",lumina?.State.Data is not null);
                SourceLine("Garland Tools","Contributor.SourceGarland",configuration.CommunityGarland);
                SourceLine("consolegameswiki","Contributor.SourceWiki",configuration.CommunityWiki);
                SourceLine("Universalis","Contributor.SourceUniversalis",configuration.CommunityUniversalis);
                SourceLine("GatherBuddy (Apache-2.0)","Contributor.SourceGatherBuddy",gatherBuddyFish?.State.Data is not null);
                SourceLine("Teamcraft (MIT)","Contributor.SourceTeamcraft",configuration.CommunityTeamcraftBites);
                SourceLine("Character Panel Refined (MIT)","Contributor.SourceCpr",true);
                SourceLine("SubmarineTracker (MIT)","Contributor.SourceSubmarineTracker",true);
                SourceLine("FFXIVGachaSpreadsheet (MIT)","Contributor.SourceGacha",configuration.CommunityFashion);
            }
            finally{ImGui.TreePop();}
        }
        if(ImGui.TreeNode(localization.Get("Contributor.ProbesTitle")+"###ContribProbes"))
        {
            try
            {
                ImGui.TextWrapped(localization.Get("Contributor.ProbesHelp"));
                foreach(var probe in new[]{"PROBE_SELLS","PROBE_GTYPE","PROBE_ALARMCMD","PROBE_ACH","PROBE_TALK","PROBE_ZONE","PROBE_MARKERS","PROBE_TRIAD","PROBE_GROW","PROBE_LOCAL"})
                {ImGui.BulletText(probe);Tip("Contributor.Probe_"+probe);ImGui.SameLine();ImGui.TextDisabled(localization.Get("Contributor.Probe_"+probe));}
                if(ImGui.SmallButton(localization.Get("Contributor.CopyProbe")))
                    ImGui.SetClipboardText("$env:PROBE_SELLS='41650'; dotnet run --project tools/ShopCurrencyProbe -c Release -- \"<FFXIV>\\game\\sqpack\"");
                Tip("Contributor.CopyProbeTip");
            }
            finally{ImGui.TreePop();}
        }
        if(ImGui.TreeNode(localization.Get("Contributor.LocalizationTitle")+"###ContribLoc"))
        {
            try
            {
                ImGui.TextWrapped(localization.Get("Contributor.LocalizationHelp"));
                var showKeys=localization.ShowKeys;
                if(ImGui.Checkbox(localization.Get("Developer.ShowKeys")+"###ContribShowKeys",ref showKeys))localization.ShowKeys=showKeys;
                Tip("Contributor.ShowKeysTip");
                var report=localization.Validate();
                foreach(var language in report.Languages)ImGui.TextDisabled($"{language.Code}: {language.TranslatedKeys}/{language.TotalKeys} ({language.Completion:P0})");
            }
            finally{ImGui.TreePop();}
        }
        ImGui.Separator();
    }
    private void SourceLine(string name,string key,bool active)
    {
        ImGui.TextColored(active ? Success:Muted,(active ? "● ":"○ ")+name);Tip(key);
        ImGui.SameLine();ImGui.TextDisabled(localization.Get(key));
    }
}
