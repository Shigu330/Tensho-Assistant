# FASHION-001 — Répertoire de Revue de mode, 0.35.0

Première capacité Avant-Garde intégrée : indices du jeu, associations équipement/indice provenant d'observations communautaires, filtre par emplacement natif, icônes, stock recensé et sélection dans le contexte commun. Sélection de l'indice manuelle depuis ce qu'affiche le jeu ; aucun thème hebdomadaire, score ou teinture déduit automatiquement. La capacité globale reste PARTIAL.

## Parcours

Accueil & outils → Avant-Garde → Ouvrir les indices de Revue de mode. Consulter les associations communautaires lance les deux lectures publiques fixes du répertoire et de son marqueur de date. Rechercher l'indice français affiché dans le jeu, cliquer pour ouvrir ses équipements, puis sélectionner l'emplacement concerné. Les noms d'indices identiques restent distincts par leur ID.

Les objets signalés ont leur nom et icône du catalogue du jeu. Cliquer place l'objet en contexte ; Stocks, Recettes et Prix réutilisent les outils existants. Les stocks sont les observations des sacs et imports manuels du personnage courant : les inconnus/partiels/caches restent explicites. Possession et possibilité d'équiper ne sont pas confondues ; niveau, métier, sexe et autres restrictions restent à vérifier.

Les références inconnues sont conservées avec Tous les emplacements, mais ne reçoivent aucun emplacement supposé. L'absence de résultats ne prouve pas qu'aucun équipement convient. Le nombre d'observations affiché est un compteur brut du fournisseur, pas un taux de réussite ni un score garanti.

## Identités et emplacements

FashionCheckThemeCategory fournit les IDs et noms natifs. Une source externe est liée par ID de catégorie et ID Item, sans traduction ou rapprochement par nom. Les partials inconnus restent identifiables. Le catalogue Item commun conserve maintenant EquipmentSlots depuis EquipSlotCategory : valeurs positives uniquement. Une valeur négative indique une case bloquée, pas un équipement porté dans cette case. FingerL/FingerR sont regroupés en Bague. Aucun numéro ItemUICategory n'est assimilé à un emplacement.

Les masques sont copiés pendant le chargement Item existant, sur le framework ; aucune ligne Lumina ni pointeur ne sort vers la vue ou un worker. Les listes, comptes et index sont managés et immuables. Le répertoire natif charge huit indices par callback framework, avec abonnement et nettoyage possédés. L'UI filtre seulement quand les données ou critères changent et utilise des clippers.

## Date, semaine et provenance

La source est le répertoire Categories de FashionReport.json de FFXIVGachaSpreadsheet. Les indices peuvent réapparaître sur plusieurs semaines ; ce dictionnaire est historique. Son marqueur LastUpdate.json n'attribue pas une date à chaque association et n'identifie pas le thème de la semaine. Les statistiques WeeklyDyes sont volontairement laissées hors de la jointure équipement/indice ; elles ne deviennent pas des suggestions d'objets.

Date de récupération et date annoncée par la source sont affichées séparément. La première ne remplace jamais la seconde. Une date absente, malformée, sans fuseau ou trop future est signalée comme inconnue/invalide/future. Un marqueur bien interprété ne constitue ni une preuve d'exhaustivité ni une validation pour la semaine courante. Les valeurs de compteur regroupent notamment des rapports gold et des données historiques importées ; elles ne dénombrent pas forcément des joueurs distincts.

Aucune collecte ni soumission de résultat n'est implémentée. Les deux appels sont des GET fixes vers raw.githubusercontent.com, sans personnage, inventaire, indice choisi ou autre donnée personnelle dans l'URL. Pas de fallback silencieux vers une autre source. Code original ; aucune dépendance ou implémentation du plugin de référence copiée.

## Service et limites

FashionDataService possède sa requête et son HttpClient par défaut ; un client emprunté reste utilisable après Dispose. Cache d'un dataset en mémoire, fraîcheur 30 min et minimum 15 s entre deux lots explicites. Actualiser contourne seulement le cache, pas la cadence. Les deux tâches du lot sont attendues, même quand le document principal échoue ; une métadonnée de date indisponible dégrade sa provenance sans supprimer les associations historiques valides. Annulation et Dispose rejettent les réponses tardives. Un échec du document principal conserve éventuellement l'ancien dataset daté avec son erreur, au lieu de publier un nouveau vide.

