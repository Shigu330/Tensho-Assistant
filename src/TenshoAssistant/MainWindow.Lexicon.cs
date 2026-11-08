using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.Lexicon;

namespace TenshoAssistant;

// W-16 (0.719): the game's words in French and in English, from its own sheets. Loaded and searched off the frame. docs/evidence/0.719-lexicon.md.
internal sealed partial class MainWindow
{
    private const int LexiconPageSize=50;
    internal Func<IReadOnlyList<LexiconEntry>?>? LexiconLoad { get; set; }
    private Task<IReadOnlyList<LexiconEntry>?>? lexiconLoad;
    private IReadOnlyList<LexiconEntry>? lexiconWords;
    private bool lexiconFailed;
    private string lexiconQuery="";
    private LexiconKind? lexiconKind;
    private int lexiconPage;
    private (string Query,LexiconKind? Kind)? lexiconAsked;
    private Task<(IReadOnlyList<LexiconEntry> Rows,int Total)>? lexiconSearch;
    private (IReadOnlyList<LexiconEntry> Rows,int Total) lexiconFound=([],0);

    private void DrawLexicon()
    {
        Heading(localization.Get("Lexicon.Title"),localization.Get("Lexicon.Subtitle"));
        if(lexiconWords is null && !lexiconFailed && lexiconLoad is null && LexiconLoad is { } load)lexiconLoad=Task.Run(load);
        if(lexiconLoad is { IsCompleted:true } done)
        {
            lexiconLoad=null;
            lexiconWords=done.IsCompletedSuccessfully ? done.Result:null;
            lexiconFailed=lexiconWords is null;
        }
        if(lexiconFailed){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("Lexicon.Unavailable"));return;}
        if(lexiconWords is not { } words){ImGui.TextDisabled(localization.Get("Lexicon.Loading"));return;}
        ImGui.SetNextItemWidth(280);
        ImGui.InputTextWithHint("###LexiconQuery",localization.Get("Lexicon.Search"),ref lexiconQuery,100);
        Tip("Lexicon.SearchTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(200);
        if(ImGui.BeginCombo("###LexiconKind",lexiconKind is { } chosen ? localization.Get("Lexicon.Kind"+chosen):localization.Get("Lexicon.KindAll")))
        {
            try
            {
                if(ImGui.Selectable(localization.Get("Lexicon.KindAll"),lexiconKind is null))lexiconKind=null;
                foreach(var kind in Enum.GetValues<LexiconKind>())
                    if(ImGui.Selectable(localization.Get("Lexicon.Kind"+kind),lexiconKind==kind))lexiconKind=kind;
            }
            finally{ImGui.EndCombo();}
        }
        Tip("Lexicon.KindTip");
        // One search at a time, off the frame; a word typed in the meantime starts the next one.
        if(lexiconSearch is { IsCompleted:true } searched)
        {
            lexiconSearch=null;
            if(searched.IsCompletedSuccessfully)lexiconFound=searched.Result;
        }
        var wanted=(lexiconQuery,lexiconKind);
        if(lexiconSearch is null && lexiconAsked!=wanted)
        {
            lexiconAsked=wanted;lexiconPage=0;
            var (query,type)=wanted;
            lexiconSearch=Task.Run(()=>LexiconBoard.Search(words,query,type));
        }
        if(lexiconQuery.Trim().Length<LexiconBoard.MinimumQuery){ImGui.TextDisabled(localization.Format("Lexicon.Hint",words.Count.ToString("N0")));return;}
        if(lexiconSearch is not null && lexiconFound.Rows.Count==0){ImGui.TextDisabled(localization.Get("Lexicon.Searching"));return;}
        if(lexiconFound.Total==0){ImGui.TextDisabled(localization.Get("Lexicon.None"));return;}
        var (rows,page,pages)=CraftFromStock.Paged(lexiconFound.Rows,lexiconPage,LexiconPageSize);
        lexiconPage=page;
        ImGui.TextColored(Accent,localization.Format(lexiconFound.Total>lexiconFound.Rows.Count ? "Lexicon.CountMore":"Lexicon.Count",lexiconFound.Total.ToString("N0"),lexiconFound.Rows.Count.ToString("N0")));
        ImGui.SameLine();
        ImGui.BeginDisabled(lexiconPage<=0);
        try{if(ImGui.SmallButton("<###LexiconPrevious"))lexiconPage--;}
        finally{ImGui.EndDisabled();}
        Tip("Lexicon.PreviousTip");
        ImGui.SameLine();ImGui.TextUnformatted(localization.Format("Lexicon.Page",lexiconPage+1,pages));
        ImGui.SameLine();
        ImGui.BeginDisabled(lexiconPage>=pages-1);
        try{if(ImGui.SmallButton(">###LexiconNext"))lexiconPage++;}
        finally{ImGui.EndDisabled();}
        Tip("Lexicon.NextTip");
        if(!ImGui.BeginTable("###LexiconRows",3,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.Resizable))return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("Lexicon.ColumnFrench"));
            ImGui.TableSetupColumn(localization.Get("Lexicon.ColumnEnglish"));
            ImGui.TableSetupColumn(localization.Get("Lexicon.ColumnKind"));
            ImGui.TableHeadersRow();
            for(var index=0;index<rows.Length;index++)
            {
                var row=rows[index];
                ImGui.TableNextRow();
                ImGui.TableNextColumn();DrawLexiconWord(row.French,$"LexiconFrench{index}");
                ImGui.TableNextColumn();DrawLexiconWord(row.English,$"LexiconEnglish{index}");
                ImGui.TableNextColumn();ImGui.TextDisabled(localization.Get("Lexicon.Kind"+row.Kind));
            }
        }
        finally{ImGui.EndTable();}
    }

    private void DrawLexiconWord(string word,string id)
    {
        if(ImGui.Selectable($"{word}##{id}"))ImGui.SetClipboardText(word);
        Tip("Lexicon.CopyTip");
    }
}
