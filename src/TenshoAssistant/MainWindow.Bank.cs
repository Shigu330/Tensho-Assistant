using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Inventory;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// Storage › Bank and Imports (on the Storage page since 0.280): the bank browser — origin tabs (all, character, each saved storage),
// category / quality / search filters and the game's 5 × 7 bag grids; armoury chest, glamour dresser and armoire as lists of 35 per page
// (0.237) — the import buttons while a retainer or saddlebag window is open, the « open it once » notices, and the per-storage stock table of
// the item sheet. Sources: the live inventory and the saved Bank snapshots, copied by Inventory/BankInventoryBridge on the framework thread
// (on request, or automatically with AutoBankCapture); glamour dresser and armoire are known once opened in the game (0.236). Views are
// rebuilt only when their inputs change.
internal sealed partial class MainWindow
{
    // 0.322: storage names translated from their kind (Inventory/StorageNames.cs).
    private string StorageRowName(StorageHolding row)=>StorageNames.Of(localization,row);
    private string BankEntryName(BankEntry entry)=>StorageNames.Of(localization,entry.Key.Kind,entry.Name);
    private readonly record struct BankItemRow(uint ItemId, long Nq, long Hq);
    private readonly record struct BankSourceViewKey(ulong Character,InventorySnapshot? Live,BankState Bank,ItemCatalog? Items,bool Personal,BankKey? Origin);
    private readonly record struct BankFrameViewKey(BankSourceViewKey Source,string Category,int Quality,string Query,int LanguageRevision,TenshoAssistant.Caching.CacheRetention Retention,DateTimeOffset? NextExpiry,DateTime Today);
    private BankState? bankEntriesSource;
    private ulong bankEntriesCharacter;
    private BankEntry[] bankEntriesView=[];
    private BankSourceViewKey? bankSourceViewKey;
    private Dictionary<uint,(long Nq,long Hq)> bankSourceRows=[];
    private string[] bankSourceCategories=[];
    private InventoryContainerSnapshot[] bankSourcePersonal=[];
    // 0.329: notice key when the character's four bags are not all read (no live inventory, or a bag still Unknown), else null.
    private string? bankPersonalNotice;
    private BankFrameViewKey? bankFrameViewKey;
    private IReadOnlyList<(string Name,InventoryContainerSnapshot Container)> bankFrameGroups=[];
    private IReadOnlyList<string> bankFrameErrors=[];
    private readonly Dictionary<InventoryContainerSnapshot,BankItemRow?[]> bankFrameRows=new(ReferenceEqualityComparer.Instance);
    private bool bankHasVisibleItems;

    private static readonly StorageCategory[] StorageGroups =
        [StorageCategory.PlayerBags, StorageCategory.Armoury, StorageCategory.GlamourDresser, StorageCategory.Armoire, StorageCategory.Chocobo, StorageCategory.Retainer, StorageCategory.FreeCompany];

