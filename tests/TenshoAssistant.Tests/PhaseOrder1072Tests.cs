// 0.1072 (audit A045): the phase table of the global plan has a State column in both halves, phase 7 says it was mostly done
// before phase 6, and no bare "step 7" / "étape 7" is left (phase 7, or a point's own step such as "X-119 step 1").
internal static class PhaseOrder1072Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Global plan: phase table with its state, no bare step 7",() =>
        {
            var plan = File.ReadAllText(Path.GetFullPath("docs/project/GLOBAL-PLAN.md")).Replace("\r\n","\n");
            Check(plan.Contains("| Phase | Content | Points | Need to play? | State |"),"English phase table has a State column");
            Check(plan.Contains("| Phase | Contenu | Points | Il faut jouer ? | État |"),"French phase table has an État column");
            Check(plan.Contains("Mostly done before phase 6, at the user's request") && plan.Contains("Faite en grande partie avant la phase 6, à la demande de l'utilisateur"),"phase 7 says it came before phase 6, in both halves");
            foreach (var bare in new[]{"Step 7 ","step 7 ","Étape 7 ","étape 7 ","l'étape 7"})
                Check(!plan.Contains(bare),$"no bare « {bare.Trim()} » left");
            Check(plan.Contains("done in fact before phase 6, like phase 7") && plan.Contains("fait en réalité avant la phase 6, comme la phase 7"),"the X-76 order says what happened, in both halves");
        }),
    ];
}
