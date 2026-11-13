namespace TenshoAssistant.MiniGames;

/// <summary>
/// X-223 (0.1233, user's request of 12/11, K-39): when the next GATE starts and when the Jumbo Cactpot is drawn (docs/evidence/0.1233-gold-saucer-times.md).
/// GATEs start every 20 minutes of real time, at :00, :20 and :40 of each hour. The draw is held every Saturday at a fixed universal time per
/// region of the data centre (WorldRegionGroup rows of the game: 1 Japan, 2 North America, 3 Europe, 4 Oceania); another region gives no time
/// (never a guessed one). No ticket count (D-32).
/// </summary>
public static class GoldSaucerTimes
{
    /// <summary>The draw's day and universal time per region (North America: Sunday 02:00 UTC, which is Saturday evening there).</summary>
    public static (DayOfWeek Day,int Hour)? DrawOf(uint region)=>region switch
    {
        1=>(DayOfWeek.Saturday,12),
        2=>(DayOfWeek.Sunday,2),
        3=>(DayOfWeek.Saturday,19),
        4=>(DayOfWeek.Saturday,9),
        _=>null,
    };

    /// <summary>The next draw strictly after <paramref name="now"/>, or null for a region without a known time.</summary>
    public static DateTimeOffset? NextDraw(uint region,DateTimeOffset now)
    {
        if(DrawOf(region) is not { } draw)return null;
        var utc=now.ToUniversalTime();
        var day=new DateTimeOffset(utc.Year,utc.Month,utc.Day,draw.Hour,0,0,TimeSpan.Zero);
        var ahead=((int)draw.Day-(int)utc.DayOfWeek+7)%7;
        var next=day.AddDays(ahead);
        return next>utc ? next:next.AddDays(7);
    }

    /// <summary>The next GATE start strictly after <paramref name="now"/>: the next :00, :20 or :40 of real time.</summary>
    public static DateTimeOffset NextGate(DateTimeOffset now)
    {
        var utc=now.ToUniversalTime();
        var hour=new DateTimeOffset(utc.Year,utc.Month,utc.Day,utc.Hour,0,0,TimeSpan.Zero);
        var slot=hour.AddMinutes((utc.Minute/20+1)*20);
        return slot;
    }
}
