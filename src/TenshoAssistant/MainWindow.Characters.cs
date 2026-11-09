using Dalamud.Bindings.ImGui;
using TenshoAssistant.Inventory;

namespace TenshoAssistant;

// g, I-03 (0.345): Storage › Characters — every character Tensho has seen (one bank file each): name, last record, gil, items in the
// bags, crystals, retainers and saddlebags saved; « See its storages » opens the Bank tab on that character; « Erase this character »
// (two clicks) deletes its file. Built from the saved bank only when it changes; the id shown is the local character id, never an Account ID.
internal sealed partial class MainWindow
{
    private sealed record CharacterSummary(ulong Id,string Name,DateTimeOffset LastSeen,long? Gil,long Items,long Crystals,int Retainers,int Storages,long? RetainerGil=null);
    private BankState? characterSummarySource;
    private CharacterSummary[] characterSummaries=[];
    private ulong characterEraseArmed;
    private string? characterEraseMessage;
    // Character shown by the Bank tab (null = the logged-in one).
    private ulong? bankViewCharacter;

    /// <summary>« Name (World) » saved with the character's own storages, else « Character …1234 » (last digits of the local id).</summary>
    // 0.609: « Where is this item? »: the item sheet, on the other characters, from what the Bank saved.
    private uint otherStocksItem;private ulong otherStocksCharacter;private BankState? otherStocksBank;private OtherCharacterStock[] otherStocks=[];
    private void DrawOtherCharacterStocks(uint item,ulong character,BankState bank)
    {
        if(otherStocksItem!=item || otherStocksCharacter!=character || !ReferenceEquals(otherStocksBank,bank))
        {otherStocks=OtherCharacterStocks.Of(item,character,bank.Entries);otherStocksItem=item;otherStocksCharacter=character;otherStocksBank=bank;}
        if(otherStocks.Length==0)return;
        ImGui.TextDisabled(localization.Get("BankUi.OtherCharacters"));
        Tip("BankUi.OtherCharactersTip");
        foreach(var row in otherStocks)
            ImGui.TextUnformatted(localization.Format("BankUi.OtherCharacterLine",CharacterName(bank.Entries,row.CharacterId),row.Nq.ToString("N0"),row.Hq.ToString("N0"),row.ObservedAt.ToLocalTime().ToString("d")));
    }

    private static string CharacterName(IEnumerable<BankEntry> entries,ulong id)
        =>entries.FirstOrDefault(entry=>entry.Key.CharacterId==id && BankStorageScope.IsCharacterOwn(entry.Key.Kind) && entry.Name.Length>0)?.Name
            ?? $"…{id%10000:0000}";

