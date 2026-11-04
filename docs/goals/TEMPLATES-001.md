# TEMPLATES-001 : modèles communautaires pour ajouter des outils à Tensho (plan, rien n'est codé)

Demande de l'utilisateur (2026-11-04) : « prévois aussi un plan pour template communautaire ajout outils dans tensho ». Les décisions à prendre sont à la fin.

**Décidé le 2026-11-04 ([DEC-029](../project/DECISIONS.md))** : les deux, code d'abord, tout de suite. A0, A1, A2 et A4 faits en 0.470. La partie sans code (B) reste à faire.

## Deux sens possibles

La demande peut se lire de deux façons, et elles ne visent pas les mêmes personnes. Le plan couvre les deux. La décision T1 tranche lequel on fait, et dans quel ordre.

| | A. Modèle de code | B. Modèle sans code |
|---|---|---|
| Pour qui | Un développeur qui propose un outil | N'importe quel joueur |
| Forme | Un squelette C#, une pull request relue | Un fichier de données, importé dans le jeu |
| Ce qu'on peut faire | Tout ce que permet un plugin | Ce que prévoient les blocs de Tensho |
| Risque | Relecture à chaque contribution | Limité par le format, aucun code exécuté |
| Numérotation | x.y.**Z**, versions de la communauté (DEC-024) | Pas de version du plugin |

## Ligne rouge commune

**Aucun code venu de la communauté n'est téléchargé ni exécuté par le plugin.** Un plugin Dalamud n'a pas de bac à sable : il a accès à tout le jeu et à l'ordinateur. La page des [règles de Dalamud](https://dalamud.dev/plugin-publishing/restrictions/) ne traite pas ce cas, mais c'est une question de sécurité avant d'en être une de règlement. Tout outil avec du code passe donc par une pull request, relue et publiée dans une version du plugin.

## A. Modèle de code

Aujourd'hui, ajouter une page demande de modifier plusieurs endroits à la main. Pour la page Jardin, par exemple : `Page` et le `switch` de `MainWindow.cs`, la catégorie de navigation, la liste des fichiers de `MainWindow.Advanced.cs`, les textes en et fr, les réglages et les tests. Rien de tout cela n'est décrit comme un parcours, même si [CONTRIBUTING.md](../../CONTRIBUTING.md) en donne les règles.

### Travail prévu

1. **A0. Guide.** Un parcours « ajouter un outil » dans CONTRIBUTING.md, avec la page Jardin comme exemple réel, et la liste de ce que `Verify.ps1` contrôlera.
2. **A1. Générateur.** `scripts/New-Tool.ps1 -Name Exemple -Category World`, qui crée :
   - `MainWindow.Exemple.cs` et `Exemple/ExempleModel.cs` (partie pure) ;
   - `tests/.../ExempleTests.cs`, enregistré dans `Program.cs` ;
   - les textes en et fr, et la ligne de navigation ;
   - une entrée de CHANGELOG à compléter.
3. **A2. Point d'enregistrement unique (à décider, T3).** Un descriptif par outil (identifiant, catégorie, icône, dessin, réglages), au lieu des listes dispersées. Refactorisation à faire avec prudence : la navigation est testée et le plugin a plus de 30 pages.
4. **A3. Contrôles automatiques.** Un test qui vérifie qu'une page est complète : navigation, textes, bulles, test associé. Les règles existantes (bulles, langues) s'étendent.
5. **A4. Accueil sur GitHub.** Un formulaire d'issue « proposer un outil » à côté de ceux existants (`bug_report`, `feature_request`, `translation`), une case dans le modèle de pull request (pas de combat, pas d'automatisation, sources citées, licence, usage de l'IA déclaré), et un label pour les bons premiers sujets.

### Règles reprises telles quelles

Pas de combat ni d'ACT, aucune automatisation, données prouvées par une sonde dans `docs/evidence/`, lecture du natif sur le thread du jeu, bornes, nettoyage de chaque abonnement, GPL v3, aucune copie de code AGPL.

## B. Modèle sans code

