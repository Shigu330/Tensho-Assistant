# Parcours des essais en jeu (phase 6)

> Document de travail (vivant, en français ; traduit avant la 1.0.0 avec les autres, décision du 2026-11-08).

Construit le 2026-11-09 (0.1010) à partir des **782 essais non cochés** de [TESTING.md](TESTING.md), rangés par activité pour enchaîner
les essais au même endroit. Ordre fixé par l'utilisateur : ce qui est libre d'abord, puis ce qui dépend d'un horaire et les instances, le JcJ
en tout dernier ; un point à la fois. La source reste TESTING.md : un essai réussi s'y coche (le numéro de ligne indiqué est celui du
2026-11-09 ; chercher la version si le fichier a bougé). Une étape impossible (pas de sous-marin, pas de client anglais…) se rapporte
« non essayé », pas « OK ». Le mode développeur reste actif pendant toute la campagne : le journal note ce que l'agent doit lire.

| Étape | Essais |
|---|---|
| 0. Après le patch 7.58 | 15 |
| 1. Fenêtres et réglages de Tensho | 122 |
| 2. Objets, inventaire, servants, marché | 125 |
| 3. Collections, carnets, succès, mode | 45 |
| 4. Artisanat | 95 |
| 5. Récolte et pêche | 107 |
| 6. Carte, téléportation, monde ouvert | 126 |
| 7. Compagnie libre, logement, île | 56 |
| 8. Mini-jeux, escouade, chocobo, dresseur | 20 |
| 9. Rappels, alarmes, minuteurs, Discord | 55 |
| 10. Missions et instances | 28 |
| 11. JcJ et classé | 4 |

## Étape 0 : après le patch 7.58, avant tout le reste