    private void DrawCharactersTab()
    {
        ImGui.TextWrapped(localization.Get("CharactersUi.Help"));
        var state=bank.State;
        if(state.Loading){ImGui.TextDisabled(localization.Get("BankUi.Loading"));return;}
        if(state.Error is { } error)DrawDataError(error,"ServiceErrors.Bank");
        if(!ReferenceEquals(characterSummarySource,state))
        {
            characterSummaries=state.Entries.GroupBy(entry=>entry.Key.CharacterId).Select(group=>
            {
                var entries=group.ToArray();
                long Sum(BankStorageKind kind)=>entries.Where(entry=>entry.Key.Kind==kind).SelectMany(entry=>entry.Snapshot.Containers.Values).SelectMany(container=>container.Quantities.Values).Sum();
                var currency=entries.FirstOrDefault(entry=>entry.Key.Kind==BankStorageKind.Currency);
                long? gil=currency is null ? null:currency.Snapshot.Containers.Values.SelectMany(container=>container.Quantities).Where(pair=>pair.Key.Id.Value==1).Sum(pair=>pair.Value);
                // 0.767 (I10): the gil the retainers hold, from the copies that recorded it.
                var retainerGil=entries.Where(entry=>entry.Key.Kind==BankStorageKind.Retainer).SelectMany(entry=>entry.Snapshot.Containers.Values)
                    .Where(container=>container.Location.Container.Value==BankStorageScope.RetainerGil).ToArray();
                return new CharacterSummary(group.Key,CharacterName(entries,group.Key),entries.Max(entry=>entry.Snapshot.ObservedAt),gil,Sum(BankStorageKind.Bags),Sum(BankStorageKind.Crystals),
                    entries.Count(entry=>entry.Key.Kind==BankStorageKind.Retainer),entries.Length,
                    retainerGil.Length==0 ? null:retainerGil.SelectMany(container=>container.Quantities).Where(pair=>pair.Key.Id.Value==1).Sum(pair=>pair.Value));
            }).OrderByDescending(summary=>summary.LastSeen).ToArray();
            characterSummarySource=state;
        }
        if(characterSummaries.Length==0){ImGui.TextDisabled(localization.Get("CharactersUi.None"));return;}
        var current=bankBridge.State.CharacterId;
        if(characterEraseMessage is { } message)ImGui.TextColored(Accent,message);
        if(!ImGui.BeginTable("Characters",7,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH))return;
        try
        {
            foreach(var column in new[]{"Name","Seen","Gil","Bags","Crystals","Retainers"})ImGui.TableSetupColumn(localization.Get("CharactersUi.Column"+column));
            ImGui.TableSetupColumn("",ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableHeadersRow();
            foreach(var summary in characterSummaries)
            {
                ImGui.PushID(summary.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
                try
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    if(summary.Id==current)ImGui.TextColored(Success,localization.Format("CharactersUi.Current",summary.Name));else ImGui.TextUnformatted(summary.Name);
                    ImGui.TableNextColumn();ImGui.TextUnformatted(summary.LastSeen.ToLocalTime().ToString("g"));
                    ImGui.TableNextColumn();ImGui.TextUnformatted(summary.Gil is { } gil ? gil.ToString("N0"):"—");
                    if(summary.RetainerGil is { } held){ImGui.SameLine();ImGui.TextDisabled(localization.Format("CharactersUi.RetainerGil",held.ToString("N0")));}
                    ImGui.TableNextColumn();ImGui.TextUnformatted(summary.Items.ToString("N0"));
                    ImGui.TableNextColumn();ImGui.TextUnformatted(summary.Crystals.ToString("N0"));
                    ImGui.TableNextColumn();ImGui.TextUnformatted(summary.Retainers.ToString());
                    ImGui.TableNextColumn();
                    if(ImGui.SmallButton(localization.Get("CharactersUi.See"))){bankViewCharacter=summary.Id==current ? null:summary.Id;focusBankTab=true;}
                    Tip("CharactersUi.SeeTip");
                    ImGui.SameLine();
                    var armed=characterEraseArmed==summary.Id;
                    ImGui.BeginDisabled(bank.State.Importing);
                    try
                    {
                        if(ImGui.SmallButton(localization.Get(armed ? "CharactersUi.EraseConfirm":"CharactersUi.Erase")))
                        {
                            if(!armed)characterEraseArmed=summary.Id;
                            else
                            {
                                characterEraseArmed=0;
                                if(bankViewCharacter==summary.Id)bankViewCharacter=null;
                                var id=summary.Id;
                                _=bank.EraseCharacterAsync(id).ContinueWith(task=>characterEraseMessage=task.Result.Message,TaskScheduler.Default);
                            }
                        }
                    }
                    finally{ImGui.EndDisabled();}
                    Tip(summary.Id==current ? "CharactersUi.EraseCurrentTip":"CharactersUi.EraseTip");
                }
                finally{ImGui.PopID();}
            }
        }
        finally{ImGui.EndTable();}
        if(characterEraseArmed!=0 && !characterSummaries.Any(summary=>summary.Id==characterEraseArmed))characterEraseArmed=0;
        ImGui.TextDisabled(localization.Get("CharactersUi.Privacy"));
    }
}
