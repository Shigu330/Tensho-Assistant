using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Monsters;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private uint selectedMonsterMap;
    private WikiMonsterSources? wikiMonsterSources;
    internal void ConfigureWikiSources(WikiMonsterSources service)=>wikiMonsterSources=service;
    private void DrawWikiMonsterSources(uint id)
    {
        // LuminaSupplemental first (0.190): monsters dropping the item, from MobDrop with names from the game sheet BNpcName.
        lumina?.Request();
        if(lumina?.State.Data?.MonstersOf(id) is { Count:>0 } mobs && ImGui.CollapsingHeader(localization.Get("MonsterUi.Lumina")+"###LuminaMonsters",ImGuiTreeNodeFlags.DefaultOpen))
        {
            foreach(var mob in mobs)ImGui.BulletText(mob);
            ImGui.TextDisabled(localization.Get("MonsterUi.LuminaHelp"));
        }
        if(wikiMonsterSources is not { } wiki)return;
        wiki.Select(id);
        if(!ImGui.CollapsingHeader(localization.Get("WikiMonsters.Title")+"###WikiMonsters",ImGuiTreeNodeFlags.DefaultOpen))return;
        ImGui.TextWrapped(localization.Get("WikiMonsters.Help"));
        var view=wiki.State;
        ImGui.BeginDisabled(view.Loading);
        try
        {
            if(ImGui.SmallButton(localization.Get("WikiMonsters.Fetch")))_=wiki.RequestAsync();
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get("WikiMonsters.Refresh")))_=wiki.RequestAsync(true);
        }
        finally{ImGui.EndDisabled();}
        if(view.Loading)ImGui.TextDisabled(localization.Get("WikiMonsters.Loading"));
        if(view.Error is { } error)
        {ImGui.TextWrapped(localization.Get("WikiMonsters.Error"));if(developerMode)ImGui.TextWrapped(error);}
        if(view.Document is not { } document)return;
        ImGui.TextWrapped(document.Url);
        DrawCacheStamp(TenshoAssistant.Caching.CacheArea.Sources,document.FetchedAt);
        if(document.Monsters.Count==0)ImGui.TextWrapped(localization.Get("WikiMonsters.Empty"));
        foreach(var row in document.Monsters)
        {ImGui.TextWrapped($"{row.Name} · {row.Level} · {row.Location}");}
    }
    private void DrawMonsterLoot()
    {
        Heading(localization.Get("MonsterUi.Title"),localization.Get("MonsterUi.Subtitle"));
        if (ImGui.CollapsingHeader(localization.Get("MonsterUi.Choose")+"##Monster",itemId <= 0 ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None))
            DrawItemSearch(160);
        monsterLoot.SelectItem(itemId > 0 ? (uint)itemId : 0);
        var state = monsterLoot.State;
        if (state.ItemId == 0) { ImGui.TextWrapped(localization.Get("MonsterUi.Help")); return; }
        DrawItemIcon(SelectedItem,new Vector2(40)); ImGui.SameLine();
        ImGui.TextColored(Accent,SelectedItem?.Name ?? localization.Format("MonsterUi.Item",state.ItemId));
        // The wiki is the primary source; Garland stays as a folded complement for monster pages and map links.
        DrawWikiMonsterSources(state.ItemId);
        if (!ImGui.CollapsingHeader(localization.Get("MonsterUi.GarlandSection")+"###GarlandMonsters")) return;
        if (state.Loading) { if (ImGui.Button(localization.Get("MonsterUi.Cancel")+"##Monster")) monsterLoot.Cancel(); ImGui.SameLine(); ImGui.TextDisabled(localization.Get("MonsterUi.Loading")); }
        else
        {
            if (ImGui.Button(localization.Get("MonsterUi.Fetch"))) _ = monsterLoot.RequestItemAsync();
            if (state.Item is not null)
            { ImGui.SameLine(); if (ImGui.SmallButton(localization.Get("MonsterUi.Refresh")+"##Monster")) _ = monsterLoot.RequestItemAsync(true); }
        }
        if (state.Error is { } error) { ImGui.TextWrapped(localization.Get("MonsterUi.Error")); if(developerMode)ImGui.TextWrapped(error); }
        ImGui.TextWrapped(localization.Get("MonsterUi.Source"));
        if (state.Item is not { } itemDocument) return;
        ImGui.TextDisabled(localization.Format("MonsterSource.Retrieved",itemDocument.FetchedAt.ToLocalTime().ToString("g")));
        ImGui.TextDisabled(localization.Get("MonsterSource.NoSourceDate"));
        DrawCacheStamp(TenshoAssistant.Caching.CacheArea.Sources,itemDocument.FetchedAt);
        if (!itemDocument.HasDrops) { ImGui.TextWrapped(localization.Get("MonsterSource.MissingDrops")); return; }
        if (itemDocument.Monsters.Count == 0) { ImGui.TextDisabled(localization.Get("MonsterSource.EmptyDrops")); return; }
        ImGui.TextDisabled(localization.Format("MonsterUi.Count",itemDocument.Monsters.Count));
        var height = Math.Clamp(itemDocument.Monsters.Count*30+30,90,210);
        var showList = ImGui.BeginChild("MonsterReferences",new Vector2(0,height),true);
        try
        {
            if (showList && ImGui.BeginTable("MonsterReferencesTable",3,ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                try
                {
                    ImGui.TableSetupColumn(localization.Get("MonsterUi.Monster"),ImGuiTableColumnFlags.WidthStretch,2);
                    ImGui.TableSetupColumn(localization.Get("MonsterUi.Level")); ImGui.TableSetupColumn(localization.Get("MonsterUi.Zone")); ImGui.TableHeadersRow();
                    var clipper = ImGui.ImGuiListClipper();
                    try
                    {
                        clipper.Begin(itemDocument.Monsters.Count);
                        while (clipper.Step())
                        for (var index=clipper.DisplayStart; index<clipper.DisplayEnd; index++)
                        {
                            var monster = itemDocument.Monsters[index];
                            ImGui.TableNextRow(); ImGui.TableNextColumn();
                            if (ImGui.Selectable($"{(monster.Name.Length == 0 ? localization.Format("MonsterUi.UnnamedMonster",monster.Id) : monster.Name)}##Mob{monster.Id}",state.MonsterId == monster.Id))
                            { monsterLoot.SelectMonster(monster.Id); selectedMonsterMap=0; }
                            if (developerMode && ImGui.IsItemHovered()) ImGui.SetTooltip(localization.Format("Developer.SourceReference",monster.Id));
                            ImGui.TableNextColumn(); ImGui.TextUnformatted(monster.Level.Length == 0 ? localization.Get("MonsterUi.Missing") : monster.Level);
                            ImGui.TableNextColumn(); ImGui.TextUnformatted(MonsterZone(monster.PlaceNameId));
                        }
                    }
                    finally { clipper.End(); clipper.Destroy(); }
                }
                finally { ImGui.EndTable(); }
            }
        }
        finally { ImGui.EndChild(); }
        if (state.MonsterId == 0) return;
        ImGui.Separator();
        var selected = itemDocument.Monsters.FirstOrDefault(monster => monster.Id == state.MonsterId);
        ImGui.TextColored(Accent,selected?.Name is { Length: >0 } name ? name : localization.Format("MonsterUi.UnnamedMonster",state.MonsterId));
        ImGui.BeginDisabled(state.Loading);
        try
        {
            if (ImGui.Button(localization.Get("MonsterUi.FetchMonster"))) _ = monsterLoot.RequestMonsterAsync();
            if (state.Monster is not null)
            { ImGui.SameLine(); if (ImGui.SmallButton(localization.Get("MonsterUi.RefreshMonster")+"##Monster")) _ = monsterLoot.RequestMonsterAsync(true); }
        }
        finally { ImGui.EndDisabled(); }
        if (state.Monster is not { } detail) return;
        ImGui.TextDisabled(localization.Format("MonsterUi.Fetched",detail.FetchedAt.ToLocalTime().ToString("g"))); if(developerMode)ImGui.TextDisabled($"ID {detail.Id}");
        DrawCacheStamp(TenshoAssistant.Caching.CacheArea.Sources,detail.FetchedAt);
        ImGui.TextUnformatted(localization.Format("MonsterUi.Description",detail.Name,detail.Level.Length == 0 ? localization.Get("MonsterUi.Missing") : detail.Level));
        ImGui.TextUnformatted(MonsterZone(detail.PlaceNameId));
        if (detail.X is { } x && detail.Y is { } y) ImGui.TextDisabled(localization.Format("MonsterUi.Position",x.ToString("F2"),y.ToString("F2")));
        else ImGui.TextDisabled(localization.Get("MonsterUi.NoPosition"));
        if (detail.HasDrops && !detail.DropItemIds.Contains(state.ItemId))
            ImGui.TextWrapped(localization.Get("MonsterUi.Mismatch"));
        DrawMonsterMap(detail);
        if (detail.InstanceId != 0)
        {
            var duty = duties.State.Catalog?.Entries.FirstOrDefault(entry => entry.InstanceId == detail.InstanceId);
            if (duty is not null && ImGui.SmallButton(localization.Format("MonsterUi.Duty",duty.Name)))
            { dutyLoot.Select(duty); showDutyDetails=true; page=Page.DutyLoot; }
            else if (duty is null) ImGui.TextDisabled(localization.Format("MonsterUi.Instance",detail.InstanceId));
        }
        if (ImGui.CollapsingHeader(localization.Format("MonsterUi.OtherItems",detail.DropItemIds.Count)+"##Monster"))
        {
            if (!detail.HasDrops) ImGui.TextDisabled(localization.Get("MonsterUi.NoDrops"));
            var showItems=ImGui.BeginChild("MonsterDropItems",new Vector2(0,155),true);
            try
            {
                if (showItems)
                {
                    var clipper=ImGui.ImGuiListClipper();
                    try
                    {
                        clipper.Begin(detail.DropItemIds.Count);
                        while (clipper.Step())
                        for (var index=clipper.DisplayStart;index<clipper.DisplayEnd;index++)
                        {
                            var id=detail.DropItemIds[index]; var item=itemCatalog.State.Catalog?.Find(id);
                            DrawItemIcon(item,new Vector2(24)); var clicked=ImGui.IsItemClicked(); ImGui.SameLine();
                            ImGui.BeginDisabled(item is null || id>int.MaxValue);
                            try { if ((ImGui.SmallButton($"{item?.Name ?? localization.Format("MonsterUi.Item",id)}##MobDrop{id}") || clicked) && item is not null && id<=int.MaxValue) SelectItem(id); }
                            finally { ImGui.EndDisabled(); }
                        }
                    }
                    finally { clipper.End(); clipper.Destroy(); }
                }
            }
            finally { ImGui.EndChild(); }
        }
    }
    private string MonsterZone(uint placeNameId) => maps.State.Catalog?.ForPlace(placeNameId).FirstOrDefault()?.Zone
        ?? (placeNameId == 0 ? localization.Get("MonsterUi.UnknownZone") : localization.Format("MonsterUi.Place",placeNameId));
    private void DrawMonsterMap(MonsterDocument monster)
    {
        var choices=maps.State.Catalog?.ForPlace(monster.PlaceNameId);
        if (choices is null || choices.Count==0) { ImGui.TextDisabled(localization.Get("MonsterUi.NoMap")); return; }
        var chosen=choices.Count==1 ? choices[0] : choices.FirstOrDefault(map=>map.MapId==selectedMonsterMap);
        if (choices.Count>1 && ImGui.BeginCombo(localization.Get("MonsterUi.Map")+"##Monster",chosen?.Zone ?? localization.Get("MonsterUi.ChooseMap")))
        {
            try { foreach(var map in choices) if(ImGui.Selectable(localization.Format("MonsterUi.MapChoice",map.Zone,map.Subzone,map.MapId),map.MapId==selectedMonsterMap)) selectedMonsterMap=map.MapId; }
            finally { ImGui.EndCombo(); }
        }
        if (chosen is not null && ImGui.SmallButton(localization.Get("MonsterUi.OpenMap")+"##Monster"))
        {
            selectedMapId=chosen.MapId; mapQuery=""; mapX=20.5f; mapY=20.5f;
            selectedBookmark=Guid.Empty; bookmarkName=""; bookmarkSaved=false; mapOpenError=null; page=Page.Maps;
        }
        ImGui.TextDisabled(localization.Get("MonsterUi.MapHelp"));
    }
}
