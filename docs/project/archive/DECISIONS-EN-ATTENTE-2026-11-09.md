# Décisions en attente : sections d'origine (archive du 2026-11-09)

Sections retirées de G-PROPOSALS et de ROADMAP le 2026-11-09 (version 0.1013.0) : les choix tranchés sont dans [DECISIONS](../DECISIONS.md) ; ce qui reste ouvert est listé, produit par script, dans [ROADMAP](../ROADMAP.md). Texte inchangé ci-dessous.

<!-- G-PROPOSALS -->

## Choix provisoires (appliqués, à confirmer)

| Version | Écart | Choix de l'agent | Alternative |
|---|---|---|---|
| 0.343 | I-01 | Ctrl+Alt pendant le survol actualise le prix (les touches F et les lettres sont prises par le jeu) | Une autre combinaison, ou une touche réglable |
| 0.344 | I-02 | Seuil de 80 % du plafond pour toutes les monnaies | 70 % pour les mémoquartz (choix de Currency Watchdog) |
| 0.344 | I-02 | Sans Grande Compagnie, pas d'alerte de sceaux ; rien d'annoncé au chargement ; pas de rappel à la connexion | Rappel à la connexion en option |
| 0.344 | I-02 | Monnaies suivies : sceaux, marques, cristaux-trophées, sceaux alliés, Centurio, insignes de chasse, gemmes bicolores, assignats d'Azurée, mauves et orange, mémoquartz | Ajouter cristaux, mandats, gils, monnaies des tribus |
| 0.345 | I-03 | Sacs, cristaux, monnaies et arsenal relevés d'office toutes les 2 minutes, comme l'arsenal depuis 0.235 | Un réglage pour ne pas les relever |
| 0.345 | I-03 | « Effacer » est permis aussi pour le personnage connecté (ses rangements sont relevés de nouveau ensuite) | Interdire l'effacement du personnage connecté |
| 0.346 | I-06 | Prix PNJ affiché pour la qualité NQ seulement (la feuille du jeu ne donne pas le prix HQ) | Sonder le prix HQ en jeu et l'ajouter |
| 0.346 | I-07 | Copie désactivée par défaut (elle remplace le presse-papiers) ; offre la plus basse toutes qualités confondues | Activée par défaut ; ne comparer qu'à la même qualité (NQ ou HQ) |
| 0.441 | N-09 | Marques vivantes proches sur la carte (couche, activée par défaut) ; 0.446 : alerte chat et son des marques A/S (réglage, désactivé) | Points d’apparition (source communautaire à choisir) |
| 0.446 | X-13 | Annonce du conducteur copiée dans le presse-papiers avec <flag> | Envoyer le message au chat (/sh, /y…) sur clic : à ta décision (défaut recommandé : copier seulement, rien envoyé par Tensho) |
| 0.440 | N-01 | Contrats : cibles et compteurs dans Rappels, « Carte » sur la zone (pas de position dans les données, pas de drapeau) | Positions des cibles d’une source communautaire, drapeau et téléportation |
| 0.436 | A-14 | Enregistreur par hook sur UseAction (lecture seule, actif seulement pendant l’enregistrement) | Relecture en ultracode du hook (code qui agit au cœur du jeu) |
| 0.433 | A-05 | Listes dans « CraftLists.json » (32 × 64), ingrédients directs de toute la liste, Teamcraft ; chaîne une recette à la fois | Chaîne des sous-recettes et liste de récolte par zone pour toute une liste |
| 0.425 | A-08 | Import de la macro collée seulement (noms dans la langue du jeu) | Lire les liens Teamcraft et Craftingway sur clic (format en ligne à étudier) ; reconnaître aussi les noms anglais |
| 0.415 | R-13 | Fenêtre de suivi = objets des alarmes Tensho actives (pas de liste à part), 12 lignes, réglage désactivé par défaut, croix = décocher | Liste « suivis » indépendante des alarmes ; afficher si la fenêtre est ouverte en ce moment (durée de la fenêtre) |
| 0.411 | R-10 | Bouton « Équiper (métier) » sur chaque lieu de récolte et de pêche, séparé de « Y aller » ; ensemble de plus petit numéro du métier ; clic seulement | Option « Y aller » équipe aussi : FAIT 0.412 (désactivée par défaut) ; choisir l’ensemble par nom ; relecture en ultracode (appel au jeu) |
| 0.410 | R-09 | Raison solide / Sagesse du fermier et Vision claire / Maîtrise florale dans l’optimiseur ; case de l’outil PR cochée par défaut, toujours comptées dans le panneau en jeu ; Eurêka compté en moyenne (0,5 essai par Raison solide dès le niveau 90) ; Vision claire placée d’abord sur les essais avec Récolte abondante | Ajouter la régénération des PR pendant la récolte ; proposer Étalage des connaissances seulement quand Eurêka est lu en jeu |
| 0.409 | R-06 | Panneau « Collectionnable » à côté de la fenêtre du jeu (même réglage que le panneau de récolte) : seuils et assignats par palier des données du jeu, PR et intégrité ; assignats affichés seulement pour les boutiques RewardType 1 (déduit des contenus des boutiques) ; collectabilité en cours non lue, aucune action conseillée | Action conseillée (règles propres à Tensho selon les seuils) une fois l’index de la collectabilité trouvé en jeu ; type d’assignat à relier (colonne Currency) ; relecture du lecteur. Collectabilité des objets en sacs : FAIT 0.432 (A-15) |
| 0.403 | R-08 | « Alerte PR au seuil » en haut de la page PR, désactivée par défaut, seuil 0 = PR pleins ; une fois par remontée, réarmée sous le seuil, jamais à la première lecture ; chat et son selon les options des alarmes, pas en combat ; pas de Discord | Seuil par job ; catégorie Discord |
| 0.402 | R-05 | Page Récolte : compteurs des trois carnets, ✓ à côté des ressources déjà au carnet, case « Masquer ce qui est déjà au carnet », export CSV dans le presse-papiers et dans un fichier (dossier du plugin, Exports) ; lecture toutes les 5 s quand la page est ouverte | Filtre « seulement ce qui manque » dans le panneau de pêche en jeu ; carnets dans la vue « Vos personnages » |
| 0.398 | R-04 | Aide à la pêche : ligne « Appât en main » avec son stock et le nombre de poissons du lieu possibles avec lui ; cadre vert sur l'appât des poissons concernés ; lecture toutes les 0,5 s, pêcheur seulement | Trier les poissons pour mettre ceux de l'appât en main en tête ; alerte quand le stock d'appât est bas |
| 0.397 | R-03 | Bouton « Y aller » à côté de « Carte de la zone » sur chaque lieu de récolte et de pêche : drapeau sur le point, carte Tensho centrée, question de téléportation vers l'éthérite la plus proche (distance sur la carte, coût affiché), jamais automatique ; grisé sans position connue ; même mécanique que les liens du chat (0.373) | Préférence « moins cher » au lieu de « plus proche » ; éthérites d'une autre carte de la zone |
| 0.396 | R-02 | Section « Prochaines fenêtres » sur la fiche GatherBuddy d'un poisson : 5 prochaines fenêtres réelles (heure ET, météo et météo précédente réunies) dans la zone du premier lieu de pêche, cherchées jusqu'à 35 jours, alarme unique par fenêtre ; recalcul une fois par minute ; météo impossible à prévoir = « inconnu » | Fenêtres dans le panneau de pêche en jeu ; choix du lieu quand le poisson en a plusieurs zones |
| 0.392 | R-01 | Nouvelle page « Pêche en mer » (Production) : 12 prochains départs (8 avec un filtre de trajet), trajets depuis Limsa (Indigo) et Kugane (Rubis) avec escales et moments au survol, compte à rebours, embarquement ouvert 15 min, alarme Tensho avant un départ ; horaires depuis la boucle du jeu | Alarme répétée pour un trajet choisi ; poissons et appâts par escale : FAIT 0.418 (R-07, panneau pendant la traversée) |
| 0.391 | X-06 | Barre du tic de régénération limitée aux PR des récolteurs (PV et PM laissés : le combat est hors périmètre, DEC-009) ; tic repéré quand les PR montent sans être pleins (maximum de la lecture précédente) ; barre à l'écran désactivée par défaut, interrupteur en haut de la page PR, cachée en combat, pour les autres jobs et à PR pleins | PV et PM hors combat ; son au tic |
| 0.388 | Demande utilisateur | Bouton « Trier » dans la fenêtre d'inventaire du jeu, en bas à gauche sur la ligne des gils (position calculée sur le bord bas de la fenêtre, à son échelle : à ajuster en jeu) ; applique les règles enregistrées de « Mes sacs », comme « Appliquer » de la page Tri ; activé par défaut, interrupteur en haut de la page Tri ; message court à côté pendant 6 s | Bouton aussi sur les fenêtres des servants et des sacoches (règles de ces rangements) |
| 0.386 | X-10 | Section « Horloges » (repliée) en haut de la page Alarmes : fuseaux de Windows choisis dans une liste avec recherche, 6 au plus, retirables ; alarme Tensho unique à une date locale tapée « AAAA-MM-JJ HH:MM » (dans l'année), à côté de l'alarme quotidienne ET, avec le même nom ; groupe « Alarmes à date » | Heure du serveur du centre de données ; alarme répétée chaque semaine à date réelle |
| 0.385 | X-08 | « Job au hasard » dans la section des ensembles du jeu : un tirage par job (son ensemble de plus petit numéro), case « seulement les jobs pas au niveau le plus haut » cochée par défaut (niveau inconnu = gardé) ; « Équiper » est un second clic, par la fonction du jeu EquipGearset (adresse vérifiée, IsValidGearset, 2 s d'écart) ; jamais automatique | Tirage parmi les jobs dont une mission aléatoire du jour n'est pas faite ; commande /tensho |
| 0.384 | M-14 | Emplacement de la carte au trésor déchiffrée : icône (gemme) sur la carte Tensho avec la couche « coffres au trésor » existante, clic = drapeau ; bouton « Voir sur la carte » dans la ligne Cartes au trésor des Rappels ; lecture toutes les 5 s, fonctions du jeu appelées seulement si résolues ; TreasureSpot ligne = rang, sous-ligne = sous-clé (à confirmer) | Zone de recherche (rayon) autour du point ; cartes des autres membres de l'équipe |
| 0.383 | M-12 | Section « Ensembles d'équipement du jeu » de la page Équipement (repliée), un ensemble par ligne, groupés par job ; amélioration = pièce possédée (sacs, arsenal, sacoches, servants enregistrés ; pas le coffre à apparence ni l'armoire) de niveau d'objet plus haut, portable par le job à son niveau, une bague proposée une seule fois ; « Copier le code » TENSHO-GEAR1 avec matérias ; Tensho ne change jamais un ensemble | Grille complète job × emplacement ; import d'un code TENSHO-GEAR1 vers un plan pour un autre job |
| 0.382 | M-11 (en partie) | Lignes « Carnet d'objectifs » (défis faits sur le total, lu une fois le carnet ouvert) et « Enclave de Doman » (dons de la semaine sur le plafond) dans les Rappels ; escouade (0.333) et mémoquartz (0.344) déjà faits ; Mini et Jumbo Cactpot et objectifs hebdomadaires du Carnaval masqué laissés de côté : aucun champ trouvé (inconnu ≠ zéro) ; pas de verrou de raid (DEC-009) | Cactpot et Carnaval masqué par une sonde en jeu (ultracode) |
| 0.381 | X-01 | Tâche libre = nouveau type « Une fois » des tâches de 0.375 (reste faite jusqu'à ce qu'on la retire) ; commande « /tensho todo <tâche> » (aussi « tache ») qui l'ajoute en fin de liste et répond dans le chat ; pas de fenêtre à part | Commande pour cocher ou lister les tâches depuis le chat |
| 0.380 | M-09 (en partie) | Paliers des valeurs réelles (critique, coup direct, détermination, vitesse et temps de recharge global, ténacité, piété, défenses) : points jusqu'à la valeur suivante dans une colonne, points perdus dans l'aide ; calcul exact sur les formules déjà utilisées ; gardé comme information sur son propre personnage (DEC-009) | Statistiques en contenu synchronisé (GetItemLevelSync, InstanceContentDirector : à sonder en jeu, ultracode) |
| 0.379 | M-10 | Onglet Compagnie libre › Historique : un enregistrement par voyage (maison, type, emplacement, heure d'envoi), mis à jour tant que c'est le même voyage, gardé seulement quand son butin est lu ; 180 jours et 5 000 voyages au plus, un fichier VoyageHistory.json ; rendement réel par trajet (moyenne par voyage) à part des chiffres communautaires ; export CSV dans le presse-papiers (champs entre guillemets, protégés contre les formules) ; le classement des trajets ne l'utilise pas encore | Utiliser l'historique dans le classement des trajets (DEC-023 f) 2) ; export en fichier |
| 0.378 | M-08 | Favoris dessinés en étoile sur la carte Tensho (couche « Vos favoris », activée, taille et priorité réglables comme les autres) ; clic sur l'étoile = drapeau ; clic droit sur la carte › « Ajouter aux favoris » avec un nom ; note (300 caractères) et groupe (40) facultatifs ; partage par code TENSHO-MAP1 dans le presse-papiers (tous les favoris, 200 au plus), import sans doublons ; favoris communs à tous les personnages (comme avant) | Partage d'un seul groupe ; filtre par groupe dans la liste ; icône choisie par favori |
| 0.377 | M-07 | Comparaison en tête du panneau sous l'infobulle, activée par défaut (nouvelle case dans les parties du panneau) ; valeurs de base NQ ou HQ des deux côtés, sans matérias (celles de l'objet survolé ne sont pas connues) ; bague comparée aux deux bagues portées ; 8 statistiques au plus par ligne | Comparaison avec matérias de la pièce portée ; comparaison avec un set d'équipement choisi au lieu de la pièce portée |
| 0.376 | M-06 | Section « Usure et symbiose » dans la page Équipement (par pièce) ; alerte dans le chat et son, activée par défaut, une fois par passage du seuil, options des alarmes appliquées ; barre à l'écran désactivée par défaut ; seuils par défaut réparer sous 10 %, extraire à 100 % ; symbiose lue à 10 000 points = 100 % (wiki, à confirmer en jeu) ; pas de Discord pour cette alerte | Catégorie Discord ; alerte aussi pour l'équipement des autres sets (arsenal) |
| 0.375 | M-05 | Section « Vos tâches » dans Rappels (50 au plus) : chaque jour ou chaque semaine aux remises du jeu, toutes les n heures (1 à 168) après avoir été faite, compteur jusqu'à un objectif (1 à 999, remis à zéro à la main) ; tâches communes à tous les personnages ; panneau « à faire » à l'écran désactivé par défaut, caché en combat, placé en haut à gauche au premier affichage puis déplaçable, le fermer décoche le réglage | Tâches propres à chaque personnage ; rappel (alarme) d'une tâche ; montrer aussi les lignes fixes des Rappels dans le panneau ; compteur remis à zéro chaque jour ou semaine |
| 0.374 | M-04 | Vue « Vos personnages » sous les Rappels : ventures, prochaine carte au trésor, Livre d'or, contrats de chasse, missions aléatoires du jour, heure de la dernière lecture ; un fichier par personnage (Characters/<id local>.json) ; cases « fait » des Rappels propres au personnage connecté, les anciennes cases communes données une fois au premier personnage vu ; effacement en deux clics, jamais du personnage connecté ; vaisseaux laissés à la page Compagnie libre (ils sont à la compagnie) ; compteurs qui se remettent à zéro (mandats, livraisons, quêtes de tribu) pas repris : une valeur d'avant la remise tromperait | Gils et monnaies par personnage dans la même vue (le journal des gils et la banque les ont déjà) ; heure manuelle de carte au trésor par personnage |
| 0.373 | M-03 | Liste dans la page Cartes, tous les canaux, filtre par canal ; liens seulement (pas de coordonnées en texte) ; mémoire seulement, 100 liens, effacés à la déconnexion ; même point du même joueur en moins d'une minute = un seul lien ; éthérite la plus proche de la même carte parmi la liste de téléportation, question habituelle avec le coût ; réglage « Liens de carte du chat » activé par défaut | Lien ajouté dans le message du chat lui-même ; coordonnées écrites en texte ; éthérite d'une autre carte de la zone ; historique gardé d'une session à l'autre |
| 0.372 | M-02 | Lignes « Livre d'or des défis » et « Contrats de chasse » dans Rappels ; carte au trésor : heure du jeu si lue, sinon le bouton « ouverte » d'avant ; contrats = nombre en main seulement (pas encore les victimes par contrat) ; état « faite / à faire » sous la mission aléatoire choisie dans l'outil de mission | FAIT : victimes de chaque contrat (0.440, N-01) ; 9 cases du livre (0.439 vignettes posées, 0.443 tâches) |
| 0.371 | M-01 | Alarmes automatiques activées par défaut (choix de GAPS.md) ; ventures connues après l'ouverture de la liste des servants, vaisseaux après l'atelier ; alarme retirée une heure après avoir sonné ; usure des vaisseaux sans alarme (pas d'heure) ; Discord selon les catégories existantes (Rappels, Compagnie libre) | Alerte d'usure au retour d'un vaisseau (réparation conseillée) |
| — | X-04 | **Pas fait, décision à prendre** : partake.gg n'a pas d'API documentée trouvée (recherche du 2026-11-03), ses conditions d'utilisation sont inconnues, et PartyPlanner (qui la lit) est sous AGPL, donc lecture seule | Soit demander l'accord de partake.gg (ou trouver une API publique), soit abandonner X-04, soit un simple lien vers partake.gg |
| 0.370 | X-02 | Musique en cours (lue en mémoire) et rouleaux favoris en tête de l'onglet Orchestrion ; changer la musique côté client non fait (fonction SetBGM aux paramètres inconnus) | Changer la musique : sonder SetBGM en jeu avec vous (ultracode) |
| 0.369 | X-03 | « Porter ce titre » et « Titre de <job> » dans la fenêtre d'un titre obtenu ; « Titre au hasard » et « Aucun titre » en tête de l'onglet Titres ; titre par job désactivé par défaut, appliqué à un changement de job seulement (pas à la connexion) | Titre au hasard à chaque connexion ; titre par job aussi à la connexion |
| 0.368 | C-15 | Progression vue gardée en mémoire seulement (perdue au redémarrage) ; bouton « Demander » = une requête au serveur sur clic, 3 s d'écart ; succès suivis communs à tous les personnages (100 au plus), en tête de l'onglet Succès | Garder la progression vue sur disque par personnage ; alerte quand un succès suivi est terminé |
| 0.367 | C-13 | Monstres lus sur la page du sort du wiki (sur clic) dans la fenêtre du sort ; bouton « Drapeau » quand le lieu a des coordonnées et que la zone anglaise est reconnue ; pas de téléportation ni d'ouverture du contenu depuis la fiche (noms anglais des contenus non reliés) | Lier les contenus du wiki au catalogue (nom anglais) pour ouvrir leur butin ; téléportation à l'éthérite la plus proche |
| 0.366 | C-10 | Alerte désactivée par défaut, réglée dans Alarmes (ligne de discussion + son des alarmes, mêmes règles de silence) ; Discord dans sa propre catégorie « Mission prête », seulement si l'alerte est activée ; pas d'autre service de notification (ntfy…) | Notification Windows ; autre service de push choisi par l'utilisateur |
| 0.365 | C-04 | 8 onglets en plus dans Collections, dans l'ordre choisi ; lus à part (pas dans le cache des collections, relus toutes les 30 s pendant que la page est ouverte) ; familiers Beastmaster (XBMPet) non faits : colonnes sans nom dans les données ; pas de sources au clic droit pour ces types ; lunettes listées par variante | Familiers quand les colonnes seront nommées ; objet de déblocage et sources pour coiffures, accessoires et livres |
| 0.364 | C-12 | Nouvelle page Progression › Carnet de chasse ; zones du jeu seulement, sans coordonnées ni drapeau (le fichier monsters.json de Hunty, MIT, 339 Ko, n'est pas copié) ; les trois carnets de grande compagnie sont listés ; ordre des carnets en mémoire repris des faits lus dans Hunty, à confirmer en jeu | Copier monsters.json (MIT, avec avis) pour le drapeau et la téléportation ; ne montrer que la grande compagnie du personnage : FAIT 0.393 |
| 0.363 | C-14 | Nouvelle page Progression › Mandats ; filtre « Non terminés » par défaut ; un état inconnu reste dans « non terminés » ; file des mandats d'artisanat acceptés = objets demandés et copies possédées (pas de lien vers l'arbre de recettes) ; aucune automatisation | Bouton « Préparer » qui envoie les objets des mandats acceptés à la liste de préparation ; tri par gils |
| 0.362 | C-05 | Onglet « Tenues » dans Collections ; « possédée » = sacs en direct + rangements enregistrés du personnage connecté ; « prête à ranger » = toutes les pièces dans le coffre ; « où l'obtenir » par le clic droit de chaque pièce (pas de colonne de sources) ; 400 tenues affichées au plus | Tenues de tous les personnages ; pièces manquantes ajoutées d'un clic à la liste de souhaits |
| 0.361 | C-11 | Onglet « Carnet d'exploration » dans les listes de la carte (pas de page à part) ; « possible maintenant » = fenêtre d'heure ouverte et météo du wiki égale à la météo actuelle de la zone ; météo inconnue gardée et marquée « heure OK » ; prochaine fenêtre seulement dans l'infobulle (calcul lourd) | Colonne « prochaine fenêtre » pour toutes les lignes (calcul étalé) ; tri par prochaine fenêtre |
| 0.360 | C-09 | Onglet « Souhaits » dans Collections ; ajout par le menu du clic droit de Tensho seulement (pas dans le menu du jeu) ; 500 objets au plus ; état = collection pour montures, mascottes, rouleaux et cartes, sinon copies possédées ; liste commune à tous les personnages | Entrée dans le menu du clic droit du jeu ; alerte quand un souhait est en vente sous un prix choisi ; liste par personnage |
| 0.359 | C-08 | Filtre « Source » pour montures, mascottes et orchestrion seulement (les autres types n'ont pas d'objet de déblocage) ; sources = celles du clic droit, sans aucune requête wiki ou Garland (seulement ce qui est déjà en cache) ; calcul de toutes les entrées en une fois au premier choix du filtre ; « encore obtenable » non fait (les données du jeu ne le disent pas) | Calcul étalé sur plusieurs images si une saccade se voit en jeu |
| 0.358 | C-07 | Dans Rangements › Rapport ; « identique » = mêmes modèles (ModelMain, ModelSub) et même emplacement ; les teintures ne comptent pas mais sont indiquées ; recolorations (autre variante d'un modèle) non groupées | Groupe « recolorations » (même modèle, variante différente) |
| 0.357 | C-03 | Dans Rangements › Rapport (pas de nouvel onglet) ; armoire connue seulement si elle a été ouverte une fois (pas de lecture de CabinetItemUnlockBits, non sondée) ; pièces d'un set d'équipement et pièces d'une tenue exclues ; « pièces manquantes par contenu » non fait | Lire CabinetItemUnlockBits (sonde en jeu) pour connaître l'armoire sans l'ouvrir ; pièces d'armoire manquantes par contenu (LuminaSupplemental) |
| 0.356 | C-02 | Affichage seulement, sous Rappels (pas encore d'alerte de bonus de rôle : elle toucherait les alarmes et Discord) ; bonus lu dans l'outil de recherche de mission, indexé par la ligne de bonus (déduction, à confirmer en jeu) | Alerte locale (ligne de discussion + son) quand le rôle choisi a le bonus sur une mission aléatoire choisie, et envoi Discord optionnel : à faire avec vous en ultracode |
| 0.355 | C-01 | État lu seulement pendant que la page Butin des contenus est ouverte, gardé en mémoire (pas de fichier par personnage) ; un état inconnu reste dans les listes « non terminés » ; identifiant passé à IsInstanceContentCompleted = ligne InstanceContent (à confirmer en jeu) | Garder l'état par personnage sur disque pour le voir hors connexion ; liste « à faire » sur l'accueil |
| 0.354 | I-13 | « Que faire ? » dans l'onglet Monnaies ; valeur = prix minimum du marché × quantité ÷ coût (pas de taxe ni de profondeur d'offres) ; collections : montures et mascottes seulement | Autres collections (bardes, orchestrion, cartes) ; tenir compte de la taxe du marché |
| 0.353 | I-12 | Filtres repliés sous la recherche ; pas encore de filtre par job (le catalogue ne garde pas ClassJobCategory, l'ajouter changerait le format du cache) ; objets clés listés par nom, sans fiche | Filtre par job ; fiche simple pour les objets clés |
| 0.352 | I-11 | Une quête (la première) et un haut fait par article ; un haut fait non confirmé s'affiche « non lu » et jamais « à faire » ; les rangs de tribu (BeastReputationRank) ne sont pas encore lus | Toutes les quêtes demandées ; rangs de tribu ; filtre « seulement ce que je peux acheter » |
| 0.351 | I-04 | Onglet Rangements › Gils (et non un widget ni la barre du serveur) ; catégorie déduite de la fenêtre du jeu vue dans les 15 dernières secondes ; gils des servants connus seulement après l'ouverture de la cloche | Entrée dans la barre du serveur (gains du jour) ; catégories plus fines (quêtes, missions, ventes au marché reçues) |
| 0.350 | I-09 | Coffre enregistré par personnage qui l'ouvre (le coffre appartient à la compagnie) ; il faut que les cinq onglets aient été affichés (le jeu les envoie un par un) ; ses objets comptent dans les stocks | Un seul coffre par compagnie, partagé entre ses membres (demanderait l'identifiant de la compagnie) |
| 0.349 | X-07 | Commandes « /tensho wiki » et « /tensho lodestone » (et non /wiki, /lodestone : ces noms peuvent déjà être pris par un autre plugin, ce qui empêcherait Tensho de démarrer sa commande) ; wiki en anglais | Wiki français s'il en existe un tenu à jour ; autres sites (Garland, Teamcraft) dans le menu |
| 0.348 | I-10 | Piles comptées par rangement (sacs en direct, sacoches et servants enregistrés), pas entre rangements ; arsenal enregistré, sets lus à la demande ; cristaux d'âme exclus | Proposer aussi les regroupements entre rangements (ex. une pile au servant et une dans les sacs) |
| 0.347 | I-08 | Deux étiquettes (« important », « à vendre au PNJ ») et une note ; rappel dans le chat à l'ouverture d'une boutique de PNJ ; son des alarmes pour les objets importants, désactivé | Étiquettes libres et colorées ; avertir aussi avant de jeter un objet important (demanderait de surveiller la fenêtre de confirmation du jeu) |


<!-- G-PROPOSALS -->

## Décision à prendre : ventes des servants (0.508, I-05)

Choix appliqué par défaut : la comparaison avec Universalis se lance seulement sur le bouton du panneau. Autre choix possible : demander une fois à l'ouverture de la liste de ventes, sur réglage. Recommandé : garder le clic, jusqu'à votre avis après un essai.


<!-- G-PROPOSALS -->

## À décider avec l'utilisateur (relevé du 2026-11-04)

Règle : une règle Dalamud en jeu demande un résumé et son accord (mémoire, consignes de l'agent).

| Point | Règle | Conseil de l'agent | État |
|---|---|---|---|
| Titre selon le job | Pas de requête automatique au serveur sans action directe | Garder pour l'usage personnel, à revoir avant la soumission | **Décidé le 2026-11-04 : gardé tel quel** (DEC-035), présenté dans la PR ; à revoir après la publication |
| Conseil en match de Triple Triade | Aucun avantage en contenu compétitif | Décidé (DEC-033) : contre un PNJ seulement | **Fait en 0.510** |
| Panneau du Dédale (DEC-032) | Combat et contenu compétitif | Informations seulement, demander à l'équipe avant la 1.0.0 | Page faite, panneau en attente, dresseur mis de côté |

La liste complète de ce qui reste pour la 1.0.0 : [PUBLISHING](../PUBLISHING.md) « Ce qui reste avant la soumission ». Compilation chez Dalamud : corrigée en 0.512 ([DEC-034](../DECISIONS.md)).


<!-- G-PROPOSALS -->

## À revoir avec l'utilisateur (notées le 2026-11-08 au soir, demande « continue en notant les propositions »)

**Revus le 2026-11-09 (DEC-045)** : 1 laissé ; 2 position exacte partout ; 3 plan à proposer ; 4 déjà confirmé (DEC-026), alternatives revues ; 5 sur clic ; 6 aide à la pêche ; 7 français ; 8 wiki + notes + schéma, bestiaire avec notes du wiki.

| # | Sujet | Proposition (recommandée) | Autre choix |
|---|---|---|---|
| 1 | Coupure JcJ des travaux déjà lancés (DEC-038, audit 0.949) | Annuler dès le début de la coupure : connexion GitHub en attente, échange de découvertes, lectures Garland et wiki, prix d'une préparation ; couper l'alerte Discord « mission trouvée » en JcJ | Laisser comme aujourd'hui (non retenu le 2026-11-08) |
| 2 | Position exacte dans les découvertes partagées (audit 0.949) | N'envoyer que la case de 50 yalms | Garder la position exacte (non retenu le 2026-11-08) |
| 3 | Réorganisation des pages de réglages (X-76 étape 3) | Proposition concrète après la passe des groupes de réglages | — |
| 4 | Choix provisoires depuis la 0.345 (tableau en tête de ce fichier) | Les passer en un seul lot, cases à cocher, à la fin du nettoyage (étape 9) | — |
| 5 | Ventes des servants (I-05) | Garder la comparaison Universalis sur clic | Demander une fois à l'ouverture, sur réglage |
| 6 | Poissons de collection « pas pris en compte » (X-97) | Question : où l'avez-vous vu ? Les prises collectionnables sont bien comptées dans les sacs et les découvertes ; proposition : afficher dans l'aide à la pêche la collectionnabilité visée et les assignats d'un poisson collectionnable | — |
| 7 | Langue des fichiers de l'agent (X-91, 2026-11-09) | consignes de l'agent, notes de l'agent et le skill de version mis à jour contre le code, mais gardés en français (exception de l'étape 4 : fichiers hors Git, consignes de l'utilisateur en français) | Les traduire en anglais avec les documents vivants, juste avant la 1.0.0 |
| 8 | Outils de contenus (X-90), docs/project/CONTENT-TOOLS.md | Tableaux stratégiques en notes écrites par vous, masquées en combat, sans dessin sur l'écran du jeu ; bestiaire des donjons profonds hors du donjon, sans conseil ; suivi partagé des NM d'Eurêka laissé aux plugins existants | Conseils de la communauté hors du donjon ; autre format de tableaux |
| 9 | Alerte « souhait en vente sous un prix » (X-101) : Tensho interrogerait Universalis en arrière-plan, sans clic, pour vos souhaits avec un prix (règle Dalamud : appel réseau automatique, à déclarer) | Réglage coupé par défaut ; au plus une fois toutes les 30 min, seulement les souhaits avec un prix, 20 objets par passage, ligne de discussion à la baisse sous le prix ; Universalis déjà dans la liste des hôtes | Seulement sur un bouton « Vérifier maintenant » (aucun appel automatique) |
| 10 | Regroupement des outils et des réglages (X-100) | Voir la section « Proposition X-100 » ci-dessous | Garder la disposition actuelle |


<!-- ROADMAP -->

## To decide / to review

Points still open, as noted at 0.512: each waits for an in-game record or is settled in a step of the plan. No value is guessed meanwhile.
ONBOARD-001 and TEMPLATES-001 are agent goal sheets, outside the repository.

| Point | Source | Finding | Record or step expected |
|---|---|---|---|
| Getting started, help, videos, issues | ONBOARD-001 | The 8 slices coded (0.502 to 0.506), DEC-031 | In-game validation; a release for the video list |
| Community templates | TEMPLATES-001, [DEC-029](../DECISIONS.md) | Finished on the code side in 0.476 | In-game validation; gallery visible after a release |
| « Cost ≤ capacity » rule below rank 50 | [0.315-vessel-builds](../../evidence/0.315-vessel-builds.md), « Cost ≤ capacity » | Rule of the community tools. Consistent with 11 published airship builds; not checked in game below rank 50. | In-game record on a vessel below rank 50. |
| Marker type of leves | CHANGELOG 0.298 | The log shows types 0, 3 (quests), 4 and 6; no record says which one is a leve. | In-game record: leve accepted, map open, developer mode. |
| Flag icon 60561 | `MainWindow.MapView.cs` (`FlagIcon`); CHANGELOG 0.102 (« assumed »); map audit 0.328 | The flag icon id is not proven in docs/evidence (placing the flag is validated in game). | In-game record: flag placed, `AgentMap.FlagMapMarkers[0].MapMarker.IconId` read on the framework (probe), log in docs/evidence. |
| Quest icons 71141 / 71151 | BACKLOG (removed in 0.1088, git history); [0.211-event-icons](../../evidence/0.211-event-icons.md) | The meaning of the variants is not in the game data: the settings show « variant n ». | In-game record, otherwise keep « variant n ». |
| Estimated Bountiful bonus (0.224) | BACKLOG (removed in 0.1088, git history), « Review of open points »; [0.224-bountiful-base](../../evidence/0.224-bountiful-base.md) | Unofficial community rule, shown « estimated » and editable. | To compare in game with the real bonus. |
| Materia: piece on several slots | [0.312-materia](../../evidence/0.312-materia.md), « Not checked in game » | A piece covering several slots takes the body's percentage. | In-game record (cap shown against the game). |
| Useful missing settings | R-06 step 5; audit of point d) (2026-11-02) | DONE 0.327 (re-read 2026-11-03): number of search results, market scope remembered, Triple Triad helpers, automatic computation of the crafting log, hover hidden during a duty, chat line silenced during a duty, opening page, parts of the hover panel. | In-game tests. |

### Decisions left for the end (agent's provisional choice, to confirm)

Batch of 2026-11-03 ([DEC-026](../DECISIONS.md)): « Monsters on hover » removed (0.447); inventory 250 ms: DEC-004 kept, sort editor kept on
screen (0.447); line endings: nothing to normalize; GitHub: protections, Dependabot and CI build turned on (0.447); memoquartz: 80 %
confirmed; choices of g) (G-PROPOSALS) confirmed as a block. The other rows remain to confirm during the in-game tests.

| Point | Provisional choice | Where to change it |
|---|---|---|
| R-12 Discord: defaults | Sending off; every category ticked; message « ⏰ [label] : dans [minutes] min ([time]) »; name « Tensho »; character name not sent. Since 0.778 (X-23), each new alarm has its Discord box off. | Settings › Alarms › Discord |
| R-12 Discord: rate | Two seconds at least between two messages, a queue of 20 messages (the oldest dropped past that), three tries at most on « 429 ». | `Alarms/DiscordNotifier.cs` |
| Fashion Report: source turned off (0.321) | No more download; a recent copy kept on disk stays shown. | `Fashion/FashionDataService.cs` |
| « Reset everything to default » (0.321) | The folded groups of the navigation are kept (interface state); the map settings, including the icon sizes and priorities per map, are reset. | `Configuration.cs` (ConfigurationReset) |
| New order of Release.ps1 (0.323) | Checks, tag and GitHub release with the zip, then main (repo.json) last; resumes if the tag exists without a release. | `scripts/Release.ps1` |
| GitHub Actions and Dependabot (0.323) | **Turned on in 0.447** (DEC-026); job building like Dalamud added in 0.512. | `.github/` (the publish/github drafts were removed in 0.1052) |
| Protections of the GitHub repository (0.323) | **Turned on in 0.447** (DEC-026): force push and deletion forbidden on main and Devloppement, Stable-* tags protected. | GitHub settings |
| Line endings eol=lf (0.323) | **Nothing to normalize** (DEC-026). | `.gitattributes` |
| Code of conduct, CODEOWNERS, pre-push hook (0.323) | Not created (code of conduct, CODEOWNERS); hook ready, inactive (`git config core.hooksPath .githooks` to turn it on). | `.githooks/pre-push` |
| R-13 Credits: partial settings | Settled provisionally in 0.324 (recommended choice applied): the Garland and wiki switches cut their whole source, Universalis has its own switch (on by default) with « price on hover » below it; copies kept stay shown. To confirm or change at the end. | `Configuration.cs`, services of `Plugin.cs` |
| R-12 Discord: alarms written in the game's list | They ring in the game and are still sent to Discord. | `Alarms/AlarmService.Dalamud.cs` (Ring) |
| Tooltip test (0.325) | Ratchet (a list of exceptions that only shrinks) and regular expressions, without dependency; no zero tolerance and no Roslyn. | `tests/TenshoAssistant.Tests/TooltipRatchet325Tests.cs`, `tooltip-baseline.txt` |
| Step g): proposals and provisional choices | Kept along g) in [G-PROPOSALS](../G-PROPOSALS.md): provisional choices applied, ideas found while coding, gaps left for an in-game session. | [G-PROPOSALS](../G-PROPOSALS.md) |
| Currency alerts (I-02, 0.344) | Agent's provisional choices: threshold 80 % of the cap for all; without a Grand Company, no seal alert; a currency already above at load is listed without a message; no reminder at login. Crystals, leves and gil not followed. To confirm or change. | `Inventory/Currencies.cs`, `MainWindow.Currencies.cs` |
| Refresh key of the hovered price (I-01, 0.343) | Agent's provisional choice: Ctrl+Alt during the hover (the F keys and letters are taken by the game), adjustable (can be turned off). | `MainWindow.MarketHover.cs`, `Items/ItemHoverReader.Dalamud.cs` |
| Map, items, crafting and gathering, other tools: what to add (step e) | Proposals of the audit of 2026-11-02, partly done since; each proposal has its « State » line. | [MAP-PROPOSALS](../MAP-PROPOSALS.md), [ITEMS-PROPOSALS](../ITEMS-PROPOSALS.md), [CRAFT-GATHER-PROPOSALS](../CRAFT-GATHER-PROPOSALS.md), [TOOLS-PROPOSALS](../TOOLS-PROPOSALS.md) |
| Inventory cleared 250 ms at each change (audit 0.329, defect 7) | Not fixed: the fix contradicts DEC-004 (« atomic invalidation, coalesced collection 250 ms ») and its tests. Recommended: keep DEC-004 and let the sort editor keep the last complete list (never for the preview or the sort). | `MainWindow.SortPreview.cs`; DEC-004 |
| Frame of the game windows (0.332) | **Removed in 0.510** (DEC-033, user's decision). | — |
| Squadron in R-11 step 4 (probe 0.332) | A squadron member's gear is only an appearance (glamour, dyes), without stats: nothing to plan. Applied in 0.340 (barding done, squadron not planned); to confirm at the end. | [0.332-squadron-probe](../../evidence/0.332-squadron-probe.md) |
| « Monsters on hover » without effect (noted in 0.327) | **Removed in 0.447** (DEC-026). | — |
| Immediate deletions (0.325) | Two-click confirmation for « Delete all » of an alarm group only; elsewhere (alarm, deck, storage template, crafting sequence), a tooltip says the deletion is immediate. | `MainWindow.Alarms.cs` |

### Decided on 2026-11-02 ([DEC-023](../DECISIONS.md))

| Point | Decision |
|---|---|
| Submarine loot threshold table | Keep the SubmarineTracker table; on the 19 sectors where it is below every record, the tier carries the note « not confirmed by the records ». |
| Value of submarine routes | Add the market price (Universalis) beside the NPC price, and the surveillance / retrieval tiers if the source splits the records by tier. |
| Airship unlock tree | LuminaSupplemental data (GPL-3.0) embedded in the DLL, with the licence notice in the file. |
| Airship route planner | Distance and duration only; the loot is shown « unknown », never estimated. |
| Materia: overmelding rate | Read the success rate from the game data and show it per advanced slot. |
| Character page removed (0.341, user request) | The Gear page shows the same real values. The panel beside the game's Character window is kept (decided by the user on 2026-11-02). |
| Other gear (R-11 step 4) | Chocobo barding: DONE 0.340 ([evidence](../../evidence/0.340-chocobo-barding.md)). Squadron: appearance only, left out. |
| Reminders: squadron, Ehcatl Gliders | Probe a readable time in the game's memory (FFXIVClientStructs); otherwise leave it aside. |
| Seasonal events (R-09) | DONE 0.342: game data only (running events, quests, NPCs and shops of the layers); names from the guide NPC (French) or LuminaSupplemental; no future dates. |
| Game frames and textures (ULD) | Frame removed in 0.510 (DEC-033). |
| D17 publication | Replaced by DEC-039 (2026-11-05): no submission, the custom repository only. |
| German and Japanese translations | Kept in the package (partial, English fallback). |
| Tone of the French texts | « vous » everywhere (done 0.317, automatic test). |
| Ocean fishing | Calendar (« Ocean fishing » page) and help during the voyage; game data (IKDRoute, IKDRouteTable, IKDSpot). |
| Additions to the 17 tools | Taken into plan c): adaptive rotation (A-04), armoury and automatic sort (I-14, later). |
| EUPL-1.2 licence | Compatible with GPL v3 through its clause (notice required): Tourist, Glamaholic, Quest Map, Better Party Finder (DEC-025). |
| `FilteredTable` template | Removed from the plan (agent's choice, rule « no premature abstraction »): to create at the first real repeated need. |


<!-- ROADMAP -->

## À décider / à revoir

Points encore ouverts, notés au 0.512 : chacun attend un relevé en jeu ou se règle dans une étape du plan. Aucune valeur n'est devinée en
attendant. ONBOARD-001 et TEMPLATES-001 sont des fiches d'objectifs de l'agent, hors du dépôt.

| Point | Source | Constat | Relevé ou étape attendus |
|---|---|---|---|
| Prise en main, aide, vidéos, tickets | ONBOARD-001 | Les 8 lots codés (0.502 à 0.506), DEC-031 | Validation en jeu ; une release pour la liste de vidéos |
| Modèles de la communauté | TEMPLATES-001, [DEC-029](../DECISIONS.md) | Terminés côté code en 0.476 | Validation en jeu ; galerie visible après une release |
| Règle « coût ≤ capacité » sous le rang 50 | [0.315-vessel-builds](../../evidence/0.315-vessel-builds.md), « Coût ≤ capacité » | Règle des outils de la communauté. Cohérente avec 11 montages de dirigeables publiés ; non vérifiée en jeu sous le rang 50. | Relevé en jeu sur un vaisseau de rang inférieur à 50. |
| Type de marqueur des mandats | CHANGELOG 0.298 | Le journal montre les types 0, 3 (quêtes), 4 et 6 ; aucun relevé ne dit lequel désigne un mandat. | Relevé en jeu : mandat accepté, carte ouverte, mode développeur. |
| Icône du drapeau 60561 | `MainWindow.MapView.cs` (`FlagIcon`) ; CHANGELOG 0.102 (« supposée ») ; audit de la carte 0.328 | L'identifiant de l'icône du drapeau n'est pas prouvé dans docs/evidence (la pose du drapeau, elle, est validée en jeu). | Relevé en jeu : drapeau posé, `AgentMap.FlagMapMarkers[0].MapMarker.IconId` lu sur le framework (sonde), journal dans docs/evidence. |
| Icônes de quête 71141 / 71151 | BACKLOG (retiré en 0.1088, historique git) ; [0.211-event-icons](../../evidence/0.211-event-icons.md) | Le sens des variantes n'est pas dans les données du jeu : les réglages affichent « variante n ». | Relevé en jeu, sinon garder « variante n ». |
| Bonus d'Abondance estimé (0.224) | BACKLOG (retiré en 0.1088, historique git), « Revue des points ouverts » ; [0.224-bountiful-base](../../evidence/0.224-bountiful-base.md) | Règle de la communauté non officielle, affichée « estimé » et modifiable. | À comparer en jeu avec le bonus réel. |
| Matérias : pièce sur plusieurs emplacements | [0.312-materia](../../evidence/0.312-materia.md), « Non vérifié en jeu » | Une pièce qui couvre plusieurs emplacements prend le pourcentage du torse. | Relevé en jeu (plafond affiché contre le jeu). |
| Réglages utiles manquants | R-06 étape 5 ; audit du point d) (2026-11-02) | FAIT 0.327 (relu le 2026-11-03) : nombre de résultats de recherche, portée du marché mémorisée, aides Triple Triad, calcul automatique du carnet, survol masqué en mission, silence en mission de la ligne de discussion, page d'ouverture, parties du panneau au survol. | Tests en jeu. |

