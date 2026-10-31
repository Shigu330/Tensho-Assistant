using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.Craft;
using TenshoAssistant.UI;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private sealed record PlannedTool(string Group, string Name, string Reference, string Description, FontAwesomeIcon Icon);
    private PlannedTool? selectedTool;
    private string ToolDetailsPopup => ToolText("DetailsTitle")+"###TenshoToolDetails";

    // Product directory, not registrations of unimplemented modules. Scope remains in ROADMAP.md.
    private static readonly PlannedTool[] PlannedTools =
    [
        new("Groups.Crafting", "Crafting.Name", "Craftimizer", "Crafting.Description", FontAwesomeIcon.Hammer),
        new("Groups.Gathering", "Gathering.Name", "GatherBuddy", "Gathering.Description", FontAwesomeIcon.Fish),
        new("Groups.Gathering", "GatheringGp.Name", "LazyGatherer", "GatheringGp.Description", FontAwesomeIcon.Seedling),
        new("Groups.Items", "Sorting.Name", "SortaKinda", "Sorting.Description", FontAwesomeIcon.Boxes),
        new("Groups.Items", "Bank.Name", "Banque d'objets", "Bank.Description", FontAwesomeIcon.Archive),
        new("Groups.Items", "ItemUses.Name", "Item Use", "ItemUses.Description", FontAwesomeIcon.Recycle),
        new("Groups.Items", "Market.Name", "Price Insight", "Market.Description", FontAwesomeIcon.Coins),
        new("Groups.Progress", "Collections.Name", "Collections", "Collections.Description", FontAwesomeIcon.Trophy),
        new("Groups.Progress", "Quests.Name", "QuestTracker", "Quests.Description", FontAwesomeIcon.Tasks),
        new("Groups.Progress", "BlueMagic.Name", "Blue Mage Helper", "BlueMagic.Description", FontAwesomeIcon.BookOpen),
        new("Groups.World", "Maps.Name", "Mappy", "Maps.Description", FontAwesomeIcon.MapMarkedAlt),
        new("Groups.World", "DutyLoot.Name", "Duty Loot Preview", "DutyLoot.Description", FontAwesomeIcon.Dungeon),
        new("Groups.World", "MonsterLoot.Name", "Monster Loot Hunter", "MonsterLoot.Description", FontAwesomeIcon.Dragon),
        new("Groups.Games", "Fashion.Name", "Avant-Garde", "Fashion.Description", FontAwesomeIcon.Tshirt),
        new("Groups.Games", "Cactpot.Name", "ezMiniCactpot", "Cactpot.Description", FontAwesomeIcon.TicketAlt),
        new("Groups.Games", "Triad.Name", "TriadBuddy", "Triad.Description", FontAwesomeIcon.Clone),
        new("Groups.Character", "Character.Name", "Character Panel Refined", "Character.Description", FontAwesomeIcon.ChartBar),
        new("Groups.Character", "Macros.Name", "Macro Chain", "Macros.Description", FontAwesomeIcon.Link),
    ];

    private string ToolText(string key)=>localization.Get("ToolDirectory."+key);

    private void DrawTools()
    {
        Heading(localization.Get("Plugin.AllTools"), localization.Get("Plugin.AllToolsDescription"));
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##ToolFilter", localization.Get("Plugin.ToolFilterHint"), ref toolQuery, 128);
        var found=false;var openDetails=false;string? previousGroup=null;
        foreach(var tool in PlannedTools)
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
                        if(ImGui.SmallButton(ToolText("Open")))OpenDirectoryTool(tool.Reference);
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

    private void OpenDirectoryTool(string reference)
    {
        switch(reference)
        {
            case "Craftimizer":page=Page.Items;focusCraftTab=true;break;
            case "GatherBuddy":page=Page.Gathering;gatheringTypeFilter=null;break;
            case "LazyGatherer":page=Page.GatheringGp;break;
            case "SortaKinda":page=Page.SortPreview;break;
            case "Banque d'objets":page=Page.Items;focusBankTab=true;break;
            case "Item Use":page=Page.Items;focusUsesTab=true;break;
            case "Price Insight":page=Page.Market;break;
            case "Collections":page=Page.Collections;break;
            case "QuestTracker":page=Page.Quests;break;
            case "Blue Mage Helper":page=Page.BlueMagic;break;
            case "Mappy":page=Page.Maps;break;
            case "Duty Loot Preview":page=Page.DutyLoot;break;
            case "Monster Loot Hunter":page=Page.MonsterLoot;break;
            case "Avant-Garde":page=Page.Fashion;break;
            case "ezMiniCactpot":page=Page.MiniCactpot;break;
            case "TriadBuddy":page=Page.TripleTriad;break;
            case "Character Panel Refined":page=Page.Character;break;
            case "Macro Chain":page=Page.MacroChain;break;
            default:OpenToolDirectory(reference);break;
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
        foreach (var tool in PlannedTools.Where(tool => tool.Reference is "Craftimizer" or "GatherBuddy" or "Price Insight" or "Monster Loot Hunter"))
        {
            ImGui.PushID($"ContextTool{tool.Reference}");
            try
            {
                ImGui.PushFont(UiBuilder.IconFont);
                bool clicked;
                try { clicked = ImGui.Button(tool.Icon.ToIconString(), new Vector2(34, 28)); }
                finally { ImGui.PopFont(); }
                if (clicked) OpenContextTool(tool.Reference);
                if (ImGui.IsItemHovered()) ImGui.SetTooltip($"{ToolText(tool.Name)} · {SelectedItem?.Name}");
                ImGui.SameLine();
            }
            finally { ImGui.PopID(); }
        }
        ImGui.NewLine();
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
            if(developerMode)ImGui.TextDisabled(tool.Reference);
            if(tool.Reference=="Item Use")DrawItemUsePreview();
            if(tool.Reference=="Craftimizer")
            {
                if(ImGui.SmallButton(localization.Get("ToolDetails.Live"))){page=Page.CraftLive;ImGui.CloseCurrentPopup();}
                if(ImGui.SmallButton(localization.Get("ToolDetails.Presets"))){page=Page.CraftPresets;ImGui.CloseCurrentPopup();}
            }
            ImGui.Spacing();
            if(ImGui.Button(ToolText("Open"),new Vector2(-1,28))){OpenDirectoryTool(tool.Reference);ImGui.CloseCurrentPopup();}
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
            if(nativeUses.IsSupplyCandidate((uint)itemId))ImGui.TextUnformatted(localization.Get("UsePreviewUi.Supply"));
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
