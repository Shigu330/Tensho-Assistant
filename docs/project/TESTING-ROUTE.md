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

- [ ] Dalamud à jour pour le patch 7.58 ; Tensho se charge ; `/tensho` affiche la dernière version.
- [ ] L'agent lit les lignes « Selftest » et « Game data check » de dalamud.log et les compare à l'autotest d'avant le patch.
- [ ] Enregistreur d'artisanat (interception de `UseAction`) : une action d'artisanat est notée.
- [ ] Enchaînement de macros (interception de `ExecuteMacro`) : une chaîne de deux macros va jusqu'au bout.
- [ ] Écriture d'une macro par Tensho : texte, icône, macro enregistrée.
- [ ] Téléportation depuis la carte Tensho vers l'éthérite proposée.
- [ ] Changement d'ensemble d'équipement depuis Tensho.
- [ ] Changement de titre depuis Tensho.
- [ ] Alarme du jeu : affichée et son joué ; commande envoyée au chat (alarme, annonce de chasse).
- [ ] Carte du jeu remplacée par la carte Tensho, puis rendue normale (touche M, plugin déchargé).
- [ ] Cabine d'essayage, carnet d'artisanat et carnet de récolte ouverts depuis Tensho.
- [ ] Progression d'un succès demandée et affichée.
- [ ] Relevés en attente, au fil du jeu : extraction de matéria ; atelier de compagnie et fenêtre des sous-marins (« Submarine voyage check ») ;
  jardin (récolte avec croisement, X-108) ; coffres des cartes au trésor ; retour d'aéronef (butin) ; carte d'Eurêka Anemos (X-86) ;
  pages des champs (X-90) ; lignes « Item gain not attributed » après un combat.
- [ ] Panneau du Dédale bestial (X-109) devant le plateau, puis disparu au début du combat.
- [ ] Couches des monstres proches (X-40) : bouclier sur les cibles d'un livre du Zodiaque, cœur sur un monstre qui fait tomber un souhait.

## Étape 1 : Fenêtres et réglages de Tensho (122)

N'importe où, sans activité de jeu : réglages, langues, caches, crédits, aide, rechargement.

- [ ] **0.248** Paquet installé : seulement les trois fichiers (TESTING l. 185)
- [ ] **0.258** Tout remettre par défaut : réglages gardés corrects (TESTING l. 187)
- [ ] **0.260–0.261** ↺ sur délai des commandes et thème (TESTING l. 190)
- [ ] **0.264** Barre latérale en catégories repliables (TESTING l. 191)
- [ ] **0.320** Crédits : sept sections présentes (TESTING l. 262)
- [ ] **0.320** Crédits : filtre et boutons copier (TESTING l. 264)
- [ ] **0.320** Crédits : Universalis coupé affiché désactivé (TESTING l. 266)
- [ ] **0.320** Crédits : copier l'adresse du dépôt (TESTING l. 268)
- [ ] **0.321** Réglages Artisanat gardés après rechargement (TESTING l. 272)
- [ ] **0.321** Remise par défaut garde les groupes repliés (TESTING l. 276)
- [ ] **0.321** Cabine d'essayage et Rappels inchangés (TESTING l. 278)
- [ ] **0.324** Universalis décoché : prix au survol grisé (TESTING l. 282)
- [ ] **0.324** Garland décoché : butin consulté encore affiché (TESTING l. 285)
- [ ] **0.324** Wiki décoché : monstres au survol grisé (TESTING l. 287)
- [ ] **0.324** Boutons ↺ des réglages fonctionnent (TESTING l. 289)
- [ ] **0.325** Infobulles des filtres, recherches et tris (TESTING l. 295)
- [ ] **0.326** Wiki ou Garland coupé : plus de téléchargement (TESTING l. 306)
- [ ] **0.326** Première release avec Verify -Release (TESTING l. 314)
- [ ] **0.327** Nombre de résultats à 50, ↺ remet 30 (TESTING l. 318)
- [ ] **0.327** Dernière page rouverte après rechargement (TESTING l. 319)
- [ ] **0.336** Crédits : LuminaSupplemental deux fois (TESTING l. 377)
- [ ] **0.347** Restaurer les réglages garde les notes (TESTING l. 453)
- [ ] **0.537** Barres PR, usure, tâches cachées par fenêtres (TESTING l. 719)
- [ ] **0.537** Trois réglages d'effacement (TESTING l. 721)
- [ ] **0.550** Quêtes : colonnes, tri, plage gardée (TESTING l. 747)
- [ ] **0.585** Équipement Plan pour : texte et majuscule (TESTING l. 817)
- [ ] **0.599** Accueil : lignes Aujourd'hui et Personnaliser (TESTING l. 845)
- [ ] **0.611** Journal : Competitive cut au chargement (TESTING l. 869)
- [ ] **0.646** Équipement section Jobs : niveaux et expérience (TESTING l. 939)
- [ ] **0.719** Lexique : chercher eureka et mot anglais (TESTING l. 1083)
- [ ] **0.722** Rien à faire : lecture dans dalamud.log (TESTING l. 1089)
- [ ] **0.724** Rien à faire : résultat dans dalamud.log (TESTING l. 1093)
- [ ] **0.726** Clignotement après rechargement (TESTING l. 1097)
- [ ] **0.732** Rien à faire : passage des pages dans log (TESTING l. 1109)
- [ ] **0.736** Rien à faire (TESTING l. 1117)
- [ ] **0.738** Rien à faire (TESTING l. 1121)
- [ ] **0.745** Jouer normalement : Slow frame dans le log (TESTING l. 1135)
- [ ] **0.746** Rien à faire (TESTING l. 1137)
- [ ] **0.748** À-coups après rechargement disparus (TESTING l. 1141)
- [ ] **0.750** Cache & péremption : nouvelles durées (TESTING l. 1145)
- [ ] **0.766** Rien à faire (TESTING l. 1177)
- [ ] **0.776** Rien à faire (TESTING l. 1197)
- [ ] **0.779** Rechargement : objets de monstres dans Monstres (TESTING l. 1203)
- [ ] **0.802** Rien à essayer en jeu (TESTING l. 1249)
- [ ] **0.855** Monde › Découvertes : relevés baissent, Discoveries.json rapetisse (TESTING l. 1355)
- [ ] **0.866** Crédits, Aide « Que garde Tensho », indication de recherche (TESTING l. 1377)
- [ ] **0.867** Rien à essayer en jeu (TESTING l. 1379)
- [ ] **0.882** Découvertes : « Partager les miennes », « Récupérer » pas publié (TESTING l. 1409)
- [ ] **0.885** Découvertes : plus de Récupérer/Partager, Échanger marche (TESTING l. 1415)
- [ ] **0.886** Aucun essai en jeu (outil du mainteneur) (TESTING l. 1417)
- [ ] **0.904** Aucune page ne montre de clé brute entre crochets (TESTING l. 1453)
- [ ] **0.911** Découvertes : libellé de pêche, plus d'ALÉA à (0,0) (TESTING l. 1467)
- [ ] **0.912** Thème clair : Équipement lisible, marques coche/croix (TESTING l. 1469)
- [ ] **0.913** Info-bulles des boutons grisés, largeur uniforme (TESTING l. 1471)
- [ ] **0.914** Curseurs carte/équipement en direct, réglages gardés au rechargement (TESTING l. 1473)
- [ ] **0.915** Réglages : bouton ↺ et ligne « Par défaut » (TESTING l. 1475)
- [ ] **0.916** Découvertes se chargent et s'enregistrent comme avant (TESTING l. 1477)
- [ ] **0.920** Erreur de source en rouge, vide en gris, « Copié » (TESTING l. 1485)
- [ ] **0.921** Pages avec icônes : présentes et même taille (TESTING l. 1487)
- [ ] **0.922** dalamud.log plus court hors mode développeur (TESTING l. 1489)
- [ ] **0.923** Listes, découvertes, historiques s'enregistrent comme avant (TESTING l. 1491)
- [ ] **0.924** Recharger le plugin sans erreur dans dalamud.log (TESTING l. 1493)
- [ ] **0.925** Journal des gils, cases « fait », voyages gardés au rechargement (TESTING l. 1495)
- [ ] **0.931** Rien de visible (TESTING l. 1507)
- [ ] **0.938** Découvertes, aide à la pêche, info-bulles comme avant (TESTING l. 1521)
- [ ] **0.939** Rien de visible avec une base normale (TESTING l. 1523)
- [ ] **0.940** Dossier Shared/kind-NN.json.gz, mêmes nombres au rechargement (TESTING l. 1525)
- [ ] **0.941** Recharger : nombres des Découvertes à la demande (TESTING l. 1527)
- [ ] **0.942** Page Découvertes : mêmes totaux qu'avant (TESTING l. 1529)
- [ ] **0.943** Rien de visible avec une base normale (TESTING l. 1531)
- [ ] **0.944** Paramètres : info-bulles, case zones de quête, bonus craft (TESTING l. 1533)
- [ ] **0.945** Avancé « Données du jeu seulement » ; vider le cache, Backups (TESTING l. 1535)
- [ ] **0.947** Rien de visible (TESTING l. 1539)
- [ ] **0.948** Paramètres › Artisanat : cases en retrait grisées (TESTING l. 1541)
- [ ] **0.949** Avancé « Terrains en vente (PaissaDB) », avertissement Découvertes (TESTING l. 1543)
- [ ] **0.952** Décocher un réglage principal grise ses parties dessous (TESTING l. 1549)
- [ ] **0.953** Paramètres › Carte : réglages en retrait grisés (TESTING l. 1551)
- [ ] **0.957** Rien de visible (TESTING l. 1559)
- [ ] **0.998** Navigation en groupes SUIVI, CONTENUS, RÉFÉRENCES (TESTING l. 1641)
- [ ] **0.999** Paramètres : sept onglets, recherche d'un réglage (TESTING l. 1643)
- [ ] **0.1004** Aucun essai en jeu (TESTING l. 1653)
- [ ] **0.1005** Aucun essai : sonde DiscoveryCheckProbe sans QuestItemAsked (TESTING l. 1655)
- [ ] **0.1009** Aucun essai : maintenance (TESTING l. 1663)
- [ ] **0.535** Panneaux à barre de titre : réduire et fermer (TESTING l. 1673)
- [ ] **0.525** Après relance du jeu : lignes Load lues (TESTING l. 1716)
- [ ] **0.516** Échap ne ferme pas, pas de focus (TESTING l. 1761)
- [ ] **0.516** Pas d'icône Dalamud sur les panneaux (TESTING l. 1762)
- [ ] **0.515** Paramètres Récolte : réglages retirés (TESTING l. 1773)
- [ ] **0.514** Quatre panneaux sans focus, Échap ne ferme pas (TESTING l. 1781)
- [ ] **0.514** Guide et carte : croix ferme du premier coup (TESTING l. 1782)
- [ ] **0.510** Option Cadre des fenêtres du jeu absente (TESTING l. 1787)
- [ ] **0.505** Guide : sept étapes, Retour, Suivant, reprise (TESTING l. 1806)
- [ ] **0.505** Accueil : proposition du guide, Non merci (TESTING l. 1807)
- [ ] **0.505** Étape fenêtres : coche automatique (TESTING l. 1808)
- [ ] **0.505** Guide caché en combat, visite des pages (TESTING l. 1809)
- [ ] **0.504** Signaler un problème : formulaire GitHub rempli (TESTING l. 1813)
- [ ] **0.502** Bouton ? sur toutes les pages (TESTING l. 1817)
- [ ] **0.502** Page Aide : recherche, ouvrir la page, FAQ (TESTING l. 1818)
- [ ] **0.503** Bandeau première ouverture, Compris (TESTING l. 1819)
- [ ] **0.502** Relire les textes d'aide (TESTING l. 1820)
- [ ] **0.498** Tensho replié se déplie quand demandé (TESTING l. 1839)
- [ ] **0.494** /tensho selftest et Diagnostics (TESTING l. 1849)
- [ ] **0.496** Fenêtre vue : OK_SEEN après rechargement (TESTING l. 1850)
- [ ] **0.475** Aucune requête sans clic (TESTING l. 1943)
- [ ] **0.475** Remerciements : ligne des outils communautaires (TESTING l. 1944)
- [ ] **0.394** Polices : famille et taille (TESTING l. 2362)

