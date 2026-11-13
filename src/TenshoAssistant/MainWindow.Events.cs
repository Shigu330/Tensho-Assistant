using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.Events;
using TenshoAssistant.Npcs;
using TenshoAssistant.Quests;

namespace TenshoAssistant;

// R-09 (0.342, DEC-023 point 7, user request: « follow the seasonal events »): the festivals running now as the game says (Events/
// FestivalReader), each with its quests from the game data, their progress (quest journal, QuestService) and their rewards; a quest
// opens in the Quests page. Names: on a French client, from the festival's guide NPC (« démarcheur de la Veillée des saints »), else
// English (LuminaSupplemental); game flags without quest or NPC are left out. Event shops: the festival's NPCs found in the vendor
// directory (user request). No upcoming dates (no reliable source, docs/evidence/0.342-festivals.md).
internal sealed partial class MainWindow
{
    internal Func<FestivalReader?>? Festivals { get; set; }
    private const string PartakeAddress="https://www.partake.gg/";
    internal Func<uint,string?>? TerritoryName { get; set; }
    // Event shops per festival, computed once per vendor directory and layouts (not every frame: one festival places up to ~900 NPCs).
    private (NpcVendorCatalog Catalog,FestivalLayouts Layouts)? eventShopKey;
    private readonly Dictionary<uint,(NpcVendor Vendor,string Places,NpcShopSale[] Sales)[]> eventShops=[];
    // Quests by id, rebuilt only when the quest journal copy changes (the Quests page keeps its own index, refreshed only while shown).

    private void DrawEventsPage()
    {
        Heading(localization.Get("Events.Title"),localization.Get("Events.Description"));
        // X-04 (0.447, decided 2026-11-03): player events stay on partake.gg, opened on click only; Tensho reads nothing from the site.
        if(ImGui.SmallButton(localization.Get("Events.Partake")))Dalamud.Utility.Util.OpenLink(PartakeAddress);
        Tip("Events.PartakeTip");
        if(characterStats.State.CharacterId==0){ImGui.TextWrapped(localization.Get("CharacterUi.Offline"));return;}
        var reader=Festivals?.Invoke();
        reader?.WantLayouts();
        var state=reader?.State;
        if(reader?.Unavailable==true){ImGui.TextDisabled(localization.Get("Events.Unavailable"));return;}
        if(state is null){ImGui.TextDisabled(localization.Get("Events.Loading"));return;}
        ImGui.TextDisabled(localization.Format("Events.ReadAt",state.ReadAt.ToLocalTime().ToString("T")));
        var questsOf=reader!.Quests;var layouts=reader.Layouts;
        // Game flags (« Special Event Flag », or no quest and no NPC of their own) are not events: left out, counted in developer mode.
        var shown=state.Active.Where(festival=>!Events.Festivals.Technical(festival.Id,reader.Names,questsOf,layouts)).ToArray();
        if(developerMode && shown.Length<state.Active.Count)ImGui.TextDisabled(localization.Format("Events.Technical",state.Active.Count-shown.Length));
        if(shown.Length==0){EmptyState("Events.None");return;}
        if(layouts is null)ImGui.TextDisabled(localization.Get(reader.LayoutsFailed ? "Events.LayoutsUnavailable":"Events.LayoutsLoading"));
        foreach(var festival in shown)
        {
            ImGui.PushID((int)festival.Id);
            try
            {
                var guide=layouts?.Guides.GetValueOrDefault(festival.Id);
                var english=reader.Names.GetValueOrDefault(festival.Id);
                var french=reader.FrenchClient && guide is not null ? Events.Festivals.FrenchNameFromGuide(guide):null;
                var name=french ?? english ?? localization.Format("Events.Unnamed",festival.Id);
                if(!TenshoAssistant.UI.UiComponents.Header(name+$"###Festival{festival.Id}",ImGuiTreeNodeFlags.DefaultOpen))continue;
                // Where the name comes from, said each time (the game itself has no festival name).
                ImGui.TextDisabled(french is not null ? localization.Format("Events.NameFromGuide",guide!,english ?? "—")
                    :english is not null ? localization.Get("Events.NameEnglishOnly"):localization.Get("Events.NameUnknown"));
                if(developerMode)ImGui.TextDisabled(localization.Format("Events.Raw",festival.Id,festival.Phase));
                // X-75.5 (0.879): the days it was seen running in game (yours or shared, this year and before), from the discoveries.
                if(DiscoveryStore is { } seenStore && DiscoveriesLoaded && seenStore.Find(Discoveries.DiscoveryKind.FestivalSeen,festival.Id,16) is { Count:>0 } seenDays)
                    ImGui.TextDisabled(localization.Format("Events.SeenDays",new DateTime(2013,8,27).AddDays(seenDays.Min(fact=>fact.LevelMin)).ToString("d"),new DateTime(2013,8,27).AddDays(seenDays.Max(fact=>fact.LevelMax)).ToString("d")));
                if(questsOf is null)ImGui.TextDisabled(localization.Get(reader.QuestsFailed ? "Events.QuestsUnavailable":"Events.Loading"));
                else if(questsOf.GetValueOrDefault(festival.Id) is { Count:>0 } ids)
                {
                    ImGui.TextColored(Accent,localization.Get("Events.Quests"));
                    DrawFestivalQuests(ids);
                }
                else ImGui.TextDisabled(localization.Get("Events.NoQuest"));
                if(layouts is not null)DrawFestivalShops(festival.Id,layouts);
            }
            finally{ImGui.PopID();}
        }
        ImGui.TextDisabled(localization.Get("Events.Sources"));
    }

