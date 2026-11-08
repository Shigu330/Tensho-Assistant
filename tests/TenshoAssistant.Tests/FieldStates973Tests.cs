// X-90 (0.973): Eureka, Bozja and Occult Crescent states written by the developer capture, on change only, in the field content only.
internal static class FieldStates973Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Field states 973: three field directors read, written on change, bounded",() =>
        {
            var capture=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs"));
            foreach(var kind in new[]{"Eureka","Bozja","OccultCrescent"})Check(capture.Contains("PublicContentDirectorType."+kind+")"),kind);
            Check(capture.Contains("if(text==fieldState)return;") && capture.Contains("Line(\"fields\",120,text);"),"on change");
        }),
    ];
}
