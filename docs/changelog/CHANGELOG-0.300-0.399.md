# Journal des versions / Changelog, 0.300 à 0.399

Archive de [CHANGELOG.md](../../CHANGELOG.md) : versions plus anciennes que la dernière release, déplacées par `tools/changelog.py archive`. Même format (français, puis `### English` depuis 0.219.0) ; liens relatifs à la racine réécrits depuis ce dossier.

## 0.399.0
- Demande de l'utilisateur : un réglage pour se téléporter dès le clic sur « Y aller ».
- Réglages › Modules › Récolte : « « Y aller » : téléporter tout de suite » (désactivé par défaut, avec ↺). Coché, un clic sur
  « Y aller » pose le drapeau et téléporte directement vers l'éthérite la plus proche, sans la question ; si le coût de cette éthérite
  n'est pas connu (liste de téléportation pas lue, éthérite non harmonisée), la question habituelle revient.
- Le clic sur « Y aller » reste le seul déclencheur ; le bouton « Éthérite » des liens du chat garde sa question.
- Les tests de 0.373 et 0.397 vérifient maintenant que la téléportation directe n'existe que pour cette demande, avec un coût connu.
- Verify.ps1 (agent) : 742/742, 0 avertissement, 0 erreur ; DLL 0.399.0.0. Non validé en jeu.
### English
- User request: an optional setting (off by default) makes « Go there » teleport at once to the nearest attuned aetheryte, without
  the question.

## 0.398.0
- Étape g), domaine 4 (Récolte), R-04 : appât en main dans l'aide à la pêche.
- En haut du tableau des poissons du lieu : « Appât en main », avec son icône et son stock, et le nombre de poissons du lieu
  possibles avec cet appât ; ces poissons ont un cadre vert autour de leur premier appât.
- Lecture (Gathering/FishingBaitReader) : PlayerState.FishingBait, toutes les 0,5 s sur le thread du jeu, pêcheur seulement ; champ lu
  sans fonction du jeu ; sens de la valeur à confirmer en jeu (docs/evidence/0.398-fishing-bait.md).
- Tests : FishingBait398Tests (1 cas). Verify.ps1 (agent) : 742/742, 0 avertissement, 0 erreur ; DLL 0.398.0.0. Non validé en jeu.
### English
- Queue step g, R-04: the fishing help shows the bait in hand with its stock and frames the fish it can catch at this spot.

## 0.397.0
- Étape g), domaine 4 (Récolte), R-03 : bouton « Y aller ».
- Fiche d'un objet de récolte, chaque lieu (récolte et pêche) : « Y aller » pose le drapeau du jeu sur le point, ouvre la carte Tensho
  centrée dessus et propose l'éthérite la plus proche de la carte (parmi votre liste de téléportation) avec son coût ; la téléportation
  se fait seulement sur « Oui ». Grisé quand la position du point n'est pas connue.
- Même mécanique que l'éthérite des liens du chat (0.373), généralisée à n'importe quel point.
- Tests : GoThere397Tests (1 cas). Verify.ps1 (agent) : 741/741, 0 avertissement, 0 erreur ; DLL 0.397.0.0. Non validé en jeu.
### English
- Queue step g, R-03: « Go there » on each gathering and fishing spot: flag, Tensho map, nearest aetheryte with its cost (teleport on
  confirmation only).

## 0.396.0
- Étape g), domaine 4 (Récolte), R-02 : prochaines fenêtres réelles des poissons.
- Fiche d'un poisson (données GatherBuddy) : nouvelle section « Prochaines fenêtres » — les 5 prochaines heures réelles où la plage ET,
  la météo et la météo précédente sont réunies en même temps, dans la zone du premier lieu de pêche, cherchées jusqu'à 35 jours devant ;
  pour chacune : début, fin, durée, compte à rebours (« ouverte maintenant ») et alarme unique.
- Calcul (Gathering/FishWindows) depuis les identifiants de météo de GatherBuddy et les tables météo du jeu ; une fenêtre à cheval sur
  minuit ou sur deux périodes est réunie ; une météo impossible à prévoir donne « inconnu », jamais « aucune fenêtre ».
- Tests : FishWindows396Tests (2 cas, chaque fenêtre vérifiée contre les tables). Verify.ps1 (agent) : 740/740, 0 avertissement,
  0 erreur ; DLL 0.396.0.0. Non validé en jeu.
### English
- Queue step g, R-02: a fish sheet now lists its next real windows (ET time, weather and previous weather together, up to 35 days ahead)
  with countdowns and one-off alarms.

## 0.395.0
- Demandes de l'utilisateur : le tri par niveau doit porter sur tous les sorts, pas seulement sur la fiche, avec un cache à l'ouverture
  de la page ; puis « ça prend 6 min ? ».
- Page Mage bleu : choix de l'ordre de la liste — numéro du sort, niveau du monstre croissant ou décroissant ; chaque sort montre le
  niveau de son monstre (« niv. 46 ») ; les sorts sans monstre (par défaut, quête…) restent à la fin.
- Source : le tableau « Blue Magic Spellbook » du wiki, une seule page pour les 124 sorts (au lieu de 124 pages, environ 6 minutes à
  3 secondes d'écart, version écartée avant d'être livrée) ; lu à l'ouverture de la page, gardé sur disque selon la durée de cache des
  sources, interrupteur du wiki respecté ; un échec réessaie au bout d'une minute.
- Niveau du monstre = le plus bas entre monstre de zone et mission, comme le tableau du wiki.
- Preuve : docs/evidence/0.395-blue-spellbook.md.
- Tests : BlueSpellIndex395Tests (1 cas). Verify.ps1 (agent) : 738/738, 0 avertissement, 0 erreur ; DLL 0.395.0.0. Non validé en jeu.
### English
- User request: the whole blue magic list can be sorted by the teaching monster's level, from the wiki's single spellbook table read
  once when the page opens (cached), instead of one page per spell.

## 0.394.0
- Demande de l'utilisateur : un réglage pour changer les polices.
- Réglages › Interface › Polices : famille et taille (10 à 40 pixels) des titres de pages, des titres de sections (et catégories du
  menu) et du texte de la fenêtre Tensho, chacun avec ↺.
  - Familles : les polices du jeu fournies par Dalamud (Axis, Jupiter, Meidinger, MiedingerMid, TrumpGothic) ; pour le texte, aussi la
    police de Dalamud (par défaut).
  - Défauts inchangés : TrumpGothic 24 et 19, police de Dalamud pour le texte.
- Les polices sont reconstruites au début de l'image suivante quand un réglage change (UI/FontManager), sinon rien n'est refait ;
  « Restaurer tous les réglages » les remet par défaut.
- Tests : Fonts394Tests (1 cas). Verify.ps1 (agent) : 737/737, 0 avertissement, 0 erreur ; DLL 0.394.0.0. Non validé en jeu.
### English
- User request: Settings › Interface › Fonts — family and size of page titles, section titles and text (game fonts, or Dalamud's for
  the text), applied at once.

## 0.393.0
- Deux signalements de l'utilisateur sur la page de 0.364.
- Nom : en français, le jeu l'appelle « Bestiaire » (« chasse » désigne les contrats de chasse). Le menu, le titre et les textes disent
  maintenant « Bestiaire ».
- Grande compagnie : la page proposait les carnets des trois compagnies. Seul celui de la compagnie du personnage est maintenant montré
  (lu dans le jeu avec les carnets, PlayerState.GrandCompany), et choisi à l'ouverture ; aucun sans compagnie ; les trois tant qu'elle
  n'est pas lue. Comme le jeu et Hunty (MIT, lu seulement : il ne montre que la compagnie actuelle).
- Tests : HuntingLogCompany393Tests (1 cas). Verify.ps1 (agent) : 736/736, 0 avertissement, 0 erreur ; DLL 0.393.0.0. Non validé en jeu.
### English
- Fix: the French hunting log page is now called « Bestiaire », and it shows only the character's own Grand Company log.

## 0.392.0
- Étape g), domaine 4 (Récolte), R-01 : calendrier de la pêche en mer. Choix de l'utilisateur : onglet « Pêche en mer » (fait en page
  à part dans Production, à côté de Récolte).
- Prochains départs (toutes les 2 heures) : heure, compte à rebours, « Embarquement ouvert » pendant 15 minutes, trajet depuis Limsa
  Lominsa (Indigo) et depuis Kugane (Rubis), nommés comme dans le jeu avec leur moment ; survol : les trois escales et leur moment.
- Filtre sur un trajet (prochains départs de ce trajet, depuis l'un ou l'autre port) ; alarme Tensho avant un départ.
- Horaires calculés depuis la boucle du jeu (IKDRouteTable, 144 départs) ; début de boucle et sens des moments lus dans GatherBuddy
  (Apache-2.0, faits seulement, code original).
- Preuve : docs/evidence/0.392-ocean-fishing.md (sonde PROBE_OCEAN).
- Tests : OceanFishing392Tests (1 cas). Verify.ps1 (agent) : 735/735, 0 avertissement, 0 erreur ; DLL 0.392.0.0. Non validé en jeu.
### English
- Queue step g, R-01: new Ocean fishing page — next departures, Indigo and Ruby voyages with their stops, countdown, alarm.

## 0.391.0
- Étape g), domaine 3 (Monde), X-06 : barre du tic de régénération, pour les PR des récolteurs.
- Barre à l'écran (désactivée par défaut, interrupteur en haut de la page PR) : PR actuels, maximum et temps avant le prochain tic
  (toutes les 3 secondes). Mineur, botaniste et pêcheur seulement ; cachée en combat et à PR pleins ; déplaçable.
- Le tic est repéré quand les PR montent sans être pleins ; un maximum qui monte (équipement changé) à PR pleins n'est pas un tic
  (trouvé par le test). PV et PM laissés de côté : le combat est hors périmètre (DEC-009).
- Code original (idée de Tick Tracker, AGPL, lu seulement).
- Tests : RegenTick391Tests (1 cas). Verify.ps1 (agent) : 734/734, 0 avertissement, 0 erreur ; DLL 0.391.0.0. Non validé en jeu.
### English
- Queue step g, X-06: an optional on-screen GP tick bar for gatherers (time to the next 3-second regeneration tick); off by default.

## 0.390.0
- Demande de l'utilisateur : trier par niveau les monstres d'un sort dans le grimoire du mage bleu.
- Fiche d'un sort, liste « Monstres qui l'enseignent » : choix de l'ordre — niveau croissant (par défaut), niveau décroissant ou ordre
  du wiki. Une plage « 43-48 » compte pour 43 ; un monstre sans niveau reste à la fin.
- Tests : BlueMonsterOrder390Tests (1 cas). Verify.ps1 (agent) : 733/733, 0 avertissement, 0 erreur ; DLL 0.390.0.0. Non validé en jeu.
### English
- User request: the monsters that teach a blue magic spell can be sorted by level (lowest or highest first) or kept in wiki order.

## 0.389.0
- Bouton « Trier » de la fenêtre d'inventaire : l'icône de tri, qui était dessinée à côté et ne faisait rien au clic, est maintenant dans
  le bouton (choix de l'utilisateur) : tout « ⇅ Trier » est cliquable.
- Verify.ps1 (agent) : 732/732, 0 avertissement, 0 erreur ; DLL 0.389.0.0. Non validé en jeu.
### English
- The inventory « Sort » button now carries its icon inside, so the whole button is clickable (the icon beside it did nothing).

## 0.388.0
- Demande de l'utilisateur : un bouton dans la fenêtre d'inventaire du jeu pour appliquer les règles de tri, « petit mais pas trop »,
  en bas à gauche sur la même ligne que les gils.
- Bouton « Trier » (avec son icône) dessiné dans la fenêtre d'inventaire du jeu, quelle que soit sa disposition (normale, grande,
  étendue), à son échelle ; un clic applique vos règles enregistrées de « Mes sacs », exactement comme « Appliquer » sur la page
  Rangements › Tri (seul l'ordre affiché change, piles et quantités gardées) ; un message court s'affiche à côté pendant 6 secondes.
- Rien ne se fait sans le clic. Position de la fenêtre copiée sur le thread du jeu (Inventory/InventoryWindowReader).
- Interrupteur « Bouton « Trier » sur la fenêtre d'inventaire du jeu » en haut de la page Tri, activé par défaut, avec ↺.
- Premier essai en jeu de l'utilisateur : bouton trop bas, sous la fenêtre (le cadre de la fenêtre du jeu dépasse sa partie visible).
  Position relevée en deux essais (57 puis 42 unités au-dessus du bas, sur la ligne des gils ; 24 depuis la gauche) et rendue réglable : « Bouton : depuis la gauche » et « Bouton :
  depuis le bas » sous l'interrupteur, avec ↺ ; elles suivent l'échelle de la fenêtre.
- Tests : SortButton388Tests (1 cas). Verify.ps1 (agent) : 732/732, 0 avertissement, 0 erreur ; DLL 0.388.0.0. Non validé en jeu
  (placement sur la ligne des gils à confirmer).
### English
- User request: a « Sort » button inside the game's inventory window (bottom left, on the gil line) applies your saved bag sorting
  rules on click, like « Apply » on the Sort page; on by default, switch at the top of that page.

## 0.387.0
- Deux signalements de l'utilisateur corrigés.
- Grimoire du mage bleu : « Chercher sur le wiki » ne donnait jamais de monstre.
  - Cause : le nom anglais du sort passait par le lien AozAction → Action, résolu dans la langue du client ; le wiki recevait
    « Mauvaise_haleine », répondait « page inexistante », et Tensho disait « aucun monstre ».
  - Correction : nom pris dans la feuille Action anglaise par numéro de ligne ; nouveau cache disque pour ne pas relire les réponses
    vides ; une page absente est maintenant dite comme telle, avec son adresse.
  - Relevé : la page « Bad Breath » du wiki donne 19 monstres, lus par les règles de Tensho.
  - Autres plugins regardés, comme demandé : Blue Mage Helper n'a toujours pas de licence (vérifié le 2026-11-03 ; table de sorts en
    lecture seule), BeastieBuddy est sous AGPL : le wiki reste la source, sur clic.
- Collections, onglets de 0.365 : pas d'infobulle « obtention ».
  - Coiffures, Accessoires de mode, Bardes et Livres de maître ont maintenant l'objet qui les apprend (données du jeu : objet de la
    coiffure, ItemAction 20086 et 1013 avec Data[0], objet du livre) : survol = infobulle de l'objet avec son obtention, clic droit =
    sources.
  - Lunettes, Portraits, Objets de collection et Archives occultes : les données du jeu ne relient aucun objet (sonde) ; l'infobulle le
    dit au lieu d'être vide.
  - Tenues : survol d'une tenue ou d'une pièce = infobulle de l'objet.
- Preuves : docs/evidence/0.387-blue-spell-name.md, docs/evidence/0.387-collection-items.md (sonde PROBE_UNLOCKS).
- Le test de 0.367 suit le nouveau nom du cache.
- Tests : BlueSpellName387Tests, CollectionItems387Tests (1 cas chacun). Verify.ps1 (agent) : 731/731, 0 avertissement, 0 erreur ;
  DLL 0.387.0.0. Non validé en jeu.
### English
- Fix: the blue magic spell window never found the teaching monsters (the French spell name was sent to the wiki); it now uses the
  English Action sheet, drops the old cached answers and says when the wiki has no page.
- Fix: the collection tabs added in 0.365 had no « how to obtain » tooltip; hairstyles, accessories, bardings and master books now
  show their teaching item, the others say it is unknown, outfits show the item tooltip.

## 0.386.0
- Étape g), domaine 3 (Monde), X-10 : horloges sur plusieurs fuseaux et alarmes à la main.
- Alarmes, nouvelle section « Horloges » : l'heure de fuseaux horaires choisis (6 au plus, liste des fuseaux de Windows avec recherche),
  chacun retirable.
- Alarme unique à une date réelle : à côté de l'alarme quotidienne ET, une date « AAAA-MM-JJ HH:MM » à l'heure du PC (dans l'année qui
  vient) et « Ajouter à cette date » ; le nom tapé pour l'alarme ET sert aussi ; groupe « Alarmes à date ».
- Tests : WorldClocks386Tests (1 cas). Verify.ps1 (agent) : 729/729, 0 avertissement, 0 erreur ; DLL 0.386.0.0. Non validé en jeu.
### English
- Queue step g, X-10: clocks for up to 6 chosen time zones on the Alarms page, and a one-off alarm at a typed local date.

## 0.385.0
- Étape g), domaine 3 (Monde), X-08 : job au hasard pour les quotidiens.
- Équipement › Ensembles d'équipement du jeu : bouton « Job au hasard » (un tirage par job, son ensemble de plus petit numéro) et case
  « Seulement les jobs pas au niveau le plus haut » (cochée) ; le job tiré s'affiche avec son ensemble.
- « Équiper » porte l'ensemble tiré, sur ce clic seulement, par la fonction du jeu EquipGearset (appelée seulement si son adresse est
  résolue et l'ensemble valide, 2 secondes d'écart) ; rien n'est changé dans les ensembles.
- Le test de 0.383 vérifie maintenant qu'aucune fonction qui modifie un ensemble n'est utilisée (mise à jour, suppression, nom).
- Tests : RandomJob385Tests (1 cas). Verify.ps1 (agent) : 728/728, 0 avertissement, 0 erreur ; DLL 0.385.0.0. Non validé en jeu.
### English
- Queue step g, X-08: « Random job » among your gear sets (one draw per job, optionally only jobs below the top level).
  - « Equip » wears it on that click only, through the game's own gear set function.

## 0.384.0
- Étape g), domaine 3 (Monde), M-14 : emplacement de la carte au trésor déchiffrée.
- Carte Tensho : une gemme à l'endroit de la carte au trésor que vous avez déchiffrée (avec la couche des coffres au trésor) ;
  survol : son nom ; clic : drapeau du jeu.
- Rappels, ligne « Cartes au trésor » : bouton « Voir sur la carte », qui ouvre la carte Tensho sur l'emplacement et y pose le drapeau.
- Lecture (Maps/TreasureMapReader) : toutes les 5 secondes, fonctions du jeu GetTreasureHuntRank et GetTreasureSpotSubKey appelées
  seulement si leur adresse est résolue ; emplacement tiré des données du jeu (TreasureSpot puis Level).
- Preuve : docs/evidence/0.384-treasure-map.md.
- Tests : TreasureMap384Tests (1 cas). Verify.ps1 (agent) : 727/727, 0 avertissement, 0 erreur ; DLL 0.384.0.0. Non validé en jeu.
### English
- Queue step g, M-14: the deciphered treasure map's spot on the Tensho map (click = flag), and « Show on the map » in Reminders.

## 0.383.0
- Étape g), domaine 3 (Monde), M-12 : ensembles d'équipement du jeu. Choix de l'utilisateur : partage par le code TENSHO-GEAR1
  seulement, aucun site tiers.
- Équipement, nouvelle section « Ensembles d'équipement du jeu » : chaque ensemble (numéro, nom, job, niveau d'objet), groupé par job.
  - Déplié : pour chaque emplacement, une pièce que vous possédez déjà (sacs, arsenal, sacoches, servants enregistrés) avec un meilleur
    niveau d'objet, que le job peut porter à son niveau ; le gain en niveaux d'objet.
  - « Copier le code » : l'ensemble en code TENSHO-GEAR1 (pièces et matérias), le même format que les plans de la page.
- Lecture (Gear/GameGearsetReader) : champs des ensembles et niveaux des jobs, seulement pendant que la page les montre ; aucune
  fonction du jeu appelée, aucun ensemble changé.
- Preuve : docs/evidence/0.383-gearsets.md.
- Tests : Gearsets383Tests (2 cas). Verify.ps1 (agent) : 726/726, 0 avertissement, 0 erreur ; DLL 0.383.0.0. Non validé en jeu.
### English
- Queue step g, M-12: the game's gear sets on the Gear page, with the better pieces you already own for each set's job.
  - Each set copies as a TENSHO-GEAR1 code; Tensho never changes a set.

## 0.382.0
- Étape g), domaine 3 (Monde), M-11, en partie : autres compteurs.
- Rappels, deux lignes de plus (remise hebdomadaire) :
  - « Carnet d'objectifs » : défis faits sur le total, lus dans le jeu une fois le carnet ouvert (le jeu ne le charge qu'alors) ;
  - « Enclave de Doman » : dons de la semaine sur le plafond, lus après un passage à l'enclave.
- Déjà là : escouade (0.333), mémoquartz plafonnés (onglet Monnaies, 0.344). Laissés de côté, rien de lisible trouvé : Mini et Jumbo
  Cactpot, objectifs hebdomadaires du Carnaval masqué.
- Lectures de champs seulement (aucune fonction du jeu appelée), protégées à part.
- Preuve : docs/evidence/0.382-counters.md. Le test de 0.356 suit le nouvel appel des compteurs.
- Tests : Counters382Tests (1 cas). Verify.ps1 (agent) : 724/724, 0 avertissement, 0 erreur ; DLL 0.382.0.0. Non validé en jeu.
### English
- Queue step g, M-11 (part): Reminders show the challenge log (once opened) and the Doman Enclave donations of the week.
  - Mini / Jumbo Cactpot and Masked Carnivale weekly targets are not readable yet.

## 0.381.0
- Étape g), domaine 3 (Monde), X-01 : liste de tâches libre.
- Vos tâches (Rappels) : nouveau type « Une fois », une tâche libre qui reste faite jusqu'à ce qu'on la retire.
- Nouvelle commande « /tensho todo <tâche> » (ou « tache ») : ajoute une tâche « Une fois » en fin de liste et le dit dans le chat ;
  refusée avec un message si le nom est vide ou trop long, ou si la liste est pleine.
- Tests : FreeTasks381Tests (1 cas). Verify.ps1 (agent) : 723/723, 0 avertissement, 0 erreur ; DLL 0.381.0.0. Non validé en jeu.
### English
- Queue step g, X-01: free « Once » tasks in Reminders › Your tasks, and « /tensho todo <task> » to add one from the chat.

## 0.380.0
- Étape g), domaine 3 (Monde), M-09, en partie : paliers de statistiques. Choix de l'utilisateur : gardé comme information sur son
  propre personnage (DEC-009).
- Tableau des valeurs réelles (page Équipement, panneau près de la fenêtre Personnage) : nouvelle colonne « +N palier », les points
  encore nécessaires pour que la valeur passe à la suivante ; l'aide au survol dit aussi les points au-delà du palier actuel qui ne
  changent rien.
- Calcul exact sur les formules déjà utilisées (aucune nouvelle donnée) ; temps de recharge global cherché pas à pas.
- Non fait : statistiques en contenu synchronisé (fonctions du jeu à sonder en jeu).
- Tests : StatTiers380Tests (2 cas, vérifiés pas à pas contre les valeurs affichées). Verify.ps1 (agent) : 722/722, 0 avertissement,
  0 erreur ; DLL 0.380.0.0. Non validé en jeu.
### English
- Queue step g, M-09 (part): the real-value table shows the points to the next stat tier, with the wasted points in its help.
  - Item-level sync stats are not done (game functions to probe in game).

