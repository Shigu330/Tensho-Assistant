# Journal des versions / Changelog

Résumé par version, tiré des notes de vérification (historique détaillé : commits git ; preuves citées : docs/evidence/).
Chaque version a été compilée et testée par `scripts/Verify.ps1` ; la validation en jeu est indiquée quand l'utilisateur l'a rapportée.

## 0.219.0
- Demande : prévoir avec Verify.ps1 le commit et la release git ; vérifier l'existant et les .md ; puis reprendre la Revue de mode.
- Existant vérifié : `Stable` locale publiée vers `origin/main`, tags `Stable-0.217.0` et `Stable-0.218.0` publiés, branche distante
  `Devloppement` en retard (ancêtre de Stable), workflow CodeQL seul, GitHub CLI absent. Choix de l'utilisateur : commit + tag + push,
  travail sur `Devloppement` puis `main` (retour : la branche de release sur GitHub est `main`, `Stable` n'est qu'un nom local), release via GitHub CLI, dépôt Dalamud personnalisé (repo.json).
- `scripts/Release.ps1` (appelé seulement après un Verify vert) : `Verify.ps1 -Commit` (sur Devloppement : commit « x.y.z » avec l'entrée
  du CHANGELOG, push) ; `Verify.ps1 -Release` (repo.json, main local remis sur origin/main, fusion Devloppement → main, push main, tag `Stable-x.y.z`,
  `gh release create` avec latest.zip et les notes, mise à jour de Devloppement) ; `-DryRun` affiche les commandes. Garde-fous : branche,
  entrée du CHANGELOG, arbre propre, tag existant, gh absent. Verify affiche aussi « VERSION x.y.z (DLL …) ». Documentation : CONTRIBUTING
  « Versions and releases », README (URL du dépôt personnalisé), CLAUDE.md étape 5. Testé : syntaxe des deux scripts, garde-fou de branche
  (-DryRun). Non testé : un vrai -Commit / -Release (à lancer par l'utilisateur).
- Revue de mode : un bouton Tensho (t-shirt) sur chaque ligne d'emplacement de la fenêtre du jeu (nœud 8 + n, position lue avec les
  catégories) ; clic = ouvre cet emplacement dans le panneau (pièces et teintures), nouveau clic = ferme. Petites fenêtres séparées pour
  laisser le reste de la fenêtre du jeu cliquable.
- Verify.ps1 (agent) : 554/554, 0 avertissement, 0 erreur ; DLL 0.219.0.0. Non validé en jeu.
## 0.218.0
- Retour (capture, clic droit sur la carte › Récolte) : le menu d'un point ouvrait toujours la première ressource. Le clic droit liste
  maintenant chaque ressource du point avec son icône (infobulle complète) ; celle choisie s'ouvre dans l'outil Récolte.
- Verify.ps1 (agent) : 554/554, 0 avertissement, 0 erreur ; DLL 0.218.0.0. Non validé en jeu.
## 0.217.0
- Retour (captures) : ne pas retirer les teintures des emplacements sans catégorie ; mettre les icônes (boutons) sur les cases d'équipement
  de la fenêtre du jeu. Fait : un emplacement teignable (arme à pieds) sans catégorie s'ouvre et montre ses teintures de la semaine.
- Non fait (quota 97 %) : boutons sur les icônes d'emplacement de la fenêtre « Revue de mode » du jeu.
- Verify.ps1 (agent) : 554/554, 0 avertissement, 0 erreur ; DLL 0.217.0.0. Non validé en jeu.
## 0.216.0
- Retour (captures) : interface mal agencée ; chaque emplacement doit être une ligne qui ouvre sa liste au clic, pas tout d'un coup.
- Revue de mode (page et panneau) : un seul emplacement ouvert à la fois (clic sur la ligne : ▶/▼, catégorie, possédés / signalés, ✓) ;
  la liste des pièces et des teintures s'ouvre dans un cadre défilant sous la ligne ; les emplacements sans catégorie cette semaine sont
  seulement listés ; le nombre de signalements est abrégé « ×28 » (texte complet en infobulle) pour ne plus couper les lignes.
- Non fait : boutons posés sur les cases de la fenêtre du jeu (autre option demandée) — à faire à la reprise si souhaité.
- Verify.ps1 (agent) : 554/554, 0 avertissement, 0 erreur ; DLL 0.216.0.0. Non validé en jeu.
## 0.215.0
- Demande : les teintures de la Revue de mode. FashionReport.json (FFXIVGachaSpreadsheet) contient « WeeklyDyes » : semaine (même
  numéro que FashionWeek.Number, 442–457 lus le 2026-11-01) → 6 emplacements (Weapon, Head, Body, Hands, Legs, Feet) → teinture
  (Count, Pct). Lu sans rejeter les données d'équipement si la section est mal formée (`FashionDataset.WeeklyDyes`, `DyesFor`).
- Sous chaque emplacement teignable (arme à pieds) : « Teintures signalées cette semaine » avec pastille de couleur et nom (feuille Stain),
  part en % et nombre de signalements, 10 au plus, triées par part.
- Verify.ps1 (agent) : 554/554, 0 avertissement, 0 erreur ; DLL 0.215.0.0. Non validé en jeu.
## 0.214.0
- Retour (capture) : l'infobulle d'une pièce non possédée était grisée aussi. Cause : l'opacité de la ligne restait active pendant le
  dessin de l'infobulle et du menu. Correction : opacité retirée juste après la ligne, avant l'infobulle et le clic droit.
- Verify.ps1 (agent) : 554/554, 0 avertissement, 0 erreur ; DLL 0.214.0.0. Non validé en jeu.
## 0.213.0
- Retour : 0.212 « c'est ok ». Demande : examiner Avant-Garde et refaire la page Gold Saucer › Revue de mode dans le même esprit.
- Avant-Garde (AGPL-3.0) lu sans copie (docs/project/REFERENCES.md, REF-FASHION-AVANTGARDE).
- Page Revue de mode : section « Cette semaine » en tête — thème de la semaine (feuille FashionCheckWeeklyTheme), prochaine revue, les
  11 emplacements avec la catégorie lue dans la fenêtre du jeu (`FashionCheckReader`, fenêtre « FashionCheck », AtkValue 2 + 11 × n),
  le nombre de pièces possédées / signalées, et par emplacement la liste des pièces : possédées en premier, non possédées grisées,
  tri par popularité (options enregistrées), infobulle complète et clic droit vers les autres outils. L'ancienne liste de toutes les
  catégories reste dessous. Données d'équipement téléchargées automatiquement à l'ouverture de la fenêtre du jeu.
- Panneau « Revue de mode · Tensho » à côté de la fenêtre du jeu quand elle est ouverte (désactivable). Mode développeur : journal des
  11 catégories lues (preuve à relever).
- Test ajouté (semaines, ligne de thème, ordre des emplacements). Verify.ps1 (agent) : 554/554, 0 avertissement, 0 erreur ;
  DLL 0.213.0.0. Non validé en jeu.
## 0.212.0
- Demande : organiser les icônes (Réglages › Carte › Icônes) par catégorie ; retour (capture) : léger décalage des lignes Tensho.
- Catégories repliables avec nombre d'icônes et boutons « Tout afficher / Tout masquer » : Couches Tensho, Quêtes, Objectifs et événements,
  ALEA, Récolte, Boutiques, Drapeau et cible, Lieux et services du jeu, Autres ; dans chacune, tri par priorité ; le filtre ouvre tout.
  Ligne d'icône extraite dans `DrawIconSizeRow`.
- Décalage : le symbole des couches Tensho était plus étroit que les images du jeu ; il est dessiné dans le même carré (hauteur de ligne).
- Verify.ps1 (agent) : 553/553, 0 avertissement, 0 erreur ; DLL 0.212.0.0. Non validé en jeu.
## 0.211.0
- Retour : « nos icônes c'est ok » (0.210) mais il manque des icônes : quêtes de l'épopée, bleues et autres, quêtes terminées.
- Réglages › Carte › Icônes : toutes les icônes de marqueurs de quête du jeu (EventIconType : MapIconAvailable / MapIconInvalid + 1..IconRange,
  sonde `PROBE_EVENTICONS`, docs/evidence/0.211-event-icons.md) quand les quêtes sont affichées, seulement celles dont l'image existe ;
  nom « Quête de l'épopée / annexe / de déblocage (bleue) / Quête (type n) · disponible|non disponible · variante n ». Même visibilité,
  taille et priorité que les autres icônes.
- Verify.ps1 (agent) : 553/553, 0 avertissement, 0 erreur ; DLL 0.211.0.0. Non validé en jeu.
## 0.210.0
- Retour : 0.209 « c'est ok » (usages). L'icône « Coffre au trésor » existe déjà dans le jeu : l'utiliser pour les coffres de la carte
  Tensho ; ajouter aux réglages les icônes créées par Tensho qui y manquent.
- Coffres proches : icône du jeu MapSymbol 63852 « Coffre au trésor » (docs/evidence/0.139.0-symbols-probe.log) au lieu du symbole
  BoxOpen (gardé seulement si la texture manque).
- Réglages › Carte › Icônes : nouvelles lignes pour les couches de Tensho — coffre au trésor, repères d'exploration, courants éthérés,
  points de récolte proches, membres du groupe — avec la même visibilité / taille / priorité (globale ou par carte) que les icônes du jeu
  (clés hors de la plage des icônes du jeu) ; le dessin de chaque couche suit ces réglages. Infobulle « couche ajoutée par Tensho ».
- Verify.ps1 (agent) : 553/553, 0 avertissement, 0 erreur ; DLL 0.210.0.0. Non validé en jeu.
## 0.209.0
- Retour : « usages ne prend pas en compte les outils adaptés pour trouver les références ». L'onglet Usages ajoute, avant les quêtes et
  lèves : « Monnaie » (boutiques du jeu qui acceptent l'objet comme paiement, par PNJ : lien outil Vendeurs + drapeau, objets achetables
  avec leur prix, clic = fiche ; index `NpcVendorCatalog.PaidWith`) ; « Ce qu'il donne » (LuminaSupplemental ItemSupplement lu dans
  l'autre sens : désynthèse, réduction, contenu, coffres, lots de cartes, Eureka, Bozja… avec ✓/✗ de collection) ; « Quêtes qui le
  demandent » (LuminaSupplemental QuestRequiredItem, noms de la feuille Quest, clic = outil Quêtes). Le menu de l'inventaire et le panneau
  d'objet en contexte proposent Usages aussi pour ces cas.
- Test ajouté (lectures LuminaSupplemental : ce qu'un objet donne, quêtes, PNJ des boutiques non reliées).
- Verify.ps1 (agent) : 553/553, 0 avertissement, 0 erreur ; DLL 0.209.0.0. Non validé en jeu.
## 0.208.0
- Demande : le menu du clic droit de l'inventaire doit suivre la logique de l'objet en contexte et n'afficher que les outils utiles.
- `RelevantRoutes(item)` (MainWindow.ItemContextMenu.cs) décide une seule fois, pour le menu « Tensho — objet » et le panneau « Objet en
  contexte » (mêmes outils, même ordre) : usages (lèves, approvisionnement, aquarium, ingrédient), artisanat (recette qui le produit),
  récolte, PNJ vendeurs (+ LuminaSupplemental), marché (objet vendable), butin des contenus, monstres, quête qui le donne, collection, puis
  quantités, fiche et outils. Un catalogue pas encore chargé garde l'outil (inconnu ≠ aucun). Ouverture commune `OpenRoute`.
- Verify.ps1 (agent) : 552/552, 0 avertissement, 0 erreur ; DLL 0.208.0.0. Non validé en jeu.
## 0.207.0
- Retours (captures) : infobulles qui dépassent l'écran ; outils manquants au clic droit dans l'inventaire ; icônes du carnet de craft qui se
  décalent ; butin : ✓/✗ sur les objets de collection et filtres par type ; panneau de mission affiché en mission aléatoire.
- Infobulles : les 80 infobulles des fenêtres passent par `WrappedTooltip` (retour à la ligne à ~32 caractères de police).
- Menu « Tensho — objet » de l'inventaire : + Prix du marché, Butin des contenus (premier contenu qui lâche l'objet, filtré dessus), Monstres
  qui le lâchent, Ouvrir la collection, Comment l'obtenir (fiche).
- Carnet de craft · métier : cause = l'icône dessinée devenait le dernier élément, le bouton suivant se plaçait après elle (+3 px à chaque
  fois) ; un élément invisible à la place du bouton rétablit l'alignement ; bouton « tous » à la même taille (38×38).
- Butin (outil Butin des contenus et panneau de mission) : pastille ✓ verte (obtenu) / ✗ rouge (non obtenu) sur l'icône des objets de
  collection ; filtre par type (montures, mascottes, cartes, rouleaux, équipement, autres) et « Non obtenus seulement ».
- Panneau de mission : plus affiché pour une mission aléatoire ni sans mission choisie.
- Verify.ps1 (agent) : 552/552, 0 avertissement, 0 erreur ; DLL 0.207.0.0. Non validé en jeu.
## 0.206.0
- Demandes : heure PC une seule fois (en-tête, à côté de la version) ; refaire l'accueil selon les outils actuels ; mode développeur avec un
  maximum d'aide pour la communauté open source ; réglages cohérents et onglet « Avancé » ; un maximum de descriptions en infobulles.
- Horloge : les outils (récolte, météo, alarmes) n'affichent plus que l'heure ET (avertissement en infobulle) ; l'en-tête montre l'heure PC
  et l'heure ET, chacune désactivable (Paramètres › Général).
- Accueil (`MainWindow.Home.cs`) : état des catalogues, travaux en cours, prochaine alarme (clic → module), erreurs ; une tuile par module
  (Artisanat, Récolte, Collections, Quêtes, Carte, Marché, Alarmes, Automatisation, Combat, Gold Saucer) avec une ligne vivante ; objet en
  contexte avec ses icônes d'outils ; annuaire des outils (désactivable).
- Paramètres › Avancé (nouveau, `MainWindow.Advanced.cs`) : mode développeur (désormais enregistré), identifiants du jeu, sources
  communautaires activables (LuminaSupplemental, Garland, wiki, prix Universalis — désactivées = aucune requête), délai entre commandes
  /alarm, suppression de toutes les alarmes (deux clics), copie du rapport de diagnostic (sans données de personnage) et de l'adresse du dépôt.
  Les prix au survol passent d'Interface à Avancé.
- Mode développeur › Diagnostics : section « Contribuer au projet » (dépôt, commande de test, licence, documents, fichiers de la page affichée,
  rôle de chaque dossier, sources de données actives, sondes et exemple de commande, traductions et clés).
- Infobulles sur les réglages (Général, Interface, Données, Alarmes, Avancé, observations, Débogage) et sur les outils de développement.
- Verify.ps1 (agent) : 552/552, 0 avertissement, 0 erreur ; DLL 0.206.0.0. Non validé en jeu.
## 0.205.0
- Retour : le jeu a une commande texte d'alarme (`/alerte "Alerte" et rp 2308 60 es00 silencieux`, `/alarm` en anglais). Sonde
  `PROBE_ALARMCMD` : TextCommand 229, « /alarm » dans toutes les langues (alias FR « /alerte »), sous-commandes anglaises acceptées partout
  (docs/evidence/0.205-alarm-command.md).