    // Event shops (user request): the festival's NPCs that the vendor directory knows as vendors, each with where it stands and what it
    // sells (item, cost). The directory is built in the background (NPC vendors page); until then, say so.
    private void DrawFestivalShops(uint festival,FestivalLayouts layouts)
    {
        if(npcVendors?.State.Catalog is not { } catalog){ImGui.TextDisabled(localization.Get("Events.ShopsLoading"));return;}
        if(eventShopKey is not { } known || known.Catalog!=catalog || known.Layouts!=layouts){eventShops.Clear();eventShopKey=(catalog,layouts);}
        if(!eventShops.TryGetValue(festival,out var vendors))
        {
            var byNpc=catalog.Vendors.GroupBy(vendor=>vendor.NpcId).ToDictionary(group=>group.Key,group=>group.First());
            var byShop=catalog.Sales.ToLookup(sale=>sale.ShopId);
            eventShops[festival]=vendors=(layouts.Npcs.GetValueOrDefault(festival) ?? []).Where(entry=>byNpc.ContainsKey(entry.Npc)).GroupBy(entry=>entry.Npc)
                .Select(group=>(byNpc[group.Key],
                    string.Join(", ",group.Select(entry=>TerritoryName?.Invoke(entry.Territory)).Where(place=>!string.IsNullOrEmpty(place)).Distinct().Take(4)),
                    byNpc[group.Key].ShopIds.SelectMany(shop=>byShop[shop]).ToArray()))
                .Where(entry=>entry.Item3.Length>0).ToArray();
        }
        if(vendors.Length==0){ImGui.TextDisabled(localization.Get("Events.NoShop"));return;}
        ImGui.TextColored(Accent,localization.Get("Events.Shops"));
        var items=itemCatalog.State.Catalog;var size=ImGui.GetTextLineHeight();
        foreach(var (vendor,places,sales) in vendors)
        {
            var label=localization.Format("Events.Vendor",vendor.Name.Length>0 ? vendor.Name:$"#{vendor.NpcId}",places.Length>0 ? places:"—",sales.Length);
            if(!ImGui.TreeNode(label+$"###EventVendor{vendor.NpcId}"))continue;
            try
            {
                foreach(var sale in sales.Take(60))
                {
                    var definition=items?.Find(sale.ItemId);
                    DrawItemIcon(definition,new Vector2(size));ImGui.SameLine();
                    ImGui.TextUnformatted(definition?.Name ?? $"#{sale.ItemId}");
                    ImGui.SameLine();ImGui.TextDisabled("·");ImGui.SameLine();
                    DrawNpcCosts(sale,size,true);
                }
                if(sales.Length>60)ImGui.TextDisabled(localization.Format("Events.More",sales.Length-60));
            }
            finally{ImGui.TreePop();}
        }
    }

    // One line per quest: progress mark, name, gil and reward items; « Open » shows it in the Quests page (steps, giver, full rewards).
    private void DrawFestivalQuests(IReadOnlyList<uint> ids)
    {
        var items=itemCatalog.State.Catalog;
        // X-01 C5 (0.1237): looked up in the service's index, built off the frame (null for a moment after the journal changes: not read yet).
        var byId=quests.State.ById;
        foreach(var id in ids)
        {
            var entry=byId?.GetValueOrDefault(id);
            ImGui.PushFont(UiBuilder.IconFont);
            try
            {
                var (icon,colour)=entry?.Progress switch
                {
                    QuestProgress.Complete=>(FontAwesomeIcon.Check,Success),
                    QuestProgress.Active=>(FontAwesomeIcon.Play,Accent),
                    _=>(FontAwesomeIcon.Circle,Muted),
                };
                ImGui.TextColored(colour,icon.ToIconString());
            }
            finally{ImGui.PopFont();}
            Tip(entry?.Progress switch{QuestProgress.Complete=>"Events.Done",QuestProgress.Active=>"Events.Active",_=>"Events.NotDone"});
            ImGui.SameLine();
            ImGui.TextUnformatted(entry?.Name ?? $"#{id}");
            if(entry?.Rewards is { } rewards)
            {
                if(rewards.Gil>0){ImGui.SameLine();ImGui.TextDisabled(localization.Format("Events.Gil",rewards.Gil.ToString("N0",Numbers)));}
                foreach(var reward in rewards.Items.Take(4))
                {
                    var definition=items?.Find(reward.ItemId);
                    ImGui.SameLine();DrawItemIcon(definition,new Vector2(ImGui.GetTextLineHeight()));
                    if(ImGui.IsItemHovered())WrappedTooltip(definition?.Name ?? $"#{reward.ItemId}");
                }
            }
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get("Events.Open")+$"##OpenQuest{id}")){SelectQuest(id);page=Page.Quests;}
            Tip("Events.OpenTip");
        }
    }
}