## 0.379.0
- Étape g), domaine 3 (Monde), M-10 : historique du butin des vaisseaux. Choix de l'utilisateur : 180 jours, affiché à part de la
  moyenne communautaire, export.
- Compagnie libre, nouvel onglet « Historique » :
  - chaque voyage lu à l'atelier avec son butin (un enregistrement par voyage, mis à jour tant que c'est le même voyage) ;
  - « Rendement réel par trajet » : nombre de voyages et objets rapportés en moyenne par voyage, sans estimation ;
  - « Derniers voyages » : les 50 derniers ;
  - « Copier en CSV » : tout l'historique dans le presse-papiers, une ligne par objet (champs protégés contre les formules des tableurs).
- Un fichier local (VoyageHistory.json), 180 jours et 5 000 voyages au plus ; un fichier abîmé n'est jamais réécrit.
- Code original (idées de Submarine Tracker, MIT, lu seulement).
- Tests : VoyageHistory379Tests (3 cas). Verify.ps1 (agent) : 720/720, 0 avertissement, 0 erreur ; DLL 0.379.0.0. Non validé en jeu.
### English
- Queue step g, M-10: Free company › History keeps each voyage's loot read in the workshop (180 days).
  - Real yield per route (average per voyage) and a CSV copy to the clipboard.

## 0.378.0
- Étape g), domaine 3 (Monde), M-08 : favoris dessinés sur la carte Tensho, avec note, groupe et partage.
- Carte Tensho :
  - nouvelle couche « Vos favoris » (activée) : une étoile par favori de la carte affichée ; survol : nom, groupe, coordonnées et
    note ; clic : drapeau du jeu sur le favori ;
  - clic droit sur la carte : « Ajouter aux favoris », avec un nom (vide : la zone et les coordonnées).
- Page Cartes :
  - un favori peut avoir un groupe (montré entre crochets) et une note (au survol) ; les favoris d'avant se chargent tels quels ;
  - « Copier le code de partage » met tous vos favoris (200 au plus) dans le presse-papiers sous la forme d'un code TENSHO-MAP1 ;
    « Importer » ajoute ceux d'un code collé qui ne sont pas déjà là. Aucun site tiers.
- Tests : MapBookmarks378Tests (3 cas). Verify.ps1 (agent) : 717/717, 0 avertissement, 0 erreur ; DLL 0.378.0.0. Non validé en jeu.
### English
- Queue step g, M-08: bookmarks drawn as stars on the Tensho map (click = flag), added from the map's right-click menu.
  - Optional note and group; shared as a TENSHO-MAP1 text code (clipboard), imported without duplicates.

## 0.377.0
- Étape g), domaine 3 (Monde), M-07 : comparaison avec la pièce portée au survol d'un objet.
- Sous l'infobulle d'une pièce d'équipement (inventaire, marché, partout où le panneau Tensho s'affiche), en tête du panneau :
  « Comparé à l'équipement porté » : niveau d'objet et statistiques en plus (vert) ou en moins (rouge) que la pièce portée au même
  emplacement ; une bague est comparée aux deux bagues portées.
- Valeurs de base, NQ ou HQ selon l'objet, sans matérias des deux côtés (celles de l'objet survolé ne sont pas connues).
- Nouvelle case « Comparaison avec la pièce portée » dans les parties du panneau (réglages, activée par défaut).
- Code original (idée de SimpleCompare, MIT, lu seulement).
- Tests : GearCompare377Tests (2 cas). Verify.ps1 (agent) : 714/714, 0 avertissement, 0 erreur ; DLL 0.377.0.0. Non validé en jeu.
### English
- Queue step g, M-07: under a gear piece's tooltip, item level and stat differences with the worn piece of that slot.
  - Both rings for a ring; base values without materia; its own switch, on by default.

## 0.376.0
- Étape g), domaine 3 (Monde), M-06 : usure et symbiose de l'équipement porté. Choix de l'utilisateur : alerte Tensho et barre à
  l'écran, chacune en option, seuils réglables.
- Équipement, nouvelle section « Usure et symbiose » : l'état et la symbiose de chaque pièce portée ; réglages :
  - « Alerte réparation et extraction » (activée) : un message dans le chat et le son des alarmes quand une pièce passe sous le seuil
    de réparation ou atteint le seuil de symbiose, une fois par passage (options des alarmes appliquées, silence en mission compris) ;
  - « Barre d'usure à l'écran » (désactivée) : l'état le plus bas et la symbiose la plus haute, déplaçable ;
  - seuils « Réparer sous » (10 %) et « Extraire à partir de » (100 %), avec ↺.
- Lecture : l'état par la fonction du jeu GetConditionPercentage (appelée seulement si son adresse est résolue), la symbiose par le
  champ de l'objet (10 000 points = 100 %, à confirmer en jeu) ; chaque lecture protégée à part ; objets de collection exclus.
- Preuve : docs/evidence/0.376-gear-wear.md.
- Tests : GearWear376Tests (3 cas). Verify.ps1 (agent) : 712/712, 0 avertissement, 0 erreur ; DLL 0.376.0.0. Non validé en jeu.
### English
- Queue step g, M-06: worn gear condition and spiritbond on the Gear page, with a repair / extraction chat alert (on by default).
  - An optional on-screen bar (off by default) and adjustable thresholds.

## 0.375.0
- Étape g), domaine 3 (Monde), M-05 : tâches personnelles et panneau « à faire ». Choix de l'utilisateur : jour, semaine, toutes les
  n heures, compteur ; panneau à l'écran en option, désactivé par défaut.
- Rappels, nouvelle section « Vos tâches » (50 au plus) :
  - chaque jour ou chaque semaine (aux remises à zéro du jeu), toutes les n heures après avoir été faite, ou compteur jusqu'à un
    objectif (boutons « - » et « + », « Remettre à 0 ») ;
  - chaque tâche dit quand elle revient ; flèches pour l'ordre ; « Retirer » ;
  - case « À l'écran » par tâche.
- Panneau « Tensho — À faire » à l'écran (réglage dans la section, désactivé par défaut) : les tâches non faites cochées « À l'écran »,
  cochables dans le panneau ; caché en combat ; le fermer décoche le réglage.
- Les tâches sont gardées par « Restaurer tous les réglages » ; le panneau, lui, revient à désactivé.
- Tests : PersonalTasks375Tests (3 cas). Verify.ps1 (agent) : 709/709, 0 avertissement, 0 erreur ; DLL 0.375.0.0. Non validé en jeu.
### English
- Queue step g, M-05: your own tasks on the Reminders page (daily, weekly, every n hours, counter).
  - Optional on-screen « to do » panel, off by default and hidden in combat.

## 0.374.0
- Étape g), domaine 3 (Monde), M-04 : suivi de plusieurs personnages. Choix de l'utilisateur : un fichier par personnage, comme I-03.
- Rappels, nouvelle section « Vos personnages » : pour chaque personnage, ce que Tensho a lu à sa dernière connexion :
  - ventures des servants (terminées, en cours, prochaine fin ; connues après l'ouverture de la liste des servants) ;
  - prochaine carte au trésor, Livre d'or des défis (vignettes, expiration), contrats de chasse en main ;
  - missions aléatoires faites aujourd'hui (« remis à zéro » quand la lecture date d'avant la remise quotidienne) ;
  - heure de la dernière lecture, « Connecté » pour le personnage en jeu.
- Les cases « fait » des Rappels sont maintenant propres au personnage connecté ; les anciennes cases communes vont une fois au premier
  personnage vu (après l'écriture de son fichier seulement).
- Un fichier par personnage (Characters/<identifiant local du personnage>.json, jamais d'identifiant de compte), écrit quand quelque
  chose change ; un fichier qui contient un autre personnage est refusé et jamais écrasé. « Effacer » puis « Confirmer » supprime le
  fichier d'un autre personnage (le personnage connecté ne peut pas l'être).
- Les vaisseaux restent sur la page Compagnie libre : ils sont à la compagnie, pas à un personnage.
- Tests : CharacterStatus374Tests (3 cas). Verify.ps1 (agent) : 706/706, 0 avertissement, 0 erreur ; DLL 0.374.0.0. Non validé en jeu.
### English
- Queue step g, M-04: Reminders show all your characters (ventures, treasure map, Wondrous Tails, hunt bills, today's roulettes).
  - One file per character, local character id only; a character can be erased.
  - The Reminders « done » boxes now belong to the logged-in character.

## 0.373.0
- Étape g), domaine 3 (Monde), M-03 : liens de carte du chat.
- Page Cartes, nouvelle section « Liens de carte du chat » :
  - les liens de carte vus dans le chat depuis la connexion, tous les canaux, du plus récent au plus ancien (100 au plus) ;
  - filtre par canal (dire, crier, hurler, équipe, alliance, compagnie libre, linkshells, linkshells inter-mondes, messages privés,
    réseau des novices, autres) ;
  - « Drapeau » pose le drapeau du jeu sur le point ;
  - « Éthérite » ouvre la carte Tensho sur le point et propose l'éthérite la plus proche de la carte parmi votre liste de
    téléportation, avec son coût ; la téléportation se fait seulement après « Oui ».
- Gardés en mémoire seulement, jamais enregistrés ; effacés à la déconnexion. Le message du chat n'est jamais modifié.
- Réglage « Liens de carte du chat » dans la carte Tensho (section Jeu), activé par défaut.
- Preuve : docs/evidence/0.373-chat-map-links.md.
- Tests : ChatMapLinks373Tests (4 cas). Verify.ps1 (agent) : 703/703, 0 avertissement, 0 erreur ; DLL 0.373.0.0. Non validé en jeu.
### English
- Queue step g, M-03: the Maps page lists the map links seen in the chat this session, filterable by channel.
  - A click places the flag; another offers the nearest teleportable aetheryte with its cost, teleporting only on confirmation.
  - Memory only, cleared at logout.

## 0.372.0
- Étape g), domaine 3 (Monde), M-02 : compteurs hebdomadaires lus dans le jeu.
- Rappels :
  - nouvelle ligne « Livre d'or des défis » : vignettes posées sur 9, points de seconde chance, expiration du livre ;
  - nouvelle ligne « Contrats de chasse » : nombre de contrats en main ;
  - « Cartes au trésor » : l'heure de la prochaine carte vient du jeu quand elle est lue (le bouton « ouverte » reste en secours).
- Panneau Tensho de l'outil de recherche de mission : sous une mission aléatoire choisie, « Faite » ou « À faire » aujourd'hui.
- Chaque lecture est protégée à part ; les fonctions du jeu sont appelées seulement si leur adresse est résolue.
- Les roulettes du jour étaient déjà dans Rappels depuis 0.356.
- Preuve : docs/evidence/0.372-weekly-counters.md.
- Tests : WeeklyCounters372Tests (1 cas). Verify.ps1 (agent) : 699/699, 0 avertissement, 0 erreur ; DLL 0.372.0.0. Non validé en jeu.
### English
- Queue step g, M-02: Reminders now read Wondrous Tails, the hunt bills held and the next treasure map allowance from the game.
  - The Duty Finder panel says whether the chosen roulette was done today.

## 0.371.0
- Étape g), domaine 3 (Monde), M-01 : alarmes automatiques.
- Tensho crée lui-même une alarme Tensho :
  - à la fin de chaque venture de servant (connue après l'ouverture de la liste des servants) ;
  - au retour de chaque vaisseau de la compagnie (connu après l'atelier).
- L'heure suit les relevés ; l'alarme disparaît une heure après avoir sonné ; les alarmes faites par le joueur ne sont jamais touchées.
- Aucune alarme du jeu (/alarm) n'est créée d'office ; la limite de 60 alarmes est gardée.
- Réglage « Alarmes automatiques (ventures, vaisseaux) » dans Alarmes, activé par défaut (choix fait dans GAPS.md).
- Lecture (Alarms/AutoAlarmWatcher) : toutes les 30 secondes sur le thread du jeu, servants en mémoire et vaisseaux déjà enregistrés.
- Tests : AutoAlarms371Tests (1 cas). Verify.ps1 (agent) : 698/698, 0 avertissement, 0 erreur ; DLL 0.371.0.0. Non validé en jeu.
### English
- Queue step g, M-01: automatic Tensho alarms at each retainer venture's end and each vessel's return.
  - The time follows the readings; the player's own alarms are never touched.
  - On by default, as chosen.

## 0.370.0
- Étape g), domaine 2 (Collections), X-02 (en partie) : musique en cours et rouleaux favoris.
- Collections › Orchestrion, en tête :
  - « Musique en cours : … », avec son rouleau obtenu ou non et un bouton « Fiche » ;
  - « Rouleaux favoris », ajoutés par la case « Favori » de la fiche d'un rouleau (300 au plus, gardés par la réinitialisation).
- Lecture (Collections/BgmReader) : scènes de BGMSystem, en mémoire seulement, une fois par seconde pendant que l'onglet est ouvert.
  - Rouleau trouvé par le chemin du fichier de la musique (feuilles BGM et OrchestrionPath).
- Changer la musique côté client n'est pas fait : la fonction du jeu a des paramètres non documentés (noté).
- Preuve : docs/evidence/0.370-orchestrion-player.md.
- Tests : OrchestrionPlayer370Tests (1 cas). Verify.ps1 (agent) : 697/697, 0 avertissement, 0 erreur ; DLL 0.370.0.0. Non validé en jeu.
### English
- Queue step g, X-02 (partly): the Orchestrion tab shows the music playing now and its roll, plus favourite rolls.
  - Changing the music is not done.

## 0.369.0
- Étape g), domaine 2 (Collections), X-03 : changer de titre.
- Fenêtre d'un titre obtenu :
  - « Porter ce titre » ;
  - « Titre de <job> » : le choisit pour le job actuel.
- Onglet Titres, en tête « Changer de titre » :
  - « Titre au hasard » (parmi les titres obtenus), « Aucun titre » ;
  - case « Changer de titre avec le job » (désactivée par défaut) et la liste job → titre, avec « Retirer ».
- Le changement passe par la fonction du jeu (TitleController.SendTitleIdUpdate), sur le thread du jeu.
  - Adresse vérifiée, 2 secondes au moins entre deux changements, jamais à la connexion.
- La liste job → titre est gardée par « Réinitialiser les réglages » ; la case revient à « désactivée ».
- Preuve : docs/evidence/0.369-titles.md.
- Tests : Titles369Tests (1 cas). Verify.ps1 (agent) : 696/696, 0 avertissement, 0 erreur ; DLL 0.369.0.0. Non validé en jeu.
### English
- Queue step g, X-03: wear a title, a random title or none.
  - Optional title by job, off by default, applied on a job change only.
  - Uses the game's TitleController with its address checked.

## 0.368.0
- Étape g), domaine 2 (Collections), C-15 : progression des succès et succès suivis.
- Fenêtre d'un succès :
  - case « Suivre ce succès » ;
  - progression « x / y » quand le jeu l'a reçue (en ouvrant le succès dans la fenêtre du jeu, ou avec « Demander ») ;
  - « Demander » envoie une requête au serveur, seulement sur ce clic, 3 secondes d'écart au moins.
- Onglet Succès : « Succès suivis » en tête (pas encore terminés d'abord), avec leur progression, « Demander » et « Retirer ».
- Lecture (Collections/AchievementProgressWatcher) : sur le thread du jeu, une fois par seconde, en mémoire par personnage.
  - L'adresse de la fonction de demande est vérifiée avant l'appel.
- Les succès suivis (100 au plus) sont gardés par « Réinitialiser les réglages ».
- Preuve : docs/evidence/0.368-achievement-progress.md.
- Tests : AchievementProgress368Tests (1 cas). Verify.ps1 (agent) : 695/695, 0 avertissement, 0 erreur ; DLL 0.368.0.0. Non validé en jeu.
### English
- Queue step g, C-15: achievement progress and tracked achievements.
  - Progress is read when the game receives it, or asked from the server on the player's click only.
  - Tracked achievements are listed at the top of the Achievements tab.

## 0.367.0
- Étape g), domaine 2 (Collections), C-13 : monstres qui enseignent un sort de mage bleu.
- Mage bleu › fenêtre d'un sort, nouvelle partie « Monstres qui l'enseignent (wiki) » :
  - un clic lit la page du sort sur le wiki (seul le nom anglais du sort est envoyé ; interrupteur du wiki respecté) ;
  - chaque monstre avec son niveau et son lieu ;
  - un bouton « Drapeau » quand le lieu a des coordonnées et que la zone est reconnue (nom anglais du lieu → zone du jeu).
- Le lecteur des tableaux du wiki est celui des butins de monstres, avec un cache à part (« wiki-blue-spells »).
- Test 0.324 mis à jour : quatre services suivent maintenant l'interrupteur du wiki.
- Preuve : docs/evidence/0.367-blue-spell-wiki.md.
- Tests : BlueSpellSources367Tests (1 cas). Verify.ps1 (agent) : 694/694, 0 avertissement, 0 erreur ; DLL 0.367.0.0. Non validé en jeu.
### English
- Queue step g, C-13: the blue magic spell window lists the monsters that teach it, from the wiki on click.
  - Each monster comes with its level and place.
  - A Flag button appears when the place has coordinates.

## 0.366.0
- Étape g), domaine 2 (Collections), C-10 : alerte quand une mission est prête.
- Alarmes › réglages, nouvelle case « Alerte quand une mission est prête » (désactivée par défaut).
  - Quand l'outil de recherche de mission trouve le groupe, Tensho écrit « Mission prête : … » dans la discussion et joue le son des
    alarmes, avec leurs règles de silence en mission.
- Discord : nouvelle catégorie « Mission prête » ; le message part seulement si l'alerte est activée et l'adresse réglée.
- Lecture : événement IClientState.CfPop de Dalamud, retiré à la fermeture du plugin.
- Tests : DutyPop366Tests (1 cas). Verify.ps1 (agent) : 693/693, 0 avertissement, 0 erreur ; DLL 0.366.0.0. Non validé en jeu.
### English
- Queue step g, C-10: an optional « duty ready » alert (chat line and alarm sound, same silence rules).
  - Off by default.
  - Discord gets its own « Duty ready » category.

## 0.365.0
- Étape g), domaine 2 (Collections), C-04 : huit types de collection en plus, dans l'ordre choisi.
- Nouveaux onglets de Collections :
  - coiffures (vendues comme objet), accessoires de mode, lunettes ;
  - bardes de chocobo ;
  - portraits (cadres, fonds, décorations), livres de maître artisan ;
  - objets de collection, archives occultes.
- Chaque entrée est obtenue ou non selon Dalamud (IUnlockState). Les filtres et la recherche sont ceux des autres onglets.
- Lecture (Collections/ExtraCollectionReader) : à part du lecteur des collections et de son cache.
  - Sur le thread du jeu, 2 ms par image au plus, pendant que la page est ouverte ; relu toutes les 30 secondes.
- Familiers Beastmaster non faits : leurs colonnes n'ont pas de nom dans les données (noté).
- Tests : ExtraCollections365Tests (1 cas). Verify.ps1 (agent) : 692/692, 0 avertissement, 0 erreur ; DLL 0.365.0.0. Non validé en jeu.
### English
- Queue step g, C-04: eight more Collections tabs.
  - Hairstyles, fashion accessories, glasses, bardings, portraits, master books, collectibles, Occult records.
  - Read with Dalamud IUnlockState apart from the cached collections.

## 0.364.0
- Étape g), domaine 2 (Collections), C-12 : carnet de chasse.
- Progression › Carnet de chasse : les carnets des 9 classes et des 3 grandes compagnies (feuille MonsterNote).
  - Choix du carnet et du rang (le rang en cours par défaut), filtre « Non terminés ».
  - Pour chaque entrée : ses monstres, les victimes faites sur le nombre demandé, et les zones.
- Lecture (Hunting/HuntingLogReader) : MonsterNoteManager, en mémoire, sur le thread du jeu, seulement pendant que la page est ouverte.
  - Le jeu ne garde que le rang en cours : les rangs d'avant comptent comme terminés, ceux d'après à zéro.
- Ordre des carnets en mémoire : faits lus dans Hunty (MIT, lecture seule), à confirmer en jeu. Sonde : docs/evidence/0.364-hunting-log.md.
- Pas de coordonnées ni de drapeau : proposition notée.
- Tests : HuntingLog364Tests (1 cas). Verify.ps1 (agent) : 691/691, 0 avertissement, 0 erreur ; DLL 0.364.0.0. Non validé en jeu.
### English
- Queue step g, C-12: a Hunting log page for the 9 class logs and 3 Grand Company logs, by rank.
  - Each entry lists its monsters, the kills read in game and the zones.
  - The log order in memory comes from facts read in Hunty (MIT) and is to be confirmed in game.

## 0.363.0
- Étape g), domaine 2 (Collections), C-14 : page Mandats.
- Progression › Mandats : chaque mandat du jeu (feuille Leve) avec son niveau, son type, son mandateur, sa zone et ses gils.
  - L'état est lu dans le jeu : terminé, accepté, pas terminé, ou « — » s'il n'est pas lu.
  - Filtres : recherche, type, mandateur, niveau min et max, état (« Non terminés » par défaut).
  - En tête : les allocations restantes et le nombre de mandats acceptés.
  - « Mandats d'artisanat acceptés » : l'objet que demande chaque mandat accepté, la quantité et les copies possédées.
- Lecture (Leves/LeveProgressReader) : sur le thread du jeu, seulement pendant que la page est ouverte.
  - Terminé : IUnlockState.IsLeveCompleted de Dalamud, 2 ms par image au plus.
  - Acceptés : QuestManager.LeveQuests, lu en mémoire.
  - Un échec laisse l'état « indisponible ».
- Aucune automatisation des mandats.
- Preuve : docs/evidence/0.363-leves.md.
- Tests : Leves363Tests (1 cas). Verify.ps1 (agent) : 690/690, 0 avertissement, 0 erreur ; DLL 0.363.0.0. Non validé en jeu.
### English
- Queue step g, C-14: a Guildleves page.
  - Every leve with its level, type, client, zone and gil, completed or accepted as read in game.
  - Filters: level, type, client and state; allowances left.
  - Accepted crafting leves list the item they ask and the copies held.

## 0.362.0
- Étape g), domaine 2 (Collections), C-05 : tenues du coffre à apparence.
- Collections, nouvel onglet « Tenues » : chaque tenue du jeu (feuille MirageStoreSetItem) avec :
  - ses pièces possédées sur le total, et son état : incomplète, toutes les pièces possédées, prête à ranger, rangée ;
  - « prête à ranger » = toutes les pièces dans le coffre : le jeu peut alors en faire une tenue ;
  - un clic ouvre ses pièces (dans le coffre, possédée ailleurs, manquante), avec leurs sources au clic droit.
- Filtres : toutes, incomplètes, toutes les pièces, rangées ; recherche par nom.
- « Possédée » = sacs en direct et rangements enregistrés du personnage connecté. Une tenue rangée compte toutes ses pièces.
- Sauvegarde avant ce travail : checkpoints/backup-20261103-013747-avant-C05-0362.zip.
- Tests : Outfits362Tests (1 cas). Verify.ps1 (agent) : 689/689, 0 avertissement, 0 erreur ; DLL 0.362.0.0. Non validé en jeu.
### English
- Queue step g, C-05: an Outfits tab in Collections.
  - Each glamour dresser outfit shows its pieces owned and its state: incomplete, every piece owned, ready to store, stored.
  - Each piece shows where it is, with its sources on right click.

