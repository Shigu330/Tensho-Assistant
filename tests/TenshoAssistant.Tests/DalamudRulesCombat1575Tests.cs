// 0.1575 (X-608, DEC-055: the user's choice of 18/11): in a PvE fight Tensho may show what the player would already know, taken from the
// game; never a mechanic prediction, a boss timer, a measure of the damage or an action in the player's place. The public documents say it
// in both languages, and the code keeps none of the functions a parser or a boss timer would need.
internal static class DalamudRulesCombat1575Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static IEnumerable<string> Sources() =>
        Directory.EnumerateFiles(Path.GetFullPath("src/TenshoAssistant"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar));
    private static string Name(string path) => Path.GetRelativePath(Path.GetFullPath("src/TenshoAssistant"), path).Replace('\\', '/');

    /// <summary>What a damage parser reads (the game's action effects, its combat log, the tools that share them) and what a boss timer or a
    /// mechanic prediction reads (an enemy's cast and its times, an enrage, a fight timeline tool).</summary>
    private static readonly string[] Parser = ["ReceiveActionEffect", "ActionEffectHandler", "ActorControl", "CombatLog", "BattleLog", "DamageDealt", "IINACT", "OverlayPlugin", "Advanced Combat Tracker", "EnmityList"];
    private static readonly string[] BossTimer = ["CurrentCastTime", "TotalCastTime", "CastInfo", "Enrage", "Cactbot", "BossMod"];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Dalamud rules combat 1575: nothing a damage parser or a boss timer needs is in the code", () =>
        {
            var found = new List<string>();
            foreach (var path in Sources())
            {
                var text = File.ReadAllText(path);
                foreach (var token in Parser.Concat(BossTimer))
                    if (text.Contains(token, StringComparison.OrdinalIgnoreCase)) found.Add(Name(path) + ": " + token);
            }
            Check(found.Count == 0, "combat stays informative (DEC-055): no parser, no boss timer: " + string.Join(", ", found));
        }),
        ("Dalamud rules combat 1575: the refusal finds a parser or a boss timer when one is written", () =>
        {
            // The same search on three made lines: a hook on the action effects, an enemy's cast time read, and an ordinary line of Tensho.
            bool Refused(string line) => Parser.Concat(BossTimer).Any(token => line.Contains(token, StringComparison.OrdinalIgnoreCase));
            Check(Refused("private Hook<ActionEffectHandler.Delegates.Receive>? receiveActionEffectHook;"), "a damage parser's hook is refused");
            Check(Refused("var left = boss->GetCastInfo()->TotalCastTime - boss->GetCastInfo()->CurrentCastTime;"), "a boss cast timer is refused");
            Check(Refused("overlayPlugin.Subscribe(\"CombatData\")") && Refused("// time to enrage"), "ACT's overlay and an enrage timer are refused");
            Check(!Refused("ImGui.TextColored(Success, enemy.Weakness); foreach (var action in enemy.Actions) ImGui.TextDisabled(BeastActionText(action));"), "what the game's sheets tell (weakness, actions, effects) is not refused");
        }),
        ("Dalamud rules combat 1575: DEC-055 is in both halves of DECISIONS.md with the user's words, and DEC-009 points to it", () =>
        {
            var text = Read("docs/project/DECISIONS.md");
            var split = text.IndexOf("\n---\n", StringComparison.Ordinal);
            Check(split > 0, "two halves");
            var (english, french) = (text[..split], text[split..]);
            Check(english.Contains("\nID: DEC-055\n") && french.Contains("\nDEC-ID: DEC-055\n"), "DEC-055 in both halves");
            foreach (var half in new[] { english, french })
                Check(half.Contains("« et pas oublie que aide au combat tant que ca fais pas a notre place c'est bon »") && half.Contains("« Infos du jeu, aussi en combat »"), "the user's words of 18/11, as given");
            Check(english.Contains("replaced in part by DEC-055") && french.Contains("remplacée en partie par DEC-055"), "DEC-009 says what replaces it in part");
            Check(english.Contains("Never a mechanic prediction nor a boss timer, never a measure of the damage") && french.Contains("Jamais de prédiction de mécanique ni de minuteur de boss, jamais de mesure des dégâts"), "the limits are written");
        }),
        ("Dalamud rules combat 1575: PUBLISHING.md says the rule of DEC-055 in both languages and no longer « nothing during a fight »", () =>
        {
            var text = Read("docs/project/PUBLISHING.md");
            Check(!text.Contains("No combat domain: nothing during a fight") && !text.Contains("Pas de domaine combat : rien pendant un combat"), "the former rule is gone from the Dalamud table");
            Check(!text.Contains("between fights only, off from the start of each fight") && !text.Contains("entre les combats seulement, coupé dès le début de chaque combat"), "the former words on the team panel are gone");
            Check(text.Contains("In a PvE fight Tensho only shows what the player would already know, taken from the game (its texts and sheets")
                && text.Contains("En combat JcE, Tensho montre seulement ce que le joueur saurait déjà, tiré du jeu (ses textes et ses feuilles"), "rule 2, in English and in French");
            Check(text.Contains("no mechanic prediction, no boss timer, no measure of the damage, no action sent in the player's place")
                && text.Contains("ni prédiction de mécanique, ni minuteur de boss, ni mesure des dégâts, ni action envoyée à la place du joueur"), "the four limits, in both languages");
            Check(text.Split('\n').Count(line => line.Contains("DEC-055")) >= 8, "DEC-055 named on the rows it changes, in both halves");
        }),
    ];
}
