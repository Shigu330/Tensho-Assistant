using System.Text.Json;
using TenshoAssistant;
using TenshoAssistant.Craft;
using TenshoAssistant.Items;
using TenshoAssistant.Inventory;
using TenshoAssistant.Gathering;
using TenshoAssistant.Collections;
using TenshoAssistant.Market;
using System.Net.Http;
using TenshoAssistant.Quests;
using TenshoAssistant.Character;
using TenshoAssistant.MiniGames;

if(args.Length==1 && args[0]=="--probe-wiki-rroneek")
{
    using var http=new HttpClient{Timeout=TimeSpan.FromSeconds(15)};
    var json=await TenshoAssistant.Community.BoundedHttp.ReadAsync(http,new Uri("https://ffxiv.consolegameswiki.com/mediawiki/api.php?action=parse&page=Rroneek_Chuck&format=json"),1024*1024,CancellationToken.None);
    var parsed=TenshoAssistant.Monsters.WikiMonsterParser.Parse(json,44106,"Rroneek Chuck",DateTimeOffset.UtcNow);
    foreach(var row in parsed.Monsters)Console.WriteLine($"{row.Name} | {row.Level} | {row.Location}");
    return parsed.Monsters.Count>0 ? 0:1;
}

if(args.Length>0 && args[0]=="--validate-localization")
{
    var directory=args.Length>1 ? Path.GetFullPath(args[1]):Path.GetFullPath("src/TenshoAssistant/Localization/Languages");
    try
    {
        var report=TenshoAssistant.Localization.LocalizationValidator.Validate(directory);
        foreach(var language in report.Languages)
        {
            Console.WriteLine($"{language.Code}: {language.TranslatedKeys}/{language.TotalKeys} ({language.Completion:P1})");
            foreach(var key in language.MissingKeys)Console.WriteLine($"  MISSING {key}");
            foreach(var key in language.UnknownKeys)Console.WriteLine($"  UNKNOWN {key}");
        }
        foreach(var error in report.Errors)Console.Error.WriteLine(error);
        Environment.ExitCode=report.Errors.Count>0 || report.Languages.Any(language=>language.UnknownKeys.Count>0) ? 1:0;
    }
    catch(Exception error)
    {
        Console.Error.WriteLine(error.Message);
        Environment.ExitCode=1;
    }
    return Environment.ExitCode;
}

