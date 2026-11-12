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
        Check(calls == 1 && service.BackgroundState?.Error is null && service.State.Quote?.ItemId == 42, "single shared throttle applies to both views, without an error during the pause");
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
        Check(CrystalClassifier.Classify("Éclat d'eau") is { Element: "Water", Size: "Shards" }, "water elision");
        Check(CrystalClassifier.Classify("Cristal de foudre") is { Element: "Lightning", Size: "Crystals" }, "lightning crystal");
        Check(CrystalClassifier.Classify("Agrégat de feu") is { Element: "Fire", Size: "Clusters" }, "fire cluster");
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
    ("Market board parses listings and sales, names the world of a world scope and keeps the tax", () =>
    {
        // Shape of docs/evidence/0.270-universalis-listings.md (world scope: no worldName per row).
        var json = """{"listings":[{"lastReviewTime":1793558424,"pricePerUnit":90,"quantity":99,"hq":false,"retainerName":"Pedalina","total":8910,"tax":445},{"pricePerUnit":0,"quantity":5},{"pricePerUnit":80,"quantity":1,"hq":true,"worldName":"Moogle","tax":4}],"recentHistory":[{"hq":false,"pricePerUnit":100,"quantity":49,"timestamp":1793557514},{"hq":true,"pricePerUnit":200,"quantity":1,"timestamp":1793557600,"worldName":"Moogle"}],"regularSaleVelocity":7,"hqSaleVelocity":0}""";
        var board = TenshoAssistant.Market.MarketBoard.Parse(json, "Ragnarok", 5057, DateTimeOffset.UnixEpoch);
        Check(board.Offers.Count == 2 && board.Offers[0].UnitPrice == 80 && board.Offers[0].World == "Moogle", "invalid row dropped, cheapest first, world kept");
        Check(board.Offers[1].World == "Ragnarok" && board.Offers[1].Retainer == "Pedalina" && board.Offers[1].Cost == 8910 + 445, "world scope name used, tax added to the cost");
        Check(board.Sales.Count == 2 && board.Sales[0].Hq && board.UnitsPerDay == 7 && board.HqUnitsPerDay == 0 && board.NqUnitsPerDay is null, "sales newest first, velocities read, missing one stays unknown");
    }),
    ("Market filter applies quality, quantity, price and world bounds", () =>
    {
        var offer = new TenshoAssistant.Market.MarketOffer(100, 20, true, "Moogle", "", 0, null);
        Check(new TenshoAssistant.Market.MarketFilter().Accepts(offer), "no bound accepts");
        Check(!new TenshoAssistant.Market.MarketFilter(Hq: false).Accepts(offer), "quality");
        Check(!new TenshoAssistant.Market.MarketFilter(MinQuantity: 21).Accepts(offer) && !new TenshoAssistant.Market.MarketFilter(MaxQuantity: 19).Accepts(offer), "quantity bounds");
        Check(!new TenshoAssistant.Market.MarketFilter(MinPrice: 101).Accepts(offer) && !new TenshoAssistant.Market.MarketFilter(MaxPrice: 99).Accepts(offer), "price bounds");
        Check(new TenshoAssistant.Market.MarketFilter(MinPrice: 100, MaxPrice: 100, World: "Moogle").Accepts(offer) && !new TenshoAssistant.Market.MarketFilter(World: "Omega").Accepts(offer), "inclusive bounds and world");
    }),
    ("Cheapest plan buys whole listings, may skip the lowest unit price and says when units are missing", () =>
    {
        TenshoAssistant.Market.MarketOffer Offer(long price, int quantity, long tax = 0) => new(price, quantity, false, "W", "", tax, null);
        var offers = new[] { Offer(10, 5), Offer(11, 10), Offer(12, 3) };
        var plan = TenshoAssistant.Market.MarketBoardMath.Cheapest(offers, 8);
        Check(plan is { Cost: 86, Bought: 8 } && plan.Offers.Count == 2, "5×10 + 3×12 = 86 beats 10×11 = 110");
        var big = TenshoAssistant.Market.MarketBoardMath.Cheapest(offers, 6);
        Check(big is { Cost: 86, Bought: 8 }, "6 units: the 5 + 3 listings (86) beat the 10-unit listing (110)");
        Check(TenshoAssistant.Market.MarketBoardMath.Cheapest([Offer(10, 5), Offer(1, 100, 5)], 3) is { Cost: 50, Bought: 5 }, "5×10 = 50 beats 100×1 + 5 tax = 105");
        Check(TenshoAssistant.Market.MarketBoardMath.Cheapest([Offer(30, 5), Offer(1, 100)], 3) is { Cost: 100, Bought: 100 }, "a bigger listing wins when its total is lower");
        Check(TenshoAssistant.Market.MarketBoardMath.Cheapest(offers, 19) is null && TenshoAssistant.Market.MarketBoardMath.Cheapest(offers, 0) is null, "not enough units or nothing wanted");
        var stats = TenshoAssistant.Market.MarketBoardMath.Stats([new(100, 3, false, "W", DateTimeOffset.UnixEpoch), new(200, 1, false, "W", DateTimeOffset.UnixEpoch)]);
        Check(stats is { Sales: 2, Units: 4, Minimum: 100, Maximum: 200 } && Math.Abs(stats.Average - 125) < 0.001, "average weighted by quantity");
        Check(TenshoAssistant.Market.MarketBoardMath.Stats([]) is null, "no sale, no stats");
    }),
    ("Starting-city quests are those whose chain leads back to one city only; the played city is the one with a completed quest", () =>
    {
        TenshoAssistant.Quests.QuestEntry Quest(uint id, string place, TenshoAssistant.Quests.QuestProgress progress, params uint[] previous)
            => new(id, $"Q{id}", "Genre", 0, progress, null) { GenreId = 1, Place = place, PreviousQuestIds = previous };
        var unconfirmed = TenshoAssistant.Quests.QuestProgress.Unconfirmed;
        var entries = new[]
        {
            Quest(1, "Gridania", TenshoAssistant.Quests.QuestProgress.Complete, 900), // root: its previous quest (class quest) is another genre
            Quest(2, "Limsa Lominsa", unconfirmed),
            Quest(3, "Forêt", TenshoAssistant.Quests.QuestProgress.Complete, 1),   // Gridania line, place of the quest itself does not matter
            Quest(4, "Noscea", unconfirmed, 2),                                      // Limsa line
            Quest(5, "Thanalan", unconfirmed, 3, 4),                                 // shared line
            new TenshoAssistant.Quests.QuestEntry(6, "Other", "Genre", 0, unconfirmed, null) { GenreId = 2, Place = "Limsa Lominsa" },
        };
        var cities = TenshoAssistant.Quests.QuestStartCities.Of(entries);
        Check(cities[1] == "Gridania" && cities[3] == "Gridania" && cities[2] == "Limsa Lominsa" && cities[4] == "Limsa Lominsa", "city lines follow their roots");
        Check(!cities.ContainsKey(5) && !cities.ContainsKey(6), "shared quests and other genres belong to no city");
        Check(TenshoAssistant.Quests.QuestStartCities.PlayerCity(entries, cities) == "Gridania", "played city = the one with completed quests");
        Check(TenshoAssistant.Quests.QuestStartCities.PlayerCity(entries.Select(entry => entry with { Progress = unconfirmed }), cities) is null, "nothing confirmed: no city, nothing hidden");
    }),
    ("Reminder resets follow the game's calendar times in UTC", () =>
    {
        // Monday 2026-11-02 08:33 UTC (the calendar capture: supply missions 2.11 21:00 CET, deliveries 3.11 09:00 CET).
        var now = new DateTimeOffset(2026, 11, 2, 8, 33, 0, TimeSpan.Zero);
        Check(TenshoAssistant.Reminders.ResetSchedule.NextDaily(now, 20) == new DateTimeOffset(2026, 11, 2, 20, 0, 0, TimeSpan.Zero), "supply missions today 20:00 UTC");
        Check(TenshoAssistant.Reminders.ResetSchedule.NextWeekly(now, DayOfWeek.Tuesday, 8) == new DateTimeOffset(2026, 11, 3, 8, 0, 0, TimeSpan.Zero), "weekly Tuesday 08:00 UTC");
        Check(TenshoAssistant.Reminders.ResetSchedule.NextDaily(now.AddHours(12), 20) == new DateTimeOffset(2026, 11, 3, 20, 0, 0, TimeSpan.Zero), "after the reset: tomorrow");
        var tuesdayAfter = new DateTimeOffset(2026, 11, 3, 9, 0, 0, TimeSpan.Zero);
        Check(TenshoAssistant.Reminders.ResetSchedule.NextWeekly(tuesdayAfter, DayOfWeek.Tuesday, 8) == new DateTimeOffset(2026, 11, 10, 8, 0, 0, TimeSpan.Zero), "after the weekly reset: next week");
        var last = TenshoAssistant.Reminders.ResetSchedule.PreviousWeekly(now, DayOfWeek.Tuesday, 8);
        Check(last == new DateTimeOffset(2026, 10, 27, 8, 0, 0, TimeSpan.Zero), "previous weekly reset");
        Check(TenshoAssistant.Reminders.ResetSchedule.DoneSince(last.AddHours(1), last) && !TenshoAssistant.Reminders.ResetSchedule.DoneSince(last.AddHours(-1), last) && !TenshoAssistant.Reminders.ResetSchedule.DoneSince(null, last), "done until the next reset");
    }),
    ("Free company voyages: state, experience, merge by house and change detection (R-07)", () =>
    {
        var now = new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
        var sub = new TenshoAssistant.FreeCompany.VoyageVessel { HouseId = 7, Kind = TenshoAssistant.FreeCompany.VesselKind.Submarine, Slot = 1, Name = "A", Rank = 10, Exp = 50, NextExp = 200, ReturnAt = now.AddHours(3), Route = ["X", "Y"] };
        Check(TenshoAssistant.FreeCompany.Voyages.StateOf(sub, now) == TenshoAssistant.FreeCompany.VoyageState.Voyaging, "out until its return");
        Check(TenshoAssistant.FreeCompany.Voyages.StateOf(sub, now.AddHours(4)) == TenshoAssistant.FreeCompany.VoyageState.Returned, "returned after it");
        Check(TenshoAssistant.FreeCompany.Voyages.StateOf(sub with { ReturnAt = null }, now) == TenshoAssistant.FreeCompany.VoyageState.Idle, "no return time: at the dock");
        Check(Math.Abs(TenshoAssistant.FreeCompany.Voyages.ExpProgress(sub) - 0.25) < 1e-9 && TenshoAssistant.FreeCompany.Voyages.ExpProgress(sub with { NextExp = 0 }) == 1, "experience share");
        var other = sub with { HouseId = 9, Name = "Other" };
        var merged = TenshoAssistant.FreeCompany.Voyages.Merge([sub, other], 7, [sub with { Name = "B", Slot = 2 }, sub with { Kind = TenshoAssistant.FreeCompany.VesselKind.Airship, Slot = 1, Name = "C" }]);
        Check(merged.Count == 3 && merged.Single(vessel => vessel.HouseId == 9).Name == "Other" && !merged.Any(vessel => vessel.Name == "A"), "a read replaces only its house");
        Check(merged[0].Kind == TenshoAssistant.FreeCompany.VesselKind.Submarine && merged[1].Kind == TenshoAssistant.FreeCompany.VesselKind.Airship, "sorted by house then kind");
        Check(TenshoAssistant.FreeCompany.Voyages.SameAs([sub], [sub with { ReadAt = now, Route = ["X", "Y"] }]) && !TenshoAssistant.FreeCompany.Voyages.SameAs([sub], [sub with { Route = ["X"] }]), "read time ignored, values compared");
        Check(TenshoAssistant.FreeCompany.Voyages.NextReturn([sub, other with { ReturnAt = now.AddHours(1) }, sub with { ReturnAt = now.AddHours(-1) }], now) == now.AddHours(1), "next return still to come");
    }),
    ("Submarine route planner: distance, time, best order, range and loot value (R-07)", () =>
    {
        // Start at the origin; A 1000 units east, B 2000 east, C 1000 north (lengths × 0.035 = 35 / 70 distance).
        TenshoAssistant.FreeCompany.SubmarineSector Sector(uint id, float x, float y, bool start = false, int rank = 1, int survey = 0) =>
            new(id, 1, start, rank, new System.Numerics.Vector3(x, y, 0), survey, 60, start ? 0u : 100u, $"S{id}");
        var start = Sector(1, 0, 0, true);
        var a = Sector(2, 1000, 0, survey: 5);
        var b = Sector(3, 2000, 0, survey: 5);
        var c = Sector(4, 0, 1000, rank: 50, survey: 5);
        Check(TenshoAssistant.FreeCompany.VoyagePlanner.LegDistance(start, a) == 35, "leg distance = floor(length × 0.035)");
        Check(TenshoAssistant.FreeCompany.VoyagePlanner.LegSeconds(start, a, 100) == (int)Math.Floor(1000 * 3990 / 10000.0 * 60), "leg time follows speed");
        Check(TenshoAssistant.FreeCompany.VoyagePlanner.SurveySeconds(a, 100) == (int)Math.Floor(60 * 7000 / 10000.0 * 60), "survey time follows speed");
        var (distance, seconds) = TenshoAssistant.FreeCompany.VoyagePlanner.Measure(start, [a], 100);
        Check(distance == 40 && seconds == TenshoAssistant.FreeCompany.VoyagePlanner.FixedSeconds + 23940 + 2520, "one sector: leg + survey + 12 h");
        var best = TenshoAssistant.FreeCompany.VoyagePlanner.BestOrder(start, [b, a], 100);
        Check(best.Order[0].Id == 2 && best.Order[1].Id == 3 && best.Distance == 35 + 5 + 35 + 5, "shortest order is A then B");
        var values = new Dictionary<uint, double> { [2] = 100, [3] = 300, [4] = 1000 };
        var routes = TenshoAssistant.FreeCompany.VoyagePlanner.Plan([start, a, b, c], 10, 100, 80, TenshoAssistant.FreeCompany.RouteGoal.ValuePerVoyage, id => values[id], 10);
        Check(routes.Count == 3 && routes[0].Value == 400 && routes[0].Sectors.SequenceEqual(new uint[] { 2, 3 }), "best value within range; rank 50 sector left out");
        Check(TenshoAssistant.FreeCompany.VoyagePlanner.Plan([start, a, b], 10, 100, 39, TenshoAssistant.FreeCompany.RouteGoal.ValuePerVoyage, id => values[id], 10).Count == 0, "nothing over the range");
        var loot = new TenshoAssistant.FreeCompany.SectorLoot(2, 10, [(5u, 30L), (6u, 5L)]);
        Check(Math.Abs(TenshoAssistant.FreeCompany.VoyagePlanner.ExpectedValue(loot, item => item == 5 ? 10 : 100) - (3 * 10 + 0.5 * 100)) < 1e-9, "value = quantity per visit × price");
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{\"Source\":\"s\",\"Built\":\"d\",\"Sectors\":[{\"Id\":2,\"Records\":10,\"Items\":[[5,30]]}]}"));
        var read = TenshoAssistant.FreeCompany.VoyagePlanner.ReadLoot(stream);
        Check(read.Loot[2].Records == 10 && read.Loot[2].Items[0] == (5u, 30L) && read.Source == "s", "embedded loot format");
        var embedded = Path.GetFullPath("src/TenshoAssistant/FreeCompany/Data/submarine-loot.json");
        using var file = File.OpenRead(embedded);
        Check(TenshoAssistant.FreeCompany.VoyagePlanner.ReadLoot(file).Loot.Count > 100, "embedded community loot readable");
    }),
    ("Gear planner: level window, candidates, totals, planned stats and build codes (R-11)", () =>
    {
        TenshoAssistant.Gear.GearItem Gear(uint id, int level, int ilvl, TenshoAssistant.Gear.GearSlot slot, ulong jobs, params (int, int)[] stats) =>
            new(id, $"I{id}", 0, level, ilvl, slot, false, jobs, stats.ToDictionary(pair => pair.Item1, pair => pair.Item2), new Dictionary<int, int> { [27] = 5 }, 2, true, false);
        var paladin = 1UL << 19;
        var catalog = new TenshoAssistant.Gear.GearCatalog([
            Gear(1, 83, 500, TenshoAssistant.Gear.GearSlot.Head, paladin, (1, 100), (27, 50)),
            Gear(2, 80, 480, TenshoAssistant.Gear.GearSlot.Head, paladin, (1, 90)),
            Gear(3, 83, 510, TenshoAssistant.Gear.GearSlot.Head, 1UL << 24, (5, 100)),
            Gear(4, 90, 560, TenshoAssistant.Gear.GearSlot.RingRight, paladin, (1, 40)),
            Gear(5, 88, 550, TenshoAssistant.Gear.GearSlot.RingRight, paladin, (1, 30))]);
        var exact = new TenshoAssistant.Gear.GearFilter(5, true);
        Check(TenshoAssistant.Gear.GearRules.LevelWindow(83, exact) == (83, 83) && TenshoAssistant.Gear.GearRules.LevelWindow(90, exact) == (85, 90), "exact level outside caps, window at caps");
        Check(TenshoAssistant.Gear.GearRules.LevelWindow(83, exact with { ExactOutsideCaps = false }) == (78, 83), "window when the option is off");
        Check(TenshoAssistant.Gear.GearRules.Candidates(catalog, TenshoAssistant.Gear.GearSlot.Head, 19, 83, exact).Select(item => item.Id).SequenceEqual(new uint[] { 1 }), "only the job's gear of the exact level");
        Check(TenshoAssistant.Gear.GearRules.Candidates(catalog, TenshoAssistant.Gear.GearSlot.Head, 19, 83, exact with { AllJobs = true }).Select(item => item.Id).SequenceEqual(new uint[] { 3, 1, 2 }), "all jobs: other jobs' armor of any level (glamour since 7.4)");
        Check(TenshoAssistant.Gear.GearRules.GlamourOnly(catalog.Find(3)!, 19, 83) && TenshoAssistant.Gear.GearRules.GlamourOnly(catalog.Find(4)!, 19, 83) && !TenshoAssistant.Gear.GearRules.GlamourOnly(catalog.Find(1)!, 19, 83), "other job or higher level: look only");
        var weapons = new TenshoAssistant.Gear.GearCatalog([Gear(9, 83, 500, TenshoAssistant.Gear.GearSlot.MainHand, 1UL << 24)]);
        Check(!TenshoAssistant.Gear.GearRules.Candidates(weapons, TenshoAssistant.Gear.GearSlot.MainHand, 19, 83, exact with { AllJobs = true }).Any(), "weapons stay job-locked");
        Check(TenshoAssistant.Gear.GearRules.Candidates(catalog, TenshoAssistant.Gear.GearSlot.RingLeft, 19, 90, exact).Select(item => item.Id).SequenceEqual(new uint[] { 4, 5 }), "rings fit both ring slots, best item level first");
        var head = catalog.Find(1)!;
        Check(head.ParamsFor(true)[27] == 55 && head.ParamsFor(false)[27] == 50, "HQ adds its bonus");
        var worn = TenshoAssistant.Gear.GearRules.Totals([(catalog.Find(2)!, false, new List<(int, int)> { (27, 20) })]);
        var planned = TenshoAssistant.Gear.GearRules.Totals([(head, true, new List<(int, int)>())]);
        Check(worn[1] == 90 && worn[27] == 20 && planned[27] == 55, "totals include materia and HQ");
        var result = TenshoAssistant.Gear.GearRules.Planned(new Dictionary<string, int> { ["Strength"] = 1000, ["CriticalHit"] = 400, ["Mind"] = 50 }, worn, planned);
        Check(result["Strength"] == 1010 && result["CriticalHit"] == 435 && result["Mind"] == 50, "planned = real + plan - worn");
        var set = new Dictionary<TenshoAssistant.Gear.GearSlot, TenshoAssistant.Gear.GearItem> { [TenshoAssistant.Gear.GearSlot.Head] = head };
        Check(Math.Abs(TenshoAssistant.Gear.GearRules.AverageItemLevel(set) - 500 / 12.0) < 1e-9, "average item level over 12 slots");
        var code = TenshoAssistant.Gear.GearShare.Export(19, new Dictionary<TenshoAssistant.Gear.GearSlot, uint> { [TenshoAssistant.Gear.GearSlot.Head] = 1, [TenshoAssistant.Gear.GearSlot.RingLeft] = 4 });
        var back = TenshoAssistant.Gear.GearShare.Import("Build Tensho\n" + code + "\nmerci");
        Check(back is { Job: 19 } imported && imported.Plan[TenshoAssistant.Gear.GearSlot.Head] == 1 && imported.Plan[TenshoAssistant.Gear.GearSlot.RingLeft] == 4, "build code round trip inside a message");
        // Materia (0.312): capped per piece, overmeld grades, share codes with materia.
        var capped = new TenshoAssistant.Gear.GearItem(20, "C", 0, 100, 730, TenshoAssistant.Gear.GearSlot.Body, false, paladin,
            new Dictionary<int, int> { [27] = 374, [19] = 262 }, new Dictionary<int, int>(), 2, false, false, true, new Dictionary<int, int> { [27] = 374, [19] = 374, [44] = 374 });
        var melds = new List<(int, int)> { (27, 54), (19, 54), (19, 54), (44, 18) };
        var melded = TenshoAssistant.Gear.GearRules.PieceParams(capped, false, melds);
        Check(melded[27] == 374 && melded[19] == 370 && melded[44] == 18, "materia stop at the piece's cap");
        Check(TenshoAssistant.Gear.GearRules.Wasted(capped, false, melds, 27) == 54 && TenshoAssistant.Gear.GearRules.Wasted(capped, false, melds, 19) == 0, "what the cap wastes");
        Check(capped.SocketCount == 5 && !TenshoAssistant.Gear.GearRules.OvermeldAllowed(11, 1) && TenshoAssistant.Gear.GearRules.OvermeldAllowed(11, 0) && TenshoAssistant.Gear.GearRules.OvermeldAllowed(10, 3), "advanced melding: 5 sockets, grade XII only in the first overmeld socket (0.338)");
        var materiaCatalog = new TenshoAssistant.Gear.GearCatalog([], [new TenshoAssistant.Gear.MateriaKind(15, 27, [(10, 41759u, "Crit XI", 18), (11, 41772u, "Crit XII", 54)])]);
        Check(materiaCatalog.MateriaValues([new(15, 11), new(15, 10), new(99, 1)]).SequenceEqual(new (int, int)[] { (27, 54), (27, 18) }), "materia values from the sheet, unknown rows ignored");
        var meldCode = TenshoAssistant.Gear.GearShare.Export(19, new Dictionary<TenshoAssistant.Gear.GearSlot, uint> { [TenshoAssistant.Gear.GearSlot.Body] = 20 },
            new Dictionary<TenshoAssistant.Gear.GearSlot, IReadOnlyList<TenshoAssistant.Gear.MeldedMateria>> { [TenshoAssistant.Gear.GearSlot.Body] = [new(15, 11), new(15, 10)] });
        Check(TenshoAssistant.Gear.GearShare.Import(meldCode) is { } withMateria && withMateria.Materia[TenshoAssistant.Gear.GearSlot.Body].SequenceEqual(new TenshoAssistant.Gear.MeldedMateria[] { new(15, 11), new(15, 10) }), "build code carries the materia");
        Check(TenshoAssistant.Gear.GearShare.Import("hello") is null && TenshoAssistant.Gear.GearShare.Import(TenshoAssistant.Gear.GearShare.Prefix + "!!!") is null, "no or broken code: nothing imported");
    }),
    ("Vessel builds: stats, capacity, ranks, breakpoints, wear, build names and best builds (R-11 step 3)", () =>
    {
        // Real SubmarinePart rows (docs/evidence/0.315-vessel-builds.md): Shark 1-4, Unkiu 5-8, Modified Shark 21-24 (slots: 0 hull, 1 stern, 2 bow, 3 bridge).
        TenshoAssistant.FreeCompany.VesselPart Part(uint row, int cls, int slot, int rank, int components, int repair, int s, int r, int sp, int rg, int f) =>
            new(row, TenshoAssistant.FreeCompany.VesselKind.Submarine, cls, slot, rank, components, repair, new(s, r, sp, rg, f), 21700 + row, $"P{row}");
        var parts = new[]
        {
            Part(1, 3, 2, 1, 5, 1, 50, 40, 10, -20, 15), Part(2, 3, 3, 1, 5, 1, 20, 20, 20, 20, 20), Part(3, 3, 0, 1, 5, 1, -10, 30, 20, 40, 20), Part(4, 3, 1, 1, 5, 1, -30, 20, 60, 30, 15),
            Part(5, 2, 2, 15, 9, 2, 60, 20, 20, -15, 10), Part(6, 2, 3, 15, 9, 2, 25, 5, 25, 30, 30), Part(7, 2, 0, 15, 9, 2, 15, 10, 0, 60, 15), Part(8, 2, 1, 15, 9, 2, 15, 0, 30, 40, 25),
            Part(21, 8, 2, 50, 20, 6, 55, 50, 15, -15, 25), Part(22, 8, 3, 50, 20, 6, 25, 25, 30, 25, 35), Part(23, 8, 0, 50, 20, 6, -5, 40, 25, 45, 35), Part(24, 8, 1, 50, 20, 6, -25, 25, 70, 35, 25),
        };
        var catalog = new TenshoAssistant.FreeCompany.VesselPartCatalog
        {
            Parts = parts.ToDictionary(part => (part.Kind, part.Row)),
            SubmarineRanks = [new(1, 20, 6180, default), new(15, 34, 100, default), new(51, 80, 551237, new(1, 2, 3, 4, 5)), new(145, 80, 46234004, new(110, 140, 100, 130, 105)), new(146, 0, 0, default)],
            AirshipRanks = [new(1, 12, 6180, default)],
            Breakpoints = new Dictionary<uint, TenshoAssistant.FreeCompany.SectorBreakpoint> { [10] = new(10, 50, 115, 80, 110, 90), [15] = new(15, 80, 115, 120, 160, 90) },
            SubmarineStatNames = ["a", "b", "c", "d", "e"], AirshipStatNames = ["a", "b", "c", "d", "e"],
        };
        var shark = new TenshoAssistant.FreeCompany.VesselBuild(TenshoAssistant.FreeCompany.VesselKind.Submarine, 1, 3, 4, 1, 2);
        var summary = TenshoAssistant.FreeCompany.VesselBuilds.Summarize(catalog, shark);
        Check(summary.Stats == new TenshoAssistant.FreeCompany.VesselStats(30, 110, 110, 70, 70) && summary.Cost == 20 && summary.Capacity == 20 && !summary.OverCapacity && summary.Repair == 4, "parts add up, cost and capacity");
        var unkiu = new TenshoAssistant.FreeCompany.VesselBuild(TenshoAssistant.FreeCompany.VesselKind.Submarine, 15, 7, 8, 5, 6);
        var full = TenshoAssistant.FreeCompany.VesselBuilds.Summarize(catalog, unkiu);
        Check(full.Cost == 36 && full.Capacity == 34 && full.OverCapacity && !full.RankTooLow, "a full Unkiu set is over capacity at rank 15");
        Check(TenshoAssistant.FreeCompany.VesselBuilds.Summarize(catalog, unkiu with { Rank = 1 }).RankTooLow, "part rank above the vessel rank");
        var high = TenshoAssistant.FreeCompany.VesselBuilds.Summarize(catalog, shark with { Rank = 145 });
        Check(high.Stats == new TenshoAssistant.FreeCompany.VesselStats(140, 250, 210, 200, 175) && TenshoAssistant.FreeCompany.VesselBuilds.MaxRank(catalog.SubmarineRanks) == 145, "rank bonus added; last rank with a capacity is the cap");
        Check(TenshoAssistant.FreeCompany.VesselBuilds.RankRow(catalog.SubmarineRanks, 20)!.Rank == 15 && TenshoAssistant.FreeCompany.VesselBuilds.RankRow(catalog.SubmarineRanks, 200)!.Rank == 145, "rank row in force, no extrapolation past the cap");
        var route = TenshoAssistant.FreeCompany.VesselBuilds.RouteBreakpoint(catalog.Breakpoints, [10, 15]);
        Check(route == new TenshoAssistant.FreeCompany.SectorBreakpoint(0, 80, 115, 120, 160, 90), "route breakpoint = highest of each column");
        Check(TenshoAssistant.FreeCompany.VesselBuilds.RouteBreakpoint(catalog.Breakpoints, [10, 99]) is null, "an unknown sector makes the route unknown, not zero");
        Check(TenshoAssistant.FreeCompany.VesselBuilds.Surveillance(100, route) == TenshoAssistant.FreeCompany.SurveillanceTier.Mid
            && TenshoAssistant.FreeCompany.VesselBuilds.Retrieval(160, route) == TenshoAssistant.FreeCompany.RetrievalTier.Optimal
            && TenshoAssistant.FreeCompany.VesselBuilds.FavorReached(89, route) == false
            && TenshoAssistant.FreeCompany.VesselBuilds.Surveillance(100, null) == TenshoAssistant.FreeCompany.SurveillanceTier.Unknown, "tiers reached");
        var damage = TenshoAssistant.FreeCompany.VesselBuilds.DamagePerVoyage(catalog, shark, [50, 50]);
        Check(damage == (335 + 50 - 1) * 7 * 2 && TenshoAssistant.FreeCompany.VesselBuilds.VoyagesUntilRepair(damage) == 6 && TenshoAssistant.FreeCompany.VesselBuilds.VoyagesUntilRepair(0) is null, "community wear estimate");
        Check(TenshoAssistant.FreeCompany.VesselBuilds.Identifier(catalog, shark) == "SSSS"
            && TenshoAssistant.FreeCompany.VesselBuilds.Identifier(catalog, new(TenshoAssistant.FreeCompany.VesselKind.Submarine, 50, 23, 24, 21, 22)) == "SSSS++"
            && TenshoAssistant.FreeCompany.VesselBuilds.Identifier(catalog, new(TenshoAssistant.FreeCompany.VesselKind.Submarine, 50, 23, 4, 5, 2)) == "S+SUS", "community build names");
        var best = TenshoAssistant.FreeCompany.VesselBuilds.BestBuilds(catalog, 50, 60, route, TenshoAssistant.FreeCompany.SurveillanceTier.Mid, TenshoAssistant.FreeCompany.RetrievalTier.Unknown, false, speed => 100000 / speed, [50], 5);
        Check(best.Count > 0 && best.All(candidate => candidate.Summary.Stats.Range >= 60 && candidate.Summary.Stats.Surveillance >= 80 && !candidate.Summary.OverCapacity), "best builds meet range, tier and capacity");
        Check(best.Zip(best.Skip(1)).All(pair => pair.First.Seconds <= pair.Second.Seconds), "fastest first");
        Check(TenshoAssistant.FreeCompany.VesselBuild.FromList(TenshoAssistant.FreeCompany.VesselKind.Submarine, shark.ToList()) == shark && TenshoAssistant.FreeCompany.VesselBuild.FromList(TenshoAssistant.FreeCompany.VesselKind.Submarine, [1u, 2u]) is null, "plan round trip in the configuration");
        // 0.315 review fixes.
        var known = new Dictionary<string, List<uint>> { ["7"] = [1u, 2u], ["9"] = [1u, 2u, 3u] };
        Check(TenshoAssistant.FreeCompany.Voyages.UnlockedFor(known, 7)!.Count == 2 && TenshoAssistant.FreeCompany.Voyages.UnlockedFor(known, 8) is null
            && TenshoAssistant.FreeCompany.Voyages.UnlockedFor(known, null) is null && TenshoAssistant.FreeCompany.Voyages.UnlockedFor(new Dictionary<string, List<uint>> { ["7"] = [1u] }, null)!.Count == 1, "unlocked sectors per company, unknown company = no filter");
        Check(TenshoAssistant.FreeCompany.VesselBuilds.EffectivePlan(shark with { Rank = 90 }, 95)!.Rank == 95 && TenshoAssistant.FreeCompany.VesselBuilds.EffectivePlan(shark with { Rank = 100 }, 95)!.Rank == 100, "plan rank follows the vessel, a higher planned rank is kept");
        Check(TenshoAssistant.FreeCompany.VesselBuilds.BestBuilds(catalog, 40, 60, route, TenshoAssistant.FreeCompany.SurveillanceTier.Unknown, TenshoAssistant.FreeCompany.RetrievalTier.Unknown, false, speed => 1, [50], 5).Count == 0
            && TenshoAssistant.FreeCompany.VesselBuilds.RouteRank([10, 50, 30]) == 50, "no build for a route above the rank");
        using var embedded = new StreamReader(Path.GetFullPath("src/TenshoAssistant/FreeCompany/Data/submarine-breakpoints.csv"));
        var table = TenshoAssistant.FreeCompany.VesselBuilds.ReadBreakpoints(embedded);
        Check(table.Count == 143 && table[1] == new TenshoAssistant.FreeCompany.SectorBreakpoint(1, 20, 80, 50, 80, 70) && !table.ContainsKey(31), "embedded breakpoints: 143 sectors, map starts excluded");
    }),
    ("Every setting tooltip named in the code exists in English and French (R-06 step 5)", () =>
    {
        // Tip("Key"), BoolSetting / IntSetting / SliderSetting(label, "TipKey", …), map SettingBox("Key" → "KeyHelp") and AreaSetting("Key" → "KeyHelp").
        // X-76 H1 (0.913): also every whole key inside Tip(…) (cond ? "A":"B") and HelpMarker("Key"); a part joined with « + » is not a key.
        var root = Path.GetFullPath("src/TenshoAssistant");
        var code = string.Join("\n", Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).Where(file => !file.Contains("\\obj\\") && !file.Contains("/obj/")).Select(File.ReadAllText));
        var keys = new HashSet<string>();
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(code, "\\bTip\\(\"([A-Za-z0-9_.]+)\"\\)")) keys.Add(match.Groups[1].Value);
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(code, "(?:BoolSetting|IntSetting|SliderSetting)\\(\"[A-Za-z0-9_.]+\",\"([A-Za-z0-9_.]+)\"")) keys.Add(match.Groups[1].Value);
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(code, "\\b(?:Tip|HelpMarker)\\(([^;\\n]*)\\);"))
            foreach (System.Text.RegularExpressions.Match literal in System.Text.RegularExpressions.Regex.Matches(match.Groups[1].Value, "(?<!\\+\\s*)\"([A-Za-z0-9_]+\\.[A-Za-z0-9_.]*[A-Za-z0-9_])\"(?!\\s*\\+)")) keys.Add(literal.Groups[1].Value);
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(code, "(?:SettingBox|AreaSetting)\\(\"([A-Za-z0-9_.]+)\"")) keys.Add(match.Groups[1].Value + "Help");
        Check(keys.Count > 40, $"setting tooltips found in the code ({keys.Count})");
        foreach (var language in new[] { "en", "fr" })
        {
            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Localization", "Languages", language + ".json")));
            var missing = keys.Where(key => { var node = document.RootElement; foreach (var part in key.Split('.')) { if (node.ValueKind != System.Text.Json.JsonValueKind.Object || !node.TryGetProperty(part, out node)) return true; } return node.ValueKind != System.Text.Json.JsonValueKind.String || node.GetString()!.Length == 0; }).ToArray();
            Check(missing.Length == 0, $"{language}.json misses setting tooltips: {string.Join(", ", missing)}");
        }
    }),
    ("French texts address the player with « vous » (decision 2026-11-02)", () =>
    {
        var path = Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json");
        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        var informal = new List<string>();
        void Walk(System.Text.Json.JsonElement node, string key)
        {
            if (node.ValueKind == System.Text.Json.JsonValueKind.Object) { foreach (var child in node.EnumerateObject()) Walk(child.Value, key.Length == 0 ? child.Name : key + "." + child.Name); }
            else if (node.ValueKind == System.Text.Json.JsonValueKind.String && System.Text.RegularExpressions.Regex.IsMatch(node.GetString()!, "\\b([Tt]u|[Tt]on|[Tt]a|[Tt]es|[Tt]oi)\\b")) informal.Add(key);
        }
        Walk(document.RootElement, "");
        Check(informal.Count == 0, "fr.json uses « tu » in: " + string.Join(", ", informal));
    }),
    ("Options not taken (locks, branches) are unplayable, with propagation, and completed quests never are", () =>
    {
        var done = TenshoAssistant.Quests.QuestProgress.Complete;
        var open = TenshoAssistant.Quests.QuestProgress.Unconfirmed;
        TenshoAssistant.Quests.QuestEntry Quest(uint id, TenshoAssistant.Quests.QuestProgress progress, byte join = 1, uint[]? previous = null, uint[]? locks = null)
            => new(id, $"Q{id}", "Genre", 0, progress, null) { PreviousQuestJoin = join, PreviousQuestIds = previous ?? [], LockIds = locks ?? [] };
        // Grand Company choice: 11, 12, 13 lock each other; 12 done. 21 follows 11 only, 23 follows 13 only; 30 needs any one of 21 / 22 / 23.
        var entries = new[]
        {
            Quest(10, done),
            Quest(11, open, previous: [10], locks: [12, 13]), Quest(12, done, previous: [10], locks: [11, 13]), Quest(13, open, previous: [10], locks: [11, 12]),
            Quest(21, open, previous: [11]), Quest(22, done, previous: [12]), Quest(23, open, previous: [13]),
            Quest(30, open, join: 2, previous: [21, 22, 23]),
            // A branch with an untaken option leading to one more quest: 41 and 42 are options of 50; 42 done, 40 comes before 41 only.
            Quest(40, open), Quest(41, open, previous: [40]), Quest(42, done), Quest(50, open, join: 2, previous: [41, 42]),
            Quest(60, open, previous: [30]),
        };
        var closed = TenshoAssistant.Quests.QuestAlternatives.NotPlayable(entries);
        Check(closed.SetEquals(new uint[] { 11, 13, 21, 23, 40, 41 }), "locked GCs, their follow-ups, untaken branch and the quest only leading to it: " + string.Join(",", closed.OrderBy(id => id)));
        Check(!closed.Contains(30) && !closed.Contains(50) && !closed.Contains(60), "the merge quests and what follows stay playable");
        Check(TenshoAssistant.Quests.QuestAlternatives.NotPlayable(entries.Select(entry => entry with { Progress = open }).ToArray()).Count == 0, "nothing chosen yet: nothing closed");
    }),
    ("Triad card score follows the stated formula for each rule", () =>
    {
        // Sides 9 / 1 / 7 / 8, typed.
        var card = new TenshoAssistant.MiniGames.TriadCardDefinition(1, 306, 4, 9, 1, 7, 8, 3, "Type", "");
        var rules = TenshoAssistant.MiniGames.TriadRules.Normal;
        Check(TenshoAssistant.MiniGames.TriadCardScore.Score(card, rules) == 25 + 2 * 1, "sum + 2 × weakest");
        Check(TenshoAssistant.MiniGames.TriadCardScore.Score(card, TenshoAssistant.MiniGames.TriadRules.Reverse) == (2 + 10 + 4 + 3) + 2 * 2, "reverse: 11 − value");
        Check(TenshoAssistant.MiniGames.TriadCardScore.Score(card, TenshoAssistant.MiniGames.TriadRules.FallenAce) == (9 + 6 + 7 + 8) + 2 * 6, "fallen ace: a 1 counts as 6");
        var pairs = new TenshoAssistant.MiniGames.TriadCardDefinition(2, 1, 1, 5, 5, 5, 2, 0, "", "");
        Check(TenshoAssistant.MiniGames.TriadCardScore.Score(pairs, TenshoAssistant.MiniGames.TriadRules.Same) == 17 + 2 * 2 + 3 * 2, "same: 2 per pair of equal sides (3 pairs of 5)");
        Check(TenshoAssistant.MiniGames.TriadCardScore.Score(card, TenshoAssistant.MiniGames.TriadRules.Ascension) == 27 + 4
              && TenshoAssistant.MiniGames.TriadCardScore.Score(card, TenshoAssistant.MiniGames.TriadRules.Descension) == 27 - 4
              && TenshoAssistant.MiniGames.TriadCardScore.Score(pairs, TenshoAssistant.MiniGames.TriadRules.Descension) == 17 + 4, "typed cards: ascension +4, descension −4, untyped unchanged");
        Check(TenshoAssistant.MiniGames.TriadCardScore.Key(card, TenshoAssistant.MiniGames.TriadCardSort.Total, rules) == 25
              && TenshoAssistant.MiniGames.TriadCardScore.Key(card, TenshoAssistant.MiniGames.TriadCardSort.Weakest, rules) == 1
              && TenshoAssistant.MiniGames.TriadCardScore.Key(card, TenshoAssistant.MiniGames.TriadCardSort.Left, rules) == 8, "sort keys");
        var ex = card with { Extra = true, Number = 1 };
        Check(ex.Label == "Ex. 1" && card.Label == "306"
              && TenshoAssistant.MiniGames.TriadCardScore.Key(ex, TenshoAssistant.MiniGames.TriadCardSort.Number, rules) > TenshoAssistant.MiniGames.TriadCardScore.Key(card with { Number = 460 }, TenshoAssistant.MiniGames.TriadCardSort.Number, rules),
              "Ex. cards print « Ex. n » and sort after every ordinary card (docs/evidence/0.278-triad-ex-cards.md)");
    }),
};