*Il faut : compte GitHub*

- [ ] **0.883** Découvertes : connecter GitHub, « Échanger maintenant » crée ticket (TESTING l. 1411)

*Il faut : hors ligne*

- [ ] **0.246** Hors ligne : pas de téléchargement relancé dans le journal (TESTING l. 195)

*Il faut : mise à jour du jeu*

- [ ] **0.569** Après mise à jour du jeu : ligne de ce qui manque (TESTING l. 785)
- [ ] **0.972** Après mise à jour du jeu : « Game data check » (TESTING l. 1589)

*Il faut : mode développeur*

- [ ] **0.836** /tensho linktest : clics droit et gauche sur A à D (TESTING l. 1317)
- [ ] **0.881** Sondes addons, fashion, île : harpon, aéronef, sertissage, lettre… (TESTING l. 1407)
- [ ] **0.951** Accueil et survols : « Slow frame » nomment les parties (TESTING l. 1547)
- [ ] **0.992** Recharger Tensho, lignes Slow reader dans le journal (TESTING l. 1629)
- [ ] **0.993** Recharger : plus de Slow reader GilWatcher, journal des gils gardé (TESTING l. 1631)
- [ ] **0.1006** Rien à faire : sonde au rechargement (TESTING l. 1657)
- [ ] **0.1008** Après mise à jour Dalamud : autotest comparé (TESTING l. 1661)
- [ ] **0.494** Autotest après rechargement (TESTING l. 1848)
- [ ] **0.406** Messages développeur dans la langue du plugin (TESTING l. 2306)

*Il faut : prochaine release*

- [ ] **0.884** Après release : « Récupérer les découvertes communes » (TESTING l. 1413)

*Il faut : release*

- [ ] **0.506** Vidéos et guides après une release (TESTING l. 1802)
- [ ] **0.475** Galerie Mes outils après release (TESTING l. 1942)

## Étape 2 : Objets, inventaire, servants, marché (125)

En ville, près d'une sonnette de servant et d'un tableau des ventes.

- [ ] **0.235–0.237** Capture auto : Banque montre l'Arsenal sous 2 min (TESTING l. 131)
- [ ] **0.280** Barre latérale OBJETS : pages Objets et Rangements (TESTING l. 136)
- [ ] **0.247–0.250** Listes : infobulles d'objets dans butin, sources, usages (TESTING l. 162)
- [ ] **0.251** Clic droit objet : sources en sous-menus par type (TESTING l. 165)
- [ ] **0.257** Survol objet inventaire : « Comment l'obtenir » affiché (TESTING l. 170)
- [ ] **0.272** Fiche objet complète avec onglets (TESTING l. 174)
- [ ] **0.273** Liens vers fiche objet et prix du marché (TESTING l. 176)
- [ ] **0.282** Objet sans recette ou invendable : onglets masqués (TESTING l. 178)
- [ ] **0.327** Marché : portée Monde gardée (TESTING l. 320)
- [ ] **0.327** Survol : décocher chaque partie du panneau (TESTING l. 321)
- [ ] **0.329** Arsenal et coffre apparaissent sans enregistrement auto (TESTING l. 342)
- [ ] **0.329** Fiche objet : stock de l'en-tête juste (TESTING l. 345)
- [ ] **0.329** Survol rapide : pas de prix indisponibles (TESTING l. 346)
- [ ] **0.329** Historique objets : bouton Retour (TESTING l. 347)
- [ ] **0.329** Aperçu du tri impossible : raison affichée (TESTING l. 348)
- [ ] **0.343** Infobulle de prix complète au survol (TESTING l. 414)
- [ ] **0.343** Le moins cher de la région affiché (TESTING l. 417)
- [ ] **0.343** Ctrl+Alt rafraîchit le prix au survol (TESTING l. 418)
- [ ] **0.344** Monnaies : soldes et plafonds comme le jeu (TESTING l. 422)
- [ ] **0.344** Monnaie au-dessus de 80 % à la connexion (TESTING l. 424)
- [ ] **0.344** Passer un seuil de monnaie : message chat (TESTING l. 425)
- [ ] **0.344** Seuil de monnaie enregistré, ↺ et 0 (TESTING l. 428)
- [ ] **0.345** Dossier InventoryBank : un fichier par personnage (TESTING l. 432)
- [ ] **0.345** Aperçu de tri : servants et sacoches seulement (TESTING l. 438)
- [ ] **0.346** Survol : prix de vente au PNJ (TESTING l. 442)
- [ ] **0.346** Copier l'offre la plus basse moins 1 (TESTING l. 443)
- [ ] **0.347** Note et étiquettes d'objet au survol (TESTING l. 449)
- [ ] **0.347** Boutique PNJ : lignes À vendre et Attention (TESTING l. 451)
- [ ] **0.348** Rapport : deux piles, case libérable (TESTING l. 457)
- [ ] **0.348** Rapport : pièces de l'arsenal hors des sets (TESTING l. 458)
- [ ] **0.349** Clic droit : liens Wiki et Lodestone (TESTING l. 463)
- [ ] **0.349** Fiche et commandes wiki / lodestone (TESTING l. 464)
- [ ] **0.351** Journal des gils : variations par catégorie (TESTING l. 476)
- [ ] **0.352** Marchands : condition de quête d'un article (TESTING l. 483)
- [ ] **0.352** Annuaire des marchands reconstruit une fois (TESTING l. 485)
- [ ] **0.353** Filtres Objets : catégorie et niveau (TESTING l. 489)
- [ ] **0.353** Recherche d'objets clés (TESTING l. 491)
- [ ] **0.353** /tensho wiki avec nom courant (TESTING l. 492)
- [ ] **0.354** Monnaies › Que faire ? sur les sceaux (TESTING l. 496)
- [ ] **0.376** Usure et symbiose comme le jeu (TESTING l. 634)
- [ ] **0.376** Seuil Réparer sous : message unique (TESTING l. 636)
- [ ] **0.377** Comparaison à l'équipement porté au survol (TESTING l. 641)
- [ ] **0.377** Pièce portée : pas de comparaison (TESTING l. 643)
- [ ] **0.379** Historique du butin : copier en CSV (TESTING l. 657)
- [ ] **0.380** Équipement : colonne +N palier (TESTING l. 661)
- [ ] **0.383** Ensembles d'équipement du jeu listés (TESTING l. 677)
- [ ] **0.383** Ensemble : amélioration possédée (TESTING l. 678)
- [ ] **0.385** Job au hasard (TESTING l. 689)
- [ ] **0.541** Mes outils : colonne de prix (TESTING l. 729)
- [ ] **0.555** Commande /tensho prix et autres (TESTING l. 757)
- [ ] **0.561** Menu objet : copier le nom, lien chat (TESTING l. 769)
- [ ] **0.562** Nom anglais dans fiche et infobulle (TESTING l. 771)
- [ ] **0.565** Aperçu du tri : cases encadrées et nombre corrects (TESTING l. 777)
- [ ] **0.566** Tri : copier, supprimer, coller un modèle (TESTING l. 779)
- [ ] **0.580** Équipement : Plan pour un autre job (TESTING l. 807)
- [ ] **0.593** Équipement : créer, renommer, partager un plan (TESTING l. 833)
- [ ] **0.594** Banque en liste : tri, filtre, onglets (TESTING l. 835)
- [ ] **0.595** Règles de tri : zones job et set d'équipement (TESTING l. 837)
- [ ] **0.605** Sac ouvert : objet déplacé toujours dans Banque (TESTING l. 857)
- [ ] **0.607** Objets : annoter, filtrer par étiquette (TESTING l. 861)
- [ ] **0.695** Monnaies : Que faire ? et Gils par unité (TESTING l. 1035)
- [ ] **0.697** Que faire avec ? : arrêt et filtres (TESTING l. 1039)
- [ ] **0.701** Monnaies des vendeurs : cochage et recherche (TESTING l. 1047)
- [ ] **0.702** Monnaies des vendeurs : liste plus courte (TESTING l. 1049)
- [ ] **0.703** PNJ à quête non faite change de catégorie (TESTING l. 1051)
- [ ] **0.705** Infobulles : vendeurs, prérequis, Gil seulement (TESTING l. 1055)
- [ ] **0.714** Grandes compagnies : grade, sceaux, boutique (TESTING l. 1073)
- [ ] **0.768** Rangements Gils : icônes et couleurs (TESTING l. 1181)
- [ ] **0.775** Téléportation : dépense dans Téléportation (TESTING l. 1195)
- [ ] **0.794** Que faire avec ?, Sceau de compagnie : tri et colonnes (TESTING l. 1233)
- [ ] **0.817** Tableau des ventes puis prix Tensho : prix vu repris (TESTING l. 1279)
- [ ] **0.821** Survol objet au tableau : votre monde, moins cher ailleurs (TESTING l. 1287)
- [ ] **0.852** Récompense de quête : clic droit Tensho, survol panneau (TESTING l. 1349)
- [ ] **0.857** Tableau sur autre monde : info-bulle donne monde le moins cher (TESTING l. 1359)
- [ ] **0.858** Tableau d'un objet deux jours : l'historique vu grandit (TESTING l. 1361)
- [ ] **0.865** Désynthèse puis survol résultat : « ← désynthèse de » (TESTING l. 1375)
- [ ] **0.868** Marché › Offres : offres du monde « (vu en jeu) » (TESTING l. 1381)
- [ ] **0.871** Marché › Offres : étendue « Région » nommée tout de suite (TESTING l. 1387)
- [ ] **0.872** Ouvrir boutique marchand : « Marchands placés : 1 » (TESTING l. 1389)
- [ ] **0.875** Désynthèse « Vu donner » ; butin monstre « Vu le laisser » (TESTING l. 1395)
- [ ] **0.888** Éthérolyse : compteur Découvertes, fiche cristal « ← éthérolyse » (TESTING l. 1421)
- [ ] **0.889** Remettre objet de quête : Usages « Demandé par des quêtes » (TESTING l. 1423)
- [ ] **0.903** Gils par lettre : journal des gils « Courrier (lettres) » (TESTING l. 1451)
- [ ] **0.907** Surlignage collectionnables et inventaires inchangé (TESTING l. 1459)
- [ ] **0.909** Objets › cristaux : éléments et tailles comme avant (TESTING l. 1463)
- [ ] **0.926** Prix à copier au marché comme avant (TESTING l. 1497)
- [ ] **0.927** Objet d'emote ou coiffure : « Apprend quelque chose » vert/rouge (TESTING l. 1499)
- [ ] **0.928** Survol minerai ou bois : « Tâche de servant : niveau » (TESTING l. 1501)
- [ ] **0.955** Vendeurs, Équipement, Banque, Tri comme avant (TESTING l. 1555)
- [ ] **0.969** Tri › « Ajouter une catégorie » : groupes entiers, plus de 64 (TESTING l. 1583)
- [ ] **0.970** Survol quête et objet récolté : ligne « Extension » (TESTING l. 1585)
- [ ] **0.991** Objets filtre Job : seules ses pièces restent (TESTING l. 1627)
- [ ] **0.1000** Souhait avec alerte de prix : ligne Souhait en vente (TESTING l. 1645)
- [ ] **0.535** Bouton Trier disparaît sous les autres fenêtres (TESTING l. 1674)
- [ ] **0.516** Bouton Trier sans fond ; survol sans clic (TESTING l. 1760)
- [ ] **0.514** Barre d'usure : même place et valeurs (TESTING l. 1779)
- [ ] **0.469** Ravitaillement GC : objet demandé aujourd'hui (TESTING l. 1967)
- [ ] **0.469** Après 20h UTC : message d'ouvrir la fenêtre (TESTING l. 1968)
- [ ] **0.462** Survol équipement : comparaison en grille (TESTING l. 1999)
- [ ] **0.462** Menu objet Tensho sans Tous les outils (TESTING l. 2000)
- [ ] **0.452** Try On depuis menu d'objet (TESTING l. 2066)