- Réglage « Créer aussi les alarmes dans le jeu (commande /alarm) » (Alarmes › Réglages) : les alarmes « chaque jour ET » créées sont
  ajoutées à la fenêtre « Alarmes » du jeu (`/alarm "nom" et rp HHmm rappel seNN [mute]`, nom ≤ 20 car. finissant par l'heure ET) ;
  sous-option pour inclure celles avec condition météo (le jeu ne vérifie pas la météo). Bouton « Envoyer au jeu » par groupe.
  Envoi par `UIModule.ProcessChatBoxEntry`, au clic seulement, une commande / 0,5 s, arrêt à 30 alarmes du jeu, commandes `/alarm`
  construites par Tensho uniquement. Créneau : « dans le jeu » (vu dans la liste du jeu) ou « envoyée, absente du jeu ». Le jeu sonne
  lui-même ces alarmes ; Tensho ne sonne plus pour elles. Suppression dans Tensho : rappel de la supprimer aussi dans le jeu (pas de
  suppression automatique : `DeleteAlarm(index)` non prouvé).
- Test ajouté (syntaxe, nom, garde de commande). Verify.ps1 (agent) : 552/552, 0 avertissement, 0 erreur ; DLL 0.205.0.0. Non validé en jeu.
## 0.204.0
- Retour : « ça ne sépare pas les alarmes par métier ». Le module Alarmes est maintenant découpé en sections repliables par métier
  (Mineur, Botaniste, Pêcheur, avec icône et nombre d'alarmes), puis « Autres alarmes (météo, repères, personnelles) » ; dans chaque
  section, les groupes par objet triés par prochaine sonnerie. Le métier n'est plus répété sur chaque groupe.
- Verify.ps1 (agent) : 551/551, 0 avertissement, 0 erreur ; DLL 0.204.0.0. Non validé en jeu.
## 0.203.0
- Demande : les alarmes doivent afficher le métier avec lequel l'objet est récolté. Chaque groupe d'alarmes d'objet affiche l'icône et le
  nom du métier (mineur, botaniste, pêcheur, noms de la feuille ClassJob) ; enregistré à la création (récolte : type du point ; pêche :
  pêcheur), sinon déduit des sources de récolte de l'objet pour les alarmes déjà créées (un seul métier possible, sinon rien).
- Preuve : docs/evidence/0.203-gathering-jobs.md (sonde `PROBE_GTYPE`). Test `JobOf` ajouté au cas des fenêtres d'alarmes.
- Verify.ps1 (agent) : 551/551, 0 avertissement, 0 erreur ; DLL 0.203.0.0. Non validé en jeu.
## 0.202.0
- Demande : alarmes dans un module à part, réglage pour utiliser les alarmes du jeu, affichage regroupé par objet avec icône et infobulle.
- Module « Alarmes (n) » dans la navigation (page dédiée) : réglages repliables, alarmes groupées par objet (icône + infobulle « Comment
  l'obtenir »), ou par météo (icône de la météo) / repère / nom ; par groupe : prochaine sonnerie, actives, rappel, tout supprimer, ouvrir
  l'objet ou la météo ; créneaux dépliables (activer, rappel, supprimer chacun). Groupes triés par prochaine sonnerie. La page Météo
  renvoie au module.
- Réglage « Créer aussi les alarmes dans le jeu » : visible mais désactivé, avec la raison (aucune fonction de création dans l'API ;
  champs natifs Timestamp / sauvegarde non prouvés). Bouton « Copier le relevé des alarmes du jeu » (index, horloge, heure, rappel, son,
  horodatage, drapeaux) pour prouver ces champs sur une vraie alarme avant d'écrire quoi que ce soit.
- Verify.ps1 (agent) : 551/551, 0 avertissement, 0 erreur ; DLL 0.202.0.0. Non validé en jeu.
## 0.201.0
- Demande : terminer la météo, un système d'alarmes relié aux alarmes du jeu, appliqué partout où la météo compte ; alarmes répétables
  chaque jour ET, délai du rappel au choix, tous les créneaux de l'objet sélectionné ; à jour dans la récolte et partout où il y a de la météo.
- Météo : choix de la zone sur la page (toutes les zones avec table météo), 12 périodes, recherche « Quand aura lieu une météo ? »
  (8 prochaines occurrences sur ~3 jours réels), cloche par période et « alarme à chaque <météo> » (périodes 00/08/16 ET avec condition).
- Alarmes Tensho (`Alarms/TenshoAlarm.cs`, testé) : une fois ou chaque jour ET (70 min réelles), rappel N min avant (par alarme, modifiable),
  conditions météo et météo précédente (prévision inconnue = pas de correspondance), 60 au plus, alarmes uniques passées retirées.
  `AlarmService` (thread framework, 1 s) : message dans le chat + son des alarmes du jeu (`AgentAlarm.PlayAlarmSoundEffect`), muet en mission
  au choix ; lit les alarmes du jeu (lecture) et ouvre leur fenêtre. Créer une alarme dans le jeu : aucune API (preuve :
  docs/evidence/0.201-agent-alarm.md).
- Cloche partout où l'heure ou la météo compte : récolte (tous les créneaux ET du point), pêche (tous les créneaux du poisson avec sa météo,
  ou 00/08/16 ET si seule la météo compte ; chaque créneau calculé), repères d'exploration (clic droit sur la carte : créneau ET + météo du
  wiki ; infobulle : prochaine fenêtre). Paramètres › Modules › Alarmes : rappel par défaut, son (+ écouter), chat, muet en mission.
- Verify.ps1 (agent) : 551/551 (3 nouveaux tests d'alarmes), 0 avertissement, 0 erreur ; DLL 0.201.0.0. Non validé en jeu.
## 0.200.0
- Retour (capture « Objet en contexte » : 4 icônes) : ajouter les icônes manquantes des outils appropriés. Toujours : artisanat, récolte,
  usages de l'objet (nouveau), marché, monstres. Selon l'objet, quand les données le relient : PNJ vendeurs (catalogue ou LuminaSupplemental
  ENpcShop), butin des contenus (premier contenu LuminaSupplemental ou Garland, ouvert filtré sur l'objet), quête (récompense), collection
  ou Triple Triad (carte sélectionnée). Les icônes passent à la ligne quand la colonne est étroite.
- Verify.ps1 (agent) : 548/548, 0 avertissement, 0 erreur ; DLL 0.200.0.0. Non validé en jeu.
## 0.199.0
- Retour (captures adamankhélone, adenium insulaire) : dans le menu du clic droit, « Échange » renvoyait sur la monnaie (PGS) alors qu'un
  vendeur est connu ; sans vendeur, rien n'indiquait que le PNJ manque. Correction : la ligne d'échange ouvre le PNJ qui vend l'objet contre
  cette monnaie (outil Vendeurs, + drapeau), la monnaie devient un sous-lien ; sans PNJ connu : « PNJ inconnu ; ouvre la monnaie ».
- LuminaSupplemental (demande « oublie pas LuminaSupplemental ») : ENpcShop (boutiques que les feuilles du jeu ne relient à aucun PNJ) et
  ENpcPlace (position) — les ventes de ces boutiques donnent un lien PNJ avec drapeau. Nouvel index `NpcVendorCatalog.SalesOf`.
- Preuve : docs/evidence/0.199-adenium-shop.md (sonde `PROBE_SELLS` : boutique 1770734 sans PNJ dans le jeu, Garland et LuminaSupplemental).
- Verify.ps1 (agent) : 548/548, 0 avertissement, 0 erreur ; DLL 0.199.0.0. Non validé en jeu.
## 0.198.0
- Retour : l'infobulle (« Comment l'obtenir ») n'est pas appliquée sur toutes les icônes de carte. Cause : seules la liste des cartes manquantes
  et les lots de cartes l'utilisaient ; ailleurs, simple nom ou rien. Correction : `TriadCardTooltip` (fiche de l'objet de la carte si elle
  en a un, sinon nom) sur toutes les icônes : collection Triple Triad, liste et détail du Triple Triad, deck en cours, decks enregistrés,
  deck généré, PNJ (cartes manquantes), lots de cartes.
- Verify.ps1 (agent) : 548/548, 0 avertissement, 0 erreur ; DLL 0.198.0.0. Non validé en jeu.
## 0.197.0
- GOAL COLL-004 (suite) : dernière source LuminaSupplemental, `ItemSupplement.csv` (objet obtenu depuis un autre objet). « Comment l'obtenir »
  (infobulle) affiche, avant Garland : désynthèse, réduction éthérée, contenu d'un objet, jardinage, bâtisseurs d'Ishgard, lots de cartes,
  coffres, Palais des morts, Pilier des Cieux, Eureka Orthos/Anemos/Pagos/Pyros/Hydatos, Bozja, logogrammes, Traversée des pèlerins, Oizys,
  Auxesia ; 3 objets sources nommés + « (+n autres) ». Clic droit : jusqu'à 4 objets sources par type, qui ouvrent la fiche de l'objet source.
- Source : LuminaSupplemental (Critical-Impact, GPL-3.0), colonnes ItemId, SourceItemId, ItemSupplementSource ; types 1–19 de l'énumération
  `ItemSupplementSource`. Fichier en cache comme les autres (limite 4 Mo, fichier ≈ 2,7 Mo).
- Verify.ps1 (agent) : 548/548, 0 avertissement, 0 erreur ; DLL 0.197.0.0. Non validé en jeu.
## 0.196.0
- Retour : le système de macros ne prend pas en charge Macro Chain.
- Option « Chaîner les pages » (Paramètres › Modules › Export, activée par défaut) : chaque page laisse une ligne et se termine par
  « /tenshonext » (marqueur de la Macro Chain de Tensho, `MacroDefinition.Marker`) ; export du simulateur et du carnet d'artisan.
- Après une copie dans les macros du jeu, les emplacements écrits deviennent le plan de la Macro Chain (outil Automatisation), et un bouton
  « Lancer la chaîne de macros » apparaît sous le bouton de copie : les pages s'exécutent l'une après l'autre.
- Verify.ps1 (agent) : 548/548, 0 avertissement, 0 erreur ; DLL 0.196.0.0. VALIDATED_IN_GAME : « macro chain fonctionne » (utilisateur, 2026-10-31).
## 0.195.0
- Retour (capture du panneau du carnet de synthèse) : bouton et prévision coupés à droite. Dans le panneau étroit : libellé court « Copier dans
  les macros du jeu », plage (livre, premier–dernier) dans l'infobulle ; ligne de prévision qui revient à la ligne au lieu de déborder.
- Verify.ps1 (agent) : 548/548, 0 avertissement, 0 erreur ; DLL 0.195.0.0. VALIDATED_IN_GAME (utilisateur, 2026-10-31).
## 0.194.0
- Retour (capture « Macros écrites dans les emplacements 50 à 51. ») : prédire les emplacements utilisés avant la copie, pas seulement le
  signaler après.
- Sous le bouton (export et carnet d'artisan) : « Utilisera les emplacements 50 à 51 (2 macro(s) : 50, 51). » ou « Pas assez d'emplacements
  libres… : rien ne sera écrit », calculé avec la même règle que la copie (`MacroSlotPlan`) sur l'état des 100 emplacements de chaque livre,
  copié une fois par seconde sur le thread framework (rien n'est lu pendant le dessin). Le message du chat reste la confirmation finale.
- Verify.ps1 (agent) : 548/548, 0 avertissement, 0 erreur ; DLL 0.194.0.0. VALIDATED_IN_GAME (prévision affichée, capture utilisateur 2026-10-31).
## 0.193.0
- Demande : notification ou message dans le chat pour prévenir du statut de la copie des macros. Message « [Tensho] Macros écrites dans les
  emplacements a à b » dans le chat du jeu, ou en erreur (pas assez de place, plage invalide, échec), après chaque copie (export et carnet).
  Nouveau service injecté : `IChatGui`.
- Verify.ps1 (agent) : 548/548, 0 avertissement, 0 erreur ; DLL 0.193.0.0. VALIDATED_IN_GAME : capture du message (utilisateur, 2026-10-31).
## 0.192.0
- Demande : même bouton dans le panneau lié au carnet d'artisan, à côté de « Copier bloc ». Bouton partagé (`DrawMacroToGame`) dans l'export
  de macros et dans le panneau du carnet ; même plage et mêmes règles (Paramètres › Modules).
- Verify.ps1 (agent) : 548/548, 0 avertissement, 0 erreur ; DLL 0.192.0.0. Non validé en jeu.
## 0.191.0
- Priorité utilisateur : copier les macros de l'outil de génération de macros dans les macros du jeu, avec des réglages pour indiquer où ça
  commence et où est la limite, comme Craftimizer.
- Bouton « Copier dans les macros du jeu (individuelles 80–89) » dans l'export de macros : pages écrites sur le thread framework au clic, via
  FFXIVClientStructs (RaptureMacroModule.ReplaceMacroLines, nom, icône par défaut si aucune, SetSavePendingFlag, puis
  RaptureHotbarModule.ReloadMacroSlots) — même suite d'appels que Craftimizer, référence fonctionnelle sans copie de code.
- Sécurité (`MacroSlotPlan`, testé) : seulement entre le premier et le dernier emplacement réglés ; par défaut seulement dans les emplacements
  vides ou déjà nommés par Tensho (« Tensho 1/3 »…) ; toutes les pages ou aucune ; écrasement des autres macros seulement si l'option est cochée.
- Paramètres › Modules › Export : macros individuelles ou partagées, premier emplacement (0–99), dernier emplacement (limite), nom des macros,
  autorisation d'écraser. Liens du dépôt mis à jour vers Shigu330/Tensho-Assistant (manifeste, CLAUDE.md, publication D17).
- Test : 1 groupe `MacroSlotPlanTests`. Verify.ps1 (agent) : 548/548, 0 avertissement, 0 erreur ; DLL 0.191.0.0. VALIDATED_IN_GAME : « ça fonctionne » (utilisateur, 2026-10-31).
## 0.190.0
- Retour (capture Butin des monstres, Épaule de rroneek : wiki puis Garland, LuminaSupplemental absent) : section « Source principale ·
  LuminaSupplemental » en tête de l'outil, monstres qui lâchent l'objet (MobDrop), lieux toujours par le wiki et Garland en dessous.
- Verify.ps1 (agent) : 547/547, 0 avertissement, 0 erreur ; DLL 0.190.0.0. VALIDATED_IN_GAME : « 188-190 ok » (utilisateur, 2026-10-31). Arrêt quota (5 h à 95 %).
## 0.189.0
- Suite LuminaSupplemental (« continue dans la mesure du quota ») : « Comment l'obtenir » montre aussi, avant Garland et le wiki, les monstres
  qui lâchent l'objet (MobDrop, noms de la feuille BNpcName), les ALEA (FateItem, noms de la feuille Fate), les entreprises de servant
  (RetainerVentureItem) et la boutique en ligne avec son prix (StoreItem). Noms résolus à la construction des données, hors du dessin.
  Reste : ItemSupplement (désynthèse, réduction, coffres, lots de cartes, donjons profonds, Eureka, Bozja) et l'outil Butin des monstres.
- Verify.ps1 (agent) : 547/547, 0 avertissement, 0 erreur ; DLL 0.189.0.0. VALIDATED_IN_GAME : « 188-190 ok » (utilisateur, 2026-10-31).
## 0.188.0
- Demande : ajouter LuminaSupplemental et, si cette source offre plus que le wiki, Garland ou d'autres, l'utiliser en premier, pour les
  autres outils aussi.
- Source : LuminaSupplemental (Critical-Impact, GPL-3.0, CSV « Generated » sur GitHub, clés = identifiants du jeu) ; couverture relevée le
  2026-10-31 : 283 contenus avec coffres de boss, 1 373 coffres de zone avec taux, butin direct des boss. Téléchargée une fois au premier
  usage (4 Mo max par fichier), gardée dans le cache disque « lumina-supplemental » pour la durée « Sources ».
- Butin des contenus (outil et panneau de l'outil de mission) : coffres de boss, butin direct et coffres de zone de LuminaSupplemental en
  premier ; récompenses de Garland gardées ; coffres de Garland seulement pour les catégories absentes de LuminaSupplemental.
- Sources d'objets : « Butin de contenu : … (LuminaSupplemental) » dans « Comment l'obtenir » avant Garland et le wiki, et lien du clic droit
  vers ce contenu. Prochaine étape : monstres (MobDrop), ALEA (FateItem), entreprises (RetainerVentureItem), boutique en ligne (StoreItem),
  désynthèse/coffres/réduction (ItemSupplement).
- Test : 1 groupe `LuminaSupplementalTests` (premières lignes réelles, Sastasha). Verify.ps1 (agent) : 547/547, 0 avertissement, 0 erreur ;
  DLL 0.188.0.0. VALIDATED_IN_GAME : « 188-190 ok » (utilisateur, 2026-10-31).
## 0.187.0
- Retour : coffres de combat et coffres de zone semblent vides.
- Vérification : l'analyseur lit bien les coffres quand Garland les donne (test sur le document réel d'Alexander - Le Fardeau du Père sadique :
  19 objets dans le coffre du boss ; Sastasha : 3 boss et des coffres de zone). Mais pour les contenus récents (Ihuykatumu, Cénote de
  Skydeep, Everkeep…) Garland n'a ni coffre de boss ni coffre de zone, seulement les récompenses. Les feuilles du jeu ne contiennent pas ces
  listes (Treasure = modèles de coffre, InstanceContentRewardItem sans objets) : le contenu des coffres est décidé par le serveur.
- Correction : nombre d'objets dans chaque filtre du panneau, et message clair quand la source n'a aucun coffre pour ce contenu.
  Piste : Duty Loot Preview utilise LuminaSupplemental (Critical-Impact) — boss, coffres de boss, butin direct, coffres de zone par contenu.
- Test : document Garland réel (A4S). Verify.ps1 (agent) : 546/546, 0 avertissement, 0 erreur ; DLL 0.187.0.0. Non validé en jeu.
## 0.186.0
- Retour (capture, Alexander - Le Fardeau du Père sadique : contenu bien identifié depuis l'outil de mission) : interface mal découpée, aucun
  lien avec les autres outils.
- Panneau de l'outil de mission compact : en-tête sur deux lignes, date de récupération et bouton d'actualisation sur une ligne, texte de source
  et limites dans l'infobulle (i), filtre en liste déroulante à côté de la recherche, objets regroupés par origine (récompenses, chaque boss
  avec son nom, coffres) dans des sections repliables, « ✓ » quand la collection de l'objet est obtenue.
- Liens : survol d'un objet = infobulle complète (collection, stock, comment l'obtenir) ; clic droit = fiche de l'objet, prix du marché,
  recettes et usages, collection, puis ses sources (vendeur, succès, quête, contenu…). Même survol et même clic droit dans l'outil Butin des
  contenus.
- Verify.ps1 (agent) : 545/545, 0 avertissement, 0 erreur ; DLL 0.186.0.0. Non validé en jeu.
## 0.185.0
- Demande : butin des contenus lié à l'outil de mission du jeu — sélectionner un donjon, raid ou autre affiche une interface avec le butin
  (équipement, récompenses, monnaies), en prenant Duty Loot Preview comme référence et en l'améliorant.
- Lecture : `AgentContentsFinder.SelectedDuty` (FFXIVClientStructs : type Aucun/Mission aléatoire/Normal + identifiant) pendant que la fenêtre
  « ContentsFinder » est visible, 5 fois par seconde ; l'identifiant normal est pris comme ContentFinderCondition (catalogue des contenus) ;
  le panneau affiche le nom du contenu pour qu'un écart se voie ; en mode développeur, chaque sélection est notée dans dalamud.log (preuve).
- Panneau « Tensho — butin du contenu » à droite de l'outil de mission : même vue que l'outil Butin des contenus (récompenses, boss, coffres,
  recherche et filtres, données Garland Tools en cache), demande faite une fois par contenu sélectionné ; mission aléatoire signalée.
  Réglage dans Paramètres › Données (activé par défaut).
- Verify.ps1 (agent) : 545/545, 0 avertissement, 0 erreur ; DLL 0.185.0.0. VALIDATED_IN_GAME (sélection) : capture utilisateur 2026-10-31, contenu sélectionné correctement identifié.
## 0.184.0
- Retour (capture, Sage niv. 71) : confirme le niveau absent de la table ; Détermination et PM non calculés.
- Correction : modificateur principal pour les niveaux 1 à 100, repris comme données de Character Panel Refined (Kouzukii, licence MIT,
  LevelModifiers.cs, plugin de référence de la feuille de route) ; ses colonnes secondaire/diviseur concordent avec la feuille du jeu ParamGrow
  aux niveaux 50/70/80/90/100, et ses valeurs principales avec les 6 déjà connues. Niveau 71 : 296.
- Test : table niveaux 1 à 100. Verify.ps1 (agent) : 545/545, 0 avertissement, 0 erreur ; DLL 0.184.0.0. VALIDATED_IN_GAME : « c'est ok » (utilisateur, 2026-10-31, stats réelles 0.182–0.184).
## 0.183.0
- Retour : pas de PM pour un soigneur. Cause probable : niveau absent de la table communautaire du modificateur principal (connue seulement aux
  niveaux 50, 60, 70, 80, 90, 100 ; absent des données du jeu), donc régénération de PM et Détermination non calculées, sans message dans le
  panneau. Le panneau le signale maintenant. Table complète non récupérée (page AkhMorning sans tableau lisible) : pas de valeur inventée.
- Verify.ps1 (agent) : 545/545, 0 avertissement, 0 erreur ; DLL 0.183.0.0. Non validé en jeu.
## 0.182.0
- Demandes : clic droit « Contenu » → outil Butin des contenus ; Combat : stats réelles en %, selon le métier équipé, seulement les stats
  utiles, dans une fenêtre à côté des stats du jeu sans doublon, avec infobulles (formule et description) ; données du jeu d'abord.
- Clic droit (sources) : un contenu cité par Garland (identifiant InstanceContent) ouvre ce contenu dans Butin des contenus (prévu en 0.181,
  livré ici).
- Stats réelles (`Character/StatFormulas.cs`) : modificateurs de niveau secondaire et diviseur lus dans la feuille du jeu ParamGrow (niv. 100 :
  420 / 2780, [docs/evidence/0.182.0-paramgrow-probe.log](docs/evidence/0.182.0-paramgrow-probe.log)), catégorie/rôle du métier depuis
  ClassJob ; modificateur principal (440 au niv. 100) et formules depuis la communauté (Allagan Studies), absents des données du jeu.
  Combat : taux et dégâts critiques, coup direct, Détermination, temps de recharge global et bonus de vitesse (Vivacité ou Incantation selon
  la catégorie), Ténacité (tanks), régénération de PM (soigneurs), réduction des dégâts. Récolte/artisanat : leurs trois stats seulement.
- Panneau « Tensho — stats réelles » à droite de la fenêtre Personnage du jeu (seulement les valeurs réelles, pas les brutes déjà affichées),
  survol = description, formule et modificateurs ; réglage dans Paramètres › Données (activé par défaut).
- Test : 1 groupe `StatFormulaTests`. Verify.ps1 (agent) : 545/545, 0 avertissement, 0 erreur ; DLL 0.182.0.0. Non validé en jeu.
## 0.180.0
- Retour (captures) : les liens du clic droit marchent pour un PNJ vendeur (adamankhélone) ; Aeturna (succès connu par le wiki) et
  adenium insulaire (échange Garland contre 200 Certificats végétaux) n'avaient aucune source cliquable.
- Preuve ([docs/evidence/0.180.0-achievement-probe.log](docs/evidence/0.180.0-achievement-probe.log)) : le succès 3185 « Vintage Vogue »
  (« Vêtements vertueux ») existe mais sa récompense (Achievement.Item) vaut 0 : le jeu donne la monture autrement ; seul le nom anglais le relie.
- Correction : nom anglais de chaque succès copié avec le nom français ; une ligne wiki « Achievement » qui nomme un succès s'affiche en
  français (« Succès : Vêtements vertueux ») et devient un lien du clic droit ; l'échange Garland devient un lien vers la monnaie (ses propres
  sources). Copie des collections antérieure relue une fois (états des titres/succès gardés).
- Verify.ps1 (agent) : 544/544, 0 avertissement, 0 erreur ; DLL 0.180.0.0. Non validé en jeu.
## 0.179.0
- Retour (captures Aeturna) : l'infobulle affichait « Succès : achievement table header » et « achievement table row Vintage Vogue » ; le menu
  du clic droit disait « aucune source connue » alors que l'infobulle en montrait une.
- Correction : modèles wiki « achievement table header » (ignoré) et « achievement table row » (nom du succès) → « Succès : Vintage Vogue » ;
  cache « wiki-items » v5. Menu : « Aucune source cliquable dans les données du jeu » + rappel que les sources wiki/Garland sont dans
  l'infobulle, sans lien.
- Test : ligne de tableau de succès. Verify.ps1 (agent) : 544/544, 0 avertissement, 0 erreur ; DLL 0.179.0.0. Non validé en jeu.
## 0.178.0
- Demande : la collection n'est pas cliquable pour faire le lien avec le PNJ vendeur, le succès lié et le reste.
- Clic droit sur une monture, une mascotte, une carte Triple Triade ou un morceau d'orchestrion (Collections), et sur un objet dans les
  listes d'outils : menu de liens vers les sources de l'objet qui le débloque — succès (onglet Succès filtré), PNJ vendeur (outil PNJ
  vendeurs centré sur lui) et « drapeau sur la carte », quête (fiche de la quête), artisanat (recette), récolte. Indication « Clic droit :
  liens vers les sources » dans l'infobulle des collections. Mêmes données que l'infobulle « Comment l'obtenir ».
- Verify.ps1 (agent) : 544/544, 0 avertissement, 0 erreur ; DLL 0.178.0.0. Non validé en jeu.
## 0.177.0
- COLL-004 (sources inconnues, reprise) : capture utilisateur d'Alsvid — la source wiki (campagne Twitch) était déjà affichée, suivie de
  4 lignes parasites (« vendor list y », « contained in », « {{quest list||…}} », « deployed from Y »).
- Cause : ces modèles du wiki génèrent des listes à l'affichage ; leur texte source ne contient aucune acquisition.
- Correction : lignes de générateurs de listes ignorées (vendor/quest/item/recipe/drop… list, contained in, deployed from, fonctions {{#…}},
  PAGENAME) ; cache « wiki-items » passé en v4 (pages relues).
- Test : page brute d'Alsvid Whistle → seule la ligne de la campagne. Verify.ps1 (agent) : 544/544, 0 avertissement, 0 erreur ;
  DLL 0.177.0.0. Non validé en jeu.
## 0.176.0
- Retour (captures) : le second Échangeur d'Auxesia est Kaede « Fouilles archéologiques » — menu « Obtenir des batteries portatives » →
  « Échange de devises », Batterie portative (Auxesia) pour 200.
- Preuve ([docs/evidence/0.176.0-kaede-talk-probe.log](docs/evidence/0.176.0-kaede-talk-probe.log)) : Kaede n'a qu'un dialogue scripté
  (CustomTalk 721862 « CtsWksTreasure_00966 ») sans aucun argument ; la boutique 1771038 (sans nom) est la seule à vendre cet objet.
- Correction : lien observé en jeu ajouté (`NpcShopLinks.ObservedTalkShops` : 721862 → 1771038), règles de copie 11 (le cache des
  marchands est reconstruit). L'autre boutique de batteries (1771006) n'est pas reliée tant qu'elle n'est pas observée.
- Verify.ps1 (agent) : 544/544, 0 avertissement, 0 erreur ; DLL 0.176.0.0. VALIDATED_IN_GAME : « ok » (utilisateur, 2026-10-31).
## 0.175.0
- Retour (Auxesia) : toujours aucun marchand sur la carte malgré la 0.171.
- Preuve ([docs/evidence/0.175.0-auxesia-maps-probe.log](docs/evidence/0.175.0-auxesia-maps-probe.log)) : le catalogue des marchands (cache disque) contient bien Mesouaidonque et Godgyth sur la carte 1246 ; mais Auxesia a 22 cartes (c1w4/01–22) pour le même territoire 1319, qui changent avec la progression de l'exploration cosmique, toutes à la même échelle et au même décalage. Le joueur voit une autre variante que la carte par défaut.
- Correction : `MapCatalog.Equivalent` — une carte du même territoire, même échelle et même décalage montre le même terrain ; marchands et listes du clic droit (quêtes, exploration, courants…) des cartes équivalentes sont affichés.
- Test : 1 groupe (variantes d'Auxesia, sous-sol et autre zone exclus). Verify.ps1 (agent) : 544/544, 0 avertissement, 0 erreur ; DLL 0.175.0.0. VALIDATED_IN_GAME : capture utilisateur 2026-10-31 — Mesouaidonque sous l'Échangeur, Godgyth sous l'Échoppe (Auxesia). Un second Échangeur reste sans marchand (PNJ dont la boutique passe par un script, non relié).
## 0.174.0
- Preuve ([docs/evidence/0.174.0-triad-seldeck-capture.log](docs/evidence/0.174.0-triad-seldeck-capture.log), Kikimo, 17:41) : dans « Choix
  du jeu », les icônes 088NNN sont lues et les 3 rangées reconnues avec les bonnes cartes, mais « challenge kept: no » : entre la fermeture
  de la fenêtre de défi et l'ouverture du choix du deck, aucune des deux n'est affichée pendant quelques images, et le PNJ était oublié.
- Correction : le défi (PNJ, règles, decks) est gardé 30 s après la fermeture de sa fenêtre et rendu au choix du deck ; les % et le cadre du
  meilleur deck peuvent s'afficher.
- Verify.ps1 (agent) : 543/543, 0 avertissement, 0 erreur ; DLL 0.174.0.0. Non validé en jeu.
## 0.173.0
- Retour (capture « Choix du jeu ») : toujours pas de % dans cette fenêtre. dalamud.log ne contient aucune capture de `TripleTriadSelDeck`
  (mode développeur désactivé) : pas de preuve, donc pas de nouvelle supposition.
- Capture renforcée en mode développeur : toutes les textures visibles de la fenêtre (pas seulement les icônes de cartes), ses valeurs, et une
  ligne « Triad deck choice » : rangées trouvées, cartes de chaque rangée, PNJ du défi gardé ou non. Correction suivante sur preuve.
- Verify.ps1 (agent) : 543/543, 0 avertissement, 0 erreur ; DLL 0.173.0.0. Non validé en jeu.
## 0.172.0
- Retour (Kikimo, Chaos + Échange + Tueur d'as) : deck généré 30 % de victoires contre 35 % pour un deck existant.
- Causes : le générateur partait de zéro sans les decks existants, comparait les decks sur 60 matchs (trop bruité avec Chaos) et donnait
  son % final sur d'autres tirages que ceux des decks listés.
- Correction : les decks existants (jeu + Tensho) sont des points de départ et un plancher ; essais sur 150 matchs ; % final calculé sur les
  mêmes tirages que la liste ; le deck généré n'est jamais moins bon que le meilleur deck existant (sinon c'est lui qui est proposé). 8 s.
  Limite : la règle Échange (une carte échangée avant le match) n'est pas simulée.
- Test : plancher du deck existant (groupe du générateur). Verify.ps1 (agent) : 543/543, 0 avertissement, 0 erreur ; DLL 0.172.0.0.
  Non validé en jeu.
## 0.171.0
- Retour (capture, Auxesia, exploration cosmique) : la carte ne semble pas voir les PNJ ; l'infobulle « Échangeur » ne listait personne.
- Preuve ([docs/evidence/0.171.0-cosmic-vendors-probe.log](docs/evidence/0.171.0-cosmic-vendors-probe.log)) : Mesouaidonque (échange de
  crédits, 3 SpecialShop) est bien placé sur la carte 1246 (Level), à 2,3 px d'une Échoppe et 3,7 px de l'Échangeur ; Godgyth (GilShop) est
  dans la disposition de la zone. Le marchand était rattaché à l'icône la plus proche (Échoppe).
- Correction : un marchand rejoint d'abord l'icône de son type à portée (Échangeur s'il n'a que des échanges, Échoppe sinon), puis la plus
  proche ; même règle pour l'icône de l'onglet Vendeurs. La région « ??? » est le nom donné par le jeu lui-même (PlaceName 2405).
- Test : cas Auxesia ajouté (groupe MapVendorIcons). Verify.ps1 (agent) : 543/543, 0 avertissement, 0 erreur ; DLL 0.171.0.0. Non validé en jeu.
## 0.170.0
- Retour 0.169 (capture) : PNJ de Triple Triade « lieu inconnu » (Aurifort l'As de trèfle, Jonas le Valet de pique, Nell la Boit-sans-soif…).
- Cause : ces PNJ ne sont pas dans la feuille Level ; ils sont placés par les fichiers de disposition des zones. Le catalogue lit maintenant
  aussi planevent/planner (en-tête LGB vérifié, comme les marchands PNJ : docs/evidence/0.98.0-lgb-probe.log) pour les PNJ sans position,
  hors du thread framework, une fois au chargement.
- Verify.ps1 (agent) : 543/543, 0 avertissement, 0 erreur ; DLL 0.170.0.0. Non validé en jeu.
## 0.169.0
- Retour 0.168 : « ImGui assertion failed : inner_window == g.CurrentWindow » (imgui_tables.cpp:1219), pile dalamud.log 17:27:37 :
  `MainWindow.Draw` ligne 226 (fin de la table de mise en page).
- Cause : la zone défilante des cartes manquantes (onglet PNJ et cartes) n'était fermée (`EndChild`) que si elle était visible ; ImGui
  exige `EndChild` après chaque `BeginChild`. Même défaut latent dans la colonne d'outils de la carte Tensho. Les deux sont corrigés.
- Verify.ps1 (agent) : 543/543, 0 avertissement, 0 erreur ; DLL 0.169.0.0. Non validé en jeu.
## 0.168.0
- Demandes : trouver les PNJ qui manquent à affronter et les cartes manquantes avec où les trouver ; Triple Triade seulement dans Gold
  Saucer (pas de retour depuis Collections) ; quel lot de cartes acheter en priorité pour débloquer le plus de cartes.
- Preuves ([docs/evidence/0.168.0-triad-sources-probe.log](docs/evidence/0.168.0-triad-sources-probe.log)) : TripleTriadResident = mêmes ids que
  TripleTriad (146) ; objet carte = ItemAction 3357, Data[0] = n° de carte (« Carte dodo » → 1) ; source de carte : AcquisitionType 6 = PNJ
  (+ position), 8 = lot de cartes (objet). PNJ battus : `UIState.IsTripleTriadNpcBeaten` (FFXIVClientStructs) avec l'id TripleTriad, lu
  toutes les 5 s — à confirmer en jeu.
- Triple Triade › onglet « PNJ et cartes » : PNJ à affronter (non battus ou avec des cartes qui manquent ; lieu, coordonnées, clic =
  drapeau ; mise et cartes possibles au survol) ; lots de cartes classés par nombre de cartes manquantes (survol = où acheter) ; cartes
  manquantes avec leurs PNJ et leur lot, survol de l'icône = « Comment l'obtenir » de l'objet carte. Catalogue construit hors du thread
  framework depuis les feuilles.
- Collections : le bouton qui sautait vers Triple Triade est retiré ; Triple Triade reste dans Gold Saucer.
- Test : 1 groupe `TriadNpcCatalogTests`. Verify.ps1 (agent) : 543/543, 0 avertissement, 0 erreur ; DLL 0.168.0.0. Non validé en jeu.
## 0.167.0
- Demande : même principe que titres/succès pour les sacs — au lieu de l'interface d'import, remplir le cache à l'ouverture, signaler dans
  l'outil si une première ouverture est requise, pareil pour tout ce qui en a besoin, réglage dans Paramètres.
- Servants et sacoches de chocobo (normale et supplémentaire) : copiés automatiquement dans le cache à chaque ouverture dans le jeu, puis
  toutes les 30 s tant que la fenêtre reste ouverte (mêmes contrôles qu'avant : inventaire chargé, bon propriétaire, lecture complète, sinon
  l'ancien cache est gardé). Le panneau d'import à côté des fenêtres du jeu n'apparaît plus que si l'option est désactivée.
- Outil Objets › rangements : messages « jamais ouvert » — sacoche, et chaque servant par son nom (liste `RetainerManager` connue après
  l'ouverture de la sonnette), sinon « ouvre chaque servant une fois ».
- Paramètres › Données : « Enregistrer automatiquement les servants et sacoches à leur ouverture » (activé par défaut). Titres et succès
  (0.166) : relus automatiquement dès que le jeu a envoyé leur liste, réglage « Titres et succès » au même endroit.
- Test : 1 groupe (option par défaut, ancienne configuration, choix gardé). Verify.ps1 (agent) : 542/542, 0 avertissement, 0 erreur ;
  DLL 0.167.0.0. Non validé en jeu.
## 0.166.0
- Demandes : titres et succès sans cache ou pas assez longtemps, réglage à ajouter ; % du PNJ visibles mais pas dans « Choix du jeu ».
- Titres et succès : le jeu n'envoie ces listes qu'après ouverture de leur fenêtre, donc une relecture remettait les états en « inconnu ».
  `CompletionMemory` : obtenu reste obtenu ; « non obtenu » gardé tant que la dernière lecture complète a moins que le nouveau réglage
  Paramètres › Données › « Titres et succès (états conservés) » (7 jours par défaut) ; mémoire gardée dans le cache disque (dates de lecture
  complète), même quand la copie des collections est périmée.
- Choix du jeu : la fenêtre de défi reste ouverte dessous, et la 0.165 effaçait alors les rangées lues à chaque passage ; elles sont lues dans
  les deux cas. Icônes de carte petites (087NNN) acceptées en plus des grandes (088NNN). Capture de la fenêtre en mode développeur si besoin.
- Test : 1 groupe `CompletionMemoryTests`. Verify.ps1 (agent) : 541/541, 0 avertissement, 0 erreur ; DLL 0.166.0.0. Non validé en jeu.
## 0.165.0
- Demande : % de victoire dans la fenêtre « Choix du jeu », comme TriadBuddy.
- Fenêtre `TripleTriadSelDeck` (nom présent dans ffxiv_dx11.exe) : rangées de decks reconnues par les icônes de cartes (088NNN = carte NNN,
  preuve 0.163), associées aux decks enregistrés par leurs cartes ; « xx % victoire · yy % nul » écrit à droite de chaque rangée, meilleur
  deck encadré en vert. Le PNJ et les règles du défi sont gardés pendant le choix du deck. Lecture seule.
- Verify.ps1 (agent) : 540/540, 0 avertissement, 0 erreur ; DLL 0.165.0.0. Non validé en jeu.
## 0.164.0
- Demande : faire comme TriadBuddy (surbrillance des bonnes cartes dans la liste du jeu) et améliorer si possible.
- Preuve : capture de l'éditeur de decks ([docs/evidence/0.163.0-triad-editdeck-capture.log](docs/evidence/0.163.0-triad-editdeck-capture.log)) —
  chaque carte de la liste est un nœud image 40×40 de texture `ui/icon/088000/088NNN_hr1.tex` = carte NNN (icône 88000 + id).
- Les cartes du deck Tensho choisi (Triple Triade › Deck, par défaut le dernier enregistré) sont encadrées en vert dans la liste du jeu et le
  cadre suit le défilement ; le nom du deck en surbrillance s'affiche au-dessus de l'éditeur. Sans mode développeur. Lecture seule.
- Verify.ps1 (agent) : 540/540, 0 avertissement, 0 erreur ; DLL 0.164.0.0. Non validé en jeu.
## 0.163.0
- Retour 0.162 : l'aide-mémoire s'affiche bien à côté de l'éditeur de decks (nom `GSInfoEditDeck` confirmé en jeu, rapport utilisateur
  avec capture) ; demande : pas une interface mais une surbrillance des bonnes cartes dans la liste du jeu, comme TriadBuddy.
- Aide-mémoire retiré. Capture en mode développeur de l'éditeur de decks : nœuds image visibles (identifiant, rectangle écran, fichier
  d'icône) et valeurs de la fenêtre, une ligne « Triad edit deck capture » dans dalamud.log par changement. Sert à prouver quelle carte est
  dessinée où avant de l'encadrer.
- Verify.ps1 (agent) : 540/540, 0 avertissement, 0 erreur ; DLL 0.163.0.0. Non validé en jeu.
## 0.162.0
- Demande : après génération, voir les cartes du deck plus facilement que les autres (comme TriadBuddy), pouvoir supprimer le deck,
  les decks enregistrés dans une liste et nommés d'après le PNJ.
- Liste des decks Tensho (`Configuration.TripleTriadDecks` : nom du PNJ, identifiant du PNJ, 5 cartes ; un deck par PNJ, remplacé à la
  génération suivante). Triple Triade › Deck : chaque deck avec ses 5 icônes, « Afficher dans l'éditeur », « Supprimer ». Le panneau de la
  fenêtre de défi donne aussi le % de chaque deck Tensho.
- Aide-mémoire à droite de l'éditeur de decks du jeu (fenêtre `GSInfoEditDeck`, nom tiré du type FFXIVClientStructs `AddonGSInfoEditDeck`,
  à confirmer en jeu) : les 5 cartes du deck choisi en grand — icône, numéro de carte (N°), nom, rareté, chiffres — pour les retrouver
  dans la liste du jeu. Lecture seule ; rien n'est modifié dans le jeu.
- Test : 1 groupe (decks persistés avec le nom du PNJ, configuration ancienne vide). Verify.ps1 (agent) : 540/540, 0 avertissement,
  0 erreur ; DLL 0.162.0.0. Non validé en jeu.
## 0.161.0
- Retour utilisateur 0.160 : « test ok » (conseil en match) ; manquaient la génération de deck depuis la fenêtre de défi du PNJ et les %
  de victoire estimés par deck, comme TriadBuddy.
- Panneau « Tensho — Triple Triade » à droite de la fenêtre de défi `TripleTriadRequest` (observation Triple Triade active) : PNJ, règles,
  % de victoires et de nuls estimés pour chaque deck enregistré dans le jeu et le deck Tensho, bouton « Générer un deck pour ce PNJ »
  (cartes possédées), enregistrement comme deck Tensho. Rien n'est écrit dans le jeu : le joueur compose le deck dans l'éditeur du jeu.
- Données : règles du défi = valeurs 102–105 de la fenêtre (identifiants TripleTriadRule 10, 12, 4, 9 dans la capture du match Munglig =
  règles ensuite affichées en match) ; decks du joueur = `GoldSaucerModule.Decks` (FFXIVClientStructs : nom + 5 identifiants de carte),
  lus seulement si le module appartient au personnage ; deck du PNJ = feuille TripleTriad.
- Estimation (`TriadMatchSimulator`) : 400 matchs simulés par deck, main du PNJ tirée de son deck (fixes + variables au hasard), premier joueur
  au hasard, chaque camp joue le coup qui maximise l'écart après une réponse ; Chaos impose une carte au hasard, Ordre la suivante. Les
  règles Roulette (tirées au début du match) et Aléatoire sont signalées. Estimation : la logique des PNJ du jeu n'est pas connue.
- Génération : cartes possédées classées par leurs chiffres pour les règles (Inversion : petits chiffres), deck de départ valide (≤ 2 cartes
  4★ ou plus, ≤ 1 carte 5★), puis échanges d'une carte gardés s'ils améliorent le score simulé (mêmes tirages pour tous les decks), 6 s.
- Capture de match : seulement en mode développeur (elle coûtait une image de 66 ms au début du match, dalamud.log 14:47:36).
- Tests : 2 groupes `TriadMatchSimulatorTests` (reproductible, deck fort > faible, Inversion + Chaos, rareté, refus). Verify.ps1 (agent) :
  539/539, 0 avertissement, 0 erreur ; DLL 0.161.0.0. Non validé en jeu.
## 0.160.0
- TRIAD-003 étape 1 : conseil pendant le match, superposé à la fenêtre du jeu (comme TriadBuddy) : cadre vert sur la meilleure carte de la
  main et la meilleure case ; texte dans Triple Triade › Match observé. Lecture seule : le joueur pose la carte.
- Preuves (match contre Munglig, PNJ 1019323, 14:39, [docs/evidence/0.160.0-triad-match-capture.log](docs/evidence/0.160.0-triad-match-capture.log)) :
  `TurnState` 1 coup libre / 2 coup imposé / 0 attente (énumération FFXIVClientStructs, 2 observé à chacun des 4 coups du joueur en Chaos) ;
  valeurs 5–9 de la fenêtre = noms des règles du match (Inversion, Bonus, Identique, Chaos — la feuille du PNJ n'en donne que 2, d'où la
  lecture dans le match) ; valeur 25 = carte imposée (4/4 coups) ; PNJ = cible ; rectangles des 5 cartes de la main et des 9 cases.
- Correction : **NumSideR et NumSideL sont inversés dans FFXIVClientStructs** — les 5 cartes jouées par Munglig ne correspondent à son
  deck (TripleTriadCardResident) qu'en échangeant droite et gauche (Garuda U7 R6 D1 L7 lu U7 « R »7 D1 « L »6). L'observation corrige l'ordre ;
  l'observation 0.26–0.159 affichait droite et gauche inversées.
- Cartes cachées de l'adversaire : deck du PNJ (feuille TripleTriad liée par ENpcBase) — toutes les cartes fixes pas encore vues, puis
  chaque combinaison des variables pas vues, équiprobables ; chaque main est résolue par le solveur existant et les coups sont classés par
  écart moyen. Règles jouées par le solveur : Identique, Plus, Inversion, Tueur d'as, Bonus, Malus ; Chaos/Ordre via la carte imposée.
  Refus explicites (pas de conseil inventé) : carte illisible, deck inconnu, cartes adverses hors du deck.
- Tests : 3 groupes `TriadMatchAdvisorTests` sur la position réelle de 14:39:58 (cartes cachées = Cid et Sadu, révélées ensuite ; carte
  imposée respectée ; refus). Verify.ps1 (agent) : 537/537, 0 avertissement, 0 erreur ; DLL 0.160.0.0. VALIDATED_IN_GAME : « test ok » (utilisateur, 2026-10-31).
## 0.159.0
- Demande : « commence Triad » (TRIAD-003, finir l'outil comme TriadBuddy ; plan : [docs/goals/TRIAD-003-PLAN.md](docs/goals/TRIAD-003-PLAN.md)).
- Preuve hors jeu : sonde `PROBE_TRIAD` — 146 lignes TripleTriad, 144 PNJ liés par ENpcData, règles (Roulette, Toutes/Trois sur table,
  Identique, Mort subite, Plus, Aléatoire, Ordre, Chaos, Inversion, Tueur d'as, Bonus, Malus, Échange, Draft), cartes fixes et variables,
  récompenses, 46 PNJ aux règles régionales ([docs/evidence/0.159.0-triad-npc-probe.log](docs/evidence/0.159.0-triad-npc-probe.log)).
  Conséquence : les règles réellement appliquées (Roulette, régionales) ne se lisent que dans le match.
- Capture de match (avec l'observation Triple Triade active) : une ligne « Triad capture » dans dalamud.log à chaque changement de la fenêtre
  `TripleTriad` (état du tour, PNJ ciblé, valeurs de la fenêtre, faces et rectangles des cartes et des cases) et « Triad request capture »
  pour la fenêtre d'avant-match `TripleTriadRequest`. Chaînes lues bornées (240 octets, 60 caractères), 160 valeurs au plus. Sert à prouver
  règles, tour et positions avant de lier l'observation au solveur. Nouveau service injecté : `ITargetManager` (PNJ ciblé).
- Verify.ps1 (agent) : 534/534, 0 avertissement, 0 erreur ; DLL 0.159.0.0. Non validé en jeu.
## 0.158.0
- Demande : « régler d'abord le Mini Cactpot qui ne superpose aucune interface en jeu pour la solution, comme ezMiniCactpot »
  (COLL-004 sources inconnues mis en pause ; Triple Triade ensuite).
- Cause : la solution n'existait que dans la page Tensho, et n'était calculée que lorsque cette page était ouverte.
- Correction : superposition sur la fenêtre du jeu `LotteryDaily`, dessinée à chaque image tant que l'observation Mini Cactpot est active
  (Paramètres › Modules), même fenêtre Tensho fermée : cadre vert sur la ou les meilleures cases, puis sur la ou les meilleures lignes.
  Positions lues au thread framework : `GameBoard` (9 cases) et `LaneSelector` (8 boutons de ligne) de FFXIVClientStructs
  `AddonLotteryDaily` (réflexion sur l'assembly installée 7.57.0.9414) ; correspondance lignes du solveur → boutons du jeu
  (lignes, colonnes, diagonale ↘, diagonale ↙ → 5,6,7,1,2,3,0,4 ; sens des diagonales à confirmer en jeu). Lecture seule : le joueur clique.
  Libellé du réglage : « … et afficher la solution sur la fenêtre du jeu ».
- Verify.ps1 (agent) : 534/534 (+1 test : rectangles complets, copies, correspondance des lignes), 0 avertissement, 0 erreur ; DLL 0.158.0.0.
  Non validé en jeu.
## 0.157.0
- Demande (PUBLISH-018) : « fais-le et déclare créé par IA » pour l'icône, la déclaration d'IA, le nom et le choix du dépôt officiel ;
  puis « l'icône doit montrer une boîte à outils en rapport avec FF14 ».
- Icône 512×512 : boîte à outils rouge et or, cristal bleu (forme de cristal élémentaire, pas le logo du jeu), clé et marteau ; dessinée par
  l'agent avec System.Drawing ; `src/TenshoAssistant/images/icon.png`, copiée à côté de la DLL (icône des plugins de développement).
- Manifeste : nom affiché « Tensho Assistant » (au lieu de « Tensho--Assistant », aussi dans le titre de la fenêtre et l'aide de /tensho) ;
  description : déclaration « code et icône générés avec un agent IA, dirigés et testés en jeu par l'auteur » (la politique IA demande de
  déclarer les éléments graphiques générés dans la description). README : icône déclarée.
- Dépôt officiel choisi : dossier prêt `publish/d17/testing/live/TenshoAssistant/` (manifest.toml, images/icon.png) et brouillon de PR
  `publish/d17/PULL_REQUEST.md` avec déclaration IA niveau « Auto ». La politique exige que la déclaration et le texte de PR soient écrits par
  un humain : brouillon à relire et reformuler par l'auteur. Hash du commit à renseigner après push.
- Verify.ps1 (agent) : 533/533, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.157.0.0. VALIDATED_IN_GAME (icône) : rapport utilisateur 2026-10-31, visible après relance du jeu (cache d'icônes de Dalamud par session).
## 0.156.0
- Demande (PUBLISH-018, choix utilisateur) : préparer une publication sur le dépôt officiel Dalamud. Règles relues le 2026-10-31
  (soumission, restrictions, politique IA, métadonnées, considérations techniques) ; liste de contrôle : [docs/project/PUBLISHING.md](docs/project/PUBLISHING.md).
- Carte Tensho : passe par le système de fenêtres Dalamud (`MapViewWindow`, `WindowSystem`) au lieu d'un `ImGui.Begin` direct, comme
  Dalamud le demande pour les fenêtres normales ; comportement inchangé (taille, agrandissement, opacité, verrouillage, masquage en
  combat/instance, Échap ne ferme pas la carte, fermeture liée à la carte du jeu).
- Manifeste : nom, accroche et description en anglais à jour (carte, collections, sources communautaires, aucune action sans clic),
  `RepoUrl`, mots-clés ; `InternalName`, `AssemblyVersion` et `DalamudApiLevel` retirés de la source (remplis par DalamudPackager).
  La version ne se change plus que dans le csproj ; `Verify.ps1` refuse ces champs dans la source et compare la version au csproj.
- Audit : aucune requête serveur sans clic, aucune automatisation, aucun identifiant d'autre joueur, aucune télémétrie, requêtes HTTPS
  minimales. Restent à l'auteur : icône (faite main de préférence), `manifest.toml` D17, déclaration IA écrite par lui, nom affiché.
- Verify.ps1 (agent) : 533/533, 0 avertissement (avertissements traités en erreurs), 0 erreur, langues et paquet verts ; DLL 0.156.0.0. Non validé en jeu.
## 0.155.0
- Demande (MAP-017) : « vérifier les icônes des commissionnaires comme Rowena, pour mettre leurs icônes au clic droit et sur la carte ».
- Cause : un marchand ne rejoignait l'icône du jeu que pour Échoppe, Échangeur et Marchand. Les commises de Rowena (objets de classe/job,
  Limsa, Ul'dah, Gridania, Ishgard, Kugane) ont l'icône du jeu 63919 « Commise aux objets de classe/job » à 2–5 pixels : la carte dessinait
  en plus une icône d'échangeur générique, et l'onglet Vendeurs du clic droit montrait cette icône générique.
- Preuve : sonde `PROBE_MARKERS` de `tools/ShopCurrencyProbe` ; recensement des marchands placés par la feuille Level : icône du jeu à moins
  de 8 pixels = Échangeur 103, Échoppe 99, Réparations 15, Officier du logement 6, Mage bleu, Guide, Commise, Commandes spéciales, Dresseur
  (1 chacun) ; 132 sans marqueur ([docs/evidence/0.155.0-vendor-markers-probe.log](docs/evidence/0.155.0-vendor-markers-probe.log)).
- Correction : règle pure `MapVendorIcons` (Maps/MapCatalog.cs) — icônes de boutique à 16 pixels comme avant, icônes de service ci-dessus à
  8 pixels et seulement sans nom de lieu. Le marchand s'ajoute à l'infobulle de l'icône du jeu ; l'onglet Vendeurs affiche la même icône.
- Verify.ps1 (agent) : 533/533 (+1 test), 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.155.0.0. VALIDATED_IN_GAME : rapport utilisateur 2026-10-31 « carte et rowena ok » (carte 0.151–0.155 comprise).
## 0.154.0
- Nettoyage (CLEANUP-017, demande utilisateur) : `checkpoints/` sorti de git (dossier local) ; 28 preuves déplacées dans `docs/evidence/` et références mises à jour ; ce CHANGELOG généré depuis les notes de vérification.
- Documentation rangée : `docs/ai/` (règles, état, objectif, prompt maître), `docs/project/` (périmètre, feuille de route, backlog, décisions, architecture, tests, références), `docs/goals/` (79 objectifs), index `docs/README.md` ; 78 liens relatifs corrigés.
- Nouveaux/réécrits : CLAUDE.md (entrée IA, procédure par version), CONTRIBUTING.md (build, règles, publication Dalamud D17, politique IA), README (structure, carte Tensho, sources communautaires, déclaration d'usage de l'IA), STATUS et GOAL.
- Paramètres : nouvel onglet « Carte » reprenant tous les réglages de la carte (même contenu que la roue dentée de la carte). Verify.ps1 (agent) : 532/532, 0 avertissement, 0 erreur. Non validé en jeu.
- Relecture de `docs/project/` contre le code (documents seuls, DLL inchangée) : ARCHITECTURE, TESTING, REFERENCES, ROADMAP et BACKLOG ont une partie « état au 0.154.0 » relue contre le code (domaines, carte, sources d'objets, cache disque, services externes, protocole de test, backlog ouvert) et leur historique est marqué comme archive ; décisions DEC-010 (dépôt et documents) et DEC-011 (ordre des sources, outil ALEA) ; hors périmètre précisé dans PROJECT.
## 0.153.0
- Retour utilisateur 0.152 : icônes réglées pour un zoom rapproché deviennent minuscules vers 40 % (captures Solution Neuf).
- Cause : taille strictement proportionnelle au zoom (32 px × zoom).
- Correction : réglage « Suivi du zoom » (MapViewSettings.ZoomFollow, 0–100 %, défaut 50 %) ; facteur = zoom^suivi (1 à 100 %) appliqué aux icônes, symboles et noms de lieux. 50 % : à 40 % de zoom la taille reste 63 %. 0 % = taille fixe comme le jeu.
- Test ajouté (défaut, fixe, borne, invalide). Verify.ps1 (agent) : 532/532, 0 avertissement, 0 erreur. Non validé en jeu.

## 0.152.0
- Choix utilisateur : « Suivre le zoom » (captures Solution Neuf 44 % / 121 %).
- Constat : les icônes du jeu suivaient déjà le zoom (32 px × zoom, plancher 4 px) ; les noms de lieux étaient bornés (0,75 à 2,6 × police) et la pastille de groupe ≥ 14 px, d'où des proportions fausses aux zooms extrêmes.
- Correction : noms de lieux = 2,4 × police × zoom × échelle des noms (plancher 6 px) ; pastille de groupe plancher 6 px. Flèche du joueur inchangée (taille fixe, comme le jeu).

## 0.151.0
- Reprise étape 2 (corrections carte/listes, captures 77–79).
- Marqueurs non rafraîchis : la carte du jeu était fermée (agent inactif → EventMarkers figés). Référence Mappy (AddonAreaMapController, lecture seule) : AreaMap gardée ouverte hors écran. Désormais : AreaMap hébergée hors écran (RootNode à −9000, chaque image) tant que la carte Tensho est ouverte ; fermer l'une ferme l'autre (AgentMap.Hide / fermeture Tensho) ; position restaurée au déchargement …
- Quête derrière un PNJ de changement de zone (Solution Neuf → Sentinelle Neuf) : GameMapRequest.Resolve préfère la carte demandée quand une cible du jeu existe (test ajouté).
- Zones par type : quêtes (marqueur type 3), mandats et autres évènements, ALEA, récolte — case et couleur RGBA (ColorEdit4) ; contour renforcé.

## 0.150.0
- Reprise étape 1 (priorité utilisateur) : traduire les lignes du wiki dans la langue du plugin ; capture Aerith miniature (ligne « clear »).
- Analyseur : modèles de mise en page (clear, -, clr, br) retirés ; ligne réduite à « clear » ignorée ; cache « wiki-items » v3 (pages relues).
- Affichage : WikiTerm traduit les titres et termes connus via la section « WikiTerms » des fichiers de langue (≈ 60 termes : Purchase → Achat, Online Store → Boutique en ligne, Achievement → Succès, PvP → JcJ, Seasonal Event → Évènement saisonnier…) et les deux formules générées (« Boutique en ligne (N USD) », « Récompense de série JcJ (rang N) ») ; texte libre laissé en anglais ; terme absent = …
- Test : {{clear}} non retenu. Verify.ps1 (agent) : 531/531, 0 avertissement, 0 erreur, langues et paquet verts. Non validé en jeu.

## 0.149.0
- Retour utilisateur (capture outil Récolte) : « Temps restant » sans dire avant quoi.
- Correction : « Temps avant la fermeture » pendant une plage ouverte, « Temps avant l'ouverture » sinon (LocalClock.CountdownClose/CountdownOpen, FR/EN).

## 0.148.0
- Retours utilisateur 0.147 (captures) : « achetable au marché » est un critère, pas une source ; lien externe brut (Alsvid) ; encore inconnus (Aeturna, armures de Livia/Nero, baleine lunaire, ballons de céruleum, bénou).
- Preuve : pages brutes du wiki — Bennu Horn et Lunar Whale Identification Key : infobox « online-store-id » + ligne sans puce {{onlinestore|24}} / {{onlinestore|42}} dans ==Acquisition==.
- Correction : lignes sans puce de la section lues ; modèles connus traduits (onlinestore → « Online Store (N USD) », seriesmalmstonesreward → « PvP series reward (rank N) ») ; liens externes réduits à leur texte ; infobox online-store-id → « Online Store » ; cache « wiki-items » passé en v2 (pages relues avec le nouvel analyseur). Marché affiché « Critère : achetable au marché » sans arrêter la r…

## 0.147.0
- Retour utilisateur 0.146 : tous les points OK (VALIDATED_IN_GAME : échanges, Garland en dernier recours, infobulle) ; il reste des inconnus (capture astéropoing).
- Preuve : astéropoing = item 52262 « Rocket Punch Identification Key » ; Garland sans source ; page brute du wiki FFXIV : section ==Acquisition== → ===PvP=== + {{seriesmalmstonesreward|12|25}}.
- Ajout : WikiItemAcquisitionParser (section Acquisition, sous-titres, puces nettoyées : liens, modèles lisibles « nom paramètres », 12 lignes) + WikiItemAcquisitionService (nom anglais du catalogue, une requête toutes les 3 s, HTTP borné, DataCache « wiki-items » pour la durée des sources). Ordre : données du plugin → Garland → wiki → « Aucune source connue (données du jeu, Garland Tools et wiki)…
- Test ajouté. Verify.ps1 (agent) : 531/531, 0 avertissement, 0 erreur. Non validé en jeu.

## 0.146.0
- Retours utilisateur 0.145 (captures Alsvid, buffle insulaire, adamankhélone, armure aérienne magitek) : sources encore inconnues ; ne pas utiliser Garland quand la source est déjà connue ; infobulle déformée (prix en colonne).
- Preuves : sonde (0.146.0-achievement-probe.log) — aucun succès ne récompense 41498 (Sifflet Alsvid), 40382 (Cor buffle insulaire) ni 39486 ; recherche boutiques — 40382 vendu par SpecialShop 1770601 (50 000 × 37549 Assignat insulaire azur) sans PNJ relié (île), 41498 dans aucune boutique ; Garland : 41498 rien, 40382 tradeShops.
- Correction : GarlandItemSourceList.Trades lus dans tradeShops (échanges de l'objet, même sans PNJ) → « Échange : 50 000 × Assignat insulaire azur » ; Garland consulté seulement si aucune source locale (vendeurs, quêtes, succès, fabrication, récolte, marché) ; ligne vendeur repliée puis prix sur sa propre ligne indentée ; message « Aucune source connue (données du jeu et Garland Tools) ».
- Test ajouté (échanges tradeShops). Verify.ps1 (agent) : 530/530, 0 avertissement, 0 erreur. Quota relevé utilisateur : 88 % sur 5 h (réinit. 12:10), 39 % semaine. Non validé en jeu.

## 0.145.0
- Retour utilisateur 0.144 : « Garland est chargé à chaque fois au lieu de faire un cache ».
- Constat (DataCache-v1 sur le PC, 0.145.0-cache-before.log) : dossier plein (≈256 Mo, limite) ; un fichier « quests » de 4,8 Mo par version du plugin (clé contenant la version : 0.120, 0.127, 0.141, 0.144…), deux formes du catalogue d'objets (18 Mo chacune). La rotation supprimait les fichiers les plus anciens : catalogues et sources Garland étaient effacés puis relus.
- Correction DataCache.Trim : un seul fichier par famille + portée (le plus récent ; lu dans l'en-tête), les « catalog-* » ne sont jamais supprimés par le nombre de fichiers, les autres entrées gardent jusqu'à 2048 fichiers, la limite d'octets retire d'abord les petites entrées les plus anciennes. Le nettoyage se fait à la prochaine écriture.
- Test ajouté (nouvelle version remplace l'ancienne, autre portée intacte). Verify.ps1 (agent) : 529/529, 0 avertissement, 0 erreur. Non validé en jeu.

## 0.144.0
- Retours utilisateur 0.143 (capture Alexander articulé) : les sources doivent suivre l'objet qui débloque (pas seulement la monture/mascotte) ; titres et succès terminés mal affichés.
- Preuve Garland : item/fr/3/16574 (Alexander articulé) → partials instance 30042 et 30046 « Alexander - L'Âme du Créateur (sadique) » ; 39486 (Cor Aeturna) sans source.
- Sources Garland : GarlandItemSourceParser (partials instance/mob/fate/quest/achievement/leve/npc, ids texte ou nombre, doublons retirés) + GarlandItemSourceService (à la demande, une requête à la fois, cache « sources »). Affichées dans l'infobulle des collections (par objet) et dans l'infobulle d'objet des outils, avec une section « Comment l'obtenir » (vendeurs, quêtes, succès, fabrication, ré…
- Titres/succès : CollectionService.TitleListLoaded/AchievementListLoaded publiés par le lecteur (thread framework) ; la page affiche en rouge « ouvre une fois la fenêtre Titres/Succès du jeu » tant que le jeu n'a pas envoyé la liste (sans elle, l'état reste inconnu). Cause exacte du retour « mal affichés » non prouvée : à confirmer avec ce message.

## 0.143.0
- Retour utilisateur 0.142 (captures Castor géant, Aeturna) : état « pas encore obtenue » OK (VALIDATED_IN_GAME) ; l'infobulle ne cite pas les récompenses de succès, quêtes…
- Vérification : Garland Tools n'a aucune source pour « Cor Aeturna » (item 39486 : pas de vendors/drops/instances).
- Correction : infobulle « comment l'obtenir » : succès dont la récompense est l'objet (Achievement.Item via l'observation des collections), quêtes (index des récompenses), fabrication, récolte (type, zone), vendeurs PNJ, marché ; si aucune de ces sources : « Aucune source connue dans les données lues (contenu, évènement, boutique en ligne… non couverts) ».
- Limite : butins de contenus/coffres, évènements et boutique en ligne ne sont pas couverts par les données lues.

## 0.142.0
- Retours utilisateur 0.141 (captures Castor purgateur, adamankhélone) : l'infobulle ne sait pas si l'objet est obtenu (« état inconnu (collections non lues) ») ; pas de clic droit objet → collection ; la collection n'a pas d'infobulle « comment l'obtenir ».
- État : montures et mascottes sont lues en direct (IUnlockState) à la connexion ; une fois les collections lues pour le personnage, « non débloqué » vaut « pas encore obtenue » (CollectionStateOf). Les autres catégories gardent la règle de preuve. Texte « collections non lues » remplacé par « état inconnu ».
- Lien : ItemCatalog.ForMount/ForCompanion ; clic droit sur un objet (fiche vendeur, objet choisi de l'outil PNJ) → « Voir dans les collections » (onglet forcé Montures/Mascottes/Emotes/Orchestrion, recherche sur le nom ; cartes → Triple Triade).
- Page Collections : infobulle « comment l'obtenir » pour montures, mascottes, emotes, cartes, rouleaux : état, lieu et description du jeu, objets qui l'enseignent, vendeurs PNJ avec prix, quêtes qui récompensent l'objet, recettes, marché ; « aucun objet du jeu ne l'enseigne » sinon.

## 0.141.0
- Retour utilisateur 0.140 : l'outil PNJ vendeurs n'affiche aucune infobulle d'objet et ne fait pas le lien avec les collections.
- Preuve (sonde, 0.141.0-itemaction-probe.log) : ItemAction.Action 1322 = monture (« Sifflet chocobo » → Mount 1 « chocobo destrier »), 853 = mascotte (« Bombino » → Companion 1 « bombino »).
- ItemDefinition.MountId/CompanionId (ItemUnlockCopy.Lumina) ; forme changée → cache des objets relu une fois depuis le jeu.
- DrawItemCollection : état « déjà obtenue / pas encore obtenue / état inconnu » pour monture, mascotte, carte Triple Triade, rouleau d'orchestrion, d'après l'observation des collections (aucun « non obtenu » sans preuve). Ajouté au survol des objets du jeu et à l'en-tête de l'outil PNJ vendeurs.

## 0.140.0
- Retours utilisateur 0.139 (capture Ose Wyd) : deux icônes pour la même chose (icône du jeu + icône Tensho) ; un PNJ d'échange prend l'icône du vendeur en gils ; pas de lien clic droit vendeur → outil PNJ vendeurs.
- Doublon : un vendeur à 16 pixels de texture au plus d'une icône du jeu Échoppe (60412), Échangeur (60935) ou Marchand (63851) n'a plus d'icône Tensho ; ses boutiques s'ajoutent à l'infobulle de l'icône du jeu (clic = drapeau sur le premier vendeur).
- Icône par type : vendeurs n'ayant que des échanges spéciaux → Échangeur (60935), sinon Échoppe (60412) ; les deux dans « Icônes de cette carte ».
- Lien : onglet Vendeurs du clic droit, clic droit sur un vendeur → « Ouvrir dans l'outil PNJ vendeurs » ; l'outil affiche une fiche vendeur (emplacements avec « Voir sur la carte », stock par boutique avec icône, nom et prix ; clic sur un objet → vue par objet ; bouton retour).

## 0.139.0
- Retour utilisateur 0.138 (capture Ys Gyuf) : infobulle correcte ; le vendeur doit utiliser l'icône du jeu et son infobulle.
- Preuve (sonde, 0.139.0-symbols-probe.log) : MapSymbol 60412 = « Échoppe » (63851 = « Marchand »).
- Correction : les vendeurs sont dessinés avec l'icône 60412 dans la liste commune des icônes du jeu (taille, masquage, priorité, liste au survol avec icône et titre doré, action « placer le drapeau » sur l'icône du dessus) ; l'ancien symbole sac est retiré ; l'icône apparaît dans les réglages « Icônes de cette carte » quand la couche est active.

## 0.138.0
- Retour utilisateur 0.137 (captures) : infobulle récolte OK ; vendeurs : nom de boutique japonais « アイテムの購入 » (Aenc Ose), vendeurs sans boutique affichée (castor géant, Ose Wyd), pas d'indication du nombre d'articles.
- Correction : chaque boutique affiche son nom, ou son type (Boutique (gils) / Grande Compagnie / Échange spécial) si le nom est vide ou un texte japonais non traduit hors langue japonaise ; nombre d'objets distincts en vente ; « N boutique(s) sans vente lue dans le catalogue » ; « Aucune boutique lue » si rien. 6 boutiques au plus puis « … et N autres ».

## 0.137.0
- Retour utilisateur 0.136 : nom du marqueur de mandat OK (VALIDATED_IN_GAME). Suite du plan MAP-016 : PNJ vendeurs.
- Couche « PNJ vendeurs » (désactivée par défaut, section À proximité) : vendeurs du catalogue NPC-VENDORS-014 en cache (positions Level et décors planevent/planner, coordonnées monde X/Z), sac doré ; infobulle nom + boutiques (6 au plus) ; clic = drapeau. Liste par carte mise en mémoire (changement de carte ou de catalogue seulement).
- Clic droit : onglet « Vendeurs » (nombre, nom, coordonnées, boutiques au survol, clic = drapeau).

## 0.136.0
- Retour utilisateur 0.135 : quêtes nommées OK (VALIDATED_IN_GAME : « Tapage nocturne », « Mais si on danse ? ») ; mandat 71041 toujours « Icône 71041 ».
- Preuve (/xllog, 0.136.0-marker-evidence.log) : StringLength = 0 pour tous les marqueurs ; BufUsed = octets + terminateur (71041 : 8 ; 71021 : 24 ; 63922 : 1 = vide), BufSize 32/64.
- Correction : longueur = BufUsed − 1 (comme Utf8String.ToString), bornée à 1–256 octets et ≤ BufSize ; copie exacte de cette longueur. Le mandat doit afficher le texte du jeu (7 octets).

## 0.135.0
- Retour utilisateur 0.134 (captures) : icônes 71041, 71151, 71021 sans nom (« Icône 71041 »…).
- Preuve (/xllog, 0.135.0-marker-evidence.log) : toutes les infobulles « unread » → la règle 0.133 « BufUsed = longueur + 1 » rejetait tout ; type 3 = quête avec DataId = ligne − 65536 (71021 : data 24 ; 71151 : data 141) ; 71041 : objective 393220 (= 6×65536 + 4, d'où la collision 0.133 avec la quête 65540).
- Correction : lecture bornée conservée (1–256 octets, dans le tampon, longueur propre) sans la règle BufUsed ; marqueur de type 3 nommé par la quête exacte (DataId + 65536) ; repli ObjectiveId limité aux quêtes en cours. Trace complétée avec longueur/used/size pour vérifier la lecture.

## 0.134.0
- Retour utilisateur 0.133 (capture Gridania) : noms des quêtes OK ; l'icône de mandat affiche le nom d'une quête (« Dites-le avec des gâteaux », quête 65540).
- Cause probable : quand l'infobulle du marqueur n'est pas lisible, le repli 0.133 cherchait une quête par les 16 bits bas de l'ObjectiveId ; 65540 & 0xFFFF = 4 peut coïncider avec l'identifiant d'un mandat.
- Correction : repli limité aux quêtes en cours (seules leurs étapes portent un ObjectiveId de quête) ; sinon pas de titre et la liste affiche le nom de symbole du jeu (MapSymbol). Trace Information « Map event marker icon … objective, type, data, tooltip read/unread » une fois par icône (64 au plus) pour établir en jeu ce que donne chaque type de marqueur.

## 0.133.0
- Retour utilisateur 0.132 : liste des icônes survolées OK (VALIDATED_IN_GAME) ; le jeu a planté une fois.
- Analyse (dalamud.crashhandler.log, 2026-10-31 08:19:55 ; extrait : 0.133.0-crash-excerpt.log) : pile du thread fautif = TenshoAssistant.Maps.MapLiveReader.ReadEvents → FFXIVClientStructs Utf8String.ToString → Encoding.GetString → Ascii.GetIndexOfFirstNonAsciiByte_Vector : lecture de l'infobulle d'un marqueur d'événement (MapMarkerData.TooltipString) pointant vers une mémoire libérée, sans borne …
- Correction : TooltipText lit seulement une chaîne cohérente (StringLength 1–256, BufUsed = longueur + 1, BufSize ≥ BufUsed et ≤ 4096, pointeur non nul) et copie exactement cette longueur ; sinon "". La carte retrouve alors le nom de la quête par ObjectiveId (EventLabel). Réf. Mappy (lecture seule) : lit StringPtr sans borne lui aussi.
- Limite : une lecture d'une mémoire libérée reste possible si l'en-tête paraît cohérent ; le risque est fortement réduit (lecture bornée à 256 octets), pas éliminé. À surveiller (plantage récurrent ?).

## 0.132.0
- Demande utilisateur : exploration en pause ; voir une liste des icônes de la zone survolée, comme Mappy.
- Référence lue (AGPL, lecture seule, rien copié) : ffxiv-tc-port/Mappy Classes/DrawHelpers.cs DrawTooltip — chaque marqueur survolé, ou dont le rayon contient le curseur, ajoute à la même infobulle une ligne icône 32 px + texte principal + texte secondaire.
- Correction : toutes les icônes du jeu sous le curseur (ou dont la zone le contient : quêtes, ALEA, points de récolte) sont listées dans une seule infobulle, la plus haute d'abord (12 au plus, « … et N autres ») : icône, titre, lignes secondaires ; points de récolte avec horaire et objets (icônes + noms) ; l'icône du dessus indique l'action du clic (téléporter, ouvrir la carte, drapeau). Le clic …

## 0.131.0
- Retour utilisateur 0.130 (capture Stèle du chant marin) : « c'est mieux mais il manque encore d'info » — météo demandée absente, pas d'indication d'emplacement.
- Source ajoutée : page brute du wiki FFXIV (consolegameswiki, Sightseeing_Log, action=raw, ~98 Ko) lue une fois (HTTP borné 25 s), gardée dans DataCache « wiki-vistas » pour la durée des sources. Couverture vérifiée sur la page téléchargée : 340 lignes lisibles (80 ARR avec météo et heure, puis 62/62/45/46/45) = 340 repères du jeu.
- Association : coordonnées à 0,3 près sur la carte du repère ; pour ARR, n° = index de ligne + 1 en plus (vérifié #001–#003) ; deux candidats ou aucun = pas d'association (rien n'est deviné).
- Infobulle : n° de carnet et extension, météo demandée traduite (WeatherCatalog.WeatherBySourceName) en vert si elle correspond à la météo actuelle, rouge sinon ; « où se placer » (commentaire du wiki, en anglais) ; source et date. Repères HW+ : « aucune condition de météo ».

## 0.130.0
- Retours utilisateur 0.129 : les points de récolte à horaire ne se distinguent pas ; exploration : rien ne se déclenche même à la bonne heure ; référence demandée : https://arr-exploration.fr/.
- Référence lue (lecture seule) : site React, données écrites à la main par repère (titre, description, time [8,12], position [9.5,7.8], weather1/weather2, emote) ; heure d'Éorzéa calculée × 1440/70 (identique à Tensho). Mêmes positions/heures/emotes que nos données du jeu ; la météo y est ajoutée à la main (absente des données du jeu). Aucune donnée copiée.
- Récolte : MapLocalGatheringPoint.Schedule/Timed/ActiveAt (fenêtres ET, y compris après minuit) ; pastille horloge verte (ouvert maintenant) ou orange (fermé) sur la carte ; infobulle « À horaire : … — ouvert/fermé maintenant ».
- Exploration : infobulle avec la météo actuelle de la zone (prévisions Tensho) et l'état de déblocage du carnet (PlayerState.SightseeingLogUnlockState : entrées 1–20 palier 1, 21–80 palier 2 ; au-delà, paliers affichés sans règle supposée). Si l'entrée n'est pas débloquée, le jeu ne la valide pas.

## 0.129.0
- Retour utilisateur 0.128 : « exploration toujours rien aux coordonnées de l'icône ».
- Vérification (sonde, 0.129.0-sightseeing-probe.log) : #001 Quais du barracuda (9,6 ; 7,8), #002 Astalicia (7,1 ; 15,1), #003 Stèle du chant marin (20,3 ; 19,1) — identiques au carnet publié (ffxiv.consolegameswiki.com, Sightseeing Log : 9,5/7,8 ; 7,1/15,1 ; 20,3/19,1). Les positions sont justes ; un repère d'exploration est invisible en jeu et ne se valide qu'avec heure, météo et emote.
- Ajout : MapLocalPosition.MinTime/MaxTime (hhmm Éorzéa, ex. 800–1159, 1800–459) et Extra (emote : Scruter, Prier…) copiés de Adventure ; infobulle : fenêtre horaire avec « (en ce moment) » selon l'heure d'Éorzéa, emote, et mention que la météo n'est pas dans les données. Forme changée → cache réécrit une fois.
- Test : fenêtres horaires (y compris après minuit). Verify.ps1 (agent) : 523/523, 0 avertissement, 0 erreur. Non validé en jeu.

## 0.128.0
- Retours utilisateur 0.127 : repères d'exploration mal placés ou état « découvert » ignoré (option Masquer cochée) ; courants éthérés : se cachent bien avec l'option (état lu correctement) ; demander les explorations et courants avec statut dans le clic droit ; liste des icônes incomplète.
- Repères : ClientStructs (PlayerState.cs, main) confirme IsAdventureComplete(index de ligne) et _completedAdventures = 340 bits. Lecture désormais par le tableau de bits brut (bit i = ligne i), la fonction native n'étant que comparée ; trace « Sightseeing: N discovered by the bit array, M by IsAdventureComplete, of 340 » dans /xllog à chaque changement. Coordonnées : Adventure → Level inchangé, à…
- Clic droit : onglets Exploration et Courants (état [x]/[ ]/[?], compteur obtenus/total, n° et coordonnées, image du repère au survol, clic = drapeau).
- Icônes : en mode « Toutes les cartes », tous les symboles de carte nommés du jeu (MapSymbol) + cible du journal ; filtre par nom.

## 0.127.0
- Retour utilisateur 0.126 : coins de pêche OK (VALIDATED_IN_GAME). Suite du plan MAP-016 : groupe, repères d'exploration, courants éthérés.
- Preuves (sonde lecture seule, 0.127.0-sightseeing-probe.log) : Adventure 340 lignes nommées (2162688…2163027), toutes avec Level + carte ; AetherCurrent 448 lignes, 151 liées à une quête, 152 courants de terrain portés par un EObj (Data) que place une Level avec carte (152/152).
- Index « catalog-map-positions » : MapLocalKind.Sightseeing (nom, image découverte) et AetherCurrent (terrain à la Level de l'EObj ; quête au donneur, Data = quête), listes par carte (OnMap). Forme changée → cache réécrit une fois.
- MapLiveReader : courants obtenus (IsAetherCurrentUnlocked) et repères découverts (IsAdventureComplete(ligne − première ligne), index À CONFIRMER en jeu) toutes les 2 s ; membres du groupe de la même zone (IPartyList) toutes les 0,5 s.

## 0.126.0
- Retour utilisateur 0.125 : points de botaniste et mineur OK (VALIDATED_IN_GAME) ; aucun point de pêche.
- Cause : seuls les points de minage/botanique avaient une position (ExportedGatheringPoint) ; les coins de pêche n'étaient pas dans l'index.
- Preuve (sonde lecture seule, 0.126.0-fishing-probe.log) : FishingSpot.X/Z sont des pixels de la texture de carte — spot 2 → 22,90/22,18 et spot 35 → 7,69/12,21, identiques aux coordonnées Garland Tools. Unité du rayon non prouvée (400, 3000) → zone non dessinée.
- Correction : MapLocalKind.FishingSpot dans l'index « catalog-map-positions » (monde = (pixel − 1024)/échelle − décalage) ; la forme change, le cache est réécrit une fois automatiquement. Les coins de pêche apparaissent sur la carte, dans l'onglet Récolte (Pêche) et avec l'infobulle des poissons ; clic = drapeau.

## 0.125.0
- Demande utilisateur : l'infobulle des points de récolte doit afficher les icônes et les noms des objets.
- Correction : DrawGatheringTooltip — icône du type, type · niveau (caché), lieu, puis une ligne par objet (icône du jeu + nom, 16 au plus), « Clic : placer le drapeau ». Utilisée sur la carte (icône du dessus) et dans l'onglet Récolte du clic droit.

## 0.124.0
- Retours utilisateur 0.123 : points de récolte sur la carte ne fonctionnent pas (le clic droit oui) ; la priorité ne change pas celle des quêtes.
- Récolte : la couche ne montrait que les points chargés par le jeu autour du joueur (ciblables seulement avec le bon métier). Désormais, avec « Points de récolte », la carte dessine tous les points connus de la carte (catalogue « catalog-gathering » + positions « catalog-map-positions », mis en mémoire par carte) : icône du type (GatheringType.IconMain), zone (rayon), niveau, survol (type, niveau…
- Priorité : toutes les icônes du jeu (fixes, récolte, ALEA, quêtes, cible du journal) dans une seule liste triée par priorité ; à priorité égale, ordre des couches du jeu. Seule l'icône du dessus réagit au survol/clic (lien de carte, téléportation, drapeau). Zones dessinées sous les icônes. Types de récolte ajoutés à la liste « Icônes de cette carte ».

## 0.123.0
- Retour utilisateur : quête depuis le journal → carte OK (VALIDATED_IN_GAME, zoom et icône). Outil ALEA jugé inutile (pas d'expérience, liste d'objets identique) : à retirer en gardant la partie carte.
- Retiré : page ALEA (Page.Fates, MainWindow.Fates.cs, lien de navigation), FateRewardService/FateRewardParser et leurs tests, route Garland « fate », entrée « Ouvrir dans l'outil ALEA » du clic droit, textes FateUi/Category.Fates.
- Conservé : couche ALEA en cours sur la carte (réglage « ALEA en cours »), onglet ALEA du clic droit (drapeau au clic), tailles/priorités d'icônes.

## 0.122.0
- Retour utilisateur 0.121 : le zoom sur la quête se fait bien (méthode TempMapMarkers validée en jeu) ; mais l'icône de la quête disparaît.
- Cause : la cible est un marqueur temporaire du jeu (pas un EventMarker) ; la carte du jeu étant fermée, il n'était dessiné nulle part.
- Correction : GameMapRequest.TargetIcon (MapMarkerBase.IconId) ; la carte Tensho garde la cible (carte, position, icône) jusqu'à la prochaine demande du jeu et la dessine (taille/masquage par icône respectés), survol « Cible indiquée par le jeu (journal) ».

## 0.121.0
- Retour utilisateur 0.120 : toujours pas de zoom sur la quête ; demande « regarde comment fait Mappy ».
- Référence lue (AGPL, lecture seule, rien copié) : fork à jour ffxiv-tc-port/Mappy, Controllers/IntegrationsController.cs — pour MapType.QuestLog, Mappy centre sur AgentMap.TempMapMarkers[0].MapMarker, dont X/Y valent la position monde × 16 (mêmes formules de centrage que pour une position monde).
- Correction : au moment où la carte du jeu apparaît (QuestLog ou GatheringLog), lecture de TempMapMarkerCount et TempMapMarkers[0].MapMarker.X/Y ÷ 16 → GameMapRequest.Target (monde X/Z) ; la carte Tensho se centre d'abord sur cette cible (zoom ≥ 100 %), puis les replis 0.120. Trace « Game map replaced: … target … » dans /xllog.
- Test : Target rend la demande pointée. Verify.ps1 (agent) : 524/524, 0 avertissement, 0 erreur. Non validé en jeu.

## 0.120.0
- Retour utilisateur 0.119 : quête du journal sur une autre zone = bonne carte ; mais sur la zone courante, le clic sur la quête (ou le bouton carte du journal) ne zoome pas sur la quête.
- Causes probables : demande du journal sans QuestId → traitée comme « M » (suivi du joueur) ; marqueur non trouvé par ObjectiveId → carte entière.
- Correction : GameMapRequest.QuestLog (MapType.QuestLog) rend la demande « pointée » ; recherche du marqueur par ObjectiveId puis par nom de quête dans l'infobulle du marqueur, réessayée 1,5 s ; repli donneur/éthérite/carte entière. Traces Information dans /xllog : « Game map replaced: … » et « Map focus found / not found … markers: objectiveId:icon:tooltip » pour prouver le lien en jeu.
- Test : GameMapRequest.Pointed. Verify.ps1 (agent) : 524/524, 0 avertissement, 0 erreur. Non validé en jeu.

## 0.119.0
- Reprise prévue : réglages globaux des icônes + surcharges par carte, priorité réglable.
- MapViewSettings : MapIconSizes (carte → icône → taille), IconPriorities (global) et MapIconPriorities (carte → icône → priorité −9..9). SizeOf/PriorityOf(icon, map) : surcharge de la carte, sinon global, sinon défaut. Reset(map) = revenir au global ; Reset(0) = remettre le global à zéro. Configurations 0.111+ conservées.
- Carte : icônes fixes, quêtes et ALEA dessinées par priorité croissante (la plus prioritaire au-dessus et reçoit le survol) ; tailles par carte appliquées (drapeau compris).
- Réglages « Icônes de cette carte » : choix Toutes les cartes / Cette carte, état (propres / suit le global), bouton Revenir aux réglages globaux, colonne priorité, lignes triées par priorité ; ALEA de la zone listées aussi.

## 0.118.0
- Git : .gitignore complété (IDE, TestResults, checkpoints/*.zip) ; 96 archives retirées du suivi (gardées sur disque). Commits faits par l'application de bureau. DEC-009.
- ACT/IINACT/Combat retirés du projet (docs : PROMPT-MAITRE §5, RULES, ROADMAP, BACKLOG, PROJECT, CLAUDE, BASE-AUDIT-002 ; aucun code concerné).
- Quête cliquée dans le journal du jeu : CurrentOpenMapInfo.QuestId/AetheryteId/FateId lus ; carte Tensho ouverte sur la carte demandée et centrée (zoom ≥ 100 %) sur le marqueur de la quête (MapMarkerData.ObjectiveId), sinon le donneur (index en cache), sinon l'éthérite donnée par le jeu, sinon carte entière. Plus de bascule « M » pour une demande pointée. Correspondance ObjectiveId ↔ QuestId non …
- Agrandir/Restaurer : l'ajustement attend 2 images (taille réelle de la fenêtre).

## 0.117.0
- Demande utilisateur : vue double au lieu des sous-onglets (capture Mer de Rubis, onglets tronqués).
- Correction : DrawSplitView — catégories (quêtes) / types (récolte) à gauche avec leur nombre, lignes à droite ; panneau élargi à 640 × 360 ; EndChild toujours appelé.

## 0.116.0
- Retour utilisateur 0.115 : la carte ne se centre pas sur le drapeau créé.
- Correction : PlaceFlagFromMapView coupe le suivi du joueur et demande le centrage sur le pixel du drapeau (MapProjection.PixelFromCoordinate), appliqué à l'image suivante quand la taille du canevas est connue. Zoom inchangé.

## 0.115.0
- Retour utilisateur 0.114 : placer un drapeau depuis la carte Tensho ferme la carte.
- Cause : le drapeau passe par OpenMapWithMapLink, qui ouvre la carte du jeu sur la carte du drapeau ; le remplaçant la ferme et demande la carte Tensho ; sur la zone courante, la demande vaut « M » (suivi du joueur) et bascule la fenêtre fermée.
- Correction : PlaceFlagFromMapView (menu « Placer le drapeau » et listes locales) ignore la prochaine demande du jeu pendant 2 s ; la carte Tensho reste ouverte sur la même carte. L'outil Carte/Monde garde l'ancien comportement (ouvre la carte Tensho sur le point).

## 0.114.0
- Demande utilisateur : dans le menu clic droit de la carte, listes locales en onglets — quêtes par catégorie, ALEA, zones de récolte, etc. — en respectant le cache.
- Preuve des liens (sonde en lecture seule, 0.114.0-local-probe.log) : Quest.IssuerLocation → Level avec carte pour 5 292/5 292 quêtes ; ExportedGatheringPoint (id = GatheringPointBase) a une position pour 1 077/1 426 bases ; Fate.Location ne pointe vers aucune Level avec carte (0/1 712) → pas de position fixe des ALEA, seules les ALEA en cours (IFateTable) sont utilisées.
- Cache : nouvel index MapLocalIndex (positions donneurs de quête + points de récolte), copié en lots de 2 ms sur le thread framework, indexé sur un worker, gardé dans DataCache famille « catalog-map-positions » (clé = versions des dépôts + schéma Lumina + règles de copie + forme, pas la version du plugin), comme CATALOG-CACHE-012. Quêtes (cache « quests » par personnage) et récolte (« catalog-gat…
- Menu clic droit : onglets Quêtes (sous-onglets par catégorie du journal, terminées masquées par défaut), ALEA (zone actuelle, niveau, progression, temps), Récolte (sous-onglets par type, objets au survol, pêche sans position), Éthérites (téléportation avec coût). Clic = drapeau du jeu à la position.

## 0.113.0
- Demande utilisateur : les icônes doivent suivre le zoom.
- Correction : bornes 10–64 px (et 8–96 px par icône, 12 px pour coffres/récoltables) retirées ; taille = 32 px × zoom × taille globale × taille de l'icône, plancher de 4 px seulement. Flèche du joueur inchangée (taille fixe).

## 0.112.0
- Retour utilisateur 0.111 : un clic sur une quête affiche le monde au lieu de la carte concernée.
- Cause probable (non prouvée en jeu) : à la première image où la carte du jeu apparaît, AgentMap.SelectedMapId vaut encore la carte du monde ; seul ce champ était lu.
- Correction : lecture aussi de AgentMap.CurrentOpenMapInfo (MapId, TerritoryId, Type) ; décision pure GameMapRequest.Resolve : sélection de zone > carte demandée > première carte du territoire demandé > sélection. Trace Debug « Game map replaced: type, map, territory, selected » dans /xllog pour diagnostic.
- Test ajouté : Game map request (monde sélectionné + zone demandée, territoire seul, M sur la zone courante, sélection de zone, rien d'autre).

## 0.111.0
- Demande utilisateur : vrai prix en gils dans la fenêtre de téléportation ; taille réglable ou masquage pour chaque icône.
- Coût : copié sur le thread framework depuis Telepo.UpdateAetheryteList() (TeleportInfo.AetheryteId → GilCost, entrées de logement exclues), relu à chaque clic sur une éthérite. Éthérite absente de la liste = « coût inconnu (non harmonisée ?) », jamais 0. Icône Gil du jeu après le montant.
- Icônes : MapViewSettings.IconSizes (id d'icône → taille, 0 = masquée, absente = 100 %), en plus de la taille globale. Section « Icônes de cette carte » : une ligne par icône présente (fixes, quêtes/événements, drapeau) avec case, aperçu, curseur 50–200 %, nom MapSymbol du jeu ; bouton « Tout remettre à 100 % ».
- Test ajouté : Per-icon sizes (défaut, masquage, bornes, JSON).

## 0.110.0
- Retour en jeu 0.109 : AltGr + M n'ouvre toujours rien. À la demande de l'utilisateur, la fonction est retirée : touche, réglage « AltGr + M ouvre quand même la carte du jeu », textes FR/EN, dépendance IKeyState.
- Conservé : bouton « Ouvrir la carte du jeu » (même mécanisme AgentMap.Show, non validé en jeu) et réglage « Remplacer la carte du jeu ».
- Barre de zoom réduite (16 × 80 px, centrée) ; pourcentage du zoom affiché sous le bouton −.
- Menus Région / Zone / Étage : largeur calculée sur l'entrée la plus longue + flèche (+ place de l'aile pour les zones).

## 0.109.0
- Retour en jeu 0.108 : M ouvre bien la carte Tensho (VALIDATED_IN_GAME) ; AltGr + M n'ouvre pas la carte officielle.
- Analyse : AltGr arrive au jeu comme Ctrl + Alt ; « Ctrl + Alt + M » n'est pas un raccourci du jeu, qui n'ouvre donc rien à garder.
- Correction : le plugin détecte AltGr + M (front montant) et ouvre lui-même la carte du jeu (AgentMap.Show) avec un passe-droit de 2 s pour ne pas la remplacer ; bouton « Ouvrir la carte du jeu » dans la colonne d'outils de la carte Tensho (même mécanisme).

## 0.108.0
- Retour en jeu 0.107 : M ouvrait toujours la carte officielle ; Maj + M est déjà utilisé dans le jeu.
- Correction : NativeMapReplacer surveille la fenêtre AreaMap à chaque tick framework (IGameGui) au lieu de l'écouteur IAddonLifecycle PostOpen. À son apparition, il la ferme (AtkUnitBase.Close(true)) et ouvre la carte Tensho sur AgentMap.SelectedMapId (ou la zone courante). Décision prise une fois par ouverture ; touche de maintien de la carte du jeu : AltGr (RMENU, ou Ctrl + Alt), option KeepGam…

## 0.107.0
- Demande : coffres en donjon, zones de mandats et de quêtes, récoltables avec info-bulles (désactivables, désactivés par défaut), téléportation sur éthérite, plus d'options par section, remplacer la carte du jeu.
- Sources vérifiées : Telepo.Teleport(aetheryteId, subIndex) ; ObjectKind.Treasure / GatheringPoint ; IAddonLifecycle PostOpen « AreaMap », AgentInterface.Hide, AgentMap.SelectedMapId ; MapMarker DataType 3 = éthérite (DataKey = Aetheryte), type 4 = éthérites secondaires (0.107.0-aetheryte-probe.log).
- Ajouts : coffres et points de récolte ciblables (table des objets, 0,5 s, 200 au plus ; niveau via GatheringPoint → GatheringPointBase) avec info-bulle et étiquette « nom · niv. » ; clic sur une éthérite → confirmation → Telepo.Teleport sur le thread framework ; remplacement de la carte du jeu (M ouvre ou ferme la carte Tensho sur la même carte ; Maj + M garde la carte du jeu ; désactivable) ; r…
- Zones de quêtes et de mandats : marqueurs d'objectifs du jeu (EventMarkers, cercle quand un rayon est donné), à confirmer en jeu pour les mandats.

## 0.106.0
- Retour en jeu 0.105 : « Réduire » donnait une fenêtre minuscule ; icônes de taille fixe ; pas d'icônes de quête ni de réglages façon Mappy.
- Corrections : taille et position de la fenêtre lues hors de la colonne d'outils (dans un enfant, GetWindowSize renvoyait la colonne) + taille minimale 320×280 ; icônes à 32 px × zoom × taille choisie (10 à 64 px) et noms de lieux qui suivent le zoom.
- Quêtes : AgentMap.EventMarkers (MapMarkerData : icône, position monde, rayon, carte, info-bulle, vérifiés par réflexion) copiés toutes les 0,5 s sur le thread framework (256 au plus) ; icône et zone de l'objectif sur la carte.
- Réglages (engrenage, enregistrés aussitôt, Configuration.MapView) : icônes du jeu, noms de lieux, quêtes, zones des objectifs, drapeau, ma position ; taille des icônes 50–200 % ; opacité 20–100 % ; verrouiller ; masquer en combat ; masquer en instance (ICondition InCombat / BoundByDuty).

## 0.105.0
- Retour en jeu 0.104 : menus Région/Zone et ailes OK, mais la liste des zones n'avait que 3 entrées sur 8 (donjons absents).
- Sonde 0.105.0-zones-probe.log (Ilsabard) : le menu du jeu = cartes de même PriorityCategoryUI que la carte de région (17), triées par PriorityUI, une entrée par priorité ; les étages (MapIndex) partagent la priorité. Le Clyteum (priorité 10) figure dans les données mais pas dans le menu de l'utilisateur : probablement masqué tant qu'il n'est pas découvert (non géré, affiché ici).
- Changements : MapDefinition.MenuCategory/MenuPriority ; MapMenus.Zones (testé sur les lignes de la sonde) ; menu Zone avec entrée « -- » (carte de région) ; menu Niveau pour les cartes à plusieurs étages.

## 0.104.0
- Demande (captures des menus de la carte du jeu) : menus déroulants Région / Zone, interface réorganisée, simple symbole de vol à côté du nom.
- Disposition : menus Région (ordre des cartes region/NN, entrée Monde) et Zone (zones liées par les marqueurs de la carte de région, dans leur ordre) en haut ; colonne d'outils à gauche (remonter, suivre, ajuster, agrandir/réduire, zoom + / curseur vertical / −, info-bulles) ; carte à droite. Aile FontAwesome après le nom de zone et dans le menu des zones : colorée si le vol est débloqué, grisée …

## 0.103.0
- Demande : retirer le drapeau depuis notre carte ; outils de la carte officielle (zoom, retour région, chemin complet, vol possible ou non, agrandir).
- Sondes (0.103.0-maps-probe.log, réflexion FFXIVClientStructs) : cartes de région « region/xx » et du monde « world/00 » sans territoire ; MapMarker DataType 1 → DataKey = carte cible ; AgentMap.FlagMarkerCount, FlagMapMarkers ; PlayerState.IsAetherCurrentUnlocked / IsAetherCurrentZoneComplete ; AetherCurrentCompFlgSet par territoire ; TerritoryType.Mount.
- Ajouts : chemin cliquable Monde › Région › Zone · sous-zone ; Remonter ; curseur de zoom 15–400 % ; Ajuster (carte entière centrée) ; Agrandir (90 % de l'écran) / Réduire ; clic sur une icône liée d'une carte de région ou du monde pour y aller ; « Retirer le drapeau » dans le clic droit (FlagMarkerCount remis à 0, écrit seulement sur le thread framework) ; état du vol de la zone affichée (vol po…
- À confirmer en jeu : effet de FlagMarkerCount = 0 sur la carte et la mini-carte du jeu ; zones sans AetherCurrentCompFlgSet (vol non géré par les courants).

## 0.102.0
- Retour en jeu 0.101 (captures Radz-at-Han) : image, icônes fixes et positions conformes à la carte du jeu, thème bleu appliqué ; noms de lieux absents, drapeau non affiché, bouton « − » rendu « = ».
- Corrections : marqueurs MapMarker sans icône dessinés comme noms de lieux (grand texte, contour) ; drapeau lu dans AgentMap (FlagMarkerCount, FlagMapMarkers[0] : carte, X/Z monde) sur le thread framework et dessiné (icône 60561 supposée, triangle rouge sinon) ; bouton de zoom arrière en ASCII.

## 0.101.0
- MAP-VIEW-016, tranche 1 de docs/goals/MAP-016-PLAN.md : fenêtre « Carte Tensho » ; image de carte du jeu (ui/map/…_m.tex via ITextureProvider), zoom molette autour du curseur, déplacement à la souris, coordonnées sous le curseur, icônes fixes MapMarker (plage Map.MapMarkerRange, info-bulle du sous-titre), flèche du joueur (position et orientation copiées sur le thread framework par MapLiveReader), sui…
- Projection testée (MapProjectionTests : chemins, cohérence avec la formule des PNJ, aller-retour, zoom). À confirmer en jeu : position des icônes MapMarker (X/Y supposés en pixels de texture), sens de la flèche (rotation 0 = +Z).

## 0.100.0
- Demande : le plugin doit prendre le thème d'interface choisi dans FF14 (avant le chantier carte).
- Ajouts : UI/ThemePalettes.cs (palettes Sombre, Clair, Classic FF, Bleu/Blanc/Vert translucides ; couleurs approximées, pas des ressources du jeu ; tests de contraste), UI/GameThemeReader.Dalamud.cs (IGameConfig SystemConfigOption.ColorThemeType au démarrage et à chaque SystemChanged), FfxivTheme lit la palette active, MainWindow.Accent/Success/Muted suivent le thème, réglage « Thème de l'interfa…
- Correspondance des valeurs du jeu : 0 Sombre, 1 Clair, 2 Classic FF, 3 Bleu translucide ; 4/5 supposés Blanc/Vert translucides, à confirmer en jeu (valeur affichée dans les réglages).

## 0.99.0
- Retour en jeu 0.98 : position de la Prêtresse à la robe noire OK ; consigneurs 1/3 ; Officier de gestion du Maelstrom (Jetée, « Échange de cristaux-insignes ») absent.
- Officier : PNJ 1043099, dialogue 721735 « RegSeaPvpCommendationCrystalShop », sans argument ; boutique 1770684 (42 objets, nom resté en japonais), seule payée en cristaux-insignes (40479), seul PNJ utilisant ce dialogue (0.99.0-crystal-probe.log). Ajout à ObservedTalkShops.
- Positions : les consigneurs sont dans planner.lgb (Limsa, Gridania) ou planevent.lgb (Ul'dah). planner.lgb est maintenant lu aussi. La sonde a montré que Lumina manque de mémoire sur certains fichiers de décor (fichier vide de 72 octets ; fichier de 312 octets avec LGP1 à l'octet 32) : seuls les fichiers ayant LGB1, la bonne taille déclarée et LGP1 à l'octet 12 sont analysés (NpcShopLinks.ValidL…
- CopyRules 10 : cache PNJ reconstruit une fois. Verify.ps1 (agent) : voir 0.99.0-verify.log. Sources : TenshoAssistant-0.99.0-source.zip. Non validé en jeu.

## 0.98.0
- Retour en jeu 0.97 : la Prêtresse à la robe noire apparaît (2 800 marques de loup, icône), mais « Position non renseignée », comme beaucoup de PNJ (Consigneur, Magasinier personnel…).
- Cause : seules les lignes Level (type 8) donnaient les positions ; la plupart des PNJ sont placés par les décors des zones. Sonde 0.98.0-lgb-probe.log : 1 134 fichiers planevent.lgb, 166 434 placements EventNPC ; Prêtresse (1018655) = territoire 250, carte 51 « Jetée de l'Antre des loups », X 5,0 Y 5,3.
- Ajout : pour les vendeurs sans position, lecture de planevent.lgb (un territoire par étape, 2 ms par tick), positions ajoutées sans doublon (même carte et même point) et 8 positions au plus. Fichier absent ou illisible = pas de position supplémentaire. CopyRules 9.
- Les PNJ convoqués (magasinier personnel, consigneur) n'ont pas de position fixe : ils resteront sans position.

## 0.97.0
- Retour en jeu 0.96 : menu clic droit Tensho dans les boutiques/échanges OK.
- Marques de loup : la Prêtresse à la robe noire (PNJ 1018655, Quai des Loups) a pour seule donnée le dialogue 721280 « RegSeaPvpGaro », dont le script compilé ouvre les échanges (menu en jeu : mêlée 1, mêlée 2, distance). Sonde 0.97.0-wolf-npc-probe.log : les seules boutiques à marques de loup (type 4) qu'aucune feuille ne référence sont 1769743 (Loup doré), 1770537 (avant-garde Makai) et 1769744…
- Ajout : NpcShopLinks.ObservedTalkShops (721280 → ces trois boutiques), table d'observations en jeu documentée, fusionnée aux liens des feuilles. CopyRules 8.

## 0.96.0
- Retour en jeu 0.95 : mémoquartz OK (validé par l'utilisateur).
- Marques de loup : la sonde (0.96.0-wolf-probe.log) montre la boutique 1769743 (Visage du Loup doré, 2 000 × t4:25, CostType 0) ; aucune feuille ne la relie à un PNJ (direct, TopicSelect, PreHandler, CustomTalk, CustomTalkNestHandlers, FateShop, InclusionShop) : le jeu l'ouvre par son code. Limite connue : PNJ non affiché. La monnaie t4:25 est traduite en Marque de loup (capture en jeu de l'échan…
- Menu clic droit : en plus de l'inventaire, les menus « par défaut » (boutiques, échanges, listes d'objets) reçoivent l'entrée Tensho pour l'objet survolé à l'ouverture (IGameGui.HoveredItem). Les menus visant un personnage (ContentId, objet ou nom ciblé) sont exclus. Mêmes gardes de session (propriétaire, révision, âge).
- CopyRules 7 : cache PNJ reconstruit une fois. Verify.ps1 (agent) : 509/509, 0 avertissement/erreur (0.96.0-verify.log). Sources : TenshoAssistant-0.96.0-source.zip. Non validé en jeu.

## 0.95.0
- Retour en jeu 0.94 : assignats orange OK (300/900, 18–19 PNJ, icônes) ; mémoquartz non résolus (Casque d'abatteur du roman royal, Zircon, 495 × code 2).
- Preuve : sonde 0.95.0-shop-currency-probe.log. SpecialShop CostType sépare les familles quel que soit UseCurrencyType : CostType 2 = mémoquartz (n° 1 boutiques poétiques/Gerolt/Forges, n° 2 boutiques du mémoquartz actuel, n° 3 mnémoniques), CostType 3 = assignats (n° 2/4 mauves artisan/récolteur, n° 6/7 orange). La boutique du casque est 1770911 « Mémoquartz allagois mathématiques (guerre - NvOb…
- Traduction : (CostType 2, n°) → 28/48/49 ; (CostType 3, n°) → 33913/33914/41784/41785 ; codes directs t16:25 / t4:27 / t4:29 → marques de loup, insignes alliés, MGP. t4:25 (aucune boutique nommée) reste non résolu.
- CopyRules 6 : cache PNJ reconstruit une fois. Verify.ps1 (agent) : voir 0.95.0-verify.log. Sources : TenshoAssistant-0.95.0-source.zip. Non validé en jeu.

## 0.94.0
- Monnaies codées des boutiques spéciales : table (type, n°) limitée aux cas prouvés par deux sources (lecture en jeu de l'utilisateur et familles de boutiques de la sonde, 0.94.0-shop-currency-probe.log) : t16 n° 2/4 = assignats mauves 33913/33914 ; n° 6/7 = assignats orange 41784/41785 ; n° 25 = marques de loup ; t2 n° 2/3 et t4 n° 3 = mémoquartz mathématique 48 / mnémonique 49 ; t4 n° 27 = insi…
- Les mémoquartz suivent le mémoquartz *actuel* : la table est à revoir à chaque patch.
- NpcVendorCache.CopyRules = 5 : le cache PNJ est reconstruit une fois.

## 0.93.0
- Assignats mauves : SpecialShop type 16 n° 2 = Assignat mauve d'artisan (33913), n° 4 = Assignat mauve de récolteur (33914), d'après l'observation en jeu de l'utilisateur (comptable des assignats de Radz-at-Han, icône violette, boutique « Assignats mauves… ») et la sonde 0.92. Les autres codes du type 16 (1, 6, 7, 25) restent non résolus.
- NpcVendorCache.CopyRules = 4 : le cache PNJ est reconstruit une fois.

## 0.92.0
- Demande : assignats d'artisanat et de récolte absents de l'outil PNJ vendeurs. Indice utilisateur : « Comptable des assignats », Radz-at-Han (11.5, 9.4), deux monnaies (artisanat et récolte).
- Preuve : `tools/ShopCurrencyProbe`, une sonde en lecture seule sur le sqpack installé (journal 0.92.0-shop-currency-probe.log).
- Chaîne réelle : PNJ → PreHandler (3539xxx) → InclusionShop (3801xxx, « Échange d'assignats ») → InclusionShopCategory → InclusionShopSeries → SpecialShop (1770xxx).
- Coût du Breuvage cordial supérieur : `UseCurrencyType` 16, `ItemCost` 4, `CostType` 3. Le même code est utilisé dans des boutiques de récolte (« Appât et autres ») et mixtes.

## 0.91.0
- Écart reconnu : de 0.76 à 0.90, seules des notes ont été écrites ; ni archive des sources ni journal n'ont été déposés dans checkpoints/ au fil de l'eau. Signalé par l'utilisateur, rattrapé le 2026-10-30 vers 20:50.
- Journaux Verify.ps1 conservés pour chaque version : 0.76.0-verify.log … 0.91.0-verify.log (exécutions de l'agent sur le PC). Tous verts : 477 → 509 tests, 0 avertissement/erreur, langues et paquet/SHA-256 passants.
- DLL canonique : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll (0.91.0.0).
- Validé en jeu (rapports/captures utilisateur) : tri natif et zones 0.77–0.80, cache des objets 0.81, survol 0.85–0.87 (accroche, stock, moins cher, monstres wiki, icône Gil), outil PNJ vendeurs pour gils, espoilles et sceaux (0.91). Non validés : 0.82 recettes, 0.83 récolte, 0.84 critères de tri, clic droit inventaire → PNJ, mise en page PNJ 0.90 ; assignats non pris en charge.

## 0.76.0
- Tranche : QUEST-ITEM-PROGRESS-007 (progression du même relevé jointe aux sources de quête d'un objet).
- Ensuite : csproj et manifeste portés à 0.76.0.0 pour identifier la livraison. Recompilation requise (.\scripts\Verify.ps1) avant l'essai en jeu.
- Code écrit depuis une session cloud sans SDK ; la seule preuve de build/tests est la sortie utilisateur.
- 18:54 : Verify.ps1 relancé par l'agent sur le PC (session desktop) : DLL bin\x64\Release 0.76.0.0, 477/477, 0 avertissement/erreur, langues et paquet verts.

## 0.75.0
- Demande : cache partagé, durées par domaine dans les réglages et actualisation explicite des outils.
- ConfigSerializationProbe : Newtonsoft None/Auto/Objects passants, durées de cache sauvegardées/restaurées. Journal : 0.75.0-config-json-probe.log.
- DLL canonique : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll.
- Cache nouveau non validé en jeu ; aucune validation différée de Macro Chain/projection revendiquée. Les outils restent PARTIAL et l'ancien objectif autonome reste en pause.

## 0.74.0

## 0.72.0

## 0.71.0

## 0.70.0

## 0.69.0

## 0.68.0
- Huit groupes index/lifecycle : variantes et IDs, couverture, copies, bornes/annulation, worker réel, réponses tardives, frontières de fin de vie et erreurs isolées.
- Probe de production : 3 496 Items/8 288 associations, tous ensembles et variantes comparés à la projection directe 5 373 quêtes ; Potion = 25 candidates. Quinze références/quatre quantités inconnues préservées, aucune progression hors personnage.
- Lien Usages → fiche de quête, cache de fiche corrigé ; aucun rendu/clic natif en jeu déclaré.
- DLL C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll, version 0.68.0.0.

## 0.67.0
- QuestRewardsProbe production : 5 373 quêtes, 9 313 lignes/variantes, identités Item/teinte résolues ; 4 quantités et 15 monnaies non résolues gardées explicitement.
- Cinq groupes supplémentaires : alternatives/slots/qualité, inconnus, monnaie par type, propriété/provenance/owner et bornes.
- Sept slots fixes et quatre slots de métier natifs couverts ; les anciennes constantes six/trois ne sont pas transplantées. Tous les modes présents traités sans choisir les conditions du personnage.
- GatheringProbe recompilé ; aucun rendu/choix/récompense en jeu observé.

## 0.66.0
- Cinq groupes de preuve d’acquisition, couverture stable, propriétaire, contradiction atomique et récompenses distinctes.
- CollectionProbe : 885 titres/530 variantes de genre ; 4 003 succès/83 catégories ; 192 récompenses Item et 891 références Title, identités/icônes résolues, jointures étrangères refusées. Orchestrion antérieur toujours passant.
- API et source primaire chargement/getters/signal Unlock inspectées ; aucun état réel de joueur ni callback en jeu déclaré validé.
- DLL canonique C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll, version 0.66.0.0.

## 0.65.0
- CollectionProbe natif : 886 morceaux, 12 catégories, 886 liens Item, tous ItemId ≠ OrchestrionId ; UI/catégories étrangères refusées et icônes présentes.
- Quatre groupes managés : identités/index multiples, copies, propriétaire/confirmation, erreurs de catégorie isolées.
- API IUnlockState et source primaire vérifiées ; aucune acquisition réelle ni rendu en jeu déclarés.
- Lien Objet/Usages ↔ fiche d’orchestrion ↔ stocks/contexte commun ; aucun réseau ou lecteur supplémentaire.

## 0.64.0
- Sept groupes supplémentaires, dont oracle indépendant d’environ 5 000 dates, frontières/minuit, météo précédente, inconnus/alternatives, bornes/copies/annulation.
- Probe réel 8753, fixture UTC 12:00 : critères météo + précédente seulement, plage 13:13:20–13:36:40, horaire absent explicitement.
- Probe réel 4915 : horaire seulement, quatre plages sur l’horizon de douze périodes ; critères météo absents explicitement.
- DLL canonique C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll, version 0.64.0.0.

## 0.63.0
- Quatre groupes pour alias exacts, portée/ambiguïtés, taux/métadonnées et copies.
- WeatherProbe : 214 lignes Weather, 43 noms anglais globalement ambigus ; correspondances par territoire et icônes vérifiées. Tables/prévisions antérieures passantes, couverture 90/100 toujours inconnue pour les tirages manquants.
- GatheringProbe réel 8753 : spot 107/territoire 137, Clear Skies → 1, Rain → 7, Showers → 8, noms/icônes français, trois appâts/intermédiaires natifs.
- La tentative globale a été refusée par preuve de doublons ; choix explicite de la portée du territoire, pas de premier résultat arbitraire.

## 0.62.0
- Cinq groupes : oracle 600 horaires, UTC/minuit, inconnus/vides, météo précédente et copies, erreurs isolées.
- Probes réels : 7685 horaire absent préservé ; 4915 Mahi-Mahi 10–18 ET, fixture UTC 00:29:10–00:52:30 à Unix ; 8753 Clear Skies précédé de Rain/Showers, trois appâts/intermédiaires natifs. Points natifs tous résolus.
- FashionProbe compilé sans avertissement/erreur ; lecteurs/services/transport communs conservés.
- DLL canonique C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll, version 0.62.0.0.

## 0.61.0
- Sept groupes de pêche communautaire dont cache/transport commun, annulation, inconnus, bornes et places typées.
- GatheringProbe --fish-baits=4869 : 19 variantes, 19 FishingSpot natifs, deux appâts/intermédiaires Item résolus ; requête réelle explicite et cache d’objet partagé.
- Source de génération et licence MIT consultées, commit 04cadd2e1e0de86c20aa9303faa082c7971f8d8b. Pas de code/base externe importé.
- Code météo 2 : divergence des sources, 491 fiches natives restent inconnues. Pas de disponibilité actuelle déduite.

## 0.60.0
- FishingNotebookProbe : 2 461 fiches/Item distincts et notes concordantes, 296 tomes, aucun lien Item/spot non résolu ; 491 codes météo non interprétés conservés inconnus. Notes absentes/étrangères rejetées.
- GatheringProbe : contrôles natifs antérieurs passants, 4 199 sources, pas d’option réseau.
- Source primaire GatherBuddy épinglée et métadonnées locales inspectées ; aucune base externe d’appâts importée.
- DLL : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll, version 0.60.0.0.

## 0.59.0
- ConfigSerializationProbe : Newtonsoft None/Auto/Objects, bibliothèque et profils imbriqués passants.
- Bibliothèque sans identifiants de personnage/servant ou observations ; géométrie strictement compatible, chargement en brouillon, pas de requête native implicite.
- DLL canonique : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll ; version 0.59.0.0.
- Seule autre copie sous bin : archive legacy-Release-20261027 0.5.22.0, laissée intacte ; aucun ancien bin/Release actif.

## 0.59.0

## 0.58.0
- ConfigSerializationProbe : Newtonsoft None/Auto/Objects avec filtres et masque combiné passants.
- FashionProbe natif sans réseau : 269 indices, 20 238 Wearable, 7 804 MainHand, 984 OffHand, 31 SoulCrystal, 51 vêtements bloquant Head ; OffHand négatifs exclus.
- ApiProbe : nouveaux champs EquipSlotCategory confirmés ; loader et probe utilisent le même helper de masque.
- Métadonnées absentes ou étrangères inconnues, ne satisfaisant ni Oui ni Non. Aucune requête marché liée aux filtres.

## 0.57.0
- Cinq groupes nouveaux, dont 20 scènes multi-scopes avec oracles de positions protégées, trous physiques et quantités.
- ConfigSerializationProbe : Newtonsoft None/Auto/Objects, zones protégées/inversées et v1 passants.
- Aucun nouveau lecteur ou changement du bridge d’écriture ; contrôle des positions par le modèle partagé.
- DLL canonique : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll.

## 0.56.0
- Huit groupes nouveaux ; 24 scènes multi-scopes vérifient piles/coordonnées/trous et quantité totale indépendante.
- ConfigSerializationProbe : Newtonsoft None/Auto/Objects, critères/zones imbriqués et configuration v1 passants.
- Catégories du jeu dans le catalogue Item partagé ; profils par personnage/rangement et critères multiples ; positions explicites conservées dans le plan natif.
- DLL canonique : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll.

## 0.55.0
- Six groupes nouveaux dont 144 scénarios couvrent sacoches/servant, géométrie, propriétaires, permutations, reçus directs, cache refusé, fraîcheur et lifecycle.
- Lecture manuelle sans import obligatoire ; application revalidée sur rangement visible et chargé. Banque persistante inchangée sans import explicite.
- DLL : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll.
- Capture/apparition des fenêtres/écriture/rendu/persistance natifs NOT VALIDATED_IN_GAME. Tests craft/Macro Chain différés.

## 0.54.0
- 48 scénarios de permutation couvrent remplissage, critères et sens ; mutations de quantité/qualité/position/ordre et lifecycle refusés.
- Source SortaKinda épinglée et licence lues ; SDK local inspecté avant écriture du bridge.
- DLL canonique : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll.
- Application explicite de l’ordre visuel des quatre sacs ; aucun InventoryItem physique modifié par ce code.

## 0.53.0
- ConfigSerializationProbe : Newtonsoft None/Auto/Objects passants, option menu conservée et ancien v1 désactivé.
- SDK local API 15 et sources primaires Dalamud inspectés ; aucun callback/rendu natif observé.
- DLL : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll.
- Menu Inventory optionnel, cinq routes, revalidation au Draw, aucune requête de prix.

## 0.52.0
- ConfigSerializationProbe : modes None/Auto/Objects, options indépendantes et anciennes configurations v1 passants.
- GatheringProbe : contrôles des fichiers natifs passants.
- DLL canonique : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll.
- Survol des usages et des prix mutualisé ; usages seuls sans réseau, scope et annulation des prix testés avec transport simulé.

## 0.51.0

## 0.50.0

## 0.50.0

## 0.49.0

## 0.49.0

## 0.48.0

## 0.47.0

## 0.46.0

## 0.45.0

## 0.44.0

## 0.43.0

## 0.42.0

## 0.41.0

## 0.40.0

## 0.39.0

## 0.38.0

## 0.37.0

## 0.35.0

## 0.34.0

## 0.33.0

## 0.32.0

## 0.31.1

## 0.31.0

## 0.30.1

## 0.30.0

## 0.29.0

## 0.28.0

## 0.27.0

## 0.26.0

## 0.25.0

## 0.24.0

## 0.23.0

## 0.22.0

## 0.21.0

## 0.20.0

## 0.17.1