## 0.361.0
- Étape g), domaine 2 (Collections), C-11 : carnet d'exploration complet.
- Carte › listes, nouvel onglet « Carnet d'exploration » : tous les panoramas, classés par zone (zones à gauche, panoramas à droite) :
  - numéro du carnet, nom, découvert ou non (lu dans le jeu) ;
  - « possible maintenant » : fenêtre d'heure d'Éorzéa ouverte et météo demandée (wiki) égale à la météo actuelle de la zone ;
  - météo inconnue : « heure OK, météo inconnue », jamais « impossible ».
- Filtres « Non découverts » et « Possibles maintenant ».
- Un clic pose le drapeau ; l'infobulle (prochaine fenêtre) et le clic droit (alarme) sont ceux de la carte.
- Lignes recalculées quand une source change, et une fois par minute réelle.
- Tests : VistaLog361Tests (1 cas). Verify.ps1 (agent) : 688/688, 0 avertissement, 0 erreur ; DLL 0.361.0.0. Non validé en jeu.
### English
- Queue step g, C-11: the whole sightseeing log, grouped by zone, as a tab of the map lists.
  - Each vista shows whether it was discovered and whether it is possible now (Eorzea time window and current weather).
  - Filters: not discovered, possible now. A click places the flag.

## 0.360.0
- Étape g), domaine 2 (Collections), C-09 : liste de souhaits.
- Le menu du clic droit de chaque objet dans Tensho a « Ajouter à mes souhaits » (ou « Retirer »).
- Collections, nouvel onglet « Souhaits » : chaque objet avec son icône et son état :
  - obtenu ou pas encore, pour une monture, une mascotte, un rouleau ou une carte ;
  - sinon les copies possédées (sacs et rangements enregistrés) ;
  - ses sources au clic droit, et un bouton « Retirer ».
- 500 objets au plus. La liste est gardée par « Réinitialiser les réglages » (liste « Kept »).
- Tests : Wishlist360Tests (1 cas). Verify.ps1 (agent) : 687/687, 0 avertissement, 0 erreur ; DLL 0.360.0.0. Non validé en jeu.
### English
- Queue step g, C-09: a wishlist.
  - « Add to my wishlist » in every item's right-click menu.
  - A Wishlist tab in Collections shows each item's collection state or held copies, with its sources on right click.
  - 500 items at most; kept on settings reset.

## 0.359.0
- Étape g), domaine 2 (Collections), C-08 : filtre des collections par source.
- Collections › Montures, Mascottes, Orchestrion : nouveau filtre « Source ». Choix possibles :
  - succès, contenus, coffres et autres objets, échanges (monnaies), marchands (gils) ;
  - quêtes, artisanat, récolte ;
  - vendable au marché, ou aucune source connue.
- Le filtre se combine avec l'état (obtenu, non confirmé) et la recherche.
- Les sources sont celles du menu du clic droit, réduites à un masque par entrée :
  - calculées au premier usage du filtre ;
  - gardées jusqu'au changement d'un catalogue ;
  - sans aucune requête au wiki ou à Garland : seulement ce qui est déjà en cache.
  - Avant ce correctif, la première version du filtre aurait demandé une page par monture.
- « Aucune source connue » ne veut pas dire « impossible à obtenir » (infobulle).
- Tests : CollectionSources359Tests (1 cas). Verify.ps1 (agent) : 686/686, 0 avertissement, 0 erreur ; DLL 0.359.0.0. Non validé en jeu.
### English
- Queue step g, C-08: a Source filter on mounts, minions and orchestrion rolls.
  - Sources: achievement, duty, coffers, exchange, vendor, quest, craft, gathering, market, or none known.
  - They are the right-click menu's sources, cached per entry, with no wiki or Garland request.

## 0.358.0
- Étape g), domaine 2 (Collections), C-07 : apparences identiques dans le coffre à apparence.
- Rangements › Rapport, nouveau bloc « Coffre à apparence : apparences identiques » :
  - groupe les objets du coffre qui ont les mêmes modèles (Item.ModelMain et ModelSub) au même emplacement ;
  - un seul suffit souvent pour le mirage ; le nombre de canaux de teinture de chacun est indiqué ;
  - les pièces d'une tenue rangée ne comptent pas ;
  - les recolorations (autre variante d'un même modèle) ne sont pas groupées.
- Les teintures sont copiées avec les groupes : aucune lecture des données du jeu dans le dessin.
- Tests : SameLook358Tests (1 cas). Verify.ps1 (agent) : 685/685, 0 avertissement, 0 erreur ; DLL 0.358.0.0. Non validé en jeu.
### English
- Queue step g, C-07: the storage report groups glamour dresser items that look the same (same models, same slot).
  - The dye channels of each item are shown.
  - Recolours are not grouped.

## 0.357.0
- Étape g), domaine 2 (Collections), C-03 : objets qui peuvent aller dans l'armoire.
- Rangements › Rapport, nouveau bloc « Peut aller dans l'armoire » : les objets que l'armoire accepte (feuille Cabinet) rangés ailleurs :
  - lieux couverts : sacs, sacoches, servants, coffre à apparence, et l'arsenal quand les sets d'équipement sont lus ;
  - les copies déjà dans l'armoire sont listées d'abord, marquées « en trop » ;
  - armoire pas encore ouverte : l'état reste inconnu, jamais « absent » ;
  - les pièces d'un set d'équipement et les pièces d'une tenue rangée ne sont pas proposées.
- Rapport seulement : rien n'est déplacé.
- Preuve : docs/evidence/0.357-armoire.md (coffre à apparence de 800 cases).
- Tests : Armoire357Tests (1 cas). Verify.ps1 (agent) : 684/684, 0 avertissement, 0 erreur ; DLL 0.357.0.0. Non validé en jeu.
### English
- Queue step g, C-03: the storage report lists items the armoire takes that are kept elsewhere.
  - Spare copies already in the armoire come first.
  - Gear set pieces and outfit pieces are left out.
  - Nothing is moved.

## 0.356.0
- Étape g), domaine 2 (Collections), C-02 : missions aléatoires du jour.
- Monde › Rappels, nouvelle section « Missions aléatoires du jour », lue dans le jeu avec les compteurs du Calendrier :
  - chaque mission aléatoire quotidienne : faite aujourd'hui, à faire, ou « — » si l'état n'est pas lu ;
  - le rôle qui a le bonus (tank, soigneur, DPS), tel que l'outil de recherche de mission le garde ;
  - le compteur « X faites sur Y » et l'heure de la remise à zéro quotidienne.
- Liste des missions : feuille ContentRoulette, celles de l'outil de recherche de mission qui ont un index de complétion. Les courses
  de chocobos et Crystalline Conflict n'en ont pas (sonde : docs/evidence/0.356-roulettes.md).
- Lecture :
  - « faite » : InstanceContent.IsRouletteIncomplete, appelée seulement si son adresse est résolue ;
  - bonus : AgentContentsFinder.ContentRouletteRoleBonuses (11 entrées), indexé par la ligne de bonus de la mission ;
  - les deux sont protégés à part : un échec laisse l'état inconnu, le reste des Rappels continue.
- L'alerte de bonus de rôle n'est pas faite : elle touche les alarmes et Discord (proposition notée pour vous).
- Sondes : `tools/ShopCurrencyProbe` mode PROBE_ROULETTE ; `tools/ApiProbe` montre maintenant aussi les champs internes et leur décalage.
- Tests : Roulettes356Tests (1 cas). Verify.ps1 (agent) : 683/683, 0 avertissement, 0 erreur ; DLL 0.356.0.0. Non validé en jeu.
### English
- Queue step g, C-02: daily duty roulettes under Reminders.
  - Each roulette shows whether it was done today and which role gets the bonus, with a count and the reset time.
  - Native calls are made only once their address is resolved, and each read is guarded on its own.
  - No role-bonus alert yet: it touches alarms and Discord.

## 0.355.0
- Étape g), domaine 2 (Collections), C-01 : état débloqué / terminé de chaque contenu.
- Monde › Butin des contenus :
  - nouvelle colonne « État » : Terminé, Débloqué, Verrouillé, ou « — » quand l'état n'est pas connu ;
  - filtre « Avancement » : tous, non terminés, débloqués non terminés, terminés ;
  - compteur « X terminés sur Y lus » pour la catégorie choisie ;
  - l'état aussi en tête de la fiche d'un contenu.
- Lecture (Duties/DutyProgressReader) :
  - sur le thread du jeu, 2 ms par image au plus, toutes les 5 secondes ;
  - seulement connecté et pendant que la page est ouverte ;
  - terminé : UIState.IsInstanceContentCompleted, appelé seulement si son adresse est résolue ;
  - débloqué : IUnlockState.IsInstanceContentUnlocked de Dalamud ;
  - un échec laisse l'état « indisponible », jamais « verrouillé » ;
  - la déconnexion efface l'état.
- Un contenu dont l'état est inconnu reste dans les listes « non terminés » : une absence de preuve n'est pas une preuve.
- Tests : DutyProgress355Tests (1 cas). Verify.ps1 (agent) : 682/682, 0 avertissement, 0 erreur ; DLL 0.355.0.0. Non validé en jeu.
### English
- Queue step g, C-01: unlocked / completed status of each duty on the Duty loot page.
  - A Status column, a Progress filter (not completed, unlocked not completed, completed) and a completed count.
  - Read on the framework thread while the page is open; the native call is made only once its address is resolved.
  - Unknown statuses are never shown as locked.

## 0.354.0
- Étape g), domaine 1, I-13 : valeur des monnaies (« que faire de mes sceaux ou de mes marques »).
- Rangements › Monnaies, bouton « Que faire ? » sur chaque monnaie : ce qu'elle achète chez les marchands connus, avec :
  - le prix dans cette monnaie ;
  - le prix minimum au marché de votre monde (Universalis, récent) ;
  - les gils obtenus par unité de monnaie en revendant (prix × quantité ÷ coût) ;
  - le classement du plus rentable au moins rentable, les prix inconnus à la fin et jamais comptés zéro ;
  - les montures et mascottes marquées « possédée » ou « manquante ».
- « Prix du marché » récupère les prix qui manquent, 32 au plus par clic, en respectant l'interrupteur Universalis.
- Les monnaies codées (mémoquartz, assignats) restent non résolues, comme avant (noté).
- Domaine 1 (inventaire et marché) terminé, sauf I-05 et I-15, gardés pour une séance en ultracode avec l'utilisateur.
- Tests : CurrencyValue354Tests (1 cas). Verify.ps1 (agent) : 681/681, 0 avertissement, 0 erreur ; DLL 0.354.0.0. Non validé en jeu.
### English
- Queue step g, I-13: « What to do? » per currency — what it buys, its price in that currency, the market price and the gil per unit
  of currency (best first, unknown last), mounts and minions marked owned or missing; market prices fetched on a click.

## 0.353.0
- Étape g), domaine 1, I-12 : recherche d'objets avec filtres, et objets clés.
- Page Objets, « Filtres » sous la recherche, combinables avec le nom ou seuls (sans rien écrire) : catégorie de l'objet, niveau
  d'équipement de … à …, niveau d'objet de … à …, emplacement d'équipement, échangeable seulement, vendable au marché seulement.
  - Chaque filtre a son infobulle, et « Effacer les filtres » les remet à zéro.
- « Objets clés » : recherche par nom dans les objets de quête (feuille EventItem), avec leur icône ; ils n'ont pas de fiche.
- Pas encore de filtre par job : le catalogue ne garde pas cette donnée (noté pour la fin).
- Correctif de 0.349 : « /tensho wiki | lodestone » demandait 200 résultats à la recherche, qui en accepte 100 au plus et lève une
  exception au-delà. La commande échouait donc toujours ; elle cherche maintenant dans les 100 premiers.
- Tests : ItemFilters353Tests (1 cas). Verify.ps1 (agent) : 680/680, 0 avertissement, 0 erreur ; DLL 0.353.0.0. Non validé en jeu.
### English
- Queue step g, I-12: item search filters (category, equip and item level ranges, slot, tradable, sold on the market), alone or with a
  name; key item search by name. Fix: the 0.349 wiki/Lodestone command asked the search for 200 results and always failed.

## 0.352.0
- Étape g), domaine 1, I-11 : conditions de déblocage des articles de boutique.
- Les articles gardent la quête et le haut fait qui les débloquent (données du jeu : GilShopItem.QuestRequired et
  AchievementRequired, SpecialShop Quest et AchievementUnlock).
  - L'annuaire des marchands est reconstruit une fois (nouvelle version de son cache).
- Partout où un prix d'article est affiché (marchands, fiche d'objet, survol, boutiques d'événement), la condition suit le prix :
  « Après la quête « … » (fait / à faire / non lu) », « Après le haut fait « … » ».
  - Un haut fait non confirmé reste « non lu », jamais « à faire ».
- Les rangs de tribu ne sont pas encore lus (noté pour la fin).
- Tests : ShopRequirements352Tests (1 cas). Verify.ps1 (agent) : 679/679, 0 avertissement, 0 erreur ; DLL 0.352.0.0. Non validé en jeu.
### English
- Queue step g, I-11: the quest and achievement that unlock a shop article are read from the game data and shown after its price,
  with done / to do from the quest journal and the achievements read (vendor cache rebuilt once).

## 0.351.0
- Étape g), domaine 1, I-04 : journal des gils. Choix de l'utilisateur : gardé 90 jours.
- Lecture des gils une fois par seconde sur le thread du jeu, une fois connecté :
  - les vôtres (GetGil) ;
  - ceux de vos servants, en somme, une fois que le jeu les a donnés (cloche des servants) ;
  - ceux du coffre de compagnie, pendant qu'il est chargé.
  - Chaque fonction n'est appelée que si le jeu l'a trouvée.
- Chaque variation est notée avec sa catégorie, déduite de la fenêtre du jeu vue dans les 15 dernières secondes : boutique PNJ,
  marché, réparation, téléportation, servants, compagnie, sinon « autre ». La première lecture d'un personnage n'est qu'un point de
  départ.
- Un fichier par personnage (GilJournal/<identifiant local>.json) : 90 jours et 20 000 lignes au plus, écrit au plus une fois par
  minute et à la fermeture, via un fichier temporaire. Un fichier illisible n'est jamais réécrit.
- Nouvel onglet Rangements › Gils : les trois soldes, gains et dépenses par catégorie sur 1, 7, 30 ou 90 jours, et les 50 dernières
  variations.
- SECURITY à jour (nouveau fichier local).
- Tests : GilJournal351Tests (2 cas). Verify.ps1 (agent) : 678/678, 0 avertissement, 0 erreur ; DLL 0.351.0.0. Non validé en jeu.
### English
- Queue step g, I-04: gil journal (character, retainers once known, company chest while open), each change categorized from the game
  window seen just before (shop, market, repair, teleport…), kept 90 days in one file per character; new Storage › Gil tab.

## 0.350.0
- Étape g), domaine 1, I-09 : le coffre de compagnie libre dans les rangements.
- Nouveau type de rangement (ajouté en dernier) : les cinq pages du coffre, ses gils et ses cristaux (InventoryType 20000 à 20004,
  22000, 22001), enregistré pour le personnage qui l'ouvre.
- Le jeu n'envoie le coffre qu'à l'ouverture, page par page. Tensho ne le lit que pendant que sa fenêtre est ouverte, et n'enregistre
  que lorsque toutes les pages sont reçues (le panneau le dit).
- Enregistrement par le bouton du panneau Tensho à côté de la fenêtre du coffre, ou d'office avec « Enregistrer automatiquement les
  servants, sacoches et le coffre de compagnie » (texte du réglage mis à jour).
- La banque a un onglet « Coffre de compagnie », et les stocks d'un objet comptent le coffre (une ligne seulement une fois ouvert).
  L'aperçu de tri ne le propose pas.
- Tests : CompanyChest350Tests (1 cas). Verify.ps1 (agent) : 676/676, 0 avertissement, 0 erreur ; DLL 0.350.0.0. Non validé en jeu.
### English
- Queue step g, I-09: the free company chest (five pages, gil, crystals) as a storage — recorded from its open window once every page
  has been received (button or automatic), shown in the Bank and counted in item stocks.

## 0.349.0
- Étape g), domaine 1, X-07 : liens vers le wiki et le Lodestone.
- Clic droit sur un objet › Tensho, et fiche d'objet : « Wiki (anglais) » et « Lodestone ».
  - Le wiki (FFXIV Console Games Wiki) est ouvert sur le nom anglais de l'objet.
  - Le Lodestone est ouvert sur la recherche de sa base de données, dans la langue de Tensho (fr, de, jp, sinon na).
- Commandes « /tensho wiki <objet> » et « /tensho lodestone <objet> » : le nom exact d'abord, sinon le plus court qui contient le texte ;
  un nom inconnu est dit dans le chat.
  - Pas de /wiki ni de /lodestone : un autre plugin peut déjà les tenir. Choix noté pour la fin.
- Les pages s'ouvrent dans votre navigateur sur votre clic ou votre commande ; Tensho n'envoie rien.
  - Adresses du Lodestone déclarées avec le crédit du jeu.
  - Description du plugin et SECURITY à jour (aussi pour les alertes Discord des monnaies de 0.344 et les fichiers par personnage de
    0.345).
- Tests : ExternalLinks349Tests (1 cas) ; le test du manifeste connaît le Lodestone. Verify.ps1 (agent) : 675/675, 0 avertissement, 0 erreur ;
  DLL 0.349.0.0. Non validé en jeu.
### English
- Queue step g, X-07: an item's page on the wiki (English name) or the Lodestone (Tensho's language) from the right-click menu, the
  item sheet or « /tensho wiki|lodestone <item> », opened in the browser on the player's click only.

## 0.348.0
- Étape g), domaine 1, I-10 : rapport de rangement. Choix de l'utilisateur : rapport seul, aucun déplacement.
- Nouvel onglet Rangements › Rapport :
  - « Piles à regrouper » : pour vos sacs (en direct) et vos sacoches et servants enregistrés, les objets d'une même qualité répartis
    en plus de piles qu'il n'en faut, avec les cases libérables (taille de pile tirée des données du jeu ; une taille de 1 ou inconnue
    n'est jamais proposée) ;
  - « Arsenal : pièces hors des sets d'équipement » : les pièces de l'arsenal enregistré qu'aucun set n'utilise (cristaux d'âme exclus),
    avec un rappel qu'une pièce peut servir de mirage.
- Les sets d'équipement sont lus dans le jeu (RaptureGearsetModule) sur le thread du jeu, seulement quand l'onglet est ouvert. Un set
  est reconnu par son drapeau « existe » : lecture de mémoire seulement, aucune fonction du jeu n'est appelée.
- Tests : StorageReport348Tests (2 cas). Verify.ps1 (agent) : 674/674, 0 avertissement, 0 erreur ; DLL 0.348.0.0. Non validé en jeu.
### English
- Queue step g, I-10: Storage › Report — stacks of one item that fit in fewer slots (bags live, saved saddlebags and retainers) and
  armoury pieces used by no gear set (gear sets read on request, memory only). Report only: nothing is moved.

## 0.347.0
- Étape g), domaine 1, I-08 : notes, étiquettes et liste « à vendre au PNJ » sur les objets. Choix de l'utilisateur : son optionnel,
  désactivé par défaut.
- Fiche d'objet, nouvel onglet « Note » :
  - une note libre (500 caractères, enregistrée en quittant le champ) ;
  - les étiquettes « Important » et « À vendre au PNJ » ;
  - deux réglages : rappel à l'ouverture d'une boutique (activé) et son pour les objets importants (désactivé), avec infobulles et ↺.
- La note et les étiquettes s'affichent en tête de l'information de l'objet (survol et fiche).
- À l'ouverture d'une boutique de PNJ (fenêtre « Shop » du jeu), le chat liste une fois :
  - les objets « à vendre » de vos quatre sacs, avec leur quantité ;
  - les objets « importants », avec le son des alarmes s'il est activé.
  - Rien n'est vendu pour vous.
- Les notes sont des données gardées (« Restaurer tous les réglages » ne les efface pas), bornées à 2 000 objets, et remplacées en
  entier à chaque changement (lues sans verrou sur le thread du jeu).
- Idées et choix provisoires ajoutés à https://github.com/Shigu330/Tensho-Assistant/blob/024c426c1186a2e8f51e09087996a9c55208c28a/docs/project/G-PROPOSALS.md.
- Tests : ItemNotes347Tests (2 cas). Verify.ps1 (agent) : 672/672, 0 avertissement, 0 erreur ; DLL 0.347.0.0. Non validé en jeu.
### English
- Queue step g, I-08: item notes and labels (« important », « to sell to an NPC ») in a new item sheet tab, shown in the item
  information; a chat reminder when an NPC shop opens (optional alarm sound for important items, off by default).

## 0.346.0
- Étape g), domaine 1, deux petits écarts.
- I-06, prix de vente au PNJ :
  - l'information d'objet (survol et fiche) affiche « Vente au PNJ (NQ) : X gils l'unité » ;
  - au survol d'une case de vos sacs, elle ajoute la valeur de la pile ;
  - donnée du jeu (Item.PriceLow), rien pour un objet qui ne se vend pas au PNJ ;
  - pour NQ seulement, la feuille ne donne pas le prix HQ.
- I-07, aide à la sous-cotation (choix de l'utilisateur : copie dans le presse-papiers seulement) :
  - quand vous ouvrez les offres d'un objet au tableau des ventes, Tensho copie l'offre la plus basse moins 1 gil et le dit dans le chat ;
  - les offres de vos propres servants sont exclues ;
  - vous collez et confirmez vous-même : aucune mise en vente automatique ;
  - offres lues par l'événement Dalamud IMarketBoard.OfferingsReceived, rien n'est demandé au serveur.
  - Réglage Paramètres › Avancé, désactivé par défaut (il remplace le presse-papiers), avec infobulle et ↺.
- Choix provisoires ajoutés à https://github.com/Shigu330/Tensho-Assistant/blob/024c426c1186a2e8f51e09087996a9c55208c28a/docs/project/G-PROPOSALS.md.
- Tests : Market346Tests (2 cas). Verify.ps1 (agent) : 670/670, 0 avertissement, 0 erreur ; DLL 0.346.0.0. Non validé en jeu.
### English
- Queue step g, I-06: NPC sale price (unit and hovered stack) in the item information. I-07: when you open an item's market offers,
  the lowest offer minus 1 (your retainers left out) is copied to the clipboard and announced; you paste and confirm yourself (off by
  default).

## 0.345.0
- Étape g), domaine 1, I-03 : vue de tous ses personnages. Choix de l'utilisateur : un fichier par personnage, vue d'ensemble, bouton
  « Effacer ce personnage », jamais d'identifiant de compte.
