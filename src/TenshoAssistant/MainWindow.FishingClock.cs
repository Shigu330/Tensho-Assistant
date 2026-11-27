using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gathering;

namespace TenshoAssistant;

/// <summary>
/// X-701 and X-702 (0.1685, user requests of 21/11 at sea): the stopwatch of the fishing help and of ocean fishing stops at the bite read in
/// the game (FishingLine: line state 5, strength from the animation) and stays frozen until the next cast, so the fish whose bite window holds
/// that time are put forward; under Chum (status 763 at the cast) the bite times are halved, the user's rule (the game and the source give no
/// time). Display only: nothing is sent to the game (R03.3). One way for both panels.
/// </summary>
internal sealed partial class MainWindow
{
    /// <summary>X-701: the last cast read by the bait reader (bite, strength, Chum).</summary>
    internal Func<FishingCast?>? FishingLastCast { get; set; }

    /// <summary>What the stopwatch shows: its seconds, frozen at the bite or running, the bite's strength (0 unknown), Chum at the cast.</summary>
    private readonly record struct FishingClock(double Seconds,bool Frozen,int Strength,bool Chum);

    /// <summary>The stopwatch started at <paramref name="since"/> (null: not fishing, nothing shown).</summary>
    private FishingClock? ReadFishingClock(DateTimeOffset? since)
    {
        if(since is null)return null;
        var cast=FishingLastCast?.Invoke();
        var frozen=FishingStopwatch.Frozen(since,cast?.Bite);
        return new FishingClock(FishingStopwatch.Elapsed(since,cast?.Bite,DateTimeOffset.UtcNow) ?? 0,frozen,frozen ? cast!.Strength:0,cast?.Chum==true);
    }

    private static string BiteMark(int strength)=>strength switch{1=>"!",2=>"!!",3=>"!!!",_=>""};

    /// <summary>The stopwatch's line: running, or frozen at the bite with its strength; Chum said; the manual restart kept.</summary>
    private void DrawFishingClock(FishingClock clock,ref DateTimeOffset? since,string id)
    {
        if(clock.Frozen)
        {
            ImGui.TextColored(Accent,localization.Format("FishingPanel.Frozen",clock.Seconds,BiteMark(clock.Strength)).TrimEnd());
            Tip("FishingPanel.FrozenTip");
        }
        else
        {
            ImGui.TextColored(Accent,localization.Format("FishingPanel.Elapsed",(int)clock.Seconds));
            Tip("FishingPanel.ElapsedTip");
        }
        if(clock.Chum)
        {
            ImGui.SameLine();
            ImGui.TextColored(Warning,localization.Get("FishingPanel.Chum"));
            Tip("FishingPanel.ChumTip");
        }
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("FishingPanel.Restart")+id))since=DateTimeOffset.UtcNow;
        Tip("FishingPanel.RestartHelp");
    }

    /// <summary>
    /// A fish's bite time against the stopwatch: green inside its window, red once passed (X-98, 0.958); frozen at the bite, the fish whose
    /// window holds that time are put forward (X-701); under Chum, the window halved (X-702).
    /// </summary>
    private void DrawBiteTimeCell(Community.FishBiteTime time,FishingClock? clock)
    {
        var chum=clock?.Chum==true;
        var text=chum
            ? localization.Format("FishingPanel.BiteShortChum",FishingStopwatch.Shown(time.Minimum,true),FishingStopwatch.Shown(time.Maximum,true),FishingStopwatch.Shown(time.Usual,true))
            : localization.Format("FishingPanel.BiteShort",time.Minimum,time.Maximum,time.Usual);
        var inside=clock is { } shown && FishingStopwatch.Inside(shown.Seconds,time.Minimum,time.Maximum,chum);
        if(inside && clock!.Value.Frozen)ImGui.TextColored(Selected,"» "+text);
        else if(inside)ImGui.TextColored(Success,text);
        else if(clock is { } passed && FishingStopwatch.Passed(passed.Seconds,time.Maximum,chum))ImGui.TextColored(Error,text);
        else ImGui.TextUnformatted(text);
        if(TipHovered())TipText(localization.Format("FishingPanel.BiteTime",time.Minimum,time.Maximum,time.Usual)+"\n"+localization.Format("FishingPanel.BiteTimeHelp",time.Reports)
                +(chum ? "\n"+localization.Get("FishingPanel.BiteTimeChum"):"")+(inside && clock!.Value.Frozen ? "\n"+localization.Get("FishingPanel.BiteTimeHeld"):""));
    }
}
