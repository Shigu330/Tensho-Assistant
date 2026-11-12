using TenshoAssistant.Reminders;

// 0.1153 (X-163, user's request of 12/11): the garden beds ring in the desktop application too. Behaviour tests of GardenBell: ripe and to tend,
// each once in the application's own memory; a bed the plugin already said is not said again; the beds (the plugin's saved settings, read by the
// application) are never changed; the settings read again into new objects do not ring twice; a new tending due rings again; a bed gone is forgotten.
internal static class GardenBell1153Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset Now = new(2026,11,12,9,0,0,TimeSpan.Zero);
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static GardenBed Copy(GardenBed bed) => new() { Id = bed.Id,Place = bed.Place,Seed = bed.Seed,PlantedAt = bed.PlantedAt,GrowHours = bed.GrowHours,TendHours = bed.TendHours,
        TendedAt = bed.TendedAt,RipeSaid = bed.RipeSaid,TendSaidFor = bed.TendSaidFor };

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Garden bell 1153: ripe and to tend rung once each, never what the plugin already said, the beds never changed",() =>
        {
            var ripe = new GardenBed { Place = "Bed 1",Seed = "Krakka Root",PlantedAt = Now.AddHours(-10),GrowHours = 5 };
            var tend = new GardenBed { Place = "Pot 1",Seed = "Mandrake",PlantedAt = Now.AddHours(-3),GrowHours = 48,TendHours = 2 };
            var said = new GardenBed { Place = "Bed 2",Seed = "Old Seed",PlantedAt = Now.AddHours(-10),GrowHours = 5,RipeSaid = true };
            var growing = new GardenBed { Place = "Bed 3",Seed = "Young",PlantedAt = Now.AddHours(-1),GrowHours = 24 };
            var later = new GardenBed { Place = "Bed 4",Seed = "Future",PlantedAt = Now.AddHours(1),GrowHours = 1 };
            var bell = new GardenBell();
            var due = bell.Due([ripe,tend,said,growing,later],Now);
            Check(due.Count == 2 && due.Any(item => item.Bed == ripe && item.State == GardenState.Ripe) && due.Any(item => item.Bed == tend && item.State == GardenState.ToTend),
                "the ripe bed and the pot to tend: " + due.Count);
            Check(!ripe.RipeSaid && tend.TendSaidFor is null,"the beds are not changed (the application never writes the plugin's settings)");
            Check(bell.Due([ripe,tend,said,growing,later],Now.AddMinutes(1)).Count == 0,"once each");
            Check(bell.Due([Copy(ripe),Copy(tend)],Now.AddMinutes(2)).Count == 0,"the settings read again into new objects: not rung twice");
            var tended = Copy(tend);tended.TendedAt = Now.AddMinutes(5);
            Check(bell.Due([Copy(ripe),tended],Now.AddHours(3)).Single().State == GardenState.ToTend,"a new tending due rings again");
            var saidNow = Copy(tended);saidNow.TendSaidFor = GardenJournal.TendAt(saidNow);
            Check(new GardenBell().Due([saidNow],Now.AddHours(3)).Count == 0,"a tending the plugin already said: not again");
            Check(bell.Due([],Now.AddHours(4)).Count == 0 && bell.Due([Copy(ripe)],Now.AddHours(5)).Single().Bed.Id == ripe.Id,"a bed gone is forgotten (rung again if it comes back)");
            Check(new GardenBell().Due(null,Now).Count == 0,"no journal: nothing");
        }),
        ("Garden bell 1153: the application rings the garden with the alarms, when the garden reminders are on, in the game's words",() =>
        {
            var data = Read("src/TenshoAssistant/MainWindow.DesktopData.cs");
            Check(data.Contains("var garden=configuration.GardenAlerts ? (desktopGarden??=new()).Due(configuration.GardenBeds,now):[];",StringComparison.Ordinal)
                && data.Contains("if(due.Count+garden.Count==0 || options.DesktopOnlyGameBehind && gameInFront)return ([],false);",StringComparison.Ordinal)
                && data.Contains(".. garden.Select(item=>localization.Format(item.State==Reminders.GardenState.Ripe ? \"Garden.RipeNotice\":\"Garden.TendNotice\",item.Bed.Seed,item.Bed.Place))",StringComparison.Ordinal),
                "wired in DesktopDueAlarms");
        }),
    ];
}
