using System.Text;
using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

/// <summary>
/// Settings › Advanced (0.206) and the contributor help of the developer mode: every setting carries a tooltip that says what it does,
/// what it costs and where its data comes from.
/// </summary>
internal sealed partial class MainWindow
{
    private const string RepositoryUrl = "https://github.com/Shigu330/Tensho-Assistant";
    private bool clearAlarmsArmed;

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

    // A saved boolean setting with its tooltip; the value is restored when saving fails.
    private void BoolSetting(string labelKey,string tipKey,Func<bool> read,Action<bool> write)
    {
        var value=read();
        if(ImGui.Checkbox(localization.Get(labelKey)+"###"+labelKey,ref value))
        {
            var previous=read();write(value);
            try{save();saveError=null;}
            catch(Exception error){write(previous);saveError=localization.Get("Common.SaveError");log.Warning(error,"Setting {Setting} could not be saved.",labelKey);}
        }
        Tip(tipKey);
    }

    // Header and home options (Settings › General).
    private void DrawHeaderSettings()
    {
        ImGui.TextColored(Accent,localization.Get("Advanced.HeaderTitle"));
        BoolSetting("Advanced.HeaderClock","Advanced.HeaderClockTip",()=>configuration.ShowHeaderClock,value=>configuration.ShowHeaderClock=value);
        BoolSetting("Advanced.HeaderEt","Advanced.HeaderEtTip",()=>configuration.ShowHeaderEorzeaTime,value=>configuration.ShowHeaderEorzeaTime=value);
        BoolSetting("Advanced.HomeTools","Advanced.HomeToolsTip",()=>configuration.HomeShowTools,value=>configuration.HomeShowTools=value);
    }

    private void DrawAdvancedSettings()
    {
        ImGui.TextWrapped(localization.Get("Advanced.Help"));
        ImGui.Separator();
        ImGui.TextColored(Accent,localization.Get("Advanced.DeveloperTitle"));
        var developer=developerMode;
        if(ImGui.Checkbox(localization.Get("Developer.Enable")+"###DeveloperMode",ref developer))
        {
            developerMode=developer;if(!developer)localization.ShowKeys=false;
            configuration.DeveloperMode=developer;
            try{save();saveError=null;}catch(Exception error){saveError=localization.Get("Common.SaveError");log.Warning(error,"Developer mode could not be saved.");}
        }
        Tip("Advanced.DeveloperTip");
        BoolSetting("Advanced.ShowIds","Advanced.ShowIdsTip",()=>configuration.ShowIds,value=>configuration.ShowIds=value);
        if(developerMode && ImGui.SmallButton(localization.Get("Navigation.Diagnostics")))page=Page.Diagnostics;

        ImGui.Separator();
        ImGui.TextColored(Accent,localization.Get("Advanced.SourcesTitle"));
        ImGui.TextDisabled(localization.Get("Advanced.SourcesHelp"));
        BoolSetting("Advanced.Lumina","Advanced.LuminaTip",()=>configuration.CommunityLumina,value=>configuration.CommunityLumina=value);
        BoolSetting("Advanced.Garland","Advanced.GarlandTip",()=>configuration.CommunityGarland,value=>configuration.CommunityGarland=value);
        BoolSetting("Advanced.Wiki","Advanced.WikiTip",()=>configuration.CommunityWiki,value=>configuration.CommunityWiki=value);
        BoolSetting("Advanced.GatherBuddy","Advanced.GatherBuddyTip",()=>configuration.CommunityGatherBuddy,value=>configuration.CommunityGatherBuddy=value);
        BoolSetting("Advanced.Teamcraft","Advanced.TeamcraftTip",()=>configuration.CommunityTeamcraftBites,value=>configuration.CommunityTeamcraftBites=value);
        BoolSetting("Settings.MarketHover","Advanced.MarketTip",()=>configuration.MarketPricesOnHover,value=>{configuration.MarketPricesOnHover=value;if(!value)market.CancelBackground();});

        ImGui.Separator();
        ImGui.TextColored(Accent,localization.Get("Advanced.AlarmsTitle"));
        var options=configuration.AlarmOptions??=new();
        var delay=options.GameCommandDelayMs;
        ImGui.SetNextItemWidth(200);
        if(ImGui.SliderInt(localization.Get("Advanced.CommandDelay")+"###CommandDelay",ref delay,250,5000,"%d ms"))
        {
            var previous=options.GameCommandDelayMs;options.GameCommandDelayMs=delay;
            try{save();}catch(Exception error){options.GameCommandDelayMs=previous;log.Warning(error,"Alarm delay could not be saved.");}
        }
        Tip("Advanced.CommandDelayTip");
        // Two clicks: the first arms the button, the second deletes.
        if(ImGui.SmallButton(localization.Get(clearAlarmsArmed ? "Advanced.ClearAlarmsConfirm":"Advanced.ClearAlarms")+"###ClearAlarms"))
        {
            if(!clearAlarmsArmed)clearAlarmsArmed=true;
            else
            {
                clearAlarmsArmed=false;var previous=configuration.Alarms;configuration.Alarms=[];
                try{save();alarmStatus=localization.Get("Advanced.AlarmsCleared");}catch(Exception error){configuration.Alarms=previous;log.Warning(error,"Alarms could not be cleared.");}
            }
        }
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
        text.AppendLine($"Garland {(configuration.CommunityGarland ? "on":"off")} · Wiki {(configuration.CommunityWiki ? "on":"off")} · Market hover {configuration.MarketPricesOnHover} ({market.State.WorldName}) {market.State.Error}");
        text.AppendLine($"Quests {quests.State.Entries.Count} {quests.State.Error} · Collections {collections.State.Entries.Count} {collections.State.Error}");
        text.AppendLine($"Alarms {configuration.Alarms?.Count ?? 0} · in game {configuration.Alarms?.Count(alarm=>alarm.GameName.Length>0) ?? 0} · game list {alarmService?.GameAlarms?.Count.ToString() ?? "?"} · status {alarmService?.GameStatus}");
        text.AppendLine($"Context item {itemId}");
        return text.ToString();
    }

