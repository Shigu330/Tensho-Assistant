# Demandes utilisateur à planifier

Liste reprise de `ISSUES.md` (2026-11-01), classée par nature puis par module. Chaque demande dit ce qui ne va pas et le résultat attendu.
Une demande traitée est reportée dans [CHANGELOG.md](../../CHANGELOG.md) avec son numéro (ex. « R-03 ») et marquée FAIT ou CLOS ici
(la ligne est gardée comme historique). L'ordre de passage est fixé dans [docs/ai/STATUS.md](../ai/STATUS.md) (« Prochaine action »).
Vue d'ensemble, points à décider et tests à faire : [ROADMAP](ROADMAP.md). État relu au 0.446 (2026-11-03).

## Corrections

| N° | Module | Problème constaté | Résultat attendu |
|---|---|---|---|
| R-01 (FAIT 0.242–0.243 : retrait par clic droit, puis automatique quand la quête n'est plus active ; à valider en jeu) | Carte | Cibler une quête depuis le journal ou la liste des quêtes actives ne se comporte pas bien (icône de cible qui reste, impossible à retirer). | Revoir le ciblage de quête : cible suivie et retirée correctement. Étudier le fonctionnement de Mappy (lecture seule, licence AGPL). |
| R-02 (FAIT 0.241 ; à valider en jeu) | Butin des contenus (panneau de mission) | Le filtre « Non obtenus seulement » ne montre pas les cartes de Triple Triad ni les rouleaux d'orchestrion, et ceux-ci n'ont pas la pastille ✓/✗. | Cartes et rouleaux pris en compte par le filtre et marqués ✓/✗ comme les montures et mascottes. |
| R-03 (FAIT 0.244 ; à valider en jeu) | Carte (clic droit) | Les PNJ de Triple Triad de la zone ne figurent pas dans les listes locales du clic droit. | Un onglet ou une section « Triple Triad » avec les PNJ de la zone (drapeau, état battu ou non). |
| R-04 (FAIT 0.245 ; réglage par icône en bouton souris 0.298, validé en jeu avec 0.297–0.299) | Carte (clic droit) | Un clic droit sur une icône de la carte n'ouvre pas directement le menu de cette icône. | Le clic droit sur une icône ouvre son menu ; une liste d'icônes à ignorer (réglable) laisse passer le clic au menu général. |

## Interface

| N° | Module | Demande | Résultat attendu |
|---|---|---|---|
| R-05 (CLOS 2026-11-01 : rien à coder, décision utilisateur ; voir docs/evidence/0.246-r05-window-order.md) | Toutes les fenêtres | Les fenêtres de Tensho passent au-dessus de tout. | Si Dalamud le permet, un ordre d'affichage proche de celui des fenêtres du jeu (rester derrière une fenêtre du jeu mise au premier plan). À étudier : faisabilité d'abord. |
| R-06 (FAIT pour l'essentiel 0.247–0.294, plan docs/goals/R-06.md ; reste : réglages utiles manquants, cadres et textures du jeu) | Objets et Rangements (Banque) ; élargie le 2026-11-01 à toute l'interface | Interface à revoir pour être plus claire et plus pratique. | Une révision de l'ergonomie de ces deux modules (disposition, filtres, lisibilité), proposée avant d'être codée. Fait : modèles communs, barre latérale par catégories, fiche objet complète, page Rangements séparée, marché enrichi, infobulle et ↺ sur chaque réglage, légèreté mesurée, police de titre du jeu. |

## Nouvelles fonctions

| N° | Module | Demande | Résultat attendu |
|---|---|---|---|
| R-07 (FAIT : page Compagnie libre et expéditions 0.307, meilleurs trajets de sous-marin 0.309, montages 0.315, suite DEC-023 0.334–0.337, historique 0.379 ; à valider en jeu) | Compagnie libre (nouveau) | Aide pour les projets de compagnie libre : sous-marins, dirigeables… | Étude des plugins existants utiles (références, licences), puis un module : trajets, retours, butins et rangs. Reste : pas de planificateur de trajets pour les dirigeables (aucune donnée sous licence compatible) ; valeur du butin sans paliers, au prix PNJ. |
| R-08 (FAIT : page Rappels 0.300, icône et infobulle dans la barre du serveur 0.300–0.302, expéditions de la compagnie 0.307, escouade 0.333 ; Planeurs d'Ehcatl laissés, rien de lisible 0.334 ; à valider en jeu) | Rappels (nouveau) | Rappels des tâches quotidiennes, hebdomadaires et autres. | Une liste de tâches par période (réinitialisation quotidienne / hebdomadaire), cochables, avec rappel, reliée au module Alarmes. Reste : escouade de Grande Compagnie, livraisons des Planeurs d'Ehcatl. |
| R-11 (FAIT : étapes 1 à 3 0.310–0.315, étape 4 barde de chocobo 0.340, plan docs/goals/R-11.md ; 0.310–0.314 validées en jeu, 0.315 et 0.340 à valider) | Équipement (nouveau) | Planificateur d'équipement style fenêtre Personnage, équipement proche du niveau, filtres (niveau exact hors paliers…), stats réelles, essayage partout, partage des builds ; même style pour sous-marins / dirigeables et tout équipement utile. | Page Équipement, cabine d'essayage, codes de build ; puis sources / possession, matérias, montages de vaisseaux. Fait : tout cela (mirage et plafonds de matérias en plus). Reste : étape 4 (autres équipements), à décider. |
| R-09 (FAIT 0.342 : page Événements ; à valider en jeu) | Événements (nouveau) | Suivre les événements saisonniers actifs (Halloween, etc.). | Un module listant les événements en cours, leurs dates, quêtes et récompenses, avec ce qui est déjà obtenu. |
| R-14 (vers la toute fin de la file, avant la campagne de tests en jeu ; règle déjà appliquée au nouveau code, docs/ai/RULES.md) | Tout le plugin (technique) | « Il faudrait ajouter une étape pour tester notre résistance aux updates FF14 et y pallier un maximum », puis « vers la toute fin et directement dans la logique des codes » (2026-11-02). | Passage sur tout le code : relevé de chaque dépendance native (structures FFXIVClientStructs, fenêtres et nœuds du jeu, index d'AtkValues, hooks, colonnes de feuilles Lumina) ; dans le code, garde de chacune (contrôles de forme et de taille, version du jeu connue, dégradation propre en « indisponible après une mise à jour du jeu », jamais de plantage ni de valeur fausse) ; auto-vérification au chargement visible dans Diagnostics ; tests qui simulent une donnée changée ; marche à suivre après un patch (docs). |
| R-13 (FAIT 0.320 : Préférences › Crédits ; à valider en jeu) | Crédits (nouvelle page) | « Réserve une section complète dans le plugin pour les crédits de ce qu'on aurait utilisé dans tout le code » (2026-11-02). | Une page « Crédits » dans le plugin, pour tout le monde (pas seulement en mode développeur) : bibliothèques (Dalamud, FFXIVClientStructs, Lumina…), données embarquées (fichier, licence, copyright), données lues en ligne (service, licence, réglage qui les coupe), références lues sans copie (AGPL ou sans licence), règles communautaires reprises comme estimations, avis sur les données du jeu ; tirée d'une seule liste dans le code, alignée avec la section « Credits » du README, avec un test qui vérifie que chaque donnée embarquée a son crédit. |
| R-12 (FAIT 0.319 : Réglages › Alarmes › Discord ; à valider en jeu) | Alarmes, Rappels, Compagnie libre | « Prévois dans une todo list un hook Discord pour envoyer les alarmes avec message sur un serveur Discord ; voir plus si possible ou intéressant » (2026-11-02). | Réglage « Webhook Discord », désactivé par défaut : URL saisie par l'utilisateur, envoi des alarmes Tensho (récolte, pêche, rappels, retours de vaisseaux) avec un message modifiable par alarme, bouton « Envoyer un test ». À étudier ensuite : autres événements utiles (retours de sous-marins et dirigeables, remises à zéro du calendrier, alerte de prix du marché), mention d'un rôle, plusieurs webhooks. Contraintes : URL du webhook traitée comme un secret (jamais dans les journaux ni dans le rapport copié de Diagnostics), aucune donnée de personnage envoyée sans choix explicite, envoi hors du rendu (worker) et limite de débit de Discord respectée, conformité aux règles de publication Dalamud à vérifier (envoi réseau seulement à la demande de l'utilisateur), plugins existants comparables à relever pendant c). |

## Vérifications techniques

| N° | Domaine | Demande | Résultat attendu |
|---|---|---|---|
| R-10 (FAIT 0.246, voir docs/evidence/0.246-cache-audit.md ; revérifié 0.281 avec la durée « Jamais ») | Caches | Vérifier tous les caches. | Un audit : aucun outil ne sollicite trop souvent les sources communautaires (Garland, wiki, LuminaSupplemental, GatherBuddy, Teamcraft, Universalis) ni les données du jeu ; durées et cadences documentées. |

## Demandes du 2026-11-02 (file d'attente de STATUS)

Elles n'ont pas de numéro R ; la lettre est celle de STATUS (« Prochaine action »), à traiter une à la fois, dans cet ordre.

| Lettre | Domaine | Demande | Résultat attendu |
|---|---|---|---|
| b) (FAIT 0.316 ; décisions 0.317, DEC-023) | Documentation | Tour des `.md` et des descriptions des outils dans le code ; feuille de route complétée. | Documents conformes au code ; points indécis et tests à faire listés dans la [ROADMAP](ROADMAP.md). |
| c) (FAIT 0.318 : docs/project/GAPS.md, choix DEC-025) | Comparaison | Ce qui manque à Tensho par rapport aux plugins existants ; élargi le 2026-11-02 : « pas que plugin de référence mais ceux du dépôt Dalamud (voir repo non officiel si ça respecte les règles Dalamud upload) » ; puis « ajoute une phase décision avant aligner avec roadmap pour que je choisisse ce qu'on rajoute ». | Un plan : fonction manquante, plugins sources (17 références, dépôt officiel, dépôts non officiels conformes aux règles), licence (lu seulement si AGPL ou sans licence), source des données ; puis choix de l'utilisateur, et seulement ensuite la feuille de route. |
| d) (FAIT 0.321–0.326) | Code | Réglages qui manquent ; outils de développement pour la communauté open source ; « regarde aussi ce que tu pourrais faire pour améliorer notre git » (2026-11-02). | Relevé des valeurs figées à rendre réglables ; outils de développement et améliorations du dépôt git / GitHub proposés (rien n'est poussé ni réglé sur GitHub sans accord). |
| e) (FAIT 0.327–0.332) | Tous les outils | Améliorations générales. | La carte d'abord, puis les autres outils. |
