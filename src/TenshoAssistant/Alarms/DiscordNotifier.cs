using System.Net;
using System.Text;

namespace TenshoAssistant.Alarms;

/// <summary>Outcome of the last Discord post, for the settings line (never contains the webhook address).</summary>
public enum DiscordStatus { Idle, Sent, Rejected, ServerError, RateLimited, Network, InvalidAddress }

/// <summary>One message waiting to be posted: address and JSON body captured when the alarm rang.</summary>
public sealed record DiscordPost(string WebhookUrl, string Body);

/// <summary>
/// Posts Discord messages on a worker (R-12, 0.319), never on the framework or draw thread: a small bounded queue (oldest dropped past
/// 20), one post at a time and at least two seconds apart (Discord allows a few posts per second per webhook and about 30 per minute per
/// channel). A post is not idempotent, so it is tried again only when it surely did not reach Discord: « 429 » (Retry-After waited, at
/// most 60 s; a longer block drops the post and holds the queue that long, at most an hour) and a connection that could not be opened.
/// A timeout or a server error is not retried (the message may already be in the channel). <see cref="Clear"/> drops the queue, stops
/// the post in progress and ends the pause Discord asked for (address cleared or changed, sending turned off). Only Discord webhook addresses are accepted
/// (DiscordWebhook.IsValidUrl). The state keeps only a status and an HTTP code, never the address.
/// </summary>
public sealed class DiscordNotifier : IDisposable
{
    public const int QueueMaximum = 20, Attempts = 3;
    public static readonly TimeSpan MinimumInterval = TimeSpan.FromSeconds(2), LongestWait = TimeSpan.FromSeconds(60), LongestBlock = TimeSpan.FromHours(1);
    private readonly HttpClient client;
    private readonly bool ownsClient;
    private readonly Func<TimeSpan, CancellationToken, Task> delay;
    private readonly object gate = new();
    private readonly Queue<DiscordPost> queue = new();
    private readonly SemaphoreSlim signal = new(0);
    private readonly CancellationTokenSource stop = new();
    private CancellationTokenSource? current;
    private Task? worker;
    private bool disposed, inFlight;
    private int sent, failed, dropped, lastCode;
    private DiscordStatus status;
    // Pause asked by Discord for the next post (Retry-After beyond LongestWait, or the bucket emptied), read by the worker.
    private TimeSpan block;
    // 0.331: end of the pause Discord asked for, while the worker waits it out (Block showed zero then), and that wait's cancellation:
    // Clear ends it, so a new address does not wait out the old one's pause.
    private DateTimeOffset blockedUntil;
    private CancellationTokenSource? pauseCancel;

