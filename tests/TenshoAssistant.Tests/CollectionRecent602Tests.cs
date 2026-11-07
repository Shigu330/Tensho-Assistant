using TenshoAssistant.Collections;

// 0.602 (T28): recently obtained.
internal static class CollectionRecent602Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static CollectionEntry Entry(CollectionKind kind,uint id,bool owned=true)=>new(kind,id,$"N{id}",0,owned);
    private static readonly DateTimeOffset At=new(2026,11,5,12,0,0,TimeSpan.Zero);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Recent: the first reading is not « recent », the next gains are, once, newest first",() =>
        {
            var memory=new CollectionMemory();
            Check(CollectionRecent.Update(memory,[Entry(CollectionKind.Mount,1),Entry(CollectionKind.Mount,2,false)],At) && memory.Recent.Count==0,"baseline");
            Check(!CollectionRecent.Update(memory,[Entry(CollectionKind.Mount,1)],At),"nothing new, nothing to save");
            Check(CollectionRecent.Update(memory,[Entry(CollectionKind.Mount,1),Entry(CollectionKind.Mount,2)],At.AddDays(1)),"a gain");
            CollectionRecent.Update(memory,[Entry(CollectionKind.Mount,3)],At.AddDays(2));
            Check(memory.Recent.Select(gain=>gain.Id).SequenceEqual<uint>([3,2]),"newest first, no repeat");
            Check(memory.Recent[1].At==At.AddDays(1) && memory.Recent[1].Name=="N2","date and name");
        }),
        ("Recent: a kind read for the first time later is a baseline too; an absence removes nothing; achievements left out; a limit",() =>
        {
            var memory=new CollectionMemory();
            CollectionRecent.Update(memory,[Entry(CollectionKind.Mount,1)],At);
            CollectionRecent.Update(memory,[Entry(CollectionKind.Emote,9)],At);
            Check(memory.Recent.Count==0,"new kind: baseline");
            CollectionRecent.Update(memory,[],At);
            Check(memory.Known[CollectionKind.Mount].Contains(1),"absence proves nothing");
            CollectionRecent.Update(memory,[Entry(CollectionKind.Mount,1)],At);
            Check(memory.Recent.Count==0,"still known");
            Check(!CollectionRecent.Update(memory,[Entry(CollectionKind.Achievement,5)],At) && !memory.Known.ContainsKey(CollectionKind.Achievement),"no achievements");
            CollectionRecent.Update(memory,Enumerable.Range(100,CollectionRecent.MaximumShown+5).Select(id=>Entry(CollectionKind.Mount,(uint)id)),At);
            Check(memory.Recent.Count==CollectionRecent.MaximumShown,"bounded");
        }),
        ("Recent: only from a real reading, reset with the player's data",() =>
        {
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Collections.cs"));
            Check(view.Contains("!state.FromCache") && view.Contains("!state.Loading") && view.Contains("state.Error is null"),"not from the cache, a load or an error");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Configuration.cs")).Contains("\"CollectionMemories\""),"reset list");
        }),
    ];
}