    private void DrawStorageGroups(ItemHoldingsSummary summary)
    {
        foreach (var category in StorageGroups)
        {
            var rows = summary.Rows.Where(row => row.Category == category).OrderBy(StorageRowName, StringComparer.CurrentCulture).ToArray();
            if (rows.Length == 0) continue;
            var label = category switch
            {
                StorageCategory.PlayerBags => localization.Get("BankUi.Character"),
                StorageCategory.Chocobo => localization.Get("BankUi.Chocobo"),
                StorageCategory.Armoury => localization.Get("BankUi.Armoury"),
                StorageCategory.GlamourDresser => localization.Get("BankUi.GlamourDresser"),
                StorageCategory.Armoire => localization.Get("BankUi.Armoire"),
                StorageCategory.FreeCompany => localization.Get("BankUi.FreeCompanyChest"),
                _ => localization.Get("BankUi.Retainers"),
            };
            if (!TenshoAssistant.UI.UiComponents.Header(label, ImGuiTreeNodeFlags.DefaultOpen)) continue;
            if (!ImGui.BeginTable($"StorageGroup{category}", 5,
                    ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.RowBg)) continue;
            try
            {
                ImGui.TableSetupColumn(localization.Get("BankUi.Storage"), ImGuiTableColumnFlags.WidthStretch, 2.2f);
                ImGui.TableSetupColumn("NQ", ImGuiTableColumnFlags.WidthStretch, 0.6f);
                ImGui.TableSetupColumn("HQ", ImGuiTableColumnFlags.WidthStretch, 0.6f);
                ImGui.TableSetupColumn(localization.Get("BankUi.Total"), ImGuiTableColumnFlags.WidthStretch, 0.8f);
                ImGui.TableSetupColumn(localization.Get("BankUi.StateDate"), ImGuiTableColumnFlags.WidthStretch, 1.8f);
                ImGui.TableHeadersRow();
                foreach (var row in rows)
                {
                    string Count(long count) => row.Coverage == InventoryCoverage.Unknown ? "—"
                        : row.Coverage == InventoryCoverage.Partial ? $"≥ {(count).ToString("N0",Numbers)}" : count.ToString("N0",Numbers);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn(); ImGui.TextWrapped(StorageRowName(row));
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(Count(row.Nq));
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(Count(row.Hq));
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(Count(checked(row.Nq + row.Hq)));
                    ImGui.TableNextColumn();
                    if (row.Coverage == InventoryCoverage.Unknown) ImGui.TextWrapped(localization.Get("BankUi.Unknown"));
                    else
                    {
                        var expired=row.IsCached && !TenshoAssistant.Caching.CacheAge.Fresh(row.ObservedAt,DateTimeOffset.UtcNow,configuration.Cache.Duration(TenshoAssistant.Caching.CacheArea.ImportedStock));
                        ImGui.TextColored(row.IsCached ? Accent : Success, row.IsCached ? expired ? localization.Get("BankUi.Expired"):localization.Get("BankUi.Manual") : localization.Get("BankUi.Live"));
                        if (row.ObservedAt is { } observed) ImGui.TextWrapped(observed.ToLocalTime().ToString("g"));
                    }
                }
            }
            finally { ImGui.EndTable(); }
            ImGui.Spacing();
        }
    }

    private BankKey? bankForgetArmed;private volatile string? bankForgetMessage;
    private void DrawImportManager()
    {
        if (!TenshoAssistant.UI.UiComponents.Header(localization.Get("BankUi.Imports"))) return;
        var state = bank.State;
        var menus = bankBridge.State;
        ImGui.TextWrapped(localization.Get("BankUi.ImportHelp"));
        DrawMenuImport(menus.Retainer);
        DrawMenuImport(menus.Saddlebag);
        if (menus.Premium?.Loaded == true) DrawMenuImport(menus.Premium);
        if (menus.Retainer is null && menus.Saddlebag is null)
            ImGui.TextDisabled(localization.Get("BankUi.NoOpen"));
        if (menus.Message is { } message) DrawServiceMessage(message,"ServiceErrors.Bank");
        if (menus.ReadsUnavailable) ImGui.TextColored(Accent,localization.Get("ServiceStatus.StorageReadUnavailable"));
        if (state.Loading) EmptyState("BankUi.Loading");
        if (state.Error is { } error) DrawDataError(error,"ServiceErrors.Bank");
        if (menus.CharacterId == 0)
        {
            ImGui.TextWrapped(localization.Get("BankUi.Login"));
            return;
        }
        var entries = state.Entries.Where(entry => entry.Key.CharacterId == menus.CharacterId)
            .OrderBy(entry => entry.Key.Kind).ThenBy(entry => entry.Name, StringComparer.Ordinal).ToArray();
        DrawFirstOpenNotices(entries,menus);
        ImGui.TextUnformatted(localization.Format("BankUi.Count",entries.Length));
        foreach (var entry in entries)
        {
            ImGui.BulletText(BankEntryName(entry));
            ImGui.TextDisabled(localization.Format("BankUi.Last",entry.Snapshot.ObservedAt.ToLocalTime().ToString("g")));
            DrawCacheStamp(TenshoAssistant.Caching.CacheArea.ImportedStock,entry.Snapshot.ObservedAt,true);
            // 0.576 (I1): a retainer the bell no longer lists is said, never erased by itself; two clicks erase a read.
            if(entry.Key.Kind==BankStorageKind.Retainer && menus.KnownRetainers is { } listed && listed.All(known=>known.Id!=entry.Key.StorageId))
                ImGui.TextColored(Accent,localization.Get("BankUi.RetainerGone"));
            var armed=bankForgetArmed==entry.Key;
            if(ImGui.SmallButton(localization.Get(armed ? "BankUi.ForgetConfirm":"BankUi.Forget")+$"##BankForget{entry.Key.Kind}{entry.Key.StorageId}"))
            {
                if(!armed)bankForgetArmed=entry.Key;
                else
                {
                    bankForgetArmed=null;
                    _=bank.ForgetAsync(entry.Key).ContinueWith(task=>bankForgetMessage=task.Result.Message,TaskScheduler.Default);
                }
            }
            Tip("BankUi.ForgetTip");
        }
        if(bankForgetMessage is { } forgetMessage)DrawServiceMessage(forgetMessage,"ServiceErrors.Bank");
        ImGui.TextWrapped(localization.Get("BankUi.Scope"));
    }