    public DiscordNotifier(HttpClient? client = null, Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        ownsClient = client is null;
        this.client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10), MaxResponseContentBufferSize = 64 * 1024 };
        // Discord asks HTTP clients for a descriptive User-Agent; no personal data in it. X-76 (step 2): Tensho's one User-Agent, with the project's address.
        if (ownsClient) this.client.DefaultRequestHeaders.UserAgent.ParseAdd($"{Community.BoundedHttp.UserAgent} (+https://github.com/Shigu330/Tensho-Assistant)");
        this.delay = delay ?? Task.Delay;
    }

    /// <summary>Last status and code, counts, and posts waiting (the one being sent included).</summary>
    public (DiscordStatus Status, int Code, int Sent, int Failed, int Dropped, int Pending) State
    { get { lock (gate) return (status, lastCode, sent, failed, dropped, queue.Count + (inFlight ? 1 : 0)); } }

    /// <summary>Pause Discord asked for before the next post (zero when none): the one not started yet, else what is left of the one the
    /// worker is waiting out; for the settings line and the tests.</summary>
    public TimeSpan Block
    { get { lock (gate) return block > TimeSpan.Zero ? block : TimeSpan.FromTicks(Math.Max(0, (blockedUntil - DateTimeOffset.UtcNow).Ticks)); } }

    /// <summary>Queues a post without waiting (safe from the framework thread); false when refused (invalid address or disposed).</summary>
    public bool Enqueue(DiscordPost post)
    {
        ArgumentNullException.ThrowIfNull(post);
        if (!DiscordWebhook.IsValidUrl(post.WebhookUrl)) { lock (gate) { status = DiscordStatus.InvalidAddress; lastCode = 0; } return false; }
        lock (gate)
        {
            if (disposed) return false;
            while (queue.Count >= QueueMaximum) { queue.Dequeue(); dropped++; }
            queue.Enqueue(post);
            // A worker that ended (it should not) is started again rather than leaving the queue stuck.
            if (worker is null || worker.IsCompleted) worker = Task.Run(() => RunAsync(stop.Token));
            // Released under the lock: Dispose marks the notifier disposed under the same lock before disposing the semaphore.
            signal.Release();
        }
        return true;
    }

    /// <summary>Drops every waiting post, stops the one in progress and ends a pause Discord asked for: the player cleared or changed the
    /// address, or turned sending off.</summary>
    public void Clear()
    {
        lock (gate)
        {
            if (disposed) return;
            dropped += queue.Count; queue.Clear(); block = TimeSpan.Zero; blockedUntil = default;
            current?.Cancel(); pauseCancel?.Cancel();
        }
    }

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await signal.WaitAsync(token).ConfigureAwait(false);
                DiscordPost? next; CancellationTokenSource one;
                lock (gate)
                {
                    next = queue.Count > 0 ? queue.Dequeue() : null;
                    if (next is null) continue;
                    inFlight = true; one = current = CancellationTokenSource.CreateLinkedTokenSource(token);
                }
                try { await SendAsync(next, one.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) when (!token.IsCancellationRequested) { lock (gate) dropped++; }
                catch (Exception) when (!token.IsCancellationRequested) { Record(DiscordStatus.Network, 0); }
                finally { lock (gate) { inFlight = false; current = null; } one.Dispose(); }
                TimeSpan pause; CancellationTokenSource held;
                lock (gate)
                {
                    pause = block > MinimumInterval ? block : MinimumInterval;
                    blockedUntil = block > TimeSpan.Zero ? DateTimeOffset.UtcNow + pause : default; block = TimeSpan.Zero;
                    held = pauseCancel = CancellationTokenSource.CreateLinkedTokenSource(token);
                }
                try { await delay(pause, held.Token).ConfigureAwait(false); }
                // Ended by Clear: the two seconds between posts are still kept.
                catch (OperationCanceledException) when (!token.IsCancellationRequested) { await delay(MinimumInterval, token).ConfigureAwait(false); }
                finally { lock (gate) { pauseCancel = null; blockedUntil = default; } held.Dispose(); }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }

    /// <summary>Posts one message now, with the retry rules above; the result is also kept in <see cref="State"/>.</summary>
    public async Task<DiscordStatus> SendAsync(DiscordPost post, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(post);
        if (!DiscordWebhook.IsValidUrl(post.WebhookUrl)) return Record(DiscordStatus.InvalidAddress, 0);
        var result = (Status: DiscordStatus.Network, Code: 0);
        for (var attempt = 1; attempt <= Attempts; attempt++)
        {
            TimeSpan? wait = null;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(post.WebhookUrl.Trim())) { Content = new StringContent(post.Body, Encoding.UTF8, "application/json") };
                TenshoAssistant.Community.BoundedHttp.Count(request.RequestUri?.Host ?? "?");
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                var code = (int)response.StatusCode;
                if (response.IsSuccessStatusCode) { HoldIfBucketEmpty(response); return Record(DiscordStatus.Sent, code); }
                if (response.StatusCode != HttpStatusCode.TooManyRequests) return Record(code >= 500 ? DiscordStatus.ServerError : DiscordStatus.Rejected, code);
                result = (DiscordStatus.RateLimited, code);
                var retry = Seconds(response, "Retry-After") ?? TimeSpan.FromSeconds(5);
                // A long block is not shortened: the post is dropped and the queue waits as long as Discord asked.
                if (retry > LongestWait) { lock (gate) block = retry < LongestBlock ? retry : LongestBlock; break; }
                wait = retry;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            // Retried only when the connection could not be opened: the message surely did not leave.
            catch (HttpRequestException error) when (error.HttpRequestError is HttpRequestError.NameResolutionError or HttpRequestError.ConnectionError or HttpRequestError.SecureConnectionError)
            { result = (DiscordStatus.Network, 0); wait = attempt < 2 ? TimeSpan.FromSeconds(2) : null; }
            catch (Exception error) when (error is HttpRequestException or TaskCanceledException or IOException) { result = (DiscordStatus.Network, 0); }
            if (wait is null || attempt == Attempts) break;
            await delay(wait.Value, token).ConfigureAwait(false);
        }
        return Record(result.Status, result.Code);
    }

    // After a post, Discord says how many are left in its bucket; at zero, the next post waits for the bucket to refill.
    private void HoldIfBucketEmpty(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var left) && left.FirstOrDefault() == "0" && Seconds(response, "X-RateLimit-Reset-After") is { } reset)
            lock (gate) block = reset < LongestBlock ? reset : LongestBlock;
    }

    // A header in seconds (Retry-After, X-RateLimit-Reset-After); only finite values from 0 to an hour, else null.
    private static TimeSpan? Seconds(HttpResponseMessage response, string header)
    {
        if (header == "Retry-After" && response.Headers.RetryAfter?.Delta is { } delta) return delta <= LongestBlock ? delta : LongestBlock;
        if (!response.Headers.TryGetValues(header, out var values) || !double.TryParse(values.FirstOrDefault(), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var seconds) || !double.IsFinite(seconds) || seconds < 0) return null;
        return TimeSpan.FromSeconds(Math.Min(seconds, LongestBlock.TotalSeconds));
    }

    private DiscordStatus Record(DiscordStatus result, int code)
    {
        lock (gate) { status = result; lastCode = code; if (result == DiscordStatus.Sent) sent++; else failed++; }
        return result;
    }

    public void Dispose()
    {
        Task? running;
        lock (gate) { if (disposed) return; disposed = true; queue.Clear(); running = worker; current?.Cancel(); }
        stop.Cancel();
        try { running?.Wait(TimeSpan.FromSeconds(2)); } catch (AggregateException) { }
        stop.Dispose(); signal.Dispose();
        if (ownsClient) client.Dispose();
    }
}
