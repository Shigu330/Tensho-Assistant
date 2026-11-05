using TenshoAssistant.Collections;
using TenshoAssistant.Gathering;

// 0.526 (user report): the game's « Légendes locales » in Collections.
internal static class Folklore526Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Folklore tomes: the bits by bytes, the twelfth tome (bit 11) included",() =>
        {
            Check(GatherTomeBits.Of(stackalloc byte[]{0,0}).Count==0,"none");
            Check(GatherTomeBits.Of(stackalloc byte[]{0,2}).SetEquals([9]),"bit 9, the botanist of the log");
            Check(GatherTomeBits.Of(stackalloc byte[]{1,8}).SetEquals([0,11]),"the first and the twelfth");
            Check(GatherTomeBits.Of(ReadOnlySpan<byte>.Empty).Count==0,"nothing to read");
        }),
        ("Folklore tomes: a tab of the collections, with its name, by job, and the map uses the same bits",() =>
        {
            Check((int)CollectionKind.Folklore==(int)CollectionKind.OccultLore+1,"added last: saved kinds keep their numbers");
            Check(Read("MainWindow.Collections.cs").Contains("CollectionKind.MasterBook,CollectionKind.Folklore,"),"a tab");
            var reader=Read("Collections/ExtraCollectionReader.Dalamud.cs");
            Check(reader.Contains("16=>miner.Contains(bit),17=>botanist.Contains(bit),18=>fisher.Contains(bit)") && reader.Contains("ItemId:item.RowId"),"each job's array; the item, for how to obtain it");
            Check(Read("Gathering/GatherKnowledgeReader.Dalamud.cs").Contains("GatherTomeBits.Of(state->UnlockedMinerFolkloreTomes)"),"the same reading");
            foreach(var (language,name) in new[]{("en","\"Folklore\": \"Folklore\""),("fr","\"Folklore\": \"Légendes locales\"")})
                Check(Read($"Localization/Languages/{language}.json").Contains(name),"named: "+language);
        }),
    ];
}
