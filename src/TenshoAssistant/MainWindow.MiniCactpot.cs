using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.MiniGames;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private readonly int[] cactpotBoard=new int[9];
    private readonly MiniCactpotSolveSession cactpotSolve=new();
    private int cactpotGeneration;
    private ulong cactpotObservedOwner;
    private void InvalidateCactpot()
    {cactpotGeneration++;cactpotSolve.Cancel();}
    private void SyncCactpot(MiniCactpotObservationState observation)
    {
        if(!observation.Enabled)return;
        if(!observation.CanSolve(DateTimeOffset.UtcNow))
        {
            if(cactpotObservedOwner!=0 || cactpotBoard.Any(value=>value!=0))
            {Array.Clear(cactpotBoard);cactpotObservedOwner=0;InvalidateCactpot();}
            return;
        }
        if(cactpotObservedOwner==observation.Owner && cactpotBoard.SequenceEqual(observation.Frame!.Board))return;
        InvalidateCactpot();cactpotObservedOwner=observation.Owner;
        for(var index=0;index<9;index++)cactpotBoard[index]=observation.Frame!.Board[index];
        _=cactpotSolve.StartAsync(cactpotBoard,cactpotObservedOwner,cactpotGeneration);
    }
    // Overlay on the game's Mini Cactpot window (0.158), like ezMiniCactpot: drawn every frame while the observation is on, even
    // with the Tensho window closed; the best cell(s) or line(s) get a green frame. Reading only: the player still clicks.
    internal void DrawCactpotOverlay()
    {
        if(windowDisposed)return;
        var observation=cactpotObservation.State;
        if(!observation.Enabled)return;
        SyncCactpot(observation);
        cactpotSolve.ValidateContext(cactpotObservedOwner,cactpotGeneration);
        if(!observation.CanSolve(DateTimeOffset.UtcNow) || observation.Frame is not { } frame)return;
        var state=cactpotSolve.State;
        if(state.Result is not { } result || state.Board is not { } solved || !solved.SequenceEqual(frame.Board))return;
        if((result.ChooseLine ? frame.Lanes:frame.Tiles) is not { } rects)return;
        var draw=ImGui.GetForegroundDrawList();
        var fill=ImGui.GetColorU32(Success with { W=0.25f });var border=ImGui.GetColorU32(Success);
        for(var index=0;index<result.Choices.Count && index<rects.Count;index++)
        {
            if(result.Choices[index] is not { } value || Math.Abs(value-result.ExpectedReward)>=0.000001)continue;
            var rect=rects[index];var min=new Vector2(rect.X,rect.Y);var max=min+new Vector2(rect.Width,rect.Height);
            draw.AddRectFilled(min,max,fill,6);draw.AddRect(min,max,border,6,ImDrawFlags.None,3);
        }
    }
    private void DrawMiniCactpot()
    {
        Heading(localization.Get("CactpotUi.Title"),localization.Get("CactpotUi.Description"));
        if(windowDisposed)return;
        var observation=cactpotObservation.State;var enabled=observation.Enabled;
        DrawToolSettingsLink("MiniCactpot");
        ImGui.TextDisabled(enabled ? localization.Get("CactpotUi.Observed"):localization.Get("CactpotUi.Manual"));
        SyncCactpot(observation);
        cactpotSolve.ValidateContext(enabled ? cactpotObservedOwner:0,cactpotGeneration);
        if(enabled)
        {
            ImGui.TextWrapped(observation.Status switch
            {
                MiniCactpotObservationStatus.Offline=>localization.Get("CactpotUi.Offline"),
                MiniCactpotObservationStatus.AddonAbsent=>localization.Get("CactpotUi.Absent"),
                MiniCactpotObservationStatus.NotReady=>localization.Get("CactpotUi.NotReady"),
                MiniCactpotObservationStatus.Error=>localization.Get("CactpotUi.Error"),
                MiniCactpotObservationStatus.Observed when observation.Frame is {Revealed:0}=>localization.Get("CactpotUi.First"),
                MiniCactpotObservationStatus.Observed when observation.Frame is {Revealed:>4}=>localization.Get("CactpotUi.Finished"),
                _=>observation.CanSolve(DateTimeOffset.UtcNow) ? localization.Get("CactpotUi.Advice"):localization.Get("CactpotUi.Waiting")
            });
            if(developerMode && observation.Error is { } error)ImGui.TextWrapped(error);
            if(observation.ObservedAt is { } observed)ImGui.TextDisabled(localization.Format("CactpotCounts.Observed",observed.ToLocalTime().ToString("HH:mm:ss")));
        }
        else ImGui.TextWrapped(localization.Get("CactpotUi.InputHelp"));
        if(ImGui.BeginTable("CactpotBoard",3,ImGuiTableFlags.SizingFixedFit))
        {
            try
            {
                for(var index=0;index<9;index++)
                {
                    ImGui.TableNextColumn();ImGui.SetNextItemWidth(55);
                    if(enabled)ImGui.Button($"{(cactpotBoard[index]==0 ? "?":cactpotBoard[index].ToString())}##Cactpot{index}",new Vector2(52));
                    else if(ImGui.InputInt($"##Cactpot{index}",ref cactpotBoard[index],0,0))InvalidateCactpot();
                }
            }
            finally{ImGui.EndTable();}
        }
        if(!enabled)
        {
            if(ImGui.Button(localization.Get("CactpotUi.Clear"))){Array.Clear(cactpotBoard);InvalidateCactpot();}
            ImGui.SameLine();ImGui.BeginDisabled(cactpotSolve.State.Loading);
            try{if(ImGui.Button(localization.Get("CactpotUi.Calculate")))_=cactpotSolve.StartAsync(cactpotBoard,0,cactpotGeneration);}
            finally{ImGui.EndDisabled();}
        }
        var state=cactpotSolve.State;
        ImGui.TextDisabled(localization.Format("CactpotCounts.Revealed",cactpotBoard.Count(value=>value!=0)));
        if(state.Loading)ImGui.TextDisabled(localization.Get("CactpotUi.Calculating"));
        if(state.Error is { } errorText){ImGui.TextWrapped(localization.Get("CactpotCounts.Error"));if(developerMode)ImGui.TextWrapped(errorText);}
        if(state.Result is not { } result)return;
        ImGui.TextColored(Accent,result.ChooseLine ? localization.Get("CactpotUi.ChooseLine"):localization.Get("CactpotUi.ChooseCell"));
        ImGui.TextUnformatted(localization.Format("CactpotCounts.Reward",result.ExpectedReward));
        var lines=new[]{localization.Get("CactpotUi.Top"),localization.Get("CactpotUi.Middle"),localization.Get("CactpotUi.Bottom"),localization.Get("CactpotUi.Left"),localization.Get("CactpotUi.Center"),localization.Get("CactpotUi.Right"),localization.Get("CactpotUi.DiagonalDown"),localization.Get("CactpotUi.DiagonalUp")};
        for(var index=0;index<result.Choices.Count;index++)
        {
            if(result.Choices[index] is not { } value)continue;
            var label=result.ChooseLine ? lines[index]:localization.Format("CactpotCounts.Cell",index/3+1,index%3+1);
            var best=Math.Abs(value-result.ExpectedReward)<0.000001;
            ImGui.TextColored(best ? Success:Muted,localization.Format("CactpotCounts.Choice",(best ? "★ ":"")+label,value));
        }
        ImGui.TextWrapped(localization.Get("CactpotUi.Help"));
    }
}
