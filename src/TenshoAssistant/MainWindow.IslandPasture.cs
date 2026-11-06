using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Island;

namespace TenshoAssistant;

// W-04 (0.685): the island's animals. The pasture comes from the game when the tab opens and on click; the list comes from the wiki on click only. docs/evidence/0.685-island-animals.md.
internal sealed partial class MainWindow
{
    internal Func<Task<PastureSnapshot>>? ReadPasture { get; set; }
    internal IslandAnimalSource? IslandAnimalBookSource { get; set; }
    internal Func<IReadOnlyList<IslandAnimal>,Task<IReadOnlyList<IslandAnimal>>>? LocalizeIslandAnimals { get; set; }
    private const uint IslandMapRow=773;
    private Task<PastureSnapshot>? pastureRead;
    private PastureSnapshot? pasture;
    private bool pastureTried,pastureFailed;
    private IslandAnimalDocument? islandAnimalsFrom;
    private Task<IReadOnlyList<IslandAnimal>>? islandAnimalsLocalizing;
    private IReadOnlyList<IslandAnimal> islandAnimals=[];
    private string islandAnimalSearch="";

    private void DrawIslandPasture()
    {
        ImGui.TextWrapped(localization.Get("Island.PastureHelp"));
        if(!pastureTried){pastureTried=true;pastureRead=ReadPasture?.Invoke();}
        if(pastureRead is { IsCompleted:true } read)
        {
            pastureRead=null;
            pastureFailed=!read.IsCompletedSuccessfully;
            pasture=read.IsCompletedSuccessfully ? read.Result:null;
        }
        ImGui.BeginDisabled(ReadPasture is null || pastureRead is not null);
        try{if(ImGui.Button(localization.Get("Island.PastureRead")))pastureRead=ReadPasture!();}
        finally{ImGui.EndDisabled();}
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get("Island.PastureReadTip"));
        if(pastureFailed)ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("Island.PastureUnavailable"));
        else if(pasture is { } current)DrawPastureTable(current);
        ImGui.Separator();
        DrawIslandAnimalList();
    }

    private void DrawPastureTable(PastureSnapshot current)
    {
        switch(current.Status)
        {
            case PastureReadStatus.NotOnIsland:ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("Island.NotOnIsland"));return;
            case PastureReadStatus.NotLoaded:ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("Island.PastureNotLoaded"));return;
        }
        if(current.Animals.Count==0){ImGui.TextDisabled(localization.Get("Island.PastureEmpty"));return;}
        if(!ImGui.BeginTable("###IslandPastureTable",6,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV))return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("Island.ColumnAnimal"));
            ImGui.TableSetupColumn(localization.Get("Island.ColumnNickname"));
            ImGui.TableSetupColumn(localization.Get("Island.ColumnMood"));
            ImGui.TableSetupColumn(localization.Get("Island.ColumnFood"));
            ImGui.TableSetupColumn(localization.Get("Island.ColumnLeavings"));
            ImGui.TableSetupColumn(localization.Get("Island.ColumnCare"));
            ImGui.TableHeadersRow();
            foreach(var animal in current.Animals)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();ImGui.TextUnformatted(animal.Name);
                ImGui.TableNextColumn();ImGui.TextUnformatted(animal.Nickname);
                ImGui.TableNextColumn();ImGui.TextUnformatted(animal.Mood.ToString());
                ImGui.TableNextColumn();ImGui.TextUnformatted(animal.Food.ToString());
                ImGui.TableNextColumn();ImGui.TextUnformatted(localization.Get(animal.Leavings ? "Island.Yes":"Island.No"));
                ImGui.TableNextColumn();ImGui.TextUnformatted(localization.Get(animal.UnderCare ? "Island.Yes":"Island.No"));
            }
        }
        finally{ImGui.EndTable();}
    }

    private void DrawIslandAnimalList()
    {
        ImGui.TextColored(Accent,localization.Get("Island.AnimalListTitle"));
        if(IslandAnimalBookSource is not { } source)return;
        var state=source.State;
        ImGui.BeginDisabled(state.Loading);
        try{if(ImGui.Button(localization.Get("Island.AnimalListRead")))source.Request();}
        finally{ImGui.EndDisabled();}
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get("Island.AnimalListReadTip"));
        if(state.Error is not null)ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("Island.AnimalListFailed"));
        if(state.Document is not { } document)return;
        if(!ReferenceEquals(document,islandAnimalsFrom))
        {
            islandAnimalsFrom=document;islandAnimals=document.Rows;
            islandAnimalsLocalizing=LocalizeIslandAnimals?.Invoke(document.Rows);
        }
        if(islandAnimalsLocalizing is { IsCompleted:true } localizing)
        {
            islandAnimalsLocalizing=null;
            if(localizing.IsCompletedSuccessfully)islandAnimals=localizing.Result;
            else log.Warning(localizing.Exception,"Island animal names could not be put in the game's language.");
        }
        ImGui.TextDisabled(localization.Format("Island.AnimalListSource",document.FetchedAt.ToLocalTime().ToString("g")));
        ImGui.SetNextItemWidth(260);
        ImGui.InputTextWithHint("##IslandAnimalSearch",localization.Get("Island.AnimalSearch"),ref islandAnimalSearch,100);
        Tip("Island.AnimalSearchTip");
        var map=maps.State.Catalog?.Find(IslandMapRow);
        if(!ImGui.BeginTable("###IslandAnimalTable",7,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.Resizable))return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("Island.ColumnAnimal"));
            ImGui.TableSetupColumn(localization.Get("Island.ColumnPlace"));
            ImGui.TableSetupColumn(localization.Get("Island.ColumnEorzeaTime"));
            ImGui.TableSetupColumn(localization.Get("Island.ColumnWeather"));
            ImGui.TableSetupColumn(localization.Get("Island.ColumnSize"));
            ImGui.TableSetupColumn(localization.Get("Island.ColumnRestraint"));
            ImGui.TableSetupColumn(localization.Get("Island.ColumnLeavings"));
            ImGui.TableHeadersRow();
            var index=0;
            foreach(var animal in islandAnimals)
            {
                index++;
                if(islandAnimalSearch.Length>0 && !animal.Name.Contains(islandAnimalSearch,StringComparison.CurrentCultureIgnoreCase)
                   && !animal.Common.Contains(islandAnimalSearch,StringComparison.CurrentCultureIgnoreCase) && !animal.Bonus.Contains(islandAnimalSearch,StringComparison.CurrentCultureIgnoreCase))continue;
                ImGui.TableNextRow();
                ImGui.TableNextColumn();ImGui.TextUnformatted(animal.Name);
                ImGui.TableNextColumn();
                ImGui.BeginDisabled(map is null);
                try{if(ImGui.SmallButton($"X {animal.X:0.#} Y {animal.Y:0.#}##IslandAnimal{index}") && map is not null)OpenMapPointFromPage(map,animal.X,animal.Y);}
                finally{ImGui.EndDisabled();}
                if(map is not null)PlaceMenu($"IslandAnimalPlace{index}",map,new(animal.X,animal.Y),animal.Name);
                if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get("Island.AnimalPlaceTip"));
                ImGui.TableNextColumn();ImGui.TextUnformatted(animal.Time.Length>0 ? animal.Time:localization.Get("Island.AnyTime"));
                ImGui.TableNextColumn();ImGui.TextUnformatted(animal.Weather.Length>0 ? animal.Weather:localization.Get("Island.AnyWeather"));
                ImGui.TableNextColumn();ImGui.TextUnformatted(animal.Size);
                ImGui.TableNextColumn();ImGui.TextUnformatted(animal.Restraint);
                ImGui.TableNextColumn();ImGui.TextWrapped($"{animal.Common}, {animal.Bonus}");
            }
        }
        finally{ImGui.EndTable();}
    }
}