*Il faut : autre personnage*

- [ ] **0.609** Fiche d'objet possédé par autre personnage (TESTING l. 865)

*Il faut : client anglais*

- [ ] **0.326** Banque client anglais : titres en anglais (TESTING l. 313)

*Il faut : deux personnages*

- [ ] **0.344** Changer de personnage : pas de fausse alerte (TESTING l. 427)
- [ ] **0.345** Rangements › Personnages : deux personnages (TESTING l. 434)
- [ ] **0.345** Voir l'autre personnage dans la Banque (TESTING l. 435)
- [ ] **0.345** Effacer l'autre personnage (TESTING l. 437)
- [ ] **0.351** Journal des gils par personnage (TESTING l. 479)

*Il faut : matéria XI ou XII*

- [ ] **0.899** Matéria XI ou XII : taux comparé à la fiche Tensho (TESTING l. 1443)

*Il faut : réseau coupé*

- [ ] **0.281** Marché, Universalis injoignable : dernière lecture affichée (TESTING l. 193)
- [ ] **0.321** Marché hors ligne : bouton Région grisé (TESTING l. 277)

*Il faut : servant*

- [ ] **0.238–0.239** La Banque se charge, servants et sacoches présents (TESTING l. 134)
- [ ] **0.567** Marché : bouton copie le bon prix (TESTING l. 781)
- [ ] **0.576** Rangements : effacer le relevé d'un servant (TESTING l. 799)
- [ ] **0.767** Servant : cristaux dans stocks, gils servants (TESTING l. 1179)
- [ ] **0.769** Déposer gils chez servant : total suit (TESTING l. 1183)
- [ ] **0.770** Déposer objet chez servant : stocks en 2 s (TESTING l. 1185)
- [ ] **0.820** Inventaire du servant : case de l'objet de liste encadrée (TESTING l. 1285)
- [ ] **0.822** Objet de liste chez un servant : icône d'entrepôt au survol (TESTING l. 1289)
- [ ] **0.839** Servant avec deux vins : seul le vin de qualité encadré (TESTING l. 1323)
- [ ] **0.870** Vente du servant : verdict sans « Comparer », survol du prix (TESTING l. 1385)
- [ ] **0.508** Liste de ventes du servant : panneau Vos ventes (TESTING l. 1796)
- [ ] **0.508** Comparer aux autres vendeurs (TESTING l. 1797)
- [ ] **0.508** Agent lit Retainer sale list (TESTING l. 1798)

*Il faut : événement saisonnier*

- [ ] **0.342** Boutiques de l'événement comparées (TESTING l. 410)

## Étape 3 : Collections, carnets, succès, mode (45)

En ville : montures, mascottes, cartes, orchestrion, succès, titres, carnets, Revue de mode, coffre à apparence.

- [ ] **0.329** Coffre à apparence rempli : import rechargé (TESTING l. 344)
- [ ] **0.340** Compteur de bardes comparé au carnet (TESTING l. 401)
- [ ] **0.341** Page Chocobo dans Progression (TESTING l. 402)
- [ ] **0.357** Pièce d'un set jamais proposée à l'armoire (TESTING l. 516)
- [ ] **0.358** Coffre à apparence : apparences identiques (TESTING l. 520)
- [ ] **0.360** Liste de souhaits : possédé et persistance (TESTING l. 533)
- [ ] **0.362** Tenues du coffre : filtres et recherche (TESTING l. 546)
- [ ] **0.364** Carnet de chasse : rang de la classe (TESTING l. 557)
- [ ] **0.364** Carnet de chasse compagnie et autre classe (TESTING l. 559)
- [ ] **0.365** Nouveaux types de collection : filtres (TESTING l. 566)
- [ ] **0.368** Succès en cours : progression x / y (TESTING l. 582)
- [ ] **0.368** Demander la progression d'un succès (TESTING l. 583)
- [ ] **0.368** Suivre et retirer un succès (TESTING l. 584)
- [ ] **0.369** Titre changé avec le job (TESTING l. 590)
- [ ] **0.370** Rouleau favori dans la liste (TESTING l. 596)
- [ ] **0.549** Équipement porté suivi, Revue de mode (TESTING l. 745)
- [ ] **0.579** Équipement : cocher Tous les jobs (mirage) (TESTING l. 805)
- [ ] **0.581** Revue de mode : pièces et Essayer la tenue (TESTING l. 809)
- [ ] **0.602** Collections : Obtenus récemment, clic droit emote (TESTING l. 851)
- [ ] **0.713** Peuples alliés : rangs et réputation (TESTING l. 1071)
- [ ] **0.854** Carnet de chasse : « Vu par vous », clic droit drapeau (TESTING l. 1353)
- [ ] **0.861** Fashion Report ouvert : catégories, thème, compté aux Découvertes (TESTING l. 1367)
- [ ] **0.878** Monstre lançant sort bleu : « vu lançant », « Vu par vous » (TESTING l. 1401)
- [ ] **0.900** Revue de mode : clic droit pièce grisée, menu lisible (TESTING l. 1445)
- [ ] **0.930** Magie bleue : sort de donjon « (mission de niveau …) » (TESTING l. 1505)
- [ ] **0.974** Reliques par version : Zodiaque étape 1, armures par ligne (TESTING l. 1593)
- [ ] **0.987** Suivre un succès presque fini, progression gardée après relance (TESTING l. 1619)
- [ ] **0.989** Tenues : pièces manquantes vers souhaits (TESTING l. 1623)
- [ ] **0.526** Clic droit ou survol d'un tome : comment l'obtenir (TESTING l. 1711)
- [ ] **0.491** Coffre à apparence : pièces de tenue possédées (TESTING l. 1861)
- [ ] **0.454** Titre par classe sur autre personnage (TESTING l. 2046)
- [ ] **0.454** Objets de collection et carnets affichés (TESTING l. 2047)
- [ ] **0.407** Noms du wiki en français (TESTING l. 2300)
- [ ] **0.395** Mage bleu : livre des sorts, tri par niveau (TESTING l. 2356)
- [ ] **0.390** Mauvaise haleine : tri par niveau croissant (TESTING l. 2372)

*Il faut : auberge*

- [ ] **0.357** Rapport : objets pour l'armoire (TESTING l. 514)

*Il faut : chocobo de compagnie*

- [ ] **0.340** Barde de chocobo : nom, rang, couleur (TESTING l. 399)

*Il faut : mage bleu*

- [ ] **0.367** Mage bleu : monstres d'un sort, drapeau (TESTING l. 576)
- [ ] **0.367** Sort appris par quête : aucun monstre (TESTING l. 578)
- [ ] **0.387** Mage bleu : monstres de Mauvaise haleine (TESTING l. 700)
- [ ] **0.798** Magie bleue : lire sorts manquants, chapeau sur la carte (TESTING l. 1241)
- [ ] **0.799** Mage bleu : icône au-dessus du monstre apprenant un sort (TESTING l. 1243)
- [ ] **0.863** Apprendre un sort bleu : Découvertes « appris de » (TESTING l. 1371)

*Il faut : événement saisonnier*

- [ ] **0.342** Événements : nom de la fête en cours (TESTING l. 407)
- [ ] **0.342** Quêtes de la fête cochées, Ouvrir (TESTING l. 409)

## Étape 4 : Artisanat (95)

Avec un métier d'artisan, à l'établi.

