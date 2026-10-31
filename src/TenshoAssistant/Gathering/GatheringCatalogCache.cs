using TenshoAssistant.Caching;

namespace TenshoAssistant.Gathering;

// Rows exactly as copied from the client; GatheringCatalog still drops incomplete rows and duplicates when rebuilding.
public sealed record GatheringCatalogCacheData(string DataVersion,IReadOnlyList<GatheringSource> Sources,IReadOnlyList<FishingNotebookEntry> Fishing);

/// <summary>Disk copy of gathering and fishing rows, reused until the client data, the language or the copy shape changes.</summary>
public static class GatheringCatalogCache
{
    public const string Family="catalog-gathering";
    public const int MaximumRows=500_000;
    // Bump when the Lumina → GatheringSource/FishingNotebookEntry copy rules change without a change of record shape.
    private const int CopyRules=1;
    public static readonly string Shape=CatalogCacheKeys.Shape(typeof(GatheringSource),typeof(GatheringSchedule),typeof(GatheringWindow),
        typeof(GatheringScheduleKind),typeof(FishingNotebookEntry));
    public static string Version(string dataVersion)=>CatalogCacheKeys.Version(dataVersion,CopyRules,Shape);
    public static GatheringCatalogCacheData Snapshot(string dataVersion,IEnumerable<GatheringSource> sources,IEnumerable<FishingNotebookEntry> fishing)
        =>new(dataVersion,Array.AsReadOnly(sources.ToArray()),Array.AsReadOnly(fishing.ToArray()));

    public static bool Valid(GatheringCatalogCacheData? data,string dataVersion)
        =>data is not null && data.DataVersion==dataVersion && data.Sources is { Count: >0 and <=MaximumRows } && data.Fishing is { Count: <=MaximumRows }
          && data.Sources.All(row=>row is not null && row.TypeName is not null && row.Zone is not null && row.Place is not null)
          && data.Fishing.All(row=>row is not null && row.Description is not null && row.Folklore is not null);
}
