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
| P11 | Parties non découvertes masquées comme sur la carte du jeu | M | Avec réglage. |
| ~~P17~~ fait en 0.652 | Relevé du type de marqueur des mandats et de leurs zones | S | Le type ne dit rien : c'est l'icône 60492 ou le nom du mandat. |
| ~~P22~~ fait en 0.665 pour les variantes 1 et 5 | Noms des variantes d'icônes de quête (71141 / 71151) | S | Les autres gardent leur numéro ; à confirmer en jeu. |

### Objets

| N° | Proposition | Taille | Précision |
|---|---|---|---|
| I10 | Cristaux et gils des servants dans le relevé du servant | M |  |

### Outils

| N° | Proposition | Taille | Précision |
|---|---|---|---|
| T5 | Une séance de relevés en jeu pour ces outils, avant de coder | S |  |
| ~~T13~~ fait en 0.648 pour les contenus que les données nomment | Contenus et quêtes : « Débloqué par » et « Débloque » | M | Environ 130 contenus sur plus de 700 ; à compléter si une source sûre est trouvée. |
| ~~T21~~ fait en 0.646 | Personnage : niveaux et expérience de tous les jobs | S |  |
| T26 | Dirigeables : trajet du dernier voyage reconstitué | S |  |
| ~~T27~~ fait en 0.651 | Actions de compagnie actives : heure de fin et alarme | M |  |

### Gros chantiers et idée

| Point | D'abord | Précision |
|---|---|---|
| Gils des servants dans la vue des personnages | Sonde à la cloche | |
| I-05, suite des ventes des servants | Essayer la 0.508 | |
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
| X-01 | Lourdeur : trouver ce qui bloque une image de Tensho (120 à 230 ms toutes les quelques minutes, 2,6 s une fois) et le sortir du dessin | En cours : relevé « Slow frame » en 0.745 ; aide à la récolte (97 ms) calculée hors de l'image en 0.747, avec la mesure de la fenêtre principale et des autres fenêtres ; les images de 110 à 160 ms venaient du passage des pages (mode développeur), espacé de deux heures en 0.748 ; pause de 5 s du moteur .NET au rechargement (mode développeur) ; panneau de l'atelier à 1,7–7,6 s depuis les zones (0.757–0.758, ma régression) : regroupement calculé une fois par seconde en 0.760 ; carte : « map overlay layout » 100 à 220 ms une fois à l'ouverture, à mesurer plus finement plus tard |
| X-02 | Données gardées plus longtemps, relues seulement si absentes ; réglages qui complètent « Cache & péremption » | Durées par défaut plus longues et « Jamais » renommé « Seulement si absent, ou sur Actualiser » en 0.750. Accord donné. Relu : quêtes et collections ont déjà une copie par personnage sur disque ; les carnets, recettes et équipement se relisent légèrement, page ouverte. À faire : durées par défaut plus longues, « Jamais » renommé « seulement si absent ou sur Actualiser », et ce que X-01 trouvera de lourd |
| X-03 | Lien « Ouvrir dans Tensho » d'une alarme : n'ouvre rien | Aucun clic n'arrivait (journal de 0.743). Dalamud ne trouve plus le lien après un rechargement du plugin : réenregistré au chargement en 0.759 |
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
| X-16 | Plan de PR : à rendement égal, préférer les actions qui durent toute la récolte, pour moins de clics | À faire |

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
