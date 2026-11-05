using Dalamud.Bindings.ImGui;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// 0.607: Items › the annotated items: what has a note or a label, filtered by label and text; a click opens the item. Built again only when something changes.
internal sealed partial class MainWindow
{
    private int annotatedFilter;
    private string annotatedQuery="";
    private (object? Notes,object? Items,int Filter,string Query,AnnotatedItem[] Rows)? annotatedView;

    private void DrawAnnotatedItems()
    {
        var notes=configuration.ItemNotes;
        if(notes is not {Count:>0})return;
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("ItemNotes.ListTitle",notes.Count)+"###AnnotatedItems"))return;
        ImGui.SetNextItemWidth(170);
        ImGui.Combo(localization.Get("ItemNotes.ListFilter")+"###AnnotatedFilter",ref annotatedFilter,[localization.Get("ItemNotes.ListAll"),localization.Get("ItemNotes.Important"),localization.Get("ItemNotes.ToSell")]);Tip("ItemNotes.ListFilterTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("###AnnotatedQuery",localization.Get("ItemNotes.ListSearch"),ref annotatedQuery,80);Tip("ItemNotes.ListSearchTip");
        var items=itemCatalog.State.Catalog;
        if(annotatedView is not { } view || !ReferenceEquals(view.Notes,notes) || !ReferenceEquals(view.Items,items) || view.Filter!=annotatedFilter || view.Query!=annotatedQuery)
            annotatedView=view=(notes,items,annotatedFilter,annotatedQuery,ItemNotes.Annotated(notes,(AnnotatedFilter)annotatedFilter,annotatedQuery,id=>items?.Find(id)?.Name ?? localization.Format("BankUi.Item",id)));
        var rows=view.Rows;
        if(rows.Length==0){ImGui.TextDisabled(localization.Get("ItemNotes.ListEmpty"));return;}
        DrawChainList("AnnotatedList",rows.Length,index=>
        {
            var row=rows[index];
            ImGui.PushID((int)row.ItemId);
            try
            {
                DrawChainItem(row.ItemId);
                ImGui.SameLine();
                ImGui.TextDisabled((row.Note.Important ? localization.Get("ItemNotes.Important")+" · ":"")+(row.Note.ToSell ? localization.Get("ItemNotes.ToSell")+" · ":"")+row.Note.Text.ReplaceLineEndings(" "));
                if(ImGui.IsItemHovered() && row.Note.Text.Length>0)WrappedTooltip(row.Note.Text);
            }
            finally{ImGui.PopID();}
        });
    }
}
