using TenshoAssistant.Localization;

namespace TenshoAssistant.UI;

internal static class UiMessages
{
    // Legacy service status strings are mapped only for presentation.
    public static string? Key(string message)=>message switch
    {
            "La banque est arrêtée." => "ServiceStatus.BankStopped",
            "La banque est encore en cours de chargement." => "ServiceStatus.BankLoading",
            "Le chargement de la banque a échoué ; les imports sont bloqués pour préserver le fichier." => "ServiceStatus.BankLoadFailed",
            "Un import est déjà en cours." => "ServiceStatus.ImportPending",
            "Le service de sauvegarde est indisponible." => "ServiceStatus.SaveUnavailable",
            "Import sauvegardé dans la banque." => "ServiceStatus.ImportSaved",
            "Banque arrêtée ; import annulé." => "ServiceStatus.ImportStopped",
            "Import en cours…" => "ServiceStatus.ImportLoading",
            "Ouvrez le bon rangement et attendez son chargement complet." => "ServiceStatus.WrongStorage",
            "Personnage du rangement modifié." => "ServiceStatus.StorageChanged",
            "Inventaire fermé, propriétaire changé ou conteneurs non chargés. Rouvrez le rangement et réessayez." => "ServiceStatus.StorageClosed",
            "Lecture incomplète ou identité modifiée. L'ancien cache est conservé ; réessayez une fois l'inventaire chargé." => "ServiceStatus.IncompleteRead",
            "Tri visuel demandé ; vérification du rangement en cours." => "ServiceStatus.SortRequested",
            "Demande annulée : personnage ou date modifiés." => "ServiceStatus.SortCancelled",
            "Lecture du rangement ouvert…" => "ServiceStatus.ReadRequested",
            "Lecture annulée : personnage ou date modifiés." => "ServiceStatus.ReadCancelled",
            "Aperçu lu en direct. Vous pouvez comparer la règle avant de l’appliquer." => "ServiceStatus.ReadReady",
            "Tri visuel appliqué aux quatre sacs ; actualisation sous une seconde." => "ServiceStatus.SortedBags",
            "Tri visuel appliqué au rangement ouvert. Actualisez l’aperçu ; réimportez séparément pour mettre la banque à jour." => "ServiceStatus.SortedStorage",
        "Le profil appartient à un autre personnage ou rangement." => "SortErrors.OtherScope",
        "Critères ou nombre de zones invalides." => "SortErrors.Criteria",
        "Zone invalide : nom, filtres ou limites de cases." => "SortErrors.Zone",
        "Les zones de cases ne doivent pas se chevaucher." => "SortErrors.Overlap",
        "Plusieurs profils existent pour ce rangement ; réinitialisez son brouillon puis enregistrez-le." => "SortErrors.DuplicateProfiles",
        "La limite de 128 profils de rangement est atteinte." => "SortErrors.ProfileLimit",
        "Les positions visuelles ne couvrent pas exactement le rangement protégé." => "SortErrors.ProtectedCoverage",
        "Les cases non réservées ne suffisent pas aux autres piles. Réduisez les zones ou libérez des cases." => "SortErrors.Space",
        "Modèle invalide : nom, version ou géométrie non pris en charge." => "SortErrors.Template",
        "Ce modèle utilise une autre géométrie de rangement." => "SortErrors.Geometry",
        "La bibliothèque dépasse 64 modèles ; supprimez les entrées excédentaires." => "SortErrors.OversizedLibrary",
        "La géométrie du modèle doit rester identique ; créez un autre modèle pour ce rangement." => "SortErrors.ChangedGeometry",
        "La bibliothèque est limitée à 64 modèles." => "SortErrors.TemplateLimit",
        "Ce nom existe déjà ; sélectionnez le modèle pour le mettre à jour." => "SortErrors.DuplicateName",
        "Aperçu incompatible avec le rangement." => "SortErrors.IncompatiblePreview",
        "L’aperçu ne correspond plus aux piles observées." => "SortErrors.ChangedStacks",
        "Positions d’aperçu incomplètes." => "SortErrors.IncompletePositions",
        "Nombre de cases vides incohérent." => "SortErrors.EmptySlots",
        "Positions d’aperçu différentes des piles observées." => "SortErrors.DifferentPositions",
        "L’aperçu ne conserve pas toutes les piles et cases vides." => "SortErrors.LostSlots",
        "Permutation incomplète." => "SortErrors.Permutation",
        _=>null,
    };
    public static string Get(LocalizationManager localization,string message,string fallbackKey)
        =>localization.Get(Key(message) ?? fallbackKey);
}
