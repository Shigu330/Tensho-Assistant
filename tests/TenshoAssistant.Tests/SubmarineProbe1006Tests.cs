// DEC-043 (0.1006): the probe of the game's submarine functions runs in developer mode only, once, logged in, pointers checked, log only.
internal static class SubmarineProbe1006Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Submarine probe 1006: developer only, once, logged in, functions resolved before any call, wired with a dispose",() =>
        {
            var probe=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/FreeCompany/SubmarineVoyageProbe.Dalamud.cs"));
            Check(probe.Contains("if(done || !developer() || !client.IsLoggedIn)return;") && probe.Contains("done=true;"),"once, developer, logged in");
            var check=probe.IndexOf("MemberFunctionPointers.GetSubmarineVoyageTime==0");var call=probe.IndexOf("HousingManager.GetSubmarineSurveyDuration(b");
            Check(check>0 && call>check && probe.Contains("MemberFunctionPointers.GetSubmarineSurveyDuration==0"),"pointers first");
            Check(probe.Contains("CompetitiveGate.Add(framework,OnUpdate)") && probe.Contains("CompetitiveGate.Remove(framework,OnUpdate)"),"gate and dispose");
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("new FreeCompany.SubmarineVoyageProbe(framework,client,data,log,()=>configuration.DeveloperMode),()=>submarineProbe?.Dispose()"),"wired");
        }),
    ];
}
