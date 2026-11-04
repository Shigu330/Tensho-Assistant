# CHAT-001 : chat personnalisable (plan, rien n'est codé)

Demande de l'utilisateur (2026-11-04) : un chat custom avec les personnalisations de WoW, portées sur FF14, améliorées si possible, avec le plus de réglages possible, après vérification de ce que font les plugins existants. Les décisions à prendre sont à la fin.

État : **plan, non commencé**. Aucune décision n'est encore prise. Rien ici n'a été essayé en jeu.

## Le point à trancher d'abord

**Chat 2 existe déjà, est actif et remplace entièrement la fenêtre du jeu.** Refaire tout ce qu'il fait serait le plus coûteux et n'apporterait rien par soi-même. L'intérêt de Tensho est ailleurs : ce que WoW a et que Chat 2 n'a pas (règles d'alerte, fenêtres de conversation, copie, thèmes, profils), et le lien avec les outils de Tensho (prix, sources, carte, rappels).

Le plan avance donc par couches. Le chat du jeu reste en place tant que le nôtre ne sait pas tout faire, et on peut s'arrêter à la fin de chaque phase avec un outil utile.

Voir la décision C1.

## Ce qui existe déjà

Licences et dates relevées par l'API GitHub le 2026-11-04. Le code n'est lu que pour comprendre, jamais copié (règle du projet).

| Plugin | Licence | Dernière activité | Ce qu'il fait |
|---|---|---|---|
| [Chat 2](https://github.com/Infiziert90/ChatTwo) | EUPL-1.2 | 2026-10-28 | Réécriture complète du chat : onglets illimités, onglets liés à un canal, filtres, couleurs RGB, police, compteurs de non-lus, émoticônes, mode capture d'écran, fenêtres détachées, historique en base SQLite, interface web, IPC |
| [Simple Tweaks](https://github.com/Caraxi/SimpleTweaksPlugin) (`Tweaks/Chat`, 25 réglages) | AGPL-3.0 | 2026-10-17 | Couleurs des noms, liens cliquables, format d'horodatage, canal collant, historique d'envoi amélioré, cacher le chat, tailles de police, renommer les onglets, canal de réponse |
| [TidyChat](https://github.com/NadyaNayme/TidyChat) | AGPL-3.0 | 2026-10-18 | Retire les messages système redondants, liste blanche de joueurs |
| [NoSoliciting](https://github.com/Noevain/NoSoliciting) (dépôt repris) | EUPL-1.2 | 2026-05-04 | Filtre anti-pub du chat et de la recherche d'équipe, modèle entraîné, filtres personnels |
| [Chat Bubbles](https://github.com/Haplo064/ChatBubbles) | GPL-3.0 | 2025-07-25 | Bulles au-dessus des personnages |
| [ChatAnywhere](https://github.com/twelvehouse/ChatAnywhere) | aucune indiquée | 2026-10-14 | Chat dans un navigateur, conversations de `/tell` en barre latérale |

Réglages de Chat 2 lus dans son `Configuration.cs` : visibilité (cinématique, combat, écran de chargement, inactivité, interface cachée), horodatage (joli, compact, masquer les doublons), par onglet (canal, non-lus, opacité, déplacement, redimensionnement, détaché), doublons repliés, sons, conservation du focus, jusqu'à 10 000 lignes affichées, polices et italique, aperçu de saisie, base (combat, récolte et artisanat optionnels), interface web.

**Ce qui lui manque par rapport à WoW, d'après ses réglages et sa page** : alertes par mots-clés, règles conditionnelles, conversations façon WIM, abréviation des canaux, copie de tout le chat, thèmes complets, profils par contexte, recherche avancée. À confirmer en l'essayant : un réglage absent de ce fichier peut exister ailleurs.

## Ce que font les addons WoW

Sources : pages de [Prat 3.0](https://www.wowace.com/projects/prat-3-0), [Chattynator](https://www.curseforge.com/wow/addons/chattynator), [WIM](https://www.curseforge.com/wow/addons/wim-3) et [Glass](https://www.curseforge.com/wow/addons/glass). La liste des plus de 30 modules de Prat n'est pas publiée telle quelle : elle est reconstituée depuis sa description.

Légende de la colonne « FF14 » : **OK** faisable, **Limité** faisable en partie, **Non** impossible ou interdit.

| Fonction WoW | Addon | FF14 | Comment, ou pourquoi pas |
|---|---|---|---|
| Fond, cadre, thèmes (sombre, ElvUI…) | Chattynator, Prat | OK | Thèmes Tensho, dont le style FF14 déjà présent |
| Police, taille, contour, espacement entre messages | Prat, Chattynator | OK | Polices Dalamud ; contour à sonder |
| Fondu après un délai, opacité | Glass, Prat | OK | Par onglet et au survol |
| Défilement doux, vitesse de la molette | Glass, Prat | OK | Dessin ImGui |
| Historique entre les sessions | Prat, Chattynator | OK | Fichier local, voir décision C6 |
| Horodatage au format choisi | Prat, Chattynator | OK | Heure locale, serveur ou Éorzéenne |
| Couleur des noms par classe | Prat | Limité | Seulement pour ce que le jeu montre : groupe, et personnages à portée. Sinon couleur choisie ou calculée sur le nom |
| Niveau à côté du nom | Prat | Limité | Mêmes limites |
| Crochets autour des noms | Prat | OK | Affichage seulement |
| Abréviation des canaux | Prat, Chattynator | OK | `[LS1]`, ou un nom choisi |
| Canal collant, dernier interlocuteur | Prat | OK | Une partie existe dans le jeu |
| Boîte de saisie déplaçable, détachée | Prat | OK | Avec notre propre saisie, phase 5 |
| Historique de saisie aux flèches | Chattynator | OK | Phase 5 |
| Copie d'URL, copie du chat, copie d'une ligne | Prat, WIM | OK | Presse-papiers uniquement |
| Inviter par alt-clic, `/tt` | Prat | Limité | Clic explicite seulement, via le menu du jeu. Aucun envoi automatique |
| Alias de commandes | Prat | Limité | Texte remplacé à la saisie, envoi par Entrée de l'utilisateur |
| Alerte sur son nom ou des mots choisis, sons par canal | Prat, WIM | OK | Moteur de règles, phase 3 |
| Onglet qui clignote ou change de couleur | Prat | OK | Plus un compteur dans la barre du serveur |
| Recherche dans le chat, filtres | Prat | OK | Phase 3 |
| Filtre anti-spam | Prat (bêta) | Limité | Règles locales ; pas de modèle entraîné dans un premier temps |
| Icônes d'objet et de classe dans le texte | Prat | OK | Icônes du jeu |
| Noms des personnages secondaires | Prat | Limité | Note locale saisie à la main. Aucune détection de personnages secondaires : ce serait une information qu'un joueur n'a pas |
| Fenêtres de conversation à onglets | WIM | OK | Phase 4 |
| Copie BBCode, animations, skins, émoticônes | WIM | Limité | Émoticônes externes seulement sur décision C9 |
| « Exposé » : tout cacher d'un geste | WIM | OK | Raccourci, plus cacher en combat |
| Félicitations automatiques de hauts faits | Prat | **Non** | Envoi automatique, interdit |
| Synchronisation de compte, partage | Prat, Chattynator | OK | Export et import, voir [TEMPLATES-001](TEMPLATES-001.md) |

## Ce qu'on peut faire de plus que WoW

Ce sont les apports propres à Tensho, ceux qui justifient de ne pas simplement installer Chat 2.

- Un objet cité dans le chat ouvre sa fiche Tensho (prix, sources, recettes, usages), et sa bulle réutilise celle qui existe.
- Une position cliquée s'ouvre sur la carte Tensho. La lecture des liens existe déjà (`ChatMapLinkWatcher`).
- Depuis un message : créer un rappel ou une alarme, ou mettre de côté un objet. Clic explicite uniquement.
- Règles conditionnelles : canal, expéditeur, texte (mot ou expression régulière), zone, métier, heure, en instance ou non. Elles peuvent colorer, cacher, jouer un son, envoyer vers un onglet, épingler, ou notifier sur Discord (décision C8).
- Profils par contexte, changés automatiquement : en donjon, en artisanat, en ville, en RP, par personnage.
- Messages épinglés et signets, qui ne défilent pas.
- Conversations : historique par personne, note personnelle, brouillon gardé à la fermeture.
- Mode capture d'écran : noms masqués ou remplacés de façon stable.
- Aperçu de saisie : longueur restante, découpage en plusieurs messages proposé mais jamais envoyé seul.
- Export d'une conversation en texte, sur demande.
- Tout est en français et en anglais, avec une bulle et un ↺ par réglage (règle du projet).

## Lignes rouges

Elles ne dépendent d'aucune décision.

- **Aucun envoi automatique** : pas de réponse automatique, de message programmé, de répétition ni de félicitations. Chaque ligne envoyée part d'un Entrée ou d'un clic de l'utilisateur. Les [règles de Dalamud](https://dalamud.dev/plugin-publishing/restrictions/) interdisent les requêtes au serveur sans action directe de l'utilisateur.
- **Aucune information qu'un joueur n'a pas** : pas de recherche de personnages secondaires, de suivi entre personnages, ni de collecte d'identifiants de compte d'autres joueurs, même en cache.
- **Combat hors périmètre** (DEC-009) : les lignes de combat peuvent s'afficher ou se cacher, mais ne sont ni analysées ni comptées.
- **Rien n'est envoyé ailleurs sans clic** : les messages des autres joueurs ne partent vers aucun service par défaut (décisions C8 et C9).
- Code original uniquement : Chat 2 et les autres se lisent pour comprendre.

## Architecture prévue

`KEEP → EXTEND → NEW`. Tensho a déjà `IChatGui` (`ChatMapLinkWatcher`, `GardenChatWatcher`), et l'envoi par `UIModule.ProcessChatBoxEntry` limité à des commandes construites par le plugin (alarmes, annonce de train).

Code pur, testé hors du jeu, dans `Chat/` :

- `ChatRecord` : copie d'un message (type, expéditeur, segments, liens, heure) ;
- `ChatFilters` et `ChatRules` : moteur de règles, borné (taille et délai des expressions régulières) ;
- `ChatFormat` : horodatage, abréviations, découpage en segments colorés ;
- `ChatHistory` : anneau borné, plus fichiers locaux selon C6 ;
- `ChatInputModel` : canal courant, cible de `/tell`, historique, longueur ;
- `ChatSettings` : tous les réglages et leurs bornes.

Code lié au jeu :

- `ChatReader.Dalamud` : `IChatGui.ChatMessage`, copie immédiate, rien de lourd dans le rappel ;
- `ChatSender.Dalamud` : le seul point d'envoi, appelé par Entrée ;
- `ChatNativeVisibility.Dalamud` : cache les fenêtres `ChatLog` et `ChatLogPanel_0` à `_3` (noms présents dans l'exécutable du jeu, comportement à sonder), toujours restaurées au déchargement.

Interface : `MainWindow.Chat*.cs` et une fenêtre par onglet détaché. Le dessin ne parcourt que les lignes visibles, avec une mise en page calculée à l'avance : c'est le risque de performance principal.

**Point dur : la saisie.** Chat 2 s'appuie sur plusieurs hooks par signature (rafraîchissement du chat, changement de canal, cible de réponse), qui peuvent casser à chaque patch. Le plan évite les hooks tant que possible : canal changé par les commandes du jeu, `/tell` construit par nous. Les limites qui en résultent (autotraduction, insertion de liens d'objet, `/tell` en Eureka ou Bozja) sont à relever en phase 5 et voir décision C5.

## Phases

La taille est une estimation subjective, pas une mesure. Chaque phase est une ou plusieurs versions, avec Verify vert et une liste dans TESTING.md.

| Phase | Contenu | Le chat du jeu | Taille |
|---|---|---|---|
| 0. Sonde | Types et contenus des messages, journal d'un soir, comportement de `ChatLog` caché ; preuve dans `docs/evidence/` | Inchangé | Petite |
| 1. Lecture | Fenêtre avec onglets, filtres par canal, couleurs, horodatage, police, opacité, non-lus, défilement, plafond de lignes | Inchangé | Moyenne |
| 2. Interaction | Liens d'objet, joueur, carte, quête, URL ; bulles d'objet et menu du jeu ; copie de ligne et de tout le chat ; fiche Tensho, carte | Inchangé | Moyenne |
| 3. Règles | Moteur de règles, mots-clés, sons, recherche, doublons repliés, masquage du bruit, épingles, compteur dans la barre du serveur | Inchangé | Moyenne |
| 4. Conversations | Fenêtres façon WIM, historique par personne, notes, brouillons, historique sur disque selon C6 | Inchangé | Moyenne |
| 5. Saisie | Boîte de saisie, canaux, `/tell`, historique, aperçu, alias | Coexiste | **Grande**, la plus risquée |
| 6. Remplacement | Cacher le chat du jeu, règles de visibilité (cinématique, combat, inactivité), repli sûr vers le chat du jeu en cas d'erreur, avertissement si Chat 2 est actif | Caché sur option | Moyenne |
| 7. Apparence | Thèmes, profils par contexte, import et export (lié à TEMPLATES-001) | Selon réglage | Moyenne |
| 8. Extras | Mode capture d'écran, export de conversation, options C8 et C9 retenues | Selon réglage | Petite |

Un arrêt après la phase 4 laisse un outil complet de lecture et d'alerte, à côté du chat du jeu.

## Réglages prévus

Chacun aura une bulle et un ↺, et sera trouvable par la recherche des réglages. Environ 120 en tout, ce qui demande une page à part et des valeurs par défaut sobres : tout ce qui n'est pas la lecture simple est désactivé au départ.

- **Général** : activer, page ou fenêtre, chat du jeu caché ou non, profil, raccourci, mode capture d'écran.
- **Visibilité** : cinématique, combat, écran de chargement, interface cachée, hors connexion, inactivité et délai, fondu et délai, opacité au repos et au survol, « tout cacher » par raccourci.
- **Fenêtre** : position, taille, verrouillage, barre de titre, onglets en haut ou sur le côté, marge, fond, cadre, thème.
- **Texte** : police, taille, gras, italique, contour, interligne, espace entre messages, alignement, retrait des lignes suivantes, ligne de séparation « non lu », plafond de lignes, vitesse de la molette, retour au bas automatique, défilement doux.
- **Horodatage** : afficher, format (12 ou 24 h, secondes), heure locale, serveur ou Éorzéenne, masquer les doublons, couleur, position, clic pour copier.
- **Noms** : format (prénom, initiales, complet), serveur affiché ou non, crochets, couleur (fixe, par nom, par métier si connu), icône de métier si connue, groupe et compagnie libre mis en évidence, notes.
- **Canaux** : pour chacun, couleur, abréviation, nom, affiché ou non, son, historique sur disque ou non.
- **Onglets** : par onglet, nom, canaux, canal de saisie, non-lus (compteur, point, rien), clignotement, détaché, opacité, horodatage, règles propres, modèle d'onglet.
- **Règles** : conditions, actions, ordre, activation, essai sur un texte, compteur de déclenchements.
- **Sons** : son du jeu ou fichier local, volume, par canal et par règle, silence en cinématique et en combat.
- **Conversations** : ouvrir à la réception ou non, regrouper, historique, notes, brouillons, sons, fermeture.
- **Liens** : comportement du clic, du clic droit et de Maj-clic sur objet, joueur, carte, URL ; confirmation avant d'ouvrir une URL (activée, non désactivable pour les URL inconnues, voir C3).
- **Saisie** : position, détachée, canal collant, retour au canal après `/tell`, historique, aperçu, alias, focus conservé, taille.
- **Filtres de bruit** : messages système par famille, doublons, contenu répété, liste blanche.
- **Historique** : conserver ou non, durée, taille, canaux exclus (combat et `/tell` selon C6), effacer, exporter.
- **Intégrations** : Discord, traduction, émoticônes (selon C8 et C9).
- **Diagnostic** : sonde, compteurs, temps de dessin.

## Essais en jeu à prévoir

- Aucune perte de message face au chat du jeu, sur une soirée, onglet « tout » comparé.
- Déchargement du plugin en plein affichage : le chat du jeu réapparaît toujours.
- Performance : 10 000 lignes, grande ville, Linkshell actif.
- Saisie : tous les canaux, Linkshells, `/tell` avec serveur, zone d'Eureka ou Bozja, autotraduction.
- Coexistence avec Chat 2, Simple Tweaks et NoSoliciting.
- Patch du jeu : un hook qui ne se résout plus laisse le chat du jeu utilisable.

## Décisions à prendre

La recommandation est donnée d'abord. Les décisions **C1 à C4 sont à prendre avant de coder**, les autres avant leur phase.

| Id | Question | Recommandation | Alternatives et conséquence |
|---|---|---|---|
| C1 | Jusqu'où aller ? | **Par couches, décision de continuer après la phase 4** | Remplacement complet d'emblée : le plus demandé mais doublonne Chat 2, avec saisie et hooks à maintenir. Compléments seulement : le moins de risque, sans saisie |
| C2 | Quelle place dans la feuille de route ? | **Après la campagne de tests en jeu en cours** ; la phase 0 peut se faire avant | Tout de suite : la campagne et la publication prennent du retard |
| C3 | Quelle attitude face à Chat 2 ? | **Détecter et prévenir, sans bloquer** ; IPC plus tard si utile | S'y intégrer (ajouter nos fonctions à son menu) : moins de travail, mais dépend de lui. L'ignorer : doublons d'affichage possibles |
| C4 | Publication D17 ? | **Demander sur le Discord de Dalamud avant la phase 5**, comme le conseillent [leurs règles](https://dalamud.dev/plugin-publishing/restrictions/). Le module serait séparable | Le chat pourrait être jugé doublon ; la page ne le traite pas |
| C5 | Hooks pour la saisie ? | **Aucun au départ**, ajout seulement pour une limite qui gêne vraiment, avec repli | Hooks d'emblée : saisie complète, mais fragile à chaque patch |
| C6 | Historique sur disque ? | **Désactivé par défaut**, puis fichiers locaux par jour, 30 jours, sans combat et `/tell` exclus par défaut. Fichiers plutôt que SQLite pour éviter une dépendance native | Ces messages sont ceux d'autres joueurs : la décision touche à leur vie privée. SQLite : recherche plus rapide, une bibliothèque de plus |
| C7 | Couleur des noms par métier ? | **Seulement si le jeu le montre à ce moment** (groupe, à portée), sans rien garder entre les sessions | Cache local des personnages vus : plus de couleurs, mais revient à suivre des joueurs |
| C8 | Notifier Discord sur une règle ? | **Oui, seulement si l'utilisateur crée la règle, avec le nom masqué par défaut**, et jamais les `/tell` par défaut | Interdit partout : plus simple et sans risque pour la vie privée d'autrui |
| C9 | Services externes (traduction, émoticônes) ? | **Non pour la première version** | Sur clic : utile, mais envoie le message d'un autre joueur vers un tiers et ajoute des requêtes |
| C10 | Filtre anti-spam ? | **Règles locales et liste partagée en modèle** | Modèle entraîné : plus efficace, très coûteux, NoSoliciting le fait déjà |
| C11 | Chat 2 comme base de code ? | **Non**, code original comme le reste du projet, même si l'EUPL prévoit une compatibilité avec la GPL | Le réutiliser : gain de temps, à vérifier juridiquement, et change la règle du projet |
| C12 | Combien de réglages visibles ? | **Tous, rangés dans des sections repliées, avec une vue « essentiels »** | Tout à plat : conforme à la demande, mais difficile à parcourir |
