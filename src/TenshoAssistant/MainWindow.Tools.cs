using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.Craft;
using TenshoAssistant.UI;

namespace TenshoAssistant;

// « All tools » directory (Home page, optional): the tools grouped by domain, each with the community plugin it is modelled on
// (shown as a name for orientation), a filter, a details popup and a link to its Tensho page. Also the item
// context icons of the sidebar (DrawContextTools: the same useful routes as the inventory's « Tensho » item menu). The list is
// fixed in PlannedTools; pages added later (weather, alarms, reminders, free company, gear, NPC vendors) joined it in 0.315.
// System plan 4.13 and 4.14: the directory is completed from the list of pages (MainWindow.Pages.cs): a page that no written row opens gets
// a row of its own (its help title and purpose, PageHelp, in every language), so a new page is never missing from it; the pages left out
// are listed below with their reason, and a test (PageCoverageTests) holds both rules.
internal sealed partial class MainWindow
{
    /// <param name="Page">The page its « Open » button shows (the written rows add a tab on some pages, OpenDirectoryTool).</param>
    private sealed record PlannedTool(string Group, string Name, string Reference, string Description, FontAwesomeIcon Icon, Page Page);
    private PlannedTool? selectedTool;
    private string ToolDetailsPopup => ToolText("DetailsTitle")+"###TenshoToolDetails";

    // Product directory, not registrations of unimplemented modules. Scope remains in ROADMAP.md.
    private static readonly PlannedTool[] PlannedTools =
    [
        new("Groups.Crafting", "Crafting.Name", "Craftimizer", "Crafting.Description", FontAwesomeIcon.Hammer, Page.Items),
        new("Groups.Gathering", "Gathering.Name", "GatherBuddy", "Gathering.Description", FontAwesomeIcon.Fish, Page.Gathering),
        new("Groups.Gathering", "GatheringGp.Name", "LazyGatherer", "GatheringGp.Description", FontAwesomeIcon.Seedling, Page.GatheringGp),
        new("Groups.Items", "Sorting.Name", "SortaKinda", "Sorting.Description", FontAwesomeIcon.Boxes, Page.SortPreview),
        new("Groups.Items", "Bank.Name", "Banque d'objets", "Bank.Description", FontAwesomeIcon.Archive, Page.Storage),
        new("Groups.Items", "ItemUses.Name", "Item Use", "ItemUses.Description", FontAwesomeIcon.Recycle, Page.Items),
        new("Groups.Items", "Market.Name", "Price Insight", "Market.Description", FontAwesomeIcon.Coins, Page.Market),
        new("Groups.Progress", "Collections.Name", "Collections", "Collections.Description", FontAwesomeIcon.Trophy, Page.Collections),
        new("Groups.Progress", "Quests.Name", "QuestTracker", "Quests.Description", FontAwesomeIcon.Tasks, Page.Quests),
        new("Groups.Progress", "BlueMagic.Name", "Blue Mage Helper", "BlueMagic.Description", FontAwesomeIcon.BookOpen, Page.BlueMagic),
        new("Groups.World", "Maps.Name", "Mappy", "Maps.Description", FontAwesomeIcon.MapMarkedAlt, Page.Maps),
        new("Groups.World", "DutyLoot.Name", "Duty Loot Preview", "DutyLoot.Description", FontAwesomeIcon.Dungeon, Page.DutyLoot),
        new("Groups.World", "MonsterLoot.Name", "Monster Loot Hunter", "MonsterLoot.Description", FontAwesomeIcon.Dragon, Page.MonsterLoot),
        new("Groups.Games", "Fashion.Name", "Avant-Garde", "Fashion.Description", FontAwesomeIcon.Tshirt, Page.Fashion),
        new("Groups.Games", "Cactpot.Name", "ezMiniCactpot", "Cactpot.Description", FontAwesomeIcon.TicketAlt, Page.MiniCactpot),
        new("Groups.Games", "Triad.Name", "TriadBuddy", "Triad.Description", FontAwesomeIcon.Clone, Page.TripleTriad),
        new("Groups.Character", "Character.Name", "Character Panel Refined", "Character.Description", FontAwesomeIcon.ChartBar, Page.Gear),
        new("Groups.Character", "Macros.Name", "Macro Chain", "Macros.Description", FontAwesomeIcon.Link, Page.MacroChain),
        // 0.315 (docs pass): tools added since 0.200 that had no directory entry; Tensho's own tools use a « Tensho: » reference.
        new("Groups.Character", "Gear.Name", "Tensho: Gear", "Gear.Description", FontAwesomeIcon.Tshirt, Page.Gear),
        new("Groups.Items", "FreeCompany.Name", "Tensho: FreeCompany", "FreeCompany.Description", FontAwesomeIcon.Ship, Page.FreeCompany),
        new("Groups.Items", "NpcVendors.Name", "Tensho: NpcVendors", "NpcVendors.Description", FontAwesomeIcon.Store, Page.NpcVendors),
        new("Groups.Gathering", "Weather.Name", "Tensho: Weather", "Weather.Description", FontAwesomeIcon.CloudSun, Page.Weather),
        new("Groups.Gathering", "Alarms.Name", "Tensho: Alarms", "Alarms.Description", FontAwesomeIcon.Bell, Page.Alarms),
        new("Groups.Progress", "Reminders.Name", "Tensho: Reminders", "Reminders.Description", FontAwesomeIcon.CalendarCheck, Page.Reminders),
    ];
    // 0.331 (audit): the directory draws a heading on each group change, so it lists the tools grouped (first-seen group order, array
    // order inside a group); entries appended later no longer repeat a heading. Built once, after PlannedTools (same file).
    private PlannedTool[]? directoryTools;
    private PlannedTool[] DirectoryTools => directoryTools ??= PlannedTools.Concat(PageTools()).GroupBy(tool => tool.Group).SelectMany(group => group).ToArray();

