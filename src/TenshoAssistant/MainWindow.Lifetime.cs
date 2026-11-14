namespace TenshoAssistant;

// Window disposal: cancels and disposes every worker the window owns (craft chain, search and log sessions, Triple Triad solver, advice
// and requests, preparation prices, Mini Cactpot, route and build searches since 0.315) and drops the cached views, once.
internal sealed partial class MainWindow
{
    public void Dispose()
    {
        if(windowDisposed)return;
        windowDisposed=true;cactpotGeneration++;
        jitWarmup.Dispose();
        // 0.327 (queue step e): the last page used is written once more when the plugin unloads with the window open.
        SaveLastPage();
        craftChain.Dispose();
        DisposeMapDiscovery();
        // X-43: the discoveries written once more when the plugin unloads; X-142 (0.1121): after a save that was running.
        FlushDiscoveries();
        // X-284 M1: the beast team plans' last write (made off the game's thread).
        FlushBeastPlans();
        mapFindCancel?.Cancel();mapFindCancel?.Dispose();mapFindCancel=null;
        fromStockCancel?.Cancel();fromStockCancel?.Dispose();fromStockCancel=null;
        craftSolve.Dispose();
        craftLogSession.Dispose();
        craftLog.Reset();
        triadSolve.Dispose();
        CancelTriadAdvice();
        CancelTriadRequest();
        simulationThresholdsCancel?.Cancel();simulationThresholdsCancel?.Dispose();
        preparationPrices.Dispose();
        toolPrices.Dispose();
        routePrices.Dispose();
        currencyPrices.Dispose();
        fromStockPrices.Dispose();
        cactpotSolve.Dispose();
        // 0.315: the route and build searches stop with the window.
        plannerCancel?.Cancel();plannerCancel?.Dispose();plannerCancel=null;
        buildSearchCancel?.Cancel();buildSearchCancel?.Dispose();buildSearchCancel=null;
        airshipPlanCancel?.Cancel();airshipPlanCancel?.Dispose();airshipPlanCancel=null;
        bankFrameRows.Clear();
        bankFrameGroups=[];bankFrameErrors=[];bankFrameGil=[];
        bankSourceRows.Clear();bankSourcePersonal=[];bankSourceCategories=[];
        bankEntriesView=[];bankEntriesSource=null;bankSourceViewKey=null;bankFrameViewKey=null;
        chainCostView=null;chainCostViewKey=null;preparationListSource=null;preparationListCatalog=null;preparationListIds=[];
        liveEffectsFrame=null;liveEffectsCatalog=null;liveEffectsObservation=null;
    }
}
