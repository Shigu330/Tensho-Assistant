# GATHER-NATIVE-004 — 0.49.0, 2026-10-29

GatherBuddy et l’outil PR partagent un panneau repliable « Point de récolte actuel », observation activable et initialement désactivée. Il affiche les objets visibles avec icônes, réussite/supplément/quantité affichés, PR et tentatives. Un clic sur l’icône ou le nom met l’objet standard résolu en contexte commun. Les IDs spéciaux non résolus sont affichés séparément et ne sont jamais envoyés à la recherche Item.

Le bouton de préparation copie PR, niveau, métier et tentatives dans le scénario PR manuel. Il invalide le calcul et remet ses bases à renseigner (rendement, taux, bonus d’Abondance). Les taux UI peuvent déjà inclure des actions actives ; ils ne sont donc pas injectés comme valeurs de base à GpOptimizer. Aucune influence absente n’est déduite par défaut. Cette tranche ne constitue pas encore un optimiseur natif tenant compte des effets actifs.

## Lecture et inconnus

Un lecteur propriétaire de son abonnement framework copie à cinq lectures/seconde au maximum : huit IDs/flags et composants de lignes, textes numériques visibles, intégrité, métier/niveau et PR. Récolte rapide distinguée (GatherStatus=2), copie de scénario refusée dans ce mode ; statut ordinaire attendu=1. Le métier doit être MIN/BTN. Une fenêtre GatheringMasterpiece visible, un addon masqué/incompatible ou un propriétaire absent bloque la capture.

Identité du personnage, fenêtre, métier/niveau, PR, état et valeurs sont revérifiés avant publication. Chaque recherche de nœud est bornée à 256, exige une liste cohérente avec sa capacité et refuse les doublons d’ID ; hiérarchie de visibilité bornée à 32, texte borné à 64 octets avant décodage SeString.TextValue. Type Text contrôlé, icône obtenue depuis un nœud de composant. Aucune lecture native dans Draw, aucun pointeur ou span différé.

Les IDs de Gathering peuvent être encodés. Le lecteur utilise ItemUtil.GetBaseId et ItemKind de l’hôte : Normal/Hq/Collectible résolus vers l’ID Item de base ; EventItem et cas non résolus n’obtiennent pas de contexte standard. ID brut conservé séparément, pas de soustraction ou rapprochement par nom inventés.

Textes nuls, vides, tirets ou malformés restent inconnus. Le parseur accepte uniquement des entiers ASCII avec espaces périphériques et éventuellement un suffixe % unique pour les taux. Un supplément affiché à 0 % reste zéro ; un tiret ne devient pas zéro. Une quantité vide ne devient pas un objet supposé. Réussite/supplément bornés 0–100, quantité explicite 1–20 ; tentatives 0–12, maximum 1–12 et cohérence restant ≤ maximum. Un champ non interprétable n’annule pas les autres valeurs connues.

Le cadre détaché/readonly ne contient que les lignes visibles identifiées, triées par leur index UI. Flags copiés mais non interprétés comme disponibilité ou effets. La copie PR exige un cadre récent ≤2 secondes, jamais futur, un propriétaire et au moins une tentative cohérente. Zéro PR reste une ressource valide. Fermeture/masquage/erreur, désactivation et changement de personnage retirent les cadres et révoquent les anciennes leases. Erreur de lecture : reprise espacée cinq secondes ; Dispose désabonne et efface l’état.

## Vérifications exécutées

COMPILED / TESTED : Verify.ps1 Release 0.49.0.0, 320/320 tests, package/versions/ZIP/exclusion hôte passants, zéro avertissement et erreur. Neuf groupes supplémentaires : parseur strict/inconnus, ordre/copier/visibilité, intégrité contradictoire, IDs encodés/spéciaux, données malformées, copie de ressources/âge/récolte rapide, activation/propriétaire, absence/erreur/données invalides et lifetime.

LOCAL_VERIFIED : ApiProbe confirme signatures des nœuds/composants/UldManager, les membres Gathering et ItemUtil.GetBaseId/ItemKind. Le namespace incorrect testé d’abord pour ItemKind a été corrigé en Dalamud.Utility ; aucune API native exécutée dans ces probes.

