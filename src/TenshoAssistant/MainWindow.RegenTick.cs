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
        // 0.488 (user request): the measure gives the base; the traits each job has unlocked are added back. The last one seen, so a wrong value does not stay.
        if(!windowDisposed && RegenTick?.Invoke() is {Gain:>0 and <=50,JobId:>0} measured && GpRegen.Base(measured.Gain,GpTraitBonus(measured.JobId)) is >0 and var measuredBase && measuredBase!=configuration.GpRegenBase){configuration.GpRegenBase=measuredBase;SaveReminders();}
        if(windowDisposed || !configuration.GpTickBar || RegenTick?.Invoke() is not { } tick || tick.Gp>=tick.MaxGp)return;
        UI.FfxivTheme.Push();
        try
        {
            ImGui.SetNextWindowPos(new Vector2(40,320),ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowBgAlpha(0.7f);
            if(ImGui.Begin("###TenshoGpTick",ImGuiWindowFlags.NoTitleBar|ImGuiWindowFlags.AlwaysAutoResize|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoNav
                   |ImGuiWindowFlags.NoScrollbar))
            {
                var gainPerTick=GpRegenFor(tick.JobId);
                var full=RegenTickTracker.ToFull(tick,gainPerTick);
                var text=tick.Stalled ? localization.Format("GpTick.Stalled",tick.Gp,tick.MaxGp):full is { } rest ? localization.Format("GpTick.Full",tick.Gp,tick.MaxGp,$"{(int)rest.TotalMinutes}:{rest.Seconds:00}"):tick.SecondsLeft is { } left ? localization.Format("GpTick.Bar",tick.Gp,tick.MaxGp,left.ToString("0.0")):localization.Format("GpTick.Waiting",tick.Gp,tick.MaxGp);
                // 0.486 (user request): the bar fills towards full GP; a small highlight runs along its foot for the tick.
                ImGui.ProgressBar(tick.MaxGp>0 ? Math.Clamp((float)tick.Gp/tick.MaxGp,0,1):0,new Vector2(180,ImGui.GetFrameHeight()),text);
                if(ImGui.IsItemHovered())WrappedTooltip(tick.SecondsLeft is { } tip ? localization.Format("GpTick.Next",tip.ToString("0.0"))+Environment.NewLine+localization.Get("GpTick.Tip"):localization.Get("GpTick.Tip"));
                if(tick.Progress is { } running)
                {
                    var from=ImGui.GetItemRectMin();var to=ImGui.GetItemRectMax();
                    const float Mark=28,Height=3;
                    var x=from.X+(to.X-from.X-Mark)*(float)Math.Clamp(running,0,1);
                    ImGui.GetWindowDrawList().AddRectFilled(new Vector2(x,to.Y-Height),new Vector2(x+Mark,to.Y),ImGui.GetColorU32(Accent));
                }
            }
            ImGui.End();
        }
        finally{UI.FfxivTheme.Pop();}
    }
}
