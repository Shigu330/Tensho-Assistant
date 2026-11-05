using TenshoAssistant.Localization;

namespace TenshoAssistant.Inventory;

/// <summary>
/// Shown names of the storages (0.322, review 0.316 point 7): translated from the storage kind, never from the French literals the code
/// used to build (and the bank file still holds for old records); only a retainer keeps its own name, which comes from the game.
/// </summary>
internal static class StorageNames
{
    public static string Of(LocalizationManager localization, BankStorageKind kind, string savedName) => kind switch
    {
        BankStorageKind.Retainer => localization.Format("BankUi.Retainer", savedName),
        BankStorageKind.Saddlebag => localization.Get("BankUi.Saddlebag"),
        BankStorageKind.PremiumSaddlebag => localization.Get("BankUi.PremiumSaddlebag"),
        BankStorageKind.Armoury => localization.Get("BankUi.Armoury"),
        BankStorageKind.GlamourDresser => localization.Get("BankUi.GlamourDresser"),
        BankStorageKind.Armoire => localization.Get("BankUi.Armoire"),
        BankStorageKind.Bags => localization.Get("BankUi.Bags"),
        BankStorageKind.Crystals => localization.Get("ItemsUi.Crystals"),
        BankStorageKind.Currency => localization.Get("Currencies.Tab"),
        BankStorageKind.FreeCompanyChest => localization.Get("BankUi.FreeCompanyChest"),
        _ => savedName,
    };

    public static string Of(LocalizationManager localization, StorageHolding row) => row.Label switch
    {
        StorageLabel.PlayerBags => localization.Get("BankUi.PlayerBags"),
        StorageLabel.Crystals => localization.Get("BankUi.Crystals"),
        StorageLabel.Worn => localization.Get("BankUi.Worn"),
        StorageLabel.SaddlebagMissing => localization.Get("BankUi.SaddlebagMissing"),
        StorageLabel.Saddlebag => localization.Get("BankUi.Saddlebag"),
        StorageLabel.PremiumSaddlebag => localization.Get("BankUi.PremiumSaddlebag"),
        StorageLabel.Retainer => localization.Format("BankUi.Retainer", row.Name),
        StorageLabel.RetainersMissing => localization.Get("BankUi.RetainersMissing"),
        StorageLabel.Armoury => localization.Get("BankUi.Armoury"),
        StorageLabel.GlamourDresser => localization.Get("BankUi.GlamourDresser"),
        StorageLabel.Armoire => localization.Get("BankUi.Armoire"),
        StorageLabel.FreeCompanyChest => localization.Get("BankUi.FreeCompanyChest"),
        _ => row.Name,
    };
}
