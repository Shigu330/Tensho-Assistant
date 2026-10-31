using TenshoAssistant.Collections;
using TenshoAssistant.Items;

internal static class OrchestrionTests
{
    private static void Check(bool value,string why){if(!value)throw new Exception(why);}
    private static void Throws<T>(Action action) where T:Exception{try{action();}catch(T){return;}throw new Exception(typeof(T).Name);}
    private static ItemCatalog Build(IEnumerable<ItemDefinition> items)=>ItemCatalog.Build(items,new("Item/AdditionalData","French","fixture",1));
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Orchestrion Item lookup keeps native music and Item identities separate and retains all associations",()=>
        {
            var catalog=Build([new(14222,"Rouleau B",false,OrchestrionId:2),new(14221,"Rouleau A",false,OrchestrionId:2),
                new(2,"Unrelated native Item two",false),new(15000,"Card",false,TripleTriadCardId:2),new(19000,"Other music",false,OrchestrionId:14221)]);
            Check(catalog.ForOrchestrion(2).Select(item=>item.Id).SequenceEqual(new uint[]{14221,14222}),"all links sorted by Item identity, not arbitrary first or row-number equality");
            Check(catalog.ForOrchestrion(14221).Single().Id==19000 && catalog.ForTriadCard(2).Single().Id==15000,"typed indices do not cross domains");
            Check(catalog.ForOrchestrion(0).Count==0 && catalog.ForOrchestrion(99).Count==0 && catalog.Find(2)?.OrchestrionId==0,"no inferred association for missing identity");
        }),
        ("Orchestrion item associations own their source lists and remain readonly with catalogue context",()=>
        {
            var source=new List<ItemDefinition>{new(14221,"Roll",false,123,OrchestrionId:1)};var catalog=Build(source);source.Clear();
            var link=catalog.ForOrchestrion(1).Single();Check(link.Id==14221 && link.IconId==123 && catalog.Find(link.Id)==link,"same common Item record, metadata and icon");
            Throws<NotSupportedException>(()=>((IList<ItemDefinition>)catalog.ForOrchestrion(1)).Clear());
            Check(catalog.Search("Roll").Single().OrchestrionId==1 && catalog.Metadata.DataVersion=="fixture","shared search and provenance preserved");
        }),
        ("Orchestrion acquisition confirmations clear with ownership while static categories remain catalogue facts",()=>
        {
            var service=new CollectionService();var music=new OrchestrionMetadata(13,"Quêtes",20,10);
            var entry=new CollectionEntry(CollectionKind.Orchestrion,1,"A Cold Wind",82036,true,Description:"Native description",Music:music);
            service.Publish(10,[entry]);Check(service.State.Entries.Single().ConfirmedUnlocked && service.State.CharacterId==10,"positive observation bound to owner");
            service.Invalidate();var invalid=service.State.Entries.Single();
            Check(!invalid.ConfirmedUnlocked && service.State.CharacterId==0 && service.State.ObservedAt is null && invalid.Music==music && invalid.Description==entry.Description,"logout clears confirmation, not static identity/metadata");
            service.Publish(0,[entry]);Check(!service.State.Entries.Single().ConfirmedUnlocked && service.State.ObservedAt is null,"offline input cannot assert acquisition");
            service.Publish(20,[entry with {ConfirmedUnlocked=false}]);Check(!service.State.Entries.Single().ConfirmedUnlocked,"new character inherits no prior positive flag");
        }),
        ("A partial collection category cannot erase other acquisitions and its issue snapshots are immutable",()=>
        {
            var issues=new List<CollectionCategoryIssue>{new(CollectionKind.Orchestrion,"Acquisitions partielles")};
            var service=new CollectionService();service.Publish(10,[new(CollectionKind.Mount,1,"Mount",1,true),new(CollectionKind.Orchestrion,1,"Music",2,false)],issues);
            issues.Clear();
            Check(service.State.Error is null && service.State.Entries.Single(entry=>entry.Kind==CollectionKind.Mount).ConfirmedUnlocked,"other category retained, no global failure");
            Check(service.State.CategoryIssues!.Single().Kind==CollectionKind.Orchestrion && !service.State.Entries.Single(entry=>entry.Kind==CollectionKind.Orchestrion).ConfirmedUnlocked,"partial domain explicit, negative remains unconfirmed");
            Throws<NotSupportedException>(()=>((IList<CollectionCategoryIssue>)service.State.CategoryIssues!).Clear());
            service.Invalidate();Check(service.State.CategoryIssues is null && service.State.Entries.All(entry=>!entry.ConfirmedUnlocked),"old issues and owner evidence reset");
            service.Publish(20,[]);Check(service.State.CategoryIssues!.Count==0,"successful subsequent snapshot does not retain earlier partial error");
            service.Clear();Check(service.State.Entries.Count==0 && service.State.CharacterId==0,"clear releases catalogue snapshots");
        }),
    };
}
