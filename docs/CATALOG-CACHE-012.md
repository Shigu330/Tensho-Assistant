# CATALOG-CACHE-012 — Cache disque des catalogues du client

0.81.0.0 — 2026-10-30 — IMPLEMENTED / COMPILED / TESTED (Verify.ps1 exécuté par l'agent sur le PC, 19:34 : 486/486, 0 avertissement/erreur, langues et paquet verts). VALIDATED_IN_GAME: YES pour la tranche objets — rapport utilisateur 2026-10-30 (« tous est ok » après le protocole ci-dessous) ; non observé directement par l'agent.

Demande utilisateur du 2026-10-30 : « Les données du client sont chargées à chaque fois ; il faut créer un cache et le mettre à jour une fois le client mis à jour. »

## Constat (lecture du code, 2026-10-30)

Dix catalogues statiques sont reconstruits depuis Lumina à chaque chargement du plugin (démarrage du jeu, rechargement ou mise à jour du plugin) : Item (~50 000 lignes estimées, le plus gros), Recipe (~12 000), récolte/pêche, Map, InstanceContent, ItemUse, Weather, actions GP et artisanat, thèmes de mode. Copie par lots sur le thread framework (2 ms par tick) puis indexation sur un worker. Aucun n'était persisté ; ils ne sont pas rechargés à l'ouverture des fenêtres. Estimations de taille non mesurées.

## Tranche livrée : catalogue des objets

- Clé DataCache : famille `catalog-items`, portée = langue du client, version = versions des dépôts du jeu + version du schéma Lumina + règles de copie (`CopyRules`) + forme des enregistrements (`ItemDefinition`, `CraftToolDefinition`, noms d'`EquipmentSlots`). **La version du plugin n'en fait pas partie** : une nouvelle DLL réutilise le cache tant que le client et la forme des données n'ont pas changé.
- Démarrage : lecture du fichier hors du thread framework ; si présent, validation (identité, bornes, ID uniques non nuls, noms, masques d'emplacement, métiers d'outil) puis `ItemCatalog.Build` sur un worker. Le catalogue publié porte la source « Lumina.Excel/Item · DataCache » (visible dans Diagnostics).
- Absent, illisible, refusé ou d'une autre version du jeu : copie Lumina comme avant, puis écriture atomique du nouvel instantané après publication réussie (il remplace un fichier refusé).
- Un cache refusé n'est jamais publié. Les anciens fichiers sont éliminés par la rotation existante (256 fichiers / 256 Mio).
- Maintenance : incrémenter `ItemCatalogCache.CopyRules` si la conversion Lumina → `ItemDefinition` change sans changer la forme des enregistrements.

Risque connu : taille réelle du fichier non mesurée (estimée bien en dessous de la limite de 32 Mio de DataCache). En cas de dépassement, l'écriture échoue proprement (erreur de cache affichée) et le chargement reste celui de Lumina.

## Tests

`ItemCatalogCacheTests` (3 groupes) : aller-retour par un vrai dossier DataCache et reconstruction équivalente (outil, cartes, orchestrion, catégories, provenance) ; clé distincte pour une autre version du jeu, une autre langue ou une autre forme ; refus des contenus corrompus. La boucle du chargeur (thread framework) n'est pas exécutée par les tests managés.

## Protocole en jeu (DLL 0.81.0.0)

1. Premier lancement : chargement habituel. Diagnostics → source « Lumina.Excel/Item ».
2. Recharger le plugin ou relancer le jeu : le catalogue doit être prêt plus vite ; source « Lumina.Excel/Item · DataCache ».
3. Après une mise à jour du client : retour à « Lumina.Excel/Item » une fois, puis cache à nouveau.

## Tranche 2 : recettes (0.82.0.0)

Même mécanisme pour le catalogue Recipe (~12 000 lignes estimées) : famille `catalog-recipes`, portée = langue du client (noms de métier), version = versions des dépôts + schéma Lumina + `RecipeCatalogCache.CopyRules` + forme (`RecipeDefinition`, `RecipeIngredient`, `CraftRecipeParameters`). Validation : ID uniques non nuls, objet produit et quantité positifs, ingrédients identifiés et positifs. Le catalogue publié garde la même `Source` (versions des dépôts) qu'une construction Lumina, car rien n'en dépend en dehors de l'affichage ; la provenance disque n'est donc pas visible pour les recettes. Clés partagées dans `Caching/CatalogCacheKeys.cs` (`CatalogCacheData<T>`, forme générique) ; la clé des objets est inchangée (un test le garantit), les fichiers écrits par 0.81 restent valides.

Verify.ps1 (agent, 2026-10-30 19:40) : 489/489, 0 avertissement/erreur, DLL 0.82.0.0. `RecipeCatalogCacheTests` (3 groupes). VALIDATED_IN_GAME: NO pour les recettes.

## Tranche 3 : récolte et pêche (0.83.0.0)

Famille `catalog-gathering` : lignes `GatheringSource` (points, pêche, liens directs GatheringItemPoint) et `FishingNotebookEntry`, telles que copiées avant le dédoublonnage du catalogue. `GatheringSchedule` reçoit un convertisseur JSON : seuls le genre et les fenêtres sont stockés ; `FromStored` ne rend un horaire que s'il est exactement celui que produiraient les règles de copie (fenêtres bornées, triées, fusionnées ; Unknown/Unrestricted/Invalid sans fenêtre, instances partagées). Sinon l'instantané est refusé et la récolte est relue depuis le jeu. `Timing` (repli calculé) n'est plus sérialisé. Verify.ps1 (agent, 19:43) : 492/492, 0 avertissement/erreur, DLL 0.83.0.0. `GatheringCatalogCacheTests` (3 groupes). VALIDATED_IN_GAME: NO.

Note 0.84 : SORT-ITEM-PROPS-008 ajoute des champs à `ItemDefinition` ; la clé de forme invalide une fois le cache des objets (réécrit au premier chargement), comme prévu.

## Suite proposée

Map, InstanceContent et ItemUse sont petits (centaines à ~1 000 lignes) : gain faible, à faire seulement si le temps de chargement reste gênant.
