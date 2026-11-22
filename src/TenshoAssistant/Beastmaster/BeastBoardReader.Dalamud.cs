using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.Beastmaster;

/// <summary>Where the game's board and team windows are (screen pixels), when they are open.</summary>
/// <param name="Content">X-549 (0.1522): the board the game's board or team window is about (their agent's ContentId; 0: not read).</param>
/// <param name="Team">X-550 (0.1526): the team the game's team window shows (empty: closed or not read).</param>
/// <param name="Shown">X-486 (0.1529): the fights the game's board window lists (their XBMBattleDetail rows; null: not read).</param>
public readonly record struct BoardWindows(bool Board, float X, float Y, float Width, float Height, bool Party, float PartyX, float PartyWidth, uint Content = 0,
    IReadOnlyList<BeastTeams.TeamMember>? Team = null, IReadOnlySet<uint>? Shown = null);

/// <summary>
/// X-109 (0.981, DEC-047): reads, four times a second, whether the game's board window (XBMStageDetailList) and team window (XBMPetParty) are open and where,
/// and the ranks the team window shows. Read only. Out of a cut as usual; in the labyrinth only while its board is open (CompetitiveGate.AddBoard).
/// X-135 (0.1163): also the board's result window (XBMResult): its numbers only, read again only when they change (BeastResults).
/// X-578 (0.1548): also the bestiary's beast page (XBMMonsterBookDetail): each beast's own rank, read again only when the page changes.
/// </summary>
internal sealed class BeastBoardReader : IDisposable
{
    private readonly IFramework framework;
    private readonly IGameGui gui;
    private readonly IPluginLog log;
    private readonly Func<IReadOnlyList<Beast>?> beasts;
    private readonly Action<Dictionary<uint, int>> ranksRead;
    private readonly Action<BoardResult>? resultRead;
    private readonly Action<BeastTeams.BookRead>? bookRead;
    /// <summary>X-135 (0.1771): an Item row's icon in the Item sheet, to check the result window's rewards (null: the rewards not read).</summary>
    public Func<uint, uint>? ItemIcon { get; init; }
    private string? bookSeen;
    private volatile uint activeBeast;
    private string? activeSaid;
    /// <summary>X-136 (0.1550): the beast the game's « Cornes d'appel » window details (its XBMPet row; 0: closed or not read).</summary>
    public uint ActiveBeast => activeBeast;
    private bool bookWarned;
    private readonly HashSet<uint> satietySaid = [];
    private DateTimeOffset next;
    private bool warned;
    private bool resultWarned;
    private string? resultSeen;
    private uint contentSaid;
    private string? stageSaid;
    private int teamSaid = -1;
    private bool teamWarned;
    private BoardWindows state;
    public BoardWindows State => state;

