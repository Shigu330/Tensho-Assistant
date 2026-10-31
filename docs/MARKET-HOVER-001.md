# MARKET-HOVER-001 — prix au survol

Historique 0.17.1 : le lecteur et le panneau privés décrits ici ont été mutualisés en [USE-HOVER-003](USE-HOVER-003.md), version 0.52.0. Les options prix/usages restent indépendantes ; les assertions historiques ne sont pas une validation runtime de la version actuelle.

État : DONE côté code et vérification managée, version 0.17.1 ; 104/104 tests et package valides. Survol réel et rendu à confirmer en jeu.

Consolidation 0.17.1 : publication volatile de l'option entre UI/framework ; relevés absents, âgés de plus de 24 h ou datés de plus de cinq minutes dans le futur signalés. Un test couvre ces dates. L'icône de l'objet est affichée et le panneau est repositionné dans le viewport.

Probe local supplémentaire : les fichiers du jeu confirment Sirop de bouleau 12892 et Potion 4551 commercialisables/HQ ; Gil 1 non commercialisable/non HQ. La normalisation 1 012 892 → 12 892 HQ est exécutée sur ce catalogue réel ; 2 012 892 et Gil sont refusés. Cette vérification statique ne constitue pas une observation du survol dans le client.

Implémentation : option MarketPricesOnHover de Configuration v1, désactivée par défaut, sauvegarde avec rollback ; copie HoveredItem sur framework via lecteur possédé/gardé ; IsUntradable et ItemSearchCategory natifs copiés dans ItemDefinition.IsMarketable. Les objets non commercialisables, HQ impossibles et IDs spéciaux non résolus ne déclenchent pas de requête.

Une seule instance MarketService gère le cache et la cadence communs : maximum une requête active et trois secondes entre appels réseau. La vue automatique possède un état distinct et ne remplace pas la sélection manuelle ; une demande manuelle préempte le travail automatique avec retour explicite si le délai n'est pas écoulé. Survol stable 600 ms, nouvelle tentative au plus toutes les cinq secondes, trente secondes de recul après erreur réseau. Les réponses d'une génération annulée n'entrent pas dans le cache. Le panneau près du pointeur est sans interaction, avec icône, NQ/HQ, monde et dates. L'objet en contexte reste intact.

Tests ajoutés : normalisation HQ une seule fois, IDs spéciaux et objets liés, migration opt-in, stabilité/délai/identité/monde/déconnexion, cache commun, cadence, priorité manuelle, erreurs/backoff, annulation et réponses retardées. API locale vérifiée par ApiProbe et Dalamud.xml ; compilation API 15 réussie.

Essai après chargement de 0.17.0 : dans Réglages, activer Afficher les prix au survol (les seuls IDs publics monde/objet sont transmis). Fermer la fenêtre puis survoler un objet commercialisable NQ et HQ pendant une seconde. Vérifier icône, nom, monde, minimum NQ/HQ et dates ; un objet lié ne doit pas déclencher de consultation. Changer rapidement d'objet, demander un prix manuel, désactiver l'option, changer de personnage/monde et décharger pendant une requête. Aucun ancien objet/prix ni appel continu après désactivation ne doit subsister. Ce protocole reste non exécuté en jeu.

Objectif : compléter Price Insight par les prix de l'objet survolé dans le jeu, sans modifier le contexte sélectionné par l'utilisateur.

Preuve locale API 15, 2026-10-27 : Dalamud.xml IGameGui.HoveredItem fournit l'ID de l'objet ou zéro ; au-delà de 1 000 000, soustraire exactement 1 000 000 et traiter HQ. IGameGui.HoveredItemChanged existe également. Vérifier le type exact et la validité dans ItemCatalog ; ne pas appliquer un modulo arbitraire aux identifiants spéciaux. Lire dans un callback framework, copier vers un état managé ; aucune lecture native dans Draw.

Périmètre : option désactivée par défaut, sauvegarde/rollback de Configuration v1, divulgation des seules données publiques monde/objet envoyées. Délai de survol stable avant toute demande, cache/déduplication, cadence réseau bornée. Utiliser un service marché distinct pour préserver la consultation manuelle et son état ; les deux instances partagent si nécessaire un limiteur global ou une infrastructure de cache, sans doubler silencieusement les appels. Alternative préférable : partager les quotes publiques dans un cache commun et distinguer les vues demandeuses.

Affichage : panneau compact daté près du pointeur ou de la fiche native, NQ/HQ et monde courant, absence de donnée distincte de zéro, erreur discrète et pas d'appel à chaque frame. Ne pas montrer une réponse d'un autre objet, monde ou personnage. Rester sur l'objet choisi au début de la frame ; ne pas remplacer l'objet en contexte partagé.

Validation : normalisation NQ/HQ/IDs spéciaux, temporisation, coalescence, invalidation, absence d'appels si option décochée, Dispose et frontières propriétaire/monde. Consulter les propriétés natives de commercialisation avant de demander les prix d'objets non commercialisables. Build/package avant tout essai demandé. La documentation locale et les tests ne prouvent pas un survol réellement observé.
