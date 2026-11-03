using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;

namespace TenshoAssistant;

// g, X-02 (0.370): above the Orchestrion tab, the music playing now (its roll, obtained or not) and the favourite rolls; a star in the
// roll window adds or removes a favourite. Changing the music is not done (game function with undocumented parameters, noted).
internal sealed partial class MainWindow
{
    // The orchestrion roll of the music playing now, or null (set by Plugin.cs; BGM → roll by file path).
    internal Func<uint?>? PlayingRoll { get; set; }
    internal Func<bool>? MusicRead { get; set; }

    private void DrawOrchestrionPlayer(CollectionState state)
    {
        var roll=PlayingRoll?.Invoke();
        var entry=roll is { } rollId ? state.Entries.FirstOrDefault(row=>row.Kind==CollectionKind.Orchestrion && row.Id==rollId):null;
        if(entry is not null)
        {
            ImGui.TextUnformatted(localization.Format("OrchestrionPlayer.Playing",entry.Name));
            ImGui.SameLine();
            ImGui.TextColored(entry.ConfirmedUnlocked ? Success:Accent,localization.Get(entry.ConfirmedUnlocked ? "OrchestrionPlayer.Owned":"OrchestrionPlayer.Missing"));
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get("OrchestrionPlayer.Open"))){selectedOrchestrion=entry.Id;openOrchestrionDetailsRequested=true;}
        }
        else ImGui.TextDisabled(localization.Get(MusicRead?.Invoke()==true ? "OrchestrionPlayer.NoRoll":"OrchestrionPlayer.Unknown"));
        if(configuration.OrchestrionFavorites is not { Count:>0 } favorites)return;
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("OrchestrionPlayer.Favorites",favorites.Count)+"###OrchestrionFavorites"))return;
        foreach(var id in favorites)
        {
            var favorite=state.Entries.FirstOrDefault(row=>row.Kind==CollectionKind.Orchestrion && row.Id==id);
            if(ImGui.Selectable((favorite?.Name ?? $"#{id}")+(favorite?.ConfirmedUnlocked==false ? "  · "+localization.Get("OrchestrionPlayer.Missing"):"")+$"##Fav{id}"))
            {selectedOrchestrion=id;openOrchestrionDetailsRequested=true;}
        }
    }

    private void DrawOrchestrionFavorite(uint id)
    {
        var favorite=configuration.OrchestrionFavorites?.Contains(id)==true;
        if(!ImGui.Checkbox(localization.Get("OrchestrionPlayer.Favorite")+$"##Favorite{id}",ref favorite))
        {Tip("OrchestrionPlayer.FavoriteTip");return;}
        Tip("OrchestrionPlayer.FavoriteTip");
        var list=configuration.OrchestrionFavorites?.ToList() ?? [];
        if(!list.Remove(id))
        {
            if(list.Count>=300){saveError=localization.Get("OrchestrionPlayer.Full");return;}
            list.Add(id);
        }
        SaveSetting("OrchestrionPlayer.Favorite",()=>configuration.OrchestrionFavorites,value=>configuration.OrchestrionFavorites=value,list.Count==0 ? null:list);
    }
}
