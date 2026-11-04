using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;
using TenshoAssistant.MiniGames;

namespace TenshoAssistant;

/// <summary>
/// TRIAD-003 (0.161), as TriadBuddy: a panel beside the NPC challenge window with the estimated win rate of each saved deck (the game's
/// decks and the Tensho deck) and a deck generated from the owned cards for this NPC and these rules. Simulations run off the framework
/// thread; nothing is written to the game (the player builds the deck in the game's deck editor). In the deck choice window (0.165)
/// the estimated win rate is written beside each deck row and the best deck is framed in green.
/// </summary>
internal sealed partial class MainWindow
{
    private readonly object triadOddsGate=new();
    private CancellationTokenSource? triadOddsCancel,triadGenerateCancel;
    private string triadOddsKey=string.Empty;
    // 0.331 (audit): keyed by the deck's position in TriadRequestDecks (two game decks can share a name); triadOddsKey holds every
    // name and card in order, so a position always means the same deck.
    private Dictionary<int,TriadDeckOdds?> triadOdds=[];
    private TriadDeckSuggestion? triadGenerated;
    private bool triadGenerating;
    private string triadGeneratedKey=string.Empty;

    private List<(string Name,IReadOnlyList<TriadCardDefinition>? Deck)> TriadRequestDecks(TriadRequestInfo request)
    {
        var decks=request.Decks.Select(deck=>(deck.Name.Length>0 ? deck.Name:localization.Format("TriadRequest.GameDeck",deck.Index+1),Resolve(deck.Cards))).ToList();
        if(configuration.TripleTriadDeck is { Count:5 } own)decks.Add((localization.Get("TriadRequest.TenshoDeck"),Resolve(own)));
        foreach(var saved in configuration.TripleTriadDecks ?? [])decks.Add(("Tensho · "+saved.Name,Resolve(saved.Cards)));
        return decks;
        IReadOnlyList<TriadCardDefinition>? Resolve(IEnumerable<uint> ids)
        {
            var cards=ids.Where(id=>id!=0).Select(id=>TriadCard(id)).ToList();
            return cards.Count==5 && cards.All(card=>card is not null) ? cards.Select(card=>card!).ToList().AsReadOnly():null;
        }
    }
    private List<TriadCardDefinition> OwnedTriadCards()=>collections.State.Entries
        .Where(entry=>entry.Kind==CollectionKind.TripleTriad && entry.ConfirmedUnlocked && entry.Card is not null).Select(entry=>entry.Card!).ToList();
    private static string TriadRequestKey(TriadRequestInfo request)=>$"{request.NpcId}|{request.RulesKnown}|{string.Join('.',request.RuleIds)}";

