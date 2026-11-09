namespace TenshoAssistant.Inventory;

/// <summary>
/// 0.839 (X-66, user report: the retainer's « Vin de cuisine » framed instead of « Vin de cuisine de qualité », same icon). A cell of a storage window shows an icon
/// and a quantity; the open storage's stacks are read too. A cell is framed when every stack with its icon and quantity is a wanted item: two items alike in both
/// are left unframed rather than guessed. A storage not read falls back to the icon alone.
/// </summary>
public static class StorageCellMatch
{
    /// <summary>One stack of the open storage: the icon it shows (HQ: 1 000 000 + icon), its quantity, whether a list wants it.</summary>
    public readonly record struct Stack(uint Icon,int Quantity,bool Wanted);

    /// <param name="quantity">The quantity the cell shows; null when it shows none (then any quantity of that icon).</param>
    public static bool Marks(uint icon,int? quantity,IReadOnlyList<Stack> stacks,IReadOnlySet<uint> wantedIcons)
    {
        if(stacks.Count==0)return wantedIcons.Contains(icon);
        var alike=stacks.Where(stack=>stack.Icon==icon && (quantity is null || stack.Quantity==quantity)).ToArray();
        return alike.Length>0 && alike.All(stack=>stack.Wanted);
    }

    /// <summary>The number a cell shows, digits only (« 1.053 » is 1053); null when it shows none.</summary>
    public static int? Quantity(string? text)
    {
        if(string.IsNullOrEmpty(text))return null;
        var digits=new string(text.Where(char.IsAsciiDigit).ToArray());
        return digits.Length is > 0 and <= 7 ? int.Parse(digits,System.Globalization.CultureInfo.InvariantCulture):null;
    }
}