    /// <summary>Pages that are not tools of their own, with the reason (a test refuses a page missing from the directory and from here).</summary>
    private static readonly Dictionary<Page, string> DirectoryLeftOut = new()
    {
        [Page.Home] = "the directory is on it",
        [Page.Settings] = "preferences, not a tool",
        [Page.Help] = "the help, reached from the « ? » of every page",
        [Page.Credits] = "the sources and thanks, not a tool",
        [Page.Diagnostics] = "developer mode only",
        [Page.CraftLive] = "a tab of Crafting, reached from the crafting row's details",
        [Page.CraftPresets] = "a tab of Crafting, reached from the crafting row's details",
        [Page.GatheringCollectables] = "a tab of Gathering",
    };

    /// <summary>One row for every page of the list that no written row opens and that is not left out.</summary>
    private IEnumerable<PlannedTool> PageTools()
    {
        var opened = PlannedTools.Select(tool => tool.Page).ToHashSet();
        foreach (var entry in PageEntries)
            if (!opened.Contains(entry.Page) && !DirectoryLeftOut.ContainsKey(entry.Page))
                yield return new(DirectoryGroup(entry), $"PageHelp.{entry.Page}Title", "Tensho: " + entry.Page, $"PageHelp.{entry.Page}Purpose", FontAwesomeIcon.Wrench, entry.Page);
    }

    /// <summary>The directory's group of a page, from its navigation group.</summary>
    private static string DirectoryGroup(PageEntry entry) => entry.Page switch
    {
        Page.OceanFishing or Page.IslandSanctuary or Page.Garden => "Groups.Gathering",
        Page.Chocobo or Page.Lexicon or Page.MyTools => "Groups.Character",
        Page.RelicWeapons => "Groups.Progress",
        _ => entry.Category switch
        {
            "Objects" or "Tracking" => "Groups.Items",
            "Production" => "Groups.Crafting",
            "World" or "Content" => "Groups.World",
            "GoldSaucer" => "Groups.Games",
            "Progression" => "Groups.Progress",
            _ => "Groups.Character",
        },
    };

    // A page's own row reads its help texts; the written rows read the directory's texts.
    private string ToolText(string key)=>localization.Get(key.StartsWith("PageHelp.",StringComparison.Ordinal) ? key : "ToolDirectory."+key);

