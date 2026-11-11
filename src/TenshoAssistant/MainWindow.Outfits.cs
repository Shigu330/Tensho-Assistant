using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;
using TenshoAssistant.Inventory;

namespace TenshoAssistant;

// g, C-05 (0.362): the « Outfits » tab of Collections — every glamour dresser outfit (MirageStoreSetItem) with its pieces owned and
// in the dresser, stored or ready to store, filters and search; a row opens its pieces (owned where, sources on right click). Owned =
// the logged-in character's live bags and saved storages (Bank). Rows rebuilt when a source or a filter changes.
internal sealed partial class MainWindow
{
    internal Func<IReadOnlyList<OutfitDefinition>>? OutfitDefinitions { get; set; }
    private bool collectionOutfitTab;
    private int outfitFilter;
    private string outfitQuery="";
    private (object? Live,object? Bank,ulong Character,object? Items,int Filter,string Query)? outfitKey;
    private IReadOnlyList<OutfitProgress> outfitRows=[];
    private IReadOnlySet<uint> outfitOwned=new HashSet<uint>(),outfitDresser=new HashSet<uint>();
    private (int Stored,int Ready,int Total) outfitCount;
    private bool outfitDresserKnown;

    // X-104 (0.989, user choice DEC-045, from C-05): the pieces not owned of an outfit, in one click into the wishlist (any piece) or the chosen
    // crafting list (pieces with a recipe).
    private void DrawOutfitMissingActions(OutfitProgress row)
    {
        if(row.Stored)return;
        var missing=row.Outfit.Pieces.Where(piece=>!outfitOwned.Contains(piece) && !outfitDresser.Contains(piece)).ToArray();
        if(missing.Length==0)return;
        if(ImGui.SmallButton(localization.Format("Outfits.ToWishlist",missing.Length)+$"##OutfitWish{row.Outfit.SetItemId}"))
        {
            if(Items.Wishlist.AddAll(configuration.Wishlist,missing) is { } next)
            {
                var added=next.Count-(configuration.Wishlist?.Count ?? 0);
                SaveSetting("Wishlist.Title",()=>configuration.Wishlist,value=>configuration.Wishlist=value,next);
                if(added>0)SayWishAdded(localization.Format("Wishlist.AddedMany",added));
            }
            else saveError=localization.Format("Wishlist.Full",Items.Wishlist.MaximumItems);
        }
        Tip("Outfits.ToWishlistTip");
        var craftable=missing.Where(piece=>recipeCatalog.State.Catalog?.ForResult(piece) is { Count:>0 }).ToArray();
        if(craftable.Length==0 || CraftListStore is not { Unavailable:false } store || store.Lists is not { Count:>0 } lists)return;
        ImGui.SameLine();
        var index=Math.Clamp(craftListIndex,0,lists.Count-1);
        if(ImGui.SmallButton(localization.Format("Outfits.ToCraftList",craftable.Length,lists[index].Name)+$"##OutfitCraft{row.Outfit.SetItemId}"))
        {
            var current=lists[index];
            foreach(var piece in craftable)if(Craft.CraftLists.Add(current,piece,1) is { } next)current=next;
            if(!store.Set(index,current))log.Warning("The outfit's pieces could not be added to the crafting list {0}.",lists[index].Name);
        }
        Tip("Outfits.ToCraftListTip");
    }

