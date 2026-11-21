// X-710 (0.1704, review of 0.1699 to 0.1703): two order checks on the code's text had become always true (a text gone gives IndexOf -1,
// before everything): CraftPreparation408 (the tab's old id, since 0.1701) and GatherNext728 (an old argument, since 0.731). Both go
// through SourceOrder.Before, false when a text is missing; and every text a test looks for with IndexOf in a file it reads must still
// be in that file (StaleIndexOf).
internal static class StaleOrder1704Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Stale order 1704: an order check is false when either text is missing, or the order reversed",() =>
        {
            Check(SourceOrder.Before("alpha beta","alpha","beta"),"in order");
            Check(!SourceOrder.Before("alpha beta","beta","alpha"),"reversed");
            Check(!SourceOrder.Before("alpha beta","gone","beta"),"first one missing: IndexOf -1 is not « before »");
            Check(!SourceOrder.Before("alpha beta","alpha","gone"),"second one missing");
            Check(!SourceOrder.Before("alpha beta","gone","also gone"),"both missing");
        }),
        ("Stale order 1704: the sweep sees a text gone from the file a test reads, and leaves alone a text that is there",() =>
        {
            var gone="        var page=Read(\"MainWindow.CraftSimulation.cs\");\n        Check(page.IndexOf(\"###NoSuchTab1704\")<page.IndexOf(\"CraftSimulation.Unsupported(recipe)\"),\"order\");\n";
            var stale=StaleIndexOf.Scan("Fake.cs",gone);
            Check(stale.Count==1 && stale[0].StartsWith("Fake.cs:2: page <- MainWindow.CraftSimulation.cs: ###NoSuchTab1704",StringComparison.Ordinal),"the gone text is found: "+string.Join("; ",stale));
            var there="        var page=Read(\"MainWindow.CraftSimulation.cs\");\r\n        Check(page.IndexOf(\"if(preparation) { DrawCraftPlan(recipe); return; }\",StringComparison.Ordinal)>0,\"there\");\r\n";
            Check(StaleIndexOf.Scan("Fake.cs",there).Count==0,"a text that is there (CRLF source) is not stale");
            var escaped="        var en=Read(\"Localization/Languages/en.json\");\n        Check(en.IndexOf(\"\\n  \\\"Navigation\\\": {\")>0,\"escaped\");\n";
            Check(StaleIndexOf.Scan("Fake.cs",escaped).Count==0,"escapes read as the compiler reads them: "+string.Join("; ",StaleIndexOf.Scan("Fake.cs",escaped)));
        }),
        ("Stale order 1704: every text a test looks for with IndexOf in a file it reads is still in that file",() =>
        {
            var stale=StaleIndexOf.Find();
            Check(stale.Count==0,"texts no longer in the file the test reads (IndexOf gives -1, before everything): "+string.Join("; ",stale.Take(10)));
        }),
        ("Stale order 1704: the two order checks of the review go through SourceOrder.Before, and hold on the code",() =>
        {
            var craft=File.ReadAllText(Path.GetFullPath("tests/TenshoAssistant.Tests/CraftPreparation408Tests.cs"));
            Check(craft.Contains("SourceOrder.Before(page,\"if(preparation) { DrawCraftPlan(recipe); return; }\",\"CraftSimulation.Unsupported(recipe)\")",StringComparison.Ordinal),"craft preparation");
            var gather=File.ReadAllText(Path.GetFullPath("tests/TenshoAssistant.Tests/GatherNext728Tests.cs"));
            Check(gather.Contains("SourceOrder.Before(panel,\"DrawGatheringNext(plan,slot,\",\"if(next is { Kind:not GpNextKind.Done })ImGui.Separator();\")",StringComparison.Ordinal),"gathering panel");
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CraftSimulation.cs"));
            Check(SourceOrder.Before(page,"if(preparation) { DrawCraftPlan(recipe); return; }","CraftSimulation.Unsupported(recipe)"),"the preparation is drawn before the simulator's support is asked");
        }),
    ];
}
