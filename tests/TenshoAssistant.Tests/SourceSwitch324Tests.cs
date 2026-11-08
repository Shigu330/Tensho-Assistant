using System.Net;
using System.Text.RegularExpressions;
using TenshoAssistant;
using TenshoAssistant.Community;
using TenshoAssistant.Credits;
using TenshoAssistant.Market;
using TenshoAssistant.Monsters;

// 0.324 (queue step d, review 0.316 points 3.0–3.2): each community source switch stops every request of its source, the pages say
// « source turned off » rather than « error », and the credits no longer describe any switch as partial.
internal static class SourceSwitch324Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string Source = Path.GetFullPath("src/TenshoAssistant");
    private static HttpClient Counting(Action count) => new(new MarketTestHandler((_,_) =>
    {
        count();
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
    }));
    private static void Wait(Func<bool> done) { var until = DateTime.UtcNow.AddSeconds(5); while (!done() && DateTime.UtcNow < until) Thread.Sleep(10); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Garland switch off: no request is sent and the refusal says which source is off",() =>
        {
            var calls = 0; using var http = Counting(() => calls++); using var garland = new GarlandClient(http) { Enabled = () => false };
            var refused = false;
            try { garland.ReadAsync(GarlandResource.Item,4,CancellationToken.None).GetAwaiter().GetResult(); }
            catch (SourceDisabledException error) { refused = error.SourceName == "Garland Tools" && error.Message.EndsWith(SourceDisabledException.Suffix,StringComparison.Ordinal); }
            Check(refused && calls == 0,"Garland refused without a request");
        }),
        ("Universalis switch off: prices and listings ask nothing",() =>
        {
            var calls = 0; using var http = Counting(() => calls++);
            using var market = new MarketService(http) { Enabled = () => false }; market.SetWorld(1,71,"World");
            market.RequestAsync(42).GetAwaiter().GetResult();
            Check(calls == 0 && market.State.Error == MarketService.SourceOff && market.State.Quote is null,"manual price: " + market.State.Error);
            var hover = market.RequestHoverAsync(42,1,71).GetAwaiter().GetResult();
            Check(calls == 0 && hover.Status == MarketFetchStatus.Failed,"hover price");
            using var board = new MarketBoardService(http) { Enabled = () => false };
            board.Request("World",42); Wait(() => !board.State.Loading);
            Check(calls == 0 && board.State.Error == MarketBoardService.SourceOff && !board.State.Loading,"listings: " + board.State.Error);
        }),
        ("Wiki switch off: monster sources ask nothing",() =>
        {
            var calls = 0; using var http = Counting(() => calls++);
            using var wiki = new WikiMonsterSources(_ => "Rroneek Chuck",() => TimeSpan.FromDays(1),http: http) { Enabled = () => false };
            wiki.Select(44106); wiki.RequestAsync().GetAwaiter().GetResult();
            Check(calls == 0 && wiki.State.Error is { } error && error.EndsWith(SourceDisabledException.Suffix,StringComparison.Ordinal),"wiki monsters: " + wiki.State.Error);
        }),
        ("Credits: every switch covers its whole source; Universalis has its own, on by default",() =>
        {
            Check(CreditCatalog.All.All(credit => !credit.SettingPartial),"no partial switch left");
            Check(CreditCatalog.All.Single(credit => credit.Id == "Universalis").Setting == "CommunityUniversalis","Universalis switch in the credits");
            Check(new Configuration().CommunityUniversalis && ConfigurationReset.Reset.Contains("CommunityUniversalis"),"default on, reset with the other settings");
        }),
        ("Plugin and pages: every service gets its switch, hover reads follow it",() =>
        {
            var plugin = File.ReadAllText(Path.Combine(Source,"Plugin.cs"));
            foreach (var wire in new[] { "garland.Enabled=()=>configuration.CommunityGarland","market.Enabled=()=>configuration.CommunityUniversalis","marketBoard.Enabled=()=>configuration.CommunityUniversalis" })
                Check(plugin.Contains(wire,StringComparison.Ordinal),wire);
            Check(Regex.Matches(plugin,@"\{Enabled=\(\)=>configuration\.CommunityWiki\}").Count == 5,"wiki monsters, item acquisition, vistas, blue magic spells (0.367) and island animals (0.685) get the wiki switch");
            var hover = File.ReadAllText(Path.Combine(Source,"Items","ItemHoverReader.Dalamud.cs")) + File.ReadAllText(Path.Combine(Source,"MainWindow.MarketHover.cs"));
            Check(Regex.Matches(hover,@"configuration\.CommunityUniversalis && configuration\.MarketPricesOnHover").Count == 2,"hover prices follow Universalis");
            // 0.447 (decided 2026-11-03): the dead « monsters on hover » setting and its wiki lookup are gone.
            Check(!hover.Contains("MonsterSourcesOnHover",StringComparison.Ordinal),"no wiki lookup on hover any more");
            Check(MarketService.SourceOff.EndsWith(SourceDisabledException.Suffix,StringComparison.Ordinal) && MarketBoardService.SourceOff == MarketService.SourceOff,"one recognisable message");
        }),
        ("Settings: the older options now have their « ↺ » (review 0.316 point 3.3)",() =>
        {
            string Read(string file) => File.ReadAllText(Path.Combine(Source,file));
            var expected = new (string File,string[] Ids)[]
            {
                ("MainWindow.Settings.cs",["\"MacroStart\"","\"MacroEnd\"","\"MacroPrefix\""]),
                ("MainWindow.Caching.cs",["\"Cache\"+area"]),
                ("MainWindow.Alarms.cs",["\"AlarmLead\"","\"AlarmPlaySound\"","\"AlarmSound\"","\"AlarmChat\"","\"AlarmMute\"","\"AlarmUseGame\"","\"AlarmGameWeather\""]),
                ("MainWindow.MapView.cs",["\"Area\"+key"]),
            };
            foreach (var (file,ids) in expected)
            {
                var text = Read(file);
                foreach (var id in ids) Check(text.Contains("DefaultButton(" + id,StringComparison.Ordinal),file + ": " + id);
            }
            // X-76 H5 (0.915): the macro book and switches and the Fashion Report options are shared rows, whose « ↺ » comes with the row.
            foreach (var (file,row) in new[]
            {
                ("MainWindow.Settings.cs","ComboSetting(\"ModuleSettings.MacroBook\","),
                ("MainWindow.Settings.cs","BoolSetting(\"ModuleSettings.MacroOverwrite\","),("MainWindow.Settings.cs","BoolSetting(\"ModuleSettings.MacroChain\","),
                ("MainWindow.FashionWeek.cs","BoolSetting(\"FashionWeek.OwnedFirst\","),("MainWindow.FashionWeek.cs","BoolSetting(\"FashionWeek.Dim\","),
                ("MainWindow.FashionWeek.cs","BoolSetting(\"FashionWeek.Popular\","),("MainWindow.FashionWeek.cs","BoolSetting(\"FashionWeek.Panel\","),
            })
                Check(Read(file).Contains(row,StringComparison.Ordinal),file + ": " + row);
            var settings = Read("MainWindow.Settings.cs");
            foreach (var key in new[] { "Settings.OpenOnLoad","Settings.OpenOnLogin","Settings.UsageHover","Settings.ContextMenu","DutyFinder.Setting","CharacterStats.Setting","BankUi.AutoSetting" })
            {
                Check(settings.Contains("BoolSetting(\"" + key + "\"",StringComparison.Ordinal),"shared row: " + key);
                Check(!settings.Contains("ImGui.Checkbox(localization.Get(\"" + key + "\")",StringComparison.Ordinal),"no bare checkbox left: " + key);
            }
        }),
    ];
}
