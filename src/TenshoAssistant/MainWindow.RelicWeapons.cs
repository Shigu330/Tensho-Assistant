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
        if(relicRead is { IsCompleted:true } read)
        {
            relicRead=null;
            relicReadFailed=!read.IsCompletedSuccessfully;
            relicProgress=read.IsCompletedSuccessfully ? read.Result:null;
        }
        ImGui.BeginDisabled(ReadRelics is null || relicRead is not null);
        try{if(ImGui.Button(localization.Get("Relics.Read")))relicRead=ReadRelics!();}
        finally{ImGui.EndDisabled();}
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get("Relics.ReadTip"));
        RefreshRecipeHoldingsCache();
        var tabs=new List<(string Key,string Label,Action Draw)>{("Zodiac",localization.Get("Relics.FamilyZodiac"),()=>{DrawRelicFamily("Zodiac",catalog.Lines);DrawRelicBook(catalog);})};
        foreach(var (key,lines) in catalog.Families ?? [])tabs.Add((key,localization.Get("Relics.Family"+key),()=>DrawRelicFamily(key,lines)));
        DetailTabs("RelicTabs",tabs,ref relicTabFocus);
    }

    private void DrawRelicFamily(string family,IReadOnlyList<IReadOnlyList<uint>> lines)
    {
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
                else if(held>0)ImGui.TextColored(Success,held.ToString("N0"));
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