var failures = 0;
tests = tests.Concat(DutyLootTests.Cases).ToArray();
tests = tests.Concat(MapZoom542Tests.Cases).ToArray();
tests = tests.Concat(CraftThresholds553Tests.Cases).ToArray();
tests = tests.Concat(CraftFollow589Tests.Cases).ToArray();
tests = tests.Concat(CraftFromStockTests.Cases).ToArray();
tests = tests.Concat(GearPlanSets593Tests.Cases).ToArray();
tests = tests.Concat(BankList594Tests.Cases).ToArray();
tests = tests.Concat(SortGear595Tests.Cases).ToArray();
tests = tests.Concat(MapLabels596Tests.Cases).ToArray();
tests = tests.Concat(MapSearch597Tests.Cases).ToArray();
tests = tests.Concat(HomeToday599Tests.Cases).ToArray();
tests = tests.Concat(RecipeFacts600Tests.Cases).ToArray();
tests = tests.Concat(ReadFailed601Tests.Cases).ToArray();
tests = tests.Concat(CollectionRecent602Tests.Cases).ToArray();
tests = tests.Concat(Workshops603Tests.Cases).ToArray();
tests = tests.Concat(BankSave605Tests.Cases).ToArray();
tests = tests.Concat(WeeklyCap606Tests.Cases).ToArray();
tests = tests.Concat(AnnotatedItems607Tests.Cases).ToArray();
tests = tests.Concat(OtherStocks609Tests.Cases).ToArray();
tests = tests.Concat(CompetitiveCut610Tests.Cases).ToArray();
tests = tests.Concat(MapAside617Tests.Cases).ToArray();
tests = tests.Concat(MapResumeTests.Cases).ToArray();
tests = tests.Concat(SelfCheckTests.Cases).ToArray();
tests = tests.Concat(HelpTests.Cases).ToArray();
tests = tests.Concat(RetainerSalesTests.Cases).ToArray();
tests = tests.Concat(GuideFlowTests.Cases).ToArray();
tests = tests.Concat(PageCoverageTests.Cases).ToArray();
tests = tests.Concat(FaqTests.Cases).ToArray();
tests = tests.Concat(PublicDocsTests.Cases).ToArray();
tests = tests.Concat(ReleaseScriptTests.Cases).ToArray();
tests = tests.Concat(DocsGen1018Tests.Cases).ToArray();
tests = tests.Concat(BeastCatalogTests.Cases).ToArray();
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
var allTests = tests.Concat(HttpDeadline1123Tests.Cases).Concat(CacheClear1122Tests.Cases).Concat(DiscoveriesSave1121Tests.Cases).Concat(SettingsRead1120Tests.Cases).Concat(LinkCategories1119Tests.Cases).Concat(BankShared1118Tests.Cases).Concat(DesktopInstall1117Tests.Cases).Concat(LabyrinthValues1116Tests.Cases).Concat(LabyrinthOpen1115Tests.Cases).Concat(LinkLive1114Tests.Cases).Concat(DesktopInstall1113Tests.Cases).Concat(VerifyDocuments1112Tests.Cases).Concat(MapWarmup1111Tests.Cases).Concat(GardenCapture1110Tests.Cases).Concat(GardenForecast1109Tests.Cases).Concat(LatePlan1108Tests.Cases).Concat(MapLockedQuests1107Tests.Cases).Concat(WishlistTool1106Tests.Cases).Concat(LaunchCheck1105Tests.Cases).Concat(MapWarmup1104Tests.Cases).Concat(MapVendorIndex1103Tests.Cases).Concat(GainParsers1102Tests.Cases).Concat(CommunityMerge1101Tests.Cases).Concat(TestingTriage1100Tests.Cases).Concat(StaleMapRequest1098Tests.Cases).Concat(MacroSound1097Tests.Cases).Concat(GainSources1096Tests.Cases).Concat(VoyagePoints1095Tests.Cases).Concat(VoyageReload1094Tests.Cases).Concat(ChangelogSplit1093Tests.Cases).Concat(PlanCompact1092Tests.Cases).Concat(ReadmeShort1091Tests.Cases).Concat(NoD17Draft1090Tests.Cases).Concat(LocalizationGuide1089Tests.Cases).Concat(RequestsOnce1088Tests.Cases).Concat(CompetitiveFolded1087Tests.Cases).Concat(AuditsFolder1086Tests.Cases).Concat(EvidenceCited1085Tests.Cases).Concat(VersionStamps1084Tests.Cases).Concat(IdRegistry1083Tests.Cases).Concat(AgentFilesDec1082Tests.Cases).Concat(LocalLink1081Tests.Cases).Concat(TrackedLinks1080Tests.Cases).Concat(TestingHeading1079Tests.Cases).Concat(ChangelogHeadings1078Tests.Cases).Concat(CodingIdeas1077Tests.Cases).Concat(ReferencesSwitches1076Tests.Cases).Concat(TestingInventory1075Tests.Cases).Concat(StepH1074Tests.Cases).Concat(TestingArchive1073Tests.Cases).Concat(PhaseOrder1072Tests.Cases).Concat(Readings1071Tests.Cases).Concat(ChangelogVoice1070Tests.Cases).Concat(Evidence1069Tests.Cases).Concat(TriageFrozen1067Tests.Cases).Concat(PageLists1066Tests.Cases).Concat(ReferencesArchive1065Tests.Cases).Concat(ProjectLinks1063Tests.Cases).Concat(DecisionsLinks1062Tests.Cases).Concat(ChangelogVoice1061Tests.Cases).Concat(ReferencesNetwork1060Tests.Cases).Concat(ProposalsShape1059Tests.Cases).Concat(ContentToolsAnswers1058Tests.Cases).Concat(ChangelogHeader1057Tests.Cases).Concat(ChangelogLanguages1056Tests.Cases).Concat(NoSubmission1055Tests.Cases).Concat(DecisionsSuperseded1054Tests.Cases).Concat(FutureDates1053Tests.Cases).Concat(PublishDrafts1052Tests.Cases).Concat(AgentPointers1051Tests.Cases).Concat(JitWarmup1050Tests.Cases).Concat(MapWeather1049Tests.Cases).Concat(PatchRecheck1048Tests.Cases).Concat(PatchShow1047Tests.Cases).Concat(PatchChanges1046Tests.Cases).Concat(GuideVisit1045Tests.Cases).Concat(SourceUses1044Tests.Cases).Concat(DeleteConfirm1043Tests.Cases).Concat(GameAlarmDiscord1042Tests.Cases).Concat(DiscoveriesSlices1040Tests.Cases).Concat(DiscoveriesBase1039Tests.Cases).Concat(FontGlyphs1038Tests.Cases).Concat(LoadTest1036Tests.Cases).Concat(PinnedHost1035Tests.Cases).Concat(LineEndings1034Tests.Cases).Concat(DesktopUpdate1033Tests.Cases).Concat(PluginUiTest1032Tests.Cases).Concat(DesktopApp1031Tests.Cases).Concat(ReleaseGuards1020Tests.Cases).Concat(DiscoveriesPrivacy1012Tests.Cases).Concat(GameUpdateInventory1008Tests.Cases).Concat(SubmarineProbe1006Tests.Cases).Concat(QuestAskedFix1005Tests.Cases).Concat(WishSales1000Tests.Cases).Concat(SettingsTabs999Tests.Cases).Concat(Navigation998Tests.Cases).Concat(NightReview997Tests.Cases).Concat(DutyNotes995Tests.Cases).Concat(DutyGuide994Tests.Cases).Concat(JobFilter991Tests.Cases).Concat(GilJournalTests.Cases).Concat(BeastmasterTests.Cases).Concat(LabyrinthTests.Cases).Concat(UnacceptedQuests977Tests.Cases).Concat(GameData972Tests.Cases).Concat(ParsersCheck971Tests.Cases).Concat(Expansion970Tests.Cases).Concat(SortGroups969Tests.Cases).Concat(AreaArrows967Tests.Cases).Concat(BiteSlots965Tests.Cases).Concat(SpectralBefore962Tests.Cases).Concat(IndividualWeather959Tests.Cases).Concat(DrawCost956Tests.Cases).Concat(DrawCost955Tests.Cases).Concat(SettingOverlaps954Tests.Cases).Concat(MapSettingGroups953Tests.Cases).Concat(SettingGroups952Tests.Cases).Concat(NetworkRules949Tests.Cases).Concat(DefaultLines947Tests.Cases).Concat(GameOnlyBackup945Tests.Cases).Concat(SettingsFixes944Tests.Cases).Concat(SharedDiscoveriesTests.Cases).Concat(NeverForget939Tests.Cases).Concat(DiscoveryIndex938Tests.Cases).Concat(Evidence937Tests.Cases).Concat(DropCounts935Tests.Cases).Concat(VoyageFacts934Tests.Cases).Concat(SRankCheck931Tests.Cases).Concat(VistaNumbers929Tests.Cases).Concat(Ventures928Tests.Cases).Concat(ItemUnlock927Tests.Cases).Concat(UndercutCut926Tests.Cases).Concat(BackgroundWriter925Tests.Cases).Concat(Registrations924Tests.Cases).Concat(AtomicFile923Tests.Cases).Concat(Logs922Tests.Cases).Concat(ItemDisplay921Tests.Cases).Concat(StatusLines920Tests.Cases).Concat(AirshipTests.Cases).Concat(DiscoveriesFormat916Tests.Cases).Concat(SettingRows915Tests.Cases).Concat(SettingsSave914Tests.Cases).Concat(Tooltips913Tests.Cases).Concat(ThemeColours912Tests.Cases).Concat(Parsers911Tests.Cases).Concat(NoFrench909Tests.Cases).Concat(SeenInGame908Tests.Cases).Concat(EorzeaTime906Tests.Cases).Concat(MapLayersAll902Tests.Cases).Concat(FashionIconMenu900Tests.Cases).Concat(MapViewTests.Cases).Concat(HandIn889Tests.Cases).Concat(Reduction888Tests.Cases).Concat(FishingWiringTests.Cases).Concat(MergeToolTests.Cases).Concat(GitHubExchange883Tests.Cases).Concat(CommunityDiscoveries882Tests.Cases).Concat(GameChecks880Tests.Cases).Concat(SeenPages879Tests.Cases).Concat(BlueMagicTests.Cases).Concat(ShopNpc872Tests.Cases).Concat(Regions871Tests.Cases).Concat(OwnOffers870Tests.Cases).Concat(ReadingCaptureTests.Cases).Concat(SeenBoard868Tests.Cases).Concat(SeenFirstTests.Cases).Concat(Seasonal864Tests.Cases).Concat(LearnedTamed863Tests.Cases).Concat(VoyageSectors862Tests.Cases).Concat(FashionGame861Tests.Cases).Concat(ItemSources860Tests.Cases).Concat(SalesMerge858Tests.Cases).Concat(DataCenterSeen857Tests.Cases).Concat(MoreKinds856Tests.Cases).Concat(Cells855Tests.Cases).Concat(SeenPlaces854Tests.Cases).Concat(NearestOnTarget853Tests.Cases).Concat(QuestRewards852Tests.Cases).Concat(QuestEvents848Tests.Cases).Concat(ChatItemMenu837Tests.Cases).Concat(DeveloperLogsTests.Cases).Concat(ZoneEntries833Tests.Cases).Concat(GatheringNode831Tests.Cases).Concat(Timers828Tests.Cases).Concat(AddToList826Tests.Cases).Concat(EventMarkers825Tests.Cases).Concat(ZoneCostIcons824Tests.Cases).Concat(Elsewhere822Tests.Cases).Concat(StorageHighlight820Tests.Cases).Concat(DropsByName819Tests.Cases).Concat(SeenPrices817Tests.Cases).Concat(CollectableHighlight815Tests.Cases).Concat(EdgeLayer814Tests.Cases).Concat(QuestTarget813Tests.Cases).Concat(RecipeMenu808Tests.Cases).Concat(MapDiscovery803Tests.Cases).Concat(NoAgentFiles802Tests.Cases).Concat(CollectionMonsters800Tests.Cases).Concat(FateMonsters797Tests.Cases).Concat(NearbyMonstersTests.Cases).Concat(CurrencySales794Tests.Cases).Concat(CraftStepsTests.Cases).Concat(WrongJob788Tests.Cases).Concat(WorkshopDelivered787Tests.Cases).Concat(FinishedOnly785Tests.Cases).Concat(NoHqLive784Tests.Cases).Concat(WikiPlaces777Tests.Cases).Concat(AlarmLinkShape771Tests.Cases).Concat(OpenStorage770Tests.Cases).Concat(RetainerExtras767Tests.Cases).Concat(FewerClicks763Tests.Cases).Concat(ZoneOrder757Tests.Cases).Concat(ListPanelsTests.Cases).Concat(TimedPointTests.Cases).Concat(AutoGather753Tests.Cases).Concat(GatheringTrackedTests.Cases).Concat(CacheDefaults750Tests.Cases).Concat(SlowFrameTests.Cases).Concat(RecipeStock735Tests.Cases).Concat(DeliveryRewards730Tests.Cases).Concat(GatherNext728Tests.Cases).Concat(SalesSpeed725Tests.Cases).Concat(PageSweep724Tests.Cases).Concat(Lexicon719Tests.Cases).Concat(DeepDungeons718Tests.Cases).Concat(FieldContentTests.Cases).Concat(SpecialDeliveries715Tests.Cases).Concat(GrandCompanies714Tests.Cases).Concat(AlliedSocieties713Tests.Cases).Concat(RelicTests.Cases).Concat(PlaceMenusTests.Cases).Concat(BaseMaterials699Tests.Cases).Concat(CurrencyValue695Tests.Cases).Concat(WorkshopSets694Tests.Cases).Concat(ReminderTabs687Tests.Cases).Concat(IslandWorkshops686Tests.Cases).Concat(IslandAnimalsTests.Cases).Concat(MaskedCarnivale683Tests.Cases).Concat(Island679Tests.Cases).Concat(WorkshopStock676Tests.Cases).Concat(CustomDeliveries675Tests.Cases).Concat(Island674Tests.Cases).Concat(Island673Tests.Cases).Concat(Island672Tests.Cases).Concat(Island671Tests.Cases).Concat(SRankConditions670Tests.Cases).Concat(GameWindowsTests.Cases).Concat(Folklore526Tests.Cases).Concat(PartyAndLoad525Tests.Cases).Concat(GpCrystals519Tests.Cases).Concat(GatherPointAccess518Tests.Cases).Concat(AttachedPanels516Tests.Cases).Concat(GatherNodeEffects515Tests.Cases).Concat(PanelWindows514Tests.Cases).Concat(ChainCostTests.Cases).Concat(PreparationCostTests.Cases).Concat(TriadObservationTests.Cases).Concat(TriadMatchAdvisorTests.Cases).Concat(TriadMatchSimulatorTests.Cases).Concat(CompletionMemoryTests.Cases).Concat(TriadNpcCatalogTests.Cases).Concat(StatFormulaTests.Cases).Concat(LuminaSupplementalTests.Cases).Concat(AlarmScheduleTests.Cases).Concat(DiscordWebhookTests.Cases).Concat(CreditCatalogTests.Cases).Concat(ReviewFixes321Tests.Cases).Concat(ReviewFixes322Tests.Cases).Concat(Tooling323Tests.Cases).Concat(SourceSwitch324Tests.Cases).Concat(TooltipRatchet325Tests.Cases).Concat(ReviewFixes326Tests.Cases).Concat(Map328Tests.Cases).Concat(Items329Tests.Cases).Concat(CraftGather330Tests.Cases).Concat(Tools331Tests.Cases).Concat(SquadronTests.Cases).Concat(Sectors334Tests.Cases).Concat(Routes335Tests.Cases).Concat(Overmeld338Tests.Cases).Concat(Barding340Tests.Cases).Concat(Chocobo341Tests.Cases).Concat(FestivalTests.Cases).Concat(Currencies344Tests.Cases).Concat(Characters345Tests.Cases).Concat(MarketServiceTests.Cases).Concat(ItemNotes347Tests.Cases).Concat(StorageReport348Tests.Cases).Concat(ExternalLinks349Tests.Cases).Concat(CompanyChest350Tests.Cases).Concat(ShopRequirements352Tests.Cases).Concat(ItemFilters353Tests.Cases).Concat(CurrencyValue354Tests.Cases).Concat(DutyProgressTests.Cases).Concat(Roulettes356Tests.Cases).Concat(Armoire357Tests.Cases).Concat(SameLook358Tests.Cases).Concat(CollectionSources359Tests.Cases).Concat(WishlistTests.Cases).Concat(VistaLog361Tests.Cases).Concat(Outfits362Tests.Cases).Concat(LevesTests.Cases).Concat(ExtraCollections365Tests.Cases).Concat(DutyPop366Tests.Cases).Concat(AchievementTests.Cases).Concat(Titles369Tests.Cases).Concat(OrchestrionPlayer370Tests.Cases).Concat(AutoAlarms371Tests.Cases).Concat(WeeklyCounters372Tests.Cases).Concat(ChatMapLinks373Tests.Cases).Concat(CharacterStatusTests.Cases).Concat(PersonalTasks375Tests.Cases).Concat(GearWear376Tests.Cases).Concat(GearCompare377Tests.Cases).Concat(VoyageHistory379Tests.Cases).Concat(StatTiers380Tests.Cases).Concat(FreeTasks381Tests.Cases).Concat(Counters382Tests.Cases).Concat(Gearsets383Tests.Cases).Concat(TreasureMap384Tests.Cases).Concat(RandomJob385Tests.Cases).Concat(WorldClocks386Tests.Cases).Concat(CollectionItems387Tests.Cases).Concat(SortButton388Tests.Cases).Concat(RegenTick391Tests.Cases).Concat(Fonts394Tests.Cases).Concat(FishWindows396Tests.Cases).Concat(GoThereTests.Cases).Concat(NotebooksTests.Cases).Concat(GpAlert403Tests.Cases).Concat(GameLogTests.Cases).Concat(Languages406Tests.Cases).Concat(CommunityNames407Tests.Cases).Concat(CraftPreparation408Tests.Cases).Concat(GpActions410Tests.Cases).Concat(GatheringGearset411Tests.Cases).Concat(AlarmLinks414Tests.Cases).Concat(Tracker415Tests.Cases).Concat(GatheringRoute416Tests.Cases).Concat(SpearfishingTests.Cases).Concat(OceanVoyageTests.Cases).Concat(Craft419Tests.Cases).Concat(CraftConsumables420Tests.Cases).Concat(RecipeLog421Tests.Cases).Concat(Teamcraft423Tests.Cases).Concat(CraftReports424Tests.Cases).Concat(CloseOnGameWindow426Tests.Cases).Concat(CraftReadiness427Tests.Cases).Concat(CraftProfit431Tests.Cases).Concat(CraftLists433Tests.Cases).Concat(CraftActions434Tests.Cases).Concat(CraftRecorder436Tests.Cases).Concat(PastedCoordinates437Tests.Cases).Concat(CraftRules438Tests.Cases).Concat(WondrousTails439Tests.Cases).Concat(BingoTasks443Tests.Cases).Concat(PlaceDirectory444Tests.Cases).Concat(Paissa445Tests.Cases).Concat(HuntTests.Cases).Concat(Decisions447Tests.Cases).Concat(GardenJournalTests.Cases).Concat(GardenCrossesTests.Cases).Concat(ResilienceTests.Cases).Concat(Campaign455Tests.Cases).Concat(Icons457Tests.Cases).Concat(Todo458Tests.Cases).Concat(Lot3Fixes462Tests.Cases).Concat(Storage463Tests.Cases).Concat(Craft464Tests.Cases).Concat(RaphaelTests.Cases).Concat(Craft468Tests.Cases).Concat(GcSupply469Tests.Cases).Concat(Pages470Tests.Cases).Concat(AlarmChatInDutiesTests.Cases).Concat(CraftLogAutoSolve327Tests.Cases).Concat(Settings327Tests.Cases).Concat(TriadSwitches327Tests.Cases).Concat(TriadFamilyTests.Cases).Concat(TriadComboTests.Cases).Concat(TriadRulesTests.Cases).Concat(TriadSolverTests.Cases).Concat(TriadCardTests.Cases).Concat(GatheringScheduleTests.Cases).Concat(InventoryTests.Cases).Concat(InventoryCaptureTests.Cases)
    .Concat(InventoryReadEvidenceTests.Cases).Concat(ItemCatalogTests.Cases).Concat(InventoryBankTests.Cases)
    .Concat(ItemHoldingsTests.Cases).Concat(CraftChainTests.Cases).Concat(MapCatalogTests.Cases).Concat(MapBookmarkTests.Cases).Concat(MacroSequenceTests.Cases).Concat(MacroChainTests.Cases).Concat(MyToolsTests.Cases).Concat(MyToolsGalleryTests.Cases).Concat(CollectablesTests.Cases).Concat(DiscoveriesTests.Cases).ToArray();
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
