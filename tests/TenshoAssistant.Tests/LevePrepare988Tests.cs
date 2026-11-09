using TenshoAssistant.Leves;

// X-103 (0.988): « Prepare » puts what the accepted crafting leves still need into a crafting list.
internal static class LevePrepare988Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Leve prepare 988: quantities added up per item, held taken off, recipes only, unknown quantity counts one",() =>
        {
            var held=new Dictionary<uint,long>{[1]=2};
            var missing=LevePrepare.Missing([(1u,3),(1u,3),(2u,null),(3u,5),(0u,4)],id=>held.GetValueOrDefault(id),id=>id!=3);
            Check(missing.Count==2 && missing[0]==(1u,4L) && missing[1]==(2u,1L),"missing");
        }),
    ];
}
