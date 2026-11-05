using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.Inventory;

namespace TenshoAssistant;

// C21 (0.592): Crafting › What can I craft?: the recipes the bags and the saved Bank can make now, worked out on a click off the frame
// (every recipe of the catalogue), filtered by job and level here. Direct ingredients only. Nothing is read again until the next click.
internal sealed partial class MainWindow
{
    private CancellationTokenSource? fromStockCancel;
    private Task<CraftFromStockResult?>? fromStockTask;
    private InventorySnapshot? fromStockLive;
    private BankState? fromStockBank;
    private string fromStockJob="";
    private int fromStockMin=1,fromStockMax=100;
    private (CraftFromStockResult Source,string Job,int Min,int Max,CraftFromStockRow[] Rows)? fromStockView;

    private void StartCraftFromStock(RecipeCatalog catalog)
    {
        fromStockCancel?.Cancel();fromStockCancel?.Dispose();
        var cancel=fromStockCancel=new CancellationTokenSource();var token=cancel.Token;
        var character=bankBridge.State.CharacterId;var live=inventory.Current;var saved=bank.State;
        fromStockLive=live;fromStockBank=saved;
        bool Crystal(uint id)=>ClassifyCrystal(id) is not null;
        fromStockTask=Task.Run(()=>
        {
            try{return (CraftFromStockResult?)CraftFromStock.Find(catalog.All,id=>ItemHoldings.Build(id,character,live,saved.Entries,includeCrystals:true,crystal:Crystal(id)),token);}
            catch(OperationCanceledException){return null;}
            catch(Exception error){log.Warning(error,"The recipes the stocks can make could not be worked out.");return null;}
        },token);
    }

    private void DrawCraftFromStock()
    {
        ImGui.TextWrapped(localization.Get("CraftStock.Help"));
        if(recipeCatalog.State.Catalog is not { } catalog){ImGui.TextDisabled(localization.Get("RecipeUi.Loading"));return;}
        if(ImGui.Button(localization.Get("CraftStock.Search")))StartCraftFromStock(catalog);
        if(fromStockTask is not { } task)return;
        if(!task.IsCompleted){ImGui.SameLine();ImGui.TextDisabled(localization.Get("CraftStock.Searching"));return;}
        if(!task.IsCompletedSuccessfully || task.Result is not { } result){ImGui.TextWrapped(localization.Get("CraftStock.Failed"));return;}
        if(!ReferenceEquals(fromStockLive,inventory.Current) || !ReferenceEquals(fromStockBank,bank.State)){ImGui.SameLine();ImGui.TextColored(Accent,localization.Get("CraftStock.Stale"));}
        ImGui.SetNextItemWidth(170);
        if(ImGui.BeginCombo(localization.Get("CraftStock.Job")+"###CraftStockJob",fromStockJob.Length>0 ? fromStockJob:localization.Get("RecipeUi.AllJobs")))
        {
            try
            {
                if(ImGui.Selectable(localization.Get("RecipeUi.AllJobs"),fromStockJob.Length==0))fromStockJob="";
                foreach(var job in result.Rows.Select(row=>row.Recipe.CraftType).Distinct().Order(StringComparer.CurrentCulture))
                    if(ImGui.Selectable(job,job==fromStockJob))fromStockJob=job;
            }
            finally{ImGui.EndCombo();}
        }
        Tip("CraftStock.JobTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(90);
        ImGui.InputInt(localization.Get("CraftStock.MinLevel")+"###CraftStockMin",ref fromStockMin,0);Tip("CraftStock.LevelTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(90);
        ImGui.InputInt(localization.Get("CraftStock.MaxLevel")+"###CraftStockMax",ref fromStockMax,0);Tip("CraftStock.LevelTip");
        fromStockMin=Math.Clamp(fromStockMin,1,200);fromStockMax=Math.Clamp(fromStockMax,1,200);
        if(fromStockView is not { } known || !ReferenceEquals(known.Source,result) || known.Job!=fromStockJob || known.Min!=fromStockMin || known.Max!=fromStockMax)
            fromStockView=known=(result,fromStockJob,fromStockMin,fromStockMax,CraftFromStock.Filter(result.Rows,fromStockJob,fromStockMin,fromStockMax));
        var rows=known.Rows;
        ImGui.TextColored(Accent,localization.Format("CraftStock.Count",rows.Length.ToString("N0"),result.Rows.Count.ToString("N0")));
        if(result.Undecided>0)ImGui.TextDisabled(localization.Format("CraftStock.Undecided",result.Undecided.ToString("N0")));
        if(rows.Length==0){ImGui.TextDisabled(localization.Get("CraftStock.Empty"));return;}
        DrawChainList("CraftStockList",rows.Length,index=>
        {
            var row=rows[index];
            ImGui.PushID((int)row.Recipe.RecipeId);
            try
            {
                DrawChainItem(row.Recipe.ResultItemId);
                ImGui.SameLine();
                ImGui.TextDisabled(localization.Format(row.LowerBound ? "CraftStock.RowAtLeast":"CraftStock.Row",row.Count.ToString("N0"),row.Recipe.CraftType,row.Recipe.Level));
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("ChainUi.Simulate")))OpenCraftSimulation(row.Recipe);
                if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("ChainUi.SimulateTip"));
            }
            finally{ImGui.PopID();}
        },Math.Max(260,ImGui.GetContentRegionAvail().Y));
    }
}
