using TenshoAssistant.Craft;

// A-10 (0.422): when Caraxi's Macro Chain is loaded, chained craft macro pages end with its « /nextmacro ».
internal static class MacroChain422Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Macro Chain: /nextmacro ends the pages when it is loaded; Tensho's marker otherwise",() =>
        {
            var export=Read("Craft/CraftMacroExport.cs");
            Check(CraftMacroExporter.MacroChainNext=="/nextmacro" && export.Contains("if(chain) page.Add(macroChain ? MacroChainNext : TenshoAssistant.Macros.MacroDefinition.Marker);"),"marker choice");
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("plugin.InternalName==\"MacroChain\" && plugin.IsLoaded") && plugin.Contains("macroChainCheckedAt=now.AddSeconds(5)"),"detected from the loaded plugins, at most every 5 s");
            Check(Read("Craft/CraftLogSession.cs").Contains("input.BuffExtraWait,input.MacroChain)") && Read("MainWindow.CraftSimulation.cs").Contains("MacroChainLoaded?.Invoke()==true)"),"both exports");
        }),
    ];
}
