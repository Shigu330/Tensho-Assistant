# Publication sur le dépôt officiel Dalamud (D17)

Relevé des règles le 2026-10-31 (non relu depuis) : [soumission](https://dalamud.dev/plugin-publishing/submission),
[restrictions](https://dalamud.dev/plugin-publishing/restrictions), [politique IA](https://dalamud.dev/plugin-publishing/ai-policy),
[métadonnées](https://dalamud.dev/plugin-development/plugin-metadata),
[considérations techniques](https://dalamud.dev/plugin-development/technical-considerations). Audit du code : PUBLISH-018 (0.156.0) ;
liste ci-dessous relue contre le code le 2026-11-02 (0.315).
La décision de soumettre appartient à l'auteur ; l'alternative est un dépôt de plugins personnel (URL ajoutée dans les réglages de Dalamud).
Décision de l'auteur (2026-11-02, DEC-023) : soumission à la version 1.0.0 ; avant, cadres et textures du jeu (ULD) ; traductions
allemande et japonaise gardées dans le paquet (partielles, repli anglais).
Ce dépôt personnel existe : `https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/repo.json`, mis à jour par
`Verify.ps1 -Release` (dernière release publiée selon les tags git : `Stable-0.242.0`).

## État de la liste de contrôle

| Exigence | État | Où |
|---|---|---|
| Manifeste JSON : Name, Author, Punchline, Description (obligatoires) | fait (0.156, anglais) ; **Description à mettre à jour** : elle ne cite ni l'Équipement, ni la Compagnie libre, ni les Rappels, ni les sources GitHub (GatherBuddy, LuminaSupplemental, FFXIVGachaSpreadsheet) et Teamcraft | `src/TenshoAssistant/TenshoAssistant.json` |
| InternalName, AssemblyVersion, DalamudApiLevel laissés à DalamudPackager | fait (0.156) ; contrôlé par `Verify.ps1` | idem |
| Version déterministe | fait : seulement `<Version>` du csproj | `TenshoAssistant.csproj` |
| Fenêtres normales via le système de fenêtres Dalamud | fait : fenêtre principale et carte Tensho dans `WindowSystem` (0.156). Restent en `ImGui.Begin` les panneaux accrochés à une fenêtre du jeu : import de banque, aide du carnet d'artisanat, survol d'objet, statistiques à côté de la fenêtre Personnage, butin à côté de l'outil de mission, Revue de mode, aides à la récolte et à la pêche, demande de Triple Triade ; dessins par-dessus le jeu (`GetForegroundDrawList`) pour le Mini Cactpot, le conseil et le rappel de deck de Triple Triade ; infobulle de l'alarme de la barre du serveur. **À justifier dans la PR** | `MainWindow.*.cs`, `BankImportOverlay.cs`, `MapViewWindow.cs` |
| Pas de requête automatique au serveur du jeu | conforme sur clic : téléportation, application du tri, chaîne de macros, écriture des macros d'artisanat dans les macros du jeu (0.191), cabine d'essayage (0.310). **À justifier** : alarmes du jeu créées par la commande `/alarm` (`UIModule.ProcessChatBoxEntry`, 0.205) sur clic, ou d'office quand le réglage « alarmes du jeu » est actif (désactivé par défaut), une commande toutes les 0,5 s par défaut (délai réglable de 0,25 à 5 s, Paramètres › Avancé), jamais plus de 30 alarmes du jeu | `Alarms/AlarmService.Dalamud.cs`, `Macros/MacroWriter.Dalamud.cs`, `Items/TryOn.Dalamud.cs` |
| Pas d'automatisation (artisanat, dialogues, butin…) | conforme : simulateur et solveur sans exécution | — |
| Pas d'avantage en combat ni JcJ, pas de parser | conforme (ACT retiré, DEC-009) ; la carte se masque en combat sur option | — |
| Identifiants de compte : seulement le sien | conforme : seul `player.ContentId` (personnage local) sert de clé | lecteurs `*.Dalamud.cs` |
| Requêtes réseau minimales, HTTPS, nom d'hôte | conforme : Universalis (monde + objet, annonces et historique sur la page Marché), Garland Tools, wiki, Teamcraft (temps de touche, une requête GraphQL par lieu de pêche), données hébergées sur GitHub (LuminaSupplemental, GatherBuddy, FFXIVGachaSpreadsheet) ; lancées par une page ouverte, un survol ou un réglage, cache disque ; aucune donnée du joueur envoyée. Exception choisie par le joueur (R-12, 0.319) : webhook Discord désactivé par défaut, adresse saisie par le joueur (hôtes de Discord seulement), POST HTTPS du message d'une alarme qui sonne, nom du personnage seulement sur option, adresse jamais journalisée. **À présenter dans la PR** ; en cas de doute, demander à l'équipe Dalamud avant la soumission | [REFERENCES](REFERENCES.md), [SECURITY](../../SECURITY.md) |
| Pas de télémétrie | conforme : aucune | — |
| Icône `images/icon.png` 1:1, 64–512 px | fait (0.157) : 512×512, générée par l'agent IA, **déclarée** dans la description (la politique préfère une icône faite main) | `src/TenshoAssistant/images/icon.png`, `publish/d17/…/images/icon.png` |
| `manifest.toml` dans `testing/live/TenshoAssistant/` | prêt (0.157) ; **hash du commit à renseigner** après push ; **`changelog` à mettre à jour** (texte de la première soumission, sans les fonctions récentes) | `publish/d17/testing/live/TenshoAssistant/manifest.toml` |
| Déclaration d'usage de l'IA dans la PR | brouillon (niveau « Auto », relu contre le code le 2026-11-02) ; **à relire et reformuler par l'auteur** (texte humain exigé) | `publish/d17/PULL_REQUEST.md` |
| L'auteur comprend, a testé et sait expliquer le code | **à l'auteur** ; les soumissions de niveau Copilot/Auto sont examinées à la discrétion des mainteneurs | — |
| Traductions assistées par IA relues par des locuteurs | allemand et japonais : quelques dizaines de clés, non relues ; anglais et français complets | `src/TenshoAssistant/Localization/Languages/` |
| Données embarquées sous licence compatible, avis conservé | seuils des secteurs de SubmarineTracker : fait (MIT, avis complet dans `submarine-breakpoints.csv`) ; butin moyen de FFXIVGachaSpreadsheet (MIT, `tools/submarine-loot.py`) : licence et copyright seulement dans le champ `Source` de `submarine-loot.json`, **texte MIT complet à ajouter (à décider)** ; table de Character Panel Refined (MIT) : seulement un commentaire dans `Character/StatFormulas.cs` ; crédits dans le README | `src/TenshoAssistant/FreeCompany/Data/` |

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
soumission ; chaque mise à jour = nouvelle PR qui change au moins `commit`. Les nouveaux plugins vont dans `testing/live/`.

## Décisions

Décidé le 2026-10-31 (demande de l'auteur) : nom affiché « Tensho Assistant » ; niveau d'IA « Auto » ; icône générée par IA et déclarée ;
dépôt officiel. Reste à l'auteur : commit + push, hash dans `manifest.toml`, fork de goatcorp/DalamudPluginsD17, copie de
`publish/d17/testing/live/TenshoAssistant/`, PR avec un texte relu et reformulé par lui.
Points ouverts avant D17 (STATUS, 2026-11-02) : icônes de quête 71141 / 71151 seulement sur relevé en jeu ; Description du manifeste
et `changelog` du `manifest.toml` à mettre à jour ; justification des panneaux `ImGui.Begin` et des commandes `/alarm`.
