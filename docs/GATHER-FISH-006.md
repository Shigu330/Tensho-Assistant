# GATHER-FISH-006 — Références du carnet de pêche

0.60.0.0 — 2026-10-29 — IMPLEMENTED / TESTED ; NOT VALIDATED_IN_GAME.

Reprise après relecture de RULES/STATUS/GOAL, bilan BASE-AUDIT-002 et documents récolte pertinents. Le compteur de cinq heures est renouvelé ; objectif global actif, réserve utilisateur de 5 % maintenue. Les essais de projection craft et Macro Chain restent reportés, sans nouvelle demande.

Dans les lieux de récolte d’un poisson sélectionné, le cadre repliable « Carnet de pêche · données du jeu » présente le texte natif, les restrictions horaire/météo signalées et le tome référencé. Ces références statiques ne prouvent ni prise du personnage, ni déblocage du tome. Les horaires précis, météo requise, appâts et disponibilité actuelle restent non renseignés. Une restriction absente du carnet n’est pas présentée comme une disponibilité permanente.

FishingNotebookIndex est un composant immuable du GatheringCatalog existant, pas un second service de récolte. Il indexe les entrées par ItemId et conserve séparément FishParameter.RowId, variantes éventuelles et FishingSpot du carnet. Une fiche peut exister sans source de lieu connue ; aucun point n’est inventé. Les appels antérieurs au constructeur du catalogue restent compatibles et reçoivent un index vide.

FishingNotebookCopy.Lumina projette les feuilles dans des valeurs managées. L’entrée doit référencer un Item standard. La fiche FishingNoteInfo doit avoir le même RowId et le même Item, de type Item ; sinon restrictions inconnues. Les codes 0/1 deviennent respectivement « non signalée »/« signalée » ; les autres restent inconnus. Le code météo 2 est réellement présent et sa signification n’est pas supposée. FolkloreBook vient de GatheringSubCategory, sans déduire son acquisition.

Le loader partagé ajoute une phase de lecture FishParameter/FishingNoteInfo dans ses lots existants de 32 lignes et budget souple de deux millisecondes par update. Texte et primitives sont copiés sur framework ; aucun RowRef/ExcelRow ou pointeur n’est envoyé au worker. Annulation, Stop/Dispose, libération des listes/feuilles et publication du catalogue gardent le lifecycle existant. Aucune lecture dans Draw, aucun réseau ajouté et aucune action de pêche exécutée.

Preuves primaires : GatherBuddyReborn commit a204f2f45cb98302dbe938921255c3a6d617c775, Classes/Fish.cs joint FishParameter/FishingNoteInfo par RowId et traite le code 1 comme restriction. Fish.CatchData.cs initialise l’appât inconnu ; Data/Fish/DataBase.cs et Data2.0.cs ajoutent appâts, transitions météo et intervalles via données supplémentaires. Ces faits distinguent références natives et enrichissements ; aucune implémentation ou base d’appâts externe importée. Licence historique Apache-2.0 consignée dans REF-GATHER, pas une nouvelle vérification de licence.

ApiProbe confirme localement FishParameter, FishingNoteInfo et GatheringSubCategory. FishingNotebookProbe, partageant la projection de production, exécuté sur sqpack : 2 512 lignes FishParameter, 2 832 FishingNoteInfo ; 2 461 fiches standard copiées, 2 461 notes concordantes, aucun lien manquant ou divergent ; 2 461 Item et lieux référencés résolus, 296 tomes. Codes horaires 0/1 ; météo 0/1/2, dont 491 restrictions météo conservées inconnues. Les notes absentes et artificiellement étrangères sont refusées. L’index reste utilisable après fermeture des données natives.

Verify.ps1 : 389/389 tests, Release sans avertissement/erreur, versions/package/SHA-256 canonique/exclusion hôte verts. Quatre groupes nouveaux : identité Fish/Item et variantes, codes connus/inconnus, copie et conflits d’ID, invalides/annulation/catalogue commun/lifecycle. GatheringProbe a aussi repassé les contrôles natifs antérieurs, dont les 4 199 sources dédupliquées, horaires, recettes, cartes, quêtes et Triple Triade, sans consultation réseau optionnelle. Le bridge framework et le rendu en jeu ne sont pas exercés par ces tests.

Protocole ultérieur conservé : sélectionner un poisson, ouvrir le carnet, comparer texte/restrictions/tome au jeu ; essayer une fiche au code météo non interprété et vérifier qu’elle ne dit pas « sans restriction ». La fiche n’indique aucune prise ou déblocage du personnage. Ce protocole n’a pas été exécuté.

Suite : GATHER-FISH-CONDITIONS-007-STUDY, déterminer la signification du code météo 2 et étudier une source explicite d’appâts/conditions avec provenance/licence avant enrichissement. Conserver Unknown tant que la preuve manque. Les 17 domaines restent PARTIAL.
