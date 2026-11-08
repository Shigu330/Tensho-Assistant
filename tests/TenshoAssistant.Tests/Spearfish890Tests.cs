// 0.890 (X-73.18): the spearfishing shadow of a catch, kept only when every shadow in sight was alike (docs/evidence/0.890-spearfishing.md).
using TenshoAssistant.Discoveries;
internal static class Spearfish890Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Spearfishing: one kind of shadow in sight gives it, several or none give nothing; catch = AtkValue 21 grows by one, item = AtkValue 12",() =>
        {
            Check(ItemGains.SpearShadow([(2,200)])==(2,200) && ItemGains.SpearShadow([(2,200),(2,200)])==(2,200),"alike");
            Check(ItemGains.SpearShadow([(2,200),(1,150)]) is null && ItemGains.SpearShadow([]) is null && ItemGains.SpearShadow([(0,200)]) is null && ItemGains.SpearShadow([(2,0)]) is null,"unknown");
            Check(DiscoveryStore.HasPlace(DiscoveryKind.SpearfishShadow),"place");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("var count=SpearValue(unit->AtkValues[21]);var item=SpearValue(unit->AtkValues[12]);"),"values");
            Check(watcher.Contains("if(spearCatches>=0 && count==spearCatches+1 && item>0 && ItemGains.SpearShadow(before) is { } shadow)"),"previous read, one catch");
            var capture=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs"));
            Check(capture.Contains("values.Append(\" | slots\");"),"capture of the slots");
            var map=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/NativeMapReplacer.Dalamud.cs"));
            Check(map.Contains("scale {9}, icon {10}"),"map request area logged");
        }),
    ];
}
