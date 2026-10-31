# COLL-002 — Orchestrion et objets liés

0.65.0.0 — 2026-10-29 — IMPLEMENTED / COMPILED / TESTED ; NOT VALIDATED_IN_GAME. Collections et les 17 domaines restent PARTIAL.

L’onglet Orchestrion est ajouté aux collections existantes, dans le même lecteur/service. Liste avec icônes des rouleaux, recherche, filtres d’acquisition confirmée/non confirmée et catégories natives avec leurs icônes. Le classement utilise les ordres de catégorie/morceau copiés du jeu ; aucun numéro d’objet n’est utilisé comme numéro de morceau. Cliquer l’icône ou le nom ouvre une fiche : catégorie, indication native, état confirmé et objets liés. Cliquer un rouleau le met dans le contexte commun et ouvre ses stocks ; les autres outils restent accessibles.

Le lien fonctionne aussi depuis Usages de l’objet : un rouleau indexé présente son acquisition confirmée ou inconnue et un bouton vers la fiche du morceau, avec l’onglet/catégorie/recherche positionnés explicitement. Cela ne lit aucun inventaire supplémentaire et ne déclenche aucune requête réseau. L’acquisition du morceau ne signifie pas qu’un rouleau est en stock. Aucune lecture audio, utilisation d’objet ou acquisition automatique.

## Sources et identités

LOCAL_VERIFIED : signatures installées IUnlockState.IsOrchestrionUnlocked(Orchestrion), Orchestrion.Name/Description, OrchestrionUiparam.Order/OrchestrionCategory et catégorie Name/Icon/Order. Le code primaire Dalamud au commit 1e514694497ae448fca8e8a46f7b63642ad1e5ad a été relu : le getter protège l’état chargé puis appelle IsOrchestrionRollUnlocked avec Orchestrion.RowId ; le chemin Item emploie AdditionalData de type Orchestrion. Licence AGPL selon REF-DALAMUD historique ; faits d’API étudiés, aucune implémentation native copiée.

OrchestrionCopy.Lumina est la projection originale commune au lecteur et au probe. Elle conserve l’identité Orchestrion distincte d’Item. Une fiche UI d’un autre RowId est refusée ; une catégorie d’un autre RowId ne fournit ni nom ni icône. UI absente : métadonnées inconnues, sans supprimer la ligne native nommée. Nom vide et RowId zéro sont exclus. Le champ HideOrder n’est pas interprété ; aucun numéro du carnet n’est affiché par supposition. Les ordres copiés servent seulement au classement.

ItemOrchestrionCopy.Lumina copie l’ID seulement si Item.AdditionalData.RowType est Orchestrion. Le loader Item commun est étendu dans sa copie existante, sans nouveau catalogue ou scan dans Draw. ItemDefinition ajoute OrchestrionId optionnel (zéro pour les anciens constructeurs), et ItemCatalog.ForOrchestrion retourne toutes les associations readonly triées par ItemId. L’index est distinct de celui des cartes Triple Triade ; les identités numériquement identiques ne se mélangent pas. Plusieurs objets futurs pour un même morceau restent tous conservés.

## Acquisition, erreurs et lifecycle

Le lecteur Collections existant ajoute une phase orchestrion dans ses lots de 32 lignes/budget souple 2 ms, sur framework. Le getter d’acquisition ne part que pour une entrée nommée et un personnage chargé/connecté. Avant publication, l’identité du personnage est revérifiée. Logout/changement de propriétaire/invalidation effacent toutes les confirmations ; les métadonnées statiques restent des faits de catalogue. Dispose désabonne le callback existant et libère aussi les feuilles ajoutées. Aucun RowRef/pointeur n’est différé dans le modèle ou l’interface.

IUnlockState n’expose pas de drapeau de chargement dédié à l’orchestrion dans l’API inspectée. Un retour false reste donc « non confirmé », sans absence ni pourcentage de complétion inventé. Le probe hors jeu ne lit aucun état d’acquisition natif.

Une erreur de feuille/ligne orchestrion produit une CollectionCategoryIssue et conserve les autres catégories. Une erreur du getter conserve le catalogue statique et les confirmations positives déjà lues, puis laisse les lectures restantes inconnues jusqu’au prochain cycle. Une seule tentative/log par type d’échec dans ce cycle ; reprise avec le rafraîchissement manuel/périodique existant. CollectionService possède les listes readonly d’entrées et d’incidents. Une nouvelle invalidation/publication saine ne reprend pas les incidents de l’ancien relevé. Les erreurs globales précédentes conservent leur comportement d’invalidation générale.

## Validation exécutée

Verify.ps1 final : 416/416 tests, zéro avertissement/erreur ; package/API/versions/SHA-256 du ZIP canonique/exclusion hôte verts. Journal checkpoints/0.65.0-verify.log. Quatre groupes nouveaux : associations Item/morceau multiples sans collision avec ItemId/cartes, propriété/readonly et contexte du catalogue commun, confirmations/propriétaire/statique, isolation et lifecycle des incidents de catégorie.

CollectionProbe exécuté sur les fichiers installés avec les projections de production : 886 morceaux nommés, 12 catégories, zéro fiche UI manquante ; 886 objets liés, tous avec ItemId différent d’OrchestrionId ; chaque morceau possède un objet, aucun lien multiple dans ces données. Toutes les icônes de catégorie copiées sont présentes. Lignes UI/catégorie étrangères et UI absente refusées sans attribuer de métadonnées étrangères. Modèles utilisables après disposal GameData ; publication hors personnage refuse les confirmations. Le premier build du nouveau probe avait une faute de nom de fichier Card/Cards, corrigée avant son exécution réussie.

Exemples natifs : Orchestrion 1 A Cold Wind, catégorie 13 Quêtes, ordre 20, icône de catégorie 82036 → Item 14221. Orchestrion 2 Wailers And Waterwheels, catégorie 2 Lieux I, ordre 1, icône 82022 → Item 14222. Les titres de morceaux natifs sont conservés, même lorsqu’ils restent anglais dans la feuille française.

Le rendu, le callback en jeu, les erreurs natives et l’acquisition réelle d’un personnage ne sont pas observés. Aucun essai demandé ; projection craft/Macro Chain toujours différés. Le protocole ultérieur peut comparer une acquisition connue, les catégories/icônes, le lien bidirectionnel, un refresh et une reconnexion.

Suite COLL-003-STUDY : titres/succès et leurs indicateurs de chargement dédiés, preuves de portée/identité et gestion d’inconnus avant ajout. Ne pas transformer un résultat non chargé en non-acquisition.