    private void DrawBankBrowser()
    {
        var bankSnapshot=bank.State;
        ImGui.TextColored(Accent, localization.Get("BankUi.Title"));
        ImGui.TextDisabled(localization.Get("BankUi.Subtitle"));
        ImGui.Separator();

        if (bankSnapshot.Loading) ImGui.TextDisabled(localization.Get("BankUi.Loading"));
        if (bankSnapshot.Error is { } bankError) DrawDataError(bankError,"ServiceErrors.Bank");
        ImGui.TextDisabled(bankPersonalFilter
            ? localization.Get("BankUi.PersonalLive")
            : localization.Get("BankUi.OtherManual"));

        // 0.345 (g, I-03): the Bank shows the logged-in character, or another one chosen here (its saved storages, bags included).
        var loggedIn = bankBridge.State.CharacterId;
        if(bankViewCharacter is { } chosen && !bankSnapshot.Entries.Any(entry=>entry.Key.CharacterId==chosen))bankViewCharacter=null;
        var characterId = bankViewCharacter ?? loggedIn;
        var others=bankSnapshot.Entries.Select(entry=>entry.Key.CharacterId).Where(id=>id!=loggedIn).Distinct().ToArray();
        if(others.Length>0)
        {
            ImGui.SetNextItemWidth(260);
            var shownName=characterId==loggedIn && loggedIn!=0 ? localization.Get("BankUi.LoggedIn"):CharacterName(bankSnapshot.Entries,characterId);
            if(ImGui.BeginCombo(localization.Get("BankUi.CharacterChoice")+"##BankCharacter",shownName))
            {
                try
                {
                    if(loggedIn!=0 && ImGui.Selectable(localization.Get("BankUi.LoggedIn"),bankViewCharacter is null))bankViewCharacter=null;
                    foreach(var id in others)
                        if(ImGui.Selectable(CharacterName(bankSnapshot.Entries,id)+$"##BankCharacter{id}",bankViewCharacter==id))bankViewCharacter=id;
                }
                finally{ImGui.EndCombo();}
            }
            Tip("BankUi.CharacterChoiceTip");
        }
        if (characterId == 0)
        {
            ImGui.TextWrapped(localization.Get("BankUi.LoginBank"));
            return;
        }
        var live=characterId==loggedIn;
        if(!live)bankPersonalFilter=false;

        if(!ReferenceEquals(bankEntriesSource,bankSnapshot) || bankEntriesCharacter!=characterId)
        {
            // The logged-in character's own bags, crystals and currencies are shown live, not from their saved copy.
            bankEntriesView=bankSnapshot.Entries.Where(entry=>entry.Key.CharacterId==characterId && (!live || !BankStorageScope.IsCharacterOwn(entry.Key.Kind)))
                .OrderBy(entry=>entry.Key.Kind).ThenBy(entry=>entry.Name,StringComparer.Ordinal).ToArray();
            bankEntriesSource=bankSnapshot;bankEntriesCharacter=characterId;
        }
        var entries=bankEntriesView;
        if (ImGui.BeginTabBar("BankOriginTabs"))
        {
            try
            {
                if (ImGui.BeginTabItem(localization.Get("BankUi.Global")))
                {
                    bankPersonalFilter = false;
                    bankOriginFilter = null;
                    ImGui.EndTabItem();
                }
                if (live && ImGui.BeginTabItem(localization.Get("BankUi.Character")))
                {
                    bankPersonalFilter = true;
                    bankOriginFilter = null;
                    ImGui.EndTabItem();
                }
                for (var index = 0; index < entries.Length; index++)
                {
                    var entry = entries[index];
                    var label = BankEntryName(entry);
                    if (ImGui.BeginTabItem($"{label}##BankOrigin{index}"))
                    {
                        bankPersonalFilter = false;
                        bankOriginFilter = entry.Key;
                        ImGui.EndTabItem();
                    }
                }
            }
            finally { ImGui.EndTabBar(); }
        }
        var selectedEntries = bankOriginFilter is { } origin
            ? entries.Where(entry => entry.Key == origin).ToArray()
            : entries;

        var sourceKey=new BankSourceViewKey(characterId,live ? inventory.Current:null,bankSnapshot,itemCatalog.State.Catalog,bankPersonalFilter,bankOriginFilter);
        if(bankSourceViewKey!=sourceKey)
        {
            var personal=sourceKey.Origin is null && sourceKey.Live is not null
                ? sourceKey.Live.Containers.Values.Where(container=>container.Location.Owner.Value==characterId
                    && container.Location.Container.Value is >=0 and <=3).OrderBy(container=>container.Location.Container.Value).ToArray()
                : [];
            var sources=personal.Concat(bankPersonalFilter ? [] : selectedEntries.SelectMany(entry=>entry.Snapshot.Containers.Values));
            var aggregated=new Dictionary<uint,(long Nq,long Hq)>();
            foreach(var source in sources)
            foreach(var quantity in source.Quantities)
            {
                var row=aggregated.GetValueOrDefault(quantity.Key.Id.Value);
                if(quantity.Key.IsHighQuality)row.Hq=checked(row.Hq+quantity.Value);else row.Nq=checked(row.Nq+quantity.Value);
                aggregated[quantity.Key.Id.Value]=row;
            }
            bankSourceRows=aggregated;
            bankSourcePersonal=personal;
            var read=personal.Count(container=>container.Coverage!=InventoryCoverage.Unknown);
            bankPersonalNotice=!live || sourceKey.Origin is not null || (personal.Length==4 && read==4) ? null : read==0 ? "BankUi.Unavailable":"BankUi.UnreadBags";
            bankSourceCategories=aggregated.Keys.Select(id=>sourceKey.Items?.Find(id)?.UiCategoryName)
                .Where(name=>!string.IsNullOrWhiteSpace(name)).Select(name=>name!).Distinct(StringComparer.Ordinal)
                .OrderBy(name=>name,StringComparer.Ordinal).ToArray();
            bankSourceViewKey=sourceKey;
        }
        var nativeCategories=bankSourceCategories;
        ImGui.SetNextItemWidth(180);
        var categoryLabel = string.IsNullOrWhiteSpace(bankCategoryFilter) ? localization.Get("BankUi.AllCategories") : bankCategoryFilter;
        if (ImGui.BeginCombo(localization.Get("BankUi.Category")+"##BankCategory", categoryLabel))
        {
            try
            {
                if (ImGui.Selectable(localization.Get("BankUi.AllCategories"), string.IsNullOrWhiteSpace(bankCategoryFilter)))
                    bankCategoryFilter = string.Empty;
                foreach (var category in nativeCategories)
                    if (ImGui.Selectable(category, bankCategoryFilter == category)) bankCategoryFilter = category;
            }
            finally { ImGui.EndCombo(); }
        }
        Tip("BankUi.CategoryTip");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(140);
        var qualityLabel = bankQualityFilter switch { 1 => localization.Get("BankUi.Nq"), 2 => localization.Get("BankUi.Hq"), _ => localization.Get("BankUi.Both") };
        if (ImGui.BeginCombo(localization.Get("BankUi.Quality")+"##BankQuality", qualityLabel))
        {
            try
            {
                foreach (var option in new[] { (0, localization.Get("BankUi.Both")), (1, localization.Get("BankUi.Nq")), (2, localization.Get("BankUi.Hq")) })
                    if (ImGui.Selectable(option.Item2, bankQualityFilter == option.Item1)) bankQualityFilter = option.Item1;
            }
            finally { ImGui.EndCombo(); }
        }
        Tip("BankUi.QualityTip");
        ImGui.SameLine();
        if (ImGui.SmallButton(localization.Get("BankUi.Reset")+"##BankFilters"))
        {
            bankCategoryFilter = string.Empty;
            bankQualityFilter = 0;
            bankQuery = string.Empty;
        }
        Tip("BankUi.ResetTip");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##BankQuery", localization.Get("BankUi.Search"), ref bankQuery, 128);
        Tip("BankUi.SearchTip");
        BoolSetting("BankUi.ListView","BankUi.ListViewTip",()=>configuration.BankListView,value=>configuration.BankListView=value,c=>c.BankListView);

        var now=DateTimeOffset.UtcNow;
        // X-76 (0.955): no longer a per-second field: the frames are rebuilt when a selected snapshot turns expired (or fresh), and when the
        // local day changes (time or date in the title), so the « expired » mark still comes on time.
        var nextExpiry=TenshoAssistant.Caching.CacheAge.NextChange(selectedEntries.Select(entry=>(DateTimeOffset?)entry.Snapshot.ObservedAt),now,
            configuration.Cache.Duration(TenshoAssistant.Caching.CacheArea.ImportedStock));
        var frameKey=new BankFrameViewKey(sourceKey,bankCategoryFilter,bankQualityFilter,bankQuery,localization.Revision,
            configuration.Cache.Get(TenshoAssistant.Caching.CacheArea.ImportedStock),nextExpiry,now.ToLocalTime().Date);
        if(bankFrameViewKey!=frameKey)
        {
            bankHasVisibleItems=bankSourceRows.Any(pair=>MatchesBankFilters(new(pair.Key,pair.Value.Nq,pair.Value.Hq)));
            var groups=new List<(string Name,InventoryContainerSnapshot Container)>();
            var errors=new List<string>();
            if(bankHasVisibleItems)
            {
                // 0.329: no grid while no bag is read (they would look like empty bags); one unread bag among read ones keeps the visual
                // pages, which do not match physical bags.
                if(bankOriginFilter is null && bankSourcePersonal.Any(container=>container.Coverage!=InventoryCoverage.Unknown))
                    AddDisplayFrames(localization.Get("BankUi.Character"),bankSourcePersonal,groups,errors,live:true);
                if(!bankPersonalFilter)
                    foreach(var entry in selectedEntries)
                    {
                        var expired=!TenshoAssistant.Caching.CacheAge.Fresh(entry.Snapshot.ObservedAt,now,configuration.Cache.Duration(TenshoAssistant.Caching.CacheArea.ImportedStock));
                        // 0.329: the date too when the snapshot is not from today (« Imported stock », CacheUi.Stocks, keeps one for a day by default).
                        var observed=entry.Snapshot.ObservedAt.ToLocalTime();
                        AddDisplayFrames(localization.Format("BankUi.Frame",BankEntryName(entry),observed.ToString(observed.Date==now.ToLocalTime().Date ? "t":"g"),expired ? localization.Get("BankUi.ExpiredSuffix"):""),entry.Snapshot.Containers.Values,groups,errors);
                    }
            }
            bankFrameGroups=groups;bankFrameErrors=errors;bankFrameRows.Clear();bankFrameViewKey=frameKey;
        }
        // 0.329: an unread inventory and an empty tab are said as such; « no match » only when a filter hides everything.
        if(bankPersonalNotice is { } notice)ImGui.TextDisabled(localization.Get(notice));
        if(!bankHasVisibleItems)
        {
            if(BankFiltering)ImGui.TextDisabled(localization.Get("BankUi.Empty"));
            else if(bankPersonalNotice is null)ImGui.TextDisabled(localization.Get("BankUi.EmptyStorage"));
            return;
        }
        if(configuration.BankListView){DrawBankList(sourceKey,selectedEntries);return;}
        foreach(var error in bankFrameErrors)ImGui.TextWrapped(error);
        DrawInventoryFrames(bankFrameGroups);
    }

