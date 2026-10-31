using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    // Presentation adapter for legacy domain explanations; simulation data and rules remain unchanged.
    private string? CraftMessageKey(string message)=>message switch
    {
        "Paramètres de simulation absents pour cette recette." => "CraftMessages.NoParameters",
        "Recette experte, ajustée, spécialisée ou à qualité imposée hors périmètre de cette simulation." => "CraftMessages.SpecialRecipe",
        "Qualité maximale du collectionnable absente ou incompatible." => "CraftMessages.CollectableQuality",
        "Paramètres natifs incomplets ou incompatibles avec le modèle." => "CraftMessages.NativeParameters",
        "Habileté ou contrôle sous le minimum de la recette." => "CraftMessages.MinimumStats",
        "100 actions maximum." => "CraftMessages.ActionLimit",
        "Condition ou bonus favorable non pris en charge." => "CraftMessages.Condition",
        "Action non prise en charge." => "CraftMessages.UnsupportedAction",
        "Niveau insuffisant pour cette action." => "CraftMessages.Level",
        "Utilisation de Manipulation non confirmée dans le scénario." => "CraftMessages.Manipulation",
        "Action réservée au premier tour." => "CraftMessages.FirstTurn",
        "Main preste exige dix niveaux de plus que la recette." => "CraftMessages.TrainedEye",
        "Action incompatible avec Parcimonie." => "CraftMessages.WasteNot",
        "Dix degrés de Calme intérieur requis." => "CraftMessages.QuietTen",
        "Main suprême utilisable une seule fois par synthèse." => "CraftMessages.TrainedOnce",
        "Calme intérieur requis pour Byregot." => "CraftMessages.Byregot",
        "Cette recette ne possède pas de cible de qualité." => "CraftMessages.NoQuality",
        "PS insuffisants pour cette action." => "CraftMessages.Cp",
        "Sélection du carnet trop ancienne ou invalide." => "CraftMessages.LogOld",
        "Recette non reliée au catalogue courant ; aucun rapprochement par nom." => "CraftMessages.LogRecipe",
        "Qualité initiale du carnet absente ou qualité maximale incompatible ; calcul suspendu." => "CraftMessages.LogQuality",
        "Qualité initiale du carnet hors des limites de la recette." => "CraftMessages.QualityBounds",
        "En attente des attributs récents du personnage." => "CraftMessages.StatsWaiting",
        "Attributs d’artisanat incomplets ou incompatibles." => "CraftMessages.StatsIncomplete",
        "Aucune synthèse terminée trouvée dans les limites de recherche." => "CraftMessages.NoCompletion",
        "La séquence termine sous la qualité cible." => "CraftMessages.BelowTarget",
        "Observation absente ou trop ancienne." => "CraftMessages.ObservationOld",
        "Action ou animation en cours." => "CraftMessages.Animating",
        "Identité de recette non établie." => "CraftMessages.RecipeIdentity",
        "Valeurs natives typées absentes, incohérentes ou condition hors des quatre états ordinaires." => "CraftMessages.TypedValues",
        "Bonus d'outil favorable inconnu : les gains de qualité concernés restent inconnus, les autres ressources restent projetées." => "CraftMessages.ToolBonus",
        "Ressources affichées, valeurs natives et recette non concordantes ou synthèse terminée." => "CraftMessages.Inconsistent",
        "Disponibilité des actions native non établie pour ce métier." => "CraftMessages.Availability",
        "Lecture complète des statuts non établie ; absence d'effet inconnue." => "CraftMessages.Statuses",
        "Métadonnées des effets incomplètes." => "CraftMessages.EffectMetadata",
        "Deux effets Parcimonie simultanés : état incohérent." => "CraftMessages.WasteNotConflict",
        "Habileté ou Contrôle sous le minimum de la recette." => "CraftMessages.MinimumStats",
        _=>null,
    };
    private string CraftMessage(string message)=>CraftMessageKey(message) is { } key ? localization.Get(key):message;

    private void DrawCraftIssue(string message)
    {
        var key=CraftMessageKey(message);
        if(key is not null){ImGui.TextWrapped(localization.Get(key));return;}
        const string transitionSuffix=" : disponible côté client, aucune transition compatible dans le modèle.";
        if(message.EndsWith(transitionSuffix,StringComparison.Ordinal))
            ImGui.TextWrapped(localization.Format("CraftMessages.NoTransition",message[..^transitionSuffix.Length]));
        else
            ImGui.TextWrapped(localization.Get(
                message.StartsWith("Statut ",StringComparison.Ordinal) ? "CraftMessages.DuplicateEffect":
                message.StartsWith("Compteur du statut ",StringComparison.Ordinal) ? "CraftMessages.UnknownCounter":
                message.StartsWith("Effet ",StringComparison.Ordinal) ? "CraftMessages.UnsupportedEffect":"CraftMessages.Uninterpreted"));
        if(developerMode)ImGui.TextWrapped(message);
    }
}