    private void DrawOutfits()
    {
        ImGui.TextWrapped(localization.Get("Outfits.Help"));
        var character=bankBridge.State.CharacterId;
        if(character==0){ImGui.TextDisabled(localization.Get("BankUi.LoginBank"));return;}
        var definitions=OutfitDefinitions?.Invoke() ?? [];
        ImGui.SetNextItemWidth(260);
        ImGui.InputTextWithHint("##OutfitSearch",localization.Get("Outfits.Search"),ref outfitQuery,64);
        Tip("Outfits.SearchTip");
        var filters=new[]{"All","Missing","Ready","Stored"};
        for(var index=0;index<filters.Length;index++)
        {
            ImGui.SameLine();
            if(ImGui.RadioButton(localization.Get("Outfits.Filter"+filters[index])+"##OutfitFilter"+index,outfitFilter==index))outfitFilter=index;
            switch(index)
            {
                case 0: Tip("Outfits.FilterAllTip"); break;
                case 1: Tip("Outfits.FilterMissingTip"); break;
                case 2: Tip("Outfits.FilterReadyTip"); break;
                case 3: Tip("Outfits.FilterStoredTip"); break;
            }
        }
        var items=itemCatalog.State.Catalog;
        var key=((object?)inventory.Current,(object?)bank.State,character,(object?)items,outfitFilter,outfitQuery);
        if(outfitKey!=key)
        {
            outfitKey=key;
            var owned=new HashSet<uint>();var dresser=new HashSet<uint>();
            if(inventory.Current is { } live)
                foreach(var container in live.Containers.Values.Where(container=>container.Location.Owner.Value==character && container.Coverage!=InventoryCoverage.Unknown))
                    foreach(var stack in container.Stacks)owned.Add(stack.Item.Id.Value);
            outfitDresserKnown=false;
            foreach(var entry in bank.State.Entries.Where(entry=>entry.Key.CharacterId==character))
                foreach(var container in entry.Snapshot.Containers.Values.Where(container=>container.Coverage!=InventoryCoverage.Unknown))
                    foreach(var stack in container.Stacks)
                    {
                        owned.Add(stack.Item.Id.Value);
                        if(entry.Key.Kind==BankStorageKind.GlamourDresser && stack.Slot<StorageReport.DresserSlots)dresser.Add(stack.Item.Id.Value);
                    }
            outfitDresserKnown=bank.State.Entries.Any(entry=>entry.Key.CharacterId==character && entry.Key.Kind==BankStorageKind.GlamourDresser);
            outfitOwned=owned;outfitDresser=dresser;
            var all=definitions.Select(outfit=>Outfits.Progress(outfit,owned,dresser)).ToArray();
            outfitCount=(all.Count(row=>row.State==OutfitState.Stored),all.Count(row=>row.State==OutfitState.InDresser),all.Length);
            var query=outfitQuery.Trim();
            outfitRows=all.Where(row=>outfitFilter switch
                {
                    1=>row.State is OutfitState.Missing,
                    2=>row.State is OutfitState.InDresser or OutfitState.Complete,
                    3=>row.State is OutfitState.Stored,
                    _=>true,
                })
                .Where(row=>query.Length==0 || (items?.Find(row.Outfit.SetItemId)?.Name ?? "").Contains(query,StringComparison.CurrentCultureIgnoreCase))
                .OrderByDescending(row=>row.State==OutfitState.InDresser).ThenByDescending(row=>row.Owned*100/Math.Max(1,row.Outfit.Pieces.Count))
                .ThenBy(row=>items?.Find(row.Outfit.SetItemId)?.Name ?? "",StringComparer.CurrentCultureIgnoreCase).ToArray();
        }
        ImGui.TextDisabled(localization.Format("Outfits.Count",outfitCount.Stored,outfitCount.Total,outfitCount.Ready));
        if(!outfitDresserKnown)ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("StorageReport.NoDresser"));
        if(outfitRows.Count==0){ImGui.TextDisabled(localization.Get("Outfits.None"));return;}
        var size=new Vector2(ImGui.GetTextLineHeight()*1.4f);
        var shown=ImGui.BeginChild("OutfitRows",new Vector2(0,ImGui.GetContentRegionAvail().Y),false);
        try
        {
            if(!shown)return;
            foreach(var row in outfitRows.Take(400))
            {
                var set=items?.Find(row.Outfit.SetItemId);
                DrawItemIcon(set,size);ImGui.SameLine();
                var color=row.State switch{OutfitState.Stored=>UI.FfxivTheme.TextMuted,OutfitState.InDresser=>UI.FfxivTheme.Success,_=>UI.FfxivTheme.Text};
                ImGui.PushStyleColor(ImGuiCol.Text,color);
                bool open;
                try{open=ImGui.TreeNode($"{set?.Name ?? $"#{row.Outfit.SetItemId}"}  {row.Owned}/{row.Outfit.Pieces.Count}  · {localization.Get("Outfits.State"+row.State)}##Outfit{row.Outfit.SetItemId}");}
                finally{ImGui.PopStyleColor();}
                // 0.387 (user report): the outfit item's tooltip (how to obtain it), as for a piece.
                if(ImGui.IsItemHovered())DrawItemInfoTooltip(row.Outfit.SetItemId);
                if(!open)continue;
                try
                {
                    foreach(var piece in row.Outfit.Pieces)
                    {
                        var definition=items?.Find(piece);
                        DrawItemIcon(definition,size);ImGui.SameLine();
                        var where=row.Stored || outfitDresser.Contains(piece) ? localization.Get("Outfits.InDresser")
                            :outfitOwned.Contains(piece) ? localization.Get("Outfits.Elsewhere"):localization.Get("Outfits.NotOwned");
                        ImGui.Selectable($"{definition?.Name ?? $"#{piece}"}  · {where}##OutfitPiece{row.Outfit.SetItemId}/{piece}");
                        if(ImGui.IsItemHovered())DrawItemInfoTooltip(piece);
                        ItemLinksMenu($"OutfitPieceMenu{row.Outfit.SetItemId}/{piece}",definition);
                    }
                    DrawOutfitMissingActions(row);
                }
                finally{ImGui.TreePop();}
            }
            if(outfitRows.Count>400)ImGui.TextDisabled(localization.Format("Events.More",outfitRows.Count-400));
        }
        finally{ImGui.EndChild();}
    }
}
