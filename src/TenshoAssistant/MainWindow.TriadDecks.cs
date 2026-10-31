using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;
using TenshoAssistant.MiniGames;

namespace TenshoAssistant;

/// <summary>
/// Tensho deck list (0.162): decks generated for an NPC are saved under the NPC's name, listed in Triple Triad › Deck with their cards,
/// and can be deleted. While the game's deck editor is open, a reminder panel beside it shows the chosen deck's cards in large (icon,
/// card number, name, sides) so they are easy to find in the game's list, as TriadBuddy highlights them.
/// </summary>
internal sealed partial class MainWindow
{
    private string? triadShownDeck;

    private bool SaveTriadDecks(Action<List<TriadSavedDeck>> change)
    {
        var previous=configuration.TripleTriadDecks;
        var copy=(previous ?? []).Select(deck=>new TriadSavedDeck{Name=deck.Name,NpcId=deck.NpcId,Cards=deck.Cards.ToList()}).ToList();
        change(copy);
        configuration.TripleTriadDecks=copy;
        try{save();return true;}
        catch(Exception error){configuration.TripleTriadDecks=previous ?? [];log.Warning(error,"Triple Triad decks could not be saved.");return false;}
    }
    private void SaveGeneratedDeck(TriadRequestInfo request,TriadDeckSuggestion generated)
    {
        var name=request.NpcName.Length>0 ? request.NpcName:localization.Format("TriadDecks.NpcFallback",request.NpcId);
        if(SaveTriadDecks(decks=>
        {
            decks.RemoveAll(deck=>request.NpcId!=0 ? deck.NpcId==request.NpcId:deck.Name==name);
            decks.Add(new TriadSavedDeck{Name=name,NpcId=request.NpcId,Cards=generated.Deck.Select(card=>card.Id).ToList()});
        }))triadShownDeck=name;
    }
    // Tooltip of every card icon (0.198): the card item's full sheet (« Comment l'obtenir », links) when the card has an item, else its name.
    private void TriadCardTooltip(uint id)
    {
        if(!ImGui.IsItemHovered())return;
        if(triadNpcs?.State.Catalog?.Cards.GetValueOrDefault(id) is { ItemId:>0 } source)DrawItemInfoTooltip(source.ItemId);
        else WrappedTooltip(TriadCardName(id));
    }
    private string TriadCardName(uint id)=>collections.State.Entries.FirstOrDefault(entry=>entry.Kind==CollectionKind.TripleTriad && entry.Id==id)?.Name ?? id.ToString();

    // Triple Triad › Deck: the saved decks, each with its cards; « Show » picks the deck for the editor reminder, « Delete » removes it.
    private void DrawTriadSavedDecks()
    {
        ImGui.TextColored(Accent,localization.Get("TriadDecks.Title"));
        var decks=configuration.TripleTriadDecks ?? [];
        if(decks.Count==0){ImGui.TextDisabled(localization.Get("TriadDecks.Empty"));ImGui.Separator();return;}
        string? remove=null;
        foreach(var deck in decks)
        {
            ImGui.PushID("TriadSaved"+deck.Name);
            try
            {
                var shown=deck.Name==(triadShownDeck ?? decks[^1].Name);
                ImGui.TextColored(shown ? Success:Muted,(shown ? "★ ":"")+deck.Name);
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("TriadDecks.Show")))triadShownDeck=deck.Name;
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("TriadDecks.Delete")))remove=deck.Name;
                foreach(var id in deck.Cards)
                {
                    if(TriadCard(id) is { } card)DrawSmallGameIcon(card.IconId);
                    TriadCardTooltip(id);
                    ImGui.SameLine();
                }
                ImGui.NewLine();
            }
            finally{ImGui.PopID();}
        }
        if(remove is { } name && SaveTriadDecks(list=>list.RemoveAll(deck=>deck.Name==name)) && triadShownDeck==name)triadShownDeck=null;
        ImGui.Separator();
    }

    // Highlight in the game's deck editor (0.164), like TriadBuddy: every card of the chosen Tensho deck shown in the list gets a green
    // frame, following the scroll; the chosen deck is the one picked in Triple Triad › Deck (default: the last saved).
    internal void DrawTriadDeckReminder()
    {
        if(windowDisposed || triadObservation.State is not { Enabled:true } || triadObservation.EditDeckWindow is null)return;
        var decks=configuration.TripleTriadDecks ?? [];
        if(decks.Count==0)return;
        var deck=decks.FirstOrDefault(entry=>entry.Name==triadShownDeck) ?? decks[^1];
        var draw=ImGui.GetForegroundDrawList();
        var fill=ImGui.GetColorU32(Success with { W=0.25f });var border=ImGui.GetColorU32(Success);
        foreach(var (id,rect) in triadObservation.EditDeckCards)
        {
            if(!deck.Cards.Contains(id) || !rect.Valid)continue;
            var min=new Vector2(rect.X-3,rect.Y-3);var max=new Vector2(rect.X+rect.Width+3,rect.Y+rect.Height+3);
            draw.AddRectFilled(min,max,fill,4);draw.AddRect(min,max,border,4,ImDrawFlags.None,3);
        }
        // Which deck is highlighted, above the editor.
        if(triadObservation.EditDeckWindow is { Valid:true } window)
            draw.AddText(new Vector2(window.X+12,window.Y-ImGui.GetTextLineHeight()-4),border,localization.Format("TriadDecks.Highlighted",deck.Name));
    }
}
