using TenshoAssistant.Reminders;

// N-14 (0.450, DEC-027, point 2): crossbreeding pairs of nick75g (MIT), English names linked to item ids, unknown names left out and reported.
internal static class GardenCrosses450Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Garden crosses: the embedded table keeps its MIT notice and gives 3 768 distinct pairs (3 886 rows, 118 repeated) once every name is known",() =>
        {
            var csv=Read("Reminders/Data/garden-crosses.csv");
            Check(csv.Contains("# Source: nick75g/FFXIV-Crossbreed-Helper") && csv.Contains("# Copyright (c) 2024 Nick75") && csv.Contains("# THE SOFTWARE IS PROVIDED \"AS IS\""),"notice");
            var names=csv.Split('\n').Where(line=>line.Length>0 && !line.StartsWith('#') && !line.StartsWith("target\t",StringComparison.Ordinal))
                .SelectMany(line=>line.Split('\t')).Distinct().Select((name,index)=>(Id:(uint)index+1,Name:name)).ToArray();
            var data=GardenCrosses.Read(new StringReader(csv),GardenCrosses.EnglishIndex(names));
            Check(data.Unmatched.Count==0 && data.Crosses.Count==3768,"distinct pairs: "+data.Crosses.Count);
            Check(data.Crosses.Select(cross=>cross.Target).Distinct().Count()==49,"49 seeds given by a crossbreed");
        }),
        ("Garden crosses: unknown names reported and their rows left out; a pair counted once whatever its order",() =>
        {
            var index=GardenCrosses.EnglishIndex([(10,"Target Seeds"),(20,"First Seeds"),(30,"Second Seeds"),(5,"first seeds"),(0,"Zero"),(40,"")]);
            Check(index("FIRST SEEDS")==5 && index("Zero") is null && index("") is null && index("Other") is null,"case ignored, lowest id, no id 0");
            var data=GardenCrosses.Read(new StringReader("# note\ntarget\tfirst\tsecond\nTarget Seeds\tFirst Seeds\tSecond Seeds\nTarget Seeds\tSecond Seeds\tFirst Seeds\n"
                +"Target Seeds\tGysahl Green Seeds\tFirst Seeds\nbad line\nTarget Seeds\t \tFirst Seeds\n"),index);
            Check(data.Crosses.Count==1 && data.Crosses[0]==new GardenCross(10,5,30),"one pair");
            Check(data.Unmatched is ["Gysahl Green Seeds"],"reported");
            Check(data.ProducersOf(10).Count==1 && data.ProducersOf(5).Count==0,"producers");
            Check(data.UsesOf(5) is [(30,10)] && data.UsesOf(30) is [(5,10)] && data.Seeds.Count==3,"uses");
        }),
        ("Garden crosses: read once off the frame through the English Item sheet, shown in the game's language, embedded and credited",() =>
        {
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("mainWindow.GardenCrossCatalog=()=>gardenCrosses??=Task.Run(()=>") && plugin.Contains("GetExcelSheet<Lumina.Excel.Sheets.Item>(Dalamud.Game.ClientLanguage.English)")
                && plugin.Contains("\"TenshoAssistant.Reminders.Data.garden-crosses.csv\""),"background, English sheet, embedded file");
            var ui=Read("MainWindow.GardenCrosses.cs");
            Check(ui.Contains("if(!task.IsCompletedSuccessfully)") && !ui.Contains("http") && !ui.Contains("unsafe"),"waits for the task, nothing online, no game memory");
            Check(Read("TenshoAssistant.csproj").Contains("<EmbeddedResource Include=\"Reminders\\Data\\garden-crosses.csv\" />"),"embedded");
            Check(Read("Credits/CreditCatalog.cs").Contains("new(\"CrossbreedHelper\", CreditKind.Embedded"),"credited (wired: Todo458Tests)");
        }),
    ];
}
