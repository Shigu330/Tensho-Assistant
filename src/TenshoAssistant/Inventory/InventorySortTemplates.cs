namespace TenshoAssistant.Inventory;

// No character, retainer or observed item identity belongs in a reusable rule template.
public sealed record InventorySortTemplate(Guid Id,string Name,int ContainerCount,int SlotsPerContainer,
    IReadOnlyList<InventorySortCriterion> Criteria,IReadOnlyList<InventorySortZone> Zones,int Version=1);

public static class InventorySortTemplates
{
    public static void Validate(InventorySortTemplate template)
    {
        if(template.Id==Guid.Empty || template.Version!=1 || string.IsNullOrWhiteSpace(template.Name) || template.Name.Length>80
            || (template.ContainerCount,template.SlotsPerContainer) is not ((4,35) or (2,35) or (7,25)))
            throw new InvalidOperationException("Modèle invalide : nom, version ou géométrie non pris en charge.");
        InventorySortRules.ValidateContent(template.Criteria,template.Zones,template.ContainerCount*template.SlotsPerContainer);
    }
    public static bool Compatible(InventorySortTemplate template,InventorySortScope scope)
    {
        try{Validate(template);return template.ContainerCount==scope.ContainerCount && template.SlotsPerContainer==scope.SlotsPerContainer;}
        catch(InvalidOperationException){return false;}
    }
    private static InventorySortTemplate Copy(InventorySortTemplate value)=>value with
    {Criteria=Array.AsReadOnly(value.Criteria.ToArray()),Zones=InventorySortRules.CopyZones(value.Zones)};
    public static InventorySortTemplate Create(InventorySortProfile profile,InventorySortScope scope,string name,Guid? id=null)
    {
        InventorySortRules.Validate(profile,scope);
        var value=new InventorySortTemplate(id ?? Guid.NewGuid(),name.Trim(),scope.ContainerCount,scope.SlotsPerContainer,profile.Criteria,profile.Zones);
        Validate(value);return Copy(value);
    }
    public static InventorySortProfile Load(InventorySortTemplate template,InventorySortScope scope)
    {
        Validate(template);
        if(!Compatible(template,scope))throw new InvalidOperationException("Ce modèle utilise une autre géométrie de rangement.");
        var profile=new InventorySortProfile(scope.CharacterId,scope.Storage?.Kind,scope.Storage?.StorageId ?? 0,template.Criteria,template.Zones);
        InventorySortRules.Validate(profile,scope);return InventorySortRules.Copy(profile);
    }
    public static List<InventorySortTemplate> Save(IEnumerable<InventorySortTemplate>? saved,InventorySortTemplate template)
    {
        Validate(template);
        var before=(saved ?? []).Where(value=>value is not null).Take(65).ToArray();
        if(before.Length>64)throw new InvalidOperationException("La bibliothèque dépasse 64 modèles ; supprimez les entrées excédentaires.");
        if(before.Any(value=>value.Id==template.Id && (value.ContainerCount!=template.ContainerCount || value.SlotsPerContainer!=template.SlotsPerContainer)))
            throw new InvalidOperationException("La géométrie du modèle doit rester identique ; créez un autre modèle pour ce rangement.");
        var values=before.Where(value=>value.Id!=template.Id).ToList();
        if(values.Count>=64)throw new InvalidOperationException("La bibliothèque est limitée à 64 modèles.");
        if(values.Any(value=>string.Equals(value.Name,template.Name,StringComparison.OrdinalIgnoreCase)))throw new InvalidOperationException("Ce nom existe déjà ; sélectionnez le modèle pour le mettre à jour.");
        values.Add(Copy(template));return values;
    }
    public static List<InventorySortTemplate> Delete(IEnumerable<InventorySortTemplate>? saved,Guid id)
        =>(saved ?? []).Where(value=>value is not null && value.Id!=id).ToList();
}
