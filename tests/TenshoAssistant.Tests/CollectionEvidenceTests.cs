using TenshoAssistant.Collections;

internal static class CollectionEvidenceTests
{
    private static void Check(bool value,string why){if(!value)throw new Exception(why);}
    private static CollectionEntry Entry=>new(CollectionKind.Title,7,"Titre",0,false,Title:new("Titre","Titre féminin",true,2));
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Collection evidence retains positives without a full list and needs both loaded checks for negatives",()=>
        {
            foreach(var positive in new[]{false,true})foreach(var before in new[]{false,true})foreach(var after in new[]{false,true})
            {
                var entry=CollectionAcquisitionEvidence.Observe(Entry,positive,before,after);
                var expected=positive ? CollectionAcquisition.Acquired:before && after ? CollectionAcquisition.NotAcquired:CollectionAcquisition.Unknown;
                Check(entry.Acquisition==expected && !(entry.ConfirmedUnlocked && entry.NegativeConfirmed),"three-state truth table, never contradictory");
            }
        }),
        ("Losing whole-category loading coverage discards negative evidence without discarding positive unlock events",()=>
        {
            var negative=CollectionAcquisitionEvidence.Observe(Entry,false,true,true);
            var positive=CollectionAcquisitionEvidence.Observe(Entry,true,false,false);
            Check(CollectionAcquisitionEvidence.RequireStableCoverage(negative,false).Acquisition==CollectionAcquisition.Unknown,"one unstable stage invalidates negative conclusion");
            Check(CollectionAcquisitionEvidence.RequireStableCoverage(positive,false).Acquisition==CollectionAcquisition.Acquired,"positive event remains proof without full list");
            Check(CollectionAcquisitionEvidence.RequireStableCoverage(negative,true).Acquisition==CollectionAcquisition.NotAcquired,"stable full coverage retains negative result");
        }),
        ("Collection owner boundaries clear positive and negative evidence while keeping static names and rewards",()=>
        {
            var service=new CollectionService();var negative=CollectionAcquisitionEvidence.Observe(Entry,false,true,true);
            var achievement=new CollectionEntry(CollectionKind.Achievement,7,"Succès",1,true,Achievement:new(1,"Catégorie",1,1,"Genre",1,1,10,123,7,0));
            service.Publish(10,[negative,achievement]);Check(service.State.Entries.Count==2 && service.State.Entries.Select(entry=>entry.Acquisition).ToHashSet().SetEquals([CollectionAcquisition.Acquired,CollectionAcquisition.NotAcquired]),"same numeric ID in two sheets remains separate");
            service.Invalidate();Check(service.State.CharacterId==0 && service.State.Entries.All(entry=>entry.Acquisition==CollectionAcquisition.Unknown),"logout clears every ownership assertion");
            Check(service.State.Entries.Single(entry=>entry.Kind==CollectionKind.Title).Title==Entry.Title,"static gender names/prefix/order retained");
            service.Publish(0,[negative,achievement]);Check(service.State.Entries.All(entry=>entry.Acquisition==CollectionAcquisition.Unknown) && service.State.ObservedAt is null,"offline cannot publish either kind of acquisition proof");
            service.Publish(20,[Entry]);Check(service.State.Entries.Single().Acquisition==CollectionAcquisition.Unknown,"new owner inherits no prior negative or positive");
        }),
        ("Contradictory collection evidence cannot replace the last valid snapshot",()=>
        {
            var service=new CollectionService();service.Publish(10,[Entry]);var previous=service.State;
            var refused=false;try{service.Publish(10,[Entry with {ConfirmedUnlocked=true,NegativeConfirmed=true}]);}catch(ArgumentException){refused=true;}
            Check(refused && ReferenceEquals(previous,service.State),"atomic rejection before publication");
            service.Fail(new InvalidOperationException("failed"));Check(service.State.Entries.All(entry=>entry.Acquisition==CollectionAcquisition.Unknown),"global failure invalidates acquisition scope");
        }),
        ("A completed achievement does not confirm ownership of its reward title or Item",()=>
        {
            var service=new CollectionService();var achievement=new CollectionEntry(CollectionKind.Achievement,99,"Succès",1,true,Achievement:new(4,"Bataille",1,2,"Combat",1,3,20,14221,7,12));
            service.Publish(10,[achievement,Entry]);
            Check(service.State.Entries.Single(entry=>entry.Kind==CollectionKind.Title).Acquisition==CollectionAcquisition.Unknown,"reward relation is not an acquisition event");
            var metadata=service.State.Entries.Single(entry=>entry.Kind==CollectionKind.Achievement).Achievement!;
            Check(metadata.RewardItemId==14221 && metadata.RewardTitleId==7 && metadata.HideConditionId==12,"reward/hide references preserved without inferring claim availability or progress");
        }),
    };
}
