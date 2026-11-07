using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.FreeCompany;
using TenshoAssistant.Npcs;

namespace TenshoAssistant;

// W-05 (0.691, user request): just the list, on screen: the materials of the workshop project and of the crafting preparation, each with its setting on its page. 0.693 (user request): shown means shown, unless the player chooses otherwise.
internal sealed partial class MainWindow
{
    private const ImGuiWindowFlags ListPanelFlags=ImGuiWindowFlags.AlwaysAutoResize|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoNav;
    private bool ListPanelsHiddenInCombat=>configuration.ListPanelsHideInCombat && mapLive?.State is { InCombat:true };
    // The same two choices for every list panel, under the box of the one that is on.
    private void DrawListPanelOptions()
    {
        BoolSetting("ListPanels.HideInCombat","ListPanels.HideInCombatTip",()=>configuration.ListPanelsHideInCombat,value=>configuration.ListPanelsHideInCombat=value,c=>c.ListPanelsHideInCombat);
        BoolSetting("ListPanels.HideBehindGame","ListPanels.HideBehindGameTip",()=>configuration.ListPanelsHideBehindGame,value=>configuration.ListPanelsHideBehindGame=value,c=>c.ListPanelsHideBehindGame);
        BoolSetting("ListPanels.Base","ListPanels.BaseTip",()=>configuration.ListPanelsBase,value=>configuration.ListPanelsBase=value,c=>c.ListPanelsBase);
        if(!configuration.ListPanelsBase)return;
        ImGui.Indent();
        BoolSetting("ListPanels.Gathering","ListPanels.OriginTip",()=>configuration.ListPanelsGathering,value=>configuration.ListPanelsGathering=value,c=>c.ListPanelsGathering);
        if(configuration.ListPanelsGathering)
        {
            ImGui.Indent();
            BoolSetting("ListPanels.ByZone","ListPanels.ByZoneTip",()=>configuration.ListPanelsByZone,value=>configuration.ListPanelsByZone=value,c=>c.ListPanelsByZone);
            ImGui.Unindent();
        }
        BoolSetting("ListPanels.Vendor","ListPanels.VendorTip",()=>configuration.ListPanelsVendor,value=>configuration.ListPanelsVendor=value,c=>c.ListPanelsVendor);
        if(configuration.ListPanelsVendor)
        {
            DrawCurrencyTicks("ListPanel",()=>configuration.ListPanelsCurrenciesOn,value=>configuration.ListPanelsCurrenciesOn=value);
            BoolSetting("ListPanels.MetOnly","ListPanels.MetOnlyTip",()=>configuration.ListPanelsMetOnly,value=>configuration.ListPanelsMetOnly=value,c=>c.ListPanelsMetOnly);
        }
        BoolSetting("ListPanels.Monster","ListPanels.OriginTip",()=>configuration.ListPanelsMonster,value=>configuration.ListPanelsMonster=value,c=>c.ListPanelsMonster);
        BoolSetting("ListPanels.Other","ListPanels.OriginTip",()=>configuration.ListPanelsOther,value=>configuration.ListPanelsOther=value,c=>c.ListPanelsOther);
        BoolSetting("ListPanels.Crystal","ListPanels.OriginTip",()=>configuration.ListPanelsCrystal,value=>configuration.ListPanelsCrystal=value,c=>c.ListPanelsCrystal);
        ImGui.Unindent();
    }
    private IReadOnlyList<WorkshopProject> workshopPanelShown=[];
    private RecipeDefinition? preparationPanelShown;

    private PanelDefinition WorkshopListPanel=>new("WorkshopPanel","###TenshoWorkshopList",new Vector2(40,420),ListPanelFlags,
        ()=>configuration.WorkshopPanel,WorkshopPanelVisible,DrawWorkshopPanelContents,()=>{if(configuration.WorkshopPanel){configuration.WorkshopPanel=false;SaveReminders();}},()=>localization.Get("FreeCompany.WorkshopPanelTitle"),0.75f,()=>configuration.ListPanelsHideBehindGame);
    private PanelDefinition PreparationListPanel=>new("PreparationPanel","###TenshoPreparationList",new Vector2(340,420),ListPanelFlags,
        ()=>configuration.PreparationPanel,PreparationPanelVisible,DrawPreparationPanelContents,()=>{if(configuration.PreparationPanel){configuration.PreparationPanel=false;SaveReminders();}},()=>localization.Get("PlanUi.PanelTitle"),0.75f,()=>configuration.ListPanelsHideBehindGame);