    internal void DrawTriadRequestPanel()
    {
        // 0.327: its own switch (TriadChallengePanel, on by default); off, no panel, no deck choice odds and no simulation started.
        if(windowDisposed || !configuration.TriadChallengePanel || triadObservation.State is not { Enabled:true } || triadObservation.Request is not { } request)return;
        var npcFixed=request.NpcFixed.Select(id=>TriadCard(id)).OfType<TriadCardDefinition>().ToList();
        var npcVariable=request.NpcVariable.Select(id=>TriadCard(id)).OfType<TriadCardDefinition>().ToList();
        var rules=TriadMatchAdvisor.RulesOf(request.RuleIds);var forced=TriadMatchSimulator.ForcedOf(request.RuleIds);
        var decks=TriadRequestDecks(request);
        // 0.491 (R-14): no odds for a match whose rules could not be read.
        if(request.RulesKnown)
        {
            StartTriadOdds(request,decks,npcFixed,npcVariable,rules,forced);
            DrawTriadSelDeckOdds(decks);
        }
        if(!request.Window.Valid)return;
        Panels?.Show("TenshoTriadRequest",localization.Get("TriadRequest.Title"),ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.AlwaysAutoResize|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse,()=>{ImGui.SetNextWindowPos(new Vector2(request.Window.X+request.Window.Width+8,request.Window.Y),ImGuiCond.Always);ImGui.SetNextWindowSizeConstraints(new Vector2(300,0),new Vector2(420,900));},()=>
        {
            ImGui.TextColored(Accent,request.NpcName.Length>0 ? request.NpcName:localization.Get("TriadRequest.UnknownNpc"));
            if(!request.RulesKnown){ImGui.TextWrapped(localization.Get("TriadRequest.RulesUnknown"));return;}
            ImGui.TextWrapped(localization.Format("TriadRequest.Rules",request.RuleNames is { Count:>0 } names ? string.Join(", ",names):localization.Get("TriadRequest.NoRules")));
            if(request.RuleIds.Contains(1u))ImGui.TextWrapped(localization.Get("TriadRequest.Roulette"));
            if(request.RuleIds.Contains(7u))ImGui.TextWrapped(localization.Get("TriadRequest.Random"));
            if(npcFixed.Count+npcVariable.Count<5){ImGui.TextWrapped(localization.Get("TriadRequest.NoNpcDeck"));return;}
            ImGui.Separator();
            ImGui.TextDisabled(localization.Get("TriadRequest.Odds"));
            if(decks.Count==0)ImGui.TextDisabled(localization.Get("TriadRequest.NoDecks"));
            Dictionary<int,TriadDeckOdds?> odds;lock(triadOddsGate)odds=triadOdds;
            for(var index=0;index<decks.Count;index++)
            {
                var (name,deck)=decks[index];
                if(deck is null){ImGui.TextDisabled(localization.Format("TriadRequest.DeckUnknown",name));continue;}
                ImGui.TextUnformatted(odds.TryGetValue(index,out var value) && value is { } result
                    ? localization.Format("TriadRequest.DeckOdds",name,(result.Win*100).ToString("0"),(result.Draw*100).ToString("0"))
                    : localization.Format("TriadRequest.DeckCalculating",name));
            }
            ImGui.Separator();
            var owned=OwnedTriadCards();
            ImGui.BeginDisabled(triadGenerating || owned.Count<5);
            try
            {
                if(ImGui.Button(localization.Get("TriadRequest.Generate")))
                    StartTriadGenerate(request,owned,npcFixed,npcVariable,rules,forced,decks.Select(deck=>deck.Deck).OfType<IReadOnlyList<TriadCardDefinition>>().ToList());
            }
            finally{ImGui.EndDisabled();}
            // 0.331 (audit): collections are read on their own at login; « still being read » only while they are, else too few confirmed.
            if(owned.Count<5)ImGui.TextDisabled(localization.Get(collections.State.Loading ? "TriadRequest.NoOwned":"TriadRequest.FewOwned"));
            if(triadGenerating)ImGui.TextDisabled(localization.Get("TriadRequest.Generating"));
            if(triadGenerated is { } generated && triadGeneratedKey==TriadRequestKey(request))
            {
                ImGui.TextColored(Success,localization.Format("TriadRequest.GeneratedOdds",(generated.Odds.Win*100).ToString("0"),(generated.Odds.Draw*100).ToString("0")));
                foreach(var card in generated.Deck)
                {
                    DrawSmallGameIcon(card.IconId);
                    TriadCardTooltip(card.Id);
                    var name=CollectionEntryOf(CollectionKind.TripleTriad,card.Id)?.Name ?? card.Id.ToString();
                    ImGui.TextUnformatted($"{name}  {new string('★',card.Stars)}  {TriadCardDefinition.Value(card.Top)} {TriadCardDefinition.Value(card.Right)} {TriadCardDefinition.Value(card.Bottom)} {TriadCardDefinition.Value(card.Left)}");
                }
                if(ImGui.Button(localization.Get("TriadRequest.SaveTensho")))SaveGeneratedDeck(request,generated);
                ImGui.TextWrapped(localization.Get("TriadRequest.BuildInGame"));
            }
            ImGui.TextDisabled(localization.Get("TriadRequest.Estimate"));
        });
    }
    // Deck choice window (0.165), like TriadBuddy: estimated win rate written beside each deck row, best deck framed in green.
    private void DrawTriadSelDeckOdds(List<(string Name,IReadOnlyList<TriadCardDefinition>? Deck)> decks)
    {
        var rows=triadObservation.SelDeckRows;
        if(rows.Count==0)return;
        Dictionary<int,TriadDeckOdds?> odds;lock(triadOddsGate)odds=triadOdds;
        var found=new List<(TriadRect Row,TriadDeckOdds Odds)>();
        foreach(var (cards,row) in rows)
        {
            var match=decks.FindIndex(deck=>deck.Deck is { } list && list.Select(card=>card.Id).Order().SequenceEqual(cards.Order()));
            if(match>=0 && odds.TryGetValue(match,out var value) && value is { } result)found.Add((row,result));
        }
        if(found.Count==0)return;
        var draw=ImGui.GetForegroundDrawList();var best=found.Max(entry=>entry.Odds.Score);
        foreach(var (row,result) in found)
        {
            var text=localization.Format("TriadRequest.RowOdds",(result.Win*100).ToString("0"),(result.Draw*100).ToString("0"));
            var top=best-result.Score<1e-9;var color=ImGui.GetColorU32(top ? Success:Accent);
            var at=new Vector2(row.X+row.Width+14,row.Y+row.Height/2-ImGui.GetTextLineHeight()/2);
            var size=ImGui.CalcTextSize(text);
            draw.AddRectFilled(at-new Vector2(4,2),at+size+new Vector2(4,2),ImGui.GetColorU32(new Vector4(0,0,0,0.75f)),4);
            draw.AddText(at,color,text);
            if(top)draw.AddRect(new Vector2(row.X-5,row.Y-5),new Vector2(row.X+row.Width+5,row.Y+row.Height+5),color,6,ImDrawFlags.None,3);
        }
    }
    private void StartTriadOdds(TriadRequestInfo request,List<(string Name,IReadOnlyList<TriadCardDefinition>? Deck)> decks,
        List<TriadCardDefinition> npcFixed,List<TriadCardDefinition> npcVariable,TriadRules rules,TriadForcedCards forced)
    {
        var key=TriadRequestKey(request)+"|"+string.Join(";",decks.Select(deck=>deck.Name+":"+string.Join(".",deck.Deck?.Select(card=>card.Id) ?? [])));
        lock(triadOddsGate)
        {
            if(key==triadOddsKey)return;
            triadOddsKey=key;triadOdds=[];
            triadOddsCancel?.Cancel();triadOddsCancel?.Dispose();
            if(npcFixed.Count+npcVariable.Count<5){triadOddsCancel=null;return;}
            var cancel=new CancellationTokenSource();triadOddsCancel=cancel;var token=cancel.Token;
            _=Task.Run(()=>
            {
                for(var index=0;index<decks.Count;index++)
                {
                    var deck=decks[index].Deck;
                    if(deck is null || token.IsCancellationRequested)continue;
                    try
                    {
                        var odds=TriadMatchSimulator.Estimate(deck,npcFixed,npcVariable,rules,forced,400,17,1,token);
                        lock(triadOddsGate)if(triadOddsKey==key)triadOdds=new(triadOdds){[index]=odds};
                    }
                    catch(OperationCanceledException){return;}
                    catch(Exception error){log.Warning(error,"Triple Triad deck estimate failed.");}
                }
            },token);
        }
    }
    private void StartTriadGenerate(TriadRequestInfo request,List<TriadCardDefinition> owned,List<TriadCardDefinition> npcFixed,
        List<TriadCardDefinition> npcVariable,TriadRules rules,TriadForcedCards forced,List<IReadOnlyList<TriadCardDefinition>> existing)
    {
        triadGenerateCancel?.Cancel();triadGenerateCancel?.Dispose();
        var cancel=new CancellationTokenSource();triadGenerateCancel=cancel;var token=cancel.Token;
        var key=TriadRequestKey(request);triadGenerating=true;triadGenerated=null;
        _=Task.Run(()=>
        {
            try
            {
                var result=TriadMatchSimulator.Optimize(owned,npcFixed,npcVariable,rules,forced,8000,17,token,existing);
                if(!token.IsCancellationRequested){triadGenerated=result;triadGeneratedKey=key;}
            }
            catch(OperationCanceledException){}
            catch(Exception error){log.Warning(error,"Triple Triad deck generation failed.");}
            finally{if(!token.IsCancellationRequested)triadGenerating=false;}
        },token);
    }
    private void CancelTriadRequest()
    {
        lock(triadOddsGate){triadOddsCancel?.Cancel();triadOddsCancel?.Dispose();triadOddsCancel=null;triadOddsKey=string.Empty;}
        triadGenerateCancel?.Cancel();triadGenerateCancel?.Dispose();triadGenerateCancel=null;
    }
}
