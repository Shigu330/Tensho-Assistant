using System.Globalization;
using System.Text;

namespace TenshoAssistant.Craft;

/// <summary>
/// A-09 (0.423): a Teamcraft list import link — « https://ffxivteamcraft.com/import/ » then, URL-encoded, the base64 of rows
/// « itemId,recipeId,quantity » joined by « ; », with « null » for no recipe (format of Teamcraft's import page, MIT, read only:
/// apps/client/src/app/pages/import/import/import.component.ts). Built locally; nothing is sent, the player copies or opens it.
/// </summary>
public static class TeamcraftExport
{
    public const string ImportUrl="https://ffxivteamcraft.com/import/";

    public static string? Link(IEnumerable<(uint ItemId,uint? RecipeId,long Quantity)> rows)
    {
        var parts=rows.Where(row=>row.ItemId!=0 && row.Quantity>0)
            .Select(row=>string.Create(CultureInfo.InvariantCulture,$"{row.ItemId},{(row.RecipeId is { } recipe and >0 ? recipe.ToString(CultureInfo.InvariantCulture):"null")},{Math.Min(row.Quantity,999999)}"))
            .ToArray();
        if(parts.Length==0)return null;
        return ImportUrl+Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(';',parts))));
    }
}
