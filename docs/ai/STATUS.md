# État courant (mis à jour 2026-11-03)

VERSION: 0.499.0.0 (dernière release : Stable-0.451.0, publiée le 2026-11-03 par l'agent, à ta demande ; notes : beaucoup de tests en jeu restent à faire) — Verify.ps1 (agent) 939/939, 0 avertissement, 0 erreur, langues et paquet verts. Détail par version : [CHANGELOG.md](../../CHANGELOG.md).
GOAL: fin de la file — commit et release, décisions par lots, tests en jeu (étape h), g2 sans ultracode ; voir [GOAL](GOAL.md) et « Prochaine action ».
QUOTA: réserve utilisateur 1 % (5 h et semaine). Relevé 2026-11-02 : forfait Max, 5 h à 0 %, semaine à 0 % (réinit. jeudi 23:00).

## Validé en jeu (rapports utilisateur, 2026-10-30/31)
- Carte Tensho : image, icônes fixes, menus Région/Zone, colonne d'outils, M remplace la carte du jeu, drapeau (pose, centrage, sans fermeture),
  téléportation avec coût réel, quête du journal centrée avec icône de la cible, liste des icônes survolées (comme Mappy), priorités,
  points de récolte (minage, botanique, pêche) avec horaires, courants éthérés obtenus masqués, vendeurs et infobulles, noms de mandats.
- Carte 0.151–0.155 (rapport utilisateur 2026-10-31, « carte et rowena ok ») : carte du jeu hôte hors écran, zones par type et couleur,
  icônes des onglets du clic droit, zoom, Paramètres › Carte, icônes des commises de Rowena sur la carte et dans l'onglet Vendeurs.
- Collections : état obtenu/non obtenu des montures et mascottes, sources (vendeurs, échanges, Garland en dernier recours, wiki).
- Antérieur : tri natif, caches objets, PNJ vendeurs, survol des objets (accroche, stock, prix, monstres), thème.

- Icône 0.157 dans la liste des plugins de Dalamud (rapport utilisateur 2026-10-31 ; visible après relance du jeu).

- Triple Triade 0.160 : conseil en match (rapport utilisateur 2026-10-31, « test ok »).

- Carte : marchands des variantes de carte d'une même zone (Auxesia, 0.171 + 0.175), capture utilisateur 2026-10-31.

## Validé en jeu (rapport utilisateur 2026-11-01 : « tous est ok »)
- Tout ce qui était « à valider » : Mini Cactpot 0.158, Triple Triade 0.161–0.162, traduction wiki 0.150, carte Dalamud 0.156, caches et
  tri anciens (0.82–0.84), sources d'objets et liens 0.197–0.199, objet en contexte 0.200, météo et alarmes 0.201–0.205 (dont `/alarm`
  dans le jeu), accueil / horloge / Avancé / contributeurs 0.206, infobulles, menu inventaire, butin ✓/✗ et mission aléatoire 0.207–0.208,
  icônes de quêtes 0.211, Revue de mode 0.213–0.223, choix de la ressource au clic droit d'un point de récolte 0.218.
- Commit et release (`Verify.ps1 -Commit/-Release`, `commit-release.bat`) : releases Stable-0.219.0 à Stable-0.223.0 publiées.

- Rapport utilisateur 2026-11-02 (« c'est ok ») : 0.265–0.269 — menus des cartes de Triple Triade (clic droit propre à la carte,
  « Voir dans Triple Triade » qui ouvre le carnet, infobulle et menu sur le nom, menu sans doublon).

- Rapport utilisateur 2026-11-02 (« c'est ok ») : 0.270–0.271 — marché enrichi (annonces, filtres, coût pour N unités, historique,
  tri des colonnes, icône de gil).

