using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.Beastmaster;

/// <summary>Where the game's board and team windows are (screen pixels), when they are open.</summary>
public readonly record struct BoardWindows(bool Board, float X, float Y, float Width, float Height, bool Party, float PartyX, float PartyWidth);

/// <summary>
/// X-109 (0.981, DEC-047): reads, four times a second, whether the game's board window (XBMStageDetailList) and team window (XBMPetParty) are open and where,
/// and the ranks the team window shows. Read only. Out of a cut as usual; in the labyrinth only while its board is open (CompetitiveGate.AddBoard).
/// </summary>
internal sealed class BeastBoardReader : IDisposable
{
    private readonly IFramework framework;
    private readonly IGameGui gui;
    private readonly IPluginLog log;
    private readonly Func<IReadOnlyList<Beast>?> beasts;
    private readonly Action<Dictionary<uint, int>> ranksRead;
    private DateTimeOffset next;
    private bool warned;
    private BoardWindows state;
    public BoardWindows State => state;

    public BeastBoardReader(IFramework framework, IGameGui gui, IPluginLog log, Func<IReadOnlyList<Beast>?> beasts, Action<Dictionary<uint, int>> ranksRead)
    {
        this.framework = framework; this.gui = gui; this.log = log; this.beasts = beasts; this.ranksRead = ranksRead;
        TenshoAssistant.Competitive.CompetitiveGate.AddBoard(framework, OnUpdate);
    }

    private unsafe void OnUpdate(IFramework _)
    {
        var now = DateTimeOffset.UtcNow;
        if (now < next) return;
        next = now.AddMilliseconds(250);
        try
        {
            var board = gui.GetAddonByName("XBMStageDetailList");
            var party = gui.GetAddonByName("XBMPetParty");
            var boardUnit = board.IsNull || !board.IsVisible ? null : (AtkUnitBase*)board.Address;
            var partyUnit = party.IsNull || !party.IsVisible ? null : (AtkUnitBase*)party.Address;
            if (boardUnit == null || boardUnit->RootNode == null) { state = default; return; }
            var hasParty = partyUnit != null && partyUnit->RootNode != null;
            state = new(true, boardUnit->X, boardUnit->Y, boardUnit->RootNode->Width * boardUnit->Scale, boardUnit->RootNode->Height * boardUnit->Scale,
                hasParty, hasParty ? partyUnit->X : 0, hasParty ? partyUnit->RootNode->Width * partyUnit->Scale : 0);
            if (hasParty && beasts() is { Count: > 0 } known)
            {
                var texts = new List<string>();
                BeastWindowProbe.Texts(&partyUnit->UldManager, 0, texts);
                var ranks = BeastTeams.ParseRanks(texts, known);
                if (ranks.Count > 0) ranksRead(ranks);
            }
            warned = false;
        }
        catch (Exception error)
        {
            state = default;
            if (!warned) { warned = true; log.Warning(error, "The Beastmaster board windows could not be read."); }
        }
    }

    public void Dispose() => TenshoAssistant.Competitive.CompetitiveGate.Remove(framework, OnUpdate);
}
