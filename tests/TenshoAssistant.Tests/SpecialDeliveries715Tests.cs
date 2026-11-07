using TenshoAssistant.Deliveries;
using TenshoAssistant.Quests;

// W-12 (0.715): the delivery quest lines.
internal static class SpecialDeliveries715Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static QuestEntry Quest(uint id,uint genre,ushort level,QuestProgress progress)=>new(id,"q"+id,"g",0,progress,null){GenreId=genre,Level=level};

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Special deliveries: the genre's quests by level, done ones hidden on request, and the count",() =>
        {
            QuestEntry[] all=[Quest(3,210,90,QuestProgress.Complete),Quest(2,210,80,QuestProgress.Unconfirmed),Quest(9,216,100,QuestProgress.Active),Quest(1,1,1,QuestProgress.Complete)];
            Check(SpecialDeliveries.Of(all,210,false).Select(row=>row.Id).SequenceEqual([2u,3u]),"by level");
            Check(SpecialDeliveries.Of(all,210,true).Single().Id==2,"only left");
            Check(SpecialDeliveries.Count(all,210)==(1,2),"count");
            Check(SpecialDeliveries.Genres.SequenceEqual([204u,210u,216u]),"the three lines");
        }),
    ];
}