- Rapport utilisateur 2026-11-02 (« c'est ok ») : 0.276–0.279 — Triple Triade : sous-onglets de « PNJ et cartes », valeurs et
  tri des cartes, score par règles, cartes Ex. et onglets « Cartes » / « Cartes Ex. ».

- Rapport utilisateur 2026-11-02 (« c'est ok ») : 0.283–0.286 — types de quêtes, pourcentages, cités de départ et options non
  prises (épopée d'ARR à 100 %).

- Rapport utilisateur 2026-11-02 (« c'est ok ») : 0.287–0.289 — infobulles des réglages, taille et cône de vision sur la carte,
  valeurs par défaut de la carte reprises de ses réglages, couleur du cône.

- Relevé utilisateur 2026-11-02 (DLL 0.292) : toutes les pages sous 0,5 ms par image en médiane
  (`docs/evidence/0.292-draw-times.md`) ; 0.290–0.292 validés pour la légèreté.

- Rapport utilisateur 2026-11-02 (« c'est ok ») : 0.293–0.294 — police de titre du jeu (titres, sections, barre latérale).
  R-06 terminé pour l'essentiel (cadres/textures du jeu découpés depuis les ULD : possible plus tard).

- Rapport utilisateur 2026-11-02 (« c'est ok ») : 0.295–0.296 — boutiques de compagnie libre, sceaux de sa seule Grande
  Compagnie dans les sources.

- Rapport utilisateur 2026-11-02 (« c'est ok ») : 0.297–0.299 — repères au bord de la carte, bouton souris des réglages d'icônes,
  « Retirer le drapeau » dans tous les menus. Reste à relever le type de marqueur des mandats (mandat accepté, carte ouverte).

## Campagne de tests en jeu (étape h)
Commencée le 2026-11-04 (lots dans l'ordre : 1 sur place FAIT ; 2 alarmes et chat FAIT (rapport du 2026-11-04 : « tous est ok ») ; 3 ville FAIT (coffre de compagnie à revoir, 0.462 ; entretien et récolte reportés par l'utilisateur) ; 4 caserne FAIT sur rapport de l'utilisateur (2026-11-04, « Tout est OK »), sonde N-06 enregistrée : docs/evidence/0.463-squadron-probe.log ; 5 atelier FAIT sauf les retours de 0.464 ; 6 récolte et pêche ; 7 carte et chasse ; 8 PNJ ; 9 collections ; 10 reste). Cases cochées dans TESTING.md ; l'agent lit dalamud.log après chaque lot.

## À valider en jeu
Liste complète et à jour, version par version jusqu'à 0.448 : [TESTING](../project/TESTING.md) (cases « [ ] »). Ci-dessous, l'ancien résumé 0.224–0.262.
- 0.224 : estimation du bonus de Récolte abondante II dans l'outil PR (en mineur/botaniste, objet récolté en contexte).
- 0.225 : panneau d'aide à la récolte en jeu (mineur / botaniste séparés).
- 0.226 : panneau d'aide à la pêche (poissons du lieu, appâts, créneau, météo) ; pêche en mer non couverte (pas de source).
- 0.227 : données de pêche de GatherBuddy (Apache-2.0) en premier, Garland en secours.
- 0.228 : ouverture manuelle de l'aide à la pêche (bouton, /tensho peche) ; détection « pêche en cours » vue en jeu (capture 2026-11-01).
- 0.229 : lecture des anciens fichiers GatherBuddy (2 616 poissons) — VALIDÉ en jeu (capture 2026-11-01).
- 0.230 : page Récolte réorganisée (GatherBuddy d'abord, liens vers les modules).
- 0.231 : temps de touche (Teamcraft) et chrono de pêche — VALIDÉ en jeu (capture 2026-11-01).
- 0.232 : aide à la pêche en tableau compact.
- 0.233 : clic droit (outils utiles) sur poissons et appâts ; menu « liens » commun aligné sur le menu de l'inventaire.
- 0.234–0.240 : arsenal, coffre à apparence, armoire, tenues complètes dans la Banque ; correctifs de chargement de la Banque.
- 0.241–0.246 : R-02 (cartes et rouleaux ✓/✗), R-01 (cible du journal retirée), R-03 (Triple Triad au clic droit de la carte),
  R-04 (menu propre à chaque icône de carte), R-10 (caches).
- 0.247–0.262 : R-06 étape 1 (icône et nom d'objet communs, infobulle unique, sources du clic droit en sous-menus, menu de
  l'inventaire du jeu et panneau sous son infobulle alignés sur Tensho, boutons ↺ et « Tout remettre par défaut », infobulles des
  réglages de la carte), langues dans la DLL seule (0.248), alarmes de pêche avec météo (0.253–0.254).

## Prochaine action
(2026-11-04) Lot 6 de la campagne (récolte, pêche, harpon, plan de collection) validé en jeu par l'utilisateur sur la 0.489 (« tout est OK »), sauf la galerie de Mes outils, qui attend une release. Lot 7 : carte, favoris, liens et terrains validés par l'utilisateur (« reste ok »), la chasse et le train de chasse (points 12 à 16) restent à tester quand il en fera. Lot 8 (Triple Triade, Mini Cactpot, revue de mode) et onglet Favoris validés en jeu par l'utilisateur sur la 0.490 (« tous es ok »). Lot 9 (collections, quêtes, contenus, compagnie) validé par l'utilisateur, testé avant l'envoi de la liste (« je l'avais déjà fait ») : rapport de l'utilisateur, le journal du 2026-11-04 après 11:20 n'en porte pas trace. Lot 10 : campagne guidée close à la demande de l'utilisateur le 2026-11-04 (« J'ai casiment tous essaie, les dernières erreurs on verra en jeu ») ; les cases non cochées de TESTING restent ouvertes, elles se règlent quand il les signale en jouant, aucune n'est déclarée validée. Auto-contrôle (0.494) : après chaque rechargement en mode développeur, l'agent lit les lignes « Selftest » de dalamud.log. Demande de l'utilisateur (2026-11-04) : dresseur (bêtes, rangs, Dédale bestial, conseil au moment de former l'équipe). Données relevées : docs/evidence/0.498-xbm-sheets.md. Exception à DEC-009 à consigner si ce conseil est retenu : préparation du Dédale, ni ACT ni mesure de combat. g2 en cours : les 4 valeurs douteuses de R-14 traitées en 0.491 ; catalogues vides et équipement en 0.492 ; il reste 35 constats R-14 à relire un par un (le tri automatique ne dit que si le code cité existe encore, une garde peut être ailleurs), puis I-05, I-15, N-10, hook A-14.
(2026-11-04) Modèles communautaires ([TEMPLATES-001](../goals/TEMPLATES-001.md)) terminés côté code en 0.476 ; la galerie lit `main`, donc une release la rendra visible. Chat personnalisable abandonné ([DEC-030](../project/DECISIONS.md)). Reprendre la campagne en jeu (lots 6 à 10).
(relu le 2026-11-03, 0.446) Files a) à g) FAITES (détail : [ROADMAP](../project/ROADMAP.md) « File d'attente », CHANGELOG 0.315–0.446).
Restent, dans cet ordre proposé :
1) Décisions du 2026-11-03 ([DEC-026](../project/DECISIONS.md)) : 0.447, 0.448 (journal de jardinage, N-08) et release Stable-0.448.0
   faites ; recherche de sources N-06 / N-14 faite le 2026-11-03 ([preuve](../evidence/0.448-n06-n14-sources.md)), décidé (DEC-027) :
   0.449 durées Lotlab du journal de jardinage, 0.450 croisements N-14 et 0.451 escouade N-06 FAITS ;
   la sonde N-06 se fait pendant la campagne de tests en jeu (TESTING 0.451) ; release Stable-0.451.0 faite.
2) Décisions par lots : [G-PROPOSALS](../project/G-PROPOSALS.md) (choix provisoires, écarts bloqués, idées), ROADMAP « Décisions
   reportées à la fin », lignes « État » des quatre fichiers de propositions (MAP, ITEMS, CRAFT-GATHER, TOOLS).
3) h) Campagne de tests en jeu guidée de tout le plugin ([TESTING](../project/TESTING.md)), avec l'utilisateur, avec les sondes en
   jeu nécessaires (N-04, N-07, M-09, A-07, R-06, icônes 71141 / 71151, marqueur des mandats, drapeau 60561).
