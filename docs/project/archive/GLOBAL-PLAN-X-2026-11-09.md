# Plan global : détail des demandes X (archive du 2026-11-09)

Texte complet de la colonne « État » du tableau « Demandes du 2026-11-07 » de [GLOBAL-PLAN](../GLOBAL-PLAN.md), tel qu'il était le 2026-11-09 à la version 0.1012.0. Le plan garde un résumé ; le détail reste ici, sans changement.

## X-01

En cours : relevé « Slow frame » en 0.745 ; aide à la récolte (97 ms) calculée hors de l'image en 0.747, avec la mesure de la fenêtre principale et des autres fenêtres ; les images de 110 à 160 ms venaient du passage des pages (mode développeur), espacé de deux heures en 0.748 ; pause de 5 s du moteur .NET au rechargement (mode développeur) ; panneau de l'atelier à 1,7–7,6 s depuis les zones (0.757–0.758, ma régression) : regroupement calculé une fois par seconde en 0.760 ; carte : « map overlay layout » 100 à 220 ms une fois à l'ouverture, à mesurer plus finement plus tard ; le 2026-11-07 la carte revient à 110–160 ms plusieurs fois par heure, pas seulement à l'ouverture : 0.791 la mesure par morceaux ; 18:41 et 18:46 : carte 195 ms (canevas 166–168 ms) et panneau d'atelier 93–97 ms, mais la ligne ne gardait que 4 morceaux, les enveloppes : 0.801 en garde 10 ; 18:50 à 20:01 : canevas 102–169 ms, dont « canvas icons » 34–71 ms et 40–60 ms hors des points de mesure : 0.805 ajoute boutiques, liste des icônes, dessin, objets proches, monstres, menus, éthérite la plus proche, fin ; 20:08:14 (0.805) : canevas 93 ms, boutiques 16, info-bulles 11, menus 11… mais la ligne coupait encore à 10 morceaux : 0.806 garde tous ceux d'1 ms et plus

## X-02

Durées par défaut plus longues et « Jamais » renommé « Seulement si absent, ou sur Actualiser » en 0.750. Accord donné. Relu : quêtes et collections ont déjà une copie par personnage sur disque ; les carnets, recettes et équipement se relisent légèrement, page ouverte. À faire : durées par défaut plus longues, « Jamais » renommé « seulement si absent ou sur Actualiser », et ce que X-01 trouvera de lourd

## X-03

Aucun clic n'arrivait (journal de 0.743). Dalamud ne trouve plus le lien après un rechargement du plugin : réenregistré au chargement en 0.759 ; toujours aucun clic à 13:37, plugins tous désactivés sauf Tensho : couleur placée autour du lien et non dedans en 0.771 ; toujours rien à 13:49 : 0.773 écrit au journal si Dalamud a le lien enregistré ; console verbose (14:09) : lien enregistré mais le jeu ne le transmet pas, il ouvre une bulle d'objet ; 0.774 : vrai lien d'objet du jeu, clic droit → menu Tensho ; validé en jeu le 2026-11-07 14:29 sur capture : clic droit sur l'objet de l'alarme, menu « Tensho — objet » complet ; 14:47 : clic droit sans effet sur l'indication grise, hors du lien : 0.780 met l'indication dans le lien ; validé en jeu le 2026-11-07 (« clic droit ok ») ; 2026-11-07 21:01, sur capture (alarme Abricot) : le clic droit ne fonctionne toujours pas : rouvert, à reprendre après X-45 ; 0.812 : le lien de 0.780 (texte changé) n'est plus pris pour un objet, aucun menu ouvert à 21:01 ; retour au lien du jeu de 0.774, l'indication à côté, `docs/evidence/0.812-alarm-link.md` ; 2026-11-07 22:34 avec 0.812 : alarme écrite avec le lien d'objet du jeu, clic droit sans aucun menu ouvert ; essai demandé : clic droit sur un objet lié dans un message normal du jeu ; 2026-11-07 23:06, après 0.812 : toujours aucun menu au clic droit sur le lien de l'objet (morille, petite icône orange) : à reprendre ; 0.836 : dalamud.log de 23:06 : la ligne s'imprime, puis aucun événement de menu, pas même avant les filtres de Tensho : le jeu n'ouvre rien. Commande de développeur « /tensho linktest » : quatre formes du lien (lien d'objet, lien Tensho ; type par défaut ou Écho), chaque clic noté au journal, pour choisir la forme qui marche ; 0.837 : essai du 23:35 : A et B ouvrent le menu du chat (objet survolé 12898) mais Tensho le prenait pour un menu de personnage (nom présent) ; C et D arrivent bien à Tensho. Le menu du chat sur un lien d'objet reçoit maintenant l'entrée Tensho (objet du lien du jeu = objet survolé) ; VALIDATED_IN_GAME (« AB OK pour tensho », 2026-11-07 : entrée Tensho au clic droit sur un lien d'objet du chat)

## X-25

Fait en 0.782 : toute valeur nulle ou trop grande refusée avant l'appel ; `docs/evidence/0.782-raphael-freeze.md` ; vérifié en jeu le 2026-11-07 15:25 sur capture : plus de gel sur cette recette ; 0.783 : « Raphael non utilisé » au lieu de « a échoué »

## X-29

Fait en 0.788 : c'était détecté (calcul avec les attributs relevés du bon métier), mais dit en couleur d'information et coupé ; maintenant « Mauvais métier : vous êtes Menuisier, cette recette est de Métallurgie », en avertissement, avec le bouton pour porter le métier

## X-31

Fait en 0.790 : une branche dépliable par étape avec son compte ; lignes « possédés / demandés » ; métier et fabrications restantes au survol de l'icône ; validé en jeu le 2026-11-07 (« c'est parfait »)

