namespace TenshoAssistant.Craft;

public sealed record CraftPreset(Guid Id,string Name,uint RecipeId,uint ResultItemId,uint CraftTypeId,IReadOnlyList<CraftActionKind> Actions,DateTimeOffset SavedAt);
public static class CraftPresets
{
    public const int Capacity=100;
    public static bool Valid(CraftPreset? preset)=>preset is not null && preset.Id!=Guid.Empty && preset.RecipeId>0 && preset.ResultItemId>0 && preset.CraftTypeId<=7
        && !string.IsNullOrWhiteSpace(preset.Name) && preset.Name.Length<=80 && !preset.Name.Any(char.IsControl)
        && preset.Actions is {Count:>=1 and <=100} && preset.Actions.All(action=>CraftActionCatalog.Specs.Any(spec=>spec.Kind==action)) && preset.SavedAt!=default;
    public static CraftPreset Create(RecipeDefinition recipe,string name,IEnumerable<CraftActionKind> actions)
    {
        var copied=actions.Take(101).ToArray();var preset=new CraftPreset(Guid.NewGuid(),name.Trim(),recipe.RecipeId,recipe.ResultItemId,recipe.CraftTypeId,Array.AsReadOnly(copied),DateTimeOffset.UtcNow);
        if(!Valid(preset))throw new ArgumentException("Nom de 1–80 caractères et séquence de 1–100 actions reconnues requis.");return preset;
    }
    public static IReadOnlyList<CraftPreset> Add(IReadOnlyList<CraftPreset> source,CraftPreset preset)
    {
        if(!Valid(preset))throw new ArgumentException("Séquence sauvegardée invalide.");
        if(source.Count>=Capacity)throw new InvalidOperationException("Limite de 100 séquences sauvegardées.");
        if(source.Any(row=>row is not null && (row.Id==preset.Id || row.RecipeId==preset.RecipeId && string.Equals(row.Name?.Trim(),preset.Name.Trim(),StringComparison.OrdinalIgnoreCase))))
            throw new InvalidOperationException("Ce nom existe déjà pour cette recette.");
        return Array.AsReadOnly(source.Append(preset with {Actions=Array.AsReadOnly(preset.Actions.ToArray())}).ToArray());
    }
    public static RecipeDefinition? Resolve(CraftPreset? preset,RecipeCatalog? catalog)=>Valid(preset) ? catalog?.ForResult(preset!.ResultItemId).FirstOrDefault(recipe=>recipe.RecipeId==preset.RecipeId && recipe.CraftTypeId==preset.CraftTypeId):null;
    public static IReadOnlyList<CraftPreset> Remove(IReadOnlyList<CraftPreset> source,int index)
    {
        if(index<0 || index>=source.Count)throw new ArgumentOutOfRangeException(nameof(index));
        return Array.AsReadOnly(source.Where((_,position)=>position!=index).ToArray());
    }
    public static void Commit(Configuration configuration,Action save,Func<IReadOnlyList<CraftPreset>,IReadOnlyList<CraftPreset>> edit)
    {
        var previous=configuration.CraftPresets;var updated=edit(previous ?? []).ToList();configuration.CraftPresets=updated;
        try{save();}catch{configuration.CraftPresets=previous;throw;}
    }
}