4) g2, sans ultracode (décision de l'utilisateur, 2026-11-03 : trop coûteux, plus de workflows) : R-14 (résistance aux mises à jour), I-05, I-15,
   A-03 / A-04 (Raphael, DEC-028, [étude](../evidence/0.451-raphael-study.md)), N-10, relecture du
   hook A-14.
5) Publication D17 à la version 1.0.0, moment fixé par l'auteur (DEC-023).
Travail déjà payé, à reprendre sans relancer d'agents (fichiers locaux, ignorés par git, sous `checkpoints/`) :
- `r14-audit-2026-11-03.json` : audit R-14 des 93 fichiers (99 constats). FAITS en 0.452–0.454 : les 23 confirmés, les 12 « plantage » et les hooks A-14.
  Restent 44 constats jamais vérifiés, aucun classé plantage, dont 4 valeurs peut-être fausses (règles de Triple Triade ×2, tenues partielles de la cabine, cases
  collectées de la carte) ; la revue de complétude n'a pas tourné.
- `features-design-2026-11-03.json` : conceptions de I-05, I-15 et N-10 ; I-05 et I-15 relues (à corriger sur quelques points), N-10 sans relecture.
XIVAPI v2 ne remplace aucune source externe : [preuve](../evidence/0.451-xivapi-v2.md).
RÈGLE (2026-11-02) : décisions gardées pour la fin, sauf si bloquant ; l'agent applique le choix recommandé et le note.

## Git (plan du 2026-11-03)
État relevé le 2026-11-03 : release **Stable-0.451.0** publiée par l'agent à 18:57 (notes : beaucoup de tests en jeu restent à faire) ; avant elle, **Stable-0.448.0** publiée par l'agent à 17:25 (décision DEC-026 ; CI verte avant) ; avant elle, Stable-0.430.0 publiée le 2026-11-03 à 12:27 (commits « 0.430.0 », « 0.430.0 · repo.json »,
« Release 0.430.0 ») ; 0.431 à 0.446 commitées et poussées sur Devloppement le 2026-11-03 (un commit « 0.446.0 », le premier fait par l'agent ;
57 fichiers modifiés, 38 nouveaux : code, tests, preuves ; aucune archive, aucun binaire) ; main = Devloppement = Stable-0.451.0 ; GitHub CLI
connecté (compte Shigu330).
Outils (CONTRIBUTING « Versions and releases ») : `.\scripts\Verify.ps1 -Commit` (contrôles, puis commit de tout sur Devloppement,
sujet « x.y.z », corps = notes anglaises depuis le dernier commit, push) ; `.\scripts\Verify.ps1 -Release` (repo.json, fusion dans
main, tag Stable-x.y.z, release GitHub avec latest.zip, main poussée en dernier, reprise si arrêtée) ; `-DryRun` montre tout sans rien faire.
Gestion par l'agent, accordée par l'utilisateur le 2026-11-03 (« OK fais le », règle de CLAUDE.md) :
- Commit : à la fin de chaque version (Verify vert, CHANGELOG, STATUS), `-Commit -DryRun` d'abord (relire la liste des nouveaux
  fichiers et le message), puis `-Commit`. Un commit par version : l'historique suit le CHANGELOG. L'agent lance la commande avec
  `$env:TENSHO_COMMIT_TRAILER` = sa ligne `Co-Authored-By` (ajoutée en fin de message ; vide pour les commits de l'utilisateur).
- Release : jamais d'office ; sur demande de l'utilisateur (par exemple après un lot de tests en jeu) : `-Release -DryRun`, puis `-Release`.
- Jamais : push forcé, réécriture d'historique, `--no-verify`, fusion sur main hors de Release.ps1, réglages GitHub (protections,
  Actions) sans accord. Un arrêt (code 2) ou une erreur : rien n'est relancé à l'aveugle, l'agent rapporte le message.
- Avant chaque commit : aucun secret (adresse Discord, jeton) dans les fichiers suivis ; `checkpoints/`, `bin/`, `obj/` restent ignorés.
- Fins de ligne : avertissements « LF will be replaced by CRLF » sans effet (`* text=auto`) ; la normalisation complète
  (`git add --renormalize .`, décision de 0.323) reste à décider, juste après un commit propre.
