# Demandes utilisateur à planifier

Liste reprise de `ISSUES.md` (2026-11-01), classée par nature puis par module. Chaque demande dit ce qui ne va pas et le résultat attendu.
Une demande traitée est reportée dans [CHANGELOG.md](../../CHANGELOG.md) avec son numéro (ex. « R-03 »), puis retirée d'ici.
L'ordre de passage est fixé dans [docs/ai/STATUS.md](../ai/STATUS.md) (« Prochaine action »).

## Corrections

| N° | Module | Problème constaté | Résultat attendu |
|---|---|---|---|
| R-01 (en partie 0.242 : retrait manuel par clic droit) | Carte | Cibler une quête depuis le journal ou la liste des quêtes actives ne se comporte pas bien (icône de cible qui reste, impossible à retirer). | Revoir le ciblage de quête : cible suivie et retirée correctement. Étudier le fonctionnement de Mappy (lecture seule, licence AGPL). |
| R-02 (FAIT 0.241) | Butin des contenus (panneau de mission) | Le filtre « Non obtenus seulement » ne montre pas les cartes de Triple Triad ni les rouleaux d'orchestrion, et ceux-ci n'ont pas la pastille ✓/✗. | Cartes et rouleaux pris en compte par le filtre et marqués ✓/✗ comme les montures et mascottes. |
| R-03 | Carte (clic droit) | Les PNJ de Triple Triad de la zone ne figurent pas dans les listes locales du clic droit. | Un onglet ou une section « Triple Triad » avec les PNJ de la zone (drapeau, état battu ou non). |
| R-04 | Carte (clic droit) | Un clic droit sur une icône de la carte n'ouvre pas directement le menu de cette icône. | Le clic droit sur une icône ouvre son menu ; une liste d'icônes à ignorer (réglable) laisse passer le clic au menu général. |

## Interface

| N° | Module | Demande | Résultat attendu |
|---|---|---|---|
| R-05 | Toutes les fenêtres | Les fenêtres de Tensho passent au-dessus de tout. | Si Dalamud le permet, un ordre d'affichage proche de celui des fenêtres du jeu (rester derrière une fenêtre du jeu mise au premier plan). À étudier : faisabilité d'abord. |
| R-06 | Objets et Rangements (Banque) | Interface à revoir pour être plus claire et plus pratique. | Une révision de l'ergonomie de ces deux modules (disposition, filtres, lisibilité), proposée avant d'être codée. |

## Nouvelles fonctions

| N° | Module | Demande | Résultat attendu |
|---|---|---|---|
| R-07 | Compagnie libre (nouveau) | Aide pour les projets de compagnie libre : sous-marins, dirigeables… | Étude des plugins existants utiles (références, licences), puis un module : trajets, retours, butins et rangs. |
| R-08 | Rappels (nouveau) | Rappels des tâches quotidiennes, hebdomadaires et autres. | Une liste de tâches par période (réinitialisation quotidienne / hebdomadaire), cochables, avec rappel, reliée au module Alarmes. |
| R-09 | Événements (nouveau) | Suivre les événements saisonniers actifs (Halloween, etc.). | Un module listant les événements en cours, leurs dates, quêtes et récompenses, avec ce qui est déjà obtenu. |

## Vérifications techniques

| N° | Domaine | Demande | Résultat attendu |
|---|---|---|---|
| R-10 | Caches | Vérifier tous les caches. | Un audit : aucun outil ne sollicite trop souvent les sources communautaires (Garland, wiki, LuminaSupplemental, GatherBuddy, Teamcraft, Universalis) ni les données du jeu ; durées et cadences documentées. |
