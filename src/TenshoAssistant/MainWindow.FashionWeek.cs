using System.Numerics;
using Dalamud.Interface;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Fashion;
using TenshoAssistant.Items;

namespace TenshoAssistant;

/// <summary>
/// Fashion Report of the week (0.213), in the spirit of Avant-Garde (behaviour only, no code): the weekly theme (FashionCheckWeeklyTheme),
/// the hint category of each of the 11 slots read from the game window, and for each slot the gear reported for that category
/// (FFXIVGachaSpreadsheet data), owned pieces first, the others dimmed, by popularity; also as a panel beside the game's window.
/// </summary>
internal sealed partial class MainWindow
{
    private FashionCheckService? fashionCheck;
    private Func<uint,string?>? fashionThemeName;
    internal void ConfigureFashionCheck(FashionCheckService service,Func<uint,string?> themeName){fashionCheck=service;fashionThemeName=themeName;}
    private readonly Dictionary<(uint Hint,EquipmentSlots Slot),IReadOnlyList<FashionCandidate>> fashionSlotCache=[];
    private FashionDataset? fashionSlotCacheData;private ItemCatalog? fashionSlotCacheItems;
    private bool fashionAutoFetched;private int fashionOpenSlot=-1;

    // Hint id of a category name shown by the game (FashionCheckThemeCategory name, same client language).
    private uint FashionHintIdOf(string category)=>category.Length==0 ? 0
        :fashionHints.State.Catalog?.Hints.FirstOrDefault(hint=>string.Equals(hint.Name,category,StringComparison.CurrentCultureIgnoreCase))?.Id ?? 0;

    private IReadOnlyList<FashionCandidate> FashionSlotCandidates(uint hint,EquipmentSlots slot)
    {
        var dataset=fashionData.State.Dataset;var items=itemCatalog.State.Catalog;
        if(dataset is null || items is null || hint==0)return [];
        if(!ReferenceEquals(dataset,fashionSlotCacheData) || !ReferenceEquals(items,fashionSlotCacheItems)){fashionSlotCache.Clear();fashionSlotCacheData=dataset;fashionSlotCacheItems=items;}
        if(fashionSlotCache.TryGetValue((hint,slot),out var cached))return cached;
        var list=dataset.Filter(hint,slot,items,"");
        fashionSlotCache[(hint,slot)]=list;
        return list;
    }

    // Owned: known stock above zero; NotOwned: observed and zero; null: not observed.
    private bool? FashionOwned(uint id){var stock=GetRecipeHoldings(id);return stock.HasObservation ? stock.Total>0:null;}

