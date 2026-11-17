using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.Beastmaster;

/// <summary>Where the game's board and team windows are (screen pixels), when they are open.</summary>
/// <param name="Content">X-549 (0.1522): the board the game's board or team window is about (their agent's ContentId; 0: not read).</param>
/// <param name="Team">X-550 (0.1526): the team the game's team window shows (empty: closed or not read).</param>
public readonly record struct BoardWindows(bool Board, float X, float Y, float Width, float Height, bool Party, float PartyX, float PartyWidth, uint Content = 0,
    IReadOnlyList<BeastTeams.TeamMember>? Team = null);

/// <summary>
/// X-109 (0.981, DEC-047): reads, four times a second, whether the game's board window (XBMStageDetailList) and team window (XBMPetParty) are open and where,
/// and the ranks the team window shows. Read only. Out of a cut as usual; in the labyrinth only while its board is open (CompetitiveGate.AddBoard).
/// X-135 (0.1163): also the board's result window (XBMResult): its numbers only, read again only when they change (BeastResults).
/// </summary>
internal sealed class BeastBoardReader : IDisposable
{
    private readonly IFramework framework;
    private readonly IGameGui gui;
    private readonly IPluginLog log;
    private readonly Func<IReadOnlyList<Beast>?> beasts;
    private readonly Action<Dictionary<uint, int>> ranksRead;
    private readonly Action<BoardResult>? resultRead;
    private DateTimeOffset next;
    private bool warned;
    private bool resultWarned;
    private string? resultSeen;
    private uint contentSaid;
    private BoardWindows state;
    public BoardWindows State => state;

    public BeastBoardReader(IFramework framework, IGameGui gui, IPluginLog log, Func<IReadOnlyList<Beast>?> beasts, Action<Dictionary<uint, int>> ranksRead, Action<BoardResult>? resultRead = null)
    {
        this.framework = framework; this.gui = gui; this.log = log; this.beasts = beasts; this.ranksRead = ranksRead; this.resultRead = resultRead;
        TenshoAssistant.Competitive.CompetitiveGate.AddBoard(framework, OnUpdate);
    }

    private unsafe void OnUpdate(IFramework _)
    {
        var now = DateTimeOffset.UtcNow;
        if (now < next) return;
        next = now.AddMilliseconds(250);
        try
        {
            ReadResult(now);
            var board = gui.GetAddonByName("XBMStageDetailList");
            var party = gui.GetAddonByName("XBMPetParty");
            var boardUnit = board.IsNull || !board.IsVisible ? null : (AtkUnitBase*)board.Address;
            var partyUnit = party.IsNull || !party.IsVisible ? null : (AtkUnitBase*)party.Address;
            if (boardUnit == null || boardUnit->RootNode == null) { state = default; return; }
            var hasParty = partyUnit != null && partyUnit->RootNode != null;
            var content = Content(hasParty);
            IReadOnlyList<BeastTeams.TeamMember>? team = null;
            if (hasParty && beasts() is { Count: > 0 } known)
            {
                var texts = new List<string>();
                BeastWindowProbe.Texts(&partyUnit->UldManager, 0, texts);
                var ranks = BeastTeams.ParseRanks(texts, known);
                if (ranks.Count > 0) ranksRead(ranks);
                // X-550 (0.1526): the team taken, with each beast's HP, read each time the window is open (after each fight).
                team = BeastTeams.ParseTeam(texts, known);
            }
            state = new(true, boardUnit->X, boardUnit->Y, boardUnit->RootNode->Width * boardUnit->Scale, boardUnit->RootNode->Height * boardUnit->Scale,
                hasParty, hasParty ? partyUnit->X : 0, hasParty ? partyUnit->RootNode->Width * partyUnit->Scale : 0, content, team);
            warned = false;
        }
        catch (Exception error)
        {
            state = default;
            if (!warned) { warned = true; log.Warning(error, "The Beastmaster board windows could not be read."); }
        }
    }

    /// <summary>
    /// X-549 (0.1522): the board the game's windows are about: the ContentId of the board window's agent (AgentXBMStageDetailList, ClientStructs),
    /// else of the team window's (AgentXBMPetParty); read only (a field, no call), while their window is open; said in the log when it changes,
    /// so a reading in game shows what the number is (an XBMContent row or a ContentFinderCondition row; BeastTeams.Current takes either).
    /// </summary>
    private unsafe uint Content(bool party)
    {
        var stage = AgentXBMStageDetailList.Instance();
        var team = party ? AgentXBMPetParty.Instance() : null;
        var fromStage = stage != null ? stage->ContentId : 0;
        var fromTeam = team != null ? team->ContentId : 0;
        var content = fromStage != 0 ? fromStage : fromTeam;
        if (content != contentSaid)
        {
            contentSaid = content;
            log.Information("Beastmaster board read in the game's windows: content {0} (board window {1}, team window {2}).", content, fromStage, party ? fromTeam.ToString() : "closed");
        }
        return content;
    }

    /// <summary>X-135 (0.1163): the result window's numbers (Int and UInt only, never a text), parsed once per change; refused whole when they do not fit.</summary>
    private unsafe void ReadResult(DateTimeOffset now)
    {
        if (resultRead is null) return;
        var addon = gui.GetAddonByName(BeastResults.Window);
        if (addon.IsNull || !addon.IsVisible) { resultSeen = null; return; }
        var unit = (AtkUnitBase*)addon.Address;
        if (unit->AtkValues == null || beasts() is not { Count: > 0 } known) return;
        var values = new Dictionary<int, long>();
        for (var index = 0; index < Math.Min((int)unit->AtkValuesCount, BeastResults.ValueCount); index++)
        {
            var value = unit->AtkValues[index];
            if (value.Type == AtkValueType.Int) values[index] = value.Int;
            else if (value.Type == AtkValueType.UInt) values[index] = value.UInt;
        }
        var seen = string.Join(";", values.Select(pair => $"{pair.Key}:{pair.Value}"));
        if (seen == resultSeen) return;
        resultSeen = seen;
        if (BeastResults.Parse(values, known, now) is { } result)
        {
            log.Information("Beastmaster board result read: score {0}, {1} beasts, {2} ranks gained.", result.Score, result.Beasts.Count, result.Beasts.Sum(gain => gain.RankAfter - gain.RankBefore));
            resultRead(result);
        }
        else if (!resultWarned)
        {
            resultWarned = true;
            // X-465 (0.1465): why, in numbers (read again at each change of the window: one filled later is still read).
            log.Warning("The Beastmaster board result window could not be read (unavailable, maybe after a game update): nothing kept ({0}).", BeastResults.Why(values, known));
        }
    }

    public void Dispose() => TenshoAssistant.Competitive.CompetitiveGate.Remove(framework, OnUpdate);
}
