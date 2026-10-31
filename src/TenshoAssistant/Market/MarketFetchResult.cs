namespace TenshoAssistant.Market;

public enum MarketFetchStatus { Success, Busy, Cooldown, Preempted, ScopeChanged, Cancelled, Failed }
public sealed record MarketFetchResult(MarketFetchStatus Status, MarketQuote? Quote = null, string? Error = null, TimeSpan RetryAfter = default);
