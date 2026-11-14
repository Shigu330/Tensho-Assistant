namespace TenshoAssistant.Discoveries;

/// <summary>X-263 (0.1305): a retainer venture as the game's sheets know it: RetainerTask (IsRandom, RetainerLevel, ClassJobCategory, Task)
/// and, for an exploration, the RetainerTaskRandom name (RetainerTask 584 → RetainerTaskRandom 30084, read on 14/11).</summary>
internal readonly record struct VentureRow(bool Random,string RandomName,string Job,int Level);

/// <summary>X-263 (0.1305): what a venture is called in the discoveries: an exploration by its name, a targeted venture by its class and
/// level, the number only when the sheets give nothing (a venture seen in game reads « Zelkova log ← venture: Woodland Exploration XXI »).</summary>
internal static class VentureNames
{
    public const string Named="DiscoveriesUi.VentureNamed",Job="DiscoveriesUi.VentureJob",Number="DiscoveriesUi.VentureNumber";

    /// <summary>The localisation key of the venture's text and its arguments.</summary>
    public static (string Key,object[] Args) Describe(uint id,VentureRow? row)=>row switch
    {
        { Random:true, RandomName.Length:>0 } known=>(Named,[known.RandomName]),
        { Random:false, Job.Length:>0, Level:>0 } known=>(Job,[known.Job,known.Level]),
        _=>(Number,[id]),
    };

    /// <summary>The class a venture asks for: the ClassJob whose abbreviation is the ClassJobCategory's name (« BOT » → « botanist »);
    /// a category of several classes keeps its own name (« Disciples of War or Magic »).</summary>
    public static string JobName(string category,IEnumerable<(string Abbreviation,string Name)> classJobs)
    {
        if(category.Length==0)return "";
        foreach(var (abbreviation,name) in classJobs)
            if(name.Length>0 && string.Equals(abbreviation,category,StringComparison.OrdinalIgnoreCase))return name;
        return category;
    }
}
