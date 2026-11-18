using TenshoAssistant.Beastmaster;

// 0.1530 (X-136): the instinctual combos from the game itself: the fifty Tricks' affinities (ActionTransient 44935 to 45033), the beastmaster's
// four instinctual skills, and the Compass rule; the Cu Sith as the game's tutorial (EventTutorialPage 29 to 37).
internal static class BeastInstinct1530Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast instinct 1530: the fifty beasts have their Trick and its affinity; 15 Rampant, 14 Durant, 13 Eldritch, 8 Volant", () =>
        {
            var all = Enumerable.Range(1, 50).Select(row => BeastInstinct.Of((uint)row)).ToArray();
            Check(all.All(affinity => affinity != Affinity.None), "every beast has one");
            Check(all.Count(a => a == Affinity.Rampant) == 15 && all.Count(a => a == Affinity.Durant) == 14 && all.Count(a => a == Affinity.Eldritch) == 13 && all.Count(a => a == Affinity.Volant) == 8,
                "the counts read in the sheets");
            Check(BeastInstinct.Trick(1) == 44935 && BeastInstinct.Trick(50) == 45033 && BeastInstinct.Trick(0) == 0 && BeastInstinct.Trick(51) == 0, "Tricks 44935 to 45033");
            Check(BeastInstinct.Of(3) == Affinity.Rampant && BeastInstinct.Of(4) == Affinity.Durant && BeastInstinct.Of(6) == Affinity.Eldritch && BeastInstinct.Of(10) == Affinity.Volant,
                "Derrière laineux (sheep) Rampant, Tournevis Durant, Odeur nauséabonde Eldritch, Dard aiguisé Volant");
            Check(BeastInstinct.Of(0) == Affinity.None && BeastInstinct.Of(99) == Affinity.None && BeastInstinct.Combos(99).Count == 0, "unknown: none");
        }),
        ("Beast instinct 1530: the Compass: clockwise only; Volant → Rampant and Durant → Eldritch Sunstrider, the two others Moonstalker", () =>
        {
            Check(BeastInstinct.Kind(Affinity.Volant, Affinity.Rampant) == IntentionalCombo.Sunstrider && BeastInstinct.Kind(Affinity.Durant, Affinity.Eldritch) == IntentionalCombo.Sunstrider, "Sunstrider");
            Check(BeastInstinct.Kind(Affinity.Rampant, Affinity.Durant) == IntentionalCombo.Moonstalker && BeastInstinct.Kind(Affinity.Eldritch, Affinity.Volant) == IntentionalCombo.Moonstalker, "Moonstalker");
            Check(BeastInstinct.Kind(Affinity.Rampant, Affinity.Volant) == IntentionalCombo.None && BeastInstinct.Kind(Affinity.Volant, Affinity.Durant) == IntentionalCombo.None
                  && BeastInstinct.Kind(Affinity.Rampant, Affinity.Rampant) == IntentionalCombo.None && BeastInstinct.Kind(Affinity.None, Affinity.Volant) == IntentionalCombo.None, "anticlockwise, across or the same: none");
        }),
        ("Beast instinct 1530: the Cu Sith (Rampant) as the game's tutorial: Gale Axe (Volant) first, Sunstrider; Mistral Axe (Durant) after, Moonstalker", () =>
        {
            var combos = BeastInstinct.Combos(1);
            Check(BeastInstinct.Of(1) == Affinity.Rampant, "the Cu Sith's Trick is Rampant (EventTutorialPage 36)");
            Check(combos.Count == 2 && combos[0] is { Skill: 44889, SkillAffinity: Affinity.Volant, AfterTrick: false, Kind: IntentionalCombo.Sunstrider }, "Gale Axe (Volant, 44889) before the Trick: Sunstrider (EventTutorialPage 37)");
            Check(combos[1] is { Skill: 44887, SkillAffinity: Affinity.Durant, AfterTrick: true, Kind: IntentionalCombo.Moonstalker }, "Mistral Axe (Durant, 44887) after the Trick: Moonstalker (EventTutorialPage 34, 36)");
            Check(BeastInstinct.TrainerSkill.Count == 4 && BeastInstinct.TrainerSkill[Affinity.Rampant] == 44884 && BeastInstinct.TrainerSkill[Affinity.Eldritch] == 44888, "the four skills");
            // Every beast: one combo before, one after, each clockwise.
            foreach (var row in Enumerable.Range(1, 50).Select(row => (uint)row))
            {
                var both = BeastInstinct.Combos(row);
                Check(both.Count == 2 && both.All(combo => combo.Kind != IntentionalCombo.None) && both[0].Kind != both[1].Kind, $"beast {row}: one Sunstrider, one Moonstalker");
            }
        }),
        ("Beast instinct 1530: the panel says each beast's affinity and combos; the developer probe reads the beast called", () =>
        {
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("DrawBeastCombo(id);") && page.Contains("BeastInstinct.Combos(beast)"), "each beast of the fight shown");
            var probe = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Beastmaster/BeastWindowProbe.Dalamud.cs"));
            Check(probe.Contains("\"XBMActivePet\""), "the window of the beast called, in the developer probe");
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("mainWindow.BeastActionName="), "the actions' names from the game");
        }),
    ];
}