- [ ] **0.292** Fiche Craft lié : onglets Produit par / Utilisé dans (TESTING l. 180)
- [ ] **0.327** Carnet d'artisanat : calcul automatique décoché (TESTING l. 324)
- [ ] **0.338** Éditeur matéria : taux par grade (TESTING l. 392)
- [ ] **0.338** Grades proposés selon emplacement avancé (TESTING l. 394)
- [ ] **0.363** Mandats : allocations et acceptés (TESTING l. 550)
- [ ] **0.363** Accepter un mandat d'artisanat (TESTING l. 552)
- [ ] **0.552** Carnet : séquence d'une autre recette proposée (TESTING l. 751)
- [ ] **0.553** Simulateur : seuils comparés à une macro (TESTING l. 753)
- [ ] **0.554** Carnet : valeurs relevées d'un autre métier (TESTING l. 755)
- [ ] **0.559** Survol matériau : détail par rangement (TESTING l. 765)
- [ ] **0.572** Simulateur : chercher recette, ajouter action (TESTING l. 791)
- [ ] **0.573** Macros : son et dernière page seulement (TESTING l. 793)
- [ ] **0.574** Préparation : moins cher du centre de données (TESTING l. 795)
- [ ] **0.575** Préparation : ingrédient PNJ moins cher affiché (TESTING l. 797)
- [ ] **0.589** Synthèse avec séquence enregistrée : suivi du plan (TESTING l. 825)
- [ ] **0.590** Page des séquences : chercher, renommer, importer (TESTING l. 827)
- [ ] **0.591** Préparation à sous-recettes : chaîne et étapes (TESTING l. 829)
- [ ] **0.592** Que puis-je fabriquer : recherche et filtres (TESTING l. 831)
- [ ] **0.600** Fiche objet recette de livre de maître (TESTING l. 847)
- [ ] **0.608** Préparation : Lire les prix rapide (TESTING l. 863)
- [ ] **0.668** Pourcentage HQ du jeu égal au simulateur (TESTING l. 983)
- [ ] **0.669** Simulateur : qualité initiale avec ingrédients HQ (TESTING l. 985)
- [ ] **0.675** Commandes spéciales : rang, points, livraisons (TESTING l. 997)
- [ ] **0.696** Que puis-je fabriquer : avec sous-recettes (TESTING l. 1037)
- [ ] **0.699** Panneau : matières de base par provenance (TESTING l. 1043)
- [ ] **0.700** Panneau provenance : vendeurs et monnaies (TESTING l. 1045)
- [ ] **0.707** Que puis-je fabriquer : tris de rentabilité (TESTING l. 1059)
- [ ] **0.709** Que puis-je fabriquer : pages et prix (TESTING l. 1063)
- [ ] **0.710** Charger tous les prix sur longue liste (TESTING l. 1065)
- [ ] **0.711** Que puis-je fabriquer : haut de liste réaliste (TESTING l. 1067)
- [ ] **0.712** Que puis-je fabriquer : assignats collectionnables (TESTING l. 1069)
- [ ] **0.715** Livraisons spéciales : quêtes faites (TESTING l. 1075)
- [ ] **0.725** Tri par marché : ventes par jour (TESTING l. 1095)
- [ ] **0.730** Commandes spéciales Récompenses vs jeu (TESTING l. 1105)
- [ ] **0.739** Rivets en cobalt : une ligne, deux métiers (TESTING l. 1123)
- [ ] **0.782** Carnet recette 5623 cuisinier sans gel (TESTING l. 1209)
- [ ] **0.783** Kit de réparation : Raphael non utilisé (TESTING l. 1211)
- [ ] **0.784** Synthèse Kit de réparation : prochains coups (TESTING l. 1213)
- [ ] **0.785** Cocher Objets finis seulement dans listes (TESTING l. 1215)
- [ ] **0.788** Carnet en Menuisier sur Lingot de mithril : « Mauvais métier » (TESTING l. 1221)
- [ ] **0.808** Carnet d'artisanat : clic droit recette et matériau, entrée Tensho (TESTING l. 1261)
- [ ] **0.826** Clic droit objet fabricable : « Ajouter à Liste 1 » (TESTING l. 1297)
- [ ] **0.954** Listes : objets finis grise provenances ; prix du marché (TESTING l. 1553)
- [ ] **0.988** Mandats d'artisanat : Préparer, objets manquants listés (TESTING l. 1621)
- [ ] **0.516** Panneaux accrochés au jeu : même place et contenu (TESTING l. 1757)
- [ ] **0.516** Panneau suit sa fenêtre et disparaît à sa fermeture (TESTING l. 1758)
- [ ] **0.516** Croix des panneaux projection, mode, pêche (TESTING l. 1759)
- [ ] **0.509** Synthèse : Conseillé change à chaque coup (TESTING l. 1791)
- [ ] **0.509** Coup suivi donne résultat annoncé (TESTING l. 1792)
- [ ] **0.497** Comparer les coups : état de synthèse et coups (TESTING l. 1843)
- [ ] **0.495** Autotest : carnet, synthèse, récolte en OK (TESTING l. 1851)
- [ ] **0.469** Carnet : panneau en sections, barre de qualité (TESTING l. 1969)
- [ ] **0.469** Carnet : Manipulation case, repas et remède icônes (TESTING l. 1970)
- [ ] **0.468** Raphael : cache disque d'une recette (TESTING l. 1974)
- [ ] **0.468** Simulateur Raphael : déjà calculée, Recalculer (TESTING l. 1975)
- [ ] **0.468** Carnet : Qualité cible et Maximum (TESTING l. 1976)
- [ ] **0.468** Carnet : Lu dans le jeu juste (TESTING l. 1977)
- [ ] **0.468** Enregistrer comme meilleure séquence (TESTING l. 1978)
- [ ] **0.468** Case Comparer les prochains coups, croix (TESTING l. 1979)
- [ ] **0.467** Raphael dans le carnet, changement de recette fluide (TESTING l. 1983)
- [ ] **0.465** Séquence Raphael jouée donne le résultat (TESTING l. 1988)
- [ ] **0.465** Rechargement sans erreur après calcul (TESTING l. 1989)
- [ ] **0.464** Simulateur : attributs copiés, ligne Détecté (TESTING l. 1993)
- [ ] **0.464** Liste de fabrication : icônes, manquants en rouge (TESTING l. 1994)
- [ ] **0.464** Projection pendant la synthèse (TESTING l. 1995)
- [ ] **0.457** Enregistreur d'artisanat : icônes des actions (TESTING l. 2029)
- [ ] **0.452** Enregistreur sur autre classe : actions reconnues (TESTING l. 2062)
- [ ] **0.452** Projection en direct comparée à la synthèse (TESTING l. 2064)
- [ ] **0.452** Chaîne de macros avec /macrolock (TESTING l. 2065)
- [ ] **0.438** Actions interdites non proposées (TESTING l. 2152)
- [ ] **0.436** Enregistreur de rotation : fabriquer à la main (TESTING l. 2162)
- [ ] **0.435** Recette experte dans le simulateur (TESTING l. 2168)
- [ ] **0.434** Actions du simulateur comparées au jeu (TESTING l. 2173)
- [ ] **0.433** Liste de fabrication : créer, ajouter, retirer (TESTING l. 2178)
- [ ] **0.432** Collectionnables des sacs : collectionnabilité (TESTING l. 2183)
- [ ] **0.431** Préparation : prix et bénéfice (TESTING l. 2188)
- [ ] **0.428–0.429** Ouvrir dans le carnet déjà ouvert sur autre recette (TESTING l. 2193)
- [ ] **0.430** Recette au-dessus du niveau s'ouvre (TESTING l. 2196)
- [ ] **0.427** Prérequis de recette en vert ou rouge (TESTING l. 2201)
- [ ] **0.426** Ouvrir dans le carnet ferme Tensho (TESTING l. 2206)
- [ ] **0.425** Importer une macro dans le simulateur (TESTING l. 2212)
- [ ] **0.424** Préparation : stocks et Retour à l'objet (TESTING l. 2217)
- [ ] **0.423** Menu objet Artisanat : onglet Préparation (TESTING l. 2222)
- [ ] **0.421** Craft lié : déjà fabriquée ou jamais (TESTING l. 2232)
- [ ] **0.420** Simulateur : repas HQ et remède (TESTING l. 2237)
- [ ] **0.419** Collectionnable : boutons Bas Moyen Haut (TESTING l. 2242)
- [ ] **0.408** Craft lié : Préparer dans Artisanat (TESTING l. 2294)
- [ ] **0.405** Ouvrir dans le carnet d'artisanat depuis Tensho (TESTING l. 2310)

*Il faut : exploration cosmique*

- [ ] **0.666** Exploration cosmique : recette, séquence, seuils (TESTING l. 979)

*Il faut : Macro Chain*

- [ ] **0.422** Macro Chain : /nextmacro enchaîne (TESTING l. 2227)

*Il faut : mise à jour du jeu*

- [ ] **0.601** Carnet : ligne HQ, synthèse rapide, livre (TESTING l. 849)

*Il faut : mode développeur*

- [ ] **0.840** Panneau Liste 1 ouvert : ligne « List panel CraftList » (TESTING l. 1325)
- [ ] **0.497** Journal Craft condition à chaque état (TESTING l. 1844)

*Il faut : servant*

- [ ] **0.838** Liste 1 : sirop de bouleau 157/1, icône d'entrepôt (TESTING l. 1321)
- [ ] **0.841** Liste 1 : sirop de bouleau dans « Autres », icône d'entrepôt (TESTING l. 1327)

## Étape 5 : Récolte et pêche (106)

Mineur, botaniste, pêcheur ; pêche en mer à son créneau.

