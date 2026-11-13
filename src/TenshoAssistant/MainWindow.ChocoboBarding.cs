using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using TenshoAssistant.Gear;

namespace TenshoAssistant;

// R-11 step 4 (0.340, DEC-023 point 5): chocobo barding; its own « Chocobo » page since 0.341 (user request, was at the bottom of Gear). Appearance only: what the companion chocobo wears,
// which bardings are unlocked, the item that teaches each (Gear/ChocoboBarding.cs). The chocobo is read on the framework thread only
// while this section is open (Gear/ChocoboReader.Dalamud.cs); the list is filtered only when an input changes.
internal sealed partial class MainWindow
{
    internal Func<ChocoboState?>? Chocobo { get; set; }
    internal Func<bool>? ChocoboUnavailable { get; set; }
    internal Func<uint,string?>? StainName { get; set; }
    private string bardingSearch="";
    private bool bardingMissingOnly;
    private (GearCatalog Catalog,string Search,bool Missing,IReadOnlySet<uint>? Unlocked)? bardingViewKey;
    private IReadOnlyList<BardingSet> bardingView=[];

    // X-204 (0.1235): the racing chocobo, folded: rank and rating, pedigree, sex, the five attributes (current / maximum), the two abilities.
    internal Func<MiniGames.RaceChocoboSnapshot?>? RaceChocobo { get; set; }
    internal Func<bool>? RaceChocoboUnavailable { get; set; }
    internal Lazy<MiniGames.RaceChocoboRank[]>? RaceChocoboRanks { get; set; }
    internal Func<uint,MiniGames.RaceChocoboAbility?>? RaceChocoboAbility { get; set; }
    private static readonly string[] RaceChocoboAttributeKeys=["MaxSpeed","Acceleration","Endurance","Stamina","Cunning"];
    private void DrawRaceChocobo()
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("RaceChocobo.Title")+"###RaceChocobo"))return;
        var race=RaceChocobo?.Invoke();
        if(RaceChocoboUnavailable?.Invoke()==true && race is null){ImGui.TextDisabled(localization.Get("RaceChocobo.Unavailable"));return;}
        if(race is null){ImGui.TextWrapped(localization.Get("RaceChocobo.NotLoaded"));return;}
        var rank=MiniGames.RaceChocobo.RankOf(race.Rating,RaceChocoboRanks?.Value ?? []);
        ImGui.TextColored(Accent,localization.Format("RaceChocobo.Summary",rank?.Name ?? "—",race.Rating,race.Pedigree,localization.Get(race.Female ? "RaceChocobo.Female":"RaceChocobo.Male")));
        Tip("RaceChocobo.SummaryTip");
        if(ImGui.BeginTable("RaceChocoboAttributes",3,ImGuiTableFlags.SizingFixedFit|ImGuiTableFlags.RowBg))
        {
            try
            {
                for(var index=0;index<MiniGames.RaceChocobo.Attributes;index++)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();ImGui.TextUnformatted(localization.Get("RaceChocobo."+RaceChocoboAttributeKeys[index]));
                    ImGui.TableNextColumn();ImGui.TextUnformatted(localization.Format("RaceChocobo.Value",race.Current[index],race.Maximum[index]));
                    ImGui.TableNextColumn();if(race.Current[index]>=race.Maximum[index] && race.Maximum[index]>0)ImGui.TextColored(Success,localization.Get("RaceChocobo.AtMaximum"));
                }
            }
            finally{ImGui.EndTable();}
        }
        foreach(var (key,id) in new[]{("RaceChocobo.Hereditary",race.AbilityHereditary),("RaceChocobo.Learned",race.AbilityLearned)})
        {
            var ability=RaceChocoboAbility?.Invoke(id);
            ImGui.TextUnformatted(localization.Format(key,ability?.Name ?? localization.Get("RaceChocobo.NoAbility")));
            if(ability is { Description.Length:>0 })TipText(ability.Description);
        }
        ImGui.TextDisabled(localization.Format("RaceChocobo.ReadAt",race.ReadAt.ToLocalTime().ToString("g")));
        ImGui.Separator();
    }

    private void DrawChocoboPage()
    {
        Heading(localization.Get("ChocoboBarding.Title"),localization.Get("ChocoboBarding.Help"));
        if(characterStats.State.CharacterId==0){ImGui.TextWrapped(localization.Get("CharacterUi.Offline"));return;}
        DrawRaceChocobo();
        if(GearCatalog?.Invoke() is not { } catalog){ImGui.TextDisabled(GearNotReady);return;}
        var chocobo=Chocobo?.Invoke();
        if(ChocoboUnavailable?.Invoke()==true)ImGui.TextDisabled(localization.Get("ChocoboBarding.Unavailable"));
        else if(chocobo is null)ImGui.TextDisabled(localization.Get("ChocoboBarding.Loading"));
        else if(chocobo.Name.Length==0)ImGui.TextDisabled(localization.Get("ChocoboBarding.NoChocobo"));
        else
        {
            var colour=chocobo.StainId==0 ? "—":StainName?.Invoke(chocobo.StainId) ?? $"#{chocobo.StainId}";
            ImGui.TextColored(Accent,localization.Format("ChocoboBarding.Summary",chocobo.Name,chocobo.Rank,colour));
            string Part(uint id)=>id==0 ? localization.Get("ChocoboBarding.None"):catalog.Bardings.FirstOrDefault(set=>set.Id==id)?.Name ?? $"#{id}";
            ImGui.TextUnformatted(localization.Format("ChocoboBarding.Worn",Part(chocobo.Head),Part(chocobo.Body),Part(chocobo.Legs)));
            Tip("ChocoboBarding.WornTip");
        }
        var unlocked=chocobo?.Unlocked;
        if(ChocoboBarding.Progress(catalog.Bardings,unlocked) is { } progress)
            ImGui.TextDisabled(localization.Format("ChocoboBarding.Progress",progress.Unlocked,progress.Total));

        ImGui.SetNextItemWidth(220);
        ImGui.InputTextWithHint("##BardingSearch",localization.Get("ChocoboBarding.Search"),ref bardingSearch,64);
        Tip("ChocoboBarding.SearchTip");
        ImGui.SameLine();
        ImGui.Checkbox(localization.Get("ChocoboBarding.MissingOnly")+"##BardingMissing",ref bardingMissingOnly);
        Tip("ChocoboBarding.MissingOnlyTip");
        var items=itemCatalog.State.Catalog;
        var key=(catalog,bardingSearch,bardingMissingOnly,unlocked);
        if(bardingViewKey!=key)
        {
            bardingView=ChocoboBarding.Filter(catalog.Bardings,bardingSearch,bardingMissingOnly,unlocked,id=>items?.Find(id)?.Name);
            bardingViewKey=key;
        }
        if(!ImGui.BeginTable("Bardings",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH|ImGuiTableFlags.ScrollY,
            new Vector2(0,Math.Max(ImGui.GetTextLineHeightWithSpacing()*10,ImGui.GetContentRegionAvail().Y))))return;
        try
        {
            ImGui.TableSetupScrollFreeze(0,1);
            foreach(var column in new[]{"Name","Company","Unlocked","Item"})ImGui.TableSetupColumn(localization.Get("ChocoboBarding.Column"+column));
            ImGui.TableHeadersRow();
            var size=new Vector2(ImGui.GetTextLineHeight());
            foreach(var set in bardingView)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                foreach(var icon in new[]{set.IconHead,set.IconBody,set.IconLegs})
                {
                    if(DrawGameIcon(icon,size,false))ImGui.SameLine();
                }
                ImGui.TextUnformatted(set.Name);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(set.GrandCompany==0 ? localization.Get("ChocoboBarding.AnyCompany"):GrandCompanyName(set.GrandCompany));
                ImGui.TableNextColumn();
                // 0.341 (user screenshot: « ✗ » showed as « = », missing from the font): icons from the icon font (DrawStateMark), as the owned mark.
                if(unlocked is null)ImGui.TextDisabled(localization.Get("ChocoboBarding.Unknown"));
                else DrawStateMark(unlocked.Contains(set.Id));
                ImGui.TableNextColumn();
                var item=set.ItemId==0 ? null:items?.Find(set.ItemId);
                if(item is null){ImGui.TextDisabled(set.ItemId==0 ? localization.Get("ChocoboBarding.NoItem"):$"#{set.ItemId}");continue;}
                DrawItemIcon(item,size);ImGui.SameLine();
                ImGui.TextUnformatted(item.Name);
                ImGui.SameLine();DrawOwnedMark(item.Id);
            }
        }
        finally{ImGui.EndTable();}
    }

    internal Func<uint,string?>? GrandCompanyNameOf { get; set; }
    private string GrandCompanyName(uint id)=>GrandCompanyNameOf?.Invoke(id) ?? $"#{id}";
}