- Stockage :
  - la banque passe en un fichier par personnage (InventoryBank/<identifiant local du personnage>.json), chacun écrit par la même
    classe éprouvée (bornes, copie .bak, refus d'écraser un fichier abîmé) ;
  - l'ancien fichier commun est découpé une fois, dans un dossier temporaire déplacé en place seulement une fois complet, puis gardé
    à côté en « .migrated » ;
  - un fichier qui contient un autre personnage est refusé ;
  - un personnage effacé voit son fichier supprimé.
- Relevés : les sacs, les cristaux et les monnaies du personnage connecté sont enregistrés d'office comme l'arsenal (toutes les
  2 minutes, sans message), sous le nom « Prénom Nom (Monde) ».
  - Ce sont trois nouveaux types de rangement, ajoutés en dernier pour que les fichiers existants gardent leurs numéros.
  - Pour le personnage connecté, ils ne s'affichent qu'en direct : pas de double compte, et les stocks des objets ne changent pas.
- Nouvel onglet Rangements › Personnages :
  - par personnage : nom, dernier relevé, gils, objets des sacs, cristaux, servants enregistrés ;
  - « Voir » ouvre la banque sur ce personnage ;
  - « Effacer » (deux clics) supprime tout ce qui le concerne.
- Banque : un choix « Personnage » montre les rangements enregistrés d'un autre personnage (sans l'onglet direct).
- Aperçu de tri : il ne propose plus que les servants et les sacoches (liste de ce qui se trie, et non plus de ce qui ne se trie pas).
- Propositions et choix provisoires de l'étape g) réunis dans https://github.com/Shigu330/Tensho-Assistant/blob/024c426c1186a2e8f51e09087996a9c55208c28a/docs/project/G-PROPOSALS.md (demande de l'utilisateur : les revoir
  ensemble).
- Tests : Characters345Tests (3 cas) ; un doublon d'inclusion de 0.344 retiré du projet de tests. Verify.ps1 (agent) : 668/668, 0 avertissement, 0 erreur ;
  DLL 0.345.0.0. Non validé en jeu.
### English
- Queue step g, I-03: the bank is now one file per character (the former shared file is split once and kept as .migrated); bags,
  crystals and currencies are recorded like the armoury; new Storage › Characters tab (gil, bag items, crystals, retainers, « See »,
  two-click « Erase »); the Bank can show another character's saved storages.

## 0.344.0
- Étape g), domaine 1, I-02 : soldes des monnaies et alertes de plafond. Choix de l'utilisateur : barre du serveur et alarme Tensho,
  Discord si R-12 est réglé. Fait en ultracode.
- Préparation : sonde des données et des fonctions du jeu (docs/evidence/0.344-currencies.md), puis un workflow de conception.
  - Il a étudié ce que font les autres plugins (CurrencyAlert MIT, Currency Watchdog sans licence, LootView, Altoholic : idées
    seulement, rien de copié) et vérifié l'intégration dans le code.
- Nouvel onglet Rangements › Monnaies :
  - solde, plafond, pourcentage et seuil d'alerte par monnaie ;
  - mémoquartz limités de la semaine ;
  - interrupteurs « Alertes de plafond » et « Dans la barre du serveur », avec infobulles et ↺.
- Lecture sur le thread du jeu toutes les 5 s, une fois connecté :
  - sceaux de la compagnie actuelle au plafond de son rang (GetCompanySeals / GetMaxCompanySeals) ;
  - les monnaies tenues par le CurrencyManager (GetItemCount / GetItemMaxCount) ;
  - les autres (GetInventoryItemCount, plafond = taille de pile) ;
  - chaque fonction n'est appelée que si le jeu l'a trouvée, et chaque lecture est gardée.
- Alerte : seuil par défaut à 80 % du plafond (0 = aucune alerte).
  - Une seule alerte quand le solde passe le seuil vers le haut ; elle revient après être repassé 2 points sous le seuil.
  - Un message dans le chat et le son des alarmes (silence en mission compris), un message Discord dans la nouvelle catégorie
    « Monnaies », et une entrée « Tensho Assistant · Monnaies » dans la barre du serveur ; un clic ouvre l'onglet.
- Relecture adverse (4 angles, 2 vérificateurs par remarque) : 6 remarques confirmées, 3 contestées, toutes corrigées.
  - À la connexion, le jeu répond 0 avant le vrai solde : cela faisait une fausse alerte (chat et Discord). Les soldes sont maintenant
    inconnus tant que le conteneur Monnaies n'est pas chargé, et un 0 n'arme plus l'alerte.
  - Seuil enregistré en quittant le champ, et non à chaque frappe.
  - Plafond inconnu affiché « inconnu » (et non « pas de plafond »).
  - L'onglet n'affiche plus l'ancien personnage.
  - Seuils de 1 % et 2 % réarmés.
  - Changer un seuil ne réarme plus que cette monnaie.
  - Une erreur qui se répète n'est écrite qu'une fois dans le journal.
  - « Pas de mémoquartz limité en ce moment » au lieu de « inconnu ».
- Une seconde passe sur ces correctifs a trouvé 3 cas de plus, corrigés :
  - un vrai solde à 0 (deux lectures à 0 de suite) arme l'alerte, sans réveiller le faux 0 de la connexion ;
  - un seuil tapé dans un champ qui a disparu n'est plus affiché comme enregistré ;
  - le pourcentage est arrondi vers le bas, en accord avec l'alerte.
- Choix provisoires notés pour la fin (ROADMAP) : 80 % partout, rien sans compagnie, rien au chargement, liste des monnaies suivies.
- Tests : Currencies344Tests (4 cas). Verify.ps1 (agent) : 665/665, 0 avertissement, 0 erreur ; DLL 0.344.0.0. Non validé en jeu.
### English
- Queue step g, I-02: Storage › Currencies (balances, caps, per-currency threshold, weekly limited tomestones) with cap alerts — once
  per upward crossing (80 % by default), chat and alarm sound, Discord « Currencies » category, server bar entry. Reads on the
  framework thread, pointer-checked; balances unknown until the Currency container is loaded (no false alert at login).

## 0.343.0
- Étape g) de la file, domaine 1 (inventaire et marché), I-01 : infobulle de prix complète (choix de l'utilisateur : prix au survol
  seulement, pas de préchargement, touche d'actualisation).
- La réponse Universalis déjà demandée donnait plus que ce qui était affiché ; Tensho lit maintenant :
  - le prix moyen de vente et les ventes par jour, sur votre monde ;
  - l'offre la moins chère de la région, avec son monde.
  - Le cache disque passe en v3 : les anciennes copies, qui n'ont pas ces champs, sont redemandées.
- Infobulle du survol :
  - sous chaque prix minimum, « Dernière vente (date) · moyenne · ventes par jour », avec « — » quand c'est inconnu ;
  - « Pile de N : ~X », pour une case de vos sacs (quantité lue dans le jeu, case vérifiée : elle doit contenir l'objet survolé) ;
  - « Le moins cher de la région », seulement quand c'est moins cher que le centre de données.
- Ctrl+Alt pendant le survol redemande le prix, même récent. C'est une action du joueur : une demande par appui, 5 s au moins entre deux.
  - Réglage dans Paramètres › Avancé, avec infobulle et ↺, activé par défaut.
  - Choix de la combinaison noté pour la fin (ROADMAP).
- Tests : HoverPrices343Tests (2 cas) ; deux anciens tests suivent l'étiquette v3 et la nouvelle ligne du lecteur de survol.
  Verify.ps1 (agent) : 661/661, 0 avertissement, 0 erreur ; DLL 0.343.0.0. Non validé en jeu.
### English
- Queue step g, inventory and market, I-01: the hover price now shows last sale, average sale price, sales per day, the region's
  cheapest listing and the hovered stack's value; Ctrl+Alt while hovering refreshes the price (one request per press, setting).

## 0.342.0
- Étape f) de la file, point 7 (DEC-023, R-09) : événements saisonniers. Demandes de l'utilisateur pendant la version : les boutiques
  d'événement, les bons noms, et vérifier si un plugin fait déjà pareil.
- Sondes (docs/evidence/0.342-festivals.md) :
  - le jeu donne les événements en cours (GameMain.ActiveFestivals) et leurs quêtes (Quest.Festival), mais ni nom ni date ;
  - les calques d'événement des décors (planevent.lgb, FestivalID) donnent les PNJ de chaque fête, et l'annuaire des marchands leurs
    boutiques (ex. « vendeuse de la Veillée des saints », boutique 263293).
- Noms :
  - LuminaSupplemental et Brio (GPL-3.0) ont les mêmes noms anglais, et aucun pour 256, 257, 261 et 263 ;
  - en français, le nom vient du PNJ guide de la fête (dialogue interne « Fes…Guide ») : « démarcheur de la Veillée des saints » donne
    « Veillée des saints » ;
  - sinon le nom anglais, sinon « nom inconnu ». La page dit toujours d'où vient le nom.
  - Les drapeaux techniques (« Special Event Flag », ou ni quête ni PNJ) sont masqués ; leur nombre est affiché en mode développeur.
- Nouvelle page Progression › « Événements » :
  - les événements en cours selon le jeu ;
  - leurs quêtes, avec ✓ / en cours / à faire, gils et objets en récompense, et « Ouvrir » vers la page Quêtes ;
  - « Boutiques de l'événement » : chaque vendeur de la fête, ses villes, et ce qu'il vend avec le prix.
  - Pas de dates à venir : aucune source fiable.
- Lecture sur le thread du jeu toutes les 10 s, une fois connecté et seulement pendant que la page est ouverte.
  - Les décors sont lus une fois (environ 1 100 fichiers, environ 1 s), sur un worker, à la première ouverture.
  - Chaque fichier est vérifié avant d'être lu, comme pour l'annuaire des marchands.
- Les noms anglais (LuminaSupplemental, GPL-3.0) sont embarqués avec leur avis ; crédit mis à jour.
- Tests : Events342Tests (2 cas). Verify.ps1 (agent) : 659/659, 0 avertissement, 0 erreur ; DLL 0.342.0.0. Non validé en jeu.
### English
- Queue step f, point 7 (R-09): new Events page — running festivals from the game, their quests with progress and rewards, and the
  event shops (festival NPCs from the planevent layouts, matched in the vendor directory). French names come from each festival's guide
  NPC, else English names from LuminaSupplemental; technical flags are hidden. No upcoming dates (no reliable source).

## 0.341.0
- Demande de l'utilisateur (capture d'écran) : un onglet propre au chocobo, et retirer l'outil Personnage, qui ne sert plus puisque
  Équipement fait la même chose.
- Nouvelle page « Chocobo » (Progression), qui reprend la barde de 0.340 jusque-là en bas d'Équipement.
  - Le tableau prend toute la hauteur.
  - Hors connexion, la page le dit (au lieu de « Lecture du chocobo… »).
- Page Personnage retirée de la navigation et de l'accueil. La tuile d'accueil mène à Équipement ; l'annuaire des outils
  (« Character Panel Refined ») aussi.
  - Gardé : le panneau à côté de la fenêtre Personnage du jeu (réglage existant, validé en jeu), qu'Équipement n'a pas.
  - Décision de l'utilisateur (2026-11-02) : le panneau est gardé.
  - Une configuration qui rouvrait la page Personnage s'ouvre sur l'accueil.
- Corrections vues sur la capture :
  - « ✗ » s'affichait « = » (caractère absent de la police) : ✓ et ✗ viennent maintenant de la police d'icônes ;
  - la croix « possédé » passait sous le nom de l'objet : elle reste sur sa ligne.
- Tests : Chocobo341Tests (1 cas). Verify.ps1 (agent) : 657/657, 0 avertissement, 0 erreur ; DLL 0.341.0.0. Non validé en jeu.
### English
- User request: chocobo barding moved to its own « Chocobo » page; the Character page is removed (the Gear page shows the same real
  values; the panel beside the game's Character window stays). Unlocked mark now from the icon font (« ✗ » showed as « = »), owned
  mark kept on the item's line.

## 0.340.0
- Étape f) de la file, point 5 (DEC-023 point 5, R-11 étape 4) : barde de chocobo.
- Sonde (docs/evidence/0.340-chocobo-barding.md) :
  - BuddyEquip n'a que l'apparence (noms, modèles, icônes), aucune caractéristique ;
  - les 101 objets qui apprennent une barde ont l'action 1013, et Data[0] est la ligne de la barde ;
  - le jeu donne la barde portée (CompanionInfo) et Dalamud l'état de déblocage (IUnlockState).
- Page Équipement, nouvelle section « Barde de chocobo » :
  - nom, rang et couleur du chocobo ;
  - barde portée sur la tête, le corps et les jambes (ordre supposé, dit dans l'infobulle) ;
  - « n bardes débloquées sur m » ;
  - liste avec icônes, Grande Compagnie, ✓ / ✗ (« ? » tant que ce n'est pas lu) et l'objet qui l'apprend, avec la marque de possession ;
  - recherche et « Seulement celles qui manquent ».
  - Lu sur le thread du jeu toutes les 2 s, une fois connecté et seulement pendant que la section est ouverte. Lecture gardée.
- Escouade (même étape) : apparence seulement, rien à planifier. Le défaut recommandé (la retirer) est appliqué ; la décision reste
  dans la liste de la fin (ROADMAP).
- Tests : Barding340Tests (2 cas). Verify.ps1 (agent) : 656/656, 0 avertissement, 0 erreur ; DLL 0.340.0.0. Non validé en jeu.
### English
- Queue step f, point 5 (R-11 step 4): chocobo barding section on the gear page (worn barding, unlocked bardings, the item that teaches
  each, read on the framework thread while shown). Squadron gear is appearance only and was left out (recommended default).

## 0.339.0
- Demande de l'utilisateur (capture d'écran) : les matérias XI et XII, celles qu'on pose aujourd'hui, affichaient « taux inconnu ».
- Cause : les feuilles du jeu MateriaJoinRate et MateriaJoinRateGatherCraft s'arrêtent au grade X. La copie de xivapi n'a aussi que
  les lignes 0 à 9.
- Correction : pour XI et XII, Tensho prend les taux de FFXIV Teamcraft (MIT) et l'affiche « selon Teamcraft ».
  - Les taux I à X de Teamcraft sont identiques à ceux du jeu.
  - XI suit les grades impairs (12/6/3/2 % NQ, 17/10/7/5 % HQ) et XII les grades pairs (12 % NQ, 17 % HQ, puis 0).
  - Les grades I à X restent lus dans le jeu ; l'infobulle explique la différence.
  - Teamcraft est crédité pour ces fichiers. Preuve : docs/evidence/0.338-overmeld-rates.md.
- Tests : Overmeld338Tests mis à jour (XI et XII). Verify.ps1 (agent) : 654/654, 0 avertissement, 0 erreur ; DLL 0.339.0.0. Non validé en jeu.
### English
- Overmeld rates of grades XI and XII (the game sheets stop at X) now come from FFXIV Teamcraft, whose rates I to X match the game,
  and are labelled « per Teamcraft » instead of « rate unknown ».

## 0.338.0
- Étape f) de la file, point 4 (DEC-023, point 8) : taux de réussite de la surmatérialisation.
- Sonde (docs/evidence/0.338-overmeld-rates.md) : les feuilles du jeu MateriaJoinRate (combat) et MateriaJoinRateGatherCraft
  (artisanat et récolte) donnent, par grade (lignes 0 à 9 = I à X), le taux NQ et HQ des quatre emplacements de surmatérialisation.
  - Exemple, grade I : 80/40/20/10 % (NQ), 90/48/28/16 % (HQ).
  - Pas de ligne pour XI et XII : taux affiché « inconnu ».
- Équipement :
  - l'éditeur de matérias affiche le taux de chaque grade dans un emplacement avancé, puis celui de la matéria choisie, avec une infobulle ;
  - l'infobulle des emplacements donne le taux des matérias déjà posées en surmatérialisation.
- Règle corrigée : les grades VI, VIII, X et XII vont dans le premier emplacement de surmatérialisation seulement, et non « jamais »
  (règle de 0.312, sans preuve).
  - Preuve : 12 % / 17 % au premier emplacement et 0 % ensuite dans la feuille, et le wiki pour XII.
  - Ces grades sont maintenant proposés dans le premier emplacement avancé.
- Tests : Overmeld338Tests (2 cas) ; le test de 0.312 suit la nouvelle règle. Verify.ps1 (agent) : 654/654, 0 avertissement, 0 erreur ; DLL 0.338.0.0.
  Non validé en jeu.
### English
- Queue step f, point 4: overmeld success rates read from the game sheets (MateriaJoinRate, MateriaJoinRateGatherCraft) and shown per
  advanced socket and piece quality; grades VI, VIII, X and XII are now allowed in the first advanced socket only (was « never »).

## 0.337.0
- Étape f) de la file, point 3 (DEC-023), seconde partie : planificateur de trajets de dirigeable par distance et durée. Accord de
  l'utilisateur pour appeler les fonctions de calcul du jeu (ultracode).
- Calcul des étapes (FreeCompany/AirshipVoyageTable.Dalamud.cs) :
  - le jeu calcule lui-même chaque étape (HousingManager.GetAirshipVoyageTimeAndDistance) et chaque relevé (GetAirshipSurveyDuration),
    sans rien envoyer au serveur ;
  - appel seulement sur le thread du jeu, une fois connecté, après vérification que les deux fonctions sont trouvées, une table par vitesse ;
  - un échec rend le calcul « indisponible » pour la session ; une étape ou un relevé à 0 reste inconnu.
- Assemblage d'un voyage : étapes depuis le départ, relevés et leur distance, 12 h fixes, sans retour, en secondes.
  - Non confirmé en jeu, et la page le dit.
  - Le jeu ne garde pas le trajet d'un dirigeable, donc aucune vérification automatique n'est possible.
  - Valeurs brutes affichées en mode développeur.
- Page Compagnie › Secteurs de dirigeable :
  - dirigeable lu (rang, vitesse, autonomie) ou saisie manuelle ;
  - colonne « Choisir » : jusqu'à 5 secteurs, mesurés dans l'ordre le plus court (distance, durée, expérience, céruleum) ;
  - « Meilleurs trajets » : expérience par jour, expérience par voyage ou plus court, cherchés sur un worker.
  - Butin non chiffré. Les secteurs sans expérience dans les données du jeu (0) donnent « ≥ » et ne sont classés que par « Plus court ».
- Relecture adverse de 0.335 à 0.337 (4 angles, 2 vérificateurs par remarque) : 8 remarques confirmées et 1 contestée, toutes corrigées ; une seconde passe sur les correctifs a trouvé 1 cas de plus (liste de prix finie entre deux images), corrigé.
  - Prix des trajets de sous-marin gardés à part, pour que le cache partagé de 128 prix ne les fasse pas revenir « manquants ».
  - Un objet sans offre NQ n'est plus redemandé en boucle.
  - Le compte des prix manquants suit le palier de surveillance.
  - Expérience inconnue jamais comptée 0.
  - Infobulles manquantes ajoutées (y compris sur une case grisée).
  - Hauteur minimale du tableau des secteurs.
  - Recherche annulée à la fermeture.
  - Deux protections en plus : demande filtrée avant comparaison, relevé à 0 tenu pour inconnu.
- Tests : Airships337Tests (5 cas). Verify.ps1 (agent) : 652/652, 0 avertissement, 0 erreur ; DLL 0.337.0.0. Non validé en jeu.
### English
- Queue step f, point 3, second part: airship route planner by distance and duration. Leg and survey times come from the game's own
  functions (framework thread, logged in, pointer-checked, one table per speed); how a voyage adds them up is not confirmed in game and
  is labelled so. Adversarial review of 0.335–0.337: 9 findings fixed (route price cache, no-listing items, band-aware missing count,
  unknown airship experience, tooltips, table height, cancellation on close).

## 0.336.0
- Étape f) de la file, point 3 (DEC-023), première partie : secteurs de dirigeable.
- Données embarquées : l'arbre de déblocage (AirshipUnlock, 24 lignes) et le butin possible (AirshipDrop, 261 lignes, sans quantité
  ni taux) de LuminaSupplemental (Critical-Impact, GPL-3.0, même licence que Tensho).
  - Commit bd265c2b ; contenu publié gardé tel quel.
  - En-tête de chaque fichier : source, empreinte SHA-256 et avis GPL-3.0.
  - Crédit embarqué ajouté (Paramètres › Crédits), README et REFERENCES à jour.
- Nouvel onglet Compagnie › « Secteurs de dirigeable », pour les 23 secteurs de la Mer de nuages (le Diadème est écarté) :
  - rang, réservoirs de céruleum, durée de relevé de base et expérience, tirés des données du jeu ;
  - « Débloqué par » (avec la surveillance demandée) et, au survol, le chemin depuis le départ ;
  - « n objets » de butin possible, listés avec leur icône au survol ;
  - un filtre par nom de secteur ou d'objet et un filtre par rang.
  - La valeur d'un voyage reste inconnue : aucune quantité n'est publiée.
- Sonde de la feuille AirshipExplorationPoint : PROBE_AIRSHIP dans tools/ShopCurrencyProbe. Preuve :
  docs/evidence/0.336-airship-sectors.md.
- Planificateur de distance et de durée : pas encore fait.
  - Les seules formules publiques sont des messages de forum de 2015, non confirmés.
  - Le jeu a ses propres fonctions de calcul (HousingManager.GetAirshipVoyageTimeAndDistance, GetAirshipSurveyDuration), mais les
    appeler exécute du code du jeu : version suivante, après accord de l'utilisateur.
- Tests : Airships336Tests (3 cas). Verify.ps1 (agent) : 647/647, 0 avertissement, 0 erreur ; DLL 0.336.0.0. Non validé en jeu.
### English
- Queue step f, point 3, first part: the airship unlock tree and possible loot of LuminaSupplemental (GPL-3.0) are embedded with their
  notice and shown in a new « Airship sectors » tab (rank, ceruleum, base survey time, experience, unlock, loot list without quantity).
  The distance/duration planner waits for the user's go-ahead, as it would call the game's own voyage functions.

## 0.335.0
- Étape f) de la file, point 2 (DEC-023) : valeur des trajets de sous-marin (Compagnie › Meilleurs trajets).
- Prix du marché : nouveau choix « Prix » : « Revente au PNJ » (par défaut, comme avant) ou « PNJ ou marché (le plus haut) ».
  - Pour chaque objet vendable au marché, on prend le prix minimum NQ de votre monde (Universalis) s'il est connu, frais et plus haut que
    le prix PNJ ; sinon le prix PNJ.
  - Un objet sans prix connu garde le prix PNJ et est compté comme manquant : la valeur s'affiche alors « ≥ … ». Jamais de prix inventé.
  - Le bouton « Prix du marché (n sur m manquants) » récupère les prix qui manquent pour le butin des trajets listés, les plus fréquents
    d'abord, 32 au plus par clic. Il suit la même cadence et le même cache que le coût de préparation, respecte l'interrupteur
    Universalis, et le classement est refait quand la liste se termine.
  - Au survol d'un trajet, chaque objet donne son prix PNJ et son prix de marché.