    // live: the character's bags 0–3 read now (they cannot be reimported).
    private void AddDisplayFrames(string name, IEnumerable<InventoryContainerSnapshot> containers,
        List<(string Name, InventoryContainerSnapshot Container)> frames,List<string> errors,bool live=false)
    {
        var copied = containers.ToArray();
        if (copied.Length == 0) return;
        try
        {
            var pages = InventoryDisplayLayout.Build(copied);
            for (var page = 0; page < pages.Count; page++)
            {
                var positioned = pages[page].Select((stack, index) => (stack, index))
                    .Where(pair => pair.stack.HasValue)
                    .Select(pair => pair.stack!.Value with { Slot = pair.index });
                frames.Add((localization.Format("BankUi.Bag",name,page+1), new InventoryContainerSnapshot(
                    copied[0].Location, copied.All(c => c.Coverage == InventoryCoverage.Complete)
                        ? InventoryCoverage.Complete : InventoryCoverage.Partial, positioned)));
            }
        }
        catch (InvalidOperationException) when (copied.All(container => InventoryDisplayLayout.HasNoBagLayout(container.Location.Container.Value)))
        {
            // Armoury chest, glamour dresser, armoire (0.237) and company chest (0.463) have no bag layout: their pieces in order, 35 per page (5 × 7).
            var stacks = copied.OrderBy(container => container.Location.Container.Value).SelectMany(container => container.Stacks.OrderBy(stack => stack.Slot)).ToArray();
            var coverage = copied.All(c => c.Coverage == InventoryCoverage.Complete) ? InventoryCoverage.Complete : InventoryCoverage.Partial;
            for (var page = 0; page * 35 < stacks.Length; page++)
                frames.Add((localization.Format("BankUi.Bag", name, page + 1), new InventoryContainerSnapshot(copied[0].Location, coverage,
                    stacks.Skip(page * 35).Take(35).Select((stack, index) => stack with { Slot = index, DisplayIndex = -1 }))));
        }
        catch (InvalidOperationException) when (live && copied.All(container => container.Location.Container.Value is >= 0 and <= 3))
        {
            // 0.329: the game's visual order of the live bags can be unreadable (a game sort running, a game update): their known
            // contents are shown anyway, one page per read bag in its own slot order (35 slots), with a note.
            errors.Add(localization.Format("BankUi.PhysicalOrder", name));
            foreach (var container in copied.Where(bag => bag.Coverage != InventoryCoverage.Unknown))
                frames.Add((localization.Format("BankUi.Bag", name, container.Location.Container.Value + 1), container));
        }
        catch (InvalidOperationException)
        {
            errors.Add(localization.Format("BankUi.Reimport",name));
        }
    }

