# CACTPOT-019 — Mini Cactpot superposé

ID: CACTPOT-019 — solution Mini Cactpot superposée à la fenêtre du jeu (demande utilisateur 2026-10-31, comme ezMiniCactpot)
DONE (0.158.0): superposition (cadre vert) des meilleures cases puis lignes sur `LotteryDaily`, active avec l'observation Mini Cactpot,
fenêtre Tensho fermée ou non.

PROTOCOLE EN JEU (DLL 0.158.0.0):
1. Paramètres › Modules : cocher « Observer le ticket Mini Cactpot et afficher la solution sur la fenêtre du jeu ».
2. Gold Saucer, Mini Cactpot : à chaque étape, une case encadrée en vert ; après la 3ᵉ case, la ou les lignes à choisir encadrées
   (vérifier que les diagonales encadrées sont les bonnes).
3. Fermer le ticket : plus rien n'est dessiné. Fermer la fenêtre Tensho : la superposition reste.

SUITE: TRIAD-003 ([plan](../goals/TRIAD-003-PLAN.md)). PUBLISH-018 : [document](../goals/PUBLISH-018.md) (reste côté auteur).
VALIDATION: Verify.ps1 vert ; rapport utilisateur en jeu.
