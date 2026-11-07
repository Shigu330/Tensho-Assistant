using TenshoAssistant.Inventory;

// 0.775 (X-18): a teleport paid after the loading screen.
internal static class GilTeleport775Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gil teleport: a spending after a loading screen is a teleport; a gain is not",() =>
        {
            var now=new DateTimeOffset(2026,11,7,13,57,44,TimeSpan.Zero);
            var seen=new Dictionary<string,DateTimeOffset>{["NowLoading"]=now.AddSeconds(-2)};
            Check(GilJournal.ForDelta(GilJournal.Classify(seen,now),-1240)==GilCategory.Teleport,"the report");
            Check(GilJournal.ForDelta(GilJournal.Classify(seen,now),1000)==GilCategory.Other,"a gain stays Other");
            Check(GilJournal.Classify(new Dictionary<string,DateTimeOffset>{["NowLoading"]=now.AddSeconds(-30)},now)==GilCategory.Other,"too long ago");
            var shop=new Dictionary<string,DateTimeOffset>{["NowLoading"]=now.AddSeconds(-10),["Shop"]=now.AddSeconds(-1)};
            Check(GilJournal.ForDelta(GilJournal.Classify(shop,now),-50)==GilCategory.NpcShop,"the latest window wins");
        }),
    ];
}
