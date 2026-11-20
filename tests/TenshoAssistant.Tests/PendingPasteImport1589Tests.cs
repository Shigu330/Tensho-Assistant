using TenshoAssistant.Maps;

// 0.1589 (X-645, review of 0.1588, minor M1): the import of pasted coordinates waiting for the zone names leaves with the clicked text
// only; a changed or emptied field, or a frame without the share area (another tab), drops it (behaviour, not the source's text).
internal static class PendingPasteImport1589Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private const string Pasted="Limsa Lominsa ( 10.2 , 11.5 )";

    public static IEnumerable<(string Name,Action Test)> Cases =>
    [
        ("Pending paste 1589: nothing waits before a click",() =>
        {
            var wait=new PendingPasteImport();
            var poll=wait.Poll(Pasted,true,1);
            Check(poll.Import is null && !poll.Cancelled && !wait.Waiting,"no import, no cancel");
        }),
        ("Pending paste 1589: the clicked text leaves once the names are read, once",() =>
        {
            var wait=new PendingPasteImport();
            wait.Start(Pasted,10);
            var reading=wait.Poll(Pasted,false,11);
            Check(reading.Import is null && !reading.Cancelled && wait.Waiting,"still reading: it waits");
            var read=wait.Poll(Pasted,true,12);
            Check(read.Import==Pasted && !read.Cancelled && !wait.Waiting,"read: the clicked text");
            var after=wait.Poll(Pasted,true,13);
            Check(after.Import is null && !after.Cancelled,"never twice");
        }),
        ("Pending paste 1589: a changed or emptied text drops the wait",() =>
        {
            foreach(var changed in new[]{Pasted+"\nUl'dah ( 9 , 9 )",""})
            {
                var wait=new PendingPasteImport();
                wait.Start(Pasted,10);
                var poll=wait.Poll(changed,true,11);
                Check(poll.Import is null && poll.Cancelled && !wait.Waiting,$"dropped for « {changed} »");
                Check(wait.Poll(Pasted,true,12).Import is null,"typing the text back does not revive it");
            }
        }),
        ("Pending paste 1589: leaving the tab (a frame not drawn) drops the wait",() =>
        {
            var wait=new PendingPasteImport();
            wait.Start(Pasted,10);
            Check(!wait.Poll(Pasted,false,11).Cancelled,"the next frame: still waiting");
            var back=wait.Poll(Pasted,true,40);
            Check(back.Import is null && back.Cancelled && !wait.Waiting,"back on the tab later: dropped, nothing imported");
        }),
    ];
}
