using TenshoAssistant.Reminders;

// 0.1160 (X-132, user request of 11/11): the garden's alarms (tending and harvest, from the journal) in an « Alarms » tab of the Garden tool,
// their number beside « Garden » in the tools' menu (0 while the garden reminders are off); they were never Tensho alarms, so the Alarms
// tool and its count do not hold them.
internal static class GardenAlarms1160Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static readonly DateTimeOffset Now=new(2026,11,12,12,0,0,TimeSpan.Zero);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Garden alarms 1160: each bed's coming tending and harvest, in time order, the passed ones left out",() =>
        {
            var tomato=new GardenBed{Place="Mist · 1",Seed="Tomato",PlantedAt=Now.AddHours(-10),GrowHours=48,TendHours=24};
            var ripe=new GardenBed{Place="Mist · 2",Seed="Old",PlantedAt=Now.AddHours(-50),GrowHours=48,TendHours=24};
            var noTend=new GardenBed{Place="Pot",Seed="Mint",PlantedAt=Now.AddHours(-1),GrowHours=5,TendHours=0};
            var future=new GardenBed{Place="Clock",Seed="Odd",PlantedAt=Now.AddHours(2),GrowHours=5,TendHours=1};
            var alarms=GardenJournal.Alarms([tomato,ripe,noTend,future],Now);
            Check(alarms.Select(a=>(a.Bed.Seed,a.Kind)).SequenceEqual([("Mint",GardenState.Ripe),("Tomato",GardenState.ToTend),("Tomato",GardenState.Ripe)]),
                "order and content: "+string.Join(", ",alarms.Select(a=>$"{a.Bed.Seed} {a.Kind} {a.At:HH:mm}")));
            Check(alarms[1].At==Now.AddHours(14) && alarms[2].At==Now.AddHours(38),"times from the journal's own rule");
            var tended=new GardenBed{Place="Mist · 3",Seed="Late",PlantedAt=Now.AddHours(-30),GrowHours=40,TendHours=24,TendedAt=Now.AddHours(-1)};
            Check(GardenJournal.Alarms([tended],Now).Select(a=>a.Kind).SequenceEqual([GardenState.Ripe]),"a tending that would come after the harvest is not listed");
            Check(GardenJournal.Alarms(null,Now).Count==0,"no journal, no alarm");
            Check(tomato.RipeSaid==false && tomato.TendSaidFor is null,"listing changes nothing");
        }),
        ("Garden alarms 1160: a tab of the Garden tool, the count in the menu, nothing in the Alarms tool",() =>
        {
            var garden=Read("src/TenshoAssistant/MainWindow.Garden.cs");
            Check(garden.Contains("(\"Alarms\",localization.Format(\"Garden.TabAlarms\",GardenAlarmCount),()=>DrawGardenAlarms()),"),"the tab");
            Check(garden.Contains("private int GardenAlarmCount=>configuration.GardenAlerts ? GardenJournal.Alarms(configuration.GardenBeds,DateTimeOffset.UtcNow).Count:0;"),"the count, 0 while off");
            Check(Read("src/TenshoAssistant/MainWindow.Pages.cs").Contains("localization.Format(\"Navigation.GardenCount\",gardenAlarms)"),"beside « Garden »");
            Check(!Read("src/TenshoAssistant/MainWindow.Alarms.cs").Contains("GardenBeds"),"not in the Alarms tool");
        }),
    ];
}
