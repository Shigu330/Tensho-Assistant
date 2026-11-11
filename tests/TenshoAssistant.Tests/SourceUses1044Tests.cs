using TenshoAssistant;

// X-122, D-101 (0.1044): one switch per use of Garland Tools, the wiki and Universalis, under the source's switch.
internal static class SourceUses1044Tests
{
    // use, where it is wired (file under src/TenshoAssistant).
    private static readonly (SourceUse Use,string File)[] Wiring =
    [
        (SourceUse.GarlandDutyLoot,"Plugin.cs"),(SourceUse.GarlandMonsterLoot,"Plugin.cs"),(SourceUse.GarlandItemSources,"Plugin.cs"),
        (SourceUse.WikiMonsters,"Plugin.cs"),(SourceUse.WikiDutyGuides,"Plugin.cs"),(SourceUse.WikiBlueMagic,"Plugin.cs"),
        (SourceUse.WikiBeastmaster,"Plugin.cs"),(SourceUse.WikiItemAcquisition,"Plugin.cs"),(SourceUse.WikiVistas,"Plugin.cs"),
        (SourceUse.WikiIslandAnimals,"Plugin.cs"),(SourceUse.UniversalisRetainerCompare,"Plugin.cs"),
        (SourceUse.UniversalisToolPrices,"MainWindow.CraftFromStock.cs"),(SourceUse.UniversalisToolPrices,"MainWindow.CurrencyValue.cs"),
        (SourceUse.UniversalisToolPrices,"MainWindow.MyTools.cs"),(SourceUse.UniversalisToolPrices,"MainWindow.RoutePlanner.cs"),
        (SourceUse.UniversalisRetainerCompare,"MainWindow.RetainerSales.cs"),(SourceUse.GarlandDutyLoot,"MainWindow.DutyFinder.cs"),
        (SourceUse.GarlandMonsterLoot,"MainWindow.Monsters.cs"),(SourceUse.WikiMonsters,"MainWindow.Monsters.cs"),
        (SourceUse.WikiDutyGuides,"MainWindow.Duties.cs"),(SourceUse.WikiBlueMagic,"MainWindow.BlueSpellSources.cs"),
        (SourceUse.WikiBeastmaster,"MainWindow.Beastmaster.cs"),(SourceUse.GarlandItemSources,"MainWindow.MarketHover.cs"),
    ];

    internal static readonly (string Name, Action Run)[] Cases =
    [
        ("Source uses 1044: a use asks only while its source and its own switch are on; game data only cuts all",() =>
        {
            var c=new Configuration();
            foreach(var use in Enum.GetValues<SourceUse>())Check(CommunitySources.Allowed(c,use),$"{use} on by default");
            c.SourceUses.WikiVistas=false;
            Check(!CommunitySources.Allowed(c,SourceUse.WikiVistas) && CommunitySources.Allowed(c,SourceUse.WikiMonsters),"one use off leaves the others");
            c.CommunityGarland=false;
            Check(!CommunitySources.Allowed(c,SourceUse.GarlandDutyLoot) && !CommunitySources.Allowed(c,SourceUse.GarlandItemSources)
                && CommunitySources.Allowed(c,SourceUse.UniversalisToolPrices),"the source's switch cuts its uses only");
            c.CommunityGarland=true;
            CommunitySources.ApplyGameDataOnly(c,true);
            foreach(var use in Enum.GetValues<SourceUse>())Check(!CommunitySources.Allowed(c,use),$"{use} cut by game data only");
            CommunitySources.ApplyGameDataOnly(c,false);
            Check(CommunitySources.Allowed(c,SourceUse.GarlandDutyLoot) && !CommunitySources.Allowed(c,SourceUse.WikiVistas),"choices come back, the use's own kept");
            c.SourceUses=null!;
            Check(c.SourceUses is not null && CommunitySources.Allowed(c,SourceUse.WikiVistas),"a null section in the file becomes the defaults");
            Check(Enum.GetValues<SourceUse>().All(use=>new SourceUseSettings().Get(use)),"Get covers every use");
        }),
        ("Source uses 1044: each use is wired, has its setting and its texts; no page asks a source by its bare switch",() =>
        {
            var root="src/TenshoAssistant/";
            foreach(var (use,file) in Wiring)
            {
                var text=File.ReadAllText(Path.GetFullPath(root+file));
                Check(text.Contains($"SourceUse.{use}"),$"{use} wired in {file}");
            }
            var advanced=File.ReadAllText(Path.GetFullPath(root+"MainWindow.Advanced.cs"));
            var index=File.ReadAllText(Path.GetFullPath(root+"SettingsIndex.cs"));
            var config=File.ReadAllText(Path.GetFullPath(root+"Configuration.cs"));
            Check(config.Contains("\"SourceUses\""),"the uses are reset with the settings");
            foreach(var language in new[]{"en","fr"})
            {
                var json=File.ReadAllText(Path.GetFullPath(root+$"Localization/Languages/{language}.json"));
                foreach(var use in Enum.GetNames<SourceUse>())
                {
                    Check(advanced.Contains($"BoolSetting(\"Advanced.Use{use}\",\"Advanced.Use{use}Tip\""),$"{use} has its setting");
                    Check(index.Contains($"(\"Advanced.Use{use}\","),$"{use} is in the settings index");
                    Check(json.Contains($"\"Use{use}\": ") && json.Contains($"\"Use{use}Tip\": "),$"{use} has its texts in {language}");
                }
            }
            var plugin=File.ReadAllText(Path.GetFullPath(root+"Plugin.cs"));
            Check(!plugin.Contains("Enabled=()=>configuration.CommunityWiki") && !plugin.Contains("()=>configuration.CommunityWiki)"),"no wiki service follows the bare source switch");
            Check(plugin.Contains("new DutyLootService(garland){Enabled="),"the duty finder's loot follows its use");
            var client=File.ReadAllText(Path.GetFullPath(root+"Community/GarlandClient.cs"));
            Check(client.Contains("if(!useOn)throw new SourceDisabledException"),"a Garland use turned off sends no request");
            foreach(var file in new[]{"Duties/DutyLootService.cs","Monsters/MonsterLootService.cs"})
                Check(File.ReadAllText(Path.GetFullPath(root+file)).Contains("force,Enabled?.Invoke()!=false)"),$"{file} passes its use");
        }),
    ];

    private static void Check(bool condition,string message)
    {
        if(!condition)throw new InvalidOperationException(message);
    }
}
