# Propositions et choix provisoires de l'étape g) — à revoir ensemble

Ce document est tenu pendant le développement de l'étape g) (les écarts de GAPS.md). On y note les idées repérées en codant et les
choix faits à titre provisoire, pour les revoir ensemble avec l'utilisateur (étape h et décisions de la fin). Rien ici n'est encore
décidé. Les choix provisoires sont aussi rappelés dans la [ROADMAP](ROADMAP.md), « Décisions reportées à la fin ».
Relu le 2026-11-03 (0.446) : une alternative faite depuis est marquée « FAIT x.y » dans sa colonne ; il reste alors le choix lui-même à confirmer.
**DÉCIDÉ 2026-11-03 (DEC-026)** : tous les choix provisoires ci-dessous sont confirmés par l'utilisateur, sauf X-13 (envoi au chat sur clic
ajouté en 0.447) ; leurs alternatives restent des idées. X-04 : simple lien (0.447). Écarts : A-03 / A-04 étudiés en séance ; N-06 et N-14 :
source communautaire à chercher, licence d'abord ; N-08 : journal avec durées tapées (0.448).

## Décisions

<!-- gen:decisions:g -->
Les choix notés dans ce fichier sont tranchés : DEC-023, DEC-026, DEC-032, DEC-033, DEC-035, DEC-045, DEC-048 ([DECISIONS](DECISIONS.md)). Texte d'origine des sections : [archive](archive/DECISIONS-EN-ATTENTE-2026-11-09.md). Ce qui reste ouvert est listé dans [ROADMAP](ROADMAP.md), « À décider / à revoir ».
<!-- /gen:decisions:g -->

## Idées repérées en codant (non faites)

