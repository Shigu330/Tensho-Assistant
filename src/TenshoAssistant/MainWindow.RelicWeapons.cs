using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Relics;

namespace TenshoAssistant;

// W-03 (0.708): the Zodiac weapons, the stages held and the book in progress, from the game's sheets and what it holds. Nothing is requested or done in the game. docs/evidence/0.708-relic-weapons.md.
internal sealed partial class MainWindow
{
    internal Func<RelicCatalog?>? RelicCatalogLoad { get; set; }
    internal Func<Task<RelicProgress>>? ReadRelics { get; set; }
    private RelicCatalog? relicCatalog;
    private bool relicCatalogTried,relicReadFailed;
    private Task<RelicProgress>? relicRead;
    private RelicProgress? relicProgress;

    private Task<RelicCatalog?>? relicCatalogTask;
    private readonly Dictionary<string,int> relicLines=[];
    private string? relicTabFocus;

    /// <summary>X-40 (0.1002): the book's reading, taken by the page or by the nearby monsters, whichever sees it done first.</summary>
    private void ConsumeRelicRead()
    {
        if(relicRead is not { IsCompleted:true } read)return;
        relicRead=null;
        relicReadFailed=!read.IsCompletedSuccessfully;
        relicProgress=read.IsCompletedSuccessfully ? read.Result:null;
    }
    private void DrawRelicWeapons()
    {
        Heading(localization.Get("Relics.Title"),localization.Get("Relics.Subtitle"));
        // W-13 (0.716): every weapon of the sheet is looked at: off the frame.
        if(!relicCatalogTried && RelicCatalogLoad is { } load)
        {
            relicCatalogTried=true;
            relicCatalogTask=Task.Run(load);
            relicRead=ReadRelics?.Invoke();
        }
        if(relicCatalogTask is { IsCompleted:false }){ImGui.TextDisabled(localization.Get("Relics.Loading"));return;}
        if(relicCatalogTask is { } done && relicCatalog is null){relicCatalog=done.IsCompletedSuccessfully ? done.Result:null;}
        if(relicCatalog is not { Lines.Count:>0 } catalog){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("Relics.SheetsUnavailable"));return;}
        ImGui.TextWrapped(localization.Get("Relics.Help"));
        ConsumeRelicRead();
        ImGui.BeginDisabled(ReadRelics is null || relicRead is not null);
        try{if(ImGui.Button(localization.Get("Relics.Read")))relicRead=ReadRelics!();}
        finally{ImGui.EndDisabled();}
        Tip("Relics.ReadTip");
        RefreshRecipeHoldingsCache();
        var tabs=new List<(string Key,string Label,Action Draw)>{("Zodiac",localization.Get("Relics.FamilyZodiac"),()=>{DrawRelicFamily("Zodiac",catalog.Lines);DrawRelicBook(catalog);})};
        foreach(var (key,lines) in catalog.Families ?? [])
            tabs.Add((key,localization.Get("Relics.Family"+key),catalog.Armours?.GetValueOrDefault(key) is { Count:>0 } sets
                ? ()=>DetailTabs($"RelicParts{key}",[("Weapons",localization.Get("Relics.PartWeapons"),()=>DrawRelicFamily(key,lines)),("Armour",localization.Get("Relics.PartArmour"),()=>DrawRelicArmour(key,sets))],ref relicPartFocus)
                :()=>DrawRelicFamily(key,lines)));
        DrawRelicViewChoice();
        DetailTabs("RelicTabs",tabs,ref relicTabFocus);
    }

    // X-90 (0.974, user request): the stage view, every job's weapon or armour of one relic version, beside the per-job view.
    private bool relicByStage;
    private readonly Dictionary<string,int> relicStageChosen=[];
    private void DrawRelicViewChoice()
    {
        ImGui.SetNextItemWidth(220);
        if(ImGui.BeginCombo(localization.Get("Relics.View")+"###RelicView",localization.Get(relicByStage ? "Relics.ViewStage":"Relics.ViewJob")))
        {
            try
            {
                if(ImGui.Selectable(localization.Get("Relics.ViewJob")+"##RelicViewJob",!relicByStage))relicByStage=false;
                if(ImGui.Selectable(localization.Get("Relics.ViewStage")+"##RelicViewStage",relicByStage))relicByStage=true;
            }
            finally{ImGui.EndCombo();}
        }
        Tip("Relics.ViewTip");
    }
    private int RelicStageCombo(string id,IReadOnlyList<string> labels)
    {
        var chosen=Math.Clamp(relicStageChosen.GetValueOrDefault(id),0,labels.Count-1);
        ImGui.SetNextItemWidth(360);
        if(ImGui.BeginCombo(localization.Get("Relics.Stage")+$"###RelicStage{id}",labels[chosen]))
        {
            try{for(var index=0;index<labels.Count;index++)if(ImGui.Selectable($"{labels[index]}##RelicStage{id}{index}",index==chosen))relicStageChosen[id]=chosen=index;}
            finally{ImGui.EndCombo();}
        }
        Tip("Relics.StageTip");
        return chosen;
    }
    private void DrawRelicStageWeapons(string family,IReadOnlyList<IReadOnlyList<uint>> lines)
    {
        if(itemCatalog.State.Catalog is not { } items){EmptyState("Relics.Loading");return;}
        var stages=RelicBoard.ByLevel(lines,id=>(int)(items.Find(id)?.ItemLevel ?? 0));
        if(stages.Count==0){EmptyState("Relics.Loading");return;}
        var chosen=RelicStageCombo("Weapons"+family,stages.Select((stage,index)=>localization.Format("Relics.StageLabel",index+1,stage.Level)).ToArray());
        long? Held(uint id){var stock=GetRecipeHoldings(id);return stock.HasObservation ? stock.Total:null;}
        if(!ImGui.BeginTable($"###RelicStageWeapons{family}",2,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV))return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("Relics.ColumnWeapon"));
            ImGui.TableSetupColumn(localization.Get("Relics.ColumnHeld"));
            ImGui.TableHeadersRow();
            var size=new Vector2(ImGui.GetTextLineHeight());
            foreach(var id in stages[chosen].Items)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();DrawItemIcon(items.Find(id),size);
                if(ImGui.IsItemHovered())DrawItemInfoTooltip(id);
                ImGui.SameLine();ImGui.TextUnformatted(items.Find(id)?.Name ?? $"#{id}");
                ImGui.TableNextColumn();
                if(Held(id) is not { } held)ImGui.TextDisabled("?");
                else if(held>0)ImGui.TextColored(Success,held.ToString("N0",Numbers));
                else ImGui.TextDisabled("0");
            }
        }
        finally{ImGui.EndTable();}
    }
    private void DrawRelicStageArmour(string family,IReadOnlyList<RelicArmourSet> sets)
    {
        var items=itemCatalog.State.Catalog;
        string Name(uint id)=>items?.Find(id)?.Name ?? $"#{id}";
        // The armour stages keep their place in every set (a stage a set does not have stays empty), so the index is the stage.
        var indexes=Enumerable.Range(0,sets.Count==0 ? 0:sets.Max(set=>set.Stages.Count)).Where(index=>sets.Any(set=>index<set.Stages.Count && set.Stages[index].Count>0)).ToArray();
        if(indexes.Length==0){EmptyState("Relics.Loading");return;}
        var labels=indexes.Select((stage,index)=>localization.Format("Relics.StageArmourLabel",index+1,Name(sets.First(set=>stage<set.Stages.Count && set.Stages[stage].Count>0).Stages[stage][0]))).ToArray();
        var chosen=indexes[RelicStageCombo("Armour"+family,labels)];
        long? Held(uint id){var stock=GetRecipeHoldings(id);return stock.HasObservation ? stock.Total:null;}
        if(!ImGui.BeginTable($"###RelicStageArmour{family}",3,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV))return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("Relics.ArmourSet"));
            ImGui.TableSetupColumn(localization.Get("Relics.ColumnArmour"));
            ImGui.TableSetupColumn(localization.Get("Relics.ColumnPieces"));
            ImGui.TableHeadersRow();
            foreach(var set in sets)
            {
                if(chosen>=set.Stages.Count || set.Stages[chosen] is not { Count:>0 } pieces)continue;
                ImGui.TableNextRow();
                ImGui.TableNextColumn();ImGui.TextUnformatted(set.Name);
                ImGui.TableNextColumn();ImGui.TextUnformatted(Name(pieces[0]));
                if(ImGui.IsItemHovered())WrappedTooltip(string.Join("\n",pieces.Select(piece=>Name(piece)+" : "+(Held(piece)?.ToString("N0",Numbers) ?? "?"))));
                ImGui.TableNextColumn();
                var (held,known)=RelicBoard.PiecesHeld(pieces,Held);
                if(known==0)ImGui.TextDisabled("?");
                else if(held==pieces.Count)ImGui.TextColored(Success,$"{held}/{pieces.Count}");
                else ImGui.TextUnformatted(known<pieces.Count ? $"{held}/{pieces.Count} ?":$"{held}/{pieces.Count}");
            }
        }
        finally{ImGui.EndTable();}
    }

    private string? relicPartFocus;
    private readonly Dictionary<string,int> relicArmourSets=[];

    // W-13 (0.720): the later relics' armour, a line per stage, its pieces in the tooltip. docs/evidence/0.720-relic-armour.md.
    private void DrawRelicArmour(string family,IReadOnlyList<RelicArmourSet> sets)
    {
        if(relicByStage){DrawRelicStageArmour(family,sets);return;}
        var items=itemCatalog.State.Catalog;
        string Name(uint id)=>items?.Find(id)?.Name ?? $"#{id}";
        var chosen=Math.Clamp(relicArmourSets.GetValueOrDefault(family),0,sets.Count-1);
        ImGui.SetNextItemWidth(320);
        if(ImGui.BeginCombo(localization.Get("Relics.ArmourSet")+$"###RelicArmourSet{family}",sets[chosen].Name))
        {
            try{for(var index=0;index<sets.Count;index++)if(ImGui.Selectable($"{sets[index].Name}##RelicArmourSet{family}{index}",index==chosen))relicArmourSets[family]=index;}
            finally{ImGui.EndCombo();}
        }
        Tip("Relics.ArmourSetTip");
        long? Held(uint id){var stock=GetRecipeHoldings(id);return stock.HasObservation ? stock.Total:null;}
        if(!ImGui.BeginTable($"###RelicArmour{family}",3,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV))return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("Relics.ColumnStage"));
            ImGui.TableSetupColumn(localization.Get("Relics.ColumnArmour"));
            ImGui.TableSetupColumn(localization.Get("Relics.ColumnPieces"));
            ImGui.TableHeadersRow();
            var stages=sets[chosen].Stages.Where(stage=>stage.Count>0).ToArray();
            for(var index=0;index<stages.Length;index++)
            {
                var pieces=stages[index];
                ImGui.TableNextRow();
                ImGui.TableNextColumn();ImGui.TextUnformatted((index+1).ToString());
                ImGui.TableNextColumn();ImGui.TextUnformatted(Name(pieces[0]));
                if(ImGui.IsItemHovered())WrappedTooltip(string.Join("\n",pieces.Select(piece=>Name(piece)+" : "+(Held(piece)?.ToString("N0",Numbers) ?? "?"))));
                ImGui.TableNextColumn();
                var (held,known)=RelicBoard.PiecesHeld(pieces,Held);
                if(known==0)ImGui.TextDisabled("?");
                else if(held==pieces.Count)ImGui.TextColored(Success,$"{held}/{pieces.Count}");
                else ImGui.TextUnformatted(known<pieces.Count ? $"{held}/{pieces.Count} ?":$"{held}/{pieces.Count}");
            }
        }
        finally{ImGui.EndTable();}
    }

    private void DrawRelicFamily(string family,IReadOnlyList<IReadOnlyList<uint>> lines)
    {
        if(relicByStage){DrawRelicStageWeapons(family,lines);return;}
        var items=itemCatalog.State.Catalog;
        string Name(uint id)=>items?.Find(id)?.Name ?? $"#{id}";
        var chosen=Math.Clamp(relicLines.GetValueOrDefault(family),0,lines.Count-1);
        var line=lines[chosen];
        ImGui.SetNextItemWidth(280);
        if(ImGui.BeginCombo(localization.Get("Relics.Weapon")+$"###RelicLine{family}",Name(line[0])))
        {
            try{for(var index=0;index<lines.Count;index++)if(ImGui.Selectable($"{Name(lines[index][0])}##RelicLine{family}{index}",index==chosen))relicLines[family]=index;}
            finally{ImGui.EndCombo();}
        }
        Tip("Relics.WeaponTip");
        long? Held(uint id){var stock=GetRecipeHoldings(id);return stock.HasObservation ? stock.Total:null;}
        var reached=RelicBoard.Reached(line,Held);
        ImGui.TextUnformatted(reached is null ? localization.Get("Relics.StocksUnknown"):reached<0 ? localization.Get("Relics.NoneHeld"):localization.Format("Relics.Reached",reached+1,line.Count,Name(line[reached.Value])));
        if(!ImGui.BeginTable($"###RelicStages{family}",3,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV))return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("Relics.ColumnStage"));
            ImGui.TableSetupColumn(localization.Get("Relics.ColumnWeapon"));
            ImGui.TableSetupColumn(localization.Get("Relics.ColumnHeld"));
            ImGui.TableHeadersRow();
            var size=new Vector2(ImGui.GetTextLineHeight());
            for(var index=0;index<line.Count;index++)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();ImGui.TextUnformatted((index+1).ToString());
                ImGui.TableNextColumn();DrawItemIcon(items?.Find(line[index]),size);
                if(ImGui.IsItemHovered())DrawItemInfoTooltip(line[index]);
                ImGui.SameLine();ImGui.TextUnformatted(Name(line[index]));
                ImGui.TableNextColumn();
                if(Held(line[index]) is not { } held)ImGui.TextDisabled("?");
                else if(held>0)ImGui.TextColored(Success,held.ToString("N0",Numbers));
                else ImGui.TextDisabled("0");
            }
        }
        finally{ImGui.EndTable();}
    }

    private void DrawRelicBook(RelicCatalog catalog)
    {
        ImGui.Separator();
        if(relicReadFailed){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("Relics.Unavailable"));return;}
        switch(relicProgress?.Status)
        {
            case null:return;
            case RelicReadStatus.NotLoaded:ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("Relics.NotLoaded"));return;
            case RelicReadStatus.Unavailable:ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("Relics.Unavailable"));return;
        }
        var progress=relicProgress!;
        if(progress.Note==0 || !catalog.Books.TryGetValue(progress.Note,out var book)){ImGui.TextDisabled(localization.Get("Relics.NoBook"));return;}
        var (done,total)=RelicBoard.Done(book,progress);
        ImGui.TextColored(Accent,localization.Format("Relics.Book",book.Name,done,total));
        void Group(string key,IReadOnlyList<RelicTarget> targets,Func<int,string> state,Func<int,bool> finished)
        {
            if(targets.Count==0)return;
            ImGui.TextDisabled(localization.Get(key));
            for(var index=0;index<targets.Count;index++)
            {
                var text=$"{targets[index].Name}{(targets[index].Place.Length>0 ? " · "+targets[index].Place:"")}  {state(index)}";
                if(finished(index))ImGui.TextColored(Success,text);else ImGui.TextUnformatted(text);
                if(key=="Relics.Fates")
                {
                    ImGui.SameLine();
                    if(ImGui.SmallButton(localization.Get("Relics.FindFate")+$"##RelicFate{index}"))OpenFate(targets[index].Name);
                    Tip("Relics.FindFateTip");
                }
            }
        }
        string Tick(bool value)=>localization.Get(value ? "Relics.Done":"Relics.Left");
        int Count(int index)=>index<progress.Monsters.Count ? progress.Monsters[index]:0;
        Group("Relics.Monsters",book.Monsters,index=>$"{Count(index)} / {book.Monsters[index].Needed}",index=>book.Monsters[index].Needed>0 && Count(index)>=book.Monsters[index].Needed);
        bool Flag(IReadOnlyList<bool> flags,int index)=>index<flags.Count && flags[index];
        Group("Relics.Dungeons",book.Dungeons,index=>Tick(Flag(progress.Dungeons,index)),index=>Flag(progress.Dungeons,index));
        Group("Relics.Fates",book.Fates,index=>Tick(Flag(progress.Fates,index)),index=>Flag(progress.Fates,index));
        Group("Relics.Leves",book.Leves,index=>Tick(Flag(progress.Leves,index)),index=>Flag(progress.Leves,index));
    }
}
