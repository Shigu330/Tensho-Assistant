namespace TenshoAssistant;

// Window disposal: cancels and disposes every worker the window owns (craft chain, search and log sessions, Triple Triad solver, advice
// and requests, preparation prices, Mini Cactpot, route and build searches since 0.315) and drops the cached views, once.
internal sealed partial class MainWindow
{
    public void Dispose()
    {
        if(windowDisposed)return;
        windowDisposed=true;cactpotGeneration++;
        // 0.327 (queue step e): the last page used is written once more when the plugin unloads with the window open.
        SaveLastPage();
        craftChain.Dispose();
        craftSolve.Dispose();
        craftLogSession.Dispose();
        craftLog.Reset();
        triadSolve.Dispose();
        CancelTriadAdvice();
        CancelTriadRequest();
        preparationPrices.Dispose();
        toolPrices.Dispose();
        routePrices.Dispose();
        currencyPrices.Dispose();
        cactpotSolve.Dispose();
        // 0.315: the route and build searches stop with the window.
        plannerCancel?.Cancel();plannerCancel?.Dispose();plannerCancel=null;
        buildSearchCancel?.Cancel();buildSearchCancel?.Dispose();buildSearchCancel=null;
        airshipPlanCancel?.Cancel();airshipPlanCancel?.Dispose();airshipPlanCancel=null;
        bankFrameRows.Clear();
        bankFrameGroups=[];bankFrameErrors=[];
        bankSourceRows.Clear();bankSourcePersonal=[];bankSourceCategories=[];
        bankEntriesView=[];bankEntriesSource=null;bankSourceViewKey=null;bankFrameViewKey=null;
        chainCostView=null;chainCostViewKey=null;preparationListSource=null;preparationListCatalog=null;preparationListIds=[];
        liveEffectsFrame=null;liveEffectsCatalog=null;liveEffectsObservation=null;
    }
}
