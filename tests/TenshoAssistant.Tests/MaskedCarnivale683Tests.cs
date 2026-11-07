using TenshoAssistant.Carnivale;

// W-02 (0.683): the stages of the Masked Carnivale, information only.
internal static class MaskedCarnivale683Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static CarnivaleStage Stage(uint row,int order=0)=>new(row,order==0 ? (int)row:order,["e"+row],30,20);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Masked Carnivale: a stage beyond the game's list is refused, and the 32 stages fit its 32 bits",() =>
        {
            Check(CarnivaleBoard.Check(Enumerable.Range(1,32).Select(id=>Stage((uint)id)).ToArray(),32)==CarnivaleReadStatus.Loaded,"32 stages, 32 bits");
            Check(CarnivaleBoard.Check([Stage(33)],32)==CarnivaleReadStatus.Unavailable,"beyond the list");
            Check(CarnivaleBoard.Check([Stage(0)],32)==CarnivaleReadStatus.Unavailable,"the empty row");
        }),
        ("Masked Carnivale: a stage reads the bit of its row minus one, shown in the game's order",() =>
        {
            var rows=CarnivaleBoard.Rows([Stage(2,1),Stage(1,2)],new(CarnivaleReadStatus.Loaded,[false,true,false]));
            Check(rows[0].Stage.Row==2 && rows[0].Done,"row 2 reads bit 1, first by order");
            Check(rows[1].Stage.Row==1 && !rows[1].Done,"row 1 reads bit 0");
        }),
        ("Masked Carnivale: no line for a reading that did not load",() =>
        {
            Check(CarnivaleBoard.Rows([Stage(1)],CarnivaleSnapshot.Of(CarnivaleReadStatus.NotLoaded)).Count==0,"not loaded");
        }),
        ("Masked Carnivale: times are minutes and seconds, zero is left blank",() =>
        {
            Check(CarnivaleBoard.Time(30)=="0:30","30 s");
            Check(CarnivaleBoard.Time(525)=="8:45","525 s");
            Check(CarnivaleBoard.Time(0)=="","unknown");
        }),
        ("Masked Carnivale: each enemy once, an unnamed row left out",() =>
        {
            var names=CarnivaleBoard.Enemies(["a","b","a",""]);
            Check(names.Count==2 && names[0]=="a" && names[1]=="b","list");
            Check(CarnivaleBoard.IsArrangement(1) && !CarnivaleBoard.IsArrangement(2) && !CarnivaleBoard.IsArrangement(3),"fight types");
        }),
        ("Masked Carnivale: the page only reads, and carries none of the sheet's weaknesses",() =>
        {
            var loader=Read("Carnivale/MaskedCarnivale.Dalamud.cs");
            Check(Read("Plugin.cs").Contains("mainWindow.ReadCarnivale=stages=>framework.RunOnFrameworkThread(()=>"),"framework thread");
            Check(loader.Contains("player->IsLoaded"),"character loaded first");
            foreach(var weakness in new[]{"Endurance","Vuln","Fire","Slashing"})Check(!loader.Contains(weakness),"DEC-033: no fight data: "+weakness);
        }),
    ];
}
