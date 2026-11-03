using TenshoAssistant.Alarms;
using TenshoAssistant.Inventory;

// 0.344 (g, I-02): currency balances and threshold alerts — one alert per upward crossing, re-armed 2 points under, silent first read,
// reset on character or threshold change, unknown values change nothing; Discord category; guards of the native reads.
internal static class Currencies344Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
    private static readonly CurrencyDefinition Centurio = new(10307,"Centurio",0,CurrencyKind.Other,4000);
    private static CurrencyReading Reading(ulong character,long? amount,long? cap = 4000) => new(character,DateTimeOffset.UnixEpoch,[new(Centurio,amount,cap)],null,null);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Currencies: one alert per upward crossing, re-armed 2 points under the threshold, silent when already over at first read",() =>
        {
            var tracker = new CurrencyAlertTracker();
            Check(tracker.Evaluate(Reading(1,1000),null).Crossed.Count == 0,"first read under: armed, nothing");
            var up = tracker.Evaluate(Reading(1,3200),null);
            Check(up.Crossed.Count == 1 && up.Over.Count == 1,"80 % of 4 000 reached: one alert");
            Check(tracker.Evaluate(Reading(1,3500),null).Crossed.Count == 0,"still over: no second alert");
            Check(tracker.Evaluate(Reading(1,3150),null).Crossed.Count == 0 && tracker.Evaluate(Reading(1,3200),null).Crossed.Count == 0,"79 %: not re-armed (2-point margin)");
            tracker.Evaluate(Reading(1,3000),null);
            Check(tracker.Evaluate(Reading(1,3300),null).Crossed.Count == 1,"under 78 %, then over again: a new alert");
            var fresh = new CurrencyAlertTracker();
            var first = fresh.Evaluate(Reading(1,3900),null);
            Check(first.Crossed.Count == 0 && first.Over.Count == 1,"already over at the first read: listed, not announced");
        }),
        ("Currencies: character or threshold change re-arms silently; unknown values, no cap, gil-like caps and 0 % never alert",() =>
        {
            var tracker = new CurrencyAlertTracker();
            tracker.Evaluate(Reading(1,1000),null);
            Check(tracker.Evaluate(Reading(2,3900),null).Crossed.Count == 0,"another character: no false gain");
            var lowered = new Dictionary<uint,int> { [10307] = 50 };
            tracker.Evaluate(Reading(2,1000),null);
            Check(tracker.Evaluate(Reading(2,2500),lowered).Crossed.Count == 0,"threshold changed: re-armed silently");
            tracker.Evaluate(Reading(2,1000),lowered);
            Check(tracker.Evaluate(Reading(2,null),lowered).Crossed.Count == 0 && tracker.Evaluate(Reading(2,2500),lowered).Crossed.Count == 1,"an unknown read changes nothing");
            Check(tracker.Evaluate(Reading(3,999_999_999,999_999_999),null).Over.Count == 0 && tracker.Evaluate(Reading(3,1,1),null).Over.Count == 0,"gil-like and 1 caps are not percent caps");
            Check(tracker.Evaluate(Reading(3,3900,null),null).Over.Count == 0,"no cap read: nothing");
            var off = new Dictionary<uint,int> { [10307] = 0 };
            tracker.Evaluate(Reading(4,1000),off);
            Check(tracker.Evaluate(Reading(4,4000),off).Over.Count == 0,"0 % = off");
            Check(CurrencyAlertTracker.PercentFor(10307,new Dictionary<uint,int> { [10307] = 150 }) == 100 && CurrencyAlertTracker.PercentFor(1,null) == 80,"bounded, 80 % by default");
            Check(CurrencyDefaults.SealItem(1) == 20 && CurrencyDefaults.SealItem(3) == 22 && CurrencyDefaults.SealItem(0) == 0,"seal of the current company only");
        }),
        ("Currencies (0.344 review): a 0 read at login does not arm; low thresholds re-arm; one threshold change re-arms only that currency",() =>
        {
            var tracker = new CurrencyAlertTracker();
            Check(tracker.Evaluate(Reading(1,0),null).Crossed.Count == 0 && tracker.Evaluate(Reading(1,3800),null).Crossed.Count == 0,"0 then the real balance: silent, no false crossing");
            var real = new CurrencyAlertTracker();
            var five = new Dictionary<uint,int> { [10307] = 5 };
            real.Evaluate(Reading(1,0),five);real.Evaluate(Reading(1,0),five);
            Check(real.Evaluate(Reading(1,250),five).Crossed.Count == 1,"a real 0 (two reads) arms: the jump over the threshold is announced");
            var low = new Dictionary<uint,int> { [10307] = 1 };
            var small = new CurrencyAlertTracker();
            small.Evaluate(Reading(1,10),low);
            Check(small.Evaluate(Reading(1,40),low).Crossed.Count == 1,"1 %: alerts at 40 of 4 000");
            small.Evaluate(Reading(1,20),low);
            Check(small.Evaluate(Reading(1,45),low).Crossed.Count == 1,"1 %: re-armed under the threshold itself");
            var two = new CurrencyDefinition(26807,"Bicolor",0,CurrencyKind.Other,1500);
            var both = new CurrencyAlertTracker();
            CurrencyReading Pair(long centurio,long bicolor) => new(1,DateTimeOffset.UnixEpoch,[new(Centurio,centurio,4000),new(two,bicolor,1500)],null,null);
            both.Evaluate(Pair(1000,100),null);
            var changed = new Dictionary<uint,int> { [10307] = 50 };
            var after = both.Evaluate(Pair(1000,1300),changed);
            Check(after.Crossed.Count == 1 && after.Crossed[0].Currency.ItemId == 26807,"the other currency still alerts after one threshold changed");
            var watcher = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Inventory/CurrencyWatcher.Dalamud.cs"));
            Check(watcher.Contains("container->IsLoaded",StringComparison.Ordinal) && watcher.Contains("if(!loaded){balances.Add(new(definition,null,cap));continue;}",StringComparison.Ordinal),"unknown until the Currency container is loaded");
            var tab = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Currencies.cs"));
            Check(tab.Contains("ImGui.IsItemDeactivatedAfterEdit() && currencyEdits.Remove(id,out var done)",StringComparison.Ordinal) && tab.Contains("else if(!ImGui.IsItemActive())currencyEdits.Remove(id);",StringComparison.Ordinal),"threshold saved once the field is left; a stale value is dropped");
        }),
        ("Currencies: Discord category of its own; native reads pointer-checked and guarded; thresholds copied on write",() =>
        {
            var settings = new DiscordSettings { Enabled = true,WebhookUrl = "https://discord.com/api/webhooks/123456789012345678/abcdefghijklmnopqrstuvwxyz0123456789" };
            Check(DiscordWebhook.ShouldSend(settings,AlarmSource.Currency),"sent when Discord is on");
            settings.Currencies = false;
            Check(!DiscordWebhook.ShouldSend(settings,AlarmSource.Currency),"its category off: not sent");
            Check((int)AlarmSource.Currency == 5,"added last: saved categories keep their number");
            var watcher = Read("Inventory/CurrencyWatcher.Dalamud.cs");
            Check(watcher.Contains("Resolved((nint)CurrencyManager.MemberFunctionPointers.GetItemCount)",StringComparison.Ordinal)
                && watcher.Contains("Resolved((nint)InventoryManager.MemberFunctionPointers.GetInventoryItemCount)",StringComparison.Ordinal),"pointers checked before calls");
            Check(watcher.Contains("framework.Update+=OnUpdate;",StringComparison.Ordinal) && watcher.Contains("framework.Update-=OnUpdate;",StringComparison.Ordinal)
                && watcher.Contains("player.ContentId==0",StringComparison.Ordinal),"framework thread, logged-in character only, removed on dispose");
            Check(Read("MainWindow.Currencies.cs").Contains("new Dictionary<uint,int>(known)",StringComparison.Ordinal),"copy on write");
            Check(File.ReadAllText(Path.GetFullPath("docs/evidence/0.344-currencies.md")).Contains("GetMaxCompanySeals",StringComparison.Ordinal),"evidence of the probe");
        }),
    ];
}
