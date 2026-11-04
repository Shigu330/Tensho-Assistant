using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.MyTools;

namespace TenshoAssistant;

// TEMPLATES-001: the editor of « My tools ». A tool is composed block by block in the player's language; « Save » goes through the strict format, as a shared file does.
internal sealed partial class MainWindow
{
    private ToolDraft? myToolDraft;
    private TemplateProblem? myToolDraftProblem;

    private bool DrawTextField(string labelKey,string tipKey,TemplateText text,int maximum,out TemplateText changed,bool multiline=false)
    {
        var language=localization.CurrentCode;
        var value=text.For(language);
        bool edited;
        if(multiline)
        {
            ImGui.TextUnformatted(localization.Get(labelKey));
            edited=ImGui.InputTextMultiline("###"+labelKey,ref value,maximum,new Vector2(-1,ImGui.GetTextLineHeight()*4));
            Tip(tipKey);
        }
        else
        {
            ImGui.SetNextItemWidth(320);
            edited=ImGui.InputText(localization.Get(labelKey)+"###"+labelKey,ref value,maximum);
            Tip(tipKey);
        }
        changed=edited ? ToolDraft.With(text,language,value):text;
        return edited;
    }

    private void DrawMyToolEditor(ToolTemplateStore store,ToolDraft draft)
    {
        ImGui.Separator();
        ImGui.TextDisabled(localization.Get("MyToolsEditor.Language"));
        if(DrawTextField("MyToolsEditor.Name","MyToolsEditor.NameTip",draft.Name,ToolTemplateFormat.MaxLabel,out var name))draft.Name=name;
        var author=draft.Author;
        ImGui.SetNextItemWidth(320);
        if(ImGui.InputText(localization.Get("MyToolsEditor.Author")+"###MyToolAuthor",ref author,ToolTemplateFormat.MaxAuthor))draft.Author=author;
        Tip("MyToolsEditor.AuthorTip");
        var free=draft.FreeToShare;
        if(ImGui.Checkbox(localization.Get("MyToolsEditor.Free")+"###MyToolFree",ref free))draft.FreeToShare=free;
        Tip("MyToolsEditor.FreeTip");
        for(var index=0;index<draft.Blocks.Count;index++)
        {
            ImGui.PushID(index);
            try
            {
                ImGui.Separator();
                ImGui.TextColored(Accent,localization.Format("MyToolsEditor.BlockTitle",index+1,localization.Get("MyToolsEditor.Kind"+KindOf(draft.Blocks[index]))));
                ImGui.SameLine();
                if(ImGui.SmallButton("^"))draft.Move(index,-1);
                if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("MyToolsEditor.UpTip"));
                ImGui.SameLine();
                if(ImGui.SmallButton("v"))draft.Move(index,1);
                if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("MyToolsEditor.DownTip"));
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("MyToolsEditor.RemoveBlock"))){draft.Blocks.RemoveAt(index);return;}
                draft.Blocks[index]=DrawMyToolBlockEditor(draft.Blocks[index]);
            }
            finally{ImGui.PopID();}
        }
        ImGui.Separator();
        ImGui.TextUnformatted(localization.Get("MyToolsEditor.AddBlock"));
        foreach(var kind in Enum.GetValues<ToolBlockKind>())
        {
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get($"MyToolsEditor.Kind{kind}")+$"###AddBlock{kind}") && draft.Blocks.Count<ToolTemplateFormat.MaxBlocks)draft.Blocks.Add(ToolDraft.NewBlock(kind));
        }
        ImGui.Separator();
        if(myToolDraftProblem is { } problem)
        {
            ImGui.PushTextWrapPos(0);
            try{ImGui.TextColored(UI.FfxivTheme.Error,localization.Get($"MyToolsErrors.{problem.Code}")+(problem.Where.Length>0 ? $" ({problem.Where})":""));}
            finally{ImGui.PopTextWrapPos();}
        }
        if(ImGui.SmallButton(localization.Get("MyToolsEditor.Save")))
        {
            var check=draft.Check();
            myToolDraftProblem=check.Problem;
            if(check.Template is { } template)
            {
                if(store.Install(template,ToolTemplateFormat.Serialize(template))){myToolSelected=template.Id;myToolDraft=null;}
                else myToolDraftProblem=new("Full");
            }
        }
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("MyToolsEditor.SaveTip"));
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("MyToolsEditor.Cancel"))){myToolDraft=null;myToolDraftProblem=null;}
    }

    private static ToolBlockKind KindOf(TemplateBlock block)=>block switch
    {
        HeadingBlock=>ToolBlockKind.Heading,NoteBlock=>ToolBlockKind.Note,ChecklistBlock=>ToolBlockKind.Checklist,CounterBlock=>ToolBlockKind.Counter,
        TimerBlock=>ToolBlockKind.Timer,ItemListBlock=>ToolBlockKind.Items,LinkBlock=>ToolBlockKind.Link,_=>throw new ArgumentOutOfRangeException(nameof(block)),
    };

    private TemplateBlock DrawMyToolBlockEditor(TemplateBlock block)
    {
        switch(block)
        {
            case HeadingBlock heading:
                return DrawTextField("MyToolsEditor.Text","MyToolsEditor.TextTip",heading.Text,ToolTemplateFormat.MaxLabel,out var title) ? heading with {Text=title}:heading;
            case NoteBlock note:
                return DrawTextField("MyToolsEditor.Note","MyToolsEditor.NoteTip",note.Text,ToolTemplateFormat.MaxNote,out var body,true) ? note with {Text=body}:note;
            case ChecklistBlock list:return DrawMyToolChecklistEditor(list);
            case CounterBlock counter:
            {
                if(DrawTextField("MyToolsEditor.Label","MyToolsEditor.LabelTip",counter.Label,ToolTemplateFormat.MaxLabel,out var label))counter=counter with {Label=label};
                var target=counter.Target;
                ImGui.SetNextItemWidth(120);
                if(ImGui.InputInt(localization.Get("MyToolsEditor.Target")+"###MyToolTarget",ref target,0))counter=counter with {Target=Math.Clamp(target,0,ToolTemplateFormat.MaxTarget)};
                Tip("MyToolsEditor.TargetTip");
                return counter;
            }
            case TimerBlock timer:
            {
                if(DrawTextField("MyToolsEditor.Label","MyToolsEditor.LabelTip",timer.Label,ToolTemplateFormat.MaxLabel,out var label))timer=timer with {Label=label};
                var at=(int)timer.Timer;
                var names=Enum.GetValues<TemplateTimer>().Select(value=>localization.Get($"MyToolsEditor.Timer{value}")).ToArray();
                ImGui.SetNextItemWidth(320);
                if(ImGui.Combo(localization.Get("MyToolsEditor.TimerAt")+"###MyToolTimer",ref at,names))timer=timer with {Timer=(TemplateTimer)at};
                Tip("MyToolsEditor.TimerAtTip");
                if(timer.Timer==TemplateTimer.Hour)
                {
                    var hour=timer.HourUtc;
                    ImGui.SetNextItemWidth(120);
                    if(ImGui.InputInt(localization.Get("MyToolsEditor.HourUtc")+"###MyToolHour",ref hour,0))timer=timer with {HourUtc=Math.Clamp(hour,0,23)};
                    Tip("MyToolsEditor.HourUtcTip");
                }
                return timer;
            }
            case ItemListBlock items:return DrawMyToolItemsEditor(items);
            case LinkBlock link:
            {
                if(DrawTextField("MyToolsEditor.Label","MyToolsEditor.LabelTip",link.Label,ToolTemplateFormat.MaxLabel,out var label))link=link with {Label=label};
                var url=link.Url;
                ImGui.SetNextItemWidth(320);
                if(ImGui.InputText(localization.Get("MyToolsEditor.Url")+"###MyToolUrl",ref url,ToolTemplateFormat.MaxLink))link=link with {Url=url};
                Tip("MyToolsEditor.UrlTip");
                return link;
            }
            default:return block;
        }
    }

    private ChecklistBlock DrawMyToolChecklistEditor(ChecklistBlock list)
    {
        var reset=(int)list.Reset;
        var names=Enum.GetValues<TemplateReset>().Select(value=>localization.Get($"MyToolsEditor.Reset{value}")).ToArray();
        ImGui.SetNextItemWidth(320);
        if(ImGui.Combo(localization.Get("MyToolsEditor.Reset")+"###MyToolReset",ref reset,names))list=list with {Reset=(TemplateReset)reset};
        Tip("MyToolsEditor.ResetTip");
        var entries=list.Entries.ToList();
        for(var index=0;index<entries.Count;index++)
        {
            ImGui.PushID(index);
            try
            {
                if(DrawTextField("MyToolsEditor.Entry","MyToolsEditor.EntryTip",entries[index],ToolTemplateFormat.MaxLabel,out var entry))entries[index]=entry;
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("MyToolsEditor.RemoveRow"))){entries.RemoveAt(index);break;}
            }
            finally{ImGui.PopID();}
        }
        if(entries.Count<ToolTemplateFormat.MaxEntries && ImGui.SmallButton(localization.Get("MyToolsEditor.AddEntry")))entries.Add(ToolDraft.Empty);
        return list with {Entries=entries};
    }

    private ItemListBlock DrawMyToolItemsEditor(ItemListBlock block)
    {
        var rows=block.Items.ToList();var catalog=itemCatalog.State.Catalog;
        for(var index=0;index<rows.Count;index++)
        {
            ImGui.PushID(index);
            try
            {
                var item=catalog?.Find(rows[index].Id);
                DrawItemIcon(item,new Vector2(24));
                if(item is not null && ImGui.IsItemHovered())DrawItemInfoTooltip(rows[index].Id);
                ImGui.SameLine();
                ImGui.TextUnformatted(item?.Name ?? localization.Format("MyToolsUi.UnknownItem",rows[index].Id));
                ImGui.SameLine();
                var wanted=rows[index].Wanted;
                ImGui.SetNextItemWidth(90);
                if(ImGui.InputInt(localization.Get("MyToolsUi.ColumnWanted")+"###MyToolWanted",ref wanted,0))rows[index]=rows[index] with {Wanted=Math.Clamp(wanted,1,ToolTemplateFormat.MaxWanted)};
                Tip("MyToolsEditor.WantedTip");
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("MyToolsEditor.RemoveRow"))){rows.RemoveAt(index);break;}
            }
            finally{ImGui.PopID();}
        }
        // The item selected in Tensho (Objects page, or any item's menu) is the way to add one: an identifier is never typed from memory.
        if(SelectedItem is { } selected && rows.Count<ToolTemplateFormat.MaxItems && rows.All(row=>row.Id!=selected.Id))
        {
            if(ImGui.SmallButton(localization.Format("MyToolsEditor.AddItem",selected.Name)+"###MyToolAddItem"))rows.Add(new(selected.Id,1));
        }
        else if(SelectedItem is null)ImGui.TextDisabled(localization.Get("MyToolsEditor.SelectItem"));
        return block with {Items=rows};
    }
}
