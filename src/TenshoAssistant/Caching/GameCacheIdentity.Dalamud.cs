using Dalamud.Plugin.Services;
namespace TenshoAssistant.Caching;
internal static class GameCacheIdentity
{
    public static string For(IDataManager data)=>typeof(GameCacheIdentity).Assembly.GetName().Version+"|"+data.Language+"|"+string.Join(";",data.GameData.Repositories.Values.OrderBy(row=>row.Name).Select(row=>$"{row.Name}={row.Version}"));
}