    private CraftList? craftListPanelShown;
    private PanelDefinition CraftListListPanel=>new("CraftListPanel","###TenshoCraftListList",new Vector2(640,420),ListPanelFlags,
        ()=>configuration.CraftListPanel,CraftListPanelVisible,DrawCraftListPanelContents,()=>{if(configuration.CraftListPanel){configuration.CraftListPanel=false;SaveReminders();}},()=>localization.Get("CraftLists.PanelTitle"),0.75f,()=>configuration.ListPanelsHideBehindGame);

    private bool CraftListPanelVisible()
    {
        if(windowDisposed || !configuration.CraftListPanel || ListPanelsHiddenInCombat || itemCatalog.State.Catalog is null || CraftListStore is not { Unavailable:false } store)return false;
        craftListPanelShown=store.Lists.FirstOrDefault(list=>list.Name==configuration.CraftListPanelName);
        return craftListPanelShown is { Entries.Count:>0 };
    }

    private void DrawCraftListPanelContents()
    {
        if(craftListPanelShown is not { } list)return;
        RefreshRecipeHoldingsCache();
        ImGui.TextColored(Accent,list.Name);
        DrawListPanelEntries("CraftList",()=>CraftLists.Ingredients(CraftListPlans(list)).Select(row=>(row.Item1,(long)row.Item2)).ToArray());
    }

    private bool WorkshopPanelVisible()
    {
        if(windowDisposed || !configuration.WorkshopPanel || ListPanelsHiddenInCombat || itemCatalog.State.Catalog is null || WorkshopProjectsLoad is not { } load)return false;
        workshopProjects??=Task.Run(load);
        if(!workshopProjects.IsCompletedSuccessfully){workshopPanelShown=[];return false;}
        // The followed projects together, or else the one chosen on the page.
        var projects=workshopProjects.Result;
        var followed=(configuration.WorkshopFollowed ?? []).Select(id=>projects.FirstOrDefault(project=>project.Id==id)).OfType<WorkshopProject>().ToArray();
        workshopPanelShown=followed.Length>0 ? followed:projects.Where(project=>project.Id==configuration.WorkshopPanelProject).ToArray();
        return workshopPanelShown.Count>0;
    }

    private void DrawWorkshopPanelContents()
    {
        RefreshRecipeHoldingsCache();
        foreach(var project in workshopPanelShown)ImGui.TextColored(Accent,WorkshopName(project));
        DrawListPanelEntries("Workshop",()=>WorkshopStock.Totals(workshopPanelShown).Select(row=>(row.Item,(long)row.Total)).ToArray());
    }

    private bool PreparationPanelVisible()
    {
        if(windowDisposed || !configuration.PreparationPanel || ListPanelsHiddenInCombat || configuration.PreparationPanelTarget<=0 || itemCatalog.State.Catalog is null)return false;
        preparationPanelShown=recipeCatalog.State.Catalog?.ForResult(configuration.PreparationPanelItem).FirstOrDefault(recipe=>recipe.RecipeId==configuration.PreparationPanelRecipe);
        return preparationPanelShown is not null;
    }

    private void DrawPreparationPanelContents()
    {
        if(preparationPanelShown is not { } recipe)return;
        RefreshRecipeHoldingsCache();
        var name=itemCatalog.State.Catalog?.Find(recipe.ResultItemId)?.Name ?? recipe.ResultItemId.ToString();
        ImGui.TextColored(Accent,$"{name} ×{configuration.PreparationPanelTarget:N0}");
        DrawListPanelEntries("Preparation",()=>CraftPlan.Build(recipe,configuration.PreparationPanelTarget,GetRecipeHoldings).Materials.Select(material=>(material.ItemId,material.Required)).ToArray());
    }

