using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.Character;
using TenshoAssistant.Gear;

namespace TenshoAssistant;

// R-11 (0.310–0.314, user requests): gear planner laid out like the game's Character window — the stats on the left (real values read
// from the game, planned values, difference; only the job kind's stats, derived values for combat jobs, 0.311), then both gear columns.
// A click on a slot lists the current job's gear near its level (filters in the settings; « All jobs (glamour) » since patch 7.4, 0.311);
// right click = the item menu (sources, « Try on »). Materia sockets and editor with per-piece caps and over-cap warnings (0.312–0.313);
// owned marks, « Only what I own » and « Pieces to obtain » with their sources (0.314). One plan per job, saved; sharing by code. Gear
// catalog built from the Item sheet on a worker (Gear/GearLoader), worn gear copied once a second on the framework thread.
internal sealed partial class MainWindow
{
    internal Func<GearCatalog?>? GearCatalog { get; set; }
    internal Func<bool>? GearCatalogFailed { get; set; }
    private string GearNotReady=>localization.Get(GearCatalogFailed?.Invoke()==true ? "Gear.CatalogFailed":"Gear.Loading");
    internal Func<uint,bool>? TryOnItem { get; set; }
    internal Func<IReadOnlyList<EquippedPiece>>? EquippedGear { get; set; }
    // 0.331 (R-14): the worn gear could not be read (GearLoader.Unavailable): the planner says so instead of computing on no worn set.
    internal Func<bool>? EquippedGearUnavailable { get; set; }
    private string gearSearch="";
    // The model's (GearModelFor): another job's pieces shown and tried on, the stats keeping the worn piece.
    private Dictionary<GearSlot,GearItem> gearLooks=[];
    // 0.312: materia counted per slot (planned materia, else the worn piece's own); the model's (GearModelFor).
    private Dictionary<GearSlot,IReadOnlyList<MeldedMateria>> gearMelds=[];

    private static readonly GearSlot[] GearLeft=[GearSlot.MainHand,GearSlot.Head,GearSlot.Body,GearSlot.Hands,GearSlot.Legs,GearSlot.Feet];
    private static readonly GearSlot[] GearRight=[GearSlot.OffHand,GearSlot.Ears,GearSlot.Neck,GearSlot.Wrists,GearSlot.RingRight,GearSlot.RingLeft];
    // Stats in the order of the game's Character window (BaseParam rows); weapon damage first.
    private static readonly int[] GearStatOrder=[12,13,1,2,3,4,5,27,22,44,45,46,19,6,21,24,70,71,11,72,73,10];

    private uint gearJob;
    private CharacterStatsState DrawGearJobPicker(CharacterStatsState real)
    {
        GearsetSource?.Want();
        var levels=GearsetSource?.State?.JobLevels;
        var shown=gearJob!=0 && gearJob!=real.JobId && levels?.TryGetValue(gearJob,out var known)==true
            ? real with {JobId=gearJob,Job=JobName(gearJob),Level=known,Values=new Dictionary<string,int>(),Category=0,Role=0,Modifiers=default}:real;
        // 0.585 (user report): the label first, then the list.
        ImGui.AlignTextToFramePadding();ImGui.TextUnformatted(localization.Get("Gear.PlanFor"));ImGui.SameLine();
        ImGui.SetNextItemWidth(180);
        if(ImGui.BeginCombo("###GearJob",JobName(shown.JobId)))
        {
            try
            {
                if(ImGui.Selectable(localization.Format("Gear.WornJob",JobName(real.JobId)),shown.JobId==real.JobId))gearJob=0;
                foreach(var job in (levels?.Keys ?? []).Where(job=>job!=real.JobId).OrderBy(JobName,StringComparer.CurrentCultureIgnoreCase))
                    if(ImGui.Selectable(JobName(job)+$" · {levels![job]}###GearJob{job}",job==shown.JobId))gearJob=job;
            }
            finally{ImGui.EndCombo();}
        }
        Tip("Gear.PlanForTip");
        if(shown.JobId!=real.JobId)ImGui.TextColored(Accent,localization.Get("Gear.OtherJobNote"));
        return shown;
    }
    private void DrawGearPlanner()
    {
        Heading(localization.Get("Gear.Title"),localization.Get("Gear.Description"));
        var catalog=GearCatalog?.Invoke();
        var state=characterStats.State;
        if(state.CharacterId==0){ImGui.TextWrapped(localization.Get("CharacterUi.Offline"));return;}
        if(catalog is null){ImGui.TextDisabled(GearNotReady);return;}
        if(EquippedGearUnavailable?.Invoke()==true){ImGui.TextWrapped(localization.Get("Gear.WornUnavailable"));return;}
        // 0.580 (T14): the plan of another job than the one worn: its pieces and materia; what is worn and read belongs to the worn job only.
        state=DrawGearJobPicker(state);
        var otherJob=state.JobId!=characterStats.State.JobId;
        var worn=(EquippedGear?.Invoke() ?? []).Where(piece=>!otherJob && catalog.Find(piece.ItemId) is not null).ToDictionary(piece=>piece.Slot);
        // 0.579 (user request): on the page itself, not only behind the filters button: the same setting.
        BoolSetting("Gear.AllJobs","Gear.AllJobsTip",()=>configuration.GearAllJobs,value=>configuration.GearAllJobs=value,c=>c.GearAllJobs);
        DrawGearWear(catalog);
        DrawGameGearsets(catalog);
        DrawJobProgress();
        DrawGearPlanPicker(state.JobId);
        var plans=configuration.GearPlans??=[];
        var plan=plans.GetValueOrDefault(state.JobId) ?? [];
        var model=GearModelFor(catalog,state,worn,plan,otherJob);
        var (plannedPieces,wornPieces)=(model.Planned,model.Worn);
        gearLooks=model.Looks;gearMelds=model.Melds;

        ImGui.TextColored(Accent,localization.Format("CharacterDates.Summary",state.Name,state.Job,state.Level));
        ImGui.SameLine();
        ImGui.TextDisabled(localization.Format("Gear.ItemLevels",model.WornLevel.ToString("0.0",Numbers),model.PlannedLevel.ToString("0.0",Numbers)));
        DrawGearToolbar(catalog,state,plan,plannedPieces,wornPieces);
        if(!ImGui.BeginTable("GearPlanner",3,ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerV))return;
        try
        {
            // 0.311 (user feedback): the stats on the left, then both gear columns side by side.
            ImGui.TableSetupColumn("##Stats",ImGuiTableColumnFlags.WidthStretch,1.2f);
            ImGui.TableSetupColumn("##Left",ImGuiTableColumnFlags.WidthStretch,1);
            ImGui.TableSetupColumn("##Right",ImGuiTableColumnFlags.WidthStretch,1);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawGearStats(state,model);
            ImGui.TableNextColumn();
            foreach(var slot in GearLeft)DrawGearSlot(catalog,state,slot,wornPieces,plannedPieces,plan);
            ImGui.TableNextColumn();
            foreach(var slot in GearRight)DrawGearSlot(catalog,state,slot,wornPieces,plannedPieces,plan);
        }
        finally{ImGui.EndTable();}
        DrawGearToObtain(catalog,plannedPieces);
        ImGui.TextDisabled(localization.Get("Gear.Notes"));
    }

