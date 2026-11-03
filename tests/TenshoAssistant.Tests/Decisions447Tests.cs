using TenshoAssistant.Maps;

// 0.447 (decisions of 2026-11-03): the hunt train call can be sent to a fixed chat channel on click; partake.gg is a plain link; the dead
// « monsters on hover » setting is gone; the sort rules editor stays through the 250 ms inventory invalidation (DEC-004 kept).
internal static class Decisions447Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Hunt call: fixed channels, cleaned text, one final <flag>, anything else refused",() =>
        {
            Check(HuntTrain.ChatCommand("/sh","Next mark: Gamma (S) — Thavnair")=="/sh Next mark: Gamma (S) — Thavnair <flag>","plain call");
            Check(HuntTrain.ChatCommand("/say","Call") is null && HuntTrain.ChatCommand("/tell Someone","Call") is null && HuntTrain.ChatCommand("","Call") is null,"only the listed channels");
            Check(HuntTrain.ChatCommand("/p","a\n/logout <me> <pos>\tb")=="/p alogout me pos\tb <flag>".Replace("\t","") ,"control characters, slashes and placeholders removed");
            Check(HuntTrain.ChatCommand("/y","  \n ") is null,"nothing to say");
            var longCall=HuntTrain.ChatCommand("/fc",new string('x',1000))!;
            Check(longCall=="/fc "+new string('x',HuntTrain.AnnounceMaximum)+" <flag>","bounded");
            foreach(var channel in HuntTrain.AnnounceChannels)
                Check(HuntTrain.IsChatCommand(HuntTrain.ChatCommand(channel,"Next mark: Gamma (S) — Thavnair")!),"round trip "+channel);
            Check(!HuntTrain.IsChatCommand("/sh hello") && !HuntTrain.IsChatCommand("/say hello <flag>") && !HuntTrain.IsChatCommand("/sh /logout <flag>")
                && !HuntTrain.IsChatCommand("/sh a <me> <flag>") && !HuntTrain.IsChatCommand("/sh  <flag>"),"only lines ChatCommand builds");
        }),
        ("Hunt call: sent on click only, once the flag is on that stop, 5 s apart, checked again before the game",() =>
        {
            var ui=Read("MainWindow.HuntTrain.cs");
            Check(ui.Contains("var ready=huntTrainCalled is not null && command is not null && SendHuntCall is not null && DateTimeOffset.UtcNow-huntCallSentAt>=TimeSpan.FromSeconds(5);"),"ready rule");
            Check(ui.Contains("if(ImGui.SmallButton(localization.Get(\"HuntTrain.Send\")) && ready){SendHuntCall!(command!);huntCallSentAt=DateTimeOffset.UtcNow;}"),"sent from the button only");
            Check(ui.Split("SendHuntCall!(").Length==2,"one call site");
            Check(ui.Contains("huntTrainCalled=next;") && ui.Contains("huntTrainCalled=index;"),"the call names the stop flagged last");
            var sender=Read("Maps/HuntAnnouncer.Dalamud.cs");
            Check(sender.Contains("if(!HuntTrain.IsChatCommand(command))") && sender.Contains("ui->ProcessChatBoxEntry(text)") && sender.Contains("text->Dtor(true)"),"checked, sent, freed");
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("mainWindow.SendHuntCall=command=>framework.RunOnFrameworkThread(()=>Maps.HuntAnnouncer.Send(command,log));"),"framework thread");
            var configuration=Read("Configuration.cs");
            Check(configuration.Contains("public string HuntAnnounceChannel { get; set; } = \"/sh\";") && configuration.Contains("\"HuntAnnounceChannel\","),"channel setting with reset");
        }),
        ("partake.gg: a link opened on click, nothing read",() =>
        {
            var events=Read("MainWindow.Events.cs");
            Check(events.Contains("if(ImGui.SmallButton(localization.Get(\"Events.Partake\")))Dalamud.Utility.Util.OpenLink(PartakeAddress);"),"click only");
            Check(events.Contains("private const string PartakeAddress=\"https://www.partake.gg/\";") && events.Split("partake.gg").Length<=4,"one fixed address");
        }),
        ("Monsters on hover: the dead setting, its service and texts are gone",() =>
        {
            Check(!File.Exists(Path.GetFullPath("src/TenshoAssistant/Monsters/MonsterHoverSources.cs")),"service removed");
            foreach(var file in new[]{"Configuration.cs","MainWindow.Settings.cs","MainWindow.MarketHover.cs","Plugin.cs","Credits/CreditCatalog.cs"})
                Check(!Read(file).Contains("MonsterSourcesOnHover") && !Read(file).Contains("MonsterHoverSources") && !Read(file).Contains("DrawHoveredSources"),file);
            foreach(var language in new[]{"en","fr"})
            {
                var text=Read($"Localization/Languages/{language}.json");
                Check(!text.Contains("\"MonsterHover\"") && !text.Contains("\"MonsterHoverHelp\"") && !text.Contains("\"MonstersLoading\""),language);
            }
        }),
        ("Sort: the rules editor stays through the invalidation; preview and apply wait for a complete list",() =>
        {
            var sort=Read("MainWindow.SortPreview.cs");
            var gap=sort.IndexOf("if(sortLastComplete is { } last && last.Scope==scope && DateTimeOffset.UtcNow-last.At<TimeSpan.FromSeconds(2)",StringComparison.Ordinal);
            var complete=sort.IndexOf("sortLastComplete=(scope,DateTimeOffset.UtcNow);",StringComparison.Ordinal);
            var build=sort.IndexOf("InventorySortRules.Build(",StringComparison.Ordinal);
            Check(gap>0 && complete>gap && build>complete,"editor during the gap, preview only after a complete list");
            var gapBlock=sort[gap..complete];
            Check(gapBlock.Contains("DrawSortRules(scope,waitingCatalog);") && gapBlock.Contains("return;") && !gapBlock.Contains("visualSort.Request("),"no preview nor sort in the gap");
        }),
    ];
}
