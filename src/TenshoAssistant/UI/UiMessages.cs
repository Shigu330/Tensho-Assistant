using TenshoAssistant.Localization;

namespace TenshoAssistant.UI;

internal static class UiMessages
{
    // Legacy service status strings are mapped only for presentation.
    public static string? Key(string message)=>message switch
    {
            "The bank is stopped." => "ServiceStatus.BankStopped",
            "The bank is still loading." => "ServiceStatus.BankLoading",
            "Bank loading failed; imports are blocked to protect the file." => "ServiceStatus.BankLoadFailed",
            "An import is already running." => "ServiceStatus.ImportPending",
            "A storage could not be read (perhaps after a game update); the other storages are still recorded." => "ServiceStatus.StorageReadUnavailable",
            "The save service is unavailable." => "ServiceStatus.SaveUnavailable",
            "Import saved in the bank." => "ServiceStatus.ImportSaved",
            "Bank stopped; import cancelled." => "ServiceStatus.ImportStopped",
            "Import en cours…" => "ServiceStatus.ImportLoading",
            "Open the right storage and wait until it is fully loaded." => "ServiceStatus.WrongStorage",
            "Storage character changed." => "ServiceStatus.StorageChanged",
            "Inventory closed, owner changed or containers not loaded. Reopen the storage and try again." => "ServiceStatus.StorageClosed",
            "Incomplete reading or identity changed. The old cache is kept; try again once the inventory is loaded." => "ServiceStatus.IncompleteRead",
            "Visual sort requested; checking the storage." => "ServiceStatus.SortRequested",
            "Request cancelled: character or date changed." => "ServiceStatus.SortCancelled",
            "Reading the open storage…" => "ServiceStatus.ReadRequested",
            "Reading cancelled: character or date changed." => "ServiceStatus.ReadCancelled",
            "Preview read live. You can compare the rule before applying it." => "ServiceStatus.ReadReady",
            "Visual sort applied to the four bags; refreshed within a second." => "ServiceStatus.SortedBags",
            "Visual sort applied to the open storage. Refresh the preview; import again separately to update the bank." => "ServiceStatus.SortedStorage",
        "The profile belongs to another character or storage." => "SortErrors.OtherScope",
        "Invalid criteria or number of zones." => "SortErrors.Criteria",
        "Invalid zone: name, filters or slot limits." => "SortErrors.Zone",
        "Slot zones must not overlap." => "SortErrors.Overlap",
        "Several profiles exist for this storage; reset its draft, then save it." => "SortErrors.DuplicateProfiles",
        "The limit of 128 storage profiles is reached." => "SortErrors.ProfileLimit",
        "The visual positions do not exactly cover the protected storage." => "SortErrors.ProtectedCoverage",
        "The unreserved slots are not enough for the other stacks. Shrink the zones or free some slots." => "SortErrors.Space",
        "Invalid template: name, version or geometry not supported." => "SortErrors.Template",
        "This template uses another storage geometry." => "SortErrors.Geometry",
        "The library exceeds 64 templates; remove the extra entries." => "SortErrors.OversizedLibrary",
        "The template's geometry must stay the same; create another template for this storage." => "SortErrors.ChangedGeometry",
        "The library is limited to 64 templates." => "SortErrors.TemplateLimit",
        "This name already exists; select the template to update it." => "SortErrors.DuplicateName",
        "Preview incompatible with the storage." => "SortErrors.IncompatiblePreview",
        "The preview no longer matches the observed stacks." => "SortErrors.ChangedStacks",
        "Incomplete preview positions." => "SortErrors.IncompletePositions",
        "Inconsistent number of empty slots." => "SortErrors.EmptySlots",
        "Preview positions differ from the observed stacks." => "SortErrors.DifferentPositions",
        "The preview does not keep every stack and empty slot." => "SortErrors.LostSlots",
        "Incomplete permutation." => "SortErrors.Permutation",
        _=>InternalMessages.Key(message),
    };
    public static string Get(LocalizationManager localization,string message,string fallbackKey)
        =>localization.Get(Key(message) ?? fallbackKey);
}