GitHub (issues, milestones, labels ; tenus par l'agent depuis le 2026-11-03, en anglais) :
- Milestone 2 « Gameplay modules & feature integration » : #10–#17 (domaines ; commentaire d'état à 0.446), #22 campagne de tests
  en jeu (h), #23 sondes en jeu, #24 décisions en attente. Une issue de domaine se ferme quand la campagne l'a validée en jeu.
- Milestone 3 « Update resilience & deep work » : #25 R-14, #26 I-05, #27 I-15, #28 A-03 / A-04 Raphael, #29 N-10, #30 relecture
  du hook A-14 (label ultracode). Milestone « 1.0.0 — Dalamud publication » : #31 D17.
- Labels ajoutés : in-game-test, probe, decision, ultracode. Une issue est commentée quand une version la fait avancer (numéro de
  version) et fermée seulement sur preuve (Verify vert pour le code, rapport de l'utilisateur pour le jeu).
- CodeQL (.github/workflows/CodeQL.yml) : échouait depuis le 2026-10-31 (branche « Stable » inexistante, Dalamud absent du runner) ;
  corrigé en 0.446 (branches main et Devloppement, Dalamud officiel téléchargé pour la compilation).

## Blocages connus
- Bonus d'Abondance : règle communautaire (frozen_rabbit_tome) appliquée comme estimation citée (0.224) ; à comparer en jeu.
- Repères d'exploration : clos, météo du wiki citée (affichée et utilisée par les alarmes depuis 0.201).
- Plantage 08:19 (2026-10-31) causé par la lecture d'une infobulle de marqueur : lecture bornée depuis 0.133 ; surveiller ([extrait](../evidence/0.133.0-crash-excerpt.log)).

## Couverture des 17 plugins de référence
| Plugin | Coverage |
| Avant-Garde | PARTIAL — répertoire manuel d'indices natifs, équipements signalés, emplacements/stock/contexte et source datée ; thème courant/teintures/score/attache non déduits |
| Blue Mage Helper | PARTIAL — grimoire, numéros, descriptions, lieux renseignés et acquisition confirmée |
| Character Panel Refined | PARTIAL — attributs natifs et valeurs dérivées (StatFormulas, validé 0.182–0.184) ; planificateur d'équipement (0.310–0.314, validé) |
| Collections | PARTIAL — montures, mascottes, emotes, cartes, orchestrion, titres et succès ; preuve de chargement, catégories/fiches/récompenses, runtime à confirmer |
| Craftimizer | PARTIAL — carnet natif, collectionnables ordinaires, qualité initiale et recherche orientée attentes ; optimum global, rotation adaptative et recettes spéciales à compléter |
| Duty Loot Preview | PARTIAL — consultation manuelle par contenu, coffres/récompenses, icônes/contexte, provenance et cache ; drops directs non interprétés, données externes partielles |
| ezMiniCactpot | PARTIAL — solveur exact manuel et mode observé opt-in LotteryDaily ; conseil actualisé, aucune commande ; adaptation/rendu natifs non validés en jeu |
| GatherBuddy | PARTIAL — sources/maps/horaires/météo statiques, point courant MIN/BTN opt-in, carnet natif pêche, appâts/conditions explicites et plages ET/UTC et météo localisée ; plages des critères renseignés, présence/prérequis/harpon et runtime à compléter |
| Item Use | PARTIAL — recettes, mandats, candidats GC/aquarium, orchestrion et sources de quête, survol et menu Inventory opt-in ; autres usages/disponibilité, liens de discussion et runtime à compléter |
| LazyGatherer | PARTIAL — optimiseur manuel avec effets explicitement déclarés, observation du point et copie de ressources ; décodeur natif/bonus/régénération/procs/collectionnables et validation runtime à compléter |
| Macro Chain | PARTIAL — séquences à marqueur final, lancement explicite, arrêt, persistance ; écriture native validée en jeu (0.191–0.196) |
| Mappy | PARTIAL — navigateur natif, repères favoris persistants, ressources de zone et prévisions météo statiques ; météo forcée non observée |
| Monster Loot Hunter | PARTIAL — associations matériau/monstre, fiches/niveaux/lieux et carte de zone, transport/cadence partagés, contexte ; données communautaires partielles |
| Price Insight | PARTIAL — consultation manuelle et prix au survol opt-in, NQ/HQ/monde, cache/cadence/priorités partagés ; contexte mutualisé et scope owner/monde, runtime à confirmer |
| QuestTracker | PARTIAL — états natifs, séquence, fiches/liens et récompenses conditionnelles vers Item ; sources inverses managées, couverture/inconnus et disponibilité non calculée |
| SortaKinda | PARTIAL — profils/modèles, critères, zones protégées/filtrées et inversion, application visuelle après capture directe ; autres filtres/automatisations choisies et validation runtime à compléter |
| TriadBuddy | PARTIAL — carnet/deck/solveur manuel, observation des faces opt-in expérimentale ; identités/règles/trait natifs et autres règles non établis |

## Archive : anciennes « Prochaine action » (file a) à g), reprises 0.191–0.315 ; état ancien, gardé pour l'historique)
FILE D'ATTENTE (demandes du 2026-11-02, à traiter UNE À LA FOIS, dans cet ordre, sauvegarde zip avant chaque grosse modification) :
a) FAIT 0.315 (montages ; revue multi-agents) → retour en jeu attendu.
b) FAIT 0.316 : tour des .md, descriptions des outils (code, annuaire, textes des pages), feuille de route complétée
   (docs/project/ROADMAP.md « À décider / à revoir », « Tests à faire en jeu » → docs/project/TESTING.md).
   Décisions prises le 2026-11-02 (FAIT 0.317, DEC-023 ; textes français au « vous » avec test).
c) FAIT 0.318 : plan des manques docs/project/GAPS.md (103 manques ; preuves docs/evidence/0.318-plugin-gaps.json) ; choix de
   l'utilisateur faits lot par lot dans la conversation (DEC-025) : 101 retenus, I-14 et X-05 plus tard ; ordre en g).
c2) FAIT 0.319 : R-12, webhook Discord pour les alarmes (Réglages › Alarmes › Discord ; revue multi-agents, 23 remarques
   confirmées corrigées) → à valider en jeu à l'étape h (docs/project/TESTING.md « Webhook Discord »).
c3) FAIT 0.320 : R-13, page « Crédits » (Préférences › Crédits, 72 entrées, Credits/CreditCatalog.cs, tests de cohérence avec le
   code) → à valider en jeu à l'étape h.
