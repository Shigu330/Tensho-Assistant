using System.Text.Json;
using TenshoAssistant;
using TenshoAssistant.Craft;

// 0.327 (queue step e, audit 2026-11-02): the crafting log panel computes automatically when a recipe is selected (default, as before)
// or only on request (its « Compute » button, « Recalculate » or the plan's « Simulate this recipe »), for one character and recipe.
internal static class CraftLogAutoSolve327Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string Source = Path.GetFullPath("src/TenshoAssistant");
    private static string Read(string relative) => File.ReadAllText(Path.Combine(Source,relative));
    private static string Text(string language,string section,string key)
    {
        using var document = JsonDocument.Parse(Read(Path.Combine("Localization","Languages",language + ".json")));
        return document.RootElement.GetProperty(section).GetProperty(key).GetString() ?? "";
    }
    private static RecipeDefinition Recipe()=>new(123,456,1,"Menuiserie",100,[],0,new(80,100,1000,100,100,100,100,0,0,0));
    private static CraftActionCatalog Catalog()=>new(CraftActionCatalog.Specs.SelectMany(spec=>Enumerable.Range(8,8)
        .Select(job=>new CraftActionMetadata(spec,(uint)job,spec.CanonicalId,spec.Kind+" "+job,1,"Native"))),"fixture");
    private static CraftLogInput Input()=>new(1,Recipe(),new(100,1000,1000,500),Catalog(),[],new(1000,4,32,5000),0,true);
    private static readonly TimeSpan Settled = TimeSpan.FromMilliseconds(350), Early = TimeSpan.FromMilliseconds(349), Later = TimeSpan.FromSeconds(5);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Crafting log « compute automatically »: on by default, reset with the other settings",() =>
        {
            Check(new Configuration().CraftLogAutoSolve,"on by default: nothing changes");
            Check(ConfigurationReset.Reset.Contains("CraftLogAutoSolve") && !ConfigurationReset.Kept.Contains("CraftLogAutoSolve"),"reset with the settings");
        }),
        ("Crafting log, automatic: the start is unchanged (once, 350 ms after the selection settles)",() =>
        {
            var input = Input();
            Check(CraftLogSession.SettleDelay == Settled,"same delay as before");
            Check(CraftLogSession.ShouldStart(input,false,Settled,true,default),"starts once settled");
            Check(!CraftLogSession.ShouldStart(input,false,Early,true,default),"waits for the selection to settle");
            Check(!CraftLogSession.ShouldStart(input,true,Later,true,default),"started once only");
            Check(!CraftLogSession.ShouldStart(null,false,Later,true,default),"nothing selected");
            Check(!CraftLogSession.WaitsForRequest(input,true,default),"never waits for a request");
        }),
        ("Crafting log, on request: waits for this character and recipe, then starts as before",() =>
        {
            var input = Input();
            Check(CraftLogSession.WaitsForRequest(input,false,default) && !CraftLogSession.ShouldStart(input,false,Later,false,default),"no request: waits");
            Check(CraftLogSession.ShouldStart(input,false,Settled,false,(1,123)) && !CraftLogSession.WaitsForRequest(input,false,(1,123)),"asked for this recipe: starts");
            Check(!CraftLogSession.ShouldStart(input,false,Early,false,(1,123)),"still after the selection settles");
            Check(!CraftLogSession.ShouldStart(input,false,Later,false,(2,123)) && !CraftLogSession.ShouldStart(input,false,Later,false,(1,124)),"another character or recipe: waits");
            Check(!CraftLogSession.WaitsForRequest(null,false,default),"nothing selected: no button");
        }),
        ("Crafting log panel and settings: the switch decides the start, the button asks, row and texts follow the rules",() =>
        {
            var panel = Read("MainWindow.CraftLog.cs");
            Check(panel.Contains("CraftLogSession.ShouldStart(input,craftLogStarted,now-craftLogStableSince,configuration.CraftLogAutoSolve,craftLogRequested)",StringComparison.Ordinal),"start decided by the shared rule");
            Check(!panel.Contains("TimeSpan.FromMilliseconds(350)",StringComparison.Ordinal),"one delay, in CraftLogSession");
            Check(panel.Contains("Tip(\"CraftLogUi.ComputeTip\");",StringComparison.Ordinal),"the button has its tooltip");
            Check(panel.Contains("craftLogRequested=(owner,recipe.RecipeId);",StringComparison.Ordinal),"the plan's « Simulate this recipe » asks too");
            var settings = Read("MainWindow.Settings.cs");
            var helper = settings.IndexOf("BoolSetting(\"ModuleSettings.CraftLog\",",StringComparison.Ordinal);
            var greyed = settings.IndexOf("ImGui.BeginDisabled(!configuration.CraftLogHelperEnabled);",StringComparison.Ordinal);
            var row = settings.IndexOf("BoolSetting(\"ModuleSettings.CraftLogAutoSolve\",\"ModuleSettings.CraftLogAutoSolveTip\",()=>configuration.CraftLogAutoSolve,value=>configuration.CraftLogAutoSolve=value,c=>c.CraftLogAutoSolve);",StringComparison.Ordinal);
            Check(helper >= 0 && greyed > helper && row > greyed,"shared row with « ↺ » under the helper switch, greyed while it is off");
            foreach (var language in new[] { "fr","en" })
                Check(Text(language,"ModuleSettings","CraftLogAutoSolve").Length > 0 && Text(language,"CraftLogUi","Compute").Length > 0 && Text(language,"CraftLogUi","ComputeTip").Length > 0,"texts: " + language);
        }),
    ];
}