- [ ] **0.224** Récolte : outil PR, Estimé +N, bouton Utiliser (TESTING l. 240)
- [ ] **0.225** Panneau Aide à la récolte à droite de la fenêtre (TESTING l. 241)
- [ ] **0.226–0.228** Aide à la pêche : poissons, appâts, créneau, météo (TESTING l. 242)
- [ ] **0.230** Page Récolte : outils de l'objet en tête (TESTING l. 245)
- [ ] **0.537** Panneaux se cachent sous une fenêtre du jeu (TESTING l. 718)
- [ ] **0.560** Chaîne : Encore N pour… baisse en récoltant (TESTING l. 767)
- [ ] **0.568** Aide à la pêche : créneau et météo comparés au jeu (TESTING l. 783)
- [ ] **0.588** Récolte : heures des points ouverts, cloche (TESTING l. 823)
- [ ] **0.604** Panneau de pêche et ligne Abondance II (TESTING l. 855)
- [ ] **0.649** Récolte : poisson au harpon, lieu et alarmes (TESTING l. 945)
- [ ] **0.663** Pêche : durée d'intuition des proies (TESTING l. 973)
- [ ] **0.728** Récolte : Ensuite et cadre suivent le plan (TESTING l. 1101)
- [ ] **0.729** Récolte : ligne séparant Ensuite du plan (TESTING l. 1103)
- [ ] **0.731** Récolte : Ensuite avec PR restants (TESTING l. 1107)
- [ ] **0.733** Récolte : cadre doré sur action Ensuite (TESTING l. 1111)
- [ ] **0.734** Récolte : cadre sur Récolte abondante I (TESTING l. 1113)
- [ ] **0.735** Récolte : cadre sur Récolte abondante I (TESTING l. 1115)
- [ ] **0.740** Récolter objets de la liste : List blink (TESTING l. 1125)
- [ ] **0.741** Récolter objets de la liste sans clignotement (TESTING l. 1127)
- [ ] **0.742** Récolter objets de la liste : pas de List blink (TESTING l. 1129)
- [ ] **0.743** Récolter puis Ouvrir dans Tensho sur alarme (TESTING l. 1131)
- [ ] **0.744** Récolter objets de la liste : pas de List zero (TESTING l. 1133)
- [ ] **0.747** Récolte : plan après Calcul du plan (TESTING l. 1139)
- [ ] **0.751** Récolte : Vos listes en demandent encore (TESTING l. 1147)
- [ ] **0.752** Liste atelier : une ligne par zone (TESTING l. 1149)
- [ ] **0.753** Récolte : cocher Récolter automatiquement (TESTING l. 1151)
- [ ] **0.756** Point avec objet des listes : aide placée (TESTING l. 1157)
- [ ] **0.757** Liste atelier : replier zone, prix téléport (TESTING l. 1159)
- [ ] **0.758** Liste atelier : groupe Points à horaire (TESTING l. 1161)
- [ ] **0.760** Liste atelier affichée sans gel (TESTING l. 1165)
- [ ] **0.761** Points à horaire : ouvert ou fermé (TESTING l. 1167)
- [ ] **0.763** Récolte : plan avec moins d'actions (TESTING l. 1171)
- [ ] **0.764** Panneau atelier réduit : ligne et cadre (TESTING l. 1173)
- [ ] **0.765** Panneau réduit, rechargement : ligne reste (TESTING l. 1175)
- [ ] **0.815** Collectionnable avec seuils : compétence du plan encadrée sur barre (TESTING l. 1275)
- [ ] **0.816** Collectionnable avec seuils : bouton du plan encadré en fenêtre (TESTING l. 1277)
- [ ] **0.827** Liste de suivi, points à horaire : minuteurs verts et rouges (TESTING l. 1299)
- [ ] **0.831** Deux points de récolte enchaînés : le second propose un plan (TESTING l. 1307)
- [ ] **0.834** Clic droit Haricots émeraude : « Y aller » et drapeau (TESTING l. 1313)
- [ ] **0.835** Point à folklore non lu : « Y aller » vers autre point (TESTING l. 1315)
- [ ] **0.859** Pêcher poisson commun : « ni heure ni météo (jeu) » (TESTING l. 1363)
- [ ] **0.877** Mooch : appât = premier poisson, « Vu par vous » (TESTING l. 1399)
- [ ] **0.880** Pêche en mer : pas de ligne « Ocean schedule check » (TESTING l. 1405)
- [ ] **0.887** Pêcher : temps de touche « Vu par vous », touches vues (TESTING l. 1419)
- [ ] **0.891** Harpon : liste du point, poisson en vue coloré, trous (TESTING l. 1427)
- [ ] **0.892** Harpon : « Poissons de ce point » liste fixe, en vue (TESTING l. 1429)
- [ ] **0.893** Harpon : icônes des poissons, cadre coloré en vue (TESTING l. 1431)
- [ ] **0.894** Harpon Tamamizu : 6 poissons ; zones de trous Mer de Rubis (TESTING l. 1433)
- [ ] **0.896** Harpon : même ombre gardée d'une prise à l'autre (TESTING l. 1437)
- [ ] **0.897** Harpon : lignes « 1 M >>> », liste « S >> Nom » (TESTING l. 1439)
- [ ] **0.908** Lignes « vu en jeu » remplacent « vu par vous » (TESTING l. 1461)
- [ ] **0.917** Pêche en mer : état par port, panneau de voyage (TESTING l. 1479)
- [ ] **0.918** Pêche en mer : panneau, appât encadré, chronomètre, spectral (TESTING l. 1481)
- [ ] **0.933** Intuition du pêcheur : lignes Découvertes, « touche avec appât » (TESTING l. 1511)
- [ ] **0.946** Voyage en mer : moment Tensho égal au soleil/lune du jeu (TESTING l. 1537)
- [ ] **0.958** Pêche : touche dépassée en rouge, objectif 5/5, Kugane (TESTING l. 1561)
- [ ] **0.959** Détroit ouest de Zorgor : météo et météo d'avant (TESTING l. 1563)
- [ ] **0.961** Voyage en mer : cible sur poissons d'objectif en cours (TESTING l. 1567)
- [ ] **0.962** Courant spectral : Découvertes « pêché avec … juste avant » (TESTING l. 1569)
- [ ] **0.963** Panneau de voyage : œil sur poissons à intuition (TESTING l. 1571)
- [ ] **0.964** Voyage : « … pts », résultat « Ocean voyage result » (TESTING l. 1573)
- [ ] **0.982** Poisson collectionnable : ligne « Collectionnable », assignats au survol (TESTING l. 1609)
- [ ] **0.535** Équilibré grisé non cliquable, infobulle, Sûr choisi (TESTING l. 1671)
- [ ] **0.534** Balance grisée si mêmes chiffres que Sûr (TESTING l. 1678)
- [ ] **0.534** Panneau fluide en changeant de cran (TESTING l. 1679)
- [ ] **0.533** Chaque cran : suite de coups et PR (TESTING l. 1683)
- [ ] **0.533** Nombre de tentatives de la suite correct (TESTING l. 1684)
- [ ] **0.532** Panneau collectionnable sans répétition, survol du calcul (TESTING l. 1688)
- [ ] **0.532** Moyenne ne baisse pas d'un cran à l'autre (TESTING l. 1689)
- [ ] **0.531** Chaque icône change la phrase et ses chiffres (TESTING l. 1693)
- [ ] **0.531** Sûr pas plus bas qu'Équilibré (TESTING l. 1694)
- [ ] **0.529** Trois icônes de risque, cran coloré, description (TESTING l. 1698)
- [ ] **0.529** Sûr : plan sans chance ; autres : moyenne et pire cas (TESTING l. 1699)
- [ ] **0.529** Le coup conseillé suit le cran (TESTING l. 1700)
- [ ] **0.527** Sous niveau 95 : jamais proposé (TESTING l. 1705)
- [ ] **0.523** Deux types d'assignats : phrase types différents (TESTING l. 1725)
- [ ] **0.522** Bénédiction de la nature : prête dans N s (TESTING l. 1729)
- [ ] **0.522** Bénédiction redevient proposée (TESTING l. 1730)
- [ ] **0.520** Sous niveau 90 : pas de cette ligne (TESTING l. 1735)
- [ ] **0.520** Étalage des connaissances proposé avant Récolte (TESTING l. 1736)
- [ ] **0.519** Cristaux : Bénédictions proposées sans nombre d'objets (TESTING l. 1740)
- [ ] **0.519** Autres objets : plan de PR habituel (TESTING l. 1741)
- [ ] **0.518** Point d'Ostrakon : ouvert pas pour vous, tome (TESTING l. 1745)
- [ ] **0.518** Point légendaire tome lu : ouvert maintenant (TESTING l. 1746)
- [ ] **0.518** Point au-dessus du niveau : ligne de niveaux (TESTING l. 1747)
- [ ] **0.517** Nœud à PR pleins : actions proposées existantes (TESTING l. 1751)
- [ ] **0.517** Action non apprise non proposée (TESTING l. 1752)
- [ ] **0.517** Cristal : supp. 0 % (TESTING l. 1753)
- [ ] **0.515** Nœud PR ≧ 770 : plan et effet du lieu (TESTING l. 1766)
- [ ] **0.515** Dépenser des PR : tentatives restent à 5 (TESTING l. 1767)
- [ ] **0.515** Seuil non atteint : ligne il manque N (TESTING l. 1768)
- [ ] **0.515** Effet de quantité : valeur du jeu déjà avec effet ? (TESTING l. 1769)
- [ ] **0.515** Cristal : supp. 0 % et un plan (TESTING l. 1770)
- [ ] **0.515** Chaque effet du lieu a sa ligne (TESTING l. 1771)
- [ ] **0.515** Liste de fabrication : il en manque N, nœuds (TESTING l. 1772)
- [ ] **0.514** Barre de PR en mineur : remplissage, tic (TESTING l. 1780)
- [ ] **0.483** Raison solide refusée non conseillée (TESTING l. 1897)

*Il faut : mode développeur*

- [ ] **0.650** Récolter sans action : journal Reading gp (TESTING l. 947)
- [ ] **0.830** Clic droit objet récolté : « Placer le drapeau », « Go there missing » (TESTING l. 1305)
- [ ] **0.869** Pêche, réduction, remise de quête : lignes « Reading … » (TESTING l. 1383)
- [ ] **0.874** Relevés par sonde : pêche, réduction, île, aéronef, coffres… (TESTING l. 1393)
- [ ] **0.890** Harpon : « vu par vous », ombres vues, trou sur carte (TESTING l. 1425)
- [ ] **0.898** Harpon prise multiple : « 2 at once » au journal (TESTING l. 1441)
- [ ] **0.965** Voyage : « bite strength unknown » au journal (TESTING l. 1575)

*Il faut : niveau 90*

- [ ] **0.520** Niveau 90 : ligne tentative gratuite 50 % (TESTING l. 1734)

*Il faut : niveau 95*

- [ ] **0.527** Niveau 95 : Toucher d'amorçage proposé après Inspection (TESTING l. 1704)

*Ajouté en 0.1011*

- [ ] **0.1011** C10 : chrono de pêche relancé tout seul à chaque lancer, aussi en mer (TESTING l. 1667)

## Étape 6 : Carte, téléportation, monde ouvert (126)

