using TenshoAssistant.Craft;

internal static class CraftObservedEffectsTests
{
    private static CraftActionCatalog Catalog(bool statuses=true)=>new(CraftActionCatalog.Specs.SelectMany(spec=>Enumerable.Range(8,8).Select(job=>new CraftActionMetadata(spec,(uint)job,spec.CanonicalId,"Action",1,"Description"))),"fixture",
        statuses ? CraftObservedEffects.Specs.Select(spec=>new CraftStatusMetadata(spec.Id,$"Statut {spec.Id}",1,"Native")):null);
    private static CraftLiveFrame Frame(IEnumerable<CraftLiveStatus> statuses,bool complete=true,int step=2,uint flags=2)=>new(new(1,2,0,(ushort)step,1,4,flags,8,new(100,3000,2800,600),400,600,"Objet","Normale","",
        new("0","2000","0","1500","40","80",step.ToString()),[],statuses.ToArray(),complete),null);
    private static void Check(bool condition,string why){if(!condition)throw new Exception(why);}
    private static void Throws<T>(Action run) where T:Exception{try{run();}catch(T){return;}throw new Exception(typeof(T).Name);}
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Observed craft effects derive counters from Param not RemainingTime",()=>
        {
            var result=CraftObservedEffects.Decode(Frame([new(251,7,999),new(2189,3,0)]),Catalog());
            Check(result.CountersKnown && result.Effects.Single(effect=>effect.Metadata.Id==251).Counter==7 && result.Effects.Single(effect=>effect.Metadata.Id==2189).Counter==3,"parameter semantics");
            Check(result.Effects.Single(effect=>effect.Metadata.Id==251).Indefinite && !result.Effects.Single(effect=>effect.Metadata.Id==2189).Indefinite,"stacks versus durations");
        }),
        ("Observed craft effects reject duplicate zero out-of-range and conflicting counters",()=>
        {
            foreach(var statuses in new[]{new[]{new CraftLiveStatus(2189,0,3)},new[]{new CraftLiveStatus(2189,5,3)},new[]{new CraftLiveStatus(251,11,3)},new[]{new CraftLiveStatus(2189,3,0),new(2189,3,0)},new[]{new CraftLiveStatus(252,4,0),new(257,8,0)}})
                Check(!CraftObservedEffects.Decode(Frame(statuses),Catalog()).CountersKnown,"incoherent effects not trusted");
        }),
        ("Observed craft effects keep missing list coverage and missing metadata unknown",()=>
        {
            Check(!CraftObservedEffects.Decode(Frame([],false),Catalog()).CountersKnown,"incomplete scan not absence");
            Check(!CraftObservedEffects.Decode(Frame([]),Catalog(false)).CountersKnown,"missing metadata");
            Check(CraftObservedEffects.Decode(Frame([new(99999,0,0)]),Catalog()).CountersKnown,"unrelated raw status not mapped as craft effect or state proof");
            var rows=CraftObservedEffects.Specs.Select(spec=>new CraftStatusMetadata(spec.Id,"Native",1,"Description")).ToArray();
            var actions=CraftActionCatalog.Specs.SelectMany(spec=>Enumerable.Range(8,8).Select(job=>new CraftActionMetadata(spec,(uint)job,1,"Action",1,"Description")));
            Throws<ArgumentException>(()=>new CraftActionCatalog(actions,"bad",rows.Skip(1)));
        }),
        ("Observed craft unique action history remains unknown when protection disappeared",()=>
        {
            Check(CraftObservedEffects.Decode(Frame([new(3813,0,0)]),Catalog()).UsedTrainedPerfection==true,"active protection proves use");
            Check(CraftObservedEffects.Decode(Frame([],true,1,0),Catalog()).UsedTrainedPerfection==false,"initial step and no protection");
            Check(CraftObservedEffects.Decode(Frame([],true,2,2),Catalog()).UsedTrainedPerfection is null,"later absence cannot prove unused");
            Check(CraftObservedEffects.Decode(Frame([],false,1,0),Catalog()).UsedTrainedPerfection is null,"incomplete initial scan");
            Check(CraftObservedEffects.Decode(Frame([new(3813,0,0),new(3813,0,0)]),Catalog()).UsedTrainedPerfection is null,"duplicate unknown");
        }),
        ("Observed craft unsupported effects and native proc bits remain separate facts",()=>
        {
            var result=CraftObservedEffects.Decode(Frame([new(2190,3,0),new(2665,0,0)],flags:0x38000002),Catalog());
            Check(!result.CountersKnown && result.Effects.All(effect=>!effect.Supported),"effects outside simulator not silently discarded");
            Check(result.NativeProcs.SequenceEqual(new[]{"StandardTouchProc","AdvancedTouchProc","ObservedProc"}),"raw native named flags, not converted into simulated combo");
            Throws<NotSupportedException>(()=>((IList<string>)result.Issues).Clear());
        })
    };
}