- Paliers (sonde d'abord, docs/evidence/0.335-submarine-tiers.md) : la source sépare bien ses relevés par surveillance du sous-marin
  (Min / Mid / High, dans chaque pool Tier1 à Tier3 ; les comptes se recoupent sur les 143 secteurs).
  - Nouveau choix « Surveillance » : tous les relevés (par défaut, comme avant), sous le palier 2, palier 2 ou palier 3.
  - Un secteur sans relevé à ce palier garde la moyenne de tous ses relevés, signalé au survol.
  - Récupération non modélisée : la source ne donne par palier que des fourchettes de quantité, pas des totaux.
- Données : tools/submarine-loot.py garde aussi les pools par palier. Même fichier source qu'en 0.309 (même SHA-256), donc base
  inchangée ; submarine-loot.json passe de 24 à 55 Ko.
- PreparationPriceSession gagne StartListAsync (liste bornée hors plan d'artisanat).
- Tests : Routes335Tests (4 cas). Verify.ps1 (agent) : 644/644, 0 avertissement, 0 erreur ; DLL 0.335.0.0. Non validé en jeu.
### English
- Queue step f, point 2: submarine route value can use the best of the NPC price and your world's lowest market price (Universalis,
  fetched on a click, 32 items at most, missing prices shown as « ≥ »), and the loot of one surveillance tier, since the community
  source splits its records by tier. Retrieval tiers are not modelled (the source only gives ranges).

## 0.334.0
- Étape f) de la file, point 1 (DEC-023) : la table des seuils de butin des sous-marins (SubmarineTracker) est gardée ; sur les 19 secteurs
  où elle est plus basse que toutes les observations de FFXIVGachaSpreadsheet (81, 86, 92, 98, 105, 109, 111, 112, 121, 125, 126, 130,
  132, 133, 144 à 148, docs/evidence/0.315-vessel-builds.md), la page Montages affiche « Palier non confirmé par les relevés sur ce
  trajet », avec une infobulle qui explique et liste les secteurs.
- Point 6, seconde moitié : livraisons des Planeurs d'Ehcatl laissées de côté, rien de lisible dans le jeu (sonde,
  docs/evidence/0.333-ehcatl-probe.md), comme DEC-023 le prévoit.
- Tests : Sectors334Tests (1 cas : 19 secteurs, tous présents dans la table embarquée). Verify.ps1 (agent) : 640/640, 0 avertissement,
  0 erreur ; DLL 0.334.0.0. Non validé en jeu.
### English
- Queue step f, point 1: the community submarine breakpoints are kept, and the 19 sectors where they are below every record are
  flagged « tier not confirmed by records » on the Builds page. Ehcatl Nine deliveries left aside: nothing readable in the game.

## 0.333.0
- Étape f) de la file, point 6 (DEC-023) : rappels de l'escouade de Grande Compagnie, mission ET entraînement (demande de l'utilisateur).
- Sonde : l'heure de fin n'est pas dans le gestionnaire de l'escouade mais dans PlayerState (SquadronMissionCompletionTimestamp,
  SquadronTrainingCompletionTimestamp, ActiveGcArmyExpedition, ActiveGcArmyTraining), disponible toute la session. Trouvé en regardant
  les autres plugins (MogHouse Companion, Accountant : lecture seule, rien de copié). Preuve : docs/evidence/0.332-squadron-probe.md.
- Rappels : deux lignes, « Mission d'escouade » et « Entraînement d'escouade » : nom tiré des données du jeu, « en cours » ou « terminé
  (à récupérer à la caserne) », heure de fin, et la cloche pour créer une alarme (et Discord, catégorie Rappels). Lecture gardée à part :
  si elle échoue, ces deux lignes disent « inconnu » et le reste des Rappels continue (R-14).
- Reporté à la fin : « équipement de l'escouade » (R-11 étape 4) n'a pas d'objet, ce n'est que de l'apparence ; recommandé : le retirer
  et garder la barde de chocobo (ROADMAP).
- Tests : Squadron333Tests (1 cas). Verify.ps1 (agent) : 639/639, 0 avertissement, 0 erreur ; DLL 0.333.0.0. Non validé en jeu (valeurs
  des heures à vérifier).
### English
- Queue step f, point 6: Grand Company squadron reminders, mission and training. Their end times are read from PlayerState (found by
  looking at other plugins, nothing copied) and shown in the Reminders with an alarm button; a failed read leaves them unknown.

## 0.332.0
- Étape e) de la file, dernier point : cadres et textures du jeu (ULD), première version, avant D17.
- Nouvelle sonde en lecture seule tools/UldProbe : quels fichiers d'interface du jeu existent, les textures qu'ils utilisent, leurs
  tailles et le découpage que le jeu déclare. Preuve : docs/evidence/0.332-uld-window-frame.md (cadre des fenêtres WindowA_BgNormal,
  coins, bords et centre, versions _hr1 au double).
- Nouveau réglage « Cadre des fenêtres du jeu » (Paramètres › Interface, infobulle et ↺), désactivé par défaut : il dessine derrière la
  fenêtre principale le cadre des fenêtres du jeu, découpé en neuf morceaux comme le jeu le fait. Si une texture manque (mise à jour du
  jeu), rien n'est dessiné et la fenêtre garde son aspect (R-14).
- Reporté à la fin : l'activer par défaut, l'étendre à la carte et aux panneaux, ajouter les séparateurs du jeu, après l'avoir vu en jeu
  (ROADMAP).
- Tests : GameFrame332Tests (1 cas). Verify.ps1 (agent) : 638/638, 0 avertissement, 0 erreur ; DLL 0.332.0.0. Non validé en jeu (aspect
  jamais vu).
### English
- Queue step e, last item: game frames (ULD), first version. New read-only probe tools/UldProbe proves the game's window frame
  textures and their nine-piece cut. New opt-in setting « Game window frame » draws that frame behind the main window; nothing is
  drawn if a texture is missing. Turning it on by default and extending it waits for an in-game look.

## 0.331.0
- Étape e) de la file : outils restants (alarmes et Discord, compagnie libre, quêtes, collections, butin, équipement, Revue de mode,
  mini-jeux, accueil). Audit en lecture seule, un relecteur par domaine, puis un vérificateur : 57 défauts confirmés sur 58, corrigés par
  5 agents sur des fichiers séparés. Sauvegarde : checkpoints/backup-20261102-212918-avant-outils-0331.zip.
- Alarmes :
  - une commande /alarm en file n'est plus envoyée au jeu après la suppression de son alarme ; un nom de jeu déjà pris n'est plus
    envoyé deux fois ;
  - une alarme copiée dans le jeu dont la copie a disparu peut de nouveau sonner par Tensho (bouton « Faire sonner par Tensho ») ;
  - une alarme météo n'est plus sautée quand le catalogue de météo n'est pas encore chargé ; une alarme sans avance ne se rate plus
    sur une image lente ;
  - Discord : la pause demandée par Discord est affichée, et « Effacer » ou une nouvelle adresse l'annulent ; les retours des vaisseaux
    suivent la catégorie « Compagnie libre » ;
  - infobulles des cloches avec le vrai délai ; barre du serveur qui signale les créneaux soumis à la météo.
- Compagnie libre : lectures de l'atelier isolées (R-14) ; catalogues en échec dits ; sous-marin choisi stable ; une compagnie
  quittée peut être oubliée (deux clics) ; rang du plan enregistré une seule fois.
- Quêtes, collections, butin : le panneau du Duty Finder a son propre service et ne change plus le contenu choisi sur la page Butin ;
  cache « non obtenus seulement » de la page Butin à jour ; états après la déconnexion corrigés ; lectures gardées (R-14).
- Équipement : un import sans pièce utilisable n'efface plus le plan ; matérias suivies quand la pièce change ; « Vider le plan » vide
  aussi les matérias ; anneau unique pas deux fois ; liste des pièces mise en cache.
- Mini-jeux et coque : « Tout remettre par défaut » remet aussi le mode développeur affiché ; lecture des PNJ battus gardée (R-14) ;
  textes corrigés (dont un « tu »).
- Relecture (3 relecteurs et un vérificateur) : 8 remarques confirmées sur 12, 7 corrigées : rappel par défaut enregistré une fois
  (plus à chaque frappe), mode développeur annulé si l'enregistrement échoue et bouton ↺, orchestrion illisible laissé « inconnu », fenêtre
  de l'orchestrion alignée sur le badge, cartes Triple Triad ignorées signalées, « encore en lecture » seulement pendant une lecture,
  description des Collections corrigée.
- Reporté : rappels « fait » et carte au trésor par personnage (format de sauvegarde à changer, prévu dans les propositions T1).
- Reporté aussi : un anneau unique importé à côté du même anneau porté peut encore compter deux fois (relecture 0.331, faible).
  30 propositions à choisir dans https://github.com/Shigu330/Tensho-Assistant/blob/024c426c1186a2e8f51e09087996a9c55208c28a/docs/project/TOOLS-PROPOSALS.md (recommandées : T0 à T4).
- Tests : Tools331Tests (3 cas) ; un test 0.326 adapté. Verify.ps1 (agent) : 637/637, 0 avertissement, 0 erreur ; DLL 0.331.0.0.
  Non validé en jeu.
### English
- Queue step e: remaining tools audit (57 confirmed defects), fixed. Alarms: a queued /alarm command is no longer sent after its alarm
  is deleted; a game copy that disappeared can ring in Tensho again; weather alarms wait for the weather catalogue; Discord pauses are
  shown and cleared. Free company reads isolated (R-14). The Duty Finder panel has its own loot service. Gear plans are no longer
  erased by an empty import. Restore-all resyncs developer mode. 30 improvement proposals in https://github.com/Shigu330/Tensho-Assistant/blob/024c426c1186a2e8f51e09087996a9c55208c28a/docs/project/TOOLS-PROPOSALS.md.

## 0.330.0
- Étape e) de la file : artisanat et récolte. Audit en lecture seule : 28 défauts confirmés (environ 20 distincts), corrigés par 4 agents.
  Le plus grave : « Lancer la chaîne de macros » pouvait envoyer au jeu les macros d'une autre recette ; il n'apparaît plus que sous les
  pages écrites par la dernière copie réussie, pour le même personnage. Le plan des séquences est rechargé (plus ajouté) ; seules les
  macros nommées exactement « préfixe page/pages » sont considérées comme celles de Tensho (vos macros ne sont plus écrasées).
- Récolte : le plan de PR n'invente plus une valeur non lue et ne se recalcule plus en cours de point ; horaires traduits ; une fenêtre
  qui passe minuit ne crée plus d'alarme à 00:00 ; téléchargement GatherBuddy arrêté par son interrupteur et réessayé après un délai.
- Paramètres : ↺ des observations ; observation de récolte décochée qui reste décochée ; « Tout remettre par défaut » efface le texte
  de macros préparé ; message du chat avec les vrais emplacements.
- Relecture (3 relecteurs et un vérificateur) : 3 remarques confirmées, soit 2 défauts distincts, corrigés :
  - une copie faite sans enchaînement ne remplace plus le plan de séquences enregistré ;
  - un bonus d'Abondance inconnu (niveau 68 et plus) ne bloque plus le plan : il compte le minimum de la règle, +1, et le panneau le dit.
- 37 propositions à choisir dans https://github.com/Shigu330/Tensho-Assistant/blob/024c426c1186a2e8f51e09087996a9c55208c28a/docs/project/CRAFT-GATHER-PROPOSALS.md. Tests : CraftGather330Tests (3 cas). Verify.ps1 (agent) :
  634/634, 0 avertissement, 0 erreur ; DLL 0.330.0.0. Non validé en jeu.
### English
- Queue step e: crafting and gathering audit, fixed. « Start the macro chain » only runs the pages written by the last successful copy;
  only macros named exactly « prefix page/pages » are Tensho's. The GP plan never guesses an unread value; schedules are translated;
  midnight windows ring once; an unknown Bountiful bonus uses the rule's +1 floor and says so. Review findings fixed.

## 0.329.0
- Étape e) de la file : objets, banque, tri et marché. Audit en lecture seule (4 agents, puis un vérificateur) : 41 défauts confirmés,
  environ 33 distincts. Corrigés par 4 agents sur des fichiers séparés, puis relus (3 relecteurs et un vérificateur : 6 remarques
  confirmées, corrigées). Sauvegarde : checkpoints/backup-20261102-173157-avant-objets-0329.zip.
- Le plus important (R-14) : une lecture secondaire du jeu en échec (coffre à apparence, armoire, liste des servants…) remettait le
  personnage à 0 dans tout le plugin : banque vide, stocks inconnus, tri refusé, chaîne de craft effacée. Le personnage reste maintenant
  connu, chaque lecture est isolée, et un avis durable dit qu'un rangement est illisible. La lecture de l'ordre visuel est gardée aussi :
  les quantités restent relevées, et le tri refuse toujours un ordre absent.
- Banque :
  - « Enregistrement automatique » ne concerne plus que les servants et les sacoches, comme son libellé le dit ; l'arsenal, le coffre
    à apparence et l'armoire (sans bouton d'import) sont relevés dans tous les cas, sans message « import » parasite ;
  - un coffre à apparence plein s'enregistre et se recharge (limite par rangement relevée, identique à l'enregistrement et au
    chargement) ;
  - titres des cadres avec la date, numéro de sac en tête ; états vides qui disent la vraie raison ; aide de l'import corrigée.
- Stocks : en-tête de la fiche à jour ; « TOTAL » exact seulement sans copie en cache ; les cristaux comptent leur sac, et ceux des
  servants (jamais importés) sont une borne basse (≥) au lieu d'un zéro connu, sur la fiche, au survol, dans les recettes et la chaîne.
- Marché : la pause de 3 s d'Universalis n'affiche plus « prix indisponibles » au survol ni n'efface un prix affiché ; dates des
  prix exactes ; ligne du plan surlignée une seule fois ; durée du cache lisible.
- Objets : historique et fil d'Ariane dans le bon ordre et bornés ; « Utilisé dans » mis en cache avec un clipper ; message d'import
  limité ; aperçu du tri qui dit pourquoi il échoue (sans recalcul à chaque image) ; clic sur une case qui ne remplace plus la recherche.
- Textes et documents : aide du survol (Garland et le wiki sont bien interrogés), menu contextuel, page Diagnostics, ARCHITECTURE et
  GAPS corrigés.
- Reporté à la fin : inventaire vidé 250 ms à chaque mouvement (DEC-004, ROADMAP). 23 propositions à choisir dans
  https://github.com/Shigu330/Tensho-Assistant/blob/024c426c1186a2e8f51e09087996a9c55208c28a/docs/project/ITEMS-PROPOSALS.md (recommandées : I0, I1, I3, puis I4 et I14).
- Tests : Items329Tests (2 cas) ; un test du marché adapté (la pause n'est plus une erreur). Verify.ps1 (agent) : 631/631,
  0 avertissement, 0 erreur ; DLL 0.329.0.0. Non validé en jeu.
### English
- Queue step e: items, bank, sorting and market audit (about 33 distinct defects), fixed and reviewed. R-14: a failed secondary game
  read no longer signs the character out of every stock feature; reads are isolated and a lasting notice says which storage is
  unreadable. Armoury, glamour dresser and armoire are recorded whatever the auto-save setting; a full dresser saves and reloads.
  Retainer crystals count as a lower bound. The 3 s Universalis pause no longer shows an error or wipes a price. History, craft book
  cache, sort preview errors, bank texts and documents fixed. 23 improvement proposals listed in https://github.com/Shigu330/Tensho-Assistant/blob/024c426c1186a2e8f51e09087996a9c55208c28a/docs/project/ITEMS-PROPOSALS.md.

## 0.328.0
- Étape e) de la file, la carte d'abord. Audit de la carte en lecture seule (4 agents, puis un vérificateur) : 50 remarques, 45
  confirmées, soit 28 défauts distincts. Corrections faites par 4 agents sur des fichiers séparés, puis relues (3 relecteurs et un
  vérificateur : 8 remarques confirmées, corrigées). Sauvegarde : checkpoints/backup-20261102-162017-avant-carte-0328.zip.
- Le plus important : avec « Remplacer la carte du jeu », la touche M ne montrait plus aucune carte quand la carte Tensho ne pouvait pas
  s'afficher (catalogue en échec ou en chargement, masquage en combat ou en mission). La carte du jeu reste maintenant à l'écran dans
  ces cas. « Ouvrir la carte du jeu » depuis la carte Tensho ramène aussi une carte du jeu cachée.
- Résistance aux mises à jour (R-14) :
  - lectures des marqueurs et des coûts de téléportation bornées ;
  - chaque lecture secondaire isolée : une panne ne coupe plus la position ni le drapeau ;
  - une lecture des icônes en échec est dite sur la carte, au lieu d'une carte vide ; une image introuvable dit « indisponible » au
    lieu de « chargement » à vie ; une position inconnue est dite ;
  - un coût de téléportation illisible est dit au lieu de « Lecture du coût… ».
- Carte :
  - zoom +/−/curseur autour du centre de la vue (la vue glissait) ;
  - un clic sur un repère au bord ne déclenche plus l'icône dessous ;
  - les repères au bord suivent le drapeau et les icônes masqués ;
  - le survol d'une zone suit les interrupteurs des zones ;
  - une erreur de lien ne suit plus la souris partout ; elle s'affiche 5 s au survol de la carte ;
  - ouvrir la carte sur une carte choisie l'affiche entière ;
  - bouton ↺ par icône ; « Rétablir les valeurs par défaut » remet aussi le mode du clic droit ;
  - priorité et menu propre grisés pour les couches Tensho, le drapeau et les coffres (sans effet) ;
  - infobulles des réglages grisés ; échec d'enregistrement des réglages de carte affiché ;
  - erreur de lecture du wiki des panoramas dite, avec un nouvel essai au plus chaque minute ;
  - vendeurs et étiquettes mis en cache (ils étaient refaits à chaque image) ; listes du clic droit aussi.
- Listes, favoris, alarmes :
  - clic droit d'un panorama dans la liste : l'alarme est proposée ; numéros des panoramas alignés sur le carnet du wiki ;
  - alarme de panorama : une météo inconnue est dite, sans promettre « créneau et météo » ;
  - favoris : nom en double, trop long ou liste pleine dits comme tels (plus « Échec de sauvegarde ») ;
  - page Cartes : le bouton pose le drapeau et ouvre la carte (Tensho si elle remplace celle du jeu, sinon celle du jeu) ; libellé
    corrigé.
