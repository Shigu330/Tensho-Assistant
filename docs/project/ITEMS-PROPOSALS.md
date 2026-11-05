# Objets, banque, tri et marché : propositions d'amélioration (audit du 2026-11-02, étape e)

Liste établie par un audit multi-agents en lecture seule (0.328). **Rien n'était implémenté** à l'audit (relu le 2026-11-03 : les propositions faites depuis, à l'étape g, portent une ligne « État ») : vous choisissez ce qu'on ajoute
(règle du 2026-11-02, [ROADMAP](ROADMAP.md) « Décisions reportées à la fin »). Les défauts trouvés par le même audit sont corrigés en
0.329 ([CHANGELOG](../../CHANGELOG.md)). Plusieurs propositions recoupent des manques déjà retenus pour l'étape g)
([GAPS](GAPS.md), I-*) : elles le disent.

Valeur et taille sont les estimations de l'audit. « Sonde » : une lecture du jeu ou un relevé en jeu est nécessaire avant de coder.

## I0 — Compter les pièces portées dans les stocks et dans « possédé »

- **État (relu le 2026-11-05)** : FAIT 0.549.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/Inventory/DalamudInventoryReader.Dalamud.cs`.
- **Ce que le joueur obtient** : L'équipement porté compte comme possédé. La Revue de mode de la semaine ne dit plus « non possédé » pour la pièce que vous portez. Le stock au survol et la fiche objet l'incluent, sur une ligne « Porté ». Les filtres « possédé » des autres outils le comptent aussi.
- **Comment, taille, dépendances** : Ajouter GameInventoryType.EquippedItems (1000) à la capture faite sur le thread framework, avec le même lecteur IGameInventory et la même couverture. Ajouter une StorageLabel « Porté » (catégorie PlayerBags), traduite dans Inventory/StorageNames.cs. Le tri (InventorySortScope, conteneurs 0-3) et la grille de la Banque (MainWindow.Bank.cs:184-186, conteneurs 0-3) ne sont pas touchés : un test doit le vérifier, avec un relevé de tous les consommateurs de inventory.Current (Diagnostics, tri, Banque). Taille S. Sonde : aucune pour la lecture ; vérification en jeu ensuite (porter puis retirer une pièce). Décision : compter le porté partout comme possédé (recommandé : oui, sur une ligne à part pour rester visible).
- **Source** : Inventory/DalamudInventoryReader.Dalamud.cs:11-13 ne capture que les sacs 1 à 4 et les cristaux. Inventory/ItemHoldings.cs:34-43 ne compte que les conteneurs 0-3 et 2001. MainWindow.FashionWeek.cs:41 (FashionOwned) passe par GetRecipeHoldings, donc sans l'équipement porté. Seule la page Équipement le traite à part (MainWindow.Gear.cs:512, IsWorn). docs/evidence/0.318-plugin-gaps.json (Allagan Tools) : « Équipement porté, monnaies et cristaux des servants comptés dans les stocks » = missing. EquippedItems est déjà lu ailleurs : Gear/GearLoader.Dalamud.cs:127 et Craft/CraftLiveReader.Dalamud.cs:22.

## I1 — Signaler et effacer le relevé d'un servant renvoyé ou d'un ancien rangement

- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/Inventory/BankService.cs`.
- **Ce que le joueur obtient** : Un servant renvoyé ne compte plus pour toujours dans vos stocks. La page Rangements signale les servants enregistrés qui n'apparaissent plus à la sonnette, et un bouton « Effacer ce relevé » (deux clics) les retire. Vos totaux redeviennent justes : survol, fiche, « manquant » de l'artisanat, Revue de mode.
- **Comment, taille, dépendances** : Ajouter InventoryBankService.DeleteAsync(BankKey), dans la même file bornée et avec la même écriture atomique (FileInventoryBankStore). Bouton dans « Imports » (DrawImportManager) avec la confirmation en deux clics de MainWindow.Alarms.cs. Mention « absent de la sonnette » quand KnownRetainers est connu et ne contient pas ce servant ; jamais d'effacement automatique. Taille S-M. Sonde : aucune. Décision : aucune nouvelle (partie du choix I-03, faisable avant g) ; à confirmer : effacement manuel seulement (recommandé).
- **Source** : BACKLOG.md:124 : « éventuelle suppression manuelle d'un ancien relevé » reste une extension séparée. GAPS.md:114 (I-03), Choix : bouton « Effacer ce personnage ». Inventory/BankService.cs:30 n'a que ImportAsync, aucune suppression. Inventory/ItemHoldings.cs:65-76 compte chaque relevé de servant enregistré pour le personnage. MainWindow.Bank.cs:428-441 compare la liste de la sonnette (BankInventoryBridge.Dalamud.cs:188-197, KnownRetainers) aux relevés seulement pour trouver les servants non importés, jamais les relevés en trop.

## I2 — I-01, partie anticipée : dernière vente et touche d'actualisation au survol

- **État (relu le 2026-11-03)** : FAIT 0.343 (I-01 : dernière vente, Ctrl+Alt pour actualiser).
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.MarketHover.cs`.
- **Ce que le joueur obtient** : Sous l'infobulle d'un objet, vous voyez déjà le prix le plus bas NQ/HQ du monde et le moins cher du centre de données. S'y ajoutent la dernière vente (prix et date) et une touche à tenir pour relire le prix, sans ouvrir la page Marché. Ensuite, après une sonde : prix de la région, ventes par jour et prix moyen.
- **Comment, taille, dépendances** : Étape 1 (S, aucune sonde) : afficher RecentSale/SaleAt NQ et HQ dans DrawHoveredPrices. La touche est lue sur le thread framework (IKeyState, dans Items/ItemHoverReader.Dalamud.cs) ; elle demande une lecture forcée, au plus toutes les 3 s (cadence de Market/MarketService.cs:138). Réglage « Touche d'actualisation » (aucune / Alt / Ctrl / Maj) avec infobulle et ↺. Étape 2 (S) : sonde d'une réponse aggregated (réponse tronquée dans docs/evidence), puis lecture de minListing.region, dailySaleVelocity et averageSalePrice dans MarketQuote.Parse (même requête ; monter la version de cache « universalis-aggregated-v2 »). Le prix de la pile attend la sonde de la quantité survolée (partagée avec I-06). Décision : la touche par défaut (recommandé : Alt, comme Price Insight, avec « aucune » possible).
- **Source** : GAPS.md:112 (I-01), Choix : « prix au survol seulement (pas de préchargement), touche de rafraîchissement ». Market/MarketQuote.cs:37 lit déjà recentPurchase.world, affiché seulement sur la page Marché (MainWindow.Market.cs:65-66). DrawHoveredPrices (MainWindow.MarketHover.cs:406-429) n'affiche que Minimum. docs/evidence/0.318-plugin-gaps.json (Price Insight) : « Dernier achat au survol » et « Rafraîchir avec Alt » = partial. Les champs region, dailySaleVelocity et averageSalePrice cités par GAPS ne sont prouvés par aucun relevé : docs/evidence/0.270-universalis-listings.md ne couvre que /api/v2/{scope}/{item}.

## I3 — Prix des marchands PNJ dans le coût de préparation et sur la page Marché

- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.PreparationCost.cs`.
- **Ce que le joueur obtient** : Pour chaque ingrédient vendu en gils par un marchand PNJ, le coût de préparation montre le prix PNJ à côté du minimum du marché et peut retenir le moins cher. Le Résumé de la page Marché dit « Vendu par un PNJ : X gils » (nom et lieu, lien vers le marchand). Vous n'achetez plus au marché ce qui coûte moins cher chez un marchand.
- **Comment, taille, dépendances** : Fonction pure « prix PNJ en gils » (offre NpcShopKind.Gil la moins chère, qualité de l'offre NpcShopSale.Hq) passée à PreparationCost.BuildDemands. Nouvelle colonne, état « PNJ » et choix de qualité « moins cher, PNJ compris ». Page Marché : une ligne au Résumé avec DrawVendorLink. Aucune requête réseau. Taille S-M. Sonde : aucune (feuilles déjà lues) ; un contrôle en jeu sur un objet connu. Dépendance : partie de A-06 (domaine Artisanat), faisable avant g. Décision : le PNJ entre-t-il dans le total « moins cher » (recommandé : oui, sur une ligne marquée « PNJ »).
- **Source** : GAPS.md:77 (A-06) cite comme donnée les « prix des marchands GilShopItem (Npcs/NpcVendorLoader.Dalamud.cs) », Choix « Ajouter ». Market/PreparationCost.cs:86-99 ne lit que le marché (un objet non échangeable reste NotMarketable). Npcs/NpcVendorCatalog.cs:7-15 : NpcShopKind.Gil et coûts en objet 1 (gil), déjà chargés. docs/evidence/0.318-plugin-gaps.json (SimpleMarketBoard) : « surbrillance quand un PNJ vend moins cher » = missing.

## I4 — Prix Universalis par lots : une requête pour toute une liste

- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/Market/MarketService.cs`.
- **Ce que le joueur obtient** : « Lire les prix » du coût de préparation (et des listes à venir) répond en une requête au lieu d'une par ingrédient : quelques secondes au lieu d'environ 100 s pour 32 ingrédients, sans risquer l'arrêt à 2 minutes, avec beaucoup moins de requêtes vers Universalis. Cela permet aussi de chiffrer, sur clic, un rangement ou un trajet de sous-marin au prix du marché.
- **Comment, taille, dépendances** : Sonde d'abord : GET aggregated/{monde}/{id1,id2,…} avec curl (nombre maximal d'objets, champ des objets en échec), réponse tronquée dans docs/evidence. Puis RequestBatchAsync(ids) dans MarketService : même verrou, même pause de 3 s, réponse bornée, cache mémoire et disque par objet. PreparationPriceSession l'utilise, avec la limite portée à la valeur prouvée. Taille M. Dépendances : aucune ; sert ensuite I-13, A-06, f) 2, et une éventuelle « valeur du rangement au prix du marché » (Retainer Inventory Price dans 0.318-plugin-gaps.json, hors des 103 manques : à décider). Décision : la nouvelle limite (recommandé : celle prouvée par la sonde, 100 si confirmée).
- **Source** : Market/MarketService.cs:138 : 3 s entre deux requêtes, une à la fois. Market/PreparationPriceSession.cs:22 : 32 objets au plus ; ligne 28 : arrêt après 2 minutes. MainWindow.PreparationCost.cs:94 affiche l'avis de limite. MarketQuote.Parse (Market/MarketQuote.cs:21-31) parcourt déjà un tableau « results » par itemId. ROADMAP « Décidé le 2026-11-02 », f) 2 : prix du marché des trajets de sous-marin. Cadence des sources : docs/evidence/0.246-cache-audit.md.

## I5 — Coût de préparation au prix du centre de données (option de A-06)

- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Market/PreparationCost.cs`.
- **Ce que le joueur obtient** : Le coût de préparation se calcule au monde actuel ou au moins cher du centre de données, avec le monde où acheter, pour qui change de monde pour faire ses achats.
- **Comment, taille, dépendances** : Combo « Portée : monde / centre de données » avec infobulle, mémorisé avec ↺. Chaque ligne indique le monde (MarketState.NameOf). Copie texte complétée. Aucune nouvelle requête. Taille S. Sonde : aucune. Dépendance : partie de A-06, faisable avant g. Décision : aucune nouvelle (option déjà choisie).
- **Source** : GAPS.md:77 (A-06), Choix : « plus basse annonce du monde (centre de données en option) ». Market/MarketQuote.cs:41-44 lit déjà NqCheapest/HqCheapest (minimum du centre de données et son monde) dans la même réponse. Market/PreparationCost.cs:93-96 n'utilise que Nq/Hq.Minimum du monde.

## I6 — I-06, partie anticipée : prix de vente au PNJ, à l'unité

- **État (relu le 2026-11-03)** : FAIT 0.346 (I-06, prix NQ seulement).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.MarketHover.cs`.
- **Ce que le joueur obtient** : Sous l'infobulle et dans l'en-tête de la fiche objet : « Vente au PNJ : X gils l'unité », pour choisir vite entre le marché et le marchand. Le total de la pile vient après la sonde de la quantité survolée.
- **Comment, taille, dépendances** : Une ligne dans le panneau de survol : cinquième partie « Prix PNJ », avec sa case, son infobulle et son ↺, comme les HoverShow*. Même ligne dans les « faits » de la fiche. Valeur 0 : rien n'est affiché (le sens « invendable » n'est pas prouvé). HQ : aucun prix tant qu'il n'est pas relevé, sans coefficient deviné. Taille S. Sonde : quantité de la pile survolée (AgentItemDetail ou InventoryItem, partagée avec I-01) pour le total ; relevé en jeu pour un prix HQ. Décision : aucune nouvelle.
- **Source** : GAPS.md:117 (I-06), Choix « Ajouter », dépend de la quantité survolée. Items/ItemCatalog.cs:11 (SellPrice) vient de Item.PriceLow (Items/ItemCatalogLoader.Dalamud.cs:138) ; seul le tri s'en sert (MainWindow.SortRules.cs). L'en-tête de la fiche (MainWindow.ItemSheet.cs:50-60) ne l'affiche pas.

## I7 — I-12, partie anticipée : filtres de catégorie et d'emplacement, niveaux dans les résultats

- **État (relu le 2026-11-03)** : FAIT 0.353 (I-12).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.Items.cs`.
- **Ce que le joueur obtient** : La recherche d'objets accepte un filtre de catégorie du jeu, d'emplacement d'équipement et de niveau ; chaque résultat montre sa catégorie et ses niveaux. Trouver « toutes les bagues de niveau 90 », ou un matériau parmi des homonymes, devient direct.
- **Comment, taille, dépendances** : ItemCatalog.Search(query, limit, filtre) avec catégorie, emplacement (EquipmentSlots) et bornes de niveau ; requête vide permise quand un filtre est posé. Combos et champs avec infobulle, bouton « Effacer les filtres ». Recalcul seulement quand la requête ou un filtre change (comme aujourd'hui, MainWindow.Items.cs:679-684). Taille S. Sonde : aucune. Le filtre de job (ClassJobCategory) et les objets clés (EventItem) restent en g : colonnes à ajouter au catalogue. Décision : aucune nouvelle.
- **Source** : GAPS.md:123 (I-12), Choix « Ajouter ». Items/ItemCatalog.cs:86-105 ne cherche que par nom ou ID. MainWindow.Items.cs:702-716 : un résultat n'est qu'une icône et un nom. Les données existent déjà : ItemCatalog.Categories (Items/ItemCatalog.cs:38, 45-47) ; Equipment, ItemLevel et EquipLevel dans ItemDefinition.

## I8 — X-07, partie anticipée : ouvrir l'objet sur Universalis, Garland Tools, Teamcraft ou le wiki

- **État (relu le 2026-11-05)** : FAIT 0.561.
- **État (relu le 2026-11-03)** : en partie 0.349 (X-07 : wiki et Lodestone) ; Universalis, Garland Tools et Teamcraft pas dans ce menu.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.DutyFinder.cs`.
- **Ce que le joueur obtient** : Clic droit sur un objet, dans les fenêtres Tensho et dans le menu « Tensho » du jeu : « Ouvrir sur… » ouvre la page de l'objet dans le navigateur.
- **Comment, taille, dépendances** : Sous-menu « Ouvrir sur » : Universalis (/market/{id}), Garland Tools (#item/{id}), Teamcraft (fiche de l'objet par ID), consolegameswiki (titre anglais). Lodestone : recherche par nom seulement, car les données du jeu ne donnent aucun identifiant Lodestone. Util.OpenLink sur clic seulement. Case « Liens externes » avec infobulle et ↺. Crédits et manifeste complétés (sites nommés). Taille S. Sonde : vérifier une fois chaque forme d'URL (docs/evidence). Décision : la liste des sites (recommandé : les quatre ci-dessus) et les commandes /wiki, /lodestone (recommandé : non, le menu suffit).
- **Source** : GAPS.md:199 (X-07), Choix « Ajouter ». Aucun OpenLink dans le code (recherche du 2026-11-02). Menus concernés : ItemLinksMenu (MainWindow.DutyFinder.cs:145-160) et Items/ItemContextMenuBridge.Dalamud.cs:60-80. Le nom anglais, déjà chargé (Items/ItemCatalog.cs:11, EnglishName), donne le titre du wiki.

## I9 — Copier le nom de l'objet et lien d'objet dans le chat (local)

- **État (relu le 2026-11-05)** : FAIT 0.561, avec I8.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.DutyFinder.cs`.
- **Ce que le joueur obtient** : Clic droit sur un objet : « Copier le nom », à coller dans la recherche du marché du jeu. « Lien dans le chat » : Tensho écrit dans votre seul chat un lien cliquable de l'objet, que vous partagez ensuite vous-même si vous le voulez.
- **Comment, taille, dépendances** : ImGui.SetClipboardText(item.Name). Lien : SeStringBuilder.AddItemLink(id, hq) puis IChatGui.Print, un message local : rien n'est envoyé au serveur. Mêmes entrées dans le menu du jeu. Taille S. Sonde : aucune. Décision : ajouter ces entrées, non choisies en c) (recommandé : « Copier le nom » oui, le lien en option).
- **Source** : ItemLinksMenu (MainWindow.DutyFinder.cs:145-160) n'a ni copie ni lien ; aucun ItemPayload dans le code. docs/evidence/0.318-plugin-gaps.json (Item Search) : « Lier l'objet dans le chat sans le posséder » = missing. Ce manque ne fait pas partie des 103 de GAPS.

## I10 — Cristaux et gils des servants dans le relevé du servant

- **État (relu le 2026-11-03)** : en partie : gils des servants dans le journal des gils (0.351) ; cristaux des servants non vérifiés.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Inventory/BankInventoryBridge.Dalamud.cs`.
- **Ce que le joueur obtient** : Les cristaux confiés aux servants comptent dans l'onglet Cristaux (une colonne par servant), dans le « manquant » de l'artisanat et au survol. Les gils de chaque servant sont gardés, avec la date du relevé.
- **Comment, taille, dépendances** : Sonde d'abord : RetainerCrystals (12001) et RetainerGil (12000) sont-ils chargés quand InventoryRetainer est ouvert (IsLoaded, taille) ? Relevé par tools/ApiProbe ou le journal en mode développeur, dans docs/evidence. Puis un groupe de conteneurs à part (nouveau BankStorageKind, ou schéma 2 du fichier) pour ne changer ni la validation « groupe exact » (BankEntryValidator) ni la portée du tri (10000-10006). Taille M. Dépendance : aucune ; prépare I-03 et I-04. Décision : avancer cette partie avant g (recommandé : oui, cristaux d'abord).
- **Source** : Inventory/BankModels.cs:9 : un servant ne couvre que les conteneurs 10000-10006. L'onglet Cristaux (MainWindow.Items.cs:54-89) ne lit que le conteneur 2001 du personnage. BACKLOG.md:124 : « équipements/cristaux/marché des servants » non inclus. docs/evidence/0.318-plugin-gaps.json : Allagan Tools « cristaux des servants comptés dans les stocks » = missing, Crystal Terror = partial. GAPS.md:114-115 (I-03, I-04) reposent sur des données de même nature.

## I11 — Vue en liste triable de la Banque

- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.Bank.cs`.
- **Ce que le joueur obtient** : En plus des grilles 5 × 7, une vue « Liste » : un objet par ligne, avec NQ, HQ, total et les rangements où il se trouve, triable par nom, quantité ou catégorie. Avec plusieurs servants, vous voyez enfin d'un coup d'œil où sont vos matériaux.
- **Comment, taille, dépendances** : Bascule « Grille / Liste », mémorisée, avec infobulle et ↺. Table ImGui triable par en-tête, avec clipper, sur le modèle de MainWindow.MarketBoard.cs. Lignes recalculées seulement quand la source ou un filtre change. Recherche alignée sur la normalisation du catalogue. Taille S-M. Sonde : aucune. Décision : vue par défaut (recommandé : la grille, comme aujourd'hui).
- **Source** : MainWindow.Bank.cs:181-203 calcule déjà les lignes agrégées par objet (bankSourceRows). L'affichage n'existe qu'en grilles (MainWindow.Bank.cs:303-370). La recherche de la Banque (MatchesBankFilters, MainWindow.Bank.cs:388-403) ne retire pas les accents, contrairement à la recherche d'objets (Items/ItemCatalog.cs:114-123). docs/evidence/0.318-plugin-gaps.json : Allagan Tools « listes de filtres enregistrées » et AetherBags = partial.

## I12 — Rapport du coffre à apparence : pièces pour l'armoire et doublons

- **État (relu le 2026-11-03)** : FAIT 0.357 (C-03, pièces pour l’armoire) et 0.358 (C-07, apparences identiques).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Inventory/BankInventoryBridge.Dalamud.cs`.
- **Ce que le joueur obtient** : Un rapport, sans rien déplacer, liste les pièces du coffre à apparence que l'armoire peut accueillir, et les pièces en double entre coffre à apparence, armoire, sacs et arsenal. Vous savez quoi retirer pour libérer des places du coffre.
- **Comment, taille, dépendances** : Projection pure sur les relevés (BankEntry GlamourDresser et Armoire, sacs, arsenal) : « accepté par l'armoire » = objet présent dans cabinetRows ; doublon = même objet dans deux rangements. Une section de la page Rangements, recalculée seulement quand un relevé change. Taille S-M. Sonde : aucune nouvelle ; validation en jeu de 0.236-0.240 d'abord. Décision : ajouter ce rapport, nouveau (recommandé : oui, après cette validation en jeu).
- **Source** : BankInventoryBridge.Dalamud.cs:224-255 copie déjà le contenu du coffre à apparence et de l'armoire ; cabinetRows donne les objets acceptés par l'armoire (feuille Cabinet). GAPS.md:121 (I-10) suit le même esprit : « rapport seul, aucun déplacement ». STATUS, « À valider en jeu » : la lecture du coffre et de l'armoire (0.234-0.240) n'est pas encore validée en jeu.

## I13 — I-10, partie anticipée : piles non regroupées

- **État (relu le 2026-11-03)** : FAIT 0.348 (I-10).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Items/ItemCatalogLoader.Dalamud.cs`.
- **Ce que le joueur obtient** : Un rapport des objets répartis en plusieurs piles incomplètes dans un même rangement (sacs, sacoches, servants), par exemple : « 3 piles de minerai de fer (12, 40, 99) tiendraient en 2 ».
- **Comment, taille, dépendances** : Ajouter Item.StackSize au catalogue (ItemCatalogLoader.Dalamud.cs:135-142, avec une version du cache d'objets montée). Projection pure par rangement : même objet, même qualité, NQ et HQ séparés. La partie « arsenal hors des sets d'équipement » attend une sonde de RaptureGearsetModule et reste en g. Taille S. Sonde : aucune pour les piles. Décision : aucune nouvelle.
- **Source** : GAPS.md:121 (I-10), Choix : « rapport seul, aucun déplacement ». ItemDefinition (Items/ItemCatalog.cs:9-13) n'a pas de taille de pile. Les piles par case sont déjà capturées (Inventory/InventorySnapshot.cs, InventoryStack).

## I14 — Annuler le dernier tri

- **État (relu le 2026-11-05)** : ÉCARTÉE à la revue avec l'utilisateur ([plan global](GLOBAL-PLAN.md)).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Inventory/InventoryVisualSortBridge.Dalamud.cs`.
- **Ce que le joueur obtient** : Après « Appliquer », un bouton « Annuler le dernier tri » remet l'ordre précédent des sacs ou du rangement ouvert, tant que rien n'a bougé depuis.
- **Comment, taille, dépendances** : Garder l'ordre d'avant (copie managée des pages et des cases) avec l'instantané vérifié. « Annuler » construit un plan inverse soumis aux mêmes contrôles (plan.Matches, RequireOpenForSort, SortFunctionIndex). L'annulation est perdue au changement de personnage ou de rangement et au moindre mouvement d'objet. Taille S-M. Sonde : aucune ; test en jeu. Décision : un seul niveau d'annulation, pour la session (recommandé).
- **Source** : InventoryVisualSortBridge.Dalamud.cs:75-84 copie l'ordre natif d'origine avant toute écriture ; lignes 99-108 : écriture après toutes les vérifications. ROADMAP, SORT-001 (référence SortaKinda). Règle du projet : un tri ne s'applique que sur clic, vérifié contre ce qui est ouvert.

## I15 — Partager un modèle de tri en texte (copier / coller)

- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.SortTemplates.cs`.
- **Ce que le joueur obtient** : Un modèle de rangement se copie en texte et se colle chez un autre joueur ou sur un autre PC.
- **Comment, taille, dépendances** : Export : JSON versionné, compressé en base64, avec un préfixe du type « Tensho-Tri-1: », mis dans le presse-papiers. Import : taille bornée (par exemple 64 Kio), lecture stricte puis InventorySortTemplates.Validate, nouvel identifiant, nom unique. Les identifiants d'objets et de catégories sont ceux du jeu, indépendants de la langue. Infobulles sur les deux boutons. Taille S. Sonde : aucune. Décision : le format (recommandé : celui-ci).
- **Source** : Inventory/InventorySortTemplates.cs:3-5 : un modèle ne contient ni personnage ni servant. MainWindow.SortTemplates.cs:13-60 : créer, mettre à jour, supprimer, charger ; ni export ni import. docs/evidence/0.318-plugin-gaps.json (SortaKinda) : « import et export des règles » = partial.

## I16 — Commandes « /tensho objet » et « /tensho prix »

- **État (relu le 2026-11-05)** : FAIT 0.555.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Plugin.cs`.
- **Ce que le joueur obtient** : « /tensho objet minerai de fer » ouvre la fiche de l'objet, ou la recherche s'il y a plusieurs résultats ; « /tensho prix … » ouvre la page Marché. Les deux marchent aussi dans une macro du jeu.
- **Comment, taille, dépendances** : Lire les arguments (mots français et anglais), chercher avec ItemCatalog.Search, puis SelectItem et OpenItemSheet, ou OpenRoute(Market). Traduire l'aide de la commande (Plugin.CommandHelp). Taille S. Sonde : aucune. Décision : les mots-clés (recommandé : objet/item et prix/price ; marchand/vendor en option).
- **Source** : Plugin.cs:377-380 : seul « peche » est reconnu. docs/evidence/0.318-plugin-gaps.json (Item Vendor Location) : « Commande de recherche (/pvendor nom) » = missing.

## I17 — I-13, partie anticipée : valeur en gils d'une monnaie

- **État (relu le 2026-11-03)** : FAIT 0.354 (I-13).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.ItemUses.cs`.
- **Ce que le joueur obtient** : Dans « Usages › Monnaie » d'un objet-monnaie (sceaux, marques, écus…), un bouton « Lire les prix » donne pour chaque article sa valeur au marché et le gain en gils par unité de monnaie, du plus rentable au moins rentable.
- **Comment, taille, dépendances** : Liste bornée lancée au clic : PreparationPriceSession, ou mieux la requête par lots de la proposition « Prix Universalis par lots ». Valeur = minimum du monde ÷ coût. Articles non échangeables marqués « non vendable ». Le solde et « combien je peux en acheter » attendent I-02 ; les monnaies codées restent « inconnues ». Taille M. Dépendance : les lots Universalis (recommandé). Décision : aucune nouvelle.
- **Source** : GAPS.md:124 (I-13), Choix « Ajouter », dépend des soldes (I-02) et des monnaies codées. MainWindow.ItemUses.cs:81-108 liste les articles et leur coût, sans valeur. docs/evidence/0.318-plugin-gaps.json : Currency Profit Scanner et Currency Spender.

## I18 — I-07, partie anticipée : copier « prix le plus bas − 1 » depuis la page Marché

- **État (relu le 2026-11-03)** : en partie 0.346 (I-07 : à l’ouverture des offres dans le jeu) ; pas depuis la page Marché.
- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/MainWindow.MarketBoard.cs`.
- **Ce que le joueur obtient** : Sur l'onglet Annonces de Tensho, un bouton copie le prix le plus bas moins 1 (NQ ou HQ). Vos propres servants sont marqués, pour ne pas vous sous-coter.
- **Comment, taille, dépendances** : Bouton avec infobulle, qui ignore les annonces de vos servants (KnownRetainers) et affiche la date de l'annonce, car Universalis peut être en retard sur le jeu. La version complète (offres du jeu lues à l'ouverture de la liste, IMarketBoard) reste en g. Taille S. Sonde : aucune. Décision : faire cette version tôt (recommandé : seulement si vous la voulez avant g ; sinon attendre I-07 complet, plus fiable).
- **Source** : GAPS.md:118 (I-07), Choix : copie dans le presse-papiers seulement. MainWindow.MarketBoard.cs:161-216 affiche les annonces avec le nom du servant, sans copie. Les noms de vos servants sont connus après l'ouverture de la sonnette (BankInventoryBridge.Dalamud.cs:188-197).

## I19 — Nom anglais de l'objet dans la fiche et l'infobulle

- **État (relu le 2026-11-05)** : FAIT 0.562.
- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/MainWindow.ItemSheet.cs`.
- **Ce que le joueur obtient** : En option, le nom anglais de l'objet s'affiche sous son nom, utile avec les guides et les wikis en anglais.
- **Comment, taille, dépendances** : Case « Nom anglais », désactivée par défaut, avec infobulle et ↺. Affiché seulement si le client n'est pas en anglais et si le nom diffère. Taille S. Sonde : aucune. Décision : l'ajouter (nouveau).
- **Source** : Items/ItemCatalog.cs:11 : EnglishName est chargé pour le wiki. docs/evidence/0.318-plugin-gaps.json (BilingualTooltips) : « EnglishName chargé pour le wiki, jamais affiché ». Ce manque ne fait pas partie des 103 de GAPS.

## I20 — Aperçu du tri : cases déplacées signalées

- **État (relu le 2026-11-05)** : FAIT 0.565.
- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/MainWindow.SortPreview.cs`.
- **Ce que le joueur obtient** : L'aperçu encadre les cases qui changent et dit combien d'objets bougent. « Appliquer » est grisé quand le tri ne changerait rien.
- **Comment, taille, dépendances** : Comparer la case d'origine (DisplayIndex) à la case cible dans la projection en cache. Ajouter un cadre de couleur et un compteur ; le calcul suit la projection et ne se refait pas à chaque image. Taille S. Sonde : aucune. Décision : aucune.
- **Source** : MainWindow.SortPreview.cs:136-146 : seul le résultat est dessiné. Le plan est déjà construit (InventoryVisualSortPlan.Build) et la projection est mise en cache (MainWindow.SortPreview.cs:73-80).

## I21 — Filtre de job et de set d'équipement dans les règles de tri

- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/Inventory/InventorySortRules.cs`.
- **Ce que le joueur obtient** : Une zone de tri peut ne prendre que l'équipement d'un job, ou les pièces de vos sets d'équipement.
- **Comment, taille, dépendances** : Copier ClassJobCategory (liste de jobs) dans le catalogue, puis ajouter un filtre de job à la zone. Pour les sets : sonde de RaptureGearsetModule d'abord. Taille M. Décision : l'ajouter, nouveau (recommandé : avec ou après I-14).
- **Source** : Inventory/InventorySortRules.cs:8-12 : InventorySortMetadata n'a pas de job. docs/evidence/0.318-plugin-gaps.json (SortaKinda) : « Filtres par classe ou job et par set d'équipement » = missing. Le catalogue ne lit pas ClassJobCategory (Items/ItemCatalogLoader.Dalamud.cs:135-142).

## I22 — Calculs faits à chaque image (reportés à e) : coût de préparation et historique du marché

- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/MainWindow.PreparationCost.cs`.
- **Ce que le joueur obtient** : Rien de visible : les pages du coût de préparation et de l'historique restent légères avec de longues listes.
- **Comment, taille, dépendances** : Mesurer d'abord en jeu, avec une chaîne de plus de 30 ingrédients. Si le gain est réel : une clé de cache comme ChainCostViewKey (MainWindow.PreparationCost.cs:20, 65-71), et marketSalesKey étendu aux ventes filtrées et aux statistiques. Taille S. Décision : aucune.
- **Source** : CHANGELOG.md:181 (0.325) : « Reporté à l'étape e) : mise en cache du plan et du coût de préparation… à mesurer en jeu avant de changer ». MainWindow.PreparationCost.cs:40-41 : PreparationCost.Build à chaque image. MainWindow.MarketBoard.cs:223-230 : filtre et statistiques des ventes à chaque image. docs/evidence/0.292-draw-times.md : sous 0,5 ms par image.
