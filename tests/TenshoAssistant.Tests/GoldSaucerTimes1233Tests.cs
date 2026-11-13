using TenshoAssistant.MiniGames;

// 0.1233 (X-223, user's request of 12/11, K-39): next GATE (every 20 minutes at :00, :20, :40) and next Jumbo Cactpot draw (fixed universal time per
// region, docs/evidence/0.1233-gold-saucer-times.md). Pure tests with fixed dates; wiring and the game's French words.
internal static class GoldSaucerTimes1233Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Source(string path)=>File.ReadAllText(Path.GetFullPath(path));
    private static DateTimeOffset U(int day,int hour,int minute,int second=0)=>new(2026,11,day,hour,minute,second,TimeSpan.Zero);

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Gold Saucer times 1233: GATE every 20 minutes, strictly after now, over the hour and the day",() =>
        {
            Check(GoldSaucerTimes.NextGate(U(13,7,5))==U(13,7,20),"7:05 → 7:20");
            Check(GoldSaucerTimes.NextGate(U(13,7,20))==U(13,7,40),"on the start → the next one");
            Check(GoldSaucerTimes.NextGate(U(13,7,59,59))==U(13,8,0),"over the hour");
            Check(GoldSaucerTimes.NextGate(U(13,23,45))==U(14,0,0),"over the day");
            Check(GoldSaucerTimes.NextGate(new DateTimeOffset(2026,11,13,9,5,0,TimeSpan.FromHours(2)))==U(13,7,20),"any offset given");
        }),
        ("Gold Saucer times 1233: Jumbo Cactpot draw per region, next week once passed, none for an unknown region",() =>
        {
            // 13/11/2026 is a Friday.
            Check(GoldSaucerTimes.NextDraw(3,U(13,10,0))==U(14,19,0),"Europe: Saturday 19:00 UTC");
            Check(GoldSaucerTimes.NextDraw(1,U(13,10,0))==U(14,12,0),"Japan: Saturday 12:00 UTC");
            Check(GoldSaucerTimes.NextDraw(4,U(13,10,0))==U(14,9,0),"Oceania: Saturday 09:00 UTC");
            Check(GoldSaucerTimes.NextDraw(2,U(13,10,0))==U(15,2,0),"North America: Sunday 02:00 UTC");
            Check(GoldSaucerTimes.NextDraw(3,U(14,19,0))==U(21,19,0),"on the draw → next week");
            Check(GoldSaucerTimes.NextDraw(3,U(14,19,0).AddSeconds(-1))==U(14,19,0),"a second before → today");
            Check(GoldSaucerTimes.NextDraw(0,U(13,10,0)) is null && GoldSaucerTimes.NextDraw(5,U(13,10,0)) is null && GoldSaucerTimes.NextDraw(7,U(13,10,0)) is null,"unknown regions: nothing");
        }),
        ("Gold Saucer times 1233: wiring, alarms and the game's French words",() =>
        {
            var page=Source("src/TenshoAssistant/MainWindow.MiniCactpot.cs");
            Check(page.Contains("DrawGoldSaucerTimes();") && page.Contains("AlarmButton(\"JumboDraw\"") && page.Contains("AlarmRepeat.RealWeekly,At=draw"),"page and weekly alarm");
            Check(Source("src/TenshoAssistant/Plugin.cs").Contains("mainWindow.HomeRegion=()=>player.HomeWorld.ValueNullable?.DataCenter.ValueNullable?.Region.RowId ?? 0;"),"home world's region");
            var french=Source("src/TenshoAssistant/Localization/Languages/fr.json");
            Check(french.Contains("\"NextGate\": \"Prochain JACTA") && french.Contains("Tirage du Méga Cactpot") && !french.Contains("Jumbo"),"Addon 2308 and 9272 in French");
        }),
    ];
}
