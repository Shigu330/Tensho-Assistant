using TenshoAssistant.Community;
using TenshoAssistant.Duties;

namespace TenshoAssistant;

/// <summary>
/// LuminaSupplemental first (0.188): duty boss coffers, boss drops and chests from LuminaSupplemental when it knows the duty, Garland's
/// rewards kept and its coffers used only for the kinds LuminaSupplemental lacks; item sources list its duties before Garland and the wiki.
/// </summary>
internal sealed partial class MainWindow
{
    private LuminaSupplementalService? lumina;
    internal void ConfigureLumina(LuminaSupplementalService service)=>lumina=service;
    private (DutyLootDocument? Garland,LuminaSupplementalData? Data,uint Duty,DutyLootDocument? Result) dutyDocumentCache;

    private IReadOnlyCollection<uint> LuminaDutiesOf(uint itemId){lumina?.Request();return lumina?.State.Data?.DutiesOf(itemId) ?? [];}

    private DutyLootDocument? DutyDocument(DutyLootState state)
    {
        lumina?.Request();
        var data=lumina?.State.Data;
        if(state.Duty is not { } duty)return state.Document;
        if(ReferenceEquals(dutyDocumentCache.Garland,state.Document) && ReferenceEquals(dutyDocumentCache.Data,data) && dutyDocumentCache.Duty==duty.DutyId)
            return dutyDocumentCache.Result;
        var extra=data?.ForDuty(duty.DutyId) ?? [];
        DutyLootDocument? result=state.Document;
        if(extra.Count>0)
        {
            var kinds=extra.Select(entry=>entry.Kind).ToHashSet();
            var garland=(state.Document?.Entries ?? []).Where(entry=>entry.Kind==DutyLootSourceKind.Reward || !kinds.Contains(entry.Kind));
            result=new DutyLootDocument(duty.InstanceId,state.Document?.FetchedAt ?? DateTimeOffset.UtcNow,state.Document?.HasRewards ?? false,true,true,
                Array.AsReadOnly(garland.Concat(extra).ToArray()));
        }
        dutyDocumentCache=(state.Document,data,duty.DutyId,result);
        return result;
    }
}
