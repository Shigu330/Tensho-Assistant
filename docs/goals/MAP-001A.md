# MAP-001A — navigateur de cartes

État : DONE côté code et vérification managée, version 0.16.1 ; rendu/ouverture en jeu à confirmer. Verify.ps1 : 97/97 tests et package valides.

Implémentation : MapCatalog/Service immuables, loader framework par lots (32 lignes / budget souple de 2 ms), construction sur worker annulable possédé et protégé contre les publications après Dispose. Recherche mémorisée et liste avec clipper ; coordonnées finies, ouverture native avec gestion du refus et lien vers les ressources de zone. Accès depuis la fiche Mappy ; diagnostics intégrés.

Probe sur les fichiers du jeu : 1 223 cartes ; source de récolte reliée à la carte 212 / territoire 398, Avant-pays dravanien. IDs distincts, sous-cartes, recherche multiterme sans accents, listes protégées, doublons/entrées invalides, annulation et états du service testés. Le probe ne prouve ni l'accès du personnage ni le rendu runtime.

Essai après chargement de la DLL 0.16.1 : Accueil & outils → Mappy → ouvrir les cartes ; rechercher une zone et une sous-carte, sélectionner puis saisir X/Y. Vérifier le repère dans la carte du jeu, le refus éventuel hors contexte et le lien vers les ressources de zone. Recharger/décharger pendant le chargement et vérifier les logs. Aucun nouvel essai demandé avant compilation.

Consolidation 0.16.1 : l'ancien RecipeCatalogLoader est désormais protégé par verrou, annulation et Dispose idempotent ; copie framework bornée aussi en temps et libération des feuilles après copie. RecipeCatalog.Build accepte l'annulation avant/pendant copie et indexation. Test supplémentaire avant et pendant la copie ; aucune validation native déduite.

Objectif : base Mappy consultative utilisant les cartes natives du jeu. Catalogue Map/Territory/PlaceName copié au framework, recherche par région/zone/sous-zone, sélection d'une carte et ouverture de la carte du jeu sur des coordonnées saisies. Lien vers les ressources connues de la zone.

Les coordonnées sont celles lisibles par le joueur, transmises au constructeur float MapLinkPayload (contrat vérifié dans Dalamud.xml local). Aucun repère exact de récolte n'est inventé. Pas de téléportation ni de disponibilité/déblocage déduit des cartes présentes. Les cartes d'événements et sous-zones peuvent apparaître dans le catalogue ; leur accessibilité est inconnue.

Preuves locales au 2026-10-27 : Map expose RowId, TerritoryType, PlaceNameRegion/PlaceName/PlaceNameSub, SizeFactor, OffsetX/Y, MapIndex. MapLinkPayload(uint,uint,float,float,float) reçoit niceXCoord/niceYCoord ; dernier argument fudgeFactor facultatif laissé à sa valeur hôte.

Validation : modèle immuable, IDs carte/territoire séparés, recherche et entrées invalides ; build et package ; probe sur les fichiers du jeu. L'ouverture effective et le rendu exigent un essai Dalamud après compilation.
