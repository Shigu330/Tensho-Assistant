using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private void DrawCharacterStats()
    {
        Heading(localization.Get("CharacterUi.Title"), localization.Get("CharacterUi.Description"));
        var state = characterStats.State;
        if (state.CharacterId == 0) { ImGui.TextWrapped(localization.Get("CharacterUi.Offline")); return; }
        ImGui.TextColored(Accent,localization.Format("CharacterDates.Summary",state.Name,state.Job,state.Level));
        ImGui.TextDisabled(localization.Format("CharacterDates.Observed",state.ObservedAt?.ToLocalTime().ToString("HH:mm:ss") ?? "—"));
        var groups = new[]
        {
            (localization.Get("CharacterUi.Attributes"), new[] { ("Strength", localization.Get("CharacterUi.Strength")), ("Dexterity", localization.Get("CharacterUi.Dexterity")), ("Vitality", localization.Get("CharacterUi.Vitality")), ("Intelligence", localization.Get("CharacterUi.Intelligence")), ("Mind", localization.Get("CharacterUi.Mind")), ("HealthPoints", localization.Get("CharacterUi.Health")), ("MagicPoints", localization.Get("CharacterUi.Magic")) }),
            (localization.Get("CharacterUi.Combat"), new[] { ("CriticalHit", localization.Get("CharacterUi.Critical")), ("DirectHitRate", localization.Get("CharacterUi.Direct")), ("Determination", localization.Get("CharacterUi.Determination")), ("Tenacity", localization.Get("CharacterUi.Tenacity")), ("Piety", localization.Get("CharacterUi.Piety")), ("SkillSpeed", localization.Get("CharacterUi.Skill")), ("SpellSpeed", localization.Get("CharacterUi.Spell")), ("Defense", localization.Get("CharacterUi.Defense")), ("MagicDefense", localization.Get("CharacterUi.MagicDefense")) }),
            (localization.Get("CharacterUi.Crafting"), new[] { ("Craftsmanship", localization.Get("CharacterUi.Craftsmanship")), ("Control", localization.Get("CharacterUi.Control")), ("CraftingPoints", localization.Get("CharacterUi.Cp")) }),
            (localization.Get("CharacterUi.GatheringGroup"), new[] { ("Gathering", localization.Get("CharacterUi.Gathering")), ("Perception", localization.Get("CharacterUi.Perception")), ("GatheringPoints", localization.Get("CharacterUi.Gp")) }),
        };
        foreach (var group in groups)
        {
            if (!ImGui.CollapsingHeader(group.Item1, ImGuiTreeNodeFlags.DefaultOpen)) continue;
            if (!ImGui.BeginTable($"Character{group.Item1}", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) continue;
            try
            {
                foreach (var (key, label) in group.Item2)
                {
                    ImGui.TableNextRow(); ImGui.TableNextColumn(); ImGui.TextUnformatted(label);
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(state.Values.TryGetValue(key, out var value) ? value.ToString("N0") : localization.Get("CharacterUi.Unknown"));
                }
            }
            finally { ImGui.EndTable(); }
        }
        ImGui.TextWrapped(localization.Get("CharacterUi.Help"));
    }
}