var tests = new (string Name, Action Run)[]
{
    ("An added language is discovered without window changes and safely falls back after its file is removed", () =>
    {
        var directory=Path.Combine(Path.GetTempPath(),"TenshoAddedLanguage-"+Guid.NewGuid().ToString("N"));
        var languages=Path.Combine(directory,"Localization","Languages");
        Directory.CreateDirectory(languages);
        var path=Path.Combine(languages,"es.json");
        try
        {
            File.WriteAllText(path,"""{"_meta":{"code":"ES","name":"Spanish","nativeName":"Español","author":"Test","version":1},"Settings":{"Language":"Idioma"}}""");
            var loc=new TenshoAssistant.Localization.LocalizationManager((error,message)=>throw new InvalidOperationException(message,error),directory);
            Check(loc.AvailableLanguages.Single(language=>language.NativeName=="Español").Code=="es","discovered metadata uses the same normalized code as selection");
            loc.SetLanguage(" ES ");
            Check(loc.CurrentCode=="es" && loc.Get("Settings.Language")=="Idioma","new language can be selected from metadata alone");
            Check(loc.Get("CraftMacro.Copy")=="Copy this block","partial added language uses bundled English");
            File.Delete(path);loc.Reload();
            Check(loc.CurrentCode=="en" && loc.Get("Settings.Language")=="Language","removed language falls back without a missing-key UI");
        }
        finally{if(File.Exists(path))File.Delete(path);Directory.Delete(languages);Directory.Delete(Path.GetDirectoryName(languages)!);Directory.Delete(directory);}
    }),
    ("Invalid external translation formats retain bundled text and corrected files reload without losing the language", () =>
    {
        var directory=Path.Combine(Path.GetTempPath(),"TenshoFormat-"+Guid.NewGuid().ToString("N"));
        var languages=Path.Combine(directory,"Localization","Languages");
        Directory.CreateDirectory(languages);
        var path=Path.Combine(languages,"fr.json");
        try
        {
            File.WriteAllText(path,"""{"_meta":{"code":"fr","name":"French","nativeName":"Français","author":"Test","version":1},"Home":{"Catalogues":"cassé {99}"},"LocalClock":{"Header":"cassé {"}}""");
            var warnings=new List<string>();
            var loc=new TenshoAssistant.Localization.LocalizationManager((error,message)=>warnings.Add(message),directory);
            loc.SetLanguage("fr");
            Check(!loc.Format("Home.Catalogues",12,34).Contains("cassé") && !loc.Format("LocalClock.Header","12:34:56").Contains("cassé"),"bad indices and unmatched braces do not reach rendering");
            Check(warnings.Count==2 && loc.Validate().Errors.Count>0,"invalid editable text remains diagnosed");
            File.WriteAllText(path,"""{"_meta":{"code":"fr","name":"French","nativeName":"Français","author":"Test","version":1},"Home":{"Catalogues":"Recettes {1}, objets {0}"}}""");
            loc.Reload();
            Check(loc.CurrentCode=="fr" && loc.Format("Home.Catalogues",12,34)=="Recettes 34, objets 12","corrected translation and reordered placeholders load with the selection preserved");
        }
        finally{File.Delete(path);Directory.Delete(languages);Directory.Delete(Path.GetDirectoryName(languages)!);Directory.Delete(directory);}
    }),
    ("Wiki monster parser preserves explicit rows and rejects foreign pages", WikiMonsterTests.Parse),
    ("Wiki monster requests are explicit cached bounded and inert after disposal", WikiMonsterTests.Cache),
    ("Wiki redirects missing pages and drop candidates are explicit", WikiMonsterTests.Redirects),
    ("Localization validator accepts partial languages and rejects malformed metadata duplicates and missing arguments", () =>
    {
        var directory=Path.Combine(Path.GetTempPath(),"TenshoLocalization-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory,"en.json"),"""{"_meta":{"code":"en","name":"English","nativeName":"English","author":"Test","version":1},"Message":"{0} and {1}","Fallback":"English"}""");
            File.WriteAllText(Path.Combine(directory,"fr.json"),"""{"_meta":{"code":"fr","name":"French","nativeName":"Français","author":"Test","version":1},"Message":"{1} puis {0}"}""");
            var valid=TenshoAssistant.Localization.LocalizationValidator.Validate(directory);
            Check(valid.Errors.Count==0 && valid.Languages.Single(language=>language.Code=="fr").MissingKeys.SequenceEqual(new[]{"Fallback"}),"partial translation and reordered placeholders accepted");
            File.WriteAllText(Path.Combine(directory,"fr.json"),"""{"_meta":{"code":"wrong","version":"bad"},"Message":"{1}","Repeated":"a","Repeated":"b"}""");
            var invalid=TenshoAssistant.Localization.LocalizationValidator.Validate(directory);
            Check(invalid.Errors.Any(error=>error.Contains("duplicate key")) && invalid.Errors.Any(error=>error.Contains("incompatible placeholders")) && invalid.Errors.Any(error=>error.Contains("_meta.code")) && invalid.Errors.Any(error=>error.Contains("_meta.version")),"invalid metadata duplicates and removed argument detected");
            File.WriteAllText(Path.Combine(directory,"fr.json"),"[]");
            Check(TenshoAssistant.Localization.LocalizationValidator.Validate(directory).Errors.Any(error=>error.Contains("root must be an object")),"invalid root reported without crashing");
            File.WriteAllText(Path.Combine(directory,"fr.json"),"{");
            Check(TenshoAssistant.Localization.LocalizationValidator.Validate(directory).Errors.Any(error=>error.Contains("invalid JSON")),"invalid JSON reported");
        }
        finally
        {
            foreach(var name in new[]{"en.json","fr.json"})File.Delete(Path.Combine(directory,name));
            Directory.Delete(directory);
        }
    }),
    ("Observation preferences persist independently and old configurations remain opt in", () =>
    {
        var old=JsonSerializer.Deserialize<Configuration>("{\"Version\":1}")!;
        Check(old.Observations.Count==0,"legacy config does not enable observations");
        var config=new Configuration();
        foreach(var id in new[]{"Craft","Gathering","Cactpot","Triad","Macros"})config.Observations[id]=true;
        config.Observations["Triad"]=false;
        var copy=JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(config))!;
        Check(copy.Observations.Count==5 && copy.Observations["Craft"] && copy.Observations["Gathering"] && copy.Observations["Cactpot"] && copy.Observations["Macros"] && !copy.Observations["Triad"],"all five settings retain independent choices");
    }),
    ("Embedded languages work without external files and preserve fallback across reload", () =>
    {
        var failures = new List<string>();
        var loc = new TenshoAssistant.Localization.LocalizationManager((error, message) => failures.Add(message), Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        Check(loc.AvailableLanguages.Count >= 4, "bundled languages discovered without external files");
        Check(loc.Get("Settings.Language") == "Language", "embedded English source loaded");
        loc.SelectInitialLanguage(null,"French");
        Check(loc.CurrentCode=="fr","first launch uses the client language metadata");
        loc.SelectInitialLanguage("en","French");
        Check(loc.CurrentCode=="en","manual saved choice overrides the client language");
        loc.SelectInitialLanguage(null,"UnsupportedLanguage");
        Check(loc.CurrentCode=="en","unsupported client language falls back to English");
        loc.SetLanguage("fr");
        Check(loc.Get("Settings.Language") == "Langue", "embedded French selected");
        var previousRevision=loc.Revision;
        loc.Reload();
        Check(loc.CurrentCode == "fr" && loc.Get("Settings.Language") == "Langue", "reload retains selected language");
        Check(loc.Revision!=previousRevision,"reload invalidates cached localized captions without changing the language");
        loc.SetLanguage("ja");
        Check(loc.Get("CraftMacro.Copy") == "Copy this block", "partial language falls back to English");
        Check(loc.Get("Missing.Example") == "[Missing.Example]", "unknown key safe");
        previousRevision=loc.Revision;
        loc.ShowKeys = true;
        Check(loc.Get("Settings.Language") == "Settings.Language", "developer key mode");
        Check(loc.Revision!=previousRevision,"key display invalidates cached captions");
        Check(failures.Count == 0, "bundled files parse successfully");
    }),
    ("Mini Cactpot exact opening agrees with independently published expected value", () =>
    {
        var board = new[] { 1, 0, 0, 0, 0, 0, 0, 0, 0 };
        var result = MiniCactpotSolver.Solve(board);
        Check(!result.ChooseLine && result.Choices.Count == 9 && result.Choices[0] is null, "only hidden cells considered");
        Check(Math.Abs(result.ExpectedReward - 1677.7854166666664) < 0.000001, "exact one-corner opening expectation");
        Check(result.Choices[2] is { } corner && Math.Abs(corner - result.ExpectedReward) < 0.000001, "opposite side corner is optimal");
        Check(board[0] == 1 && board.Skip(1).All(value => value == 0), "input board never modified");
    }),
    ("Mini Cactpot line averages match independent full-board enumeration", () =>
    {
        var payouts = new Dictionary<int, int> { [6]=10000, [7]=36, [8]=720, [9]=360, [10]=80,
            [11]=252, [12]=108, [13]=72, [14]=54, [15]=180, [16]=72, [17]=180,
            [18]=119, [19]=36, [20]=306, [21]=1080, [22]=144, [23]=1800, [24]=3600 };
        var lines = new[] { new[] {0,1,2}, new[] {3,4,5}, new[] {6,7,8}, new[] {0,3,6},
            new[] {1,4,7}, new[] {2,5,8}, new[] {0,4,8}, new[] {2,4,6} };
        var random = new Random(20261027);
        for (var sample = 0; sample < 16; sample++)
        {
            var digits = Enumerable.Range(1, 9).OrderBy(_ => random.Next()).ToArray();
            var revealed = Enumerable.Range(0, 9).OrderBy(_ => random.Next()).Take(4).ToHashSet();
            var board = digits.Select((digit, index) => revealed.Contains(index) ? digit : 0).ToArray();
            var result = MiniCactpotSolver.Solve(board);
            var sums = new double[8];
            var completions = 0;
            void Enumerate()
            {
                var blank = Array.IndexOf(board, 0);
                if (blank < 0)
                {
                    completions++;
                    for (var line = 0; line < 8; line++) sums[line] += payouts[lines[line].Sum(index => board[index])];
                    return;
                }
                for (var digit = 1; digit <= 9; digit++)
                {
                    if (board.Contains(digit)) continue;
                    board[blank] = digit; Enumerate(); board[blank] = 0;
                }
            }
            Enumerate();
            Check(completions == 120 && result.ChooseLine, "all five hidden digits enumerated independently");
            for (var line = 0; line < 8; line++)
                Check(Math.Abs(result.Choices[line]!.Value - sums[line] / completions) < 0.000001, "line expectation agrees with all completions");
            Check(Math.Abs(result.ExpectedReward - sums.Max() / completions) < 0.000001, "best line maximizes actual expected payout");
        }
    }),
    ("Mini Cactpot selects guaranteed 10000 line and validates digits and cancellation", () =>
    {
        var result = MiniCactpotSolver.Solve([1, 2, 3, 4, 0, 0, 0, 0, 0]);
        Check(result.ChooseLine && result.Choices.Count == 8 && result.Choices[0] == 10000 && result.ExpectedReward == 10000, "sum six line pays 10000");
        Throws<ArgumentException>(() => MiniCactpotSolver.Solve([1, 1, 0, 0, 0, 0, 0, 0, 0]));
        Throws<ArgumentException>(() => MiniCactpotSolver.Solve([0, 0, 0, 0, 0, 0, 0, 0, 0]));
        Throws<ArgumentException>(() => MiniCactpotSolver.Solve([10, 0, 0, 0, 0, 0, 0, 0, 0]));
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        Throws<OperationCanceledException>(() => MiniCactpotSolver.Solve([1, 0, 0, 0, 0, 0, 0, 0, 0], cancel.Token));
    }),
    ("Character attributes copy values and clear all previous-owner evidence", () =>
    {
        var service = new CharacterStatsService();
        var values = new Dictionary<string, int> { ["Craftsmanship"] = 4000, ["Control"] = 3900, ["CraftingPoints"] = 600 };
        service.Publish(1, "Player", 8, "Carpenter", 100, values);
        values["Craftsmanship"] = 0;
        Check(service.State.Values["Craftsmanship"] == 4000 && service.State.ObservedAt is not null, "copied observation");
        service.Clear();
        Check(service.State.CharacterId == 0 && service.State.Values.Count == 0 && service.State.ObservedAt is null, "logout clears attributes");
        Throws<ArgumentOutOfRangeException>(() => service.Publish(0, "", 0, "", 0, values));
    }),
    ("Blue magic keeps native unlock row distinct from displayed spell number", () =>
    {
        var service = new CollectionService();
        service.Publish(10, [new(CollectionKind.BlueMagic, 1, "Souffle nasal", 10, true, 25, 4, "Description", "Lieu"),
            new(CollectionKind.Mount, 1, "Mount", 20, false)]);
        var spell = service.State.Entries.Single(entry => entry.Kind == CollectionKind.BlueMagic);
        Check(spell.Id == 1 && spell.Number == 25 && spell.Rank == 4 && spell.ConfirmedUnlocked, "native row used for unlock, number only for display");
        service.Invalidate();
        spell = service.State.Entries.Single(entry => entry.Kind == CollectionKind.BlueMagic);
        Check(!spell.ConfirmedUnlocked && spell.Description == "Description" && spell.Location == "Lieu", "invalidate acquisition without erasing catalog details");
    }),
    ("Quest observations preserve active sequence and invalidate previous character on logout or errors", () =>
    {
        var service = new QuestService();
        var rows = new List<QuestEntry> { new(65537, "Active", "Story", 1, QuestProgress.Active, 3), new(65538, "Done", "Story", 1, QuestProgress.Complete, null) };
        service.Publish(1, rows);
        rows.Clear();
        Check(service.State.Entries.Count == 2 && service.State.Entries.Any(row => row.Sequence == 3), "copied sequence is not a guessed percentage");
        Throws<NotSupportedException>(() => ((IList<QuestEntry>)service.State.Entries).Clear());
        service.Invalidate();
        Check(service.State.CharacterId == 0 && service.State.Entries.All(row => row.Progress == QuestProgress.Unconfirmed && row.Sequence is null), "logout clears progression");
        service.Publish(0, [new(65537, "Active", "Story", 1, QuestProgress.Active, 3)]);
        Check(service.State.Entries[0].Progress == QuestProgress.Unconfirmed && service.State.ObservedAt is null, "offline observations cannot assert progress");
        service.Fail(new InvalidOperationException("test"));
        Check(service.State.Error == "test" && !service.State.Loading, "explicit failure");
        service.RequestRefresh(); service.RequestRefresh();
        Check(service.ConsumeRefresh() && !service.ConsumeRefresh(), "bounded refresh request");
        service.Clear();
        Check(service.State.Entries.Count == 0, "dispose clears quest data");
    }),
    ("Market hover normalizes HQ only once and excludes unmarketable and special IDs", () =>
    {
        var items = ItemCatalog.Build([new(42, "Marketable", true, IsMarketable: true), new(43, "Bound", true),
            new(44, "NQ only", false, IsMarketable: true)], new("test", "French", "test", 1));
        Check(MarketHoverTracker.Normalize(42, items) == new MarketHoverItem(42, false)
            && MarketHoverTracker.Normalize(1000042, items) == new MarketHoverItem(42, true), "native HQ offset normalized exactly once");
        Check(MarketHoverTracker.Normalize(2000042, items) is null && MarketHoverTracker.Normalize(ulong.MaxValue, items) is null
            && MarketHoverTracker.Normalize(43, items) is null && MarketHoverTracker.Normalize(1000044, items) is null,
            "no modulo for special IDs, no bound or impossible HQ requests");
        var legacy = JsonSerializer.Deserialize<Configuration>("{\"Version\":1}")!;
        Check(!legacy.MarketPricesOnHover, "existing configurations remain opt-out");
        legacy.MarketPricesOnHover = true;
        Check(JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(legacy))!.MarketPricesOnHover, "explicit opt-in survives persistence");
    }),
    ("Market hover requires stable opt-in ownership and throttles repeated attempts", () =>
    {
        var items = ItemCatalog.Build([new(42, "Item", true, IsMarketable: true)], new("test", "French", "test", 1));
        var tracker = new MarketHoverTracker();
        var now = DateTimeOffset.UtcNow;
        tracker.Observe(false, 1, 71, 42, items, now);
        Check(!tracker.TakeRequest(now.AddMinutes(1)), "no request while disabled");
        tracker.Observe(true, 1, 71, 42, items, now);
        Check(!tracker.TakeRequest(now.AddMilliseconds(599)) && tracker.TakeRequest(now.AddMilliseconds(600)), "600ms stable hover required");
        Check(!tracker.TakeRequest(now.AddSeconds(1)) && tracker.TakeRequest(now.AddSeconds(6)), "per-hover retries bounded to five seconds");
        tracker.Observe(true, 2, 71, 42, items, now.AddSeconds(6));
        Check(!tracker.TakeRequest(now.AddSeconds(6)), "same item on another owner restarts delay");
        tracker.Observe(true, 2, 72, 42, items, now.AddSeconds(7));
        Check(!tracker.TakeRequest(now.AddSeconds(7)), "world changes restart delay");
        tracker.Observe(true, 0, 0, 42, items, now.AddSeconds(8));
        Check(tracker.Current is null && !tracker.TakeRequest(now.AddMinutes(1)), "logout clears candidate");
    }),
    ("Market background views share cache and throttle without replacing manual selection", () =>
    {
        var calls = 0;
        using var client = new HttpClient(new MarketTestHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("{\"results\":[{\"itemId\":42}]}") });
        }));
        using var service = new MarketService(client); service.SetWorld(1, 71, "World");
        var before = service.State;
        service.RequestAsync(42, background: true).GetAwaiter().GetResult();
        Check(ReferenceEquals(service.State, before) && service.GetCached(71, 42) is not null, "hover does not change manual state");
        service.RequestAsync(42).GetAwaiter().GetResult();
        Check(calls == 1 && service.State.Quote?.ItemId == 42, "manual view reuses shared cached hover quote");
        service.RequestAsync(43, background: true).GetAwaiter().GetResult();
        Check(calls == 1 && service.BackgroundState?.Error is not null && service.State.Quote?.ItemId == 42, "single shared throttle applies to both views");
    }),
    ("Market background failures back off without changing manual state", () =>
    {
        using var client = new HttpClient(new MarketTestHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable))));
        using var service = new MarketService(client); service.SetWorld(1, 71, "World");
        var before = service.State;
        var started = DateTimeOffset.UtcNow;
        service.RequestAsync(42, background: true).GetAwaiter().GetResult();
        Check(ReferenceEquals(service.State, before) && service.BackgroundState?.Error is not null
            && service.BackgroundState.RetryAt >= started.AddSeconds(30), "failed automatic lookups carry a thirty-second backoff");
    }),
    ("Market hover cancellation rejects late responses while preserving manual requests", () =>
    {
        var response = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = new HttpClient(new MarketTestHandler((_, _) => response.Task));
        using var service = new MarketService(client); service.SetWorld(1, 71, "World");
        var pending = service.RequestAsync(42, background: true);
        Check(service.BackgroundState?.Loading == true && !service.State.Loading, "separate loading state");
        service.CancelBackground();
        response.SetResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("{\"results\":[{\"itemId\":42}]}") });
        pending.GetAwaiter().GetResult();
        Check(service.GetCached(71, 42) is null && service.BackgroundState is null && service.State.ItemId == 0, "cancelled hover cannot publish a quote later");
        var manualResponse = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var manualClient = new HttpClient(new MarketTestHandler((_, _) => manualResponse.Task));
        using var manual = new MarketService(manualClient); manual.SetWorld(1, 71, "World");
        var manualPending = manual.RequestAsync(42); manual.CancelBackground();
        Check(manual.State.Loading, "ending hover does not cancel manual work");
        manualResponse.SetResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("{\"results\":[{\"itemId\":42}]}") });
        manualPending.GetAwaiter().GetResult();
        Check(manual.State.Quote?.ItemId == 42, "manual request still publishes");
    }),
    ("Market manual requests preempt hover work without accepting its late response", () =>
    {
        var response = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken sent = default;
        using var client = new HttpClient(new MarketTestHandler((_, token) => { sent = token; return response.Task; }));
        using var service = new MarketService(client); service.SetWorld(1, 71, "World");
        var hover = service.RequestAsync(42, background: true);
        service.RequestAsync(43).GetAwaiter().GetResult();
        Check(sent.IsCancellationRequested && service.State.ItemId == 43 && service.State.Error is not null,
            "manual demand cancels hover and receives shared-throttle feedback");
        response.SetResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("{\"results\":[{\"itemId\":42}]}") });
        hover.GetAwaiter().GetResult();
        Check(service.State.ItemId == 43 && service.State.Quote is null && service.GetCached(71, 42) is null,
            "late background success cannot replace the manual view or enter cache");
    }),
    ("Market upload freshness never treats missing old or future dates as recent", () =>
    {
        var now = DateTimeOffset.UtcNow;
        var quote = new MarketQuote(71, 42, now, now.AddHours(-1), new(null, null, null), new(null, null, null));
        Check(quote.HasRecentUpload(now), "recent community upload recognized");
        Check(!(quote with { UploadedAt = null }).HasRecentUpload(now)
            && !(quote with { UploadedAt = now.AddHours(-25) }).HasRecentUpload(now)
            && !(quote with { UploadedAt = now.AddHours(1) }).HasRecentUpload(now), "unknown expired and incoherent future upload dates flagged");
    }),
    ("Market prices isolate world and quality and preserve upload versus fetch timestamps", () =>
    {
        var json = """
        {"results":[{"itemId":42,"nq":{"minListing":{"world":{"price":120},"dc":{"price":2}},"recentPurchase":{"world":{"price":110,"timestamp":1700000000000}}},"hq":{"minListing":{"dc":{"price":50}}},"worldUploadTimes":[{"worldId":71,"timestamp":1700000001000}]}]}
        """;
        var fetched = DateTimeOffset.UtcNow;
        var quote = MarketQuote.Parse(json, 71, 42, fetched);
        Check(quote.Nq.Minimum == 120 && quote.Nq.RecentSale == 110 && quote.Hq.Minimum is null, "DC never substituted for world; missing HQ not zero");
        Check(quote.UploadedAt == DateTimeOffset.FromUnixTimeMilliseconds(1700000001000)
            && quote.FetchedAt == fetched && quote.Nq.SaleAt != quote.UploadedAt, "three different timestamp meanings");
        Throws<InvalidDataException>(() => MarketQuote.Parse(json, 71, 43, fetched));
        Check(MarketQuote.Parse("{\"results\":[{\"itemId\":42,\"nq\":{\"minListing\":{\"world\":{\"price\":0}}}}]}", 71, 42, fetched).Nq.Minimum is null,
            "zero price is unknown");
    }),
    ("Market requests are manual and cached by world and item", () =>
    {
        var calls = 0;
        using var client = new HttpClient(new MarketTestHandler((request, _) =>
        {
            calls++;
            Check(request.RequestUri!.AbsolutePath == "/api/v2/aggregated/71/42", "only expected public IDs transmitted");
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("{\"results\":[{\"itemId\":42}]}") });
        }));
        using var service = new MarketService(client);
        service.RequestAsync(42).GetAwaiter().GetResult();
        Check(calls == 0, "no request without current world");
        service.SetWorld(1, 71, "World");
        Check(calls == 0, "world change does not initiate network");
        service.RequestAsync(42).GetAwaiter().GetResult();
        service.RequestAsync(42).GetAwaiter().GetResult();
        Check(calls == 1 && service.State.Quote?.ItemId == 42 && !service.State.Loading, "cached requests coalesced");
        service.SetWorld(2, 72, "Other");
        Check(service.State.Quote is null && service.State.WorldId == 72, "quote cleared on world or owner change");
    }),
    ("Market owner and world changes cancel pending quotes without late publication", () =>
    {
        foreach (var next in new[] { (Owner: 2ul, World: 71u), (Owner: 1ul, World: 72u), (Owner: 0ul, World: 0u) })
        {
            var response = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationToken sentToken = default;
            using var client = new HttpClient(new MarketTestHandler((_, token) => { sentToken = token; return response.Task; }));
            using var service = new MarketService(client);
            service.SetWorld(1, 71, "First");
            var pending = service.RequestAsync(42);
            service.SetWorld(next.Owner, next.World, "Next");
            Check(sentToken.IsCancellationRequested && !service.State.Loading, "old HTTP work cancelled immediately");
            response.SetResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("{\"results\":[{\"itemId\":42}]}") });
            pending.GetAwaiter().GetResult();
            Check(service.State.WorldId == next.World && service.State.ItemId == 0 && service.State.Quote is null
                && service.State.Error is null, "old result or error cannot cross identity boundary");
        }
    }),
    ("Market throttles forced and changed-item requests and reports HTTP errors explicitly", () =>
    {
        var calls = 0;
        using var client = new HttpClient(new MarketTestHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable));
        }));
        using var service = new MarketService(client);
        service.SetWorld(1, 71, "World");
        service.RequestAsync(42).GetAwaiter().GetResult();
        Check(calls == 1 && !service.State.Loading && service.State.Error is not null && service.State.Quote is null,
            "HTTP error is not an empty successful quote");
        service.RequestAsync(43, true).GetAwaiter().GetResult();
        Check(calls == 1 && service.State.ItemId == 43 && service.State.Error is not null, "forced calls respect network throttle");
    }),
    ("Market disposal prevents delayed request publication", () =>
    {
        var response = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = new HttpClient(new MarketTestHandler((_, _) => response.Task));
        var service = new MarketService(client);
        service.SetWorld(1, 71, "World");
        var pending = service.RequestAsync(42);
        Check(service.State.Loading, "pending request visible");
        service.Dispose();
        response.SetResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("{\"results\":[{\"itemId\":42}]}") });
        pending.GetAwaiter().GetResult();
        Check(service.State.WorldId == 0 && service.State.Quote is null && !service.State.Loading, "no late publication after dispose");
        service.Dispose();
    }),
    ("Collections clear confirmations on logout invalidation and refuse offline ownership", () =>
    {
        var service = new CollectionService();
        var source = new List<CollectionEntry> { new(CollectionKind.Mount, 1, "Mount", 1, true) };
        service.Publish(10, source);
        source.Clear();
        Check(service.State.CharacterId == 10 && service.State.Entries[0].ConfirmedUnlocked, "copied positive evidence");
        service.Invalidate();
        Check(service.State.CharacterId == 0 && service.State.Entries.All(entry => !entry.ConfirmedUnlocked)
            && service.State.ObservedAt is null, "no previous-owner evidence retained");
        service.Publish(0, [new(CollectionKind.Companion, 1, "Companion", 2, true)]);
        Check(!service.State.Entries[0].ConfirmedUnlocked && service.State.ObservedAt is null, "offline cannot confirm ownership");
        service.Clear();
        Check(service.State.Entries.Count == 0, "dispose clears entries");
    }),
    ("Collections isolate categories and keep refresh and error evidence explicit", () =>
    {
        var service = new CollectionService();
        service.Publish(10, [new(CollectionKind.Mount, 1, "Mount", 1, true), new(CollectionKind.Companion, 1, "Companion", 2, false)]);
        Check(service.State.Entries.Count == 2 && service.State.Entries.Count(entry => entry.ConfirmedUnlocked) == 1,
            "same row ID in different sheets not merged; false remains unconfirmed");
        Throws<NotSupportedException>(() => ((IList<CollectionEntry>)service.State.Entries).Clear());
        service.RequestRefresh(); service.RequestRefresh();
        Check(service.ConsumeRefresh() && !service.ConsumeRefresh(), "refresh coalesced");
        service.Fail(new InvalidOperationException("read failed"));
        Check(service.State.Error == "read failed" && service.State.Entries.All(entry => !entry.ConfirmedUnlocked), "errors invalidate ownership");
    }),
    ("Sort preview preserves separate stacks and qualities without modifying source slots", () =>
    {
        var container = new InventoryContainerSnapshot(new(new(1), new(0)), InventoryCoverage.Complete,
            [new(new(new(1), false), 2, 12, 5), new(new(new(2), true), 9, 3, 1), new(new(new(1), false), 4, 20, 30)]);
        var preview = InventorySortPreview.Build([container], 35, InventorySortField.Quantity, true, id => ($"Item {id}", 1));
        Check(preview.Stacks.Count == 3 && preview.Stacks[0].Stack.Quantity == 9, "descending quantity and no stack merging");
        Check(container.Stacks[0].Slot == 12 && container.Stacks[0].DisplayIndex == 5, "native and visual slots unchanged");
        Check(preview.Stacks[0].Origin == container.Location && preview.Stacks[0].Stack.Item.IsHighQuality, "owner and quality retained");
        Throws<NotSupportedException>(() => ((IList<InventoryPreviewStack>)preview.Stacks).Clear());
    }),
    ("Sort preview uses native categories and refuses incomplete mixed or overflowing sources", () =>
    {
        var container = new InventoryContainerSnapshot(new(new(1), new(0)), InventoryCoverage.Complete,
            [new(new(new(1), false), 2), new(new(new(2), true), 9)]);
        var preview = InventorySortPreview.Build([container], 35, InventorySortField.Category, false, id => ($"Item {id}", id == 2 ? 1u : 2u));
        Check(preview.Stacks[0].Stack.Item.Id.Value == 2, "native category ordering");
        Throws<InvalidOperationException>(() => InventorySortPreview.Build([container], 1, InventorySortField.Name, false, _ => ("", 0)));
        var partial = new InventoryContainerSnapshot(new(new(1), new(1)), InventoryCoverage.Partial, []);
        Throws<InvalidOperationException>(() => InventorySortPreview.Build([container, partial], 70, InventorySortField.Name, false, _ => ("", 0)));
        var other = new InventoryContainerSnapshot(new(new(2), new(1)), InventoryCoverage.Complete, []);
        Throws<InvalidOperationException>(() => InventorySortPreview.Build([container, other], 70, InventorySortField.Name, false, _ => ("", 0)));
        Throws<InvalidOperationException>(() => InventorySortPreview.Build([container, container], 70, InventorySortField.Name, false, _ => ("", 0)));
    }),
    ("Sort preview preferences persist without changing legacy configuration defaults", () =>
    {
        var config = new Configuration { SortPreviewField = 3, SortPreviewDescending = true };
        var loaded = JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(config))!;
        Check(loaded.SortPreviewField == 3 && loaded.SortPreviewDescending, "sort preferences roundtrip");
        var legacy = JsonSerializer.Deserialize<Configuration>("{\"Version\":1}")!;
        Check(legacy.SortPreviewField == 0 && !legacy.SortPreviewDescending, "legacy safely defaults to name ascending");
    }),
    ("Gathering sources preserve item identity and distinct zones while coalescing equivalent points", () =>
    {
        var source = new GatheringSource(42, 100, 10, 0, "Extraction", 1, 30, 20, "Zone A", "Lieu A");
        var rows = new List<GatheringSource> { source, source with { PointId = 101 }, source with { PointId = 102, TerritoryId = 21, Zone = "Zone B" } };
        var catalog = new GatheringCatalog(rows, "test-version");
        rows.Clear();
        Check(catalog.ForItem(42).Count == 2 && catalog.ForItem(10).Count == 0, "base ID is not item ID and zones stay distinct");
        Check(catalog.Count == 2 && catalog.Source == "test-version", "copied deterministic index");
        Check(catalog.Sources.Count == catalog.Count && catalog.Sources.All(row => row.ItemId == 42), "browser and per-item views share the same copied sources");
        Throws<NotSupportedException>(() => ((IList<GatheringSource>)catalog.Sources).Clear());
        var copy = (IList<GatheringSource>)catalog.ForItem(42);
        Throws<NotSupportedException>(() => copy[0] = source);
    }),
    ("Gathering rejects missing locations and supports cancellation and service clearing", () =>
    {
        var source = new GatheringSource(42, 100, 10, 0, "Extraction", 1, 30, 20, "Zone A", "Lieu A");
        var catalog = new GatheringCatalog([source with { ItemId = 0 }, source with { Zone = "" }, source with { TerritoryId = 0 }], "test");
        Check(catalog.Count == 0, "no invented locations for incomplete rows");
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        Throws<OperationCanceledException>(() => new GatheringCatalog([source], "test", cancel.Token));
        var service = new GatheringCatalogService();
        service.Progress(3, 10);
        Check(service.State.Catalog is null && service.State.Copied == 3, "loading is not an empty ready catalog");
        service.Publish(catalog);
        Check(service.State.Catalog == catalog, "published atomically");
        service.Fail(new InvalidOperationException("test-error"));
        Check(service.State.Catalog is null && service.State.Error == "test-error", "error not empty source evidence");
        service.Clear();
        Check(service.State.Catalog is null && service.State.Error is null, "cleared for disposal");
    }),
    ("Fishing and gathering sources never collide when row identifiers coincide", () =>
    {
        var source = new GatheringSource(42, 100, 10, 0, "Extraction", 1, 30, 20, "Zone A", "Lieu A", 7);
        var catalog = new GatheringCatalog([source, source with { IsFishing = true, TypeId = 100, TypeName = "Pêcheur" }], "test");
        Check(catalog.ForItem(42).Count == 2 && catalog.ForItem(42).Any(row => row.IsFishing), "different sheets keep separate identities");
        Check(catalog.ForItem(42).All(row => row.MapId == 7), "zone map link identity retained");
    }),
    ("Craft plan rounds yield up and merges repeated ingredient requirements", () =>
    {
        var recipe = new RecipeDefinition(1, 500, 3, "Cuisine", 59,
            [new(42, 2), new(42, 1), new(43, 8)]);
        var plan = CraftPlan.Build(recipe, 7, id => new([], id == 42 ? 2 : 30, id == 42 ? 4 : 0, true, false, false, true));
        Check(plan.Crafts == 3 && plan.Produced == 9 && plan.Requested == 7, "yield rounds upward without subtracting owned result");
        Check(plan.Materials.Count == 2 && plan.Materials[0].Required == 9, "duplicate ingredients add");
        Check(plan.Materials[0].Uncovered == 3 && plan.Materials[1].Uncovered == 0, "NQ and HQ combined; excess stock never negative");
    }),
    ("Craft plan preserves unknown and cached coverage and rejects invalid targets", () =>
    {
        var recipe = new RecipeDefinition(1, 500, 1, "Cuisine", 59, [new(42, 2)]);
        var unknown = new ItemHoldingsSummary([], 0, 0, false, true, false, false);
        var plan = CraftPlan.Build(recipe, 4, _ => unknown);
        Check(!plan.Materials[0].Holdings.HasObservation && plan.Materials[0].Holdings.HasUnknown, "unknown remains unknown");
        var cached = new ItemHoldingsSummary([], 5, 2, true, true, true, false);
        var cachedPlan = CraftPlan.Build(recipe, 4, _ => cached);
        Check(cachedPlan.Materials[0].Uncovered == 1 && cachedPlan.Materials[0].Holdings.HasCached, "cache provenance retained");
        Throws<ArgumentOutOfRangeException>(() => CraftPlan.Build(recipe, 0, _ => unknown));
        Throws<ArgumentOutOfRangeException>(() => CraftPlan.Build(recipe with { ResultAmount = 0 }, 1, _ => unknown));
    }),
    ("Display order moves physical stacks across visual bags and preserves holes", () =>
    {
        var first = new InventoryContainerSnapshot(new(new(1), new(0)), InventoryCoverage.Complete,
            [new(new(new(42), false), 9, 0, 40), new(new(new(42), true), 2, 1, 6)]);
        var second = new InventoryContainerSnapshot(new(new(1), new(1)), InventoryCoverage.Complete,
            [new(new(new(77), false), 3, 0, 0)]);
        var pages = InventoryDisplayLayout.Build([first, second]);
        Check(pages.Count == 2 && pages[0][0]?.Item.Id.Value == 77, "sort can cross physical bags");
        Check(pages[0][1] is null && pages[0][6]?.Item.IsHighQuality == true, "holes and HQ retained");
        Check(pages[1][5]?.Quantity == 9 && first.Quantities.Count == 2, "display does not mutate physical counts");
        Throws<InvalidOperationException>(() => InventoryDisplayLayout.Build([
            new InventoryContainerSnapshot(first.Location, InventoryCoverage.Complete, [new(new(new(42), false), 1)])]));
    }),
    ("Imported slots and visual order survive bank disk reload", () =>
    {
        var directory = Path.Combine(Path.GetTempPath(), "Tensho-slot-test-" + Guid.NewGuid().ToString("N"));
        var file = Path.Combine(directory, "bank.json");
        try
        {
            var entry = new BankEntry(new(1, BankStorageKind.Saddlebag, 0), "Chocobo",
                new InventorySnapshot(DateTimeOffset.UtcNow, "test", [
                    new InventoryContainerSnapshot(new(new(1), new(4000)), InventoryCoverage.Complete,
                        [new(new(new(42), false), 3, 2, 37), new(new(new(42), false), 8, 5, 0)]),
                    new InventoryContainerSnapshot(new(new(1), new(4001)), InventoryCoverage.Complete, [])]));
            new FileInventoryBankStore(file).Save([entry]);
            var loaded = new FileInventoryBankStore(file).Load()[0];
            var pages = InventoryDisplayLayout.Build(loaded.Snapshot.Containers.Values);
            Check(pages[0][0]?.Quantity == 8 && pages[1][2]?.Quantity == 3, "split stacks and cross-page order restored");
        }
        finally
        {
            if (File.Exists(file)) File.Delete(file);
            if (Directory.Exists(directory)) Directory.Delete(directory);
        }
    }),
    ("Crystal classifier separates French elements and sizes", () =>
    {
        Check(CrystalClassifier.Classify("Éclat d'eau") is { Element: "Eau", Size: "Éclats" }, "water elision");
        Check(CrystalClassifier.Classify("Cristal de foudre") is { Element: "Foudre", Size: "Cristaux" }, "lightning crystal");
        Check(CrystalClassifier.Classify("Agrégat de feu") is { Element: "Feu", Size: "Agrégats" }, "fire cluster");
        Check(CrystalClassifier.Classify("Potion de soin") is null, "unrelated item ignored");
    }),
    ("Recipe catalog preserves variants and links ingredients", () =>
    {
        var recipes = RecipeCatalog.Build([
            new RecipeDefinition(10, 500, 3, "Cuisine", 90, [new RecipeIngredient(100, 2), new RecipeIngredient(101, 1)], 7),
            new RecipeDefinition(11, 500, 1, "Cuisine", 90, [new RecipeIngredient(102, 4)])
        ], "test=1");
        Check(recipes.Count == 2 && recipes.ForResult(500).Count == 2 && recipes.ForIngredient(100).Count == 1, "variants and ingredient links preserved");
        Check(recipes.ForResult(500)[0].Ingredients[0].ItemId == 100, "ingredient ID linked");
        Check(recipes.ForResult(500)[0].ResultAmount == 3, "yield preserved");
        Check(recipes.ForIngredient(100)[0].CraftTypeId == 7, "native craft identity preserved in reverse links");
    }),
    ("Recipe catalog isolates source mutations and prevents mutation through public views", () =>
    {
        var ingredients = new List<RecipeIngredient> { new(42, 2) };
        var source = new List<RecipeDefinition> { new(1, 500, 3, "Cuisine", 59, ingredients) };
        var catalog = RecipeCatalog.Build(source, "test");
        ingredients.Clear(); source.Clear();
        var recipe = catalog.ForResult(500)[0];
        Check(recipe.Ingredients.Count == 1 && catalog.ForIngredient(42)[0] == recipe, "both indexes share the same immutable recipe copy");
        Throws<NotSupportedException>(() => ((IList<RecipeDefinition>)catalog.ForResult(500)).Clear());
        Throws<NotSupportedException>(() => ((IList<RecipeDefinition>)catalog.ForIngredient(42))[0] = recipe with { ResultItemId = 999 });
        Throws<NotSupportedException>(() => ((IList<RecipeIngredient>)recipe.Ingredients).Clear());
        Check(catalog.ForResult(500)[0].Ingredients[0].Amount == 2 && catalog.ForResult(999).Count == 0, "failed mutations preserve all consumers");
    }),
    ("Recipe catalog cancellation applies before and during copying", () =>
    {
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        Throws<OperationCanceledException>(() => RecipeCatalog.Build([], "test", cancel.Token));
        using var duringCopy = new CancellationTokenSource();
        IEnumerable<RecipeDefinition> Rows()
        {
            yield return new(1, 100, 1, "A", 1, [new(5, 1)]);
            duringCopy.Cancel();
            yield return new(2, 101, 1, "A", 1, [new(6, 1)]);
        }
        Throws<OperationCanceledException>(() => RecipeCatalog.Build(Rows(), "test", duringCopy.Token));
    }),
    ("Recipe catalog rejects duplicate or invalid recipe rows", () =>
    {
        Throws<ArgumentException>(() => RecipeCatalog.Build([
            new RecipeDefinition(10, 500, 1, "", 1, [new RecipeIngredient(100, 0)]),
            new RecipeDefinition(10, 500, 1, "", 1, [new RecipeIngredient(100, 1)])
        ], "test=1"));
    }),
    ("Configuration defaults and persistence round trip", () =>
    {
        var settings = new Configuration();
        Check(settings.Version == 1 && !settings.OpenOnLoad && !settings.OpenOnLogin, "safe defaults");
        foreach (var onLoad in new[] { false, true })
        foreach (var onLogin in new[] { false, true })
        {
            settings.OpenOnLoad = onLoad;
            settings.OpenOnLogin = onLogin;
            var loaded = JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(settings))!;
            Check(loaded.Version == 1 && loaded.OpenOnLoad == onLoad && loaded.OpenOnLogin == onLogin,
                "load and login settings persisted independently");
        }
    }),
    ("Existing configuration keeps load setting without enabling login opening", () =>
    {
        var loaded = JsonSerializer.Deserialize<Configuration>("{\"Version\":1,\"OpenOnLoad\":true}")!;
        Check(loaded.OpenOnLoad && !loaded.OpenOnLogin, "existing preference preserved, new option defaults off");
    }),
    ("Dispose reverses registrations and is idempotent", () =>
    {
        var order = new List<int>();
        var scope = new RegistrationScope();
        scope.Register(() => order.Add(1), () => order.Add(4));
        scope.Register(() => order.Add(2), () => order.Add(3));
        scope.Dispose();
        scope.Dispose();
        Check(order.SequenceEqual([1, 2, 3, 4]), "reverse release exactly once");
        Throws<ObjectDisposedException>(() => scope.Register(() => { }, () => { }));
    }),
    ("Failed initialization releases only owned registrations", () =>
    {
        var active = 0;
        using (var scope = new RegistrationScope())
        {
            scope.Register(() => active++, () => active--);
            Throws<InvalidOperationException>(() => scope.Register(
                () => throw new InvalidOperationException(), () => active -= 100));
        }
        Check(active == 0, "no leak and no cleanup of unowned resource");
    }),
    ("Cleanup continues after a release throws", () =>
    {
        var released = false;
        var scope = new RegistrationScope();
        scope.Register(() => { }, () => released = true);
        scope.Register(() => { }, () => throw new InvalidOperationException());
        Throws<AggregateException>(scope.Dispose);
        Check(released, "remaining resources released");
        scope.Dispose();
    }),
    ("Reload does not retain old subscriptions", () =>
    {
        Action? signal = null;
        var calls = 0;
        Action callback = () => calls++;
        for (var i = 0; i < 3; i++)
        {
            using (var scope = new RegistrationScope())
            {
                scope.Register(() => signal += callback, () => signal -= callback);
                signal?.Invoke();
            }
            Check(signal is null, "no callback after unload");
        }
        Check(calls == 3, "one callback per load");
    }),
};

var failures = 0;
tests = tests.Concat(DutyLootTests.Cases).ToArray();
tests = tests.Concat(DutyLootServiceTests.Cases).ToArray();
tests = tests.Concat(MonsterLootTests.Cases).ToArray();
tests = tests.Concat(GpOptimizerTests.Cases).ToArray();
tests = tests.Concat(FashionTests.Cases).ToArray();
tests = tests.Concat(CraftSimulationTests.Cases).ToArray();
tests = tests.Concat(CraftMacroTests.Cases).ToArray();
tests = tests.Concat(CraftLogTests.Cases).ToArray();
tests = tests.Concat(CraftCollectableTests.Cases).ToArray();
tests = tests.Concat(CraftOptimizationTests.Cases).ToArray();
tests = tests.Concat(CacheTests.Cases).ToArray();
tests = tests.Concat(CraftSolverTests.Cases).ToArray();
tests = tests.Concat(CraftAdvancedTests.Cases).ToArray();
tests = tests.Concat(CraftLiveTests.Cases).ToArray();
tests = tests.Concat(CraftObservedEffectsTests.Cases).ToArray();
tests = tests.Concat(WeatherForecastTests.Cases).ToArray();
tests = tests.Concat(GatheringTimelineTests.Cases).ToArray();
tests = tests.Concat(CraftPresetTests.Cases).ToArray();
tests = tests.Concat(CraftProjectionTests.Cases).ToArray();
tests = tests.Concat(CraftConditionTests.Cases).ToArray();
tests = tests.Concat(CraftConditionEnvelopeTests.Cases).ToArray();
tests = tests.Concat(MiniCactpotObservationTests.Cases).ToArray();
tests = tests.Concat(GatheringObservationTests.Cases).ToArray();
tests = tests.Concat(GpActiveEffectsTests.Cases).ToArray();
tests = tests.Concat(ItemUseCatalogTests.Cases).ToArray();
tests = tests.Concat(ItemHoverTests.Cases).ToArray();
tests = tests.Concat(ItemContextMenuTests.Cases).ToArray();
tests = tests.Concat(InventoryVisualSortTests.Cases).ToArray();
tests = tests.Concat(InventorySortScopeTests.Cases).ToArray();
tests = tests.Concat(InventorySortRulesTests.Cases).ToArray();
tests = tests.Concat(InventoryProtectedSortTests.Cases).ToArray();
tests = tests.Concat(InventorySortMetadataTests.Cases).ToArray();
tests = tests.Concat(InventorySortTemplateTests.Cases).ToArray();
tests = tests.Concat(InventorySortSelectionTests.Cases).ToArray();
tests = tests.Concat(ItemCatalogCacheTests.Cases).ToArray();
tests = tests.Concat(RecipeCatalogCacheTests.Cases).ToArray();
tests = tests.Concat(GatheringCatalogCacheTests.Cases).ToArray();
tests = tests.Concat(InventorySortPropsTests.Cases).ToArray();
tests = tests.Concat(MarketHoverDetailsTests.Cases).ToArray();
tests = tests.Concat(MonsterHoverSourcesTests.Cases).ToArray();
tests = tests.Concat(NpcVendorTests.Cases).ToArray();
tests = tests.Concat(ThemePaletteTests.Cases).ToArray();
tests = tests.Concat(MapProjectionTests.Cases).ToArray();
tests = tests.Concat(MapLocalIndexTests.Cases).ToArray();
tests = tests.Concat(WikiVistaTests.Cases).ToArray();
tests = tests.Concat(GarlandItemSourceTests.Cases).ToArray();
tests = tests.Concat(WikiItemAcquisitionTests.Cases).ToArray();

tests = tests.Concat(FishingNotebookTests.Cases).ToArray();
tests = tests.Concat(FishingSourcesTests.Cases).ToArray();
tests = tests.Concat(FishingConditionsTests.Cases).ToArray();
tests = tests.Concat(WeatherNamesTests.Cases).ToArray();
tests = tests.Concat(FishingForecastTests.Cases).ToArray();
tests = tests.Concat(OrchestrionTests.Cases).ToArray();
tests = tests.Concat(CollectionEvidenceTests.Cases).ToArray();
tests = tests.Concat(QuestRewardsTests.Cases).ToArray();
tests = tests.Concat(QuestRewardIndexTests.Cases).ToArray();
tests = tests.Concat(QuestLinkTests.Cases).ToArray();
tests = tests.Concat(QuestItemProgressTests.Cases).ToArray();
var allTests = tests.Concat(ChainCostTests.Cases).Concat(PreparationCostTests.Cases).Concat(TriadObservationTests.Cases).Concat(TriadMatchAdvisorTests.Cases).Concat(TriadMatchSimulatorTests.Cases).Concat(CompletionMemoryTests.Cases).Concat(TriadNpcCatalogTests.Cases).Concat(StatFormulaTests.Cases).Concat(LuminaSupplementalTests.Cases).Concat(MacroSlotPlanTests.Cases).Concat(AlarmScheduleTests.Cases).Concat(TriadFamilyTests.Cases).Concat(TriadComboTests.Cases).Concat(TriadRulesTests.Cases).Concat(TriadSolverTests.Cases).Concat(TriadCardTests.Cases).Concat(GatheringScheduleTests.Cases).Concat(InventoryTests.Cases).Concat(InventoryCaptureTests.Cases)
    .Concat(InventoryReadEvidenceTests.Cases).Concat(ItemCatalogTests.Cases).Concat(InventoryBankTests.Cases)
    .Concat(ItemHoldingsTests.Cases).Concat(CraftChainTests.Cases).Concat(MapCatalogTests.Cases).Concat(MapBookmarkTests.Cases).Concat(MacroSequenceTests.Cases).Concat(MacroChainTests.Cases).ToArray();
foreach (var test in allTests)
{
    try { test.Run(); Console.WriteLine($"PASS {test.Name}"); }
    catch (Exception error) { failures++; Console.Error.WriteLine($"FAIL {test.Name}: {error}"); }
}
Console.WriteLine($"{allTests.Length - failures}/{allTests.Length} passed");
return failures == 0 ? 0 : 1;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

static void Throws<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}");
}