    // Developer mode: help for contributors (open source), at the top of Diagnostics.
    private void DrawContributorHelp()
    {
        if(!ImGui.CollapsingHeader(localization.Get("Contributor.Title")+"###ContributorHelp",ImGuiTreeNodeFlags.DefaultOpen))return;
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
                foreach(var folder in new[]{"Alarms","Caching","Collections","Community","Craft","Duties","Gathering","Inventory","Items","Macros","Maps","Market","MiniGames","Npcs","Quests"})
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
                SourceLine("Universalis","Contributor.SourceUniversalis",configuration.MarketPricesOnHover);
                SourceLine("GatherBuddy (Apache-2.0)","Contributor.SourceGatherBuddy",gatherBuddyFish?.State.Data is not null);
                SourceLine("Teamcraft (MIT)","Contributor.SourceTeamcraft",configuration.CommunityTeamcraftBites);
                SourceLine("Character Panel Refined (MIT)","Contributor.SourceCpr",true);
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
        Page.Home=>"MainWindow.Home.cs",Page.Items=>"MainWindow.Items*.cs",Page.Gathering or Page.GatheringGp=>"MainWindow.Gathering*.cs",
        Page.Maps=>"MainWindow.Maps.cs, MainWindow.MapView.cs",Page.Weather=>"MainWindow.Weather.cs, Gathering/WeatherForecast.cs",
        Page.Alarms=>"MainWindow.Alarms.cs, Alarms/",Page.MacroChain=>"MainWindow.MacroChain*.cs, Macros/",Page.Collections=>"MainWindow.Collections.cs, Collections/",
        Page.TripleTriad=>"MainWindow.TripleTriad.cs, MainWindow.Triad*.cs, MiniGames/",Page.MiniCactpot=>"MainWindow.MiniCactpot.cs, MiniGames/",
        Page.Market=>"MainWindow.Market*.cs, Market/",Page.Quests=>"MainWindow.Quests.cs, Quests/",Page.Character=>"MainWindow.Character.cs, Character/",
        Page.DutyLoot=>"MainWindow.Duties.cs, Duties/",Page.MonsterLoot=>"MainWindow.MonsterLoot*.cs",Page.NpcVendors=>"MainWindow.NpcVendors.cs, Npcs/",
        Page.CraftSimulation or Page.CraftLive or Page.CraftPresets=>"MainWindow.Craft*.cs, Craft/",Page.Settings=>"MainWindow.Settings.cs, MainWindow.Advanced.cs",
        Page.Diagnostics=>"MainWindow.Diagnostics.cs, MainWindow.Advanced.cs",_=>$"MainWindow.{shown}.cs",
    };
}
