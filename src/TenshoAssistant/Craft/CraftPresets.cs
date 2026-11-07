namespace TenshoAssistant.Craft;

public sealed record CraftPreset(Guid Id,string Name,uint RecipeId,uint ResultItemId,uint CraftTypeId,IReadOnlyList<CraftActionKind> Actions,DateTimeOffset SavedAt);
/// <summary>0.590 (C16): the orders of the saved sequences' page.</summary>
public enum CraftPresetSort {Saved,Name,Item,Job,Newest}
public static class CraftPresets
{
    public const int Capacity=100;
    public static bool Valid(CraftPreset? preset)=>preset is not null && preset.Id!=Guid.Empty && preset.RecipeId>0 && preset.ResultItemId>0 && preset.CraftTypeId<=7
        && !string.IsNullOrWhiteSpace(preset.Name) && preset.Name.Length<=80 && !preset.Name.Any(char.IsControl)
        && preset.Actions is {Count:>=1 and <=100} && preset.Actions.All(action=>CraftActionCatalog.Specs.Any(spec=>spec.Kind==action)) && preset.SavedAt!=default;
    public static CraftPreset Create(RecipeDefinition recipe,string name,IEnumerable<CraftActionKind> actions)
    {
        var copied=actions.Take(101).ToArray();var preset=new CraftPreset(Guid.NewGuid(),name.Trim(),recipe.RecipeId,recipe.ResultItemId,recipe.CraftTypeId,Array.AsReadOnly(copied),DateTimeOffset.UtcNow);
        if(!Valid(preset))throw new ArgumentException("A name of 1–80 characters and a sequence of 1–100 recognised actions are required.");return preset;
    }
    public static IReadOnlyList<CraftPreset> Add(IReadOnlyList<CraftPreset> source,CraftPreset preset)
    {
        if(!Valid(preset))throw new ArgumentException("Invalid saved sequence.");
        if(source.Count>=Capacity)throw new InvalidOperationException("Limit of 100 saved sequences.");
        if(source.Any(row=>row is not null && (row.Id==preset.Id || row.RecipeId==preset.RecipeId && string.Equals(row.Name?.Trim(),preset.Name.Trim(),StringComparison.OrdinalIgnoreCase))))
            throw new InvalidOperationException("This name already exists for this recipe.");
        return Array.AsReadOnly(source.Append(preset with {Actions=Array.AsReadOnly(preset.Actions.ToArray())}).ToArray());
    }
    /// <summary>0.468: a name not yet used for this recipe (Add refuses a duplicate): the name, then « name (2) » and so on.</summary>
    public static string UniqueName(IReadOnlyList<CraftPreset> source,uint recipeId,string name)
    {
        bool Used(string candidate)=>source.Any(row=>row is not null && row.RecipeId==recipeId && string.Equals(row.Name?.Trim(),candidate.Trim(),StringComparison.OrdinalIgnoreCase));
        if(!Used(name))return name;
        for(var number=2;;number++)if($"{name} ({number})" is var candidate && !Used(candidate))return candidate;
    }
    /// <summary>0.590 (C16): a new name for a sequence, unique for its recipe; changing only its capitals is allowed.</summary>
    public static IReadOnlyList<CraftPreset> Rename(IReadOnlyList<CraftPreset> source,int index,string name)
    {
        if(index<0 || index>=source.Count || source[index] is not { } row)throw new ArgumentOutOfRangeException(nameof(index));
        var renamed=row with {Name=name.Trim()};
        if(!Valid(renamed))throw new ArgumentException("A name of 1\u201380 characters is required.");
        if(source.Where((_,position)=>position!=index).Any(other=>other is not null && other.RecipeId==row.RecipeId && string.Equals(other.Name?.Trim(),renamed.Name,StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("This name already exists for this recipe.");
        return Array.AsReadOnly(source.Select((other,position)=>position==index ? renamed:other).ToArray());
    }
    /// <summary>0.590 (C16): a copy of a sequence under the next free name, added at the end.</summary>
    public static IReadOnlyList<CraftPreset> Duplicate(IReadOnlyList<CraftPreset> source,int index)
    {
        if(index<0 || index>=source.Count || source[index] is not { } row || !Valid(row))throw new ArgumentOutOfRangeException(nameof(index));
        return Add(source,row with {Id=Guid.NewGuid(),Name=UniqueName(source,row.RecipeId,row.Name),SavedAt=DateTimeOffset.UtcNow});
    }
    /// <summary>
    /// 0.590 (C16): the positions to show: every word of the search in the name, the item or the job, in the chosen order. An unreadable entry
    /// has nothing to search: it shows only without a search, last.
    /// </summary>
    public static int[] View(IReadOnlyList<CraftPreset> source,string? query,CraftPresetSort sort,Func<CraftPreset,string> item,Func<CraftPreset,string> job)
    {
        var words=(query ?? "").Split(' ',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
        var rows=new List<int>();var unreadable=new List<int>();
        for(var index=0;index<source.Count;index++)
        {
            if(source[index] is not { } row){if(words.Length==0)unreadable.Add(index);continue;}
            var text=$"{row.Name} {item(row)} {job(row)}";
            if(words.All(word=>text.Contains(word,StringComparison.OrdinalIgnoreCase)))rows.Add(index);
        }
        string Name(int index)=>source[index].Name ?? "";
        IEnumerable<int> sorted=sort switch
        {
            CraftPresetSort.Name=>rows.OrderBy(Name,StringComparer.CurrentCultureIgnoreCase),
            CraftPresetSort.Item=>rows.OrderBy(index=>item(source[index]),StringComparer.CurrentCultureIgnoreCase).ThenBy(Name,StringComparer.CurrentCultureIgnoreCase),
            CraftPresetSort.Job=>rows.OrderBy(index=>job(source[index]),StringComparer.CurrentCultureIgnoreCase).ThenBy(Name,StringComparer.CurrentCultureIgnoreCase),
            CraftPresetSort.Newest=>rows.OrderByDescending(index=>source[index].SavedAt),
            _=>rows,
        };
        return sorted.Concat(unreadable).ToArray();
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
