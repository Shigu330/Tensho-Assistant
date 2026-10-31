# CACHE-001 — Cache et péremption

Livraison 0.75.0.0, 2026-10-29 : COMPILED / TESTED. Validation en jeu du cache encore à effectuer.

## Utilisation

Dans **Réglages → Cache & péremption**, choisir indépendamment 5 min, 1 h, 24 h ou 7 j. Une modification sauvegardée prend effet sur les consultations suivantes ; une donnée ancienne conserve sa date, elle ne devient pas fraîche à la lecture.

| Domaine | Valeur initiale | Actualisation |
|---|---|---|
| Price Insight | 24 h | Demande de prix forcée ; cadence réseau conservée |
| Quêtes | 5 min | Bouton dans QuestTracker et sources de quête ; relecture native à expiration |
| Collections / cartes | 24 h | Bouton de relecture ; expiration et signaux de déverrouillage |
| Sources communautaires, monstres et contenus | 7 j | Actualisation explicite du document demandé |
| Associations Revue de mode | 7 j | Actualisation explicite |
| Stocks importés | 24 h | Réimport manuel depuis le rangement ouvert |
| Calculs du carnet de synthèse | 24 h | Recalculer force un nouveau calcul |

Les panneaux indiquent la date d'origine et l'état valide/périmé. Les outils conservent leurs commandes de rafraîchissement. Un cache externe périmé peut rester visible comme historique jusqu'à une nouvelle demande ; il ne déclenche pas à lui seul une requête réseau. La fraîcheur des données chez le fournisseur reste distincte de la durée locale choisie.

Les stocks servant/chocobo restent des instantanés manuels : la péremption avertit, sans effacer les quantités ni accéder automatiquement aux rangements fermés. Les catalogues statiques du jeu sont déjà chargés et partagés en mémoire ; ils ne sont pas réinterprétés à chaque affichage. Les inventaires vivants, états de synthèse, attributs et mini-jeux gardent leurs observations courtes invalidées par contexte.

## Stockage et cohérence

Les fichiers sont placés dans **DataCache-v1**, sous le dossier de configuration du plugin. Le schéma et les clés séparent famille, portée et version. Les états privés quêtes/collections séparent personnage, langue, version du jeu et du plugin ; les prix publics séparent monde et objet. Une déconnexion retire les preuves actives de progression/acquisition.

Lecture et sérialisation se font hors du thread du jeu sur des copies managées. Les écritures sont ordonnées et atomiques ; une lecture attend les écritures déjà programmées. Une invalidation manuelle empêche de restaurer l'ancien état. Les fichiers absents, corrompus, incompatibles ou trop gros ne sont pas utilisés. Limites : 32 Mio par entrée, 256 fichiers et 256 Mio pour le dossier. Les noms sont des empreintes SHA-256.

La clé des calculs couvre recette, attributs, qualité initiale, options, presets, métadonnées d'actions et version du modèle. Un résultat restauré est rejoué et son export comparé avant utilisation ; la révision transitoire d'observation n'empêche pas de réutiliser des entrées identiques. Recalculer contourne ce cache.

## Vérification

Verify.ps1 : **466/466**, zéro avertissement/erreur ; identité/API/version, empreintes exactes DLL/manifest du ZIP, fichiers de langue et exclusion des assemblages hôtes passants. Onze groupes cache couvrent dates, durées, portée, configuration, écritures concurrentes, invalidation, corruption, annulation, fournisseurs, coûts et simulation. ConfigSerializationProbe : Newtonsoft None/Auto/Objects passants, préférences de cache incluses.

Les tests de simulation et Macro Chain précédemment reportés restent reportés. Les 17 outils restent PARTIAL. L'objectif autonome antérieur reste en pause à la réserve de quota ; cette livraison répond à la nouvelle demande explicite de cache.