    private void DrawFashionWeek()
    {
        var now=DateTimeOffset.UtcNow;var (_,next)=FashionWeek.Bounds(now);
        var theme=fashionThemeName?.Invoke(FashionWeek.ThemeRow(now));
        ImGui.TextColored(Accent,localization.Format("FashionWeek.Theme",theme is { Length:>0 } ? theme:localization.Get("FashionWeek.ThemeUnknown")));
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("FashionWeek.ThemeHelp"));
        ImGui.TextDisabled(localization.Format("FashionWeek.Reset",next.ToLocalTime().ToString("g"),Math.Max(0,(int)(next-now).TotalHours)));
        DrawFashionOptions();
        if(fashionCheck?.Last is not { } frame){ImGui.TextWrapped(localization.Get("FashionWeek.OpenInGame"));return;}
        ImGui.TextDisabled(localization.Format("FashionWeek.Read",frame.ObservedAt.ToLocalTime().ToString("g")));
        if(frame.ObservedAt<FashionWeek.Bounds(now).Start)ImGui.TextColored(Accent,localization.Get("FashionWeek.OldRead"));
        DrawFashionSlots(frame,false);
    }

    private void DrawFashionOptions()
    {
        var options=configuration.FashionOptions??=new();
        var owned=options.OwnedFirst;var dim=options.DimUnowned;var popular=options.ByPopularity;var panel=options.ShowPanel;var changed=false;
        changed|=ImGui.Checkbox(localization.Get("FashionWeek.OwnedFirst"),ref owned);Tip("FashionWeek.OwnedFirstTip");ImGui.SameLine();
        changed|=ImGui.Checkbox(localization.Get("FashionWeek.Dim"),ref dim);Tip("FashionWeek.DimTip");ImGui.SameLine();
        changed|=ImGui.Checkbox(localization.Get("FashionWeek.Popular"),ref popular);Tip("FashionWeek.PopularTip");
        changed|=ImGui.Checkbox(localization.Get("FashionWeek.Panel"),ref panel);Tip("FashionWeek.PanelTip");
        if(!changed)return;
        var previous=configuration.FashionOptions;
        configuration.FashionOptions=new(){OwnedFirst=owned,DimUnowned=dim,ByPopularity=popular,ShowPanel=panel};
        try{save();}catch(Exception error){configuration.FashionOptions=previous;log.Warning(error,"Fashion options could not be saved.");}
    }

    // The 11 slots: category of the week, pieces reported for it (owned / total), expandable list.
    private void DrawFashionSlots(FashionCheckFrame frame,bool compact)
    {
        if(fashionData.State.Dataset is null)
        {
            if(!fashionAutoFetched && !fashionData.State.Loading){fashionAutoFetched=true;_=fashionData.RequestAsync();}
            ImGui.TextDisabled(localization.Get(fashionData.State.Loading ? "FashionUi.Loading":"FashionUi.NeedFetch"));
        }
        RefreshRecipeHoldingsCache();
        for(var slot=0;slot<FashionWeek.SlotCount && slot<frame.Categories.Count;slot++)
        {
            var category=frame.Categories[slot];
            var hint=FashionHintIdOf(category);
            var candidates=FashionSlotCandidates(hint,FashionWeek.Slots[slot]);
            var ownedCount=candidates.Count(entry=>FashionOwned(entry.Match.ItemId)==true);
            var label=$"{localization.Get("FashionWeek.Slot"+slot)} · {(category.Length>0 ? category:localization.Get("FashionWeek.NoCategory"))}";
            var counts=category.Length==0 ? "":localization.Format("FashionWeek.Counts",ownedCount,candidates.Count);
            ImGui.PushID(slot);
            try
            {
                // 0.216: one slot open at a time (click its row); slots without a category this week are only listed.
                // 0.217: a slot without category still opens when it can be dyed (weapon to feet): its dyes of the week stay visible.
                if(category.Length==0 && slot>=6){ImGui.TextDisabled(label);continue;}
                var open=fashionOpenSlot==slot;
                if(ImGui.Selectable($"{(open ? "▼":"▶")} {label}  {counts}{(ownedCount>0 ? " ✓":"")}###FashionSlot",open))fashionOpenSlot=open ? -1:slot;
                if(!open)continue;
                var childVisible=ImGui.BeginChild("FashionSlotList",new Vector2(0,compact ? 200:260),true);
                try{if(childVisible){if(category.Length>0)DrawFashionSlotItems(candidates,compact,hint);if(slot<6)DrawFashionDyes(slot);}}
                finally{ImGui.EndChild();}
            }
            finally{ImGui.PopID();}
        }
    }

    private void DrawFashionSlotItems(IReadOnlyList<FashionCandidate> candidates,bool compact,uint hint)
    {
        if(hint==0){ImGui.TextDisabled(localization.Get("FashionWeek.UnknownCategory"));return;}
        if(candidates.Count==0){ImGui.TextDisabled(localization.Get("FashionWeek.NoData"));return;}
        var options=configuration.FashionOptions ?? new();
        IEnumerable<FashionCandidate> ordered=options.ByPopularity ? candidates.OrderByDescending(entry=>entry.Match.Reports):candidates.OrderBy(entry=>entry.Match.ItemId);
        if(options.OwnedFirst)ordered=ordered.OrderByDescending(entry=>FashionOwned(entry.Match.ItemId)==true);
        foreach(var entry in ordered.Take(compact ? 30:80))
        {
            var id=entry.Match.ItemId;var item=entry.Item;var owned=FashionOwned(id);
            var dim=options.DimUnowned && owned==false;
            if(dim)ImGui.PushStyleVar(ImGuiStyleVar.Alpha,0.45f);
            try
            {
                DrawItemIcon(item,new Vector2(compact ? 22:26));var clicked=ImGui.IsItemClicked();ImGui.SameLine();
                var stock=owned==true ? $" · ×{GetRecipeHoldings(id).Total:N0}":"";
                clicked|=ImGui.Selectable($"{item?.Name ?? localization.Format("BankUi.Item",id)}{stock}##FashionWeekItem{hint}/{id}",itemId==id);
                var hovered=ImGui.IsItemHovered();
                // 0.214: the tooltip and the menu are drawn without the dimming (they inherited the row's alpha).
                if(dim){ImGui.PopStyleVar();dim=false;}
                if(hovered && item is not null)DrawItemInfoTooltip(id);
                if(item is not null)ItemLinksMenu($"FashionWeekLinks{hint}/{id}",item);
                // Report count kept short at the end of the row; the full wording is in the tooltip of the count.
                ImGui.SameLine();ImGui.TextDisabled($"×{entry.Match.Reports:N0}");
                if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("FashionWeek.Reports",entry.Match.Reports.ToString("N0")));
                if(clicked && item is not null){SelectItem(id);if(compact){page=Page.Fashion;IsOpen=true;}}
            }
            finally{if(dim)ImGui.PopStyleVar();}
        }
    }

    // Dyes reported for this slot this week (0.215): swatch from the Stain sheet, name, share and reports.
    private static readonly string[] FashionDyeSlots=["Weapon","Head","Body","Hands","Legs","Feet"];
    private Func<uint,(string Name,uint Color)?>? fashionStain;
    internal void ConfigureFashionStains(Func<uint,(string Name,uint Color)?> stains)=>fashionStain=stains;
    private void DrawFashionDyes(int slot)
    {
        var dyes=fashionData.State.Dataset?.DyesFor(FashionWeek.Number(DateTimeOffset.UtcNow),FashionDyeSlots[slot]) ?? [];
        ImGui.TextColored(Accent,localization.Format("FashionWeek.Dyes",dyes.Count));
        if(dyes.Count==0){ImGui.TextDisabled(localization.Get("FashionWeek.NoDyes"));return;}
        foreach(var dye in dyes.Take(10))
        {
            var stain=fashionStain?.Invoke(dye.StainId);
            var rgb=stain?.Color ?? 0;
            var colour=new Vector4(((rgb>>16)&0xFF)/255f,((rgb>>8)&0xFF)/255f,(rgb&0xFF)/255f,1);
            ImGui.ColorButton($"##Dye{slot}/{dye.StainId}",colour,ImGuiColorEditFlags.NoTooltip|ImGuiColorEditFlags.NoDragDrop,new Vector2(ImGui.GetTextLineHeight()));
            ImGui.SameLine();
            ImGui.TextUnformatted(localization.Format("FashionWeek.Dye",stain?.Name is { Length:>0 } name ? name:dye.StainId.ToString(),(dye.Share*100).ToString("0.#"),dye.Reports));
        }
    }

    // Buttons on the slot rows of the game window (0.219): one small Tensho button at the right end of each row with a category or dyes;
    // a click opens that slot in the panel (again: closes it). Each is its own tiny window so the rest of the game window stays clickable.
    private void DrawFashionSlotButtons(FashionCheckFrame frame)
    {
        if(frame.Slots is not { } rects)return;
        for(var slot=0;slot<rects.Count && slot<frame.Categories.Count;slot++)
        {
            var rect=rects[slot];
            if(rect.Width<=0 || rect.Height<=0 || frame.Categories[slot].Length==0 && slot>=6)continue;
            var size=Math.Clamp(rect.Height*0.55f,16,30);
            ImGui.SetNextWindowPos(new Vector2(rect.X+rect.Width-size-4,rect.Y+(rect.Height-size)/2),ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(size+2),ImGuiCond.Always);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding,new Vector2(1));
            try
            {
                if(ImGui.Begin($"###TenshoFashionSlot{slot}",ImGuiWindowFlags.NoDecoration|ImGuiWindowFlags.NoBackground|ImGuiWindowFlags.NoSavedSettings
                    |ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoScrollbar))
                {
                    var open=fashionOpenSlot==slot;
                    if(open)ImGui.PushStyleColor(ImGuiCol.Button,Accent);
                    ImGui.PushFont(Dalamud.Interface.UiBuilder.IconFont);
                    bool clicked;
                    try{clicked=ImGui.Button(Dalamud.Interface.FontAwesomeIcon.Tshirt.ToIconString()+"##Open",new Vector2(size));}
                    finally{ImGui.PopFont();if(open)ImGui.PopStyleColor();}
                    if(clicked)fashionOpenSlot=open ? -1:slot;
                    if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("FashionWeek.SlotButton",localization.Get("FashionWeek.Slot"+slot),
                        frame.Categories[slot].Length>0 ? frame.Categories[slot]:localization.Get("FashionWeek.NoCategory")));
                }
            }
            finally{ImGui.End();ImGui.PopStyleVar();}
        }
    }

    // Panel beside the game's Fashion Report window (like the Duty Finder panel).
    internal void DrawFashionCheckPanel()
    {
        if(windowDisposed || configuration.FashionOptions is { ShowPanel:false } || fashionCheck?.Live is not { } frame)return;
        using var theme=UI.FfxivTheme.Apply();
        DrawFashionSlotButtons(frame);
        ImGui.SetNextWindowPos(new Vector2(frame.X+frame.Width+6,frame.Y),ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(380,Math.Max(360,frame.Height)),ImGuiCond.Always);
        var visible=ImGui.Begin(localization.Get("FashionWeek.PanelTitle")+"###TenshoFashionCheck",ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoMove);
        try
        {
            if(!visible)return;
            var theme_=fashionThemeName?.Invoke(FashionWeek.ThemeRow(DateTimeOffset.UtcNow));
            if(theme_ is { Length:>0 })ImGui.TextColored(Accent,theme_);
            DrawFashionSlots(frame,true);
            ImGui.Separator();
            ImGui.TextDisabled(localization.Get("FashionWeek.PanelSource"));
        }
        finally{ImGui.End();}
    }
}