    // X-01 C5 (0.1242): the planned set, its stats and the derived values are rebuilt only when one of their inputs changes, not in every
    // frame: the catalog, the character's state as read (a new record on each change), the job and level planned for, « assume HQ », the
    // worn gear as published (a new list on each reading), the plan and its materia (their exact content, compared each frame: twelve
    // slots at most, so a change made anywhere is seen at once) and the language (the derived values are texts).
    private readonly record struct GearModelKey(GearCatalog Catalog,CharacterStatsState Real,uint Job,int Level,bool AssumeHq,IReadOnlyList<EquippedPiece>? Equipped,
        string Plan,int Language);
    internal sealed record GearModel(Dictionary<GearSlot,(GearItem Item,bool Hq,IReadOnlyList<(int,int)> Materia)> Planned,
        Dictionary<GearSlot,(GearItem Item,bool Hq,IReadOnlyList<(int,int)> Materia)> Worn,Dictionary<GearSlot,GearItem> Looks,
        Dictionary<GearSlot,IReadOnlyList<MeldedMateria>> Melds,double WornLevel,double PlannedLevel,Dictionary<int,int> WornTotals,
        Dictionary<int,int> PlannedTotals,Dictionary<string,int> PlannedValues,IReadOnlyList<(string Key,string Value)> CombatNow,
        IReadOnlyDictionary<string,string> CombatLater);
    private GearModelKey? gearModelKey;
    private GearModel? gearModel;

    private GearModel GearModelFor(GearCatalog catalog,CharacterStatsState state,Dictionary<GearSlot,EquippedPiece> worn,Dictionary<GearSlot,uint> plan,bool otherJob)
    {
        var assumeHq=configuration.GearAssumeHq;
        var materiaPlan=configuration.GearMateria?.GetValueOrDefault(state.JobId);
        var key=new GearModelKey(catalog,characterStats.State,state.JobId,state.Level,assumeHq,otherJob ? null:EquippedGear?.Invoke(),
            GearPlanKey.Signature(plan,materiaPlan),localization.Revision);
        if(gearModel is { } known && gearModelKey==key)return known;

        // The set as planned: a planned item where one is chosen, the worn piece (with its materia) elsewhere.
        var plannedPieces=new Dictionary<GearSlot,(GearItem Item,bool Hq,IReadOnlyList<(int,int)> Materia)>();
        var looks=new Dictionary<GearSlot,GearItem>();var meldsBySlot=new Dictionary<GearSlot,IReadOnlyList<MeldedMateria>>();
        var wornPieces=new Dictionary<GearSlot,(GearItem Item,bool Hq,IReadOnlyList<(int,int)> Materia)>();
        foreach(var slot in Enum.GetValues<GearSlot>())
        {
            if(worn.TryGetValue(slot,out var piece) && catalog.Find(piece.ItemId) is { } wornItem)wornPieces[slot]=(wornItem,piece.Hq,piece.Materia);
            // Materia of the slot: the planned ones when set (on the worn piece or a planned one), else the worn piece's own.
            IReadOnlyList<MeldedMateria> melds=materiaPlan?.TryGetValue(slot,out var codes)==true ? codes.Select(MeldedMateria.From).ToArray()
                :!plan.ContainsKey(slot) && worn.TryGetValue(slot,out var wornMelds) ? wornMelds.Melds ?? []:[];
            // 0.331: only as many as the piece they go on has sockets (the planned piece, else the worn one): materia left from a piece
            // with more sockets are neither counted, drawn nor shared (a piece with no socket and no cap counted them all).
            var socketItem=(plan.TryGetValue(slot,out var plannedId) ? catalog.Find(plannedId):null) ?? (wornPieces.TryGetValue(slot,out var socketPiece) ? socketPiece.Item:null);
            if(melds.Count>(socketItem?.SocketCount ?? 0))melds=melds.Take(socketItem?.SocketCount ?? 0).ToArray();
            meldsBySlot[slot]=melds;
            if(plan.TryGetValue(slot,out var id) && catalog.Find(id) is { } planned)
            {
                // 0.311: another job's piece (glamour since 7.4) is shown and tried on, the stats keep the worn piece.
                if(GearRules.GlamourOnly(planned,state.JobId,state.Level)){looks[slot]=planned;if(wornPieces.TryGetValue(slot,out var keptForStats))plannedPieces[slot]=keptForStats;}
                else plannedPieces[slot]=(planned,assumeHq && planned.CanBeHq,catalog.MateriaValues(melds));
            }
            else if(wornPieces.TryGetValue(slot,out var kept))plannedPieces[slot]=materiaPlan?.ContainsKey(slot)==true ? (kept.Item,kept.Hq,catalog.MateriaValues(melds)):kept;
        }
        // A planned two-handed weapon frees the off hand.
        if(plannedPieces.TryGetValue(GearSlot.MainHand,out var weapon) && weapon.Item.BlocksOffHand)plannedPieces.Remove(GearSlot.OffHand);
        var wornTotals=GearRules.Totals(wornPieces.Values.Select(piece=>(piece.Item,piece.Hq,(IReadOnlyList<(int Param,int Value)>)piece.Materia)));
        var plannedTotals=GearRules.Totals(plannedPieces.Values.Select(piece=>(piece.Item,piece.Hq,(IReadOnlyList<(int Param,int Value)>)piece.Materia)));
        var plannedValues=GearRules.Planned(state.Values,wornTotals,plannedTotals);
        // Combat jobs: the derived values (critical rate, damage bonus…), same formulas as the Character page.
        var derived=CombatCategory(state.Category) && state.Modifiers.Div>0;
        var model=new GearModel(plannedPieces,wornPieces,looks,meldsBySlot,
            GearRules.AverageItemLevel(wornPieces.ToDictionary(pair=>pair.Key,pair=>pair.Value.Item)),
            GearRules.AverageItemLevel(plannedPieces.ToDictionary(pair=>pair.Key,pair=>pair.Value.Item)),
            wornTotals,plannedTotals,plannedValues,
            derived ? [.. StatFormulas.Combat(state.Values,state.Category,state.Role,state.Modifiers).Select(row=>(row.Key,row.Value))]:[],
            derived ? StatFormulas.Combat(plannedValues,state.Category,state.Role,state.Modifiers).ToDictionary(row=>row.Key,row=>row.Value):new Dictionary<string,string>());
        gearModelKey=key;gearModel=model;
        return model;
    }

