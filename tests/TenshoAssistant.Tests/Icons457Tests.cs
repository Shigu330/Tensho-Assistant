// 0.457 (in-game campaign): crafting actions show their icons in the two lists that had only names.
internal static class Icons457Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Crafting action icons: in the forbidden actions and the recorder's list, from the game's data",() =>
        {
            const string icon="DrawItemIcon(metadata is null ? null:new ItemDefinition(0,metadata.Name,false,metadata.Icon),";
            var rules=Read("MainWindow.CraftRules.cs");
            Check(rules.Contains(icon) && rules.IndexOf(icon,StringComparison.Ordinal)<rules.IndexOf("ImGui.Checkbox(",StringComparison.Ordinal),"forbidden actions: icon before the box");
            var recorder=Read("MainWindow.CraftRecorder.cs");
            Check(recorder.Contains(icon) && recorder.Contains("WrappedTooltip($\"{index+1}. {metadata?.Name ?? actions[index].ToString()}\");") && !recorder.Contains("string.Join(\" › \""),"recorder: icons, name on hover");
        }),
    ];
}
