using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gear;

namespace TenshoAssistant;

// g, M-12 (0.383): the game's gear sets on the Gear page (GameGearsetReader): one row per set (number, name, job, item level), the owned
// pieces with a higher item level its job can wear (unfolded), and the set as a TENSHO-GEAR1 code. Tensho never changes a set.
internal sealed partial class MainWindow
{
    internal GameGearsetReader? GearsetSource { get; set; }
    private string? gearsetMessage;
    // 0.385 (g, X-08): the set drawn at random, and « only jobs below the top level ».
    private GameGearset? randomGearset;
    private bool randomBelowTop=true;
    private (GearsetState State,object? Owned,IReadOnlyDictionary<int,IReadOnlyList<GearsetUpgrade>> Upgrades)? gearsetCache;

    private void DrawGameGearsets(GearCatalog catalog)
    {
        if(GearsetSource is not { } source)return;
        source.Want();
        var state=source.State;
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("GameGearsets.Title",state?.Sets.Count ?? 0)+"###GameGearsets"))return;
        ImGui.TextDisabled(localization.Get("GameGearsets.Help"));
        if(state is null){ImGui.TextDisabled(localization.Get("GameGearsets.Reading"));return;}
        if(state.Sets.Count==0){ImGui.TextDisabled(localization.Get("GameGearsets.Empty"));return;}
        // Upgrades computed once per read and holdings (not every frame).
        var ownedKey=GearOwnedSource();
        if(gearsetCache is not { } cache || !ReferenceEquals(cache.State,state) || !Equals(cache.Owned,ownedKey))
        {
            var owned=GameGearsets.Owned(ownedKey.Current,ownedKey.State.Entries,ownedKey.CharacterId);
            cache=(state,ownedKey,state.Sets.ToDictionary(set=>set.Number,set=>GameGearsets.Upgrades(set,catalog,owned,state.JobLevels.TryGetValue(set.Job,out var level) ? level:null)));
            gearsetCache=cache;
        }
        if(gearsetMessage is { } message)ImGui.TextDisabled(message);
        // 0.385 (g, X-08): a job at random for the dailies; wearing it is a separate click.
        if(ImGui.SmallButton(localization.Get("GameGearsets.Random")))
            randomGearset=GameGearsets.RandomJob(state.Sets,state.JobLevels,randomBelowTop,Random.Shared.Next);
        Tip("GameGearsets.RandomTip");
        ImGui.SameLine();
        ImGui.Checkbox(localization.Get("GameGearsets.BelowTop")+"##BelowTop",ref randomBelowTop);
        Tip("GameGearsets.BelowTopTip");
        if(randomGearset is { } drawn)
        {
            ImGui.TextColored(Accent,localization.Format("GameGearsets.Drawn",JobName(drawn.Job),drawn.Number,drawn.Name));
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get("GameGearsets.Equip"))){source.Equip(drawn.Number);randomGearset=null;}
            Tip("GameGearsets.EquipTip");
        }
        foreach(var set in state.Sets.OrderBy(set=>set.Job).ThenBy(set=>set.Number))
        {
            ImGui.PushID(set.Number);
            try
            {
                var upgrades=cache.Upgrades.GetValueOrDefault(set.Number) ?? [];
                var label=localization.Format("GameGearsets.Row",set.Number,set.Name.Length>0 ? set.Name:JobName(set.Job),JobName(set.Job),set.ItemLevel)
                    +(upgrades.Count>0 ? " · "+localization.Format("GameGearsets.UpgradeCount",upgrades.Count):"");
                var open=ImGui.TreeNodeEx(label+"###Set",upgrades.Count>0 ? ImGuiTreeNodeFlags.None:ImGuiTreeNodeFlags.Leaf);
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("GameGearsets.Copy")))
                {
                    ImGui.SetClipboardText(GameGearsets.ShareCode(set));
                    gearsetMessage=localization.Format("GameGearsets.Copied",set.Number);
                }
                Tip("GameGearsets.CopyTip");
                if(!open)continue;
                try
                {
                    if(upgrades.Count==0)ImGui.TextDisabled(localization.Get("GameGearsets.NoUpgrade"));
                    foreach(var upgrade in upgrades)
                    {
                        var gain=upgrade.Better.ItemLevel-(upgrade.Current?.ItemLevel ?? 0);
                        ImGui.TextDisabled(localization.Get("Gear.Slot"+upgrade.Slot));
                        ImGui.SameLine();
                        ImGui.TextUnformatted(localization.Format("GameGearsets.Upgrade",upgrade.Current?.Name ?? localization.Get("GameGearsets.EmptySlot"),
                            upgrade.Better.Name+(upgrade.Hq ? " HQ":""),gain));
                    }
                }
                finally{ImGui.TreePop();}
            }
            finally{ImGui.PopID();}
        }
    }
}
