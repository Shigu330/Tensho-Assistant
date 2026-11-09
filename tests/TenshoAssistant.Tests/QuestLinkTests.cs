using TenshoAssistant.Quests;
using TenshoAssistant.UI;

internal static class QuestLinkTests
{
    private static QuestEntry Q(uint id, QuestProgress progress = QuestProgress.Unconfirmed, byte join = 1, params uint[] previous)
        => new(id, $"Quest {id}", "Category", 0, progress, null) { PreviousQuestIds = previous, PreviousQuestJoin = join };
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Followed quest settings migrate without losing full native identities or foreign preferences", () =>
        {
            var legacy = System.Text.Json.JsonSerializer.Deserialize<TenshoAssistant.Configuration>("{\"Version\":1,\"OpenOnLoad\":true}")!;
            Check(legacy.FollowedQuests.Count == 0 && legacy.OpenOnLoad, "legacy preferences preserved with empty follow list");
            legacy.FollowedQuests = [65537,70001];
            var copied = System.Text.Json.JsonSerializer.Deserialize<TenshoAssistant.Configuration>(System.Text.Json.JsonSerializer.Serialize(legacy))!;
            Check(copied.FollowedQuests.SequenceEqual(new uint[] {65537,70001}) && copied.OpenOnLoad, "quest row identities survive settings reload");
        }),
        ("Quest details preserve native map territory issuer and level identity through invalidation", () =>
        {
            var service = new QuestService();
            service.Publish(42,[Q(65537,QuestProgress.Active) with
            { Level=50, AdditionalLevel=10, Zone="Zone", IssuerId=1000123, IssuerMapId=42, IssuerTerritoryId=100 }]);
            service.Invalidate();
            var entry = new QuestLinks(service.State.Entries).Find(65537)!;
            Check(entry.Level == 50 && entry.AdditionalLevel == 10 && entry.Zone == "Zone", "static metadata survives logout");
            Check(entry.IssuerId == 1000123 && entry.IssuerMapId == 42 && entry.IssuerTerritoryId == 100, "map territory and untyped issuer IDs remain distinct");
            Check(entry.Progress == QuestProgress.Unconfirmed && entry.Sequence is null, "static details imply no character eligibility");
        }),
        ("Quest links retain full row identities and direct reverse links without recursive cycles", () =>
        {
            var links = new QuestLinks([Q(65537, previous: [65538,65538,0]), Q(65538, previous: [65537]), Q(1)]);
            Check(links.Find(65537)!.PreviousQuestIds.SequenceEqual(new uint[] {65538}), "duplicates and zero removed");
            Check(links.Following(65537).Single().Id == 65538 && links.Following(65538).Single().Id == 65537, "cycles are finite direct links");
            Check(links.Find(1)!.Id == 1 && links.Following(1).Count == 0, "full ID not truncated to ushort");
        }),
        ("Quest all and any joins only confirm completion with positive evidence", () =>
        {
            var links = new QuestLinks([Q(1,QuestProgress.Complete),Q(2,QuestProgress.Active),Q(3)]);
            Check(links.CompletionEvidence(Q(4,join:2,previous:[1,2])), "one positive confirms ANY completion condition");
            Check(!links.CompletionEvidence(Q(4,previous:[1,2])), "active does not confirm completion for ALL");
            Check(links.CompletionEvidence(Q(4,previous:[1])), "all complete confirmed");
            Check(!links.CompletionEvidence(Q(4,join:2,previous:[2,3,999])), "unconfirmed and missing never count as complete");
        }),
        ("Quest unknown joins missing rows and absent links never invent satisfied conditions", () =>
        {
            var links = new QuestLinks([Q(1,QuestProgress.Complete)]);
            Check(!links.CompletionEvidence(Q(2,join:99,previous:[1])), "unknown join preserved");
            Check(!links.CompletionEvidence(Q(2)) && !links.CompletionEvidence(Q(2,previous:[999])), "no vacuous success");
            Check(QuestLinks.JoinLabel(99).Contains("99") && links.Find(999) is null, "raw mode and unresolved identity retained");
        }),
        ("Quest publication snapshots nested links and logout removes character progression", () =>
        {
            var ids = new uint[] { 1,2 };
            var service = new QuestService();
            service.Publish(42,[Q(1,QuestProgress.Complete),Q(3,previous:ids) with { IsRepeatable = true, PreviousQuestStep = 4 }]);
            ids[0] = 999;
            var entry = service.State.Entries.Single(q => q.Id == 3);
            Check(entry.PreviousQuestIds[0] == 1 && entry.PreviousQuestStep == 4 && entry.IsRepeatable, "managed data detached with raw metadata");
            var links = new QuestLinks(service.State.Entries);
            service.Invalidate();
            Check(links.Find(1)!.Progress == QuestProgress.Complete && service.State.Entries.All(q => q.Progress == QuestProgress.Unconfirmed), "old snapshot and ownerless state distinct");
            Check(!new QuestLinks(service.State.Entries).CompletionEvidence(Q(4,previous:[1])), "old positive does not leak after logout");
        }),
        ("Quest reverse index owns producer lists and preserves unresolved prerequisite links", () =>
        {
            var ids = new List<uint> {999};
            var source = new List<QuestEntry> { Q(2) with { PreviousQuestIds = ids } };
            var links = new QuestLinks(source); ids.Clear(); source.Clear();
            Check(links.Following(999).Single().Id == 2 && links.Find(2)!.PreviousQuestIds.Single() == 999, "missing named row retained, source detached");
            try { ((IList<uint>)links.Find(2)!.PreviousQuestIds).Clear(); throw new Exception("mutable link list"); }
            catch (NotSupportedException) { }
        }),
        ("0.550: the quest list has a level range and sorted columns",() =>
        {
            Check(TenshoAssistant.Quests.QuestList.InLevels(0,0,0) && TenshoAssistant.Quests.QuestList.InLevels(50,0,0),"no limit: everything");
            Check(!TenshoAssistant.Quests.QuestList.InLevels(0,10,0),"unknown level is not level 0");
            Check(TenshoAssistant.Quests.QuestList.InLevels(50,50,50) && !TenshoAssistant.Quests.QuestList.InLevels(49,50,0) && !TenshoAssistant.Quests.QuestList.InLevels(51,0,50),"bounds");
            TenshoAssistant.Quests.QuestEntry Of(uint id,string name,int level,string zone)=>new(id,name,"Cat",0,TenshoAssistant.Quests.QuestProgress.Unconfirmed,null){Level=(ushort)level,Zone=zone};
            var list=new[]{Of(1,"B",10,"Z"),Of(2,"A",20,"Y"),Of(3,"C",10,"X")};
            Check(TenshoAssistant.Quests.QuestList.Sorted(list,TenshoAssistant.Quests.QuestList.ColumnLevel,true).First().Id==2,"by level, descending");
            Check(TenshoAssistant.Quests.QuestList.Sorted(list,TenshoAssistant.Quests.QuestList.ColumnLevel,false).Select(entry=>entry.Id).SequenceEqual(new uint[]{1,3,2}),"then by name");
            Check(TenshoAssistant.Quests.QuestList.Sorted(list,TenshoAssistant.Quests.QuestList.ColumnZone,false).First().Id==3,"by zone");
        }),
        ("0.582: a quest is available when not started and its known prerequisites are done",() =>
        {
            TenshoAssistant.Quests.QuestEntry Of(uint id,TenshoAssistant.Quests.QuestProgress progress,params uint[] previous)=>new(id,"Q"+id,"C",0,progress,null){PreviousQuestIds=previous,PreviousQuestJoin=1};
            var done=Of(1,TenshoAssistant.Quests.QuestProgress.Complete);
            var open=Of(2,TenshoAssistant.Quests.QuestProgress.Unconfirmed,1);
            var locked=Of(3,TenshoAssistant.Quests.QuestProgress.Unconfirmed,2);
            var free=Of(4,TenshoAssistant.Quests.QuestProgress.Unconfirmed);
            var links=new TenshoAssistant.Quests.QuestLinks([done,open,locked,free]);
            Check(!TenshoAssistant.Quests.QuestList.Available(links,done),"done");
            Check(TenshoAssistant.Quests.QuestList.Available(links,open) && TenshoAssistant.Quests.QuestList.Available(links,free),"open");
            Check(!TenshoAssistant.Quests.QuestList.Available(links,locked),"locked");
        }),
        ("0.583: the way to a quest lists what is left, first to take first, and survives a long chain and a cycle",() =>
        {
            TenshoAssistant.Quests.QuestEntry Of(uint id,TenshoAssistant.Quests.QuestProgress progress,byte join,params uint[] previous)=>new(id,"Q"+id,"C",0,progress,null){PreviousQuestIds=previous,PreviousQuestJoin=join};
            var open=TenshoAssistant.Quests.QuestProgress.Unconfirmed;var done=TenshoAssistant.Quests.QuestProgress.Complete;
            var links=new TenshoAssistant.Quests.QuestLinks([Of(1,done,0),Of(2,open,1,1),Of(3,open,1,2),Of(4,open,1,3)]);
            Check(TenshoAssistant.Quests.QuestList.PathTo(links,4).Todo.Select(entry=>entry.Id).SequenceEqual(new uint[]{2,3}),"in order, without itself or what is done");
            var either=new TenshoAssistant.Quests.QuestLinks([Of(1,done,0),Of(2,open,0),Of(3,open,2,1,2)]);
            Check(TenshoAssistant.Quests.QuestList.PathTo(either,3).Todo.Count==0,"any of them, one done");
            var cycle=new TenshoAssistant.Quests.QuestLinks([Of(1,open,1,2),Of(2,open,1,1)]);
            Check(TenshoAssistant.Quests.QuestList.PathTo(cycle,1).Todo.Select(entry=>entry.Id).SequenceEqual(new uint[]{2}),"a cycle ends");
            var chain=Enumerable.Range(1,5000).Select(index=>Of((uint)index,open,1,index>1 ? [(uint)index-1]:[])).ToArray();
            var cut=TenshoAssistant.Quests.QuestList.PathTo(new TenshoAssistant.Quests.QuestLinks(chain),5000);
            Check(cut.Cut,"a very long chain is cut, no stack overflow");
            var unknown=new TenshoAssistant.Quests.QuestLinks([Of(1,open,9,2),Of(2,open,0)]);
            Check(TenshoAssistant.Quests.QuestList.PathTo(unknown,1).Unknown,"unknown join");
        }),
    ];
}
