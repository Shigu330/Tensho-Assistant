# ONBOARD-001 : première prise en main, aide, vidéos et tickets (plan, rien n'est codé)

Demande de l'utilisateur (2026-11-04) : un plan complet, en pensant à l'open source et à la communauté, pour la première prise en main avec une interface guidée qu'on peut passer (visiter le plugin, donner des infos, faire les premiers réglages importants, ouvrir les bons menus une première fois, expliquer une première fois), une aide à chaque page et une aide globale, la possibilité d'ajouter des vidéos, et si possible un système de tickets GitHub depuis le jeu.

**Décidé le 2026-11-04 ([DEC-031](../project/DECISIONS.md)) : tous les choix recommandés de la fin de ce document.** Suivi : issue GitHub ONBOARD-001.

## Ce qui existe déjà

| Élément | État |
|---|---|
| Première ouverture | Rien de prévu : le plugin s'ouvre sur l'accueil, sans explication |
| Pages | 38, dans une seule liste (`MainWindow.Pages.cs`), avec un test qui vérifie qu'aucune n'en sort |
| Aide | Un texte sous le titre de certaines pages (`Heading`) et des infobulles, imposées par un test sur les champs. Pas de page d'aide |
| Sources en ligne | 7 sources activées par défaut (Universalis, Garland, wiki, Lumina Supplemental, GatherBuddy, Teamcraft, FFXIVGachaSpreadsheet). Le joueur ne les découvre que dans Paramètres › Avancé |
| Données que le jeu ne donne qu'après ouverture d'une fenêtre | Servants, coffre à apparence, armoire, carnets, titres, succès, calendrier, atelier, bestiaire |
| Rapport de bug | `BugReport()`, bouton « Copier le rapport » ; sans adresse Discord ni nom de personnage |
| Tickets | Formulaires GitHub `bug_report.yml` (champs `version`, `client`, `steps`, `expected`, `report`, `log`), `feature_request.yml`, `tool_proposal.yml`, `translation.yml` ; vulnérabilités en privé |
| Contenu communautaire | `community-templates/` : index avec empreinte SHA-256, lu sur `main`, sur clic, relu en pull request. **Modèle à réutiliser pour les vidéos** |
| Vérifications en jeu | Auto-contrôle (0.494) : il peut dire si une fenêtre a été lue |

## Lignes rouges

- **Passer à tout moment**, sans rien perdre : le guide n'est jamais bloquant, ne revient pas de lui-même une fois passé, et se relance depuis l'aide.
- **Le joueur agit, Tensho constate.** Le guide ne clique pas à la place du joueur et n'envoie rien au serveur du jeu. Il explique quelle fenêtre ouvrir, et coche l'étape quand le jeu a réellement donné la donnée (règles D17 de Dalamud : pas de requête automatique ni de jeu automatisé).
- **Aucune requête réseau sans clic**, pour les vidéos et les tickets. Rien n'est envoyé à notre insu.
- **Aucun secret dans le plugin ni dans le dépôt** : pas de jeton GitHub écrit dans le code, jamais.
- **Ce que voit le joueur est ce qui part** : un ticket montre son contenu avant l'envoi, et le joueur peut le modifier.
- **Le contenu de la communauté n'est jamais exécuté** : l'index est une donnée, relue en pull request.
- Textes en anglais d'origine plus `fr.json`, « vous », aucun français dans le code, comme le reste du plugin.

## A. Guide de première ouverture

Une fenêtre dédiée, qui s'ouvre une seule fois à la première ouverture de Tensho (pas au chargement du plugin, pour ne pas surgir en combat ou en cinématique). Barre de progression, « Précédent », « Suivant », « Passer cette étape », « Quitter le guide ».

| Étape | Contenu | Détecté par |
|---|---|---|
| 1. Bienvenue | Ce qu'est Tensho, ce qu'il ne fait pas (lecture seule, aucune automatisation). Langue | Rien |
| 2. Sources en ligne | Chaque source, ce qu'elle apporte, ce qui lui est envoyé, un interrupteur | Rien (décision G3) |
| 3. Réglages importants | 5 à 7 réglages avec aperçu : carte à la place de M, panneau au survol, alarmes et son, thème, polices, panneaux à côté du jeu | Rien |
| 4. Selon vos métiers | « Que faites-vous ? » artisanat, récolte, collection, Triple Triade, compagnie libre : la suite du guide et les catégories repliées s'adaptent, sans rien cacher pour de bon | Rien |
| 5. Fenêtres du jeu à ouvrir une fois | Liste cochable : servants, coffre à apparence, armoire, carnet d'artisanat, succès, bestiaire… Chacune dit où aller et ce que ça débloque dans Tensho. Facultatif, peut se finir plus tard | Lecture réelle, comme l'auto-contrôle (`OK` ou `OK_SEEN`) |
| 6. Visite | Pour chaque catégorie de la barre latérale, la page s'ouvre et une bulle montre l'essentiel | Le joueur avance |
| 7. Fin | Où trouver l'aide, les vidéos, le rapport de bug, comment relancer le guide. Résumé de ce qui reste à faire | Rien |

