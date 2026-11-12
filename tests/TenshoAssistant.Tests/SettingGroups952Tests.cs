// X-76 step 3 (0.952): settings groups, batch A. Each part is drawn after its parent's BeginDisabled and before the matching EndDisabled
// (greyed while the parent is off), and the rows that sat under the wrong parent are drawn outside its greyed block.
internal static class SettingGroups952Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");

    private static string Text(string language,string section,string key)
    {
        using var document=System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/"+language+".json")));
        return document.RootElement.GetProperty(section).GetProperty(key).GetString() ?? "";
    }

    private static int Count(string text,string part)=>text.Split(part).Length-1;

    // Open greyed blocks between the guard and the row: 1 or more = the row is greyed by the guard; 0 = the block closed before the row.
    // -1 when the guard or the row is missing, or the row comes first.
    private static int Depth(string text,string guard,string row)
    {
        var at=text.IndexOf(guard,StringComparison.Ordinal);
        if(at<0)return -1;
        var end=text.IndexOf(row,at,StringComparison.Ordinal);
        if(end<0)return -1;
        var between=text[at..end];
        return Count(between,"ImGui.BeginDisabled(")-Count(between,"ImGui.EndDisabled();");
    }

    private static void Greyed(string text,string guard,string row,string message)
    {
        Check(Count(text,row)==1,message+": drawn once");
        Check(Depth(text,guard,row)>=1,message+": greyed under its parent");
        var at=text.IndexOf(guard,StringComparison.Ordinal);
        Check(text.LastIndexOf("ImGui.Indent();",at,StringComparison.Ordinal) is var indent && indent>=0 && at-indent<160,message+": indented under its parent");
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Setting groups 952: wrong parents left (vendors in item tooltips, English item name)",() =>
        {
            var settings=Read("MainWindow.Settings.cs");
            const string usages="ImGui.BeginDisabled(!configuration.ItemUsagesOnHover);";
            Check(Depth(settings,usages,"BoolSetting(\"Settings.HoverCompare\"")==1,"the item uses parts are still greyed");
            Check(Depth(settings,usages,"BoolSetting(\"Settings.HoverVendors\",")==0,"NPC vendors out of the item uses block");
            Check(Depth(settings,usages,"BoolSetting(\"Settings.HoverVendorsMetOnly\"")==0 && Depth(settings,usages,"DrawCurrencyTicks(\"HoverVendor\"")==0,"vendor options follow it");
            Check(settings.Contains("if(configuration.HoverVendors)\n        {\n            ImGui.Indent();\n            BoolSetting(\"Settings.HoverVendorsMetOnly\""),"vendor options indented under it");
            const string english="BoolSetting(\"ModuleSettings.ShowEnglishItemName\"";
            Check(Count(settings,english)==1,"English name drawn once");
            Check(settings.IndexOf(english,StringComparison.Ordinal)>settings.IndexOf("BoolSetting(\"Settings.ContextMenu\"",StringComparison.Ordinal)
                && settings.IndexOf(english,StringComparison.Ordinal)<settings.IndexOf("private void DrawModuleSettings()",StringComparison.Ordinal),"English name in Settings › Interface, after the item menu");
            Check(Depth(settings,"ImGui.BeginDisabled(!configuration.CraftLogHelperEnabled);","BoolSetting(\"ModuleSettings.CraftLogAutoSolve\"")==1,"the crafting log parts are still greyed");
        }),
        ("Setting groups 952: craft search limits under Raphael, echo options under the echo, simulation above Export",() =>
        {
            var settings=Read("MainWindow.Settings.cs");
            foreach(var key in new[]{"Steps","Width","Budget"})
            {
                Greyed(settings,"ImGui.BeginDisabled(search.Raphael);","IntSetting(\"ModuleSettings."+key+"\"",key);
                foreach(var language in new[]{"en","fr"})Check(Text(language,"SettingTips",key).Contains("Raphael",StringComparison.Ordinal),key+" tip names Raphael: "+language);
            }
            Check(Text("en","SettingTips","Steps").EndsWith("\nDefault: 40.",StringComparison.Ordinal) && Text("fr","SettingTips","Budget").EndsWith("\nPar défaut : 100 000.",StringComparison.Ordinal),"defaults kept");
            Check(settings.IndexOf("BoolSetting(\"CraftSearch.SettingRaphael\"",StringComparison.Ordinal)<settings.IndexOf("ImGui.BeginDisabled(search.Raphael);",StringComparison.Ordinal),"Raphael row first");
            Greyed(settings,"ImGui.BeginDisabled(!configuration.CraftSearch.Echo);","IntSetting(\"ModuleSettings.EchoSound\"","echo sound");
            Greyed(settings,"ImGui.BeginDisabled(!configuration.CraftSearch.Echo);","BoolSetting(\"ModuleSettings.EchoLastOnly\"","echo last only");
            Check(settings.IndexOf("BoolSetting(\"ModuleSettings.AutoSimulate\"",StringComparison.Ordinal) is var simulate && simulate>0
                && simulate<settings.IndexOf("ImGui.TextColored(Accent,localization.Get(\"ModuleSettings.Export\"));",StringComparison.Ordinal),"automatic simulation above the Export heading");
        }),
        ("Setting groups 952: prices refresh under prices on hover, server bar tooltip under the bar entries",() =>
        {
            var advanced=Read("MainWindow.Advanced.cs");
            Greyed(advanced,"ImGui.BeginDisabled(!configuration.MarketPricesOnHover);","BoolSetting(\"Advanced.HoverRefresh\"","refresh key");
            // X-162 (0.1129): the currency and gil entries draw Tensho's tooltip too.
            const string bars="ImGui.BeginDisabled(!configuration.AlarmServerBar && !configuration.ReminderServerBar && !configuration.ReminderFinishedServerBar && !configuration.CurrencyServerBar && !configuration.GilServerBar);";
            Greyed(advanced,bars,"BoolSetting(\"Advanced.AlarmBarTooltip\"","bar tooltip");
            Greyed(advanced,"ImGui.BeginDisabled(!configuration.AlarmBarTooltip || !configuration.AlarmServerBar);","SliderSetting(\"Advanced.AlarmBarTooltipCount\"","bar tooltip count");
            Check(advanced.IndexOf("BoolSetting(\"Advanced.ReminderFinishedServerBar\"",StringComparison.Ordinal)<advanced.IndexOf(bars,StringComparison.Ordinal),"right after the three bar rows");
            Check(Depth(advanced,bars,"BoolSetting(\"Tracker.Setting\"")==0 && Depth(advanced,bars,"BoolSetting(\"Advanced.CloseOnGameWindow\"")==0,"tracker and close-on-window not greyed");
            Check(advanced.IndexOf("BoolSetting(\"Tracker.Setting\"",StringComparison.Ordinal)>advanced.IndexOf("SliderSetting(\"Advanced.AlarmBarTooltipCount\"",StringComparison.Ordinal),"no longer under the tracking window");
        }),
        ("Setting groups 952: gathering panel parts greyed under it, next action highlight drawn once there",() =>
        {
            var plan=Read("MainWindow.GatheringPlanSettings.cs");
            const string panel="ImGui.BeginDisabled(!configuration.GatheringPanel);";
            foreach(var row in new[]{"BoolSetting(\"GatheringPanel.ListSetting\"","BoolSetting(\"GatheringPanel.ListHighlight\"","BoolSetting(\"GatheringPanel.NextHighlight\"",
                "BoolSetting(\"CollectablePanel.UseScrutiny\"","DrawCollectableRisk();","BoolSetting(\"CollectablePanel.UseFocus\"","BoolSetting(\"CollectablePanel.UsePriming\"","BoolSetting(\"CollectablePanel.UseRestore\""})
                Greyed(plan,panel,row,row);
            Check(Depth(plan,panel,"IntSetting(\"GatheringPanel.Regen\"")==0 && Depth(plan,panel,"        DrawGpForbiddenActions();")==0,"regeneration and forbidden actions stay outside");
            var forbidden=plan[plan.IndexOf("private void DrawGpForbiddenActions()",StringComparison.Ordinal)..];
            Check(!forbidden.Contains("GatheringNextHighlight",StringComparison.Ordinal),"no longer under the forbidden actions (nor on the GP page)");
        }),
        ("Setting groups 952: Discord parts under « Post alarms to Discord », duty and currency categories under their alert",() =>
        {
            var discord=Read("MainWindow.Discord.cs");
            const string enabled="ImGui.BeginDisabled(!configuration.Discord.Enabled);";
            foreach(var row in new[]{"DiscordText(\"Discord.Username\"","DiscordText(\"Discord.Template\"","BoolSetting(\"Discord.Gathering\"","BoolSetting(\"Discord.Fishing\"",
                "BoolSetting(\"Discord.Reminders\"","BoolSetting(\"Discord.Voyages\"","BoolSetting(\"Discord.Other\"","BoolSetting(\"Discord.Currencies\"","BoolSetting(\"Discord.DutyPop\"",
                "DrawDiscordCategoryChannels();","BoolSetting(\"Discord.Character\"","DiscordText(\"Discord.Mention\""})
                Greyed(discord,enabled,row,row);
            Check(discord.IndexOf("DiscordText(\"Discord.Url\"",StringComparison.Ordinal)<discord.IndexOf(enabled,StringComparison.Ordinal),"address outside");
            Check(Depth(discord,enabled,"localization.Get(\"Discord.Test\")")==1,"test post outside (only its own « ready » block)");
            Check(Depth(discord,"ImGui.BeginDisabled(!configuration.CurrencyAlerts);","BoolSetting(\"Discord.Currencies\"")==1,"currencies greyed under the cap alerts");
            Check(Depth(discord,"ImGui.BeginDisabled(configuration.AlarmOptions?.DutyPopAlert!=true);","BoolSetting(\"Discord.DutyPop\"")==1,"duty ready greyed under its alert");
            Check(Text("en","Discord","CurrenciesTip").Contains("« Cap alerts »",StringComparison.Ordinal) && Text("fr","Discord","CurrenciesTip").Contains("« Alertes de plafond »",StringComparison.Ordinal),"currencies tip names its switch");
            Check(Text("en","Discord","DutyPopTip").Contains("« Alert when a duty is ready »",StringComparison.Ordinal) && Text("fr","Discord","DutyPopTip").Contains("« Alerte quand une mission est prête »",StringComparison.Ordinal),"duty tip names its switch");
            Check(Text("en","Discord","CurrenciesTip").EndsWith("\nDefault: on.",StringComparison.Ordinal) && Text("fr","Discord","DutyPopTip").EndsWith("\nPar défaut : activé.",StringComparison.Ordinal),"defaults kept");
        }),
        ("Setting groups 952: weekly cap, list panels behind game, automatic exchange, plot watch, important item sound",() =>
        {
            Greyed(Read("MainWindow.Currencies.cs"),"ImGui.BeginDisabled(!configuration.CurrencyAlerts);","BoolSetting(\"Currencies.WeeklyCap\"","weekly cap");
            var lists=Read("MainWindow.ListPanels.cs");
            Check(Depth(lists,"ImGui.BeginDisabled(!configuration.HideBarsBehindGame);","BoolSetting(\"ListPanels.HideBehindGame\"")==1,"list panels behind game greyed under the bars switch");
            Check(Text("en","ListPanels","HideBehindGameTip").Contains("« Bars and free panels step aside for game windows »",StringComparison.Ordinal)
                && Text("fr","ListPanels","HideBehindGameTip").Contains("« Barres et panneaux libres effacés devant les fenêtres du jeu »",StringComparison.Ordinal),"list panel tip names the switch");
            Check(Text("en","Advanced","HideBarsBehindGameTip").Contains("list panels (with their own switch)",StringComparison.Ordinal) && Text("fr","Advanced","HideBarsBehindGameTip").Contains("panneaux de liste (avec leur propre case)",StringComparison.Ordinal),"bars tip names the list panels");
            Check(Depth(Read("MainWindow.Discoveries.cs"),"ImGui.BeginDisabled(!configuration.Discovery.Enabled);","BoolSetting(\"DiscoveriesUi.AutoExchange\"")==1,"automatic exchange greyed while discoveries are not noted");
            Check(Text("en","DiscoveriesUi","AutoExchangeTip").Contains("« Note discoveries »",StringComparison.Ordinal) && Text("fr","DiscoveriesUi","AutoExchangeTip").Contains("« Noter les découvertes »",StringComparison.Ordinal)
                && Text("en","DiscoveriesUi","AutoExchangeTip").EndsWith("\nDefault: off.",StringComparison.Ordinal),"exchange tip names the switch");
            var plots=Read("MainWindow.Plots.cs");
            Check(Depth(plots,"ImGui.BeginDisabled(!configuration.CommunityPaissa);","BoolSetting(\"Plots.Watch\"")==1,"watch greyed while PaissaDB is off");
            Greyed(plots,"ImGui.BeginDisabled(!configuration.PlotWatch);","IntSetting(\"Plots.Minutes\"","watch interval");
            Check(Count(plots,"localization.Get(\"Advanced.SourceOffTip\")")==1 && plots.IndexOf("localization.Get(\"Advanced.SourceOffTip\")",StringComparison.Ordinal)<plots.IndexOf("BoolSetting(\"Plots.Watch\"",StringComparison.Ordinal),"source-off note above the watch, once");
            Greyed(Read("MainWindow.ItemNotes.cs"),"ImGui.BeginDisabled(!configuration.ItemNoteShopReminder);","BoolSetting(\"ItemNotes.ImportantSound\"","important item sound");
        }),
    ];
}
