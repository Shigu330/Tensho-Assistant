using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.Items;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private CraftLiveFrame? projectionFrame;private CraftActionCatalog? projectionCatalog;private CraftProjectionResult? projectionResult;
    private uint projectionRecipe;private int projectionTarget,projectionSort;
    private CraftProjectionResult? sortedProjectionSource;
    private int sortedProjectionOrder=-1;
    private CraftActionProjection[] sortedProjectionRows=[];
    private void DrawCraftProjection(CraftLiveState state)
    {
        if(!ImGui.CollapsingHeader(localization.Get("CraftProjectionUi.Title"),ImGuiTreeNodeFlags.DefaultOpen))return;
        ImGui.TextWrapped(localization.Get("CraftProjectionUi.Help"));
        if(state.Frame is not { } frame || craftActions.State.Catalog is not { } catalog)return;
        var now=DateTimeOffset.UtcNow;
        if(!ReferenceEquals(projectionFrame,frame) || !ReferenceEquals(projectionCatalog,catalog))
        {projectionFrame=frame;projectionCatalog=catalog;projectionResult=CraftLiveProjection.Build(state,catalog,now);}
        if(state.ObservedAt is not { } date || now<date || now-date>TimeSpan.FromSeconds(2))
        {ImGui.TextWrapped(localization.Get("CraftProjectionUi.Old"));return;}
        if(projectionResult is not { } result)return;
        if(frame.Recipe is { } recipe && projectionRecipe!=recipe.RecipeId)
        {projectionRecipe=recipe.RecipeId;projectionTarget=recipe.Simulation?.Quality ?? 0;}
        ImGui.SetNextItemWidth(110);ImGui.InputInt(localization.Get("CraftProjectionUi.Target"),ref projectionTarget,0);
        if(projectionTarget<0 || projectionTarget>(frame.Recipe?.Simulation?.Quality ?? 0)){ImGui.TextWrapped(localization.Get("CraftProjectionUi.Invalid"));return;}
        if(ImGui.RadioButton(localization.Get("CraftProjectionUi.Quality"),projectionSort==0))projectionSort=0;ImGui.SameLine();
        if(ImGui.RadioButton(localization.Get("CraftProjectionUi.Progress"),projectionSort==1))projectionSort=1;ImGui.SameLine();
        if(ImGui.RadioButton(localization.Get("CraftProjectionUi.Durability"),projectionSort==2))projectionSort=2;
        foreach(var issue in result.Issues)DrawCraftIssue(issue);
        if(result.Actions.Count==0){ImGui.TextDisabled(localization.Get("CraftProjectionUi.Empty"));return;}
        ImGui.TextDisabled(localization.Get("CraftProjectionUi.SortHelp"));
        if(ImGui.BeginTable("LiveCraftProjection",7,ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            try
            {
                ImGui.TableSetupColumn(localization.Get("CraftProjectionUi.Action"),ImGuiTableColumnFlags.WidthStretch,2);
                foreach(var title in new[]{localization.Get("CraftProjectionUi.AfterProgress"),localization.Get("CraftProjectionUi.AfterQuality"),localization.Get("CraftProjectionUi.AfterDurability"),localization.Get("CraftProjectionUi.Cp"),localization.Get("CraftProjectionUi.Quiet"),localization.Get("CraftProjectionUi.Result")})ImGui.TableSetupColumn(title);
                ImGui.TableHeadersRow();
                if(!ReferenceEquals(sortedProjectionSource,result) || sortedProjectionOrder!=projectionSort)
                {
                    sortedProjectionSource=result;sortedProjectionOrder=projectionSort;
                    sortedProjectionRows=result.Actions.OrderBy(row=>row.Broken).ThenByDescending(row=>projectionSort==0 ? row.Quality?.Min ?? -1:projectionSort==1 ? row.Progress.Min:row.Durability.Min).ThenBy(row=>row.CpCost.Max).ToArray();
                }
                var rows=sortedProjectionRows;
                foreach(var row in rows)
                {
                    ImGui.TableNextRow();ImGui.TableNextColumn();DrawItemIcon(new ItemDefinition(0,row.Metadata.Name,false,row.Metadata.Icon),new Vector2(24));ImGui.SameLine();ImGui.TextWrapped(row.Metadata.Name);
                    if(ImGui.IsItemHovered())ImGui.SetTooltip(localization.Format("LiveDetails.Alternatives",row.Metadata.Description,row.Alternatives));
                    ImGui.TableNextColumn();ImGui.TextUnformatted(row.Progress.ToString());ImGui.TableNextColumn();ImGui.TextUnformatted(row.Quality?.ToString() ?? localization.Get("CraftProjectionUi.Unknown"));
                    ImGui.TableNextColumn();ImGui.TextUnformatted(row.Durability.ToString());ImGui.TableNextColumn();ImGui.TextUnformatted(row.CpCost.ToString());
                    ImGui.TableNextColumn();ImGui.TextUnformatted(row.InnerQuiet.ToString());ImGui.TableNextColumn();
                    var text=row.Broken ? localization.Get("CraftProjectionUi.Broken"):row.Completed ? row.Quality is null ? localization.Get("CraftProjectionUi.UnknownEnd"):row.Quality.Value.Min>=projectionTarget ? localization.Get("CraftProjectionUi.Reached"):localization.Get("CraftProjectionUi.Below"):localization.Get("CraftProjectionUi.Continue");
                    ImGui.TextColored(row.Broken || row.Completed && (row.Quality is null || row.Quality.Value.Min<projectionTarget) ? Accent:row.Completed ? Success:Muted,text);
                }
            }
            finally{ImGui.EndTable();}
        }
    }
}
