# GOAL actif

ID: TRIAD-003 — finir l'outil Triple Triade comme TriadBuddy (demande utilisateur 2026-10-31). Plan : [TRIAD-003-PLAN](../goals/TRIAD-003-PLAN.md).
ÉTAPE 1: conseil pendant le match superposé au jeu. Avant de lier, prouver en jeu : règles appliquées, joueur au trait, PNJ, positions.

DONE (0.159.0): sonde PNJ ([preuve](../evidence/0.159.0-triad-npc-probe.log)) ; capture de match dans dalamud.log.

DONE (0.160.0): preuves du match (tour, règles, carte imposée, PNJ, positions) ; inversion droite/gauche corrigée ; cartes cachées
depuis le deck du PNJ ; conseil superposé (meilleure carte + meilleure case) et texte dans Match observé.

DONE (0.161.0): panneau de la fenêtre de défi — % de victoire estimés par deck (decks du jeu + deck Tensho) et génération de deck
depuis les cartes possédées ; conseil en match 0.160 validé par l'utilisateur.

PROTOCOLE EN JEU (DLL 0.161.0.0):
1. Observation Triple Triade active ; Collections ouvertes une fois (cartes possédées chargées).
2. Cibler un PNJ et lui parler jusqu'à la fenêtre de défi : un panneau Tensho apparaît à droite (PNJ, règles, % par deck).
3. « Générer un deck pour ce PNJ » : quelques secondes, puis 5 cartes et leur % ; « Enregistrer comme deck Tensho » ; composer le deck
   dans l'éditeur du jeu et vérifier que son % apparaît ensuite dans la liste.
4. Comparer les % avec l'impression en match (estimations, pas des promesses).

DONE (0.162.0): liste des decks Tensho par PNJ (suppression) ; aide-mémoire des cartes à côté de l'éditeur de decks du jeu
(ouvrir l'éditeur de decks du jeu avec l'observation active : panneau « Tensho — deck à composer »).

EN COURS (0.163): surbrillance des cartes du deck dans l'éditeur de decks du jeu — capture en attente (voir STATUS).

SUITE: liste des PNJ (étape 3 du plan), infos de carte ; CACTPOT-019 à valider ; PUBLISH-018 côté auteur.
VALIDATION: Verify.ps1 vert ; rapport utilisateur en jeu.
