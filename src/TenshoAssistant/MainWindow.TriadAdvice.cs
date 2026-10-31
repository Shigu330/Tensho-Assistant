using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;
using TenshoAssistant.MiniGames;

namespace TenshoAssistant;

/// <summary>
/// TRIAD-003 step 1: advice during a match, like TriadBuddy. The observed match is solved off the framework thread (TriadMatchAdvisor)
/// and the best card and square get a green frame on the game window. Reading only: the player still plays the card.
/// </summary>
internal sealed partial class MainWindow
{
    private readonly object triadAdviceGate=new();
    private CancellationTokenSource? triadAdviceCancel;
    private string triadAdviceKey=string.Empty;
    private TriadAdvice? triadAdvice;
    private bool triadAdviceLoading;
    private IReadOnlyList<CollectionEntry>? triadAdviceCardSource;
    private Dictionary<uint,TriadCardDefinition> triadAdviceCards=[];

    // Card faces by id, from the collection catalogue (TripleTriadCardResident copies); rebuilt when the catalogue changes.
    private TriadCardDefinition? TriadCard(uint id)
    {
        var entries=collections.State.Entries;
        if(!ReferenceEquals(entries,triadAdviceCardSource))
        {
            triadAdviceCardSource=entries;
            triadAdviceCards=entries.Where(entry=>entry.Kind==CollectionKind.TripleTriad && entry.Card is not null).GroupBy(entry=>entry.Id).ToDictionary(group=>group.Key,group=>group.First().Card!);
        }
        return triadAdviceCards.GetValueOrDefault(id);
    }
    private static string TriadAdviceKey(TriadObservedFrame frame)
    {
        var text=new StringBuilder().Append(frame.NativePhase).Append('|');
        foreach(var card in frame.BlueHand.Concat(frame.RedHand).Concat(frame.Board))
            text.Append((int)card.Presence).Append(card.Owner).Append(card.Top).Append(card.Right).Append(card.Bottom).Append(card.Left).Append(card.TypeId).Append(',');
        if(frame.Context is { } context)
            text.Append(string.Join('.',context.RuleIds)).Append('|').Append(context.ForcedSlot).Append('|').Append(context.NpcId);
        return text.ToString();
    }
    /// <summary>Advice for this frame: starts a solve when the match changed; null while it runs or when it is not the player's turn.</summary>
    private TriadAdvice? TriadAdviceFor(TriadObservedFrame frame,out bool loading)
    {
        var key=TriadAdviceKey(frame);
        lock(triadAdviceGate)
        {
            if(key==triadAdviceKey){loading=triadAdviceLoading;return triadAdvice;}
            triadAdviceCancel?.Cancel();triadAdviceCancel?.Dispose();triadAdviceCancel=null;
            triadAdviceKey=key;triadAdvice=null;triadAdviceLoading=false;
            if(!frame.BlueToMove || windowDisposed){loading=false;return null;}
            var cancel=new CancellationTokenSource();triadAdviceCancel=cancel;triadAdviceLoading=true;
            // Collection lookups copied now: the worker never touches the services.
            var cards=new Dictionary<uint,TriadCardDefinition>();
            if(frame.Context is { } context)
                foreach(var id in context.NpcFixed.Concat(context.NpcVariable))if(TriadCard(id) is { } card)cards[id]=card;
            var token=cancel.Token;
            _=Task.Run(()=>
            {
                TriadAdvice result;
                try{result=TriadMatchAdvisor.Advise(frame,id=>cards.GetValueOrDefault(id),new TriadSolveOptions(Milliseconds:2500),token);}
                catch(OperationCanceledException){return;}
                catch(Exception error){log.Warning(error,"Triple Triad advice failed.");result=TriadAdvice.Failed(TriadAdviceProblem.Unsolvable);}
                lock(triadAdviceGate)if(triadAdviceKey==key && !token.IsCancellationRequested){triadAdvice=result;triadAdviceLoading=false;}
            },token);
            loading=true;return null;
        }
    }
    private TriadObservedFrame? FreshTriadFrame()
    {
        var state=triadObservation.State;
        return state is { Status:TriadObservationStatus.Observed, Frame:{ } frame, ObservedAt:{ } at } && DateTimeOffset.UtcNow-at<=TimeSpan.FromSeconds(2) ? frame:null;
    }
    // Overlay on the game's Triple Triad window: best card in hand and best square, every frame while the observation is on.
    internal void DrawTriadOverlay()
    {
        if(windowDisposed || FreshTriadFrame() is not { } frame)return;
        if(TriadAdviceFor(frame,out _) is not { Problem:TriadAdviceProblem.None, Best:[var best,..] })return;
        if(frame.Context is not { BlueRects:{ } hand, BoardRects:{ } squares })return;
        var draw=ImGui.GetForegroundDrawList();
        var fill=ImGui.GetColorU32(Success with { W=0.2f });var border=ImGui.GetColorU32(Success);
        foreach(var rect in new[]{hand[best.HandSlot],squares[best.Square]})
        {
            var min=new Vector2(rect.X,rect.Y);var max=min+new Vector2(rect.Width,rect.Height);
            draw.AddRectFilled(min,max,fill,6);draw.AddRect(min,max,border,6,ImDrawFlags.None,4);
        }
    }
    private void CancelTriadAdvice()
    {lock(triadAdviceGate){triadAdviceCancel?.Cancel();triadAdviceCancel?.Dispose();triadAdviceCancel=null;triadAdviceKey=string.Empty;triadAdvice=null;}}
    // Text form for the « Observed match » tab.
    private void DrawTriadAdviceText()
    {
        if(FreshTriadFrame() is not { } frame){ImGui.TextDisabled(localization.Get("TriadAdvice.NoMatch"));return;}
        var advice=TriadAdviceFor(frame,out var loading);
        if(loading){ImGui.TextDisabled(localization.Get("TriadAdvice.Calculating"));return;}
        if(advice is null){ImGui.TextDisabled(localization.Get("TriadAdvice.Waiting"));return;}
        if(advice.Problem!=TriadAdviceProblem.None){ImGui.TextWrapped(localization.Get("TriadAdvice."+advice.Problem));return;}
        var best=advice.Best[0];
        ImGui.TextColored(Success,localization.Format("TriadAdvice.Move",best.HandSlot+1,best.Square/3+1,best.Square%3+1,best.AverageDifference.ToString("+0.#;-0.#;0")));
        ImGui.TextDisabled(localization.Format("TriadAdvice.Detail",advice.Hands,advice.Exact ? localization.Get("TriadAdvice.Exact"):localization.Get("TriadAdvice.Approximate")));
    }
}