d) FAIT (5 versions, audit multi-agents du 2026-11-02) : 0.321 (points de la revue 0.316, 1/2) ; 0.322 (textes figés,
   manifeste, SECURITY) ; 0.323 (git, GitHub, outils) ; 0.324 (interrupteurs de sources respectés partout, Universalis a le sien,
   ↺ des anciens réglages) ; 0.325 (infobulles des filtres avec test à cliquet, confirmation de « Tout supprimer » des alarmes,
   Duty Finder) ; 0.326 FAIT (revue multi-agents du point d) : 33 remarques confirmées corrigées, dont la reprise de release).
e) 0.327 FAITE (8 réglages) ; 0.328 FAITE (carte : 28 défauts de l'audit corrigés et relus ; 28 propositions à choisir dans
   docs/project/MAP-PROPOSALS.md) ; 0.329 FAITE (objets, banque, tri, marché ; propositions dans ITEMS-PROPOSALS.md).
   0.330 FAITE (artisanat, récolte ; relue, remarques corrigées). 0.331 FAITE (outils restants ; relue, 7 remarques corrigées, 1 reportée).
   0.332 FAITE (cadre des fenêtres du jeu, réglage désactivé par défaut, à voir en jeu). e) TERMINÉE. f) : point 6 FAIT 0.333 (escouade : mission et entraînement ;
   Planeurs d'Ehcatl : rien de lisible, laissés de côté 0.334) ; point 1 FAIT 0.334 (19 secteurs non confirmés) ; point 2 FAIT 0.335 (prix du marché et paliers de surveillance dans la valeur des trajets ; récupération non modélisée, la source ne donne que des fourchettes) ; point 3 FAIT 0.336-0.337 (arbre de déblocage et butin possible des dirigeables ; planificateur distance/durée par les fonctions de calcul du jeu, accord de l'utilisateur, assemblage non confirmé en jeu ; relecture adverse 0.335-0.337 : 9 remarques corrigées) ; point 8 de DEC-023 (« point 4 » de la file) FAIT 0.338 (taux de surmatérialisation lus dans MateriaJoinRate ; règle des grades VI/VIII/X/XII corrigée : premier emplacement seulement ; 0.339 : XI et XII selon Teamcraft, la feuille du jeu s'arrête à X). point 5 FAIT 0.340 (barde de chocobo ; escouade retirée, défaut recommandé) ; 0.341 : page Chocobo à part, page Personnage retirée (demande de l'utilisateur ; panneau à côté de la fenêtre du jeu gardé, décision de l'utilisateur 2026-11-02). point 7 FAIT 0.342 (R-09 : page Événements, quêtes, boutiques d'événement, noms tirés du PNJ guide). f) TERMINÉE. g) EN COURS, domaine 1 (inventaire et marché) : I-01 FAIT 0.343 (infobulle de prix complète) ; I-02 FAIT 0.344 (monnaies et alertes de plafond, ultracode : conception par workflow, relecture adverse, 9 remarques corrigées). I-03 FAIT 0.345 (un fichier par personnage, onglet Personnages, banque d'un autre personnage). Développement continu pendant la nuit (demande 2026-11-03) : propositions et choix provisoires dans docs/project/G-PROPOSALS.md ; I-05 et I-15 laissés pour une séance en ultracode avec l'utilisateur. I-06 et I-07 FAITS 0.346 (prix PNJ au survol ; offre la plus basse moins 1 copiée). I-08 FAIT 0.347 (notes et étiquettes d'objets, rappel en boutique). I-10 FAIT 0.348 (rapport de rangement). X-07 FAIT 0.349 (liens wiki et Lodestone). I-09 FAIT 0.350 (coffre de compagnie). I-04 FAIT 0.351 (journal des gils, 90 jours). I-11 FAIT 0.352 (conditions des articles de boutique). I-12 FAIT 0.353 (filtres de recherche, objets clés). I-13 FAIT 0.354 (valeur des monnaies). Domaine 1 FAIT sauf I-05 et I-15 (ultracode, avec l'utilisateur ; I-14 « plus tard »). C-01 FAIT 0.355 (contenus débloqués / terminés). C-02 FAIT 0.356 (missions aléatoires du jour, bonus de rôle). C-03 FAIT 0.357 (objets à ranger dans l'armoire). C-07 FAIT 0.358 (apparences identiques du coffre). C-08 FAIT 0.359 (filtre des collections par source). C-09 FAIT 0.360 (liste de souhaits). C-11 FAIT 0.361 (carnet d'exploration complet). Petits écarts du domaine 2 faits. C-05 FAIT 0.362 (tenues du coffre à apparence). C-14 FAIT 0.363 (page Mandats). Consigne 2026-11-03 : toute la feuille de route sans ultracode jusqu'au reset du quota jeudi 2026-11-05. C-12 FAIT 0.364 (carnet de chasse). C-04 FAIT 0.365 (8 types de collection en plus). C-10 FAIT 0.366 (alerte « mission prête »). C-13 FAIT 0.367 (monstres des sorts de mage bleu). C-15 FAIT 0.368 (progression des succès, succès suivis). X-03 FAIT 0.369 (porter un titre, titre au hasard, titre par job). X-02 FAIT 0.370 en partie (musique en cours, favoris ; changer la musique non fait). Domaine 2 FAIT sauf X-04 (partake.gg : API non documentée, conditions inconnues, décision à prendre ensemble, voir G-PROPOSALS). M-01 FAIT 0.371 (alarmes automatiques ventures et vaisseaux). M-02 FAIT 0.372 (Livre d'or, contrats de chasse, cartes au trésor lues dans le jeu, état des roulettes dans le panneau de mission). M-03 FAIT 0.373 (liens de carte du chat : liste filtrable, drapeau, éthérite la plus proche sur clic). M-04 FAIT 0.374 (vos personnages : un fichier par personnage, cases « fait » par personnage). M-05 FAIT 0.375 (vos tâches, panneau « à faire » à l'écran désactivé par défaut). M-06 FAIT 0.376 (usure et symbiose : alerte, barre à l'écran en option, seuils). M-07 FAIT 0.377 (comparaison avec la pièce portée au survol). M-08 FAIT 0.378 (favoris sur la carte Tensho, note, groupe, code de partage TENSHO-MAP1). M-10 FAIT 0.379 (historique du butin des vaisseaux, 180 jours, export CSV). M-09 FAIT 0.380 en partie (paliers de statistiques ; niveau d'objet synchronisé non fait, sonde en jeu nécessaire). X-01 FAIT 0.381 (tâches « Une fois », /tensho todo). M-11 FAIT 0.382 en partie (carnet d'objectifs, Enclave de Doman ; Cactpot et Carnaval masqué : rien de lisible trouvé). M-12 FAIT 0.383 (ensembles d'équipement du jeu, pièces possédées meilleures, code TENSHO-GEAR1). M-14 FAIT 0.384 (emplacement de la carte au trésor déchiffrée). X-08 FAIT 0.385 (job au hasard, équiper sur clic). X-10 FAIT 0.386 (horloges de fuseaux, alarme à date réelle). 0.387 : deux signalements corrigés (grimoire du mage bleu : nom français envoyé au wiki ; onglets de collection de 0.365 sans « obtention »). 0.388 : bouton « Trier » sur la fenêtre d'inventaire du jeu (demande de l'utilisateur). X-06 FAIT 0.391 (barre du tic de PR des récolteurs ; PV/PM laissés : combat, DEC-009). R-01 FAIT 0.392 (page Pêche en mer). 0.393 : Bestiaire (nom français) limité à la grande compagnie du personnage (signalement). 0.394 : réglage des polices (demande). 0.395 : grimoire trié par niveau de monstre sur tous les sorts (livre des sorts du wiki, une page). R-02 FAIT 0.396 (vraies prochaines fenêtres des poissons). R-03 FAIT 0.397 (« Y aller » : drapeau, éthérite la plus proche, téléportation sur confirmation). R-04 FAIT 0.398 (appât en main dans l'aide à la pêche). 0.399 : réglage « Y aller : téléporter tout de suite » (demande). 0.400 : onglets du clic droit de la carte plus coupés (signalement). 0.401 : lecture du wiki des panoramas réparée, onglet Exploration retiré (signalements). R-05 FAIT 0.402 (carnets de pêche, harpon et récolte : ✓, masquer, export CSV). R-08 FAIT 0.403 (alerte PR au seuil). 0.404 : carnets lus malgré 15 lignes de harpon hors table (signalement). 0.405 : ouvrir les carnets du jeu sur une recette ou un objet (demande). 0.406 : règle des langues appliquée à tout le code (messages en anglais traduits, mode développeur traduit, journal en anglais, test de garde). 0.407 : noms du wiki et de Garland (monstres, lieux, missions) dans la langue du jeu. 0.408 : préparation complète dans Artisanat (onglet), vue courte dans Objets ; « tâches de servant » (demandes). R-06 FAIT 0.409 en partie (seuils et assignats à côté de la fenêtre Collectionnable ; collectabilité en cours et action conseillée : à sonder en jeu). R-09 FAIT 0.410 (Raison solide, Eurêka, Vision claire dans l’optimiseur PR). R-10 FAIT 0.411 (« Équiper (métier) » sur chaque lieu de récolte ; 0.412 : option « Y aller » équipe aussi). R-11 FAIT 0.413 (tableau des collectionnables). R-12 FAIT 0.414 (message d’alarme cliquable). R-13 FAIT 0.415 (fenêtre de suivi épinglée). R-14 FAIT 0.416 (liste de récolte par zone depuis la chaîne). R-15 FAIT 0.417 (aide au harpon). R-07 FAIT 0.418 (aide pendant la pêche en mer). Domaine 4 FAIT. Domaine 5 : A-01, X-11, X-12 FAITS 0.419 ; A-02 FAIT 0.420 (repas et remède) ; A-11 FAIT 0.421 (recettes jamais fabriquées) ; A-03 et A-04 (Raphael, dépendance native) pour la séance ultracode. A-10 FAIT 0.422 (Macro Chain) ; A-09 FAIT 0.423 (lien Teamcraft) et signalement « Artisanat : objet » corrigé ; 0.424 : recensé inconnu dans Préparation et « Retour à l’objet » (signalements). A-08 FAIT 0.425 en partie (macro collée ; liens en ligne à étudier) ; 0.426 : Tensho se ferme quand il ouvre une fenêtre du jeu (demande, réglage). A-12 FAIT 0.427 (prérequis et équiper le métier) ; 0.428-0.429 : recette refusée par le carnet du jeu déjà ouvert (signalement : carnet fermé puis rouvert ; bonne recette parmi plusieurs ; 0.430 : blocage par niveau retiré, le carnet montre aussi des recettes au-dessus du niveau). A-06 FAIT 0.431 (bénéfice) ; A-15 FAIT 0.432 (collectionnables des sacs). A-05 FAIT 0.433 (listes de fabrication). A-13 FAIT 0.434 (9 actions, spécialiste). A-07 FAIT 0.435 en partie (recettes expertes en condition normale, conditions dans le moteur ; synthèse en direct experte à prouver en jeu). A-14 FAIT 0.436 (enregistreur de rotation). Domaine 5 FAIT sauf A-03/A-04 (Raphael, dépendance native). Domaine 6 : N-11 FAIT 0.437 (coordonnées collées). 0.438 : actions interdites et « Équiper » dans le panneau du carnet (demande). N-03 FAIT 0.439 (probabilités du Livre d’or). N-01 FAIT 0.440 (contrats de chasse, sans drapeau : pas de position dans les données). N-09 FAIT 0.441 en partie (marques proches sur la carte). N-15 FAIT 0.442 (train de chasse). N-02 FAIT 0.443 (tâches du Livre d’or). X-09 FAIT 0.444 (annuaire des lieux). N-05 FAIT 0.445 (terrains en vente, PaissaDB). 0.446 : N-09 alerte marques A/S (réglage) ; X-13 en partie (annonce copiée, rien envoyé). PROCHAINE : domaine 6 (N-01 à N-10, N-12 à N-15, X-09, X-13, X-14) ; M-13 (Faux Hollows) attend une source des plans du plateau (C-04, C-10, C-13, C-15 : décisions ou ultracode, voir G-PROPOSALS) ; puis I-04, I-05, I-06 à I-10, X-07, I-11 à I-13, I-15 ; puis g2) balayage R-14. (même méthode : audit en lecture seule, défauts corrigés,
   propositions notées pour la fin). Plan détaillé : CHANGELOG 0.321.
   Contenu de d) (demandes) : réglages qui manquent (réglages partiels d'Universalis, Garland et du wiki : résolu à titre provisoire en 0.324), filtres sans infobulle, outils de développement pour la communauté open source,
   amélioration du dépôt git et de GitHub (demande 2026-11-02 : CI, modèles d'issues et de PR, .gitignore, hooks, flux de
   release, protection des branches à proposer — rien n'est poussé ni réglé sur GitHub sans accord) ; points de
   la revue 0.316 : description du manifeste (TenshoAssistant.json) et changelog de manifest.toml, interrupteur de la source de la
   Revue de mode, taille de réponse Teamcraft contrôlée avant lecture, exceptions possibles dans Draw (rappels, essayage),
   « Tout remettre par défaut » (MapView, NavigationCollapsed), limites de recherche d'artisanat non enregistrées, textes français
   figés dans MarketService et TriadSolveSession, politique de versions suivies dans SECURITY.
e) Améliorations générales : la carte en priorité, puis les autres outils ; cadres et textures du jeu (ULD) avant D17.
f) Suite des décisions (DEC-023), dans cet ordre : seuils des 19 secteurs signalés ; prix du marché puis paliers des trajets ;
   dirigeables (arbre de déblocage GPL embarqué, planificateur distance/durée) ; taux de surmatérialisation ; R-11 étape 4 (barde de
   chocobo, escouade) ; rappels escouade / Planeurs d'Ehcatl (sonde) ; R-09 (sonde).
