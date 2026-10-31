namespace TenshoAssistant.Craft;

public sealed record CraftStatusSpec(uint Id,int MaxCounter,bool Indefinite=false,bool Unsupported=false);
public sealed record CraftStatusMetadata(uint Id,string Name,uint Icon,string Description,byte MaxStacks=0);
public sealed record CraftObservedEffect(CraftStatusMetadata Metadata,int? Counter,bool Indefinite,bool Supported);
public sealed record CraftEffectObservation(IReadOnlyList<CraftObservedEffect> Effects,IReadOnlyList<string> Issues,bool CountersKnown,
    bool? UsedTrainedPerfection,IReadOnlyList<string> NativeProcs);

public static class CraftObservedEffects
{
    public static IReadOnlyList<CraftStatusSpec> Specs {get;}=Array.AsReadOnly(new CraftStatusSpec[]
    {
        new(251,10,true),new(252,4),new(2226,4),new(254,3),new(2189,4),new(257,8),new(2191,5),new(1164,8),
        new(3813,0,true),new(2190,5,false,true),new(2665,0,true,true),new(3812,1,false,true)
    });
    public static CraftEffectObservation Decode(CraftLiveFrame frame,CraftActionCatalog catalog)
    {
        var issues=new List<string>();var effects=new List<CraftObservedEffect>();var raw=frame.Raw;
        if(!raw.StatusesComplete)issues.Add("Lecture complète des statuts non établie ; absence d'effet inconnue.");
        if(Specs.Any(spec=>catalog.FindStatus(spec.Id) is null))issues.Add("Métadonnées des effets incomplètes.");
        foreach(var spec in Specs)
        {
            var matches=raw.Statuses.Where(status=>status.Id==spec.Id).ToArray();if(matches.Length==0)continue;
            var metadata=catalog.FindStatus(spec.Id);
            if(matches.Length>1)issues.Add($"Statut {spec.Id} présent plusieurs fois.");
            int? counter=spec.MaxCounter==0 ? null:matches.Length==1 && matches[0].Param>=1 && matches[0].Param<=spec.MaxCounter ? matches[0].Param:null;
            if(spec.MaxCounter>0 && counter is null)issues.Add($"Compteur du statut {spec.Id} inconnu ou hors limites.");
            if(spec.Unsupported)issues.Add($"Effet {metadata?.Name ?? spec.Id.ToString()} non modélisé.");
            if(metadata is not null)effects.Add(new(metadata,counter,spec.Indefinite,!spec.Unsupported));
        }
        if(raw.Statuses.Any(status=>status.Id==252) && raw.Statuses.Any(status=>status.Id==257))issues.Add("Deux effets Parcimonie simultanés : état incohérent.");
        var perfection=raw.Statuses.Count(status=>status.Id==3813);
        bool? used=perfection==1 ? true:perfection==0 && raw.StatusesComplete && frame.UiStep==1 && (raw.StateFlags&2)==0 ? false:null;
        var procs=new List<string>();
        if((raw.StateFlags&0x08000000)!=0)procs.Add("StandardTouchProc");
        if((raw.StateFlags&0x10000000)!=0)procs.Add("AdvancedTouchProc");
        if((raw.StateFlags&0x20000000)!=0)procs.Add("ObservedProc");
        return new(Array.AsReadOnly(effects.ToArray()),Array.AsReadOnly(issues.ToArray()),issues.Count==0,used,Array.AsReadOnly(procs.ToArray()));
    }
}
