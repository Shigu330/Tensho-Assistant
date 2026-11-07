// 0.819 (X-53, user report): drops matched by the monster's name and by the wiki pages on disk, not only by its row.
internal static class DropsByName819Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Drops by name: another row of the same name drops it too; case ignored",() =>
        {
            var csv=new Dictionary<string,string>{["MobDrop"]="ItemId,BNpcNameId\n12873,700\n"};
            var names=new Dictionary<uint,string>{[700]="loaghtan",[701]="Loaghtan"};
            var data=new TenshoAssistant.Community.LuminaSupplementalData(csv,id=>names.GetValueOrDefault(id));
            Check(data.ItemsDroppedBy(701).Count==0 && data.ItemsDroppedByName("Loaghtan").SequenceEqual(new uint[]{12873}),"by name");
        }),
        ("Drops by name: the nearby monsters use the row, the name and the wiki's copies, without a request",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.NearbyMonsters.cs"));
            Check(page.Contains("data.ItemsDroppedBy(id).Concat(names.TryGetValue(id,out var name) ? data.ItemsDroppedByName(name):[])") && page.Contains("wiki.Warm(item);"),"three sources");
            var wiki=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Monsters/WikiMonsterSources.cs"));
            Check(wiki.Contains("_=ReadCachedAsync(itemId,disposal.Token);") && wiki.Contains("public WikiMonsterDocument? Cached(uint itemId)"),"disk and memory only");
        }),
    ];
}
