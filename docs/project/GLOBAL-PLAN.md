# Plan global

Plan du 2026-11-05, à la version 0.539.0, issu de la revue faite avec l'utilisateur et réorganisé à sa demande ; détail de chaque point : [PROPOSALS-TRIAGE](PROPOSALS-TRIAGE.md). **En cours depuis le 2026-11-05 (feu vert de l'utilisateur).** L'ordre des phases et des points est une proposition de l'agent.

**La 1.0.0 vient après tout le plan** (décision de l'utilisateur, [DEC-037](DECISIONS.md)) : il veut un plugin complet, et accepte qu'une soumission jugée trop grosse puisse être refusée ([PUBLISHING](PUBLISHING.md), point 6).

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
| Ensuite | Version 1.0.0 et soumission | | |
| Après la 1.0.0 | C10, chrono de touche | 1 | |

Total avant la 1.0.0 : 99 points. Le bouton pour afficher ou cacher des icônes sur la carte existe déjà (rapport de l'utilisateur) : la demande est retirée.

## Déroulement en jeu (phases 5 et 6)

1. L'agent donne **un seul point** : quoi faire dans le jeu, en quelques lignes.
2. L'utilisateur le fait quand il veut, puis répond ; l'agent lit le journal.
3. S'il y a du code à écrire ou une erreur, l'agent corrige, puis on refait ce même point.
4. Seulement alors, le point suivant. Une session peut s'arrêter entre deux points.

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
| I22 | Calculs faits à chaque image (reportés à e) : coût de préparation et historique du marché | S |  |
| C0 | ~~A-01, partie anticipée : choisir la qualité visée dans le panneau du carnet~~ déjà présent | S |  |
| C2 | ~~Essayer ses séquences sauvegardées sur toutes les recettes~~ fait en 0.552 | S |  |
| C3 | ~~Simuler un autre métier sans le porter : derniers attributs relevés par métier et attributs prévus de la page Équipement~~ fait en 0.554 | S |  |
| C4 | ~~Seuils d'attributs d'une séquence : Habileté, Contrôle et PS minimum~~ fait en 0.553 | S |  |
| C9 | ~~R-02, partie anticipée : « possible maintenant ? » dans l'aide à la pêche~~ fait en 0.568 | S |  |
| C12 | Qualité initiale calculée à partir des ingrédients HQ choisis | S |  |
| C14 | A-12, partie anticipée : fiche de recette complète (livre de maître, synthèse rapide, HQ, prérequis, écart chiffré) | S |  |
| C15 | ~~Simulateur plus direct : choix de la recette dans la page, résultat recalculé tout seul, glisser-déposer~~ fait en 0.572 | S |  |
| C17 | ~~Options d'export des macros : attentes séparées (X-12), son, écho final, icône~~ fait en 0.573 | S |  |
| C19 | ~~Où sont mes matériaux : détail par rangement dans la préparation et la chaîne~~ fait en 0.559 | S |  |
| C24 | ~~Besoin restant de la préparation dans les aides de récolte et de pêche~~ fait en 0.560 | S |  |
| C31 | ~~Météo de plusieurs zones en un tableau (zones favorites)~~ fait en 0.563 | S |  |
| C32 | ~~Commandes « /tensho recolte » et « /tensho recette » (suite de I16)~~ fait en 0.555 | S |  |
| C35 | Aide à la pêche : lectures du jeu et premières tables hors de Draw | S |  |
| C36 | R-14, partie artisanat et récolte : une aide illisible le dit au lieu de disparaître | S |  |
| T3 | ~~Quêtes : colonnes Niveau et Zone, tri et filtre de niveau~~ fait en 0.550 | S |  |
| T4 | ~~Monstres : drapeau sur la position du monstre, pas au centre de la carte~~ fait en 0.551 | S |  |
| T16 | ~~Tâches de servants : une ligne par servant, avec alarme~~ fait en 0.570 | S |  |
| T17 | ~~Rappels cochés d'eux-mêmes quand le jeu le dit~~ fait en 0.558 | S |  |
| T18 | ~~Discord : heure lue dans le fuseau de chaque membre, et balises [zone], [objet], [et]~~ fait en 0.564 | S |  |
| T19 | ~~Alarmes : modifier le nom et l'heure, et « Me rappeler dans 5 min »~~ fait en 0.557 | S |  |
| T20 | ~~Alarmes : sonner aussi à l'heure exacte~~ fait en 0.556 | S |  |
| T23 | Compagnie libre : vaisseaux groupés par compagnie, ancien relevé effaçable, valeur du dernier butin | S |  |
| T28 | Collections : « Obtenus récemment » et menu des sources pour les emotes | S |  |
| T29 | ~~Mini Cactpot : table des gains prouvée et visible~~ fait en 0.571 | S |  |

C12 passe en phase 5 : sa règle de calcul doit être comparée à la qualité initiale lue en jeu.

## Phase 3 : propositions moyennes

| N° | Proposition | Taille | Précision |
|---|---|---|---|
| P10 | Recherche sur la carte (lieux, éthérites, PNJ, quêtes, ressources) | M | Depuis la section Carte du plugin, pas depuis la carte. |
| P12 | Noms écrits à côté des icônes, comme la carte du jeu | M |  |
| I1 | ~~Signaler et effacer le relevé d'un servant renvoyé ou d'un ancien rangement~~ fait en 0.576 | S-M |  |
| I3 | ~~Prix des marchands PNJ dans le coût de préparation et sur la page Marché~~ fait en 0.575 | S-M |  |
| I11 | Vue en liste triable de la Banque | S-M |  |
| I21 | Filtre de job et de set d'équipement dans les règles de tri | M |  |
| C5 | ~~Suivre sa séquence pendant la synthèse : action suivante et écart avec le plan~~ fait en 0.589 | M | Information seulement, aucune action jouée. |
| C7 | ~~R-13, partie anticipée : points à horaire ouverts maintenant ou bientôt~~ fait en 0.588 | S-M |  |
| C16 | ~~Bibliothèque des séquences : recherche, renommer, dupliquer, copier et coller en texte (A-08, partie texte)~~ fait en 0.590 | S-M |  |
| C18 | ~~Chaîne de fabrication : boutons par étape, étapes identiques regroupées, cases « fait »~~ fait en 0.591 | S-M |  |
| C21 | ~~« Que puis-je fabriquer ? » avec mes stocks~~ fait en 0.592 | M |  |
| T0 | ~~Alarmes en heure réelle : à la main, chaque jour, chaque semaine ou à chaque remise à zéro (X-10, partie anticipée)~~ fait en 0.577 | S-M |  |
| T10 | Accueil : bloc « Aujourd'hui » et tuiles au choix | M |  |
| T11 | ~~Quêtes « disponibles maintenant » et quêtes de la zone actuelle~~ fait en 0.582 | M |  |
| T12 | ~~Chemin vers une quête : prérequis restants, nombre et prochaine à faire~~ fait en 0.583 | M |  |
| T14 | ~~Équipement : préparer le plan d'un autre job que celui porté~~ fait en 0.580 | M |  |
| T15 | ~~Revue de mode : tenue de la semaine choisie, essayée en un clic, avec l'endroit de chaque pièce~~ fait en 0.581 | M | Sur clic seulement. |
| T24 | Discord : un salon par catégorie (plusieurs webhooks) | M |  |
| T25 | ~~Équipement : plusieurs plans nommés par job~~ fait en 0.593 | S-M |  |

## Phase 4 : idées, sonde web et sources sans le jeu

| Point | Note |
|---|---|
| I4, prix Universalis par lots | Sonde par une requête web, sans le jeu |
| N-12, conditions des rangs S | Choisir les sources, licence d'abord |
| Alerte du plafond hebdomadaire des mémoquartz | |
| « Où est cet objet ? » sur tous les personnages | Gros changement des stocks |
| Liste des objets annotés, filtre par étiquette | |
| Écrire le fichier d'un personnage seulement s'il change | Technique |

## Phase 5 : relevés en jeu, puis code

Une donnée n'est pas prouvée : le relevé précède le code. Ordre : par activité, pour ne pas changer sans cesse d'endroit dans le jeu.

### Corrections

| Où | Ce qu'il faut relever | Issue |
|---|---|---|
| Jardin | Un entretien et une récolte, pour reconnaître leurs messages ; puis retirer les lignes de journal de capture | #36 |
| Récolte | La régénération de PR pendant une récolte | #37 |

### Récolte et pêche

| N° | Proposition | Taille | Précision |
|---|---|---|---|
| C33 | Une seule séance de relevés en jeu pour l'artisanat et la récolte, avant de coder | S-M |  |
| C27 | Classe du point (normal, éphémère, légendaire…) et livre de folklore requis | S |  |
| C28 | Durée de l'intuition et compte à rebours | S |  |
| C29 | R-15, partie anticipée : poissons au harpon dans la page Récolte | S-M |  |
| C30 | Relevé local de vos prises et temps de touche personnels | M |  |

### Artisanat

| N° | Proposition | Taille | Précision |
|---|---|---|---|
| C6 | Recettes à niveau ajusté, à qualité imposée et Exploration cosmique | M-L |  |
| C13 | Chance HQ d'une qualité finale | S |  |
| C22 | Projets de l'atelier de compagnie libre : matériaux par pièce et par phase | M |  |

### Carte

| N° | Proposition | Taille | Précision |
|---|---|---|---|
| P6 | Coordonnées du personnage toujours visibles et distances | S | Coordonnées du drapeau sous son icône de bord de carte, pas dans un coin ; avec réglage. |
| P7 | Éthérites non harmonisées signalées et coût au survol | S |  |
| P11 | Parties non découvertes masquées comme sur la carte du jeu | M | Avec réglage. |
| P17 | Relevé du type de marqueur des mandats et de leurs zones | S |  |
| P22 | Noms des variantes d'icônes de quête (71141 / 71151) | S |  |

### Objets

| N° | Proposition | Taille | Précision |
|---|---|---|---|
| I10 | Cristaux et gils des servants dans le relevé du servant | M |  |

### Outils

| N° | Proposition | Taille | Précision |
|---|---|---|---|
| T5 | Une séance de relevés en jeu pour ces outils, avant de coder | S |  |
| T13 | Contenus et quêtes : « Débloqué par » et « Débloque » | M |  |
| T21 | Personnage : niveaux et expérience de tous les jobs | S |  |
| T26 | Dirigeables : trajet du dernier voyage reconstitué | S |  |
| T27 | Actions de compagnie actives : heure de fin et alarme | M |  |

### Gros chantiers et idée

| Point | D'abord | Précision |
|---|---|---|
| Gils des servants dans la vue des personnages | Sonde à la cloche | |
| I-05, suite des ventes des servants | Essayer la 0.508 | |
| N-04, loterie immobilière | Sonde : structure du jeu à trouver | |
| N-07, ressources de l'île | Vérifier l'unité des positions | |
| N-10, atelier de l'île | Relire la conception, puis sonde | Mis de côté par DEC-033 ; repris à la revue |
| Panneau du Dédale et suite du dresseur | **Règle Dalamud** : résumé, accord de l'utilisateur, puis contact de l'équipe (DEC-032) | Protections demandées par l'utilisateur : bloqué en JcJ, JcE seulement, pas de classé ; informations seulement |

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
