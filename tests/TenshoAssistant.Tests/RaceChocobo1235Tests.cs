using TenshoAssistant.MiniGames;

// 0.1235 (X-204, user's request of 12/11, K-33): the racing chocobo sheet. Pure tests (rank from the game's bands, plausibility bounds) and the
// wiring (read only once loaded, never RequestParameters, the game's French words).
internal static class RaceChocobo1235Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Source(string path)=>File.ReadAllText(Path.GetFullPath(path));
    private static readonly RaceChocoboRank[] Ranks=[new(1,0,0,"Training Course"),new(2,1,20,"Maiden"),new(3,21,40,"R-40"),new(14,241,300,"R-300")];
    private static RaceChocoboSnapshot Snap(int rating,int[] current,int[] maximum)=>new(rating,3,true,current,maximum,1,0,DateTimeOffset.UnixEpoch);

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Race chocobo 1235: rank from the rating bands, the first named row",() =>
        {
            Check(RaceChocobo.RankOf(0,Ranks)?.Name=="Training Course","0");
            Check(RaceChocobo.RankOf(20,Ranks)?.Name=="Maiden" && RaceChocobo.RankOf(21,Ranks)?.Name=="R-40","band edges");
            Check(RaceChocobo.RankOf(300,Ranks)?.Name=="R-300" && RaceChocobo.RankOf(301,Ranks) is null,"top band, then nothing");
        }),
        ("Race chocobo 1235: a reading out of bounds is not shown",() =>
        {
            Check(RaceChocobo.Plausible(Snap(40,[10,10,10,10,10],[20,20,20,20,20])),"normal");
            Check(!RaceChocobo.Plausible(Snap(40,[10,10,10,10],[20,20,20,20,20])),"four attributes");
            Check(!RaceChocobo.Plausible(Snap(-1,[10,10,10,10,10],[20,20,20,20,20])),"negative rating");
            Check(!RaceChocobo.Plausible(Snap(40,[0,0,0,0,0],[0,0,0,0,0])),"all zero: not loaded");
            Check(!RaceChocobo.Plausible(Snap(40,[10,10,10,10,20_000],[20,20,20,20,20])),"beyond the bound");
        }),
        ("Race chocobo 1235: read only once loaded, never asked of the server; the game's French words",() =>
        {
            var reader=Source("src/TenshoAssistant/MiniGames/RaceChocoboReader.Dalamud.cs");
            Check(reader.Contains("manager->State!=RaceChocoboManager.RaceChocoboState.Loaded") && !reader.Contains("->RequestParameters(") && reader.Contains("MemberFunctionPointers.GetRating==0"),"loaded only, no request, addresses checked");
            var french=Source("src/TenshoAssistant/Localization/Languages/fr.json");
            foreach(var word in new[]{"\"MaxSpeed\": \"Vitesse maximale\"","\"Endurance\": \"Vitalité\"","\"Stamina\": \"Endurance\"","\"Cunning\": \"Agilité\"","Cote {1}"})
                Check(french.Contains(word),"French: "+word);
            Check(Source("src/TenshoAssistant/MainWindow.ChocoboBarding.cs").Contains("DrawRaceChocobo();"),"on the Chocobo page");
        }),
    ];
}