GatheringUiProbe exécuté sur les fichiers installés et utilisant GatheringUiLayout de production : Gathering.uld contient 13 composants, 40 nœuds de widget, huit instances du composant1010 CheckBox (35 nœuds). Textes réussite10/supplément16, icône31→composant1005 Icon (12 nœuds), quantité texte7 ; tous dans la borne256. La vérification démontre les IDs/types de cette structure installée, pas la valeur ou visibilité d’une fenêtre active.

CURRENT_VERIFIED : [FFXIVClientStructs AddonGathering](https://github.com/aers/FFXIVClientStructs/blob/385b0821bc4573b77fbde84c930774eeca8283ec/FFXIVClientStructs/FFXIV/Client/UI/AddonGathering.cs) et AtkResNode, types et getters nommés. [LazyGatherer GatheringItemComponent](https://github.com/Hiroa/LazyGatherer/blob/0bb8ec9a47dc4521411c705221cdeda361015d75/LazyGatherer/Components/GatheringItemComponent.cs) et GatheringController pour les faits de lecture et la présence d’effets actifs dans les valeurs affichées. Dépôt AGPL-3.0 déjà signalé ; aucune implémentation, dépendance ou fixture externe copiée. Le probe ULD valide indépendamment les identifiants de notre lecteur.

VALIDATED_IN_GAME : non. Le protocole est conservé dans TESTING.md : ouvrir le point, comparer lignes/taux/tentatives et icônes, mettre un objet en contexte, copier les ressources puis renseigner les bases ; vérifier retrait en fermeture/masquage et refus de copie en récolte rapide. Aucun nouvel essai projection ou Macro Chain demandé, leurs reports explicites sont conservés.

DLL canonique C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll. [Étude préparatoire](GATHER-NATIVE-004-STUDY.md) conservée comme historique. Les 17 domaines restent PARTIAL et le goal global actif.

## Prochaine tranche : GP-ACTIVE-005

Établir les effets déjà actifs et étendre le modèle partagé sans double comptage, avant de proposer un calcul sur les taux du point. La source primaire GathererStatus consultée déclare Tidings2667, Yield219, Gift12666, Gift2759 et Bountiful1286 ; il reste à vérifier les métadonnées installées, les rangs, les statuts de chance et autres effets avant usage. Ne pas traiter cette liste partielle comme exhaustive.

Autre fait à vérifier : la source GatheringController relie GatheringItem.GatheringItemLevel.RowId à ItemLevel.Gathering, puis compare Collecte à 90/110 % pour Abondance II ; signatures des feuilles locales confirmées, jointure et seuils natifs non encore démontrés sur sqpack. Les seuils peuvent être étudiés indépendamment ; aucune formule supplémentaire intégrée en 0.49. Les bonus et influences non établis restent inconnus.

Étude complémentaire exécutée après le checkpoint initial 0.49 : GatheringActionsProbe confirme les deux scénarios natifs PR déjà établis (600 PR → espérance18,2 pour550PR, base4,6, seuil700), puis inspecte Action.StatusGainSelf. Les vingt associations du modèle sont toutes à 0 dans les fichiers installés : cette colonne ne permet donc pas de retrouver leurs effets ni de les déclarer absents. Les cinq IDs de la source primaire existent dans Status et leurs noms/descriptions sont cohérents ; ParamModifier/ParamEffect/MaxStacks sont tous à 0, donc ne fournissent pas la magnitude d’un bonus courant. Aucun décodeur natif d’effets ajouté à partir de cette preuve insuffisante.

Reprise GP-ACTIVE-005 : étendre d’abord le noyau partagé pour un état d’effets explicitement fourni (familles déjà actives, supplément d’Évangile et bonus déjà payé de la première tentative), avec oracle indépendant et UI de scénario déclarée. Ce noyau est nécessaire à l’intégration native, mais ne remplacera pas l’établissement du décodeur : liaison depuis l’observation bloquée tant que les influences/magnitudes ne sont pas établies, modèle manuel actuel conservé. Les paramètres affichés d’une action ou d’un statut ne doivent pas devenir des compteurs natifs inventés.