Un modèle est un fichier de données qui décrit une page faite de blocs fournis par Tensho. Il s'inspire de ce qui existe du côté de WoW, où [Chattynator](https://www.curseforge.com/wow/addons/chattynator) propose des réglages à importer, mais Tensho n'exécute jamais rien de ce que contient le fichier.

### Exemples de modèles

- Liste de préparation de raid : objets à avoir, comptés dans les sacs, avec leur prix.
- Routine du jour pour un artisan : cases remises à zéro chaque jour, liens vers les recettes.
- Liste d'objets à récolter pour un événement, avec fenêtres horaires et carte.
- Pense-bête de compagnie libre : horaires, liens, notes.
- Thème de l'interface.

### Format

```json
{
  "schema": 1,
  "id": "prepa-raid-exemple",
  "name": { "en": "Raid prep", "fr": "Préparation de raid" },
  "author": "pseudonyme",
  "license": "CC0-1.0",
  "blocks": [
    { "type": "heading", "text": { "en": "Food", "fr": "Repas" } },
    { "type": "itemList", "items": [ { "id": 12345, "wanted": 10 } ], "show": ["owned", "price"] },
    { "type": "checklist", "reset": "daily", "entries": [ { "en": "Roulette", "fr": "Roulette" } ] },
    { "type": "note", "text": { "en": "Free text", "fr": "Texte libre" } }
  ]
}
```

L'identifiant d'objet ci-dessus est un exemple. Les noms affichés viennent toujours des données du jeu, dans sa langue.

### Blocs prévus

| Bloc | Ce qu'il montre | Lit quoi |
|---|---|---|
| `heading`, `note` | Titre, texte | Rien |
| `checklist` | Cases, remise à zéro quotidienne, hebdomadaire ou aucune | État local |
| `counter` | Compteur manuel | État local |
| `itemList` | Icônes, possédés, prix, sources | Sacs et prix déjà connus de Tensho |
| `recipeList` | Recettes, ingrédients | Catalogue de recettes |
| `questList`, `dutyList` | Suivi | Catalogues existants |
| `mapLink` | Position sur la carte Tensho | Carte |
| `timer` | Compte à rebours vers un horaire ou une remise à zéro | Horloge, `ResetSchedule` |
| `link` | URL `https`, ouverte après confirmation | Rien |
| `settingsPreset` | Valeurs de réglages listés (thème) | Réglages autorisés, appliqués après aperçu |

Chaque bloc correspond à une lecture existante de Tensho. Un modèle ne peut pas accéder à autre chose.

### Sécurité

Un modèle est traité comme une donnée non fiable, et l'import montre un aperçu complet avant tout enregistrement.

- **Aucune commande** : pas de `/commande`, de macro, de script, d'expression ni de chemin de fichier. Un texte qui commence par `/` s'affiche comme du texte et ne peut pas s'envoyer.
- **Format strict** : champ inconnu ou type inconnu = refus, avec la raison. Tailles bornées (fichier, blocs, texte).
- **Identifiants vérifiés** dans les données du jeu. Un identifiant inconnu est signalé, jamais deviné.
- **Texte brut** : pas de payload du jeu, caractères de contrôle retirés.
- **Liens** : `https` uniquement, domaine affiché et confirmation à chaque ouverture.
- **Réglages** : liste blanche. Jamais le webhook Discord ni un réglage qui envoie quelque chose.
- **Aucune requête au serveur du jeu**, et aucune donnée personnelle dans un export (ni personnage, ni serveur, ni contenu des sacs).

### Partage

Du plus simple au plus coûteux. Chaque niveau est utile seul.

1. **Texte à copier-coller** : un bloc compressé, comme les listes déjà exportées vers Teamcraft.
2. **Fichier** : import et export sur disque.
3. **Galerie dans le dépôt** : dossier `community-templates/` (ou dépôt séparé), ajout par pull request, validé automatiquement par les mêmes règles que le plugin, puis relu.
4. **Galerie dans le plugin** : la liste se charge **sur clic**, jamais toute seule, avec contrôle de l'empreinte de chaque fichier et mention « non relu par l'auteur » selon T7.

