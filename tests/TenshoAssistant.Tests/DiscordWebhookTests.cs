// Test audit of 2026-11-09, batch 2 (0.1021): DiscordOff778Tests, DiscordChannels598Tests merged into DiscordWebhookTests; every case is kept.
// R-12 (0.319): Discord webhook rules (addresses, message, mentions) and the notifier's retry and queue rules.
using System.Net;
using System.Net.Http;
using System.Text.Json;
using TenshoAssistant.Alarms;
internal static class DiscordWebhookTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static bool HasCode(string text,string code)=>System.Text.RegularExpressions.Regex.Replace(text,@"\s+","").Contains(System.Text.RegularExpressions.Regex.Replace(code,@"\s+",""),StringComparison.Ordinal);
    private const string Url = "https://discord.com/api/webhooks/123456789012345678/abcdefghijklmnopqrstuvwxyz_ABCDEF-123456";
    private const string Token = "abcdefghijklmnopqrstuvwxyz_ABCDEF-123456";
    private static string ChannelUrl(char mark)=>"https://discord.com/api/webhooks/123456789012345678/"+new string(mark,40);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discord webhook addresses: only HTTPS on Discord's own hosts with a webhook path are accepted",()=>
        {
            Check(DiscordWebhook.IsValidUrl(Url),"discord.com webhook");
            Check(DiscordWebhook.IsValidUrl(" https://discordapp.com/api/webhooks/123456789012345678/" + Token + " "),"discordapp.com, spaces trimmed");
            Check(DiscordWebhook.IsValidUrl("https://ptb.discord.com/api/v10/webhooks/123456789012345678/" + Token),"ptb host and API version");
            foreach (var bad in new[]
            {
                "", "   ", "http://discord.com/api/webhooks/123456789012345678/" + Token, "https://discord.com.evil.net/api/webhooks/123456789012345678/" + Token,
                "https://evil.net/api/webhooks/123456789012345678/" + Token, "https://discord.com:8443/api/webhooks/123456789012345678/" + Token,
                "https://user:pass@discord.com/api/webhooks/123456789012345678/" + Token, "https://discord.com/api/webhooks/123456789012345678/" + Token + "?wait=true",
                "https://discord.com/api/webhooks/123456789012345678/" + Token + "#x", "https://discord.com/api/webhooks/123/" + Token,
                "https://discord.com/api/webhooks/123456789012345678/", "https://discord.com/api/channels/123456789012345678/messages", "discord.com/api/webhooks/123456789012345678/" + Token,
            })
                Check(!DiscordWebhook.IsValidUrl(bad),"refused: " + bad);
            var shown = DiscordWebhook.Redact(Url);
            Check(shown.Length > 0 && !shown.Contains(Token,StringComparison.Ordinal) && !shown.Contains("123456789012345678",StringComparison.Ordinal),"redacted address hides the token and the id: " + shown);
            Check(DiscordWebhook.Redact("https://evil.net/x") == "","nothing shown for another address");
        }),
        ("Discord messages: placeholders, own message first, character only when allowed, role mention and Discord's limits",()=>
        {
            var settings = new DiscordSettings { Enabled = true, WebhookUrl = Url };
            var at = new DateTimeOffset(2026,11,2,14,5,0,TimeSpan.Zero);
            var off=new DiscordSettings{ReminderRowsOff=["Ventures"]};
            var live=new DiscordSettings{Enabled=true,Reminders=true,WebhookUrl=settings.WebhookUrl.Length>0 ? settings.WebhookUrl:"https://discord.com/api/webhooks/1/abc",ReminderRowsOff=["Ventures"]};
            var byRow=new TenshoAlarm{ReminderRow="Supply",Source=AlarmSource.Reminder,Discord=false};
            Check(DiscordWebhook.ShouldSend(live,byRow),"0.584 a reminder follows its row, not an old switch of its own");
            Check(!DiscordWebhook.ShouldSend(live,new TenshoAlarm{AutoKey="venture:1",Source=AlarmSource.Reminder}),"0.584 row off");
            Check(!DiscordWebhook.ShouldSend(live,new TenshoAlarm{Source=AlarmSource.Reminder,Discord=false}),"0.584 not a row: its own switch");
            Check(DiscordWebhook.ReminderRowOf(new TenshoAlarm{AutoKey="venture:12"})=="Ventures" && DiscordWebhook.ReminderRowOf(new TenshoAlarm{AutoKey="squadron:mission"})=="SquadronMission" && DiscordWebhook.ReminderRowOf(new TenshoAlarm{ReminderRow="Supply"})=="Supply","0.578 the row");
            Check(!DiscordWebhook.ReminderRowOn(off,new TenshoAlarm{AutoKey="venture:12"}) && DiscordWebhook.ReminderRowOn(off,new TenshoAlarm{ReminderRow="Supply"}) && DiscordWebhook.ReminderRowOn(off,new TenshoAlarm()),"0.578 only the row switched off");
            var unix=at.ToUnixTimeSeconds();
            Check(DiscordWebhook.Format(settings,"[discordtime] [relative] [zone] [item]",null,"x",1,at,null,"Limsa","Fer") == $"<t:{unix}:t> <t:{unix}:R> Limsa Fer","0.564 markers");
            Check(DiscordWebhook.Format(settings,"[zone][item]",null,"x",1,at,null) == "","unknown names leave nothing");
            Check(DiscordWebhook.EorzeaClock(DateTimeOffset.FromUnixTimeSeconds(0)) == "00:00" && DiscordWebhook.EorzeaClock(DateTimeOffset.FromUnixTimeSeconds(175)) == "01:00","Eorzea time");
            Check(DiscordWebhook.Format(settings,"⏰ [label] : dans [minutes] min ([time])",null,"Poisson",3,at,"Tensho Tester") == "⏰ Poisson : dans 3 min (14:05)","default template, no character by default");
            settings.Template = "[label] ([minutes])";
            Check(DiscordWebhook.Format(settings,"default",null,"A",-2,at,null) == "A (0)","settings template, negative minutes shown as 0");
            Check(DiscordWebhook.Format(settings,"default","Propre : [label]","B",1,at,null) == "Propre : B","the alarm's own message wins");
            settings.IncludeCharacter = true;
            Check(DiscordWebhook.Format(settings,"default",null,"C",1,at,"Ami Nom") == "C (1) — Ami Nom","character appended when the template has no marker");
            Check(DiscordWebhook.Format(settings,"default","[character] : [label]","D",1,at,"Ami Nom") == "Ami Nom : D","character marker");
            settings.IncludeCharacter = false;
            Check(DiscordWebhook.Format(settings,"default","[character] : [label]","E",1,at,"Ami Nom") == ": E","marker removed when not allowed");
            settings.MentionRoleId = "112233445566778899";
            Check(DiscordWebhook.Format(settings,"default",null,"F",1,at,null) == "<@&112233445566778899> F (1)","role mention first");
            settings.MentionRoleId = "@everyone";
            Check(!DiscordWebhook.Format(settings,"default",null,"G",1,at,null).StartsWith("<@&",StringComparison.Ordinal),"an invalid role id mentions nothing");
            var longText = DiscordWebhook.Format(settings,"default",new string('x',3000),"H",1,at,null);
            Check(longText.Length == DiscordWebhook.ContentMaximum && longText.EndsWith('…'),"cut to 2000 characters");
            Check(DiscordWebhook.Username("") == "Tensho" && DiscordWebhook.Username("My Discord bot") == "Tensho" && DiscordWebhook.Username("Clyde") == "Tensho","names Discord refuses fall back to Tensho");
            Check(DiscordWebhook.Username(new string('n',100)).Length == DiscordWebhook.UsernameMaximum && DiscordWebhook.Username(" Bot ") == "Bot","name bounded and trimmed");
        }),
        ("Discord payload never parses mentions from the text and only allows the configured role",()=>
        {
            var settings = new DiscordSettings { Username = "Alarmes", MentionRoleId = "" };
            using (var body = JsonDocument.Parse(DiscordWebhook.Payload(settings,"@everyone @here <@1234> label")))
            {
                var root = body.RootElement;
                Check(root.GetProperty("content").GetString() == "@everyone @here <@1234> label" && root.GetProperty("username").GetString() == "Alarmes","content and name");
                var allowed = root.GetProperty("allowed_mentions");
                Check(allowed.GetProperty("parse").GetArrayLength() == 0 && allowed.GetProperty("roles").GetArrayLength() == 0,"no mention parsed, no role");
            }
            settings.MentionRoleId = " 112233445566778899 ";
            using var withRole = JsonDocument.Parse(DiscordWebhook.Payload(settings,"x"));
            var roles = withRole.RootElement.GetProperty("allowed_mentions").GetProperty("roles");
            Check(roles.GetArrayLength() == 1 && roles[0].GetString() == "112233445566778899","only the configured role, trimmed");
        }),
        ("Discord categories follow the alarm's source, else its gathering job; each alarm and category can be left out",()=>
        {
            var settings = new DiscordSettings { Enabled = true, WebhookUrl = Url };
            Check(DiscordWebhook.SourceOf(new TenshoAlarm { JobId = 16 }) == AlarmSource.Gathering && DiscordWebhook.SourceOf(new TenshoAlarm { JobId = 17 }) == AlarmSource.Gathering,"miner and botanist");
            Check(DiscordWebhook.SourceOf(new TenshoAlarm { JobId = 18 }) == AlarmSource.Fishing && DiscordWebhook.SourceOf(new TenshoAlarm()) == AlarmSource.Other,"fisher, no job");
            Check(DiscordWebhook.SourceOf(new TenshoAlarm { JobId = 18, Source = AlarmSource.Reminder }) == AlarmSource.Reminder,"stored source wins");
            var fish = new TenshoAlarm { JobId = 18, Discord = true }; // 0.778 (X-23): off by default, turned on here
            Check(DiscordWebhook.ShouldSend(settings,fish),"sent once the alarm and the channel are on, with an address");
            settings.Fishing = false; Check(!DiscordWebhook.ShouldSend(settings,fish),"category off"); settings.Fishing = true;
            fish.Discord = false; Check(!DiscordWebhook.ShouldSend(settings,fish),"alarm left out"); fish.Discord = true;
            Check(!DiscordWebhook.ShouldSend(new DiscordSettings { WebhookUrl = Url },fish),"off by default");
            Check(!DiscordWebhook.ShouldSend(new DiscordSettings { Enabled = true, WebhookUrl = "https://evil.net/api/webhooks/1/2" },fish),"invalid address");
            Check(!DiscordWebhook.ShouldSend(null,fish),"no settings");
            var voyage = new TenshoAlarm { Source = AlarmSource.Voyage };
            settings.Voyages = false; Check(!DiscordWebhook.ShouldSend(settings,voyage),"voyages off");
            var fresh = new TenshoAssistant.Configuration();
            Check(!fresh.Discord.Enabled && fresh.Discord.WebhookUrl.Length == 0 && !fresh.Discord.IncludeCharacter,"configuration default: off, no address, no character");
        }),
        ("Discord notifier: success; rejection, server error and unknown network failure not retried; refused connection tried once more",()=>
        {
            var calls = 0;
            using (var ok = new DiscordNotifier(new HttpClient(new MarketTestHandler((request,_) =>
            {
                calls++;
                Check(request.Method == HttpMethod.Post && request.RequestUri!.Host == "discord.com","posts to the webhook");
                Check(request.Content!.Headers.ContentType!.MediaType == "application/json","JSON body");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            })),(_,_) => Task.CompletedTask))
            {
                Check(ok.SendAsync(new(Url,"{}")).GetAwaiter().GetResult() == DiscordStatus.Sent && calls == 1 && ok.State.Sent == 1,"204 is a success");
            }
            calls = 0;
            using (var rejected = new DiscordNotifier(new HttpClient(new MarketTestHandler((_,_) => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)); })),(_,_) => Task.CompletedTask))
            {
                Check(rejected.SendAsync(new(Url,"{}")).GetAwaiter().GetResult() == DiscordStatus.Rejected && calls == 1,"404 not retried");
                Check(rejected.State.Code == 404 && rejected.State.Failed == 1,"code kept");
            }
            // A post is not idempotent: a server error or an unknown network failure may have reached the channel, so it is not sent again.
            calls = 0;
            using (var server = new DiscordNotifier(new HttpClient(new MarketTestHandler((_,_) => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)); })),(_,_) => Task.CompletedTask))
                Check(server.SendAsync(new(Url,"{}")).GetAwaiter().GetResult() == DiscordStatus.ServerError && calls == 1 && server.State.Code == 502,"502 not retried, server error");
            calls = 0;
            using (var network = new DiscordNotifier(new HttpClient(new MarketTestHandler((_,_) => { calls++; throw new HttpRequestException("socket"); })),(_,_) => Task.CompletedTask))
                Check(network.SendAsync(new(Url,"{}")).GetAwaiter().GetResult() == DiscordStatus.Network && calls == 1 && network.State.Code == 0,"unknown network error not retried, no code");
            calls = 0;
            using (var refused = new DiscordNotifier(new HttpClient(new MarketTestHandler((_,_) =>
            {
                calls++;
                if (calls == 1) throw new HttpRequestException(HttpRequestError.ConnectionError,"refused");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            })),(_,_) => Task.CompletedTask))
                Check(refused.SendAsync(new(Url,"{}")).GetAwaiter().GetResult() == DiscordStatus.Sent && calls == 2,"a connection that could not be opened is tried once more");
            using var invalid = new DiscordNotifier(new HttpClient(new MarketTestHandler((_,_) => throw new Exception("must not be called"))),(_,_) => Task.CompletedTask);
            Check(invalid.SendAsync(new("https://evil.net/api/webhooks/1/2","{}")).GetAwaiter().GetResult() == DiscordStatus.InvalidAddress,"another host is never contacted");
            Check(!invalid.Enqueue(new("https://evil.net/api/webhooks/1/2","{}")),"nor queued");
        }),
        ("Discord notifier: 429 waits Retry-After up to 60 s; a longer block drops the post and holds the queue; three attempts at most",()=>
        {
            var waits = new List<TimeSpan>(); var calls = 0;
            using (var limited = new DiscordNotifier(new HttpClient(new MarketTestHandler((_,_) =>
            {
                calls++;
                var response = new HttpResponseMessage(calls < 3 ? HttpStatusCode.TooManyRequests : HttpStatusCode.NoContent);
                if (calls == 1) response.Headers.TryAddWithoutValidation("Retry-After","2");
                if (calls == 2) response.Headers.TryAddWithoutValidation("Retry-After","1e309");
                return Task.FromResult(response);
            })),(wait,_) => { waits.Add(wait); return Task.CompletedTask; }))
            {
                Check(limited.SendAsync(new(Url,"{}")).GetAwaiter().GetResult() == DiscordStatus.Sent && calls == 3,"sent on the third attempt");
                Check(waits.SequenceEqual([TimeSpan.FromSeconds(2),TimeSpan.FromSeconds(5)]),"Retry-After waited; an unreadable value (1e309) falls back to 5 s: " + string.Join(",",waits));
            }
            calls = 0; waits.Clear();
            using (var blocked = new DiscordNotifier(new HttpClient(new MarketTestHandler((_,_) =>
            {
                calls++;
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.TryAddWithoutValidation("Retry-After","600");
                return Task.FromResult(response);
            })),(wait,_) => { waits.Add(wait); return Task.CompletedTask; }))
            {
                Check(blocked.SendAsync(new(Url,"{}")).GetAwaiter().GetResult() == DiscordStatus.RateLimited && calls == 1 && waits.Count == 0,"a 600 s block is not shortened: dropped at once");
                Check(blocked.Block == TimeSpan.FromSeconds(600),"the queue holds for the block: " + blocked.Block);
            }
            calls = 0;
            using (var always = new DiscordNotifier(new HttpClient(new MarketTestHandler((_,_) => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)); })),(_,_) => Task.CompletedTask))
                Check(always.SendAsync(new(Url,"{}")).GetAwaiter().GetResult() == DiscordStatus.RateLimited && calls == DiscordNotifier.Attempts,"gives up after three attempts");
            using var empty = new DiscordNotifier(new HttpClient(new MarketTestHandler((_,_) =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.NoContent);
                response.Headers.TryAddWithoutValidation("X-RateLimit-Remaining","0");
                response.Headers.TryAddWithoutValidation("X-RateLimit-Reset-After","3.5");
                return Task.FromResult(response);
            })),(_,_) => Task.CompletedTask);
            Check(empty.SendAsync(new(Url,"{}")).GetAwaiter().GetResult() == DiscordStatus.Sent && empty.Block == TimeSpan.FromSeconds(3.5),"an empty bucket holds the next post");
        }),
        ("Discord notifier: Clear drops waiting posts and stops the one in progress; the worker survives an unexpected error",()=>
        {
            var started = new TaskCompletionSource(); var calls = 0;
            var notifier = new DiscordNotifier(new HttpClient(new MarketTestHandler(async (_,token) =>
            {
                Interlocked.Increment(ref calls); started.TrySetResult();
                await Task.Delay(Timeout.Infinite,token);
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            })),(_,_) => Task.CompletedTask);
            try
            {
                for (var index = 0; index < 5; index++) notifier.Enqueue(new(Url,"m" + index));
                Check(started.Task.Wait(TimeSpan.FromSeconds(5)),"first post started");
                notifier.Clear();
                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (notifier.State.Pending > 0) { if (DateTime.UtcNow > deadline) throw new Exception("not cleared"); Thread.Sleep(10); }
                Check(notifier.State.Sent == 0 && notifier.State.Dropped == 5 && Volatile.Read(ref calls) == 1,"four waiting dropped and the one in progress stopped: " + notifier.State);
            }
            finally { notifier.Dispose(); }
            var attempts = 0;
            using var survivor = new DiscordNotifier(new HttpClient(new MarketTestHandler((_,_) =>
            {
                if (Interlocked.Increment(ref attempts) == 1) throw new InvalidOperationException("unexpected");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            })),(_,_) => Task.CompletedTask);
            survivor.Enqueue(new(Url,"a")); survivor.Enqueue(new(Url,"b"));
            var until = DateTime.UtcNow.AddSeconds(5);
            while (survivor.State.Sent < 1) { if (DateTime.UtcNow > until) throw new Exception("worker died: " + survivor.State); Thread.Sleep(10); }
            Check(survivor.State.Failed == 1 && survivor.State.Status == DiscordStatus.Sent,"the error is recorded and the next post still leaves");
        }),
        ("Discord role ids: ASCII digits only, a pasted mention is accepted; legacy alarms get their source and job",()=>
        {
            Check(!DiscordWebhook.IsValidRoleId("１１２２３３４４５５６６７７８８９９") && DiscordWebhook.RoleIdFrom("１１２２３３４４５５６６７７８８９９") is null,"full-width digits refused");
            Check(!DiscordWebhook.IsValidUrl("https://discord.com/api/webhooks/１２３４５６７８９０１２３４５６７８/" + Token),"full-width digits refused in the address");
            Check(DiscordWebhook.RoleIdFrom(" <@&112233445566778899> ") == "112233445566778899" && DiscordWebhook.RoleIdFrom("112233445566778899") == "112233445566778899","mention or bare id");
            Check(DiscordWebhook.RoleIdFrom("  ") == "" && DiscordWebhook.RoleIdFrom("Modérateurs") is null,"empty removes, a name is refused");
            var reminder = new TenshoAlarm { Group = "Rappels" }; var voyage = new TenshoAlarm { Group = "Compagnie libre" }; var weather = new TenshoAlarm { Group = "Pluie · Limsa" };
            Check(DiscordWebhook.AssignLegacySource(reminder,"Rappels","Compagnie libre") && reminder.Source == AlarmSource.Reminder,"reminder by its group");
            Check(DiscordWebhook.AssignLegacySource(voyage,"Rappels","Compagnie libre") && voyage.Source == AlarmSource.Voyage,"voyage by its group");
            Check(!DiscordWebhook.AssignLegacySource(weather,"Rappels","Compagnie libre") && weather.Source == AlarmSource.Other,"other groups untouched");
            Check(!DiscordWebhook.AssignLegacySource(reminder,"Rappels","Compagnie libre"),"done once");
            Check(AlarmSchedule.JobOfAlarm(new TenshoAlarm { ItemId = 5, JobId = 17 },_ => null) == 17,"stored job kept");
            Check(AlarmSchedule.JobOfAlarm(new TenshoAlarm { ItemId = 5 },_ => null) == 0 && AlarmSchedule.JobOfAlarm(new TenshoAlarm(),_ => throw new Exception("not asked")) == 0,"no sources, no item");
        }),
        ("Discord notifier queue: posts in order on its worker, two seconds apart, drops the oldest past 20, nothing after disposal",()=>
        {
            var bodies = new List<string>(); var gate = new object(); var waits = 0;
            var release = new TaskCompletionSource();
            var notifier = new DiscordNotifier(new HttpClient(new MarketTestHandler(async (request,token) =>
            {
                await release.Task.WaitAsync(token);
                var body = await request.Content!.ReadAsStringAsync(token);
                lock (gate) bodies.Add(body);
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            })),(wait,token) => { if (wait == DiscordNotifier.MinimumInterval) Interlocked.Increment(ref waits); return Task.CompletedTask; });
            try
            {
                for (var index = 0; index < DiscordNotifier.QueueMaximum + 5; index++) Check(notifier.Enqueue(new(Url,"m" + index)),"queued " + index);
                var state = notifier.State;
                // Pending counts the post in progress too: at most the full queue plus the one the worker already took.
                Check(state.Dropped >= 4 && state.Pending <= DiscordNotifier.QueueMaximum + 1,"queue bounded, oldest dropped: dropped " + state.Dropped + ", pending " + state.Pending);
                release.SetResult();
                var deadline = DateTime.UtcNow.AddSeconds(10);
                // Every post not dropped is sent (the first one may already have been taken by the worker before the queue filled).
                var expected = DiscordNotifier.QueueMaximum + 5 - notifier.State.Dropped;
                while (notifier.State.Sent < expected) { if (DateTime.UtcNow > deadline) throw new Exception("queue not drained"); Thread.Sleep(10); }
                lock (gate)
                {
                    Check(bodies[^1] == "m" + (DiscordNotifier.QueueMaximum + 4),"newest kept and sent last");
                    Check(bodies.Zip(bodies.Skip(1)).All(pair => int.Parse(pair.First[1..]) < int.Parse(pair.Second[1..])),"posted in order");
                }
                Check(Volatile.Read(ref waits) >= DiscordNotifier.QueueMaximum - 1,"the two-second pause after each post");
            }
            finally { notifier.Dispose(); }
            Check(!notifier.Enqueue(new(Url,"late")),"nothing queued after disposal");
            notifier.Dispose();
        }),
        ("Discord status and errors never contain the webhook address",()=>
        {
            using var notifier = new DiscordNotifier(new HttpClient(new MarketTestHandler((_,_) => throw new HttpRequestException("failed " + Url))),(_,_) => Task.CompletedTask);
            notifier.SendAsync(new(Url,"{}")).GetAwaiter().GetResult();
            Check(!notifier.State.ToString().Contains(Token,StringComparison.Ordinal),"state has only a status and a code");
        }),
        // From DiscordOff778Tests: 0.778 (X-23, user request): Discord off for a new alarm.
        ("Discord off by default: a new alarm does not post; a saved one keeps its choice",() =>
        {
            Check(!new TenshoAlarm().Discord,"new");
            var saved=System.Text.Json.JsonSerializer.Deserialize<TenshoAlarm>("{\"Discord\":true}")!;
            Check(saved.Discord,"saved choice kept");
        }),
        // From DiscordChannels598Tests: 0.598 (T24): a Discord channel per category. Addresses here are made up, of the right shape.
        ("Discord channels: a category's own address, else the main one",() =>
        {
            var settings=new DiscordSettings{Enabled=true,WebhookUrl=ChannelUrl('a'),CategoryUrls=new(){[AlarmSource.Voyage]=ChannelUrl('b')}};
            Check(DiscordWebhook.UrlFor(settings,AlarmSource.Voyage)==ChannelUrl('b'),"its own");
            Check(DiscordWebhook.UrlFor(settings,AlarmSource.Gathering)==ChannelUrl('a'),"the main one");
            Check(DiscordWebhook.UrlFor(new DiscordSettings{WebhookUrl=ChannelUrl('a')},AlarmSource.Voyage)==ChannelUrl('a'),"none set");
        }),
        ("Discord channels: a bad address never receives anything",() =>
        {
            var settings=new DiscordSettings{Enabled=true,WebhookUrl=ChannelUrl('a'),CategoryUrls=new(){[AlarmSource.Voyage]="https://evil.net/api/webhooks/1/2"}};
            Check(DiscordWebhook.UrlFor(settings,AlarmSource.Voyage)==ChannelUrl('a'),"falls back on the main one");
            var alone=new DiscordSettings{Enabled=true,CategoryUrls=new(){[AlarmSource.Voyage]="https://evil.net/api/webhooks/1/2"}};
            Check(!DiscordWebhook.ShouldSend(alone,AlarmSource.Voyage),"no valid address at all: nothing is sent");
        }),
        ("Discord channels: a category can send without a main address, the others cannot; switches still count",() =>
        {
            var settings=new DiscordSettings{Enabled=true,CategoryUrls=new(){[AlarmSource.Currency]=ChannelUrl('b')}};
            Check(DiscordWebhook.ShouldSend(settings,AlarmSource.Currency) && !DiscordWebhook.ShouldSend(settings,AlarmSource.DutyPop),"only the one with an address");
            settings.Currencies=false;
            Check(!DiscordWebhook.ShouldSend(settings,AlarmSource.Currency),"its category off");
            settings.Currencies=true;settings.Enabled=false;
            Check(!DiscordWebhook.ShouldSend(settings,AlarmSource.Currency),"sending off");
            var fish=new TenshoAlarm{JobId=18,Discord=true};
            var fishing=new DiscordSettings{Enabled=true,CategoryUrls=new(){[AlarmSource.Fishing]=ChannelUrl('c')}};
            Check(DiscordWebhook.ShouldSend(fishing,fish),"an alarm, through its category");
            fish.Discord=false;
            Check(!DiscordWebhook.ShouldSend(fishing,fish),"its own switch");
        }),
        ("Discord channels: every sender uses it, the reset keeps them, no report shows them",() =>
        {
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(!plugin.Contains("discord.Enqueue(new(settings.WebhookUrl"),"no sender left on the main address alone");
            Check(plugin.Split("DiscordWebhook.UrlFor(settings,").Length==4,"alarms, currencies, duty ready");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/UI/Components/SettingRow.cs")).Contains("configuration.Discord.CategoryUrls=categoryWebhooks;"),"kept by a reset, as the main address");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Discord.cs"));
            Check(HasCode(view,"ImGuiInputTextFlags.Password,reset:false); discordCategoryEdits"),"typed masked");
            Check(view.Contains("DiscordWebhook.Redact(Read())"),"shown redacted");
            Check(!File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Advanced.cs")).Contains("CategoryUrls"),"not in the diagnostic report");
            Check(TenshoAssistant.Help.ProblemReport.Clean("x "+ChannelUrl('b')+" y")=="x <webhook> y","and a report cleans any address");
        }),
    ];
}
