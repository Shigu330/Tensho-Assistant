// 0.831 (X-61, user report): a new node of the same kind kept the last node's « GP spent » mark, and no plan was offered.
internal static class GatheringNode831Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gathering node: another row or attempts going up again is a new node; the same node keeps its record",() =>
        {
            Check(TenshoAssistant.Gathering.GatheringNodes.IsNew(31000,1,31001,4),"another row");
            Check(TenshoAssistant.Gathering.GatheringNodes.IsNew(0,1,0,4),"attempts up again, rows unread");
            Check(!TenshoAssistant.Gathering.GatheringNodes.IsNew(31000,4,31000,3),"same node, one attempt used");
            Check(!TenshoAssistant.Gathering.GatheringNodes.IsNew(31000,4,0,4),"row not read this time");
            Check(!TenshoAssistant.Gathering.GatheringNodes.IsNew(0,null,0,4),"nothing read before");
        }),
    ];
}
