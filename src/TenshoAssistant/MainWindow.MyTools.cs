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

    private static string SafeLabel(string text)=>text.Replace("##","# #");

    private void DrawMyTools()
    {
        TenshoAssistant.UI.UiComponents.SectionHeader(localization.Get("MyToolsUi.Title"),localization.Get("MyToolsUi.Help"));
        if(MyToolsStore is not { } store)return;
        if(store.Unavailable){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("MyToolsUi.Unavailable"));return;}
        if(store.RefusedCount>0)ImGui.TextColored(UI.FfxivTheme.Warning,localization.Format("MyToolsUi.Refused",store.RefusedCount));
        if(ImGui.SmallButton(localization.Get(myToolsImporting ? "MyToolsUi.ImportClose":"MyToolsUi.Import"))){myToolsImporting=!myToolsImporting;myToolsPreview=null;}
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("MyToolsUi.ImportTip"));
        if(myToolsImporting)DrawMyToolsImport(store);
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
        if(ImGui.SmallButton(localization.Get("MyToolsUi.Share")))ImGui.SetClipboardText(ToolTemplateFormat.Encode(tool.Json));
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("MyToolsUi.ShareTip"));
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("MyToolsUi.CopyJson")))ImGui.SetClipboardText(tool.Json);
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("MyToolsUi.CopyJsonTip"));
        ImGui.SameLine();
        if(myToolRemoveArmed!=tool.Template.Id)
        {
            if(ImGui.SmallButton(localization.Get("MyToolsUi.Remove")))myToolRemoveArmed=tool.Template.Id;
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("MyToolsUi.RemoveTip"));
        }
        else if(ImGui.SmallButton(localization.Get("MyToolsUi.RemoveConfirm"))){store.Remove(tool.Template.Id);myToolSelected=null;myToolRemoveArmed=null;}
    }

    private void DrawMyToolsImport(ToolTemplateStore store)
    {
        ImGui.InputTextMultiline("###MyToolsPasted",ref myToolsPasted,120000,new Vector2(-1,ImGui.GetTextLineHeight()*5));
        Tip("MyToolsUi.PasteTip");
        if(ImGui.SmallButton(localization.Get("MyToolsUi.Paste")))myToolsPasted=ImGui.GetClipboardText() ?? "";
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("MyToolsUi.Check")))myToolsPreview=ToolTemplateFormat.Import(myToolsPasted);
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("MyToolsUi.CheckTip"));
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
        var replaces=store.Tools.Any(tool=>tool.Template.Id==template.Id);
        if(replaces)ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("MyToolsUi.Replaces"));
        if(ImGui.SmallButton(localization.Get("MyToolsUi.Install")))
        {
            if(store.Install(template,ToolTemplateFormat.Serialize(template))){myToolSelected=template.Id;myToolsImporting=false;myToolsPreview=null;myToolsPasted="";return;}
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

    private void DrawMyToolItems(ItemListBlock block)
    {
        if(!ImGui.BeginTable("###MyToolItems",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH))return;
        try
        {
            foreach(var column in new[]{"ColumnItem","ColumnWanted","ColumnHeld","ColumnMissing"})ImGui.TableSetupColumn(localization.Get("MyToolsUi."+column));
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
            }
        }
        finally{ImGui.EndTable();}
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