### Décisions reportées à la fin (choix provisoire de l'agent, à confirmer)

Lot du 2026-11-03 ([DEC-026](../DECISIONS.md)) : « Monstres au survol » retiré (0.447) ; inventaire 250 ms : DEC-004 gardée, éditeur du tri
gardé à l'écran (0.447) ; fins de ligne : rien à normaliser ; GitHub : protections, Dependabot et compilation en CI activés (0.447) ;
mémoquartz : 80 % confirmé ; choix de g) (G-PROPOSALS) confirmés en bloc. Les autres lignes restent à confirmer pendant les tests en jeu.

| Point | Choix provisoire | Où le changer |
|---|---|---|
| R-12 Discord : valeurs par défaut | Envoi désactivé ; toutes les catégories cochées ; message « ⏰ [label] : dans [minutes] min ([time]) » ; nom « Tensho » ; nom du personnage non envoyé. Depuis la 0.778 (X-23), chaque nouvelle alarme a sa case Discord décochée. | Réglages › Alarmes › Discord |
| R-12 Discord : débit | Deux secondes au moins entre deux messages, file de 20 messages (les plus anciens écartés au-delà), trois essais au plus sur « 429 ». | `Alarms/DiscordNotifier.cs` |
| Revue de mode : source coupée (0.321) | Plus aucun téléchargement ; une copie récente gardée sur disque reste affichée. | `Fashion/FashionDataService.cs` |
| « Tout remettre par défaut » (0.321) | Les groupes repliés de la navigation sont gardés (état de l'interface) ; les réglages de la carte, dont les tailles et priorités d'icônes par carte, sont remis à zéro. | `Configuration.cs` (ConfigurationReset) |
| Nouvel ordre de Release.ps1 (0.323) | Contrôles, tag et release GitHub avec le zip, puis main (repo.json) en dernier ; reprise si le tag existe sans release. | `scripts/Release.ps1` |
| GitHub Actions et Dependabot (0.323) | **Activés en 0.447** (DEC-026) ; job de compilation à la manière de Dalamud ajouté en 0.512. | `.github/` (les brouillons de publish/github sont retirés en 0.1052) |
| Protections du dépôt GitHub (0.323) | **Activées en 0.447** (DEC-026) : push forcé et suppression interdits sur main et Devloppement, tags Stable-* protégés. | réglages GitHub |
| Fins de ligne eol=lf (0.323) | **Rien à normaliser** (DEC-026). | `.gitattributes` |
| Code de conduite, CODEOWNERS, hook pre-push (0.323) | Non créés (code de conduite, CODEOWNERS) ; hook prêt, inactif (`git config core.hooksPath .githooks` pour l'activer). | `.githooks/pre-push` |
| R-13 Crédits : réglages partiels | Résolu à titre provisoire en 0.324 (choix recommandé appliqué) : les interrupteurs Garland et wiki coupent toute leur source, Universalis a son propre interrupteur (activé par défaut) avec « prix au survol » en dessous ; les copies gardées restent affichées. À confirmer ou à changer à la fin. | `Configuration.cs`, services de `Plugin.cs` |
| R-12 Discord : alarmes écrites dans la liste du jeu | Elles sonnent dans le jeu et partent quand même sur Discord. | `Alarms/AlarmService.Dalamud.cs` (Ring) |
| Test des infobulles (0.325) | Cliquet (liste d'exceptions qui ne fait que diminuer) et expressions régulières, sans dépendance ; pas de tolérance zéro ni de Roslyn. | `tests/TenshoAssistant.Tests/TooltipRatchet325Tests.cs`, `tooltip-baseline.txt` |
| Étape g) : propositions et choix provisoires | Tenus au fil de g) dans [G-PROPOSALS](../G-PROPOSALS.md) : choix provisoires appliqués, idées repérées en codant, écarts laissés pour une séance en jeu. | [G-PROPOSALS](../G-PROPOSALS.md) |
| Alertes de monnaies (I-02, 0.344) | Choix provisoires de l'agent : seuil de 80 % du plafond pour toutes ; sans Grande Compagnie, pas d'alerte de sceaux ; une monnaie déjà au-dessus au chargement est listée sans message ; pas de rappel à la connexion. Cristaux, mandats et gils non suivis. À confirmer ou changer. | `Inventory/Currencies.cs`, `MainWindow.Currencies.cs` |
| Touche d'actualisation du prix survolé (I-01, 0.343) | Choix provisoire de l'agent : Ctrl+Alt pendant le survol (les touches F et les lettres sont prises par le jeu), réglable (désactivable). | `MainWindow.MarketHover.cs`, `Items/ItemHoverReader.Dalamud.cs` |
| Carte, objets, artisanat et récolte, autres outils : quoi ajouter (étape e) | Propositions de l'audit du 2026-11-02, en partie faites depuis ; chaque proposition a sa ligne « État ». | [MAP-PROPOSALS](../MAP-PROPOSALS.md), [ITEMS-PROPOSALS](../ITEMS-PROPOSALS.md), [CRAFT-GATHER-PROPOSALS](../CRAFT-GATHER-PROPOSALS.md), [TOOLS-PROPOSALS](../TOOLS-PROPOSALS.md) |
| Inventaire vidé 250 ms à chaque changement (audit 0.329, défaut 7) | Non corrigé : la correction contredit DEC-004 (« invalidation atomique, collecte coalescée 250 ms ») et ses tests. Recommandé : garder DEC-004 et faire garder à l'éditeur de tri la dernière liste complète (jamais pour l'aperçu ni le tri). | `MainWindow.SortPreview.cs` ; DEC-004 |
| Cadre des fenêtres du jeu (0.332) | **Retiré en 0.510** (DEC-033, décision de l'utilisateur). | — |
| Escouade dans R-11 étape 4 (sonde 0.332) | L'équipement d'un membre d'escouade n'est qu'une apparence (mirage, teintures), sans caractéristiques : rien à planifier. Appliqué en 0.340 (barde faite, escouade non planifiée) ; à confirmer à la fin. | [0.332-squadron-probe](../../evidence/0.332-squadron-probe.md) |
| « Monstres au survol » sans effet (relevé en 0.327) | **Retiré en 0.447** (DEC-026). | — |
| Suppressions immédiates (0.325) | Confirmation en deux clics pour « Tout supprimer » d'un groupe d'alarmes seulement ; ailleurs (alarme, deck, modèle de rangement, séquence d'artisanat), une infobulle dit que la suppression est immédiate. | `MainWindow.Alarms.cs` |

### Décidé le 2026-11-02 ([DEC-023](../DECISIONS.md))

| Point | Décision |
|---|---|
| Table des seuils de butin des sous-marins | Garder la table SubmarineTracker ; sur les 19 secteurs où elle est sous toutes les observations, le palier porte la mention « non confirmé par les relevés ». |
| Valeur des trajets de sous-marin | Ajouter le prix du marché (Universalis) à côté du prix PNJ, et les paliers de surveillance / récupération si la source sépare les relevés par palier. |
| Arbre de déblocage des dirigeables | Données de LuminaSupplemental (GPL-3.0) embarquées dans la DLL, avec l'avis de licence dans le fichier. |
| Planificateur de trajets de dirigeable | Distance et durée seulement ; le butin est affiché « inconnu », jamais estimé. |
| Matérias : taux de surmatérialisation | Lire le taux de réussite dans les données du jeu et l'afficher par emplacement avancé. |
| Page Personnage retirée (0.341, demande de l'utilisateur) | La page Équipement montre les mêmes valeurs réelles. Le panneau à côté de la fenêtre Personnage du jeu est gardé (décidé par l'utilisateur le 2026-11-02). |
| Autres équipements (R-11 étape 4) | Barde de chocobo : FAITE 0.340 ([preuve](../../evidence/0.340-chocobo-barding.md)). Escouade : apparence seulement, retirée. |
| Rappels : escouade, Planeurs d'Ehcatl | Sonder une heure lisible dans la mémoire du jeu (FFXIVClientStructs) ; sinon laisser de côté. |
| Événements saisonniers (R-09) | FAIT 0.342 : données du jeu seulement (événements en cours, quêtes, PNJ et boutiques des calques) ; noms par le PNJ guide (français) ou LuminaSupplemental ; pas de dates à venir. |
| Cadres et textures du jeu (ULD) | Cadre retiré en 0.510 (DEC-033). |
| Publication D17 | Remplacée par DEC-039 (2026-11-05) : pas de soumission, le dépôt personnalisé seulement. |
| Traductions allemande et japonaise | Gardées dans le paquet (partielles, repli anglais). |
| Ton des textes français | Vouvoiement partout (fait 0.317, test automatique). |
| Pêche en mer | Calendrier (page « Pêche en mer ») et aide pendant la traversée ; données du jeu (IKDRoute, IKDRouteTable, IKDSpot). |
| Compléments des 17 outils | Repris dans le plan c) : rotation adaptative (A-04), tri de l'arsenal et automatique (I-14, plus tard). |
| Licence EUPL-1.2 | Compatible avec la GPL v3 par sa clause (avis requis) : Tourist, Glamaholic, Quest Map, Better Party Finder (DEC-025). |
| Modèle `FilteredTable` | Retiré du plan (choix de l'agent, règle « pas d'abstraction prématurée ») : à créer au premier besoin réel répété. |

