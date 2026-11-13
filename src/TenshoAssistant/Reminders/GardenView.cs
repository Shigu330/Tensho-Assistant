namespace TenshoAssistant.Reminders;

/// <summary>X-178: the small round mark on a seed's icon: nothing (no tending interval), tending later, tending due, or ripe.</summary>
public enum GardenBadge { None, Later, Due, Ripe }

/// <summary>X-178: the mark on a possible crossbreed's icon. Unknown = the inventories were not read: never « not owned » without a reading (R06).</summary>
public enum GardenCrossMark { None, Wished, NotOwned, WishedNotOwned, Unknown }

/// <summary>
/// X-178 (0.1256): what the icon view of a garden patch shows, computed from the journal alone: how far a bed has grown, the time left
/// in short form, its mark, the mark of a crossbreed, the seed's item and whether a cell is too narrow for icon and buttons side by side.
/// Pure: the page draws.
/// </summary>
public static class GardenView
{
    /// <summary>How far the bed has grown, 0 to 1; 0 when planted in the future (clock change).</summary>
    public static float Growth(GardenBed bed,DateTimeOffset now)
    {
        var total=(GardenJournal.RipeAt(bed)-bed.PlantedAt).TotalSeconds;
        var done=(now-bed.PlantedAt).TotalSeconds;
        return total<=0 ? 1:(float)Math.Clamp(done/total,0,1);
    }

    /// <summary>The time left before the harvest, rounded down (never ripe too early); null once ripe.</summary>
    public static (int Days,int Hours,int Minutes)? Left(GardenBed bed,DateTimeOffset now)
    {
        var left=GardenJournal.RipeAt(bed)-now;
        if(left<=TimeSpan.Zero)return null;
        var minutes=(long)Math.Floor(left.TotalMinutes);
        return ((int)(minutes/1440),(int)(minutes/60%24),(int)(minutes%60));
    }

    /// <summary>The text of a time left: its key (Common.LeftDays « {0} d {1} h », LeftHours « {0} h {1:00} », LeftMinutes « {0} min ») and its two numbers.</summary>
    public static (string Key,int First,int Second) LeftText((int Days,int Hours,int Minutes) left)
        =>left.Days>0 ? ("Common.LeftDays",left.Days,left.Hours):left.Hours>0 ? ("Common.LeftHours",left.Hours,left.Minutes):("Common.LeftMinutes",left.Minutes,0);

    public static GardenBadge Badge(GardenBed bed,DateTimeOffset now)
    {
        if(now>=GardenJournal.RipeAt(bed))return GardenBadge.Ripe;
        if(GardenJournal.TendAt(bed) is not { } due)return GardenBadge.None;
        return now>=due ? GardenBadge.Due:GardenBadge.Later;
    }

    /// <param name="wished">The seed is on the wishlist.</param>
    /// <param name="observed">The bags and storages were read (at least in part).</param>
    /// <param name="total">How many the player holds in what was read.</param>
    public static GardenCrossMark Mark(bool wished,bool observed,long total)
    {
        var missing=observed && total<=0;
        if(wished)return missing ? GardenCrossMark.WishedNotOwned:GardenCrossMark.Wished;
        if(!observed)return GardenCrossMark.Unknown;
        return missing ? GardenCrossMark.NotOwned:GardenCrossMark.None;
    }

    /// <summary>The seed's item: the id noted at planting when there is one, else by the name noted (game language at that time), else null.</summary>
    public static uint? SeedIdOf(GardenBed bed,Func<string,uint?> byName)=>bed.SeedId>0 ? bed.SeedId:byName(bed.Seed) is { } id and >0 ? id:null;

    /// <summary>A cell too narrow for the icon (two frame heights) with the time and the two buttons beside it: everything stacks under the icon.</summary>
    public static bool Stacked(float columnWidth,float frameHeight,float spacing)=>columnWidth<frameHeight*4+spacing*3;
}