g) Ajouts retenus du plan c) (DEC-025), domaine par domaine : Inventaire et marché → Collections → Monde → Récolte →
   Artisanat → Nouveaux domaines ; liste ordonnée dans docs/project/ROADMAP.md (étape g) ; choix précis dans GAPS.md.
g2) VERS LA TOUTE FIN, juste avant h (demande utilisateur 2026-11-02, R-14) : résistance aux mises à jour de FF14, directement dans la
   logique du code (gardes de chaque dépendance native, dégradation propre, auto-vérification dans Diagnostics, tests, marche à suivre
   après un patch). Règle déjà appliquée à tout nouveau code (docs/ai/RULES.md).
h) À LA FIN DE LA FILE (demande utilisateur 2026-11-02 : « prévois les tests en jeu une fois fini, le code total pas juste
   Discord, tu m'aideras à les faire ») : campagne de tests en jeu guidée de tout le plugin, pas à pas avec l'utilisateur
   (docs/project/TESTING.md, ROADMAP « Tests à faire en jeu »), puis les décisions reportées.
RÈGLE (demande utilisateur 2026-11-02) : « garde les décisions que je dois prendre pour la fin un maximum, sauf si trop important
   pour ne pas être prises sur le moment » : l'agent prend le choix recommandé, le note dans ROADMAP « À décider / à revoir »
   (décisions reportées) et ne pose sur le moment que ce qui bloque ou serait coûteux à défaire.
Publication D17 : à la version 1.0.0, moment fixé par l'auteur (DEC-023).REPRISE 0.303 (2026-11-02, relu contre RULES/GOAL/BACKLOG/USER-REQUESTS) :
1) R-06 : étapes 1 (0.263), 2 (0.264) et 4 (0.270) terminées ; étape 3 faite pour l'essentiel (fiche objet 0.272, Rangements 0.280 ; les fiches quête / PNJ / monstre / contenu existent déjà comme vues détaillées) ; étape 5 en cours (0.287 : infobulles de tous les réglages + test ; reste : nouveaux réglages utiles). Puis étape 6 (légèreté faite et mesurée 0.290–0.292 ; reste le style : cadres et textures du jeu)
   (docs/goals/R-06.md) ; les réglages restants (↺ et infobulle) sont couverts à l'étape 5.
