namespace TenshoAssistant.Inventory;

/// <summary>
/// X-431 M2 (0.1416): a retainer's gil, read one way everywhere. The retainer list (RetainerManager) keeps each retainer's gil as the list last
/// showed it: after a deposit or a withdrawal it changes only when the list is opened again. The open retainer's own purse (the RetainerGil
/// container) is updated by the game at once. X-19 (0.769, user report) read it in GilWatcher; the retainers' table (X-196) now reads it too.
/// </summary>
public static partial class RetainerPurse
{
    /// <summary>The gil of retainer <paramref name="retainerId"/>: the open retainer's purse when it is that one and was read, else the list's figure.</summary>
    public static long Gil(ulong retainerId,long listed,(ulong Id,long Gil)? open)
        =>retainerId!=0 && open is { } purse && purse.Id==retainerId ? purse.Gil:listed;
}