    // 0.701 (user request): the currencies the vendors take, gil first, none ticked at the start, with a search by name. Listed once per catalogue.
    // Left out: 0, a currency the game encodes and nothing resolves, and rows with no name in the game's sheet (8223): offers in them never count.
    // One still ticked stays listed, so that it can be unticked.
    private (object? Vendors,object? Recipes,Task<(uint Id,string Name)[]>? Rows) listPanelCurrencies;
    private HashSet<uint> listPanelCurrenciesOn=[];
    private string listPanelCurrencySearch="";
    private void DrawCurrencyTicks(string menu,Func<List<uint>?> read,Action<List<uint>> write)
    {
        var vendors=npcVendors?.State.Catalog;var recipes=recipeCatalog.State.Catalog;
        if(vendors is null || recipes is null)return;
        if(!ReferenceEquals(listPanelCurrencies.Vendors,vendors) || !ReferenceEquals(listPanelCurrencies.Recipes,recipes) || listPanelCurrencies.Rows is null)
        {
            var items=itemCatalog.State.Catalog;var load=WorkshopProjectsLoad;
            var credits=localization.Get("NpcUi.CompanyCredits");
            credits=credits.Length>0 ? char.ToUpper(credits[0],System.Globalization.CultureInfo.CurrentCulture)+credits[1..]:credits;
            // 0.702 (user request): only the vendors that really have materials. Every sale against every recipe: off the frame, once per catalogue.
            listPanelCurrencies=(vendors,recipes,Task.Run(()=>
            {
                var materials=recipes.All.SelectMany(recipe=>recipe.Ingredients).Select(ingredient=>ingredient.ItemId).ToHashSet();
                try{if(load is not null)materials.UnionWith(load().SelectMany(project=>project.Totals()).Select(row=>row.Item));}
                catch(Exception error){log.Warning(error,"Workshop supplies could not be added to the vendor currencies.");}
                var found=BaseMaterials.MaterialCurrencies(vendors.Sales.Select(sale=>(sale.ItemId,sale.Costs.Select(cost=>cost.CurrencyItemId))),materials);
                return found.Select(id=>(Id:id,Name:id==NpcCost.CompanyCredits ? credits:items?.Find(id)?.Name ?? "")).Where(row=>row.Name.Length>0)
                    .OrderBy(row=>row.Id==1 ? 0:1).ThenBy(row=>row.Name,StringComparer.CurrentCulture).ToArray();
            }));
        }
        if(listPanelCurrencies.Rows is not { IsCompletedSuccessfully:true } ready)
        {
            if(listPanelCurrencies.Rows is { IsFaulted:true } failed){log.Warning(failed.Exception,"The vendor currencies could not be listed.");listPanelCurrencies=(vendors,recipes,Task.FromResult<(uint,string)[]>([]));}
            ImGui.TextDisabled(localization.Get("ListPanels.CurrenciesLoading"));
            return;
        }
        var on=read() ?? [];
        ImGui.SetNextItemWidth(280);
        if(ImGui.BeginCombo($"###{menu}Currencies",localization.Format("ListPanels.Currencies",on.Count)))
        {
            try
            {
                ImGui.SetNextItemWidth(-1);
                ImGui.InputTextWithHint("###ListPanelCurrencySearch",localization.Get("ListPanels.CurrencySearch"),ref listPanelCurrencySearch,60);
                Tip("ListPanels.CurrencySearchTip");
                var shown=ready.Result.Concat(on.Where(id=>ready.Result.All(row=>row.Id!=id)).Select(id=>(Id:id,Name:itemCatalog.State.Catalog?.Find(id)?.Name ?? $"#{id}"))).Where(row=>listPanelCurrencySearch.Length==0 || row.Name.Contains(listPanelCurrencySearch,StringComparison.CurrentCultureIgnoreCase)).ToArray();
                if(ImGui.SmallButton(localization.Get("ListPanels.CurrencyNone")))
                {
                    on=[];write(on);SaveReminders();
                }
                Tip("ListPanels.CurrencyNoneTip");
                foreach(var (id,name) in shown)
                {
                    var ticked=on.Contains(id);
                    if(ImGui.Checkbox($"{name}###ListPanelCurrency{id}",ref ticked))
                    {
                        on=ticked ? [..on,id]:on.Where(other=>other!=id).ToList();
                        write(on);
                        SaveReminders();
                    }
                    Tip("ListPanels.CurrenciesTip");
                }
            }
            finally{ImGui.EndCombo();}
        }
        Tip("ListPanels.CurrenciesTip");
    }