En zone ouverte : carte Tensho, téléportation, ALEA, chasse, cartes au trésor, panoramas, monstres proches.

- [ ] **0.259 / 0.262** Paramètres carte : infobulles et bouton ↺ (TESTING l. 155)
- [ ] **0.308** Menus par-dessus la carte : zoom lisible (TESTING l. 157)
- [ ] Relevé des icônes de quête 71141 / 71151 (TESTING l. 236)
- [ ] **0.325** Carte : remise à zéro des tailles d'icônes (TESTING l. 302)
- [ ] **0.328** Repère au bord du drapeau : clic centre la carte (TESTING l. 332)
- [ ] **0.328** Drapeau ou zone masqués : plus de repère (TESTING l. 333)
- [ ] **0.328** Icônes carte : ↺ d'une ligne (TESTING l. 334)
- [ ] **0.328** Liste des panoramas : clic droit propose alarme (TESTING l. 337)
- [ ] **0.328** Ville chargée : carte fluide (TESTING l. 338)
- [ ] **0.361** Carnet d'exploration : compteur comme le jeu (TESTING l. 538)
- [ ] **0.361** Panoramas possibles maintenant (TESTING l. 540)
- [ ] **0.373** Lien de carte du chat : drapeau, éthérite (TESTING l. 614)
- [ ] **0.373** Carte sans éthérite : message clair (TESTING l. 616)
- [ ] **0.538** Infobulle du jeu sur la carte : effacement (TESTING l. 722)
- [ ] **0.542** Carte : zoom à l'ouverture (TESTING l. 731)
- [ ] **0.543** Carte : réglages des noms et étiquettes (TESTING l. 733)
- [ ] **0.545** Carte : molette sur colonne d'outils (TESTING l. 737)
- [ ] **0.546** Carte : clic gauche pose le drapeau (TESTING l. 739)
- [ ] **0.547** Carte : bouton Couches (TESTING l. 741)
- [ ] **0.548** ALEA à bonus d'expérience (TESTING l. 743)
- [ ] **0.551** Monstres : drapeau à la position (TESTING l. 749)
- [ ] **0.563** Météo : tableau de deux zones (TESTING l. 773)
- [ ] **0.582** Quêtes : filtres Disponibles et Cette zone (TESTING l. 811)
- [ ] **0.583** Quêtes : épopée lointaine et À prendre ensuite (TESTING l. 813)
- [ ] **0.586** Carte : aile de vol dans le menu des zones (TESTING l. 819)
- [ ] **0.596** Carte : position des noms d'icônes comme le jeu (TESTING l. 839)
- [ ] **0.597** Carte : recherche de lieux, PNJ, ressources (TESTING l. 841)
- [ ] **0.617** Carte agrandie avec fenêtre du jeu ouverte (TESTING l. 881)
- [ ] **0.618** Carte agrandie avec inventaire ouvert (TESTING l. 883)
- [ ] **0.619** Le Tillac : troisième menu nom et aérodrome (TESTING l. 885)
- [ ] **0.620** Le Tillac : menu au-dessus de la carte (TESTING l. 887)
- [ ] **0.621** L'Entrepont : lignes suivent la carte du jeu (TESTING l. 889)
- [ ] **0.622** Sans sous-lieu : une seule ligne (TESTING l. 891)
- [ ] **0.624** Carte : menu qui la cache, refermer, attendre (TESTING l. 895)
- [ ] **0.627** Quête du journal : carte centrée puis rouverte sur soi (TESTING l. 901)
- [ ] **0.629** Recliquer même quête, carte reste dessus (TESTING l. 905)
- [ ] **0.633** Forgeron fantomatique loin : Tuliyollal centrée (TESTING l. 913)
- [ ] **0.634** Quête de donjon : carte de l'entrée (TESTING l. 915)
- [ ] **0.635** Recliquer une quête, touche de carte (TESTING l. 917)
- [ ] **0.644** Carte : coordonnées, distance, drapeau hors vue (TESTING l. 935)
- [ ] **0.645** Coût d'éthérite au survol, non harmonisée grisée (TESTING l. 937)
- [ ] **0.647** Carte : classe des points légendaires et éphémères (TESTING l. 941)
- [ ] **0.660** Vieille Sharlayan : largeur des noms comme le jeu (TESTING l. 967)
- [ ] **0.661** Téléportation et carte ; dézoom ville (TESTING l. 969)
- [ ] **0.662** Téléportations successives sans plantage (TESTING l. 971)
- [ ] **0.665** Icônes de quêtes : états sans numéro (TESTING l. 977)
- [ ] **0.670** Conditions des rangs S lisibles et filtrées (TESTING l. 987)
- [ ] **0.677** Cartes & repères : 4 onglets et favoris (TESTING l. 1001)
- [ ] **0.682** Carte : décocher ressources Félicité insulaire (TESTING l. 1011)
- [ ] **0.689** Mandats : infobulle au survol, fenêtre étroite (TESTING l. 1023)
- [ ] **0.698** Mandats : clic droit, drapeau et téléportation (TESTING l. 1041)
- [ ] **0.704** Clic droit sur lignes avec coordonnées (TESTING l. 1053)
- [ ] **0.721** ALEA : lieu comparé à la carte du jeu (TESTING l. 1087)
- [ ] **0.749** Ouvrir la carte plusieurs fois : Slow frame (TESTING l. 1143)
- [ ] **0.754** Carte : point à horaire fermé, délai (TESTING l. 1153)
- [ ] **0.762** Liste atelier : clic droit Y aller (TESTING l. 1169)
- [ ] **0.777** Peau d'hippogriffe : clic droit, éthérite proche (TESTING l. 1199)
- [ ] **0.793** Mandat sans carte du jeu : zone sur carte Tensho (TESTING l. 1231)
- [ ] **0.795** Objet de monstre en liste : patte sur carte près du monstre (TESTING l. 1235)
- [ ] **0.796** Monstre du bestiaire proche : livre sur la carte, survol (TESTING l. 1237)
- [ ] **0.800** Monstre lâchant mascotte ou rouleau manquant : gemme sur carte (TESTING l. 1245)
- [ ] **0.803** Carte Tensho : zones non découvertes vierges comme le jeu (TESTING l. 1251)
- [ ] **0.807** Monde › Découvertes : chercher monstre, exporter puis importer (TESTING l. 1259)
- [ ] **0.810** Limsa Dunette : marchands absents retirés de la carte Tensho (TESTING l. 1265)
- [ ] **0.811** Limsa Tillac et Dunette : seuls marchands présents sur carte (TESTING l. 1267)
- [ ] **0.813** Valider une étape de quête : cible quitte la carte Tensho (TESTING l. 1271)
- [ ] **0.814** Cible hors vue en haut à gauche : icône de bord visible (TESTING l. 1273)
- [ ] **0.819** Yak laineux et lait demandé : patte carte et liste (TESTING l. 1283)
- [ ] **0.824** Liste de suivi par zone : téléportation, gils et montant (TESTING l. 1293)
- [ ] **0.828** Cocatrix : patte ; Limsa marchands ; Découvertes se remplit (TESTING l. 1301)
- [ ] **0.829** Limsa : plus de marchand fantôme, marchands permanents présents (TESTING l. 1303)
- [ ] **0.833** « Y aller » Arrière-pays dravanien : Idyllée puis éthernet (TESTING l. 1311)
- [ ] **0.844** Mor Dhona quête cuisinier : flèche vers le bas, réglage (TESTING l. 1333)
- [ ] **0.845** Journal, quête cuisinier Mor Dhona, « Carte » : zone remplit vue (TESTING l. 1335)
- [ ] **0.846** Journal, « Carte » sur quête : centrage sans changer le zoom (TESTING l. 1337)
- [ ] **0.847** « Carte » sur quête, faire l'étape : cible disparaît vite (TESTING l. 1339)
- [ ] **0.848** Étape de quête, carte du jeu fermée : zone suivante apparaît (TESTING l. 1341)
- [ ] **0.849** Mer de Rubis ou Mor Dhona : chevron orange de hauteur (TESTING l. 1343)
- [ ] **0.850** Changer de zone, M : carte au zoom réglé (TESTING l. 1345)
- [ ] **0.851** Icône plus haute ou basse : chevron plus discret (TESTING l. 1347)
- [ ] **0.853** Tillac, clic droit cible de quête : « Éthérite la plus proche » (TESTING l. 1351)
- [ ] **0.856** Près d'une ALÉA ou chasse : chercher son nom aux Découvertes (TESTING l. 1357)
- [ ] **0.860** Désynthèse, coffre, pêche : Découvertes compte ces relevés (TESTING l. 1365)
- [ ] **0.895** Carte › calques : décocher Botaniste masque ses points (TESTING l. 1435)
- [ ] **0.902** Calques : « Rien » et « Tout » sur lignes de monstres (TESTING l. 1449)
- [ ] **0.905** Coordonnées écrites avec un point partout (TESTING l. 1455)
- [ ] **0.929** Vue panoramique Stormblood+ : « Carnet n° », numéros en liste (TESTING l. 1503)
- [ ] **0.956** Carte, collections, guide, monstres proches comme avant (TESTING l. 1557)
- [ ] **0.960** Carte Zorgor : trou de pêche sans « point légendaire » (TESTING l. 1565)
- [ ] **0.966** Carte : décocher « Quêtes pas encore disponibles » (TESTING l. 1577)
- [ ] **0.967** Zone de quête sur autre étage : chevron discret (TESTING l. 1579)
- [ ] **0.968** Mandat de combat commencé : zone acceptée disparaît, cibles marquées (TESTING l. 1581)
- [ ] **0.971** Conditions rangs S : « ALÉA vu en jeu en x, y » (TESTING l. 1587)
- [ ] **0.977** Téléportation, carte Tensho : quêtes non acceptées en rouge (TESTING l. 1599)
- [ ] **0.983** Conditions rangs S : clic droit, menu drapeau et éthérite (TESTING l. 1611)
- [ ] **0.985** Carte Tensho : icône de bête manquante, survol, clic drapeau (TESTING l. 1615)
- [ ] **0.535** Carte sans barre de titre : déplacer et fermer (TESTING l. 1672)
- [ ] **0.499** Lien vers carte : carte devant Tensho (TESTING l. 1835)
- [ ] **0.491** Carte : panoramas et courants éthérés (TESTING l. 1862)
- [ ] **0.454** Carte Tensho à la place de celle du jeu (TESTING l. 2048)
- [ ] **0.453** Téléportation : coûts, mandats, chocobo lus (TESTING l. 2057)
- [ ] **0.447** Train de chasse : Envoyer grisé sans drapeau (TESTING l. 2101)
- [ ] **0.446** Annoncer marques A et S proches (TESTING l. 2111)
- [ ] **0.446** Copier l'annonce avec lien drapeau (TESTING l. 2113)
- [ ] **0.442** Train de chasse : enregistrer les marques (TESTING l. 2132)
- [ ] **0.441** Marques de chasse sur la carte Tensho (TESTING l. 2137)
- [ ] **0.437** Coller liste Siren ou Faloop en favoris (TESTING l. 2157)
- [ ] **0.401** Survol d'un panorama : météo, pas d'erreur (TESTING l. 2326)

