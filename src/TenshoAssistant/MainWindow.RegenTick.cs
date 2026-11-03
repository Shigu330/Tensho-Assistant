using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gathering;

namespace TenshoAssistant;

// g, X-06 (0.391): the on-screen GP tick bar of gatherers (RegenTickReader; off by default, its switch on the GP page): GP now and at
// most, and the time to the next regeneration tick. Hidden for other jobs, in combat and once GP is full.
internal sealed partial class MainWindow
{
    internal Func<RegenTickState?>? RegenTick { get; set; }

    internal void DrawRegenTickBar()
    {
        if(windowDisposed || !configuration.GpTickBar || RegenTick?.Invoke() is not { } tick || tick.Gp>=tick.MaxGp)return;
        UI.FfxivTheme.Push();
        try
        {
            ImGui.SetNextWindowPos(new Vector2(40,320),ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowBgAlpha(0.7f);
            if(ImGui.Begin("###TenshoGpTick",ImGuiWindowFlags.NoTitleBar|ImGuiWindowFlags.AlwaysAutoResize|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoNav
                   |ImGuiWindowFlags.NoScrollbar))
            {
                var text=tick.SecondsLeft is { } left ? localization.Format("GpTick.Bar",tick.Gp,tick.MaxGp,left.ToString("0.0")):localization.Format("GpTick.Waiting",tick.Gp,tick.MaxGp);
                ImGui.ProgressBar((float)(tick.Progress ?? 0),new Vector2(180,ImGui.GetFrameHeight()),text);
                if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("GpTick.Tip"));
            }
            ImGui.End();
        }
        finally{UI.FfxivTheme.Pop();}
    }
}
