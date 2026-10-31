using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gathering;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private void DrawFishingNotebook(GatheringCatalog catalog)
    {
        var entries=catalog.Fishing.ForItem((uint)itemId);
        if(entries.Count==0 || !ImGui.CollapsingHeader(localization.Get("FishingNotebook.Title")+"###FishingNotebook"))return;
        string Restriction(bool? value)=>localization.Get(value switch{true=>"FishingNotebook.Reported",false=>"FishingNotebook.NotReported",_=>"FishingNotebook.Unknown"});
        foreach(var entry in entries)
        {
            ImGui.PushID((int)entry.FishId);
            try
            {
                if(entries.Count>1 && developerMode)ImGui.TextDisabled(localization.Format("FishingNotebook.Reference",entry.FishId));
                if(!string.IsNullOrWhiteSpace(entry.Description))ImGui.TextWrapped(entry.Description);
                ImGui.TextWrapped(localization.Format("FishingNotebook.Time",Restriction(entry.TimeRestricted)));
                ImGui.TextWrapped(localization.Format("FishingNotebook.Weather",Restriction(entry.WeatherRestricted)));
                if(!string.IsNullOrWhiteSpace(entry.Folklore))ImGui.TextWrapped(localization.Format("FishingNotebook.Folklore",entry.Folklore));
                if(!entry.InLog)ImGui.TextDisabled(localization.Get("FishingNotebook.NotInLog"));
                if(!entry.NoteLinked)ImGui.TextDisabled(localization.Get("FishingNotebook.MissingNote"));
            }
            finally{ImGui.PopID();}
        }
        ImGui.TextWrapped(localization.Get("FishingNotebook.Disclaimer"));
        ImGui.Separator();
    }
}
