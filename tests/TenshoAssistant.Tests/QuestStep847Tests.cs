// 0.847 (X-69, user report): the journal's target goes when the quest's step is done, not only when the quest ends.
internal static class QuestStep847Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Quest step: any journal target follows the quest point it stood on",() =>
        {
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(view.Contains("if(mapGameTarget is { } bare && mapLive?.State?.QuestPoints is { } points)"),"every target");
            (uint,uint,float,float)[] before=[(2046,11,100,50)];(uint,uint,float,float)[] after=[(2046,11,300,80)];
            Check(TenshoAssistant.Maps.MapQuestTarget.QuestAt(before,11,new(101,51))==2046,"found at the target");
            Check(TenshoAssistant.Maps.MapQuestTarget.Gone(after,2046,11,new(101,51)),"next step elsewhere: gone");
        }),
    ];
}
