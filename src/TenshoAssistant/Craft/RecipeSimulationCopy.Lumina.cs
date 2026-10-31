using Lumina.Excel.Sheets;

namespace TenshoAssistant.Craft;

internal static class RecipeSimulationCopy
{
    public static CraftRecipeParameters Copy(Recipe row,RecipeLevelTable table)
    {
        if(row.RowId==0 || row.ItemResult.RowId==0 || table.RowId!=row.RecipeLevelTable.RowId)
            throw new InvalidDataException("Recipe simulation projection has foreign identities.");
        var collectable=row.ItemResult.Value.AlwaysCollectable;
        var quality=CraftRecipeQuality.Maximum(row.CanHq,collectable,checked((int)row.RequiredQuality),checked((int)table.Quality),row.QualityFactor);
        return new(table.Durability*row.DurabilityFactor/100,table.Difficulty*row.DifficultyFactor/100,quality,
            table.ProgressDivider,table.QualityDivider,table.ProgressModifier,table.QualityModifier,row.RequiredCraftsmanship,row.RequiredControl,
            checked((int)((long)quality*row.MaterialQualityFactor/100)),row.IsExpert,row.MaxAdjustableJobLevel.RowId!=0,row.IsSpecializationRequired,
            collectable,checked((int)row.RequiredQuality));
    }
}
