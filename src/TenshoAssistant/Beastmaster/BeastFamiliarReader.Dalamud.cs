using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using TenshoAssistant.Competitive;

namespace TenshoAssistant.Beastmaster;

/// <summary>
/// X-655 (0.1599): the beast called, for the combo highlight, read on the framework thread twice a second while the option is on: the
/// player's own battle NPCs (owner = the player) and the battlehorns window's beast (BeastBoardReader), resolved by BeastCalled. Said in the
/// log at each change, with the name, name row, base row and kind of each battle NPC of the player, so an in-game test tells what the game
/// gives (docs/evidence/0.1599-beast-called.md). Reads only: nothing is sent to the game (R03.3).
/// </summary>
internal sealed class BeastFamiliarReader : IDisposable
{
    private const int FramesBetweenReads = 30, MaximumOwned = 8;
    private readonly IFramework framework;
    private readonly IObjectTable objects;
    private readonly IPluginLog log;
    private readonly Func<bool> wanted;
    private readonly Func<IReadOnlyList<Beast>?> beasts;
    private readonly Func<uint> window;
    private int frames;
    private bool failed;
    private string said = "";
    private volatile BeastCalledRead called = BeastCalledRead.None;

    public BeastCalledRead Called => called;

    public BeastFamiliarReader(IFramework framework, IObjectTable objects, IPluginLog log, Func<bool> wanted, Func<IReadOnlyList<Beast>?> beasts, Func<uint> window)
    {
        this.framework = framework; this.objects = objects; this.log = log; this.wanted = wanted; this.beasts = beasts; this.window = window;
        CompetitiveGate.Add(framework, OnUpdate);
    }

    private void OnUpdate(IFramework _)
    {
        if (!wanted()) { called = BeastCalledRead.None; said = ""; return; }
        if (failed || ++frames < FramesBetweenReads) return;
        frames = 0;
        try
        {
            var names = new List<string>();
            var shown = new List<string>();
            if (objects.LocalPlayer is { } player)
                foreach (var actor in objects)
                {
                    if (actor is not IBattleNpc npc || npc.OwnerId != player.EntityId || npc.EntityId == player.EntityId) continue;
                    var name = npc.Name.TextValue;
                    names.Add(name);
                    shown.Add($"{name}/{npc.NameId}/{npc.BaseId}/{npc.BattleNpcKind}");
                    if (names.Count >= MaximumOwned) break;
                }
            var windowBeast = window();
            called = BeastCalled.Resolve(names, windowBeast, beasts());
            var line = $"{called.Beast}|{called.Source}|{windowBeast}|{string.Join(";", shown)}";
            if (line == said) return;
            said = line;
            log.Information("Beastmaster beast called: beast {0} ({1}); battlehorns window beast {2}; the player's battle NPCs: {3}.",
                called.Beast, called.Source, windowBeast, shown.Count == 0 ? "none" : string.Join(", ", shown));
        }
        catch (Exception error)
        {
            failed = true; called = BeastCalledRead.None;
            log.Warning(error, "The beast called could not be read (maybe after a game update): no combo highlight until the plugin reloads.");
        }
    }

    public void Dispose() => CompetitiveGate.Remove(framework, OnUpdate);
}
