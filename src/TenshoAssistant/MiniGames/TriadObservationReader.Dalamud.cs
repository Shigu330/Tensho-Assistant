using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace TenshoAssistant.MiniGames;

/// <summary>Opt-in bounded copies of the visible addon on framework; no native commands, hooks or deferred pointers.</summary>
internal sealed class TriadObservationReader : IDisposable
{
    private readonly object gate = new();
    private readonly TriadObservationService service;
    private readonly IFramework framework;
    private readonly IPlayerState player;
    private readonly IClientState client;
    private readonly IGameGui gui;
    private readonly IPluginLog log;
    private bool disposed;
    private long nextRead;
    private ulong owner;
    public TriadObservationReader(TriadObservationService service, IFramework framework, IPlayerState player, IClientState client, IGameGui gui, IPluginLog log)
    {
        this.service=service; this.framework=framework; this.player=player; this.client=client; this.gui=gui; this.log=log;
        framework.Update += Update;
    }
    private ulong CurrentOwner => client.IsLoggedIn && player.IsLoaded ? player.ContentId : 0;
    private unsafe void Update(IFramework _)
    {
        lock (gate)
        {
            if (disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading) return;
            var character = CurrentOwner;
            if (character != owner) { owner=character; nextRead=0; }
            var lease = service.BeginRead(character);
            if (lease is not { } ticket) return;
            var now = Stopwatch.GetTimestamp();
            if (now < nextRead) return;
            nextRead = now + Stopwatch.Frequency / 5;
            try
            {
                var handle = gui.GetAddonByName("TripleTriad");
                if (handle.IsNull) { service.Missing(ticket,character,TriadObservationStatus.AddonAbsent); return; }
                if (gui.GameUiHidden || !handle.IsReady || !handle.IsVisible)
                { service.Missing(ticket,character,TriadObservationStatus.NotReady); return; }
                var addon = (AddonTripleTriad*)handle.Address;
                var phase = (byte)addon->TurnState;
                var blue = Copy(addon->BlueDeck);
                var red = Copy(addon->RedDeck);
                var board = Copy(addon->Board);
                if (CurrentOwner != character) { service.BeginRead(CurrentOwner); return; }
                var after = gui.GetAddonByName("TripleTriad");
                if (after.IsNull || after.Address != handle.Address || !after.IsReady || !after.IsVisible || gui.GameUiHidden)
                { service.Missing(ticket,character,TriadObservationStatus.NotReady); return; }
                service.Publish(ticket,character,phase,blue,red,board);
            }
            catch (Exception error)
            {
                service.Fail(ticket,character,error); nextRead=now + 5 * Stopwatch.Frequency;
                log.Warning(error,"Tensho Triad observation failed; retry in five seconds.");
            }
        }
    }
    private static unsafe TriadRawCard[] Copy(ReadOnlySpan<AddonTripleTriad.TripleTriadCard> cards)
    {
        var result = new TriadRawCard[cards.Length];
        for (var index = 0; index < result.Length; index++)
        {
            var card = cards[index];
            result[index] = new(card.HasCard,(byte)card.CardOwner,card.CardRarity,(byte)card.CardType,
                card.NumSideU,card.NumSideR,card.NumSideD,card.NumSideL);
        }
        return result;
    }
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed=true; framework.Update -= Update; service.Dispose();
        }
    }
}