### Phases

La taille est une estimation subjective, pas une mesure.

| Phase | Contenu | Taille |
|---|---|---|
| B0 | Format pur, validation, tests avec fichiers malveillants | Petite |
| B1 | Page « Mes outils » : afficher un modèle, blocs simples (titre, note, cases, compteur, minuteur), état local | Moyenne |
| B2 | Blocs reliés aux données (objets, recettes, carte, quêtes) | Moyenne |
| B3 | Éditeur dans le jeu, sans écrire de JSON | Moyenne à grande |
| B4 | Partage : copier-coller et fichier, aperçu à l'import | Petite |
| B5 | Galerie dans le dépôt, validation automatique | Petite |
| B6 | Galerie dans le plugin, sur clic | Moyenne |
| B7 | Préréglages (thèmes) | Petite |

### Essais en jeu à prévoir

- Import d'un modèle correct, d'un modèle trop gros, et d'un modèle au champ inconnu.
- Langue du plugin changée, langue du jeu changée : textes et noms d'objets corrects.
- Modèle dont un objet n'existe plus après un patch.
- Remise à zéro quotidienne et hebdomadaire des cases.
- Désinstallation d'un modèle : son état local part avec lui, rien d'autre ne change.

## Décisions prises (DEC-029)

| Id | Choix |
|---|---|
| T1 | Les deux, code d'abord |
| T2 | **Tout de suite** |
| T3 | **Registre des pages tout de suite** (fait, 0.470) |
| T4 et T5 | Dossier dans ce dépôt, CC0 |
| T6 et T7 | Galerie dans le plugin, sur clic ; validation automatique et relecture |
| T8 et T9 | Réglages en liste blanche ; sept blocs en première version |
| T10 | Case IA dans la pull request (recommandation appliquée, elle y était déjà) |

## Options étudiées (historique)

La recommandation est donnée d'abord.

| Id | Question | Recommandation | Alternatives et conséquence |
|---|---|---|---|
| T1 | Quel sens de « template » ? | **Les deux : A0 et A1 d'abord, puis B** | A seul : peu de travail, ne sert qu'aux développeurs. B seul : sert à tous les joueurs, sans aider les contributeurs |
| T2 | Quand ? | **Après la campagne de tests en jeu**, A0 et A1 avant la publication D17 | Tout de suite : retarde la campagne |
| T3 | Refaire l'enregistrement des pages (A2) ? | **Plus tard, seulement si des contributions arrivent** ; le générateur suffit au début | Tout de suite : plus propre, mais touche la navigation de toutes les pages |
| T4 | Galerie où ? | **Dossier dans ce dépôt** pour commencer | Dépôt séparé : historique du plugin plus propre, un dépôt de plus à gérer |
| T5 | Licence des modèles partagés ? | **CC0-1.0**, déclarée à l'envoi | GPL v3 : cohérente avec le plugin, mais lourde pour une simple liste |
| T6 | Galerie dans le plugin (B6) ? | **Oui, sur clic, plus tard** ; à vérifier avec l'équipe Dalamud avant D17 | Non : copier-coller et fichiers suffisent, aucun réseau et aucune question de règlement |
| T7 | Qui valide ? | **Validation automatique plus relecture humaine pour entrer dans la galerie** ; ce qui est importé à la main reste à la responsabilité du joueur | Automatique seule : ouvert et sans effort, mais du texte déplaisant peut passer |
| T8 | Modèles qui changent des réglages ? | **Oui, liste blanche et aperçu**, utile pour les thèmes | Non : plus simple, sans partage de thèmes |
| T9 | Blocs de la première version ? | **Titre, note, cases, compteur, minuteur, objets, lien** | Tous d'un coup : plus long avant un premier résultat |
| T10 | Déclaration de l'IA pour les contributions ? | **Case obligatoire dans le modèle de pull request**, conforme à la politique reprise dans CONTRIBUTING.md | Rien : risque au moment de la publication D17 |
