using System.Net;
using System.Text.RegularExpressions;
using TenshoAssistant.Community;

// 0.326 (review of queue step d, versions 0.321–0.325): a switch turned off also stops the requests already queued and forgets
// them, so they are asked again once the switch is back on; the release resume and its probes; the page fixes found by the review.
internal static class ReviewFixes326Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static void Until(Func<bool> done,string what) { var deadline = DateTime.UtcNow.AddSeconds(10); while (!done()) { if (DateTime.UtcNow > deadline) throw new Exception("timed out: " + what); Thread.Sleep(10); } }
    private static readonly string Source = Path.GetFullPath("src/TenshoAssistant");
    private static string Read(string file) => File.ReadAllText(Path.Combine(Source,file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Teamcraft turned off while spots wait: they are not posted, and are asked again once it is back on",() =>
        {
            var calls = 0; var on = true;
            var first = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            HttpResponseMessage Empty() => new(HttpStatusCode.OK) { Content = new StringContent("{\"data\":{\"biteTimes\":[]}}") };
            using var service = new FishBiteTimeService(() => TimeSpan.FromHours(1),null,new HttpClient(new MarketTestHandler((_,_) =>
                Interlocked.Increment(ref calls) == 1 ? first.Task : Task.FromResult(Empty())))) { Enabled = () => Volatile.Read(ref on) };
            Check(service.Get(1).Loading && service.Get(2).Loading,"two spots queued");
            Until(() => Volatile.Read(ref calls) == 1,"first spot posted");
            Volatile.Write(ref on,false);
            first.SetResult(Empty());
            Thread.Sleep(300);
            Check(Volatile.Read(ref calls) == 1,"the waiting spot is not posted while off: " + calls);
            Volatile.Write(ref on,true);
            Check(service.Get(2).Loading,"forgotten while off, so queued again");
            Until(() => !service.Get(2).Loading,"second spot answered");
            Check(Volatile.Read(ref calls) == 2 && service.Get(2).Error is null,"posted once the switch is back on");
        }),
        ("Garland and wiki queues: a refusal of the switch is neither retried as the 3 s pause nor kept as an error",() =>
        {
            var garland = Read("Community/GarlandItemSources.cs");
            Check(garland.Contains("catch(SourceDisabledException){lock(gate){if(!disposed)states.Remove(id);}continue;}",StringComparison.Ordinal),"Garland item: forgotten");
            Check(garland.Contains("error is not SourceDisabledException && attempt<5",StringComparison.Ordinal),"Garland item: not retried");
            var wiki = Read("Community/WikiItemAcquisition.cs");
            Check(wiki.Contains("catch(SourceDisabledException){lock(gate){if(!disposed)states.Remove(id);}continue;}",StringComparison.Ordinal),"wiki item: forgotten");
            var check = wiki.IndexOf("if(Enabled?.Invoke()==false)throw new SourceDisabledException",StringComparison.Ordinal);
            Check(check > 0 && check < wiki.IndexOf("BoundedHttp.ReadAsync",StringComparison.Ordinal),"wiki item: switch checked before the download");
        }),
        ("Pages: Duty Finder follows the collections and Garland coming back; « Delete all » is armed per section; texts fixed",() =>
        {
            var finder = Read("MainWindow.DutyFinder.cs");
            Check(finder.Contains("ReferenceEquals(cached.Collections,collections.State)",StringComparison.Ordinal),"view keyed on the collections");
            Check(finder.Contains("SourceOff(loot.State.Error))dutyFinderRequested=0;",StringComparison.Ordinal),"asked again after the switch (0.331: the panel has its own loot service)");
            var alarms = Read("MainWindow.Alarms.cs");
            Check(alarms.Contains("var armKey=$\"{section.Key}/{group.Key}\";",StringComparison.Ordinal) && alarms.Contains("alarmGroupDeleteSeenFrame+1",StringComparison.Ordinal),"armed per section, dropped when not drawn");
            Check(alarms.Contains("AlarmSchedule.ClampLead(AlarmRepeat.EorzeaDaily,DefaultAlarmLead)",StringComparison.Ordinal),"daily lead shown as created");
            Check(Read("MainWindow.Bank.cs").Contains("localization.Format(\"BankUi.Frame\",BankEntryName(entry),",StringComparison.Ordinal),"bank frame names translated");
            Check(Read("MainWindow.Home.cs").Contains("!SourceOff(marketError)",StringComparison.Ordinal),"a turned-off source is not a failed service");
            var french = File.ReadAllText(Path.Combine(Source,"Localization","Languages","fr.json"));
            Check(!french.Contains("Réglages ›",StringComparison.Ordinal),"the French menu is « Paramètres »");
        }),
    ];
}
