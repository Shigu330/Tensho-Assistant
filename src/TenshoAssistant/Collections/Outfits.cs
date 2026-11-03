namespace TenshoAssistant.Collections;

/// <summary>An outfit of the glamour dresser (MirageStoreSetItem: the outfit item and its pieces, 0 left out).</summary>
public sealed record OutfitDefinition(uint SetItemId,IReadOnlyList<uint> Pieces);

/// <summary>Where the character stands with an outfit: stored as an outfit, every piece in the dresser, every piece owned, or not.</summary>
public enum OutfitState { Missing, Complete, InDresser, Stored }

/// <summary>Pieces owned anywhere (saved storages and live bags) and pieces in the glamour dresser's own slots.</summary>
public sealed record OutfitProgress(OutfitDefinition Outfit,int Owned,int InDresser,bool Stored)
{
    public OutfitState State=>Stored ? OutfitState.Stored:InDresser==Outfit.Pieces.Count ? OutfitState.InDresser
        :Owned==Outfit.Pieces.Count ? OutfitState.Complete:OutfitState.Missing;
}

/// <summary>
/// g, C-05 (0.362): outfit progress — the game turns pieces kept in the glamour dresser into an outfit once all of them are there. A
/// stored outfit counts all its pieces as owned. Original code (ideas from Glamour Log and AkuItemSets, read only).
/// </summary>
public static class Outfits
{
    /// <param name="owned">Item ids held anywhere by the character (bags, armoury, dresser, armoire, saddlebags, retainers).</param>
    /// <param name="dresser">Item ids in the dresser's own slots (outfit pieces not included).</param>
    public static OutfitProgress Progress(OutfitDefinition outfit,IReadOnlySet<uint> owned,IReadOnlySet<uint> dresser)
    {
        var stored=dresser.Contains(outfit.SetItemId);
        return new(outfit,stored ? outfit.Pieces.Count:outfit.Pieces.Count(owned.Contains),stored ? outfit.Pieces.Count:outfit.Pieces.Count(dresser.Contains),stored);
    }
}