BoundedHttp est partagé avec GarlandClient pour la lecture par flux, la taille réellement lue et le décodage UTF-8 strict. Limites : 1 Mio pour les associations, 4 Kio pour la date, profondeur JSON 32, 1 024 catégories, 4 096 objets par catégorie et 32 768 liens au total. IDs nuls, clés contradictoires/dupliquées et compteurs non positifs sont rejetés. Délai du lot 12 s ; aucune requête périodique, au survol ou à la sélection d'un indice.

## Sources primaires examinées le 2026-10-28

- [Avant-Garde, README](https://github.com/NeNeppie/AvantGarde/blob/362113eea36a1a793638418a30482362d5c294ed/README.md) et [DataManager](https://github.com/NeNeppie/AvantGarde/blob/362113eea36a1a793638418a30482362d5c294ed/AvantGarde/Managers/DataManager.cs) : fonction attendue, séparation catégories/teintures hebdomadaires et source publique identifiées. Dépôt AGPL-3.0, examiné pour les faits et non recopié.
- [Export des observations](https://github.com/Infiziert90/FFXIVGachaSpreadsheet/blob/7c4a77d8d696c9370084572178a125f476732463/Export/SupabaseExporter/SupabaseExporter/Structures/Exports/Fashion.cs) et [traitement FashionReport](https://github.com/Infiziert90/FFXIVGachaSpreadsheet/blob/7c4a77d8d696c9370084572178a125f476732463/Export/SupabaseExporter/SupabaseExporter/Processing/FashionReport/FashionReport.cs) : compteur par catégorie/Item et import historique ; licence MIT signalée par GitHub.
- [Données publiques](https://raw.githubusercontent.com/Infiziert90/FFXIVGachaSpreadsheet/refs/heads/master/website/static/data/FashionReport.json), [marqueur de date](https://raw.githubusercontent.com/Infiziert90/FFXIVGachaSpreadsheet/refs/heads/master/website/static/data/LastUpdate.json). Ces fichiers évoluent ; le snapshot de vérification ci-dessous décrit une lecture ponctuelle.

## Vérifications exécutées

COMPILED / TESTED : Verify.ps1, Release 0.35.0.0, 225/225 tests et package verts, zéro avertissement/erreur. Onze nouveaux cas : associations et inconnus, dates non qualifiées/futures/absentes, clés/compteurs/malformations/limites, masques natifs et cases bloquées, noms identiques/IDs distincts, routes GET fixes, cache/cadence/expiration, date indisponible, annulation/réponse tardive, erreur conservant les données et propriété du lot/client. Les tests de Duty Loot/Monster Loot passent après extraction de BoundedHttp.

LOCAL_VERIFIED / CURRENT_VERIFIED : `tools/FashionProbe` avec sqpack et --fetch. 269 indices nommés, 20 238 objets avec emplacement pris en charge, 51 objets de torse qui bloquent la tête sans s'y équiper. Source : 214 catégories / 3 882 liens, 0 ID d'indice inconnu, 0 référence Item inconnue dans cette lecture. Date annoncée : 2026-10-28 16:21:18 UTC, interprétée, distincte de la récupération. Exemple de filtre : indice 3 « Forges de Garlond », 11 objets de torse ; échantillon 8877 « Armure de protecteur des Forges améliorée », compteur brut 1. Cela ne certifie pas la semaine courante ni l'aptitude à équiper de chaque personnage.

VALIDATED_IN_GAME : non. La lecture native du menu Revue de mode et l'attache à ses indices restent une extension. Aucun score ou teinture promis, aucune soumission externe. Macro Chain reste reporté selon la réponse utilisateur.

## Essai en jeu différé

DLL `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`, version 0.35.0.0. Ouvrir Avant-Garde, consulter, rechercher l'indice réellement affiché dans le jeu et filtrer le bon emplacement. Vérifier les icônes/noms, sélectionner un équipement puis ouvrir Stocks/Recettes/Prix ; contrôler que le contexte suit la sélection. Vérifier les dates et l'absence de score automatique, puis annuler une actualisation et recharger. Les stocks doivent rester ceux du personnage courant. Ce protocole est conservé pour plus tard, sans bloquer la suite du projet.
