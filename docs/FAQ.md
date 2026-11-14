<!-- Written by tools/faq.py from src/TenshoAssistant/Help/Faq.cs and the English and French language files. Do not edit by hand: change the list or the texts, then run « python -I -X utf8 tools/faq.py write ». -->

# Frequent questions

The same questions are in the plugin, on the Help page (`/tensho`, then Help), in your language, and its search finds them. Ask a new question in [Discussions › Q&A](https://github.com/Shigu330/Tensho-Assistant/discussions/categories/q-a); a question that comes back joins this list. A problem or an idea goes in [a ticket](https://github.com/Shigu330/Tensho-Assistant/issues/new/choose).

## Where do I start?

With the guide: Help › « Start the guide » shows the main pages in a few minutes, and you can leave it at any step. Then Home has one tile per main tool with its state. Anywhere in Tensho, right-click an item for its item page and its tools; the « ? » next to a page's title explains it.

## A page is empty or says « not read ».

The game only gives some data once its window has been opened, or once you are logged in. The « ? » of the page says what it waits for. Open that window once: Tensho keeps what it read.

## Why must I open a game window?

Tensho only reads what the game has on your computer. Retainers, the Glamour Dresser, titles or achievements are only sent by the server when you open them.

## What do the check mark, the cross and the question mark mean?

A check mark: the game confirms it; a cross: it says no; a question mark: it has not said yet. Tensho does not turn an unknown into a no.

## How do I change the language?

Settings › General › Language. English and French are complete. German and Japanese are partial (marked « (partial) » in the menu): only a few titles are translated and the rest shows in English, so Tensho starts in English with a German or Japanese game client; you can still pick them. Game data (item, place and quest names) stays in the game client's language.

## What goes on the Internet?

Prices, loot, wiki pages and community data are asked of those community sites, with the item or page you are looking at. Each source can be turned off in Settings › Data and sources. The list and the licences are in Credits. Prices you open at the market board are kept and, for your world, used in place of Universalis' when they are newer.

## Can I turn off the community data?

Yes: Settings › Data and sources › « Game data and what you observed only » turns off every community source at once (prices, loot, wiki…); each one can also be turned off on its own there. The discoveries exchange is off unless you turn it on, and the community guides are only read when you click.

## Is Tensho safe, and is it allowed?

Tensho follows Dalamud's plugin rules: it reads and shows, acts on the game only when you click (see « Does Tensho play for me? »), and stops by itself in PvP and ranked content. Its code is open (GPL-3.0) on GitHub. Like every third-party tool, Dalamud and its plugins are not allowed by Square Enix's terms of service: use them discreetly and never talk about them in the game.

## What does Tensho keep on my PC?

What the game has shown it (storages, collections, progress), your settings, notes, lists and alarms, a gil journal (90 days), the voyage history (180 days) and the discoveries base. These files stay on this PC; your discoveries leave it only through the GitHub exchange, after you connect GitHub (or as a file you export and share yourself), and alarms only through the Discord webhook, if you set one.

## Where are my files, and how do I erase everything (and what I shared)?

Settings: %APPDATA%\XIVLauncher\pluginConfigs\TenshoAssistant.json. Data: the folder %APPDATA%\XIVLauncher\pluginConfigs\TenshoAssistant\ (TenshoData.db, the discoveries, a cache). The desktop app adds %LOCALAPPDATA%\TenshoAssistant\ (its copy of the data, its cache and versions), which stays after you delete the app. To erase everything: remove Tensho in Dalamud, then delete that file and those folders. One character only: Storage › Characters › « Erase ». What you shared: « Disconnect » keeps your gist and your ticket on GitHub; delete the gist on github.com and close the ticket. Discoveries already merged into the shared base (CC0) stay in it: ask on GitHub to have yours withdrawn.

## Does Tensho play for me?

No. It reads and shows. What acts on the game, such as teleporting, sorting or writing a macro, only happens when you click. Two settings act without a click: the M key opens Tensho's map instead of the game's (on by default), and wearing the title you chose for a job when you change job (off by default).

## M opens another map: how do I get the game's back?

That map is Tensho's: by default it replaces the game's on the M key, with the same markers and more. Settings › Map › « Replace the game map (M key) » gives the game's map back; Tensho's map stays in the Tensho window.

## How do I remove the panels around the game's windows?

Tensho adds small panels next to some game windows (character sheet, Duty Finder, retainer sales, synthesis, gathering, fishing, the Sort button, Triple Triad) and lines in the server bar. The guide's step « Panels next to the game's windows » turns them all off or on at once (Help › « Start the guide »); each one also has its switch in Settings, and the reset button (curved arrow) puts it back.

## Why does Tensho stop in PvP, and why does its map give way in a deep dungeon?

In PvP zones and other ranked content (Triple Triad tournaments, Mahjong), the whole plugin stops reading the game and says so. It comes back on its own when you leave. No setting changes this. In deep dungeons Tensho works as in any dungeon, but the game draws its own map there: Tensho's map hides and leaves the game's map until you leave.

## How do I update Tensho?

Nothing to do: Dalamud updates it like any other plugin, when the game starts or from the plugin installer (/xlplugins › « Update »). What changed in each version is in CHANGELOG.md on GitHub. The desktop app updates itself.

## How do I start over?

Settings › Advanced › « Restore all defaults » puts every setting back to its default value. Your data stays: alarms, presets, decks, plans and what the game showed. Its tooltip lists everything that is kept.

## Why is Tensho not in Dalamud's official list?

Its author chose to publish it on its own repository. Dalamud's support does not answer for it: ask your questions and report problems on the project's GitHub.

## Where can I ask a question?

On the project's GitHub, in Discussions › Q&A (github.com/Shigu330/Tensho-Assistant/discussions). A question that comes back joins this list. A problem or an idea goes in a ticket (« Report a problem or help » on the Help page).

## Something is wrong.

Use « Report a problem or help » on the Help page, then « Send on GitHub… »: the exact text is shown first. With GitHub connected (World › Discoveries), the ticket leaves in one click; otherwise the GitHub form opens in your browser, filled in.

## How can I help translate Tensho?

In « Report a problem or help » on the Help page, choose « A translation »: it opens the GitHub form. To translate a whole language, the steps are in src/TenshoAssistant/Localization/README.md on GitHub.

## Where is my wishlist?

In the side bar, ITEMS › Wishlist (it used to be a tab of Collections). Add an item by right-clicking it anywhere in Tensho › « Add to my wishlist »; the line « added to your wishlist » at the bottom of the window opens the list.

## Hover prices still say « old » after Ctrl+Alt: why?

Ctrl+Alt asks Universalis again: « Fetched » shows the time of that request. Universalis only knows what players have uploaded, so the world's date is its last upload, by anyone; when nobody has opened that item at the board since, it stays old. For each line Tensho shows the newest reading it knows, Universalis' upload or what you saw yourself at the game's market board (« Seen at the market board »). Open the item at the market board for today's prices.

## The garden is shown as icons: how do I get the text back?

Gardening › Garden › Garden journal › « Detailed view » shows each bed as text again (seed, dates, crossbreeds). The icons say the same: hover a seed for its dates; the round mark tells the tending (drop: later, « ! »: due, check: ripe).

## How do I install Tensho?

In Dalamud › Settings › Experimental › Custom Plugin Repositories, add https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/repo.json and save, then install « Tensho Assistant » from the plugin installer. /tensho opens the window.

## Can I see Tensho without the game?

Yes, on Windows: the desktop app (TenshoAssistant.Desktop.zip, next to latest.zip in each GitHub release) shows the same pages with the game closed. It needs XIVLauncher with Dalamud and the game installed (it carries its own .NET runtime); it reads your saved settings without changing them: what you create or change in the app (tools, alarms, garden, craft presets, settings) is not saved, its controls are greyed and the status bar says so; make changes in the game. Live parts (bags, timers, alarms) stay empty and actions are off; the map, live synthesis and the Fashion Report stay in the game. Take the app and the plugin from the same release: the app says so when the versions differ.

## Does the desktop app update itself?

Yes, with no step from you. At start (and on « Check for update ») it asks this repository's GitHub release of the installed plugin's version, else the latest Stable release: HTTPS, no player data. It downloads TenshoAssistant.Desktop.zip, checks its SHA-256 against TenshoAssistant.Desktop.zip.sha256 of the same release, installs it beside (%LOCALAPPDATA%\TenshoAssistant\Desktop\versions) and keeps the version before. The running app is never overwritten nor restarted while you use it: the new version opens at the next start, or with « Restart now ». « Previous version » goes back; « Automatic update: on » turns it off. Limit: the app is not code-signed, so the SHA-256 protects against a damaged download, not against a stolen GitHub account.

## How do I link the desktop app to the game?

In the game, turn on Settings › Advanced › « Link with the desktop app » (off by default), then start the desktop app: its status bar says « Linked to the game » with the zone you are in, your job and level, GP and your bags' free slots. When the game saves Tensho's settings, the app reads them again (its pages follow). The link is a local named pipe open to your Windows user only, with no network port; the app only reads. The other live parts of the pages and the actions come in later versions.

## Can Tensho install the desktop app for me?

Yes, if you want it (it is optional): Settings › Advanced › Desktop application › « Install the desktop app ». Tensho downloads TenshoAssistant.Desktop.zip from this repository's GitHub release of its own version (else the latest Stable release), checks its SHA-256 against the one published in the same release and unpacks it in the install folder (empty: %LOCALAPPDATA%\TenshoAssistant\Desktop\versions). Nothing is sent but the version asked, and the app is never started without your click on « Open the desktop app ». After that the app updates itself (« Automatic update », in the same section). Limit: the app is not code-signed, so the SHA-256 protects against a damaged download, not against a stolen GitHub account.

Français :

# Questions fréquentes

Les mêmes questions sont dans le plugin, sur la page Aide (`/tensho`, puis Aide), dans votre langue, et sa recherche les trouve. Posez une nouvelle question dans [Discussions › Q&A](https://github.com/Shigu330/Tensho-Assistant/discussions/categories/q-a) ; une question qui revient rejoint cette liste. Un problème ou une idée va dans [un ticket](https://github.com/Shigu330/Tensho-Assistant/issues/new/choose).

## Par où commencer ?

Par le guide : Aide › « Lancer le guide » montre les pages principales en quelques minutes, et vous pouvez le quitter à n'importe quelle étape. Ensuite, l'Accueil a une tuile par outil principal avec son état. Partout dans Tensho, un clic droit sur un objet ouvre sa fiche et ses outils ; le « ? » à côté du titre d'une page l'explique.

## Une page est vide ou dit « non lu ».

Le jeu ne donne certaines données qu'une fois sa fenêtre ouverte, ou une fois que vous êtes connecté. Le « ? » de la page dit ce qu'elle attend. Ouvrez une fois cette fenêtre : Tensho garde ce qu'il a lu.

## Pourquoi dois-je ouvrir une fenêtre du jeu ?

Tensho ne lit que ce que le jeu a sur votre ordinateur. Les servants, la coiffeuse mirage, les titres ou les hauts faits ne sont envoyés par le serveur qu'à leur ouverture.

## Que veulent dire la coche, la croix et le point d'interrogation ?

Une coche : le jeu le confirme ; une croix : il dit non ; un point d'interrogation : il ne l'a pas encore dit. Tensho ne transforme pas un inconnu en non.

## Comment changer la langue ?

Paramètres › Général › Langue. L'anglais et le français sont complets. L'allemand et le japonais sont partiels (marqués « (partiel) » dans le menu) : seuls quelques titres sont traduits et le reste s'affiche en anglais, donc Tensho démarre en anglais avec un client du jeu allemand ou japonais ; vous pouvez quand même les choisir. Les données du jeu (noms d'objets, de lieux et de quêtes) restent dans la langue du client du jeu.

## Qu'est-ce qui part sur Internet ?

Les prix, le butin, les pages du wiki et les données communautaires sont demandés à ces sites, avec l'objet ou la page que vous consultez. Chaque source se coupe dans Paramètres › Données et sources. La liste et les licences sont dans Crédits. Les prix que vous ouvrez au tableau des ventes sont gardés et, pour votre monde, remplacent ceux d'Universalis quand ils sont plus récents.

## Puis-je couper les données communautaires ?

Oui : Paramètres › Données et sources › « Données du jeu et observées seulement » coupe d'un coup toutes les sources communautaires (prix, butin, wiki…) ; chacune peut aussi s'y couper seule. L'échange des découvertes reste coupé tant que vous ne l'activez pas, et les guides de la communauté ne sont lus que sur votre clic.

## Tensho est-il sûr, et est-il permis ?

Tensho suit les règles des plugins de Dalamud : il lit et montre, n'agit sur le jeu que sur votre clic (voir « Tensho joue-t-il à ma place ? »), et s'arrête de lui-même en JcJ et en contenu classé. Son code est ouvert (GPL-3.0) sur GitHub. Comme tout outil tiers, Dalamud et ses plugins ne sont pas permis par les conditions d'utilisation de Square Enix : utilisez-les discrètement et n'en parlez jamais dans le jeu.

## Que garde Tensho sur mon PC ?

Ce que le jeu lui a montré (rangements, collections, progression), vos réglages, notes, listes et alarmes, un journal des gils (90 jours), l'historique des expéditions (180 jours) et la base des découvertes. Ces fichiers restent sur ce PC ; vos découvertes n'en sortent que par l'échange GitHub, après la connexion de GitHub (ou par un fichier que vous exportez et partagez vous-même), et les alarmes seulement par le webhook Discord, si vous en indiquez un.

## Où sont mes fichiers, et comment tout effacer (y compris ce que j'ai partagé) ?

Réglages : %APPDATA%\XIVLauncher\pluginConfigs\TenshoAssistant.json. Données : le dossier %APPDATA%\XIVLauncher\pluginConfigs\TenshoAssistant\ (TenshoData.db, les découvertes, un cache). L'application de bureau ajoute %LOCALAPPDATA%\TenshoAssistant\ (sa copie des données, son cache et ses versions), qui reste après la suppression de l'application. Pour tout effacer : retirez Tensho dans Dalamud, puis supprimez ce fichier et ces dossiers. Un seul personnage : Rangements › Personnages › « Effacer ». Ce que vous avez partagé : « Déconnecter » garde votre gist et votre ticket sur GitHub ; supprimez le gist sur github.com et fermez le ticket. Les découvertes déjà fusionnées dans la base commune (CC0) y restent : demandez sur GitHub que les vôtres soient retirées.

## Tensho joue-t-il à ma place ?

Non. Il lit et il affiche. Ce qui agit sur le jeu, comme se téléporter, trier ou écrire une macro, ne se fait que sur votre clic. Deux réglages agissent sans clic : la touche M ouvre la carte de Tensho au lieu de celle du jeu (activé par défaut), et le titre choisi pour un job se porte quand vous changez de job (désactivé par défaut).

## M ouvre une autre carte : comment retrouver celle du jeu ?

Cette carte est celle de Tensho : par défaut, elle remplace celle du jeu sur la touche M, avec les mêmes repères et d'autres. Paramètres › Carte › « Remplacer la carte du jeu (touche M) » rend la carte du jeu ; celle de Tensho reste dans la fenêtre de Tensho.

## Comment retirer les panneaux autour des fenêtres du jeu ?

Tensho ajoute de petits panneaux à côté de certaines fenêtres du jeu (fiche du personnage, outil de mission, ventes des servants, synthèse, récolte, pêche, bouton Trier, Triple Triade) et des lignes dans la barre serveur. L'étape « Panneaux à côté des fenêtres du jeu » du guide les coupe ou les remet tous d'un coup (Aide › « Lancer le guide ») ; chacun a aussi son interrupteur dans Paramètres, et le bouton de retour au défaut (flèche courbe) le remet.

## Pourquoi Tensho s'arrête-t-il en JcJ, et pourquoi sa carte s'efface-t-elle en donjon sans fond ?

Dans les zones JcJ et les autres contenus classés (tournois de Triple Triade, Mahjong), tout le plugin cesse de lire le jeu et le dit. Il revient tout seul quand vous en sortez. Aucun réglage ne change cela. En donjon sans fond, Tensho marche comme dans tout donjon, mais le jeu y dessine sa propre carte : la carte de Tensho se cache et laisse celle du jeu jusqu'à votre sortie.

## Comment mettre Tensho à jour ?

Rien à faire : Dalamud le met à jour comme les autres plugins, au démarrage du jeu ou depuis l'installateur de plugins (/xlplugins › « Mettre à jour »). Les changements de chaque version sont dans CHANGELOG.md sur GitHub. L'application de bureau se met à jour seule.

## Comment repartir de zéro ?

Paramètres › Avancé › « Tout remettre par défaut » remet chaque réglage à sa valeur par défaut. Vos données restent : alarmes, préréglages, decks, plans et ce que le jeu a montré. Son info-bulle liste tout ce qui est gardé.

## Pourquoi Tensho n'est-il pas dans la liste officielle de Dalamud ?

Son auteur a choisi de le publier sur son propre dépôt. Le support de Dalamud n'en répond pas : posez vos questions et signalez les problèmes sur le GitHub du projet.

## Où poser une question ?

Sur le GitHub du projet, dans Discussions › Q&A (github.com/Shigu330/Tensho-Assistant/discussions). Une question qui revient rejoint cette liste. Un problème ou une idée va dans un ticket (« Signaler un problème ou aider » de la page Aide).

## Quelque chose ne va pas.

Utilisez « Signaler un problème ou aider » de la page Aide, puis « Envoyer sur GitHub… » : le texte exact est montré d'abord. Si GitHub est connecté (Monde › Découvertes), le ticket part en un clic ; sinon le formulaire GitHub s'ouvre rempli dans votre navigateur.

## Comment aider à traduire Tensho ?

Dans « Signaler un problème ou aider » de la page Aide, choisissez « Une traduction » : cela ouvre le formulaire GitHub. Pour traduire toute une langue, la marche à suivre est dans src/TenshoAssistant/Localization/README.md sur GitHub.

## Où est ma liste de souhaits ?

Dans la barre latérale, OBJETS › Souhaits (c'était un onglet de Collections). Ajoutez un objet par un clic droit dessus n'importe où dans Tensho › « Ajouter à mes souhaits » ; la ligne « ajouté à vos souhaits » en bas de la fenêtre ouvre la liste.

## Les prix au survol disent encore « ancien » après Ctrl+Alt : pourquoi ?

Ctrl+Alt redemande les prix à Universalis : « Consulté » donne l'heure de cette demande. Universalis ne connaît que ce que les joueurs ont envoyé : la date du monde est son dernier relevé, par n'importe qui ; si personne n'a ouvert cet objet au tableau des ventes depuis, elle reste ancienne. Pour chaque ligne, Tensho montre le relevé le plus récent qu'il connaît, celui d'Universalis ou ce que vous avez vu vous-même au tableau des ventes du jeu (« Vu au tableau des ventes »). Ouvrez l'objet au tableau des ventes pour les prix du jour.

## Le jardin est en icônes : comment retrouver le texte ?

Jardinage › Jardin › Journal de jardinage › « Vue détaillée » remet chaque parcelle en texte (graine, dates, croisements). En icônes, tout y est aussi : survolez une graine pour ses dates ; la pastille dit l'entretien (goutte : à venir, « ! » : à faire, coche : mûr).

## Comment installer Tensho ?

Dans Dalamud › Settings › Experimental › Custom Plugin Repositories, ajoutez https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/repo.json et enregistrez, puis installez « Tensho Assistant » depuis l'installateur de plugins. /tensho ouvre la fenêtre.

## Puis-je voir Tensho sans le jeu ?

Oui, sous Windows : l'application de bureau (TenshoAssistant.Desktop.zip, à côté de latest.zip dans chaque release GitHub) montre les mêmes pages, jeu fermé. Il lui faut XIVLauncher avec Dalamud et le jeu installés (elle porte son propre runtime .NET) ; elle lit vos réglages enregistrés sans les changer : ce que vous créez ou changez dans l'application (outils, alarmes, potager, préréglages d'artisanat, réglages) n'est pas enregistré, ses contrôles sont grisés et la barre d'état le dit ; faites les changements dans le jeu. Les parties en direct (sacs, minuteurs, alarmes) restent vides et les actions sont coupées ; la carte, la synthèse en direct et la Revue de mode restent dans le jeu. Prenez l'application et le plugin dans la même release : l'application le signale quand les versions diffèrent.

## L'application de bureau se met-elle à jour seule ?

Oui, sans geste de votre part. Au démarrage (et sur « Chercher une mise à jour »), elle lit la release GitHub de ce dépôt qui a la version du plugin installé, sinon la dernière release Stable : HTTPS, aucune donnée du joueur. Elle télécharge TenshoAssistant.Desktop.zip, compare son SHA-256 à TenshoAssistant.Desktop.zip.sha256 de la même release, l'installe à côté (%LOCALAPPDATA%\TenshoAssistant\Desktop\versions) et garde la version d'avant. L'application en cours n'est jamais écrasée ni redémarrée pendant que vous l'utilisez : la nouvelle version s'ouvre au prochain démarrage, ou avec « Redémarrer maintenant ». « Version d'avant » revient en arrière ; « Mise à jour automatique : active » la coupe. Limite : l'application n'est pas signée, donc le SHA-256 protège d'un téléchargement abîmé, pas d'un compte GitHub volé.

## Comment relier l'application de bureau au jeu ?

Dans le jeu, activez Paramètres › Avancé › « Lien avec l'application de bureau » (désactivé par défaut), puis lancez l'application de bureau : sa barre d'état indique « Relié au jeu » avec la zone où vous êtes, votre classe et son niveau, vos PR et les places libres de vos sacs. Quand le jeu enregistre les réglages de Tensho, l'application les relit (ses pages suivent). Le lien est un canal nommé local ouvert à votre seul utilisateur Windows, sans port réseau ; l'application ne fait que lire. Les autres parties en direct des pages et les actions viendront dans des versions suivantes.

## Tensho peut-il installer l'application de bureau pour moi ?

Oui, si vous le voulez (c'est facultatif) : Paramètres › Avancé › Application de bureau › « Installer l'application de bureau ». Tensho télécharge TenshoAssistant.Desktop.zip depuis la release GitHub de ce dépôt qui a sa propre version (sinon la dernière Stable), vérifie son SHA-256 avec celui publié dans la même release et le dépaquette dans le dossier d'installation (vide : %LOCALAPPDATA%\TenshoAssistant\Desktop\versions). Rien n'est envoyé sauf la version demandée, et l'application n'est jamais lancée sans votre clic sur « Ouvrir l'application de bureau ». Ensuite l'application se met à jour seule (« Mise à jour automatique », dans la même section). Limite : l'application n'est pas signée, donc le SHA-256 protège d'un téléchargement abîmé, pas d'un compte GitHub volé.
