using TenshoAssistant.Craft;

// 0.782 (X-25): the game froze when Raphael divided by a base quality gain of 0.
internal static class RaphaelGuard782Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Raphael guard: a zero gain, no durability or progress, or a value past 16 bits is refused before the native call",() =>
        {
            Check(RaphaelActions.InputRefusal(120,0,80,2000,500,4000)=="base quality gain is 0","the freeze");
            Check(RaphaelActions.InputRefusal(0,100,80,2000,500,4000) is not null,"no progress gain");
            Check(RaphaelActions.InputRefusal(120,100,0,2000,500,4000) is not null && RaphaelActions.InputRefusal(120,100,80,0,500,4000) is not null,"no durability, no progress");
            Check(RaphaelActions.InputRefusal(120,100,80,2000,500,70000) is not null,"past 16 bits");
            Check(RaphaelActions.InputRefusal(120,100,80,2000,500,0) is null && RaphaelActions.InputRefusal(120,100,80,2000,500,4000) is null,"a real craft is called");
        }),
        ("Raphael guard: checked before the solver is built, and the refusal is the answer the cache already handles",() =>
        {
            var solver=Read("Craft/RaphaelCraftSolver.Dalamud.cs");
            Check(solver.IndexOf("RaphaelActions.InputRefusal(")<solver.IndexOf("new Solver(config,input,pool)"),"before");
            Check(solver.Contains("throw new ArgumentException(\"Raphael not called: \"") && Read("Craft/RaphaelSequenceCache.cs").Contains("catch(ArgumentException){return null;}"),"handled");
        }),
    ];
}
