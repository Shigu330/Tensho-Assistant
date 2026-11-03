using TenshoAssistant.Craft;

// A-08 (0.425): a pasted crafting macro read back into simulator actions.
internal static class CraftMacroImport425Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Macro import: /ac lines with or without quotes and waits; other commands ignored; unknown names listed",() =>
        {
            var names=new Dictionary<string,CraftActionKind>(StringComparer.OrdinalIgnoreCase)
                {["Mémoire musculaire"]=CraftActionKind.MuscleMemory,["Manipulation"]=CraftActionKind.Manipulation,["Ouvrage de base"]=CraftActionKind.BasicSynthesis};
            CraftActionKind? Find(string name)=>names.TryGetValue(name,out var kind) ? kind:null;
            var text="/mlock\n/ac \"Mémoire musculaire\" <wait.3>\r\n/ac Manipulation <wait.2>\n  /action \"ouvrage de base\"\n/ac \"Inconnue\" <wait.3>\n/echo fini <se.1>\n/nextmacro\n";
            var result=CraftMacroImport.Parse(text,Find);
            Check(result.Actions.SequenceEqual([CraftActionKind.MuscleMemory,CraftActionKind.Manipulation,CraftActionKind.BasicSynthesis]),"actions in order, case ignored");
            Check(result.Unknown.SequenceEqual(["Inconnue"]) && result.Ignored==3,"unknown listed, other commands counted");
            Check(CraftMacroImport.Parse(new string('a',CraftMacroImport.MaxText+1),Find).Actions.Count==0 && CraftMacroImport.Parse(null,Find).Ignored==0,"bounded");
            var many=string.Join('\n',Enumerable.Repeat("/ac Manipulation",150));
            Check(CraftMacroImport.Parse(many,Find).Actions.Count==CraftMacroImport.MaxActions,"at most 100 actions");
        }),
    ];
}
