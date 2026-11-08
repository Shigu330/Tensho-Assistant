using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using TenshoAssistant.UI;

namespace TenshoAssistant.Beastmaster;

/// <summary>
/// Probe (0.501, developer mode only): while one of the game's Beastmaster windows is open, writes to the log the texts it shows and its window
/// values, each time they change. The rank of a beast and the weakness of an enemy are only known from these windows; this finds where. Read-only.
/// The ranking and result windows are left out: they name other players or the character.
/// </summary>
internal sealed class BeastWindowProbe : IDisposable
{
    private static readonly string[] Windows = ["XBMMonsterBookDetail", "XBMPetParty", "XBMBattleMonsterDetail", "XBMBattleMonster", "XBMStageDetailList"];
    private const int MaximumDepth = 4, MaximumNodes = 2000, MaximumTexts = 300, MaximumValues = 400, MaximumText = 60, MaximumLinesPerWindow = 60;
    private readonly IFramework framework;
    private readonly IGameGui gui;
    private readonly IPluginLog log;
    private readonly Func<bool> developerMode;
    private readonly Dictionary<string, string> last = [];
    private readonly Dictionary<string, int> lines = [];
    private DateTimeOffset nextRead;
    private bool failed;

    public BeastWindowProbe(IFramework framework, IGameGui gui, IPluginLog log, Func<bool> developerMode)
    {
        this.framework = framework; this.gui = gui; this.log = log; this.developerMode = developerMode;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    private static string Bounded(string text)
    {
        var line = text.ReplaceLineEndings(" ").Replace(';', ',').Trim();
        return line.Length <= MaximumText ? line : line[..MaximumText];
    }

    private static unsafe void Texts(AtkUldManager* manager, int depth, List<string> found)
    {
        if (depth > MaximumDepth || !UiNodes.Walkable(manager)) return;
        var count = Math.Min((int)manager->NodeListCount, MaximumNodes);
        for (var index = 0; index < count && found.Count < MaximumTexts; index++)
        {
            var node = manager->NodeList[index];
            if (node == null || !node->IsVisible()) continue;
            if (node->Type == NodeType.Text)
            {
                var text = Bounded(((AtkTextNode*)node)->NodeText.ToString());
                if (text.Length > 0) found.Add(text);
            }
            else if ((int)node->Type >= 1000 && node->GetComponent() is var component && component != null)
                Texts(&component->UldManager, depth + 1, found);
        }
    }

    private static unsafe string Values(AtkUnitBase* unit)
    {
        if (unit->AtkValues == null) return "";
        var values = new List<string>();
        for (var index = 0; index < Math.Min((int)unit->AtkValuesCount, MaximumValues); index++)
        {
            var value = unit->AtkValues[index];
            var text = value.Type switch
            {
                AtkValueType.Int => value.Int.ToString(),
                AtkValueType.UInt => value.UInt.ToString(),
                AtkValueType.Bool => value.Byte != 0 ? "true" : "false",
                _ when AtkValueRead.IsText(value.Type) => '"' + Bounded(value.String.ToString() ?? "") + '"',
                _ => null,
            };
            if (text is not null) values.Add($"{index}:{text}");
        }
        return string.Join(";", values);
    }

    private unsafe void OnUpdate(IFramework _)
    {
        if (failed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading) return;
        var now = DateTimeOffset.UtcNow;
        if (now < nextRead || !developerMode()) return;
        nextRead = now + TimeSpan.FromSeconds(1);
        try
        {
            foreach (var name in Windows)
            {
                var handle = gui.GetAddonByName(name);
                if (handle.IsNull || !handle.IsReady || !handle.IsVisible) { last.Remove(name); continue; }
                var unit = (AtkUnitBase*)handle.Address;
                var texts = new List<string>();
                Texts(&unit->UldManager, 0, texts);
                var capture = $"texts={string.Join(";", texts)} values={Values(unit)}";
                if (last.GetValueOrDefault(name) == capture) continue;
                last[name] = capture;
                var written = lines.GetValueOrDefault(name);
                if (written >= MaximumLinesPerWindow) continue;
                lines[name] = written + 1;
                log.Information("Beast window {0}: {1}", name, capture);
            }
        }
        catch (Exception error)
        {
            failed = true;
            log.Warning(error, "Beast window probe stopped.");
        }
    }

    public void Dispose() => TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
}
