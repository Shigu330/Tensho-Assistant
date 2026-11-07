namespace TenshoAssistant.Craft;

public sealed record CraftThresholds(int Craftsmanship,int Control,int Cp,int CraftsmanshipMargin,int ControlMargin,int CpMargin);

/// <summary>
/// 0.553 (C4): the lowest attributes with which a finished sequence still finishes at the same quality. Each search keeps the other attributes and replays the same simulation:
/// progress depends only on Craftsmanship and quality only on Control (CraftSimulation), and the recipe's minimums are the floor. Off the frame, on a cancellable task.
/// </summary>
public static class CraftStatThresholds
{
    public static CraftThresholds? Find(RecipeDefinition recipe,CraftStats stats,int startingQuality,IReadOnlyList<CraftActionKind> actions,CancellationToken token=default)
    {
        var p=recipe.Simulation;
        if(p is null)return null;
        var done=CraftSimulation.Run(recipe,stats,startingQuality,actions,token);
        if(done.Final.Finish!=CraftFinish.Completed || done.Error is not null)return null;
        var quality=done.Final.Quality;
        bool Holds(CraftStats tried)
        {
            var run=CraftSimulation.Run(recipe,tried,startingQuality,actions,token);
            return run.Error is null && run.Final.Finish==CraftFinish.Completed && run.Final.Quality>=quality;
        }
        var craftsmanship=Lowest(p.RequiredCraftsmanship,stats.Craftsmanship,value=>Holds(stats with {Craftsmanship=value}));
        var control=Lowest(p.RequiredControl,stats.Control,value=>Holds(stats with {Control=value}));
        var cp=Math.Max(0,stats.Cp-done.Final.Cp);
        return new(craftsmanship,control,cp,stats.Craftsmanship-craftsmanship,stats.Control-control,stats.Cp-cp);
    }

    private static int Lowest(int floor,int known,Func<int,bool> holds)
    {
        var low=Math.Min(floor,known);var high=known;
        if(holds(low))return low;
        while(high-low>1)
        {
            var middle=low+(high-low)/2;
            if(holds(middle))high=middle;else low=middle;
        }
        return high;
    }
}
