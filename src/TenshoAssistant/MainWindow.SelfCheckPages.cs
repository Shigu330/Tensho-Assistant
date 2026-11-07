using TenshoAssistant.Carnivale;
using TenshoAssistant.Deliveries;
using TenshoAssistant.Diagnostics;
using TenshoAssistant.GrandCompanies;
using TenshoAssistant.Relics;
using TenshoAssistant.Societies;

namespace TenshoAssistant;

// 0.722: the pages coded without the game read their sheets and the game's data here too, once per load, in a self-check (developer mode, or on
// request): the same reads as opening the page, off the frame or on the framework thread as there. Only the numbers are kept: docs/evidence/0.722-selfcheck-pages.md.
internal sealed partial class MainWindow
{
    private IReadOnlyList<(string Domain,Task<SelfCheckLine> Read)>? selfCheckPages;

    private void StartSelfCheckPages()
    {
        if(selfCheckPages is not null)return;
        static (string,Task<SelfCheckLine>) Page(string domain,Func<Task<SelfCheckLine>> read)=>(domain,Task.Run(read));
        selfCheckPages=
        [
            Page("Eureka and Bozja",()=>Task.FromResult(FieldCatalogLoad?.Invoke() is { } field && field.Zones.Sum(zone=>zone.Events.Count)>0
                ? Diagnostics.SelfCheck.Ok("Eureka and Bozja",$"{string.Join("+",field.Zones.Select(zone=>zone.Events.Count))} places, {field.Records.Count} records, {field.Actions.Count} actions")
                :Diagnostics.SelfCheck.Failed("Eureka and Bozja","nothing read"))),
            Page("FATEs",()=>Task.FromResult(FatesLoad?.Invoke() is { Count:>0 } zones
                ? Diagnostics.SelfCheck.Ok("FATEs",$"{zones.Count} zones, {zones.Sum(zone=>zone.Events.Count)} FATEs")
                :Diagnostics.SelfCheck.Failed("FATEs","nothing read"))),
            Page("deep dungeons",()=>Task.FromResult(DeepDungeonLoad?.Invoke() is { Count:>0 } dungeons
                ? (dungeons.SelectMany(dungeon=>dungeon.Pomanders.Concat(dungeon.Others)).Any(thing=>thing.Tooltip.Contains('<'))
                    ? Diagnostics.SelfCheck.Suspect("deep dungeons","a text kept a macro")
                    :Diagnostics.SelfCheck.Ok("deep dungeons",string.Join(", ",dungeons.Select(dungeon=>$"{dungeon.Pomanders.Count}+{dungeon.Others.Count}"))+" items"))
                :Diagnostics.SelfCheck.Failed("deep dungeons","nothing read"))),
            Page("lexicon",()=>Task.FromResult(LexiconLoad?.Invoke() is { Count:>0 } words
                ? Diagnostics.SelfCheck.Ok("lexicon",$"{words.Count} words, {words.Select(word=>word.Kind).Distinct().Count()} kinds")
                :Diagnostics.SelfCheck.Failed("lexicon","nothing read"))),
            Page("relics",async ()=>
            {
                if(RelicCatalogLoad?.Invoke() is not { } catalog)return Diagnostics.SelfCheck.Failed("relics","sheets unreadable");
                var armour=catalog.Armours?.Values.Sum(sets=>sets.Count) ?? 0;
                var whole=catalog.Armours?.Values.SelectMany(sets=>sets).SelectMany(set=>set.Stages).Where(stage=>stage.Count>0).All(stage=>stage.Count==5)==true;
                var note=ReadRelics is null ? null:await ReadRelics();
                var text=$"{catalog.Lines.Count} zodiac lines, {catalog.Families?.Count ?? 0} families, {armour} armour sets, {note?.Status}";
                return armour>0 && !whole ? Diagnostics.SelfCheck.Suspect("relics",text+", a stage without five pieces"):Diagnostics.SelfCheck.Ok("relics",text);
            }),
            Page("allied societies",async ()=>
            {
                if(AlliedCatalogLoad?.Invoke() is not { } catalog)return Diagnostics.SelfCheck.Failed("allied societies","sheets unreadable");
                var read=ReadAllied is null ? null:await ReadAllied(catalog);
                return read?.Status==AlliedReadStatus.Loaded ? Diagnostics.SelfCheck.Ok("allied societies",$"{catalog.Societies.Count} societies, {read.Standings.Count} read"):Diagnostics.SelfCheck.NotRead("allied societies",read?.Status.ToString() ?? "no reader");
            }),
            Page("grand companies",async ()=>
            {
                if(GcCatalogLoad?.Invoke(false) is not { } catalog)return Diagnostics.SelfCheck.Failed("grand companies","sheets unreadable");
                var read=ReadGc is null ? null:await ReadGc(catalog.Ranks.Count+1);
                return read?.Status==GcReadStatus.Loaded ? Diagnostics.SelfCheck.Ok("grand companies",$"{catalog.Ranks.Count} ranks, {read.Seals.Count} companies"):Diagnostics.SelfCheck.NotRead("grand companies",read?.Status.ToString() ?? "no reader");
            }),
            Page("custom deliveries",async ()=>
            {
                if(DeliveryClientsLoad?.Invoke() is not { Count:>0 } clients)return Diagnostics.SelfCheck.Failed("custom deliveries","sheets unreadable");
                var read=ReadDeliveries is null ? null:await ReadDeliveries(clients);
                return read?.Status==DeliveryReadStatus.Loaded ? Diagnostics.SelfCheck.Ok("custom deliveries",$"{clients.Count} clients, {read.Remaining} left"):Diagnostics.SelfCheck.NotRead("custom deliveries",read?.Status.ToString() ?? "no reader");
            }),
            Page("masked carnivale",async ()=>
            {
                if(CarnivaleStagesLoad?.Invoke() is not { Count:>0 } stages)return Diagnostics.SelfCheck.Failed("masked carnivale","sheets unreadable");
                var read=ReadCarnivale is null ? null:await ReadCarnivale(stages);
                return read?.Status==CarnivaleReadStatus.Loaded ? Diagnostics.SelfCheck.Ok("masked carnivale",$"{stages.Count} stages, {read.Completed.Count(done=>done)} done"):Diagnostics.SelfCheck.NotRead("masked carnivale",read?.Status.ToString() ?? "no reader");
            }),
        ];
    }

    private IEnumerable<(string Domain,Func<SelfCheckLine> Probe)> SelfCheckPageProbes()=>(selfCheckPages ?? []).Select(page=>(page.Domain,(Func<SelfCheckLine>)(()=>
        !page.Read.IsCompleted ? Diagnostics.SelfCheck.NotRead(page.Domain,"still reading")
        :page.Read.IsCompletedSuccessfully ? page.Read.Result
        :Diagnostics.SelfCheck.Failed(page.Domain,page.Read.Exception?.GetBaseException().Message))));
}
