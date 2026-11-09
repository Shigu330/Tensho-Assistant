using Dalamud.Bindings.ImGui;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// g, I-08 (0.347): the « Note » tab of the item sheet (note, « important », « to sell to an NPC »), the note under the item
// information (hover and sheet), and the two shop reminder settings. Saved like a setting; the dictionary is replaced whole.
internal sealed partial class MainWindow
{
    private uint itemNoteEditing;
    private string itemNoteText="";

    private void DrawItemNoteTab(uint id)
    {
        var notes=configuration.ItemNotes;
        var note=notes?.GetValueOrDefault(id) ?? new ItemNote("",false,false);
        if(itemNoteEditing!=id){itemNoteEditing=id;itemNoteText=note.Text;}
        ImGui.TextWrapped(localization.Get("ItemNotes.Help"));
        ImGui.InputTextMultiline("##ItemNote",ref itemNoteText,ItemNotes.MaximumText,new System.Numerics.Vector2(-1,ImGui.GetTextLineHeight()*4));
        if(ImGui.IsItemDeactivatedAfterEdit())SaveItemNote(id,note with { Text=itemNoteText });
        Tip("ItemNotes.TextTip");
        var important=note.Important;
        if(ImGui.Checkbox(localization.Get("ItemNotes.Important")+"##NoteImportant",ref important))SaveItemNote(id,note with { Important=important,Text=itemNoteText });
        Tip("ItemNotes.ImportantTip");
        ImGui.SameLine();
        var toSell=note.ToSell;
        if(ImGui.Checkbox(localization.Get("ItemNotes.ToSell")+"##NoteToSell",ref toSell))SaveItemNote(id,note with { ToSell=toSell,Text=itemNoteText });
        Tip("ItemNotes.ToSellTip");
        ImGui.Separator();
        BoolSetting("ItemNotes.ShopReminder","ItemNotes.ShopReminderTip",()=>configuration.ItemNoteShopReminder,value=>configuration.ItemNoteShopReminder=value,c=>c.ItemNoteShopReminder);
        // X-76 step 3 (0.952): the sound is played only by the shop reminder, greyed while it is off.
        ImGui.Indent();
        ImGui.BeginDisabled(!configuration.ItemNoteShopReminder);
        try{BoolSetting("ItemNotes.ImportantSound","ItemNotes.ImportantSoundTip",()=>configuration.ItemNoteImportantSound,value=>configuration.ItemNoteImportantSound=value,c=>c.ItemNoteImportantSound);}
        finally{ImGui.EndDisabled();ImGui.Unindent();}
        ImGui.TextDisabled(localization.Format("ItemNotes.Count",notes?.Count ?? 0,ItemNotes.MaximumItems));
    }

    private void SaveItemNote(uint id,ItemNote note)
    {
        if(ItemNotes.With(configuration.ItemNotes,id,note) is not { } next){saveError=localization.Get("ItemNotes.Full");return;}
        SaveSetting("ItemNotes.Tab",()=>configuration.ItemNotes,value=>configuration.ItemNotes=value,next.Count==0 ? null:next);
    }

    // Under the item information: the player's note and labels, in gold.
    private void DrawItemNoteLine(uint id)
    {
        if(configuration.ItemNotes?.GetValueOrDefault(id) is not { } note)return;
        var labels=new List<string>();
        if(note.Important)labels.Add(localization.Get("ItemNotes.Important"));
        if(note.ToSell)labels.Add(localization.Get("ItemNotes.ToSell"));
        if(labels.Count>0)ImGui.TextColored(UI.FfxivTheme.Gold,string.Join(" · ",labels));
        if(note.Text.Trim().Length>0)ImGui.TextWrapped(localization.Format("ItemNotes.Line",note.Text.Trim()));
    }
}
