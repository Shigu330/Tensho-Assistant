using TenshoAssistant.Craft;
using TenshoAssistant.Gear;

// X-229 (the user's request of 12/11, K-31, R04 agreement given): a meal in the gear planner: each bonus its percent of the planned value up
// to its cap (HQ values for an HQ meal), or a flat value; an attribute not read is left out (never counted from 0); the synced level unknown.
internal static class GearMeal1784Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    // A combat meal: critical hit +10 % (max 90, HQ 12 % max 110), determination +10 % (max 54, HQ max 66), vitality +8 % (max 100, HQ 120).
    private static readonly CraftConsumable Meal = new(44091, "Tarte", false,
        [new CraftBonus(27, true, 10, 90, 12, 110), new CraftBonus(44, true, 10, 54, 10, 66), new CraftBonus(3, true, 8, 100, 10, 120), new CraftBonus(11, false, 15, 0, 20, 0)], 3001);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Gear meal 1784: behaviour: the cap reached, the percent alone, the HQ values, a flat bonus; an attribute not read left out", () =>
        {
            var planned = new Dictionary<string, int> { ["CriticalHit"] = 2000, ["Determination"] = 400, ["Vitality"] = 3000, ["CraftingPoints"] = 500, ["SkillSpeed"] = 800 };
            var (nq, nqBonus) = GearMeals.Apply(planned, Meal, false);
            Check(nqBonus["CriticalHit"] == 90 && nq["CriticalHit"] == 2090, "critical hit: 10 % of 2000 is 200, capped at 90");
            Check(nqBonus["Determination"] == 40 && nq["Determination"] == 440, "determination: 10 % of 400 = 40, under its cap of 54");
            Check(nqBonus["Vitality"] == 100 && nq["Vitality"] == 3100, "vitality: 8 % of 3000 = 240, capped at 100");
            Check(nqBonus["CraftingPoints"] == 15 && nq["CraftingPoints"] == 515, "a flat bonus: added as it is");
            Check(nq["SkillSpeed"] == 800 && !nqBonus.ContainsKey("SkillSpeed"), "a stat the meal does not touch: unchanged");
            var (hq, hqBonus) = GearMeals.Apply(planned, Meal, true);
            Check(hqBonus["CriticalHit"] == 110 && hqBonus["Determination"] == 40 && hqBonus["Vitality"] == 120 && hqBonus["CraftingPoints"] == 20, "HQ: its own percents and caps");
            Check(hq["CriticalHit"] == 2110, "HQ value");
            var small = GearMeals.Apply(new Dictionary<string, int> { ["CriticalHit"] = 509 }, Meal, false);
            Check(small.Bonuses["CriticalHit"] == 50 && !small.Values.ContainsKey("Determination") && !small.Bonuses.ContainsKey("Determination"), "rounded down; determination not read: left out, never counted from 0");
            Check(GearMeals.Apply(planned, null, false).Bonuses.Count == 0, "no meal: no bonus");
            Check(planned["CriticalHit"] == 2000, "the planned values not changed in place");
        }),
        ("Gear meal 1784: the meal chosen per job, found in the sheets' list; gone from the list: none", () =>
        {
            IReadOnlyList<CraftConsumable> meals = [Meal];
            Check(GearMeals.Chosen(meals, new GearMealChoice { ItemId = 44091, Hq = true }) is { Meal.ItemId: 44091, Hq: true }, "found, HQ kept");
            Check(GearMeals.Chosen(meals, new GearMealChoice { ItemId = 1 }) is null && GearMeals.Chosen(meals, null) is null && GearMeals.Chosen(null, new GearMealChoice { ItemId = 44091 }) is null, "unknown item, no choice, no list: none");
        }),
        ("Gear meal 1784: a fifth column « With meal », its own cache on top of the model; the synced level said unknown; the meals from the sheets; kept per job", () =>
        {
            var gear = Read("src/TenshoAssistant/MainWindow.Gear.cs");
            Check(gear.Contains("model=GearMealModel(model,state);", StringComparison.Ordinal) && gear.Contains("meal is null ? 4:5", StringComparison.Ordinal), "the meal's model and its column");
            Check(Read("src/TenshoAssistant/MainWindow.GearMeal.cs").Contains("ReferenceEquals(cached.Key.Model,model)", StringComparison.Ordinal), "built once per change of the model or the meal");
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("(uint)action.Data[0]!=Craft.CraftConsumables.WellFed", StringComparison.Ordinal), "the meals: Well Fed items with an ItemFood row");
            Check(Read("src/TenshoAssistant/Configuration.cs").Contains("\"GearMeals\",", StringComparison.Ordinal), "kept by « reset all » (the player's data)");
            foreach (var (language, synced) in new[] { ("en", "At a synced level: unknown"), ("fr", "Au niveau synchronisé : inconnu") })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                Check(text.Contains(synced, StringComparison.Ordinal), $"{language}: the synced level said unknown (R06)");
                foreach (var key in new[] { "ColumnMeal", "MealColumnTip", "MealBonus", "MealNone", "MealTip", "MealHqTip" })
                    Check(text.Contains($"\"{key}\": ", StringComparison.Ordinal), $"{language}: {key}");
            }
        }),
    ];
}