    public BeastBoardReader(IFramework framework, IGameGui gui, IPluginLog log, Func<IReadOnlyList<Beast>?> beasts, Action<Dictionary<uint, int>> ranksRead, Action<BoardResult>? resultRead = null,
        Action<BeastTeams.BookRead>? bookRead = null)
    {
        this.framework = framework; this.gui = gui; this.log = log; this.beasts = beasts; this.ranksRead = ranksRead; this.resultRead = resultRead; this.bookRead = bookRead;
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
            ReadBook();
            ReadActive();
            var board = gui.GetAddonByName("XBMStageDetailList");
            var party = gui.GetAddonByName("XBMPetParty");
            var boardUnit = board.IsNull || !board.IsVisible ? null : (AtkUnitBase*)board.Address;
            var partyUnit = party.IsNull || !party.IsVisible ? null : (AtkUnitBase*)party.Address;
            if (boardUnit == null || boardUnit->RootNode == null) { state = default; return; }
            var hasParty = partyUnit != null && partyUnit->RootNode != null;
            var content = Content(hasParty);
            var shown = Shown();
            IReadOnlyList<BeastTeams.TeamMember>? team = null;
            Dictionary<uint, int>? ranks = null;
            if (hasParty && beasts() is { Count: > 0 } known)
            {
                var texts = new List<string>();
                BeastWindowProbe.Texts(&partyUnit->UldManager, 0, texts);
                ranks = BeastTeams.ParseRanks(texts, known);
                // X-550 (0.1526): the team taken, with each beast's HP, read each time the window is open (after each fight).
                team = BeastTeams.ParseTeam(texts, known);
                // X-692 (0.1661): said when it changes (how many, never a name), and once when the window names beasts but none is read
                // (the rank's glyph of 20/11 was such a case, silent).
                if (team.Count != teamSaid)
                {
                    teamSaid = team.Count;
                    if (team.Count > 0) log.Information("Beastmaster team window read: {0} beasts, {1} down, {2} low.", team.Count, team.Count(member => member.Down), team.Count(member => member.Low));
                }
                if (!teamWarned && BeastTeams.NamesWithoutTeam(texts, known, team))
                {
                    teamWarned = true;
                    log.Warning("The Beastmaster team window could not be read (unavailable, maybe after a game update): the panel shows the advised beasts.");
                }
            }
            state = new(true, boardUnit->X, boardUnit->Y, boardUnit->RootNode->Width * boardUnit->Scale, boardUnit->RootNode->Height * boardUnit->Scale,
                hasParty, hasParty ? partyUnit->X : 0, hasParty ? partyUnit->RootNode->Width * partyUnit->Scale : 0, content, team, shown);
            // X-561 (review of 0.1526): the ranks are merged after the state holds this turn's board, so their limit is that board's.
            if (ranks is { Count: > 0 }) ranksRead(ranks);
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

    /// <summary>
    /// X-486 (0.1529): the fights the game's board window lists: the BattleDetailId of AgentXBMStageDetailList's entries (fields only, no call;
    /// ClientStructs: 1000 entries, EntrySelection 100 bytes). Said in the log when it changes, with the selection and the stage to scroll to,
    /// so a reading in game tells what they are (docs/evidence/0.1529-beast-fight-shown.md). Null when the agent is not there.
    /// </summary>
    private unsafe IReadOnlySet<uint>? Shown()
    {
        var stage = AgentXBMStageDetailList.Instance();
        if (stage == null) return null;
        var details = new HashSet<uint>();
        var entries = stage->Entries;
        var count = 0;
        for (var index = 0; index < entries.Length; index++)
        {
            var detail = entries[index].BattleDetailId;
            if (entries[index].EntryType != 0 || detail != 0) count++;
            if (detail != 0) details.Add(detail);
        }
        var selection = stage->EntrySelection;
        var selected = new List<int>();
        for (var index = 0; index < selection.Length; index++) if (selection[index] != 0) selected.Add(index);
        var said = $"{count}|{string.Join(",", details.Order())}|{string.Join(",", selected)}|{stage->ScrollToStageId}|{stage->Mode}";
        if (said != stageSaid)
        {
            stageSaid = said;
            log.Information("Beastmaster board window entries: {0} entries, fights {1}; selection {2}; stage to scroll to {3}, mode {4}.",
                count, string.Join(",", details.Order()), selected.Count == 0 ? "none" : string.Join(",", selected), stage->ScrollToStageId, stage->Mode);
        }
        return details;
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
        if (BeastResults.Parse(values, known, now, ItemIcon) is { } result)
        {
            log.Information("Beastmaster board result read: score {0}, {1} beasts, {2} ranks gained.", result.Score, result.Beasts.Count, result.Beasts.Sum(gain => gain.RankAfter - gain.RankBefore));
            // X-562 M4 (review of 0.1526): the window names its bonuses, Tensho numbers them; its numbers from 47 on are not studied: said
            // here (numbers only) to see in game whether they are the bonuses' XBMScoreBonus rows.
            log.Information("Beastmaster board result, numbers 47 to 62: {0}.", string.Join(",", Enumerable.Range(47, 16).Select(index => values.TryGetValue(index, out var number) ? number.ToString(System.Globalization.CultureInfo.InvariantCulture) : "-")));
            // X-135 (0.1771): the rewards (numbers only: item rows, icons, counts), or why not, to compare with the game's window.
            log.Information("Beastmaster board result, rewards: {0}; numbers 47 to 67: {1}.", result.Rewards is { } rewards ? string.Join(", ", rewards.Select(reward => $"item {reward.Item} x{reward.Count}")) : "not read",
                string.Join(",", Enumerable.Range(47, 21).Select(index => values.TryGetValue(index, out var number) ? number.ToString(System.Globalization.CultureInfo.InvariantCulture) : "-")));
            resultRead(result);
        }
        else if (!resultWarned)
        {
            resultWarned = true;
            // X-465 (0.1465): why, in numbers (read again at each change of the window: one filled later is still read).
            log.Warning("The Beastmaster board result window could not be read (unavailable, maybe after a game update): nothing kept ({0}).", BeastResults.Why(values, known));
        }
    }

    /// <summary>
    /// X-578 (0.1548, the user's request of 18/11: « in the labyrinth, its bestiary shows the ranks of every beast one holds »): the bestiary's
    /// beast page (XBMMonsterBookDetail, open in and out of the labyrinth): its texts (read only), parsed when they change (BeastTeams.ParseBook);
    /// a page of a held beast that does not fit is said once in the log, nothing kept.
    /// </summary>
    private unsafe void ReadBook()
    {
        if (bookRead is null) return;
        var addon = gui.GetAddonByName("XBMMonsterBookDetail");
        if (addon.IsNull || !addon.IsVisible) { bookSeen = null; return; }
        var unit = (AtkUnitBase*)addon.Address;
        if (unit->RootNode == null || beasts() is not { Count: > 0 } known) return;
        var texts = new List<string>();
        BeastWindowProbe.Texts(&unit->UldManager, 0, texts);
        var seen = string.Join("\u001f", texts);
        if (seen == bookSeen) return;
        bookSeen = seen;
        if (BeastTeams.ParseBook(texts, known) is { } read)
        {
            log.Information("Beastmaster bestiary read: beast {0}, rank {1} (synchronised {2}).", read.Beast, read.Rank, read.Synced);
            bookRead(read);
            // X-744 (0.1770): the satiety the page shows against XBMPet column 3, said once per beast (R06: each page read checks the column).
            if (BeastSpaces.SatietyOfBook(texts) is var shown and > 0 && known.FirstOrDefault(beast => beast.Id == read.Beast) is { } beast && satietySaid.Add(read.Beast))
            {
                if (shown == beast.Satiety) log.Information("Beastmaster satiety checked: beast {0}, the bestiary shows {1}, as XBMPet column 3.", read.Beast, shown);
                else log.Warning("Beastmaster satiety differs: beast {0}, the bestiary shows {1}, XBMPet column 3 holds {2} (the panel's satiety is not the game's).", read.Beast, shown, beast.Satiety);
            }
        }
        else if (!bookWarned && texts.Any(text => text.Length > 2 && text[0] == '(' && text[^1] == ')'))
        {
            bookWarned = true;
            log.Warning("The Beastmaster bestiary's beast page could not be read (unavailable, maybe after a game update): nothing kept.");
        }
    }

    /// <summary>
    /// X-136 (0.1550): the beast the game's « Cornes d'appel » window (XBMActivePet) details: its values 4 (name) and 19 (Trick), read only
    /// (BeastInstinct.FromWindow); said in the log when it changes, with value 2, so a reading in game tells whether it is the beast out.
    /// </summary>
    private unsafe void ReadActive()
    {
        var addon = gui.GetAddonByName("XBMActivePet");
        var unit = addon.IsNull || !addon.IsVisible ? null : (AtkUnitBase*)addon.Address;
        if (unit == null || unit->AtkValues == null || unit->AtkValuesCount <= 19 || beasts() is not { Count: > 0 } known) { activeBeast = 0; return; }
        var name = TenshoAssistant.UI.AtkValueRead.IsText(unit->AtkValues[4].Type) ? unit->AtkValues[4].String.ToString() ?? "" : "";
        var value = unit->AtkValues[19];
        var trick = value.Type == AtkValueType.UInt ? value.UInt : value.Type == AtkValueType.Int && value.Int > 0 ? (uint)value.Int : 0u;
        var out2 = unit->AtkValues[2].Type == AtkValueType.Bool ? (unit->AtkValues[2].Byte != 0).ToString() : "-";
        activeBeast = BeastInstinct.FromWindow(trick, name, known);
        var said = $"{activeBeast}|{trick}|{out2}";
        if (said == activeSaid) return;
        activeSaid = said;
        log.Information("Beastmaster battlehorns window: beast {0} (Trick {1}, value 2 {2}).", activeBeast, trick, out2);
    }

    public void Dispose() => TenshoAssistant.Competitive.CompetitiveGate.Remove(framework, OnUpdate);
}