2) Points décidés avant D17 : boutiques de compagnie libre FAIT 0.295 ; repères au bord de la carte (icônes) FAIT 0.297 ; reste : icônes de quête 71141 / 71151 seulement avec un relevé en jeu.
3) R-11 (équipement, docs/goals/R-11.md : étape 1 faite 0.310, mirage 0.311, matérias 0.312, sources et possession 0.314, montages de vaisseaux 0.315 ; suite : étape 4 à décider), R-07 (compagnie libre : page 0.307, meilleurs trajets 0.309, à valider en jeu ; reste éventuel : paliers surveillance / récupération, prix du marché), R-08 (rappels : page faite 0.300, expéditions 0.307 ; reste escouade, Planeurs d'Ehcatl), R-09 (événements) ; R-05 clos sans code.
4) Publication D17. À valider en jeu : liste à jour dans docs/project/ROADMAP.md « Tests à faire en jeu » (protocole : docs/project/TESTING.md) ; résumé : 0.224–0.264 sauf les points validés, 0.272–0.275, 0.280–0.282, 0.300–0.302, 0.307–0.309, 0.315 (0.310 à 0.314, page Équipement, matérias, sources et possession, validées en jeu par l'utilisateur le 2026-11-02 ; 0.303 à 0.306, carte par-dessus, validées en jeu par l utilisateur le 2026-11-02).
REPRISE (quota semaine 97 % le 2026-11-01, arrêt après 0.237) : 1) boutiques de compagnie libre ; 2) flèches boussole sur la carte ;
3) correctifs carte (cible de quête persistante = R-01) ; 4) demandes R-02 à R-10 de docs/project/USER-REQUESTS.md (ex-ISSUES.md, réorganisé),
dans l'ordre que l'utilisateur choisira ; 5) publication D17.
FAIT 0.237 (à valider en jeu) — retour en jeu 0.236 — onglet « Coffre à apparence » de la Banque affiche
« ordre visuel indisponible. Réimportez le rangement ouvert » : la vue par onglet attend une grille (DisplayIndex) ; pour Arsenal, Coffre
à apparence et Armoire, afficher une liste (objet, quantité) au lieu de la grille (MainWindow.Bank.cs, vue des onglets de rangement).
La lecture elle-même a fonctionné (onglets créés, relevé 10:46).
ÉTAPE AJOUTÉE (demande 2026-11-01, après le plan prévu : correctif Banque, boutiques de compagnie libre) : sur la carte Tensho, quand une
quête active ou le drapeau est hors de la vue, une flèche de direction posée sur le bord de la carte, orientée vers la cible (style boussole
de FF14 : icône de la quête ou du drapeau, distance éventuelle) ; clic = recentrer sur la cible.
DERNIERS CORRECTIFS DE CARTE (demande 2026-11-01, avant D17) : une quête ciblée (« cible » de la carte, mapGameTarget) puis accomplie garde
son icône de cible sur la carte et la minicarte : effacer la cible quand la quête n'est plus active ou que son étape change (relire l'état
des quêtes / marqueurs à chaque relevé) ; relever les autres défauts de carte signalés à ce moment. Capture utilisateur : l'icône « Cible indiquée
par le jeu (journal) » (quête « Kuiyki évolue ») reste à vie et ne peut pas être retirée : ajouter aussi un retrait manuel (clic droit sur
l'icône ou bouton « Retirer la cible ») et la retirer quand le journal du jeu n'indique plus de cible. Puis publication D17.
REPRISE — 0.235 arsenal et 0.236 coffre à apparence + armoire FAITS (lecture du jeu, sans autre plugin ; à valider en jeu). Suite du plan :
point 3 boutiques de compagnie libre, puis D17. Point 2 élargi par l'utilisateur : coffre à apparence + armoire +
arsenal, le tout relié au module Banque. API vérifiées (FFXIVClientStructs, lecture seule, réflexion 2026-11-01) :
- arsenal : InventoryType ArmoryMainHand/OffHand/Head/Body/Hands/Waist/Legs/Feets/Ear/Neck/Wrist/Rings/SoulCrystal — non lus aujourd'hui
  par Inventory/ (aucune occurrence « Armory ») ;
- coffre à apparence : MirageManager.Instance() → PrismBoxLoaded, PrismBoxItemIds (rempli seulement après ouverture du coffre en jeu) ;
- armoire : UIState.Cabinet → IsCabinetLoaded(), IsItemInCabinet(cabinetRowId) (lien objet → ligne de la feuille Cabinet à sonder).
Plan : lecture sur le thread framework (copie), conteneurs « Arsenal », « Coffre à apparence », « Armoire » ajoutés aux entrées de la
Banque (BankEntry/InventorySnapshot, Inventory/BankModels.cs) avec date de relevé ; « possédé » de la Revue de mode et stocks partout
les incluent ; inconnu tant que le coffre / l'armoire n'ont pas été ouverts.
PLAN AVANT PUBLICATION (décidé 2026-11-01, détail : docs/project/BACKLOG.md « Revue des points ouverts ») : 1) FAIT 0.224 (à valider en jeu) : bonus d'Abondance estimé
(règle communautaire citée, modifiable) ; 2) Revue de mode : coffre à apparence + armoire dans « possédé » ; 3) boutiques de compagnie libre
(FccShop) ; 4) icônes de quête seulement sur relevé en jeu ; puis publication D17. Non faits par décision : score de la revue, suppression
d'alarme du jeu, sources inconnues restantes, injection dans l'infobulle native.
0a. FAIT et validé en jeu (2026-11-01) : 0.197–0.223. Dépôt perso : https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/repo.json
0. FAIT et validé en jeu (0.191–0.196 : copie, carnet, prévision, Macro Chain) — PRIORITÉ utilisateur (2026-10-31 18:47) : copier les macros de l'outil de génération de macros (CRAFT-MACRO-001, `Craft/CraftMacroExport.cs`)
   directement dans les macros du jeu, avec réglages : numéro de macro de départ, limite (dernier emplacement utilisable), individuel/partagé ;
   référence fonctionnelle Craftimizer (lecture seule, pas de copie de code). Écriture dans RaptureMacroModule = action sur clic du joueur ;
   à prouver par capture/sonde avant usage.
1. Essai en jeu 0.188 (butin des contenus récents via LuminaSupplemental, sources d'objets).
2. LuminaSupplemental, reste : ItemSupplement (désynthèse, coffres, réduction…) dans « Comment l'obtenir », (Butin des monstres : MobDrop en tête depuis 0.190). Fait en 0.189 : monstres, ALEA, entreprises, boutique dans l'infobulle.
3. Commit (GitHub Desktop). Publication D17 côté auteur.
