using Dalamud.Game.Network.Structures;
using Dalamud.Plugin.Services;

namespace TenshoAssistant.Market;

/// <summary>
/// X-698 M4 (0.1671): keeps the market tax rates the game sends (IMarketBoard.TaxRatesReceived), in memory only, for the retainer sale
/// list's total after tax. Nothing is asked of the game: the rates are kept when the game itself sends them.
/// </summary>
internal sealed class MarketTaxReader : IDisposable
{
    private readonly IMarketBoard board;
    private readonly IPluginLog log;
    private MarketTaxRates? rates;
    private string? logged;
    private int lines;
    private bool disposed, warned;

    public MarketTaxRates? Rates => Volatile.Read(ref rates);

    public MarketTaxReader(IMarketBoard board, IPluginLog log)
    {
        this.board = board; this.log = log;
        board.TaxRatesReceived += OnRates;
    }

    private void OnRates(IMarketTaxRates received)
    {
        if (disposed || TenshoAssistant.Competitive.CompetitiveGate.Cut) return;
        try
        {
            var until = received.ValidUntil.Kind == DateTimeKind.Local ? received.ValidUntil.ToUniversalTime() : DateTime.SpecifyKind(received.ValidUntil, DateTimeKind.Utc);
            var next = MarketTax.From([received.LimsaLominsaTax, received.GridaniaTax, received.UldahTax, received.IshgardTax, received.KuganeTax,
                received.CrystariumTax, received.SharlayanTax, received.TuliyollalTax], new DateTimeOffset(until));
            if (next is null) { if (!warned) { warned = true; log.Warning("The market tax rates the game sent are out of shape: not kept."); } return; }
            Volatile.Write(ref rates, next);
            var text = string.Join(", ", next.ByTown.Select(pair => $"{pair.Key}={pair.Value}%"));
            if (text != logged && lines < 5) { logged = text; lines++; log.Information("Market tax rates received: {0}, valid until {1:u}.", text, next.ValidUntil); }
        }
        catch (Exception error) { if (!warned) { warned = true; log.Warning(error, "The market tax rates could not be kept."); } }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        board.TaxRatesReceived -= OnRates;
    }
}
