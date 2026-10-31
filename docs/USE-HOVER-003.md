# USE-HOVER-003 — Usages au survol et contexte commun

Version 0.52.0.0, vérifications locales du 2026-10-29. IMPLEMENTED / TESTED ; NOT VALIDATED_IN_GAME.

Dans Réglages, « Afficher les usages au survol des objets en jeu » active les références locales. Cette option v1 est désactivée par défaut et indépendante des prix. Les usages fonctionnent pour les objets non vendables et sans monde de marché. Ils ne déclenchent aucune requête de prix. Le panneau apparaît aussi lorsque la fenêtre principale est fermée.

Un seul ItemHoverReader copie le contexte depuis le thread framework. ItemHoverService conserve propriétaire, identifiant normalisé, qualité, date et début de stabilité. Le contexte expire après deux secondes ; les usages et nouvelles requêtes de prix attendent 600 ms de stabilité. Déconnexion, masquage de l’interface, objet incohérent et nettoyage effacent le contexte. Aucun pointeur natif différé.

Les marqueurs standard sont Normal=0, Collectible=500000 et Hq=1000000. Le catalogue doit connaître l’objet de base ; une qualité HQ impossible est rejetée. Les identifiants >=2000000 sont exclus, sans modulo ni tentative de conversion en objet standard. Les collectionnables affichent seulement les références de leur objet de base : aucune acceptation réelle n’est déduite.

ItemHoverCoordinator partage ce contexte avec le marché. Les prix exigent leur propre option, un monde et un objet vendable NQ/HQ. Cache cinq minutes, cadence globale trois secondes, délai par survol cinq secondes et reprise après échec trente secondes restent en place. RequestHoverAsync contrôle propriétaire et monde avant le transport. Une désactivation annule le travail de fond ; les consultations manuelles gardent leur priorité et ne sont pas annulées par le nettoyage du survol.

Un panneau sans capture de souris regroupe icône carrée, nom, qualité, recettes et références mandats/GC/aquarium, puis éventuellement les prix avec provenance et date. Catalogues absents et demandes quotidiennes non observées restent explicitement inconnus. Le survol ne change pas l’objet sélectionné.

Validation : Verify.ps1 passe 343/343 tests, compilation Release sans avertissement ni erreur, versions/package/ZIP/exclusion hôte conformes. Neuf groupes couvrent normalisation, propriétaire, délai, expiration, horloge, nettoyage, absence HTTP en usages seuls, annulation de réponses tardives, scope, cache, reprise et priorité manuelle avec le vrai MarketService et un transport simulé. ConfigSerializationProbe passe les modes Newtonsoft None/Auto/Objects, options indépendantes et valeurs par défaut v1. GatheringProbe passe également sur les fichiers du jeu.

Référence primaire : [Dalamud ItemUtil au commit étudié](https://github.com/goatcorp/Dalamud/blob/1e514694497ae448fca8e8a46f7b63642ad1e5ad/Dalamud/Utility/ItemUtil.cs). Les marqueurs publics y sont vérifiés ; le rejet conservateur de tous les identifiants >=2000000 ne prétend pas reproduire la résolution EventItem du SDK.

Le rendu et le survol natifs restent à observer en jeu. Les essais de projection de synthèse et Macro Chain sont reportés par l’utilisateur ; aucune nouvelle demande ne leur est substituée. Prochaine tranche indépendante : accès explicite depuis IContextMenu, en réutilisant la sélection et les routes communes.