    private void DrawGearToolbar(GearCatalog catalog,CharacterStatsState state,Dictionary<GearSlot,uint> plan,
        Dictionary<GearSlot,(GearItem Item,bool Hq,IReadOnlyList<(int,int)> Materia)> planned,Dictionary<GearSlot,(GearItem Item,bool Hq,IReadOnlyList<(int,int)> Materia)> worn)
    {
        ImGui.BeginDisabled(TryOnItem is null || planned.Count==0);
        try
        {
            if(ImGui.Button(localization.Get("Gear.TryPlan")))
            {
                foreach(var (slot,piece) in planned)TryOnItem?.Invoke(gearLooks.GetValueOrDefault(slot)?.Id ?? piece.Item.Id);
                foreach(var (slot,look) in gearLooks)if(!planned.ContainsKey(slot))TryOnItem?.Invoke(look.Id);
            }
        }
        finally{ImGui.EndDisabled();}
        Tip("Gear.TryPlanTip");
        ImGui.SameLine();
        // 0.331: the planned materia are part of the plan (they also apply to worn pieces): cleared too, the button active while either is set.
        ImGui.BeginDisabled(plan.Count==0 && configuration.GearMateria?.GetValueOrDefault(state.JobId) is not { Count:>0 });
        if(DeleteButton("Gear.ClearPlan",small:false)){configuration.GearPlans?.Remove(state.JobId);configuration.GearMateria?.Remove(state.JobId);SaveGearPlan();}
        ImGui.EndDisabled();
        Tip("Gear.ClearPlanTip");
        ImGui.SameLine();
        if(ImGui.Button(localization.Get("Gear.Filters")))ImGui.OpenPopup("GearFilters");
        Tip("Gear.FiltersTip");
        if(ImGui.BeginPopup("GearFilters"))
        {
            try{DrawGearSettings();}
            finally{ImGui.EndPopup();}
        }
        // Sharing (user request): a readable list and the code, copied together; import reads a code from the clipboard.
        ImGui.SameLine();
        ImGui.BeginDisabled(planned.Count==0);
        if(ImGui.Button(localization.Get("Gear.Share")))
        {
            // Glamour-only pieces (0.311) are part of the shared build too.
            var pieces=planned.ToDictionary(pair=>pair.Key,pair=>pair.Value.Item);
            foreach(var (slot,look) in gearLooks)pieces[slot]=look;
            var whole=pieces.ToDictionary(pair=>pair.Key,pair=>pair.Value.Id);
            var sharedMelds=gearMelds.Where(pair=>pair.Value.Count>0 && !gearLooks.ContainsKey(pair.Key)).ToDictionary(pair=>pair.Key,pair=>pair.Value);
            var lines=pieces.OrderBy(pair=>pair.Key).Select(pair=>$"{localization.Get("Gear.Slot"+pair.Key)} : {pair.Value.Name} ({localization.Format("Gear.LevelLine",pair.Value.Level,pair.Value.ItemLevel)}){(gearLooks.ContainsKey(pair.Key) ? " · "+localization.Get("Gear.GlamourOnly"):"")}");
            ImGui.SetClipboardText(string.Join(Environment.NewLine,lines.Prepend(localization.Format("Gear.ShareHeader",state.Job,state.Level)).Append(GearShare.Export(state.JobId,whole,sharedMelds))));
            gearMessage=localization.Get("Gear.Shared");
        }
        ImGui.EndDisabled();
        Tip("Gear.ShareTip");
        ImGui.SameLine();
        if(ImGui.Button(localization.Get("Gear.Import")))
        {
            if(GearShare.Import(ImGui.GetClipboardText()) is { } imported)
            {
                var known=imported.Plan.Where(pair=>catalog.Find(pair.Value) is { } item && GearRules.Fits(item,pair.Key)).ToDictionary(pair=>pair.Key,pair=>pair.Value);
                GearRules.DropUniqueTwin(catalog,known);
                // 0.331: a code with no piece this client knows (newer patch, pieces that fit no slot) leaves both saved plans untouched.
                if(known.Count==0)gearMessage=localization.Get("Gear.ImportUnknown");
                else
                {
                    // The code's materia replace the job's planned ones, also when it has none (older code): old materia never land on new pieces.
                    var materia=imported.Materia.Where(pair=>known.ContainsKey(pair.Key)).ToDictionary(pair=>pair.Key,pair=>pair.Value.Select(meld=>meld.Code).ToList());
                    if(materia.Count==0)configuration.GearMateria?.Remove(imported.Job);else (configuration.GearMateria??=[])[imported.Job]=materia;
                    StoreGearPlan(imported.Job,known);
                    gearMessage=imported.Job==state.JobId ? localization.Format("Gear.Imported",known.Count):localization.Format("Gear.ImportedOtherJob",known.Count,imported.Job);
                }
            }
            else gearMessage=localization.Get("Gear.ImportNone");
        }
        Tip("Gear.ImportTip");
        if(gearMessage is not null){ImGui.SameLine();ImGui.TextDisabled(gearMessage);}
    }
    private string? gearMessage;

    // Filters and options, also reachable from Settings (every setting with its tooltip and « ↺ »).
    private void DrawGearSettings()
    {
        // X-76 H4 (0.914): the slider and the field follow the edit in memory and are written once, when released or left (they were
        // written on every frame of a drag and every key press).
        SliderSetting("Gear.LevelsBelow","Gear.LevelsBelowTip",()=>configuration.GearLevelsBelow,value=>configuration.GearLevelsBelow=value,0,30,DefaultConfiguration.GearLevelsBelow,width:140);
        BoolSetting("Gear.ExactAtCaps","Gear.ExactAtCapsTip",()=>configuration.GearExactAtCaps,value=>configuration.GearExactAtCaps=value,c=>c.GearExactAtCaps);
        IntSetting("Gear.MinItemLevel","Gear.MinItemLevelTip",()=>configuration.GearMinItemLevel,value=>configuration.GearMinItemLevel=value,0,int.MaxValue,DefaultConfiguration.GearMinItemLevel,140);
        BoolSetting("Gear.AssumeHq","Gear.AssumeHqTip",()=>configuration.GearAssumeHq,value=>configuration.GearAssumeHq=value,c=>c.GearAssumeHq);
        BoolSetting("Gear.AllJobs","Gear.AllJobsTip",()=>configuration.GearAllJobs,value=>configuration.GearAllJobs=value,c=>c.GearAllJobs);
        BoolSetting("Gear.OwnedOnly","Gear.OwnedOnlyTip",()=>configuration.GearOwnedOnly,value=>configuration.GearOwnedOnly=value,c=>c.GearOwnedOnly);
    }