    private void DrawInventoryFrames(IReadOnlyList<(string Name, InventoryContainerSnapshot Container)> groups)
    {
        if (groups.Count == 0)
        {
            if (bankFrameErrors.Count == 0 && bankPersonalNotice is null) ImGui.TextDisabled(localization.Get("BankUi.Unavailable"));
            return;
        }
        const int columns = 5;
        const int rows = 7;
        const float slotSize = 42;
        const float frameWidth = columns * 46 + 12;
        const float frameHeight = rows * 46 + 48;
        var filtering = BankFiltering;
        var displayedFrames = 0;
        var framesPerRow=Math.Max(1,(int)((ImGui.GetContentRegionAvail().X+ImGui.GetStyle().ItemSpacing.X)/(frameWidth+ImGui.GetStyle().ItemSpacing.X)));
        for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            var group = groups[groupIndex];
            var items = GetContainerRows(group.Container, rows * columns);
            if (filtering && !items.Any(item => item is not null)) continue;
            // Keep the native 5 x 7 bag layout and wrap to the available window width.
            if (displayedFrames % framesPerRow != 0) ImGui.SameLine();
            displayedFrames++;
            if (!ImGui.BeginChild($"BankFrame{groupIndex}", new Vector2(frameWidth, frameHeight), true,
                    ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.EndChild();
                continue;
            }
            try
            {
                ImGui.TextColored(Accent, group.Name);
                ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(2, 2));
                if (ImGui.BeginTable($"BankFrameGrid{groupIndex}", columns, ImGuiTableFlags.SizingFixedFit))
                {
                    try
                    {
                        for (var rowIndex = 0; rowIndex < rows; rowIndex++)
                        {
                            for (var columnIndex = 0; columnIndex < columns; columnIndex++)
                            {
                                ImGui.TableNextColumn();
                                var itemIndex = rowIndex * columns + columnIndex;
                                ImGui.PushID(itemIndex);
                                try
                                {
                                if (itemIndex < items.Length && items[itemIndex] is { } item)
                                    DrawBankSlot(item, slotSize);
                                else
                                {
                                    var corner = ImGui.GetCursorScreenPos();
                                    ImGui.GetWindowDrawList().AddRectFilled(corner, corner + new Vector2(slotSize),
                                        ImGui.GetColorU32(ImGuiCol.FrameBg), 4);
                                    ImGui.Dummy(new Vector2(slotSize));
                                }
                                }
                                finally { ImGui.PopID(); }
                            }
                        }
                    }
                    finally { ImGui.EndTable(); }
                }
                ImGui.PopStyleVar();
            }
            finally { ImGui.EndChild(); }
        }
        if (filtering && displayedFrames == 0) ImGui.TextDisabled(localization.Get("BankUi.NoBags"));
    }

    private BankItemRow?[] GetContainerRows(InventoryContainerSnapshot container, int capacity)
    {
        if(bankFrameRows.TryGetValue(container,out var cached) && cached.Length==capacity)return cached;
        var result = new BankItemRow?[capacity];
        foreach (var stack in container.Stacks)
        {
            var row = new BankItemRow(stack.Item.Id.Value,
                stack.Item.IsHighQuality ? 0 : stack.Quantity,
                stack.Item.IsHighQuality ? stack.Quantity : 0);
            if (stack.Slot >= 0 && stack.Slot < capacity && MatchesBankFilters(row)) result[stack.Slot] = row;
        }
        bankFrameRows[container]=result;
        return result;
    }

    private bool BankFiltering=>!string.IsNullOrWhiteSpace(bankCategoryFilter) || bankQualityFilter!=0 || !string.IsNullOrWhiteSpace(bankQuery);

    private bool MatchesBankFilters(BankItemRow row)
    {
        var item = itemCatalog.State.Catalog?.Find(row.ItemId);
        var categoryMatch = string.IsNullOrWhiteSpace(bankCategoryFilter)
            || string.Equals(item?.UiCategoryName, bankCategoryFilter, StringComparison.Ordinal);
        var qualityMatch = bankQualityFilter switch
        {
            1 => row.Nq > 0,
            2 => row.Hq > 0,
            _ => true,
        };
        var query = bankQuery.Trim();
        return categoryMatch && qualityMatch && (string.IsNullOrWhiteSpace(query)
            || (item?.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
            || row.ItemId.ToString().Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    // 0.329: select false for the sort preview, whose own click only opens its zone menu (it must not replace the selected item).
    private void DrawBankSlot(BankItemRow row, float slotSize, bool select = true)
    {
        var item = itemCatalog.State.Catalog?.Find(row.ItemId);
        ImGui.PushID((int)row.ItemId);
        try
        {
            var slotMin = ImGui.GetCursorScreenPos();
            var slotMax = slotMin + new Vector2(slotSize);
            var drawList = ImGui.GetWindowDrawList();
            drawList.AddRectFilled(slotMin, slotMax, ImGui.GetColorU32(itemId == (int)row.ItemId ? ImGuiCol.FrameBgActive : ImGuiCol.FrameBg), 4);
            ImGui.SetCursorScreenPos(slotMin + new Vector2(3));
            DrawItemIcon(item, new Vector2(slotSize - 6));
            if (select && ImGui.IsItemClicked()) SelectItem(row.ItemId);
            var count = checked(row.Nq + row.Hq).ToString("N0",Numbers);
            var countSize = ImGui.CalcTextSize(count);
            var countPosition = new Vector2(slotMax.X - countSize.X - 3, slotMax.Y - countSize.Y - 2);
            drawList.AddRectFilled(countPosition - new Vector2(2, 1), slotMax - new Vector2(1, 1), ImGui.GetColorU32(ImGuiCol.PopupBg));
            drawList.AddText(countPosition, ImGui.GetColorU32(ImGuiCol.Text), count);
        }
        finally { ImGui.PopID(); }
    }

    // What still needs one opening in the game before its contents are known (0.167).
    private void DrawFirstOpenNotices(IReadOnlyList<BankEntry> entries,BankMenuState menus)
    {
        ImGui.TextWrapped(localization.Get(configuration.AutoBankCapture ? "BankUi.AutoOn":"BankUi.AutoOff"));
        if(!entries.Any(entry=>entry.Key.Kind==BankStorageKind.Saddlebag))ImGui.TextColored(Accent,localization.Get("BankUi.OpenSaddlebag"));
        if(!entries.Any(entry=>entry.Key.Kind==BankStorageKind.GlamourDresser))ImGui.TextColored(Accent,localization.Get("BankUi.OpenDresser"));
        if(!entries.Any(entry=>entry.Key.Kind==BankStorageKind.Armoire))ImGui.TextColored(Accent,localization.Get("BankUi.OpenArmoire"));
        if(menus.KnownRetainers is { } known)
        {
            foreach(var (id,name) in known)
                if(!entries.Any(entry=>entry.Key.Kind==BankStorageKind.Retainer && entry.Key.StorageId==id))
                    ImGui.TextColored(Accent,localization.Format("BankUi.OpenRetainer",name));
        }
        else if(!entries.Any(entry=>entry.Key.Kind==BankStorageKind.Retainer))ImGui.TextColored(Accent,localization.Get("BankUi.OpenBell"));
    }

    private void DrawMenuImport(BankMenuContext? context)
    {
        if (context is null) return;
        var disabled = !context.Loaded || bank.State.Loading || bank.State.Importing || bank.State.Error is not null;
        ImGui.BeginDisabled(disabled);
        try
        {
            if (ImGui.Button(localization.Format("BankUi.Refresh",StorageNames.Of(localization,context.Key.Kind,context.Name))+$"##{context.Key.Kind}", new Vector2(-1, 28)))
                bankBridge.RequestImport(context.Key);
        }
        finally { ImGui.EndDisabled(); }
        if (!context.Loaded) ImGui.TextDisabled(localization.Get("BankUi.Waiting"));
    }
}
