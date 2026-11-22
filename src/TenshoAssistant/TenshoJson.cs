using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace TenshoAssistant;

/// <summary>
/// X-685 (0.1741, the memory that grows with each reload: every copy of Tensho unloaded before stayed in the game process, « context alive »
/// in the log): System.Text.Json is the runtime's own, shared by the whole game process. The type information it makes for a type is kept by
/// the options that made it; its default options (JsonSerializer called without options) and any options equal to someone else's (the same
/// settings: another plugin's, Dalamud's, the next copy of Tensho's) share one cache for the whole process, so Tensho's types stayed in it
/// after an unload, and with them the unloaded copy of Tensho (its code and its static tables) until the game closed. Tried out of the game
/// (a copy loaded in a context that can unload: JsonUnload1741Tests; docs/evidence/0.1741-unload-json.md). Every options of Tensho is made
/// here, with a resolver of its own (never equal to anyone else's options): its cache goes with this copy of Tensho. The reflection's
/// accessors the runtime makes for it expire on their own once others use System.Text.Json.
/// </summary>
public static class TenshoJson
{
    /// <summary>Options of Tensho: <paramref name="options"/> given a resolver of its own when it has none (before any use).</summary>
    public static JsonSerializerOptions Own(JsonSerializerOptions options)
    {
        options.TypeInfoResolver??=new DefaultJsonTypeInfoResolver();
        return options;
    }

    /// <summary>The default settings, in place of JsonSerializer without options.</summary>
    public static readonly JsonSerializerOptions Plain=Own(new());
}