La liste de l'étape 5 reste visible ensuite sur l'accueil, repliable, jusqu'à ce qu'elle soit finie ou masquée.

**Joueurs existants :** pas de guide imposé. Une ligne sur l'accueil le propose une fois (décision G2). Une future version peut ajouter une étape : `GuideVersion` en mémoire, seules les nouveautés sont proposées.

**Conception :** données et code séparés, pour que la communauté puisse corriger un texte sans toucher à la logique.
- `Guide/GuideStep.cs`, pur et testé : étapes, progression, reprise, ordre selon les métiers, sans dépendance à ImGui.
- `MainWindow.Guide.cs` : affichage.
- `Configuration` : `GuideVersion`, `GuideStep`, `GuideDismissed`, `GuideChoices`. À classer dans `ConfigurationReset` : « Tout remettre par défaut » ne relance pas le guide.
- Contrôle : le guide s'efface en combat et en mission, comme les panneaux, et reprend ensuite.

## B. Aide

**Par page.** Un bouton « ? » dans l'en-tête de chaque page ouvre son aide : à quoi elle sert, comment commencer, ce qui est lu dans le jeu, quelle fenêtre ouvrir si elle est vide, ce qui peut manquer, et les vidéos de cette page.

**Première visite.** La première fois qu'on ouvre une page, un bandeau résume l'essentiel en deux lignes, avec « Compris » et « En savoir plus ». Il ne revient pas (`HelpSeen`, à classer dans `ConfigurationReset`). Option générale pour tout couper.

**Globale.** Une page Préférences › Aide :
- une recherche dans toute l'aide, qui va à la bonne page ;
- démarrage, relancer le guide ;
- questions fréquentes : « une page est vide », « pourquoi ouvrir cette fenêtre », « que signifient ✓, ✗ et ? », « que part sur Internet » ;
- les vidéos, le rapport de bug et les tickets ;
- le lien vers le dépôt et la manière de contribuer.

**État de la page.** L'aide d'une page réutilise l'auto-contrôle : « Cette page attend : ouvrez le coffre à apparence ». Elle dit ce qui manque sur ce personnage, pas seulement en général. C'est ce qui évite le plus de tickets.

**Conception.** Une clé par page dans les langues (`Help.<Page>.Purpose`, `.Start`, `.Empty`), ajoutée à `PageEntry`. **Un test refuse une page sans aide**, comme pour les infobulles. `scripts/New-Tool.ps1` crée ces clés, et CONTRIBUTING « Adding a tool » les exige.

## C. Vidéos et guides de la communauté

ImGui ne lit pas de vidéo : **le lien s'ouvre dans le navigateur**, après confirmation qui montre l'adresse.

- `community-help/index.json` : pour chaque entrée, page concernée, langue, titre, auteur, adresse, durée, version du jeu ou de Tensho.
- Même circuit que `community-templates/` : lu sur `main`, sur clic, gardé pendant la session, proposé en pull request, vérifié par un test (pas de script : ce fichier n'a pas d'empreinte à calculer).
- **Liste blanche d'hôtes** (décision G6) : une adresse d'un autre site est refusée à la vérification et dans le plugin.
- Tri par langue du plugin, puis l'anglais. Une vidéo plus ancienne que la page est marquée « peut-être dépassée ».
- Règles dans `community-help/README.md` : aucune automatisation ni outil interdit montré, droits clairs, relecture en pull request, retrait sur demande de l'auteur.
- Nos propres vidéos passent par le même index, sans exception.
- Le même format accepte des guides écrits.

**Limite :** tant qu'aucune release n'est publiée, `main` n'a pas cet index, comme pour la galerie.

## D. Tickets depuis le jeu

| Option | Comment | Pour | Contre |
|---|---|---|---|
| **1. Formulaire prérempli (recommandé)** | Fenêtre dans le jeu, puis le navigateur s'ouvre sur le formulaire GitHub déjà rempli. Le joueur valide | Aucun jeton, aucun serveur, rien à maintenir, le joueur voit tout | Compte GitHub requis, on quitte le jeu un instant, longueur d'adresse limitée |
| 2. Connexion GitHub dans le jeu (« device flow ») | Le joueur autorise le plugin avec un code, le ticket part du jeu | Tout se fait dans le jeu | Jeton conservé en clair dans la configuration, droits larges sur les dépôts publics, à faire accepter par Dalamud |
| 3. Serveur relais | Le plugin envoie à notre serveur, qui crée le ticket | Pas de compte requis | Serveur à payer et à surveiller, spam, jeton côté serveur, données qui transitent |