*Il faut : carte au trésor*

- [ ] **0.384** Carte au trésor : coffre ouvert, icône disparaît (TESTING l. 685)

*Il faut : contrat de chasse*

- [ ] **0.440** Contrat de chasse : cibles et victimes (TESTING l. 2142)

*Il faut : fête saisonnière*

- [ ] **0.864** Fête : « Fêtes vues » ; panorama inscrit avec météo (TESTING l. 1373)
- [ ] **0.879** Fête « Vue en cours » ; panorama « Inscrit par vous » (TESTING l. 1403)

*Il faut : livre du Zodiaque en cours*

- [ ] **0.1002** Livre du Zodiaque : bouclier sur cibles proches (TESTING l. 1649)

*Il faut : mode développeur*

- [ ] **0.791** Jouer avec la carte Tensho ouverte, relire « Slow frame » (TESTING l. 1227)
- [ ] **0.792** Prendre un mandat, carte fermée, relire « Reading questMarkers » (TESTING l. 1229)
- [ ] **0.797** ALÉA en cours : étoiles ; mandat de combat, « Reading monsterEvents » (TESTING l. 1239)
- [ ] **0.801** Jouer avec la carte ouverte, relire « Slow frame » (TESTING l. 1247)
- [ ] **0.805** Jouer avec la carte Tensho ouverte, relire « Slow frame » (TESTING l. 1255)
- [ ] **0.806** Jouer avec la carte ouverte, relire « Slow frame » (TESTING l. 1257)
- [ ] **0.809** Vaincre monstres, ramasser butin, relire « Reading messages » (TESTING l. 1263)
- [ ] **0.818** Approcher un yak laineux, relire « Nearby monsters » (TESTING l. 1281)
- [ ] **0.823** Limsa Dunette : marchands présents, relire « Map vendors on map » (TESTING l. 1291)
- [ ] **0.825** Quête « Le pari » : carte Tillac, Entrepont, « Reading eventMarkers » (TESTING l. 1295)
- [ ] **0.842** Carte du jeu à Mor Dhona : « Reading mapMarkers » (TESTING l. 1329)
- [ ] **0.843** Icône avec flèche : « Reading mapMarkers » et « eventMarkers » (TESTING l. 1331)
- [ ] **0.950** Changer de zone, carte ouverte : moins d'à-coups, une ligne (TESTING l. 1545)

## Étape 7 : Compagnie libre, logement, île (56)

Atelier de compagnie, sous-marins, aéronefs, coffre, jardin, île sanctuaire.

- [ ] **0.691** Panneau projet : options, stock, combat (TESTING l. 1027)
- [ ] **0.693** Panneau en combat et sous fenêtre du jeu (TESTING l. 1031)
- [ ] **0.444** Annuaire : ajouter maison et appartement (TESTING l. 2122)

*Il faut : aéronef*

- [ ] **0.919** Aéronef : onglet navires « Dernier voyage » (TESTING l. 1483)

*Il faut : compagnie libre*

- [ ] **0.664** Atelier : chercher projet, comparer matériaux (TESTING l. 975)
- [ ] **0.676** Atelier : colonne En stock comme l'infobulle (TESTING l. 999)
- [ ] **0.694** Atelier : suivre toutes les parties d'une coque (TESTING l. 1033)
- [ ] **0.727** Atelier coque Bronco : 4 parties, croix (TESTING l. 1099)
- [ ] **0.737** Atelier matériaux : clic droit, menu ouvert (TESTING l. 1119)
- [ ] **0.786** Établi atelier : livraison matériaux projet (TESTING l. 1217)
- [ ] **0.787** Livraison à l'établi : panneau d'atelier, reste à livrer (TESTING l. 1219)
- [ ] **0.789** Liste d'atelier, « Fabrications restantes » : ordre lingots, plaques, livraisons (TESTING l. 1223)
- [ ] **0.790** « Fabrications restantes » : étapes repliables, lignes « 0 / 36 » (TESTING l. 1225)

*Il faut : compagnie libre, aéronef*

- [ ] **0.1003** Retour d'aéronef : Découvertes ; combat lignes Item gain (TESTING l. 1651)

*Il faut : deux compagnies libres*

- [ ] **0.603** Deux compagnies relevées : deux cadres datés (TESTING l. 853)

*Il faut : dirigeable*

- [ ] **0.315** Dirigeable : stats, capacité, rang des pièces (TESTING l. 230)
- [ ] **0.336** Dirigeable : survol de n objets, filtre par objet (TESTING l. 376)
- [ ] **0.337** Planificateur dirigeable déconnecté puis reconnecté (TESTING l. 385)
- [ ] **0.337** Meilleurs trajets dirigeable par expérience (TESTING l. 386)

*Il faut : Félicité insulaire*

- [ ] **0.678** Félicité insulaire : mots comme le jeu (TESTING l. 1003)
- [ ] **0.679** Félicité insulaire : « ! » et PNJ sur la carte (TESTING l. 1005)
- [ ] **0.680** Félicité insulaire : Laracheteuse et Marchandréa (TESTING l. 1007)
- [ ] **0.681** Félicité insulaire : 5 PNJ et boutiques (TESTING l. 1009)
- [ ] **0.684** Carte Affichage : types d'icônes Félicité (TESTING l. 1015)
- [ ] **0.685** Félicité insulaire : Pâturage, animaux (TESTING l. 1017)
- [ ] **0.686** Félicité insulaire : fabrique et besoins (TESTING l. 1019)

*Il faut : jardin*

- [ ] **0.540** Journal du jardin : graines de centaurée (TESTING l. 727)
- [ ] **0.978** Jardin : planter, récolter, ligne « retiré du journal » (TESTING l. 1601)
- [ ] **0.997** Butin une ligne d'état ; jardin récolter seul le mûr (TESTING l. 1639)
- [ ] **0.461** Jardin : Entretenu et Retirer depuis la case (TESTING l. 2006)
- [ ] **0.460** Replanter : ligne remplacée ; pot (TESTING l. 2011)
- [ ] **0.460** Phrase d'un joueur n'ajoute rien (TESTING l. 2012)
- [ ] **0.460** Entretenir et récolter : Garden chat capture (TESTING l. 2013)
- [ ] **0.456** Planter une paire : graines croisées (TESTING l. 2035)
- [ ] **0.449** Comparer heure de récolte d'une graine (TESTING l. 2090)
- [ ] **0.448** Journal de jardinage : parcelle, Mûr le (TESTING l. 2094)
- [ ] **0.448** Entretien : ligne entretenir, Entretenu repousse (TESTING l. 2096)

*Il faut : logement avec jardin*

- [ ] **0.616** Jardin : entretien et plante arrachée (TESTING l. 879)

*Il faut : sous-marin*

- [ ] **0.315** Montages : statistiques d'un vaisseau lu (TESTING l. 223)
- [ ] **0.315** Meilleurs trajets : secteurs débloqués seulement (TESTING l. 226)
- [ ] **0.334** Montages : palier non confirmé secteur 86/130 (TESTING l. 361)
- [ ] **0.335** Meilleurs trajets : bouton Prix du marché (TESTING l. 366)
- [ ] **0.335** Universalis coupé : bouton grisé, revente PNJ (TESTING l. 368)
- [ ] **0.335** Surveillance palier 3 change le classement (TESTING l. 370)
- [ ] **0.337** Sous-marins : « ≥ » ne revient pas après survols (TESTING l. 387)
- [ ] **0.862** Sous-marin rentré : Découvertes compte le butin par secteur (TESTING l. 1369)
- [ ] **0.934** Sous-marin rentré : Découvertes « Voyage de sous-marin » (TESTING l. 1513)

*Il faut : sous-marin, mode développeur*

- [ ] **0.1007** Atelier : ouvrir fenêtre des sous-marins, Submarine voyage check (TESTING l. 1659)

*Il faut : île sanctuaire*

- [ ] **0.667** Île sanctuaire : ressources sur la carte Tensho (TESTING l. 981)
- [ ] **0.671** Île sanctuaire : lire agenda, valeurs du jeu (TESTING l. 989)
- [ ] **0.672** Île sanctuaire : proposer un agenda (TESTING l. 991)
- [ ] **0.673** Île sanctuaire : matériaux d'agenda (TESTING l. 993)
- [ ] **0.674** Île sanctuaire : entrain sur ce cycle (TESTING l. 995)
- [ ] **0.692** Île : panneau projet d'atelier et liste (TESTING l. 1029)
- [ ] **0.901** Île sanctuaire : icônes d'animaux, calque « Animaux de l'île » (TESTING l. 1447)
- [ ] **0.932** Île, carte : animal sauvage, taille et produits au pâturage (TESTING l. 1509)

## Étape 8 : Mini-jeux, escouade, chocobo, dresseur (20)

Gold Saucer, Triple Triad, Mini Cactpot, escouade, chocobo, Dédale bestial hors combat.

- [ ] **0.571** Mini Cactpot : comparer la table au ticket (TESTING l. 789)
- [ ] **0.690** Dresseur Bêtes : colonne Capture au bestiaire (TESTING l. 1025)
- [ ] **0.984** Dresseur Bêtes : lieux vus, clic droit drapeau (TESTING l. 1613)
- [ ] **0.1001** Entrer dans le Dédale : Competitive cut on, journal propre (TESTING l. 1647)
- [ ] **0.510** Triple Triade : conseils PNJ seulement, pas contre joueur (TESTING l. 1786)
- [ ] **0.500** Dresseur Bêtes : 50 bêtes et stats (TESTING l. 1828)
- [ ] **0.500** Compteur bêtes obtenues égal au jeu (TESTING l. 1829)
- [ ] **0.500** Plateaux : niveau, équipe, bonus (TESTING l. 1830)
- [ ] **0.500** Faiblesses comparées à la fiche ennemie (TESTING l. 1831)
- [ ] **0.493** Triple Triade : colonne Disponible des PNJ (TESTING l. 1855)
- [ ] **0.493** Masquer PNJ pas disponibles (TESTING l. 1856)
- [ ] **0.491** Défi Triple Triade : règles lues (TESTING l. 1860)