- Textes : « Niveau à côté des points de récolte » (le nom n'était pas écrit) ; aide des infobulles de quête ; boutique des crédits de
  compagnie traduite ; ARCHITECTURE et DECISIONS mis à jour.
- Reporté à la fin : identifiant 60561 de l'icône du drapeau à prouver en jeu (ROADMAP). 28 propositions d'amélioration de la carte,
  rien d'implémenté : à choisir dans https://github.com/Shigu330/Tensho-Assistant/blob/024c426c1186a2e8f51e09087996a9c55208c28a/docs/project/MAP-PROPOSALS.md (recommandées : P1, P2, P3, puis P4, P6, P8).
- Tests : Map328Tests (4 cas). Verify.ps1 (agent) : 629/629, 0 avertissement, 0 erreur ; DLL 0.328.0.0. Non validé en jeu.
### English
- Queue step e, the map first: a read-only map audit (28 distinct defects), fixed and reviewed. With « Replace the game map », M no
  longer shows no map at all when the Tensho map cannot draw (catalogue failed or loading, hidden in combat or duties): the game map
  stays. Native map reads are bounded and isolated per part (R-14); unreadable icons, textures, position and teleport costs are said.
  Zoom buttons keep the view centre; edge markers no longer trigger the icon under them and follow hidden layers; area hover follows
  the area switches; map errors no longer follow the mouse; per-icon reset; vendor labels and right-click lists cached. Bookmark,
  vista alarm and map page fixes; texts corrected. 28 improvement proposals listed for the user in https://github.com/Shigu330/Tensho-Assistant/blob/024c426c1186a2e8f51e09087996a9c55208c28a/docs/project/MAP-PROPOSALS.md.

## 0.327.0
- Étape e) de la file, 1re version : réglages « sans question » de l'audit du point d), tous avec infobulle et ↺. Par défaut, rien ne
  change. Fiches préparées par 4 agents en lecture seule, puis appliquées (90 modifications) :
  - nombre de résultats de la recherche d'objets (30 par défaut, 10 à 100) ; l'avis « N résultats au plus » suit la valeur ;
  - ouvrir sur la dernière page utilisée (désactivé : Accueil, comme avant) ;
  - portée de la page Marché mémorisée (centre de données par défaut) ;
  - masquer le panneau de survol en mission (désactivé par défaut) ;
  - quatre parties du panneau de survol (collection, stock, usages, comment l'obtenir), toutes activées ;
  - trois interrupteurs des aides Triple Triad en jeu (plateau, défi d'un PNJ, éditeur de decks), activés ;
  - calcul automatique de l'aide du carnet d'artisanat (activé ; sinon bouton « Calculer ») ;
  - alarmes : pas de ligne de discussion en mission (désactivé par défaut, sous « Pas de son en mission »).
- Écarté : « prix de la boutique » au survol (cette partie n'existe pas dans le panneau).
- Trois textes français au tutoiement corrigés (decks Triple Triad).
- Reporté à la fin : « Monstres au survol » ne commande plus rien (code mort) ; recommandé : le retirer (ROADMAP).
- Tests : 5 nouvelles classes (17 cas). Verify.ps1 (agent) : 625/625, 0 avertissement, 0 erreur ; DLL 0.327.0.0. Non validé en jeu.
- Relecture multi-agents en lecture seule (3 relecteurs puis un vérificateur ; relancée après la limite de dépense) : 9 remarques,
  6 confirmées (4 défauts distincts), corrigées :
  - carnet : couper « calcul automatique » pendant un calcul affichait un bouton « Calculer » qui ne faisait rien ;
  - Marché : une portée « Région » mémorisée dont le nom n'est pas encore connu disait « Monde inconnu » ; elle dit maintenant
    pourquoi (table d'Universalis pas encore lue, ou Universalis coupé) ;
  - Triple Triad : l'aide du match ne promet plus le cadre vert quand « Conseil en match » est décoché ;
  - recherche d'objets : l'avis ne conseille plus d'augmenter un nombre déjà à 100.
  Écartées (3) : les calculs Triple Triad déjà lancés finissent seuls en quelques secondes, comme quand la fenêtre de défi se ferme.
### English
- Queue step e (1/…): eight settings with tooltip and reset, defaults unchanged: item search result count, open on the last page,
  remembered Market scope, hide the hover panel in duties, four hover panel parts, three Triple Triad in-game aid switches, crafting
  log auto compute, alarm chat line muted in duties. Three French texts fixed. A read-only multi-agent review confirmed 4 small defects,
  fixed: a dead Compute button, a saved Region scope with no reason shown, a Triple Triad help text and the item search notice.

## 0.326.0
- Revue multi-agents du point d) (0.321 à 0.325), en lecture seule : 4 relecteurs (ImGui et état de l'interface ; services,
  réseau et threads ; exactitude des textes ; tests et scripts), puis un vérificateur chargé de réfuter chaque remarque. Base : le
  diff exact depuis la sauvegarde prise avant 0.321. 35 remarques, 33 confirmées (4 en double), toutes corrigées ici.
  Sauvegarde avant les corrections : checkpoints/backup-20261102-135157-avant-revue-d.zip.
- Interrupteurs de sources :
  - Garland (sources d'objets), wiki (mode d'obtention) et Teamcraft : les demandes déjà en file ne partent plus une fois
    l'interrupteur coupé. Le wiki téléchargeait encore une page toutes les 3 s, Teamcraft postait les lieux restants.
  - Ces demandes sont oubliées, donc redemandées quand l'interrupteur est rallumé. Avant, Garland répétait le refus 5 fois
    (attente de 3 s chaque fois, file bloquée environ 15 s par objet), puis gardait l'erreur jusqu'à la fin de la session.
  - Duty Finder : le contenu refusé par l'interrupteur est redemandé une fois quand Garland est rallumé.
  - Revue de mode : « Actualiser » est grisé quand sa source est coupée (il ne faisait rien sans rien dire).
  - Accueil : une source coupée n'est plus listée comme « service en erreur ».
  - Marché : la portée « Région » grisée dit que Universalis est coupé (elle parlait d'une table illisible, réessayée chaque minute).
  - Paramètres › Interface : la raison du grisé de « Monstres au survol » est l'infobulle de la case elle-même (elle passait sur ↺).
- Corrections de 0.325 :
  - Duty Finder : le cache des groupes suit l'état des collections ; « non obtenus seulement » ne reste plus vide après le
    chargement des collections, ni périmé après une obtention ;
  - alarmes : « Tout supprimer » est armé pour son groupe dans sa section seulement (le même objet dans deux sections de métier
    armait les deux). Il est désarmé dès que le groupe n'est plus affiché ;
  - « Ajouter (chaque jour ET) » affiche le délai réellement donné à l'alarme (60 min au plus).
- Corrections de 0.324 :
  - copie des macros : chaque ↺ garde début ≤ fin (le bornage annulait le ↺ de la fin) ;
  - Banque : les titres des cadres utilisent aussi le nom traduit du rangement.
- Textes :
  - les textes qui promettaient des copies affichées pour toutes les sources sont corrigés. C'est vrai pour le marché, le butin, les
    monstres du wiki, le carnet d'exploration et la Revue de mode ; c'est faux pour les sources d'objets, le mode d'obtention,
    Teamcraft, LuminaSupplemental et GatherBuddy. Sont concernés l'infobulle Universalis (les prix au survol sont masqués),
    « Source désactivée », SECURITY.md et l'entrée 0.324 ;
  - menus en français : « Paramètres › … » (le menu ne s'appelle pas « Réglages »), dans les textes et les listes de tests en jeu ;
  - la ligne Réglage de la page Crédits ne parle plus de réglage partiel ;
  - CONTRIBUTING : ordre réel de la release, client Teamcraft borné à la lecture depuis 0.321 ;
  - commentaire de MapViewSettings remis au-dessus de sa classe.
- Release :
  - Verify.ps1 -Release ne s'arrête plus sur un tag existant. Release.ps1 décide : il s'arrête si la release est complète (release
    GitHub et main), sinon il reprend les étapes manquantes, y compris main non poussé (repo.json jamais publié) ;
  - à la reprise, Devloppement est de nouveau sélectionné avant d'être avancé ;
  - les sondes gh passent par Test-Native : sous Windows PowerShell 5.1, « release not found » arrêtait le script (essayé ici
    sous 5.1 et 7 sur une commande factice) ;
  - les nouveaux fichiers aux noms accentués sont listés sans guillemets, donc l'archive ou le gros fichier est bien refusé ;
  - vérifié par l'analyse de syntaxe sous PowerShell 5.1 et 7 seulement : l'agent ne lance ni -Commit ni -Release.
- Tests :
  - le test Teamcraft vérifie la méthode, le User-Agent et le refus pour taille après la réponse (avant, une erreur dans le
    gestionnaire était avalée par le service et le test passait) ;
  - le cliquet des infobulles voit aussi les curseurs verticaux et les champs multi-lignes ; il s'arrête à tout autre élément
    dessiné (un bouton suivant ne couvre plus le contrôle) ; il échoue s'il voit moins de 150 contrôles (201 aujourd'hui) ;
  - les filtres bin/obj portent sur le chemin relatif (un dépôt cloné sous un dossier « bin » vidait les scans) ;
  - nouvelles infobulles : choix « Tous » des collections et des quêtes, zoom de la carte ;
  - ReviewFixes326Tests : 4 cas, dont un test de comportement Teamcraft (le lieu en attente n'est pas posté, puis il est redemandé).
- Écartées par le vérificateur (2) : « relevés » dans l'infobulle « Tout remettre par défaut » (formulation antérieure, non
  contredite par le code) ; le nom d'un test des brouillons GitHub.
- Verify.ps1 (agent) : 608/608, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.326.0.0. Non validé en jeu.
### English
- Read-only multi-agent review of queue step d (0.321–0.325): 33 confirmed findings, all fixed. Turning a source off now also stops
  the requests already queued for Garland item sources, wiki item pages and Teamcraft, and forgets them so they are asked again
  when the switch is back on (Garland retried the refusal for about 15 s per item and then kept the error). The Duty Finder asks
  again after Garland comes back, and its 0.325 cache now follows the collections state. « Delete all » of an alarm group is armed
  per job section and dropped when not shown. Fashion Report « Refresh » follows its switch; Home no longer lists a turned-off
  source as failed. Texts that promised cached copies for every source were corrected. The French menu is named « Paramètres ».
  Release: Verify.ps1 -Release no longer stops on an existing tag; Release.ps1 resumes when the GitHub release or main is missing,
  and its gh probes no longer abort Windows PowerShell 5.1. Non-ASCII new file names are checked. The tests are stronger: the
  Teamcraft checks can now fail, the tooltip ratchet is wider and guarded, and bin/obj filters use relative paths.

## 0.325.0
- Point d) de la file, 5/5 : infobulles des filtres et des champs, confirmations, petits restes (revue 0.316). Fait sans
  l'utilisateur, choix provisoires notés pour la fin.
- Cause : sur les pages, environ 115 filtres, recherches, tris et champs de saisie n'avaient pas d'infobulle (relevé par un
  scan du code). Le test des clés existant ne voyait ni l'emplacement d'une infobulle ni les clés construites.
- Infobulles : 140 nouvelles clés en français et en anglais (dont les textes des confirmations), posées sur les pages suivantes :
  - Banque, Collections, Succès, Orchestrion, Butin des contenus, Quêtes et sources de quêtes ;
  - Récolte et optimiseur de PR ;
  - Objets, simulateur et recherche d'artisanat, projection, plan, séquences, chaîne, enveloppe des conditions ;
  - Marché et coût de préparation ;
  - Cartes, favoris, listes locales, menus Région/Zone/Étage, tailles des icônes ;
  - Rangement (aperçu, règles, modèles) ;
  - Triple Triad (carnet, PNJ, simulation) ;
  - Revue de mode, Équipement (matérias), Météo, Monstres, Outils, Macros, Itinéraires de sous-marin.
- Méthode des textes : rédigés par l'audit en lecture seule, puis revérifiés sur le code actuel par 4 agents (un groupe de
  fichiers chacun). Une trentaine de textes ont été corrigés quand le code disait autre chose. Exemples :
  - les catégories de la Banque sont celles de l'onglet affiché ;
  - « Non confirmés » ne veut pas dire la même chose pour les titres et succès que pour les autres collections ;
  - « Tout afficher » et « Tout masquer » des icônes de carte n'agissent que sur les icônes filtrées.
- Libellés corrigés :
  - carte : « Tout remettre à 100 % » devient un libellé exact, avec une infobulle par portée. En réalité, le groupe revient à
    50 % et quelques priorités à ±1, et les réglages propres d'une carte sont gardés ;
  - quêtes : le filtre dit « Non confirmées » (l'état d'une quête reste « Non confirmée ») ;
  - Revue de mode : l'aide de recherche ne dit plus « nom français », car la recherche suit la langue du client.
- Duty Finder (panneau du butin) :
  - la recherche accepte l'identifiant exact d'un objet, comme son texte d'aide l'annonçait (seul le nom était comparé) ;
  - ↻ et la liste d'origine du butin ont leur infobulle ;
  - les comptes et les groupes sont calculés une fois par changement de filtre, au lieu d'à chaque image.
- Suppressions :
  - « Tout supprimer » d'un groupe d'alarmes demande un second clic (« Confirmer : supprimer N alarme(s) », ou Annuler) ;
  - la suppression d'une alarme, d'un deck, d'un modèle de rangement ou d'une séquence d'artisanat dit, au survol, qu'elle est
    immédiate.
- Alarme manuelle : nom, heure, minute et bouton « Ajouter » ont leur infobulle.
- Commentaire ShowVendors (Configuration.cs) mis d'accord avec la valeur réelle (activé par défaut).
- Nouveau test « à cliquet » (TooltipRatchet325Tests.cs, liste tests/TenshoAssistant.Tests/tooltip-baseline.txt) :
  - tout nouveau contrôle sans infobulle fait échouer Verify ;
  - une ligne de la liste qui ne correspond plus à rien aussi, pour que la liste ne fasse que diminuer ;
  - restent 3 exceptions : la grille du Mini Cactpot et la fenêtre « Règles du score » (aide en tête) ;
  - deuxième cas : une infobulle ne doit jamais devenir le corps d'un « if » autour d'un contrôle (le défaut de 0.287).
- Reporté à l'étape e) : mise en cache du plan et du coût de préparation (mesurés sous 0,5 ms par image en 0.292,
  https://github.com/Shigu330/Tensho-Assistant/blob/024c426c1186a2e8f51e09087996a9c55208c28a/docs/evidence/0.292-draw-times.md), à mesurer en jeu avant de changer.
- Reporté à la fin : la confirmation en deux clics ne couvre que « Tout supprimer » d'un groupe d'alarmes ; les autres
  suppressions ont une infobulle (choix recommandé appliqué).
- Verify.ps1 (agent) : 604/604, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.325.0.0. Non validé en jeu.
### English
- Queue step d (5/5): about 115 filters, searches, sorts and input fields gain a tooltip (140 new keys, English and French). The
  texts were checked against the current code, and about thirty audit drafts were corrected. The map « Reset all to 100 % » label
  is now accurate, the quest filter reads « Unconfirmed », and the French Fashion Report search hint no longer says « French
  name ». The Duty Finder loot panel accepts an exact item ID, has tooltips, and no longer recomputes its groups every frame.
  « Delete all » of an alarm group asks for a second click; other immediate deletions say so on hover. A ratchet test fails on
  any new control without a tooltip, and on a baseline line that no longer matches (3 exemptions left). A second check rejects
  a Tip that becomes an if-body.

## 0.324.0
- Point d) de la file, 4/5 : les interrupteurs de sources coupent toute leur source (revue 0.316, points 3.0 à 3.3). Fait sans
  l'utilisateur, choix provisoires notés pour la fin.
- Cause : les interrupteurs de Réglages › Avancé ne coupaient qu'une partie de leur source, et la page Crédits le disait :
  - Garland Tools : seulement les sources d'objets ; le butin des contenus et des monstres l'interrogeait encore ;
  - le wiki : seulement le mode d'obtention des objets ; les monstres et le carnet d'exploration l'interrogeaient encore ;
  - Universalis : rien pour la page Marché ; seul existait « prix au survol ».
- Correction :
  - Garland Tools et le wiki : l'interrupteur arrête toutes leurs requêtes. Pour le butin des contenus et des monstres, les
    monstres du wiki et le carnet d'exploration, la copie gardée sur disque reste lue ; quand la source est coupée, elle est
    montrée quel que soit son âge (rien ne peut la rafraîchir). Les sources d'objets Garland et le mode d'obtention du wiki sont
    masqués tant que leur interrupteur est coupé (précision apportée en 0.326).
  - Universalis : nouvel interrupteur « Universalis (prix du marché) », activé par défaut. Il coupe la page Marché, les annonces,
    les prix au survol, le coût de préparation et la table des centres de données. « Prix au survol » passe en dessous, grisé quand
    Universalis est coupé.
  - « Monstres au survol » est grisé quand le wiki est coupé, et le survol ne le lit plus.
  - « Actualiser » est grisé quand sa source est coupée, avec une infobulle qui dit pourquoi. « Charger » reste actif, car il lit
    la copie en cache.
  - Une page qui tombe sur une source coupée affiche « Source désactivée » au lieu de « erreur » (Community/SourceDisabledException.cs,
    un seul message reconnaissable).
  - Le tableau des annonces se recharge une fois quand Universalis est réactivé.
  - Crédits : plus aucun réglage « partiel » ; Universalis est relié au nouvel interrupteur. Le manifeste et SECURITY.md sont mis
    à jour.
- Bouton ↺ (valeur par défaut) ajouté aux anciens réglages qui ne l'avaient pas :
  - ouverture de la fenêtre au chargement et à la connexion ;
  - survol (usages, monstres) et menu contextuel ;
  - butin du Duty Finder, panneau du personnage, capture automatique de la banque ;
  - durée de cache de chaque domaine ;
  - options des alarmes ;
  - copie des macros ;
  - options de la Revue de mode ;
  - zones de la carte, avec une infobulle sur la couleur.
- Tests : 2 nouvelles classes de cas (6 cas) :
  - aucune requête n'est envoyée quand Garland, Universalis ou le wiki sont coupés (services de test qui comptent les requêtes) ;
  - les crédits ne contiennent plus de réglage partiel ;
  - le branchement est vérifié dans Plugin.cs et dans le survol ;
  - les nouveaux boutons ↺ sont présents.
- Reporté à la fin : la décision R-13 sur les réglages partiels est résolue à titre provisoire. Les interrupteurs Garland et wiki
  couvrent toute leur source, et Universalis a le sien.
- Verify.ps1 (agent) : 602/602, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.324.0.0. Non validé en jeu.
### English
- Queue step d (4/5): each community source switch now stops every request of its source. Garland Tools also covers duty and
  monster loot. The wiki also covers monsters and the sightseeing log. A new « Universalis (market prices) » switch covers the
  Market page, listings, hover prices, preparation cost and the data centre list, with « prices on hover » nested under it.
  Copies kept on disk are still shown for duty and monster loot, wiki monsters and the sightseeing log (Garland item sources and
  wiki item acquisition are hidden while off). Forced refreshes are greyed with a tooltip, and pages say « source turned off » instead of
  « error ». The Credits page no longer lists any partial switch; the manifest and SECURITY.md are updated. Older settings gain
  their « ↺ » reset button: window opening, hover and menu options, Duty Finder, character panel, bank auto capture, cache
  durations, alarm options, macro copy, Fashion Report options and map areas.

## 0.323.0
- Point d) de la file, 3/5 : git, GitHub et outils pour les contributeurs. Fait sans l'utilisateur. Seuls des fichiers sont créés ou
  modifiés : rien n'est poussé, aucun réglage GitHub n'est changé, aucun workflow n'est activé.
- Release.ps1 (choix provisoire, à suivre de près à la première vraie release) :
  - Nouvel ordre : contrôles d'abord (fetch, Devloppement à jour, origin/main contenu dans Devloppement, gh connecté, paquet
    présent) ; puis les étapes locales (repo.json, fusion, tag) ; puis la publication (tag, release GitHub avec latest.zip, main
    qui porte repo.json en dernier, enfin Devloppement).
  - Avant, main et repo.json partaient avant la release : si gh échouait, les joueurs voyaient une version dont le téléchargement
    renvoyait 404, et une relance s'arrêtait tout de suite.
  - Reprise : si le tag existe sans release GitHub, seules les étapes manquantes sont faites.
  - Mode Commit : liste des nouveaux fichiers ajoutés, et arrêt (code 2) sur une archive, un binaire ou un fichier de plus de 5 Mo.
  - Vérifié par l'analyse de syntaxe seulement : l'agent ne lance pas -Commit ni -Release, même en -DryRun.
- Verify.ps1 :
  - nouvelle option -TestsOnly : tests et langues sans Dalamud, pour les contributeurs sans le jeu (596/596 en local) ;
  - l'empreinte inclut la version de Dalamud et du SDK, donc une mise à jour relance tous les contrôles ;
  - le rapport du validateur de langues est affiché quand il échoue (il était toujours jeté) ;
  - avertissement si l'entrée de tête du CHANGELOG ne correspond pas à <Version> ;
  - commentaire de -Release corrigé.
- Fichiers ajoutés :
  - .github : modèles d'issues en anglais (bug avec le rapport de diagnostic, demande de fonction, traduction, config sans issue
    vide, lien vers le signalement privé) et modèle de pull request (Verify, tests, CHANGELOG, réglages avec infobulle et ↺,
    vouvoiement, licences, essai en jeu, déclaration d'IA) ;
  - publish/github : brouillons inactifs avec leur README d'activation, à savoir CodeQL corrigé (branches main et Devloppement,
    build-mode none, l'actuel surveille une branche « Stable » absente de GitHub et échoue chaque semaine), compilation et tests
    sous Windows avec Dalamud téléchargé, et Dependabot pour les actions ;
  - tools/README.md : les 15 sondes en lecture seule et le script du butin des sous-marins ;
  - .editorconfig, aligné sur les fichiers actuels ;
  - .githooks/pre-push : lance Verify avant chaque push, désactivé tant que vous ne l'activez pas.
- .gitignore : réglages de l'agent, Thumbs.db, desktop.ini, __pycache__/. Sauvegarde zip mal placée
  (src/TenshoAssistant/checkpoints) déplacée dans checkpoints/.
- CONTRIBUTING et README : DALAMUD_HOME, -TestsOnly, outils pour contributeurs ; la mention « restored during the build », ambiguë,
  est corrigée.
- Tests : 2 nouveaux, dont un qui exige que toute classe de tests soit dans la liste (une classe oubliée ne tournait jamais).
- Reportés à la fin, recommandations notées :
  - activer les brouillons GitHub ;
  - protections de branches ;
  - passage des fins de ligne en eol=lf (renormalisation, à faire par vous après un commit propre) ;
  - code de conduite et CODEOWNERS (non créés).
- Verify.ps1 (agent) : 596/596, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.323.0.0 ; Verify.ps1 -TestsOnly aussi
  vert. Non validé en jeu.
### English
- Queue step d (3/5), files only (nothing pushed or enabled on GitHub): Release.ps1 now checks first, then publishes the tag and the
  GitHub release before main (which carries repo.json), and resumes a release whose tag exists without its GitHub release; commit mode
  lists new files and refuses archives, binaries and large files. Verify.ps1 gains -TestsOnly (no Dalamud needed), a stamp that
  includes Dalamud and SDK versions, and shows the language report on failure. Issue and PR templates, inactive CI drafts
  (publish/github), tools/README.md, .editorconfig, an optional pre-push hook, .gitignore entries; a test catches unregistered test
  classes.

## 0.322.0
- Point d) de la file, 2/5 (points ouverts de la revue 0.316, 2/2), fait sans l'utilisateur (« fais un max sans moi »), choix
  provisoires notés pour la fin.
- Noms de rangements traduits. Avant, « Sacs du personnage », « Cristaux », « Sacoche chocobo non importée », « Servants non
  importés », « Arsenal », « Coffre à apparence », « Armoire » et « Sacoche chocobo (supplémentaire) » étaient des littéraux français,
  affichés à tous les joueurs (Banque, survol, Équipement, panneau d'import, page Rangements). Maintenant :
  - chaque ligne porte un libellé (StorageLabel), et Inventory/StorageNames.cs en donne le nom traduit d'après le type ;
  - seul un servant garde son nom, qui vient du jeu ;
  - les anciennes banques qui gardent un nom français s'affichent traduites.
- Cristaux : classés d'après le nom anglais de l'objet (que le catalogue garde dans toutes les langues), le nom français en secours.
  Sur un client allemand ou japonais, aucun cristal n'était classé. Le classement est calculé une fois par objet (il l'était pour
  chaque case à chaque image). Non validé sur un client non français.
- Erreurs du catalogue d'actions d'artisanat : texte traduit (« ServiceErrors.Craft »), le détail seulement en mode développeur.
- Messages internes de MarketService et TriadSolveSession en anglais (journal et mode développeur).
- Nouveau test : chaque phrase des tables UiMessages et CraftMessages doit être émise par le code, et chaque clé doit exister en
  anglais et en français. Avant, une ponctuation changée dans un service faisait retomber l'affichage sur le message générique sans
  que rien ne le voie. Le passage de toutes les chaînes des services à des codes de message (environ 290) est reporté à l'étape g2.
- Texte au vouvoiement corrigé (« utilisez le bouton d'import »).
- Manifeste :
  - Description réécrite : fonctions actuelles, toutes les sources réseau, interrupteurs (dont les partiels), Discord facultatif,
    alarmes du jeu automatiques seulement avec leur réglage, « German and Japanese: menu titles only » ;
  - changelog de manifest.toml aligné ;
  - nouveau test : la description nomme chaque source réseau du catalogue des crédits.
- SECURITY.md : section « Supported versions » (seule la dernière release reçoit les correctifs, choix provisoire), portée datée sur
  la branche Devloppement, limite Teamcraft et étendue réelle des interrupteurs corrigées.
- Tests : 4 nouveaux.
- Verify.ps1 (agent) : 594/594, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.322.0.0. Non validé en jeu.
### English
- Queue step d (2/5): storage names are translated (they were French literals shown to everyone); crystals classified from the
  English item name on every client and once per item; crafting action errors translated; MarketService and TriadSolveSession internal
  messages in English; a test keeps the French-sentence → key tables in step with the sentences services emit; manifest description and
  D17 changelog rewritten (every network source, Discord, partial switches) with a test; SECURITY.md gains « Supported versions ».

## 0.321.0
- Point d) de la file (tour du code), 1/5. Sauvegarde : checkpoints/backup-20261102-121837-avant-point-d.zip.
- Audit multi-agents en lecture seule, sur quatre angles : réglages manquants, contrôles sans infobulle, points ouverts de la revue
  0.316, git, GitHub et outils pour contributeurs. Une synthèse contre-vérifiée a retenu 32 constats, rejeté 7 et découpé le travail
  en cinq versions :
  - 0.321 : enregistrement, réseau, Draw, réglages oubliés ;
  - 0.322 : textes figés en français, manifeste, SECURITY ;
  - 0.323 : git, GitHub et outils, en fichiers seulement ;
  - 0.324 : interrupteurs de sources respectés partout ;
  - 0.325 : infobulles des filtres et petits restes.
  Les choix de l'utilisateur sont reportés à la fin, avec un choix provisoire appliqué.
