using System.Diagnostics;
using System.Text;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;

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
    private readonly Func<uint> targetBaseId;
    private readonly IDataManager? data;
    // Capture lines only in developer mode (0.161): building them cost a 66 ms frame at the start of a match.
    private readonly Func<bool> capture;
    private Dictionary<string, uint>? ruleIds;
    private uint deckNpc;
    private string deckNpcName = string.Empty;
    private uint[] deckFixed = [], deckVariable = [];
    private bool disposed;
    private long nextRead;
    private ulong owner;
    private string lastCapture = string.Empty, lastRequest = string.Empty;
    public TriadObservationReader(TriadObservationService service, IFramework framework, IPlayerState player, IClientState client, IGameGui gui, IPluginLog log,
        Func<uint>? targetBaseId = null, IDataManager? data = null, Func<bool>? capture = null)
    {
        this.service=service; this.framework=framework; this.player=player; this.client=client; this.gui=gui; this.log=log;
        this.targetBaseId=targetBaseId ?? (() => 0); this.data=data; this.capture=capture ?? (() => false);
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
                if (capture()) CaptureRequest();
                ReadRequest(ticket, character);
                ReadEditDeck(ticket, character);
                var handle = gui.GetAddonByName("TripleTriad");
                if (handle.IsNull) { if (service.Request is null) { deckNpc = 0; deckFixed = deckVariable = []; } service.Missing(ticket,character,TriadObservationStatus.AddonAbsent); return; }
                if (gui.GameUiHidden || !handle.IsReady || !handle.IsVisible)
                { service.Missing(ticket,character,TriadObservationStatus.NotReady); return; }
                var addon = (AddonTripleTriad*)handle.Address;
                if (capture()) Capture(addon);
                var phase = (byte)addon->TurnState;
                var blue = Copy(addon->BlueDeck);
                var red = Copy(addon->RedDeck);
                var board = Copy(addon->Board);
                var context = Context(addon, phase);
                if (CurrentOwner != character) { service.BeginRead(CurrentOwner); return; }
                var after = gui.GetAddonByName("TripleTriad");
                if (after.IsNull || after.Address != handle.Address || !after.IsReady || !after.IsVisible || gui.GameUiHidden)
                { service.Missing(ticket,character,TriadObservationStatus.NotReady); return; }
                service.Publish(ticket,character,phase,blue,red,board,context);
            }
            catch (Exception error)
            {
                service.Fail(ticket,character,error); nextRead=now + 5 * Stopwatch.Frequency;
                log.Warning(error,"Tensho Triad observation failed; retry in five seconds.");
            }
        }
    }
    // TRIAD-003 capture (0.159): while the observation is on, one log line per change of the match window — turn state, target NPC,
    // window values (rules, names…), card faces and screen rectangles — and of the pre-match window « TripleTriadRequest ». It is the
    // evidence for linking the observation to the solver (rules, whose turn, positions); nothing is sent anywhere.
    private unsafe void Capture(AddonTripleTriad* addon)
    {
        var text = new StringBuilder();
        text.Append("turn=").Append((byte)addon->TurnState).Append(" target=").Append(targetBaseId()).Append(" scale=").Append(addon->AtkUnitBase.Scale.ToString("0.###"));
        void Cards(string name, ReadOnlySpan<AddonTripleTriad.TripleTriadCard> cards)
        {
            text.Append(' ').Append(name).Append('=');
            for (var index = 0; index < cards.Length; index++)
            {
                var card = cards[index];
                text.Append(index == 0 ? "" : "|").Append(card.HasCard ? $"{(byte)card.CardOwner}:{card.CardRarity}:{(byte)card.CardType}:{card.NumSideU}{card.NumSideR}{card.NumSideD}{card.NumSideL}" : "-");
                if (Rect(card.CardDropControl) is { } rect) text.Append('@').Append(rect);
            }
        }
        Cards("blue", addon->BlueDeck); Cards("red", addon->RedDeck); Cards("board", addon->Board);
        text.Append(" values=").Append(Values(&addon->AtkUnitBase));
        var line = text.ToString();
        // Rectangles move with the window: compare without them so a drag does not flood the log.
        var key = System.Text.RegularExpressions.Regex.Replace(line, @"@[^|\s]*", "");
        if (key == lastCapture) return;
        lastCapture = key;
        log.Information("Triad capture: {Line}", line);
    }
    private unsafe void CaptureRequest()
    {
        var handle = gui.GetAddonByName("TripleTriadRequest");
        if (handle.IsNull || !handle.IsVisible) { lastRequest = string.Empty; return; }
        var line = $"target={targetBaseId()} values={Values((AtkUnitBase*)handle.Address)}";
        if (line == lastRequest) return;
        lastRequest = line;
        log.Information("Triad request capture: {Line}", line);
    }
    // Window values, bounded: 160 values, text cut at 60 characters.
    private static unsafe string Values(AtkUnitBase* unit)
    {
        var text = new StringBuilder();
        var count = Math.Min((int)unit->AtkValuesCount, 160);
        for (var index = 0; index < count && unit->AtkValues != null; index++)
        {
            var value = unit->AtkValues[index];
            var shown = value.Type switch
            {
                AtkValueType.Int => value.Int.ToString(),
                AtkValueType.UInt => value.UInt.ToString(),
                AtkValueType.Bool => value.Byte != 0 ? "true" : "false",
                AtkValueType.String or AtkValueType.ManagedString or AtkValueType.ConstString => Bounded(value.String),
                AtkValueType.Undefined => null,
                _ => $"<{value.Type}>",
            };
            if (shown is not null) text.Append(index).Append(':').Append(shown).Append(';');
        }
        return text.ToString();
    }
    private static unsafe string Bounded(InteropGenerator.Runtime.CStringPointer pointer)
    {
        if (!pointer.HasValue) return "\"\"";
        var bytes = (byte*)pointer.Value; var length = 0;
        while (length < 240 && bytes[length] != 0) length++;
        var text = Dalamud.Game.Text.SeStringHandling.SeString.Parse(new ReadOnlySpan<byte>(bytes, length)).TextValue;
        return "\"" + (text.Length > 60 ? text[..60] : text).Replace(";", ",") + "\"";
    }
    private static unsafe string? Rect(AtkComponentBase* component)
    {
        if (component == null || component->OwnerNode == null) return null;
        var node = &component->OwnerNode->AtkResNode;
        return $"{node->ScreenX:0},{node->ScreenY:0},{node->Width * node->ScaleX:0}x{node->Height * node->ScaleY:0}";
    }
    // Match context (0.160, capture docs/evidence/0.160.0-triad-match-capture.log): window values 5–9 hold the rule names shown in the
    // match (matched against TripleTriadRule names; unknown text is ignored); value 25 is the slot of the card the game imposes on a
    // masked move (4/4 moves of the captured Chaos match). The NPC is the target when the match window opens; its deck comes from the
    // TripleTriad row linked by ENpcBase data (docs/evidence/0.159.0-triad-npc-probe.log).
    private unsafe TriadMatchContext Context(AddonTripleTriad* addon, byte phase)
    {
        var unit = &addon->AtkUnitBase;
        if (data is not null) ruleIds ??= data.GetExcelSheet<TripleTriadRule>().Where(rule => rule.RowId != 0)
            .Select(rule => (rule.RowId, Name: rule.Name.ExtractText())).Where(rule => rule.Name.Length > 0)
            .GroupBy(rule => rule.Name).ToDictionary(group => group.Key, group => group.First().RowId);
        var rules = new List<uint>();
        for (var index = 5; index <= 9 && index < unit->AtkValuesCount && unit->AtkValues != null; index++)
        {
            var value = unit->AtkValues[index];
            if (value.Type is AtkValueType.String or AtkValueType.ManagedString or AtkValueType.ConstString
                && ruleIds?.TryGetValue(Bounded(value.String).Trim('"'), out var rule) == true && !rules.Contains(rule)) rules.Add(rule);
        }
        int? forced = null;
        if (phase == 2 && unit->AtkValuesCount > 25 && unit->AtkValues != null && unit->AtkValues[25].Type == AtkValueType.Int) forced = unit->AtkValues[25].Int;
        var target = targetBaseId();
        if (deckNpc == 0 && target != 0) LoadDeck(target);
        return new(rules, forced, deckNpc, deckFixed, deckVariable, Rects(addon->BlueDeck, unit->Scale), Rects(addon->Board, unit->Scale));
    }
    // Challenge window: rules (values 102–105, TripleTriadRule ids 1–15), NPC deck, window rectangle and the player's saved decks.
    private unsafe void ReadRequest(int ticket, ulong character)
    {
        var handle = gui.GetAddonByName("TripleTriadRequest");
        if (handle.IsNull || !handle.IsReady || !handle.IsVisible || gui.GameUiHidden)
        {
            // The deck choice follows the challenge, with a few frames where neither window is shown (capture 2026-10-31 17:41:18,
            // « challenge kept: no »): the challenge is held 30 s after its window closes and given back to the deck choice.
            var now = Stopwatch.GetTimestamp();
            if (service.Request is { } current) { heldRequest = current with { Window = default }; heldUntil = now + Stopwatch.Frequency * 30; }
            if (ReadSelDeck(ticket, character))
            {
                if (heldRequest is { } held && now < heldUntil && (service.Request is null || service.Request.Window.Valid)) service.PublishRequest(ticket, character, held);
                return;
            }
            if (service.Request is not null) service.PublishRequest(ticket,character,null);
            if (now >= heldUntil) heldRequest = null;
            return;
        }
        // The challenge window can stay open under the deck choice (0.166): read the deck rows in both cases.
        ReadSelDeck(ticket, character);
        var unit = (AtkUnitBase*)handle.Address;
        var rules = new List<uint>();
        for (var index = 102; index <= 105 && index < unit->AtkValuesCount && unit->AtkValues != null; index++)
            if (unit->AtkValues[index].Type == AtkValueType.Int && unit->AtkValues[index].Int is >= 1 and <= 15 && !rules.Contains((uint)unit->AtkValues[index].Int))
                rules.Add((uint)unit->AtkValues[index].Int);
        if (data is not null) ruleIds ??= data.GetExcelSheet<TripleTriadRule>().Where(rule => rule.RowId != 0)
            .Select(rule => (rule.RowId, Name: rule.Name.ExtractText())).Where(rule => rule.Name.Length > 0)
            .GroupBy(rule => rule.Name).ToDictionary(group => group.Key, group => group.First().RowId);
        var target = targetBaseId();
        if (target != 0 && target != deckNpc) LoadDeck(target);
        var root = unit->RootNode;
        var window = root == null ? default : new TriadRect(unit->X, unit->Y, root->Width * unit->Scale, root->Height * unit->Scale);
        var decks = new List<TriadGameDeck>();
        var module = GoldSaucerModule.Instance();
        if (module != null && module->CharacterContentId == character)
            for (var index = 0; index < module->Decks.Length; index++)
            {
                ref var deck = ref module->Decks[index];
                var cards = new uint[deck.Cards.Length];
                for (var slot = 0; slot < cards.Length; slot++) cards[slot] = deck.Cards[slot];
                if (cards.Any(card => card != 0)) decks.Add(new(index, deck.NameString, Array.AsReadOnly(cards)));
            }
        var previous = service.Request;
        if (previous is not null && previous.NpcId == deckNpc && previous.RuleIds.SequenceEqual(rules) && previous.Window == window
            && previous.Decks.Count == decks.Count && previous.Decks.Zip(decks).All(pair => pair.First.Name == pair.Second.Name && pair.First.Cards.SequenceEqual(pair.Second.Cards)))
            return;
        var names = rules.Select(rule => ruleIds?.FirstOrDefault(pair => pair.Value == rule).Key ?? rule.ToString()).ToArray();
        service.PublishRequest(ticket, character, new(deckNpc, rules.AsReadOnly(), deckFixed, deckVariable, window, decks.AsReadOnly(), DateTimeOffset.UtcNow,
            deckNpcName, Array.AsReadOnly(names)));
    }
    private unsafe void ReadEditDeck(int ticket, ulong character)
    {
        var handle = gui.GetAddonByName("GSInfoEditDeck");
        TriadRect? rect = null;
        if (!handle.IsNull && handle.IsReady && handle.IsVisible && !gui.GameUiHidden)
        {
            var unit = (AtkUnitBase*)handle.Address;
            if (unit->RootNode != null) rect = new TriadRect(unit->X, unit->Y, unit->RootNode->Width * unit->Scale, unit->RootNode->Height * unit->Scale);
            if (capture()) CaptureEditDeck(unit);
            var cards = new List<(uint, TriadRect)>();
            ListedCards(&unit->UldManager, 0, cards);
            service.PublishEditDeck(ticket, character, rect, cards.AsReadOnly());
            return;
        }
        lastEditDeck = string.Empty;
        if (rect != service.EditDeckWindow) service.PublishEditDeck(ticket, character, rect);
    }
    // TRIAD-003 capture (0.163, developer mode): visible image nodes of the deck editor with their screen rectangle and icon texture,
    // to prove where each card of the game's list is drawn before highlighting the cards of a Tensho deck there.
    private string lastEditDeck = string.Empty;
    private int lastSelDeckRows = -1;
    private TriadRequestInfo? heldRequest;
    private long heldUntil;
    private unsafe void CaptureEditDeck(AtkUnitBase* unit, bool allTextures = false)
    {
        var text = new StringBuilder();
        var count = 0;
        void Walk(AtkUldManager* manager, int depth)
        {
            if (manager == null || depth > 4) return;
            for (var index = 0; index < manager->NodeListCount && count < 160; index++)
            {
                var node = manager->NodeList[index];
                if (node == null || !node->IsVisible()) continue;
                if (node->Type == NodeType.Image)
                {
                    var image = (AtkImageNode*)node;
                    var path = string.Empty;
                    if (image->PartsList != null && image->PartId < image->PartsList->PartCount)
                    {
                        var asset = image->PartsList->Parts[image->PartId].UldAsset;
                        if (asset != null && asset->AtkTexture.Resource != null && asset->AtkTexture.Resource->TexFileResourceHandle != null)
                            path = asset->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName.ToString();
                    }
                    if (allTextures || path.Contains("icon", StringComparison.OrdinalIgnoreCase))
                    { text.Append($"{node->NodeId}:{node->ScreenX:0},{node->ScreenY:0},{node->Width * node->ScaleX:0}x{node->Height * node->ScaleY:0}:{path};"); count++; }
                }
                else if ((int)node->Type >= 1000) Walk(((AtkComponentNode*)node)->Component == null ? null : &((AtkComponentNode*)node)->Component->UldManager, depth + 1);
            }
        }
        Walk(&unit->UldManager, 0);
        var line = text.ToString();
        if (line == lastEditDeck) return;
        lastEditDeck = line;
        log.Information("Triad {Window} capture: values={Values} images={Images}", unit->NameString, Values(unit), line);
    }
    // Visible card images of the editor's list: texture ui/icon/088000/088NNN(_hr1).tex = card NNN.
    private static unsafe void ListedCards(AtkUldManager* manager, int depth, List<(uint, TriadRect)> cards)
    {
        if (manager == null || depth > 4) return;
        for (var index = 0; index < manager->NodeListCount && cards.Count < 200; index++)
        {
            var node = manager->NodeList[index];
            if (node == null || !node->IsVisible()) continue;
            if (node->Type == NodeType.Image)
            {
                var image = (AtkImageNode*)node;
                if (image->PartsList == null || image->PartId >= image->PartsList->PartCount) continue;
                var asset = image->PartsList->Parts[image->PartId].UldAsset;
                if (asset == null || asset->AtkTexture.Resource == null || asset->AtkTexture.Resource->TexFileResourceHandle == null) continue;
                var path = asset->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName.ToString();
                // Large card images use icon 088NNN, small ones 087NNN (both listed with each card in the editor's window values).
                var at = path.IndexOf("icon/088000/", StringComparison.Ordinal); var baseIcon = 88000u;
                if (at < 0) { at = path.IndexOf("icon/087000/", StringComparison.Ordinal); baseIcon = 87000; }
                if (at >= 0 && path.Length >= at + 18 && uint.TryParse(path.AsSpan(at + 12, 6), out var icon) && icon > baseIcon && icon < baseIcon + 1000)
                    cards.Add((icon - baseIcon, new TriadRect(node->ScreenX, node->ScreenY, node->Width * node->ScaleX, node->Height * node->ScaleY)));
            }
            else if ((int)node->Type >= 1000 && ((AtkComponentNode*)node)->Component != null)
                ListedCards(&((AtkComponentNode*)node)->Component->UldManager, depth + 1, cards);
        }
    }
    // Deck choice window: card images grouped by row (same height on screen), five cards per saved deck.
    private unsafe bool ReadSelDeck(int ticket, ulong character)
    {
        var handle = gui.GetAddonByName("TripleTriadSelDeck");
        if (handle.IsNull || !handle.IsReady || !handle.IsVisible || gui.GameUiHidden) { if (service.SelDeckRows.Count > 0) service.PublishSelDeck(ticket, character, []); return false; }
        var cards = new List<(uint Id, TriadRect Rect)>();
        if (capture()) CaptureEditDeck((AtkUnitBase*)handle.Address, true);
        ListedCards(&((AtkUnitBase*)handle.Address)->UldManager, 0, cards);
        var rows = cards.GroupBy(card => (int)MathF.Round(card.Rect.Y / 8)).Select(group =>
        {
            var items = group.OrderBy(card => card.Rect.X).ToArray();
            var left = items[0].Rect; var right = items[^1].Rect;
            return ((IReadOnlyList<uint>)Array.AsReadOnly(items.Select(card => card.Id).ToArray()),
                new TriadRect(left.X, left.Y, right.X + right.Width - left.X, left.Height));
        }).OrderBy(row => row.Item2.Y).ToArray();
        service.PublishSelDeck(ticket, character, rows);
        if (capture() && rows.Length != lastSelDeckRows)
        {
            lastSelDeckRows = rows.Length;
            log.Information("Triad deck choice: {Rows} row(s) {Cards}, challenge kept: {Request}", rows.Length,
                string.Join(" | ", rows.Select(row => string.Join(".", row.Item1))), service.Request is { } request ? $"NPC {request.NpcId}" : "no");
        }
        return true;
    }
    private void LoadDeck(uint npc)
    {
        deckNpc = npc; deckFixed = deckVariable = [];
        deckNpcName = data is not null && data.GetExcelSheet<ENpcResident>().TryGetRow(npc, out var resident) ? resident.Singular.ExtractText() : string.Empty;
        if (data is null || !data.GetExcelSheet<ENpcBase>().TryGetRow(npc, out var row)) return;
        var matches = data.GetExcelSheet<TripleTriad>();
        foreach (var id in row.ENpcData.Select(value => value.RowId))
            if (id != 0 && matches.TryGetRow(id, out var match))
            {
                deckFixed = match.TripleTriadCardFixed.Select(card => card.RowId).Where(card => card != 0).ToArray();
                deckVariable = match.TripleTriadCardVariable.Select(card => card.RowId).Where(card => card != 0).ToArray();
                return;
            }
    }
    private static unsafe TriadRect[] Rects(ReadOnlySpan<AddonTripleTriad.TripleTriadCard> cards, float scale)
    {
        var rects = new TriadRect[cards.Length];
        for (var index = 0; index < cards.Length; index++)
        {
            var component = cards[index].CardDropControl;
            if (component == null || component->OwnerNode == null) return [];
            var node = &component->OwnerNode->AtkResNode;
            rects[index] = new(node->ScreenX, node->ScreenY, node->Width * node->ScaleX * scale, node->Height * node->ScaleY * scale);
        }
        return rects;
    }
    // NumSideR and NumSideL are swapped in FFXIVClientStructs: the five cards of the captured match only match their TripleTriadCardResident
    // rows with right = NumSideL and left = NumSideR (e.g. Garuda U7 R6 D1 L7 read U7 « R »7 D1 « L »6). The capture log keeps the raw order.
    private static unsafe TriadRawCard[] Copy(ReadOnlySpan<AddonTripleTriad.TripleTriadCard> cards)
    {
        var result = new TriadRawCard[cards.Length];
        for (var index = 0; index < result.Length; index++)
        {
            var card = cards[index];
            result[index] = new(card.HasCard,(byte)card.CardOwner,card.CardRarity,(byte)card.CardType,
                card.NumSideU,card.NumSideL,card.NumSideD,card.NumSideR);
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
