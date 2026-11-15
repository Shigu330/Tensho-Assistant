using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1444, twelfth slice, GPT review of 11/11, accepted by the user): the collections family out of Plugin.cs, after the desktop (0.1225),
// persistence (0.1239), server bar (0.1240), panels (0.1249), discoveries (0.1250), voyages (0.1403), map (0.1404), gathering (0.1412), ocean
// fishing (0.1417), chocobo (0.1419) and chat (0.1443) families. The music playing now and its orchestrion roll (X-02), the title changer (X-03),
// the achievement progress (C-15, asked from the server only on the player's click) and the extra collections (C-04), each created in one
// install path and released in one release path of the plugin's registrations, in this file, in the order of before. Plugin.cs calls the family
// once, at the same place. CollectionsFamily1444Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The collections family; and what the main window reads from it.</summary>
    private void RegisterCollections(IDalamudPluginInterface pluginInterface,Configuration configuration,IDataManager data,IFramework framework,IClientState client,
        IPlayerState player,IUnlockState unlocks,TenshoDataFile tenshoData)
    {
        // g, X-02 (0.370): the music playing now (BGMSystem, memory only) and its roll (BGM file = OrchestrionPath file).
        Collections.BgmReader? bgm=null;
        registrations.Register(()=>bgm=new Collections.BgmReader(framework,client,log),()=>bgm?.Dispose());
        Dictionary<uint,uint>? rollByBgm=null;
        mainWindow.PlayingRoll=()=>
        {
            bgm?.Want();
            if(bgm?.Playing is not { } playing)return null;
            rollByBgm??=BuildRollByBgm();
            return rollByBgm.TryGetValue(playing,out var roll) ? roll:null;
        };
        mainWindow.MusicRead=()=>bgm?.Playing is not null;
        Dictionary<uint,uint> BuildRollByBgm()
        {
            var rolls=data.GetExcelSheet<Lumina.Excel.Sheets.OrchestrionPath>().Where(row=>row.RowId!=0 && !row.File.IsEmpty)
                .GroupBy(row=>row.File.ExtractText(),StringComparer.OrdinalIgnoreCase).ToDictionary(group=>group.Key,group=>group.First().RowId,StringComparer.OrdinalIgnoreCase);
            return data.GetExcelSheet<Lumina.Excel.Sheets.BGM>().Where(row=>!row.File.IsEmpty)
                .Select(row=>(row.RowId,Roll:rolls.GetValueOrDefault(row.File.ExtractText()))).Where(pair=>pair.Roll!=0).ToDictionary(pair=>pair.RowId,pair=>pair.Roll);
        }
        // g, X-03 (0.369): wear a title (click), or the job's title on a job change when the player turned it on.
        Collections.TitleChanger? titleChanger=null;
        registrations.Register(()=>mainWindow.TitleChangerSource=titleChanger=new Collections.TitleChanger(framework,client,player,log,
            ()=>configuration.JobTitlesEnabled,()=>configuration.JobTitles),()=>titleChanger?.Dispose());
        // g, C-15 (0.368): achievement progress, read passively; asked from the server only on the player's click.
        Collections.AchievementProgressWatcher? achievementProgress=null;
        registrations.Register(()=>mainWindow.AchievementProgressSource=achievementProgress=new Collections.AchievementProgressWatcher(framework,client,player,log,
            new Collections.AchievementProgressData(Path.Combine(pluginInterface.GetPluginConfigDirectory(),"AchievementProgress.json"),tenshoData),id=>configuration.TrackedAchievements?.Contains(id)==true,mainWindow.AchievementCompleted),
            ()=>achievementProgress?.Dispose());
        // g, C-04 (0.365): hairstyles, accessories, glasses, bardings, portraits, master books, McGuffins, Occult Crescent lore.
        Collections.ExtraCollectionReader? extraCollections=null;
        registrations.Register(()=>extraCollections=new Collections.ExtraCollectionReader(framework,client,player,unlocks,data,log),()=>extraCollections?.Dispose());
        mainWindow.ExtraCollections=()=>{extraCollections?.Want();return extraCollections?.State;};
        mainWindow.ExtraCollectionsUnavailable=()=>extraCollections?.Unavailable==true;
    }
}