    private void DrawTools()
    {
        Heading(localization.Get("Plugin.AllTools"), localization.Get("Plugin.AllToolsDescription"));
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##ToolFilter", localization.Get("Plugin.ToolFilterHint"), ref toolQuery, 128); Tip("Plugin.ToolFilterTip");
        var found=false;var openDetails=false;string? previousGroup=null;
        foreach(var tool in DirectoryTools)
        {
            if(!MatchesToolFilter($"{ToolText(tool.Group)} {ToolText(tool.Name)} {tool.Reference}"))continue;
            found=true;
            if(previousGroup!=tool.Group)
            {
                // Adjacent tools share their localized heading even when metadata keys differ.
                var groupName=ToolText(tool.Group);
                if(previousGroup is null || ToolText(previousGroup)!=groupName)
                { ImGui.Spacing();UiComponents.SectionHeader(groupName); }
                previousGroup=tool.Group;
            }
            ImGui.PushID(tool.Reference);
            try
            {
                if(ImGui.BeginTable("DirectoryRow",2,ImGuiTableFlags.SizingStretchProp))
                {
                    try
                    {
                        ImGui.TableSetupColumn("Content",ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableSetupColumn("Actions",ImGuiTableColumnFlags.WidthFixed,145);
                        ImGui.TableNextRow();ImGui.TableNextColumn();
                        ImGui.TextWrapped(ToolText(tool.Name));
                        ImGui.PushStyleColor(ImGuiCol.Text,Muted);
                        try{ImGui.TextWrapped(ToolText(tool.Description));}finally{ImGui.PopStyleColor();}
                        ImGui.TableNextColumn();
                        if(ImGui.SmallButton(ToolText("Open")))OpenDirectoryTool(tool);
                        ImGui.SameLine();
                        if(ImGui.SmallButton(ToolText("Details")))
                        {selectedTool=tool;openDetails=true;}
                    }
                    finally{ImGui.EndTable();}
                }
            }
            finally{ImGui.PopID();}
        }
        if(!found)ImGui.TextWrapped(localization.Get("Plugin.NoToolMatch"));
        if(openDetails)ImGui.OpenPopup(ToolDetailsPopup);
        DrawToolDetails();
    }

    private void OpenDirectoryTool(PlannedTool tool)
    {
        page = tool.Page;
        switch(tool.Reference)
        {
            case "Craftimizer":focusCraftTab=true;break;
            case "GatherBuddy":gatheringTypeFilter=null;break;
            case "Banque d'objets":focusBankTab=true;break;
            case "Item Use":focusUsesTab=true;break;
        }
    }

    private bool MatchesToolFilter(string text) => string.IsNullOrWhiteSpace(toolQuery)
        || CultureInfo.InvariantCulture.CompareInfo.IndexOf(text, toolQuery.Trim(),
            CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    private void OpenToolDirectory(string reference)
    {
        revealHomeTools = true;
        toolQuery = reference;
        page = Page.Home;
    }

    private void OpenContextTool(string reference)
    {
        switch (reference)
        {
            case "Craftimizer": page = Page.Items; focusCraftTab = true; break;
            case "Item Use": page = Page.Items; focusUsesTab = true; break;
            case "GatherBuddy": page = Page.Items; gatheringTypeFilter = null; focusGatherTab = true; break;
            case "Price Insight": page = Page.Market; break;
            case "Monster Loot Hunter": page = Page.MonsterLoot; break;
            case "LazyGatherer": page = Page.GatheringGp; break;
            default: OpenToolDirectory(reference); break;
        }
    }

    private void DrawContextTools()
    {
        ImGui.Spacing();
        if (SelectedItem is not { } item) return;
        var width = ImGui.GetContentRegionAvail().X; var used = 0f;
        void Button(string id, FontAwesomeIcon icon, string tooltip, Action open)
        {
            // Wraps to a new row when the sidebar is too narrow for the next icon.
            if (used > 0 && used + 34 <= width) ImGui.SameLine(); else used = 0;
            ImGui.PushID($"ContextTool{id}");
            try
            {
                ImGui.PushFont(UiBuilder.IconFont);
                bool clicked;
                try { clicked = ImGui.Button(icon.ToIconString(), new Vector2(34, 28)); }
                finally { ImGui.PopFont(); }
                if (clicked) open();
                if (ImGui.IsItemHovered()) WrappedTooltip($"{tooltip} · {item.Name}");
            }
            finally { ImGui.PopID(); }
            used += 34 + ImGui.GetStyle().ItemSpacing.X;
        }
        // 0.208: the same useful tools as the inventory's « Tensho — item » menu (ItemMenu.Title) (RelevantRoutes), as icons.
        foreach (var route in RelevantRoutes(item.Id))
        {
            if (route is Items.ItemContextRoute.Tools) continue;
            var (icon, label) = route switch
            {
                Items.ItemContextRoute.Usages => (FontAwesomeIcon.Recycle, localization.Get("ItemUsesUi.Title")),
                Items.ItemContextRoute.Craft => (FontAwesomeIcon.Hammer, localization.Get("ItemsUi.Craft")),
                Items.ItemContextRoute.Gathering => (FontAwesomeIcon.Fish, localization.Get("GatheringDetails.Title")),
                Items.ItemContextRoute.NpcVendors => (FontAwesomeIcon.Store, localization.Get("NpcUi.Title")),
                Items.ItemContextRoute.Market => (FontAwesomeIcon.Coins, localization.Get("ItemLinks.Market")),
                Items.ItemContextRoute.DutyLoot => (FontAwesomeIcon.Dungeon, localization.Get("ItemMenu.DutyLoot")),
                Items.ItemContextRoute.MonsterLoot => (FontAwesomeIcon.Dragon, localization.Get("ItemMenu.MonsterLoot")),
                Items.ItemContextRoute.Quest => (FontAwesomeIcon.Tasks, localization.Get("ItemMenu.Quest")),
                Items.ItemContextRoute.Collection => (CollectionOf(item)?.Kind == Collections.CollectionKind.TripleTriad ? FontAwesomeIcon.Clone : FontAwesomeIcon.Trophy, localization.Get(CollectionOf(item)?.Kind == Collections.CollectionKind.TripleTriad ? "ItemMenu.OpenTriad" : "HoverUi.OpenCollection")),
                Items.ItemContextRoute.Stocks => (FontAwesomeIcon.Boxes, localization.Get("ItemsUi.Stocks")),
                _ => (FontAwesomeIcon.InfoCircle, localization.Get("ItemMenu.Sources")),
            };
            Button(route.ToString(), icon, label, () => OpenRoute(route, item.Id));
        }
    }

    private void DrawToolDetails()
    {
        var width=Math.Min(510,Math.Max(200,ImGui.GetMainViewport().WorkSize.X-24));
        ImGui.SetNextWindowSize(new Vector2(width,0), ImGuiCond.Appearing);
        ImGui.SetNextWindowSizeConstraints(new Vector2(Math.Min(200,width),0),new Vector2(width,Math.Max(100,ImGui.GetMainViewport().WorkSize.Y-24)));
        var open=true;
        if(!ImGui.BeginPopupModal(ToolDetailsPopup,ref open,ImGuiWindowFlags.AlwaysAutoResize))return;
        try
        {
            if(selectedTool is not { } tool)return;
            ImGui.TextColored(Accent,ToolText(tool.Name));
            ImGui.TextDisabled(ToolText(tool.Group));
            ImGui.Separator();
            var detailKey=tool.Reference switch
            {
                "Item Use"=>"Uses",
                "Avant-Garde"=>"Fashion",
                "LazyGatherer"=>"Gp",
                "Monster Loot Hunter"=>"Monsters",
                "Duty Loot Preview"=>"Duties",
                "TriadBuddy"=>"Triad",
                "Macro Chain"=>"Macros",
                "Mappy"=>"Maps",
                "ezMiniCactpot"=>"Cactpot",
                "Character Panel Refined"=>"Character",
                "Blue Mage Helper"=>"Blue",
                "QuestTracker"=>"Quests",
                "Price Insight"=>"Market",
                "Collections"=>"Collections",
                "SortaKinda"=>"Sorting",
                "GatherBuddy"=>"Gathering",
                "Craftimizer"=>"Craft",
                "Banque d'objets"=>"Bank",
                _=>null
            };
            ImGui.TextWrapped(detailKey is null ? ToolText(tool.Description) : localization.Get("ToolDetails."+detailKey));
            // 0.1038 (night test T-0.1014-1): « Tensho: Lexicon » showed in English; the reference line is a translated text.
            if(developerMode)ImGui.TextDisabled(tool.Reference.StartsWith("Tensho: ",StringComparison.Ordinal) ? localization.Get("ToolDetails.OwnTool"):localization.Format("ToolDetails.Reference",tool.Reference));
            if(tool.Reference=="Item Use")DrawItemUsePreview();
            if(tool.Reference=="Craftimizer")
            {
                if(ImGui.SmallButton(localization.Get("ToolDetails.Live"))){page=Page.CraftLive;ImGui.CloseCurrentPopup();}
                if(ImGui.SmallButton(localization.Get("ToolDetails.Presets"))){page=Page.CraftPresets;ImGui.CloseCurrentPopup();}
            }
            ImGui.Spacing();
            if(ImGui.Button(ToolText("Open"),new Vector2(-1,28))){OpenDirectoryTool(tool);ImGui.CloseCurrentPopup();}
            if(ImGui.Button(ToolText("Close"),new Vector2(-1,28)))ImGui.CloseCurrentPopup();
        }
        finally{ImGui.EndPopup();}
    }

    private void DrawItemUsePreview()
    {
        if (itemId <= 0)
        {
            ImGui.TextDisabled(localization.Get("UsePreviewUi.Select"));
            return;
        }

        if(itemUses.State.Catalog is { } nativeUses)
        {
            ImGui.TextUnformatted(localization.Format("UsePreviewUi.Leves",nativeUses.LevesFor((uint)itemId).Count));
            if(nativeUses.IsSupplyCandidate((uint)itemId))ImGui.TextUnformatted(GcSupplyLine((uint)itemId));
            if(nativeUses.IsAquariumFish((uint)itemId))ImGui.TextUnformatted(localization.Get("UsePreviewUi.Aquarium"));
        }
        if (recipeCatalog.State.Catalog is not { } catalog)
        {
            ImGui.TextDisabled(localization.Get("UsePreviewUi.Loading"));
            return;
        }

        var uses = catalog.ForIngredient((uint)itemId)
            .GroupBy(recipe => recipe.ResultItemId)
            .Select(group => group.First())
            .ToArray();
        ImGui.TextColored(Accent, localization.Format("UsePreviewUi.Count",uses.Length));
        ImGui.TextDisabled(localization.Get("UsePreviewUi.Help"));
        if (uses.Length == 0)
        {
            ImGui.TextDisabled(localization.Get("UsePreviewUi.Empty"));
            return;
        }

        foreach (var recipe in uses.Take(6))
        {
            var result = itemCatalog.State.Catalog?.Find(recipe.ResultItemId);
            ImGui.PushID($"ItemUse{recipe.RecipeId}");
            try
            {
                DrawItemIcon(result, new Vector2(28));
                if (ImGui.IsItemClicked())
                {
                    SelectItem(recipe.ResultItemId);
                    focusCraftTab = true;
                    page = Page.Items;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                var label = result?.Name ?? localization.Format("BankUi.Item",recipe.ResultItemId);
                if (ImGui.SmallButton($"{label}##ItemUseResult"))
                {
                    SelectItem(recipe.ResultItemId);
                    focusCraftTab = true;
                    page = Page.Items;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                ImGui.TextDisabled(localization.Format("RecipeUi.JobLevel",recipe.CraftType,recipe.Level));
            }
            finally { ImGui.PopID(); }
        }
        if (uses.Length > 6) ImGui.TextDisabled(localization.Format("UsePreviewUi.More",uses.Length-6));
    }
}
