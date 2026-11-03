using TenshoAssistant.Gathering;

// R-06 (0.409): collectable thresholds and rewards from the game's sheets, shown beside the game's Collectable window.
internal static class Collectables409Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Collectables: thresholds by item and by the name shown, tiers, empty rows left out",() =>
        {
            // Values of docs/evidence/0.409-collectables.md (rows 23.0 and 14.0).
            var catalog=new CollectableCatalog([
                new(30970,"Arc long en cèdre collectionnable",50,50,110,150,190,45,49,54,true),
                new(12713,"Pic-à-glace",50,51,49,81,114,6,7,11,true),
                new(5108,"Minerai de cuivre jaune",50,50,0,0,0,0,0,0,true),
            ]);
            Check(catalog.Count==2,"rows with no threshold left out");
            Check(catalog.For(5108).Count==0 && catalog.For(30970).Count==1,"by item");
            Check(catalog.Named("Arc long en  cèdre collectionnable ") is [{ ItemId:30970 }],"by the name shown, icon and spaces removed");
            Check(catalog.Named("pic-à-glace") is [{ Low:49,Mid:81,High:114 }],"name case ignored");
            var rule=catalog.For(30970)[0];
            Check(Collectables.Tier(rule,109)==CollectableTier.Below && Collectables.Tier(rule,110)==CollectableTier.Low
                && Collectables.Tier(rule,150)==CollectableTier.Mid && Collectables.Tier(rule,999)==CollectableTier.High,"tiers");
        }),
        ("Collectables: numbers read as shown by the game; anything else is unknown",() =>
        {
            Check(Collectables.Number("600")==600 && Collectables.Number("1,234")==1234 && Collectables.Number("1 234")==1234,"numbers");
            Check(Collectables.Number(null) is null && Collectables.Number("") is null && Collectables.Number("12a") is null && Collectables.Number("-5") is null,"unknown");
        }),
        ("Collectables: the reader stays on the framework thread, reads named fields only, and no action is advised",() =>
        {
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/CollectableReader.Dalamud.cs"));
            Check(reader.Contains("framework.IsInFrameworkUpdateThread") && reader.Contains("addon->ItemName") && reader.Contains("addon->IntegrityLeftover")
                && reader.Contains("MaxValues=64") && reader.Contains("_=Task.Run(Load)"),"reader");
            Check(!reader.Contains("FireCallback") && !reader.Contains("SetAtkValues"),"nothing sent to the game");
            var panel=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CollectablePanel.cs"));
            Check(panel.Contains("CollectablePanel.Unknown") && panel.Contains("developerMode && ImGui.CollapsingHeader"),"current collectability unknown, raw values in developer mode");
        }),
        ("Collectables table 0.413: every kept rule by level, filtered by item or group words",() =>
        {
            var catalog=new CollectableCatalog([
                new(2,"Noyau de glacier",50,51,141,236,332,13,16,26,true,"Nv 50-60"),
                new(1,"Madrier de cèdre d'affinage",80,80,360,540,720,5,10,15,false,"Composants d'affinage pour menuisier"),
                new(3,"Pic-à-glace",50,51,49,81,114,6,7,11,true,"Nv 50-60"),
                new(4,"Vide",50,50,0,0,0,0,0,0,true,"Nv 50-60"),
            ]);
            Check(catalog.All.Select(rule=>rule.ItemId).SequenceEqual(new uint[]{2,3,1}),"by level then name, empty rows left out");
            Check(Collectables.Filter(catalog.All,"nv 50").Length==2 && Collectables.Filter(catalog.All,"menuisier madrier").Length==1
                && Collectables.Filter(catalog.All,"  ").Length==3 && Collectables.Filter(catalog.All,"glace 80").Length==0,"filter");
            var ui=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CollectableTable.cs"));
            Check(ui.Contains("Tip(\"CollectableTable.SearchTip\")") && ui.Contains("clipper.End();clipper.Destroy();") && ui.Contains("CollectableTable.ItemReward"),"table");
        }),
    ];
}
