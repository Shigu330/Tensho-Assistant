using Dalamud.Bindings.ImGui;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

// X-09 (0.444): Maps › « Place directory » (folded): residential addresses (world, district, ward, plot or apartment, note), added from
// a small form, copied as text, removed with Ctrl; kept in the configuration. Districts: the game's residential areas, its language.
internal sealed partial class MainWindow
{
    internal Func<IReadOnlyList<(uint Territory,string Name)>>? HousingDistricts { get; set; }
    private string placeName="",placeWorld="",placeNote="";
    private int placeDistrict,placeWard=1,placePlot=1;
    private bool placeApartment;
    private string? placeMessage;

    private void DrawPlaceDirectory()
    {
        var places=(IReadOnlyList<PlaceAddress>?)configuration.PlaceDirectory ?? [];
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("Places.Title",places.Count)+"###PlaceDirectory"))return;
        var districts=HousingDistricts?.Invoke() ?? [];
        if(districts.Count==0){ImGui.TextDisabled(localization.Get("Places.NoDistricts"));return;}
        string District(uint territory)=>districts.FirstOrDefault(district=>district.Territory==territory).Name ?? territory.ToString();
        foreach(var place in places)
        {
            ImGui.PushID(place.Id.ToString());
            try
            {
                var text=PlaceDirectory.Text(place,District(place.District),localization.Get("Places.Ward"),localization.Get("Places.Plot"),localization.Get("Places.ApartmentRoom"));
                ImGui.TextUnformatted(text);
                if(place.Note.Length>0 && ImGui.IsItemHovered())WrappedTooltip(place.Note);
                ImGui.SameLine();
                CopyButton(localization.Get("Places.Copy"),()=>text+(place.Note.Length>0 ? " ("+place.Note+")":""));
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("Places.Remove")) && ImGui.GetIO().KeyCtrl){SavePlaces(PlaceDirectory.Remove(places,place.Id));return;}
                Tip("Places.RemoveTip");
            }
            finally{ImGui.PopID();}
        }
        ImGui.Separator();
        ImGui.SetNextItemWidth(180);ImGui.InputTextWithHint("###PlaceName",localization.Get("Places.Name"),ref placeName,PlaceDirectory.TextLimit);Tip("Places.NameTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(120);ImGui.InputTextWithHint("###PlaceWorld",localization.Get("Places.World"),ref placeWorld,PlaceDirectory.TextLimit);Tip("Places.WorldTip");
        placeDistrict=Math.Clamp(placeDistrict,0,districts.Count-1);
        ImGui.SetNextItemWidth(160);
        if(ImGui.BeginCombo(localization.Get("Places.District")+"###PlaceDistrict",districts[placeDistrict].Name))
        {
            try{for(var index=0;index<districts.Count;index++)if(ImGui.Selectable($"{districts[index].Name}##District{index}",index==placeDistrict))placeDistrict=index;}
            finally{ImGui.EndCombo();}
        }
        Tip("Places.DistrictTip");
        ImGui.SetNextItemWidth(90);ImGui.InputInt(localization.Get("Places.Ward")+"###PlaceWard",ref placeWard,0);Tip("Places.WardTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(90);ImGui.InputInt((placeApartment ? localization.Get("Places.ApartmentRoom"):localization.Get("Places.Plot"))+"###PlacePlot",ref placePlot,0);Tip("Places.PlotTip");
        ImGui.SameLine();ImGui.Checkbox(localization.Get("Places.Apartment")+"###PlaceApartment",ref placeApartment);Tip("Places.ApartmentTip");
        ImGui.SetNextItemWidth(320);ImGui.InputTextWithHint("###PlaceNote",localization.Get("Places.Note"),ref placeNote,PlaceDirectory.TextLimit);Tip("Places.NoteTip");
        if(ImGui.SmallButton(localization.Get("Places.Add")))
        {
            var added=PlaceDirectory.Add(places,new(Guid.Empty,placeName,placeWorld,districts[placeDistrict].Territory,placeWard,placePlot,placeApartment,placeNote));
            if(added is null)placeMessage=localization.Get("Places.Invalid");
            else{SavePlaces(added);placeName="";placeNote="";placeMessage=null;}
        }
        if(placeMessage is { } message)ImGui.TextDisabled(message);
    }

    private void SavePlaces(IReadOnlyList<PlaceAddress> places)
    {
        var previous=configuration.PlaceDirectory;
        configuration.PlaceDirectory=places.ToList();
        TrySave("Place directory",()=>configuration.PlaceDirectory=previous);
    }
}