## X-32

Fait en 0.793 : la capture 0.792 montre la zone du mandat dans la liste propre du jeu quand celle de la fenêtre carte est vide ; Tensho l'ajoute, et les points du mandat commencé ; `docs/evidence/0.793-game-leve-markers.md` ; validé en jeu le 2026-11-07 (« ok pour mandats ça fonctionne »)

## X-34

Cadre résumé le 2026-11-07 (information seulement, coupé en JcJ, dessin sur la vue 3D déclaré) ; réponse « ça en plus » avec X-41, lue comme accord ; lecteur des monstres proches en 0.795 (ennemis vivants ciblables, 100 au plus, nom, lieu, niveau)

## X-36

Fait en 0.796 : livre sur la carte (rang actuel de chaque carnet, victoires au survol) ; réglage dans la carte et sur la page Carnet de chasse ; une seule couche « monstres proches », toutes les raisons au survol

## X-37

ALÉA fait en 0.797 (étoile, nom de l'ALÉA au survol, `GameObject.FateId`) ; mandats : 0.797 capture l'événement des monstres en mode développeur, à relever pendant un mandat de combat ; ALÉA validé en jeu le 2026-11-07 sur capture (étoiles sur les monstres de l'ALÉA)

## X-38

Fait en 0.798 (carte, réglages, données du wiki lues au clic) et 0.799 (icône du mage bleu au-dessus de la tête, dans la vue du jeu ; aussi au choix pour listes, bestiaire, ALÉA ; taille, distance, combat)

## X-43

Cadre accepté le 2026-11-07 (« Oui, ce cadre ») : fichier local, provenance par entrée, aucun autre joueur, rien d'envoyé, pas d'aide au combat, coupé en JcJ, import avec fusion et export de fichiers ; 0.807 : le socle (page Découvertes, export, import qui joint, débogage) et les lieux des monstres ; 0.809 : capture des messages du jeu (5 par type, noms masqués) pour le butin, les sorts bleus et les captures

## X-44

Fait en 0.808 : le journal montrait un menu « Default » de RecipeNote avec l'objet survolé (12893, 12846), écarté par la règle des personnages ; les fenêtres d'objets du carnet sont maintenant acceptées

## X-45

0.810 : les PNJ placés par un événement (planevent.lgb, FestivalID) ne sont montrés que pendant cet événement ; 0.811 : aussi ceux des couches de quête et d'ouverture (QST_, QB_, EVT_OP), jamais montrés, `docs/evidence/0.811-conditional-npcs.md` ; 2026-11-07 21 h, sur capture : PNJ d'événement toujours sur la carte (Limsa, l'Entrepont) avec 0.810 : rouvert, à reprendre après la présentation des relevés ; rouvert encore le 2026-11-07 (capture de la Dunette avec 0.811) : les positions venues de la feuille Level ne portaient pas la condition de leur couche ; régression de 0.823 : carte 12, 1 marchand montré et 13 écartés (marchands permanents de la feuille Level marqués « quête » par une couche de quête) ; 0.828 : une position de la feuille Level ne prend plus que la condition d'événement ; 0.829 : « QST_ENPC » permanent (0.811 cachait de vrais marchands) ; un marchand placé par aucune couche, seulement par la feuille Level, n'est plus dessiné (les 9 de la Dunette sont des PNJ d'événement) ; validé en jeu le 2026-11-07 (« pnj event ok »)

## X-46

0.850 : une autre carte que celle du joueur (le jeu garde la dernière vue, donc M en demandait souvent une autre) et une carte choisie dans les menus s'ouvraient entières, hors du réglage ; elles suivent maintenant le réglage d'ouverture (fixe, dernier, entière), centrées

## X-47

0.852 : menu : le journal du 2026-11-07 22:11:48 montre le menu de JournalAccept ouvert (objet 5707) mais écarté comme menu de personnage (nom présent) ; les fenêtres de quête (JournalAccept, JournalResult, JournalDetail) sont maintenant des fenêtres d'objets. Panneau : pas de preuve ; le mode développeur note « Item hover without panel » avec la raison (piste : l'interface du jeu masquée pendant le dialogue, qui masque aussi les fenêtres des plugins : règle Dalamud, accord à demander si c'est ça)

## X-48

0.853 : l'entrée existait sur toutes les icônes mais n'apparaissait que sur une carte qui a sa propre éthérite ; le Tillac de Limsa n'en a pas (portes d'éthernet seulement) : une carte avec une porte d'éthernet ou une voisine compte maintenant, et la question de téléportation dit le chemin (0.833)

## X-51

Fait en 0.815 : même cadre et même réglage que l'aide à la récolte ; sans plan (aucun seuil connu, comme l'Œil-de-verre), rien à surligner ; rouvert le 2026-11-07 : l'encadré doit être sur la fenêtre des collectionnables du jeu, pas sur les barres de raccourcis ; 0.816 : encadré sur le bouton de la fenêtre du jeu qui porte l'icône de l'action (composants d'icône de la fenêtre, icônes listées dans les valeurs brutes du mode développeur)

