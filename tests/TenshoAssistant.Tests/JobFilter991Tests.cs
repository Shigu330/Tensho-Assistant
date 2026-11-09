using TenshoAssistant.Items;

// X-105 (0.991): the item search filtered by job, through the item's job category (kept in the catalogue, whose cache shape changes with it).
internal static class JobFilter991Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Job filter 991: an item matches when its job category includes the job; no job, no filter",() =>
        {
            var sword=new ItemDefinition(1,"Sword",false,JobCategory:2);var pan=new ItemDefinition(2,"Pan",false,JobCategory:9);
            var filter=new ItemSearchFilter(Job:19,JobCategories:new HashSet<uint>{2,1});
            Check(filter.Matches(sword) && !filter.Matches(pan),"job");
            Check(new ItemSearchFilter().Matches(pan) && !new ItemSearchFilter(Job:19).Matches(sword),"none, or no categories known");
            Check(ItemCatalogCache.Shape.Length>0,"the shape follows the record");
        }),
    ];
}