*Il faut : dresseur*

- [ ] **0.804** Dresseur : lire bestiaire wiki, icône au-dessus des bêtes manquantes (TESTING l. 1253)
- [ ] **0.980** Dresseur › Équipe : bêtes, faiblesses, étoiles des ennemis (TESTING l. 1605)
- [ ] **0.981** Plateau du Dédale : panneau Tensho, bêtes, disparition au combat (TESTING l. 1607)

*Il faut : escouade*

- [ ] **0.333** Mission d'escouade : nom et heure de fin (TESTING l. 355)
- [ ] **0.333** Entraînement d'escouade, ou aucun en cours (TESTING l. 357)
- [ ] **0.455** Mission d'escouade : alarme automatique (TESTING l. 2042)

*Il faut : mode développeur*

- [ ] **0.501** Sonde : bestiaire, Formation de l'équipe (TESTING l. 1824)

*Il faut : mode développeur, dresseur*

- [ ] **0.979** Dédale : plateaux et combats, « Labyrinth reading » (TESTING l. 1603)

## Étape 9 : Rappels, alarmes, minuteurs, Discord (55)

Liés aux horaires du jeu : à faire quand le moment arrive (réinitialisations, minuteurs).

- [ ] **0.300** Rappels comparés au calendrier du jeu (TESTING l. 200)
- [ ] **0.300** Icône d'alarme dans la barre du serveur (TESTING l. 204)
- [ ] **0.301** Survol icône : infobulle par-dessus la carte (TESTING l. 207)
- [ ] **0.302** Barre du serveur affiche le temps restant (TESTING l. 210)
- [ ] **0.325** Alarmes : Tout supprimer demande confirmation (TESTING l. 300)
- [ ] **0.326** Alarmes : Tout supprimer par section (TESTING l. 310)
- [ ] **0.372** Rappels : livre de Khloe et contrats de chasse (TESTING l. 606)
- [ ] **0.375** Vos tâches : ajouter, cocher, compteur (TESTING l. 627)
- [ ] **0.375** Panneau à faire à l'écran (TESTING l. 629)
- [ ] **0.381** /tensho todo ajoute une tâche (TESTING l. 666)
- [ ] **0.382** Carnet d'objectifs : nombre de défis (TESTING l. 671)
- [ ] **0.382** Enclave de Doman : don et plafond (TESTING l. 673)
- [ ] **0.386** Horloges Tokyo et New York (TESTING l. 694)
- [ ] **0.386** Alarme à date dans deux minutes (TESTING l. 695)
- [ ] **0.556** Alarme avec avance puis à l'heure (TESTING l. 759)
- [ ] **0.557** Alarmes : modifier, Dans 5 min, date refusée (TESTING l. 761)
- [ ] **0.558** Rappels cochés seuls après quêtes de tribu (TESTING l. 763)
- [ ] **0.577** Alarmes quotidienne et à la remise à zéro (TESTING l. 801)
- [ ] **0.584** Alarmes : groupe de Rappels sans case Discord (TESTING l. 815)
- [ ] **0.606** Plafond de la semaine : une seule alerte (TESTING l. 859)
- [ ] **0.687** Rappels : 5 onglets complets (TESTING l. 1021)
- [ ] **0.755** Liste atelier : Ajouter les alarmes (TESTING l. 1155)
- [ ] **0.759** Alarme : Ouvrir dans Tensho ouvre la fiche (TESTING l. 1163)
- [ ] **0.771** Prochaine alarme : Ouvrir dans Tensho (TESTING l. 1187)
- [ ] **0.772** Liste atelier : alarmes Minerai de sombracier (TESTING l. 1189)
- [ ] **0.773** Alarme qui sonne : Ouvrir dans Tensho (TESTING l. 1191)
- [ ] **0.774** Alarme : clic droit sur nom dans le chat (TESTING l. 1193)
- [ ] **0.778** Créer alarme : case Discord décochée (TESTING l. 1201)
- [ ] **0.780** Alarme : clic droit sur la ligne Minerai (TESTING l. 1205)
- [ ] **0.781** Alarme : clic droit, Context menu opened (TESTING l. 1207)
- [ ] **0.812** Alarme : clic droit sur l'objet, menu avec « Tensho » (TESTING l. 1269)
- [ ] **0.832** Alarmes du sel gemme réajoutées : « objet réuni : muette » (TESTING l. 1309)
- [ ] **0.837** Clic droit morille d'alarme : entrée Tensho ; joueur : aucune (TESTING l. 1319)
- [ ] **0.906** Horaires ET, comptes à rebours et voyages inchangés (TESTING l. 1457)
- [ ] **0.910** Barre d'infos : entrée « Tensho Assistant (Currencies) » (TESTING l. 1465)
- [ ] **0.936** Rappels, « Vos personnages » : colonne « Autres », escouade (TESTING l. 1517)
- [ ] **0.986** Gils du jour dans la barre du serveur, survol (TESTING l. 1617)
- [ ] **0.514** Fenêtre de suivi : alarmes, objet, carte, croix (TESTING l. 1777)
- [ ] **0.514** Tâches à l'écran : cocher, caché en combat (TESTING l. 1778)
- [ ] **0.458** Tâches à l'écran dans la liste des rappels (TESTING l. 2024)
- [ ] **0.453** Rappels à l'écran titre sans zéros (TESTING l. 2054)
- [ ] **0.448** Rappel non répété après redémarrage (TESTING l. 2097)
- [ ] **0.443** Livre d'or : tâches à faire et à réclamer (TESTING l. 2127)
- [ ] **0.439** Livre d'or : chances de lignes (TESTING l. 2147)
- [ ] **0.415** Fenêtre de suivi épinglée : alarmes actives (TESTING l. 2263)

*Il faut : deux personnages*

- [ ] **0.374** Rappels › Vos personnages avec deux persos (TESTING l. 620)
- [ ] **0.374** Cases des Rappels propres à chaque perso (TESTING l. 622)

*Il faut : salon Discord de test*

- [ ] **0.319** Discord : catégorie décochée n'envoie plus (TESTING l. 254)
- [ ] **0.319** Discord : rôle mentionné, @everyone neutralisé (TESTING l. 256)
- [ ] **0.319** Discord : effacer l'adresse pendant un envoi (TESTING l. 257)
- [ ] **0.366** Discord : message Mission prête (TESTING l. 572)

*Il faut : servant*

- [ ] **0.570** Rappels : tâches de servants, heures et alarmes (TESTING l. 787)

*Il faut : webhook Discord*

- [ ] **0.564** Discord : message avec cinq balises, heures correctes (TESTING l. 775)
- [ ] **0.578** Rappels avec Discord : ligne décochée sans message (TESTING l. 803)
- [ ] **0.598** Discord : un salon par catégorie (TESTING l. 843)

## Étape 10 : Missions et instances (28)

Outil de mission, roulettes, donjons, Eurêka, Bozja, donjons profonds, Carnaval masqué.

- [ ] **0.325** Panneau Duty Finder : recherche par identifiant, ↻ (TESTING l. 298)
- [ ] **0.326** Duty Finder, Garland coupé puis rallumé (TESTING l. 308)
- [ ] **0.327** Pas de ligne de discussion en mission (TESTING l. 325)
- [ ] **0.355** Filtre Non terminés des contenus (TESTING l. 503)
- [ ] **0.356** Missions aléatoires du jour passent à Faite (TESTING l. 507)
- [ ] **0.356** Colonne Bonus de rôle dans l'outil de mission (TESTING l. 509)
- [ ] **0.366** Alerte mission prête à l'ouverture (TESTING l. 570)
- [ ] **0.372** Mission aléatoire : Faite ou À faire (TESTING l. 608)
- [ ] **0.648** Butin des contenus : Palais des morts débloqué par (TESTING l. 943)
- [ ] **0.659** Carte rouverte après scène d'entrée en contenu (TESTING l. 965)
- [ ] **0.683** Grande Mascarade : niveaux, ennemis, temps (TESTING l. 1013)
- [ ] **0.708** Armes reliques : étapes et livre en cours (TESTING l. 1061)
- [ ] **0.716** Armes reliques : étapes de chaque lignée (TESTING l. 1077)
- [ ] **0.717** Eureka et Bozja : positions ; onglet Résistance (TESTING l. 1079)
- [ ] **0.718** Donjons profonds : poteries et étages (TESTING l. 1081)
- [ ] **0.720** Reliques Eurêka Armures : pièces et stocks (TESTING l. 1085)
- [ ] **0.723** Masked Carnivale : épreuves faites (TESTING l. 1091)
- [ ] **0.873** Donjon sans fond, coffre : coffre de mission pour l'étage (TESTING l. 1391)
- [ ] **0.876** Coffre de mission ouvert : fiche « Coffre (vu par vous) » (TESTING l. 1397)
- [ ] **0.935** Ennemis vaincus, coffre en mission : « vaincu », « coffre » (TESTING l. 1515)
- [ ] **0.937** Donjon profond : butin des coffres noté après 256 objets (TESTING l. 1519)
- [ ] **0.975** Page Eurêka et Bozja : Croissant occulte, ALÉA placés (TESTING l. 1595)
- [ ] **0.976** Croissant occulte type « ALÉA », Bozja « escarmouche » (TESTING l. 1597)
- [ ] **0.990** Butin des contenus après déconnexion : état enregistré affiché (TESTING l. 1625)
- [ ] **0.994** Donjon : lire le wiki mécaniques, masqué en combat (TESTING l. 1633)
- [ ] **0.995** Raid : notes et schéma d'arène, copier et coller le code (TESTING l. 1635)
- [ ] **0.996** Palais des morts : wiki des étages 1-10 (TESTING l. 1637)

*Il faut : mode développeur*

- [ ] **0.973** Eurêka : « Reading fields: eureka », changer la magia (TESTING l. 1591)

## Étape 11 : JcJ et classé (4)

En tout dernier (règle de l'utilisateur).

- [ ] **0.610** Coupure compétitive : JcJ, Mahjong, donjon profond (TESTING l. 867)
- [ ] **0.612** Hall de l'Arène : actif en JcE, coupé contre joueur (TESTING l. 871)
- [ ] **0.613** Arène contre joueur : coupure, journal PvP instance (TESTING l. 873)
- [ ] **0.615** Lien de carte pendant une coupure ignoré (TESTING l. 877)