| Repérée en | Idée | Pourquoi pas encore |
|---|---|---|
| 0.344 | Alerte sur le plafond hebdomadaire des mémoquartz (en plus du plafond total) | Hors du choix de l'utilisateur pour I-02 |
| 0.345 | « Où est cet objet ? » sur tous les personnages (infobulle et fiche d'objet), en étendant ItemHoldings | Gros changement des stocks ; à faire après validation en jeu de I-03 |
| 0.345 | Gils des servants dans la vue des personnages | Le jeu ne les donne qu'à l'ouverture de la cloche ; à sonder |
| 0.347 | Liste de tous les objets annotés (page Objets) avec filtre par étiquette | Petit ; à faire avec les autres listes d'objets (I-12) |
| 0.346 | Sous-cotation : afficher aussi le prix moyen de vente récent avant de coller (garde-fou contre une offre trop basse) | Demande un appel Universalis de plus |
| 0.345 | Ne réécrire le fichier d'un personnage que si ses quantités ont changé | Économie d'écritures disque ; aujourd'hui une écriture toutes les 2 minutes par rangement relevé d'office |

Tri des propositions des quatre audits (2026-11-05) : [PROPOSALS-TRIAGE](PROPOSALS-TRIAGE.md).
Revue du 2026-11-05 avec l'utilisateur : [plan global](GLOBAL-PLAN.md). I-15, N-13 et X-14 sont écartés ; N-10 est repris.

## Écarts laissés pour une séance avec l'utilisateur

| Écart | Raison |
|---|---|
| I-05 (annonces des servants au marché) | Première version FAITE 0.508, à essayer en jeu ; la suite plus bas |
| A-03, A-04 (solveur Raphael et suggestions adaptatives) | A-03 FAIT 0.465 (DEC-028, en option). A-04 FAIT 0.509, à essayer en jeu |
| N-06 (escouade, composition des missions) | FAIT 0.451 (DEC-027) : planificateur depuis les feuilles du jeu et GcArmyManager ; pourcentage « ≈ » jusqu'à la sonde en jeu (TESTING 0.451) ; affinités non comptées ([preuve](../evidence/0.448-n06-n14-sources.md)) |
| N-07 (île sanctuaire, carte des ressources et matériaux) | Carte faite en 0.667 (unité des positions à comparer sur l'île) ; matériaux des créations et d'un agenda faits en 0.673, sans vos stocks ([preuve](../evidence/0.673-island-materials.md)) |
| N-10 (atelier de l'île, solveur) | Gros : lecture de l'offre et de la demande du jeu et solveur ; à faire avec l'utilisateur, voir plus bas |
| N-04 (loterie immobilière) | État des mises du joueur : structure du jeu non trouvée dans FFXIVClientStructs (à sonder en jeu) |
| N-12 (conditions d'apparition des rangs S), N-13 (meubles visibles), X-14 (outils d'aménagement) | Règles communautaires par marque, lecture des meubles et correctifs d'affichage : sources et risques à décider ensemble |
| N-08 (journal de jardinage) | FAIT 0.448 (décidé le 2026-11-03, DEC-026) : journal avec durées tapées par le joueur, page Rappels |
| N-14 (planificateur de croisements de jardinage) | FAIT 0.450 (DEC-027) : paires MIT de nick75g reliées aux objets par la feuille Item anglaise, affichées dans la langue du jeu ([preuve](../evidence/0.448-n06-n14-sources.md)) |
| I-15 (recherche dans les fenêtres d'inventaire du jeu) | Touche l'interface du jeu : à faire avec l'utilisateur, voir plus bas |

## Écarts laissés pour une séance en jeu (2026-11-04)

| Point | Pourquoi pas en autonomie | Ce qu'il faudrait |
|---|---|---|
| I-15, recherche dans les fenêtres d'inventaire | Passe sur des pointeurs natifs à chaque image, 15 inconnues (numéros d'onglets, noms des grilles de servants, ordre des cases). Coder à l'aveugle peut faire planter le jeu | D'abord une sonde en lecture seule, avec l'utilisateur devant les fenêtres |
| N-10, atelier de l'île | Fait sans le jeu en 0.671 et 0.672, inconnues listées dans `docs/evidence/0.671-island-workshop.md` | Essais et capture devant l'agenda de l'île ; prévision de la semaine plus tard |
| I-05, suite | Relevé des offres du jeu, vue de tous les servants | Après un essai de la 0.508 |
| Dresseur, suite | Mis de côté par l'utilisateur | Sonde déjà en place (0.501) |

## Proposition X-100 : regroupement des outils et des réglages (2026-11-10) — **validée (DEC-048)** ; n° 9 : automatique coupé par défaut (DEC-048)

Constat (0.991) : la navigation a 8 groupes ; « Progression » compte 16 pages et « Monde » 11 ; les réglages sont dans 7 onglets et aussi sur les pages (MapView 92, Paramètres 44, Avancé 24, panneaux des listes 13…, 320 réglages au total, docs/project/SETTINGS.md).

| Groupe proposé | Pages | Changement |
|---|---|---|
| Objets | Objets, Rangements, Tri, Marché | Lexique part dans « Références » |
| Production | Artisanat, Récolte, Pêche en mer, Île sanctuaire, Enchaînement de macros | inchangé (sans les livraisons) |
| Monde | Carte, Météo, ALÉA, Marchands, Butin des contenus, Butin des monstres, Découvertes | sans les minuteurs |
| Suivi (nouveau) | Alarmes, Rappels, Jardin, Compagnie libre | les minuteurs et rappels ensemble |
| Progression | Quêtes, Fêtes, Collections, Mandats, Carnet de chasse, Peuples alliés, Grandes compagnies, Livraisons (spéciales et sur mesure), Équipement, Chocobo | 10 pages au lieu de 16 ; les deux livraisons réunies |
| Contenus (nouveau, X-90) | Eurêka/Bozja/Croissant, Donjons profonds, Reliques, Carnaval masqué, Dresseur, plus tard tableaux stratégiques | une famille par contenu (X-92) |
| Gold Saucer | Revue de mode, Triple Triade, Mini Cactpot | inchangé |
| Références (nouveau) | Lexique, Aide, Crédits | ce qu'on consulte |
| Préférences | Paramètres, Diagnostics (développeur) | Accueil et Mes outils restent en tête, hors groupe |

Réglages : chaque réglage reste aussi à côté de son outil (comme aujourd'hui), et la page Paramètres les regroupe par les mêmes groupes que la navigation :
Général (langue, thème, polices, discussion, sons) · Panneaux à côté des fenêtres du jeu (récolte, artisanat, collectionnable, ventes des servants, recherche de mission, revue de mode, Triple Triade, plateau du dresseur…) · Carte · Suivi et notifications (alarmes, rappels, Discord, barre du serveur : monnaies, gils) · Données et sources (sources communautaires, cache, découvertes) · Avancé · Débogage.
Plus un champ de recherche en haut des Paramètres qui trouve n'importe quel réglage par son nom (les 320) et ouvre son onglet.
| 11 | Monstres proches, autres usages (X-40) ; aujourd'hui : vos listes, carnet de chasse, ALÉA, mage bleu, collections manquantes, dresseur, cibles de mandat | Ajouter (chacun une couche de la carte, cochée dans Paramètres › Carte et sur sa page) : a) le monstre fait tomber un ingrédient d'une de vos listes d'artisanat (butin vu en jeu, puis wiki) ; b) cible d'un livre de relique en cours (Zodiaque : « vaincre N … ») ; c) le monstre fait tomber un objet de vos souhaits | Seulement a) ; ou rien |
