using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.MyTools;

namespace TenshoAssistant;

// TEMPLATES-001 (DEC-029): « My tools », pages made of data: titles, notes, checklists, counters, timers, item lists and links. A tool holds no code and sends nothing to the game. What is pasted is
// checked by ToolTemplateFormat and shown in full before it is installed; a link opens only after a confirmation that shows its address.
internal sealed partial class MainWindow
{
    internal ToolTemplateStore? MyToolsStore{get;set;}
    private string? myToolSelected;
    private bool myToolsImporting;
    private string myToolsPasted="";
    private TemplateParse? myToolsPreview;
    private string? myToolRemoveArmed;
    private string? myToolPendingLink;
    private bool myToolsGalleryOpen;
    internal ToolGallery? MyToolsGallery{get;set;}

    private static string SafeLabel(string text)=>text.Replace("##","# #");

    private void DrawMyTools()
    {
        TenshoAssistant.UI.UiComponents.SectionHeader(localization.Get("MyToolsUi.Title"),localization.Get("MyToolsUi.Help"));
        if(MyToolsStore is not { } store)return;
        if(store.Unavailable){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("MyToolsUi.Unavailable"));return;}
        if(store.RefusedCount>0)ImGui.TextColored(UI.FfxivTheme.Warning,localization.Format("MyToolsUi.Refused",store.RefusedCount));
        DrawMyToolsThemeReturn(store);
        if(myToolDraft is { } draft){DrawMyToolEditor(store,draft);return;}
        if(ImGui.SmallButton(localization.Get("MyToolsUi.Create"))){myToolDraft=ToolDraft.New();myToolDraftProblem=null;return;}
        Tip("MyToolsUi.CreateTip");
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get(myToolsImporting ? "MyToolsUi.ImportClose":"MyToolsUi.Import"))){myToolsImporting=!myToolsImporting;myToolsPreview=null;}
        Tip("MyToolsUi.ImportTip");
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get(myToolsGalleryOpen ? "MyToolsUi.GalleryClose":"MyToolsUi.Gallery")))myToolsGalleryOpen=!myToolsGalleryOpen;
        Tip("MyToolsUi.GalleryTip");
        if(myToolsImporting)DrawMyToolsImport(store);
        if(myToolsGalleryOpen)DrawMyToolsGallery(store);
        var tools=store.Tools;
        if(tools.Count==0){ImGui.Spacing();ImGui.TextDisabled(localization.Get("MyToolsUi.Empty"));return;}
        ImGui.Spacing();
        var language=localization.CurrentCode;
        foreach(var entry in tools)
            if(ImGui.Selectable(SafeLabel(entry.Template.Name.For(language))+"###MyTool"+entry.Template.Id,myToolSelected==entry.Template.Id)){myToolSelected=entry.Template.Id;myToolRemoveArmed=null;myToolPendingLink=null;}
        if(tools.FirstOrDefault(entry=>entry.Template.Id==myToolSelected) is not { } tool)return;
        ImGui.Separator();
        DrawMyToolActions(store,tool);
        ImGui.Spacing();
        DrawMyToolBlocks(tool.Template,tool,store);
    }

    private void DrawMyToolActions(ToolTemplateStore store,InstalledTool tool)
    {
        if(tool.Template.Author is { } author){ImGui.TextDisabled(localization.Format("MyToolsUi.Author",author));ImGui.SameLine();}
        if(ImGui.SmallButton(localization.Get("MyToolsUi.Edit"))){myToolDraft=ToolDraft.From(tool.Template);myToolDraftProblem=null;return;}
        Tip("MyToolsUi.EditTip");
        ImGui.SameLine();
        CopyButton(localization.Get("MyToolsUi.Share"),()=>ToolTemplateFormat.Encode(tool.Json));
        Tip("MyToolsUi.ShareTip");
        ImGui.SameLine();
        CopyButton(localization.Get("MyToolsUi.CopyJson"),()=>tool.Json);
        Tip("MyToolsUi.CopyJsonTip");
        ImGui.SameLine();
        if(myToolRemoveArmed!=tool.Template.Id)
        {
            if(ImGui.SmallButton(localization.Get("MyToolsUi.Remove")))myToolRemoveArmed=tool.Template.Id;
            Tip("MyToolsUi.RemoveTip");
        }
        else if(ImGui.SmallButton(localization.Get("MyToolsUi.RemoveConfirm"))){store.Remove(tool.Template.Id);myToolSelected=null;myToolRemoveArmed=null;}
        // T8: a tool only proposes; the setting changes on this click, and the button at the top of the page brings it back.
        if(tool.Template.Theme is { } theme && configuration.ThemeMode!=ToolTemplateFormat.ThemeMode(theme))
        {
            if(ImGui.SmallButton(localization.Format("MyToolsUi.ThemeApply",ThemeName(theme))+"###MyToolThemeApply"))SaveTheme(ToolTemplateFormat.ThemeMode(theme),configuration.ThemeModeBeforeTool ?? configuration.ThemeMode);
            Tip("MyToolsUi.ThemeApplyTip");
        }
    }

    // 0.477 (user report: no way back to the original theme): always in view on this page while a tool's theme is in use. Without a remembered theme (one applied by 0.476), « original » is the default: follow the game.
    private void DrawMyToolsThemeReturn(ToolTemplateStore store)
    {
        var current=configuration.ThemeMode;
        var fromTool=store.Tools.Any(tool=>tool.Template.Theme is { } theme && ToolTemplateFormat.ThemeMode(theme)==current);
        var target=configuration.ThemeModeBeforeTool ?? (fromTool ? DefaultConfiguration.ThemeMode:current);
        if(target==current)return;
        var name=target==0 ? localization.Get("MyToolsUi.ThemeGame"):localization.Get("Theme."+(UI.PluginTheme)Math.Clamp(target-1,0,Enum.GetValues<UI.PluginTheme>().Length-1));
        if(ImGui.SmallButton(localization.Format("MyToolsUi.ThemeReturn",name)+"###MyToolThemeReturn"))SaveTheme(target);
        Tip("MyToolsUi.ThemeReturnTip");
    }

    private string ThemeName(string theme)=>localization.Get("Theme."+(UI.PluginTheme)(ToolTemplateFormat.ThemeMode(theme)-1));

    private void DrawMyToolsImport(ToolTemplateStore store)
    {
        ImGui.InputTextMultiline("###MyToolsPasted",ref myToolsPasted,120000,new Vector2(-1,ImGui.GetTextLineHeight()*5));
        Tip("MyToolsUi.PasteTip");
        if(ImGui.SmallButton(localization.Get("MyToolsUi.Paste")))myToolsPasted=ImGui.GetClipboardText() ?? "";
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("MyToolsUi.Check")))myToolsPreview=ToolTemplateFormat.Import(myToolsPasted);
        Tip("MyToolsUi.CheckTip");
        if(myToolsPreview is not { } preview)return;
        if(preview.Problem is { } problem)
        {
            ImGui.PushTextWrapPos(0);
            try{ImGui.TextColored(UI.FfxivTheme.Error,localization.Get($"MyToolsErrors.{problem.Code}")+(problem.Where.Length>0 ? $" ({problem.Where})":""));}
            finally{ImGui.PopTextWrapPos();}
            return;
        }
        if(preview.Template is not { } template)return;
        DrawMyToolPreview(store,template);
    }

    // T6: nothing is read before this click.
    private void DrawMyToolsGallery(ToolTemplateStore store)
    {
        if(MyToolsGallery is not { } gallery)return;
        var state=gallery.State;
        ImGui.Separator();
        ImGui.TextDisabled(localization.Get("MyToolsUi.GalleryHelp"));
        if(state.Entries is null && !state.Loading)
        {
            if(ImGui.SmallButton(localization.Get("MyToolsUi.GalleryLoad")))_=MyToolsGallery.LoadListAsync();
            Tip("MyToolsUi.GalleryLoadTip");
        }
        if(state.Loading)ImGui.TextDisabled(localization.Get("MyToolsUi.GalleryLoading"));
        if(state.Problem is { } problem)
        {
            ImGui.PushTextWrapPos(0);
            try{ImGui.TextColored(UI.FfxivTheme.Error,localization.Get($"MyToolsErrors.{problem.Code}"));}
            finally{ImGui.PopTextWrapPos();}
        }
        if(state.Entries is { } entries)
        {
            if(entries.Count==0)ImGui.TextDisabled(localization.Get("MyToolsUi.GalleryEmpty"));
            var language=localization.CurrentCode;
            foreach(var entry in entries)
            {
                ImGui.PushID(entry.Id);
                try
                {
                    ImGui.BeginDisabled(state.Loading);
                    try{if(ImGui.SmallButton(localization.Get("MyToolsUi.GalleryOpen")))_=gallery.OpenAsync(entry);}
                    finally{ImGui.EndDisabled();}
                    Tip("MyToolsUi.GalleryOpenTip");
                    ImGui.SameLine();
                    ImGui.TextUnformatted(entry.Name.For(language));
                    if(entry.Author is { } author){ImGui.SameLine();ImGui.TextDisabled(localization.Format("MyToolsUi.Author",author));}
                    if(store.Tools.Any(tool=>tool.Template.Id==entry.Id)){ImGui.SameLine();ImGui.TextColored(Success,localization.Get("MyToolsUi.GalleryInstalled"));}
                }
                finally{ImGui.PopID();}
            }
        }
        if(state.Opened?.Template is { } opened)
        {
            DrawMyToolPreview(store,opened);
            if(ImGui.SmallButton(localization.Get("MyToolsUi.GalleryCloseTool")))gallery.CloseTool();
        }
    }

    /// <summary>Everything the file will do, before it is installed.</summary>
    private void DrawMyToolPreview(ToolTemplateStore store,ToolTemplate template)
    {
        ImGui.Separator();
        ImGui.TextColored(Accent,localization.Format("MyToolsUi.Preview",template.Name.For(localization.CurrentCode)));
        if(template.Author is { } author)ImGui.TextDisabled(localization.Format("MyToolsUi.Author",author));
        var catalog=itemCatalog.State.Catalog;
        var unknown=catalog is null ? 0:template.Blocks.OfType<ItemListBlock>().SelectMany(block=>block.Items).Count(item=>catalog.Find(item.Id) is null);
        if(unknown>0)ImGui.TextColored(UI.FfxivTheme.Warning,localization.Format("MyToolsUi.UnknownItems",unknown));
        foreach(var host in template.Blocks.OfType<LinkBlock>().Select(link=>ToolTemplateFormat.SafeLink(link.Url)?.IdnHost).OfType<string>().Distinct())
            ImGui.TextColored(UI.FfxivTheme.Warning,localization.Format("MyToolsUi.LinksTo",host));
        if(template.Theme is { } theme)ImGui.TextColored(UI.FfxivTheme.Warning,localization.Format("MyToolsUi.ThemeProposed",ThemeName(theme)));
        var replaces=store.Tools.Any(tool=>tool.Template.Id==template.Id);
        if(replaces)ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("MyToolsUi.Replaces"));
        if(ImGui.SmallButton(localization.Get("MyToolsUi.Install")))
        {
            if(store.Install(template,ToolTemplateFormat.Serialize(template))){myToolSelected=template.Id;myToolsImporting=false;myToolsPreview=null;myToolsPasted="";MyToolsGallery?.CloseTool();return;}
            myToolsPreview=new(null,new("Full"));return;
        }
        ImGui.Separator();
        ImGui.PushID("MyToolPreview");
        try{DrawMyToolBlocks(template,null,store);}
        finally{ImGui.PopID();}
        ImGui.Separator();
    }

    /// <param name="tool">Null: a preview, where nothing can be ticked or counted.</param>
    private void DrawMyToolBlocks(ToolTemplate template,InstalledTool? tool,ToolTemplateStore store)
    {
        var language=localization.CurrentCode;var now=DateTimeOffset.UtcNow;
        for(var index=0;index<template.Blocks.Count;index++)
        {
            ImGui.PushID(index);
            try
            {
                switch(template.Blocks[index])
                {
                    case HeadingBlock heading:
                        ImGui.Spacing();
                        using(TenshoAssistant.UI.UiComponents.PushSectionFont())ImGui.TextColored(Accent,heading.Text.For(language));
                        ImGui.Separator();
                        break;
                    case NoteBlock note:ImGui.TextWrapped(note.Text.For(language));break;
                    case ChecklistBlock list:DrawMyToolChecklist(list,index,tool,store,language,now);break;
                    case CounterBlock counter:DrawMyToolCounter(counter,index,tool,store,language);break;
                    case TimerBlock timer:
                        var left=ToolProgress.NextTime(timer,now)-now;
                        ImGui.TextUnformatted(localization.Format("MyToolsUi.TimerLine",timer.Label.For(language),(int)left.TotalHours,left.Minutes.ToString("00")));
                        break;
                    case ItemListBlock items:DrawMyToolItems(items);break;
                    case LinkBlock link:DrawMyToolLink(link,index,language);break;
                }
            }
            finally{ImGui.PopID();}
        }
    }

    private void DrawMyToolChecklist(ChecklistBlock list,int block,InstalledTool? tool,ToolTemplateStore store,string language,DateTimeOffset now)
    {
        if(list.Reset!=TemplateReset.None)ImGui.TextDisabled(localization.Get(list.Reset==TemplateReset.Daily ? "MyToolsUi.ResetDaily":"MyToolsUi.ResetWeekly"));
        for(var entry=0;entry<list.Entries.Count;entry++)
        {
            ImGui.PushID(entry);
            try
            {
                var key=ToolProgress.Key(block,entry);
                var done=tool is not null && ToolProgress.IsDone(tool,key,list.Reset,now);
                ImGui.BeginDisabled(tool is null);
                try{if(ImGui.Checkbox("###Done",ref done) && tool is not null)store.SetDone(tool.Template.Id,key,done,now);}
                finally{ImGui.EndDisabled();}
                Tip("MyToolsUi.TickTip");
                ImGui.SameLine();ImGui.TextWrapped(list.Entries[entry].For(language));
            }
            finally{ImGui.PopID();}
        }
    }

    private void DrawMyToolCounter(CounterBlock counter,int block,InstalledTool? tool,ToolTemplateStore store,string language)
    {
        var key=ToolProgress.Key(block);
        var count=tool?.Counts.GetValueOrDefault(key) ?? 0;
        ImGui.BeginDisabled(tool is null);
        try
        {
            if(ImGui.SmallButton("-") && tool is not null)store.SetCount(tool.Template.Id,key,count-1);
            ImGui.SameLine();
            if(ImGui.SmallButton("+") && tool is not null)store.SetCount(tool.Template.Id,key,count+1);
        }
        finally{ImGui.EndDisabled();}
        ImGui.SameLine();
        var line=counter.Target>0 ? $"{counter.Label.For(language)} : {count} / {counter.Target}":$"{counter.Label.For(language)} : {count}";
        if(counter.Target>0 && count>=counter.Target)ImGui.TextColored(Success,line);else ImGui.TextUnformatted(line);
    }

    // 0.541: its own session, as the preparation session is cleared when the shown recipe changes.
    private readonly Market.PreparationPriceSession toolPrices=new();
    private void DrawMyToolItems(ItemListBlock block)
    {
        var worldId=market.State.WorldId;var costRows=new List<(int? Missing,decimal? Price)>();var unpriced=new List<uint>();
        if(!ImGui.BeginTable("###MyToolItems",5,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH))return;
        try
        {
            foreach(var column in new[]{"ColumnItem","ColumnWanted","ColumnHeld","ColumnMissing","ColumnPrice"})ImGui.TableSetupColumn(localization.Get("MyToolsUi."+column));
            ImGui.TableHeadersRow();
            RefreshRecipeHoldingsCache();
            var catalog=itemCatalog.State.Catalog;
            foreach(var row in block.Items)
            {
                var item=catalog?.Find(row.Id);var held=GetRecipeHoldings(row.Id);
                ImGui.TableNextRow();ImGui.TableNextColumn();
                DrawItemIcon(item,new Vector2(24));
                if(item is not null && ImGui.IsItemHovered())DrawItemInfoTooltip(row.Id);
                ImGui.SameLine();
                if(item is not null)ImGui.TextUnformatted(item.Name);
                else if(catalog is not null)ImGui.TextColored(UI.FfxivTheme.Warning,localization.Format("MyToolsUi.UnknownItem",row.Id));
                else ImGui.TextDisabled($"ID {row.Id}");
                ImGui.TableNextColumn();ImGui.TextUnformatted(row.Wanted.ToString("N0"));
                ImGui.TableNextColumn();ImGui.TextUnformatted(held.HasObservation ? held.Total.ToString("N0"):"?");
                ImGui.TableNextColumn();
                if(!held.HasObservation)ImGui.TextDisabled("?");
                else
                {
                    var missing=Math.Max(0,row.Wanted-held.Total);
                    ImGui.TextColored(missing>0 ? UI.FfxivTheme.Error:UI.FfxivTheme.Success,missing.ToString("N0"));
                }
                // 0.541 (issue #37): the price of one piece, as the shared cache knows it. Nothing is asked without the button.
                ImGui.TableNextColumn();
                var quote=worldId==0 ? null:market.GetCached(worldId,row.Id);
                var price=quote is null ? null:ToolItemCost.Lowest(quote.Nq.Minimum,quote.Hq.Minimum);
                if(item is {IsMarketable:false})ImGui.TextDisabled("—");
                else if(price is { } known)ImGui.TextUnformatted(known.ToString("N0"));
                else ImGui.TextDisabled("?");
                if(item is {IsMarketable:true})
                {
                    costRows.Add((held.HasObservation ? (int)Math.Clamp(row.Wanted-held.Total,0,int.MaxValue):null,price));
                    if(quote is null || !market.IsFresh(quote,DateTimeOffset.UtcNow))unpriced.Add(row.Id);
                }
            }
        }
        finally{ImGui.EndTable();}
        var (cost,unknown)=ToolItemCost.Missing(costRows);
        if(cost>0 || unknown>0)ImGui.TextDisabled(localization.Format(unknown>0 ? "MyToolsUi.MissingCostUnknown":"MyToolsUi.MissingCost",cost.ToString("N0"),unknown));
        var character=bankBridge.State.CharacterId;var batch=toolPrices.State;
        if(batch.Loading)ImGui.TextDisabled(localization.Format("MyToolsUi.PricesLoading",batch.Completed,batch.Items.Count));
        else if(unpriced.Count>0 && character!=0 && worldId!=0)
        {
            if(SourceButton(localization.Format("MyToolsUi.AskPrices",Math.Min(unpriced.Count,ToolItemCost.MaximumAsked)),SourceOn(SourceUse.UniversalisToolPrices),true))
                try{_=toolPrices.StartListAsync(market,character,worldId,unpriced.Take(ToolItemCost.MaximumAsked));}
                catch(Exception error){log.Warning(error,"My Tools prices could not be started.");}
            Tip("MyToolsUi.AskPricesTip");
        }
    }

    private void DrawMyToolLink(LinkBlock link,int block,string language)
    {
        var id=$"{myToolSelected}:{block}";
        if(ImGui.SmallButton(SafeLabel(link.Label.For(language))+"###Link"))myToolPendingLink=myToolPendingLink==id ? null:id;
        if(ImGui.IsItemHovered())WrappedTooltip(link.Url);
        if(myToolPendingLink!=id || ToolTemplateFormat.SafeLink(link.Url) is not { } uri)return;
        ImGui.PushTextWrapPos(0);
        try{ImGui.TextColored(UI.FfxivTheme.Warning,localization.Format("MyToolsUi.LinkConfirm",uri.IdnHost,link.Url));}
        finally{ImGui.PopTextWrapPos();}
        if(ImGui.SmallButton(localization.Get("MyToolsUi.LinkOpen"))){Dalamud.Utility.Util.OpenLink(link.Url);myToolPendingLink=null;}
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("MyToolsUi.LinkCancel")))myToolPendingLink=null;
    }
}
