using Dalamud.Bindings.ImGui;
using TenshoAssistant.Carnivale;

namespace TenshoAssistant;

// W-02 (0.683): the stages of the Masked Carnivale, done or not, read when the page opens and on the player's click. No advice on the fights. docs/evidence/0.683-masked-carnivale.md.
internal sealed partial class MainWindow
{
    internal Func<IReadOnlyList<CarnivaleStage>?>? CarnivaleStagesLoad { get; set; }
    internal Func<IReadOnlyList<CarnivaleStage>,Task<CarnivaleSnapshot>>? ReadCarnivale { get; set; }
    private IReadOnlyList<CarnivaleStage>? carnivaleStages;
    private bool carnivaleStagesTried,carnivaleReadFailed,carnivaleOnlyLeft;
    private Task<CarnivaleSnapshot>? carnivaleRead;
    private CarnivaleSnapshot? carnivaleSnapshot;
    private IReadOnlyList<CarnivaleRow> carnivaleRows=[];

    private void DrawMaskedCarnivale()
    {
        Heading(localization.Get("MaskedCarnivale.Title"),localization.Get("MaskedCarnivale.Subtitle"));
        if(!carnivaleStagesTried)
        {
            carnivaleStagesTried=true;
            carnivaleStages=CarnivaleStagesLoad?.Invoke();
            if(carnivaleStages is { Count:>0 } && ReadCarnivale is not null)carnivaleRead=ReadCarnivale(carnivaleStages);
        }
        if(carnivaleStages is not { Count:>0 } stages){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("MaskedCarnivale.SheetsUnavailable"));return;}
        ImGui.TextWrapped(localization.Get("MaskedCarnivale.Help"));
        if(carnivaleRead is { IsCompleted:true } read)
        {
            carnivaleRead=null;
            carnivaleReadFailed=!read.IsCompletedSuccessfully;
            carnivaleSnapshot=read.IsCompletedSuccessfully ? read.Result:null;
            carnivaleRows=carnivaleSnapshot is { } snapshot ? CarnivaleBoard.Rows(stages,snapshot):[];
        }
        ImGui.BeginDisabled(ReadCarnivale is null || carnivaleRead is not null);
        try{if(ImGui.Button(localization.Get("MaskedCarnivale.Read")))carnivaleRead=ReadCarnivale!(stages);}
        finally{ImGui.EndDisabled();}
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get("MaskedCarnivale.ReadTip"));
        if(carnivaleReadFailed){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("MaskedCarnivale.Unavailable"));return;}
        if(carnivaleSnapshot is not { } current)return;
        switch(current.Status)
        {
            case CarnivaleReadStatus.NotLoaded:ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("MaskedCarnivale.NotLoaded"));return;
            case CarnivaleReadStatus.Unavailable:ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("MaskedCarnivale.Unavailable"));return;
        }
        ImGui.TextUnformatted(localization.Format("MaskedCarnivale.Count",carnivaleRows.Count(row=>row.Done),carnivaleRows.Count));
        ImGui.SameLine();
        ImGui.Checkbox(localization.Get("MaskedCarnivale.OnlyLeft"),ref carnivaleOnlyLeft);
        Tip("MaskedCarnivale.OnlyLeftTip");
        if(!ImGui.BeginTable("###MaskedCarnivaleTable",5,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.Resizable))return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("MaskedCarnivale.ColumnStage"));
            ImGui.TableSetupColumn(localization.Get("MaskedCarnivale.ColumnEnemies"));
            ImGui.TableSetupColumn(localization.Get("MaskedCarnivale.ColumnStandard"));
            ImGui.TableSetupColumn(localization.Get("MaskedCarnivale.ColumnIdeal"));
            ImGui.TableSetupColumn(localization.Get("MaskedCarnivale.ColumnState"));
            ImGui.TableHeadersRow();
            foreach(var row in carnivaleRows)
            {
                if(carnivaleOnlyLeft && row.Done)continue;
                ImGui.TableNextRow();
                ImGui.TableNextColumn();ImGui.TextUnformatted(row.Stage.Order.ToString());
                ImGui.TableNextColumn();ImGui.TextWrapped(string.Join(", ",row.Stage.Enemies));
                ImGui.TableNextColumn();ImGui.TextUnformatted(CarnivaleBoard.Time(row.Stage.StandardSeconds));
                ImGui.TableNextColumn();ImGui.TextUnformatted(CarnivaleBoard.Time(row.Stage.IdealSeconds));
                ImGui.TableNextColumn();
                if(row.Done)ImGui.TextColored(Accent,localization.Get("MaskedCarnivale.Done"));
                else ImGui.TextDisabled(localization.Get("MaskedCarnivale.Left"));
            }
        }
        finally{ImGui.EndTable();}
    }
}
