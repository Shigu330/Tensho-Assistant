using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.MiniGames;
using TenshoAssistant.Npcs;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    /// <summary>
    /// X-684 (0.1689, the user's request of 20/11: « every tooltip gives everything Tensho knows »): the one tooltip of an NPC, wherever it is
    /// named (the vendors and Triple Triad players of the Tensho map's lists, the Vendors page, the vendors among an item's sources, the
    /// Triple Triad NPCs page): icon, name, places, shops, what else Tensho knows (NpcTips), then what the caller adds (a click's effect).
    /// </summary>
    private void DrawNpcTooltip(NpcTipFacts facts,string? after=null)
    {
        using(TooltipScope.Begin())
        {
            if(facts.Icon!=0){DrawGameIcon(facts.Icon,new Vector2(32));ImGui.SameLine();}
            TooltipTitle(facts.Name);
            if(TipPart("Npc.Places"))foreach(var place in facts.Places)ImGui.TextDisabled(place);
            if(TipPart("Npc.Shops"))foreach(var shop in facts.Shops)ImGui.TextUnformatted(shop);
            if(TipPart("Npc.Notes"))foreach(var note in facts.Notes)ImGui.TextUnformatted(note);
            if(after is { Length:>0 }){ImGui.Separator();ImGui.TextDisabled(after);}
        }
    }

    // X-684 (0.1689): the facts of the NPC under the cursor, built once while it stays hovered (its key, the language, the catalogues),
    // not every frame.
    private (string Key,int Revision,object? Vendors,object? Triad,NpcTipFacts Facts)? npcTip;
    private NpcTipFacts NpcTipOf(string key,Func<NpcTipFacts> build)
    {
        var vendors=npcVendors?.State.Catalog;var triad=triadNpcs?.State.Catalog;
        if(npcTip is { } cached && cached.Key==key && cached.Revision==localization.Revision && ReferenceEquals(cached.Vendors,vendors) && ReferenceEquals(cached.Triad,triad))
            return cached.Facts;
        var facts=build();
        npcTip=(key,localization.Revision,vendors,triad,facts);
        return facts;
    }

    /// <summary>X-684 (0.1689): a vendor's facts under the cursor (built once while hovered).</summary>
    private NpcTipFacts VendorTipFacts(NpcVendor vendor)=>NpcTipOf("v"+vendor.NpcId,()=>BuildVendorTip(vendor));

    /// <summary>X-684 (0.1689): a vendor's facts: the map's icon of its shops, its name, its places, each shop with its items.</summary>
    private NpcTipFacts BuildVendorTip(NpcVendor vendor)
    {
        var shops=npcVendors?.State.Catalog?.ShopSummaries;
        var places=NpcTips.Lines(VendorLocations(vendor).Select(NpcPlace),NpcTips.MaxPlaces,more=>localization.Format("MapView.HoverMore",more));
        return NpcTips.Build(NpcName(vendor),VendorIconOf(vendor,shops),places,VendorShopLines(vendor,shops),[]);
    }

    /// <summary>X-684 (0.1689): a vendor's shop lines, each shop with its items (the tooltip's and the map label's). X-707 (0.1693, review of
    /// 0.1692): the map's labels (VendorLabel, built once per map for every vendor) take these lines alone; through the whole tooltip, the places
    /// and the icon were formatted for every vendor and thrown away at once.</summary>
    private IReadOnlyList<string> VendorShopLines(NpcVendor vendor,IReadOnlyDictionary<uint,(string Name,NpcShopKind Kind,int Items)>? shops)
    {
        var read=new List<string>();var unknown=0;
        foreach(var id in vendor.ShopIds.Distinct())
        {
            if(shops is null || !shops.TryGetValue(id,out var shop)){unknown++;continue;}
            // Its name, or its kind when the game gives none or only an untranslated Japanese placeholder such as « アイテムの購入 » (0.137).
            var shopName=string.IsNullOrWhiteSpace(shop.Name) || localization.CurrentCode!="ja" && shop.Name.Any(c=>c is >= '぀' and <= 'ヿ' or >= '一' and <= '鿿')
                ? localization.Get("MapLocal.ShopKind"+shop.Kind):shop.Name;
            read.Add(localization.Format("MapLocal.ShopLine",shopName,shop.Items));
        }
        return NpcTips.Shops(vendor.ShopIds.Count,read,unknown,more=>localization.Format("MapView.HoverMore",more),
            count=>localization.Format("MapLocal.ShopsUnknown",count),localization.Get("MapLocal.ShopsNone"));
    }

    /// <summary>X-684 (0.1689): a Triple Triad NPC's facts: its name, its place, fee and cards it may give, the cards still missing, beaten or
    /// not, the quests it asks for (one body for the Triple Triad page and the map's list: the page's lacked « beaten »).</summary>
    private NpcTipFacts TriadTipFacts(TriadNpc npc,bool? beaten,int missing,IReadOnlySet<uint>? questsDone,Func<uint,string> card)
        =>NpcTipOf($"t{npc.MatchId}/{beaten}/{missing}/{questsDone?.Count}",()=>
    {
        // Written as a vendor's place (NpcPlace); the place menu stays the caller's (NpcMenu beside each name).
        var place=npc.MapId!=0 ? NpcPlace(new NpcLocation(0,npc.MapId,npc.Zone,npc.X,npc.Y,0,0,0)):localization.Get("TriadNpcs.NoPlace");
        var notes=new List<string?>
        {
            localization.Format("TriadNpcs.Tooltip",npc.Fee,npc.RewardCards.Count==0 ? "—":string.Join(", ",npc.RewardCards.Select(card))),
            missing>0 ? localization.Format("MapLocal.TriadMissing",missing):null,
            localization.Format("TriadNpcs.BeatenTip",localization.Get(beaten==true ? "TriadNpcs.Yes":beaten==false ? "TriadNpcs.No":"TriadNpcs.Unknown")),
            TriadQuestLines(npc,questsDone).TrimStart('\r','\n'),
        };
        return NpcTips.Build(npc.Name,0,[place],[],notes);
    });
}
