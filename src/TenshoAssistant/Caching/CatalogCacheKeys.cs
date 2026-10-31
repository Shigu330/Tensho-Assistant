namespace TenshoAssistant.Caching;

// Static catalogue copies: rows only, indexes are rebuilt. DataVersion is rechecked after the hashed file lookup.
public sealed record CatalogCacheData<T>(string DataVersion,IReadOnlyList<T> Rows);

/// <summary>Keys static catalogue copies on game data and record shape, never on the plugin version.</summary>
public static class CatalogCacheKeys
{
    public static string Shape(params Type[] types)=>string.Join("/",types.Select(Describe));
    public static string Describe(Type type)=>type.IsEnum
        ? type.Name+"("+string.Join(",",Enum.GetNames(type))+")"
        : type.Name+"("+string.Join(",",type.GetProperties().OrderBy(property=>property.Name,StringComparer.Ordinal)
            .Select(property=>property.Name+":"+TypeName(property.PropertyType)))+")";
    private static string TypeName(Type type)=>Nullable.GetUnderlyingType(type) is { } inner ? inner.Name+"?"
        : type.IsGenericType ? type.Name+"<"+string.Join(",",type.GetGenericArguments().Select(TypeName))+">":type.Name;
    public static string Version(string dataVersion,int copyRules,string shape)=>$"{dataVersion}|copy={copyRules}|{shape}";
}
