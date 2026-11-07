using TenshoAssistant.Diagnostics;

// 0.740 (user report): a list panel line goes back and forth while gathering.
internal static class ListBlink740Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=new(2026,11,7,11,0,0,TimeSpan.Zero);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("List blink: a value that comes back within three seconds is said, once in thirty seconds; a steady rise is not",() =>
        {
            var watch=new BlinkWatch();
            Check(watch.Observe("CraftList",5,10,30,At) is null && watch.Observe("CraftList",5,10,30,At.AddSeconds(1)) is null,"first and same");
            Check(watch.Observe("CraftList",5,11,30,At.AddSeconds(2)) is null,"one more gathered");
            var back=watch.Observe("CraftList",5,10,30,At.AddSeconds(3));
            Check(back is not null && back.Contains("held 10->11->10") && back.Contains("needed 30->30->30"),back ?? "none");
            Check(watch.Observe("CraftList",5,11,30,At.AddSeconds(4)) is null,"quiet for thirty seconds");
            var rise=new BlinkWatch();
            foreach(var held in new[]{1,2,3,4})Check(rise.Observe("Workshop",7,held,9,At.AddSeconds(held)) is null,"rise");
            var slow=new BlinkWatch();
            slow.Observe("Workshop",8,1,9,At);slow.Observe("Workshop",8,2,9,At.AddSeconds(1));
            Check(slow.Observe("Workshop",8,1,9,At.AddSeconds(10)) is null,"too slow to be a blink");
            var lists=new BlinkWatch();
            lists.Observe("A",9,1,5,At);lists.Observe("B",9,2,5,At);
            Check(lists.Observe("A",9,1,5,At) is null,"each list apart");
            var zero=new BlinkWatch();
            Check(!zero.FellToZero("W",1,0,At) && !zero.FellToZero("W",1,5,At),"from nothing, or a rise");
            Check(zero.FellToZero("W",1,0,At.AddSeconds(1)),"5 to 0");
            zero.FellToZero("W",2,4,At);
            Check(!zero.FellToZero("W",2,0,At.AddSeconds(2)),"once in thirty seconds per list");
        }),
    ];
}
