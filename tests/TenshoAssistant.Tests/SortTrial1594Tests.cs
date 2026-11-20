using TenshoAssistant.Inventory;

// 0.1594 (X-649, review of 0.1593, R07): the sample inventory's free-slot search is bounded; more stacks than a bag holds fail clearly.
internal static class SortTrial1594Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=new(2026,11,20,10,0,0,TimeSpan.Zero);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Sort trial 1594: the sample with more stacks than bag 1 holds fails with a clear message, without looping",() =>
        {
            var task=Task.Run(()=>
            {
                try{InventorySortTrial.Snapshot(At,200);return "no error";}
                catch(InvalidOperationException error){return error.Message;}
            });
            Check(task.Wait(TimeSpan.FromSeconds(10)),"returns within ten seconds");
            Check(task.Result.Contains("at most 35 stacks per bag",StringComparison.Ordinal) && task.Result.Contains("bag 1",StringComparison.Ordinal),"clear message: "+task.Result);
        }),
        ("Sort trial 1594: the largest sample bag 1 can hold still builds, each bag's slots distinct",() =>
        {
            var largest=Enumerable.Range(0,200).TakeWhile(count=>{try{InventorySortTrial.Snapshot(At,count);return true;}catch(InvalidOperationException){return false;}}).Last();
            Check(largest>=InventorySortTrial.Stacks && largest<200,"the default sample fits: "+largest);
            var snapshot=InventorySortTrial.Snapshot(At,largest);
            Check(snapshot.Containers.Values.All(bag=>bag.Stacks.Select(stack=>stack.Slot).Distinct().Count()==bag.Stacks.Count() && bag.Stacks.All(stack=>stack.Slot is >=0 and <35)),"slots distinct, within the bag");
            Check(snapshot.Containers.Values.Sum(bag=>bag.Stacks.Count())==largest,"every stack placed");
        }),
        ("Sort trial 1594: a negative count is refused",() =>
        {
            try{InventorySortTrial.Snapshot(At,-1);Check(false,"refused");}catch(ArgumentOutOfRangeException){}
        }),
    ];
}