**Plan avec l'option 1 :**
1. Bouton « Signaler un problème » : aide globale, Diagnostics, et chaque message d'erreur d'une page.
2. Fenêtre : type (bug, idée, traduction), ce qui s'est passé, ce qui était attendu. La version, la langue, la page et le rapport sont préremplis et **visibles et modifiables**. L'auto-contrôle y est ajouté.
3. **Avant d'envoyer** : recherche des tickets proches, sur clic, dans le navigateur. (Fait ainsi en 0.504 plutôt que par l'API de GitHub : le plugin n'a alors aucune requête à faire.)
4. Ouverture de `issues/new?template=bug_report.yml&title=…&version=…&steps=…` : GitHub accepte le préremplissage des formulaires par les identifiants de champs. À vérifier à la mise en œuvre. Si l'adresse est trop longue, le rapport va dans le presse-papiers et le formulaire le demande.
5. Nettoyage : aucun nom de personnage, chemin de dossier ou adresse Discord (nettoyage de `SelfCheck.Clean` réutilisé), avec un test.
6. Le rapport indique qu'il vient du formulaire du jeu. (Pas d'étiquette : GitHub demande les droits sur le dépôt pour en poser par l'adresse.) Tri en anglais, comme aujourd'hui.

Une faille de sécurité reste signalée en privé : le bouton renvoie vers la page dédiée, jamais vers un ticket public. Sur le dépôt officiel, Dalamud a aussi son propre retour d'information dans l'installateur des plugins : à regarder au moment de D17.

## E. Ce que ça change pour les contributeurs

- Ajouter un outil demande son aide (test), et `New-Tool.ps1` en prépare le squelette.
- Une traduction d'aide passe par le circuit actuel (`translation.yml`).
- Une vidéo ne demande ni version du plugin ni entrée au CHANGELOG, comme un outil de la galerie.
- `docs/` : une page « Écrire une aide », courte, avec un exemple.

## Ordre proposé

Chaque ligne est une version, testée avant la suivante.

| Lot | Contenu | Taille | Dépend de |
|---|---|---|---|
| 1 | Registre d'aide par page, test, bouton « ? », page Aide globale, textes des 38 pages | Grand (surtout les textes) | Rien |
| 2 | Bandeau de première visite, état de la page lu par l'auto-contrôle | Moyen | 1 |
| 3 | Signaler un problème (option 1) | Moyen | Décision G5 |
| 4 | Moteur du guide, étapes 1, 2, 3, 7 | Moyen | 1 |
| 5 | Étape 5 (fenêtres à ouvrir, détection), liste sur l'accueil | Moyen | 4 |
| 6 | Étapes 4 et 6 (métiers, visite) | Moyen | 4 |
| 7 | `community-help/`, liste dans le plugin, script, README | Moyen | 1, puis une release |
| 8 | CONTRIBUTING, page « Écrire une aide », `New-Tool.ps1` | Petit | 1 |

## Ce qui ne pourra se valider qu'en jeu ou par vous

- Le guide sur un personnage neuf : je ne peux ni créer ni jouer de personnage. Un personnage secondaire chez vous, ou un testeur de la communauté, sera le vrai test.
- La lisibilité des textes et de la visite : je relis, mais seul un joueur qui découvre le plugin peut le dire.
- La création d'un vrai ticket : je ne l'envoie jamais sans votre accord.

## Décisions à prendre

| N° | Question | Choix recommandé |
|---|---|---|
| G1 | Par quoi commencer ? | L'aide avant le guide : elle sert à tout le monde et le guide s'appuie dessus |
| G2 | Pour les joueurs existants ? | Une ligne proposée une fois sur l'accueil, jamais imposée |
| G3 | Les 7 sources en ligne, activées par défaut, dans le guide ? | Le guide les présente avec leur interrupteur et ne change pas leur défaut |
| G4 | La visite s'adapte aux métiers choisis ? | Oui, sans rien cacher pour de bon |
| G5 | Tickets ? | Option 1 : formulaire prérempli, sans jeton ni serveur |
| G6 | Hôtes vidéo acceptés ? | YouTube d'abord, liste qu'on peut élargir par pull request |
| G7 | Qui relit les vidéos ? | Vous, comme pour la galerie, l'agent vérifie le format |
| G8 | Bandeau de première visite ? | Activé, avec une option pour tout couper |
| G9 | Langues de l'aide au départ ? | Anglais et français ; le reste par la communauté |
| G10 | Place dans la feuille de route ? | Après la page Dresseur et son conseil, avant la publication D17 |
