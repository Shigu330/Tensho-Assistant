using TenshoAssistant.Community;
using TenshoAssistant.Duties;

namespace TenshoAssistant;

/// <summary>
/// LuminaSupplemental first (0.188): a duty's boss coffers, boss drops and chests from LuminaSupplemental (GPL-3.0 CSVs, downloaded once
/// and cached, Community/LuminaSupplemental.cs) when it knows the duty; Garland's rewards kept and its coffers used only for the kinds
/// LuminaSupplemental lacks; item sources list its duties before Garland and the wiki. The merged document is cached until the Garland
/// document, the data or the duty change; nothing when the LuminaSupplemental source is off (Settings › Advanced).
/// </summary>
internal sealed partial class MainWindow
{
    private LuminaSupplementalService? lumina;
    internal void ConfigureLumina(LuminaSupplementalService service)=>lumina=service;
    private (DutyLootDocument? Garland,LuminaSupplementalData? Data,uint Duty,DutyLootDocument? Result) dutyDocumentCache,dutyFinderDocumentCache;

    private IReadOnlyCollection<uint> LuminaDutiesOf(uint itemId){lumina?.Request();return lumina?.State.Data?.DutiesOf(itemId) ?? [];}

    // 0.331: one cache slot per DutyLootService (panel = the Duty Finder panel's own service), so the page and the panel both visible do
    // not rebuild the merged document on every frame.
    private DutyLootDocument? DutyDocument(DutyLootState state,bool panel=false)
    {
        lumina?.Request();
        var data=lumina?.State.Data;
        if(state.Duty is not { } duty)return state.Document;
        var cached=panel ? dutyFinderDocumentCache:dutyDocumentCache;
        if(ReferenceEquals(cached.Garland,state.Document) && ReferenceEquals(cached.Data,data) && cached.Duty==duty.DutyId)
            return cached.Result;
        var extra=data?.ForDuty(duty.DutyId) ?? [];
        DutyLootDocument? result=state.Document;
        if(extra.Count>0)
        {
            var kinds=extra.Select(entry=>entry.Kind).ToHashSet();
            var garland=(state.Document?.Entries ?? []).Where(entry=>entry.Kind==DutyLootSourceKind.Reward || !kinds.Contains(entry.Kind));
            result=new DutyLootDocument(duty.InstanceId,state.Document?.FetchedAt ?? DateTimeOffset.UtcNow,state.Document?.HasRewards ?? false,true,true,
                Array.AsReadOnly(garland.Concat(extra).ToArray()));
        }
        if(panel)dutyFinderDocumentCache=(state.Document,data,duty.DutyId,result);else dutyDocumentCache=(state.Document,data,duty.DutyId,result);
        return result;
    }
}