    // W-09 (0.699): worked out at most once a second per panel, not on every frame.
    private readonly Dictionary<string,(long At,(uint Item,long Needed)[] Lines)> listPanelLines=[];
    private long listPanelQuestsAsked=-60001;
    private void DrawListPanelEntries(string key,Func<(uint Item,long Needed)[]> entries)
    {
        var now=Environment.TickCount64;
        if(!listPanelLines.TryGetValue(key,out var known) || now-known.At>1000)
        {
            var wanted=entries();
            listPanelCurrenciesOn=(configuration.ListPanelsCurrenciesOn ?? []).ToHashSet();
            // The quest journal is read when something asks for it: ask, at most once a minute, while it is empty.
            if(configuration.ListPanelsBase && configuration.ListPanelsMetOnly && quests.State.Entries.Count==0 && now-listPanelQuestsAsked>60000){listPanelQuestsAsked=now;quests.RequestRefresh();}
            listPanelLines[key]=known=(now,configuration.ListPanelsBase ? BaseMaterials.Expand(wanted,ListPanelStock,ListPanelRecipe,ListPanelFetched).ToArray():wanted);
        }
        DrawListPanelAlarms(key,known.Lines);
        if(!configuration.ListPanelsBase)
        {
            foreach(var (item,needed) in known.Lines)DrawListPanelLine(key,item,needed,GetRecipeHoldings(item));
            return;
        }
        // 0.760 (X-01, the slow-frame log: this panel held frames of 1.7 to 7.6 s, mostly the runtime collecting): the grouping is worked out with the lines,
        // once a second, or when the zone changes; a frame only draws it.
        var here=ListPanelZoneHere();
        if(!listPanelLayouts.TryGetValue(key,out var layout) || layout.At!=known.At || layout.Here!=here)listPanelLayouts[key]=layout=ListLayoutOf(known.At,known.Lines,here);
        foreach(var (origin,lines) in layout.Origins)
        {
            // 0.700 (user request): a long list folds by category; each panel remembers its own.
            var done=lines.Count(line=>GetRecipeHoldings(line.Item) is { HasObservation:true } held && held.Total>=line.Needed);
            if(!ImGui.CollapsingHeader($"{localization.Get("ListPanels."+origin)} ({done}/{lines.Length})###ListPanel{key}{origin}",ImGuiTreeNodeFlags.DefaultOpen))continue;
            if(origin!=ItemOrigin.Gathering || layout.Zones is null)
            {
                foreach(var (item,needed) in lines)DrawListPanelLine(key,item,needed,GetRecipeHoldings(item));
                continue;
            }
            // 0.752 (X-05), 0.757 (X-11, X-12), 0.758 (X-13), user requests: timed points apart, then by zone, here first, then the cheapest teleport; each folds.
            RequestListTeleportCosts();
            foreach(var group in layout.Zones)
            {
                var zoneDone=group.Lines.Count(line=>GetRecipeHoldings(line.Item) is { HasObservation:true } held && held.Total>=line.Needed);
                if(!ImGui.TreeNodeEx($"{group.Label} ({zoneDone}/{group.Lines.Length}){group.Note}###ListZone{key}{group.Id}",ImGuiTreeNodeFlags.DefaultOpen))continue;
                try
                {
                    foreach(var (item,needed) in group.Lines)
                    {
                        DrawListPanelLine(key,item,needed,GetRecipeHoldings(item));
                        if(group.Timed && ImGui.IsItemHovered())WrappedTooltip(string.Join(", ",ListPanelZones(item)));
                    }
                }
                finally{ImGui.TreePop();}
            }
        }
    }
    private sealed record ListZoneGroup(string Id,string Label,string Note,bool Timed,(uint Item,long Needed)[] Lines);
    private sealed record ListLayout(long At,string? Here,(ItemOrigin Origin,(uint Item,long Needed)[] Lines)[] Origins,ListZoneGroup[]? Zones);
    private readonly Dictionary<string,ListLayout> listPanelLayouts=[];
    private ListLayout ListLayoutOf(long at,(uint Item,long Needed)[] all,string? here)
    {
        var origins=Enum.GetValues<ItemOrigin>().Where(ListPanelOriginShown).Select(origin=>(Origin:origin,Lines:all.Where(line=>ListPanelOrigin(line.Item)==origin).ToArray()))
            .Where(group=>group.Lines.Length>0).ToArray();
        if(!configuration.ListPanelsByZone || origins.FirstOrDefault(group=>group.Origin==ItemOrigin.Gathering) is not { Lines:{ } gathered })return new(at,here,origins,null);
        var zones=new List<ListZoneGroup>();
        var timed=gathered.Where(line=>ListPanelTimedOnly(line.Item)).ToArray();
        if(timed.Length>0)zones.Add(new("Timed",localization.Get("ListPanels.TimedGroup"),"",true,timed));
        var rest=gathered.Except(timed).ToArray();
        var costs=new Dictionary<string,uint?>();
        uint? Cost(string zone){if(!costs.TryGetValue(zone,out var known))costs[zone]=known=ListPanelZoneCost(zone);return known;}
        foreach(var (zone,inZone) in BaseMaterials.OrderZones(BaseMaterials.ByZone(rest,line=>ListPanelZones(line.Item)),here,Cost))
        {
            var note=zone==here && zone.Length>0 ? " · "+localization.Get("ListPanels.ZoneHere"):Cost(zone) is { } gil ? " · "+localization.Format("ListPanels.ZoneCost",gil.ToString("N0")):"";
            zones.Add(new(zone,zone.Length>0 ? zone:localization.Get("ListPanels.ZoneUnknown"),note,false,inZone.ToArray()));
        }
        return new(at,here,origins,zones.ToArray());
    }
    // 0.755 (X-09, user request): the alarms of every timed point of what the list still needs, those already there left out (same label and time, as the
    // Gathering page names them). Worked out with the list, once a second at most.
    private readonly Dictionary<string,(long At,TenshoAssistant.Alarms.TenshoAlarm[] Alarms)> listPanelAlarms=[];
    private void DrawListPanelAlarms(string key,(uint Item,long Needed)[] lines)
    {
        var now=Environment.TickCount64;
        if(!listPanelAlarms.TryGetValue(key,out var known) || now-known.At>5000)listPanelAlarms[key]=known=(now,ListAlarms(lines));
        ImGui.BeginDisabled(known.Alarms.Length==0);
        try{if(ImGui.SmallButton(localization.Format("ListPanels.AddAlarms",known.Alarms.Length)+$"###ListAlarms{key}")){AddAlarms(known.Alarms);listPanelAlarms.Remove(key);}}
        finally{ImGui.EndDisabled();}
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get(known.Alarms.Length==0 ? "ListPanels.AddAlarmsNone":"ListPanels.AddAlarmsTip"));
    }
    private TenshoAssistant.Alarms.TenshoAlarm[] ListAlarms((uint Item,long Needed)[] lines)
    {
        if(gatheringCatalog.State.Catalog is not { } catalog)return [];
        var existing=configuration.Alarms ?? [];
        var created=new List<TenshoAssistant.Alarms.TenshoAlarm>();
        foreach(var (item,needed) in lines)
        {
            if(GetRecipeHoldings(item) is { HasObservation:true } held && held.Total>=needed)continue;
            var name=itemCatalog.State.Catalog?.Find(item)?.Name ?? item.ToString();
            foreach(var source in catalog.ForItem(item))
            {
                var timing=source.Timing;
                if(timing.Kind!=Gathering.GatheringScheduleKind.Limited || timing.OpeningMinutes.Count==0)continue;
                var place=string.IsNullOrWhiteSpace(source.Place) ? source.Zone:source.Place;
                var job=TenshoAssistant.Alarms.AlarmSchedule.JobOf(source.TypeId,source.IsFishing);
                foreach(var alarm in TenshoAssistant.Alarms.AlarmSchedule.ForWindows($"{name} · {place}",timing.OpeningMinutes,DefaultAlarmLead,source.TerritoryId,item))
                {
                    alarm.JobId=job;
                    if(existing.Any(known=>known.Label==alarm.Label && known.Repeat==alarm.Repeat && known.EtMinute==alarm.EtMinute)
                        || created.Any(known=>known.Label==alarm.Label && known.EtMinute==alarm.EtMinute))continue;
                    created.Add(alarm);
                }
            }
        }
        return created.ToArray();
    }

    // 0.757 (X-12): the zone's cheapest teleport, as the game prices it from where the character is (asked again once a minute while a list shows zones).
    internal IReadOnlyDictionary<uint,uint[]>? TerritoryAetherytes { get; set; }
    private long listTeleportAsked;
    private void RequestListTeleportCosts()
    {
        var now=Environment.TickCount64;
        if(now-listTeleportAsked<60000)return;
        listTeleportAsked=now;mapLive?.RefreshTeleportCosts();
    }
    private bool ListPanelTimedOnly(uint item)=>gatheringCatalog.State.Catalog?.ForItem(item) is { Count:>0 } sources
        && sources.All(source=>source.Timing.Kind==Gathering.GatheringScheduleKind.Limited);
    // 0.760: zones and territories of the gathering catalogue, indexed once per catalogue (the panel used to scan every source for each zone, each frame).
    private (object? Catalog,Dictionary<uint,string> ZoneOf,Dictionary<string,uint[]> Territories) listZoneIndex=(null,[],[]);
    private (Dictionary<uint,string> ZoneOf,Dictionary<string,uint[]> Territories) ListZoneIndex()
    {
        var catalog=gatheringCatalog.State.Catalog;
        if(!ReferenceEquals(listZoneIndex.Catalog,catalog))
        {
            var sources=catalog?.Sources.Where(source=>source.Zone.Length>0 && source.TerritoryId!=0).ToArray() ?? [];
            listZoneIndex=(catalog,sources.GroupBy(source=>source.TerritoryId).ToDictionary(group=>group.Key,group=>group.First().Zone),
                sources.GroupBy(source=>source.Zone).ToDictionary(group=>group.Key,group=>group.Select(source=>source.TerritoryId).Distinct().ToArray()));
        }
        return (listZoneIndex.ZoneOf,listZoneIndex.Territories);
    }
    private string? ListPanelZoneHere()
    {
        var territory=CurrentTerritory?.Invoke() ?? 0;
        return territory==0 ? null:ListZoneIndex().ZoneOf.GetValueOrDefault(territory);
    }
    private uint? ListPanelZoneCost(string zone)
    {
        if(zone.Length==0 || mapLive?.TeleportCosts is not { } costs || TerritoryAetherytes is not { } aetherytes || !ListZoneIndex().Territories.TryGetValue(zone,out var territories))return null;
        uint? best=null;
        foreach(var territory in territories)
            foreach(var id in aetherytes.GetValueOrDefault(territory) ?? [])
                if(costs.TryGetValue(id,out var gil) && (best is null || gil<best))best=gil;
        return best;
    }

    // 0.752: the zones where an item is gathered, from the gathering catalogue, kept per catalogue.
    private (object? Catalog,Dictionary<uint,string[]> Zones) listPanelZones=(null,[]);
    private IReadOnlyCollection<string> ListPanelZones(uint item)
    {
        var catalog=gatheringCatalog.State.Catalog;
        if(!ReferenceEquals(listPanelZones.Catalog,catalog))listPanelZones=(catalog,[]);
        if(listPanelZones.Zones.TryGetValue(item,out var known))return known;
        var zones=catalog?.ForItem(item).Select(source=>source.Zone).Where(zone=>zone.Length>0).Distinct().ToArray() ?? [];
        listPanelZones.Zones[item]=zones;
        return zones;
    }
    private long ListPanelStock(uint item){var held=GetRecipeHoldings(item);return held.HasObservation ? held.Total:0;}
    private RecipeDefinition? ListPanelRecipe(uint item)=>recipeCatalog.State.Catalog?.ForResult(item) is { Count:>0 } recipes ? PreferredRecipe(recipes):null;
    private bool ListPanelGathered(uint item)=>gatheringCatalog.State.Catalog?.ForItem(item) is { Count:>0 };
    private bool ListPanelSold(uint item)=>npcVendors?.State.Catalog?.ForItem(item) is { Count:>0 } offers
        && BaseMaterials.VendorCounts(configuration.ListPanelsVendor,offers.Where(ListPanelOfferOpen).Select(offer=>(IReadOnlyList<uint>)offer.Sale.Costs.Select(cost=>cost.CurrencyItemId).ToArray()),listPanelCurrenciesOn);
    private bool ListPanelOfferOpen(NpcVendorOffer offer)
    {
        if(!configuration.ListPanelsMetOnly)return true;
        var (quest,achievement)=SaleRequirementState(offer.Sale);
        return !BaseMaterials.RequirementBlocks(quest,achievement);
    }
    // 0.705 (user request): the vendors an item tooltip shows, under their own settings.
    private IReadOnlyList<NpcVendorOffer> HoverOffers(uint item)
    {
        if(!configuration.HoverVendors || npcVendors?.State.Catalog?.ForItem(item) is not { Count:>0 } offers)return [];
        if(!configuration.HoverVendorsMetOnly && !configuration.HoverVendorsByCurrency)return offers;
        if(configuration.HoverVendorsMetOnly && quests.State.Entries.Count==0 && Environment.TickCount64-listPanelQuestsAsked>60000){listPanelQuestsAsked=Environment.TickCount64;quests.RequestRefresh();}
        var on=(configuration.HoverVendorCurrenciesOn ?? []).ToHashSet();
        return offers.Where(offer=>
        {
            if(configuration.HoverVendorsByCurrency && !BaseMaterials.VendorCounts(true,[offer.Sale.Costs.Select(cost=>cost.CurrencyItemId).ToArray()],on))return false;
            if(!configuration.HoverVendorsMetOnly)return true;
            var (quest,achievement)=SaleRequirementState(offer.Sale);
            return !BaseMaterials.RequirementBlocks(quest,achievement);
        }).ToArray();
    }
    private bool ListPanelFetched(uint item)=>ListPanelGathered(item) || ListPanelSold(item);
    private ItemOrigin ListPanelOrigin(uint item)=>ClassifyCrystal(item) is not null ? ItemOrigin.Crystal:BaseMaterials.OriginOf(ListPanelGathered(item),ListPanelSold(item),lumina?.State.Data?.MonstersOf(item) is { Count:>0 });
    private bool ListPanelOriginShown(ItemOrigin origin)=>origin switch
    {
        ItemOrigin.Gathering=>configuration.ListPanelsGathering,
        ItemOrigin.Vendor=>configuration.ListPanelsVendor,
        ItemOrigin.Monster=>configuration.ListPanelsMonster,
        ItemOrigin.Crystal=>configuration.ListPanelsCrystal,
        _=>configuration.ListPanelsOther,
    };

    // Stock over what is needed; a dash when the stocks have not been read: unknown is not zero.
    private readonly Diagnostics.BlinkWatch listPanelBlinks=new();
    private void DrawListPanelLine(string list,uint itemId,long required,Inventory.ItemHoldingsSummary stock)
    {
        // 0.740: developer mode, to find the line that goes back and forth while gathering (docs/evidence/0.740-list-blink.md).
        if(developerMode)
        {
            var at=DateTimeOffset.UtcNow;var heldNow=stock.HasObservation ? stock.Total:-1;
            if(listPanelBlinks.Observe(list,itemId,heldNow,required,at) is { } blink)log.Information(blink);
            // 0.743 (the blink came back in 0.742): what each storage said at the moment the stock read 0, and what the bags reading was.
            if(listPanelBlinks.FellToZero(list,itemId,heldNow,at))
            {
                var shown=inventory.Shown;var current=inventory.Current;
                log.Information("List zero: {0}, item {1}: character {2}; rows {3}; shown {4}; current {5}",list,itemId,bankBridge.State.CharacterId!=0 ? "known":"0",
                    string.Join(", ",stock.Rows.Select(row=>$"{row.Label} {row.Coverage} {(row.IsCached ? "cached ":"")}{row.Nq + row.Hq}")),
                    shown is null ? "none":$"{(at-shown.ObservedAt).TotalMilliseconds:0} ms old, "+string.Join(" ",shown.Containers.Values.Select(container=>$"{container.Location.Container.Value}:{container.Coverage}:{container.Stacks.Count}")),
                    current is null ? "none":ReferenceEquals(current,shown) ? "same":"other");
            }
        }
        var item=itemCatalog.State.Catalog?.Find(itemId);
        DrawItemIcon(item,new Vector2(ImGui.GetTextLineHeight()));
        if(ImGui.IsItemHovered())DrawItemInfoTooltip(itemId);
        ImGui.SameLine();
        ImGui.TextUnformatted(item?.Name ?? itemId.ToString());
        ImGui.SameLine();
        if(!stock.HasObservation)ImGui.TextDisabled($"— / {required:N0}");
        else if(stock.Total<required)ImGui.TextColored(UI.FfxivTheme.Warning,$"{stock.Total:N0} / {required:N0}");
        else ImGui.TextColored(Success,$"{stock.Total:N0} / {required:N0}");
    }
}
