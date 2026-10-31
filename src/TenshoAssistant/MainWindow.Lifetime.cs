namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    public void Dispose()
    {
        if(windowDisposed)return;
        windowDisposed=true;cactpotGeneration++;
        craftChain.Dispose();
        craftSolve.Dispose();
        craftLogSession.Dispose();
        craftLog.Reset();
        triadSolve.Dispose();
        preparationPrices.Dispose();
        cactpotSolve.Dispose();
        bankFrameRows.Clear();
        bankFrameGroups=[];bankFrameErrors=[];
        bankSourceRows.Clear();bankSourcePersonal=[];bankSourceCategories=[];
        bankEntriesView=[];bankEntriesSource=null;bankSourceViewKey=null;bankFrameViewKey=null;
        chainCostView=null;chainCostViewKey=null;preparationListSource=null;preparationListCatalog=null;preparationListIds=[];
        liveEffectsFrame=null;liveEffectsCatalog=null;liveEffectsObservation=null;
    }
}
