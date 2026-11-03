using TenshoAssistant.Reminders;

// N-08 (0.448, DEC-026): the garden journal; growing and tending hours are typed by the player, each reminder is said once.
internal static class Garden448Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Garden journal: ripe and tending times from the typed hours, each reminder said once",() =>
        {
            var start=new DateTimeOffset(2026,11,3,12,0,0,TimeSpan.Zero);
            var bed=GardenJournal.Plant("House, pot 1","Seed",30,10,start);
            Check(GardenJournal.RipeAt(bed)==start.AddHours(30) && GardenJournal.TendAt(bed)==start.AddHours(10),"times");
            Check(GardenJournal.State(bed,start.AddHours(9))==GardenState.Growing && GardenJournal.State(bed,start.AddHours(10))==GardenState.ToTend,"tend due");
            Check(GardenJournal.Due([bed],start.AddHours(9)).Count==0,"nothing yet");
            Check(GardenJournal.Due([bed],start.AddHours(11)) is [(_, GardenState.ToTend)] && GardenJournal.Due([bed],start.AddHours(12)).Count==0,"tend said once");
            GardenJournal.Tend(bed,start.AddHours(12));
            Check(GardenJournal.TendAt(bed)==start.AddHours(22) && GardenJournal.State(bed,start.AddHours(13))==GardenState.Growing,"tending moves the next one");
            Check(GardenJournal.TendAt(bed) is not null && GardenJournal.Due([bed],start.AddHours(23)) is [(_, GardenState.ToTend)],"next tending said");
            Check(GardenJournal.Due([bed],start.AddHours(31)) is [(_, GardenState.Ripe)] && GardenJournal.Due([bed],start.AddHours(40)).Count==0,"ripe said once");
            GardenJournal.Tend(bed,start.AddHours(24));
            Check(GardenJournal.TendAt(bed) is null,"no tending once the next one would come after the harvest");
            GardenJournal.Replant(bed,start.AddHours(41));
            Check(!bed.RipeSaid && bed.TendSaidFor is null && GardenJournal.RipeAt(bed)==start.AddHours(71),"replant starts over");
        }),
        ("Garden journal: no tending reminder at 0 h, typed values bounded, texts cleaned, future plantings silent",() =>
        {
            var start=new DateTimeOffset(2026,11,3,12,0,0,TimeSpan.Zero);
            var plain=GardenJournal.Plant("Pot","Seed",5,0,start);
            Check(GardenJournal.TendAt(plain) is null && GardenJournal.State(plain,start.AddHours(4))==GardenState.Growing,"no tending");
            Check(GardenJournal.ClampGrow(0)==1 && GardenJournal.ClampGrow(9999)==GardenJournal.MaximumGrowHours && GardenJournal.ClampTend(-3)==0 && GardenJournal.ClampTend(500)==GardenJournal.MaximumTendHours,"bounds");
            Check(GardenJournal.CleanText("  Pot 1 ")=="Pot 1" && GardenJournal.CleanText("")==null && GardenJournal.CleanText("a\nb")==null && GardenJournal.CleanText(new string('x',41))==null,"texts");
            var future=GardenJournal.Plant("Pot","Seed",1,1,start.AddDays(1));
            Check(GardenJournal.Due([future],start).Count==0,"planted in the future: silent");
            Check(GardenJournal.Due(null,start).Count==0,"no journal");
        }),
        ("Garden journal: kept by « reset all », reminders a reset setting, checked every 10 s from the frame, nothing read from the game",() =>
        {
            string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");
            var configuration=Read("Configuration.cs");
            Check(configuration.Contains("        \"GardenBeds\",\n    };") && configuration.Contains("\"GardenAlerts\",") && configuration.Contains("public bool GardenAlerts { get; set; } = true;"),"kept list, reset setting");
            var ui=Read("MainWindow.Garden.cs");
            Check(ui.Contains("if(now-gardenObservedAt<TimeSpan.FromSeconds(10))return;") && ui.Contains("if(!configuration.GardenAlerts ||"),"throttled, setting");
            Check(!ui.Contains("unsafe") && !ui.Contains("FFXIVClientStructs"),"no game reading");
            Check(Read("Plugin.cs").Contains("mainWindow.ObserveGarden") && Read("MainWindow.Garden.cs").Contains("DrawGardenJournal();"),"wired");
        }),
    ];
}
