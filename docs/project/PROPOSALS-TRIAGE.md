# Tri des propositions d'audit

Tri du 2026-11-05, à la version 0.539.0, des 95 propositions restées ouvertes dans [MAP-PROPOSALS](MAP-PROPOSALS.md), [ITEMS-PROPOSALS](ITEMS-PROPOSALS.md), [CRAFT-GATHER-PROPOSALS](CRAFT-GATHER-PROPOSALS.md) et [TOOLS-PROPOSALS](TOOLS-PROPOSALS.md). Le détail de chaque proposition reste dans ces documents, sous le même numéro.

**Méthode et limites.** L'état vient d'une recherche dans le code et les textes de l'interface, sans lire chaque fonction ni essayer en jeu : un « non fait » peut être en partie fait. Valeur et taille (S, M) sont les estimations des audits. Le classement est une recommandation de l'agent : rien n'est décidé tant que l'utilisateur n'a pas choisi. Aucune de ces propositions n'est exigée pour la 1.0.0 ([PUBLISHING](PUBLISHING.md)).

| Classe | Nombre | Recommandation |
|---|---|---|
| [Déjà faites](#déjà-faites) | 3 | Marquées faites |
| [Lot 1 : valeur haute, petites](#lot-1--valeur-haute-petites) | 8 | À faire en premier |
| [Lot 2 : valeur moyenne, petites](#lot-2--valeur-moyenne-petites) | 10 | Ensuite |
| [Lot 3 : valeur haute, moyennes](#lot-3--valeur-haute-moyennes) | 12 | Une par une, selon l'envie |
| [À sonder en jeu d'abord](#à-sonder-en-jeu-dabord) | 21 | Une séance de relevés avant de coder |
| [Après la 1.0.0](#après-la-100) | 33 | Gardées comme idées |
| [À écarter](#à-écarter) | 8 | Ne pas faire, sauf avis contraire |

## Déjà faites

| N° | Proposition | Preuve dans le code |
|---|---|---|
| C8 | Plan de PR du panneau de récolte : quantité et effets | Plan par nœud et effets du lieu (0.515 à 0.517, `Gathering/GatherNodeEffects.cs`) |
| C34 | Missions d'approvisionnement du jour | `Items/GcSupplyToday.cs` (0.469) |
| P14 | Mesurer le temps d'affichage de la carte | Ligne « Map » des diagnostics (0.489, `MapViewWindow.cs`) ; l'allègement éventuel reste à décider sur mesure |

## Lot 1 : valeur haute, petites

Taille S, aucune sonde avant de coder.

| N° | Proposition | Pourquoi d'abord |
|---|---|---|
| P1 | Zoom de la carte à l'ouverture, réglable et gardé | Geste répété à chaque ouverture |
| P2 | Taille des étiquettes séparée de celle des icônes | Lisibilité de la carte |
| I0 | Compter les pièces portées dans « possédé » | Corrige un compte faux ; à contrôler en jeu après coup |
| C2 | Essayer ses séquences sauvegardées sur toutes les recettes | Réutilise le simulateur |
| C3 | Simuler un autre métier sans le porter | Évite de changer de job pour simuler |
| C4 | Seuils d'attributs d'une séquence | Dit si une macro passe avec son équipement |
| T3 | Quêtes : colonnes Niveau et Zone, tri et filtre | Données déjà lues |
| T4 | Monstres : drapeau sur la position du monstre | Évite un drapeau au centre de la carte |

## Lot 2 : valeur moyenne, petites

Taille S, aucune sonde.

| N° | Proposition |
|---|---|
| P5 | Molette de la carte réglable |
| P20 | Bouton « Couches » : tout afficher ou masquer |
| C19 | Où sont mes matériaux, par rangement |
| C24 | Besoin restant de la préparation dans les aides de récolte et de pêche |
| C31 | Météo de plusieurs zones en un tableau |
| C32 | Commandes « /tensho recolte » et « /tensho recette » |
| I16 | Commandes « /tensho objet » et « /tensho prix » |
| T17 | Rappels cochés d'eux-mêmes quand le jeu le dit |
| T19 | Alarmes : modifier le nom et l'heure, « Me rappeler dans 5 min » |
| T20 | Alarmes : sonner aussi à l'heure exacte |

## Lot 3 : valeur haute, moyennes

| N° | Proposition | Note |
|---|---|---|
| I1 | Signaler et effacer le relevé d'un servant renvoyé | Taille S à M |
| I3 | Prix des marchands PNJ dans le coût de préparation | Taille S à M ; un contrôle en jeu sur un objet connu |
| C0 | Qualité visée dans le panneau du carnet | En partie (0.419) |
| C5 | Suivre sa séquence pendant la synthèse | Information seulement, sans jouer d'action |
| C7 | Points de récolte ouverts maintenant ou bientôt | En partie (0.415) |
| C9 | « Possible maintenant ? » dans l'aide à la pêche | En partie (0.396) |
| T0 | Alarmes en heure réelle : chaque jour, chaque semaine, à chaque remise à zéro | En partie : `AlarmRepeat` n'a que Once et EorzeaDaily |
| T10 | Accueil : bloc « Aujourd'hui » | Une décision : quelles lignes par défaut (recommandé : toutes, chacune masquable) |
| T11 | Quêtes disponibles maintenant, quêtes de la zone | Une colonne de la feuille Quest à sonder |
| T12 | Chemin vers une quête : prérequis restants | |
| T14 | Équipement : préparer le plan d'un autre job | |
| T15 | Revue de mode : tenue de la semaine, essayée en un clic | Sur clic seulement |

## À sonder en jeu d'abord

Une donnée n'est pas prouvée : pas de code avant le relevé (règle du projet, aucune donnée inventée). À regrouper avec [l'issue #23](https://github.com/Shigu330/Tensho-Assistant/issues/23).

| Domaine | N° |
|---|---|
| Carte | P6 unité des distances ; P7 éthérites non harmonisées ; P11 parties non découvertes ; P17 marqueurs des mandats ; P19 tests de la carte ; P22 variantes d'icônes de quête |
| Objets et marché | I4 requêtes Universalis par lots ; I10 cristaux des servants |
| Artisanat et récolte | C6 recettes à niveau ajusté et Exploration cosmique ; C13 chance HQ ; C22 projets de l'atelier ; C27 classe du point ; C28 durée de l'intuition ; C29 harpon dans la page Récolte ; C30 relevé des prises ; C33 séance de relevés |
| Autres outils | T5 séance de relevés ; T13 « Débloqué par » ; T21 expérience de tous les jobs ; T26 dernier voyage des dirigeables ; T27 actions de compagnie |

## Après la 1.0.0

Utiles sans être prioritaires. « En partie » : une première version existe.

| Domaine | N° |
|---|---|
| Carte | P3 éthérite la plus proche du drapeau (en partie) ; P9 ALEA ; P10 recherche sur la carte (en partie, page Cartes) ; P12 noms à côté des icônes (en partie : noms de lieux, étiquettes de récolte) ; P15 état des lectures ; P16 clic gauche pour le drapeau |
| Objets et tri | I5 prix du centre de données ; I8 liens Universalis, Garland, Teamcraft (en partie) ; I9 copier le nom, lien d'objet ; I11 Banque en liste triable ; I14 annuler le dernier tri ; I15 partager un modèle de tri ; I18 prix moins 1 depuis la page Marché (en partie) ; I19 nom anglais ; I20 cases déplacées dans l'aperçu ; I21 filtre de job dans les règles ; I22 calculs à chaque image |
| Artisanat et récolte | C12 qualité initiale selon les ingrédients HQ (en partie, valeur saisie) ; C14 fiche de recette complète (en partie) ; C15 simulateur plus direct ; C16 bibliothèque des séquences (en partie) ; C17 options d'export (en partie) ; C18 chaîne de fabrication ; C21 « Que puis-je fabriquer ? » ; C35 aide à la pêche hors de Draw ; C36 une aide illisible le dit |
| Autres outils | T16 une ligne par servant (en partie) ; T18 balises Discord (en partie) ; T23 vaisseaux par compagnie ; T24 un salon Discord par catégorie ; T25 plans nommés par job ; T28 obtenus récemment ; T29 table du Mini Cactpot visible (en partie) |

## À écarter

| N° | Proposition | Raison |
|---|---|---|
| P21 | Raccourcis clavier dans la carte | Valeur basse, risque de conflit avec les touches du jeu |
| P23 | Amis proches sur la carte | Valeur basse ; montre d'autres joueurs, à éviter avant l'examen Dalamud |
| P24 | Commande de téléportation par nom | Valeur basse ; « Y aller » couvre le besoin |
| P25 | Mini-carte Tensho | Seconde carte à maintenir, le jeu en a une |
| P26 | Position mise en évidence au survol des listes | Valeur basse |
| P27 | Couleur réglable par icône | Valeur basse |
| C10 | Chrono de touche à chaque lancer | Demande une sonde, et les plugins de pêche existants le font déjà |
| C23 | Action suivante sur les barres de raccourcis | Dessin sur l'interface du jeu à chaque image, sensible aux mises à jour ; C5 donne l'information sans ce risque |
