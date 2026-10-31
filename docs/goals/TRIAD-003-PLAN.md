# TRIAD-003 — finir l'outil Triple Triade (référence fonctionnelle : TriadBuddy)

Demande utilisateur 2026-10-31 : « l'outil triad doit être fini comme le plugin existant sur Dalamud TriadBuddy ». TriadBuddy est une
référence fonctionnelle seulement (aucune copie de code). État au 0.158 : carnet de cartes, deck, solveur manuel (règles, profondeur bornée)
testés ; observation des faces du match (TRIAD-002, 0.26) expérimentale, **jamais validée en jeu**, sans lien au solveur.

## Écart avec TriadBuddy

| Fonction TriadBuddy | Tensho 0.158 | Étape |
|---|---|---|
| Conseil pendant le match, superposé au jeu (meilleure carte et meilleure case) | fait en 0.160 (à valider en jeu) | 1 |
| Règles du match (PNJ + roulette) lues automatiquement | fait en 0.160 (noms lus dans la fenêtre du match) | 1 |
| Cartes cachées de l'adversaire estimées depuis le deck du PNJ | fait en 0.160 (combinaisons équiprobables) | 2 |
| Liste des PNJ : règles, cartes à gagner, obtenues ou non, battu ou non | fait en 0.168 (+ lots prioritaires, cartes manquantes) | 3 |
| Optimiseur de deck contre un PNJ | fait en 0.161 (génération depuis la fenêtre de défi) | 4 |
| % de victoire par deck sur la fenêtre de défi | fait en 0.161 (decks du jeu + deck Tensho) | 5 |
| Infos de carte (source, obtenue) dans le carnet du jeu | carnet Tensho seulement | 6 |

## Étapes (une version vérifiable chacune)

1. Lien observation → solveur + superposition : lecture des faces (prouvée en jeu d'abord), règles depuis la feuille TripleTriad du PNJ
   ciblé (sonde) et texte des règles de l'addon, positions des cartes et des cases (`CardDropControl`), cadre vert sur la meilleure carte
   puis la meilleure case. Prérequis : un match observé par l'utilisateur pour valider la lecture TRIAD-002.
2. Cartes rouges cachées : candidats du deck du PNJ (feuille TripleTriad), solveur sur l'ensemble des possibilités, sinon prudence.
3. Liste des PNJ (position carte Tensho, règles, récompenses, état).
4. Optimiseur de deck par PNJ (cartes possédées).
5. Superposition du choix de deck. 6. Infos de carte dans le carnet du jeu.

Toutes les données de jeu (identité de carte, règles, decks PNJ) se prouvent par sonde avant usage ; rien n'est automatisé (le joueur clique).