- Corrigé :
  - Case « Aide du carnet » (Réglages › Modules) : depuis la passe d'infobulles 0.287, son « if » ne gardait plus que l'infobulle.
    La configuration était donc réécrite sur le disque à chaque image tant que la page était ouverte, et l'infobulle ne s'affichait
    qu'au clic. Elle passe par la ligne de réglage commune (enregistrement au clic, ↺).
  - Un test parcourt le code : une infobulle ne doit jamais suivre directement le « if » d'un contrôle.
  - Limites de recherche d'artisanat (étapes, largeur, budget) et options d'export des macros (attente, écho) : enregistrées dans la
    configuration (elles revenaient aux valeurs d'origine à chaque chargement), avec ↺ et les mêmes bornes. Nouvelle ligne commune
    IntSetting : enregistrement en quittant le champ.
  - Universalis : après un échec, la table des centres de données n'est redemandée qu'une fois par minute (elle l'était à chaque
    image) ; le bouton « Région » grisé a une infobulle.
  - Teamcraft : la réponse est lue dans la limite de 1 Mio pendant la lecture, avec le User-Agent (elle était chargée entière puis
    mesurée). BoundedHttp accepte maintenant une requête POST.
  - Rappels : la lecture des minuteurs du jeu se fait une fois par seconde, et une fois toutes les dix secondes après un échec ; le
    journal n'est écrit qu'une fois par message (avant, il l'était à chaque image après un patch).
  - Cabine d'essayage : un échec est rattrapé, consigné une fois, puis ses boutons se grisent ; EndDisabled est dans un finally.
  - Revue de mode : nouvel interrupteur (Réglages › Avancé › « Revue de mode (FFXIVGachaSpreadsheet) »). Coupé, plus aucun
    téléchargement, et la page le dit ; une copie récente sur disque reste lue (choix provisoire). Le crédit est relié au réglage.
  - « Tout remettre par défaut » :
    - les groupes repliés de la navigation sont gardés ;
    - les anciennes valeurs reviennent si l'enregistrement échoue ;
    - les deux listes (gardé et remis à zéro) sont dans Configuration.cs, et un test exige que chaque propriété soit dans une
      seule, ce qui a aussitôt classé trois réglages oubliés ;
    - l'infobulle est complétée.
- Choix provisoires notés dans ROADMAP « Décisions reportées à la fin » ; liste de contrôle en jeu dans TESTING.md (« 0.321 »).
- Tests : 6 nouveaux.
- Verify.ps1 (agent) : 590/590, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.321.0.0. Non validé en jeu.
### English
- Queue step d (1/5), from a multi-agent audit (32 findings in five versions): the « Crafting log helper » checkbox no longer saves
  the configuration on every frame (a tooltip had slipped under its « if »; a test now catches that pattern); crafting search limits
  and macro export options are saved, with ↺; Universalis data-centre table retried at most once a minute; Teamcraft replies read
  within 1 MiB; reminders and Try On no longer fail or log on every frame; new Fashion Report source switch; « reset all » keeps folded
  navigation groups, rolls back on save failure, and a test assigns every configuration property to kept or reset.

## 0.320.0
- Demande (R-13) : « réserve une section complète dans le plugin pour les crédits de ce qu'on aurait utilisé dans tout le code ».
  Sauvegarde avant modification : checkpoints/backup-20261102-112058-avant-r13-credits.zip.
- Inventaire multi-agents en lecture seule : relevé du code (bibliothèques, données intégrées, hôtes réseau, règles reprises), relevé
  de la documentation (README, REFERENCES, DECISIONS, preuves), licences relues à leur source, puis fusion contre-vérifiée.
  72 entrées, preuve : docs/evidence/0.320-credits.json.
- Nouveau : page **Crédits** (Préférences › Crédits), visible par tout le monde.
  - Sept sections : le jeu, bibliothèques, données intégrées à Tensho, données lues en ligne, règles et formules reprises, plugins et
    sites lus pour comprendre, outils de fabrication.
  - Pour chaque entrée : nom, auteur, licence (propriétaire, aucune licence publiée ou inconnue quand c'est le cas), à quoi elle sert,
    ce qui est copié (données avec avis, règle reprise comme fait) ou seulement lu, état du réglage lié et ce qu'il coupe quand il est
    partiel, exemples de fichiers ; boutons « Copier le lien », « Copier la licence » et l'adresse du dépôt ; filtre.
  - Mention SQUARE ENIX (FINAL FANTASY XIV © SQUARE ENIX CO., LTD.), avec la politique d'usage des contenus du jeu en lien.
- Une seule liste dans le code (Credits/CreditCatalog.cs) ; trois tests la gardent alignée :
  - chaque donnée intégrée et chaque hôte réseau écrit dans le code a son crédit ;
  - fichiers et réglages cités existants, noms et auteurs identiques dans toutes les langues ;
  - lien de licence selon la licence, avis MIT complet pour chaque donnée MIT copiée ;
  - une référence n'est jamais marquée copiée.
- Avis de licence complétés (exigés par MIT pour une copie) : texte MIT complet de Character Panel Refined en tête de
  Character/StatFormulas.cs, et de FFXIVGachaSpreadsheet dans submarine-loot.json (champ « License », aussi écrit par
  tools/submarine-loot.py).
- README : les deux sections Crédits renvoient à la page, portent la mention SQUARE ENIX et sont corrigées (Mappy et SortaKinda
  AGPL-3.0-or-later, licence d'Argus non confirmée, règle de frozen_rabbit_tome reprise comme fait).
- Revue (relecteur, puis contre-vérificateur adverse) : 14 remarques, toutes confirmées et corrigées.
  - Réglages partiels annoncés comme complets : le comportement réseau n'a pas changé, la décision est reportée au point d.
  - Noms et auteurs en français dans l'interface anglaise.
  - Avis MIT manquants.
  - Jeu présenté « sans licence » au lieu de « propriétaire ».
  - Textes « jamais copié » contradictoires.
  - Pied de page vers une page du mode développeur.
  - Textes coupés dans les colonnes.
  - Recalcul de la liste à chaque image.
  - Licence d'Argus affirmée sans preuve.
  - Table du Mini Cactpot non mentionnée.
  - Liste de fichiers présentée comme complète.
  - README désaligné.
  - Lien du guide en français.
- Localisation : méthode Has (texte facultatif avec repli). Tests : 4 nouveaux.
- Verify.ps1 (agent) : 584/584, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.320.0.0. Non validé en jeu.
### English
- R-13: new Credits page (Preferences › Credits) listing everything Tensho uses (72 entries from a multi-agent inventory with each
  licence read at its source): the game (SQUARE ENIX notice), libraries, embedded data, online data (with the tied setting and what a
  partial one turns off), rules taken as facts, references read but never copied, and build tools; filter and copy-link buttons.
  One list in code (Credits/CreditCatalog.cs) kept in step with the code by tests (embedded data, network hosts, settings, MIT notices).
  Full MIT notices added for Character Panel Refined and FFXIVGachaSpreadsheet data; README credits aligned.

## 0.319.0
- Demande (R-12) : « prévois dans une todo list un hook Discord pour envoyer les alarmes avec message sur un serveur Discord ; voir
  plus si possible ou intéressant ». Sauvegarde avant modification : checkpoints/backup-20261102-104125-avant-r12-discord.zip.
- Nouveau : Réglages › Alarmes › **Discord**, désactivé par défaut.
  - Adresse du webhook saisie par le joueur, masquée. Seules les adresses de webhook des hôtes de Discord sont acceptées
    (discord.com, ptb, canary, discordapp.com ; HTTPS, port par défaut, ni requête ni fragment) ; une autre reste dans le champ avec un
    message, sans être enregistrée.
  - Quand une alarme Tensho sonne, son message part dans le salon : modèle réglable (repères [label], [minutes], [time], [character]),
    nom de l'expéditeur, catégories (récolte, pêche, rappels, compagnie libre, autres), nom du personnage seulement sur option, mention
    d'un rôle (identifiant ou mention collée). « allowed_mentions » ne laisse passer que ce rôle : @everyone ou @here dans un nom
    d'alarme ne notifie personne.
  - Page Alarmes : case « Discord » et message propre à chaque groupe ; une alarme qui rejoint un groupe en reprend les réglages.
  - Bouton « Envoyer un test », ligne d'état (envoyé, refusé avec le code, Discord indisponible, limite, réseau) et compteurs.
  - Les alarmes écrites dans la liste du jeu sonnent toujours dans le jeu, et partent aussi sur Discord.
- Envoi (Alarms/DiscordNotifier.cs) sur un worker, jamais sur le thread du jeu ni dans Draw :
  - file de 20 messages au plus et deux secondes entre deux messages ;
  - « 429 » : attente de Retry-After jusqu'à 60 s ; au-delà, le message est abandonné et la file attend la pause demandée ;
  - pas de nouvel essai après un délai dépassé ni une erreur 5xx (un POST pourrait être publié deux fois), un seul après une connexion
    qui n'a pas pu s'ouvrir ;
  - « Effacer » l'adresse, la changer, couper l'envoi ou « Tout remettre par défaut » abandonne les messages en attente et l'envoi en
    cours ;
  - User-Agent du plugin, sans donnée personnelle.
- Secret : l'adresse n'est jamais écrite dans les journaux, dans le rapport de diagnostic (seulement « webhook valid/invalid/empty »)
  ni dans l'état affiché. « Tout remettre par défaut » coupe l'envoi mais garde l'adresse, et l'infobulle le dit maintenant.
- Catégories : les alarmes de poisson de la prévision de pêche portent maintenant le métier 18 ; une alarme d'objet sans métier le
  prend dans le catalogue de récolte ; les rappels et retours de vaisseaux d'avant 0.319 sont reconnus une fois par leur groupe.
- Correctif au passage : changer un réglage d'alarme remettait le délai des commandes « /alarm » (Réglages › Avancé) à 500 ms.
- Revue multi-agents (justesse et fils, sécurité et règles Dalamud, interface et textes), puis contre-vérification adverse :
  28 remarques, 23 confirmées et corrigées, 5 réfutées.
  - Défauts corrigés : worker qui pouvait s'arrêter sur une erreur imprévue, messages encore envoyés après « Effacer », doublons
    possibles, alarmes de pêche rangées dans « Autres », message de groupe pris à moitié tapé, chiffres Unicode acceptés dans
    l'identifiant de rôle, textes inexacts.
  - Documents de publication mis à jour : SECURITY, PUBLISHING et le brouillon de PR D17 déclarent l'envoi Discord.
- Règles de l'utilisateur notées pendant ce point :
  - les tests en jeu de tout le plugin se feront à la fin, guidés (étape h) ;
  - les décisions sont gardées pour la fin, avec un choix provisoire noté (ROADMAP « Décisions reportées à la fin ») ;
  - R-13, page « Crédits » complète, vient juste après ;
  - R-14, résistance aux mises à jour de FF14 directement dans la logique du code : règle ajoutée à notes de l'agent pour tout
    nouveau code, passage complet sur l'existant vers la toute fin de la file (étape g2, avant les tests en jeu).
- Tests : 11 nouveaux (adresses, message, mentions, catégories, nouvelles tentatives, pause demandée, file, « Effacer », worker qui
  survit, identifiants de rôle, sources des anciennes alarmes) ; TESTING.md : liste de contrôle en jeu du webhook.
- Un test de la file comptait mal le message en cours d'envoi (instable) : corrigé, puis 8 passages de suite verts.
- Verify.ps1 (agent) : 580/580, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.319.0.0. Non validé en jeu.
### English
- R-12: optional Discord webhook for Tensho alarms (Settings › Alarms › Discord, off by default): Discord hosts only, masked address
  never logged or reported, message template, categories, per-group switch and message, optional character name and role mention
  (mentions otherwise disabled), test button and status. Posts leave on a worker (queue of 20, 2 s apart, Retry-After honoured, no
  retry that could double-post); clearing the address or turning it off drops queued posts. Also fixes the /alarm command delay being
  reset by other alarm settings. Multi-agent review: 23 confirmed findings fixed.

## 0.318.0
- Demande (file c) : « prévois aussi un plan pour ajouter à notre plugin ce qui manque par rapport aux plugins dispo », élargie à
  « pas que plugin de référence mais ceux du dépôt Dalamud (voir repo non officiel si ça respecte les règles Dalamud upload) », puis
  « ajoute une phase décision avant aligner avec roadmap pour que je choisisse ce qu'on rajoute ».
- Comparaison en lecture seule par 11 agents :
  - règles de publication relevées sur dalamud.dev et le dépôt D17 ;
  - dépôt officiel complet : 500 plugins, dont 177 dans un domaine d'aide non combat de Tensho ;
  - dépôts non officiels : 658 plugins, 76 gardés en tout ou en partie, jugés règle par règle ;
  - chaque fonction cherchée dans le code de Tensho, licence lue dans environ 250 dépôts ;
  - contre-vérification adverse : 20 affirmations de couverture corrigées, 5 noms de structures du jeu rectifiés par sonde,
    portée de licence d'Allagan Tools (AllaganLib sans licence), tri automatique classé en zone grise.
  Résultat : docs/project/GAPS.md (103 manques, plugins sources, donnée du jeu, licence, conformité, taille, valeur) et
  docs/evidence/0.318-plugin-gaps.json (données brutes).
- Phase de décision faite dans la conversation (la page de choix publiée n'a pas servi) : 101 manques retenus, I-14 et X-05 plus
  tard ; ordre domaine par domaine (Inventaire et marché → Collections → Monde → Récolte → Artisanat → Nouveaux domaines) ; choix
  précis tranchés (colonne « Choix » de GAPS.md) ; licence EUPL-1.2 acceptée comme compatible. Décision DEC-025 ; nouvelle étape g)
  de la file (ROADMAP, STATUS) ; BACKLOG, USER-REQUESTS, CONTRIBUTING et l'index des docs alignés.
- Autres demandes notées pendant ce point :
  - R-12, webhook Discord pour les alarmes, placé juste après c) (USER-REQUESTS, ROADMAP, STATUS) ;
  - amélioration du dépôt git et de GitHub, rattachée au point d) ;
  - règle de numérotation des versions (DEC-024) : X = grandes étapes (1.0.0 = première publication Dalamud), Y = nos versions,
    Z = versions de la communauté ; écrite dans consignes de l'agent et CONTRIBUTING.md.
- Aucun changement de code. Verify.ps1 (agent) : 570/570, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.318.0.0.
  Non validé en jeu.
### English
- Gap plan against the 17 reference plugins, the whole official Dalamud repository (500 plugins) and rule-compliant unofficial
  plugins (658 seen): docs/project/GAPS.md (103 gaps with sources, game data, licence, D17 compliance) plus raw evidence. The user
  chose 101 of them (DEC-025), done domain by domain as queue step g); EUPL-1.2 accepted as GPL-compatible. Also recorded: R-12
  Discord webhook for alarms (next), git/GitHub improvements in step d), and the version numbering rule (DEC-024).

## 0.317.0
- Demande : « D'abord les décisions » sur les points « À décider / à revoir » de la feuille de route (0.316), avant la suite de la
  file. Douze questions posées, réponses de l'utilisateur inscrites dans docs/project/DECISIONS.md (DEC-023) :
  - sous-marins : seuils gardés, 19 secteurs signalés « non confirmé par les relevés » ; prix du marché (Universalis) et paliers
    de surveillance / récupération (si la source le permet) dans la valeur des trajets ;
  - dirigeables : arbre de déblocage de LuminaSupplemental (GPL-3.0) embarqué avec avis de licence ; planificateur de trajets par
    distance et durée, butin « inconnu » (DEC-021 modifiée en conséquence) ;
  - matérias : taux de surmatérialisation lu dans les données du jeu ; R-11 étape 4 : barde de chocobo et escouade ; rappels de
    l'escouade et des Planeurs d'Ehcatl : sonde ; R-09 après la file ;
  - publication D17 à la version 1.0.0 (décision de l'auteur) ; cadres ULD avant D17 ; traductions allemande et japonaise gardées ;
  - textes français au vouvoiement.
- Feuille de route : section « À décider / à revoir » réduite aux points qui attendent un relevé en jeu ou une étape de la file,
  nouvelle section « Décidé le 2026-11-02 » ; nouvelle étape f) de la file (suite des décisions, ordre fixé). STATUS, R-11,
  PUBLISHING et USER-REQUESTS alignés ; les points de la revue 0.316 sont rattachés à l'étape d). `FilteredTable` retiré du plan
  (choix de l'agent : pas d'abstraction sans besoin répété).
- Vouvoiement : 14 textes français passés du « tu » au « vous » (Triple Triade, Titres et Succès, servants et sacoche, Quêtes,
  ALEA, butin des missions) ; nouveau test « French texts address the player with « vous » » qui refuse tu / ton / ta / tes / toi
  dans fr.json.
- Verify.ps1 (agent) : 570/570, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.317.0.0. Non validé en jeu.
### English
- Roadmap decisions recorded (DEC-023): flag 19 unconfirmed submarine breakpoint sectors; add market prices and survey tiers to
  route value; embed the GPL-3.0 airship unlock tree; airship route planner by distance and duration only; overmeld success rate
  from game data; chocobo barding and squadron gear; probe squadron / Ehcatl timers; seasonal events after the queue; Dalamud
  submission at 1.0.0; ULD frames before it; German and Japanese kept. French texts now consistently use « vous » (14 strings, new
  test).

## 0.316.0
- Demande : faire le tour des .md et des descriptions du code des outils pour les mettre à jour, et compléter la feuille de route
  avec les points indécis à revoir et les tests à faire. Sauvegarde avant modification :
  checkpoints/backup-20261102-045314-avant-revue-docs.zip.
- Travail fait en parallèle par huit agents, chacun sur ses propres fichiers, avec vérification de chaque affirmation dans le code.
  Un agent critique a ensuite contre-vérifié l'ensemble : il a corrigé 10 erreurs et confirmé que les fichiers .cs ne diffèrent
  que par des commentaires et que tous les liens existent.
  - README (deux langues), docs/README, PROJECT, CONTRIBUTING, SECURITY, Localization/README, PUBLISHING et brouillon de PR D17 :
    fonctions, commandes, options de Verify, crédits et licences à jour.
  - ARCHITECTURE : carte des modules (Compagnie libre, Équipement, Rappels, marché, modèles communs, essayage…), flux de données,
    workers, ressources embarquées, configuration et KeptOnReset.
  - REFERENCES et DECISIONS : sources étudiées depuis 0.235 (SubmarineTracker, FFXIVGachaSpreadsheet, Argus,
    HimbeertoniRaidTool, Glamourer, xivgear.app…) et décisions de la période.
  - TESTING : inventaire des tests automatiques (569) et liste de contrôle en jeu pour chaque version non validée.
  - ROADMAP, BACKLOG, USER-REQUESTS et GOAL : état des demandes R-01 à R-11, file d'attente, et deux nouvelles sections :
    **« À décider / à revoir »** (18 points) et **« Tests à faire en jeu »**.
  - Descriptions des pages dans le plugin (fr / en), corrigées d'après le code : Accueil, Revue de mode, « Tout remettre par
    défaut »… ; en-têtes de description en tête de 86 fichiers d'outils (commentaires seulement).
- Descriptions des outils dans le code, signalées par le critique :
  - annuaire « Tous les outils » : six entrées ajoutées (Équipement, Compagnie libre, Marchands PNJ, Météo, Alarmes, Rappels),
    chacune avec son bouton « Ouvrir » ;
  - tuiles de l'accueil : « Enchaînement de macros » et « Personnage », au lieu des anciens noms ;
  - fiche contributeur : fichiers source exacts par page (Monstres, Mage bleu, Équipement, Compagnie libre…), liste des dossiers
    complète, sources SubmarineTracker et FFXIVGachaSpreadsheet.
