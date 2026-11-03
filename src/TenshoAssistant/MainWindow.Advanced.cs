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
    // 0.331 (audit): value of the slider being dragged as it was before the drag, put back when the save on release fails.
    private int sliderBefore;

    // Tooltip text wrapped to a readable width (0.207: long help texts went past the screen edge).
    private static void WrappedTooltip(string text)
    {
        ImGui.BeginTooltip();
        try
        {
            ImGui.PushTextWrapPos(ImGui.GetFontSize()*32);
            try{ImGui.TextUnformatted(text);}
            finally{ImGui.PopTextWrapPos();}
        }
        finally{ImGui.EndTooltip();}
    }

    // Tooltip of the last drawn item (also when disabled).
    private void Tip(string key)
    {
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get(key));
    }


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
        // 0.426 (user request): close Tensho when it opens a game window.
        BoolSetting("Advanced.CloseOnGameWindow","Advanced.CloseOnGameWindowTip",()=>configuration.CloseOnGameWindow,value=>configuration.CloseOnGameWindow=value,c=>c.CloseOnGameWindow);
        // 0.415 (g, R-13): pinned tracking window.
        BoolSetting("Tracker.Setting","Tracker.SettingTip",()=>configuration.TrackerWindow,value=>configuration.TrackerWindow=value,c=>c.TrackerWindow);
        ImGui.Indent();
        BoolSetting("Advanced.AlarmBarTooltip","Advanced.AlarmBarTooltipTip",()=>configuration.AlarmBarTooltip,value=>configuration.AlarmBarTooltip=value,c=>c.AlarmBarTooltip);
        // 0.331 (audit): the sliders follow the drag in memory and save once, when released (no disk write per frame).
        var barBefore=configuration.AlarmBarTooltipCount;
        var barCount=Math.Clamp(barBefore,1,10);ImGui.SetNextItemWidth(120);
        if(ImGui.SliderInt(localization.Get("Advanced.AlarmBarTooltipCount")+"###AlarmBarCount",ref barCount,1,10))configuration.AlarmBarTooltipCount=barCount;
        if(ImGui.IsItemActivated())sliderBefore=barBefore;
        if(ImGui.IsItemDeactivatedAfterEdit())
            try{save();saveError=null;}
            catch(Exception error){configuration.AlarmBarTooltipCount=sliderBefore;saveError=localization.Get("Common.SaveError");log.Warning(error,"Setting {Setting} could not be saved.","AlarmBarTooltipCount");}
        Tip("Advanced.AlarmBarTooltipCountTip");
        if(DefaultButton("AlarmBarTooltipCount",configuration.AlarmBarTooltipCount!=DefaultConfiguration.AlarmBarTooltipCount))
            SaveSetting("AlarmBarTooltipCount",()=>configuration.AlarmBarTooltipCount,value=>configuration.AlarmBarTooltipCount=value,DefaultConfiguration.AlarmBarTooltipCount);
        ImGui.Unindent();
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
            try{save();saveError=null;}catch(Exception error){developerMode=configuration.DeveloperMode=!developer;saveError=localization.Get("Common.SaveError");log.Warning(error,"Developer mode could not be saved.");}
        }
        Tip("Advanced.DeveloperTip");
        if(BoolDefaultButton("DeveloperMode",DefaultConfiguration.DeveloperMode,developerMode))
        {
            developerMode=configuration.DeveloperMode=false;localization.ShowKeys=false;
            try{save();saveError=null;}catch(Exception error){developerMode=configuration.DeveloperMode=true;saveError=localization.Get("Common.SaveError");log.Warning(error,"Developer mode could not be saved.");}
        }
        BoolSetting("Advanced.ShowIds","Advanced.ShowIdsTip",()=>configuration.ShowIds,value=>configuration.ShowIds=value,c=>c.ShowIds);
        if(developerMode && ImGui.SmallButton(localization.Get("Navigation.Diagnostics")))page=Page.Diagnostics;

        ImGui.Separator();
        ImGui.TextColored(Accent,localization.Get("Advanced.SourcesTitle"));
        ImGui.TextDisabled(localization.Get("Advanced.SourcesHelp"));
        BoolSetting("Advanced.Lumina","Advanced.LuminaTip",()=>configuration.CommunityLumina,value=>configuration.CommunityLumina=value,c=>c.CommunityLumina);
        BoolSetting("Advanced.Garland","Advanced.GarlandTip",()=>configuration.CommunityGarland,value=>configuration.CommunityGarland=value,c=>c.CommunityGarland);
        BoolSetting("Advanced.Wiki","Advanced.WikiTip",()=>configuration.CommunityWiki,value=>configuration.CommunityWiki=value,c=>c.CommunityWiki);
        BoolSetting("Advanced.GatherBuddy","Advanced.GatherBuddyTip",()=>configuration.CommunityGatherBuddy,value=>configuration.CommunityGatherBuddy=value,c=>c.CommunityGatherBuddy);
        BoolSetting("Advanced.Teamcraft","Advanced.TeamcraftTip",()=>configuration.CommunityTeamcraftBites,value=>configuration.CommunityTeamcraftBites=value,c=>c.CommunityTeamcraftBites);
        // 0.321 (review 0.316, point 2): the Fashion Report source had no switch.
        BoolSetting("Advanced.Fashion","Advanced.FashionTip",()=>configuration.CommunityFashion,value=>configuration.CommunityFashion=value,c=>c.CommunityFashion);
        // 0.324: Universalis as a whole; « prices on hover » under it, greyed while Universalis is off.
        BoolSetting("Advanced.Universalis","Advanced.UniversalisTip",()=>configuration.CommunityUniversalis,value=>{configuration.CommunityUniversalis=value;if(!value)market.CancelBackground();},c=>c.CommunityUniversalis);
        ImGui.Indent();
        if(!configuration.CommunityUniversalis)ImGui.BeginDisabled();
        try
        {
            BoolSetting("Settings.MarketHover","Advanced.MarketTip",()=>configuration.MarketPricesOnHover,value=>{configuration.MarketPricesOnHover=value;if(!value)market.CancelBackground();},c=>c.MarketPricesOnHover);
            // 0.343 (I-01): refresh key for the hovered price.
            BoolSetting("Advanced.HoverRefresh","Advanced.HoverRefreshTip",()=>configuration.HoverPriceRefresh,value=>configuration.HoverPriceRefresh=value,c=>c.HoverPriceRefresh);
        }
        finally{if(!configuration.CommunityUniversalis)ImGui.EndDisabled();ImGui.Unindent();}
        // 0.346 (g, I-07): not tied to Universalis: it uses the offers the game shows when you open them.
        BoolSetting("Advanced.Undercut","Advanced.UndercutTip",()=>configuration.UndercutClipboard,value=>configuration.UndercutClipboard=value,c=>c.UndercutClipboard);

        ImGui.Separator();
        ImGui.TextColored(Accent,localization.Get("Advanced.AlarmsTitle"));
        var options=configuration.AlarmOptions??=new();
        var delayBefore=options.GameCommandDelayMs;var delay=delayBefore;
        ImGui.SetNextItemWidth(200);
        // 0.331 (audit): saved once, when the slider is released (it saved the whole configuration on every frame of a drag).
        if(ImGui.SliderInt(localization.Get("Advanced.CommandDelay")+"###CommandDelay",ref delay,250,5000,"%d ms"))options.GameCommandDelayMs=delay;
        if(ImGui.IsItemActivated())sliderBefore=delayBefore;
        if(ImGui.IsItemDeactivatedAfterEdit())
            try{save();}catch(Exception error){options.GameCommandDelayMs=sliderBefore;log.Warning(error,"Alarm delay could not be saved.");}
        Tip("Advanced.CommandDelayTip");
        // 0.260: « ↺ » back to the default delay (read from a fresh AlarmSettings).
        var defaultDelay=new TenshoAssistant.Alarms.AlarmSettings().GameCommandDelayMs;
        if(DefaultButton("CommandDelay",options.GameCommandDelayMs!=defaultDelay))
        {
            var previous=options.GameCommandDelayMs;options.GameCommandDelayMs=defaultDelay;
            try{save();}catch(Exception error){options.GameCommandDelayMs=previous;log.Warning(error,"Alarm delay could not be saved.");}
        }
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

        ImGui.Separator();
        ImGui.TextColored(Accent,localization.Get("Advanced.SupportTitle"));
        if(ImGui.SmallButton(localization.Get("Advanced.CopyReport")))ImGui.SetClipboardText(BugReport());
        Tip("Advanced.CopyReportTip");
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("Advanced.CopyRepository")))ImGui.SetClipboardText(RepositoryUrl);
        Tip("Advanced.CopyRepositoryTip");
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
        if(ImGui.SmallButton(localization.Get("Advanced.CopyRepository")+"##ContribRepo"))ImGui.SetClipboardText(RepositoryUrl);
        Tip("Advanced.CopyRepositoryTip");
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("Advanced.CopyReport")+"##ContribReport"))ImGui.SetClipboardText(BugReport());
        Tip("Advanced.CopyReportTip");
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("Contributor.CopyBuild")))ImGui.SetClipboardText(@".\scripts\Verify.ps1");
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
                ImGui.TextWrapped(localization.Format("Contributor.CodeHere",PageSource(page)));
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
    // Source file of a page, to start reading the code from what is on screen.
    private static string PageSource(Page shown)=>shown switch
    {
        Page.Home=>"MainWindow.Home.cs",Page.Items=>"MainWindow.Items*.cs, MainWindow.ItemSheet.cs",Page.Storage=>"MainWindow.Storage.cs, MainWindow.Bank.cs",Page.Reminders=>"MainWindow.Reminders.cs, Reminders/",Page.Gathering or Page.GatheringGp=>"MainWindow.Gathering*.cs",
        Page.Garden=>"MainWindow.Garden.cs, MainWindow.GardenCrosses.cs",Page.Maps=>"MainWindow.Maps.cs, MainWindow.MapView.cs",Page.Weather=>"MainWindow.Weather.cs, Gathering/WeatherForecast.cs",
        Page.Alarms=>"MainWindow.Alarms.cs, Alarms/",Page.MacroChain=>"MainWindow.MacroChain*.cs, Macros/",Page.Collections=>"MainWindow.Collections.cs, Collections/",
        Page.TripleTriad=>"MainWindow.TripleTriad.cs, MainWindow.Triad*.cs, MiniGames/",Page.MiniCactpot=>"MainWindow.MiniCactpot.cs, MiniGames/",
        Page.Market=>"MainWindow.Market*.cs, Market/",Page.Quests=>"MainWindow.Quests.cs, Quests/",Page.Chocobo=>"MainWindow.ChocoboBarding.cs, Gear/ChocoboBarding.cs, Gear/ChocoboReader.Dalamud.cs",Page.Events=>"MainWindow.Events.cs, Events/",
        Page.DutyLoot=>"MainWindow.Duties.cs, Duties/",Page.MonsterLoot=>"MainWindow.Monsters.cs, Monsters/",Page.BlueMagic=>"MainWindow.Collections.cs, Collections/",
        Page.Gear=>"MainWindow.Gear.cs, Gear/",Page.Credits=>"MainWindow.Credits.cs, Credits/",Page.FreeCompany=>"MainWindow.FreeCompany.cs, MainWindow.RoutePlanner.cs, MainWindow.VesselBuilds.cs, FreeCompany/",
        Page.Fashion=>"MainWindow.Fashion*.cs, Fashion/",Page.SortPreview=>"MainWindow.Sort*.cs, Inventory/",Page.NpcVendors=>"MainWindow.NpcVendors.cs, Npcs/",
        Page.CraftSimulation or Page.CraftLive or Page.CraftPresets=>"MainWindow.Craft*.cs, Craft/",Page.Settings=>"MainWindow.Settings.cs, MainWindow.Advanced.cs",
        Page.Diagnostics=>"MainWindow.Diagnostics.cs, MainWindow.Advanced.cs",_=>$"MainWindow.{shown}.cs",
    };
}