Détail : [docs/evidence/0.1008-game-update-inventory.md](../evidence/0.1008-game-update-inventory.md) (risques et autotest d'avant le patch).

- [x] Dalamud à jour pour le patch 7.58 ; Tensho se charge ; `/tensho` affiche la dernière version.
- [x] L'agent lit les lignes « Selftest » et « Game data check » de dalamud.log et les compare à l'autotest d'avant le patch.
- [x] Enregistreur d'artisanat (interception de `UseAction`) : une action d'artisanat est notée.
- [x] Enchaînement de macros (interception de `ExecuteMacro`) : une chaîne de deux macros va jusqu'au bout.
- [x] Écriture d'une macro par Tensho : texte, icône, macro enregistrée.
- [x] Téléportation depuis la carte Tensho vers l'éthérite proposée.
- [x] Changement d'ensemble d'équipement depuis Tensho.
- [x] Changement de titre depuis Tensho.
- [x] Alarme du jeu : affichée et son joué ; commande envoyée au chat (alarme, annonce de chasse).
- [x] Carte du jeu remplacée par la carte Tensho, puis rendue normale (touche M, plugin déchargé).
- [x] Cabine d'essayage, carnet d'artisanat et carnet de récolte ouverts depuis Tensho.
- [x] Progression d'un succès demandée et affichée.
- [ ] Relevés en attente, au fil du jeu : la liste et le moment de chacun sont dans la seule table des
  [rappels de relevés en jeu](GLOBAL-PLAN.md#rappels-de-relevés-en-jeu-par-moment) du plan global.
- [ ] Panneau du Dédale bestial (X-109) devant le plateau, puis disparu au début du combat.
- [ ] Couches des monstres proches (X-40) : bouclier sur les cibles d'un livre du Zodiaque, cœur sur un monstre qui fait tomber un souhait.

## Étape 1 : Fenêtres et réglages de Tensho (122)

N'importe où, sans activité de jeu : réglages, langues, caches, crédits, aide, rechargement.

- [ ] **0.258** Tout remettre par défaut : réglages gardés corrects (TESTING l. 123)
- [ ] **0.320** Crédits : filtre et boutons copier (TESTING l. 174)
- [ ] **0.321** Réglages Artisanat gardés après rechargement (TESTING l. 179)
- [ ] **0.325** Infobulles des filtres, recherches et tris (TESTING l. 190)
- [x] **0.327** Nombre de résultats à 50, ↺ remet 30 (TESTING l. 204)
- [ ] **0.327** Dernière page rouverte après rechargement (TESTING l. 205)
- [x] **0.336** Crédits : LuminaSupplemental deux fois (TESTING l. 248)
- [ ] **0.537** Barres PR, usure, tâches cachées par fenêtres (TESTING l. 520)
- [ ] **0.550** Quêtes : colonnes, tri, plage gardée (TESTING l. 548)
- [x] **0.599** Accueil : lignes Aujourd'hui et Personnaliser (TESTING l. 635)
- [x] **0.646** Équipement section Jobs : niveaux et expérience (TESTING l. 710)
- [x] **0.719** Lexique : chercher eureka et mot anglais (TESTING l. 822)
- [ ] **0.779** Rechargement : objets de monstres dans Monstres (TESTING l. 902)
- [x] **0.885** Découvertes : plus de Récupérer/Partager, Échanger marche (TESTING l. 1044)
- [ ] **0.911** Découvertes : libellé de pêche, plus d'ALÉA à (0,0) (TESTING l. 1075)
- [ ] **0.913** Info-bulles des boutons grisés, largeur uniforme (TESTING l. 1078)
- [ ] **0.914** Curseurs carte/équipement en direct, réglages gardés au rechargement (TESTING l. 1080)
- [x] **0.915** Réglages : bouton ↺ et ligne « Par défaut » (TESTING l. 1082)
- [ ] **0.920** Erreur de source en rouge, vide en gris, « Copié » (TESTING l. 1091)
- [ ] **0.921** Pages avec icônes : présentes et même taille (TESTING l. 1093)
- [ ] **0.944** Paramètres : info-bulles, case zones de quête, bonus craft (TESTING l. 1120)
- [x] **0.999** Paramètres : sept onglets, recherche d'un réglage (TESTING l. 1188)
- [ ] **0.514** Guide et carte : croix ferme du premier coup (TESTING l. 1895)
- [ ] **0.505** Accueil : proposition du guide, Non merci (TESTING l. 1915)
- [ ] **0.502** Bouton ? sur toutes les pages (TESTING l. 1922)
- [x] **0.502** Page Aide : recherche, ouvrir la page, FAQ (TESTING l. 1923)
- [ ] **0.503** Bandeau première ouverture, Compris (TESTING l. 1924)
- [ ] **0.498** Tensho replié se déplie quand demandé (TESTING l. 1939)
- [x] **0.494** /tensho selftest et Diagnostics (TESTING l. 1946)
- [ ] **0.496** Fenêtre vue : OK_SEEN après rechargement (TESTING l. 1947)
- [ ] **0.394** Polices : famille et taille (TESTING l. 2403)

*Il faut : compte GitHub*


*Il faut : hors ligne*

- [ ] **0.246** Hors ligne : pas de téléchargement relancé dans le journal (TESTING l. 126)

*Il faut : mise à jour du jeu*


*Il faut : mode développeur*

- [ ] **0.406** Messages développeur dans la langue du plugin (TESTING l. 2350)

*Il faut : prochaine release*


*Il faut : release*

- [ ] **0.506** Vidéos et guides après une release (TESTING l. 1911)
- [ ] **0.475** Galerie Mes outils après release (TESTING l. 2037)

## Étape 2 : Objets, inventaire, servants, marché (125)

En ville, près d'une sonnette de servant et d'un tableau des ventes.

- [ ] **0.235–0.237** Capture auto : Banque montre l'Arsenal sous 2 min (TESTING l. 76)
- [ ] **0.247–0.250** Listes : infobulles d'objets dans butin, sources, usages (TESTING l. 104)
- [ ] **0.251** Clic droit objet : sources en sous-menus par type (TESTING l. 107)
- [ ] **0.257** Survol objet inventaire : « Comment l'obtenir » affiché (TESTING l. 112)
- [ ] **0.272** Fiche objet complète avec onglets (TESTING l. 116)
- [ ] **0.273** Liens vers fiche objet et prix du marché (TESTING l. 118)
- [ ] **0.327** Survol : décocher chaque partie du panneau (TESTING l. 206)
- [ ] **0.329** Fiche objet : stock de l'en-tête juste (TESTING l. 226)
- [ ] **0.329** Survol rapide : pas de prix indisponibles (TESTING l. 227)
- [ ] **0.329** Historique objets : bouton Retour (TESTING l. 228)
- [ ] **0.343** Infobulle de prix complète au survol (TESTING l. 272)
- [ ] **0.343** Ctrl+Alt rafraîchit le prix au survol (TESTING l. 275)
- [ ] **0.344** Monnaies : soldes et plafonds comme le jeu (TESTING l. 279)
- [ ] **0.346** Survol : prix de vente au PNJ (TESTING l. 289)
- [ ] **0.346** Copier l'offre la plus basse moins 1 (TESTING l. 290)
- [ ] **0.347** Note et étiquettes d'objet au survol (TESTING l. 296)
- [ ] **0.347** Boutique PNJ : lignes À vendre et Attention (TESTING l. 298)
- [ ] **0.348** Rapport : deux piles, case libérable (TESTING l. 303)
- [ ] **0.349** Clic droit : liens Wiki et Lodestone (TESTING l. 307)
- [x] **0.351** Journal des gils : variations par catégorie (TESTING l. 318)
- [ ] **0.352** Marchands : condition de quête d'un article (TESTING l. 324)
- [ ] **0.353** Filtres Objets : catégorie et niveau (TESTING l. 329)
- [ ] **0.354** Monnaies › Que faire ? sur les sceaux (TESTING l. 334)
- [ ] **0.376** Usure et symbiose comme le jeu (TESTING l. 447)
- [ ] **0.377** Comparaison à l'équipement porté au survol (TESTING l. 452)
- [ ] **0.379** Historique du butin : copier en CSV (TESTING l. 467)
- [ ] **0.380** Équipement : colonne +N palier (TESTING l. 471)
- [ ] **0.383** Ensembles d'équipement du jeu listés (TESTING l. 485)
- [ ] **0.541** Mes outils : colonne de prix (TESTING l. 530)
- [ ] **0.555** Commande /tensho prix et autres (TESTING l. 558)
- [ ] **0.561** Menu objet : copier le nom, lien chat (TESTING l. 570)
- [ ] **0.562** Nom anglais dans fiche et infobulle (TESTING l. 572)
- [ ] **0.565** Aperçu du tri : cases encadrées et nombre corrects (TESTING l. 577)
- [ ] **0.580** Équipement : Plan pour un autre job (TESTING l. 601)
- [ ] **0.594** Banque en liste : tri, filtre, onglets (TESTING l. 626)
- [ ] **0.697** Que faire avec ? : arrêt et filtres (TESTING l. 790)
- [ ] **0.701** Monnaies des vendeurs : cochage et recherche (TESTING l. 796)
- [ ] **0.714** Grandes compagnies : grade, sceaux, boutique (TESTING l. 814)
- [ ] **0.817** Tableau des ventes puis prix Tensho : prix vu repris (TESTING l. 954)
- [ ] **0.852** Récompense de quête : clic droit Tensho, survol panneau (TESTING l. 1000)
- [ ] **0.871** Marché › Offres : étendue « Région » nommée tout de suite (TESTING l. 1024)
- [ ] **0.872** Ouvrir boutique marchand : « Marchands placés : 1 » (TESTING l. 1026)
- [ ] **0.875** Désynthèse « Vu donner » ; butin monstre « Vu le laisser » (TESTING l. 1031)
- [ ] **0.889** Remettre objet de quête : Usages « Demandé par des quêtes » (TESTING l. 1050)
- [ ] **0.927** Objet d'emote ou coiffure : « Apprend quelque chose » vert/rouge (TESTING l. 1100)
- [ ] **0.970** Survol quête et objet récolté : ligne « Extension » (TESTING l. 1149)
- [ ] **0.516** Bouton Trier sans fond ; survol sans clic (TESTING l. 1885)
- [ ] **0.469** Ravitaillement GC : objet demandé aujourd'hui (TESTING l. 2060)
- [ ] **0.462** Menu objet Tensho sans Tous les outils (TESTING l. 2084)
- [ ] **0.452** Try On depuis menu d'objet (TESTING l. 2135)

*Il faut : autre personnage*

- [ ] **0.609** Fiche d'objet possédé par autre personnage (TESTING l. 650)

*Il faut : client anglais*

- [ ] **0.326** Banque client anglais : titres en anglais (TESTING l. 200)

*Il faut : deux personnages*

- [ ] **0.344** Changer de personnage : pas de fausse alerte (TESTING l. 281)
- [ ] **0.345** Rangements › Personnages : deux personnages (TESTING l. 285)

*Il faut : matéria XI ou XII*


*Il faut : réseau coupé*

- [ ] **0.321** Marché hors ligne : bouton Région grisé (TESTING l. 183)

*Il faut : servant*

- [ ] **0.576** Rangements : effacer le relevé d'un servant (TESTING l. 594)
- [ ] **0.820** Inventaire du servant : case de l'objet de liste encadrée (TESTING l. 959)
- [ ] **0.870** Vente du servant : verdict sans « Comparer », survol du prix (TESTING l. 1022)
- [x] **0.508** Liste de ventes du servant : panneau Vos ventes (TESTING l. 1907)

*Il faut : événement saisonnier*

- [ ] **0.342** Boutiques de l'événement comparées (TESTING l. 268)

## Étape 3 : Collections, carnets, succès, mode (45)

En ville : montures, mascottes, cartes, orchestrion, succès, titres, carnets, Revue de mode, coffre à apparence.

- [ ] **0.329** Coffre à apparence rempli : import rechargé (TESTING l. 225)
- [ ] **0.340** Compteur de bardes comparé au carnet (TESTING l. 264)
- [ ] **0.357** Pièce d'un set jamais proposée à l'armoire (TESTING l. 350)
- [ ] **0.360** Liste de souhaits : possédé et persistance (TESTING l. 365)
- [ ] **0.362** Tenues du coffre : filtres et recherche (TESTING l. 375)
- [ ] **0.364** Carnet de chasse : rang de la classe (TESTING l. 384)
- [ ] **0.365** Nouveaux types de collection : filtres (TESTING l. 391)
- [ ] **0.368** Succès en cours : progression x / y (TESTING l. 403)
- [ ] **0.369** Titre changé avec le job (TESTING l. 409)
- [ ] **0.370** Rouleau favori dans la liste (TESTING l. 415)
- [ ] **0.549** Équipement porté suivi, Revue de mode (TESTING l. 546)
- [ ] **0.579** Équipement : cocher Tous les jobs (mirage) (TESTING l. 599)
- [ ] **0.581** Revue de mode : pièces et Essayer la tenue (TESTING l. 603)
- [ ] **0.602** Collections : Obtenus récemment, clic droit emote (TESTING l. 640)
- [ ] **0.713** Peuples alliés : rangs et réputation (TESTING l. 812)
- [ ] **0.861** Fashion Report ouvert : catégories, thème, compté aux Découvertes (TESTING l. 1012)
- [ ] **0.878** Monstre lançant sort bleu : « vu lançant », « Vu par vous » (TESTING l. 1036)
- [ ] **0.989** Tenues : pièces manquantes vers souhaits (TESTING l. 1174)
- [ ] **0.526** Clic droit ou survol d'un tome : comment l'obtenir (TESTING l. 1848)
- [ ] **0.491** Coffre à apparence : pièces de tenue possédées (TESTING l. 1957)
- [ ] **0.407** Noms du wiki en français (TESTING l. 2344)
- [ ] **0.395** Mage bleu : livre des sorts, tri par niveau (TESTING l. 2397)

*Il faut : auberge*


*Il faut : chocobo de compagnie*


*Il faut : mage bleu*

- [ ] **0.387** Mage bleu : monstres de Mauvaise haleine (TESTING l. 502)
- [ ] **0.799** Mage bleu : icône au-dessus du monstre apprenant un sort (TESTING l. 929)

*Il faut : événement saisonnier*


## Étape 4 : Artisanat (95)

Avec un métier d'artisan, à l'établi.

- [ ] **0.327** Carnet d'artisanat : calcul automatique décoché (TESTING l. 209)
- [ ] **0.338** Éditeur matéria : taux par grade (TESTING l. 259)
- [ ] **0.363** Mandats : allocations et acceptés (TESTING l. 379)
- [ ] **0.552** Carnet : séquence d'une autre recette proposée (TESTING l. 552)
- [ ] **0.553** Simulateur : seuils comparés à une macro (TESTING l. 554)
- [ ] **0.554** Carnet : valeurs relevées d'un autre métier (TESTING l. 556)
- [ ] **0.559** Survol matériau : détail par rangement (TESTING l. 566)
- [ ] **0.572** Simulateur : chercher recette, ajouter action (TESTING l. 588)
- [ ] **0.573** Macros : son et dernière page seulement (TESTING l. 590)
- [ ] **0.589** Synthèse avec séquence enregistrée : suivi du plan (TESTING l. 617)
- [ ] **0.590** Page des séquences : chercher, renommer, importer (TESTING l. 619)
- [ ] **0.591** Préparation à sous-recettes : chaîne et étapes (TESTING l. 621)
- [ ] **0.592** Que puis-je fabriquer : recherche et filtres (TESTING l. 623)
- [ ] **0.600** Fiche objet recette de livre de maître (TESTING l. 637)
- [ ] **0.668** Pourcentage HQ du jeu égal au simulateur (TESTING l. 753)
- [ ] **0.669** Simulateur : qualité initiale avec ingrédients HQ (TESTING l. 755)
- [ ] **0.675** Commandes spéciales : rang, points, livraisons (TESTING l. 764)
- [ ] **0.699** Panneau : matières de base par provenance (TESTING l. 793)
- [ ] **0.715** Livraisons spéciales : quêtes faites (TESTING l. 816)
- [x] **0.725** Tri par marché : ventes par jour (TESTING l. 831)
- [ ] **0.782** Carnet recette 5623 cuisinier sans gel (TESTING l. 908)
- [ ] **0.783** Kit de réparation : Raphael non utilisé (TESTING l. 910)
- [ ] **0.788** Carnet en Menuisier sur Lingot de mithril : « Mauvais métier » (TESTING l. 917)
- [ ] **0.808** Carnet d'artisanat : clic droit recette et matériau, entrée Tensho (TESTING l. 940)
- [ ] **0.826** Clic droit objet fabricable : « Ajouter à Liste 1 » (TESTING l. 966)
- [ ] **0.516** Panneaux accrochés au jeu : même place et contenu (TESTING l. 1884)
- [ ] **0.509** Synthèse : Conseillé change à chaque coup (TESTING l. 1903)
- [ ] **0.495** Autotest : carnet, synthèse, récolte en OK (TESTING l. 1948)
- [ ] **0.469** Carnet : panneau en sections, barre de qualité (TESTING l. 2061)
- [ ] **0.468** Raphael : cache disque d'une recette (TESTING l. 2065)
- [ ] **0.465** Séquence Raphael jouée donne le résultat (TESTING l. 2073)
- [ ] **0.465** Rechargement sans erreur après calcul (TESTING l. 2074)
- [ ] **0.464** Simulateur : attributs copiés, ligne Détecté (TESTING l. 2078)
- [ ] **0.464** Liste de fabrication : icônes, manquants en rouge (TESTING l. 2079)
- [ ] **0.464** Projection pendant la synthèse (TESTING l. 2080)
- [ ] **0.438** Actions interdites non proposées (TESTING l. 2207)
- [ ] **0.436** Enregistreur de rotation : fabriquer à la main (TESTING l. 2217)
- [ ] **0.435** Recette experte dans le simulateur (TESTING l. 2223)
- [ ] **0.434** Actions du simulateur comparées au jeu (TESTING l. 2228)
- [ ] **0.433** Liste de fabrication : créer, ajouter, retirer (TESTING l. 2233)
- [ ] **0.432** Collectionnables des sacs : collectionnabilité (TESTING l. 2238)
- [ ] **0.431** Préparation : prix et bénéfice (TESTING l. 2243)
- [ ] **0.427** Prérequis de recette en vert ou rouge (TESTING l. 2252)
- [ ] **0.426** Ouvrir dans le carnet ferme Tensho (TESTING l. 2257)
- [ ] **0.425** Importer une macro dans le simulateur (TESTING l. 2263)
- [ ] **0.423** Menu objet Artisanat : onglet Préparation (TESTING l. 2271)
- [ ] **0.421** Craft lié : déjà fabriquée ou jamais (TESTING l. 2281)
- [ ] **0.420** Simulateur : repas HQ et remède (TESTING l. 2286)
- [ ] **0.419** Collectionnable : boutons Bas Moyen Haut (TESTING l. 2291)

*Il faut : exploration cosmique*

- [ ] **0.666** Exploration cosmique : recette, séquence, seuils (TESTING l. 750)

*Il faut : Macro Chain*

- [ ] **0.422** Macro Chain : /nextmacro enchaîne (TESTING l. 2276)

*Il faut : mise à jour du jeu*


*Il faut : mode développeur*


*Il faut : servant*

- [ ] **0.841** Liste 1 : sirop de bouleau dans « Autres », icône d'entrepôt (TESTING l. 986)

## Étape 5 : Récolte et pêche (106)

Mineur, botaniste, pêcheur ; pêche en mer à son créneau.

- [ ] **0.224** Récolte : outil PR, Estimé +N, bouton Utiliser (TESTING l. 157)
- [ ] **0.225** Panneau Aide à la récolte à droite de la fenêtre (TESTING l. 158)
- [ ] **0.226–0.228** Aide à la pêche : poissons, appâts, créneau, météo (TESTING l. 159)
- [ ] **0.230** Page Récolte : outils de l'objet en tête (TESTING l. 162)
- [ ] **0.560** Chaîne : Encore N pour… baisse en récoltant (TESTING l. 568)
- [ ] **0.568** Aide à la pêche : créneau et météo comparés au jeu (TESTING l. 581)
- [ ] **0.588** Récolte : heures des points ouverts, cloche (TESTING l. 615)
- [ ] **0.604** Panneau de pêche et ligne Abondance II (TESTING l. 644)
- [ ] **0.649** Récolte : poisson au harpon, lieu et alarmes (TESTING l. 716)
- [ ] **0.663** Pêche : durée d'intuition des proies (TESTING l. 744)
- [x] **0.728** Récolte : Ensuite et cadre suivent le plan (TESTING l. 835)
- [x] **0.729** Récolte : ligne séparant Ensuite du plan (TESTING l. 837)
- [ ] **0.731** Récolte : Ensuite avec PR restants (TESTING l. 840)
- [ ] **0.733** Récolte : cadre doré sur action Ensuite (TESTING l. 843)
- [x] **0.734** Récolte : cadre sur Récolte abondante I (TESTING l. 845)
- [ ] **0.744** Récolter objets de la liste : pas de List zero (TESTING l. 856)
- [ ] **0.747** Récolte : plan après Calcul du plan (TESTING l. 860)
- [x] **0.751** Récolte : Vos listes en demandent encore (TESTING l. 865)
- [x] **0.752** Liste atelier : une ligne par zone (TESTING l. 867)
- [ ] **0.753** Récolte : cocher Récolter automatiquement (TESTING l. 869)
- [ ] **0.756** Point avec objet des listes : aide placée (TESTING l. 874)
- [ ] **0.757** Liste atelier : replier zone, prix téléport (TESTING l. 876)
- [ ] **0.763** Récolte : plan avec moins d'actions (TESTING l. 884)
- [ ] **0.816** Collectionnable avec seuils : bouton du plan encadré en fenêtre (TESTING l. 952)
- [ ] **0.831** Deux points de récolte enchaînés : le second propose un plan (TESTING l. 973)
- [ ] **0.834** Clic droit Haricots émeraude : « Y aller » et drapeau (TESTING l. 977)
- [ ] **0.859** Pêcher poisson commun : « ni heure ni météo (jeu) » (TESTING l. 1009)
- [ ] **0.887** Pêcher : temps de touche « Vu par vous », touches vues (TESTING l. 1047)
- [ ] **0.891** Harpon : liste du point, poisson en vue coloré, trous (TESTING l. 1053)
- [ ] **0.917** Pêche en mer : état par port, panneau de voyage (TESTING l. 1085)
- [ ] **0.918** Pêche en mer : panneau, appât encadré, chronomètre, spectral (TESTING l. 1087)
- [ ] **0.959** Détroit ouest de Zorgor : météo et météo d'avant (TESTING l. 1136)
- [ ] **0.982** Poisson collectionnable : ligne « Collectionnable », assignats au survol (TESTING l. 1166)
- [ ] **0.529** Trois icônes de risque, cran coloré, description (TESTING l. 1838)
- [ ] **0.523** Deux types d'assignats : phrase types différents (TESTING l. 1861)
- [ ] **0.519** Cristaux : Bénédictions proposées sans nombre d'objets (TESTING l. 1872)
- [ ] **0.518** Point d'Ostrakon : ouvert pas pour vous, tome (TESTING l. 1876)
- [ ] **0.517** Nœud à PR pleins : actions proposées existantes (TESTING l. 1880)
- [ ] **0.515** Nœud PR ≧ 770 : plan et effet du lieu (TESTING l. 1889)
- [ ] **0.514** Barre de PR en mineur : remplissage, tic (TESTING l. 1894)
- [ ] **0.483** Raison solide refusée non conseillée (TESTING l. 1992)

*Il faut : mode développeur*

- [ ] **0.650** Récolter sans action : journal Reading gp (TESTING l. 718)
- [ ] **0.874** Relevés par sonde : pêche, réduction, île, aéronef, coffres… (TESTING l. 1029)

*Il faut : niveau 90*

- [ ] **0.520** Niveau 90 : ligne tentative gratuite 50 % (TESTING l. 1868)

*Il faut : niveau 95*

- [ ] **0.527** Niveau 95 : Toucher d'amorçage proposé après Inspection (TESTING l. 1842)

*Ajouté en 0.1011*

- [ ] **0.1011** C10 : chrono de pêche relancé tout seul à chaque lancer, aussi en mer (TESTING l. 1201)

## Étape 6 : Carte, téléportation, monde ouvert (126)

En zone ouverte : carte Tensho, téléportation, ALEA, chasse, cartes au trésor, panoramas, monstres proches.

- [ ] **0.259 / 0.262** Paramètres carte : infobulles et bouton ↺ (TESTING l. 97)
- [ ] **0.308** Menus par-dessus la carte : zoom lisible (TESTING l. 99)
- [ ] Relevé des icônes de quête 71141 / 71151 (TESTING l. 153)
- [ ] **0.328** Repère au bord du drapeau : clic centre la carte (TESTING l. 217)
- [ ] **0.328** Liste des panoramas : clic droit propose alarme (TESTING l. 220)
- [ ] **0.328** Ville chargée : carte fluide (TESTING l. 221)
- [ ] **0.373** Lien de carte du chat : drapeau, éthérite (TESTING l. 432)
- [ ] **0.542** Carte : zoom à l'ouverture (TESTING l. 532)
- [ ] **0.543** Carte : réglages des noms et étiquettes (TESTING l. 534)
- [ ] **0.545** Carte : molette sur colonne d'outils (TESTING l. 538)
- [ ] **0.546** Carte : clic gauche pose le drapeau (TESTING l. 540)
- [ ] **0.547** Carte : bouton Couches (TESTING l. 542)
- [ ] **0.548** ALEA à bonus d'expérience (TESTING l. 544)
- [ ] **0.551** Monstres : drapeau à la position (TESTING l. 550)
- [ ] **0.563** Météo : tableau de deux zones (TESTING l. 574)
- [ ] **0.582** Quêtes : filtres Disponibles et Cette zone (TESTING l. 605)
- [ ] **0.583** Quêtes : épopée lointaine et À prendre ensuite (TESTING l. 607)
- [ ] **0.586** Carte : aile de vol dans le menu des zones (TESTING l. 611)
- [ ] **0.596** Carte : position des noms d'icônes comme le jeu (TESTING l. 629)
- [ ] **0.597** Carte : recherche de lieux, PNJ, ressources (TESTING l. 631)
- [ ] **0.618** Carte agrandie avec inventaire ouvert (TESTING l. 663)
- [ ] **0.621** L'Entrepont : lignes suivent la carte du jeu (TESTING l. 667)
- [ ] **0.644** Carte : coordonnées, distance, drapeau hors vue (TESTING l. 706)
- [ ] **0.645** Coût d'éthérite au survol, non harmonisée grisée (TESTING l. 708)
- [ ] **0.647** Carte : classe des points légendaires et éphémères (TESTING l. 712)
- [ ] **0.660** Vieille Sharlayan : largeur des noms comme le jeu (TESTING l. 738)
- [ ] **0.661** Téléportation et carte ; dézoom ville (TESTING l. 740)
- [ ] **0.662** Téléportations successives sans plantage (TESTING l. 742)
- [ ] **0.665** Icônes de quêtes : états sans numéro (TESTING l. 748)
- [ ] **0.670** Conditions des rangs S lisibles et filtrées (TESTING l. 757)
- [ ] **0.677** Cartes & repères : 4 onglets et favoris (TESTING l. 767)
- [ ] **0.689** Mandats : infobulle au survol, fenêtre étroite (TESTING l. 780)
- [ ] **0.704** Clic droit sur lignes avec coordonnées (TESTING l. 800)
- [ ] **0.721** ALEA : lieu comparé à la carte du jeu (TESTING l. 825)
- [ ] **0.754** Carte : point à horaire fermé, délai (TESTING l. 871)
- [ ] **0.807** Monde › Découvertes : chercher monstre, exporter puis importer (TESTING l. 938)
- [ ] **0.814** Cible hors vue en haut à gauche : icône de bord visible (TESTING l. 949)
- [ ] **0.819** Yak laineux et lait demandé : patte carte et liste (TESTING l. 957)
- [x] **0.829** Limsa : plus de marchand fantôme, marchands permanents présents (TESTING l. 970)
- [ ] **0.844** Mor Dhona quête cuisinier : flèche vers le bas, réglage (TESTING l. 990)
- [ ] **0.846** Journal, « Carte » sur quête : centrage sans changer le zoom (TESTING l. 993)
- [x] **0.853** Tillac, clic droit cible de quête : « Éthérite la plus proche » (TESTING l. 1002)
- [ ] **0.905** Coordonnées écrites avec un point partout (TESTING l. 1068)
- [ ] **0.929** Vue panoramique Stormblood+ : « Carnet n° », numéros en liste (TESTING l. 1103)
- [ ] **0.960** Carte Zorgor : trou de pêche sans « point légendaire » (TESTING l. 1138)
- [ ] **0.977** Téléportation, carte Tensho : quêtes non acceptées en rouge (TESTING l. 1158)
- [ ] **0.535** Carte sans barre de titre : déplacer et fermer (TESTING l. 1822)
- [ ] **0.499** Lien vers carte : carte devant Tensho (TESTING l. 1935)
- [ ] **0.453** Téléportation : coûts, mandats, chocobo lus (TESTING l. 2130)
- [ ] **0.442** Train de chasse : enregistrer les marques (TESTING l. 2191)
- [ ] **0.437** Coller liste Siren ou Faloop en favoris (TESTING l. 2212)
- [ ] **0.401** Survol d'un panorama : météo, pas d'erreur (TESTING l. 2367)

*Il faut : carte au trésor*

- [ ] **0.384** Carte au trésor : coffre ouvert, icône disparaît (TESTING l. 491)

*Il faut : contrat de chasse*

- [ ] **0.440** Contrat de chasse : cibles et victimes (TESTING l. 2199)

*Il faut : fête saisonnière*


*Il faut : livre du Zodiaque en cours*


*Il faut : mode développeur*

- [ ] **0.809** Vaincre monstres, ramasser butin, relire « Reading messages » (TESTING l. 942)

## Étape 7 : Compagnie libre, logement, île (56)

Atelier de compagnie, sous-marins, aéronefs, coffre, jardin, île sanctuaire.

- [ ] **0.693** Panneau en combat et sous fenêtre du jeu (TESTING l. 785)
- [ ] **0.444** Annuaire : ajouter maison et appartement (TESTING l. 2181)

*Il faut : aéronef*

- [ ] **0.919** Aéronef : onglet navires « Dernier voyage » (TESTING l. 1089)

*Il faut : compagnie libre*

- [ ] **0.664** Atelier : chercher projet, comparer matériaux (TESTING l. 746)
- [ ] **0.787** Livraison à l'établi : panneau d'atelier, reste à livrer (TESTING l. 915)

*Il faut : compagnie libre, aéronef*


*Il faut : deux compagnies libres*

- [ ] **0.603** Deux compagnies relevées : deux cadres datés (TESTING l. 642)

*Il faut : dirigeable*


*Il faut : Félicité insulaire*


*Il faut : jardin*

- [ ] **0.540** Journal du jardin : graines de centaurée (TESTING l. 528)
- [ ] **0.978** Jardin : planter, récolter, ligne « retiré du journal » (TESTING l. 1160)

*Il faut : logement avec jardin*


*Il faut : sous-marin*

- [ ] **0.315** Montages : statistiques d'un vaisseau lu (TESTING l. 146)

*Il faut : sous-marin, mode développeur*


*Il faut : île sanctuaire*

- [ ] **0.671** Île sanctuaire : lire agenda, valeurs du jeu (TESTING l. 759)

## Étape 8 : Mini-jeux, escouade, chocobo, dresseur (20)

Gold Saucer, Triple Triad, Mini Cactpot, escouade, chocobo, Dédale bestial hors combat.

- [ ] **0.571** Mini Cactpot : comparer la table au ticket (TESTING l. 586)
- [ ] **0.510** Triple Triade : conseils PNJ seulement, pas contre joueur (TESTING l. 1899)
- [ ] **0.500** Dresseur Bêtes : 50 bêtes et stats (TESTING l. 1931)
- [ ] **0.493** Triple Triade : colonne Disponible des PNJ (TESTING l. 1952)
- [ ] **0.491** Défi Triple Triade : règles lues (TESTING l. 1956)

*Il faut : dresseur*

- [ ] **0.980** Dresseur › Équipe : bêtes, faiblesses, étoiles des ennemis (TESTING l. 1163)

*Il faut : escouade*

- [ ] **0.333** Mission d'escouade : nom et heure de fin (TESTING l. 235)

*Il faut : mode développeur*


*Il faut : mode développeur, dresseur*


## Étape 9 : Rappels, alarmes, minuteurs, Discord (55)

Liés aux horaires du jeu : à faire quand le moment arrive (réinitialisations, minuteurs).

- [ ] **0.300** Rappels comparés au calendrier du jeu (TESTING l. 131)
- [ ] **0.372** Rappels : livre de Khloe et contrats de chasse (TESTING l. 425)
- [ ] **0.375** Vos tâches : ajouter, cocher, compteur (TESTING l. 442)
- [ ] **0.382** Carnet d'objectifs : nombre de défis (TESTING l. 479)
- [ ] **0.382** Enclave de Doman : don et plafond (TESTING l. 481)
- [ ] **0.386** Horloges Tokyo et New York (TESTING l. 498)
- [ ] **0.556** Alarme avec avance puis à l'heure (TESTING l. 560)
- [ ] **0.557** Alarmes : modifier, Dans 5 min, date refusée (TESTING l. 562)
- [ ] **0.558** Rappels cochés seuls après quêtes de tribu (TESTING l. 564)
- [ ] **0.577** Alarmes quotidienne et à la remise à zéro (TESTING l. 596)
- [ ] **0.759** Alarme : Ouvrir dans Tensho ouvre la fiche (TESTING l. 879)
- [x] **0.774** Alarme : clic droit sur nom dans le chat (TESTING l. 896)
- [x] **0.780** Alarme : clic droit sur la ligne Minerai (TESTING l. 904)
- [x] **0.781** Alarme : clic droit, Context menu opened (TESTING l. 906)
- [x] **0.812** Alarme : clic droit sur l'objet, menu avec « Tensho » (TESTING l. 946)
- [ ] **0.837** Clic droit morille d'alarme : entrée Tensho ; joueur : aucune (TESTING l. 981)
- [ ] **0.936** Rappels, « Vos personnages » : colonne « Autres », escouade (TESTING l. 1111)
- [ ] **0.514** Fenêtre de suivi : alarmes, objet, carte, croix (TESTING l. 1893)
- [ ] **0.453** Rappels à l'écran titre sans zéros (TESTING l. 2127)
- [ ] **0.448** Rappel non répété après redémarrage (TESTING l. 2162)
- [ ] **0.443** Livre d'or : tâches à faire et à réclamer (TESTING l. 2186)

*Il faut : deux personnages*

- [ ] **0.374** Rappels › Vos personnages avec deux persos (TESTING l. 437)

*Il faut : salon Discord de test*


*Il faut : servant*

- [ ] **0.570** Rappels : tâches de servants, heures et alarmes (TESTING l. 584)

*Il faut : webhook Discord*

- [ ] **0.598** Discord : un salon par catégorie (TESTING l. 633)

## Étape 10 : Missions et instances (28)

Outil de mission, roulettes, donjons, Eurêka, Bozja, donjons profonds, Carnaval masqué.

- [ ] **0.325** Panneau Duty Finder : recherche par identifiant, ↻ (TESTING l. 193)
- [ ] **0.327** Pas de ligne de discussion en mission (TESTING l. 210)
- [ ] **0.355** Filtre Non terminés des contenus (TESTING l. 341)
- [ ] **0.356** Missions aléatoires du jour passent à Faite (TESTING l. 345)
- [ ] **0.366** Alerte mission prête à l'ouverture (TESTING l. 395)
- [ ] **0.648** Butin des contenus : Palais des morts débloqué par (TESTING l. 714)
- [ ] **0.659** Carte rouverte après scène d'entrée en contenu (TESTING l. 736)
- [ ] **0.683** Grande Mascarade : niveaux, ennemis, temps (TESTING l. 774)
- [ ] **0.708** Armes reliques : étapes et livre en cours (TESTING l. 806)
- [ ] **0.718** Donjons profonds : poteries et étages (TESTING l. 820)
- [ ] **0.723** Masked Carnivale : épreuves faites (TESTING l. 828)
- [ ] **0.876** Coffre de mission ouvert : fiche « Coffre (vu par vous) » (TESTING l. 1033)
- [ ] **0.975** Page Eurêka et Bozja : Croissant occulte, ALÉA placés (TESTING l. 1155)
- [ ] **0.990** Butin des contenus après déconnexion : état enregistré affiché (TESTING l. 1176)
- [ ] **0.994** Donjon : lire le wiki mécaniques, masqué en combat (TESTING l. 1181)
- [ ] **0.995** Raid : notes et schéma d'arène, copier et coller le code (TESTING l. 1183)

*Il faut : mode développeur*


## Étape 11 : JcJ et classé (4)

En tout dernier (règle de l'utilisateur).

- [ ] **0.610** Coupure compétitive : JcJ, Mahjong, donjon profond (TESTING l. 652)
- [ ] **0.615** Lien de carte pendant une coupure ignoré (TESTING l. 659)