    private void DrawGearSlot(GearCatalog catalog,CharacterStatsState state,GearSlot slot,
        Dictionary<GearSlot,(GearItem Item,bool Hq,IReadOnlyList<(int,int)> Materia)> worn,Dictionary<GearSlot,(GearItem Item,bool Hq,IReadOnlyList<(int,int)> Materia)> planned,
        Dictionary<GearSlot,uint> plan)
    {
        var side=Math.Max(36,ImGui.GetTextLineHeight()*2.4f);
        var look=gearLooks.GetValueOrDefault(slot);
        var hasPiece=planned.TryGetValue(slot,out var piece);
        var shown=look ?? (hasPiece ? piece.Item:null);
        var hq=look is null && hasPiece && piece.Hq;
        var changed=plan.ContainsKey(slot) && (!worn.TryGetValue(slot,out var current) || current.Item.Id!=shown?.Id);
        ImGui.PushID("GearSlot"+slot);
        try
        {
            var start=ImGui.GetCursorScreenPos();
            var definition=shown is null ? null:itemCatalog.State.Catalog?.Find(shown.Id);
            if(shown is null)
            {
                // Empty slot: a framed square, like the game's empty gear slot.
                ImGui.Dummy(new Vector2(side));
                ImGui.GetWindowDrawList().AddRect(start,start+new Vector2(side),ImGui.GetColorU32(Muted),4,ImDrawFlags.None,1.5f);
            }
            else DrawItemIcon(definition,new Vector2(side));
            if(changed)ImGui.GetWindowDrawList().AddRect(start-new Vector2(1),start+new Vector2(side+1),ImGui.GetColorU32(UI.FfxivTheme.Gold),4,ImDrawFlags.None,2);
            if(ImGui.IsItemClicked(ImGuiMouseButton.Left)){gearSearch="";ImGui.OpenPopup("GearPick");}
            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.TextDisabled(localization.Get("Gear.Slot"+slot));
            if(shown is null)ImGui.TextDisabled("—");
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text,changed ? UI.FfxivTheme.Gold:Text);
                var clicked=ImGui.Selectable($"{shown.Name}{(hq ? " ":"")}##GearName",false,ImGuiSelectableFlags.None,new Vector2(0,0));
                ImGui.PopStyleColor();
                if(clicked){gearSearch="";ImGui.OpenPopup("GearPick");}
                // X-294 (0.1295): the slot's piece shows its stats too (the common item tooltip), with its known HQ state.
                if(ImGui.IsItemHovered())DrawItemInfoTooltip(shown.Id,glamourOnly:look is not null,hq:hq);
                ItemLinksMenu("GearSlotMenu",definition);
                ImGui.TextDisabled(localization.Format("Gear.LevelLine",shown.Level,shown.ItemLevel));
                if(changed || look is not null){ImGui.SameLine();DrawOwnedMark(shown.Id);}
                if(look is not null){ImGui.SameLine();ImGui.TextColored(UI.FfxivTheme.Gold,localization.Get("Gear.GlamourOnly"));Tip("Gear.GlamourOnlyTip");}
                else if(shown.SocketCount>0){ImGui.SameLine();DrawMateriaSockets(catalog,state,slot,shown,hq);}
            }
            ImGui.EndGroup();
            DrawGearPick(catalog,state,slot,worn,plan);
        }
        finally{ImGui.PopID();}
        ImGui.Spacing();
    }

    // Candidate list of one slot: gear of the job near its level, best item level first, with what each changes.
    private void DrawGearPick(GearCatalog catalog,CharacterStatsState state,GearSlot slot,
        Dictionary<GearSlot,(GearItem Item,bool Hq,IReadOnlyList<(int,int)> Materia)> worn,Dictionary<GearSlot,uint> plan)
    {
        ImGui.SetNextWindowSizeConstraints(new Vector2(420,120),new Vector2(640,520));
        if(!ImGui.BeginPopup("GearPick"))return;
        try
        {
            ImGui.TextColored(UI.FfxivTheme.Gold,localization.Get("Gear.Slot"+slot));
            var filter=new GearFilter(configuration.GearLevelsBelow,configuration.GearExactAtCaps,configuration.GearMinItemLevel,gearSearch.Trim(),configuration.GearAllJobs);
            var (min,max)=GearRules.LevelWindow(state.Level,filter);
            // 0.331: « All jobs (glamour) » lists armor and accessories of every level: no level window shown for them.
            ImGui.SameLine();ImGui.TextDisabled(GearRules.AnyJobAndLevel(slot,filter) ? localization.Get("Gear.WindowAll"):localization.Format("Gear.Window",min,max));
            ImGui.SetNextItemWidth(220);ImGui.InputTextWithHint("##GearSearch",localization.Get("Gear.Search"),ref gearSearch,80);Tip("Gear.SearchTip");
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get("Gear.Filters")))ImGui.OpenPopup("GearFiltersInPick");
            Tip("Gear.FiltersTip");
            if(ImGui.BeginPopup("GearFiltersInPick")){try{DrawGearSettings();}finally{ImGui.EndPopup();}}
            if(plan.ContainsKey(slot) && ImGui.Selectable(localization.Get("Gear.KeepWorn"))){plan.Remove(slot);StoreGearPlan(state.JobId,plan);ImGui.CloseCurrentPopup();}
            worn.TryGetValue(slot,out var current);
            var rows=GearPickRows(catalog,state,slot,worn,plan,filter);
            if(rows.Length==0){ImGui.TextDisabled(localization.Get("Gear.NoCandidate"));return;}
            if(!ImGui.BeginChild("GearPickList",new Vector2(0,Math.Min(rows.Length*ImGui.GetTextLineHeightWithSpacing()*1.6f+8,380)),false))
            {ImGui.EndChild();return;}
            try
            {
                var items=itemCatalog.State.Catalog;
                var clipper=ImGui.ImGuiListClipper();
                try
                {
                    clipper.Begin(rows.Length);
                    while(clipper.Step())
                    for(var index=clipper.DisplayStart;index<clipper.DisplayEnd;index++)
                    {
                        var (candidate,glamourOnly)=rows[index];
                        var definition=items?.Find(candidate.Id);
                        DrawItemIcon(definition,new Vector2(ImGui.GetTextLineHeight()*1.4f));
                        ImGui.SameLine();DrawOwnedMark(candidate.Id);
                        ImGui.SameLine();
                        var selected=plan.TryGetValue(slot,out var chosen) ? chosen==candidate.Id:current.Item?.Id==candidate.Id;
                        if(ImGui.Selectable($"{candidate.Name}##Pick{candidate.Id}",selected))
                        {
                            if(current.Item?.Id==candidate.Id)plan.Remove(slot);else plan[slot]=candidate.Id;
                            StoreGearPlan(state.JobId,plan);ImGui.CloseCurrentPopup();
                        }
                        // X-290 (0.1294, user request of 13/11 about 23:30): the stat differences written at the end of the line went past the list and
                        // were cut (« +4 I », « -5 Détermination »): they are in the piece's tooltip, with its stats, as the hover comparator shows them.
                        if(ImGui.IsItemHovered())DrawItemInfoTooltip(candidate.Id,glamourOnly:glamourOnly);
                        ItemLinksMenu($"GearPickMenu{candidate.Id}",definition);
                        SameLineOrNext(localization.Format("Gear.LevelLine",candidate.Level,candidate.ItemLevel));
                        ImGui.TextDisabled(localization.Format("Gear.LevelLine",candidate.Level,candidate.ItemLevel));
                        if(glamourOnly){SameLineOrNext(localization.Get("Gear.GlamourOnly"));ImGui.TextColored(UI.FfxivTheme.Gold,localization.Get("Gear.GlamourOnly"));}
                    }
                }
                finally{clipper.End();clipper.Destroy();}
            }
            finally{ImGui.EndChild();}
        }
        finally{ImGui.EndPopup();}
    }

    // 0.331 (« nothing heavy in Draw »): the rows of the open list — filtered, sorted — rebuilt only when an input changes: catalog, slot, job,
    // level, filters, owned-only, assumed HQ, what is owned (same key as gearOwned), the worn gear and the other ring. X-290 (0.1294): the
    // differences against the worn piece left the rows for the tooltip (the common item tooltip since X-294), worked out for the hovered piece only.
    private readonly record struct GearPickKey(GearCatalog Catalog,GearSlot Slot,uint Job,int Level,GearFilter Filter,bool OwnedOnly,bool AssumeHq,
        (Inventory.InventorySnapshot? Current,Inventory.BankState State,ulong CharacterId) Owned,uint OtherRing);
    private GearPickKey? gearPickKey;
    private IReadOnlyList<EquippedPiece>? gearPickWorn;
    private (GearItem Item,bool GlamourOnly)[] gearPickRows=[];

    private (GearItem Item,bool GlamourOnly)[] GearPickRows(GearCatalog catalog,CharacterStatsState state,GearSlot slot,
        Dictionary<GearSlot,(GearItem Item,bool Hq,IReadOnlyList<(int,int)> Materia)> worn,Dictionary<GearSlot,uint> plan,GearFilter filter)
    {
        // 0.331: a Unique ring already in the other ring slot of the planned set (planned there, else worn) is left out: the game refuses two.
        uint otherRing=0;
        if(GearRules.OtherRing(slot) is { } ring)
            otherRing=plan.TryGetValue(ring,out var plannedRing) && catalog.Find(plannedRing) is not null ? plannedRing:worn.TryGetValue(ring,out var wornRing) ? wornRing.Item.Id:0;
        var equipped=EquippedGear?.Invoke() ?? [];
        var key=new GearPickKey(catalog,slot,state.JobId,state.Level,filter,configuration.GearOwnedOnly,configuration.GearAssumeHq,GearOwnedSource(),otherRing);
        if(gearPickKey!=key || !SameWorn(gearPickWorn,equipped))
        {
            var rows=new List<(GearItem Item,bool GlamourOnly)>();
            foreach(var candidate in GearRules.Candidates(catalog,slot,state.JobId,state.Level,filter).Where(piece=>!(piece.Unique && piece.Id==otherRing))
                .Where(piece=>!configuration.GearOwnedOnly || IsWorn(piece.Id) || GearOwned(piece.Id) is { Total:>0 }).Take(300))
            {
                rows.Add((candidate,GearRules.GlamourOnly(candidate,state.JobId,state.Level)));
            }
            gearPickRows=rows.ToArray();
        }
        gearPickKey=key;gearPickWorn=equipped;
        return gearPickRows;
    }

    /// <summary>X-290 (0.1294): the next text on the same line when it fits in what is left of it, else on the next line: never cut at the edge.</summary>
    private static void SameLineOrNext(string text)
    {
        ImGui.SameLine();
        if(ImGui.GetContentRegionAvail().X<ImGui.CalcTextSize(text).X)ImGui.NewLine();
    }

    // Same worn pieces (slot, item, HQ): the loader publishes a new list every second, usually with the same content.
    private static bool SameWorn(IReadOnlyList<EquippedPiece>? before,IReadOnlyList<EquippedPiece> now)
    {
        if(ReferenceEquals(before,now))return true;
        if(before is null || before.Count!=now.Count)return false;
        for(var index=0;index<now.Count;index++)
            if(before[index].Slot!=now[index].Slot || before[index].ItemId!=now[index].ItemId || before[index].Hq!=now[index].Hq)return false;
        return true;
    }

    // Stats column: real values (game), planned values (real + planned set − worn set) and the difference; then derived values.
    // X-01 C5 (0.1242): the totals and the derived values come from the model (GearModelFor), not computed in every frame.
    private void DrawGearStats(CharacterStatsState state,GearModel model)
    {
        var (wornTotals,plannedTotals,plannedValues)=(model.WornTotals,model.PlannedTotals,model.PlannedValues);
        if(!ImGui.BeginTable("GearStats",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp))return;
        try
        {
            foreach(var key in new[]{"Stat","Worn","Planned","Delta"})ImGui.TableSetupColumn(localization.Get("Gear.Column"+key));
            ImGui.TableHeadersRow();
            // 0.311: only the stats of the job's kind (no GP / CP on a combat job, no critical hit on a gatherer).
            var combat=CombatCategory(state.Category);
            foreach(var param in GearStatOrder)
            {
                if(combat ? param is 10 or 11 or >= 70:param is not (10 or 11 or >= 70 or 3 or 21 or 24))continue;
                int now,later;
                if(GearRules.AttributeOf(param) is { } key)
                {
                    if(!state.Values.TryGetValue(key,out now) || now==0 && plannedTotals.GetValueOrDefault(param)==0)continue;
                    later=plannedValues.GetValueOrDefault(key);
                }
                else
                {
                    // Weapon damage is not a character attribute: the weapons' values.
                    now=wornTotals.GetValueOrDefault(param);later=plannedTotals.GetValueOrDefault(param);
                    if(now==0 && later==0)continue;
                }
                ImGui.TableNextRow();ImGui.TableNextColumn();ImGui.TextUnformatted(localization.Get("Gear.Param"+param));
                ImGui.TableNextColumn();ImGui.TextUnformatted(now.ToString("N0",Numbers));
                ImGui.TableNextColumn();ImGui.TextColored(later!=now ? UI.FfxivTheme.Gold:Text,later.ToString("N0",Numbers));
                ImGui.TableNextColumn();
                if(later!=now)ImGui.TextColored(later>now ? Success:Error,$"{(later>now ? "+":"")}{(later-now).ToString("N0",Numbers)}");
            }
            // Combat jobs: the derived values (critical rate, damage bonus…) in the same columns, same formulas as the Character page.
            if(!combat || state.Modifiers.Div<=0)return;
            ImGui.TableNextRow();ImGui.TableNextColumn();ImGui.Spacing();
            foreach(var (rowKey,rowValue) in model.CombatNow)
            {
                ImGui.TableNextRow();ImGui.TableNextColumn();ImGui.TextUnformatted(localization.Get("CharacterStats."+rowKey));
                ImGui.TableNextColumn();ImGui.TextUnformatted(rowValue);
                ImGui.TableNextColumn();
                var later=model.CombatLater.GetValueOrDefault(rowKey);
                ImGui.TextColored(later is not null && later!=rowValue ? UI.FfxivTheme.Gold:Text,later ?? "—");
            }
        }
        finally{ImGui.EndTable();}
    }

    // 0.312 (user request: materia on worn and planned gear): the sockets of a piece, filled ones coloured; click = choose materia.
    private void DrawMateriaSockets(GearCatalog catalog,CharacterStatsState state,GearSlot slot,GearItem item,bool hq)
    {
        var melds=gearMelds.GetValueOrDefault(slot) ?? [];
        var radius=ImGui.GetTextLineHeight()*0.32f;var at=ImGui.GetCursorScreenPos();var draw=ImGui.GetWindowDrawList();
        var size=new Vector2(item.SocketCount*(radius*2+4),ImGui.GetTextLineHeight());
        ImGui.InvisibleButton("##Sockets",size);
        var hovered=ImGui.IsItemHovered();
        for(var socket=0;socket<item.SocketCount;socket++)
        {
            var centre=at+new Vector2(radius+socket*(radius*2+4),size.Y/2);
            var overmeld=socket>=item.MateriaSlots;
            if(socket<melds.Count)draw.AddCircleFilled(centre,radius,ImGui.GetColorU32(overmeld ? Warning:Success));
            draw.AddCircle(centre,radius,ImGui.GetColorU32(overmeld ? Warning with {W=0.9f}:UI.FfxivTheme.Gold),0,1.2f);
        }
        if(hovered)
        {
            using(TooltipScope.Begin())
            {
                ImGui.TextColored(UI.FfxivTheme.Gold,localization.Format("Gear.Sockets",item.MateriaSlots,item.SocketCount-item.MateriaSlots));
                for(var socket=0;socket<melds.Count;socket++)
                    if(catalog.FindMateria(melds[socket].Row) is { } kind)
                    {
                        var meld=melds[socket];
                        // 0.338: an advanced socket gives its success rate from the game data.
                        var rate=socket>=item.MateriaSlots ? "  · "+OvermeldRateText(catalog,kind,meld.Grade,socket-item.MateriaSlots,hq):"";
                        ImGui.TextUnformatted($"{kind.NameOf(meld.Grade)} : +{kind.ValueOf(meld.Grade)} {localization.Get("Gear.Param"+kind.Param)}{rate}");
                    }
                foreach(var param in melds.Select(meld=>catalog.FindMateria(meld.Row)?.Param ?? 0).Where(param=>param!=0).Distinct())
                    if(GearRules.Wasted(item,hq,catalog.MateriaValues(melds),param) is > 0 and var lost)
                        ImGui.TextColored(Error,localization.Format("Gear.Wasted",lost,localization.Get("Gear.Param"+param)));
                DrawPieceCaps(item,hq,catalog.MateriaValues(melds),CombatCategory(state.Category));
                ImGui.TextDisabled(localization.Get("Gear.SocketsClick"));
            }
        }
        if(ImGui.IsItemClicked())ImGui.OpenPopup("GearMateria");
        if(!ImGui.BeginPopup("GearMateria"))return;
        try{DrawMateriaEditor(catalog,state,slot,item,hq,melds);}
        finally{ImGui.EndPopup();}
    }

    // « 12 % (NQ) » from the game data, « 12 % (NQ, Teamcraft) » for grades XI and XII, or « rate unknown ».
    private string OvermeldRateText(GearCatalog catalog,MateriaKind kind,int grade,int advancedIndex,bool hq)
        =>catalog.OvermeldRate(kind,grade,advancedIndex,hq) is { } rate
            ? localization.Format(rate.Community ? "Gear.OvermeldRateCommunity":"Gear.OvermeldRate",rate.Percent.ToString("0.#",Numbers),hq ? "HQ":"NQ")
            :localization.Get("Gear.OvermeldRateUnknown");

    private void DrawMateriaEditor(GearCatalog catalog,CharacterStatsState state,GearSlot slot,GearItem item,bool hq,IReadOnlyList<MeldedMateria> melds)
    {
        ImGui.TextColored(UI.FfxivTheme.Gold,$"{localization.Get("Gear.Slot"+slot)} · {item.Name}");
        var list=melds.ToList();var changed=false;
        // Only the stats of the job's kind (combat or crafting / gathering).
        var combat=CombatCategory(state.Category);
        var kinds=catalog.Materia.Where(kind=>combat ? kind.Param is not (10 or 11 or >= 70):kind.Param is 10 or 11 or >= 70).ToArray();
        for(var socket=0;socket<item.SocketCount;socket++)
        {
            ImGui.PushID(socket);
            try
            {
                var overmeld=socket>=item.MateriaSlots;
                ImGui.TextDisabled(overmeld ? localization.Format("Gear.SocketOvermeld",socket+1):localization.Format("Gear.Socket",socket+1));
                ImGui.SameLine(110);
                var current=socket<list.Count ? list[socket]:default;
                var kind=current.Row==0 ? null:catalog.FindMateria(current.Row);
                ImGui.SetNextItemWidth(200);
                if(ImGui.BeginCombo("##Kind",kind is null ? "—":localization.Get("Gear.Param"+kind.Param)))
                {
                    try
                    {
                        if(ImGui.Selectable("—",kind is null) && socket<list.Count){list.RemoveAt(socket);changed=true;}
                        foreach(var option in kinds)
                        {
                            // Highest grade allowed in this socket; options that would go over the cap are marked (0.313).
                            var grade=option.Grades.Where(entry=>!overmeld || GearRules.OvermeldAllowed(entry.Grade,socket-item.MateriaSlots)).Select(entry=>entry.Grade).DefaultIfEmpty(0).Max();
                            var meld=new MeldedMateria(option.Row,grade);
                            var waste=SocketWaste(catalog,item,hq,list,socket,meld);
                            if(waste>0)ImGui.PushStyleColor(ImGuiCol.Text,Warning);
                            var picked=ImGui.Selectable(localization.Get("Gear.Param"+option.Param)+(waste>0 ? "  "+localization.Format("Gear.OverCap",waste):"")+$"##Kind{option.Row}",option.Row==current.Row);
                            if(waste>0)ImGui.PopStyleColor();
                            if(picked)
                            {
                                if(socket<list.Count)list[socket]=meld;else list.Add(meld);
                                changed=true;
                            }
                        }
                    }
                    finally{ImGui.EndCombo();}
                }
                Tip("Gear.MateriaKindTip");
                if(kind is not null)
                {
                    ImGui.SameLine();ImGui.SetNextItemWidth(170);
                    if(ImGui.BeginCombo("##Grade",kind.NameOf(current.Grade)))
                    {
                        try
                        {
                            foreach(var (grade,_,name,value) in kind.Grades)
                            {
                                if(overmeld && !GearRules.OvermeldAllowed(grade,socket-item.MateriaSlots))continue;
                                var waste=SocketWaste(catalog,item,hq,list,socket,new(kind.Row,grade));
                                if(waste>0)ImGui.PushStyleColor(ImGuiCol.Text,Warning);
                                var gradeRate=overmeld ? "  · "+OvermeldRateText(catalog,kind,grade,socket-item.MateriaSlots,hq):"";
                                var picked=ImGui.Selectable($"{name} (+{value}){gradeRate}{(waste>0 ? "  "+localization.Format("Gear.OverCap",waste):"")}##Grade{grade}",grade==current.Grade);
                                if(waste>0)ImGui.PopStyleColor();
                                if(picked){list[socket]=new(kind.Row,grade);changed=true;}
                            }
                        }
                        finally{ImGui.EndCombo();}
                    }
                    Tip("Gear.MateriaGradeTip");
                    // 0.338 (DEC-023, point 8): success rate of this materia in this advanced socket, for this piece's quality.
                    if(overmeld && socket<list.Count)
                    {
                        ImGui.SameLine();ImGui.TextColored(UI.FfxivTheme.Gold,OvermeldRateText(catalog,kind,list[socket].Grade,socket-item.MateriaSlots,hq));
                        Tip("Gear.OvermeldRateTip");
                    }
                    // 0.313 (user request): warn when this materia goes over the piece's cap for its stat.
                    if(socket<list.Count && SocketWaste(catalog,item,hq,list,socket,list[socket]) is > 0 and var lost)
                    {
                        ImGui.SameLine();
                        ImGui.PushFont(UiBuilder.IconFont);
                        try{ImGui.TextColored(Warning,FontAwesomeIcon.ExclamationTriangle.ToIconString());}
                        finally{ImGui.PopFont();}
                        ImGui.SameLine();ImGui.TextColored(Warning,localization.Format("Gear.OverCap",lost));
                        if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("Gear.OverCapTip",lost,localization.Get("Gear.Param"+kind.Param),item.Caps?.GetValueOrDefault(kind.Param) ?? 0));
                    }
                }
            }
            finally{ImGui.PopID();}
            if(socket>=list.Count)break;
        }
        if(item.SocketCount>item.MateriaSlots)ImGui.TextDisabled(localization.Get("Gear.OvermeldNote"));
        // What the materia give once capped.
        foreach(var param in list.Select(meld=>catalog.FindMateria(meld.Row)?.Param ?? 0).Where(param=>param!=0).Distinct())
        {
            var values=catalog.MateriaValues(list);
            var gained=GearRules.PieceParams(item,hq,values).GetValueOrDefault(param)-item.ParamsFor(hq).GetValueOrDefault(param);
            var lost=GearRules.Wasted(item,hq,values,param);
            var cap=item.Caps?.GetValueOrDefault(param) ?? 0;
            ImGui.TextColored(lost>0 ? Error:Success,localization.Format("Gear.MeldResult",localization.Get("Gear.Param"+param),gained,lost,cap));
        }
        DrawPieceCaps(item,hq,catalog.MateriaValues(list),combat);
        if(gearMeldsWorn(slot) is { Count:>0 } wornMelds && ImGui.Button(localization.Get("Gear.MateriaWorn"))){list=wornMelds.ToList();changed=true;}
        ImGui.SameLine();
        if(ImGui.Button(localization.Get("Gear.MateriaClear"))){list.Clear();changed=true;}
        Tip("Gear.MateriaClearTip");
        // 0.331: the reset tooltip only on its own button (with no planned materia it used to land on « Remove all », which does the opposite).
        if(configuration.GearMateria?.GetValueOrDefault(state.JobId)?.ContainsKey(slot)==true)
        {
            ImGui.SameLine();
            var reset=ImGui.Button(localization.Get("Gear.MateriaReset"));
            Tip("Gear.MateriaResetTip");
            if(reset){configuration.GearMateria[state.JobId].Remove(slot);SaveGearPlan();return;}
        }
        if(!changed)return;
        var plans=configuration.GearMateria??=[];
        if(!plans.TryGetValue(state.JobId,out var jobPlan))plans[state.JobId]=jobPlan=[];
        jobPlan[slot]=list.Select(meld=>meld.Code).ToList();
        SaveGearPlan();
    }

    // 0.312 (user request: « the caps of each piece for each stat »): every stat a materia can raise on this piece — value with its
    // materia, the piece's cap, and the room left (gold at the cap).
    private void DrawPieceCaps(GearItem item,bool hq,IReadOnlyList<(int Param,int Value)> materia,bool combat)
    {
        if(item.Caps is not { Count:>0 } caps)return;
        var values=GearRules.PieceParams(item,hq,materia);
        if(!ImGui.BeginTable("PieceCaps",3,ImGuiTableFlags.SizingFixedFit|ImGuiTableFlags.RowBg))return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("Gear.ColumnStat"));ImGui.TableSetupColumn(localization.Get("Gear.ColumnCap"));ImGui.TableSetupColumn(localization.Get("Gear.ColumnRoom"));
            ImGui.TableHeadersRow();
            foreach(var param in GearStatOrder.Where(caps.ContainsKey))
            {
                if(combat ? param is 10 or 11 or >= 70:param is not (10 or 11 or >= 70))continue;
                var value=values.GetValueOrDefault(param);var cap=caps[param];
                ImGui.TableNextRow();ImGui.TableNextColumn();ImGui.TextUnformatted(localization.Get("Gear.Param"+param));
                ImGui.TableNextColumn();ImGui.TextColored(value>=cap ? UI.FfxivTheme.Gold:Text,$"{value} / {cap}");
                ImGui.TableNextColumn();
                if(value>=cap)ImGui.TextColored(UI.FfxivTheme.Gold,localization.Get("Gear.AtCap"));else ImGui.TextDisabled($"+{cap-value}");
            }
        }
        finally{ImGui.EndTable();}
    }

    // R-11 step 2 (0.314): what the character owns — bags, armoury chest, glamour dresser, armoire, saddlebag, retainers (the same
    // counts as the item tooltips, Inventory/ItemHoldings.cs), cached per item until the inventory or the Bank changes.
    private readonly Dictionary<uint,Inventory.ItemHoldingsSummary> gearOwned=[];
    private object? gearOwnedKey;
    private (Inventory.InventorySnapshot? Current,Inventory.BankState State,ulong CharacterId) GearOwnedSource()=>(inventory.Current,bank.State,bankBridge.State.CharacterId);
    private Inventory.ItemHoldingsSummary? GearOwned(uint id)
    {
        var key=GearOwnedSource();
        if(!Equals(gearOwnedKey,key)){gearOwned.Clear();gearOwnedKey=key;}
        if(gearOwned.TryGetValue(id,out var known))return known;
        try{return gearOwned[id]=Inventory.ItemHoldings.Build(id,key.CharacterId,key.Current,key.State.Entries,Inventory.StorageFilter.All);}
        catch(Exception error){log.Warning(error,"Gear ownership could not be read.");return null;}
    }
    private bool IsWorn(uint id)=>(EquippedGear?.Invoke() ?? []).Any(piece=>piece.ItemId==id);

    // ✓ owned (worn, or in a storage Tensho knows), ✗ not owned; the tooltip says where.
    private void DrawOwnedMark(uint id)
    {
        var worn=IsWorn(id);var owned=GearOwned(id);
        var have=worn || owned is { Total:>0 };
        DrawStateMark(have);
        if(!ImGui.IsItemHovered())return;
        using(TooltipScope.Begin())
        {
            if(worn)ImGui.TextColored(Success,localization.Get("Gear.Worn"));
            if(owned is null || !owned.HasObservation){if(!worn)ImGui.TextDisabled(localization.Get("HoverUi.OwnedUnknown"));return;}
            foreach(var row in owned.Rows.Where(row=>row.Nq+row.Hq>0))ImGui.TextUnformatted(localization.Format("HoverUi.StorageRow",StorageRowName(row),(row.Nq+row.Hq).ToString("N0",Numbers)));
            if(!have)ImGui.TextDisabled(localization.Get(owned.HasUnknown ? "Gear.NotOwnedPartial":"Gear.NotOwned"));
        }
    }

    // Pieces of the plan the character does not own yet, each with its sources (the item menu's groups: vendors, crafting, duties…).
    private void DrawGearToObtain(GearCatalog catalog,Dictionary<GearSlot,(GearItem Item,bool Hq,IReadOnlyList<(int,int)> Materia)> planned)
    {
        var pieces=planned.Select(pair=>(Slot:pair.Key,Item:gearLooks.GetValueOrDefault(pair.Key) ?? pair.Value.Item))
            .Concat(gearLooks.Where(pair=>!planned.ContainsKey(pair.Key)).Select(pair=>(Slot:pair.Key,Item:pair.Value)))
            .Where(piece=>!IsWorn(piece.Item.Id) && GearOwned(piece.Item.Id) is not { Total:>0 })
            .GroupBy(piece=>piece.Item.Id).Select(group=>group.First()).OrderBy(piece=>piece.Slot).ToArray();
        if(pieces.Length==0 || !TenshoAssistant.UI.UiComponents.Header(localization.Format("Gear.ToObtain",pieces.Length),ImGuiTreeNodeFlags.DefaultOpen))return;
        var items=itemCatalog.State.Catalog;
        foreach(var (slot,item) in pieces)
        {
            ImGui.PushID("Obtain"+item.Id);
            try
            {
                ItemCell(items?.Find(item.Id),item.Id,24,$"{item.Name} · {localization.Get("Gear.Slot"+slot)}");
                var groups=GearSources(item.Id);
                if(groups.Count==0){ImGui.SameLine();ImGui.TextDisabled(localization.Get("SourceLinks.None"));continue;}
                foreach(var (label,lines) in groups)
                {
                    ImGui.SameLine();
                    if(ImGui.SmallButton(label+"##"+label))ImGui.OpenPopup("ObtainSource"+label);
                    if(!ImGui.BeginPopup("ObtainSource"+label))continue;
                    try
                    {
                        foreach(var line in lines)
                        {
                            if(ImGui.Selectable(line.Text+"##"+line.Text))line.Open();
                            if(line.Hint is { Length:>0 } hint && ImGui.IsItemHovered())WrappedTooltip(hint);
                            if(line.Flag is { } flag){ImGui.SameLine();if(ImGui.SmallButton(localization.Get("Gear.Flag")+"##Flag"+line.Text))flag();}
                        }
                    }
                    finally{ImGui.EndPopup();}
                }
            }
            finally{ImGui.PopID();}
        }
    }

    // 0.331 (« nothing heavy in Draw »): the sources of a piece to obtain, rebuilt at most once a second (and on a language change) instead
    // of every frame. Not keyed on the ownership like gearOwned: the sources follow community data (Garland, wiki…) that arrive later.
    private readonly Dictionary<uint,(long At,int Revision,IReadOnlyList<(string Label,IReadOnlyList<ItemSourceLine> Lines)> Groups)> gearSources=[];
    private IReadOnlyList<(string Label,IReadOnlyList<ItemSourceLine> Lines)> GearSources(uint id)
    {
        var now=Environment.TickCount64;
        if(gearSources.TryGetValue(id,out var known) && known.Revision==localization.Revision && now-known.At<1000)return known.Groups;
        if(gearSources.Count>=64)gearSources.Clear();
        var groups=SourceMenu(id);
        gearSources[id]=(now,localization.Revision,groups);
        return groups;
    }

    /// <summary>Points this socket's materia would lose to the cap, the sockets before it being melded first (as in game).</summary>
    private static int SocketWaste(GearCatalog catalog,GearItem item,bool hq,IReadOnlyList<MeldedMateria> list,int socket,MeldedMateria meld)
    {
        if(catalog.FindMateria(meld.Row) is not { } kind)return 0;
        var before=list.Take(socket).ToList();
        var with=before.Append(meld).ToList();
        return GearRules.Wasted(item,hq,catalog.MateriaValues(with),kind.Param)-GearRules.Wasted(item,hq,catalog.MateriaValues(before),kind.Param);
    }

    private IReadOnlyList<MeldedMateria>? gearMeldsWorn(GearSlot slot)
        =>(EquippedGear?.Invoke() ?? []).FirstOrDefault(piece=>piece.Slot==slot)?.Melds;

    private void StoreGearPlan(uint job,Dictionary<GearSlot,uint> plan)
    {
        var plans=configuration.GearPlans??=[];
        if(plan.Count==0)plans.Remove(job);else plans[job]=plan;
        SaveGearPlan();
    }

    private void SaveGearPlan()=>TrySave("Gear plan");
}
