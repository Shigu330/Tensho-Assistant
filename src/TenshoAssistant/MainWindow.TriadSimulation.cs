using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Items;
using TenshoAssistant.MiniGames;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private readonly TriadSolveSession triadSolve = new();
    private readonly List<uint> triadBlueHand = [];
    private readonly List<uint> triadRedHand = [];
    private readonly Stack<TriadPosition> triadUndo = [];
    private TriadPosition? triadPosition;
    private int triadRevision;
    private int triadSelectedToken = -1;
    private int triadAddHand = 1;
    private bool triadRedStarts;
    private bool triadRulesConfirmed;
    private bool triadReverse;
    private bool triadFallenAce;
    private bool triadSame;
    private bool triadPlus;
    private bool triadOrder;
    private int triadFamilyRule;
    private bool triadEditingHands = true;
    private string triadSimulationQuery = "";
    private string? triadSimulationError;
    private static readonly Vector4 TriadBlue = new(0.35f, 0.65f, 1, 1);
    private static readonly Vector4 TriadRed = new(1, 0.45f, 0.4f, 1);
    private TriadRules SelectedTriadRules => (triadReverse ? TriadRules.Reverse : TriadRules.Normal)
        | (triadFallenAce ? TriadRules.FallenAce : TriadRules.Normal)
        | (triadSame ? TriadRules.Same : TriadRules.Normal)
        | (triadPlus ? TriadRules.Plus : TriadRules.Normal)
        | (triadOrder ? TriadRules.Order : TriadRules.Normal)
        | (triadFamilyRule == 1 ? TriadRules.Ascension : triadFamilyRule == 2 ? TriadRules.Descension : TriadRules.Normal);

    private void InvalidateTriadSimulation(bool resetBoard)
    {
        triadRevision++; triadSolve.Cancel(); triadSimulationError = null;
        if (resetBoard) { triadPosition = null; triadUndo.Clear(); triadSelectedToken = -1; triadEditingHands = true; }
    }
    private void AddTriadSimulationCard(uint id, bool red)
    {
        var hand = red ? triadRedHand : triadBlueHand;
        if (hand.Count >= 5 || hand.Contains(id))
        { triadSimulationError = localization.Get("TriadSim.Full"); return; }
        hand.Add(id); InvalidateTriadSimulation(true);
    }

    private void DrawTriadSimulation()
    {
        ImGui.TextWrapped(localization.Get("TriadSim.Help"));
        var rulesChanged = ImGui.Checkbox(localization.Get("TriadSim.Reverse"), ref triadReverse);
        ImGui.SameLine();
        rulesChanged |= ImGui.Checkbox(localization.Get("TriadSim.FallenAce"), ref triadFallenAce);
        ImGui.SameLine();
        rulesChanged |= ImGui.Checkbox(localization.Get("TriadSim.Same"), ref triadSame);
        ImGui.SameLine();
        rulesChanged |= ImGui.Checkbox(localization.Get("TriadSim.Plus"), ref triadPlus);
        rulesChanged |= ImGui.Checkbox(localization.Get("TriadSim.Order"), ref triadOrder);
        ImGui.SameLine(); ImGui.SetNextItemWidth(150);
        rulesChanged |= ImGui.Combo(localization.Get("TriadSim.Families"), ref triadFamilyRule, new[] {localization.Get("TriadSim.NoEffect"), localization.Get("TriadSim.Ascension"), localization.Get("TriadSim.Descension")}, 3);
        if (rulesChanged) { InvalidateTriadSimulation(true); triadRulesConfirmed = false; }
        if (ImGui.Checkbox(localization.Get("TriadSim.Confirm"), ref triadRulesConfirmed)) InvalidateTriadSimulation(false);
        ImGui.TextWrapped(localization.Get("TriadSim.RulesHelp"));
        if (ImGui.RadioButton(localization.Get("TriadSim.Initial"), triadEditingHands)) triadEditingHands = true;
        ImGui.SameLine();
        if (ImGui.RadioButton(localization.Get("TriadSim.BoardAdvice"), !triadEditingHands)) triadEditingHands = false;
        if (triadEditingHands)
        {
            ImGui.TextWrapped(localization.Get("TriadSim.OrderHelp"));
            if (ImGui.SmallButton(localization.Get("TriadSim.LoadDeck")))
            {
                triadBlueHand.Clear(); triadBlueHand.AddRange(triadDeck!.Take(5));
                InvalidateTriadSimulation(true);
            }
            ImGui.SameLine();
            if (ImGui.SmallButton(localization.Get("TriadSim.Clear")))
            { triadBlueHand.Clear(); triadRedHand.Clear(); InvalidateTriadSimulation(true); }
            ImGui.SetNextItemWidth(240);
            ImGui.InputTextWithHint("##SimulationCardSearch", localization.Get("TriadSim.Search"), ref triadSimulationQuery, 128);
            if (ImGui.RadioButton(localization.Get("TriadSim.AddBlue"), triadAddHand == 0)) triadAddHand = 0;
            ImGui.SameLine();
            if (ImGui.RadioButton(localization.Get("TriadSim.AddRed"), triadAddHand == 1)) triadAddHand = 1;
            ImGui.SetNextItemWidth(320);
            if (ImGui.BeginCombo("##AddSimulationCard", localization.Get("TriadSim.Choose")))
            {
                try
                {
                    var target = triadAddHand == 0 ? triadBlueHand : triadRedHand;
                    var query = triadSimulationQuery.Trim();
                    foreach (var entry in triadCards.Where(entry => query.Length == 0
                        || entry.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || entry.Card!.Number.ToString() == query))
                    {
                        ImGui.BeginDisabled(target.Count >= 5 || target.Contains(entry.Id));
                        try
                        {
                            if (ImGui.Selectable($"{entry.Card!.Number} · {entry.Name}##Sim{entry.Id}"))
                                AddTriadSimulationCard(entry.Id, triadAddHand == 1);
                        }
                        finally { ImGui.EndDisabled(); }
                    }
                }
                finally { ImGui.EndCombo(); }
            }
            DrawTriadHand(false, true);
            DrawTriadHand(true, true);
            if (ImGui.Checkbox(localization.Get("TriadSim.RedStarts"), ref triadRedStarts)) InvalidateTriadSimulation(true);
            if (ImGui.Button(localization.Get("TriadSim.Start")))
            {
                InvalidateTriadSimulation(true);
                try
                {
                    TriadCardDefinition Resolve(uint id) => triadCards.FirstOrDefault(entry => entry.Id == id)?.Card
                        ?? throw new InvalidOperationException($"Carte indisponible : ID {id}.");
                    triadPosition = TriadPosition.Create(triadBlueHand.Select(Resolve), triadRedHand.Select(Resolve), triadRedStarts ? TriadPlayer.Red : TriadPlayer.Blue, SelectedTriadRules);
                    triadEditingHands = false;
                }
                catch (Exception error) { triadSimulationError = error.Message; }
            }
        }
        if (triadSimulationError is { } message) DrawDataError(message,"ServiceErrors.Triad");
        if (triadEditingHands) return;
        if (triadPosition is not { } position) { ImGui.TextWrapped(localization.Get("TriadSim.MissingHands")); return; }
        ImGui.Separator();
        ImGui.TextColored(position.Turn == TriadPlayer.Blue ? TriadBlue : TriadRed,
            position.IsTerminal ? localization.Get("TriadSim.Finished") : localization.Format("TriadDynamic.Turn",localization.Get(position.Turn==TriadPlayer.Blue ? "TriadSim.Blue":"TriadSim.Red")));
        ImGui.TextUnformatted(localization.Format("TriadSimCounts.Score",position.BlueScore,position.RedScore));
        if (triadSelectedToken >= 0 && !position.CanPlayCard(triadSelectedToken)) triadSelectedToken = -1;
        if (ImGui.GetContentRegionAvail().X >= 620 && ImGui.BeginTable("TriadBoardAndHands", 2, ImGuiTableFlags.SizingFixedFit))
        {
            try
            {
                ImGui.TableSetupColumn(localization.Get("TriadSim.Board"), ImGuiTableColumnFlags.WidthFixed, 280);
                ImGui.TableSetupColumn(localization.Get("TriadSim.Hands"), ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow(); ImGui.TableNextColumn(); DrawTriadBoard(position);
                ImGui.TableNextColumn(); DrawTriadHand(false); DrawTriadHand(true);
            }
            finally { ImGui.EndTable(); }
        }
        else { DrawTriadBoard(position); DrawTriadHand(false); DrawTriadHand(true); }
        ImGui.BeginDisabled(triadUndo.Count == 0);
        try
        {
            if (ImGui.SmallButton(localization.Get("TriadSim.Undo")))
            { triadPosition = triadUndo.Pop(); triadSelectedToken = -1; InvalidateTriadSimulation(false); }
        }
        finally { ImGui.EndDisabled(); }
        DrawTriadAdvice();
    }

    private void DrawTriadBoard(TriadPosition position)
    {
        if (ImGui.BeginTable("TriadManualBoard", 3, ImGuiTableFlags.SizingFixedFit))
        {
            try
            {
                for (var column = 0; column < 3; column++) ImGui.TableSetupColumn($"BoardColumn{column}", ImGuiTableColumnFlags.WidthFixed, 90);
                for (var square = 0; square < 9; square++)
                {
                    ImGui.TableNextColumn(); ImGui.PushID(square);
                    try
                    {
                        var token = position.CardAt(square);
                        if (token < 0)
                        {
                            ImGui.BeginDisabled(triadSelectedToken < 0 || position.IsTerminal);
                            try
                            {
                                if (ImGui.Button(localization.Format("UiFragments.Square",square/3+1,square%3+1), new Vector2(80, 80)))
                                    ApplyTriadMove(new(triadSelectedToken, square));
                            }
                            finally { ImGui.EndDisabled(); }
                        }
                        else
                        {
                            var definition = position.Cards[token];
                            var entry = triadCards.FirstOrDefault(entry => entry.Id == definition.Id);
                            DrawItemIcon(new ItemDefinition(0, entry?.Name ?? localization.Format("UiFragments.Card",definition.Number), false, definition.IconId), new Vector2(62));
                            if (ImGui.IsItemHovered()) WrappedTooltip(TriadSimulationTooltip(entry?.Name, definition, token, position));
                            var blue = position.OwnerAt(square) == TriadPlayer.Blue;
                            ImGui.TextColored(blue ? TriadBlue : TriadRed, $"{(blue ? localization.Get("TriadSim.Blue") : localization.Get("TriadSim.Red"))} · {definition.Number}");
                        }
                    }
                    finally { ImGui.PopID(); }
                }
            }
            finally { ImGui.EndTable(); }
        }
    }

    private void DrawTriadHand(bool red, bool editing = false)
    {
        var hand = red ? triadRedHand : triadBlueHand;
        var count = !editing && triadPosition is { } current
            ? localization.Format("TriadDynamic.InHand",Enumerable.Range(red ? 5 : 0, 5).Count(current.InHand))
            : localization.Format("TriadDynamic.Initial",hand.Count);
        ImGui.TextColored(red ? TriadRed : TriadBlue, localization.Format("TriadDynamic.Hand",localization.Get(red ? "TriadSim.Red":"TriadSim.Blue"),count));
        var remove = -1;
        var swapFrom = -1; var swapTo = -1;
        if (!ImGui.BeginTable(red ? "RedHand" : "BlueHand", 5, ImGuiTableFlags.SizingFixedFit)) return;
        try
        {
            for (var index = 0; index < 5; index++) ImGui.TableSetupColumn($"Hand{index}", ImGuiTableColumnFlags.WidthFixed, 58);
            for (var index = 0; index < 5; index++)
            {
                ImGui.TableNextColumn(); ImGui.PushID(index + (red ? 5 : 0));
                try
                {
                    if (index >= hand.Count) { ImGui.Dummy(new Vector2(44, 44)); continue; }
                    var entry = triadCards.FirstOrDefault(entry => entry.Id == hand[index]);
                    var token = index + (red ? 5 : 0);
                    var playable = triadPosition is { } position && position.CanPlayCard(token);
                    ImGui.BeginDisabled(triadPosition is not null && !playable);
                    try
                    {
                        DrawItemIcon(new ItemDefinition(0, entry?.Name ?? $"ID {hand[index]}", false, entry?.IconId ?? 0), new Vector2(44));
                        if (playable && ImGui.IsItemClicked()) triadSelectedToken = token;
                        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) WrappedTooltip(entry?.Card is { } card
                            ? TriadSimulationTooltip(entry.Name,card,token,triadPosition) + "\n"+localization.Get("TriadDynamic.Click")
                            : localization.Get("TriadSim.Unavailable"));
                        var caption = !editing && triadPosition is { } board && !board.InHand(token)
                            ? localization.Get("TriadSim.Played") : triadSelectedToken == token ? localization.Get("TriadSim.Selected") : entry?.Card?.Number.ToString() ?? "?";
                        ImGui.TextColored(triadSelectedToken == token ? Accent : Muted, caption);
                    }
                    finally { ImGui.EndDisabled(); }
                    if (editing)
                    {
                        if (ImGui.SmallButton(localization.Get("TriadSim.Remove"))) remove = index;
                        ImGui.BeginDisabled(index == 0);
                        try { if (ImGui.SmallButton("<")) { swapFrom = index; swapTo = index - 1; } }
                        finally { ImGui.EndDisabled(); }
                        ImGui.SameLine(); ImGui.BeginDisabled(index == hand.Count - 1);
                        try { if (ImGui.SmallButton(">")) { swapFrom = index; swapTo = index + 1; } }
                        finally { ImGui.EndDisabled(); }
                    }
                }
                finally { ImGui.PopID(); }
            }
        }
        finally { ImGui.EndTable(); }
        if (remove >= 0) { hand.RemoveAt(remove); InvalidateTriadSimulation(true); }
        else if (swapFrom >= 0) { (hand[swapFrom],hand[swapTo]) = (hand[swapTo],hand[swapFrom]); InvalidateTriadSimulation(true); }
    }

    private string TriadSimulationTooltip(string? name, TriadCardDefinition card, int token, TriadPosition? position)
    {
        static string Values(int top,int right,int bottom,int left) => $"↑{TriadCardDefinition.Value(top)} →{TriadCardDefinition.Value(right)} ↓{TriadCardDefinition.Value(bottom)} ←{TriadCardDefinition.Value(left)}";
        var text = localization.Format("TriadDynamic.Base",name ?? card.Number.ToString(),Values(card.Top,card.Right,card.Bottom,card.Left));
        if (position is { } state)
            text += "\n"+localization.Format("TriadDynamic.Effective",Values(state.EffectiveSide(token,0),state.EffectiveSide(token,1),state.EffectiveSide(token,2),state.EffectiveSide(token,3)),state.FamilyModifier(token));
        return text;
    }

    private void ApplyTriadMove(TriadMove move)
    {
        if (triadPosition is not { } previous) return;
        try
        {
            var next = previous.Play(move);
            triadUndo.Push(previous); triadPosition = next; triadSelectedToken = -1;
            InvalidateTriadSimulation(false);
        }
        catch (Exception error) { triadSimulationError = error.Message; }
    }

    private void DrawTriadAdvice()
    {
        // The board may have changed earlier in this frame; always use the authoritative snapshot.
        if (triadPosition is not { } position) return;
        var state = triadSolve.State;
        ImGui.BeginDisabled(!triadRulesConfirmed || position.IsTerminal || state.Loading);
        try
        {
            if (ImGui.Button(localization.Get("TriadSim.Advice")))
                _ = triadSolve.StartAsync(position, bankBridge.State.CharacterId, triadRevision);
        }
        finally { ImGui.EndDisabled(); }
        if (state.Loading)
        {
            ImGui.SameLine();
            if (ImGui.SmallButton(localization.Get("TriadSim.Cancel"))) triadSolve.Cancel();
            ImGui.TextDisabled(localization.Get("TriadSim.Searching"));
        }
        if (state.Error is { } error) DrawDataError(error,"ServiceErrors.Triad");
        if (state.Result is not { } result || state.Revision != triadRevision) return;
        if (result.Moves.Count == 0) { ImGui.TextWrapped(localization.Get("TriadSim.NoDepth")); return; }
        ImGui.TextColored(result.Exact ? Success : Accent, result.Exact ? localization.Get("TriadSim.Exact") : localization.Get("TriadSim.Estimate"));
        ImGui.TextDisabled(localization.Format("TriadSimCounts.Depth",result.CompletedDepth,result.Nodes));
        var best = result.Best(position.Turn);
        foreach (var candidate in best.Take(8))
        {
            var card = position.Cards[candidate.Move.CardIndex];
            var entry = triadCards.FirstOrDefault(entry => entry.Id == card.Id);
            var blue = (10 + candidate.BlueDifference) / 2;
            ImGui.PushID($"Advice{candidate.Move.CardIndex}/{candidate.Move.Square}");
            try
            {
                if (ImGui.SmallButton(localization.Format("UiFragments.Move",entry?.Name ?? localization.Format("UiFragments.Card",card.Number),candidate.Move.Square/3+1,candidate.Move.Square%3+1,blue,10-blue)))
                { ApplyTriadMove(candidate.Move); break; }
                if (ImGui.IsItemHovered()) WrappedTooltip(localization.Get("TriadSim.Apply"));
            }
            finally { ImGui.PopID(); }
        }
        if (best.Count > 8) ImGui.TextDisabled(localization.Format("TriadSimCounts.Tied",best.Count));
        ImGui.TextWrapped(localization.Get("TriadSim.Assumptions"));
    }
}
