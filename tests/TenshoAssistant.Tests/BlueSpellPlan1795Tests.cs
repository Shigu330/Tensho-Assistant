using TenshoAssistant.Collections;
using TenshoAssistant.Monsters;

// X-742 (0.1795, idea I-035 chosen by the user on 21/11, slices 1 and 2): blue magic spells checked as wanted, copied as a list grouped by the
// place the game gives (AozActionTransient.Location), the list sorted by place, the place linked to one map or one duty only (never guessed).
internal static class BlueSpellPlan1795Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static CollectionEntry Spell(uint id, int number, string name, string place, bool learned = false)
        => new(CollectionKind.BlueMagic, id, name, 0, learned, number, 1, "", place);

    private static readonly CollectionEntry[] Spells =
    [
        Spell(1, 1, "Water Cannon", "Lower La Noscea"),
        Spell(2, 2, "Flame Thrower", "Sastasha"),
        Spell(3, 3, "Aqua Breath", "Lower La Noscea"),
        Spell(4, 4, "Flying Frenzy", "", false),
        Spell(5, 5, "Drill Cannons", "Sastasha", learned: true),
        Spell(6, 6, "High Voltage", "Central Thanalan"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Blue spell plan 1795: behaviour: spells checked and unchecked; the copy groups the wanted ones not learned by place, places in order, no place last", () =>
        {
            var wanted = new List<uint>();
            foreach (var id in new uint[] { 3, 2, 1, 4, 5 }) Check(BlueSpellPlan.Toggle(wanted, id), "checked: " + id);
            Check(!BlueSpellPlan.Toggle(wanted, 1) && !wanted.Contains(1u), "checked again: unchecked");
            BlueSpellPlan.Toggle(wanted, 1);
            var text = BlueSpellPlan.CopyText(Spells.Append(new CollectionEntry(CollectionKind.Mount, 3, "Not a spell", 0, false)), wanted, "No place");
            Check(text == "Lower La Noscea: 1 Water Cannon, 3 Aqua Breath\nSastasha: 2 Flame Thrower\nNo place: 4 Flying Frenzy",
                "by place, numbers in order, the learned one and the unchecked one left out, another kind ignored: " + text);
            Check(BlueSpellPlan.CopyText(Spells, [5], "No place") == "", "only learned ones wanted: nothing to copy");
        }),
        ("Blue spell plan 1795: behaviour: the list by place keeps every spell, places in order then numbers, no place last", () =>
        {
            var sorted = BlueSpellPlan.ByPlace(Spells.Reverse());
            Check(sorted.Length == Spells.Length, "every spell kept");
            Check(string.Join(",", sorted.Select(spell => spell.Number)) == "6,1,3,2,5,4", "Central Thanalan, Lower La Noscea (1, 3), Sastasha (2, 5), then no place: " + string.Join(",", sorted.Select(spell => spell.Number)));
        }),
        ("Blue spell plan 1795: behaviour: a place name is linked to a map only when every territory with it has one same map", () =>
        {
            (uint, uint)[] territories = [(30, 16), (30, 16), (30, 0), (41, 20), (41, 21), (52, 0)];
            Check(BlueSpellPlan.MapOfPlace(territories, 30) == 16, "one map (twice, and a territory without a map): linked");
            Check(BlueSpellPlan.MapOfPlace(territories, 41) == 0, "two maps for one place: not linked, never guessed");
            Check(BlueSpellPlan.MapOfPlace(territories, 52) == 0 && BlueSpellPlan.MapOfPlace(territories, 99) == 0, "no map, or no territory: not linked");
        }),
        ("Blue spell plan 1795: the page: a box per spell, the copy and the order by place above the list, the link in the spell's window; kept as player data", () =>
        {
            var page = Read("src/TenshoAssistant/MainWindow.Collections.cs");
            Check(page.Contains("if(ImGui.Checkbox($\"##BlueWant{entry.Id}\",ref wanted)){Monsters.BlueSpellPlan.Toggle(configuration.BlueSpellsWanted??=[],entry.Id);blueWantedVersion++;save();}\n                    Tip(\"BlueSpell.WantTip\");", StringComparison.Ordinal), "the box, its tooltip");
            Check(page.Contains("if(blueMagic){DrawBlueSpellWanted(state);DrawBlueSpellOrder();DrawBlueSpellNearby();}", StringComparison.Ordinal)
                  && page.Contains("if(collectionViewBlueByPlace)collectionViewEntries=Monsters.BlueSpellPlan.ByPlace(collectionViewEntries);", StringComparison.Ordinal), "the list by place, made again only when the view changes");
            Check(page.Contains("DrawBlueSpellPlaceLink(spell);", StringComparison.Ordinal), "the link in the spell's window");
            var helpers = Read("src/TenshoAssistant/MainWindow.BlueSpellSources.cs");
            Check(helpers.Contains("CopyButton(localization.Get(\"BlueSpell.CopyWanted\"),()=>Monsters.BlueSpellPlan.CopyText(", StringComparison.Ordinal), "copied through the common copy button");
            Check(helpers.Contains("if(spell.PlaceMap!=0)", StringComparison.Ordinal) && helpers.Contains("duty.DutyId==spell.PlaceDuty", StringComparison.Ordinal), "map or duty only when the reader found one");
            var reader = Read("src/TenshoAssistant/Collections/CollectionReader.Dalamud.cs");
            Check(reader.Contains("placeMap = Monsters.BlueSpellPlan.MapOfPlace(bluePlaceMaps, source.RowId);", StringComparison.Ordinal)
                  && reader.Contains("if (location.Length > 0) placeMap = territory.Map.RowId;", StringComparison.Ordinal) && reader.Contains("placeDuty = source.RowId;", StringComparison.Ordinal), "the links from the game's sheets");
            Check(Read("src/TenshoAssistant/Configuration.cs").Contains("\"SRankKills\", \"BlueSpellsWanted\",", StringComparison.Ordinal), "player data: in the player's data reset list");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                foreach (var key in new[] { "WantTip", "Wanted", "CopyWanted", "NoPlace", "ByPlace", "ByPlaceTip", "OpenMap", "OpenMapTip", "OpenDuty", "OpenDutyTip" })
                    Check(text.Contains($"\"{key}\": ", StringComparison.Ordinal), $"{language}: BlueSpell.{key}");
            }
        }),
    ];
}
