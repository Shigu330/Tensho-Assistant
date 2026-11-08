# Plan global

Plan du 2026-11-05, à la version 0.539.0, issu de la revue faite avec l'utilisateur et réorganisé à sa demande ; détail de chaque point : [PROPOSALS-TRIAGE](PROPOSALS-TRIAGE.md). **En cours depuis le 2026-11-05 (feu vert de l'utilisateur).** L'ordre des phases et des points est une proposition de l'agent.

**Depuis [DEC-039](DECISIONS.md) : pas de soumission au dépôt officiel, dépôt personnalisé seulement, règles Dalamud gardées.** **La 1.0.0 vient après tout le plan** (décision de l'utilisateur, [DEC-037](DECISIONS.md)) : il veut un plugin complet, et accepte qu'une soumission jugée trop grosse puisse être refusée ([PUBLISHING](PUBLISHING.md), point 6).

**Tout ce qui demande de jouer vient en dernier, un point à la fois** (demande de l'utilisateur, 2026-11-05). Les phases 1 à 4 se font sans lui : `Verify.ps1` vert, commit et poussée, sans essai en jeu. Une compilation complète recharge la DLL si le jeu tourne : il n'a rien à faire pour autant.

| Phase | Contenu | Points | Il faut jouer ? |
|---|---|---:|---|
| 1 | Corrections sans le jeu | 3 | Non |
| 2 | Petites propositions | 43 | Non |
| 3 | Propositions moyennes | 19 | Non |
| 4 | Idées, sonde web et sources sans le jeu | 6 | Non |
| 5 | Relevés en jeu, puis le code qui en dépend | 27 | Oui, l'un après l'autre |
| 6 | Tests en jeu de tout ce qui précède | 1 | Oui, l'un après l'autre |
| 7 | Juste avant la 1.0.0 : listes d'outils, documents, aide | | Non |
| Ensuite | Version 1.0.0, sur le dépôt personnalisé (pas de soumission, DEC-039) | | |
| Après la 1.0.0 | C10, chrono de touche | 1 | |

Total avant la 1.0.0 : 99 points. Le bouton pour afficher ou cacher des icônes sur la carte existe déjà (rapport de l'utilisateur) : la demande est retirée.

## Déroulement en jeu (phases 5 et 6)

1. L'agent donne **un seul point** : quoi faire dans le jeu, en quelques lignes.
2. L'utilisateur le fait quand il veut, puis répond ; l'agent lit le journal.
3. S'il y a du code à écrire ou une erreur, l'agent corrige, puis on refait ce même point.
4. Seulement alors, le point suivant. Une session peut s'arrêter entre deux points.

**Ordre (demande de l'utilisateur, 2026-11-06) :** 1. ce qui se fait librement ; 2. le contenu classé, ce qui dépend d'un timer et les instances particulières : tournois de Triple Triade, Lord of Verminion, Mahjong, Dédale, donjon profond, récolte du jardin ; 3. **le JcJ tout à la fin** : match de l'arène contre un joueur, Crystalline Conflict, Frontline.

## Phase 1 : corrections sans le jeu

| Où | Ce qui manque | Issue |
|---|---|---|
| Jardin | ~~Ajouter les Bleuets à la liste de graines~~ fait en 0.540 | #36 |
| Objets | ~~Blocs d'objets sans prix~~ fait en 0.541 | #37 |
| Escouade | ~~Bouton d'alarme pour une mission en cours~~ déjà présent : cloche de la ligne dans Rappels, et alarme automatique depuis 0.455 (`MainWindow.Reminders.cs`, `Alarms/AutoAlarmWatcher.Dalamud.cs`) ; à vérifier en phase 6 | #37 |

Si l'une d'elles demande finalement un relevé en jeu, elle passe en phase 5.

## Phase 2 : petites propositions

| N° | Proposition | Taille | Précision |
|---|---|---|---|
| P1 | ~~Zoom à l'ouverture, réglable et gardé entre les sessions~~ fait en 0.542 | S |  |
| P2 | ~~Taille des étiquettes réglée à part de celle des icônes~~ fait en 0.543 | S |  |
| P3 | ~~Éthérite la plus proche du drapeau ou d'un point, avec son coût~~ fait en 0.544 | S |  |
| P5 | ~~Molette : zoom depuis toute la fenêtre et pas réglable~~ fait en 0.545 | S |  |
| P9 | ~~ALEA : bonus d'expérience et couleur selon le temps restant~~ fait en 0.548 | S | La zone d'ALEA reste rose : pas de couleur selon le temps restant. |
| P15 | ~~État des lectures du jeu affiché sur la carte (R-14, partie carte)~~ fait en 0.569 | S |  |
| P16 | ~~Option : clic gauche sur la carte pour poser le drapeau~~ fait en 0.546 | S | Avec réglage. |
| P20 | ~~Bouton « Couches » pour tout afficher ou masquer en un clic~~ fait en 0.547 | S |  |
| I0 | ~~Compter les pièces portées dans les stocks et dans « possédé »~~ fait en 0.549 | S |  |
| I5 | ~~Coût de préparation au prix du centre de données (option de A-06)~~ fait en 0.574 | S |  |
| I8 | ~~X-07, partie anticipée : ouvrir l'objet sur Universalis, Garland Tools, Teamcraft ou le wiki~~ fait en 0.561 | S |  |
| I9 | ~~Copier le nom de l'objet et lien d'objet dans le chat (local)~~ fait en 0.561 | S |  |
| I15 | ~~Partager un modèle de tri en texte (copier / coller)~~ fait en 0.566 | S |  |
| I16 | ~~Commandes « /tensho objet » et « /tensho prix »~~ fait en 0.555 | S |  |
| I18 | ~~I-07, partie anticipée : copier « prix le plus bas − 1 » depuis la page Marché~~ fait en 0.567 | S |  |
| I19 | ~~Nom anglais de l'objet dans la fiche et l'infobulle~~ fait en 0.562 | S |  |
| I20 | ~~Aperçu du tri : cases déplacées signalées~~ fait en 0.565 | S |  |
| ~~I22~~ fait en 0.669 sans mesure en jeu | Calculs faits à chaque image (reportés à e) : coût de préparation et historique du marché | S |  |
| C0 | ~~A-01, partie anticipée : choisir la qualité visée dans le panneau du carnet~~ déjà présent | S |  |
| C2 | ~~Essayer ses séquences sauvegardées sur toutes les recettes~~ fait en 0.552 | S |  |
| C3 | ~~Simuler un autre métier sans le porter : derniers attributs relevés par métier et attributs prévus de la page Équipement~~ fait en 0.554 | S |  |
| C4 | ~~Seuils d'attributs d'une séquence : Habileté, Contrôle et PS minimum~~ fait en 0.553 | S |  |
| C9 | ~~R-02, partie anticipée : « possible maintenant ? » dans l'aide à la pêche~~ fait en 0.568 | S |  |
| ~~C12~~ fait en 0.669 | Qualité initiale calculée à partir des ingrédients HQ choisis | S | À comparer à la qualité du carnet. |
| C14 | ~~A-12, partie anticipée : fiche de recette complète (livre de maître, synthèse rapide, HQ, prérequis, écart chiffré)~~ fait en 0.600 | S |  |
| C15 | ~~Simulateur plus direct : choix de la recette dans la page, résultat recalculé tout seul, glisser-déposer~~ fait en 0.572 | S |  |
| C17 | ~~Options d'export des macros : attentes séparées (X-12), son, écho final, icône~~ fait en 0.573 | S |  |
| C19 | ~~Où sont mes matériaux : détail par rangement dans la préparation et la chaîne~~ fait en 0.559 | S |  |
| C24 | ~~Besoin restant de la préparation dans les aides de récolte et de pêche~~ fait en 0.560 | S |  |
| C31 | ~~Météo de plusieurs zones en un tableau (zones favorites)~~ fait en 0.563 | S |  |
| C32 | ~~Commandes « /tensho recolte » et « /tensho recette » (suite de I16)~~ fait en 0.555 | S |  |
| C35 | ~~Aide à la pêche : lectures du jeu et premières tables hors de Draw~~ fait en 0.604 | S |  |
| C36 | ~~R-14, partie artisanat et récolte : une aide illisible le dit au lieu de disparaître~~ fait en 0.601 | S |  |
| T3 | ~~Quêtes : colonnes Niveau et Zone, tri et filtre de niveau~~ fait en 0.550 | S |  |
| T4 | ~~Monstres : drapeau sur la position du monstre, pas au centre de la carte~~ fait en 0.551 | S |  |
| T16 | ~~Tâches de servants : une ligne par servant, avec alarme~~ fait en 0.570 | S |  |
| T17 | ~~Rappels cochés d'eux-mêmes quand le jeu le dit~~ fait en 0.558 | S |  |
| T18 | ~~Discord : heure lue dans le fuseau de chaque membre, et balises [zone], [objet], [et]~~ fait en 0.564 | S |  |
| T19 | ~~Alarmes : modifier le nom et l'heure, et « Me rappeler dans 5 min »~~ fait en 0.557 | S |  |
| T20 | ~~Alarmes : sonner aussi à l'heure exacte~~ fait en 0.556 | S |  |
| T23 | ~~Compagnie libre : vaisseaux groupés par compagnie, ancien relevé effaçable, valeur du dernier butin~~ fait en 0.603 | S |  |
| T28 | ~~Collections : « Obtenus récemment » et menu des sources pour les emotes~~ fait en 0.602 | S |  |
| T29 | ~~Mini Cactpot : table des gains prouvée et visible~~ fait en 0.571 | S |  |

C12 passe en phase 5 : sa règle de calcul doit être comparée à la qualité initiale lue en jeu.

## Phase 3 : propositions moyennes

| N° | Proposition | Taille | Précision |
|---|---|---|---|
| P10 | ~~Recherche sur la carte (lieux, éthérites, PNJ, quêtes, ressources)~~ fait en 0.597 | M | Depuis la section Carte du plugin, pas depuis la carte. |
| P12 | ~~Noms écrits à côté des icônes, comme la carte du jeu~~ fait en 0.596 | M |  |
| I1 | ~~Signaler et effacer le relevé d'un servant renvoyé ou d'un ancien rangement~~ fait en 0.576 | S-M |  |
| I3 | ~~Prix des marchands PNJ dans le coût de préparation et sur la page Marché~~ fait en 0.575 | S-M |  |
| I11 | ~~Vue en liste triable de la Banque~~ fait en 0.594 | S-M |  |
| I21 | ~~Filtre de job et de set d'équipement dans les règles de tri~~ fait en 0.595 | M |  |
| C5 | ~~Suivre sa séquence pendant la synthèse : action suivante et écart avec le plan~~ fait en 0.589 | M | Information seulement, aucune action jouée. |
| C7 | ~~R-13, partie anticipée : points à horaire ouverts maintenant ou bientôt~~ fait en 0.588 | S-M |  |
| C16 | ~~Bibliothèque des séquences : recherche, renommer, dupliquer, copier et coller en texte (A-08, partie texte)~~ fait en 0.590 | S-M |  |
| C18 | ~~Chaîne de fabrication : boutons par étape, étapes identiques regroupées, cases « fait »~~ fait en 0.591 | S-M |  |
| C21 | ~~« Que puis-je fabriquer ? » avec mes stocks~~ fait en 0.592 | M |  |
| T0 | ~~Alarmes en heure réelle : à la main, chaque jour, chaque semaine ou à chaque remise à zéro (X-10, partie anticipée)~~ fait en 0.577 | S-M |  |
| T10 | ~~Accueil : bloc « Aujourd'hui » et tuiles au choix~~ fait en 0.599 | M |  |
| T11 | ~~Quêtes « disponibles maintenant » et quêtes de la zone actuelle~~ fait en 0.582 | M |  |
| T12 | ~~Chemin vers une quête : prérequis restants, nombre et prochaine à faire~~ fait en 0.583 | M |  |
| T14 | ~~Équipement : préparer le plan d'un autre job que celui porté~~ fait en 0.580 | M |  |
| T15 | ~~Revue de mode : tenue de la semaine choisie, essayée en un clic, avec l'endroit de chaque pièce~~ fait en 0.581 | M | Sur clic seulement. |
| T24 | ~~Discord : un salon par catégorie (plusieurs webhooks)~~ fait en 0.598 | M |  |
| T25 | ~~Équipement : plusieurs plans nommés par job~~ fait en 0.593 | S-M |  |

## Phase 4 : idées, sonde web et sources sans le jeu

| Point | Note |
|---|---|
| ~~I4, prix Universalis par lots~~ fait en 0.608 | Sonde faite, sans le jeu |
| ~~N-12, conditions des rangs S~~ fait en 0.670 sans compteurs (à vérifier en jeu ; compteurs : sonde d'abord) ; gardé : le principe de Tensho est de tout regrouper. S Rank Assistant (sans licence) en lecture seule, Hunt Helper (MIT) n'a pas les conditions. Conditions relevées dans plusieurs sources, réécrites et créditées ; compteurs à essayer en jeu |
| ~~Alerte du plafond hebdomadaire des mémoquartz~~ fait en 0.606 | |
| ~~« Où est cet objet ? » sur tous les personnages~~ fait en 0.609 | Version compacte sur la fiche objet, sans changer les stocks |
| ~~Liste des objets annotés, filtre par étiquette~~ fait en 0.607 | |
| ~~Écrire le fichier d'un personnage seulement s'il change~~ fait en 0.605 | Technique |

## Avant la phase 5 : coupure en JcJ et en contenu classé

Tensho se coupe tout seul et repart tout seul, sans réglage (DEC-038). ~~À coder~~ fait en 0.610 : [plan, écarts et essais en jeu](COMPETITIVE-OFF-PLAN.md). Essai en jeu : premier point de la phase 5.

## Phase 5 : relevés en jeu, puis code

Une donnée n'est pas prouvée : le relevé précède le code. Ordre : par activité, pour ne pas changer sans cesse d'endroit dans le jeu.

### Corrections

| Où | Ce qu'il faut relever | Issue |
|---|---|---|
| Jardin | Un entretien et une récolte, pour reconnaître leurs messages ; puis retirer les lignes de journal de capture | #36 |
| Récolte | ~~La régénération de PR pendant une récolte~~ relevée le 2026-11-06 : aucune hausse dans la fenêtre, +8 toutes les 3 s dehors, rien à coder ([preuve](../evidence/0.650-gp-while-gathering.md)) | #37 |

### Récolte et pêche

| N° | Proposition | Taille | Précision |
|---|---|---|---|
| C33 | Une seule séance de relevés en jeu pour l'artisanat et la récolte, avant de coder | S-M |  |
| ~~C27~~ fait en 0.647 | Classe du point (normal, éphémère, légendaire…) et livre de folklore requis | S |  |
| ~~C28~~ fait en 0.663 | Durée de l'intuition et compte à rebours | S | Unité à confirmer sur une vraie intuition. |
| ~~C29~~ fait en 0.649 | R-15, partie anticipée : poissons au harpon dans la page Récolte | S-M |  |
| C30 | Relevé local de vos prises et temps de touche personnels | M |  |

### Artisanat

| N° | Proposition | Taille | Précision |
|---|---|---|---|
| ~~C6~~ fait en 0.666 pour la simulation | Recettes à niveau ajusté, à qualité imposée et Exploration cosmique | M-L | À comparer en jeu ; seuils de mission affichés, pas encore cibles de la recherche. |
| ~~C13~~ fait en 0.668 | Chance HQ d'une qualité finale | S | Table à comparer à la synthèse. |
| ~~C22~~ fait en 0.664 | Projets de l'atelier de compagnie libre : matériaux par pièce et par phase | M | À comparer à la fenêtre de l'atelier. |

### Carte

| N° | Proposition | Taille | Précision |
|---|---|---|---|
| ~~P6~~ fait en 0.644 | Coordonnées du personnage toujours visibles et distances | S | Coordonnées du drapeau sous son icône de bord de carte, pas dans un coin ; avec réglage. |
| ~~P7~~ fait en 0.645 | Éthérites non harmonisées signalées et coût au survol | S |  |
| P11 | Parties non découvertes masquées comme sur la carte du jeu | M | Fait en 0.803 : `d.tex` est une grille de 4×3 cases, chacune la carte entière au 1/16, une région par canal ; parchemin sur les régions non découvertes, réglage « Parties non découvertes vierges » ; `docs/evidence/0.803-map-discovery-layout.md`. Avant : `docs/evidence/0.766-map-discovery.md` |
| ~~P17~~ fait en 0.652 | Relevé du type de marqueur des mandats et de leurs zones | S | Le type ne dit rien : c'est l'icône 60492 ou le nom du mandat. |
| ~~P22~~ fait en 0.665 pour les variantes 1 et 5 | Noms des variantes d'icônes de quête (71141 / 71151) | S | Les autres gardent leur numéro ; à confirmer en jeu. |

### Objets

| N° | Proposition | Taille | Précision |
|---|---|---|---|
| I10 | Cristaux et gils des servants dans le relevé du servant | M | Fait en 0.767 : relevé du 2026-11-07 confirmé par l'utilisateur (0 gil, 0 cristal) ; `docs/evidence/0.767-retainer-gil-crystals.md` |

### Outils

| N° | Proposition | Taille | Précision |
|---|---|---|---|
| T5 | Une séance de relevés en jeu pour ces outils, avant de coder | S |  |
| ~~T13~~ fait en 0.648 pour les contenus que les données nomment | Contenus et quêtes : « Débloqué par » et « Débloque » | M | Environ 130 contenus sur plus de 700 ; à compléter si une source sûre est trouvée. |
| ~~T21~~ fait en 0.646 | Personnage : niveaux et expérience de tous les jobs | S |  |
| T26 | Dirigeables : trajet du dernier voyage reconstitué | S | 2026-11-07 : l'aéronef de l'utilisateur est en construction, à relever à son premier retour. 2026-11-08 18:50 : premier retour relevé (« Reading airship » : butin par escale, 23 lignes du journal de bord avec les noms des secteurs, heure de retour ; `docs/evidence/0.919-airship-reading-2026-11-08.log`) : lecteur du trajet fait en 0.919 (« Dernier voyage » sur l'onglet des navires, compagnie à un seul aéronef) ; à vérifier en jeu au prochain retour |
| ~~T27~~ fait en 0.651 | Actions de compagnie actives : heure de fin et alarme | M |  |

### Gros chantiers et idée

| Point | D'abord | Précision |
|---|---|---|
| Gils des servants dans la vue des personnages | Sonde à la cloche | Fait en 0.767 avec I10 : « (+N chez les servants) » dans Personnages |
| I-05, suite des ventes des servants | Essayer la 0.508 | Validé en jeu le 2026-11-07 sur capture de l'utilisateur : 4 ventes, prix, quantités et comparaison justes |
| N-04, loterie immobilière | Sonde : structure du jeu à trouver | |
| N-07, ressources de l'île | Vérifier l'unité des positions | Carte faite en 0.667, liste des matériaux en 0.673 ; vos stocks de l'île ne sont pas lus |
| N-10, atelier de l'île | Fait sans le jeu en 0.671 et suivantes (lecture, table) ; à vérifier en jeu : `docs/evidence/0.671-island-workshop.md` | Demandé par l'utilisateur le 2026-11-06 ; montée de l'entrain comptée en 0.674 ; prévision de la semaine plus tard |
| ~~Panneau du Dédale~~ abandonné (DEC-038) ; suite du dresseur | La page reste une liste d'informations, hors du Dédale | |

## Phase 6 : tests en jeu

| N° | Proposition | Taille | Précision |
|---|---|---|---|
| P19 | Tests en jeu de la carte encore en attente | S |  |

P19 s'élargit en campagne guidée de fin : tout ce qui a été codé, avec les cases ouvertes de [TESTING](TESTING.md), selon le déroulement ci-dessus.

## Phase 7 : juste avant la 1.0.0

Demande de l'utilisateur (2026-11-05) : tout remettre à jour une fois le plan codé, dans un ordre logique, pour ne pas tout refaire. Chaque étape s'appuie sur la précédente :

1. Revoir tous les crédits : ils dépendent de ce qui a été ajouté au code.
2. La liste de tous les outils, sur l'accueil et partout où elle apparaît, en anglais et en français.
3. L'aide et la prise en main du plugin : elles décrivent les outils.
4. Tous les fichiers `.md` du dépôt, à jour puis en français et en anglais : l'anglais toujours en premier, avec une mention en français disant que la traduction est en bas.
5. Sur Git, tout en anglais si possible : messages de commit, issues, releases et ce que GitHub affiche.

**Revue du plan (2026-11-08, consigne de nuit X-74), pour coller au code de la 0.865 :**
- L'étape 7 passe avant la phase 6 (essais en jeu), à la demande de l'utilisateur ; ce qui changera aux essais retouchera seulement les textes concernés.
- 1. Crédits : `Credits/CreditCatalog.cs` contre les sources réellement appelées (dossiers `Community/`, `Market/`, `Monsters/`, `Fashion/`, `Maps/`, fichiers embarqués) ; la base de découvertes (X-43, X-72 à X-74) n'est pas une source extérieure, elle se décrit dans l'aide, pas dans les crédits. **0.866 : chaque hôte appelé est crédité ; 14 références lues pour des idées (citées dans le code) manquaient : ajoutées, vérifiées à la source (docs/evidence/0.866-credit-references.md).**
- 2. Liste des outils : l'accueil (`MainWindow.Pages.cs`), le README et partout où une liste apparaît, avec les outils ajoutés depuis la 0.539 (Découvertes, panneaux des listes, alarmes des listes, etc.), en anglais et en français (fichiers de langue).
- 3. Aide et prise en main : `Guide/GuideFlow.cs` et `MainWindow.Guide.cs`, à relire contre la liste de l'étape 2. **0.866 : l'aide avait une entrée par page ; textes dépassés corrigés (Découvertes, rangements, artisanat, compagnie, carte, île, livraisons, rappels), une question « Que garde Tensho sur mon PC ? », deux arrêts de plus au guide (Découvertes, pêche océanique).**
- 4. Fichiers `.md` suivis par Git (30 hors preuves) : anglais d'abord, puis la mention en français et la traduction en bas. Exceptions gardées : `CHANGELOG.md` (chaque entrée a déjà sa partie anglaise), `docs/evidence/` (preuves techniques, en anglais, citées par le code), les fichiers de l'agent hors Git (`docs/ai/`, `docs/goals/`, `CLAUDE.md`). **0.866 : README (toutes les pages, les deux moitiés) et les 14 fichiers publics faits ; ARCHITECTURE et ROADMAP allégés de leurs notes d'archive (restent dans Git) ; les documents vivants attendent la décision ci-dessous.**
- 5. Git : messages de commit déjà en anglais (partie « English » du CHANGELOG) ; reste à vérifier issues, jalons, étiquettes et description du dépôt. **2026-11-08 : tickets déjà en anglais ; #31 (soumission au dépôt officiel) fermé « non prévu » (DEC-039) ; jalon renommé « 1.0.0 — Custom repository release » ; #42 ouvert pour les découvertes (X-73, X-74). La description du dépôt (« TenshoAssistant ») est un réglage GitHub : à changer seulement avec l'accord de l'utilisateur.** **2026-11-08 (accord de l'utilisateur) : description du dépôt rédigée en anglais, adresse du dépôt Dalamud en page d'accueil, sujets ajoutés, protection des secrets à l'envoi activée, branches fusionnées supprimées ; #39 et #40 rangés au jalon 2 avec « in-game-test ».**
- **Précision de l'utilisateur (2026-11-08) :** les documents `.md` ne sont pas seulement traduits : chacun est revérifié contre le code actuel, juste après le nettoyage X-76.
- **Décision de l'utilisateur (2026-11-08, « Oui, à la fin ») :** les documents de travail vivants de `docs/project` (GLOBAL-PLAN, TESTING, BACKLOG, USER-REQUESTS, DECISIONS, GAPS, REFERENCES, les *-PROPOSALS, PROPOSALS-TRIAGE, COMPETITIVE-OFF-PLAN) changent à chaque version ; les traduire maintenant obligerait à tenir deux copies. Défaut recommandé : les traduire en dernier, juste avant la 1.0.0, une fois la phase 6 finie ; les fichiers publics sont faits maintenant. **Choisi : traduits juste avant la 1.0.0, une seule fois.**

## Nettoyage complet avant la 1.0.0 (X-76, plan détaillé du 2026-11-08) — approuvé par l'utilisateur (« Oui, ce plan »)

Demande de l'utilisateur : « un nettoyage qui te semblerait plus complet et cohérent avant une version 1.0.0 ». Ordre : après les essais en jeu nécessaires,
avant les documents `.md` (revérifiés contre le code puis traduits). Ultracode accordé. Règle de toute l'étape : **rien n'est retiré sans preuve qu'il est inutile**,
une famille de changements par version, `Verify` vert à chaque fois, sauvegarde zip dans `checkpoints/` avant de commencer.

**Avancement :** étape 0 faite (0.904 : `docs/evidence/x76-0-baseline.md`, sauvegarde `checkpoints/backup-20261108-1450-x76-start.zip`) ; étape 1, première passe faite (0.904 : `docs/evidence/0.904-dead-code.md`, 91 éléments retirés) ; restent dans l'étape 1 les parties de la capture déjà prouvées (après les relevés de voyage et d'extraction). Étape 2 : 37 constats (`docs/evidence/x76-2-duplicates.md`), traités par familles ; 0.905 : coordonnées de carte ; 0.906 : heures d'Eorzéa et durées ; 0.907 : fenêtres du jeu et requêtes web ; 0.908 : découvertes (« vu en jeu ») ; 0.909 : français retiré du code, commentaires remis en place. Titre français « Tensho Assistant · Monnaies » de la barre d'infos du serveur : renommé « Tensho Assistant (Currencies) » en 0.910 avec l'accord de l'utilisateur (« effacer les réglages n'est pas grave, il n'y a que moi qui utilise le plugin »).

**0. Point de départ mesuré.** Chiffres relevés et gardés dans `docs/evidence/` pour comparer à la fin : fichiers et lignes par dossier, tests, clés de langue
(en, fr, de, ja), réglages, hôtes réseau appelés, parties de la capture, types natifs lus, avertissements désactivés.

**1. Code mort et restes.**
- Compilation d'essai avec les analyseurs « membre jamais utilisé » activés (sans les garder) : types, méthodes, champs, propriétés jamais lus.
- Clés de langue jamais demandées par le code (un script compare les fichiers de langue au code ; les clés construites par nom sont listées à part).
- Ressources embarquées, fichiers de données, outils et scripts que plus rien n'utilise.
- Parties de la capture du mode développeur déjà prouvées (leves, retainer, synthesis…) et journaux de débogage d'enquêtes closes.
- Réglages toujours vrais ou jamais lus ; anciennes branches de migration de configuration trop vieilles (la lecture des anciennes valeurs reste).
- Contre-vérification par un second agent qui cherche à prouver que chaque élément sert encore (réflexion, nom construit, test, sauvegarde, commande).

**2. Doublons et cohérence du code.**
- Logiques écrites plusieurs fois (formatage des lieux et heures, caches « par révision », lectures de fenêtres du jeu, clients HTTP, menus de lieu) : une seule version partagée quand le risque est faible.
- Noms et termes alignés (« découverte », « vu par vous », « importé », « source communautaire ») dans le code et les textes.
- Commentaires dépassés corrigés ; aucun français dans le code (règle des langues).
- Fichiers trop longs (Plugin.cs, gros fichiers MainWindow) : découpés seulement là où le découpage est mécanique et couvert par les tests.

**2b. Harmonisation du code** (ajout de l'utilisateur, 2026-11-08 : « que tout soit pareil où ça a besoin de l'être, par exemple info-bulles ou autre »).
- Une seule façon de faire chaque geste d'interface : info-bulles (aide « ? », survol, réglages), cases à cocher et réglages avec défaut, en-têtes et
  sections, tableaux, boutons, couleurs et icônes du thème, messages d'erreur et d'état, journaux (niveaux, anglais, forme des messages).
- Repérage par agents (ultracode), puis remplacement des écritures à la main par les composants communs, famille par famille, textes inchangés.

**2c. Couverture des parseurs** (ajout de l'utilisateur, 2026-11-08 : « vérifier les parseurs, si on n'oublie pas de données, qu'on couvre tous nos besoins
et plus, en prévision de futurs outils »).
- Pour chaque parseur et lecteur du jeu : ce qu'il lit, ce qu'il pourrait lire de plus au même moment (champs ignorés), ce qui manque pour remplacer une
  source communautaire encore lue, et ce que les outils prévus du plan auront besoin de garder dès maintenant.
- Les ajouts sûrs sont codés ; ceux qui demandent un relevé en jeu vont dans la table des rappels.

**Ordre rappelé par l'utilisateur (2026-11-08, soir) :** « uniformisation en premier, comme ça on l'applique à tout nouveau parseur » : 2b H7–H10 puis 2c P4–P10 et E2–E5 passent avant la suite de X-85 (V2 points à V6), X-86 à X-89 et tout nouveau parseur.

**Avancement 2b et 2c :** plan rendu par un workflow (`docs/evidence/x76-2bc-plan.md`) : harmonisation H1 info-bulles (0.913), H2 couleurs du thème (thèmes clairs, 0.912),
H3 enregistrement des réglages et retour arrière, H4 curseurs enregistrés au relâchement (0.914), H5 composants de réglage, H6 ligne « Par défaut » (0.915), H7 états des pages (0.920),
H8 affichage des objets (0.921), H9 journaux (0.922), H10 fichiers et abonnements (0.923–0.925 ; DEC-038 : lecture des offres du marché coupée en JcJ en 0.926, accord de l'utilisateur) ; parseurs P1 HQ des remises, P2 attribution des gains, P3 libellé de chaque sorte (0.911),
P4 état « débloqué » des objets (0.927), P5 feuilles du jeu à la place de données communautaires (0.928–0.932), P6 Intuition et prédateurs (0.933, sauf le leurre nageur : relevé en jeu), P7 secteurs des sous-marins (0.934 : HQ, visites ; sens des lignes Survey/Yield/Discovered à relever en jeu),
P8 compteurs pour des taux (0.935), P9 données lues mais jetées, P10 récompenses de fin de mission ; étapes ajoutées E1 format du fichier des découvertes figé (0.916, DEC-042)
avant la 1.0.0, E2 test des règles d'interface (couvert par les tests 912 à 922), E3 test des preuves citées (0.937), E4 aide « une fois par événement » (0.937), E5 tenue face à une grosse masse de données (X-93 : essai de charge et index des recherches en 0.938, docs/evidence/0.938-discoveries-load.md ; restent le plafond de 200 000 faits et la compression). Vérification des données déjà relevées
(demande de l'utilisateur) : sonde `tools/DiscoveryCheckProbe`, 1281 faits, 11 faux venant de 3 défauts corrigés en 0.911
(`docs/evidence/0.911-discoveries-check.md`). Décision pour le lot de fin : la lecture des offres du marché pendant une mise en vente
(UndercutWatcher) ne s'arrête pas en JcJ (DEC-038), résumé à faire à l'utilisateur avant d'y toucher.

**3. Réglages : inventaire complet et remise en ordre.**
- Tableau de tous les réglages (`docs/project/SETTINGS.md`) : nom, valeur par défaut, où il est lu, page, info-bulle, effet réel, ce qui se passe s'il est coupé.
- Corrections : réglage sans effet, effet différent de son nom, défaut incohérent, doublon entre pages.
- Réorganisation des pages de réglages par thème si l'inventaire le justifie (décision groupée soumise à l'utilisateur, ancienne valeur toujours lue).
- Réglages manquants là où un comportement est imposé sans choix.
- Demandes déjà notées : un réglage unique « données du jeu et observées seulement » (coupe d'un coup toutes les sources communautaires) ; vider le cache et les découvertes avec une sauvegarde zip juste avant.

**4. Info-bulles et textes.**
- Chaque info-bulle relue contre le code, celles des réglages d'abord, avec une ligne « Par défaut : … » juste.
- Parité anglais et français ; allemand et japonais : signaler seulement ce qui manque.
- Textes de l'aide et du guide alignés sur l'état final des pages.

**5. Réseau, vie privée et règles Dalamud.**
- Liste de chaque hôte appelé : quand (clic, automatique choisi par l'utilisateur), limite de taille, délai, en-tête, coupure en JcJ, réglage qui le coupe.
- Contrôle de la liste des règles Dalamud (`docs/project/PUBLISHING.md`) point par point ; tout écart résumé à l'utilisateur avant de corriger.
- Données gardées sur le disque : liste, taille maximale, purge, ce que « vider » efface.

**6. Robustesse et performance.**
- Chaque lecture native : thread du framework, pointeur vérifié, copie, coupure en JcJ, résistance aux mises à jour du jeu (R-14).
- Chaque abonnement, minuterie et tâche : propriétaire et nettoyage (test existant étendu si besoin).
- Draw : boucles et allocations par image, caches recalculés trop souvent ; le relevé « Slow frame » sert de juge.

**7. Tests.**
- Tests qui figent un ancien texte exact au lieu d'un comportement : réécrits sur le comportement.
- Tests manquants pour ce que le nettoyage touche (réglages, migrations, nouveau réglage global).
- Tests morts ou en double retirés.

**8. Dépôt.**
- `.gitignore`, scripts, `tools/README.md`, flux CodeQL et compilation, alertes ouvertes, étiquettes, jalons et tickets fermés sur preuve.
- `community-discoveries` et `community-help` contrôlés (règle de validation des découvertes).

**9. Fin de l'étape.**
- Comparaison avec le point de départ (preuve dans `docs/evidence/`).
- Un passage en jeu rapide de chaque page (liste guidée, un point à la fois), avant les documents puis la 1.0.0.

## Sujets ajoutés le 2026-11-06

Choisis par l'utilisateur à partir de la liste de rubriques d'un site français. Aucune donnée de ce site : voir [REFERENCES](REFERENCES.md). Tout vient des feuilles et des structures du jeu.

| Point | Sujet | État |
|---|---|---|
| W-01 | Page Commandes spéciales : clients, rang, points, semaine | Fait sans le jeu en 0.675 ; à comparer en jeu : `docs/evidence/0.675-custom-deliveries.md`. Récompenses par rang en 0.730 : `docs/evidence/0.730-delivery-rewards.md` |
| W-02 | Grande Mascarade : épreuves faites et restantes, sans conseil de combat | Fait sans le jeu en 0.683 ; à comparer en jeu : `docs/evidence/0.683-masked-carnivale.md`. Défis hebdomadaires et récompenses plus tard |
| W-03 | Armes reliques : progression par extension | Fait sans le jeu : zodiaque et livre en cours en 0.708 (`docs/evidence/0.708-relic-weapons.md`), extensions suivantes avec W-13 |
| W-04 | Animaux de la Félicité insulaire | Demandé par l'utilisateur le 2026-11-06. Fait sans le jeu en 0.685 : onglet Pâturage, liste du wiki sur clic ; à comparer en jeu : `docs/evidence/0.685-island-animals.md`. Animaux présents maintenant et sur la carte plus tard |
| W-05 | Panneaux à l'écran : matériaux du projet d'atelier et liste de préparation, réglables | Fait sans le jeu en 0.691 |
| W-06 | « Que puis-je fabriquer ? » : trier par le plus rentable en gils, assignats ou autre | Fait sans le jeu en 0.707 |
| W-07 | « Que puis-je fabriquer ? » : compter les sous-recettes | Fait sans le jeu en 0.696 |
| W-08 | Mandats : clic droit vers l'éthérite la plus proche | Fait sans le jeu en 0.698 |
| W-09 | Liste de suivi par provenance (récolte, PNJ, monstre, autre), atelier et artisanat | Fait sans le jeu en 0.699 |
| W-10 | Peuples alliés : rang et quêtes de chaque peuple | Fait sans le jeu en 0.713 : rang, réputation, lieu, monnaie, quêtes du jour ; à comparer en jeu : `docs/evidence/0.713-allied-societies.md` |
| W-11 | Grande compagnie : rang, sceaux et missions sur une page | Fait sans le jeu en 0.714 : grades, sceaux, promotion, boutique ; missions du jour non lues : `docs/evidence/0.714-grand-company.md` |
| W-12 | Livraisons spéciales : Cristarium, Académie de Sharlayan, Bazar aux Mille Perles | Fait sans le jeu en 0.715 : `docs/evidence/0.715-special-deliveries.md` |
| W-13 | Reliques suivantes : Anima, Eurêka, Résistance, Manderville, fantômes, et leurs armures | Fait sans le jeu : armes en 0.716 (`docs/evidence/0.716-relic-families.md`), armures en 0.720 (`docs/evidence/0.720-relic-armour.md`) |
| W-14 | Eureka et Bozja : ALEA, escarmouches, affrontements cruciaux, rapports du front, actions oubliées, sans conseil de combat | Fait sans le jeu en 0.717 : `docs/evidence/0.717-eureka-bozja.md` ; ce que le personnage possède n'est pas lu |
| W-15 | Donjons profonds : Palais des morts, Pilier des cieux, Eureka Orthos, Sanctuaire des pèlerins, étages et butin, sans conseil de combat | Fait sans le jeu en 0.718 : `docs/evidence/0.718-deep-dungeons.md` |
| W-16 | Lexique des mots du jeu, français et anglais, à partir de ses feuilles | Fait sans le jeu en 0.719 : `docs/evidence/0.719-lexicon.md` |
| W-14b | ALEA de toutes les zones ouvertes, lieu et niveau, d'après les fichiers de zone | Trouvé en faisant W-14 ; fait sans le jeu en 0.721 : `docs/evidence/0.721-fates.md` |

## Demandes du 2026-11-07, en jouant

Notées dans l'ordre où l'utilisateur les a faites ; à faire après la tâche en cours, une version à la fois, sans en oublier. Une ligne passe à « fait » avec sa version.

| Point | Demande | État |
|---|---|---|
| X-01 | Lourdeur : trouver ce qui bloque une image de Tensho (120 à 230 ms toutes les quelques minutes, 2,6 s une fois) et le sortir du dessin | En cours : relevé « Slow frame » en 0.745 ; aide à la récolte (97 ms) calculée hors de l'image en 0.747, avec la mesure de la fenêtre principale et des autres fenêtres ; les images de 110 à 160 ms venaient du passage des pages (mode développeur), espacé de deux heures en 0.748 ; pause de 5 s du moteur .NET au rechargement (mode développeur) ; panneau de l'atelier à 1,7–7,6 s depuis les zones (0.757–0.758, ma régression) : regroupement calculé une fois par seconde en 0.760 ; carte : « map overlay layout » 100 à 220 ms une fois à l'ouverture, à mesurer plus finement plus tard ; le 2026-11-07 la carte revient à 110–160 ms plusieurs fois par heure, pas seulement à l'ouverture : 0.791 la mesure par morceaux ; 18:41 et 18:46 : carte 195 ms (canevas 166–168 ms) et panneau d'atelier 93–97 ms, mais la ligne ne gardait que 4 morceaux, les enveloppes : 0.801 en garde 10 ; 18:50 à 20:01 : canevas 102–169 ms, dont « canvas icons » 34–71 ms et 40–60 ms hors des points de mesure : 0.805 ajoute boutiques, liste des icônes, dessin, objets proches, monstres, menus, éthérite la plus proche, fin ; 20:08:14 (0.805) : canevas 93 ms, boutiques 16, info-bulles 11, menus 11… mais la ligne coupait encore à 10 morceaux : 0.806 garde tous ceux d'1 ms et plus |
| X-02 | Données gardées plus longtemps, relues seulement si absentes ; réglages qui complètent « Cache & péremption » | Durées par défaut plus longues et « Jamais » renommé « Seulement si absent, ou sur Actualiser » en 0.750. Accord donné. Relu : quêtes et collections ont déjà une copie par personnage sur disque ; les carnets, recettes et équipement se relisent légèrement, page ouverte. À faire : durées par défaut plus longues, « Jamais » renommé « seulement si absent ou sur Actualiser », et ce que X-01 trouvera de lourd |
| X-03 | Lien « Ouvrir dans Tensho » d'une alarme : n'ouvre rien | Aucun clic n'arrivait (journal de 0.743). Dalamud ne trouve plus le lien après un rechargement du plugin : réenregistré au chargement en 0.759 ; toujours aucun clic à 13:37, plugins tous désactivés sauf Tensho : couleur placée autour du lien et non dedans en 0.771 ; toujours rien à 13:49 : 0.773 écrit au journal si Dalamud a le lien enregistré ; console verbose (14:09) : lien enregistré mais le jeu ne le transmet pas, il ouvre une bulle d'objet ; 0.774 : vrai lien d'objet du jeu, clic droit → menu Tensho ; validé en jeu le 2026-11-07 14:29 sur capture : clic droit sur l'objet de l'alarme, menu « Tensho — objet » complet ; 14:47 : clic droit sans effet sur l'indication grise, hors du lien : 0.780 met l'indication dans le lien ; validé en jeu le 2026-11-07 (« clic droit ok ») ; 2026-11-07 21:01, sur capture (alarme Abricot) : le clic droit ne fonctionne toujours pas : rouvert, à reprendre après X-45 ; 0.812 : le lien de 0.780 (texte changé) n'est plus pris pour un objet, aucun menu ouvert à 21:01 ; retour au lien du jeu de 0.774, l'indication à côté, `docs/evidence/0.812-alarm-link.md` ; 2026-11-07 22:34 avec 0.812 : alarme écrite avec le lien d'objet du jeu, clic droit sans aucun menu ouvert ; essai demandé : clic droit sur un objet lié dans un message normal du jeu ; 2026-11-07 23:06, après 0.812 : toujours aucun menu au clic droit sur le lien de l'objet (morille, petite icône orange) : à reprendre ; 0.836 : dalamud.log de 23:06 : la ligne s'imprime, puis aucun événement de menu, pas même avant les filtres de Tensho : le jeu n'ouvre rien. Commande de développeur « /tensho linktest » : quatre formes du lien (lien d'objet, lien Tensho ; type par défaut ou Écho), chaque clic noté au journal, pour choisir la forme qui marche ; 0.837 : essai du 23:35 : A et B ouvrent le menu du chat (objet survolé 12898) mais Tensho le prenait pour un menu de personnage (nom présent) ; C et D arrivent bien à Tensho. Le menu du chat sur un lien d'objet reçoit maintenant l'entrée Tensho (objet du lien du jeu = objet survolé) ; VALIDATED_IN_GAME (« AB OK pour tensho », 2026-11-07 : entrée Tensho au clic droit sur un lien d'objet du chat) |
| X-04 | Au point de récolte, mettre en évidence les objets dont les listes de suivi (artisanat et atelier de compagnie) ont besoin | Fait en 0.751 : « Vos listes en demandent encore N » dans l'aide, cadre vert sur la ligne de la fenêtre du jeu (réglable) ; validé en jeu le 2026-11-07 sur capture de l'utilisateur |
| X-05 | Listes de suivi d'artisanat et d'atelier rassemblées par zone de récolte | Fait en 0.752 : sous « Récolte », une ligne par zone, celle qui a le plus d'objets d'abord (réglable) ; validé en jeu le 2026-11-07 sur capture de l'utilisateur |
| X-06 | « Récolter automatiquement » coché dans la fenêtre du jeu : le plan ne garde que les actions faites avant la première récolte, puis plus rien, et maximise la récolte restante | Fait en 0.753 : case lue dans la fenêtre du jeu ; effets durables gardés, une action « pour une récolte » une fois au plus, aucun essai rendu |
| X-07 | « Que puis-je fabriquer ? » : un objet faisable par deux métiers compté une fois | Fait en 0.739 |
| X-08 | Infobulle d'un point à horaire (carte) : « fermé maintenant » sans dire dans combien de temps il ouvre ; ajouter le compte à rebours | Fait en 0.754 : « ouvre dans », ou « ferme dans » quand il est ouvert |
| X-09 | Listes de suivi d'artisanat et d'atelier : un bouton qui ajoute les alarmes de tous leurs points à horaire, sans doubler une alarme qui existe déjà | Fait en 0.755 : « Ajouter les alarmes (N) » en tête de chaque liste, pour ce qui manque encore ; validé en jeu le 2026-11-07 (« fonctionne il semble ») |
| X-10 | Aide à la récolte : se placer d'abord sur un objet dont les listes de suivi ont besoin | Fait en 0.756 : à l'ouverture d'un point, l'objet dont les listes ont le plus besoin |
| X-11 | Listes de suivi : pouvoir replier chaque zone de récolte | Fait en 0.757 |
| X-12 | Listes de suivi par zone : la zone où l'on se trouve en premier, puis les autres triées en tenant compte du coût de téléportation vers chacune | Fait en 0.757 : « vous y êtes » d'abord, puis la téléportation la moins chère (prix du jeu depuis votre position), prix inconnu ensuite |
| X-13 | Listes de suivi : les objets des points à horaire (alarmes) dans leur propre groupe repliable, avec un nom approprié, au lieu du nom de zone | Fait en 0.758 : groupe « Points à horaire » en tête, repliable |
| X-14 | Groupe « Points à horaire » des listes de suivi : dire pour chaque objet si son point est ouvert ou fermé | Fait en 0.761 : « ouvert · encore h:mm:ss » ou « fermé · ouvre dans h:mm:ss » |
| X-15 | Menu du clic droit d'un objet : « Y aller » directement (point de récolte, éthérite la plus proche) | Fait en 0.762 : point ouvert d'abord, puis zone actuelle, puis téléportation la moins chère |
| X-16 | Plan de PR : à rendement égal, préférer les actions qui durent toute la récolte, pour moins de clics | Fait en 0.763 : à rendement égal, le moins de clics, puis le moins cher |
| X-17 | Panneau de suivi réduit : l'aide perd « Vos listes en demandent encore » et le cadre ; les cadres passent au-dessus des autres fenêtres | Fait en 0.764 : lignes recalculées panneau réduit, cadres dessinés sous les fenêtres de Tensho ; 0.765 : vaut aussi pour un panneau réduit depuis le chargement |
| X-18 | Journal des gils : icônes (gils, catégories) et moins de « Autre » : trouver d'autres catégories d'après le jeu | Icônes et couleurs en 0.768 ; « Autre » : relevé au journal (« Gil other ») ; 0.775 : dépense après un écran de chargement = téléportation ; « +1 000 » sans fenêtre encore inconnu |
| X-19 | Journal des gils : un dépôt chez un servant compté comme dépense ; le total des servants ne suit qu'en rouvrant | Fait en 0.769 : échanges avec servants et coffre en transferts hors net ; hausse des servants au-delà des dépôts = ventes ; servant ouvert lu en direct |
| X-20 | Objets déposés chez un servant, dans les sacoches ou au coffre de compagnie : la copie doit suivre pendant que la fenêtre est ouverte, sans la rouvrir | Fait en 0.770 : rangement ouvert relu toutes les 2 s (au lieu de 30 s) ; fichier écrit seulement s'il change |
| X-21 | « Ajouter les alarmes » proposait 19 alarmes, dont des objets déjà complets en apparence et des cristaux qu'on trouve aussi sans horaire | Fait en 0.772 : seulement les objets qui manquent et ne poussent qu'à des points à horaire |
| X-22 | Butin des monstres, lignes du wiki « Monstre · niveau · Zone (x,y) » : pas de clic droit vers l'éthérite la plus proche | Fait en 0.777 : tous les numéros de lieu d'un nom essayés (« Mor Dhona » en a 3) ; un lieu sans carte est noté au journal en mode développeur |
| X-23 | Alarmes : l'envoi sur Discord désactivé par défaut | Fait en 0.778 : une nouvelle alarme n'envoie rien sur Discord ; une alarme enregistrée garde son choix |
| X-24 | Après un chargement du plugin, les objets de monstres passent un moment en « Autres » : la source des monstres n'est relue du disque que plus tard | Fait en 0.779 : les listes demandent elles-mêmes la source des monstres, relue de la copie sur disque |
| X-25 | **Gel du jeu, 15:18:45** : le solveur Raphael (natif) panique sur une division par zéro (quality_upper_bound_solver/state.rs:59) en ouvrant le carnet d'artisan, cuisinier, recette 5623 ; la panique arrête le jeu | Fait en 0.782 : toute valeur nulle ou trop grande refusée avant l'appel ; `docs/evidence/0.782-raphael-freeze.md` ; vérifié en jeu le 2026-11-07 15:25 sur capture : plus de gel sur cette recette ; 0.783 : « Raphael non utilisé » au lieu de « a échoué » |
| X-26 | « Comparer les prochains coups » sur une recette en haute qualité impossible : « valeurs non concordantes », aucun coup projeté (le jeu montre une qualité sur 2500, Tensho 0) | Fait en 0.784 : cet écart accepté pour une recette sans qualité à chercher |
| X-27 | Les listes de suivi doivent pouvoir n'afficher que les objets finis (les objets à fabriquer, sans leurs matériaux) ; « oublie pas atelier et artisanat » | Fait en 0.785 : réglage « Objets finis seulement » pour les listes d'artisanat, d'atelier et de préparation |
| X-28 | Atelier de compagnie : tenir compte de ce qui est déjà livré au projet ; « oublie pas de verifie si on a valide des etapes aussi » | Fait en 0.787 : lu dans la fenêtre de livraison (capture 0.786), phases validées retirées, lots livrés déduits ; `docs/evidence/0.787-workshop-delivered.md` |
| X-29 | Carnet de synthèse : en Menuisier sur une recette de Métallurgie (Lingot de mithril), Tensho ne signale pas qu'on n'est pas le bon métier et propose sa macro | Fait en 0.788 : c'était détecté (calcul avec les attributs relevés du bon métier), mais dit en couleur d'information et coupé ; maintenant « Mauvais métier : vous êtes Menuisier, cette recette est de Métallurgie », en avertissement, avec le bouton pour porter le métier |
| X-30 | Voir les étapes de fabrication qui restent (objets intermédiaires à fabriquer) pour les listes d'atelier et d'artisanat | Fait en 0.789 : section « Fabrications restantes » repliable dans chaque liste, par étape, stock déduit, métier et nombre de fabrications ; validé en jeu le 2026-11-07 (« c'est parfait ») |
| X-31 | « Fabrications restantes » : chaque étape doit pouvoir se déplier jusqu'à l'étape finale ; un seul format de chiffres, comme les autres lignes (« possédés / demandés ») | Fait en 0.790 : une branche dépliable par étape avec son compte ; lignes « possédés / demandés » ; métier et fabrications restantes au survol de l'icône ; validé en jeu le 2026-11-07 (« c'est parfait ») |
| X-32 | Carte : trouver une solution au blocage contourné en ouvrant puis fermant la carte du jeu (zones de mandats et de quêtes absentes tant que la carte du jeu n'a pas été ouverte à portée, `docs/evidence/0.654-leve-area-memory.md`) | Fait en 0.793 : la capture 0.792 montre la zone du mandat dans la liste propre du jeu quand celle de la fenêtre carte est vide ; Tensho l'ajoute, et les points du mandat commencé ; `docs/evidence/0.793-game-leve-markers.md` ; validé en jeu le 2026-11-07 (« ok pour mandats ça fonctionne ») |
| X-33 | Rangements, « Que faire avec ? » : régler les prix comme « Que puis-je fabriquer » et tenir compte des ventes par jour (Matière sombre grade I à 350 000 en tête, prix d'une vente isolée) | Fait en 0.794 : jours de vente partagés, colonnes « Ventes / jour » et « Gils en N jours » (borné par le solde), classement par gils réellement obtenus |
| X-34 | Monstres proches (table des objets du jeu), information seulement : règle Dalamud à résumer et accord de l'utilisateur avant tout code | Cadre résumé le 2026-11-07 (information seulement, coupé en JcJ, dessin sur la vue 3D déclaré) ; réponse « ça en plus » avec X-41, lue comme accord ; lecteur des monstres proches en 0.795 (ennemis vivants ciblables, 100 au plus, nom, lieu, niveau) |
| X-35 | Monstres proches : ceux qui lâchent un objet demandé par les listes de suivi (catégorie « Monstre »), sur la carte | Fait en 0.795 : patte sur la carte (réglage « Monstres de vos listes ») et à côté de l'objet dans la liste (réglage sous « Monstre ») ; données MobDrop de LuminaSupplemental |
| X-36 | Monstres proches : ceux qui manquent au carnet de chasse | Fait en 0.796 : livre sur la carte (rang actuel de chaque carnet, victoires au survol) ; réglage dans la carte et sur la page Carnet de chasse ; une seule couche « monstres proches », toutes les raisons au survol |
| X-37 | Monstres proches : cibles des mandats acceptés et des ALÉA en cours | ALÉA fait en 0.797 (étoile, nom de l'ALÉA au survol, `GameObject.FateId`) ; mandats : 0.797 capture l'événement des monstres en mode développeur, à relever pendant un mandat de combat ; ALÉA validé en jeu le 2026-11-07 sur capture (étoiles sur les monstres de l'ALÉA) |
| X-38 | Mage bleu : monstres proches qui apprennent une compétence pas encore apprise, icône mage bleu au-dessus de leur tête ; réglage pour l'avoir tout le temps, même hors mage bleu | Fait en 0.798 (carte, réglages, données du wiki lues au clic) et 0.799 (icône du mage bleu au-dessus de la tête, dans la vue du jeu ; aussi au choix pour listes, bestiaire, ALÉA ; taille, distance, combat) |
| X-39 | Dresseur : monstres proches du bestiaire du dresseur, même affichage et mêmes réglages | Fait en 0.804 : bestiaire du wiki (une page, lue au clic), icône du dresseur au-dessus de la tête, dragon sur la carte, réglages (carte, page Dresseur) ; `docs/evidence/0.804-beast-wiki.md` |
| X-40 | Monstres proches : autres usages à proposer, et des réglages cohérents à l'endroit approprié (carte, page concernée) pour chaque usage | Après X-34 |
| X-41 | Monstres proches : ceux qui lâchent un objet de collection qui manque (monture, mascotte, orchestrion), si les données de butin le disent | Fait en 0.800 : gemme sur la carte, au choix au-dessus de la tête ; réglages dans la carte et sur la page Collections (montures, mascottes, orchestrion) ; MobDrop de LuminaSupplemental |
| X-42 | Avant la phase 7 : prendre les réglages de l'utilisateur et en faire les valeurs par défaut | Abandonné le 2026-11-07 à la demande de l'utilisateur (« garde valeur par défaut cohérente ») : les valeurs par défaut restent celles du code, rien n'a été publié |
| X-43 | Base locale de découvertes : relever ce qui est à portée (monstres et leurs lieux, butin, sorts bleus appris, captures du dresseur, points de récolte, pêche…) pour compléter les outils ; options de débogage mais actif hors débogage ; import (fusion avec la nôtre) et export, pour ne plus dépendre des données communautaires à force de jouer | Cadre accepté le 2026-11-07 (« Oui, ce cadre ») : fichier local, provenance par entrée, aucun autre joueur, rien d'envoyé, pas d'aide au combat, coupé en JcJ, import avec fusion et export de fichiers ; 0.807 : le socle (page Découvertes, export, import qui joint, débogage) et les lieux des monstres ; 0.809 : capture des messages du jeu (5 par type, noms masqués) pour le butin, les sorts bleus et les captures |
| X-44 | Carnet d'artisanat : pas de menu Tensho au clic droit, ni sur une recette de la liste ni sur un matériau | Fait en 0.808 : le journal montrait un menu « Default » de RecipeNote avec l'objet survolé (12893, 12846), écarté par la règle des personnages ; les fenêtres d'objets du carnet sont maintenant acceptées |
| X-45 | Carte : des marchands d'événement sont affichés alors qu'ils ne sont pas là en jeu (Limsa, la Dunette) ; à corriger juste avant la phase 7 | 0.810 : les PNJ placés par un événement (planevent.lgb, FestivalID) ne sont montrés que pendant cet événement ; 0.811 : aussi ceux des couches de quête et d'ouverture (QST_, QB_, EVT_OP), jamais montrés, `docs/evidence/0.811-conditional-npcs.md` ; 2026-11-07 21 h, sur capture : PNJ d'événement toujours sur la carte (Limsa, l'Entrepont) avec 0.810 : rouvert, à reprendre après la présentation des relevés ; rouvert encore le 2026-11-07 (capture de la Dunette avec 0.811) : les positions venues de la feuille Level ne portaient pas la condition de leur couche ; régression de 0.823 : carte 12, 1 marchand montré et 13 écartés (marchands permanents de la feuille Level marqués « quête » par une couche de quête) ; 0.828 : une position de la feuille Level ne prend plus que la condition d'événement ; 0.829 : « QST_ENPC » permanent (0.811 cachait de vrais marchands) ; un marchand placé par aucune couche, seulement par la feuille Level, n'est plus dessiné (les 9 de la Dunette sont des PNJ d'événement) ; validé en jeu le 2026-11-07 (« pnj event ok ») |
| X-46 | Carte : le zoom ne reste pas toujours celui qu'on a réglé ; à faire juste avant la phase 7 | 0.850 : une autre carte que celle du joueur (le jeu garde la dernière vue, donc M en demandait souvent une autre) et une carte choisie dans les menus s'ouvraient entières, hors du réglage ; elles suivent maintenant le réglage d'ouverture (fixe, dernier, entière), centrées |
| X-47 | Récompenses de quête (fenêtre de la quête) : ni panneau Tensho sous l'info-bulle de l'objet, ni entrée Tensho ; à faire juste avant la phase 7 | 0.852 : menu : le journal du 2026-11-07 22:11:48 montre le menu de JournalAccept ouvert (objet 5707) mais écarté comme menu de personnage (nom présent) ; les fenêtres de quête (JournalAccept, JournalResult, JournalDetail) sont maintenant des fenêtres d'objets. Panneau : pas de preuve ; le mode développeur note « Item hover without panel » avec la raison (piste : l'interface du jeu masquée pendant le dialogue, qui masque aussi les fenêtres des plugins : règle Dalamud, accord à demander si c'est ça) |
| X-48 | Carte : le clic droit sur la cible d'une quête (« Cible indiquée par le jeu (journal) ») n'offre pas l'éthérite la plus proche ; à faire juste avant la phase 7 | 0.853 : l'entrée existait sur toutes les icônes mais n'apparaissait que sur une carte qui a sa propre éthérite ; le Tillac de Limsa n'en a pas (portes d'éthernet seulement) : une carte avec une porte d'éthernet ou une voisine compte maintenant, et la question de téléportation dit le chemin (0.833) |
| X-49 | Carte : la cible de quête ne disparaît pas après validation de la quête (Ishgard, les Contreforts, capture du 2026-11-07) | Fait en 0.813 : la demande du journal n'a pas de quête (« quest 0 » au journal) ; la cible suit le point de la quête acceptée au même endroit (liste propre du jeu) et part quand il n'y est plus |
| X-50 | Carte : les icônes de bord de carte doivent passer au-dessus de tout (cachées sous le bandeau des noms, capture d'Abalathia) | Fait en 0.814 : dessinées dans une couche fille sans entrée, créée après le bandeau et les outils |
| X-51 | Panneau des collectionnables : surligner sur les barres de raccourcis les compétences que le plan demande, comme l'aide à la récolte (capture : Œil-de-verre) | Fait en 0.815 : même cadre et même réglage que l'aide à la récolte ; sans plan (aucun seuil connu, comme l'Œil-de-verre), rien à surligner ; rouvert le 2026-11-07 : l'encadré doit être sur la fenêtre des collectionnables du jeu, pas sur les barres de raccourcis ; 0.816 : encadré sur le bouton de la fenêtre du jeu qui porte l'icône de l'action (composants d'icône de la fenêtre, icônes listées dans les valeurs brutes du mode développeur) |
| X-52 | Base de découvertes : les prix vus au tableau des ventes remplacent Universalis (plus fiables), après avoir vérifié qu'on a autant d'informations | Fait en 0.817 : pour son monde, le jeu donne autant (offres, 20 dernières ventes, moyenne, ventes par jour), pas les autres mondes ni les objets jamais ouverts ; un prix vu remplace celui d'Universalis quand il est plus récent ; rouvert le 2026-11-07 : sur un objet sans copie d'Universalis, le prix vu effaçait les autres mondes (plus de requête Universalis) ; 0.821 : sans copie d'Universalis, le prix vu attend sa réponse (sauf source coupée), puis les deux sont réunis |
| X-53 | Monstres proches : le yak laineux et le loaghtan (lait de yak, viande de loaghtan de la liste de suivi) n'ont pas d'icône sur la carte ni au-dessus d'eux (captures, Coerthas occidental, Avant-pays dravanien) | En cours : les données MobDrop relient bien 12888 aux yaks laineux 3479 et 11214 ; 0.818 note la chaîne au journal en mode développeur ; l'icône au-dessus de la tête des monstres de liste est désactivée par défaut (réglage « Butin pour vos listes ») ; 2026-11-07 : le loaghtan aussi ; « données viennent du wiki pour le moment » : correspondance par nom et monstres du wiki en plus ; 0.819 : correspondance par numéro, par nom et par les pages du wiki sur le disque ; à vérifier en jeu ; 2026-11-07 22:28, cocatrix : toujours rien, et aucune ligne « Nearby monsters » au journal ; cause trouvée : le minuteur « une fois par seconde » partait de long.MinValue, le calcul débordait et le calcul des monstres proches ne s'exécutait jamais (aussi la base de découvertes et l'encadré des rangements) ; corrigé en 0.828 ; VALIDATED_IN_GAME pour les monstres du carnet de chasse (« monstre bestiaire OK », capture d'un élémentaire de terre, Gladiateur 07, 2026-11-07) ; VALIDATED_IN_GAME aussi pour les monstres des listes (« monstre ressource liste ok », océanide, viande d'océanide, 2026-11-07) |
| X-54 | À l'ouverture d'un autre rangement que l'inventaire (servant, sacoche du chocobo, coffre de compagnie, autres) : surligner les objets qui y sont et que demandent les listes de suivi d'artisanat ou d'atelier | Fait en 0.820 : cases repérées par l'icône de l'objet dans les fenêtres de rangement, encadrées ; journal « Storage highlight » en mode développeur pour vérifier les fenêtres et les icônes ; 2026-11-07 : « toujours pas présent » : même cause que X-53 (minuteur à long.MinValue), corrigé en 0.828, à réessayer |
| X-55 | Listes de suivi d'artisanat et d'atelier : préciser quand l'objet n'est pas sur soi (chez un servant, dans la sacoche, le coffre de compagnie…) | Fait en 0.822 : icône d'entrepôt à côté du compte quand les sacs ne couvrent pas le besoin ; les rangements et leurs quantités au survol |
| X-56 | Listes de suivi d'artisanat et d'atelier : à la place de « téléportation 219 gils », l'icône de la compétence Téléportation puis l'icône des gils et le montant, sans texte | Fait en 0.824 : icône 111 (Action 5, Téléportation), icône du gil, montant ; le texte au survol |
| X-57 | Carte, quête de cuisinier à Limsa : la cible (au bon endroit) et l'icône de quête ne sont pas au même endroit, la quête affichée tombe hors de la zone, et elle n'apparaît pas sur la carte du Tillac (captures du 2026-11-07, la carte du jeu montre le lien vers le Tillac) | À faire, après X-56 ; en cours : le jeu donne deux marqueurs pour la quête 2046 « Le pari » sur la carte 11 (icônes 71141 et 71143, objectifs 2046 et 520), sa liste propre la place à X 10,7 Y 9,7 ; 0.825 capture chaque marqueur avec sa position ; 2026-11-08, « Une faim en queue de poisson » (Tillac) : la cible est juste (Les Sept Sauges, X 11,7 Y 8,1), l'icône de quête non (au Bismarck, X 9,9 Y 8,1) ; la carte du jeu ne montre que celle des Sept Sauges |
| X-58 | Menu Tensho du clic droit sur un objet : ajouter sa recette à une liste d'artisanat déjà créée | Fait en 0.826 : « Ajouter à une liste d'artisanat » dans le menu de Tensho, et « Ajouter à « liste » » dans le sous-menu Tensho du jeu, pour un objet qui a une recette |
| X-59 | Listes de suivi, points à horaire : retirer « ouvert » / « fermé », garder le minuteur seul, en vert quand le point est ouvert, en rouge quand il est fermé | Fait en 0.827 : minuteur seul en vert ou rouge, le sens au survol |
| X-60 | Listes de suivi, clic droit sur un objet : « Placer le drapeau » absent quand on est sur la carte du lieu, et « Y aller » absent pour certains (Échalion cyclope, Avant-pays dravanien) | 0.830 : « Placer le drapeau » ajouté (même point que « Y aller », sans téléportation) ; « Y aller » absent quand la position du point n'est pas connue : journal « Go there missing » en mode développeur pour savoir pourquoi ; 0.834 : « Y aller » absent pour l'Échalion cyclope : objet caché, voir X-64 |
| X-61 | Aide à la récolte : pas de plan proposé, « objet lu après une dépense de PR sur ce point » alors que les essais sont à 4/4 (PR 291/823, Échalion cyclope, capture du 2026-11-07) | 0.831 : le point suivant du même genre partage ses objets, et sa fenêtre peut s'ouvrir avant que la fermeture soit lue : il gardait la marque « PR dépensés » du point d'avant. Un autre point (ligne du jeu) ou des essais qui remontent repartent de zéro |
| X-62 | Alarmes ajoutées depuis une liste de suivi : elles sonnent encore quand la ressource est déjà réunie (Sel gemme d'Abalathia 24/2, capture du 2026-11-07) | 0.832 : une alarme ajoutée depuis une liste garde la quantité demandée ; objet détenu en cette quantité : elle ne sonne plus (ni chat, ni son, ni Discord), « objet réuni : muette » dans la page Alarmes. Les alarmes ajoutées avant 0.832 ne savent pas la quantité : à retirer et ajouter de nouveau depuis la liste |
| X-63 | « Y aller » vers une zone sans éthérite (Arrière-pays dravanien) : téléporter à une éthérite d'une zone voisine qui donne sur la zone visée, la moins chère en gils (Idyllée ici), et de même pour toute zone sans éthérite ; l'éthérite choisie doit avoir une téléportation du réseau local (Éthernet) vers la sortie qui donne sur la zone ; s'il n'y en a pas, le signaler avant la téléportation | 0.833 : zone sans éthérite : d'abord une porte de l'éthernet placée dans la zone (Porte du Prologue, de l'Épilogue : rangées par le jeu sous l'Arrière-pays), par l'éthérite de sa ville ; sinon l'éthérite harmonisée la moins chère d'une carte voisine, et la porte de son éthernet la plus proche de l'entrée ; la question de téléportation dit l'étape d'éthernet, ou en rouge qu'il n'y a pas d'éthernet (pas de téléportation directe) ; preuve docs/evidence/0.833-zone-entries.md |
| X-64 | Listes de suivi, clic droit : ni « Y aller » ni « Placer le drapeau » pour Haricots émeraude (Fauche, L'Écume des cieux de Dravania, capture du 2026-11-07), suite de X-60 : lire le journal « Go there missing » | 0.834 : cause trouvée par la sonde des données : Haricots émeraude et Échalion cyclope ne poussent que comme objets cachés de leur point (Colonie du Propriétaire, Forêt des Chocobos), et « Y aller » écartait les sources cachées ; elles passent maintenant après les autres ; preuve docs/evidence/0.834-hidden-gathering-items.md ; 0.835 (règle de l'utilisateur : « écarter seulement si indisponible ») : un point que le personnage ne peut pas encore récolter (livre de folklore non lu, niveau trop bas) est écarté |
| X-65 | Liste de suivi d'artisanat (Marrons glacés) : le sirop de bouleau, ingrédient intermédiaire détenu (157/1), n'apparaît pas dans le panneau ; et le panneau ne signale pas qu'un objet est chez un servant (captures du 2026-11-07) | 0.838 : un ingrédient qui se fabrique, pris dans le stock, n'était inscrit dans aucun groupe (le calcul s'arrêtait là) : il reste maintenant une ligne, pour la part prise ; l'icône d'entrepôt (0.822) dit alors qu'il est chez un servant ; après 0.838, toujours pas de sirop (capture) : 0.840 note au journal (mode développeur) ce que le panneau demande et obtient, chaque ligne avec stock, recette, groupe ; journal du 2026-11-08 00:00 : la liste d'artisanat passe ses ingrédients directs (12892 sirop compris) au premier niveau, que la 0.838 laissait de côté : 0.841, à tous les niveaux |
| X-66 | Encadré des rangements (X-54) : chez le servant, il encadre le vin de cuisine au lieu du vin de cuisine de qualité (même icône), et l'encadré reste en changeant d'onglet de sac (captures du 2026-11-07) | 0.839 : les cases étaient trouvées par l'icône seule ; maintenant icône et quantité affichée, comparées aux piles du rangement ouvert (deux objets pareils en tout : pas d'encadré plutôt qu'un faux) ; une case vide garde son ancienne icône image cachée, et l'onglet caché cache un parent : la case, son image et tous ses parents doivent être visibles |
| X-67 | Carte de Tensho : signaler qu'une cible est plus haut ou plus bas, comme la carte du jeu (flèche haut ou bas sur l'icône) (capture de Mor Dhona, 2026-11-07) | 0.842 : la règle du jeu (seuil, codage) n'est pas connue : capture « mapMarkers » en mode développeur (marqueurs de la carte du jeu, drapeaux et icône secondaire, hauteur du joueur), et hauteur des marqueurs d'événement ; à relever carte du jeu ouverte devant une flèche ; aussi sur les zones de quête (grande flèche orange dans la zone, capture de la quête de cuisinier, Mor Dhona) ; capture du 2026-11-08 00:12 : le jeu ne garde pas la flèche (Flags 40 à la hauteur du joueur comme 38 yalms plus bas), il compare les hauteurs en dessinant ; 0.844 : flèche orange haut ou bas sur les icônes de quête, d'événement et d'ALÉA, seuil réglable (10 yalms par défaut, choix de Tensho, celui du jeu n'est pas connu) |
| X-68 | Carte de Tensho : la zone de la quête de cuisinier (Mor Dhona) « ne se fait pas zoomer dessus » (capture du 2026-11-08) | Précisé : « depuis le journal ». 0.845 : ouverte depuis le journal, une quête qui a une zone est zoomée pour la montrer entière (60 % de la vue) ; sans zone, le zoom reste (demande de la 0.637) ; correction de l'utilisateur : « il faut pas zoom il faut centrer sur quête, objectif, ressource ou autre » : 0.846 retire le zoom, la carte centre sans changer le zoom |
| X-69 | Carte de Tensho : « la map ne se rafraîchit pas assez » : la quête est passée à « Parler à Raulf », mais la « Cible indiquée par le jeu (journal) » reste affichée (capture de Mor Dhona, 2026-11-08) | 0.847 : la cible d'une quête précise ne partait que quand la quête n'était plus active (journal relu une fois par minute) ; elle suit maintenant le point de quête où elle était, comme la cible sans quête (0.813) : étape faite, elle disparaît |
| X-70 | Carte de Tensho : la zone de la nouvelle étape d'une quête manque tant que la carte du jeu n'a pas été fermée puis rouverte (Kugane, « L'art du sushi », 2026-11-08) ; rouvrir n'est pas une solution (la carte clignote à l'écran) | 0.848 : la liste de la fenêtre carte (AgentMap.EventMarkers) n'est refaite qu'à son ouverture ; la liste de quêtes du jeu (Map.QuestMarkers) suit chaque étape (journal 00:36:56 : quête 2607, carte 371, rayon 35, fenêtre vide) : ses quêtes affichées remplacent celles de la fenêtre ; preuve docs/evidence/0.848-quest-markers-current.md |
| X-71 | Carte de Tensho : reprendre les icônes plus haut / plus bas de la carte du jeu (vues à Mor Dhona) partout, à la place de la flèche de la 0.844, « on en a besoin ailleurs » (2026-11-08) | 0.849 : le chevron orange du jeu (ui/uld/NaviMap.tex, pièce U 352 V 96, 24 × 24, sondes UldProbe et MapMaskProbe --tex), pointe en haut pour plus haut, retourné pour plus bas, sur toutes les icônes de quête, d'événement et d'ALÉA ; preuve docs/evidence/0.849-height-chevron.md ; 0.851 : un peu plus petit (capture de l'utilisateur), 55 % de l'icône |
| X-72 | Base de découvertes (parseurs) : vérifier qu'elle est efficace, l'utilisateur a déjà un peu joué (2026-11-08) | Revue du 2026-11-08 : le collecteur marche (1 469 relevés, 82 monstres, 10 cartes, 418 Ko en quelques heures ; prix vus : MarketSeen.json, 1 objet), mais : (1) aucun outil ne s'en sert, seule la page Découvertes les montre ; (2) cases de 10 yalms : 87 % vus une fois, 640 relevés au lieu de 1 469 avec des cases de 50 yalms ; (3) un seul genre relevé (places de monstres). Propositions : A brancher les places vues sur la page Butin des monstres et le carnet de chasse ; B cases de 50 yalms (fusion des relevés existants) ; C d'autres genres. Choix de l'utilisateur : « ABC ». 0.854 (A) : la page Butin des monstres et le carnet de chasse montrent « Vu par vous : zone (X, Y), niveau », un lieu par carte, quatre au plus, clic droit éthérite la plus proche ou drapeau ; 0.855 (B) : cases de 50 yalms ; au chargement et à l'import, chaque relevé est replacé dans sa case et ceux d'une même case sont fusionnés (vues et imports additionnés, niveaux et dates les plus larges, premier lieu gardé), la base est réécrite ; 0.856 (C) : deux genres de plus : où apparaissent les monstres de chasse (rang B, A, S : dans aucune donnée du jeu) et les ALÉA vues (lieu, niveau, quand, une fois par heure au plus) ; chacun son réglage, cherchés dans Monde › Découvertes |
| X-73 | Parseurs : découvrir en jeu tout ce que Tensho prend aux sources communautaires (LuminaSupplemental, Garland Tools, wiki, GatherBuddy pêche, Teamcraft temps de touche, revue de mode) et compléter Universalis (prix des autres mondes du centre de données quand on s'y rend) ; à terme ne plus dépendre que d'Universalis, si c'est possible ; ultracode autorisé si besoin (2026-11-08) | Cartographie faite (docs/evidence/0.857-community-map.md) ; étapes X-73.1 à X-73.12 ci-dessous, dans cet ordre |
| X-73.1 | Marché : prix vus sur chaque monde visité du centre de données, préférés à Universalis quand plus récents ; âge montré ; purge après 90 jours | 0.857 : le meilleur prix du centre de données prend aussi les prix vus sur chaque monde visité (offres de moins de 24 h ; le monde d'Universalis cède à un relevé plus récent sur place) ; rien de plus vieux que 90 jours n'est chargé ; la page Marché et le comparatif des servants restent à faire (X-73.12) |
| X-73.2 | Marché : historiques de ventes fusionnés d'une ouverture à l'autre (vitesse de vente calculée sur place) | 0.858 : chaque ouverture du tableau ajoute ses 20 ventes à celles gardées (sans doublon), 200 ventes et 30 jours au plus ; la vitesse de vente se calcule sur tout ce qui est gardé |
| X-73.3 | Pêche : drapeaux du carnet du jeu (« sans condition d'heure ni de météo ») à la place de la donnée communautaire | 0.859 : le panneau de pêche lit d'abord le carnet du jeu ; « aucune condition d'heure » ou « de météo » y retire la fenêtre ou la météo de GatherBuddy et de Garland ; « ni heure ni météo (jeu) » affiché ; inconnu : rien ne change |
| X-73.4 | Revue de mode : thème et catégories lus dans le jeu (FashionCheckManager) au lieu des suppositions | 0.861 : catégories de la semaine par leur numéro dans le jeu (FashionCheckManager.Theme.ItemThemes), le nom en secours ; thème lu dans le jeu au lieu de « semaine + 9 » ; la tenue jugée (objet, catégorie, note brute) gardée dans les découvertes ; le sens des notes reste à prouver |
| X-73.5 | Désynthèse et réduction : objets obtenus de quel objet (AgentSalvage, AgentPurify) | 0.860 : désynthèse lue dans AgentSalvage (objet désynthétisé, résultats et quantités) |
| X-73.6 | Tâches des servants : récompenses vues (AgentRetainerTask) | 0.860 : récompenses des tâches lues dans AgentRetainerTask quand le résultat s'affiche |
| X-73.7 | Coffres des missions : objets vus par mission (Loot.Items) | 0.860 : objets des coffres lus dans Loot.Items en mission (CFC du territoire) |
| X-73.8 | Voyages : butin par secteur (déjà lu dans l'atelier) | 0.862 : butin des sous-marins par secteur (HousingWorkshopSubmarineGathered.Point) et secteurs débloqués (UnlockedPoint), une fois par voyage ; les aéronefs ne donnent pas de secteur par entrée : laissés de côté |
| X-73.9 | Mage bleu et dresseur : sort appris et bête apprivoisée, avec le monstre | 0.863 : sort appris (IUnlockState.Unlock, AozAction) et bête apprivoisée (ensemble des bêtes acquises qui grandit), reliés au monstre tué dans les 15 s ou à la cible, avec son lieu |
| X-73.10 | Pêche : prises (appât, mooch, lieu), temps de touche, conditions vues ; sondes en jeu d'abord | À faire |
| X-73.11 | Butin des monstres : objets reçus après la mort de sa cible (le plus risqué : attribution) | 0.860 : gains des sacs dans les 3 s après la mort de sa propre cible, hors mission, hors fenêtres de boutique, d'échange, de servant, de courrier, d'artisanat |
| X-73.12 | Le reste selon la carte : coffres d'objets, ALÉA récompenses, panoramas, animaux de l'île, fêtes vues | 0.864 : fêtes vues (GameMain.ActiveFestivals, une fois par jour, premier et dernier jour) et panoramas inscrits (météo, heure d'Eorzéa) ; 0.868 : page Marché, onglets Offres et Historique complétés par les offres et ventes vues au tableau du jeu (offres d'un monde vues après la dernière revue d'Universalis, moins de 24 h ; ventes sans doublon ; seules si Universalis n'a rien) ; restent : animaux de l'île, marchands sans Level, comparatif des servants (les offres vues n'ont pas de vendeur : vos autres servants ne se distinguent pas, laissé à Universalis) |
| X-73.13 | Demande du 2026-11-08 (« oui tous en faisant tests en jeu nécessaires ») : sondes en mode développeur pour la pêche (lancer, touche, type de touche, effets), la réduction éthérée et les objets demandés par les quêtes ; relevés en jeu par l'utilisateur | 0.869 : relevés ajoutés à la capture du mode développeur (« Reading fishline », « purify », « npctrade » dans le journal ; les effets de pêche étaient déjà relevés par « statuses ») ; attend les relevés en jeu |
| X-73.14 | Offres vues marquées « à moi / pas à moi » (numéro de servant comparé aux vôtres, aucun nom gardé) : comparatif des servants et « copier le prix » avec les offres vues | 0.870 : chaque offre vue garde seulement « à vous / pas à vous » ; ventes des servants comparées aux offres vues sur votre monde quand plus récentes qu'Universalis (moins de 24 h) ; « copier le prix » de la page Marché compte les offres vues qui ne sont pas à vous |
| X-73.15 | Table centre de données → région depuis les feuilles du jeu (WorldDCGroupType) au lieu d'Universalis | 0.871 : région lue dans la feuille du jeu (Japon, Amérique du Nord, Europe, Océanie, Chine, Corée) ; la table d'Universalis n'est plus demandée que pour le centre « cloud » ou un inconnu ; `docs/evidence/0.871-regions.md` |
| X-73.16 | Lieu des marchands absents des feuilles : noté à l'ouverture de leur boutique (cible, zone, position) | 0.872 : PNJ ciblé noté avec son lieu quand une fenêtre de boutique s'ouvre (une fois par ouverture) ; utilisé pour les vendeurs que les feuilles ne placent pas (page Marchands, info-bulle, provenances, avant LuminaSupplemental) |
| X-73.17 | Butin des coffres des donjons sans fond, d'Eurêka et de Bozja | 0.873 : en Eurêka, Bozja, Croissant occulte et donjon sans fond (reconnus par le jeu lui-même), un objet reçu juste après un monstre vaincu compte comme butin de ce monstre ; en donjon sans fond, un objet reçu sans rien dépenser compte comme coffre de l'étage ; les sacs et coffres ouverts ensuite étaient déjà notés (objets ouverts, 0.860) |
| X-73.18 | Après les relevés de X-73.13 : temps et type de touche, mooch, Intuition et prédateurs ; réduction éthérée ; objets de quête | 0.887 : relevé de pêche de l'utilisateur (`docs/evidence/0.887-fishing-bite.md`) : type de touche (animation 7 : 292 !, 293 !!, 294 !!!), temps depuis le lancer (les leurres ne le relancent pas), lancers sous Amorçage (763) exclus ; ligne « Vu par vous » du panneau de pêche ; 0.888 : éthérolyse relevée en jeu (`docs/evidence/0.888-aetherial-reduction.md`) et parseur ; correction de vie privée de la capture « messages » ; 0.889 : remise relevée en jeu sur un mandat (`docs/evidence/0.889-hand-in.md`) ; parseur des objets demandés par les quêtes (quête dont l'étape change dans les 15 s), à confirmer à la première remise de quête ; reste Intuition et prédateurs ; 0.890 : harpon relevé en jeu (`docs/evidence/0.890-spearfishing.md`), parseur de l'ombre (taille, vitesse) d'une prise quand une seule sorte d'ombre est en vue, à valider au prochain harpon |
| X-74 | Consigne de nuit (2026-11-08, l'utilisateur dort) : ajouter le plus de parseurs cohérents possible (« mieux vaut prendre plus et retirer ensuite »), compléter le jeu de données selon nos besoins, revoir le plan de l'étape 7 pour qu'il colle au code actuel, puis passer à l'étape 7 une fois les plans en place ; ultracode autorisé au besoin | Fait pour la nuit (0.857 à 0.867) ; 0.860 : observateur des objets obtenus (ItemDiscoveryWatcher) : désynthèse, tâches, coffres de mission, récompenses d'ALÉA, butin de sa cible, objets ouverts, poissons pris (appât, lieu, météo, heure) ; textes du résumé des genres de la 0.856 qui manquaient ; 0.861 à 0.864 : revue de mode, voyages, mage bleu et dresseur, fêtes et panoramas ; 0.865 : « Comment l'obtenir » montre d'abord « Vu par vous », qui dispense de Garland et du wiki |
| X-75 | Demande du 2026-11-08 : un test pour chaque sonde ; revérifier qu'aucune sonde ne manque pour découvrir en jeu les données prises ailleurs que dans les feuilles du jeu. Placement : après le plan en cours (X-73.15 à X-73.17), avant la traduction des documents vivants | À faire |
| X-75.1 | Un test pour chaque sonde : liste des parties de la capture figée par un test, test de texte pour chaque sonde et parseur qui n'en a pas ; relevés en jeu ajoutés à TESTING et aux rappels | 0.874 : rapport `docs/evidence/0.874-probe-coverage.md` (64 sources, 46 sondes) ; `ProbeSet874Tests` fige les 17 parties de la capture et teste les gardes des sondes qui n'en avaient pas ; relevés en jeu de chaque sonde listés dans TESTING (0.874) |
| X-75.2 | « Vu en jeu » d'abord partout où la donnée apprise existe déjà : page Monstres, monstres proches (butin, bêtes), menu clic droit, provenances des collections, « ce qu'il donne », donjons sans fond, coffres de missions | 0.875 : page Monstres (« Vu le laisser »), monstres proches (butin vu, bêtes apprivoisées même sans le wiki), provenances (missions dont un coffre l'a donné, objets d'où il vient), « Vu donner » dans les usages ; 0.876 : menu clic droit (mission et monstres vus), listes des coffres de missions (« Coffre (vu par vous) »), donjons sans fond (« Vu dans les coffres ») |
| X-75.3 | Pêche : prise au mooch notée avec le poisson moochés (et non l'appât), leurre, météo précédente, « vu en … » dans le panneau de pêche | 0.877 : drapeaux du lancer lus (mooch → poisson pris avant, leurres), météo de la période précédente (prévision du jeu), ligne « Vu par vous » dans le détail du panneau de pêche (appâts, météos, heures, leurres) ; jamais présentée comme une condition |
| X-75.4 | Mage bleu : monstres vus lançant un sort (barre d'incantation), niveaux des sorts tirés de ce qui a été vu | 0.878 : monstres vus lançant l'action d'un sort de mage bleu (barre d'incantation, une fois par incantation, tout job), avec niveau et lieu ; fenêtre d'un sort : « Vu par vous » avant le wiki ; le tri de la liste par niveau garde le wiki |
| X-75.5 | Autres pages : fêtes « vues du … au … », lieux des ALÉA vus sur la page des rangs S, météos des panoramas vues, meilleur prix de la région complété par les mondes visités | 0.879 : fêtes (« Vue en cours par vous du … au … »), panoramas (« Inscrit par vous par … à … ET »), meilleur prix de la région complété par les mondes de la région visités (feuilles World et WorldDCGroupType) ; reste la page des rangs S (lieux des ALÉA vus), avec X-76 |
| X-75.6 | Calculs du jeu à la place des formules communautaires : durées des voyages de sous-marins (fonctions du jeu), contrôle de l'horaire de la pêche en mer et de la météo prévue | 0.880 : horaire de la pêche en mer contrôlé contre la route lue à bord (une fois par voyage, écart signalé dans le journal) ; météo prévue contrôlée contre celle du jeu 10 s après l'entrée dans une zone ouverte (comptée, journal de débogage). Décision à soumettre à l'utilisateur (lot de fin) : appeler les fonctions de calcul du jeu pour les sous-marins, comme pour les aéronefs (DEC-023 point 3) |
| X-75.7 | Nouvelles sondes en mode développeur, puis parseurs après les relevés : animaux de l'île, pêche au harpon, secteurs et destinations des aéronefs, lignes de résultat des sous-marins, lien solide, collectionnabilité, taux de sertissage forcé, revue de mode (aperçu, teintures), boutique ouverte, objets reçus par courrier, récolte du jardin (croisements), récompenses de fin de mission, récolte abondante | 0.881 : parties « addons » (nombres seulement des fenêtres harpon, aéronef, résultats de sous-marin, sertissage forcé, extraction de matéria, courrier), « fashion » (aperçu et meilleur score avec teintures), « islandobjects » (animaux de l'île) ; déjà couverts : récolte abondante et effets (statuses), collectionnabilité (synthesis), récolte du jardin (capture du jardin), fin de mission (messages). Boutique ouverte : structure non sondée, laissée. Parseurs après les relevés en jeu |
| X-75.8 | Hors parseur : PaissaDB (lire la pancarte demande un crochet : règle Dalamud, accord de l'utilisateur d'abord), ferrage utilisé (crochet), boutique Mog Station, dates futures, mondes jamais visités : restent aux sources communautaires | Noté |
| X-77 | Demande du 2026-11-08 : si possible, un système qui passe par Git pour envoyer les données découvertes par les parseurs et récupérer celles des autres qu'on n'a pas. Placement : après les tâches en cours (X-75), avant le nettoyage X-76. Règle Dalamud en jeu (envoi de données, pas de compte imposé) : résumé court et accord de l'utilisateur avant de coder. Piste : téléchargement d'un fichier commun du dépôt sur clic ; envoi par un fichier exporté proposé au dépôt (ticket ou demande de fusion ouverts dans le navigateur, sans jeton dans le plugin), fusionné après vérification | 0.882 (accord de l'utilisateur, « Oui, ce schéma ») : « Récupérer les découvertes communes » (fichier `community-discoveries/discoveries.json` de la branche main, sur clic, joint comme importé, une fois par version du fichier) ; « Partager les miennes » (vos propres découvertes dans un zip, dossier Export et page « nouveau ticket » ouverts dans le navigateur, modèle « Discoveries ») ; fusion par le mainteneur avec `tools/discoveries-merge.py` ; le fichier arrive chez les joueurs avec la release suivante |
| X-77.1 | Retour de l'utilisateur (2026-11-08) : « le système actuel pour import export avec git est pas très pratique » ; choix de l'utilisateur : connexion GitHub (code à approuver sur github.com, une fois), et la question « l'automatique ferait aussi l'envoi ? » : oui, proposé ainsi : un réglage facultatif, coupé par défaut, qui une fois GitHub connecté récupère le fichier commun et envoie vos nouvelles découvertes une fois par jour (un gist privé par joueur mis à jour, un seul ticket par joueur) ; il faut que l'utilisateur crée une application OAuth GitHub (Device Flow) et donne son identifiant client | 0.883 : application OAuth créée par l'utilisateur (Client ID Ov23liBmk4RCUYw6pU6Z, Device Flow, jetons sans expiration) ; « Connecter GitHub » (code à approuver), « Échanger maintenant », « Déconnecter », réglage « Échange automatique » (coupé par défaut, une fois par jour) ; jeton protégé par Windows (DPAPI) ; gist secret par joueur mis à jour, un ticket par joueur ; outil de fusion lit les gists ; 0.884 : essai réel de l'utilisateur réussi (connexion, échange, ticket #43, gist) ; outil corrigé (préfixe « gist: », propriété « Key » calculée), 668 faits du premier gist fusionnés dans le fichier commun (aucun refusé, aucun nom) ; 0.885 (« garde que auto ») : boutons « Récupérer les découvertes communes » et « Partager les miennes » et modèle de ticket retirés ; restent l'échange automatique et « Échanger maintenant » ; Exporter et Importer gardés (fichiers locaux, sans Git) ; 0.886 : outil « sync » (lit les tickets « Discoveries », un fichier par joueur dans community-discoveries/players/, fichier commun reconstruit, jamais compté deux fois, FLAG au-delà de 5 % de refus) ; règle de l'agent écrite (contrôle à chaque reprise et avant chaque release, demande de l'utilisateur) |
| X-78 | Demande du 2026-11-08 (capture de la carte de Tensho, La Mer de Rubis) : garder les icônes de gauche (poisson du point de harpon, avec sa zone) et donner aussi une zone à l'icône de pêche de droite (trou de pêche à la ligne, icône 62118) | 0.890 : icônes identifiées dans les fichiers du jeu ; le rayon de FishingSpot n'a pas d'unité prouvée : la ligne « Game map hosted » écrit maintenant la zone du jeu (Scale) ; relevé à faire : carnet de pêche › un trou de La Mer de Rubis › afficher sur la carte ; puis la zone sera dessinée ; 0.891 (précision de l'utilisateur : l'icône de pêche normale n'a ni rayon ni la bonne icône, prendre le poisson du harpon en orange comme sur FF14) : icône orange du jeu 60465 sur les trous de pêche ; zone toujours en attente du relevé ; 0.894 : zone mesurée sur la carte du carnet de pêche (capture de l'utilisateur, `docs/evidence/0.894-fishing-spot-area.md`) : rayon ≈ 0,155 × FishingSpot.Radius, dessinée sur les trous de pêche ; comparée au carnet de pêche par l'utilisateur : « c'est ok je pense » |
| X-79 | Demande du 2026-11-08 (essai du harpon) : la liste du panneau de harpon doit rester tout le temps et mettre en évidence les poissons de la bonne taille en vue (elle disparaissait avec les poissons) | 0.891 : tous les poissons du point (sinon de la zone) toujours listés dans un ordre fixe, avec leur ombre (vue par vous d'abord), ceux en vue en couleur ; règle du poisson harponné corrigée (emplacement qui part en gardant sa taille, `docs/evidence/0.891-spearfishing-slots.md`) ; 0.892 (capture de l'utilisateur : liste de toute la zone, « elle change en permanence ») : mise en page fixe (trois lignes d'emplacement, lignes qui ne bougent pas), point trouvé à l'ouverture (cible, sinon le point le plus proche) et gardé ; sans point, seulement vos prises et les poissons en vue ; 0.893 (demande : l'icône du poisson pour aider visuellement) : icône de chaque poisson dans la liste (cadre en couleur quand il est en vue) et icônes des poissons de cette ombre sur chaque ligne d'emplacement ; 0.894 : la liste du point se trouve par sa base de récolte (plusieurs points partagent une base : 32074–32076 → 465), les poissons que le jeu montre pour ce point ; 0.896 (capture : Crabe-pagaie noté grand et très rapide) : le poisson pris arrive un instant après le compteur, attendu ; vitesse gardée à l'apparition (elle peut doubler), le panneau compare aussi à sa moitié ; ombres de la 0.891 écartées (`docs/evidence/0.896-spearfishing-lag.md`) ; 0.896 validé en jeu (9 prises cohérentes) ; 0.897 (demande : moins de texte, lisible en temps réel) : tailles S/M/L, vitesse en chevrons, étoile pour grand garanti, œil pour vu par vous, mots au survol ; 0.898 (demande : les prises multiples, fréquentes) : poissons lus dans le chat, emplacements harponnés dans la fenêtre ; ombres pareilles → pour tous, sinon par élimination avec les ombres déjà vues, l'ambigu n'est pas noté ; validé en jeu : 6 doubles prises toutes justes (les triples, même règle, pas encore vues) |
| X-80 | Demande du 2026-11-08 (capture du filtre de la carte) : « Points de récolte » séparé en mineur, botaniste et pêcheur | 0.895 : trois cases sous « Points de récolte » (grisées quand il est coupé), aussi dans « Tout » / « Rien » ; pêcheur = trous de pêche et points de harpon ; validé en jeu par l'utilisateur |
| X-81 | Essai en jeu du sertissage forcé (X-75.7) | 0.899 : taux affichés par le jeu (matéria de degré I : 80, 40, 20, 10 %) identiques à MateriaJoinRate / MateriaJoinRateGatherCraft que Tensho lit déjà (`docs/evidence/0.899-overmeld-rates.md`) ; seuls les degrés XI et XII viennent de Teamcraft, à vérifier avec une matéria XI ou XII ; capture : limite de lignes par fenêtre |
| X-82 | Essai en jeu de la revue de mode (X-75.7) et retour de l'utilisateur : menu du clic droit grisé sur l'icône d'une pièce de la liste | Relevé : tenues jugées lues (thème 467, catégories, note par emplacement : 4 sur les quatre emplacements thématiques, 5 ailleurs ; teintures de la semaine : 68 → 70), aperçu lu (thème, meilleur score, tentatives) ; parseur des tenues jugées (0.861) validé en jeu ; 0.900 : seule l'image de l'icône est atténuée, son info-bulle et son menu ne le sont plus |
| X-83 | Retour de l'utilisateur (2026-11-08, sur l'île à côté d'un monstre : aucune icône sur la carte) et relevé des animaux de l'île (X-75.7) | 0.901 : les animaux capturables (MJIAnimals, par BNpcBase) autour de vous sur la carte, avec leur icône du jeu ; réglage « Animaux de l'île » (`docs/evidence/0.901-island-animals.md`) ; validé en jeu (« icônes ok ») ; 0.902 (retour : les calques de monstres n'étaient pas pris par « Tout » et « Rien ») : corrigé, test sur toutes les cases du menu ; validé en jeu (« filtre ok ») |
| X-84 | Essai en jeu du courrier (X-75.7) : une lettre avec un objet et 1 gil | 0.903 : pièces jointes lues (emplacements, quantités, vides après « Tout prendre », `docs/evidence/0.903-letters.md`) ; les gils d'une lettre (reçus ou joints) vont dans la catégorie « Courrier » du journal des gils ; les objets pris d'une lettre étaient déjà écartés des découvertes |
| X-85 | Document « a faire » de l'utilisateur (2026-11-08), pêche en mer (priorités données par l'utilisateur) : le panneau de pêche en mer doit ressembler à l'aide à la pêche (icônes des poissons, temps de touche) ; l'aide à la pêche ordinaire coupée pendant la pêche en mer ; poissons du courant spectral et poissons qui rapportent des assignats orange mis en évidence ; **prio 1** : meilleur appât pour déclencher le courant spectral ; **prio 2** : pêche au vif et objectifs en cours ; défaut vu au journal : l'horaire calculé donne la route 12 ou 15 quand le jeu dit 14 | V1 faite (0.917) : horaire vérifié par port par vos voyages, aide à la pêche ordinaire coupée en mer. V2 faite en partie (0.918) : panneau avec icônes, appât en main, touche et temps de touche du lieu, poissons du courant spectral repliés ; reste pour V2 : points par poisson et type de mer (lecture de GatherBuddy à étendre). Suite : V2 panneau, V3 objectifs, V4 courant spectral, V5 pêche au vif, V6 assignats (indice de l'utilisateur, 2026-11-08 : les assignats orange viendraient du score en fin de voyage ; source communautaire Reddit r/ffxiv « lvl100 ocean fishing scrips » : seuil de 10 000 points, en dessous assignats mauves seulement ; à prouver par le relevé de fin de voyage, points contre Scrip1/Scrip2). Conséquence pour V2/V3 : mettre en évidence les poissons qui rapportent beaucoup de points et le courant spectral, avec le score visé. À faire juste après le format des découvertes (DEC-042, « au plus vite »). Courant spectral : vos relevés d'abord, puis une source communautaire marquée (DEC-044). Indice de l'utilisateur : en français, le poisson qui déclenche le courant spectral porte « spectral » dans son nom (à vérifier dans les feuilles Item et IKDSpot, puis par le relevé du déclenchement ; captures de l'utilisateur : « Discus spectral », détroit sud de Merlthor, `checkpoints/x85-*`). Plan proposé V1 à V6 et questions ouvertes : `docs/ai/RESUME-2026-11-08.md`. Recherche faite : `checkpoints/ocean-fishing-research-2026-11-08.md` (plan V1 à V6). Relevé du 2026-11-08 : 27 prises en mer notées, touche « !!! » (animation 294) lue trois fois : parseur de touche 0.887 validé pour « !!! » |
| X-86 | Document « a faire », carte : menu du clic droit, une case pour afficher ou non les quêtes pas encore disponibles ; les zones de quête n'ont pas la flèche de hauteur (elle doit couvrir la zone sans rien masquer) ; les parties non découvertes de la carte ne sont pas tout à fait alignées (capture : Eurêka Anemos) | Après X-85 |
| X-87 | Document « a faire », mandats : voir les monstres de mandat avec l'icône du jeu ; la zone du mandat reste affichée après le début du mandat | Après X-86. Relevé : mandat de combat validé en jeu par l'utilisateur (X-37 « OK ») |
| X-88 | Document « a faire », tri d'inventaire : plus de 64 catégories impossible (capture : 64 catégories) ; revoir les tris pour en ajouter plus facilement par groupes | Après X-87 |
| X-89 | Document « a faire » : dans toutes les info-bulles, l'extension d'origine d'un objet, pareil pour les quêtes | Après X-88 |
| X-90 | Document « a faire », outils de contenus (placement donné par l'utilisateur : juste avant la 1.0.0, après le reste des tâches) : regarder ce que font les plugins existants puis faire des outils adéquats pour Eurêka (capture : niveau élémentaire et répartition de la magia), Bozja, le Palais des morts, le Pilier des Cieux, le Sanctuaire des pèlerins, l'île Lunule (Croissant occulte, retiré de « Retenu pour plus tard » par cette demande), des tableaux stratégiques pour les contenus et les raids fatals, armures et armes reliques pour chaque version de relique (pas par job) | Décision de l'utilisateur (DEC-041) : préparation des combats et des contenus permise, rien pendant le combat, rien qui lise le combat, JcE seulement, coupé en JcJ ; règles Dalamud gardées |
| X-91 | Document « a faire » : revérifier une dernière fois tous les parseurs (ce qui était prévu est-il bien relevé ?) et mettre à jour les documents `.md` pour l'agent et pour les autres, surtout CLAUDE.md et les règles | Avec l'étape des documents `.md` (après X-76), avant la 1.0.0 |
| X-92 | Demande du 2026-11-08 : « prévoir les contenus à venir ». Principe pour X-90 et pour tout outil : rien de figé par contenu quand le jeu le donne (listes tirées des feuilles : zones, contenus, donjons profonds, champs de bataille, reliques, extensions), pour qu'une nouvelle mise à jour du jeu soit couverte sans code ; une vérification après chaque mise à jour du jeu (version des données) qui liste ce qui est nouveau dans les feuilles (zones, contenus, objets, types de contenu, nouvelles colonnes) et ce que Tensho ne couvre pas encore, dans le journal et l'auto-contrôle ; les outils de contenus (X-90) pensés par famille (champs de bataille, donjons profonds, raids) pour qu'un nouveau contenu de la même famille s'y ajoute | Avec X-90 (juste avant la 1.0.0) ; la vérification après mise à jour peut venir plus tôt, avec l'étape 6 de X-76 (robustesse) |
| X-93 | Demande du 2026-11-08 : « prévisions pour une masse de données conséquente possible » (découvertes partagées par beaucoup de joueurs, longues durées de jeu). Mesure de départ : votre fichier Discoveries.json fait 409 Ko après environ une journée de jeu, MarketSeen.json 60 Ko, le fichier commun 166 Ko pour 664 faits (2026-11-08) ; à 100 joueurs sur quelques mois, le fichier commun peut dépasser des dizaines de Mo. À prévoir : (1) essai de charge avec des données inventées (100 000 puis 1 000 000 de faits) : temps de chargement, mémoire, temps d'enregistrement, temps de la fusion, coût par image des pages qui les lisent ; (2) chargement et enregistrement hors du fil du jeu, jamais tout le fichier à chaque relevé (écriture groupée, fichier temporaire puis remplacement) ; (3) index en mémoire par sorte, objet et zone, pour que les pages et info-bulles ne parcourent jamais tout ; (4) plafond par fait (compteurs bornés, valeurs distinctes déjà limitées à 32 par DEC-042) et fusion des relevés trop fins (cases de carte) ; (5) fichier commun découpé si besoin (par sorte ou par extension) et téléchargé seulement en cas de changement (somme de contrôle), compressé ; (6) outil de fusion : limites par joueur et par ticket (déjà : refus ≤ 5 %, volumes plausibles), temps de fusion mesuré, joueurs fusionnés par lots ; (7) page Découvertes et export : pagination et filtres au lieu de tout afficher ; (8) le format reste celui figé par DEC-042 (version 2) : le découpage et la compression se font autour, sans changer le contenu d'un fait. | Avec 2c du nettoyage X-76 (P8 compteurs, P9 données jetées), avant la 1.0.0 : l'essai de charge (1) d'abord, la suite selon ses chiffres |
| X-94 | Signalement de l'utilisateur (2026-11-08, capture) : sur la carte, au survol d'un trou de pêche « pêcheur · Niv. 95 · Détroit ouest de Zorgor », Tensho affiche « Point légendaire », « Demande le tome « Légendes géologiques du Coerthas », pas encore lu » et « Mineur : niv. 86, ce point : niv. 95 », alors que l'utilisateur peut y pêcher : un trou de pêche est pris pour un point légendaire de mineur (tome et métier d'un autre point). Vérifier d'où viennent le « légendaire », le tome et le métier pour les points de pêche (FishingSpot, pas GatheringPoint) | Pas tout de suite (demande de l'utilisateur) : avec X-86 carte |
| X-95 | Relevé du journal (2026-11-08, 20 h–21 h, en pêchant) : 22 images lentes (« Slow frame ») de 170 à 234 ms : carte 228 ms (mise en page 223 ms, vendeurs 59 ms), fenêtre principale 168 ms (accueil 94 ms), aide à la pêche 35 ms. À chercher : ce qui est recalculé à chaque image sur la carte et l'accueil | Avec X-93 (tenue face aux grosses données), après le nettoyage 2c |
| X-96 | Signalement de l'utilisateur (2026-11-08, capture) : aide à la pêche au Détroit ouest de Zorgor (pas en train de pêcher), sous « Fantôme des profondeurs » (Vents violents › Couvert, 1 proie) la ligne « météo : ? · météo d'avant : ? » reste vide : la météo actuelle et la précédente de la zone ne sont pas trouvées (prévision de cette zone de Dawntrail, ou zone de l'aide différente de la zone du joueur). Chercher pourquoi FishNow ne trouve pas la météo ici | À faire avec les autres points de pêche (X-85, X-94), demande de l'utilisateur |
| X-97 | Signalement de l'utilisateur (2026-11-08, en pêchant) : « les poissons de collection ne semblent pas être pris en compte ». À vérifier : prise d'un poisson collectionnable notée ou non dans les découvertes (le sac le range-t-il comme un gain ? valeur de collection), compte dans l'aide à la pêche (stock, ligne « vu ») et dans les listes ; préciser avec l'utilisateur où il l'a vu si le journal ne le montre pas | À faire avec les autres points de pêche (X-85, X-94, X-96), demande de l'utilisateur |
| X-76 | Demande du 2026-11-08 : nettoyage complet du code (« ne plus avoir de chose qui serait inutile maintenant », sans rien casser) : code mort, réglages inactifs, cassés ou plus cohérents avec le code actuel, réglages oubliés à ajouter ; ajout du 2026-11-08 : vérifier la cohérence de chaque réglage existant, ajouter ceux qui manquent, réorganiser les réglages au besoin, vérifier les info-bulles du code et surtout des réglages. Ajout du 2026-11-08 (au moment du nettoyage) : un réglage unique qui coupe toutes les sources autres que les données du jeu et celles observées en jeu ; pouvoir vider le cache et les données déjà observées, avec une sauvegarde faite avant. Ordre fixé par l'utilisateur (2026-11-08) : après X-75 puis les essais en jeu nécessaires, juste avant les documents `.md` ; ultracode accordé | À faire |

## Rappels de relevés en jeu, par moment

Relevés du document « a faire » (2026-11-08) : mandat de combat OK (X-37) ; loterie immobilière OK (N-04) ; sous-marin fait (fenêtres de résultat capturées, parseur P7 à coder) ;
pêche en mer faite (27 prises notées, « !!! » vu) ; pas encore : quête « vaincre X », récolte du jardin (timer), première remise d'objet de quête.


Consigne de l'utilisateur (2026-11-07) : les relevés se font en jouant ; au moment venu, il demande « rappelle-moi » et l'agent donne les étapes de ce moment-là. Mode développeur activé pour tous (le journal porte les lignes « Reading … »).

| Quand | Quoi faire | Ligne lue au journal | Point |
|---|---|---|---|
| Carte Tensho ouverte, en jouant | Rien de plus, laisser tourner quelques minutes | « Slow frame » (10 morceaux) | X-01 |
| Une alarme sonne dans le chat | Clic droit sur la ligne de l'objet | « Context menu opened » | X-03 |
| Mandat de combat | L'accepter, le commencer, aller vers ses cibles | « Reading monsterEvents » | X-37 |
| Quête « vaincre X » | Aller vers les monstres de la quête | « Reading monsterEvents » | X-40 |
| Dresseur | Page Dresseur : « Lire le bestiaire du wiki », puis en dresseur près d'un monstre d'une bête manquante : icône du dresseur au-dessus de lui | rien, à l'œil (capture si faux) | X-39 |
| Un gain de gils sans fenêtre (« +1 000 ») | Noter l'heure | « Gil other » | X-18 |
| Jardin, timer fini | Récolter | messages du jardin | #36 |
| L'aéronef revient | Ouvrir le panneau de voyage | relevé du voyage | T26 |
| Liste des servants ouverte (mode développeur) | Ouvrir la liste des servants | métier, niveau et fin des ventes de chaque servant (champs ClassJob, Level, MarketExpire) | X-76 P9 |
| Fin d'une mission (donjon, défi) | Finir la mission, ouvrir les coffres | récompenses de fin de mission et identifiant des coffres (P10 : DutyCompleted, ChestObjectId) | X-76 P10 |
| Pêche avec un leurre nageur | Lancer avec un leurre nageur, pêcher un poisson | indicateur 32 du lancer et objet du leurre | X-76 P6 |
| Loterie immobilière | Ouvrir la fenêtre de la loterie | relevé de la loterie | N-04 |
| Île sanctuaire | Ressources et atelier de l'île | « Reading island », « Reading islandAgenda » | N-07, N-10 |
| Pêche | Quelques prises | relevé de pêche | C30 |
| Rang S | Être dans sa zone | conditions des rangs S | N-12 |
| JcJ ou contenu classé, en tout dernier | Entrer : Tensho doit se couper | coupure compétitive | #39 |
| Monstres proches (0.795–0.800) | Près de monstres de vos listes, du bestiaire, d'un ALÉA ; en mage bleu après « Lire les sorts manquants » | carte et icônes | X-35 à X-41 |
| Zone pas entièrement découverte (une zone récente, ou un autre personnage) | Ouvrir la carte du jeu et la carte Tensho côte à côte | les mêmes parties vierges ; sinon une capture des deux | P11 |
| Combat contre des monstres (mode développeur) | Vaincre quelques monstres, ramasser leur butin ; en mage bleu apprendre un sort ; en dresseur capturer une bête | « Reading messages » | X-43 |
| Quête de cuisinier « Le pari » (mode développeur) | Ouvrir la carte Tensho sur le Tillac puis sur l'Entrepont | « Reading eventMarkers » | X-57 |
| Un objet lié dans le chat (butin, message d'un joueur) | Clic droit sur son nom : y a-t-il une entrée Tensho ? | « Context menu opened » | X-03 |

## Retenu pour plus tard

Demande de l'utilisateur (2026-11-06) : à garder, sans y travailler pour l'instant ; ne pas le commencer sans sa demande.

| Point | État relevé le 2026-11-06 |
|---|---|
| Île de Lunule (Croissant occulte) | Dans Tensho : les archives occultes dans les Collections (`CollectionKind.OccultLore`, feuille `MKDLore`) et un libellé pour la carte, rien d'autre. Données du jeu à étudier : feuilles `MKDSupportJob`, `MKDGrowData`, `MKDGrowDataSJob`, `MKDBNpcData`, `MKDChain`, `MKDData`, `MKDTrait`, structures `OccultCrescentMKDData`, `AgentMKDInfo`, `AgentMKDSupportJob`. Occult Crescent Helper et Phantom Job Switcher sont sous AGPL : lecture seule, jamais copiés. |

## Écartés à la revue

Ne pas refaire sans nouvelle demande :

| Point | Raison donnée |
|---|---|
| P21 raccourcis, P23 amis, P24 téléportation par nom, P25 mini-carte | Refusés par l'utilisateur |
| P26 clignotement, P27 teinte d'icône | Non retenus après explication |
| C23 barres de raccourcis | Refusé |
| I14 annuler un tri | « Aucun intérêt » |
| I-15 recherche dans les fenêtres d'inventaire | « Pas l'intérêt » |
| N-13 meubles visibles, X-14 aménagement | Non retenus après explication |
| Prix moyen récent avant de coller | Non coché, donc rejeté |
