# NPC-VENDORS-014 — PNJ vendeurs : lieux, coûts, drapeau sur la carte

0.88.0.0 — 2026-10-30 — IMPLEMENTED / COMPILED / TESTED (Verify.ps1 exécuté par l'agent sur le PC, 20:27 : 509/509, 0 avertissement/erreur, langues et paquet verts). VALIDATED_IN_GAME: NO.

Demande utilisateur du 2026-10-30 :
- un outil qui fait les liens objet → PNJ, avec le nom, le lieu et les coordonnées ;
- un clic droit qui place un marqueur sur la position du PNJ choisi ;
- une liste courte au survol, en signalant qu'elle est tronquée ;
- le coût et la monnaie dans l'outil, puis aussi au survol si c'est compact ;
- les monnaies autres que le gil (assignats, etc.) ;
- les icônes du jeu à la place des noms de monnaie.

## Données (toutes lues dans le client, colonnes vérifiées par réflexion sur Lumina.Excel installé, 2026-10-30)

- **Boutiques à gils** : `GilShop` + `GilShopItem` (sous-lignes). Le coût est `Item.PriceMid` en Gil (objet 1).
- **Grande compagnie** : `GCShop.GrandCompany`, puis les `GCScripShopCategory` de la même compagnie, puis `GCScripShopItem` (`CostGCSeals`). La monnaie est l'objet 19 + n° de compagnie. Objets 20, 21 et 22 = sceaux limséen, gridanien, uldien, vérifiés dans la copie locale du catalogue.
- **Boutiques spéciales** : `SpecialShop.Item` (objets reçus, quantité, HQ, jusqu'à trois coûts).
  - Monnaie directe (`UseCurrencyType` = 0, ou identifiant ≥ 100) : exacte. Exemples : marques de loup (25), insignes alliés (27), MGP (29), gemmes et autres objets-monnaie.
  - Monnaie codée de type mémoquartz (`UseCurrencyType` = 16, petit identifiant) : résolue par `TomestonesItem` seulement si un objet unique correspond.
  - Tout autre codage (certains assignats notamment) : affiché « monnaie codée n° X (non résolue) », jamais deviné.
- **Liens PNJ → boutique** : `ENpcBase.ENpcData`, suivi au plus sur 4 niveaux : boutique directe, `TopicSelect.Shop`, `PreHandler.Target`, `CustomTalk.Script[].ScriptArg`. Cycles ignorés.
- **Positions** : lignes `Level` de type 8 pointant le PNJ (au plus 8 par PNJ). Coordonnées de carte selon la formule du jeu (`SizeFactor`, `OffsetX` / `OffsetY`, testée), zone = `Map.PlaceName`.
- **Noms** : `ENpcResident.Singular`.

Copie par lots sur le thread framework (itérateur repris à chaque tick, 2 ms au maximum), index sur un worker. Le résultat est mis en cache disque (`catalog-npc-vendors`, clé = versions du client + schéma Lumina + forme), comme CATALOG-CACHE-012.

## Interface

- **Outil « PNJ vendeurs »** (catégorie Cartes, à côté de Matériaux des monstres) : recherche d'objet, puis tableau PNJ · Lieu (zone et coordonnées, deux positions au plus, puis +N) · Coût · Boutique.
  - Coût = montant + **icône de la monnaie** (nom en info-bulle).
  - Ordre : PNJ localisés d'abord, puis gil, sceaux et spéciales, puis coût croissant.
  - **Clic droit** sur une ligne : une entrée par position connue. « Placer le drapeau » ouvre la carte du jeu avec le drapeau, via `IGameGui.OpenMapWithMapLink` (API Dalamud, déjà utilisée par Mappy).
- **Survol** (section Obtention) : au plus 2 PNJ sur une ligne chacun (nom · zone (x, y) · montant + icône), puis « +N autre(s) · liste complète dans l'outil PNJ vendeurs ».
- **Prix du marché au survol** : l'icône Gil remplace le mot « gils ».

## Correctif 0.89.0.0 — marchands de gemmes bicolores

Retour utilisateur 2026-10-30 : l'Épaule de rroneek s'achète en gemmes bicolores (appelées « aléa » par l'utilisateur), mais aucun PNJ n'apparaissait. Vérification sur la copie locale du cache 0.88 : 866 PNJ et 39 255 ventes ; cinq boutiques spéciales (1770736, 1770746–1770749) vendent l'objet 44106 contre 3 × objet 26807, monnaie bien résolue, mais **aucun PNJ n'y est relié**. Cause : les marchands de gemmes passent par la feuille `FateShop` (`SpecialShop[]`, `DefaultTalk[]`, vérifiée par réflexion), indexée comme le PNJ. Correctif : liens `FateShop` → boutiques spéciales, et l'identifiant du PNJ lui-même est ajouté aux entrées résolues. `NpcVendorCache.CopyRules` passe à 2 : le cache PNJ 0.88 est reconstruit une fois. Test ajouté au groupe des liens. Verify.ps1 (agent, 20:33) : 509/509, 0 avertissement/erreur. VALIDATED_IN_GAME: NO.

## Mise en page du survol (0.90.0.0)

Capture utilisateur 2026-10-30 : les liens FateShop/boutiques fonctionnent (officiers de la logistique listés, « +5 autre(s) »), mais la ligne passait à la ligne et repoussait coût et icône contre le bord droit (« 5 / 0 / 0 ») ; coordonnées « (13,2, 12,8) » ambiguës avec la virgule décimale ; noms génériques en minuscules. Correctif : coût (montant + icône) en tête, puis « · Nom · Lieu » en texte replié ; lieu au format du jeu « Zone · X 13,2 Y 12,8 » (outil et survol) ; première lettre du nom en majuscule. Verify.ps1 (agent, 20:36) : 509/509, 0 avertissement/erreur, DLL 0.90.0.0. Rendu non couvert par les tests ; VALIDATED_IN_GAME: NO.

## Menu clic droit de l'inventaire (0.91.0.0)

Demande utilisateur 2026-10-30 : le menu clic droit de l'inventaire du jeu (USE-CONTEXT-004, opt-in) ne proposait pas l'outil PNJ. Ajout de la route `ItemContextRoute.NpcVendors` (en fin d'énumération) : entrée « PNJ vendeurs » qui sélectionne l'objet et ouvre l'outil. Même garde que les autres routes (propriétaire, révision, âge) ; le test existant parcourt toutes les routes, y compris la nouvelle. Verify.ps1 (agent, 20:39) : 509/509, 0 avertissement/erreur, DLL 0.91.0.0. VALIDATED_IN_GAME: NO.

## Retour en jeu 0.91 (2026-10-30 ~20:45)

Capture utilisateur, Breuvage cordial supérieur : 12 offres / 12 PNJ (Négociante d'espoilles, Mesouaidonque), lieux et coordonnées X/Y, deux positions par ligne, « Position non renseignée » si absente, coûts avec icônes de monnaie → outil VALIDATED_IN_GAME pour ces boutiques. **Manque signalé** : l'achat contre 20 assignats d'artisanat/récolte (échange d'assignats, 2ᵉ capture) n'apparaît pas.

Diagnostic sur le cache local 0.89 (2026-10-30 ~20:55, aucun code modifié) : Breuvage cordial supérieur = objet 12669.
- Neuf ventes lues pour cet objet. Cinq boutiques spéciales (1770193, 1770275, 1770430, 1770508, 1770879) coûtent 20 × « objet 47 » = **Mémoquartz allagois héliologique** dans le catalogue ; **aucun PNJ ne leur est relié**.
- Les autres ventes sont visibles dans l'outil, avec PNJ : 1 × 13631, 40 × 45690, 60/150 × assignats insulaires 37549/37550.
- Identifiants des assignats dans le catalogue : 10308–10311, 17833–17834, 25199–25200, 33913–33914 (mauve), 41784–41785 (orange), 28063, 37549–37550.
- Coûts codés non résolus dans tout le catalogue, par identifiant brut : 1 (2119), 2 (483), 3 (77), 4 (5), 6 (116), 7 (56), 25 (838), 27 (221), 29 (1355). Les identifiants 25, 27 et 29 sont des objets-monnaie (marques de loup, insignes alliés, MGP) : la règle « codé et < 100 ⇒ non résolu » perd donc aussi des coûts lisibles.
- Question ouverte : l'utilisateur parle de 20 assignats (icône violette) ; les données directes donnent 20 mémoquartz héliologiques. Il faut le confirmer en jeu (nom de la monnaie au survol de l'icône chez le marchand, et nom du PNJ) avant toute table.

Piste de reprise (non commencée, quota) :
1. Vérifier par réflexion `InclusionShop.Category` → `InclusionShopCategory` → `InclusionShopSeries` (sous-lignes → `SpecialShop`) et relier les PNJ (via `ENpcData`/`CustomTalk`) à `InclusionShop`. Les échanges d'assignats récents passent probablement par là ; c'est à confirmer.
2. Monnaie codée des assignats (`UseCurrencyType` ≠ 0, ≠ 16) : n'établir la table qu'avec une preuve, par exemple une feuille du jeu ou la comparaison avec l'observation en jeu (Breuvage cordial supérieur = 20 assignats violets d'artisanat). Ne pas deviner.
3. Passer `NpcVendorCache.CopyRules` à 3 et ajouter les tests correspondants.

## Limites connues

Présence actuelle du PNJ, prérequis (quêtes, rang de compagnie, festivals) et boutiques hors de ces feuilles (compagnie libre, loteries, échanges d'objets inclus `InclusionShop`) : non couverts. Les monnaies codées autres que les mémoquartz restent non résolues jusqu'à une table vérifiée.

## Tests

`NpcVendorTests` (4 groupes) :
- liens directs, menus, pré-dialogues, scripts, cycles et profondeur ;
- monnaies directes, mémoquartz résolus, codages inconnus non devinés ;
- formule des coordonnées (centre, bords, échelle, décalage) ;
- ordre des offres, aller-retour DataCache réel et refus des lignes corrompues.

Lecteur de feuilles et rendu non couverts par les tests managés.

## Protocole en jeu (DLL 0.88.0.0)

1. Premier lancement : l'outil affiche « Lecture des boutiques et des PNJ… » puis la liste. Au lancement suivant, la liste est immédiate (cache).
2. Objet vendu par un marchand (ex. un cristal ou une teinture) : PNJ, zone, coordonnées, coût avec icône Gil.
3. Clic droit → « Placer le drapeau » : la carte du jeu s'ouvre avec le drapeau sur le PNJ.
4. Objet à sceaux ou à monnaie spéciale : icône de la bonne monnaie ; noter toute « monnaie codée ».
5. Survol de ces objets : 2 PNJ au plus et la mention « +N … outil PNJ vendeurs ».
