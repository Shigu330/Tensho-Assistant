# Publication sur le dépôt officiel Dalamud (D17)

Règles relues le 2026-11-04 : [soumission](https://dalamud.dev/plugin-publishing/submission),
[approbation](https://dalamud.dev/plugin-publishing/approval-process),
[restrictions](https://dalamud.dev/plugin-publishing/restrictions), [politique IA](https://dalamud.dev/plugin-publishing/ai-policy),
[métadonnées](https://dalamud.dev/plugin-development/plugin-metadata),
[considérations techniques](https://dalamud.dev/plugin-development/technical-considerations), README de
[DalamudPluginsD17](https://github.com/goatcorp/DalamudPluginsD17). Compilation chez eux : [relevé](../evidence/0.512-d17-build.md).
Liste relue contre le code à la version 0.512.

La décision de soumettre appartient à l'auteur (DEC-023 : à la version 1.0.0). Un nouveau plugin passe d'abord par le canal de test. L'alternative
reste le dépôt personnel, qui existe : `https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/repo.json`, mis à jour par
`Verify.ps1 -Release` (dernière release : `Stable-0.451.0`, 2026-11-03).

## Ce qui reste avant la soumission

Par ordre. Les points marqués **règle Dalamud** demandent un résumé et l'accord de l'auteur (consigne du 2026-11-04).

| # | Point | Qui | État |
|---|---|---|---|
| 1 | Jouer sur la dernière version : Dalamud demande que l'auteur ait testé. 284 cases ouvertes dans [TESTING](TESTING.md), rien de 0.490 à 0.512 validé en jeu sauf ce que lit l'auto-contrôle | auteur + agent | à faire |
| 2 | **Règle Dalamud** : titre selon le job, seule requête au serveur sans clic hors alarmes (réglage désactivé par défaut) | auteur | à décider : le retirer de la version publiée, ou le présenter dans la PR |
| 3 | **Règle Dalamud** : alarmes du jeu par `/alarm` sans clic quand le réglage est actif (désactivé par défaut) | auteur | à décider : garder et présenter, ou clic seulement |
| 4 | **Règle Dalamud** : dresseur. La page est une liste d'informations ; le panneau du Dédale n'est pas codé (DEC-032). Contacter l'équipe avant de le coder | auteur | en attente : le dresseur est mis de côté |
| 5 | **Règle Dalamud** : fenêtres. « Si ça ressemble à une fenêtre, utiliser l'API de fenêtres. » Plusieurs panneaux utilisent `ImGui.Begin` (liste plus bas) | agent, sur accord | à convertir : fenêtres libres d'abord |
| 6 | **Règle Dalamud** : déclaration d'IA et texte de la PR écrits par l'auteur ; il doit pouvoir expliquer le code. Taille à assumer : 474 fichiers, 53 700 lignes C# (3,4 Mo), soumission refusable à la discrétion de l'équipe si elle dépasse ce que l'auteur peut justifier | auteur | à faire ; brouillon dans `publish/d17/PULL_REQUEST.md` |
| 7 | **Règle Dalamud** : icône générée par IA, déclarée. La politique préfère une icône faite main | auteur | à décider : la garder ou la remplacer |
| 8 | Captures pour l'installeur : `images/image1.png` à `image5.png`, 730×380 au plus. Facultatif, aucune n'existe | auteur (en jeu) | à faire |
| 9 | Passer à 1.0.0 (décision de l'auteur), Verify, commit public, release `Stable-1.0.0` | agent, sur demande | à la fin |
| 10 | Hash du commit et `changelog` dans `manifest.toml`, fork de goatcorp/DalamudPluginsD17, branche, copie de `publish/d17/testing/live/TenshoAssistant/`, PR | auteur | à la fin |

## État de la liste de contrôle

| Exigence | État | Où |
|---|---|---|
| Compilation chez eux : Linux, SDK 10.0.101, réseau coupé, fichier de verrouillage | **`global.json` corrigé en 0.512** (il exigeait 10.0.403, leur image n'aurait pas compilé). Job CI qui reprend leur image et leur commande ; il garde le réseau | `global.json`, `.github/workflows/build.yml`, [relevé](../evidence/0.512-d17-build.md) |
| Fichier `packages.lock.json` suivi par git | fait | `src/TenshoAssistant/packages.lock.json` |
| Dépendance NuGet examinée par l'équipe | `Raphael.Net` 5.0.0 (MIT, bibliothèque native) ; même version que Craftimizer, déjà publié ; à présenter dans la PR | `TenshoAssistant.csproj` |
| Manifeste JSON : Name, Author, Punchline, Description | fait ; Description réécrite en 0.512 : lignes coupées à la main comme le demande la documentation, résumé en première ligne, fonctions et réseau à jour | `src/TenshoAssistant/TenshoAssistant.json` |
| InternalName, AssemblyVersion, DalamudApiLevel laissés à DalamudPackager | fait ; contrôlé par `Verify.ps1` | idem |
| Version déterministe | fait : seulement `<Version>` du csproj | `TenshoAssistant.csproj` |
| Fenêtres normales via le système de fenêtres Dalamud | partiel : principale, carte et guide y sont. En `ImGui.Begin` : panneaux accrochés à une fenêtre du jeu (import de banque, carnet d'artisanat, survol d'objet, Personnage, outil de mission, Revue de mode, récolte, pêche, harpon, collectionnable, pêche en mer, ventes des servants, projection d'artisanat, demande de Triple Triade, bouton de tri) ; **panneaux libres à convertir** : fenêtre de suivi, tâches à l'écran, barres d'usure et de PR | `MainWindow.*.cs`, `BankImportOverlay.cs` |
| Dessins par-dessus le jeu | `GetForegroundDrawList` pour le Mini Cactpot, le conseil et le rappel de deck de Triple Triade ; à présenter dans la PR | `MainWindow.TriadAdvice.cs` |
| Pas de requête automatique au serveur du jeu | **Deux points à trancher** (points 2 et 3 ci-dessus). Tout le reste part sur un clic : téléportation, application du tri, chaîne de macros, écriture des macros, cabine d'essayage, équiper un job, appel du train de chasse. `/alarm` : une commande toutes les 0,5 s par défaut (réglable de 0,25 à 5 s), jamais plus de 30 alarmes du jeu | `Collections/TitleChanger.Dalamud.cs`, `Alarms/AlarmService.Dalamud.cs` |
| Pas d'automatisation (artisanat, dialogues, butin…) | conforme : simulateur, solveur et coup conseillé sans exécution | — |
| Éléments de combat seulement informatifs, ce que le joueur saurait déjà | parser : aucun (DEC-009). Page dresseur : données du jeu (DEC-032). Dédale : point 4 | `MainWindow.Beastmaster.cs` |
| Aucun avantage en contenu compétitif | Conseil de Triple Triade contre les PNJ seulement, jamais en tournoi ni contre un joueur (0.510, DEC-033) | `MainWindow.TriadAdvice.cs` |
| Identifiants de compte : seulement le sien | conforme : seul `player.ContentId` (personnage local) sert de clé | lecteurs `*.Dalamud.cs` |
| Aucune donnée personnelle envoyée, HTTPS, nom d'hôte | conforme. Universalis, Garland Tools, wiki, Teamcraft (une requête GraphQL par lieu de pêche), PaissaDB, GitHub (LuminaSupplemental, GatherBuddy, FFXIVGachaSpreadsheet, galerie et vidéos de ce dépôt). Exception choisie par le joueur : webhook Discord désactivé par défaut, hôtes de Discord seulement, nom du personnage seulement sur option, adresse jamais journalisée. **À présenter dans la PR** | [SECURITY](../../SECURITY.md) |
| Signaler un problème | formulaire GitHub prérempli, ouvert dans le navigateur ; rien n'est envoyé par le plugin | `Help/ProblemReport.cs` |
| Pas de télémétrie | conforme : aucune | — |
| Lumina plutôt que XIVAPI | conforme : feuilles du jeu ; XIVAPI n'est pas appelé par le plugin | [preuve](../evidence/0.451-xivapi-v2.md) |
| Icône `images/icon.png` 1:1, 64–512 px | fait : 512×512, générée par l'agent, déclarée dans la description | `src/TenshoAssistant/images/icon.png`, `publish/d17/…/images/icon.png` |
| `manifest.toml` dans `testing/live/TenshoAssistant/` | prêt ; `changelog` à jour en 0.512 ; **hash du commit à renseigner** | `publish/d17/testing/live/TenshoAssistant/manifest.toml` |
| Déclaration d'IA dans la PR | brouillon niveau « Auto » relu en 0.512 ; **à réécrire par l'auteur** | `publish/d17/PULL_REQUEST.md` |
| Traductions assistées par IA relues par des locuteurs | allemand et japonais : quelques dizaines de clés, non relues, déclarées ; anglais et français complets | `src/TenshoAssistant/Localization/Languages/` |
| Données embarquées sous licence compatible, avis conservé | fait : SubmarineTracker et FFXIVGachaSpreadsheet (MIT, texte complet dans le fichier), Character Panel Refined (MIT, dans le code), LuminaSupplemental et Lotlab (GPL-3.0), croisements de nick75g (MIT) ; crédits dans le README et la page Crédits | `FreeCompany/Data/`, `Reminders/Data/`, `Character/StatFormulas.cs`, `Credits/CreditCatalog.cs` |

## Modèle de `manifest.toml` (dépôt DalamudPluginsD17, `testing/live/TenshoAssistant/manifest.toml`)

```toml
[plugin]
repository = "https://github.com/Shigu330/Tensho-Assistant.git"
commit = "<hash du commit public à construire>"
owners = ["Shigu330"]
project_path = "src/TenshoAssistant"
changelog = "<résumé de la version>"
```

À côté : `images/icon.png` (obligatoire), `images/image1.png` à `image5.png` (facultatifs). Une PR par plugin, une branche par
soumission ; chaque mise à jour = nouvelle PR qui change au moins `commit`. Reconstruire une PR : commentaire `bleatbot, rebuild`.
Passer du test au stable : déplacer le dossier, sans changer de version. Approbation : quatre votes favorables, un veto suffit, la file
peut dépasser une semaine.

## Décisions déjà prises

2026-10-31 : nom affiché « Tensho Assistant » ; niveau d'IA « Auto » ; icône générée et déclarée ; dépôt officiel. 2026-11-02 (DEC-023) :
soumission à la version 1.0.0 ; allemand et japonais gardés dans le paquet, partiels, repli sur l'anglais. 2026-11-04 (DEC-033) : cadre
des fenêtres du jeu retiré (il était prévu avant D17) ; conseils de jeu en JcE seulement.
Icônes de quête 71141 / 71151 : le réglage dit « variante n » tant qu'aucun relevé en jeu n'en donne le sens ; rien de bloquant.
