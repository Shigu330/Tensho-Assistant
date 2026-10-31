# Contribuer à Tensho Assistant / Contributing

*English summary at the end.*

## Prérequis

- Windows, Final Fantasy XIV avec XIVLauncher/Dalamud (plugins de développement activés).
- SDK .NET **10.0.403** (fixé par `global.json`), Dalamud.NET.Sdk 15 (récupéré au build).
- PowerShell 7 recommandé.

## Compiler et tester

```powershell
.\scripts\Verify.ps1
# SDK à un autre emplacement :
.\scripts\Verify.ps1 -DotNet 'C:\chemin\dotnet.exe'
```

Le script compile en Release, exécute les tests managés (`tests/TenshoAssistant.Tests`), valide les fichiers de langue et le paquet.
Une contribution doit le laisser **vert, sans avertissement**. DLL produite : `src/TenshoAssistant/bin/x64/Release/TenshoAssistant.dll`
(à ajouter dans les plugins de développement de Dalamud, puis `/tensho`).

## Organisation

- Code : `src/TenshoAssistant/` (un dossier par domaine ; les fichiers `*.Dalamud.cs` touchent au jeu, les autres sont du C# pur testé).
- Tests : `tests/TenshoAssistant.Tests/` (programme de tests sans framework : `Check(condition,"message")`).
- Documentation : voir [docs/README.md](docs/README.md). Historique : [CHANGELOG.md](CHANGELOG.md).

## Règles du projet

- **Ne jamais inventer une donnée de jeu.** Un lien de données (feuille Excel, champ natif, format externe) se prouve : sonde en lecture seule
  (`tools/ShopCurrencyProbe`, `tools/ApiProbe`) ou source citée ; la sortie va dans `docs/evidence/` et le code la cite.
- Inconnu ≠ zéro : afficher « inconnu » plutôt qu'une valeur supposée ; citer la source des données communautaires (Garland Tools, wiki FFXIV, Universalis).
- Mémoire du jeu : lue sur le thread framework puis copiée en données managées ; rien de lourd dans `Draw` ; lecture de chaînes natives bornée.
- Chaque abonnement, tâche ou fenêtre a un propriétaire et un nettoyage (déchargement propre du plugin).
- Interface : textes dans `src/TenshoAssistant/Localization/Languages/*.json` (anglais = référence, français complet) ; pas de texte en dur.
- Style : suivre le code voisin (nommage, densité des commentaires, C# compact du projet).
- Références sous licence AGPL ou autre (Mappy, SortaKinda…) : lues pour comprendre, **jamais copiées**. Le projet est sous **GPL v3**.
- Hors périmètre : ACT/IINACT, parsers, tout avantage en combat ou en JcJ.

## Proposer une modification

1. Une branche par sujet ; un sujet petit et vérifiable.
2. `Verify.ps1` vert ; nouveaux tests pour la logique pure.
3. Entrée dans [CHANGELOG.md](CHANGELOG.md) (quoi, pourquoi, preuves, « validé en jeu » ou non).
4. Pull request décrivant le test en jeu effectué.

## Publication Dalamud

Le dépôt officiel ([DalamudPluginsD17](https://github.com/goatcorp/DalamudPluginsD17), [règles](https://dalamud.dev/plugin-publishing/submission))
impose notamment (relevé 2026-10-31) :

- dépôt Git public, build depuis un commit précis ; `manifest.toml` dans `testing/live/<plugin>/` (repository, commit, owners, project_path, changelog) ;
- `images/icon.png` de 64×64 à 512×512 (images d'aperçu PNG ≤ 730×380, facultatives) ; DalamudPackager ; version et AssemblyVersion
  déterministes (pas d'horodatage) ; fenêtres via le système de fenêtres Dalamud ; état détaillé du projet : [docs/project/PUBLISHING.md](docs/project/PUBLISHING.md) ;
- [restrictions](https://dalamud.dev/plugin-publishing/restrictions) : pas de requête automatique au serveur du jeu sans action du joueur, pas
  d'automatisation de jeu (artisanat, dialogues, butin…), pas d'avantage en combat/JcJ, pas de contournement de la Mog Station, pas de collecte
  d'identifiants d'autres joueurs ;
- [politique IA](https://dalamud.dev/plugin-publishing/ai-policy) : l'usage d'IA doit être **déclaré** dans la pull request (rédigée par un humain) ;
  une soumission **entièrement générée par IA sans implication humaine réelle est refusée** ; l'auteur doit comprendre, tester et savoir expliquer le code.

**Situation de ce projet :** le code est écrit avec un agent IA (niveau « Auto ») sous la direction et les tests en jeu de son auteur.
Avant toute soumission officielle, l'auteur doit pouvoir répondre de chaque choix ; à défaut, distribuer via un dépôt de plugins tiers (URL
de dépôt personnalisé dans Dalamud). Points à vérifier avant soumission : requêtes externes (Universalis, Garland Tools, wiki) déclenchées
uniquement par l'action ou les réglages du joueur, téléportation et tri seulement sur clic, carte du jeu masquée et non modifiée.

---

## English summary

Build and test with `.\scripts\Verify.ps1` (.NET SDK 10.0.403); it must stay green with no warnings. Never invent game data: prove data links with a
read-only probe (`tools/`) and keep the output in `docs/evidence/`. Unknown is not zero. Native memory is read on the framework thread and copied;
native strings are read with a bound. UI text lives in `Localization/Languages/*.json` (English is the reference). AGPL references (Mappy,
SortaKinda) are read, never copied; this project is GPL-3.0. No ACT/parsers, no combat or PvP advantage. Add a [CHANGELOG.md](CHANGELOG.md) entry.
Official Dalamud publishing follows D17 rules and the AI policy: AI use must be disclosed, fully AI-generated submissions are rejected, and the
author must understand and test the code. See the French section « Publication Dalamud » above.
