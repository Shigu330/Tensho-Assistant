using TenshoAssistant.Craft;
using TenshoAssistant.Gear;

// X-778 (the review of 0.1784, I2): the « With meal » column of the gear planner added the meal chosen on values read from the game that
// already held the meal eaten: two meals added up, the percent taken on a value holding one. The meal eaten is now taken out first (in the
// game a new meal replaces it); when it cannot be found back exactly, the column is unknown, never two meals.
internal static class GearEaten1787Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    // The meal eaten: critical hit +10 % (max 90, HQ 12 % max 110), determination +10 % (max 54, HQ max 66), crafting points +15 (HQ +20).
    private static readonly CraftConsumable Eaten = new(44091, "Tarte", false,
        [new CraftBonus(27, true, 10, 90, 12, 110), new CraftBonus(44, true, 10, 54, 10, 66), new CraftBonus(11, false, 15, 0, 20, 0)], 3001);
    // The meal chosen: critical hit +8 % (max 120), determination +8 % (max 70).
    private static readonly CraftConsumable Chosen = new(44092, "Soupe", false,
        [new CraftBonus(27, true, 8, 120, 10, 150), new CraftBonus(44, true, 8, 70, 10, 90)], 3002);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Gear eaten 1787: behaviour: the meal eaten taken out before the meal chosen is added: one meal, its percent on the value without the other", () =>
        {
            // Real values without any meal: critical hit 2000, determination 400, crafting points 500, skill speed 800. The meal eaten (NQ)
            // gives critical hit +90 (10 % of 2000 capped), determination +40, crafting points +15: the game shows 2090, 440, 515.
            var real = new Dictionary<string, int> { ["CriticalHit"] = 2090, ["Determination"] = 440, ["CraftingPoints"] = 515, ["SkillSpeed"] = 800 };
            // The planned gear adds 100 critical hit and 60 determination to what is worn.
            var planned = new Dictionary<string, int> { ["CriticalHit"] = 2190, ["Determination"] = 500, ["CraftingPoints"] = 515, ["SkillSpeed"] = 800 };
            var without = GearMeals.WithoutActive(real, planned, Eaten, false);
            Check(without is { } found && found.Values["CriticalHit"] == 2100 && found.Values["Determination"] == 460 && found.Values["CraftingPoints"] == 500
                  && found.Values["SkillSpeed"] == 800, "the planned values without the meal eaten: " + string.Join(", ", without?.Values ?? []));
            Check(without!.Value.Removed["CriticalHit"] == 90 && without.Value.Removed["Determination"] == 40 && without.Value.Removed["CraftingPoints"] == 15
                  && !without.Value.Removed.ContainsKey("SkillSpeed"), "what was taken out: its bonus on each stat it raises");
            var (withMeal, bonuses) = GearMeals.Apply(without.Value.Values, Chosen, false);
            Check(bonuses["CriticalHit"] == 120 && withMeal["CriticalHit"] == 2220, "critical hit: 8 % of 2100 = 168, capped at 120: 2220 (not 2190 + 120 = 2310)");
            Check(bonuses["Determination"] == 36 && withMeal["Determination"] == 496, "determination: 8 % of 460 = 36: 496 (one meal, not 540)");
            Check(withMeal["CraftingPoints"] == 500, "crafting points: the meal eaten's +15 gone, the meal chosen gives none");
            var doubled = GearMeals.Apply(planned, Chosen, false).Values;
            Check(doubled["CriticalHit"] == 2310 && doubled["Determination"] == 540, "what 0.1784 showed: two meals added up");
            Check(planned["CriticalHit"] == 2190 && real["CriticalHit"] == 2090, "the values read and planned not changed in place");
            // HQ eaten: its own percents and caps found back.
            var hqReal = new Dictionary<string, int> { ["CriticalHit"] = 900 + 108, ["CraftingPoints"] = 520 };
            var hq = GearMeals.WithoutActive(hqReal, hqReal, Eaten, true);
            Check(hq is { } h && h.Values["CriticalHit"] == 900 && h.Values["CraftingPoints"] == 500, "HQ eaten: 12 % of 900 = 108 and +20 taken out");
        }),
        ("Gear eaten 1787: behaviour: a value the meal eaten cannot give is unknown (null), never guessed; a stat not read is left out", () =>
        {
            // 10 % rounded down: 9 gives 9, 10 gives 11: a value read of 10 is not one the meal eaten can give (the stats not yet up to date).
            var odd = new Dictionary<string, int> { ["CriticalHit"] = 10 };
            Check(GearMeals.WithoutActive(odd, odd, Eaten, false) is null, "no value found back: unknown");
            // Determination not read for this job: left out, the others taken out.
            var partial = new Dictionary<string, int> { ["CriticalHit"] = 2090 };
            Check(GearMeals.WithoutActive(partial, partial, Eaten, false) is { } p && p.Values.Count == 1 && p.Values["CriticalHit"] == 2000 && !p.Values.ContainsKey("Determination"),
                "a stat not read: left out, never counted from 0");
            // Exactly at the cap's edge: 900 → +90 (10 %, cap 90), 901 → +90: 990 and 991 both found back.
            Check(GearMeals.WithoutActive(new Dictionary<string, int> { ["CriticalHit"] = 990 }, new Dictionary<string, int> { ["CriticalHit"] = 990 }, Eaten, false)?.Values["CriticalHit"] == 900
                  && GearMeals.WithoutActive(new Dictionary<string, int> { ["CriticalHit"] = 991 }, new Dictionary<string, int> { ["CriticalHit"] = 991 }, Eaten, false)?.Values["CriticalHit"] == 901,
                "at the cap's edge: found back exactly");
        }),
        ("Gear eaten 1787: the planner takes out the meal eaten for the job worn, reads it once a second, says it, and shows « — » when unknown", () =>
        {
            var model = Read("src/TenshoAssistant/MainWindow.GearMeal.cs");
            Check(model.Contains("GearMeals.WithoutActive(state.Values,model.PlannedValues,eaten.Item,eaten.Hq)", StringComparison.Ordinal), "taken out of the values read");
            Check(model.Contains("var fromGame=state.JobId==characterStats.State.JobId;", StringComparison.Ordinal), "only the job worn: another job's values are not read from the game");
            Check(model.Contains("now-gearMealEatenAt>=TimeSpan.FromSeconds(1)", StringComparison.Ordinal) && model.Contains("cached.Key.Eaten==key.Item5", StringComparison.Ordinal),
                "read once a second, in the cache's key");
            var gear = Read("src/TenshoAssistant/MainWindow.Gear.cs");
            Check(gear.Contains("if(model.MealUnknown){ImGui.TextDisabled(\"—\");continue;}", StringComparison.Ordinal), "unknown: « — »");
            Check(gear.Contains("\"Gear.MealEatenOut\"", StringComparison.Ordinal) && gear.Contains("\"Gear.MealEatenUnknown\"", StringComparison.Ordinal), "said in the column's tooltip");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                foreach (var key in new[] { "MealEatenOut", "MealEatenStat", "MealEatenUnknown" })
                    Check(text.Contains($"\"{key}\": ", StringComparison.Ordinal), $"{language}: {key}");
            }
        }),
    ];
}