- STATUS : la dernière release est Stable-0.242.0 (tag git vérifié) ; GOAL R-11 ; liste « à valider en jeu » renvoyée à la
  feuille de route ; couverture de Character Panel et de Macro Chain mise à jour. consignes de l'agent : règle de sauvegarde zip avant toute
  grosse modification (demande de l'utilisateur).
- Verify.ps1 (agent) : 569/569, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.316.0.0. Non validé en jeu.
### English
- Documentation pass (8 agents + critic, every claim checked in code): README, CONTRIBUTING, SECURITY, ARCHITECTURE, REFERENCES,
  DECISIONS, TESTING (in-game checklist), ROADMAP (« to decide / review » and « in-game tests »), page descriptions (fr/en), file
  header comments. Tool directory gains Gear, Free company, NPC vendors, Weather, Alarms and Reminders; home tile names and
  contributor source map fixed; STATUS aligned (last release Stable-0.242.0).

## 0.315.0
- Retour en jeu : 0.314 (sources et possession) validée par l'utilisateur.
- Demande R-11 étape 3 : montages de sous-marins et de dirigeables « dans le même style » que la page Équipement. Recherche faite
  par plusieurs agents (données du jeu, SubmarineTracker, dirigeables, code Tensho), contre-vérifiée par un agent critique ;
  preuves : docs/evidence/0.315-vessel-builds.md.
  - Compagnie libre, nouvel onglet **Montages** :
    - on choisit un vaisseau lu dans l'atelier (son montage lu sert de départ) ou une saisie manuelle, et un rang ;
    - les statistiques sont à gauche : lues, prévues (pièces + bonus de rang), écart, coût / capacité, rang des pièces,
      réparation et nom court communautaire (« SSUC++ ») ;
    - les quatre pièces sont à droite, comme dans la page Équipement : icône, nom, rang, cadre doré si le plan diffère ; un clic
      liste les pièces de l'emplacement avec ce qu'elles changent ;
    - ⚠ si les statistiques lues ne valent pas la somme calculée (contrôle de la règle, non encore vérifiée en jeu) ;
    - un plan est gardé par vaisseau.
  - Sous-marins, section **Trajet** (bouton « Montage » d'un trajet dans Meilleurs trajets, ou « Trajet en cours ») :
    - distance contre autonomie, durée avec la vitesse prévue ;
    - paliers atteints d'exploration, de prélèvement et de faveur, d'après les seuils communautaires ; « inconnu » si un secteur
      manque, jamais 0 ;
    - usure estimée (nombre de voyages avant réparation) ;
    - **Meilleurs montages pour ce trajet** : les 10 000 combinaisons au plus sont essayées sur un worker, avec les paliers voulus,
      l'autonomie et la capacité ; les plus rapides d'abord, puis la réparation la moins chère ; bouton « Appliquer ».
  - Meilleurs trajets :
    - nouveau filtre « Secteurs débloqués seulement » (activé par défaut ; les secteurs sont lus dans l'atelier avec
      IsSubmarineExplorationUnlocked) ;
    - bouton « Montage prévu » pour reprendre le rang, la vitesse et l'autonomie du plan ;
    - bouton « Montage » sur chaque trajet.
  - Atelier : Tensho lit maintenant aussi les pièces des vaisseaux, les statistiques des dirigeables et les secteurs débloqués.
  - Dirigeables : statistiques, capacité et rang des pièces seulement. Aucune donnée de seuils ni d'arbre de déblocage n'existe
    sous une licence compatible.
- Correctifs trouvés pendant la recherche :
  - « Tout remettre par défaut » effaçait les vaisseaux lus, les plans d'équipement et les plans de matérias : ils sont gardés
    maintenant, comme les nouvelles données ;
  - les recherches de trajets et de montages sont annulées à la fermeture du plugin.
- Données : seuils de 143 secteurs copiés de SubmarineTracker (MIT, avis de licence dans le fichier, crédits README).
- Sauvegarde avant modification : checkpoints/backup-20261102-043419-pendant-0.315.zip (demande de l'utilisateur).
- Revue adversariale à plusieurs agents (4 angles, chaque point contre-vérifié) avant livraison : 14 points confirmés (9
  distincts), tous corrigés.
  - Le filtre des secteurs débloqués utilise maintenant la compagnie du sous-marin choisi, et non toutes les compagnies réunies ;
    une compagnie non lue = pas de filtre.
  - Le bouton « Montage » ouvre toujours un sous-marin : le vaisseau du planificateur, celui déjà choisi, ou le sous-marin manuel
    au rang du planificateur. Avant, il pouvait ouvrir un dirigeable, sans trajet affiché.
  - Une seule infobulle par ligne de trajet.
  - Le rang d'un plan suit le rang lu du vaisseau quand celui-ci monte.
  - Un trajet au-dessus du rang est signalé, et aucun montage n'est proposé pour lui.
  - Textes : « Recherche des montages… », aide du planificateur mise à jour, « d'Elena ».
  - Preuve corrigée : liste exacte des secteurs dont le seuil embarqué est sous les observations.
- Test ajouté : statistiques, capacité, rang des pièces, rangs, seuils de trajet et inconnus, paliers, usure, noms de montage,
  meilleurs montages, aller-retour du plan, fichier embarqué ; puis les correctifs de la revue (secteurs débloqués par
  compagnie, rang du plan, rang du trajet).
- Verify.ps1 (agent) : 569/569, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.315.0.0. Non validé en jeu.
### English
- Free company « Builds » tab (R-11 step 3): submarine and airship builds in the Gear page style (read / planned / difference stats,
  cost vs capacity, part rank, repair, community build name, part picker), a mismatch check of read vs computed stats; submarine
  route check (range, duration, breakpoint tiers from SubmarineTracker's MIT table, wear estimate) and best builds for the route.
  Route planner: unlocked-sectors filter (read in the workshop), « planned build » and « Build » buttons. Fix: « reset all » no
  longer erases saved vessels and gear / materia plans; searches are cancelled on unload.

## 0.314.0
- Retour en jeu : 0.310 à 0.313 (page Équipement, mirage, matérias, plafonds) validées par l'utilisateur.
- Demande R-11 étape 2 : sources et possession. Page Équipement :
  - ✓ / ✗ devant chaque pièce de la liste de choix et sur les pièces prévues : ✓ si elle est portée ou présente dans un rangement
    connu (sacs, armurerie, coffre à apparence, armoire, sacoche, servants : les mêmes comptes que l'infobulle des objets et la
    Banque). L'infobulle dit où, ou que certains rangements ne sont pas encore importés.
  - Nouveau filtre « Seulement ce que je possède » (infobulle, ↺, désactivé par défaut).
  - Nouvelle section « Pièces à obtenir (N) » sous l'équipement : les pièces du plan (mirage compris) que vous n'avez pas, chacune
    avec ses groupes de sources (marchands, artisanat, récolte, donjons, quêtes…, comme le menu Sources). Un clic ouvre la liste ;
    chaque ligne mène à l'outil concerné, avec un bouton Drapeau quand la source a une position.
- Verify.ps1 (agent) : 568/568, 0 avertissement, 0 erreur ; DLL 0.314.0.0. Non validé en jeu.
### English
- Gear page: owned / not owned marks (worn or in known storages, with where), an « Only what I own » filter, and a « Pieces to
  obtain » section listing the plan's missing pieces with their source groups (vendors, crafting, duties…, flags when placed).

## 0.313.0
- Demande : prévenir quand on essaie de poser une matéria qui dépasse le plafond. Dans l'éditeur de matérias :
  - les statistiques et les grades qui dépasseraient le plafond de la pièce sont écrits en orange, avec « dépasse le plafond :
    N perdus » ;
  - une matéria déjà posée qui dépasse affiche ⚠ et le nombre de points perdus ; l'infobulle donne le plafond et conseille un
    grade plus bas ou une autre statistique.
  - Le calcul suit l'ordre des emplacements, comme en jeu : les matérias des emplacements précédents sont comptées d'abord.
- Verify.ps1 (agent) : 568/568, 0 avertissement, 0 erreur ; DLL 0.313.0.0. Non validé en jeu.
### English
- Materia editor warns before going over a piece's cap: options that would waste points are shown in orange with the loss, and a
  melded materia over the cap shows ⚠ with the points lost.

## 0.312.0
- Demande : une fonction pour les matérias, sur l'équipement porté et sur l'équipement à l'essai. Page Équipement :
  - Sous chaque pièce, ses emplacements de matéria (pleins = matéria posée ; orange = surmatérialisation). L'infobulle liste les
    matérias et ce qui est perdu au plafond. Un clic ouvre l'éditeur : statistique et grade par emplacement (seules les
    statistiques du type de job sont proposées ; en surmatérialisation, les grades VI, VIII, X et XII ne le sont pas). L'éditeur
    montre le gain réel une fois le plafond appliqué et les points perdus. Boutons : « Reprendre les matérias portées »,
    « Tout enlever », « Revenir aux matérias de la pièce ».
  - Les statistiques prévues comptent les matérias plafonnées par pièce : la pièce portée garde les siennes, sauf si on en prévoit
    d'autres ; une pièce prévue n'en a pas, sauf celles qu'on lui pose.
  - Demande (pendant le travail) : « ne pas oublier les plafonds de chaque équipement pour chaque statistique ». Ils sont appliqués
    par pièce et par statistique dans les calculs, et maintenant affichés : l'infobulle des emplacements et l'éditeur montrent un
    tableau « statistique · valeur / plafond · place restante » (doré au plafond).
  - Le partage emporte les matérias (« emplacement=objet:matéria.matéria ») ; les anciens codes restent lisibles.
- Données du jeu : plafond = ItemLevel × pourcentage de l'emplacement (BaseParam) / 1000. Vérifié : il est égal à la plus haute
  statistique secondaire de la pièce (111 au NO 560, 374 au NO 730). Valeurs des matérias tirées de la feuille Materia.
  docs/evidence/0.312-materia.md.
- Test complété : plafonds, perte, surmatérialisation, valeurs des matérias, code de partage avec matérias.
- Verify.ps1 (agent) : 568/568, 0 avertissement, 0 erreur ; DLL 0.312.0.0. Non validé en jeu.
### English
- Gear page materia: sockets under every piece (worn or planned), an editor per socket (stat and grade, overmeld rules), gains
  capped per piece (ItemLevel × slot percentage, checked against real items), planned stats and shared builds include materia.

## 0.311.0
- Retour en jeu sur 0.310 (capture) : la page Équipement fonctionne.
- Demande : un filtre pour ajouter l'équipement des autres jobs à essayer, « on peut mirager tout le stuff maintenant (vérifie) ».
  - Vérifié : depuis le patch 7.4 (décembre 2025), toute armure ou tout accessoire peut servir de mirage, quels que soient le job
    et le niveau ; les armes et les outils restent liés au job (sources : PC Gamer, Game Informer, communiqué 7.4).
  - Nouveau filtre « Tous les jobs (mirage) » (infobulle, ↺, désactivé par défaut) : les listes montrent alors les armures et
    accessoires de tous les jobs et de tout niveau ; les armes restent celles du job.
  - Une pièce qu'on ne peut pas porter (autre job ou niveau plus élevé) est marquée « Mirage ». On peut l'essayer et la partager ;
    les statistiques gardent la pièce portée.
- Retour (capture) sur la disposition : statistiques à gauche, puis les deux colonnes d'équipement côte à côte. Les statistiques
  sans rapport avec le job sont masquées (plus de PR / PS sur un job de combat), et les valeurs dérivées sont alignées sur les
  mêmes colonnes que les statistiques.
- Correctif : « Essayer le plan » ajoutait les pièces de mirage à chaque image (accolades manquantes), corrigé avant livraison.
- Test complété : tous les jobs pour le mirage, armes toujours liées au job, pièces « mirage seulement ».
- Verify.ps1 (agent) : 568/568, 0 avertissement, 0 erreur ; DLL 0.311.0.0. Non validé en jeu.
### English
- Gear page: « All jobs (glamour) » filter — since patch 7.4 any armor or accessory can be a glamour whatever the job and level,
  weapons stay job-locked; such pieces are marked « Glamour » (try on, share; stats keep the worn piece). Stats now on the left,
  both gear columns side by side, only the job kind's stats, derived values in the same columns.

## 0.310.0
- Demande R-11 (nouvelle, plan : docs/goals/R-11.md) : planificateur d'équipement pour le personnage, dans le style de la
  fenêtre Personnage ; essayage de l'équipement, partout où l'apparence compte ; partage des builds. Étape 1 faite :
  - Page **Équipement** (catégorie Progression) : l'équipement porté à gauche et à droite, comme dans le jeu (pièce prévue
    encadrée en doré). Au milieu, les statistiques actuelles (lues dans le jeu), prévues (actuelles + plan − porté), l'écart, et
    pour les jobs de combat les valeurs dérivées. En tête, le niveau d'objet moyen porté et prévu.
  - Un clic sur un emplacement liste l'équipement du job proche de son niveau, du meilleur niveau d'objet au plus bas, avec ce que
    chaque pièce change, et une recherche. Filtres réglables (infobulle, ↺, aussi dans Paramètres) : niveaux en dessous (5), niveau
    exact hors paliers 50 / 60 / 70 / 80 / 90 / 100 (activé), niveau d'objet minimum, HQ supposé. Un plan est gardé par job.
  - **Essayer** : bouton « Essayer le plan », et « Essayer (cabine d'essayage) » dans le menu clic droit de tout équipement
    affiché par Tensho (Revue de mode, objets, butins…). Ces deux entrées ouvrent la cabine d'essayage du jeu.
  - **Partager** : copie la liste lisible du build et un code « TENSHO-GEAR1: » ; **Importer** lit un code dans le presse-papiers.
- Sources : données du jeu uniquement (Item, EquipSlotCategory, ClassJobCategory, BaseParam, Materia, équipement porté), voir
  docs/evidence/0.310-gear-sheets.md. Références lues : HimbeertoniRaidTool (MIT), Glamourer (Apache-2.0), xivgear.app (sans
  licence : lu seulement).
- Test ajouté : fenêtre de niveaux, choix par job et emplacement, bagues, HQ, matérias, statistiques prévues, niveau moyen, codes.
- Verify.ps1 (agent) : 568/568, 0 avertissement, 0 erreur ; DLL 0.310.0.0. Non validé en jeu.
### English
- New Gear page (R-11 step 1): character-window style gear planner — worn slots, real / planned / difference stats, candidates of
  the job near its level with filters (levels below, exact level outside caps, minimum item level, assumed HQ), one plan per job;
  « Try on » (the game's Try On window) from the plan and from every equipment menu in Tensho; build sharing by code.

## 0.309.0
- Demande : prévoir les meilleurs trajets de sous-marin et de dirigeable, « surtout le butin », en s'appuyant sur les plugins
  existants. Page Compagnie libre, nouvel onglet **Meilleurs trajets** :
  - on choisit un sous-marin lu dans l'atelier (rang, vitesse et autonomie lus dans le jeu, modifiables) ou une saisie manuelle,
    une zone et un tri : valeur par voyage, valeur par jour (voyages enchaînés) ou expérience par jour ;
  - Tensho essaie tous les ensembles de 1 à 5 secteurs que le rang et l'autonomie permettent, chacun dans l'ordre le plus court,
    sur un worker, et affiche les 25 meilleurs : trajet, distance / autonomie, durée, valeur, gils par jour, expérience ;
  - le survol d'un trajet montre le butin moyen d'un voyage, objet par objet, avec sa valeur.
- Sources : le butin n'est pas dans les données du jeu. Les moyennes viennent de FFXIVGachaSpreadsheet (MIT, relevés
  communautaires), embarquées dans la DLL (24 Ko, `tools/submarine-loot.py`). La valeur est le prix de revente au PNJ (données
  du jeu). Les formules de durée et de distance ont été comprises en lisant SubmarineTracker (MIT) ; le code est original.
  Crédits dans le README. Pas de relevé public pour les dirigeables : pas de planificateur pour eux.
- Contrôle sur les vraies feuilles : sur la Mer des noyades, les trajets OJ, ROJ et JORZ arrivent en tête, les trajets rentables
  connus de la communauté (docs/evidence/0.309-submarine-routes.md).
- Vaisseaux lus : on garde aussi les secteurs du trajet et les statistiques du sous-marin (surveillance, récupération, vitesse,
  autonomie, faveur).
- Test ajouté : distance, temps, ordre le plus court, autonomie, rang, valeur, lecture du fichier embarqué.
- Verify.ps1 (agent) : 567/567, 0 avertissement, 0 erreur ; DLL 0.309.0.0. Non validé en jeu.
### English
- Free company page, new « Best routes » tab: best submarine routes for a vessel's rank, speed and range, ranked by expected loot
  value (community averages from FFXIVGachaSpreadsheet, MIT, embedded; NPC sell prices from the game), value per day or
  experience; formulas understood from SubmarineTracker (MIT), original code. Checked: OJ / ROJ / JORZ come first on the first map.

## 0.308.0
- Retour (capture) : le niveau de zoom sous la colonne d'outils était presque invisible sur la carte (option « Menus et outils
  par-dessus la carte »). Il a maintenant un fond sombre arrondi, comme les noms de zone, et un texte clair.
- Verify.ps1 (agent) : 566/566, 0 avertissement, 0 erreur ; DLL 0.308.0.0. Non validé en jeu.
### English
- Map overlay: the zoom level gets a dark backdrop and light text, like the zone names.

## 0.307.0
- Demande R-07 (compagnie libre) : nouvelle page **Compagnie libre** (catégorie Monde). Dans l'atelier de la compagnie, Tensho lit
  toutes les 3 s les sous-marins et dirigeables (nom, rang, expérience vers le rang suivant, date d'enregistrement, retour, trajet
  des sous-marins, butin du dernier voyage) et les garde dans la configuration (le jeu ne les donne que dans l'atelier ; une
  lecture qui ne change rien n'est pas réenregistrée). La page affiche par type : rang + barre d'expérience, trajet (noms
  `SubmarineExploration`), retour (temps restant / « Rentré » / « Au port »), butin (clic, icônes d'objets interactives) et un
  bouton d'alarme au retour. La page Rappels gagne la ligne « Expéditions de la compagnie » (en mer / rentrés, prochain retour,
  alarme, bouton « Détails »).
- Preuve : docs/evidence/0.307-fc-voyages.md (structures et feuilles sondées). Test ajouté (état, expérience, fusion par maison,
  détection de changement, prochain retour).
- Verify.ps1 (agent) : 566/566, 0 avertissement, 0 erreur ; DLL 0.307.0.0. Non validé en jeu.
### English
- New Free company page (R-07): submarines and airships read in the company workshop and kept (rank, experience, route, return,
  last finds), with return alarms; a « Company voyages » line on the Reminders page.

## 0.306.0
- Retour : 0.305 validée en jeu par l'utilisateur, sauf les boutons et menus par-dessus la carte qui ne répondaient toujours pas.
  Cause : un bouton enfoncé devient « l'élément actif » d'ImGui ; le test « souris sur la fenêtre » qui affiche les outils ne le
  comptait pas, donc les outils et menus disparaissaient entre l'appui et le relâchement, et le clic (qui part au relâchement)
  n'arrivait jamais. Correction : le test accepte l'élément actif (AllowWhenBlockedByActiveItem), de même pour la détection de
  la souris sur les outils et sur les noms.
- Verify.ps1 (agent) : 565/565, 0 avertissement, 0 erreur ; DLL 0.306.0.0. Non validé en jeu.
### English
- Map overlay buttons and menus: the hover test ignored the active (pressed) item, so the overlays vanished between press and
  release; it now allows the active item.

## 0.305.0
- Retour (captures) sur « Menus et outils par-dessus la carte » :
  - **Aucun bouton ne répondait** : la carte était posée avant les outils et prenait le clic. Les outils et les noms sont
    maintenant posés avant la carte (ils prennent le clic en premier, tout en restant dessinés par-dessus) et la carte ignore la
    souris quand elle est sur eux.
  - Les noms (région, zone, étage) ne sont plus sous la colonne d'outils mais **à sa droite**, en haut ; la colonne commence en
    haut et ne dépend plus du nombre de lignes de noms.
  - Le **bandeau sombre suit le texte** : sombre sous les noms, puis dégradé vers la droite et vers le bas (plus toute la largeur).
  - **Nom de la zone toujours affiché** (nouveau réglage, Paramètres de la carte › Affichage, activé par défaut, infobulle et ↺) :
    quand la souris n'est pas sur la carte, le nom de la zone affichée (et la sous-zone) reste écrit en haut.
  - La **colonne d'outils s'adapte à la fenêtre** (avec ou sans l'option) : le curseur de zoom raccourcit, puis disparaît, puis les
    boutons passent sur plusieurs colonnes ; le **% de zoom** reste toujours visible (infobulle) et laisse la place aux
    coordonnées écrites en bas de la carte.
- Verify.ps1 (agent) : 565/565, 0 avertissement, 0 erreur ; DLL 0.305.0.0. Non validé en jeu.
### English
- Map overlay: buttons clickable again (overlays submitted before the map, which ignores the mouse over them); names to the right
  of the tool column; dark band sized to the text; new "zone name always shown" setting; the tool column fits the window
  (shorter slider, then none, then several columns) and always shows the zoom %.

## 0.304.0
- Retour (captures, carte du jeu en exemple) sur l'option « Menus et outils par-dessus la carte » : les menus du haut ne sont plus
  des listes déroulantes encadrées. Comme la carte du jeu, la région, la zone et l'étage s'affichent l'un sous l'autre, en texte
  doré détouré, avec une flèche ; un clic déroule leur liste **vers le bas** (« > nom », le choix actuel en doré, ailes de vol
  pour les zones) et un clic ailleurs la referme. Opacité : le bandeau sombre de 0.303 était dessiné sous la carte, donc
  invisible ; il est maintenant dans sa propre couche (sombre derrière les noms puis dégradé vers transparent), sans capter la
  souris : la carte se fait toujours glisser dessous.
- Verify.ps1 (agent) : tous les tests verts (0 échec), 0 avertissement, 0 erreur ; DLL 0.304.0.0. Non validé en jeu.
### English
- Map overlay header like the game map: region, zone and floor names stacked in outlined gold text, each unrolling its list
  downward; the dark gradient band (hidden under the map in 0.303) is now visible and does not block dragging.

## 0.303.0
- Demande (captures de la carte Tensho et de la carte du jeu) : option « Menus et outils par-dessus la carte » (Paramètres de la carte
  › Affichage, infobulle et ↺, désactivée par défaut). La carte occupe alors toute la fenêtre ; la colonne d'outils de gauche et les
  menus de région / zone du haut s'affichent par-dessus, seulement quand la souris est sur la carte (ou qu'un de ses menus est
  ouvert). Le haut est un bandeau en dégradé (sombre vers transparent) avec les noms en doré et la flèche des menus, comme la
  carte du jeu. La colonne d'outils ne couvre que la hauteur de ses boutons : le reste de la carte se fait glisser normalement.
- Verify.ps1 (agent) : 565/565, 0 avertissement, 0 erreur ; DLL 0.303.0.0. Non validé en jeu.
### English
- New map option: menus and tool column drawn over the map (gradient header like the game map), shown only while the mouse is
  over it, the map using the whole window.

## 0.302.0
- Demande (capture « 🕒 1 · 03:35 ») : la barre du serveur affiche maintenant le **temps restant** avant la prochaine alarme
  (« 2 h 15 », « 45 min », « < 1 min ») au lieu de son heure ; l'infobulle Tensho donne pour chaque alarme le temps restant, puis
  l'heure entre parenthèses.
- Verify.ps1 (agent) : 565/565, 0 avertissement, 0 erreur ; DLL 0.302.0.0. Non validé en jeu.
### English
- The server bar shows the time left before the next alarm instead of its clock time; the tooltip lists time left and clock time.

## 0.301.0
- Demande : l'infobulle de l'icône d'alarme de la barre du serveur doit s'afficher en dessous, et être réglable. L'infobulle du jeu
  passe sous les fenêtres des plugins (la carte Tensho la cachait). Tensho retrouve maintenant le texte de son entrée dans la barre
  (addon « _DTR ») et dessine **sa propre infobulle juste en dessous**, par-dessus toutes les fenêtres : les prochaines alarmes
  (heure et nom) et « Clic : page Alarmes ». Si l'entrée n'est pas retrouvée à l'écran, l'infobulle du jeu reste utilisée.
  Réglages (Paramètres › Avancé, sous l'icône d'alarme) : « Infobulle Tensho sous l'icône » et « Alarmes listées » (1 à 10, 5 par
  défaut), avec infobulles et ↺.
- Verify.ps1 (agent) : 565/565, 0 avertissement, 0 erreur ; DLL 0.301.0.0. Non validé en jeu (la recherche du texte dans la barre
  est à confirmer en jeu).
### English
- The server bar alarm icon gets Tensho's own tooltip drawn just below it (above plugin windows), listing the next alarms; it can
  be turned off and the number of alarms set (1–10).

## 0.300.0
- R-08 (Rappels), d'après le Calendrier du jeu (capture, `docs/evidence/0.300-calendar-resets.md`) : nouvelle page **Rappels** (MONDE)
  avec, pour chaque minuteur, l'état, la prochaine fois (compte à rebours et heure locale), une case « fait » qui se décoche seule à
  la remise à zéro suivante, et une cloche qui crée l'alarme à la prochaine heure (module Alarmes) :
  - missions de ravitaillement (chaque jour 20:00 UTC) ; commandes spéciales avec les livraisons restantes lues dans le jeu, et Revue
    de mode (mardi 08:00 UTC) ; quêtes quotidiennes des peuples alliés avec le nombre restant lu dans le jeu (15:00 UTC) ;
    autorisations de mandats avec leur nombre et leur prochaine heure lus dans le jeu ; cartes au trésor (le jeu ne donne pas
    l'heure : bouton « Carte ouverte », 18 h d'attente) ; tâches de servant (en cours, terminées, prochaine fin ; lues après avoir
    ouvert la liste des servants).
  - Calcul des remises testé (`Reminders/ResetSchedule.cs`) ; compteurs copiés depuis le jeu une fois par seconde
    (`Reminders/GameTimers.Dalamud.cs`).
  - Pas encore : escouade de Grande Compagnie, livraisons des Planeurs d'Ehcatl, expéditions d'aéronef / sous-marin (à relever).
- Demande : **icône d'alarme dans la barre d'informations du jeu** (à côté du nom du serveur) quand au moins une alarme Tensho est
  active : horloge du jeu, nombre d'alarmes et heure de la prochaine ; clic = page Alarmes ; réglage dans Paramètres › Avancé. L'heure
  est dans le texte de la barre, car l'infobulle du jeu passe sous les fenêtres des plugins (la carte Tensho la cachait).
- Verify.ps1 (agent) : 565/565, 0 avertissement, 0 erreur ; DLL 0.300.0.0. Non validé en jeu.
### English
- New Reminders page (game calendar timers: supply missions, custom deliveries, Fashion Report, allied society quests, leves,
  treasure maps, retainer ventures) with resets, game counters, « done » boxes and alarm buttons; alarm clock with count and next
  time in the game's server info bar.