## X-52

Fait en 0.817 : pour son monde, le jeu donne autant (offres, 20 dernières ventes, moyenne, ventes par jour), pas les autres mondes ni les objets jamais ouverts ; un prix vu remplace celui d'Universalis quand il est plus récent ; rouvert le 2026-11-07 : sur un objet sans copie d'Universalis, le prix vu effaçait les autres mondes (plus de requête Universalis) ; 0.821 : sans copie d'Universalis, le prix vu attend sa réponse (sauf source coupée), puis les deux sont réunis

## X-53

En cours : les données MobDrop relient bien 12888 aux yaks laineux 3479 et 11214 ; 0.818 note la chaîne au journal en mode développeur ; l'icône au-dessus de la tête des monstres de liste est désactivée par défaut (réglage « Butin pour vos listes ») ; 2026-11-07 : le loaghtan aussi ; « données viennent du wiki pour le moment » : correspondance par nom et monstres du wiki en plus ; 0.819 : correspondance par numéro, par nom et par les pages du wiki sur le disque ; à vérifier en jeu ; 2026-11-07 22:28, cocatrix : toujours rien, et aucune ligne « Nearby monsters » au journal ; cause trouvée : le minuteur « une fois par seconde » partait de long.MinValue, le calcul débordait et le calcul des monstres proches ne s'exécutait jamais (aussi la base de découvertes et l'encadré des rangements) ; corrigé en 0.828 ; VALIDATED_IN_GAME pour les monstres du carnet de chasse (« monstre bestiaire OK », capture d'un élémentaire de terre, Gladiateur 07, 2026-11-07) ; VALIDATED_IN_GAME aussi pour les monstres des listes (« monstre ressource liste ok », océanide, viande d'océanide, 2026-11-07)

## X-54

Fait en 0.820 : cases repérées par l'icône de l'objet dans les fenêtres de rangement, encadrées ; journal « Storage highlight » en mode développeur pour vérifier les fenêtres et les icônes ; 2026-11-07 : « toujours pas présent » : même cause que X-53 (minuteur à long.MinValue), corrigé en 0.828, à réessayer

## X-57

À faire, après X-56 ; en cours : le jeu donne deux marqueurs pour la quête 2046 « Le pari » sur la carte 11 (icônes 71141 et 71143, objectifs 2046 et 520), sa liste propre la place à X 10,7 Y 9,7 ; 0.825 capture chaque marqueur avec sa position ; 2026-11-08, « Une faim en queue de poisson » (Tillac) : la cible est juste (Les Sept Sauges, X 11,7 Y 8,1), l'icône de quête non (au Bismarck, X 9,9 Y 8,1) ; la carte du jeu ne montre que celle des Sept Sauges

## X-60

0.830 : « Placer le drapeau » ajouté (même point que « Y aller », sans téléportation) ; « Y aller » absent quand la position du point n'est pas connue : journal « Go there missing » en mode développeur pour savoir pourquoi ; 0.834 : « Y aller » absent pour l'Échalion cyclope : objet caché, voir X-64

## X-61

0.831 : le point suivant du même genre partage ses objets, et sa fenêtre peut s'ouvrir avant que la fermeture soit lue : il gardait la marque « PR dépensés » du point d'avant. Un autre point (ligne du jeu) ou des essais qui remontent repartent de zéro

## X-62

0.832 : une alarme ajoutée depuis une liste garde la quantité demandée ; objet détenu en cette quantité : elle ne sonne plus (ni chat, ni son, ni Discord), « objet réuni : muette » dans la page Alarmes. Les alarmes ajoutées avant 0.832 ne savent pas la quantité : à retirer et ajouter de nouveau depuis la liste

## X-63

0.833 : zone sans éthérite : d'abord une porte de l'éthernet placée dans la zone (Porte du Prologue, de l'Épilogue : rangées par le jeu sous l'Arrière-pays), par l'éthérite de sa ville ; sinon l'éthérite harmonisée la moins chère d'une carte voisine, et la porte de son éthernet la plus proche de l'entrée ; la question de téléportation dit l'étape d'éthernet, ou en rouge qu'il n'y a pas d'éthernet (pas de téléportation directe) ; preuve docs/evidence/0.833-zone-entries.md

## X-64

0.834 : cause trouvée par la sonde des données : Haricots émeraude et Échalion cyclope ne poussent que comme objets cachés de leur point (Colonie du Propriétaire, Forêt des Chocobos), et « Y aller » écartait les sources cachées ; elles passent maintenant après les autres ; preuve docs/evidence/0.834-hidden-gathering-items.md ; 0.835 (règle de l'utilisateur : « écarter seulement si indisponible ») : un point que le personnage ne peut pas encore récolter (livre de folklore non lu, niveau trop bas) est écarté

## X-65

0.838 : un ingrédient qui se fabrique, pris dans le stock, n'était inscrit dans aucun groupe (le calcul s'arrêtait là) : il reste maintenant une ligne, pour la part prise ; l'icône d'entrepôt (0.822) dit alors qu'il est chez un servant ; après 0.838, toujours pas de sirop (capture) : 0.840 note au journal (mode développeur) ce que le panneau demande et obtient, chaque ligne avec stock, recette, groupe ; journal du 2026-11-08 00:00 : la liste d'artisanat passe ses ingrédients directs (12892 sirop compris) au premier niveau, que la 0.838 laissait de côté : 0.841, à tous les niveaux

## X-66

0.839 : les cases étaient trouvées par l'icône seule ; maintenant icône et quantité affichée, comparées aux piles du rangement ouvert (deux objets pareils en tout : pas d'encadré plutôt qu'un faux) ; une case vide garde son ancienne icône image cachée, et l'onglet caché cache un parent : la case, son image et tous ses parents doivent être visibles

## X-67

0.842 : la règle du jeu (seuil, codage) n'est pas connue : capture « mapMarkers » en mode développeur (marqueurs de la carte du jeu, drapeaux et icône secondaire, hauteur du joueur), et hauteur des marqueurs d'événement ; à relever carte du jeu ouverte devant une flèche ; aussi sur les zones de quête (grande flèche orange dans la zone, capture de la quête de cuisinier, Mor Dhona) ; capture du 2026-11-08 00:12 : le jeu ne garde pas la flèche (Flags 40 à la hauteur du joueur comme 38 yalms plus bas), il compare les hauteurs en dessinant ; 0.844 : flèche orange haut ou bas sur les icônes de quête, d'événement et d'ALÉA, seuil réglable (10 yalms par défaut, choix de Tensho, celui du jeu n'est pas connu)

## X-68

Précisé : « depuis le journal ». 0.845 : ouverte depuis le journal, une quête qui a une zone est zoomée pour la montrer entière (60 % de la vue) ; sans zone, le zoom reste (demande de la 0.637) ; correction de l'utilisateur : « il faut pas zoom il faut centrer sur quête, objectif, ressource ou autre » : 0.846 retire le zoom, la carte centre sans changer le zoom

## X-69

0.847 : la cible d'une quête précise ne partait que quand la quête n'était plus active (journal relu une fois par minute) ; elle suit maintenant le point de quête où elle était, comme la cible sans quête (0.813) : étape faite, elle disparaît

## X-70

0.848 : la liste de la fenêtre carte (AgentMap.EventMarkers) n'est refaite qu'à son ouverture ; la liste de quêtes du jeu (Map.QuestMarkers) suit chaque étape (journal 00:36:56 : quête 2607, carte 371, rayon 35, fenêtre vide) : ses quêtes affichées remplacent celles de la fenêtre ; preuve docs/evidence/0.848-quest-markers-current.md

## X-71

0.849 : le chevron orange du jeu (ui/uld/NaviMap.tex, pièce U 352 V 96, 24 × 24, sondes UldProbe et MapMaskProbe --tex), pointe en haut pour plus haut, retourné pour plus bas, sur toutes les icônes de quête, d'événement et d'ALÉA ; preuve docs/evidence/0.849-height-chevron.md ; 0.851 : un peu plus petit (capture de l'utilisateur), 55 % de l'icône

## X-72

Revue du 2026-11-08 : le collecteur marche (1 469 relevés, 82 monstres, 10 cartes, 418 Ko en quelques heures ; prix vus : MarketSeen.json, 1 objet), mais : (1) aucun outil ne s'en sert, seule la page Découvertes les montre ; (2) cases de 10 yalms : 87 % vus une fois, 640 relevés au lieu de 1 469 avec des cases de 50 yalms ; (3) un seul genre relevé (places de monstres). Propositions : A brancher les places vues sur la page Butin des monstres et le carnet de chasse ; B cases de 50 yalms (fusion des relevés existants) ; C d'autres genres. Choix de l'utilisateur : « ABC ». 0.854 (A) : la page Butin des monstres et le carnet de chasse montrent « Vu par vous : zone (X, Y), niveau », un lieu par carte, quatre au plus, clic droit éthérite la plus proche ou drapeau ; 0.855 (B) : cases de 50 yalms ; au chargement et à l'import, chaque relevé est replacé dans sa case et ceux d'une même case sont fusionnés (vues et imports additionnés, niveaux et dates les plus larges, premier lieu gardé), la base est réécrite ; 0.856 (C) : deux genres de plus : où apparaissent les monstres de chasse (rang B, A, S : dans aucune donnée du jeu) et les ALÉA vues (lieu, niveau, quand, une fois par heure au plus) ; chacun son réglage, cherchés dans Monde › Découvertes

## X-73.1

0.857 : le meilleur prix du centre de données prend aussi les prix vus sur chaque monde visité (offres de moins de 24 h ; le monde d'Universalis cède à un relevé plus récent sur place) ; rien de plus vieux que 90 jours n'est chargé ; la page Marché et le comparatif des servants restent à faire (X-73.12)

## X-73.3

0.859 : le panneau de pêche lit d'abord le carnet du jeu ; « aucune condition d'heure » ou « de météo » y retire la fenêtre ou la météo de GatherBuddy et de Garland ; « ni heure ni météo (jeu) » affiché ; inconnu : rien ne change

## X-73.4

0.861 : catégories de la semaine par leur numéro dans le jeu (FashionCheckManager.Theme.ItemThemes), le nom en secours ; thème lu dans le jeu au lieu de « semaine + 9 » ; la tenue jugée (objet, catégorie, note brute) gardée dans les découvertes ; le sens des notes reste à prouver

## X-73.8

0.862 : butin des sous-marins par secteur (HousingWorkshopSubmarineGathered.Point) et secteurs débloqués (UnlockedPoint), une fois par voyage ; les aéronefs ne donnent pas de secteur par entrée : laissés de côté

## X-73.12

0.864 : fêtes vues (GameMain.ActiveFestivals, une fois par jour, premier et dernier jour) et panoramas inscrits (météo, heure d'Eorzéa) ; 0.868 : page Marché, onglets Offres et Historique complétés par les offres et ventes vues au tableau du jeu (offres d'un monde vues après la dernière revue d'Universalis, moins de 24 h ; ventes sans doublon ; seules si Universalis n'a rien) ; restent : animaux de l'île, marchands sans Level, comparatif des servants (les offres vues n'ont pas de vendeur : vos autres servants ne se distinguent pas, laissé à Universalis)

## X-73.13

0.869 : relevés ajoutés à la capture du mode développeur (« Reading fishline », « purify », « npctrade » dans le journal ; les effets de pêche étaient déjà relevés par « statuses ») ; attend les relevés en jeu

## X-73.14

0.870 : chaque offre vue garde seulement « à vous / pas à vous » ; ventes des servants comparées aux offres vues sur votre monde quand plus récentes qu'Universalis (moins de 24 h) ; « copier le prix » de la page Marché compte les offres vues qui ne sont pas à vous

## X-73.15

0.871 : région lue dans la feuille du jeu (Japon, Amérique du Nord, Europe, Océanie, Chine, Corée) ; la table d'Universalis n'est plus demandée que pour le centre « cloud » ou un inconnu ; `docs/evidence/0.871-regions.md`

## X-73.16

0.872 : PNJ ciblé noté avec son lieu quand une fenêtre de boutique s'ouvre (une fois par ouverture) ; utilisé pour les vendeurs que les feuilles ne placent pas (page Marchands, info-bulle, provenances, avant LuminaSupplemental)

## X-73.17

0.873 : en Eurêka, Bozja, Croissant occulte et donjon sans fond (reconnus par le jeu lui-même), un objet reçu juste après un monstre vaincu compte comme butin de ce monstre ; en donjon sans fond, un objet reçu sans rien dépenser compte comme coffre de l'étage ; les sacs et coffres ouverts ensuite étaient déjà notés (objets ouverts, 0.860)

## X-73.18

0.887 : relevé de pêche de l'utilisateur (`docs/evidence/0.887-fishing-bite.md`) : type de touche (animation 7 : 292 !, 293 !!, 294 !!!), temps depuis le lancer (les leurres ne le relancent pas), lancers sous Amorçage (763) exclus ; ligne « Vu par vous » du panneau de pêche ; 0.888 : éthérolyse relevée en jeu (`docs/evidence/0.888-aetherial-reduction.md`) et parseur ; correction de vie privée de la capture « messages » ; 0.889 : remise relevée en jeu sur un mandat (`docs/evidence/0.889-hand-in.md`) ; parseur des objets demandés par les quêtes (quête dont l'étape change dans les 15 s), à confirmer à la première remise de quête ; reste Intuition et prédateurs ; 0.890 : harpon relevé en jeu (`docs/evidence/0.890-spearfishing.md`), parseur de l'ombre (taille, vitesse) d'une prise quand une seule sorte d'ombre est en vue, à valider au prochain harpon

## X-74

Fait pour la nuit (0.857 à 0.867) ; 0.860 : observateur des objets obtenus (ItemDiscoveryWatcher) : désynthèse, tâches, coffres de mission, récompenses d'ALÉA, butin de sa cible, objets ouverts, poissons pris (appât, lieu, météo, heure) ; textes du résumé des genres de la 0.856 qui manquaient ; 0.861 à 0.864 : revue de mode, voyages, mage bleu et dresseur, fêtes et panoramas ; 0.865 : « Comment l'obtenir » montre d'abord « Vu par vous », qui dispense de Garland et du wiki

## X-75

Fait par X-75.1 à X-75.8 (0.874–0.903) ; revérifié par X-91 (0.971, docs/evidence/x91-parsers-check.md) : chaque donnée prise hors des feuilles a un relevé en jeu ou une raison écrite (Universalis pour les servants, crochet pour PaissaDB)

## X-75.1

0.874 : rapport `docs/project/audits/0.874-probe-coverage.md` (64 sources, 46 sondes) ; `ProbeSet874Tests` fige les 17 parties de la capture et teste les gardes des sondes qui n'en avaient pas ; relevés en jeu de chaque sonde listés dans TESTING (0.874)

## X-75.2

0.875 : page Monstres (« Vu le laisser »), monstres proches (butin vu, bêtes apprivoisées même sans le wiki), provenances (missions dont un coffre l'a donné, objets d'où il vient), « Vu donner » dans les usages ; 0.876 : menu clic droit (mission et monstres vus), listes des coffres de missions (« Coffre (vu par vous) »), donjons sans fond (« Vu dans les coffres »)

## X-75.3

0.877 : drapeaux du lancer lus (mooch → poisson pris avant, leurres), météo de la période précédente (prévision du jeu), ligne « Vu par vous » dans le détail du panneau de pêche (appâts, météos, heures, leurres) ; jamais présentée comme une condition

## X-75.4

0.878 : monstres vus lançant l'action d'un sort de mage bleu (barre d'incantation, une fois par incantation, tout job), avec niveau et lieu ; fenêtre d'un sort : « Vu par vous » avant le wiki ; le tri de la liste par niveau garde le wiki

## X-75.5

0.879 : fêtes (« Vue en cours par vous du … au … »), panoramas (« Inscrit par vous par … à … ET »), meilleur prix de la région complété par les mondes de la région visités (feuilles World et WorldDCGroupType) ; reste la page des rangs S (lieux des ALÉA vus), avec X-76

## X-75.6

0.880 : horaire de la pêche en mer contrôlé contre la route lue à bord (une fois par voyage, écart signalé dans le journal) ; météo prévue contrôlée contre celle du jeu 10 s après l'entrée dans une zone ouverte (comptée, journal de débogage). Décision à soumettre à l'utilisateur (lot de fin) : appeler les fonctions de calcul du jeu pour les sous-marins, comme pour les aéronefs (DEC-023 point 3). Décidé : DEC-043 (« oui si lecture pure »). 0.1006 : sonde en mode développeur (« Submarine voyage check » au journal), fonctions du jeu comparées à la formule ; leur emploi suit le relevé ; premier relevé (hors de l'atelier) : tout à 0, la formule reste ; 0.1007 : nouvel essai à chaque zone, relevé à l'atelier (docs/evidence/0.1006-submarine-functions.md)

## X-75.7

0.881 : parties « addons » (nombres seulement des fenêtres harpon, aéronef, résultats de sous-marin, sertissage forcé, extraction de matéria, courrier), « fashion » (aperçu et meilleur score avec teintures), « islandobjects » (animaux de l'île) ; déjà couverts : récolte abondante et effets (statuses), collectionnabilité (synthesis), récolte du jardin (capture du jardin), fin de mission (messages). Boutique ouverte : structure non sondée, laissée. Parseurs après les relevés en jeu

## X-77

0.882 (accord de l'utilisateur, « Oui, ce schéma ») : « Récupérer les découvertes communes » (fichier `community-discoveries/discoveries.json` de la branche main, sur clic, joint comme importé, une fois par version du fichier) ; « Partager les miennes » (vos propres découvertes dans un zip, dossier Export et page « nouveau ticket » ouverts dans le navigateur, modèle « Discoveries ») ; fusion par le mainteneur avec `tools/discoveries-merge.py` ; le fichier arrive chez les joueurs avec la release suivante

## X-77.1

0.883 : application OAuth créée par l'utilisateur (Client ID Ov23liBmk4RCUYw6pU6Z, Device Flow, jetons sans expiration) ; « Connecter GitHub » (code à approuver), « Échanger maintenant », « Déconnecter », réglage « Échange automatique » (coupé par défaut, une fois par jour) ; jeton protégé par Windows (DPAPI) ; gist secret par joueur mis à jour, un ticket par joueur ; outil de fusion lit les gists ; 0.884 : essai réel de l'utilisateur réussi (connexion, échange, ticket #43, gist) ; outil corrigé (préfixe « gist: », propriété « Key » calculée), 668 faits du premier gist fusionnés dans le fichier commun (aucun refusé, aucun nom) ; 0.885 (« garde que auto ») : boutons « Récupérer les découvertes communes » et « Partager les miennes » et modèle de ticket retirés ; restent l'échange automatique et « Échanger maintenant » ; Exporter et Importer gardés (fichiers locaux, sans Git) ; 0.886 : outil « sync » (lit les tickets « Discoveries », un fichier par joueur dans community-discoveries/players/, fichier commun reconstruit, jamais compté deux fois, FLAG au-delà de 5 % de refus) ; règle de l'agent écrite (contrôle à chaque reprise et avant chaque release, demande de l'utilisateur)

## X-78

0.890 : icônes identifiées dans les fichiers du jeu ; le rayon de FishingSpot n'a pas d'unité prouvée : la ligne « Game map hosted » écrit maintenant la zone du jeu (Scale) ; relevé à faire : carnet de pêche › un trou de La Mer de Rubis › afficher sur la carte ; puis la zone sera dessinée ; 0.891 (précision de l'utilisateur : l'icône de pêche normale n'a ni rayon ni la bonne icône, prendre le poisson du harpon en orange comme sur FF14) : icône orange du jeu 60465 sur les trous de pêche ; zone toujours en attente du relevé ; 0.894 : zone mesurée sur la carte du carnet de pêche (capture de l'utilisateur, `docs/evidence/0.894-fishing-spot-area.md`) : rayon ≈ 0,155 × FishingSpot.Radius, dessinée sur les trous de pêche ; comparée au carnet de pêche par l'utilisateur : « c'est ok je pense »

## X-79

0.891 : tous les poissons du point (sinon de la zone) toujours listés dans un ordre fixe, avec leur ombre (vue par vous d'abord), ceux en vue en couleur ; règle du poisson harponné corrigée (emplacement qui part en gardant sa taille, `docs/evidence/0.891-spearfishing-slots.md`) ; 0.892 (capture de l'utilisateur : liste de toute la zone, « elle change en permanence ») : mise en page fixe (trois lignes d'emplacement, lignes qui ne bougent pas), point trouvé à l'ouverture (cible, sinon le point le plus proche) et gardé ; sans point, seulement vos prises et les poissons en vue ; 0.893 (demande : l'icône du poisson pour aider visuellement) : icône de chaque poisson dans la liste (cadre en couleur quand il est en vue) et icônes des poissons de cette ombre sur chaque ligne d'emplacement ; 0.894 : la liste du point se trouve par sa base de récolte (plusieurs points partagent une base : 32074–32076 → 465), les poissons que le jeu montre pour ce point ; 0.896 (capture : Crabe-pagaie noté grand et très rapide) : le poisson pris arrive un instant après le compteur, attendu ; vitesse gardée à l'apparition (elle peut doubler), le panneau compare aussi à sa moitié ; ombres de la 0.891 écartées (`docs/evidence/0.896-spearfishing-lag.md`) ; 0.896 validé en jeu (9 prises cohérentes) ; 0.897 (demande : moins de texte, lisible en temps réel) : tailles S/M/L, vitesse en chevrons, étoile pour grand garanti, œil pour vu par vous, mots au survol ; 0.898 (demande : les prises multiples, fréquentes) : poissons lus dans le chat, emplacements harponnés dans la fenêtre ; ombres pareilles → pour tous, sinon par élimination avec les ombres déjà vues, l'ambigu n'est pas noté ; validé en jeu : 6 doubles prises toutes justes (les triples, même règle, pas encore vues)

## X-81

0.899 : taux affichés par le jeu (matéria de degré I : 80, 40, 20, 10 %) identiques à MateriaJoinRate / MateriaJoinRateGatherCraft que Tensho lit déjà (`docs/evidence/0.899-overmeld-rates.md`) ; seuls les degrés XI et XII viennent de Teamcraft, à vérifier avec une matéria XI ou XII ; capture : limite de lignes par fenêtre

## X-82

Relevé : tenues jugées lues (thème 467, catégories, note par emplacement : 4 sur les quatre emplacements thématiques, 5 ailleurs ; teintures de la semaine : 68 → 70), aperçu lu (thème, meilleur score, tentatives) ; parseur des tenues jugées (0.861) validé en jeu ; 0.900 : seule l'image de l'icône est atténuée, son info-bulle et son menu ne le sont plus

## X-83

0.901 : les animaux capturables (MJIAnimals, par BNpcBase) autour de vous sur la carte, avec leur icône du jeu ; réglage « Animaux de l'île » (`docs/evidence/0.901-island-animals.md`) ; validé en jeu (« icônes ok ») ; 0.902 (retour : les calques de monstres n'étaient pas pris par « Tout » et « Rien ») : corrigé, test sur toutes les cases du menu ; validé en jeu (« filtre ok »)

## X-84

0.903 : pièces jointes lues (emplacements, quantités, vides après « Tout prendre », `docs/evidence/0.903-letters.md`) ; les gils d'une lettre (reçus ou joints) vont dans la catégorie « Courrier » du journal des gils ; les objets pris d'une lettre étaient déjà écartés des découvertes

## X-85

V1 faite (0.917) : horaire vérifié par port par vos voyages, aide à la pêche ordinaire coupée en mer. V2 faite en partie (0.918) : panneau avec icônes, appât en main, touche et temps de touche du lieu, poissons du courant spectral repliés ; reste pour V2 : points par poisson et type de mer (lecture de GatherBuddy à étendre). Suite : V2 panneau, V3 objectifs, V4 courant spectral, V5 pêche au vif, V6 assignats (indice de l'utilisateur, 2026-11-08 : les assignats orange viendraient du score en fin de voyage ; source communautaire Reddit r/ffxiv « lvl100 ocean fishing scrips » : seuil de 10 000 points, en dessous assignats mauves seulement ; à prouver par le relevé de fin de voyage, points contre Scrip1/Scrip2). Conséquence pour V2/V3 : mettre en évidence les poissons qui rapportent beaucoup de points et le courant spectral, avec le score visé. À faire juste après le format des découvertes (DEC-042, « au plus vite »). Courant spectral : vos relevés d'abord, puis une source communautaire marquée (DEC-044). Indice de l'utilisateur : en français, le poisson qui déclenche le courant spectral porte « spectral » dans son nom (à vérifier dans les feuilles Item et IKDSpot, puis par le relevé du déclenchement ; captures de l'utilisateur : « Discus spectral », détroit sud de Merlthor, `checkpoints/x85-*`). Plan proposé V1 à V6 et questions ouvertes : `docs/ai/RESUME-2026-11-08.md`. Recherche faite : `checkpoints/ocean-fishing-research-2026-11-08.md` (plan V1 à V6). Relevé du 2026-11-08 : 27 prises en mer notées, touche « !!! » (animation 294) lue trois fois : parseur de touche 0.887 validé pour « !!! »

## X-99

0.977 : cause trouvée (journal : la liste de la fenêtre carte du jeu était celle de la carte du monde, 919) ; ces quêtes sont lues aussi dans la liste du jeu (Map.UnacceptedQuestMarkers), à jour sans fenêtre

## X-107

0.986 : entrée « Tensho Assistant (Gil) » de la barre du serveur (réglage de l'onglet Gils, coupé par défaut) : net du jour depuis minuit, gains et dépenses au survol, transferts exclus, clic : le journal
